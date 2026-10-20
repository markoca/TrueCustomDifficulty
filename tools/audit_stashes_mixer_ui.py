#!/usr/bin/env python3
"""Audit actual-source Stashes previews and stable edit/preset/writer contracts.

Reads the same embedded CFG writer source fixture used by the WPF preview.
Does not substitute static source checks for .NET/Wine or game QA.
"""
import csv
import json
import math
import re
import subprocess
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[1]
data = root / "src/TrueCustomDifficulty/Data"
source = (root / "src/TrueCustomDifficulty/Controls/AggregateVisualControl.cs").read_text()
main_vm_source = (root / "src/TrueCustomDifficulty/ViewModels/MainViewModel.cs").read_text()
catalog = json.loads((data / "parameters.json").read_text())
baseline = json.loads((data / "cfg-writer-baseline.json").read_text())["special"]
lock = (root / "docs/UI-DESIGN-LOCK.md").read_text()
system = (root / "docs/VISUAL-CONTROL-SYSTEM.md").read_text()
proposal = (root / "docs/STASHES-TAB-AND-REDESIGN-PROPOSAL.md").read_text()
coverage = (root / "src/TrueCustomDifficulty/Controls/AggregateVisualControl.StashBranch.cs").read_text()
tier_preview = coverage.split("private void DrawStashChanceTier(", 1)[1]

expected = {
    "StashContentMix": [
        "loot.stashMixAmmo", "loot.stashMixMedical", "loot.stashMixFood",
        "loot.stashMixDrinks", "loot.stashMixGrenades", "loot.stashMixCoupons",
        "loot.cheapStashAmmoQuantity", "loot.mediumStashAmmoQuantity",
        "loot.expensiveStashAmmoQuantity",
        "loot.standardStashChanceScale", "loot.rareStashChanceScale",
        "loot.cheapStashGrenadeChance", "loot.cheapStashGrenadeQuantity",
        "loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance",
        "loot.mediumStashCouponsChance", "loot.expensiveStashAmmoChance",
        "loot.expensiveStashMedicalChance", "loot.expensiveStashFoodChance",
        "loot.expensiveStashDrinksChance", "loot.expensiveStashGrenadesChance",
        "loot.expensiveStashMoneyChance"],
    "StashMedicalComposition": [
        "loot.stashMedicalBandageBias", "loot.stashMedicalMedkitBias",
        "loot.stashMedicalArmyMedkitBias", "loot.stashMedicalAntiRadBias",
        "loot.stashMedicalBandageChanceScale", "loot.stashMedicalMedkitChanceScale",
        "loot.stashMedicalArmyMedkitChanceScale", "loot.stashMedicalAntiRadChanceScale",
        "loot.stashMedkitMinCount", "loot.stashMedkitMaxCount",
        "loot.stashArmyMedkitMinCount", "loot.stashArmyMedkitMaxCount"],
    "StashGear": ["loot.stashGearEnabled", "loot.stashGearCoveragePercent",
                  "loot.stashGearWeaponWeight", "loot.stashGearArmorWeight",
                  "loot.stashGearArtifactWeight", "loot.stashGearAttachmentWeight",
                  "loot.stashGearBonusChance"],
}
ids = [p["id"] for p in catalog["parameters"] if p["tab"] == "stashes"]
assert len(ids) == len(set(ids)) == 41
assert set(ids) == set(sum(expected.values(), []))
family_ids = source.split("private static IReadOnlyList<string> GetFamilyIds(", 1)[1]
for family, expected_ids in expected.items():
    hit = re.search(r'"' + re.escape(family) + r'"\s*=>\s*new\[\]\s*\{(.*?)\}', family_ids, re.S)
    assert hit, family
    assert re.findall(r'"(loot\.[A-Za-z0-9.]+)"', hit.group(1)) == expected_ids, family
assert next(x for x in catalog["tabs"] if x["id"] == "stashes")["order"] == 55

def method(start, finish):
    return source[source.index("    private void " + start + "("):
                  source.index("    private void " + finish + "(")]

content = method("DrawStashContentMix", "DrawStashMedicalComposition")
medical = method("DrawStashMedicalComposition", "DrawStashMedicalQuantityCards")
pair = method("DrawStashMedicalPairShares", "DrawStashAntiRadWeightSwitch")
anti_weight = method("DrawStashAntiRadWeightSwitch", "DrawStashMedicalQuantityCards")
quantity = method("DrawStashMedicalQuantityCards", "DrawStashQuantityDualRail")
quantity_rail = method("DrawStashQuantityDualRail", "DrawStashGear")
gear = method("DrawStashGear", "DrawStashPoolBar")
preview = method("DrawStashPoolBar", "DrawStashNumericReadout")

assert 'DrawStashWeightMixer(dc, new Rect(left, 141.0, width,' in content
assert all(f'("{label}"' in medical for label in
    ("BANDAGES", "MEDKITS", "ARMY MEDKITS", "ANTI-RAD"))
assert quantity.count('("MEDKITS"') + quantity.count('("ARMY MEDKITS"') == 2
assert len([p for p in catalog["parameters"]
    if p["group"] == "Stash Medical Composition"]) == 12
assert all(p["group"] == "Stash Medical Composition" for p in catalog["parameters"]
    if p["id"] in expected["StashMedicalComposition"])
assert '"StashMedicalQuantity" => new[]' not in family_ids
assert 'case "StashMedicalQuantity":' not in source
assert '"StashMedicalQuantity" => StashMedicalQuantityHeight' not in source
assert "DrawStashMedicalQuantityCards(dc, left, width," in medical
assert "DrawStashMedicalWeightPreview(" not in coverage
assert "DrawStashMedicalWeightPreview(" not in medical
assert "wide ? 473.0 : 782.0" in medical
assert "cardWidth, 122.0" in quantity
assert "(i / columns) * 132.0" in quantity
assert "DrawStashQuantityDualRail(dc, P(family.Min), P(family.Max)" in quantity
assert "DrawHalf(" not in quantity_rail
assert "var rail = new Rect(card.Left + 20.0, card.Top + 80.0," in quantity_rail
assert "var xMin = ValueToX(minValue, 0.0, 5.0, rail, false);" in quantity_rail
assert "var xMax = ValueToX(maxValue, 0.0, 5.0, rail, false);" in quantity_rail
assert "dc.DrawLine(activePen, new Point(xMin, y), new Point(xMax, y));" in quantity_rail
assert "AddHorizontalTarget(min, fullHit, rail, 0.0, 5.0);" in quantity_rail
assert "AddHorizontalTarget(max, fullHit, rail, 0.0, 5.0);" in quantity_rail
assert "var overlap = Math.Abs(xMax - xMin) < 16.0;" in quantity_rail
# Hover regression: shared numeric glow must not cover native medical controls.
hover_renderer = source.split("private void DrawHoveredHandle(", 1)[1].split("// Anomalies are four independent", 1)[0]
assert 'Family == "StashMedicalComposition"' in hover_renderer
assert 'IsStashMedicalMin(source.Id)' in hover_renderer
assert 'IsStashMedicalMax(source.Id)' in hover_renderer
assert 'source.Id == "loot.stashMedicalAntiRadBias"' in hover_renderer
assert "ControlHoverGlow.Draw(dc, handle.Position," in quantity_rail
assert "ReferenceEquals(target.Parameter, handle.Parameter)" in quantity_rail
assert "hovered ? Brushes.White : enabled ? tone : muted" in quantity_rail
assert "hovered ? CloneWithOpacity(StashMedicalPalette[3], .10)" in anti_weight
assert "var rect = new Rect(x + i * cellWidth, card.Top + 24.0, cellWidth, 25.0);" in anti_weight
assert "AddHorizontalTarget(p, rect, rect, value, value);" in anti_weight
assert 'target?.Parameter.Id == "loot.stashMedicalAntiRadBias"' in source
assert "Cursor = Cursors.Hand;" in source
assert 'IsStashMedicalMax(_dragTarget.Parameter.Id)' in source

assert "ReferenceEquals(t.Parameter, min) && t.Kind == HitKind.Horizontal" in source
assert "ReferenceEquals(t.Parameter, max) && t.Kind == HitKind.Horizontal" in source
assert "dMin < dMax ? lower : upper" in source
assert "private static bool IsStashMedicalMin(string id)" in source
assert "width >= 700.0 ? 609.0 : 1050.0" in source
assert '"VANILLA"' not in quantity_rail and '"AUTO"' not in quantity_rail
assert 'StashMedicalMaxDisplay(p.CurrentNumber)' in quantity_rail
assert 'StashMedicalMaxStore(Math.Clamp(Math.Round(raw), 0.0, 5.0))' in source
assert "AddHorizontalTarget(p," in quantity_rail
# The same source-backed MIN/MAX coupling is applied to live input and bulk
# preset/save/build normalization without removing the native MAX=-1 exclusion.
assert "EnforceStashMedicalQuantityBounds(p);" in main_vm_source
assert "var cap = max.CurrentNumber < 0.0 ? 0.0 :" in main_vm_source
assert "max.CurrentNumber = min.CurrentNumber;" in main_vm_source
assert '("loot.stashMedkitMinCount", "loot.stashMedkitMaxCount")' in main_vm_source
assert '("loot.stashArmyMedkitMinCount", "loot.stashArmyMedkitMaxCount")' in main_vm_source
assert main_vm_source.count("NormalizeEditorConstraints();") == 5
assert "_sliderConstraintCoordinator.NormalizeAll();" in main_vm_source
for medical_id in ("loot.stashMedkitMinCount", "loot.stashMedkitMaxCount",
                   "loot.stashArmyMedkitMinCount", "loot.stashArmyMedkitMaxCount"):
    entry = next(p for p in catalog["parameters"] if p["id"] == medical_id)
    assert "MAX" in entry["interaction"] and "MIN" in entry["interaction"]
assert 'var shareWidth = Math.Max(1.0, innerWidth * .64);' in medical
assert '_medicalWeightRankTargets.Clear();' not in source
assert 'innerWidth - shareWidth - gap' in medical
assert 'previewTop + 2.0' in medical
assert 'g.Field == "Weight" && g.RankIndex == 2' in pair
assert 'const double rankWidth = 62.0;' not in pair
assert 'var rail = new Rect(outer.Left + 9.0, outer.Top + 28.0,' in pair
assert 'for (var i = 0; i < groups.Length; i++)' not in pair
assert '_medicalWeightRankTargets.Add((rect, rank));' not in source
assert 'var medkitShare = total > Epsilon ? medkit / total : 0.0;' in pair
assert 'DrawStashAntiRadWeightSwitch(dc,' in medical
assert '"ANTI-RAD · PRIORITY"' in anti_weight
assert "dc.DrawRectangle(accent, null" not in anti_weight
assert 'new Rect(x + i * cellWidth, card.Top + 24.0, cellWidth, 25.0)' in anti_weight
assert 'var dialMaximum = visualMaximum ?? parameter.Maximum;' in source
assert source.count('visualMaximum: 1.0') == 3
assert 'IsMedicalPriorityWeight(target.Parameter)' in source
assert 'IsMedicalPriorityWeight(_selectedParameter)' in source
assert '_medicalWeightPreviewRank' not in source
assert '_medicalWeightHoveredRank' not in source
assert '_medicalWeightRankTargets' not in source
assert 'var group = groups[0];' in pair
assert 'groups.Length != 1' in pair
for pid in ("loot.stashMedicalBandageBias", "loot.stashMedicalMedkitBias",
            "loot.stashMedicalArmyMedkitBias"):
    entry = next(p for p in catalog["parameters"] if p["id"] == pid)
    assert entry["slider"] == {"min": 0, "max": 3,
        "step": 0.25, "stepMode": "absolute"}
    assert "0–1×" in entry["interaction"]
assert "DrawStashMedicalPairShares(dc," in medical
assert "DrawStashAntiRadWeightSwitch(dc," in medical
assert '"DROP ROLLS · CHANCE %"' in medical
assert '"LOOT PRIORITY · NATIVE WEIGHTS"' in medical
assert 'var chanceColumns = wide ? 4 : 2;' in medical
assert 'var weightColumns = wide ? 3 : 2;' in medical
assert 'percentageReadout: true, dialAccent: source.Color' in medical
assert 'DrawSourceKnob(dc, P("loot.stashMedicalBandageBias")' in medical
assert 'DrawSourceKnob(dc, P("loot.stashMedicalMedkitBias")' in medical
assert 'DrawSourceKnob(dc, P("loot.stashMedicalArmyMedkitBias")' in medical
assert 'g.Prototype == "StashMedicine_Smart"' in pair
assert 'g.RankIndex is 2 or 3' not in pair
assert 'group.EffectiveSelection(StashPreviewBaseline.Value, state,' in pair
assert 'group.Items.Single(item => item.ItemSid == sid)' in pair
assert 'StashMedicalPalette[1]' in pair and 'StashMedicalPalette[2]' in pair
assert "AddHorizontalTarget(p, rect, rect, value, value);" in anti_weight
assert '"loot.stashMedicalAntiRadBias"' in anti_weight
assert 'var value = i == 0 ? 0.0 : 1.0;' in anti_weight
assert "_stashMedicalShowChance" not in source
assert "_stashMedicalModes" not in source
assert 'target.Parameter.Id == "loot.stashMedicalAntiRadBias"' in source
assert '"OFF"' in anti_weight and '"ON"' in anti_weight
assert "_stashMedicalShowChance" not in source
assert "DrawStashQuantityDots(" not in source
assert "DrawStashMedicalSourcePreview(" not in source
assert '=> width >= 700.0 ? 585.0 : 1001.0;' in source
assert 'P("loot.stashGearCoveragePercent")' in gear
for key in ("Weapon", "Armor", "Artifact", "Attachment"):
    assert '"loot.stashGear' + key + 'Weight"' in gear
assert 'var mixStrip = new Rect(left + 2.0, 194.0,' in gear
assert 'mixStrip, 4.0, 4.0);' in gear
assert 'var mixWidth = Math.Max(1.0,' in gear
assert 'DrawSourceKnob(dc, P(weights[i].Item1), weights[i].Item2,' in gear
assert 'dialScale: .74' in gear
assert 'var mixColumns = narrowMix ? 2 : 4;' in gear
assert 'narrowMix ? 390.0 : 224.0' in gear
assert 'var shareTop = mixStrip.Top + (narrowMix ? 332.0 : 166.0);' in gear
assert 'var shareTrack = new Rect(mixStrip.Left + 10.0, shareTop + 20.0,' in gear
assert 'DrawText(dc, "PRIORITY SHARE"' in gear
assert 'var shares = StashGearWeightShares(weights.Select(channel =>' in gear
assert 'P(channel.Item1)?.CurrentNumber ?? 0.0' in gear
assert 'shareEnabled ? StashGearMixPalette[i]' in gear
assert 'var shareCell = shareTrack.Width / shares.Length;' in gear
assert 'DrawRightAlignedText(dc, $"{shares[i] * 100.0:0.#}%"' in gear
assert '"ItemGeneratorPrototypeSID"' not in gear
assert 'AddHorizontalTarget(' not in gear.split('// Read-only weight share.',1)[1].split('// One real CFG Chance setting',1)[0]
assert 'private static double[] StashGearWeightShares(IReadOnlyList<double> raw)' in source
assert '(int)Math.Round(value * 10.0), 0, 30' in source
assert 'var total = ticks.Sum();' in source
assert 'total == 0 ? 0.0' in source
assert 'value / total' in source
assert '(i % mixColumns)' in gear and '(i / mixColumns)' in gear
assert 'AddHorizontalTarget(weight,' not in gear
assert 'labelY = 204.0 + i * 52.0' not in gear
assert 'DrawSourceKnob(' not in content
assert "DrawStashWeightMixer(dc," in content
assert "DrawStashBasicNativePools(dc," in content
assert "DrawStashRewardGraphRow(dc, P(entries[i].Id)" not in medical
assert "DrawStashMedicalSourcePreview(" not in source
assert "DrawSourceKnob(dc, P(source.Id), source.Label, card," in medical
assert "percentageReadout: true, dialAccent: source.Color" in medical
assert "StashMedicalPalette[0]" in medical and "StashMedicalPalette[3]" in medical
assert '"MEDICAL COMPOSITION"' in medical
assert "DrawStashMedicalModeSelector" not in source
assert "DrawStashGearMode(" not in gear
assert 'P("loot.stashGearBonusChance")' in gear
assert 'AddHorizontalTarget(chance,' in gear
assert '"BONUS CHANCE · ALL ITEMS"' in gear
assert '=> width < 570.0 ? 670.0 : 502.0;' in source
assert 'var chanceY = narrowMix ? 593.0 : 429.0;' in gear
assert gear.count('DrawSourceKnob(dc, P(weights[i].Item1), weights[i].Item2,') == 1
assert 'var width = Math.Min(630.0, Math.Max(1.0, ActualWidth - 30.0));' in gear
assert 'var narrowMix = width < 540.0;' in gear
assert '"loot.stashGearAttachmentWeight",' in family_ids
for key in ("Weapon", "Armor", "Artifact", "Attachment"):
    row = next(p for p in catalog["parameters"]
               if p["id"] == "loot.stashGear" + key + "Weight")
    assert row["slider"] == {"min": 0, "max": 3, "step": 0.1, "stepMode": "absolute"}
    assert "knob" in row["interaction"].lower()
assert "Bonus Mix" in lock and "three" in system
assert "DrawStashBiasPreview" not in source
assert "DrawStashCountKnob" not in source
assert "DrawStashGearDial" not in source
assert "AddHorizontalTarget(" not in preview  # never an editable preview
assert "DrawStashNumericReadout" not in preview
assert "MULTIPLIERS, NOT DROP ODDS" not in source
assert "weights.Sum()" not in preview # normalized six source multipliers retired
assert 'DrawStashNativeRollControl(dc, "loot.cheapStashGrenadeChance",' in content
assert 'DrawStashChanceTier(dc, "STANDARD STASH", "Medium", 10,' in content
assert 'DrawStashChanceTier(dc, "RARE STASH", "Expensive", 19,' in content
assert 'DrawStashChanceTier(dc, "STANDARD", "Medium", 10,' not in content
assert 'DrawStashChanceTier(dc, "RARE", "Expensive", 19,' not in content
assert "wide ? 441.0 : 595.0, width," in content
assert "wide ? 122.0 : 200.0" in content
assert "wide ? 571.0 : 803.0, width," in content
assert "wide ? 200.0 : 278.0" in content
assert "wide ? 295.0 : 449.0, width, 74.0" in content
assert "var pairTop = wide ? 377.0 : 531.0;" in content
assert '"WEIGHT APPLIES TO ALL TIERS"' not in content
assert '"RELATIVE SELECTION"' not in source
assert '"BASIC POOL COMPOSITION"' not in coverage

# One bounded 3x2/6x1 relative-Weight mixer, not six rotary dials
# and never an invented 100% stash-wide probability.
assert "Math.Min(778.0, Math.Max(1.0, ActualWidth - 30.0))" in content
assert "DrawStashWeightMixer(dc," in content
assert "DrawStashBasicNativePools(dc," in content
assert "DrawSourceKnob(" not in content
assert "DrawStashBasicNativePools(dc," in content
assert '"RGD-5 AMOUNT"' in content
assert "percentage: false" in content
assert source.count("new SolidColorBrush(Color.FromRgb(") >= 6
assert 'StashPreviewGroup("BASIC", 1)' in preview
assert 'StashPreviewGroup("BASIC", 7)' in preview  # retained only in dormant source-inspection helper
assert 'DrawStashPoolBar(dc, hard, string.Empty, inner,' in coverage
assert 'DrawStashPoolBar(dc, hard, "VETERAN"' not in coverage
assert 'StashPreviewGroup("RARE", 11)' not in preview  # chance only, never a weight pool
assert "StashChanceSample(" in preview
assert '"SOURCE CHANCE FIELDS"' in preview
assert '"NATIVE CHANCE · INDEPENDENT"' in preview
assert 'CurrentStashEffectiveState().TuneSelection(sid, "Weight", raw)' in preview
assert "effective.TierField(" in preview
assert "StashEffectiveState.Number(" in preview
assert "StashSourceFactor(" not in source
assert "StashSourceNumber(" not in source
assert "CfgWriterBaselineService().Load()" in source
assert "StashMedicalSourcePreview" not in source
assert '"0 ITEMS"' not in quantity_rail and '"AUTO"' not in quantity_rail
assert 'value < -.5 ? "0"' in quantity_rail

# Authoritative baseline input: 2 representative DIFFERENT actual-native
# weight pools from GameData/ItemGeneratorPrototypes.cfg. At 1x the real
# category shares are not 16.7%; chance rows are NOT added to these pools.
basic = baseline["cheapStashProduction"]["templateCategories"]
hard = basic[1]
exp = basic[7]
assert hard["node"] == "[1]"
assert hard["fields"]["Diff"] == "EGameDifficulty::Hard"
assert exp["node"] == "[7]"
assert exp["fields"]["Diff"] == "EGameDifficulty::Easy, EGameDifficulty::Medium"
assert exp["fields"]["PlayerRank"] == "ERank::Experienced"

category = {
    "Milk": 3, "Bread": 2, "Sausage": 2, "CannedFood": 2,
    "Bandage": 1, "MoneyCommon": 5,
}
def native_group_weights(group, multipliers):
    out = [0.0] * 6
    for item in group["items"]:
        row = item["fields"]
        sid = row["ItemPrototypeSID"]
        assert "Weight" in row and "Chance" not in row, (group["node"], sid)
        out[category[sid]] += float(row["Weight"].rstrip("fF")) * multipliers[category[sid]]
    return out

# Only the three categories in the retained HARD/Veteran native source
# pool have visible knobs. The excluded Easy/Medium Experienced branch
# contains the coupon entry, which remains untouched in game/presets.
pool_kinds = sorted({category[row["fields"]["ItemPrototypeSID"]]
    for row in hard["items"]})
assert pool_kinds == [1, 2, 3], pool_kinds
assert sorted({category[row["fields"]["ItemPrototypeSID"]]
    for row in exp["items"]}) == [1, 2, 3, 5]
for pid in ("loot.stashMixAmmo", "loot.stashMixGrenades", "loot.stashMixCoupons"):
    assert next(p for p in catalog["parameters"] if p["id"] == pid)["exposeInUi"] is False
for pid in ("loot.stashMixMedical", "loot.stashMixFood", "loot.stashMixDrinks"):
    assert next(p for p in catalog["parameters"] if p["id"] == pid)["exposeInUi"] is True

base = native_group_weights(exp, [1] * 6)
assert base == [0, 2, 13, 1, 0, 1.77], base
assert math.isclose(sum(base), 17.77, abs_tol=1e-9)
assert math.isclose(100 * base[2] / sum(base), 73.157, abs_tol=0.01)
hard_default = native_group_weights(hard, [1] * 6)
assert hard_default == [0, 2, 13, 1, 0, 0], hard_default
assert sum(hard_default) == 16
food_disabled = native_group_weights(exp, [1, 1, 0, 1, 1, 1])
assert food_disabled[2] == 0 and food_disabled[1] == 2
all_disabled = native_group_weights(exp, [0] * 6)
assert sum(all_disabled) == 0
food_half = native_group_weights(exp, [1, 1, .5, 1, 1, 1])
assert food_half[2] == 6.5 and math.isclose(sum(food_half), 11.27)

samples = [
    ("expensiveStashProduction", 1, "A918A", 1.0),
    ("cheapStashProduction", 0, "Bandage", .4),
    ("expensiveStashProduction", 0, "CannedFood", 1.0),
    ("cheapStashProduction", 0, "Vodka", .5),
    ("cheapStashProduction", 5, "GrenadeRGD5", .1),
    ("expensiveStashProduction", 11, "MoneyCommon", .2),
    ("cheapStashProduction", 0, "Medkit", .4),
    ("expensiveStashProduction", 4, "ArmyMedkit", 1.0),
    ("expensiveStashProduction", 4, "AntiRad", 1.0),
]
for family, branch, sid, expected_chance in samples:
    source_group = baseline[family]["templateCategories"][branch]
    rows = [item["fields"] for item in source_group["items"]
            if item["fields"]["ItemPrototypeSID"] == sid]
    assert len(rows) == 1 and "Chance" in rows[0], (family, branch, sid)
    chance = float(rows[0]["Chance"].rstrip("fF"))
    assert math.isclose(chance, expected_chance, abs_tol=1e-8)
    assert 0 <= chance <= 1  # Native Chance is separate from global Weight.

# Three native tiers now contain their own quantity controls and Chance sources.
# The host must exactly contain each painted panel at all breakpoints.
height_match = re.search(
    r"private double StashContentMixHeight\(double width\)\s*"
    r"=> width >= ([\d.]+) \? ([\d.]+) : ([\d.]+);",
    source,
)
mixer_match = re.search(
    r"DrawStashWeightMixer\(dc, new Rect\(left, ([\d.]+), width,\s*"
    r"wide \? ([\d.]+) : ([\d.]+)\)",
    content,
)
assert height_match is not None, "missing StashContentMix measured height"
assert mixer_match is not None, "missing StashContentMix mixer"
assert tuple(map(float, height_match.groups())) == (700.0, 839.0, 1185.0)
assert tuple(map(float, mixer_match.groups())) == (141.0, 146.0, 300.0)
assert "wide ? 441.0 : 595.0, width," in content
assert "wide ? 122.0 : 200.0" in content
assert "wide ? 571.0 : 803.0, width," in content
assert "wide ? 200.0 : 278.0" in content
assert "wide ? 295.0 : 449.0, width, 74.0" in content
assert "var pairTop = wide ? 377.0 : 531.0;" in content
assert '"WEIGHT APPLIES TO ALL TIERS"' not in content
assert '"RELATIVE SELECTION"' not in source
assert '"BASIC POOL COMPOSITION"' not in coverage
assert "var wide = width >= 670.0;" in content
assert 839.0 - (589.0 + 236.0) == 14.0
assert 1185.0 - (839.0 + 332.0) == 14.0
assert "=> width >= 700.0 ? 373.0 : 409.0;" in source
assert "=> width >= 700.0 ? 220.0 : 383.0;" in source
assert "=> width >= 746.0 ? 220.0 : width >= 580.0 ? 383.0 : 546.0;" in source
for name, document in [("UI-DESIGN-LOCK", lock),
                       ("VISUAL-CONTROL-SYSTEM", system),
                       ("STASHES-TAB-AND-REDESIGN-PROPOSAL", proposal)]:
    assert "STASHES SOURCE-NATIVE WEIGHT/CHANCE PREVIEWS" in document, name

# Phase 1 invariant: the Stashes UI audit also checks the native source
# inventory, which does not establish engine-runtime probabilities.
consumer_audit = subprocess.run(
    [sys.executable, str(root / "tools/audit_stash_consumer_graph.py")],
    capture_output=True, text=True, check=False,
)
assert consumer_audit.returncode == 0, (
    consumer_audit.stdout + consumer_audit.stderr
)
print("PASS: guarded consumer -> generator -> native branch graph synchronized")

branch_audit = subprocess.run(
    [sys.executable, str(root / "tools/audit_stash_generator_branches.py")],
    capture_output=True, text=True, check=False,
)
assert branch_audit.returncode == 0, (
    branch_audit.stdout + branch_audit.stderr
)
print("PASS: branch-level source inventory synchronized with CFG inputs")

print("PASS: 26 original source IDs, editable controls, no synthetic six-way probability")
print("PASS: source-native Easy/Medium Experienced and Hard weighted pools")
print("PASS: 0x/0.5x/1x native-group ratios, Chance-only fields untouched")
print("PASS: real grenade Chance and Amount adjacent; Rare Coupon Chance preserved")
print("PENDING: local .NET build, Wine layout/behavior, in-game stash runtime results")

# Phase-3 executable CFG output parity lives in LootSpecialGeneratorsContract.
# Static source auditing ensures both UI and tier/SmartLoot producers take the
# SAME shared transformation. It cannot replace running the Windows harness.
effective = (root / "src/TrueCustomDifficulty/Services/StashEffectiveState.cs").read_text()
writer = (root / "src/TrueCustomDifficulty/Services/CfgPatchWriterService.cs").read_text()
smart = (root / "src/TrueCustomDifficulty/Services/CfgPatchWriterService.StashSmartLoot.cs").read_text()
tuning = (root / "src/TrueCustomDifficulty/Services/CfgPatchWriterService.StashTuning.cs").read_text()
harness = (root / "tests/LootSpecialGeneratorsContract/Program.cs").read_text()
assert writer.count("effective.TierField(") == 3
assert "effective.SmartSelection(" in smart
assert "StashEffectiveState.UniformSmartFactor(" in smart
assert 'selectionField == "Chance" || factor <= 0.0000001' in smart
assert "effective.SmartSelectionFactor(item.ItemSid, selectionField)" in smart
assert "StashEffectiveState.SmartSpawnEndpoint(" in smart
assert "StashState(all).TuneSelection(" in tuning
assert "StashState(all).TuneCount(" in tuning
assert "effective.TierField(" in preview
assert "CurrentStashEffectiveState()" in source
assert "loot.cheapStashGrenadeChance" in family_ids.split('"StashContentMix" => new[]', 1)[1].split('"StashMedicalComposition"', 1)[0]
assert "loot.expensiveStashMoneyChance" in family_ids.split('"StashContentMix" => new[]', 1)[1].split('"StashMedicalComposition"', 1)[0]
assert "VerifyStashEffectiveParity();" in harness
assert "snapshot.TierField(" in harness
assert "snapshot.SmartSelection(" in harness
assert "MedicalDisabled(" in effective and "TuneCount(" in effective
assert "Changed(config.GrenadeChanceParameterId)" in effective
assert "Changed(config.CouponChanceParameterId)" in effective
print("PASS: shared writer/preview tier and SmartLoot calculation, bonus Chance composition")


# Stashes edits are genuinely GLOBAL by item SID, not branch or rank.
# The fixed authored native group catalog remains backend-only parity evidence.
mix_ids = expected["StashContentMix"][:6]  # global Weight only; remaining two are native Chance
for pid in mix_ids:
    item = next(p for p in catalog["parameters"] if p["id"] == pid)
    slider = item["slider"]
    assert (slider["min"], slider["max"], slider["step"]) == (0, 1, 0.1), pid
    assert item["vanilla"]["All"] == 1, pid
    assert "Weight" in item["targets"][0]["path"], pid
    assert all("Chance" not in target["path"] for target in item["targets"]), pid
    assert len(item["targets"]) == 2, pid
    assert item["targets"][0]["file"] == "GameData/ItemGeneratorPrototypes.cfg", pid
    assert item["targets"][1]["file"] == "GameData/StashPrototypes.cfg", pid
    assert "SmartLootParams" in item["targets"][1]["path"], pid

coverage = (root / "src/TrueCustomDifficulty/Controls/AggregateVisualControl.StashBranch.cs").read_text()
branches = (root / "src/TrueCustomDifficulty/Services/StashBranchPreviewCatalog.cs").read_text()
csproj = (root / "src/TrueCustomDifficulty/TrueCustomDifficulty.csproj").read_text()
assert 'DrawStashWeightMixer(dc, new Rect(left, 141.0, width,' in content
assert "StashContentMixHeight(width)" in source
assert '"loot.mediumStashMedicalChance"' in source
assert '"loot.mediumStashCouponsChance"' in source
assert '"loot.expensiveStashAmmoChance"' in source
assert '"loot.expensiveStashGrenadesChance"' in source
assert 'var id = tierName == "Medium"' in coverage
assert "StashPreviewBaseline.Value, effective, r.Item" in coverage
assert 'CreateText("V "' not in tier_preview and "vanillaX" not in tier_preview
assert "Math.Clamp(Number(updated) * multiplier, 0.0, 1.0)" in effective
assert 'StashTierChanceParameterIds.Any(id =>' in tuning

assert '=> width >= 700.0 ? 839.0 : 1185.0;' in source
assert "GlobalStashKindsVerified.Value" in coverage
assert "StashBranchPreviewCatalog.Load(StashPreviewBaseline.Value)" in coverage
assert "groups.Sum(x => x.Items.Count) != 1124" in coverage
assert "StashCategoryIndex(item.ItemSid)" in coverage
assert '"ITEMGEN ' not in coverage and '"WORLD ' not in coverage
assert '"STASH ITEM WEIGHTS"' not in coverage
assert '"WEIGHT OFF"' in coverage and '"REDUCED"' in coverage
assert '"INCREASED"' in coverage and '"EXCLUDED"' in coverage
assert '"ORIGINAL"' in coverage and '"PARTIAL"' in coverage
assert '"WEIGHT OFF · INDEPENDENT CHANCE MAY STILL ROLL"' in coverage
assert "StashEffectiveState.ItemBiasId(sid)" in coverage
assert "StashEffectiveState.SmartItemBiasId(sid)" in coverage
for retired in ("DrawStashBranchBrowser(", "NavigateStash(", "TryStashBrowserClick(",
                "UpdateStashBrowserHover(", "_stashBrowserMode", "_stashBrowserRank",
                "_stashBrowserGroup", "_stashBrowserPrototype", "_stashBrowserHover",
                "SOURCE DETAILS", "HIDE DETAILS"):
    assert retired not in source + coverage, retired
assert "CurrentNumber =" not in coverage
assert "AddHorizontalTarget(p," in coverage
assert "for (var stop = " not in coverage
assert "foreach (var stop in new[] { p.Minimum, p.Maximum })" in coverage
assert "const double cardHeight = 146.0;" in coverage
assert "var columns = outer.Width >= 670.0 ? 3 : 2;" in coverage
assert "var rail = new Rect(x + 13.0, y + 26.0," in coverage
assert "p.IsValueModifiedFromReference()" in coverage
assert "number.SetForegroundBrush(accent, 0, digits.Length)" in coverage
assert "ValueToX(p.CurrentNumber, p.Minimum, p.Maximum, rail, false)" in coverage
assert "p.ReferenceNumber" in coverage
assert "Normalize(" not in coverage
assert "percentages" not in coverage
# Reconcile ALL ItemGenerator categories and their source-native fields
# in each tier, not a sampled ammo/consumable Weight group.
source_kind_sids = {
    1: {"Bandage", "Medkit", "ArmyMedkit", "EcoMedkit", "AntiRad", "Antirad"},
    2: {"Bread", "Sausage", "CannedFood"},
    3: {"Water", "Energetic", "Beer", "Milk", "Vodka"},
    4: {"GrenadeRGD5", "GrenadeF1"},
    5: {"MoneyCommon", "MoneyRare", "MoneyEpic", "MoneyLegendary"}
}
def source_category(sid):
    if len(sid) > 1 and sid[0] == "A" and sid[1].isdigit():
        return 0
    return next((k for k, names in source_kind_sids.items() if sid in names), -1)

tier_coverage = (
    ("cheapStashProduction", "Cheap", 12, 52,
     [3, 6, 18, 8, 0, 3], [0, 6, 4, 3, 1, 0]),
    ("mediumStashAmmoProduction", "Medium", 10, 39,
     [25, 0, 0, 0, 0, 0], [0, 6, 0, 5, 0, 3]),
    ("expensiveStashProduction", "Expensive", 19, 42,
     [23, 0, 0, 0, 0, 0], [2, 6, 1, 1, 1, 8])
)
for source_key, tier_id, group_count, item_count, expected_weight, expected_chance in tier_coverage:
    groups = baseline[source_key]["templateCategories"]
    assert len(groups) == group_count, tier_id
    assert sum(len(g["items"]) for g in groups) == item_count, tier_id
    weight = [0] * 6
    chance = [0] * 6
    for group in groups:
        assert group["items"]
        group_field = "Weight" if "Weight" in group["items"][0]["fields"] else "Chance"
        for item in group["items"]:
            fields = item["fields"]
            assert (("Weight" in fields), ("Chance" in fields)) in (
                (True, False), (False, True)), (tier_id, fields)
            assert group_field in fields
            index = source_category(fields["ItemPrototypeSID"])
            assert 0 <= index < 6, (tier_id, fields["ItemPrototypeSID"])
            (weight if group_field == "Weight" else chance)[index] += 1
    assert weight == expected_weight, (tier_id, weight)
    assert chance == expected_chance, (tier_id, chance)


# Only Veteran's original Basic source pool remains visible, not the
# nearly identical Easy/Medium Experienced variant containing coupons.
basic_preview = baseline["cheapStashProduction"]["templateCategories"]
assert basic_preview[7]["fields"]["Diff"] == (
    "EGameDifficulty::Easy, EGameDifficulty::Medium")
assert basic_preview[7]["fields"]["PlayerRank"] == "ERank::Experienced"
assert basic_preview[1]["fields"]["Diff"] == "EGameDifficulty::Hard"
assert "PlayerRank" not in basic_preview[1]["fields"]

assert '"LOOT PRIORITY · ALL TIERS"' in content
assert '"BASIC  ·  WEIGHT MIX"' not in content
assert '"BASIC · LOOT PREVIEW"' in coverage
native_preview_method = coverage.split("private void DrawStashBasicNativePools(", 1)[1].split("private void DrawStashChanceTier(", 1)[0]
assert '"NOVICE / STALKER"' not in native_preview_method
assert 'rankCaption: "RANK: EXPERIENCED"' not in native_preview_method
assert 'StashPreviewGroup("BASIC", 7)' not in native_preview_method
assert 'StashPreviewGroup("BASIC", 1)' in native_preview_method
assert 'DrawStashPoolBar(dc, hard, string.Empty, inner,' in native_preview_method
assert 'DrawStashPoolBar(dc, hard, "VETERAN"' not in native_preview_method
assert '"EASY/MEDIUM · EXPERIENCED"' not in coverage
assert 'CreateText(label, 11.4, text' in coverage
assert 'GlobalStashCategoryNames[kind], 10.2, text,' in coverage
assert 'CreateText("NATIVE " + currentText, 10.4,' in coverage
assert 'var compactOffset = string.IsNullOrWhiteSpace(caption) ? -8.0 : 0.0;' in source
assert "row.Width < 440.0 ? 9.6 : 10.8" in source
assert "row.Width >= 580.0 ? 10.0 : 8.9" in source

# Regression: WPF can render a visible StashContentMix family before the
# Window-level AllParameters binding arrives. Eight new authored Chance
# parameters must be present in the embedded catalog, aggregate family and
# main ViewModel, but missing early VMs must never throw during OnRender.
tier_chance_ids = [
    "loot.standardStashChanceScale", "loot.rareStashChanceScale",
    "loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance",
    "loot.mediumStashCouponsChance", "loot.expensiveStashAmmoChance",
    "loot.expensiveStashMedicalChance", "loot.expensiveStashFoodChance",
    "loot.expensiveStashDrinksChance", "loot.expensiveStashGrenadesChance",
    "loot.expensiveStashMoneyChance",
]
catalog_by_id = {p["id"]: p for p in catalog["parameters"]}
viewmodel = (root / "src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs").read_text()
main_vm = (root / "src/TrueCustomDifficulty/ViewModels/MainViewModel.cs").read_text()
for pid in tier_chance_ids:
    entry = catalog_by_id[pid]
    assert entry["exposeInUi"] is (pid not in ("loot.standardStashChanceScale", "loot.rareStashChanceScale")), pid
    assert entry["group"] == "Stash Content", pid
    if entry["exposeInUi"]:
        assert entry["slider"]["min"] == 0, pid
        assert entry["slider"]["max"] == 1, pid
        assert entry["slider"]["step"] == 0.01, pid
    else:
        assert entry["slider"]["max"] == 2, pid
    assert pid in expected["StashContentMix"], pid
    assert f'["{pid}"] = "StashContentMix"' in viewmodel, pid
    assert f'"{pid}"' in main_vm, pid
# The three ammo rails remain independent existing native MinCount/MaxCount
# multipliers and must be drawn exactly once: one BASIC, two within tier cards.
assert content.count('"loot.cheapStashAmmoQuantity"') == 1
assert 'endValue + (multiplier ? "×" : percentage ? "%" : "")' in coverage
assert 'multiplier ? "0.##"' in coverage
# Regression: the Basic WEIGHT mixer has no tierName in scope. Tier-local
# ammo quantity was accidentally injected here, causing CS0103 on build.
# The Standard/Rare cards must own it, not the Basic Weight mixer.
mixer_method = coverage.split("private void DrawStashWeightMixer(", 1)[1].split(
    "private void DrawStashBasicNativePools(", 1)[0]
tier_method = coverage.split("private void DrawStashChanceTier(", 1)[1]
assert "tierName" not in mixer_method
assert "ammoId" not in mixer_method
assert "DrawStashNativeRollControl(" not in mixer_method
assert 'var y = outer.Top + 35.0 + n / columns * pitch;' in tier_preview
assert '"StashRewardProfile"' not in viewmodel and '"StashRewardProfile"' not in main_vm
assert 'Parameters="{Binding DataContext.AllParameters' in (
    root / "src/TrueCustomDifficulty/MainWindow.xaml").read_text()



# No four-way medical Weight pool exists in the authored sources. All four
# medical item classes nevertheless have real independent native Weight rows,
# so each has a real 0–3x knob without a synthetic relative-selection bar.
assert "DrawStashMedicalWeightPreview(" not in coverage
assert "RANK: VETERAN" not in medical and "RANK: MASTER" not in medical
with (data / "smartloot-world-source-entries.csv").open(newline="") as f:
    medical_rows = list(csv.DictReader(f))
for sid in ("Bandage", "Medkit", "ArmyMedkit", "Antirad"):
    assert any(row["ItemSID"] == sid
               and row["Weight"] != ""
               and row["Chance"] == ""
               for row in medical_rows), sid
# The Basic ItemGenerator also carries genuine Bandage Weight:
assert any(item["fields"].get("ItemPrototypeSID") == "Bandage"
           and "Weight" in item["fields"]
           for group in baseline["cheapStashProduction"]["templateCategories"]
           for item in group["items"])

# New global ammo UI does not invent a fourth CFG source.
assert 'DrawStashNativeRollControl(dc, "loot.cheapStashAmmoQuantity",' in content
assert '"loot.mediumStashAmmoQuantity"' not in content
assert '"loot.expensiveStashAmmoQuantity"' not in content
assert '"loot.mediumStashAmmoQuantity"' not in tier_preview
assert '"loot.expensiveStashAmmoQuantity"' not in tier_preview
assert "SyncStashAmmoQuantity();" in main_vm_source
assert "stashAmmoSources.Skip(1)" in main_vm_source
assert 'var digits = (parameter.CurrentNumber * 100.0).ToString("0.##"' in tier_preview
assert 'var number = CreateText(digits + "%", 11.8,' in tier_preview
assert 'DrawText(dc, "0%"' in tier_preview
assert 'DrawRightAlignedText(dc, maximum + "%"' in tier_preview
assert 'var now = ValueToX(parameter.CurrentNumber,' in tier_preview
assert 'rail, parameter.Minimum, parameter.Maximum);' in tier_preview
assert 'Visible category factors are 0..100%' in tier_preview
assert 'digits + (absolute ? "%" : "×")' not in tier_preview
for pid in ("loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance",
            "loot.mediumStashCouponsChance", "loot.expensiveStashAmmoChance",
            "loot.expensiveStashMedicalChance", "loot.expensiveStashFoodChance",
            "loot.expensiveStashDrinksChance", "loot.expensiveStashGrenadesChance"):
    assert next(p for p in catalog["parameters"] if p["id"] == pid)["display"]["primary"] == "percentOfVanilla"

assert "StashSourceGroups.Value" in tier_preview
assert "!g.IsSmart && g.Tier == tierName" in tier_preview
assert "hasWeight[kind] = true" in tier_preview
assert "hasChance[kind] = true" in tier_preview
assert "hasWeight[k] || hasChance[k]" in tier_preview
assert '"NATIVE CHANCE"' not in tier_preview
assert 'var chanceKinds = present.Where(k => hasChance[k]).ToArray();' in tier_preview
assert '"WEIGHT ONLY"' not in tier_preview
assert "const double pitch = 96.0;" in tier_preview
assert "cellWidth, 88.0" in tier_preview
assert 'CreateText("V "' not in tier_preview
assert "vanillaX" not in tier_preview
assert "var expected = tierName == \"Medium\"" in tier_preview
assert "SequenceEqual(expected)" in tier_preview
assert "100.0 * totals" not in tier_preview
assert "totals.Sum()" not in tier_preview
assert 'var id = tierName == "Medium"' in tier_preview
assert 'var parameter = P(id);' in tier_preview
assert 'AddHorizontalTarget(parameter,' in tier_preview
assert 'AddHorizontalTarget(scale,' not in tier_preview
assert 'var y = outer.Top + 35.0' in tier_preview
assert 'var currentText = PercentRange(activeValues.Min(), activeValues.Max());' in tier_preview
assert 'if (parameter is null)' in tier_preview
assert 'var label = Parameters is null' not in tier_preview
assert "continue;" in tier_preview
assert 'throw new InvalidDataException(\n                "Missing live stash Chance control "' not in tier_preview
assert "var vanillaValues = rows.Select(" not in tier_preview
assert "OnBindingChanged" in source and "Loaded += (_, _) => RebindParameters();" in source
assert "CurrentNumber =" not in tier_preview
assert "DrawStashNativeRollControl(" in coverage
assert 'DrawStashPoolBar(dc, hard, string.Empty, inner,' in coverage
assert 'DrawStashPoolBar(dc, hard, "VETERAN"' not in coverage
assert '"NOVICE / STALKER"' not in coverage
assert 'StashPreviewGroup("BASIC", 1)' in coverage
assert "dialAccent: StashMixPalette[kind], dialScale: .74" in coverage
assert "private static readonly int[] BasicVisibleWeightKinds = { 1, 2, 3 };" in coverage
assert "BasicVisibleWeightKinds.Length" in coverage
assert "previewGroup.Items" in coverage
assert "var columns = outer.Width >= 670.0 ? 3 : 2;" in coverage
assert '"RGD-5 CHANCE"' in source
assert '"loot.expensiveStashMoneyChance"' in coverage
for doc in (lock, system, proposal):
    assert "STASHES — ACTUAL TIER CONTENT / WEIGHT-CHANCE COVERAGE (CURRENT)" in doc

assert "StashMixPalette[i]" in coverage
assert "GlobalMedicalItems.Select(item => state.ChanceFactor(item.Sid))" in coverage
assert "EXCLUDED: " in coverage
assert 'GlobalStashStatus(int category, StashEffectiveState state)' in coverage
assert 'GlobalStashDetail(int category, string status,' in coverage
assert 'GlobalStashParameterIds' in coverage
bound_mix = re.search(
    r"private static readonly string\[\] GlobalStashParameterIds\s*=\s*\{(.*?)\};",
    coverage, re.S
)
assert bound_mix is not None
assert re.findall(r'"(loot\.stashMix[A-Za-z]+)"', bound_mix.group(1)) == mix_ids
assert 'foreach (var stop in new[] { p.Minimum, p.Maximum })' in coverage
assert 'AddHorizontalTarget(p,' in coverage
assert 'DrawCompactStashRewards(' not in source
assert 'DrawStashRewardGraphRow(' not in source
assert '"StashRewardProfile" => new[]' not in source
assert 'case "StashRewardProfile":' not in source
assert '"loot.cheapStashGrenadeQuantity"' in content
assert '"loot.cheapStashAmmoQuantity"' in content
assert '"AMMO QUANTITY · ALL TIERS"' in content
assert 'percentage: false, multiplier: true' in content
assert 'ToString(multiplier ? "0.##"' in coverage
for pid in ("loot.cheapStashAmmoQuantity", "loot.mediumStashAmmoQuantity",
            "loot.expensiveStashAmmoQuantity"):
    row = catalog_by_id[pid]
    assert row["group"] == "Stash Content"
    assert row["slider"] == {"min": 0.25, "max": 2, "step": 0.25, "stepMode": "absolute"}
    assert f'["{pid}"] = "StashContentMix"' in viewmodel
    assert f'"{pid}"' in main_vm
assert 'GlobalStashItemDescriptions' in coverage
assert "GetManifestResourceStream(SmartSource)" in branches
assert "public static string? SmartItemBiasId(string sid) => ItemBiasId(sid);" in effective
assert "public double TierFactor(string sid)" in effective
assert "public double SmartFactor(string sid)" in effective
assert "public double ChanceFactor(string sid)" in effective
assert "public double SelectionFactor(string sid, string field)" in effective
assert 'return MedicalDetailId(sid) is { } detail && Value(detail) <= 0;' in effective
assert "public double SmartSelectionFactor(string sid, string selectionField)" in effective
assert "if (SmartSelectionFactor(sid, selectionField) <= 0.0000001)" in effective
assert 'if (ItemBiasId(sid) is null) return raw;' in effective
# SmartLoot membership is checked directly inside SmartSelectionFactor;
# the old temporary 'var id = SmartItemBiasId(sid)' no longer exists.
# Check the actual guarded selection branch and retain Weight/Chance split.
smart_selection = effective.split("public double SmartSelectionFactor(", 1)[1].split(
    "public string SmartSelection(", 1)[0]
assert "if (SmartItemBiasId(sid) is null)" in smart_selection
assert 'throw new InvalidDataException("Unclassified SmartLoot item: " + sid)' in smart_selection
assert "return SelectionFactor(sid, selectionField);" in smart_selection
smart_factor = effective.split("public double SmartFactor(", 1)[1].split(
    "public double SmartSelectionFactor(", 1)[0]
assert "if (SmartItemBiasId(sid) is null)" in smart_factor
assert "return TierFactor(sid);" in smart_factor
assert "StashMixParameterIds.Any(modified.ContainsKey)" in tuning
assert "StashMixParameterIds.Concat(StashMedicalParameterIds)" in smart
assert "StashMixParameterIds.Concat(StashMedicalParameterIds)" in (
    root / "src/TrueCustomDifficulty/Services/CfgPatchWriterService.StashDlc1.cs"
).read_text()
assert "GetManifestResourceStream(SmartSource)" in branches
assert "Data\\smartloot-world-source-entries.csv" in csproj

tier_groups = sum(len(baseline[key]["templateCategories"]) for key in [
    "cheapStashProduction", "mediumStashAmmoProduction", "expensiveStashProduction"])
tier_items = sum(len(g["items"]) for key in [
    "cheapStashProduction", "mediumStashAmmoProduction", "expensiveStashProduction"]
    for g in baseline[key]["templateCategories"])
with (data / "smartloot-world-source-entries.csv").open(newline="") as stream:
    rows = list(csv.DictReader(stream))
group_keys = {(r["Prototype"], r["RankIndex"], r["Group"], r["GroupIndex"])
              for r in rows}
assert (tier_groups, tier_items, len(group_keys), len(rows)) == (41, 133, 422, 991)
assert all(r["Weight"] != r["Chance"] for r in rows)
assert "result.Count != 463" in branches and "result.Sum(x => x.Items.Count) != 1124" in branches
assert sum(1 for r in rows if r["Weight"]) > 0
assert sum(1 for r in rows if r["Chance"]) > 0
for doc in (lock, system, proposal):
    assert "STASHES GLOBAL ITEM-CATEGORY SOURCE COVERAGE (CURRENT)" in doc
    assert "STASHES PLAYER-FACING LOOT STATUS (CURRENT)" in doc
    assert "STASHES GLOBAL WEIGHT WITH NATIVE CHANCE EXCEPTIONS (CURRENT)" in doc
print("PASS: Basic rotary Weight + native source pools, Standard/Rare Chance-led tiles")

# Phase 5: emitted patch parity must be wired into the executable contract.
# This is a static integration gate, not a claim that the .NET/Wine suite ran.
phase5 = (root / "tests/LootSpecialGeneratorsContract/StashBranchParity.cs").read_text()
program = (root / "tests/LootSpecialGeneratorsContract/Program.cs").read_text()
assert "internal static partial class Program" in program
assert "VerifyStashBranchBrowserParity();" in program
assert "internal static partial class Program" in phase5
assert "StashBranchPreviewCatalog.Load(Baseline)" in phase5
assert "VerifyNativeEmittedFields(" in phase5
assert "new CfgPatchWriterService().Write(" in phase5
assert "ReadOutput(dir)" in phase5
assert "group.EffectiveSelection(Baseline, snapshot, row)" in phase5
assert "group.EffectiveCount(Baseline, snapshot, row" in phase5
assert "group.EffectiveSpawn(snapshot)" in phase5
assert "AssertProtectedBaseStashesUntouched(generated, name)" in phase5
assert "DlcEligibleActorIds.Value" in phase5
assert '"all-category-zero"' in phase5 and '"food-3x"' in phase5
assert '"hard-bonus-medical-dlc"' in phase5
assert 'Math.Abs(NativeValue(grenade, chance) - .8)' in phase5
assert 'Math.Abs(NativeValue(rareEasy, couponEasy) - .6)' in phase5
assert "public string SmartCount(" in effective
assert "effective.SmartCount(item.ItemSid, fieldName," in smart
assert "raw, item.MinCount, item.MaxCount, selectionField);" in smart
assert "state.SmartCount(item.ItemSid, field, raw," in branches
assert "item.MinCount, item.MaxCount, Field);" in branches
assert 'var half = NativeState(difficulty, (categoryId, .5));' in phase5
for doc in (lock, system, proposal):
    assert "STASHES PHASE 5 CFG PARITY GATE" in doc
assert "STASH-PHASE-5-CFG-PARITY-AND-ACCEPTANCE-2026-10-16.md" in (
    root / "docs/generators/STASH-GENERATOR-BRANCH-ARCHITECTURE-2026-10-16.md"
).read_text()
print("PASS: Phase 5 six-category global parity and emitted-CFG guard harness wired")
print("PENDING: cross-compiled .NET 10, Wine suite, wide/narrow visual and STALKER 2 gameplay")
