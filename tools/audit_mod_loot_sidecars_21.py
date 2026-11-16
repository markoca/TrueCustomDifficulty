#!/usr/bin/env python3
"""Source-locked mod generator Chance reduction and preset/UI regression."""
from pathlib import Path
import json
import re

ROOT = Path(__file__).resolve().parents[1]
def read(p): return (ROOT / p).read_text(encoding="utf-8")
def required(source, needle, msg):
    assert needle in source, msg + ": " + needle

catalog = json.loads(read("src/TrueCustomDifficulty/Data/parameters.json"))
assert len(catalog["parameters"]) == 361, "Mod-only sidecars must not become native source IDs"
native_ids = {p["id"] for p in catalog["parameters"]}
assert not any("ModItemsRemoval" in x or "ModItemsBlocked" in x for x in native_ids)
writer = read("src/TrueCustomDifficulty/Services/CfgPatchWriterService.ModItems.cs")
main = read("src/TrueCustomDifficulty/ViewModels/MainViewModel.cs")
preset = read("src/TrueCustomDifficulty/Models/PresetFile.cs")
preset_svc = read("src/TrueCustomDifficulty/Services/PresetService.cs")
service = read("src/TrueCustomDifficulty/Services/CfgPatchWriterService.cs")
build = read("src/TrueCustomDifficulty/Services/ModBuildService.cs")
xaml = read("src/TrueCustomDifficulty/MainWindow.xaml")
world_ui = read("src/TrueCustomDifficulty/Controls/WorldLootRemovalRadar.cs")
npc_ui = read("src/TrueCustomDifficulty/Controls/AggregateVisualControl.cs")
vm = read("src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs")
for prop, field in [
    ("WorldModItemsRemoval", "worldModItemsRemoval"),
    ("CorpseModItemsRemoval", "corpseModItemsRemoval"),
]:
    required(main, "public double " + prop, "numeric sidecar")
    required(main, prop + " = 0.0;", "both reset routes")
    required(main, prop + " = preset." + prop, "preset loads numeric first")
    required(main, prop + " = " + prop + ",", "preset saves percentage")
    required(main, "double.IsFinite(value)", "finite editor value guard")
    required(preset, 'JsonPropertyName("' + field + '")', "new preset fraction")
    required(preset_svc, "must be between 0% and 100%", "saved range validation")
    required(xaml, "DataContext." + prop + ", Mode=TwoWay", "live slider binding")
    required(writer, "vm." + prop, "writer sidecar")
    required(world_ui if prop.startswith("World") else npc_ui,
             "ModItemsRemoval = 0.0;", "native right-click/R reset")
for legacy in ("worldModItemsBlocked", "corpseModItemsBlocked"):
    required(preset, 'JsonPropertyName("' + legacy + '")', "legacy flag load")
    required(main, "== true ? 1.0 : 0.0", "legacy true => full removal")
required(service, "WriteAuditedModItemRemovals(vm, modsFolder, documents)", "writer hook")
required(build, "_cfgWriter.Write(vm, catalog, writerInstall, stagingRoot, modsFolder)", "build path")
required(writer, '"Chance", effectiveChance.ToString("0.######", CultureInfo.InvariantCulture)',
         "native Chance change")
required(writer, 'sourceChance * (1.0 - removal)', "exact fractional multiplier")
required(writer, 'if (removal >= 1.0)', "full removal uses branch delete")
required(writer, 'AddRemovalNode(root, new[] { "ItemGenerator" }, branch)',
         "100% isolated branch removal")
required(writer, 'new[] { "ItemGenerator", branch, "PossibleItems", "[0]" }',
         "intermediate isolated leaf Chance edit")
required(writer, 'if (worldRemoval <= 0.0 && corpseRemoval <= 0.0)', "zero emit nothing")
required(writer, "SHA256.HashData(stream)", "exact PAK fingerprint")
required(writer, "ProjectItemizationReborn", "unsafe PIR exclusion")
required(writer, "no matching audited mod PAK set was found", "fail closed without mod")
assert not re.search(r'AddRemovalNode\([^;\n]*Pool', writer), "never globally remove shared rank pools"
# Native integrated editors replaced the retired XAML overlay and its
# MainWindow handlers. Assert actual source controls, not obsolete names.
required(world_ui, "protected override void OnMouseRightButtonDown",
         "world native right-click reset")
required(npc_ui, "protected override void OnMouseRightButtonDown",
         "corpse native right-click reset")
required(world_ui, "protected override void OnKeyDown", "world native keyboard")
required(npc_ui, "protected override void OnKeyDown", "corpse native keyboard")
required(world_ui, "if (e.Key == Key.R && IsEnabled && ModItemsEditorAvailable)",
         "world R reset")
required(npc_ui, "if (e.Key == Key.R)", "corpse R reset")
required(world_ui, "step * 0.01", "world 1% keyboard increment")
required(npc_ui, "modStep * 0.01", "corpse 1% keyboard increment")
required(world_ui, "Math.Round(modRatio, 2", "world 1% pointer snapping")
required(npc_ui, "Math.Round(ratio, 2", "corpse 1% pointer snapping")
required(world_ui, "private const int ModLane = 5;", "sixth world lane")
required(world_ui, 'DrawCenteredText(dc, "MOD ITEMS"', "world native caption")
required(npc_ui, 'DrawNpcCompactDial(dc, card, "FEWER MOD ITEMS"', "corpse native dial caption")
required(npc_ui, 'DrawNpcModItemsCard(dc, layout.Mod,', "corpse native mod knob card")
required(npc_ui, 'DrawHorizontalTrackCard(dc, ammo, "BONUS AMMO", ammoCard,', "bonus ammo native track card")
required(npc_ui, 'DrawNpcCompactMultiplierKnob(dc, medical,', "independent medical compact knob")
required(npc_ui, 'DrawNpcCompactMultiplierKnob(dc, scarcity,', "source-backed percent knob")
required(npc_ui, 'DrawNpcCompactDial(dc, card, "FEWER MOD ITEMS"', "optional mod percent knob")
required(npc_ui, '_npcModKnobHit.Contains(point)', "mod knob click/drag hit geometry")
required(npc_ui, '_npcModKnobHit.Contains(pointer)', "mod knob mouse wheel editing")
required(npc_ui, 'CurrentNpcLootLayout(ActualWidth)', "shared contextual layout")
required(npc_ui, 'NpcLootBoardHeight(width)', "measure follows compact NPC strip")
required(world_ui, "BooleanCheckboxChrome.Draw", "world checkbox")
required(npc_ui, "BooleanCheckboxChrome.Draw", "corpse checkbox")
assert xaml.count('Text="MOD ITEMS"') == 0, "obsolete floating XAML strips returned"
for prefix in ("World", "Corpse"):
    for suffix in ("ModItemsEnabled", "ModItemsRemoval"):
        required(xaml, "DataContext." + prefix + suffix + ", Mode=TwoWay",
                 "native two-way enabled/value binding")
required(xaml, "DataContext.IsBuildIdle", "build-phase editor lock binding")
required(world_ui, "ModItemsEditorAvailable", "world disabled during build")
required(npc_ui, "ModItemsEditorAvailable", "corpse disabled during build")
required(world_ui, 'new("loot.worldAmmoCratesRemoved"', "world source anchor")
required(npc_ui, 'P("loot.npcCorpseBonus")', "corpse source anchor")
mod_audit = read("docs/MOD-AUDIT.md")
for mod in ("ZoneWatch", "Backpacks Pack", "Repair System", "AR Goggles Pack",
            "Project Itemization Reborn"):
    required(mod_audit, mod, "audited mod rationale roster")
required(mod_audit, "Percentage-based MOD ITEMS removal", "original source audit")
for source in ("docs/UI-DESIGN-LOCK.md", "docs/VISUAL-CONTROL-SYSTEM.md"):
    required(read(source), "Mod-item reduction controls (2026-11-15)",
             "current native integrated UI contract")
for name, expected in (("BackpackCorpseRoots", 48), ("RepairCorpseRoots", 55)):
    match = re.search(r"private static readonly string\[\] " + name +
                      r" =\s*\{(.*?)\n    \};", writer, re.S)
    assert match, "Missing " + name
    roots = re.findall(r'"(GeneralNPC_[A-Za-z0-9_]+_ItemGenerator)"', match[1])
    assert len(roots) == len(set(roots)) == expected, (name, len(roots))
names = re.findall(r'new\(\s*"([^"]+)",\s*new\[\]', writer)
assert names == ["ZoneWatch", "PointOfNoReturn-BackpacksPack",
                 "PointOfNoReturn-RepairSystem", "PointOfNoReturn-ArGogglesPack"], names
for source in ("1.0, 0.0", "0.10, 0.10", "0.05, 0.04", "0.05, 0.0"):
    required(writer, source, "source-verified Chance by mod/domain")
required(writer, "DugaRadar_Officer", "corrected ZoneWatch world root")
assert "Promzona_AhropromResearchInstitute_StashGenerator3_1B259E604F001F533511BD889B771B3C\", \"ZST_SignalmanHold207" not in writer
# Exact arithmetic contract independent of intermediate display rounding.
for original in (1.0, 0.10, 0.05, 0.04):
    for reduction, ratio in ((0, 1.0), (0.25, .75), (0.50, .5), (0.75, .25), (1.0, 0)):
        chance = round(original * (1.0 - reduction), 6)
        assert abs(chance - original * ratio) < 1e-10
print("[OK] 0-100% world and corpse mod Chance reduction, 0% neutral, 100% branch removal;")
print("     exact PAKs, isolated pools, 48/55 NPC roots, UI, old preset migration, native 361-lock.")
