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
print("OK: 14 authored weapon stashes; 6 gate-linked tier sources; rank/tier safe regular SIDs; 1+1 new-entry intent")
