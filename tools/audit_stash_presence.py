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

# ODDS PER STASH reads the SAME rule.
assert "state.WeightListEdit(baseline, tier, category) is { } edit" in odds

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

# ---- 5. SmartLoot Weight groups: kept-share spawn endpoints --------------
smart_writer = read("Services/CfgPatchWriterService.StashSmartLoot.cs")
assert "public static double SmartKeptShare(" in state
assert "groupFactor = StashEffectiveState.SmartKeptShare(" in smart_writer
for weights, factors in (([5, 1], [0.5, 1.0]), ([4, 1], [1.0, 0.0]), ([1], [0.3])):
    source = sum(weights)
    kept = [w * k for w, k in zip(weights, factors)]
    share = min(1.0, sum(kept) / source)
    for i, (w, k) in enumerate(zip(weights, factors)):
        item_odds = share * (kept[i] / sum(kept) if sum(kept) else 0.0)
        assert abs(item_odds - k * w / source) < 1e-12, (weights, factors)

# ---- 6. Medical: one control per item, Chance scale <-> Weight factor -----
assert "Mirror(scale, bias);" in vm and "Mirror(bias, scale);" in vm
assert 'DrawStashMedicalCell(dc, P(StashMedicalItems[k].Id)' in aggregate

# ---- 7. No SMART column / no Basic mix preview (removed 2026-11-16) --------
assert '"SMART"' not in odds and "StashSmartOdds" not in aggregate
assert 'DrawStashBasicNativePools(dc,' not in aggregate.split("private void DrawStashContentMix(", 1)[1].split("private void DrawStashMedicalComposition(", 1)[0]

# ---- 8. Basic Chance rows follow the Basic MEDS / FOOD / DRINKS cells -----
assert '"Cheap" when suffix is "Medical" or "Food" or "Drinks"' in state
assert '=> "loot.stashMix" + suffix,' in state
basic_chance = [c for c in baseline["cheapStashProduction"]["templateCategories"]
                if all("Chance" in i["fields"] for i in c["items"])]
chance_sids = {i["fields"]["ItemPrototypeSID"] for c in basic_chance for i in c["items"]
               if float(i["fields"]["Chance"].rstrip("f")) > 0}
# Real authored Basic Chance rows: meds and vodka (and the RGD-5, its own CHANCE cell).
assert chance_sids == {"Bandage", "Medkit", "Vodka", "GrenadeRGD5"}, chance_sids
# Novice (Easy) before the fix: list [4] rolls Bandage/Medkit/Vodka at 100%, so
# a 0% MEDS cell could not lower Basic meds. With the cell owning these rows,
# 0% means no meds from any Basic list.
easy = [c for c in basic_chance if "Easy" in c["fields"].get("Diff", "")]
vanilla_none = 1.0
for c in easy:
    for i in c["items"]:
        if i["fields"]["ItemPrototypeSID"] in ("Bandage", "Medkit"):
            vanilla_none *= 1 - float(i["fields"]["Chance"].rstrip("f"))
assert vanilla_none == 0.0  # Novice: Basic meds are certain from Chance rows alone

# ---- 9. ADJUST ODDS: absolute CHANCE cells are tagged, one 5% grid --------
assert 'id is "loot.cheapStashGrenadeChance" or "loot.expensiveStashMoneyChance"' in grid
assert '"100% = GAME ODDS · CHANCE = REAL %"' in grid
assert 'CreateText("CHANCE", 8.4, muted, FontWeights.Bold)' in grid
catalog = {p["id"]: p for p in json.loads(read("Data/parameters.json"))["parameters"]}
import re as _re
grid_ids = set(_re.findall(r'"(loot\.[A-Za-z]+)"', grid.split("StashControlGrid =", 1)[1].split("};", 1)[0]))
assert len(grid_ids) == 14, grid_ids  # + 2 presence sidecars, 2 empty cells
for pid in grid_ids:
    assert catalog[pid]["slider"]["step"] == 0.05, pid

for token in ("ONE entry", "Empty", "Desolation", "13564", "never mix", "SmartKeptShare",
              "Basic Chance rows", "CHANCE"):
    assert token in docs, token

print("PASS: one Weight/Chance rule — Weight lists keep their source total via Empty; writer, odds and presets share it; Basic Chance rows follow the cells")
