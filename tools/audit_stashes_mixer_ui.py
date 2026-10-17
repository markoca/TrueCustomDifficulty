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

assert content.count('("loot.stashMix') == 6
assert medical.count('("loot.stashMedical') == 4
assert quantity.count('("MEDKITS"') + quantity.count('("ARMY MEDKITS"') == 2
assert gear.count('("BASIC"') + gear.count('("STANDARD"') + gear.count('("RARE"') == 3
assert 'DrawSourceKnob(dc, source, entries[i].Label' in content
assert "DrawStashBranchBrowser(dc," in content
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

# Unboxed single-row six distinct color/category knobs retain native input.
assert "var cursor = left;" in content and "cursor += cell + gap;" in content
assert "DrawRoundedRectangle" not in content
assert "dialAccent: StashMixPalette[i], dialScale: dialScales[i]" in content
assert "var dialInk = dialAccent ?? accent;" in source
assert source.count("new SolidColorBrush(Color.FromRgb(") >= 6
assert "StashMixPalette[kind]" in preview
assert 'StashPreviewGroup("BASIC", 7)' in preview
assert 'StashPreviewGroup("BASIC", 1)' in preview
assert 'StashPreviewGroup("RARE", 11)' not in preview  # chance only, never a weight pool
assert "StashChanceSample(" in preview
assert '"SOURCE CHANCE FIELDS"' in preview
assert '"NATIVE CHANCE · BONUS + BIAS"' in preview
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
food_triple = native_group_weights(exp, [1, 1, 3, 1, 1, 1])
assert food_triple[2] == 39 and math.isclose(sum(food_triple), 43.77)

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
    assert math.isclose(min(1, max(0, chance * 3)), min(1, chance * 3))

assert "=> 675.0;" in source
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
print("PASS: 1x/0x/3x numeric regressions and nine original authored Chance fields")
print("PASS: separate chance field examples, zero guards, responsive geometry/UI catalogs")
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


# Phase 4 contract: runtime branch navigation derives from original embedded
# writer inputs; it does not add persistable sliders or independent edits.
# All six category knobs are capped at original 1x and advance by 0.10x.
mix_ids = expected["StashContentMix"]
for pid in mix_ids:
    item = next(p for p in catalog["parameters"] if p["id"] == pid)
    slider = item["slider"]
    assert (slider["min"], slider["max"], slider["step"]) == (0, 1, 0.1), pid
    assert item["vanilla"]["All"] == 1, pid
    assert "Weight / Chance" in item["targets"][0]["path"], pid
assert "source Weight or clamps source Chance" not in "\n".join(
    p["description"] for p in catalog["parameters"] if p["id"] in mix_ids
)

browser = (root / "src/TrueCustomDifficulty/Controls/AggregateVisualControl.StashBranch.cs").read_text()
branches = (root / "src/TrueCustomDifficulty/Services/StashBranchPreviewCatalog.cs").read_text()
csproj = (root / "src/TrueCustomDifficulty/TrueCustomDifficulty.csproj").read_text()
assert "public sealed partial class AggregateVisualControl" in source
assert "public sealed partial class AggregateVisualControl" in browser
assert "StashBranchPreviewCatalog.Load(StashPreviewBaseline.Value)" in browser
assert 'DrawStashBranchBrowser(dc, new Rect(left, 237.0, width, 420.0)' in content
assert '_stashBrowserExpanded' not in content
assert 'SOURCE DETAILS' not in content and 'HIDE DETAILS' not in content
assert 'ToggleStashBrowser' not in source + browser
assert 'private bool _stashBrowserExpanded;' not in browser
assert '&& _stashBrowserExpanded' not in source
assert 'StashContentMixHeight(width)' in source
assert 'TryStashBrowserClick(point)' in source
# Read-only live source inputs must cover the complete TierField pipeline:
# selected source count floors/caps and all Stash Rewards bonus sources.
mix_links = source.split('"StashContentMix" => new[]', 1)[1].split('"StashMedicalComposition"', 1)[0]
for linked in (
    "loot.stashMedkitMinCount", "loot.stashMedkitMaxCount",
    "loot.stashArmyMedkitMinCount", "loot.stashArmyMedkitMaxCount",
    "loot.cheapStashAmmoQuantity", "loot.mediumStashAmmoQuantity",
    "loot.expensiveStashAmmoQuantity", "loot.cheapStashGrenadeChance",
    "loot.cheapStashGrenadeQuantity", "loot.expensiveStashMoneyChance",
):
    assert f'"{linked}"' in mix_links, linked
assert 'UpdateStashBrowserHover(point)' in source
assert 'private StashEffectiveState CurrentStashEffectiveState(string difficultySid = "Easy")' in source
# The top Base Preset is the SINGLE source of native difficulty for the
# non-editable generator inspector. UI and CFG use different Stalker labels.
window_xaml = (root / "src/TrueCustomDifficulty/MainWindow.xaml").read_text()
model = (root / "src/TrueCustomDifficulty/ViewModels/MainViewModel.cs").read_text()
assert 'Text="{Binding SelectedReferenceDifficulty}"' in window_xaml
assert 'Value="{Binding BasePresetIndex, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"' in window_xaml
assert ('SelectedReferenceDifficulty="{Binding DataContext.SelectedReferenceDifficulty, '
        'RelativeSource={RelativeSource AncestorType=Window}}"') in window_xaml
assert 'public static readonly DependencyProperty SelectedReferenceDifficultyProperty' in source
assert 'FrameworkPropertyMetadataOptions.AffectsRender, OnReferenceDifficultyChanged' in source
assert 'control._stashBrowserGroup = 0;' in source
assert 'public string SelectedReferenceDifficulty' in source
assert 'public string SelectedReferenceDifficulty' in model
assert 'OnPropertyChanged(nameof(SelectedReferenceDifficulty))' in model
for ui, native in (
    ("Novice", "Easy"), ("Stalker", "Medium"),
    ("Veteran", "Hard"), ("Master", "Stalker"),
):
    assert f'"{ui}" => "{native}"' in browser
assert 'x.AppliesTo(_stashBrowserRank,' in browser
assert 'CurrentStashNativeDifficulty))' in browser
assert 'CurrentStashEffectiveState(CurrentStashNativeDifficulty)' in browser
assert '_stashBrowserDifficulty' not in browser
assert 'StashDifficulties' not in browser
assert 'NavRail(x, half, "DIFFICULTY"' not in browser
assert 'NavRail(x + (width - half) / 2.0, half, "RANK"' in browser
assert 'NavigateStash(3, direction);' in source
assert '|| _stashBrowserMode == 3)' in source
assert 'NavigateStash(_stashBrowserMode == 3 ? 3 : 1, direction)' not in source
assert "NavigateStash(" in source and "NavigateStash(" in browser
assert "branch.EffectiveSelection(StashPreviewBaseline.Value, state, item)" in browser
assert "branch.EffectiveSpawn(state)" in browser
assert "CurrentStashEffectiveState(" in browser
assert "state.TierField(" in branches and "state.SmartSelection(" in branches
assert "StashEffectiveState.UniformSmartFactor(" in branches
assert "StashEffectiveState.SmartSpawnEndpoint(" in branches
assert "state.SmartCount(" in branches
assert "private static int StashCategoryIndex(string sid)" in browser
assert "StashEffectiveState.ItemBiasId(sid)" in browser
assert "StashEffectiveState.SmartItemBiasId(sid)" in browser
assert 'if (_dragTarget is null && next >= 0)' in browser
assert 'if (changed) InvalidateVisual();' in browser
assert "GetManifestResourceStream(SmartSource)" in branches
# CloneWithOpacity is a WPF brush utility, not a generator clone or a value write.
assert "CurrentNumber =" not in browser
assert "CloneWithOpacity(" in browser
assert 'public static string? SmartItemBiasId(string sid) => ItemBiasId(sid);' in effective
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
    assert "STASHES PHASE 4 NATIVE BRANCH BROWSER" in doc
assert "STASH-PER-BRANCH-BROWSER-2026-10-16.md" in (
    root / "docs/generators/STASH-GENERATOR-BRANCH-ARCHITECTURE-2026-10-16.md"
).read_text()
print("PASS: phase 4 native branch browser, 463 source groups / 1,124 authored rows")


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
assert "public string SmartCount(" in effective
assert "effective.SmartCount(item.ItemSid, fieldName," in smart
assert "state.SmartCount(item.ItemSid, field, raw," in branches
assert 'new Point(x + width - 3.0, rowY + 12.0)' in browser
for doc in (lock, system, proposal):
    assert "STASHES PHASE 5 CFG PARITY GATE" in doc
assert "STASH-PHASE-5-CFG-PARITY-AND-ACCEPTANCE-2026-10-16.md" in (
    root / "docs/generators/STASH-GENERATOR-BRANCH-ARCHITECTURE-2026-10-16.md"
).read_text()
print("PASS: Phase 5 browser/writer SmartLoot count parity and real-emitted-CFG harness wired")
print("PENDING: cross-compiled .NET 10, Wine suite, wide/narrow visual and STALKER 2 gameplay")
