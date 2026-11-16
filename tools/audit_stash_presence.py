"""STASH LOOT MIX presence sidecars + odds-centred controls.

Single-category native Weight picks (Basic ammo, Basic water/energy drinks,
Standard ammo, Rare ammo) always yield one item; there is no category-level
Chance in ItemGenerator. Presence < 100% appends a native `Empty` weighted
entry so Empty / (total + Empty) = 1 - presence. This audit checks the shared
math, the writer emission, the sidecar plumbing and the UI grid mapping.
"""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
src = root / "src" / "TrueCustomDifficulty"
state = (src / "Services/StashEffectiveState.cs").read_text(encoding="utf-8")
writer = (src / "Services/CfgPatchWriterService.cs").read_text(encoding="utf-8")
tuning = (src / "Services/CfgPatchWriterService.StashTuning.cs").read_text(encoding="utf-8")
vm = (src / "ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
sidecar = (src / "ViewModels/StashPresenceViewModel.cs").read_text(encoding="utf-8")
preset_service = (src / "Services/PresetService.cs").read_text(encoding="utf-8")
preset_file = (src / "Models/PresetFile.cs").read_text(encoding="utf-8")
grid = (src / "Controls/AggregateVisualControl.StashControls.cs").read_text(encoding="utf-8")
odds = (src / "Controls/AggregateVisualControl.StashOdds.cs").read_text(encoding="utf-8")
xaml = (src / "MainWindow.xaml").read_text(encoding="utf-8")
baseline = json.loads((src / "Data/cfg-writer-baseline.json").read_text(encoding="utf-8"))["special"]

# Shared math lives in StashEffectiveState and is used by writer AND preview.
assert "public static string? PresenceId(string tier, CfgWriterCheapStashCategory category)" in state
assert '("Cheap", "loot.stashMixAmmo") => StashPresenceViewModel.CheapAmmoId,' in state
assert '("Cheap", "loot.stashMixDrinks") => StashPresenceViewModel.CheapDrinksId,' in state
assert '("Medium", "loot.stashMixAmmo") => StashPresenceViewModel.MediumAmmoId,' in state
assert '("Expensive", "loot.stashMixAmmo") => "loot.expensiveStashAmmoChance",' in state
assert "var empty = total * (1.0 - presence) / presence;" in state
assert 'return ("1", true);' in state  # 0% => Empty only, real entries zeroed

# Writer: all three tier templates use the presence-aware state and emit Empty.
assert writer.count(".WithValues(s_stashPresence);") == 3
assert writer.count("effective.PresenceEdit(baseline, \"") == 3
assert writer.count("edits += WriteStashPresenceEmpty(possibleItems, categoryDefinition, emptyWeight);") == 3
assert writer.count('var value = presenceZero && field.Key == "Weight"') == 3
assert "s_stashPresence = vm.StashPresence.Values;" in writer and "s_stashPresence = null;" in writer
assert '!StashPresenceChanged("Cheap")' in writer and '!StashPresenceChanged("Medium")' in writer
assert 'empty.SetAssignment("ItemPrototypeSID", "Empty");' in tuning
assert '$"[{category.Items.Count}]"' in tuning

# Sidecar plumbing: VM, reset, presets, validation, XAML.
for token in ("CheapAmmoId", "CheapDrinksId", "MediumAmmoId", "public Dictionary<string, double>? ToPreset()"):
    assert token in sidecar, token
assert "public StashPresenceViewModel StashPresence { get; } = new();" in vm
assert vm.count("StashPresence.Reset();") == 2
assert "StashPresence = StashPresence.ToPreset()," in vm
assert "StashPresence.FromPreset(preset.StashPresence);" in vm
assert '[JsonPropertyName("stashPresence")]' in preset_file
assert "Unknown stash presence entry" in preset_service
assert 'StashPresence="{Binding DataContext.StashPresence' in xaml

# UI: grid mirrors ODDS rows/columns; odds include presence.
assert "private static readonly string?[,] StashControlGrid =" in grid
assert 'DrawText(CreateText("ADJUST ODDS", 11.4' in grid
assert "state.Presence(tier, group.TierCategory)" in odds
assert "presence * values.Where(v => v.Kind == kind)" in odds

# Every presence target really is a single-category Weight pick in the source.
def kind(sid):
    if sid[0] == "A" and sid[1].isdigit():
        return "ammo"
    if sid in ("Vodka", "Water", "Milk", "Energetic", "Beer"):
        return "drinks"
    return "other"

hits = {}
for key, tier in (("cheapStashProduction", "Cheap"), ("mediumStashAmmoProduction", "Medium"),
                  ("expensiveStashProduction", "Expensive")):
    for category in baseline[key]["templateCategories"]:
        items = category["items"]
        if not items or any("Weight" not in i["fields"] for i in items):
            continue
        kinds = {kind(i["fields"]["ItemPrototypeSID"]) for i in items}
        if len(kinds) == 1 and kinds <= {"ammo", "drinks"}:
            hits.setdefault((tier, kinds.pop()), []).append(category["node"])
assert hits[("Cheap", "ammo")] == ["[3]"], hits
assert hits[("Cheap", "drinks")] == ["[2]"], hits
assert len(hits[("Medium", "ammo")]) == 4, hits
assert len(hits[("Expensive", "ammo")]) == 7, hits
assert ("Medium", "drinks") not in hits and ("Expensive", "drinks") not in hits

# Math: appended Empty weight gives exactly 1 - presence "nothing".
for total in (1.0, 5.0, 100.0):
    for presence in (0.05, 0.25, 0.5, 0.95):
        empty = total * (1.0 - presence) / presence
        assert abs(empty / (total + empty) - (1.0 - presence)) < 1e-12

print("PASS: stash presence sidecars — Empty weighted pick for Basic ammo/drinks, Standard ammo, Rare ammo; writer, presets, odds grid")
