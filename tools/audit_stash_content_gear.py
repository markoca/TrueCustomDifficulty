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
gear = [f"loot.stashGear{tier}{kind}" for tier in
        ("Cheap", "Medium", "Expensive") for kind in ("Weapon", "Armor")]
medical = ["loot.stashMedical" + kind + "Bias" for kind in
           ("Bandage", "Medkit", "ArmyMedkit", "AntiRad")]
quantity = ["loot.stash" + kind + end + "Count" for kind in
            ("Medkit", "ArmyMedkit") for end in ("Min", "Max")]

assert len(catalog["parameters"]) == baseline["supportedParameterCount"] == 268
assert baseline["special"]["stashContentAndGearProduction"]["parameterIds"] == mix + gear
assert baseline["special"]["stashMedicalProduction"]["parameterIds"] == medical + quantity
assert len({*mix, *gear, *medical, *quantity}) == 20
assert all(parameters[x]["tab"] == "stashes" and
           parameters[x]["control"] == "slider" for x in mix + gear + medical + quantity)
for name in mix:
    p = parameters[name]
    assert (p["slider"]["min"], p["slider"]["max"], p["slider"]["step"]) == (0, 3, 0.1)
    assert p["vanilla"]["All"] == 1
    assert len(p["targets"]) == 2
    assert "Weight" in p["targets"][0]["path"]
    assert all("Chance" not in entry["path"] for entry in p["targets"])
for name in gear:
    p = parameters[name]
    assert (p["slider"]["min"], p["slider"]["max"], p["slider"]["step"]) == (0, 3, 1)
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
assert "DrawStashWeightMixer(dc, new Rect(left, 38.0, width," in ui
assert "width >= 670.0 ? 262.0 : 496.0" in ui
assert "=> width >= 700.0 ? 634.0 : 986.0;" in ui
assert "DrawStashTierWeightPools(dc, new Rect(left," in ui
assert "narrowPreview ? 302.0 : 244.0" in ui
assert "private void DrawStashTierWeightPools(" in mixer
tier_preview = mixer.split("private void DrawStashTierWeightPools(", 1)[1]
assert "StashSourceGroups.Value" in tier_preview
assert "!g.IsSmart && g.Tier == tier.SourceTier" in tier_preview
assert "hasWeight[kind] = true" in tier_preview
assert "hasChance[kind] = true" in tier_preview
assert "hasWeight[index] || hasChance[index]" in tier_preview
assert 'number + "x + CHANCE"' in tier_preview
assert '"CHANCE ONLY"' in tier_preview
assert '"STASH CONTENT BY TIER"' in tier_preview
assert '"WEIGHT = SCALAR · OUTLINED = INDEPENDENT CHANCE · NOT DROP ODDS"' in tier_preview
assert "100.0 * totals" not in tier_preview
assert "TierField(" not in tier_preview
assert "AddHorizontalTarget(" not in tier_preview
assert "const double rowPitch = 78.0;" in mixer
assert "var columns = outer.Width >= 670.0 ? 2 : 1;" in mixer
assert "AddHorizontalTarget(p," in mixer
assert "foreach (var stop in new[] { 0.0, 1.0, 3.0 })" in mixer
assert "p.IsValueModifiedFromReference()" in mixer
assert "formatted.SetForegroundBrush(accent, 0, digits.Length)" in mixer
assert "CurrentStashEffectiveState()" in mixer
assert "GlobalStashDetail(i, status, state)" in mixer
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

assert 'DrawStashGearMode(dc, P(tiers[i].Weapon)' in ui
assert 'DrawStashGearMode(dc, P(tiers[i].Armor)' in ui
assert '\"StashGear\" => StashGearHeight(width)' in ui
assert '\"StashMedicalComposition\" => StashMedicalCompositionHeight(width)' in ui
assert '\"StashMedicalQuantity\" => StashMedicalQuantityHeight(width)' in ui
assert 'DrawStashMedicalSourcePreview(dc,' in ui
assert 'DrawStashQuantityDots(dc,' in ui
assert '\"0 ITEMS\"' in ui and '\"AUTO\"' in ui
assert "IsStashMedicalSelectionDisabled(" in writer
assert "TuneStashMedicalCountField(" in writer
assert "StashState(all).TuneSelection(" in writer
assert "StashContentMix" in vm and "independent native Chance rolls" in vm
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
assert "WriteExtraStashGear(" in writer
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

print("PASS: 20 Stash CFG IDs, 6-lane Weight mixer, three complete native tier category/Weight/Chance strips; no invented tier drop percentages.")
print("NOT RUN: Windows .NET publish, Wine interaction, real game RNG acceptance.")
