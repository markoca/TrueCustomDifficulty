#!/usr/bin/env python3
"""SMART LOOT NEED + SMART odds columns contract (2026-11-17).

Two source-backed knobs scale the stash SmartLoot spawn endpoints:
EXTRA LOOT WHEN STOCKED -> MinSpawnChance, EXTRA LOOT WHEN LOW -> MaxSpawnChance.
Writer and previews share StashEffectiveState.SmartNeedEndpoints (clamp 0..1,
stocked never above short). The ODDS tables show the same SMART endpoints.
"""
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src" / "TrueCustomDifficulty"
IDS = ("loot.stashSmartStockedChance", "loot.stashSmartShortChance")
failures = []


def check(ok, label):
    print(("[PASS] " if ok else "[FAIL] ") + label)
    if not ok:
        failures.append(label)


def read(rel):
    return (APP / rel).read_text(encoding="utf-8")


catalog = {p["id"]: p for p in json.loads(read("Data/parameters.json"))["parameters"]}
baseline = json.loads(read("Data/cfg-writer-baseline.json"))
for pid in IDS:
    row = catalog.get(pid, {})
    check(row.get("tab") == "stashes" and row.get("group") == "Stash Smart Loot"
          and row.get("vanilla") == {"All": 1}
          and row.get("slider") == {"min": 0, "max": 2, "step": 0.05, "stepMode": "absolute"}
          and row.get("support", {}).get("status") == "runtime_test_required", f"catalog {pid}")
SMART_ODDS = ["loot.stashSmartMedsOdds", "loot.stashSmartAmmoOdds",
              "loot.stashSmartFoodDrinksOdds", "loot.stashSmartGrenadesOdds"]
check(baseline["special"].get("stashSmartNeed", {}).get("parameterIds")
      == ["loot.stashSmartLootEnabled", *IDS, *SMART_ODDS],
      "baseline special.stashSmartNeed")
check(not any(p["parameterId"] in IDS for p in baseline["editPoints"]), "no direct points")

effective = read("Services/StashEffectiveState.cs")
smart = read("Services/CfgPatchWriterService.StashSmartLoot.cs")
tuning = read("Services/CfgPatchWriterService.StashTuning.cs")
preview = read("Services/StashBranchPreviewCatalog.cs")
check("public static (string Min, string Max) SmartNeedEndpoints(" in effective
      and "if (hasMin && hasMax && min > max) min = max;" in effective, "shared endpoint rule (stocked <= short)")
check(".Concat(StashSmartNeedParameterIds)" in smart and "if (affected.Length == 0 && !needChanged)" in smart
      and "StashEffectiveState.SmartNeedEndpoints(" in smart, "writer: need ids trigger every group rewrite")
check("StashEffectiveState.SmartStockedId, StashEffectiveState.SmartShortId" in tuning, "need id list")
check("StashEffectiveState.SmartNeedEndpoints(min, max, factor, stocked, shortFactor)" in preview,
      "preview EffectiveSpawn uses the writer rule")

ui = read("Controls/AggregateVisualControl.cs")
board = read("Controls/AggregateVisualControl.StashSmartLoot.cs")
odds = read("Controls/AggregateVisualControl.StashOdds.cs")
check('case "StashSmartLoot":' in ui and '"StashSmartLoot" => StashSmartLootHeight' in ui
      and '"STASH SMART LOOT"' in board and '"EXTRA LOOT WHEN STOCKED"' in board
      and '"EXTRA LOOT WHEN LOW"' in board
      and '"CHANCE A SET APPEARS"' in board, "SMART LOOT NEED board")
check("new Rect(12.0, StashSmartToggleTop, 190.0, WorldListRow)" in board
      and "dc.DrawTrackedRoundedRectangle(panel, new Pen(border, 0.9), card" not in board
      and "DrawSourceKnob(dc, parameter, knobs[i].Label, card" in board,
      "master checkbox at top left; borderless source dials")
check('"SMART"' in odds and "StashSmartCategoryOdds(current)" in odds
      and 'StashSmartOdds(state, StashCategoryIndex' in odds
      and "(outer.Right - 12.0 - gridLeft - 3.0 * gap) / 4.0" in odds, "SMART column in STASH LOOT MIX odds")
check('"ArtifactHunting" or "StashSmartLoot" or "AmmoDistribution"' in ui, "mouse wheel on knob boards")
pvm = read("ViewModels/ParameterViewModel.cs")
check('["StashSmartLoot"] = "loot.stashSmartStockedChance"' in pvm
      and all(f'["{pid}"] = "StashSmartLoot"' in pvm for pid in IDS), "family map and anchor")

# ---- corpse NEED BOOST: one SMART LOOT master, no secondary switch ------------
boost = read("Controls/AggregateVisualControl.NeedBoost.cs")
vm = read("ViewModels/MainViewModel.cs")
preset = read("Models/PresetFile.cs")
writer = read("Services/CfgPatchWriterService.cs")
xaml = read("MainWindow.xaml")
check("NeedBoostEnabledProperty" not in boost and "_npcBoostCheckbox" not in boost
      and '"FLAT", "2×"' in boost and "NpcNeedBoostKnobActive()" in boost,
      "NEED BOOST dial without redundant checkbox")
check('p.Id == "loot.npcSmartLootEnabled"' in vm
      and "vm.EffectiveCorpseSmartLootNeedBoost" in writer,
      "single SMART LOOT master gates effective NEED BOOST")
check('"corpseSmartLootNeedBoostEnabled"' not in preset
      and "preset.CorpseSmartLootNeedBoostEnabled" not in vm,
      "no redundant NEED BOOST preset toggle")
check('NeedBoost="{Binding DataContext.CorpseSmartLootNeedBoost' in xaml
      and 'NeedBoostEnabled=' not in xaml, "only NEED BOOST value bound")

# ---- SMART ODDS: SmartLoot owns its category odds ------------------------------
check(all(catalog.get(i, {}).get("requiresToggle") == "loot.stashSmartLootEnabled"
          and catalog[i].get("slider") == {"min": 0, "max": 1, "step": 0.05, "stepMode": "absolute"}
          for i in SMART_ODDS), "SMART ODDS catalog (keep 0-100%, require the switch)")
check("public static string? SmartCategoryId(string sid)" in effective
      and '"loot.stashMixFood" or "loot.stashMixDrinks" => SmartFoodDrinksId' in effective
      and ".Concat(StashSmartOddsParameterIds)" in smart, "SMART ODDS writer/state wiring")
check('"SMART ODDS"' in board and "DrawStashMedicalCell(dc, P(spec.Id)" in board, "SMART ODDS cells on the board")
check(not any("smart world" in (catalog[i].get("description","") + catalog[i].get("rationale",""))
              for i in ("loot.stashMixMedical", "loot.stashMixFood", "loot.stashMixDrinks")),
      "Basic cells no longer claim to change smart loot")

# ---- SMART LOOT IN STASHES master switch ---------------------------------------
sw = catalog.get("loot.stashSmartLootEnabled", {})
check(sw.get("control") == "toggle" and sw.get("vanilla") == {"All": True}
      and all(catalog[i].get("requiresToggle") == "loot.stashSmartLootEnabled" for i in IDS),
      "switch: vanilla ON, knobs require it")
check("var smartOff = !effective.SmartLootEnabled;" in smart
      and "var endpointFactor = smartOff ? 0.0 : groupFactor * remaining;" in smart,
      "writer: OFF zeroes every group (0 / 0 / ItemSetCount 0)")
check("* (state.SmartLootEnabled ? 1.0 : 0.0)" in preview, "previews follow the switch")
check('DrawWorldCheckRow(dc, enabled, "STASH SMART LOOT"' in board, "board checkbox")

if failures:
    print(f"\n{len(failures)} failure(s)")
    sys.exit(1)
print("\nSMART LOOT NEED contract OK")
