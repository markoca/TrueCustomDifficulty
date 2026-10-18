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
for tier in ("Cheap", "Medium", "Expensive"):
    for kind in ("Weapon", "Armor"):
        p = catalog[f"loot.stashGear{tier}{kind}"]
        assert p["requiresToggle"] == "loot.stashGearEnabled"
        assert p["slider"] == {"min": 0, "max": 3, "step": 1, "stepMode": "absolute"}
assert 'if (!all.TryGetValue("loot.stashGearEnabled", out var master) || !master.CurrentBoolean)' in svc
assert 'category.SetAssignment("bAllowSameCategoryGeneration", "false")' in svc
assert 'item.SetAssignment("MinCount", "1")' in svc
assert 'item.SetAssignment("MaxCount", "1")' in svc
assert 'DrawTogglePill(dc, P("loot.stashGearEnabled")' in ui
# Every rank-specific SID in the writer maps to a rank and equipment cap
# no higher than the current player rank and the selected stash tier.
lines = re.findall(r'^\s*\("(Cheap|Medium|Expensive)", (true|false)\) => (.*),$', svc, re.M)
assert len(lines) == 6, lines
for tier, weapon, expr in lines:
    values = re.findall(r'"([^"]+)"', expr)
    assert len(values) in (1, 4)
    if len(values) == 1:
        values *= 4
    for rank, sid in enumerate(values, 1):
        row = ranks[sid]
        assert row["ItemClass"] == ("Weapon" if weapon == "true" else "Armor")
        item_tier = int(row["ProgressionTier"][1:])
        assert item_tier <= rank
        assert item_tier <= {"Cheap": 1, "Medium": 2, "Expensive": 4}[tier]
        assert not re.search(r"Gauss|RPG|Trophy", sid, re.I)

# Opt-in suppression has its own strict subset manifest; never infer this
# authority from a broad actor classification or from a shuffled pool.
with (data / "stash-authored-weapon-removal-manifest.csv").open(encoding="utf-8", newline="") as f:
    removals = list(csv.DictReader(f))
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
assert actual == approved, "Weapon-removal roster differs from audited original regular entries"
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
assert len(catalog["loot.stashGearEnabled"]["targets"]) == 2
assert any(t["operation"] == "removeNode" for t in catalog["loot.stashGearEnabled"]["targets"])

print("OK: 13 exact regular authored weapon removal nodes, Trophy/STC excluded; six tier strengths gated and rank-safe; native cap still unverified")
