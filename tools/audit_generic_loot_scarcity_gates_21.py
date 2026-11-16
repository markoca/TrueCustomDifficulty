#!/usr/bin/env python3
"""Read-only 2.1 audit: group-level scarcity gates versus within-group Weight mix.

Fails closed on original source inventory drift. Does not claim engine acceptance.
"""
import csv
import json
from collections import Counter, defaultdict
from fractions import Fraction
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text(encoding="utf-8")


def csv_rows(path):
    with (ROOT / path).open(encoding="utf-8", newline="") as stream:
        return list(csv.DictReader(stream))


def native_num(raw):
    return float(raw.rstrip("fF"))


def norm(values):
    total = sum(values)
    assert total > 0, "Cannot normalize an all-zero group"
    return tuple(value / total for value in values)


baseline = json.loads(read("src/TrueCustomDifficulty/Data/cfg-writer-baseline.json"))
world = csv_rows("src/TrueCustomDifficulty/Data/smartloot-world-source-entries.csv")
classified = csv_rows("docs/generators/generic-loot-weight-group-gates-2.1.csv")
source = baseline["special"]
expected = {}
parent = {
    "Basic": ("cheapStashProduction", "GamePass_Stash_ItemGenerator_Cheap"),
    "Standard": ("mediumStashAmmoProduction", "GamePass_Stash_ItemGenerator_Common_Var1"),
    "Rare": ("expensiveStashProduction", "GamePass_Stash_ItemGenerator_Rare"),
}

for tier, (config, sid) in parent.items():
    categories = source[config]["templateCategories"]
    for category in categories:
        fields = category["fields"]
        items = category["items"]
        selections = {field for item in items for field in ("Chance", "Weight")
                      if field in item["fields"]}
        assert len(selections) == 1, (tier, category["node"], selections)
        # These are exact group-level source fields. There is NO independent
        # roll Chance/SpawnChance gate on any of the 41 tier groups.
        assert set(fields) <= {
            "Category", "Diff", "PlayerRank", "bAllowSameCategoryGeneration"
        }, (tier, fields)
        assert not set(fields) & {
            "Chance", "Weight", "MinSpawnChance", "MaxSpawnChance",
            "ItemSetCount", "MinItemCount", "MaxItemCount"
        }
        for item in items:
            f = item["fields"]
            if "Weight" in f:
                assert int(f["MinCount"]) > 0 and int(f["MaxCount"]) > 0
        key = ("STASH_ITEMGEN", sid, tier, fields.get("PlayerRank", ""), category["node"])
        expected[key] = {
            "selection_field": next(iter(selections)),
            "item_rows": str(len(items)),
            "item_sids": ";".join(dict.fromkeys(
                item["fields"]["ItemPrototypeSID"] for item in items)),
            "native_group_activation_field": "",
            "source_min_spawn": "",
            "source_max_spawn": "",
            "source_item_set_count": "",
            "scarcity_gate_status": "NOT_AUTHORED_AT_GROUP_LEVEL",
            "group_family": fields["Category"],
            "rank_gate": fields.get("PlayerRank", ""),
            "difficulty_gate": fields.get("Diff", "")
        }

world_groups = defaultdict(list)
for row in world:
    key = (row["Prototype"], row["RankIndex"],
           row["Group"], row["GroupIndex"])
    world_groups[key].append(row)

assert len(world) == 991 and len(world_groups) == 422
for (sid, rank_index, family, index), items in world_groups.items():
    first = items[0]
    fields = {("Chance" if row["Chance"] else "Weight") for row in items}
    assert len(fields) == 1 and all(row["Weight"] or row["Chance"] for row in items)
    assert all((row["Rank"], row["MinSpawnChance"], row["MaxSpawnChance"],
                row["ItemSetCount"]) ==
               (first["Rank"], first["MinSpawnChance"], first["MaxSpawnChance"],
                first["ItemSetCount"]) for row in items)
    assert 0 <= native_num(first["MinSpawnChance"]) <= native_num(first["MaxSpawnChance"]) <= 1
    positive = native_num(first["MaxSpawnChance"]) > 0
    key = ("WORLD_SMARTLOOT", sid, "", first["Rank"], family + "[" + index + "]")
    expected[key] = {
        "selection_field": next(iter(fields)),
        "item_rows": str(len(items)),
        "item_sids": ";".join(dict.fromkeys(row["ItemSID"] for row in items)),
        "native_group_activation_field": "MinSpawnChance;MaxSpawnChance",
        "source_min_spawn": first["MinSpawnChance"],
        "source_max_spawn": first["MaxSpawnChance"],
        "source_item_set_count": first["ItemSetCount"],
        "scarcity_gate_status": ("SOURCE_SPAWN_ENDPOINT_POSITIVE_TEST_REQUIRED"
                                if positive else "SOURCE_SPAWN_ENDPOINT_ZERO_PRESERVE"),
        "group_family": family,
        "rank_gate": first["Rank"],
        "difficulty_gate": ""
    }

assert len(expected) == 463, "Source audit must classify 41 + 422 groups"
actual = {}
for row in classified:
    key = tuple(row[name] for name in
                ("domain", "source_sid", "tier", "rank_gate", "source_group"))
    assert key not in actual, "Duplicate native group entry: " + str(key)
    actual[key] = row
assert set(actual) == set(expected), "Native group key coverage has drifted"
for key, expected_fields in expected.items():
    actual_row = actual[key]
    for field, value in expected_fields.items():
        assert actual_row[field] == value, (key, field, actual_row[field], value)

counts = Counter((r["domain"], r["selection_field"]) for r in actual.values())
assert counts == {
    ("STASH_ITEMGEN", "Weight"): 19,
    ("STASH_ITEMGEN", "Chance"): 22,
    ("WORLD_SMARTLOOT", "Weight"): 410,
    ("WORLD_SMARTLOOT", "Chance"): 12
}, counts
statuses = Counter(r["scarcity_gate_status"] for r in actual.values())
assert statuses == {
    "NOT_AUTHORED_AT_GROUP_LEVEL": 41,
    "SOURCE_SPAWN_ENDPOINT_POSITIVE_TEST_REQUIRED": 310,
    "SOURCE_SPAWN_ENDPOINT_ZERO_PRESERVE": 112
}, statuses
assert sum(r["selection_field"] == "Weight"
           and native_num(r["source_max_spawn"]) > 0
           for r in classified if r["domain"] == "WORLD_SMARTLOOT") == 300
assert sum(r["selection_field"] == "Weight"
           and native_num(r["source_max_spawn"]) == 0
           for r in classified if r["domain"] == "WORLD_SMARTLOOT") == 110
assert sum(r["source_item_set_count"] != ""
           for r in classified if r["domain"] == "WORLD_SMARTLOOT") == 30

npc = source["npcMedicalLootProduction"]["pools"]
assert len(npc) == 6
npc_items = [item["fields"] for pool in npc
             for category in pool["templateCategories"]
             for item in category["items"]]
assert len(npc_items) == 104
assert all("Chance" in item and "Weight" not in item for item in npc_items)
all_links = csv_rows("src/TrueCustomDifficulty/Data/npc-medical-production-redirect-manifest.csv")
assert len(all_links) == 92 and len({r["prototype"] for r in all_links}) == 92
# NPC-CONSUMABLES-AUDIT.md documents the first 33 (original guard/custom)
# links; the 59 roaming GeneralNPC_* links appended for 2.1 are verified
# against the original ZIPs by tools/audit_npc_generalnpc_scope_21.py.
links = all_links[:33]

# The original SubItemGenerator link for each of these 33 source roots
# uses Weight=1, not a direct Chance-to-run bundle gate.
link_audit = read("docs/generators/NPC-CONSUMABLES-AUDIT.md")
active = {}
for line in link_audit.splitlines():
    if not line.startswith("| `") or "ACTIVE STANDARD-NPC LINK" not in line:
        continue
    cells = [cell.strip() for cell in line.split("|")]
    active[cells[1].strip("`")] = cells[4]
assert len(active) == 37
for link in links:
    assert active[link["prototype"]] == "Weight=1", link["prototype"]

# Verify native source-weight normalization; 1/2 uniform Weight scaling
# does NOT reduce the probability of a group firing.
cheap = source["cheapStashProduction"]["templateCategories"][1]
assert cheap["node"] == "[1]"
bandage = food = drink = Fraction(0)
for item in cheap["items"]:
    sid = item["fields"]["ItemPrototypeSID"]
    weight = Fraction(item["fields"]["Weight"])
    if sid == "Bandage":
        bandage += weight
    elif sid in {"Bread", "Sausage", "CannedFood"}:
        food += weight
    elif sid == "Milk":
        drink += weight
    else:
        raise AssertionError("Unexpected source group SID: " + sid)
assert (bandage, food, drink) == (2, 13, 1)
source_mix = norm((bandage, food, drink))
assert source_mix == (Fraction(1, 8), Fraction(13, 16), Fraction(1, 16))
assert norm(tuple(x * Fraction(1, 2) for x in (bandage, food, drink))) == source_mix
assert norm((bandage, food / 2, drink)) == (
    Fraction(4, 19), Fraction(13, 19), Fraction(2, 19))

print("PASS: 41 tier groups (19 Weight / 22 Chance), no native group-roll gates;")
print("      422 world SmartLoot groups (410 Weight / 12 Chance),")
print("      310 positive MaxSpawnChance and 112 native zero-Max groups;")
print("      92 generic-NPC pool links (33 doc-audited here); 104 consumables use item Chance;")
print("      native Weight ratio is unchanged under uniform scaling.")
