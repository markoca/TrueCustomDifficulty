#!/usr/bin/env python3
"""Validate the pinned 2.1 normal corpse/world stash generator ownership map."""
import csv
import io
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
def read(path):
    return (ROOT / path).read_text(encoding="utf-8")

def rows(path):
    return list(csv.DictReader(io.StringIO(read(path))))

manifest_all = rows("src/TrueCustomDifficulty/Data/npc-medical-production-redirect-manifest.csv")
assert len(manifest_all) == 92, "Ordinary-human manifest is not the 92-root 2.1 scope"
# The ownership catalog classifies the first 33 (original guard/custom)
# roots; the 59 roaming GeneralNPC_* roots appended for 2.1 are checked by
# tools/audit_npc_generalnpc_scope_21.py against the original ZIPs.
manifest = manifest_all[:33]
catalog = rows("docs/generators/generic-corpse-stash-generator-ownership-2.1.csv")
branches = rows("docs/generators/stash-generator-branches.csv")
corpse = read("docs/generators/CORPSE-OVERRIDE-AUDIT.md")
footprint = read("docs/generators/ITEM-GENERATOR-USAGE-FOOTPRINT.md")
world_audit = read("docs/generators/SMARTLOOT-WORLD-AUDIT.md")
document = read("docs/generators/GENERIC-CORPSE-STASH-GENERATOR-OWNERSHIP-2.1-2026-11-15.md")

expected_counts = {
    "GENERIC_HUMAN_LOADOUT": 33,
    "GENERIC_HUMAN_CONSUMABLE_POOL": 6,
    "NPC_CORPSE_BONUS_SMARTLOOT": 3,
    "WORLD_STASH_TIER_SOURCE": 3,
    "WORLD_STASH_TIER_WRAPPER": 12,
    "WORLD_STASH_SMARTLOOT": 13,
    "PHYSICAL_CORPSE_OVERRIDE_SHARED": 31,
    "PHYSICAL_CORPSE_OVERRIDE_ONLY": 8,
    "UNRESOLVED_CORPSE_NAMED_SMARTLOOT": 2,
    "WORLD_DESTRUCTIBLE_NOT_STASH_TIER": 12,
}
counts = Counter(row["owner"] for row in catalog)
assert counts == expected_counts, (counts, expected_counts)
assert len(catalog) == 123
all_sids = [row["source_sid"] for row in catalog]
assert len(all_sids) == len(set(all_sids)), "Classification SID occurs under more than one ownership"
def group(name):
    return {row["source_sid"] for row in catalog if row["owner"] == name}

npc_sources = {row["prototype"] for row in manifest}
npc_pools = {row["source_value"] for row in manifest}
assert len(manifest) == 33 and len(npc_sources) == 33 and len(npc_pools) == 6
assert npc_sources == group("GENERIC_HUMAN_LOADOUT")
assert npc_pools == group("GENERIC_HUMAN_CONSUMABLE_POOL")
assert group("NPC_CORPSE_BONUS_SMARTLOOT") == {
    "NPC_Medicine_Smart", "NPC_Ammo_Smart", "NPC_Water_Smart"}

tier = {
    "GamePass_Stash_ItemGenerator_Cheap",
    "GamePass_Stash_ItemGenerator_Common_Var1",
    "GamePass_Stash_ItemGenerator_Rare",
}
assert tier == group("WORLD_STASH_TIER_SOURCE")
rank_wrappers = {f"Stash_{tier}_{rank}"
    for tier in ("Cheap", "Medium", "Expensive")
    for rank in ("Newbie", "Experienced", "Veteran", "Master")}
assert group("WORLD_STASH_TIER_WRAPPER") == rank_wrappers

branch_parents = {r["source_sid"] for r in branches if r["layer"] == "ItemGenerator"}
branch_world = {r["source_sid"] for r in branches if r["layer"] == "SmartLoot"}
assert branch_parents == tier and len(branches) == 463
assert len(branch_world) == 13 and branch_world == group("WORLD_STASH_SMARTLOOT")
assert sum(r["layer"] == "ItemGenerator" for r in branches) == 41
assert sum(r["layer"] == "SmartLoot" for r in branches) == 422

# Only parsed 'direct corpse override targets' inventory rows are authoritative.
start = corpse.index("## All 39 direct corpse override targets")
end = corpse.find("\n## ", start + 5)
body = corpse[start:end if end >= 0 else None]
overrides = {}
for line in body.splitlines():
    if not line.startswith("| `"):
        continue
    cells = [cell.strip() for cell in line.split("|")]
    sid = cells[1].strip("`")
    overrides[sid] = cells[4]
assert len(overrides) == 39
assert group("PHYSICAL_CORPSE_OVERRIDE_ONLY") == {
    sid for sid, context in overrides.items() if context == "corpse_override"}
assert group("PHYSICAL_CORPSE_OVERRIDE_SHARED") == {
    sid for sid, context in overrides.items() if context != "corpse_override"}
assert not npc_sources.intersection(overrides), "Normal human roots overlap physical corpse override roots"

assert group("UNRESOLVED_CORPSE_NAMED_SMARTLOOT") == {
    "StashMedicine_Corpse", "StashVodka_Corpse"}
assert "no resolved external consumer" in world_audit
assert all(f"`{sid}`" in footprint
    for sid in group("WORLD_DESTRUCTIBLE_NOT_STASH_TIER"))
assert "Echo Detector Chance" in document
assert "Echo Detector Chance" in document and "must not" in document
assert all("NO_UI" in row["writer_policy"]
    for row in catalog if row["owner"] == "UNRESOLVED_CORPSE_NAMED_SMARTLOOT")
assert all(row["writer_policy"] == "EXCLUDE_FROM_GENERIC_CORPSE_CONTROLS"
    for row in catalog if row["owner"].startswith("PHYSICAL_CORPSE_OVERRIDE"))
print("PASS: 123 distinct generators/prototypes; ordinary NPC 33+6+3; "
      "world stashes 3+12+13; corpse override 31 shared/8 special; "
      "2 unresolved; 12 destructible; no Echo UI.")
