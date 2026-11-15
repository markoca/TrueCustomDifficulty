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
assert len(catalog["parameters"]) == 359, "Never add mod-only controls as vanilla source IDs"
native_ids = {p["id"] for p in catalog["parameters"]}
assert not any("ModItemsRemoval" in x or "ModItemsBlocked" in x for x in native_ids)
writer = read("src/TrueCustomDifficulty/Services/CfgPatchWriterService.ModItems.cs")
main = read("src/TrueCustomDifficulty/ViewModels/MainViewModel.cs")
preset = read("src/TrueCustomDifficulty/Models/PresetFile.cs")
preset_svc = read("src/TrueCustomDifficulty/Services/PresetService.cs")
service = read("src/TrueCustomDifficulty/Services/CfgPatchWriterService.cs")
build = read("src/TrueCustomDifficulty/Services/ModBuildService.cs")
xaml = read("src/TrueCustomDifficulty/MainWindow.xaml")
cs = read("src/TrueCustomDifficulty/MainWindow.xaml.cs")
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
    required(cs, "vm." + prop + " = 0.0;", "right-click reset")
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
required(cs, "ModLootSlider_PreviewMouseRightButtonDown", "right-click reset")
required(cs, "e.Key != Key.R", "keyboard reset")
for name in ("WorldModLootControl", "CorpseModLootControl"):
    required(xaml, 'TargetName="' + name + '" Property="Visibility" Value="Visible"',
             "existing panel context")
assert xaml.count('Text="MOD ITEMS"') == 2, "exactly two compact labels"
assert xaml.count('TickFrequency="0.01" IsSnapToTickEnabled="True"') >= 2, "1% steps"
for mod in ("ZoneWatch", "Backpacks Pack", "Repair System", "AR Goggles Pack",
            "Project Itemization Reborn"):
    required(vm, mod, "rationale mod roster")
for anchor in ('Id == "loot.worldAmmoCratesRemoved"', 'Id == "loot.npcCorpseBonus"'):
    required(vm, anchor, "rationale anchor")
for source in ("docs/UI-DESIGN-LOCK.md", "docs/VISUAL-CONTROL-SYSTEM.md", "docs/MOD-AUDIT.md"):
    required(read(source), "Percentage-based MOD ITEMS removal", "new current docs")
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
print("     exact PAKs, isolated pools, 48/55 NPC roots, UI, old preset migration, native 359-lock.")
