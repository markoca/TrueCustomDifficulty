#!/usr/bin/env python3
"""Current Stash Content Mix/Extra Gear UI and native source contract.

This is static source verification only: no Wine, .NET execution, or
in-game loot probability can be inferred from these assertions.
"""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
src = root / "src/TrueCustomDifficulty"
data = src / "Data"
catalog = json.loads((data / "parameters.json").read_text(encoding="utf-8"))
baseline = json.loads((data / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
parameters = {p["id"]: p for p in catalog["parameters"]}
mix = ["loot.stashMix" + category for category in (
    "Ammo", "Medical", "Food", "Drinks", "Grenades", "Coupons")]
gear = ["loot.stashGearCoveragePercent"]
medical = ["loot.stashMedical" + kind + "Bias" for kind in
           ("Bandage", "Medkit", "ArmyMedkit", "AntiRad")]
quantity = ["loot.stash" + kind + end + "Count" for kind in
            ("Medkit", "ArmyMedkit") for end in ("Min", "Max")]

assert len(catalog["parameters"]) == 276
assert baseline["supportedParameterCount"] == 276
assert baseline["special"]["stashContentAndGearProduction"]["parameterIds"] == mix + gear + ["loot.stashGearEnabled"] + [
    "loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance", "loot.mediumStashCouponsChance",
    "loot.expensiveStashAmmoChance", "loot.expensiveStashMedicalChance", "loot.expensiveStashFoodChance",
    "loot.expensiveStashDrinksChance", "loot.expensiveStashGrenadesChance"]
assert baseline["special"]["stashMedicalProduction"]["parameterIds"] == medical + quantity + [
    "loot.stashMedical" + kind + "ChanceScale" for kind in ("Bandage", "Medkit", "ArmyMedkit", "AntiRad")]
assert len({*mix, *gear, *medical, *quantity}) == 15
assert all(parameters[x]["tab"] == "stashes" and
           parameters[x]["control"] == "slider" for x in mix + gear + medical + quantity)
for name in mix:
    p = parameters[name]
    assert (p["slider"]["min"], p["slider"]["max"], p["slider"]["step"]) == (0, 1, 0.1)
    assert p["vanilla"]["All"] == 1
    assert len(p["targets"]) == 2
    assert "Weight" in p["targets"][0]["path"]
    assert all("Chance" not in entry["path"] for entry in p["targets"])
# The mixer only displays four source-effective categories; the old Ammo
# and Grenades backend IDs remain hidden to preserve legacy preset loading.
for name in ("loot.stashMixAmmo", "loot.stashMixGrenades"):
    assert parameters[name]["exposeInUi"] is False
for name in ("loot.stashMixMedical", "loot.stashMixFood",
             "loot.stashMixDrinks", "loot.stashMixCoupons"):
    assert parameters[name]["exposeInUi"] is True

assert parameters["loot.stashGearEnabled"]["control"] == "toggle"
assert parameters["loot.stashGearEnabled"]["vanilla"]["All"] is False
for name in gear:
    p = parameters[name]
    assert p["requiresToggle"] == "loot.stashGearEnabled"
    assert (p["slider"]["min"], p["slider"]["max"], p["slider"]["step"]) == (0, 100, 1)
    assert p["vanilla"]["All"] == 0
for name in medical:
    p = parameters[name]
    assert (p["slider"]["min"], p["slider"]["max"], p["slider"]["step"]) == (0, 3, .25)
for name in quantity:
    p = parameters[name]
    assert (p["slider"]["min"], p["slider"]["max"], p["slider"]["step"]) == (
        -1 if name.endswith("MaxCount") else 0, 6, 1)

ui = (src / "Controls/AggregateVisualControl.cs").read_text(encoding="utf-8")
mixer = (src / "Controls/AggregateVisualControl.StashBranch.cs").read_text(encoding="utf-8")
vm = (src / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
writer = (src / "Services/CfgPatchWriterService.StashTuning.cs").read_text(encoding="utf-8")
assert "DrawStashWeightMixer(dc, new Rect(left, 53.0, width," in ui
assert "DrawStashBasicNativePools(dc," in ui
assert '"RGD-5 AMOUNT"' in ui
assert "percentage: false" in ui
assert "wide ? 146.0 : 300.0" in ui
assert "=> width >= 700.0 ? 637.0 : 1019.0;" in ui
assert 'DrawStashChanceTier(dc, "STANDARD", "Medium", 10,' in ui
assert "wide ? 100.0 : 177.0" in ui
assert "private void DrawStashChanceTier(" in mixer
tier_preview = mixer.split("private void DrawStashChanceTier(", 1)[1]
assert "StashSourceGroups.Value" in tier_preview
assert "!g.IsSmart && g.Tier == tierName" in tier_preview
assert "hasWeight[kind] = true" in tier_preview
assert "hasChance[kind] = true" in tier_preview
assert "hasWeight[k] || hasChance[k]" in tier_preview
assert '"WEIGHT ONLY"' in tier_preview
assert '"NATIVE CHANCE"' in tier_preview
assert '"NATIVE CHANCE"' in tier_preview
assert '"NATIVE CHANCE"' in tier_preview
assert "100.0 * totals" not in tier_preview
assert "EffectiveSelection(" in tier_preview
assert "AddHorizontalTarget(parameter," in tier_preview
assert "const double cardHeight = 146.0;" in mixer
assert "BasicVisibleWeightKinds = { 1, 2, 3, 5 }" in mixer
assert "previewGroups.Where(g => g is not null)" in mixer
assert "var columns = outer.Width >= 670.0 ? 4 : 2;" in mixer
assert "AddHorizontalTarget(p," in mixer
assert "foreach (var stop in new[] { p.Minimum, p.Maximum })" in mixer
assert "p.IsValueModifiedFromReference()" in mixer
assert "number.SetForegroundBrush(accent, 0, digits.Length)" in mixer
assert "CurrentStashEffectiveState()" in ui
assert "DrawStashBasicNativePools(" in mixer
assert '"WEIGHT OFF · INDEPENDENT CHANCE MAY STILL ROLL"' in mixer
assert "Normalize(" not in mixer and "percentages" not in mixer
assert "DrawStashGlobalCoverage(" not in ui + mixer
assert '"STASH ITEM WEIGHTS"' not in mixer
assert "DrawSourceKnob(" not in ui.split("private void DrawStashContentMix(", 1)[1].split(
    "private void DrawStashMedicalComposition(", 1)[0]
ids_match = re.search(
    r"private static readonly string\[\] GlobalStashParameterIds\s*=\s*\{(.*?)\};",
    mixer, re.S)
assert ids_match, "missing mixer source binding array"
assert re.findall(r'"(loot\.stashMix[A-Za-z]+)"', ids_match.group(1)) == mix

assert 'DrawTogglePill(dc, P("loot.stashGearEnabled")' in ui
assert 'P("loot.stashGearCoveragePercent")' in ui
assert "DrawStashGearMode(" not in ui
assert '\"StashGear\" => StashGearHeight(width)' in ui
assert '\"StashMedicalComposition\" => StashMedicalCompositionHeight(width)' in ui
assert '\"StashMedicalQuantity\" => StashMedicalQuantityHeight(width)' in ui
assert 'DrawStashMedicalSourcePreview(dc,' in ui
assert 'DrawStashQuantityDots(dc,' in ui
assert '\"0 ITEMS\"' in ui and '\"AUTO\"' in ui
assert "IsStashMedicalSelectionDisabled(" in writer
assert "TuneStashMedicalCountField(" in writer
assert "StashState(all).TuneSelection(" in writer
assert "StashContentMix" in vm and "source-native per-entry Chance controls" in vm
assert "StashMedicalComposition" in vm
for doc in ("UI-DESIGN-LOCK.md", "VISUAL-CONTROL-SYSTEM.md",
            "STASHES-TAB-AND-REDESIGN-PROPOSAL.md",
            "STASH-CONTENT-GEAR-IMPLEMENTATION.md"):
    text = (root / "docs" / doc).read_text(encoding="utf-8")
    assert "STASHES COMPACT RELATIVE WEIGHT MIXER" in text or (
        doc == "STASH-CONTENT-GEAR-IMPLEMENTATION.md"
        and "COMPACT STASH WEIGHT MIXER" in text)
# Keep the source-native writer and regression harness gates: changing
# the mixer must never silently weaken inventory, actor protection, or
# per-field Weight/Chance behavior.
effective = (src / "Services/StashEffectiveState.cs").read_text(encoding="utf-8")
smart = (src / "Services/CfgPatchWriterService.StashSmartLoot.cs").read_text(encoding="utf-8")
production = (src / "Services/CfgPatchWriterService.cs").read_text(encoding="utf-8")
program = (root / "tests/LootSpecialGeneratorsContract/Program.cs").read_text(encoding="utf-8")
assert "public bool MedicalDisabled(string sid)" in effective
assert "public double TierFactor(string sid)" in effective
assert "public double ChanceFactor(string sid)" in effective
assert "public double SelectionFactor(string sid, string field)" in effective
assert '"Weight" => TierFactor(sid)' in effective
assert '"Chance" => ChanceFactor(sid)' in effective
assert "public double SmartSelectionFactor(string sid, string selectionField)" in effective
assert "SmartSelectionFactor(item.ItemSid, selectionField)" in smart
assert "StashMixParameterIds.Concat(StashMedicalParameterIds)" in smart
assert "WriteWorldStashSmartLoot(" in smart
assert "GetOrAddReferencedRoot(" in smart
assert "WriteExtraStashGear(" not in writer
coverage_writer = (src / "Services/CfgPatchWriterService.StashGearCoverage.cs").read_text(encoding="utf-8")
assert "WriteStashGearCoverage(" in coverage_writer
assert "SelectStashGearActors(" in coverage_writer
assert "LoadStashGearPool(" in coverage_writer
assert "LoadStashGearActors(" in coverage_writer
assert "MarkModifiedStashTuning(" in production
assert "VerifyMedicalZeroExclusion();" in program
assert "VerifyWorldStashSmartLoot();" in program
assert "VerifyStashEffectiveParity();" in program
assert "VerifyStashBranchBrowserParity();" in program
assert "GetFamilyIds(" in ui and '\"StashContentMix\" => new[]' in ui
# The 463 native groups and 1,124 original item entries are backend
# parity evidence only. Never report them as a combined game drop chance.
assert "groups.Count != 463" in mixer
assert "groups.Sum(x => x.Items.Count) != 1124" in mixer
assert not re.search(r'new\[\] \{ .*?1\.0 / 6', mixer)

print("PASS: four visible 0–1x native Basic weight knobs; six legacy IDs retained, source-native Chance kept separate.")
print("NOT RUN: Windows .NET publish, Wine interaction, real game RNG acceptance.")
