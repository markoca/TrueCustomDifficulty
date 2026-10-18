#!/usr/bin/env python3
"""Static guard for opt-in extra stash gear and documented authored weapon slots.

This verifies catalog/code source consistency; it does NOT simulate native
multi-generator inventory or establish an absolute per-physical-stash cap.
"""
import csv
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
data = root / "src/TrueCustomDifficulty/Data"
svc = (root / "src/TrueCustomDifficulty/Services/CfgPatchWriterService.StashTuning.cs").read_text(encoding="utf-8")
ui = (root / "src/TrueCustomDifficulty/Controls/AggregateVisualControl.cs").read_text(encoding="utf-8")
catalog = {row["id"]: row for row in json.loads((data / "parameters.json").read_text(encoding="utf-8"))["parameters"]}
ranks = {row["ItemSID"]: row for row in csv.DictReader((data / "gear-progression-ranks.csv").open(encoding="utf-8", newline=""))}
with (root / "docs/generators/stash-gear-unique-randomization-backlog.csv").open(encoding="utf-8", newline="") as f:
    weapons = [row for row in csv.DictReader(f) if row["FuturePool"] == "WEAPON"]
with (data / "physical-stash-safety-catalog.csv").open(encoding="utf-8", newline="") as f:
    statuses = {row["actor_sid"]: row["safety_class"] for row in csv.DictReader(f)}
assert len(weapons) == 14
assert len({row["ActorSID"] for row in weapons}) == 14
assert len({row["ItemPrototypeSID"] for row in weapons}) == 9
assert all(statuses[row["ActorSID"]] == "REGULAR_SMARTLOOT" for row in weapons)
assert catalog["loot.stashGearEnabled"]["control"] == "toggle"
assert catalog["loot.stashGearEnabled"]["vanilla"]["All"] is False
p = catalog["loot.stashGearCoveragePercent"]
assert p["requiresToggle"] == "loot.stashGearEnabled"
assert p["slider"] == {"min": 0, "max": 100, "step": 1, "stepMode": "absolute"}
coverage = (root / "src/TrueCustomDifficulty/Services/CfgPatchWriterService.StashGearCoverage.cs").read_text(encoding="utf-8")
assert 'P("loot.stashGearCoveragePercent")' in ui
assert "WriteExtraStashGear(" not in svc
assert "StashGearStableHash(" in coverage and "SelectStashGearActors(" in coverage
assert "LoadStashGearPool(" in coverage and "LoadStashGearActors(" in coverage
assert 'item.SetAssignment("MinCount", "1")' in coverage
assert 'item.SetAssignment("MaxCount", "1")' in coverage

# Opt-in suppression has its own strict subset manifest; never infer this
# authority from a broad actor classification or from a shuffled pool.
with (data / "stash-authored-weapon-removal-manifest.csv").open(encoding="utf-8", newline="") as f:
    removals = list(csv.DictReader(f))
with (data / "stash-weapon-source-inventory.csv").open(encoding="utf-8", newline="") as f:
    inventory = list(csv.DictReader(f))
from collections import Counter
assert len(inventory) == 39
assert Counter(row["status"] for row in inventory) == {
    "REMOVE_REGULAR": 13, "KEEP_QUEST": 16,
    "KEEP_UNIQUE_REVIEW": 1, "KEEP_PROTECTED": 1, "KEEP_DLC_SPECIAL_REVIEW": 8
}, "Full source inventory status drift"
assert len({(row["origin"], row["actor_sid"], row["generator_sid"],
            row["category_index"], row["item_index"], row["item_sid"])
            for row in inventory}) == 39
approved_inventory = {
    (row["actor_sid"], row["generator_sid"], row["item_sid"],
     f'ItemGenerator/[{row["category_index"]}]/PossibleItems/[{row["item_index"]}]')
    for row in inventory if row["status"] == "REMOVE_REGULAR"
}
excluded_inventory = [row for row in inventory if row["status"] != "REMOVE_REGULAR"]
assert all(row["actor_sid"] not in {entry["ActorSID"] for entry in removals}
           and row["generator_sid"] not in {entry["GeneratorSID"] for entry in removals}
           for row in excluded_inventory), "Protected or quest actor leaked into production"

with (data / "world-weapon-shuffle-manifest.csv").open(encoding="utf-8", newline="") as f:
    shuffle = {row["generator_sid"]: row for row in csv.DictReader(f)}
with (data / "smartloot-regular-world-actor-manifest.csv").open(encoding="utf-8", newline="") as f:
    ordinary = {row["ActorSID"] for row in csv.DictReader(f)}
with (data / "base-stash-protected-actor-manifest.csv").open(encoding="utf-8", newline="") as f:
    protected = {row["ActorSID"] for row in csv.DictReader(f)}
approved = {(row["ActorSID"], row["GeneratorSID"], row["ItemPrototypeSID"],
             f'ItemGenerator/[{row["CategoryIndex"]}]/PossibleItems/[{row["ItemIndex"]}]')
            for row in weapons if row["ItemPrototypeSID"] != "Gun_Trophy_AR"}
actual = {(row["ActorSID"], row["GeneratorSID"], row["ItemPrototypeSID"], row["NodePath"])
          for row in removals}
assert len(approved) == len(actual) == len(removals) == 13, "Unsafe weapon-removal count drift"
assert actual == approved == approved_inventory, "Removal manifest differs from source-wide reviewed inventory"
assert all(row["ActorSID"] in ordinary and row["ActorSID"] not in protected and
           statuses[row["ActorSID"]] == "REGULAR_SMARTLOOT" and
           row["GeneratorSID"].endswith("_" + row["ActorSID"]) and
           row["NodePath"] == "ItemGenerator/[1]/PossibleItems/[0]" and
           shuffle[row["GeneratorSID"]]["item_sid"] == row["ItemPrototypeSID"]
           for row in removals), "Unverified original weapon target or protected stash"
assert not any(row["ItemPrototypeSID"] == "Gun_Trophy_AR" for row in removals)
assert "464E8FB14AEEC7B154602AB22BEA1EBC" in protected, "Protected STC Malachite missing"
suppression = (root / "src/TrueCustomDifficulty/Services/CfgPatchWriterService.StashAuthoredWeapons.cs").read_text(encoding="utf-8")
writer = (root / "src/TrueCustomDifficulty/Services/CfgPatchWriterService.cs").read_text(encoding="utf-8")
csproj = (root / "src/TrueCustomDifficulty/TrueCustomDifficulty.csproj").read_text(encoding="utf-8")
assert 'WriteStashAuthoredWeaponRemovals(allById, documents)' in writer
assert 'document.AddRemovalNode(row.GeneratorSid, path[..^1], path[^1]);' in suppression
assert 'LoadSmartStashActors()' in suppression and 'BaseProtectedStashActors.Value' in suppression
assert 'shuffle.CurrentBoolean' in suppression and 'cannot both be enabled' in suppression
assert 'stash-authored-weapon-removal-manifest.csv' in csproj
assert 'stash-weapon-source-inventory.csv' in csproj
assert 'CompleteStashWeaponInventoryResource' in suppression
assert len(catalog["loot.stashGearEnabled"]["targets"]) == 2
assert any(t["operation"] == "removeNode" for t in catalog["loot.stashGearEnabled"]["targets"])

print("OK: 39 reviewed weapon source entries; exactly 13 approved regular removals; 26 protected/quest/review untouched")
