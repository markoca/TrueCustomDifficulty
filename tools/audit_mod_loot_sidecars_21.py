#!/usr/bin/env python3
"""Static 2.1 regression: optional mod-item removal stays outside native 359 sources."""
from pathlib import Path
import json
import re

ROOT = Path(__file__).resolve().parents[1]
def read(p): return (ROOT / p).read_text(encoding="utf-8")
def assert_has(source, needle, msg):
    assert needle in source, msg + ": " + needle

catalog = json.loads(read("src/TrueCustomDifficulty/Data/parameters.json"))
assert len(catalog["parameters"]) == 359, "Do not add fake mod generator CFG IDs"
ids = {p["id"] for p in catalog["parameters"]}
assert not any("ModItemsBlocked" in x for x in ids)
writer = read("src/TrueCustomDifficulty/Services/CfgPatchWriterService.ModItems.cs")
main = read("src/TrueCustomDifficulty/ViewModels/MainViewModel.cs")
preset = read("src/TrueCustomDifficulty/Models/PresetFile.cs")
service = read("src/TrueCustomDifficulty/Services/CfgPatchWriterService.cs")
build = read("src/TrueCustomDifficulty/Services/ModBuildService.cs")
xaml = read("src/TrueCustomDifficulty/MainWindow.xaml")
cs = read("src/TrueCustomDifficulty/MainWindow.xaml.cs")
vm = read("src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs")
for prop, serialized in [
    ("WorldModItemsBlocked", "worldModItemsBlocked"),
    ("CorpseModItemsBlocked", "corpseModItemsBlocked"),
]:
    assert_has(main, "public bool " + prop, "live sidecar")
    assert_has(main, prop + " = false;", "reset all/default")
    assert_has(main, prop + " = preset." + prop + ";", "preset load")
    assert_has(main, prop + " = " + prop + ",", "preset save")
    assert_has(preset, 'JsonPropertyName("' + serialized + '")', "preset contract")
    assert_has(xaml, "DataContext." + prop, "checkbox binding")
    assert_has(writer, "vm." + prop, "writer uses sidecar state")
    assert_has(cs, "vm." + prop, "code behind edits sidecar")
assert_has(service, "WriteAuditedModItemRemovals(vm, modsFolder, documents)", "writer hook")
assert_has(build, "_cfgWriter.Write(vm, catalog, writerInstall, stagingRoot, modsFolder)", "build path")
assert_has(writer, 'new[] { "ItemGenerator" }, branch', "native branch-only deletion")
assert_has(writer, 'AddRemovalNode(root, new[] { "ItemGenerator" }, branch)', "do not delete vanilla root")
assert_has(writer, "SHA256.HashData(stream)", "mod source fingerprint")
assert_has(writer, "ProjectItemizationReborn", "unsafe mod protection")
assert_has(writer, "no matching audited mod PAK set was found", "fail closed")
assert not re.search(r"AddRemovalNode\([^;\n]*Pool", writer), "never globally remove shared rank pools"
assert_has(cs, "ModLootToggle_PreviewMouseRightButtonDown", "right-click")
assert_has(cs, "Key.R", "focused reset")
for tag in ['Tag="world"', 'Tag="corpse"']:
    assert_has(xaml, tag, "compact toggle")
for name in ["WorldModLootControl", "CorpseModLootControl"]:
    assert_has(xaml, 'TargetName="' + name + '" Property="Visibility" Value="Visible"', "UI trigger")
for rationale_mod in ["ZoneWatch", "Backpacks Pack", "Repair System", "AR Goggles Pack",
                      "Project Itemization Reborn"]:
    assert_has(vm, rationale_mod, "right-hand rationale")
assert_has(vm, 'Id == "loot.worldAmmoCratesRemoved"', "world rationale anchor")
assert_has(vm, 'Id == "loot.npcCorpseBonus"', "corpse rationale anchor")
assert_has(writer, '"GameData/ItemGeneratorPrototypes.cfg"', "native source path")
assert_has(writer, '"Backpacks"', "exact branch")
assert_has(writer, '"RepairKits"', "exact branch")
assert_has(writer, '"ArGoggles"', "exact branch")
for source in ["docs/UI-DESIGN-LOCK.md", "docs/VISUAL-CONTROL-SYSTEM.md",
               "docs/MOD-AUDIT.md"]:
    assert_has(read(source), "Modded item-generator removal in Loot panels", "matching docs")
# Source-audited NPC root totals; count only actual list entries, not comments.
for name, expected in [("BackpackCorpseRoots", 48), ("RepairCorpseRoots", 55)]:
    match = re.search(r"private static readonly string\[\] " + name +
                      r" =\s*\{(.*?)\n    \};", writer, re.S)
    assert match, "Missing NPC root list " + name
    roots = re.findall(r'"(GeneralNPC_[A-Za-z0-9_]+_ItemGenerator)"', match[1])
    assert len(roots) == len(set(roots)) == expected, (name, len(roots))
# Require exactly the 4 confirmed mod families. No broader claims.
names = re.findall(r'new\(\s*"([^"]+)",\s*new\[\]', writer)
assert names == ["ZoneWatch", "PointOfNoReturn-BackpacksPack",
                 "PointOfNoReturn-RepairSystem", "PointOfNoReturn-ArGogglesPack"], names
print("[OK] Two sidecar mod loot switches, rationale lists, 359-native lock, 48+55 NPC roots,")
print("     exact-version PAK guards, safe native named-branch removal, UI/preset contract")
