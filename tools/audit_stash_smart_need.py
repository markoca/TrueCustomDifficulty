#!/usr/bin/env python3
"""SMART LOOT NEED + SMART odds columns contract (2026-11-17).

Two catalog sliders scale the stash SmartLoot spawn endpoints:
WHEN STOCKED -> MinSpawnChance, WHEN SHORT -> MaxSpawnChance. Writer and every
preview share StashEffectiveState.SmartNeedEndpoints (clamp 0..1, stocked never
above short). The ODDS tables show a SMART column from the same endpoints.
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
check(baseline["special"].get("stashSmartNeed", {}).get("parameterIds") == list(IDS),
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
      and '"SMART LOOT NEED"' in board and '"WHEN STOCKED"' in board and '"WHEN SHORT"' in board
      and '"CHANCE A SET APPEARS"' in board, "SMART LOOT NEED board")
check('"SMART"' in odds and "StashSmartCategoryOdds(current)" in odds
      and 'StashSmartOdds(state, StashCategoryIndex' in odds
      and "(outer.Right - 12.0 - gridLeft - 3.0 * gap) / 4.0" in odds, "SMART column in STASH LOOT MIX odds")
check('"ArtifactHunting" or "StashSmartLoot" or "AmmoDistribution"' in ui, "mouse wheel on knob boards")
pvm = read("ViewModels/ParameterViewModel.cs")
check('["StashSmartLoot"] = "loot.stashSmartStockedChance"' in pvm
      and all(f'["{pid}"] = "StashSmartLoot"' in pvm for pid in IDS), "family map and anchor")

if failures:
    print(f"\n{len(failures)} failure(s)")
    sys.exit(1)
print("\nSMART LOOT NEED contract OK")
