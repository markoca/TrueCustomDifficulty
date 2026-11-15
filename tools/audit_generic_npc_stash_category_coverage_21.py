#!/usr/bin/env python3
"""Audit 33 repeatable-human loadouts against the three 2.1 generic stash tiers.

Read-only stdlib check. Source catalogs contain authored row/consumer evidence,
not measurements of which items the game engine actually drops.
"""
import csv
import io
import re
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
NPC_PATH = "docs/generators/npc-generic-33-item-category-coverage-2.1.csv"
STASH_PATH = "docs/generators/stash-basic-standard-rare-item-category-coverage-2.1.csv"


def source(path):
    return (ROOT / path).read_text(encoding="utf-8")


def csv_rows(path):
    return list(csv.DictReader(io.StringIO(source(path))))


def fail_if(condition, message):
    if condition:
        raise AssertionError(message)


npc = csv_rows(NPC_PATH)
stash = csv_rows(STASH_PATH)
manifest = csv_rows("src/TrueCustomDifficulty/Data/npc-medical-production-redirect-manifest.csv")
chance = csv_rows("docs/generators/npc-standard-consumable-original-chances-2.1.csv")
branches = csv_rows("docs/generators/stash-generator-branches.csv")
weapon_audit = source("docs/generators/NPC-WEAPONS-AMMO-AUDIT.md")
equipment_audit = source("docs/generators/NPC-EQUIPMENT-ARTIFACTS-AUDIT.md")

normal_roots = {r["prototype"] for r in manifest}
fail_if(len(manifest) != 33 or len(normal_roots) != 33, "Standard-human manifest drift")
fail_if(len(npc) != 33 or {r["generator_sid"] for r in npc} != normal_roots,
        "NPC CSV does not match exact 33 generic roots")
fail_if(len(stash) != 3 or {r["tier"] for r in stash} != {"Basic", "Standard", "Rare"},
        "Stash-tier CSV is not Basic / Standard / Rare")

by_generator = {r["generator_sid"]: r for r in npc}
by_pool = defaultdict(set)
for entry in chance:
    by_pool[entry["PoolSID"]].add(entry["ItemPrototypeSID"])
fail_if(len(chance) != 104 or len(by_pool) != 6, "104 Chance rows / 6 pools drift")

MEDICAL = {"Bandage", "Medkit", "ArmyMedkit", "AntiRad"}
FOOD = {"Bread", "Sausage", "CannedFood"}
DRINKS = {"Energetic", "Beer", "Vodka", "Water", "Milk"}


def sid_set(raw):
    return set(filter(None, raw.split(";")))


for entry in manifest:
    actual = by_generator[entry["prototype"]]
    fail_if(actual["pool_sid"] != entry["source_value"],
            "Wrong consumable pool at " + entry["prototype"])
    fail_if(actual["runtime_consumers"] != entry["external_consumers"],
            "Consumer mismatch at " + entry["prototype"])
    possible = by_pool[entry["source_value"]]
    for field, category in [("medical_sids", MEDICAL),
                            ("food_sids", FOOD), ("drink_sids", DRINKS)]:
        fail_if(sid_set(actual[field]) != possible & category,
                "Consumable SID mismatch in " + field + ": " + entry["prototype"])

# Weapon-summary rows are the complete direct local weapon and attached-ammo
# scope; this must not be confused with the 43 *bonus* NPC_Ammo_Smart rows.
start = weapon_audit.index("## Direct standard-human loadout summary")
end = weapon_audit.index("## Complete direct weapon-entry table", start)
weapon_rows = {}
for line in weapon_audit[start:end].splitlines():
    match = re.match(r"^\| `([^`]+)` \| (.*?) \| (\d+) \|", line)
    if match and match.group(1) in normal_roots:
        weapon_rows[match.group(1)] = int(match.group(3))
fail_if(set(weapon_rows) != normal_roots, "Weapon local generator inventory drift")
fail_if(sum(weapon_rows.values()) != 121, "121 original weapon rows drift")
for sid, count in weapon_rows.items():
    fail_if(int(by_generator[sid]["weapon_rows"]) != count,
            "Weapon row mismatch: " + sid)

# The equipment table's third column identifies raw direct categories.
# Category Artifact here is grenade, never an anomaly artifact.
start = equipment_audit.index("## Full direct standard-human row table")
end = equipment_audit.index("## Full nested-link table", start)
actual_counts = defaultdict(Counter)
grenade_sids = defaultdict(set)
for line in equipment_audit[start:end].splitlines():
    match = re.match(
        r"^\| `([^`]+)` \| .*? \| "
        r"(Armor|Helmet|Detector|Grenade \(CFG category Artifact\)) \|", line)
    if not match or match.group(1) not in normal_roots:
        continue
    sid, kind = match.groups()
    fields = line.split(" | ")
    if kind == "Grenade (CFG category Artifact)":
        if fields[7] not in ("", "—"):
            actual_counts[sid]["grenade_weight_rows"] += 1
        elif fields[8] not in ("", "—"):
            actual_counts[sid]["grenade_chance_rows"] += 1
        else:
            raise AssertionError("Grenade without native selection field: " + sid)
        grenade_sids[sid].add(fields[6].strip("`"))
    else:
        fld = {"Armor": "direct_armor_rows", "Helmet": "direct_helmet_rows",
               "Detector": "detector_rows"}[kind]
        actual_counts[sid][fld] += 1

for sid in normal_roots:
    record = by_generator[sid]
    for column in ("grenade_weight_rows", "grenade_chance_rows",
                   "direct_armor_rows", "direct_helmet_rows", "detector_rows"):
        fail_if(int(record[column]) != actual_counts[sid][column],
                column + " mismatch for " + sid)
    fail_if(sid_set(record["grenade_sids"]) != grenade_sids[sid],
            "Grenade SID mismatch for " + sid)

fail_if(sum(int(x["grenade_chance_rows"]) for x in npc) != 32 or
        sum(int(x["grenade_weight_rows"]) for x in npc) != 2,
        "Generic NPC grenades must retain 32 native Chance + 2 Weight rows")
fail_if(sum(int(x["direct_armor_rows"]) for x in npc) != 41 or
        sum(int(x["direct_helmet_rows"]) for x in npc) != 5,
        "Generic NPC local armor/head row inventory drift")
fail_if(sum(int(x["detector_rows"]) for x in npc) != 32,
        "Historical detector evidence changed; no Echo control is authorized")

# All nested source SID links are actual source edges, not permission to patch
# shared faction armor or the source Rare stash parent globally.
start = equipment_audit.index("## Full nested-link table")
end = equipment_audit.index("## Full nested-pool item rows", start)
seen_nested = defaultdict(lambda: {"armor_pool_links": set(),
                                   "nvg_pool_links": set(),
                                   "special_shared_stash_links": set()})
for line in equipment_audit[start:end].splitlines():
    match = re.match(r"^\| `([^`]+)` \| .*? \| `([^`]+)` \|", line)
    if not match or match.group(1) not in normal_roots:
        continue
    sid, target = match.groups()
    col = ("armor_pool_links" if target.endswith("_Armor") else
           "nvg_pool_links" if target.endswith("_NVG") else
           "special_shared_stash_links")
    seen_nested[sid][col].add(target)
for sid in normal_roots:
    for column in ("armor_pool_links", "nvg_pool_links", "special_shared_stash_links"):
        fail_if(sid_set(by_generator[sid][column]) != seen_nested[sid][column],
                "Nested dependency mismatch at " + sid + ":" + column)
fail_if(sum(bool(x["special_shared_stash_links"]) for x in npc) != 1,
        "Rare stash / NPC sniper shared edge drift")
fail_if(sum(bool(x["armor_pool_links"]) for x in npc) != 28,
        "Shared NPC armor link count drift")

parents = {
    "Basic": "GamePass_Stash_ItemGenerator_Cheap",
    "Standard": "GamePass_Stash_ItemGenerator_Common_Var1",
    "Rare": "GamePass_Stash_ItemGenerator_Rare",
}
tier_groups = {"Basic": 12, "Standard": 10, "Rare": 19}
tier_unique = {"Basic": 16, "Standard": 15, "Rare": 21}
itemgen = [x for x in branches if x["layer"] == "ItemGenerator"]
fail_if(len(itemgen) != 41, "Stash ItemGenerator 41 branch group inventory drift")
for record in stash:
    tier = record["tier"]
    entries = [x for x in itemgen if x["tier"] == tier]
    items = {sid for x in entries for sid in x["item_sids"].split(";") if sid}
    fail_if(record["source_sid"] != parents[tier] or
            len(entries) != tier_groups[tier] or
            int(record["source_groups"]) != len(entries) or
            len(items) != tier_unique[tier] or
            int(record["unique_sids"]) != len(items), "Stash tier drift: " + tier)
    matchers = {
        "medical_sids": MEDICAL,
        "food_sids": FOOD,
        "drink_sids": DRINKS,
    }
    for field, family in matchers.items():
        fail_if(sid_set(record[field]) != items & family,
                "Stash item category mismatch: " + tier + "/" + field)
    for field, predicate in (
        ("loose_ammo_sids", lambda x: bool(re.match(r"^A\d", x))),
        ("grenade_sids", lambda x: x.startswith("Grenade")),
        ("coupon_sids", lambda x: x.startswith("Money")),
    ):
        fail_if(sid_set(record[field]) != {x for x in items if predicate(x)},
                "Stash item category mismatch: " + tier + "/" + field)
    fail_if(bool(record["weapon_sids"] or record["armor_sids"]),
            "Native stash tiers must not inherit opt-in stash-gear item categories")

print("PASS: 33 NPC generators / 121 weapon rows / 104 consumable Chances; "
      "34 grenade rows (32 Chance, 2 Weight); 41 armor + 5 helmet direct rows; "
      "3 stash tiers (12/10/19 groups; 16/15/21 unique items).")
