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
catalog = json.loads((data / "parameters.json").read_text())
baseline = json.loads((data / "cfg-writer-baseline.json").read_text())["special"]
lock = (root / "docs/UI-DESIGN-LOCK.md").read_text()
system = (root / "docs/VISUAL-CONTROL-SYSTEM.md").read_text()
proposal = (root / "docs/STASHES-TAB-AND-REDESIGN-PROPOSAL.md").read_text()

expected = {
    "StashRewardProfile": [
        "loot.cheapStashAmmoQuantity", "loot.mediumStashAmmoQuantity",
        "loot.expensiveStashAmmoQuantity"],
    "StashContentMix": [
        "loot.stashMixAmmo", "loot.stashMixMedical", "loot.stashMixFood",
        "loot.stashMixDrinks", "loot.stashMixGrenades", "loot.stashMixCoupons",
        "loot.cheapStashGrenadeChance", "loot.cheapStashGrenadeQuantity",
        "loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance",
        "loot.mediumStashCouponsChance", "loot.expensiveStashAmmoChance",
        "loot.expensiveStashMedicalChance", "loot.expensiveStashFoodChance",
        "loot.expensiveStashDrinksChance", "loot.expensiveStashGrenadesChance",
        "loot.expensiveStashMoneyChance"],
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
assert len(ids) == len(set(ids)) == 34
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
medical = method("DrawStashMedicalComposition", "DrawStashMedicalQuantity")
quantity = method("DrawStashMedicalQuantity", "DrawStashGear")
gear = method("DrawStashGear", "DrawStashGearMode")
preview = method("DrawStashPoolBar", "DrawCompactStashRewards")

assert 'DrawStashWeightMixer(dc, new Rect(left, 53.0, width,' in content
assert medical.count('("loot.stashMedical') == 4
assert quantity.count('("MEDKITS"') + quantity.count('("ARMY MEDKITS"') == 2
assert gear.count('("BASIC"') + gear.count('("STANDARD"') + gear.count('("RARE"') == 3
assert 'DrawSourceKnob(' not in content
assert "DrawStashWeightMixer(dc," in content
assert "DrawStashBasicNativePools(dc," in content
assert "DrawStashRewardGraphRow(dc, P(entries[i].Id)" in medical
assert "DrawStashMedicalSourcePreview(dc," in medical
assert "DrawStashQuantityDots(dc, P(families[i].Min)" in quantity
assert "DrawStashQuantityDots(dc, P(families[i].Max)" in quantity
assert "DrawStashGearMode(dc, P(tiers[i].Weapon)" in gear
assert "DrawStashGearMode(dc, P(tiers[i].Armor)" in gear
assert "DrawStashBiasPreview" not in source
assert "DrawStashCountKnob" not in source
assert "DrawStashGearDial" not in source
assert "AddHorizontalTarget(" not in preview  # never an editable preview
assert "DrawStashNumericReadout" not in preview
assert "MULTIPLIERS, NOT DROP ODDS" not in source
assert "weights.Sum()" not in preview # normalized six source multipliers retired
assert 'DrawStashNativeRollControl(dc, "loot.cheapStashGrenadeChance",' in content
assert 'DrawStashChanceTier(dc, "STANDARD", "Medium", 10,' in content
assert "wide ? 394.0 : 548.0, width," in content
assert "wide ? 86.0 : 146.0" in content
assert "wide ? 488.0 : 702.0, width," in content
assert "wide ? 86.0 : 206.0" in content
assert "wide ? 207.0 : 361.0, width, 115.0" in content
assert "var pairTop = wide ? 330.0 : 484.0;" in content
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
assert 'StashPreviewGroup("BASIC", 7)' in preview
assert 'StashPreviewGroup("BASIC", 1)' in preview
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
assert "StashMedicalSourcePreview" in source
assert 'new[] { "0 ITEMS", "AUTO", "1", "2", "3", "4", "5", "6" }' in quantity
assert "var minimum = isMax ? -1.0 : 0.0;" in quantity

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

# Only categories that actually change the two rendered native
# multi-category Weight pools have knobs.
pool_kinds = sorted({category[row["fields"]["ItemPrototypeSID"]]
    for group in (hard, exp) for row in group["items"]})
assert pool_kinds == [1, 2, 3, 5], pool_kinds
for pid in ("loot.stashMixAmmo", "loot.stashMixGrenades"):
    assert next(p for p in catalog["parameters"] if p["id"] == pid)["exposeInUi"] is False
for pid in ("loot.stashMixMedical", "loot.stashMixFood",
            "loot.stashMixDrinks", "loot.stashMixCoupons"):
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

# Three source-native tier pools sit below the editable mixer.
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
assert tuple(map(float, height_match.groups())) == (700.0, 637.0, 1019.0)
assert tuple(map(float, mixer_match.groups())) == (53.0, 146.0, 300.0)
assert "wide ? 394.0 : 548.0, width," in content
assert "wide ? 86.0 : 146.0" in content
assert "wide ? 488.0 : 702.0, width," in content
assert "wide ? 86.0 : 206.0" in content
assert "wide ? 207.0 : 361.0, width, 115.0" in content
assert "var pairTop = wide ? 330.0 : 484.0;" in content
assert '"WEIGHT APPLIES TO ALL TIERS"' not in content
assert '"RELATIVE SELECTION"' not in source
assert '"BASIC POOL COMPOSITION"' not in coverage
assert "var wide = width >= 670.0;" in content
assert 637.0 - (523.0 + 100.0) == 14.0
assert 1019.0 - (754.0 + 251.0) == 14.0
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
assert "loot.cheapStashGrenadeChance" in source.split('"StashContentMix" => new[]', 1)[1].split('"StashMedicalComposition"', 1)[0]
assert "loot.expensiveStashMoneyChance" in source.split('"StashContentMix" => new[]', 1)[1].split('"StashMedicalComposition"', 1)[0]
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
assert 'DrawStashWeightMixer(dc, new Rect(left, 53.0, width,' in content
assert "StashContentMixHeight(width)" in source
assert '"loot.mediumStashMedicalChance"' in source
assert '"loot.mediumStashCouponsChance"' in source
assert '"loot.expensiveStashAmmoChance"' in source
assert '"loot.expensiveStashGrenadesChance"' in source
assert "TierChanceParameterId(tierName," in coverage
assert "StashPreviewBaseline.Value, effective, r.Item" in coverage
assert 'CreateText("V "' not in tier_preview and "vanillaX" not in tier_preview
assert "Math.Clamp(Number(updated) * multiplier, 0.0, 1.0)" in effective
assert 'StashTierChanceParameterIds.Any(id =>' in tuning

assert '=> width >= 700.0 ? 588.0 : 922.0;' in source
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
assert "var columns = outer.Width >= 670.0 ? 4 : 2;" in coverage
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


# These two visible Basic source pools have different difficulty and rank
# gates; neither is a distinct stash tier or a percentage drop chance.
basic_preview = baseline["cheapStashProduction"]["templateCategories"]
assert basic_preview[7]["fields"]["Diff"] == (
    "EGameDifficulty::Easy, EGameDifficulty::Medium")
assert basic_preview[7]["fields"]["PlayerRank"] == "ERank::Experienced"
assert basic_preview[1]["fields"]["Diff"] == "EGameDifficulty::Hard"
assert "PlayerRank" not in basic_preview[1]["fields"]

assert '"WEIGHT MIX · ALL TIERS"' in content
assert '"BASIC  ·  WEIGHT MIX"' not in content
assert '"BASIC · LOOT EXAMPLES"' in coverage
assert '"NOVICE / STALKER"' in coverage
assert 'rankCaption: "RANK: EXPERIENCED"' in coverage
assert '"VETERAN"' in coverage
assert '"EASY/MEDIUM · EXPERIENCED"' not in coverage
assert 'CreateText(label, 11.4, text' in coverage
assert 'CreateText(name, 10.2, text' in coverage
assert 'CreateText(currentText, 10.4,' in coverage
assert "row.Width < 440.0 ? 9.6 : 10.8" in source
assert "row.Width >= 580.0 ? 10.0 : 8.9" in source

# Regression: WPF can render a visible StashContentMix family before the
# Window-level AllParameters binding arrives. Eight new authored Chance
# parameters must be present in the embedded catalog, aggregate family and
# main ViewModel, but missing early VMs must never throw during OnRender.
tier_chance_ids = [
    "loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance",
    "loot.mediumStashCouponsChance", "loot.expensiveStashAmmoChance",
    "loot.expensiveStashMedicalChance", "loot.expensiveStashFoodChance",
    "loot.expensiveStashDrinksChance", "loot.expensiveStashGrenadesChance",
]
catalog_by_id = {p["id"]: p for p in catalog["parameters"]}
viewmodel = (root / "src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs").read_text()
main_vm = (root / "src/TrueCustomDifficulty/ViewModels/MainViewModel.cs").read_text()
for pid in tier_chance_ids:
    entry = catalog_by_id[pid]
    assert entry["exposeInUi"] is True, pid
    assert entry["group"] == "Stash Content", pid
    assert pid in expected["StashContentMix"], pid
    assert f'["{pid}"] = "StashContentMix"' in viewmodel, pid
    assert f'"{pid}"' in main_vm, pid
assert 'Parameters="{Binding DataContext.AllParameters' in (
    root / "src/TrueCustomDifficulty/MainWindow.xaml").read_text()

tier_preview = coverage.split("private void DrawStashChanceTier(", 1)[1]
assert "StashSourceGroups.Value" in tier_preview
assert "!g.IsSmart && g.Tier == tierName" in tier_preview
assert "hasWeight[kind] = true" in tier_preview
assert "hasChance[kind] = true" in tier_preview
assert "hasWeight[k] || hasChance[k]" in tier_preview
assert '"NATIVE CHANCE"' not in tier_preview
assert 'CreateText("WEIGHT", 9.1' in tier_preview
assert '"WEIGHT ONLY"' not in tier_preview
assert "const double pitch = 60.0;" in tier_preview
assert "cellWidth, 53.0" in tier_preview
assert 'CreateText("V "' not in tier_preview
assert "vanillaX" not in tier_preview
assert "var expected = tierName == \"Medium\"" in tier_preview
assert "SequenceEqual(expected)" in tier_preview
assert "100.0 * totals" not in tier_preview
assert "totals.Sum()" not in tier_preview
assert "AddHorizontalTarget(parameter," in tier_preview
assert "var parameter = P(id);" in tier_preview
assert "if (parameter is null)" in tier_preview
assert 'var unavailableText = Parameters is null ? "LOADING" : "CONTROL UNAVAILABLE";' in tier_preview
assert "CreateText(unavailableText, 9.0" in tier_preview
assert 'var label = Parameters is null' not in tier_preview
assert "continue;" in tier_preview
assert 'throw new InvalidDataException(\n                "Missing live stash Chance control "' not in tier_preview
assert "var vanillaValues = rows.Select(" not in tier_preview
assert "OnBindingChanged" in source and "Loaded += (_, _) => RebindParameters();" in source
assert "CurrentNumber =" not in tier_preview
assert "DrawStashNativeRollControl(" in coverage
assert "DrawStashPoolBar(dc, experienced," in coverage
assert "DrawStashPoolBar(dc, hard," in coverage
assert 'StashPreviewGroup("BASIC", 7)' in coverage
assert 'StashPreviewGroup("BASIC", 1)' in coverage
assert "dialAccent: StashMixPalette[kind], dialScale: .74" in coverage
assert "private static readonly int[] BasicVisibleWeightKinds = { 1, 2, 3, 5 };" in coverage
assert "BasicVisibleWeightKinds.Length" in coverage
assert "previewGroups.Where(g => g is not null)" in coverage
assert '"BASIC RGD-5 CHANCE"' in source
assert '"RARE COUPON CHANCE"' in source
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
reward_body = source.split('private void DrawCompactStashRewards(',1)[1].split(
    'private void DrawStashRewardGraphRow(',1)[0]
assert '"loot.cheapStashGrenadeChance"' not in reward_body
assert '"loot.expensiveStashMoneyChance"' not in reward_body
assert '"loot.cheapStashGrenadeQuantity"' not in reward_body
assert '"loot.cheapStashGrenadeQuantity"' in content
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
