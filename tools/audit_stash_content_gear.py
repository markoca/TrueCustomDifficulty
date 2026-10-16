#!/usr/bin/env python3
"""Static evidence for optional stash contents and gear; game runtime unverified."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = root / "src" / "TrueCustomDifficulty"
data = source / "Data"
catalog = json.loads((data / "parameters.json").read_text(encoding="utf-8"))
baseline = json.loads((data / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
by_id = {p["id"]: p for p in catalog["parameters"]}
mix = ["loot.stashMix" + v for v in
       ("Ammo", "Medical", "Food", "Drinks", "Grenades", "Coupons")]
gear = [f"loot.stashGear{t}{g}" for t in
        ("Cheap", "Medium", "Expensive") for g in ("Weapon", "Armor")]
ids = mix + gear
contract = baseline["special"]["stashContentAndGearProduction"]
assert len(ids) == 12 and contract["parameterIds"] == ids
assert len(catalog["parameters"]) == baseline["supportedParameterCount"] == 260
assert all(x in by_id and by_id[x]["tab"] == "loot" and
           by_id[x]["control"] == "slider" and
           by_id[x]["support"]["status"] == "runtime_test_required" for x in ids)
assert all(by_id[x]["slider"]["min"] == 0 and
           by_id[x]["slider"]["max"] == 3 and
           by_id[x]["slider"]["step"] == (.25 if x in mix else 1)
           for x in ids)
assert all(by_id[x]["vanilla"]["All"] == (1 if x in mix else 0)
           for x in ids)
writers = (source / "Services" / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
tuning = (source / "Services" / "CfgPatchWriterService.StashTuning.cs").read_text(encoding="utf-8")
vm = (source / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")
ui = (source / "Controls" / "AggregateVisualControl.cs").read_text(encoding="utf-8")
assert all(f'"{x}"' in tuning and f'"{x}"' in vm and f'"{x}"' in ui for x in ids)
assert all(f'MarkModifiedStashTuning(modifiedById, "{tier}"' in writers and
           f'"{tier}", config.TemplateCategories.Count' in writers
           for tier in ("Cheap", "Medium", "Expensive"))
assert all(name in writers for name in ("TuneStashSelectionField", "WriteExtraStashGear",
                                       "StashMixParameterIds", "StashGearParameterIds"))
assert "Math.Clamp(source * bias, 0.0, 1.0)" in tuning
assert "StashTuningCurrent(all, biasId, 1)" in tuning
assert "Source" not in contract or contract["source"] == "GameData/ItemGeneratorPrototypes.cfg"
for tier, name, n in (("Cheap", "cheapStashProduction", 12),
                      ("Medium", "mediumStashAmmoProduction", 10),
                      ("Expensive", "expensiveStashProduction", 19)):
    assert len(baseline["special"][name]["templateCategories"]) == n
for p in (source / "Presets").glob("*.json"):
    document = json.loads(p.read_text(encoding="utf-8"))
    for key in ids:
        assert document["values"][key] == {
            "kind": "slider", "number": 1 if key in mix else 0
        }, (p.name, key)
for path in ("UI-DESIGN-LOCK.md", "VISUAL-CONTROL-SYSTEM.md",
             "STASH-CONTENT-GEAR-IMPLEMENTATION.md"):
    assert (root / "docs" / path).is_file(), path
print("PASS: 12 new stash source IDs, 3 clone writers, source matrix, UI, "
      "and 12 neutral factory profiles structurally mapped.")
print("NOT RUN: Windows C# production build, Wine UI or game drop-frequency acceptance.")
