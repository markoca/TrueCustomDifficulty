"""STASH LOOT MIX: one Weight / Chance rule, used by writer, odds and preview.

Verified reading of the CFG (see docs/STASH-WEIGHT-VS-CHANCE.md):
  * Weight list  = ONE entry is picked per list, by Weight; it never yields
    nothing unless an `Empty` entry is present.
  * Chance list  = every entry rolls on its own (0..1).
  * Weight and Chance never mix inside one list (regular stash tiers and
    SmartLoot: 0 mixed lists; whole 2.1 dump: 3 NPC grenade lists only).
Controls that reduce items in a Weight list move the removed Weight to an
appended native `Empty` entry, so the list total stays the source total and
the OTHER items keep their odds (a control means "keep X% of game odds").
"""
import csv
import json
from collections import defaultdict
from pathlib import Path

root = Path(__file__).resolve().parents[1]
src = root / "src" / "TrueCustomDifficulty"
read = lambda rel: (src / rel).read_text(encoding="utf-8")
state = read("Services/StashEffectiveState.cs")
writer = read("Services/CfgPatchWriterService.cs")
tuning = read("Services/CfgPatchWriterService.StashTuning.cs")
vm = read("ViewModels/MainViewModel.cs")
sidecar = read("ViewModels/StashPresenceViewModel.cs")
preset_service = read("Services/PresetService.cs")
preset_file = read("Models/PresetFile.cs")
grid = read("Controls/AggregateVisualControl.StashControls.cs")
odds = read("Controls/AggregateVisualControl.StashOdds.cs")
aggregate = read("Controls/AggregateVisualControl.cs")
xaml = read("MainWindow.xaml")
baseline = json.loads(read("Data/cfg-writer-baseline.json"))["special"]
docs = (root / "docs" / "STASH-WEIGHT-VS-CHANCE.md").read_text(encoding="utf-8")

# ---- 1. Source shape: no list mixes Weight and Chance ----------------------
tiers = (("cheapStashProduction", "Cheap"), ("mediumStashAmmoProduction", "Medium"),
         ("expensiveStashProduction", "Expensive"))
for key, _ in tiers:
    for category in baseline[key]["templateCategories"]:
        fields = {("Weight" in i["fields"], "Chance" in i["fields"]) for i in category["items"]}
        assert fields in ({(True, False)}, {(False, True)}), (key, category["node"], fields)
smart = list(csv.DictReader(open(src / "Data/smartloot-world-source-entries.csv", encoding="utf-8")))
groups = defaultdict(set)
for row in smart:
    groups[(row["Prototype"], row["RankIndex"], row["Group"], row["GroupIndex"])].add(
        "W" if row["Weight"] else "C")
assert len(groups) == 422 and all(len(kinds) == 1 for kinds in groups.values())

# ---- 2. One shared rule in StashEffectiveState -----------------------------
assert "WeightListEdit(CfgWriterBaseline baseline, string tier, CfgWriterCheapStashCategory category)" in state
assert "var empty = source - values.Sum();" in state
assert "PresenceEdit" not in state
assert '("Cheap", "loot.stashMixAmmo") => StashPresenceViewModel.CheapAmmoId,' in state
assert '("Medium", "loot.stashMixAmmo") => StashPresenceViewModel.MediumAmmoId,' in state
assert '("Expensive", "loot.stashMixAmmo") => "loot.expensiveStashAmmoChance",' in state
assert "loot.stashMixDrinks\") => StashPresenceViewModel" not in state

# Writer: all three tier templates use it and append Empty.
assert writer.count(".WithValues(s_stashPresence);") == 3
assert writer.count("var weightEdit = effective.WeightListEdit(baseline, \"") == 3
assert writer.count("edit.Weights[categoryDefinition.Items.IndexOf(itemDefinition)]") == 3
assert writer.count("edits += WriteStashPresenceEmpty(possibleItems, categoryDefinition, weightEdit?.Empty);") == 3
assert 'empty.SetAssignment("ItemPrototypeSID", "Empty");' in tuning
assert '$"[{category.Items.Count}]"' in tuning
assert "s_stashPresence = vm.StashPresence.Values;" in writer

# Odds and Basic mix preview read the SAME rule.
assert "state.WeightListEdit(baseline, tier, category) is { } edit" in odds
assert ".WeightListEdit(StashPreviewBaseline.Value, \"Cheap\", group);" in aggregate
assert 'DrawText(dc, "NOTHING"' in aggregate

# ---- 3. Sidecars, presets, UI ----------------------------------------------
assert "new[] { CheapAmmoId, MediumAmmoId };" in sidecar
assert "LegacyCheapDrinksId" in sidecar and "LegacyCheapDrinksId" in preset_service
assert "public StashPresenceViewModel StashPresence { get; } = new();" in vm
assert vm.count("StashPresence.Reset();") == 2
assert "StashPresence.FromPreset(preset.StashPresence);" in vm
assert '[JsonPropertyName("stashPresence")]' in preset_file
assert 'StashPresence="{Binding DataContext.StashPresence' in xaml
assert '{ "loot.stashMixDrinks", "loot.mediumStashDrinksChance", "loot.expensiveStashDrinksChance" },' in grid
assert '"MILK IN MIX"' not in aggregate

# ---- 4. Rule behaviour on the real source lists (Python twin) -------------
def edit(items, factors, presence=1.0):
    source = sum(items)
    kept = [w * f * presence for w, f in zip(items, factors)]
    return kept, max(0.0, source - sum(kept))

basic_hard = [c for c in baseline["cheapStashProduction"]["templateCategories"] if c["node"] == "[1]"][0]
sids = [i["fields"]["ItemPrototypeSID"] for i in basic_hard["items"]]
weights = [float(i["fields"]["Weight"].rstrip("f")) for i in basic_hard["items"]]
assert sids == ["Milk", "Bread", "Sausage", "CannedFood", "Bandage"]
# MEDS (bandage) to 50%: bandage odds halve, food/milk odds unchanged.
kept, empty = edit(weights, [1, 1, 1, 1, 0.5])
total = sum(kept) + empty
vanilla = [w / sum(weights) for w in weights]
now = [k / total for k in kept]
assert abs(now[4] - vanilla[4] * 0.5) < 1e-12
assert all(abs(now[i] - vanilla[i]) < 1e-12 for i in range(4))
# Ammo-only list at 25% presence: 75% nothing.
ammo = [float(i["fields"]["Weight"]) for i in
        [c for c in baseline["cheapStashProduction"]["templateCategories"] if c["node"] == "[3]"][0]["items"]]
kept, empty = edit(ammo, [1, 1, 1], presence=0.25)
assert abs(empty / (sum(kept) + empty) - 0.75) < 1e-12

for token in ("ONE entry", "Empty", "Desolation", "13564", "never mix"):
    assert token in docs, token

print("PASS: one Weight/Chance rule — Weight lists keep their source total via Empty; writer, odds, preview, presets share it")
