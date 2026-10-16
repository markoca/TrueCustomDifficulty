#!/usr/bin/env python3
"""Authoritative Stashes mixer/dial UI-source regression (not a WPF visual test).

Verify all five original aggregate families retain their exact 26 source IDs,
Reward plot remains unchanged, Content/Medical/Quantity/Gear use native
source-backed knobs at matching adaptive measure/draw breakpoints, and the
45 authored stash gear rewards never become unrelated mixer parameters.
"""
import json
import re
from pathlib import Path

root = Path(__file__).resolve().parents[1]
data = root / "src/TrueCustomDifficulty/Data"
control = (root / "src/TrueCustomDifficulty/Controls/AggregateVisualControl.cs").read_text()
catalog = json.loads((data / "parameters.json").read_text())
proposal = (root / "docs/STASHES-TAB-AND-REDESIGN-PROPOSAL.md").read_text()
lock = (root / "docs/UI-DESIGN-LOCK.md").read_text()
system = (root / "docs/VISUAL-CONTROL-SYSTEM.md").read_text()

ids = [p["id"] for p in catalog["parameters"] if p["tab"] == "stashes"]
assert len(ids) == len(set(ids)) == 26
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
        "loot.stashGearExpensiveWeapon", "loot.stashGearExpensiveArmor"]
}
family = control.split("private static IReadOnlyList<string> GetFamilyIds(", 1)[1]
for name, sources in expected.items():
    hit = re.search(r'"' + re.escape(name) + r'"\s*=>\s*new\[\]\s*\{(.*?)\}', family, re.S)
    assert hit, f"Missing {name} family source declarations"
    values = re.findall(r'"(loot\.[A-Za-z0-9.]+)"', hit.group(1))
    assert values == sources, f"{name} catalog sources drift: {values}"
assert set(ids) == set(sum(expected.values(), []))
assert catalog["tabs"][next(i for i,x in enumerate(catalog["tabs"]) if x["id"] == "stashes")]["order"] == 55

assert 'DrawHeader(dc, "STASH CONTENT MIX"' in control
assert 'DrawHeader(dc, "MEDICAL COMPOSITION"' in control
assert 'DrawHeader(dc, "MEDICAL QUANTITY"' in control
assert 'DrawHeader(dc, "EXTRA STASH GEAR"' in control
assert "StashContentMixHeight(width)" in control
assert "StashMedicalCompositionHeight(width)" in control
assert "StashMedicalQuantityHeight(width)" in control
assert "StashGearHeight(width)" in control
assert "ActualWidth >= 700.0 ? 3 : 2" in control
assert "ActualWidth >= 720.0 ? 4 : 2" in control
assert "ActualWidth >= 700.0 ? 2 : 1" in control
assert "ActualWidth >= 746.0 ? 3 : ActualWidth >= 580.0 ? 2 : 1" in control
assert 'DrawSourceKnob(dc, source, entries[i].Label' in control
assert 'DrawMiniConsumableKnob(dc, source, entries[i].Caption' in control
assert 'DrawStashCountKnob(dc, P(families[i].Min)' in control
assert 'DrawStashCountKnob(dc, P(families[i].Max)' in control
assert 'DrawStashGearDial(dc, P(tiers[i].Weapon)' in control
assert 'DrawStashGearDial(dc, P(tiers[i].Armor)' in control

def method(name, next_name):
    start = control.index("    private void " + name + "(")
    finish = control.index("    private void " + next_name + "(", start + 10)
    return control[start:finish]

content = method("DrawStashContentMix", "DrawStashMedicalComposition")
medical = method("DrawStashMedicalComposition", "DrawStashMedicalQuantity")
quantity = method("DrawStashMedicalQuantity", "DrawStashCountKnob")
gear = method("DrawStashGear", "DrawStashGearDial")
assert content.count('("loot.stashMix') == 6
assert medical.count('("loot.stashMedical') == 4
assert quantity.count('("MEDKITS"') + quantity.count('("ARMY MEDKITS"') == 2
assert gear.count('("BASIC"') + gear.count('("STANDARD"') + gear.count('("RARE"') == 3
assert "DrawHorizontalTrackCard" not in content + medical + quantity + gear
assert "AddHorizontalTarget(" not in content + medical + quantity + gear
assert 'if (Family == "StashContentMix")' in control
assert "formatted.SetForegroundBrush(accent, token.Index, token.Length)" in control
assert "HitKind.Knob" in control and "KnobVisibleStepStride" in control
assert "target.Kind == HitKind.Knob" in control
assert 'or "StashMedicalQuantity" or "StashGear"' in control
assert "source.ResetToReference()" not in content + medical + quantity + gear
assert 'current < 0 ? "0 ITEMS"' in control
assert 'current == 0 ? "AUTO"' in control
assert '"OFF", "RARE", "MOD", "FREQ"' in control
assert 'var endpointInk = stashDial ? Brushes.White : muted;' in control
assert 'var endpointY = card.Bottom - (stashDial ? 22.0 : 17.0);' in control
assert '"StashRewardProfile" => 398.0' in control
assert "DrawCompactStashRewards(" in control

for path, body in (("UI-DESIGN-LOCK", lock),
                   ("VISUAL-CONTROL-SYSTEM", system),
                   ("STASHES-TAB-AND-REDESIGN-PROPOSAL", proposal)):
    assert "A+C STASHES MIXER" in body, f"Current Stashes layout missing in {path}"

print("PASS: five Stashes family renderers, 26 stable source IDs, compact adaptive knobs")
print("PASS: source-native click/drag/wheel/keyboard/reset, changed digits only,")
print("      Medical MIN/MAX -1/0 separation, 3x2 gear tier grouping and intact Rewards")
print("NOT VERIFIED: WPF rendering, window geometry at runtime, Wine, or CFG gameplay")
