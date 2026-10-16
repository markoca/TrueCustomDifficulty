#!/usr/bin/env python3
"""Static source/control audit for Stashes mixed native editors and bias previews.

This does not claim a .NET/Wine render or gameplay probability validation.
"""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = (root / "src/TrueCustomDifficulty/Controls/AggregateVisualControl.cs").read_text()
catalog = json.loads((root / "src/TrueCustomDifficulty/Data/parameters.json").read_text())
lock = (root / "docs/UI-DESIGN-LOCK.md").read_text()
system = (root / "docs/VISUAL-CONTROL-SYSTEM.md").read_text()
proposal = (root / "docs/STASHES-TAB-AND-REDESIGN-PROPOSAL.md").read_text()

expected = {
    "StashRewardProfile": [
        "loot.cheapStashAmmoQuantity", "loot.mediumStashAmmoQuantity",
        "loot.expensiveStashAmmoQuantity", "loot.cheapStashGrenadeChance",
        "loot.cheapStashGrenadeQuantity", "loot.expensiveStashMoneyChance"],
    "StashContentMix": [
        "loot.stashMixAmmo", "loot.stashMixMedical", "loot.stashMixFood",
        "loot.stashMixDrinks", "loot.stashMixGrenades", "loot.stashMixCoupons"],
    "StashMedicalComposition": [
        "loot.stashMedicalBandageBias", "loot.stashMedicalMedkitBias",
        "loot.stashMedicalArmyMedkitBias", "loot.stashMedicalAntiRadBias"],
    "StashMedicalQuantity": [
        "loot.stashMedkitMinCount", "loot.stashMedkitMaxCount",
        "loot.stashArmyMedkitMinCount", "loot.stashArmyMedkitMaxCount"],
    "StashGear": [
        "loot.stashGearCheapWeapon", "loot.stashGearCheapArmor",
        "loot.stashGearMediumWeapon", "loot.stashGearMediumArmor",
        "loot.stashGearExpensiveWeapon", "loot.stashGearExpensiveArmor"],
}
ids = [p["id"] for p in catalog["parameters"] if p["tab"] == "stashes"]
assert len(ids) == len(set(ids)) == 26
assert set(ids) == set(sum(expected.values(), []))
family_ids = source.split("private static IReadOnlyList<string> GetFamilyIds(", 1)[1]
for family, values in expected.items():
    match = re.search(r'"' + re.escape(family) + r'"\s*=>\s*new\[\]\s*\{(.*?)\}', family_ids, re.S)
    assert match, family
    assert re.findall(r'"(loot\.[A-Za-z0-9.]+)"', match.group(1)) == values, family
assert next(x for x in catalog["tabs"] if x["id"] == "stashes")["order"] == 55

def method(name, successor):
    start = source.index("    private void " + name + "(")
    end = source.index("    private void " + successor + "(", start + 10)
    return source[start:end]

content = method("DrawStashContentMix", "DrawStashMedicalComposition")
medical = method("DrawStashMedicalComposition", "DrawStashMedicalQuantity")
quantity = method("DrawStashMedicalQuantity", "DrawStashGear")
gear = method("DrawStashGear", "DrawStashGearMode")
preview = method("DrawStashBiasPreview", "DrawCompactStashRewards")

assert content.count('("loot.stashMix') == 6
assert medical.count('("loot.stashMedical') == 4
assert quantity.count('("MEDKITS"') + quantity.count('("ARMY MEDKITS"') == 2
assert gear.count('("BASIC"') + gear.count('("STANDARD"') + gear.count('("RARE"') == 3
assert 'DrawSourceKnob(dc, source, entries[i].Label' in content
assert 'DrawStashBiasPreview(dc, entries,' in content
assert 'DrawStashRewardGraphRow(dc, P(entries[i].Id)' in medical
assert 'DrawStashBiasPreview(dc, entries,' in medical
assert "DrawStashQuantityDots(dc, P(families[i].Min)" in quantity
assert "DrawStashQuantityDots(dc, P(families[i].Max)" in quantity
assert "DrawStashGearMode(dc, P(tiers[i].Weapon)" in gear
assert "DrawStashGearMode(dc, P(tiers[i].Armor)" in gear
assert "DrawMiniConsumableKnob" not in content + medical + quantity + gear
assert "DrawStashCountKnob" not in source
assert "DrawStashGearDial" not in source
assert "DrawHorizontalTrackCard" not in content + medical + quantity + gear
assert "AddHorizontalTarget(source" in quantity and "AddHorizontalTarget(source" in source
assert 'new[] { "0 ITEMS", "AUTO", "1", "2", "3", "4", "5", "6" }' in quantity
assert "var minimum = isMax ? -1.0 : 0.0;" in quantity
assert 'new[] { "OFF", "RARE", "MOD", "FREQ" }' in source
assert "DrawStashNumericReadout(dc, display" in quantity
assert "HitKind.Horizontal" in source and "HitKind.Knob" in source
assert "target.Kind == HitKind.Knob" in source
assert 'or "StashMedicalQuantity" or "StashGear"' in source
assert 'DrawCompactStashRewards(' in source
assert '"StashRewardProfile" => 398.0' in source

# Preview must be noninteractive, explicitly NOT a probability estimate, and
# must avoid fabricated nonzero percentages when every bias is disabled.
assert 'MULTIPLIERS, NOT DROP ODDS' in preview
assert "Math.Max(0.0, P(e.Id)?.CurrentNumber ?? 0.0)" in preview
assert "weights.Sum()" in preview
assert "sum > Epsilon ? weights[i] / sum : 0.0" in preview
assert "sum > Epsilon ? weights[i] * 100.0 / sum : 0.0" in preview
assert "AddHorizontalTarget" not in preview and "AddRangeTarget" not in preview
assert "DrawStashNumericReadout" not in preview  # derived % always neutral

# Same breakpoints and measured last-card clearance as renderers.
assert "var sizeWeights = new[] { 1.28, 1.28, 1.05, 1.05, .92, .92 };" in content
assert "var dialScales = new[] { 1.24, 1.24, .98, .98, .78, .78 };" in content
assert "var legendColumns = ActualWidth >= 700.0 ? 6 : 3;" in content
assert "dialAccent: StashMixPalette[i], dialScale: dialScales[i]" in content
assert "DrawRoundedRectangle" not in content  # no per-knob bounding boxes
assert "var card = new Rect(cursor, 49.0, cell, 176.0);" in content
assert "new Rect(left, 237.0, width, previewHeight)" in content
assert "var dialInk = dialAccent ?? accent;" in source
assert "new SolidColorBrush(Color.FromRgb(" in source
assert source.count("new SolidColorBrush(Color.FromRgb(") >= 6
assert "var isContentMix = entries.Length == 6" in preview
assert "StashMixPalette[index]" in preview
assert "CategoryBrush(i)" in preview
assert "ActualWidth >= 700.0 ? 2 : 1" in quantity
assert "ActualWidth >= 746.0 ? 3 : ActualWidth >= 580.0 ? 2 : 1" in gear
assert "=> width >= 700.0 ? 360.0 : 385.0;" in source
assert "=> 373.0;" in source
assert "=> width >= 700.0 ? 220.0 : 383.0;" in source
assert "=> width >= 746.0 ? 220.0 : width >= 580.0 ? 383.0 : 546.0;" in source
assert 'var endpointInk = stashDial ? Brushes.White : muted;' in source
assert "DrawStashNumericReadout(dc, display" in source

for name, doc in [("UI-DESIGN-LOCK", lock), ("VISUAL-CONTROL-SYSTEM", system),
                  ("STASHES-TAB-AND-REDESIGN-PROPOSAL", proposal)]:
    assert "STASH CONTENT MIX SINGLE-ROW COLOR-KEYED DIALS" in doc, name
    assert "STASHES MIXED CONTROLS + LIVE BIAS FEEDBACK" in doc, name
    assert "NOT" in doc and "drop" in doc.lower(), name

print("PASS: five families, 26 persisted sources, independent native editors")
print("PASS: live noninteractive bias preview, 0x guard, source-native quantity/gear")
print("PASS: responsive measurements and matched authoritative UI catalogs")
print("PENDING: local .NET build, Wine appearance and real game outcome tests")
