#!/usr/bin/env python3
"""Current Stash Content Mix/Extra Gear UI and native source contract.

This is static source verification only: no Wine, .NET execution, or
in-game loot probability can be inferred from these assertions.
"""
import json
import re
import sys
from pathlib import Path

root = Path(__file__).resolve().parents[1]
src = root / "src/TrueCustomDifficulty"
data = src / "Data"
catalog = json.loads((data / "parameters.json").read_text(encoding="utf-8"))
baseline = json.loads((data / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
parameters = {p["id"]: p for p in catalog["parameters"]}
mix = ["loot.stashMix" + category for category in (
    "Ammo", "Medical", "Food", "Drinks", "Grenades", "Coupons")]
gear = [
    "loot.stashGearCoveragePercent", "loot.stashGearEnabled",
    "loot.stashGearWeaponWeight", "loot.stashGearArmorWeight",
    "loot.stashGearArtifactWeight", "loot.stashGearAttachmentWeight",
    "loot.stashGearBonusChance",
]
medical = ["loot.stashMedical" + kind + "Bias" for kind in
           ("Bandage", "Medkit", "ArmyMedkit", "AntiRad")]
quantity = ["loot.stash" + kind + end + "Count" for kind in
            ("Medkit", "ArmyMedkit") for end in ("Min", "Max")]

assert len(catalog["parameters"]) == baseline["supportedParameterCount"] == 287
# A master checkbox is ONE routable source ID, not an extra duplicate.
# Match exact production dispatch order and require unique members so a
# superseded Stashes roster cannot silently pass on set equivalence alone.
tier_chance = [
    "loot.standardStashChanceScale", "loot.rareStashChanceScale",
    "loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance", "loot.mediumStashCouponsChance",
    "loot.expensiveStashAmmoChance", "loot.expensiveStashMedicalChance", "loot.expensiveStashFoodChance",
    "loot.expensiveStashDrinksChance", "loot.expensiveStashGrenadesChance",
]
stash_routes = baseline["special"]["stashContentAndGearProduction"]["parameterIds"]
assert stash_routes == mix + gear + tier_chance, (
    "Stashes production route differs from 6 mixer + 7 Extra Gear + "
    "2 common scale + 8 original category Chance IDs")
assert len(stash_routes) == len(set(stash_routes)) == 23, (
    "Duplicate or missing Stashes writer route")
assert stash_routes.count("loot.stashGearEnabled") == 1
assert baseline["special"]["stashMedicalProduction"]["parameterIds"] == medical + quantity + [
    "loot.stashMedical" + kind + "ChanceScale" for kind in ("Bandage", "Medkit", "ArmyMedkit", "AntiRad")]
assert len({*mix, *gear, *medical, *quantity}) == 21
assert all(parameters[x]["tab"] == "stashes" and
           parameters[x]["control"] == "slider"
           for x in mix + [p for p in gear if p != "loot.stashGearEnabled"] + medical + quantity)
assert parameters["loot.stashGearEnabled"]["tab"] == "stashes"
for name in mix:
    p = parameters[name]
    assert (p["slider"]["min"], p["slider"]["max"], p["slider"]["step"]) == (0, 1, 0.1)
    assert p["vanilla"]["All"] == 1
    assert len(p["targets"]) == 2
    assert "Weight" in p["targets"][0]["path"]
    assert all("Chance" not in entry["path"] for entry in p["targets"])
# The remaining three knobs all belong to the retained Veteran source
# branch. Ammo, grenades and coupons remain persisted legacy sources.
for name in ("loot.stashMixAmmo", "loot.stashMixGrenades", "loot.stashMixCoupons"):
    assert parameters[name]["exposeInUi"] is False
for name in ("loot.stashMixMedical", "loot.stashMixFood",
             "loot.stashMixDrinks"):
    assert parameters[name]["exposeInUi"] is True

assert parameters["loot.stashGearEnabled"]["control"] == "toggle"
assert parameters["loot.stashGearEnabled"]["vanilla"]["All"] is False
for name in gear:
    if name == "loot.stashGearEnabled":
        continue
    p = parameters[name]
    assert p["requiresToggle"] == "loot.stashGearEnabled"
    slider = p["slider"]
    if name.endswith("Weight"):
        assert (slider["min"], slider["max"], slider["step"]) == (0, 3, 0.1)
        assert p["vanilla"]["All"] == 1
    else:
        assert (slider["min"], slider["max"], slider["step"]) == (0, 100, 1)
        assert p["vanilla"]["All"] == (50 if name.endswith("BonusChance") else 0)
for name in medical:
    p = parameters[name]
    assert (p["slider"]["min"], p["slider"]["max"], p["slider"]["step"]) == (0, 3, .25)
for name in quantity:
    p = parameters[name]
    assert (p["slider"]["min"], p["slider"]["max"], p["slider"]["step"]) == (
        -1 if name.endswith("MaxCount") else 0, 6, 1)

# CFG-only selection: preserve the default full UI audit separately.
if sys.argv[1:] == ["--cfg-only"]:
    main = (src / "Services/CfgPatchWriterService.cs").read_text(encoding="utf-8")
    tuning = (src / "Services/CfgPatchWriterService.StashTuning.cs").read_text(encoding="utf-8")
    smart = (src / "Services/CfgPatchWriterService.StashSmartLoot.cs").read_text(encoding="utf-8")
    gear_writer = (src / "Services/CfgPatchWriterService.StashGearCoverage.cs").read_text(encoding="utf-8")
    removals = (src / "Services/CfgPatchWriterService.StashAuthoredWeapons.cs").read_text(encoding="utf-8")
    entry = main.split("public CfgWriteResult Write(", 1)[1].split("private static void ValidateBaseline", 1)[0]
    for method in ("WriteCheapStashProduction", "WriteMediumStashAmmoProduction",
                   "WriteExpensiveStashProduction", "WriteWorldStashSmartLoot",
                   "WriteStashGearCoverage", "WriteStashAuthoredWeaponRemovals"):
        assert method + "(" in entry, "Missing production dispatch: " + method
    assert "StashMixParameterIds.Any(modified.ContainsKey)" in tuning
    assert "StashMedicalParameterIds.Any(id =>" in tuning
    assert "StashTierChanceParameterIds.Any(id =>" in tuning
    assert "StashMixParameterIds.Concat(StashMedicalParameterIds)" in smart
    assert "StashState(all).TuneSelection(" in tuning
    assert "GetOrAddReferencedRoot(" in smart
    assert 'GetDocument(documents, "GameData/ItemGeneratorPrototypes.cfg")' in gear_writer
    assert 'GetDocument(documents, "GameData/ItemGeneratorPrototypes.cfg")' in removals
    assert "document.AddRemovalNode(row.GeneratorSid" in removals
    assert 'all.TryGetValue("loot.stashGearBonusChance", out var chanceControl)' in gear_writer
    assert 'item.SetAssignment("Chance", itemChance)' in gear_writer
    effective = (src / "Services/StashEffectiveState.cs").read_text(encoding="utf-8")
    assert '"Weight" => TierFactor(sid)' in effective
    assert '"Chance" => ChanceFactor(sid)' in effective
    print("PASS: Stashes CFG route ownership, Weight/Chance logic, generator emission paths and authored removals")
    print("NOT RUN: UI layout, C# executable writer, emitted patch verification")
    raise SystemExit(0)
if sys.argv[1:]:
    raise SystemExit("usage: audit_stash_content_gear.py [--cfg-only]")

ui = (src / "Controls/AggregateVisualControl.cs").read_text(encoding="utf-8")
mixer = (src / "Controls/AggregateVisualControl.StashBranch.cs").read_text(encoding="utf-8")
vm = (src / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
writer = (src / "Services/CfgPatchWriterService.StashTuning.cs").read_text(encoding="utf-8")
assert "DrawStashWeightMixer(dc, new Rect(left, 141.0, width," in ui
assert "DrawStashBasicNativePools(dc," in ui
assert '"RGD-5 AMOUNT"' in ui
assert "percentage: false" in ui
assert "wide ? 146.0 : 300.0" in ui
assert "=> width >= 700.0 ? 839.0 : 1185.0;" in ui
assert 'DrawStashChanceTier(dc, "STANDARD", "Medium", 10,' in ui
assert "wide ? 140.0 : 236.0" in ui
assert "private void DrawStashChanceTier(" in mixer
tier_preview = mixer.split("private void DrawStashChanceTier(", 1)[1]
assert "StashSourceGroups.Value" in tier_preview
assert "!g.IsSmart && g.Tier == tierName" in tier_preview
assert "hasWeight[kind] = true" in tier_preview
assert "hasChance[kind] = true" in tier_preview
assert "hasWeight[k] || hasChance[k]" in tier_preview
assert 'var chanceKinds = present.Where(k => hasChance[k]).ToArray();' in tier_preview
assert 'CreateText(label, 11.4, text' in tier_preview
assert '"loot.mediumStashCouponsChance"' in tier_preview
assert '"loot.expensiveStashMoneyChance"' in tier_preview
assert "100.0 * totals" not in tier_preview
assert "EffectiveSelection(" in tier_preview
assert "AddHorizontalTarget(parameter," in tier_preview
assert "const double cardHeight = 146.0;" in mixer
assert "BasicVisibleWeightKinds = { 1, 2, 3 }" in mixer
assert "previewGroup.Items" in mixer
assert "var columns = outer.Width >= 670.0 ? 3 : 2;" in mixer
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
assert '\"StashMedicalQuantity\" => StashMedicalQuantityHeight(width)' not in ui
assert 'DrawStashMedicalPairShares(dc,' in ui
assert 'var shareWidth = Math.Max(1.0, innerWidth * .64);' in ui
assert '_medicalWeightRankTargets.Add((rect, rank));' in ui
assert 'source.count' not in ui
assert ui.count('visualMaximum: 1.0') == 3
assert 'DrawStashAntiRadWeightSwitch(dc,' in ui
assert 'DrawStashMedicalModeSelector(' not in ui
assert '=> width >= 700.0 ? 585.0 : 1001.0;' in ui
assert 'DrawStashQuantityDualRail(dc,' in ui
assert 'value < -.5 ? "0"' in ui and '"VANILLA"' in ui
assert "IsStashMedicalSelectionDisabled(" in writer
assert "TuneStashMedicalCountField(" in writer
assert "StashState(all).TuneSelection(" in writer
assert "StashContentMix" in vm and "AMMO QUANTITY" in vm
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

print("PASS: three visible 0–1x Basic weight knobs and one native Veteran mix bar; six source IDs retained, coupon Chance untouched.")
print("NOT RUN: Windows .NET publish, Wine interaction, real game RNG acceptance.")
