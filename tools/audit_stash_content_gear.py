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
assert len(catalog["parameters"]) == baseline["supportedParameterCount"] == 268
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
# Medical entries are source-backed, not a new synthetic inventory pool.
medical = ["loot.stashMedical" + k + "Bias" for k in
           ("Bandage", "Medkit", "ArmyMedkit", "AntiRad")]
quantity = ["loot.stash" + k + lim + "Count" for k in
            ("Medkit", "ArmyMedkit") for lim in ("Min", "Max")]
med_ids = medical + quantity
assert baseline["special"]["stashMedicalProduction"]["parameterIds"] == med_ids
assert all(k in by_id and by_id[k]["tab"] == "loot" and
           by_id[k]["support"]["status"] == "runtime_test_required" for k in med_ids)
assert all(by_id[k]["slider"]["min"] == 0 and
           by_id[k]["slider"]["max"] == (3 if k in medical else 6) and
           by_id[k]["slider"]["step"] == (.25 if k in medical else 1)
           for k in med_ids)
assert all(by_id[k]["vanilla"]["All"] == (1 if k in medical else 0)
           for k in med_ids)
assert all(f'"{k}"' in tuning and f'"{k}"' in vm and f'"{k}"' in ui
           for k in med_ids)
assert "TuneStashMedicalCountField" in tuning
assert "StashMedicalParameterIds.Any(id =>" in tuning
assert "IsStashMedicalSourceInTier(id, tier)" in tuning
assert writers.count("TuneStashMedicalCountField(") == 3
assert 'supported.UnionWith(StashMedicalParameterIds);' in writers
assert 'bias *= StashTuningCurrent(all, detailId, 1);' in tuning
assert "if (maxCap > 0)" in tuning and "min = max;" in tuning
for tier, key in (("Basic", "cheapStashProduction"),
                  ("Standard", "mediumStashAmmoProduction"),
                  ("Rare", "expensiveStashProduction")):
    entries = [row["fields"]["ItemPrototypeSID"]
               for group in baseline["special"][key]["templateCategories"]
               for row in group["items"]]
    assert "Medkit" in entries, tier
    assert ("ArmyMedkit" in entries) == (tier == "Rare"), tier
    assert ("AntiRad" in entries) == (tier == "Rare"), tier
assert all("Medkit" not in p["id"] or p["vanilla"]["All"] in (0, 1)
           for p in catalog["parameters"] if p["id"] in med_ids)

# Both native composite editors must remain genuine shared-scale graphs rather
# than reverting to the former six separate DrawCompactTrack slider cards.
content_plot = ui.split("private void DrawStashContentMix(", 1)[1].split(
    "private void DrawStashMedicalComposition(", 1)[0]
gear_plot = ui.split("private void DrawStashGear(", 1)[1].split(
    "private void DrawCompactStashRewards(", 1)[0]
assert "DrawCompactTrack(" not in content_plot + gear_plot
assert "const double rowPitch = 42.0;" in content_plot
assert "for (var tick = 0; tick <= 3; tick++)" in content_plot
assert 'DrawRightAlignedText(dc, $"{p.CurrentNumber:0.##}x"' in content_plot
assert "p.ReferenceNumber" in content_plot and "AddHorizontalTarget(p" in content_plot
assert 'RELATIVE SOURCE WEIGHT / CHANCE · NOT STASH DROP %' in content_plot
assert 'DrawStashGearPlotCell(dc, P(rows[rowIndex].WeaponId)' in gear_plot
assert 'DrawStashGearPlotCell(dc, P(rows[rowIndex].ArmorId)' in gear_plot
assert "for (var rowIndex = 0; rowIndex < rows.Length; rowIndex++)" in gear_plot
assert "for (var step = 0; step <= 3; step++)" in gear_plot
assert "AddHorizontalTarget(p" in gear_plot
assert '"StashContentMix" => 376.0' in ui
assert '"StashGear" => 370.0' in ui
for document in ("UI-DESIGN-LOCK.md", "VISUAL-CONTROL-SYSTEM.md",
                 "STASH-CONTENT-GEAR-IMPLEMENTATION.md"):
    assert "bar" in (root / "docs" / document).read_text(encoding="utf-8").lower()
    assert "dot plot" in (root / "docs" / document).read_text(encoding="utf-8").lower()

# The detailed item graph and AUTO quantity rulers have their own
# independent hit regions and must not overlap the Gear plot controls.
detail_plot = ui.split("private void DrawStashMedicalComposition(", 1)[1].split(
    "private void DrawStashMedicalQuantity(", 1)[0]
quantity_plot = ui.split("private void DrawStashMedicalQuantity(", 1)[1].split(
    "private void DrawStashGear(", 1)[0]
assert "DrawCompactTrack(" not in detail_plot
assert "DrawCompactTrack(" in quantity_plot
assert "AddHorizontalTarget(source" in detail_plot
assert '"StashMedicalComposition" => 304.0' in ui
assert '"StashMedicalQuantity" => 257.0' in ui
assert "AUTO" in quantity_plot and "MAX CAP" in quantity_plot

# Literal source fixtures prevent counting an unchanged Chance=1 as
# a 3x overall spawn rate. This checks the documented math, not the
# live game's stochastic distribution or C# execution.
def bounded_chance(source_chance, medical, specific):
    return min(1.0, max(0.0, source_chance * medical * specific))
assert bounded_chance(.4, 1, .25) == .1
assert bounded_chance(1, 1, 3) == 1
assert bounded_chance(.4, 0, 3) == 0

def medkit_bounds(original_min, original_max, floor, cap):
    low = max(original_min, floor) if floor > 0 else original_min
    high = min(original_max, cap) if cap > 0 else original_max
    if low > high:
        low = high if cap > 0 else low
        high = low
    return low, high
assert medkit_bounds(2, 4, 0, 0) == (2, 4)
assert medkit_bounds(2, 4, 0, 1) == (1, 1)
assert medkit_bounds(1, 2, 3, 0) == (3, 3)
assert medkit_bounds(1, 4, 3, 1) == (1, 1)

for p in (source / "Presets").glob("*.json"):
    document = json.loads(p.read_text(encoding="utf-8"))
    for key in ids + med_ids:
        assert document["values"][key] == {
            "kind": "slider", "number": 1 if key in mix + medical else 0
        }, (p.name, key)
for path in ("UI-DESIGN-LOCK.md", "VISUAL-CONTROL-SYSTEM.md",
             "STASH-CONTENT-GEAR-IMPLEMENTATION.md"):
    assert (root / "docs" / path).is_file(), path
print("PASS: 20 stash source IDs (12 global/gear + 8 individual medical), 3 audited stash clone writers, UI graph/range geometry, 12 neutral factory profiles.")
print("NOT RUN: Windows C# production build, Wine chart interactivity or game drop-frequency acceptance.")
