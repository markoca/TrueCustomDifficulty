#!/usr/bin/env python3
"""LAIR POPULATION gate (docs/ALIFE-LAIR-POPULATION.md).

Static by default. With --base / --dlc pointing at the game's
LairPopulation sources it also rebuilds the manifest and requires an exact
match with the embedded one.
"""
import argparse
import csv
import io
import json
import re
import subprocess
import sys
import tempfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src/TrueCustomDifficulty"
MANIFEST = APP / "Data/lair-population-manifest.csv"
IDS = {"alife.lairPopulation.human": "human", "alife.lairPopulation.mutant": "mutant"}
fails = []


def check(cond, msg):
    if not cond:
        fails.append(msg)


def scale(source, m):
    return 0 if source <= 0 else max(1, int(source * m + 0.5))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base")
    ap.add_argument("--dlc")
    args = ap.parse_args()

    rows = list(csv.DictReader(MANIFEST.open(encoding="utf-8")))
    check(len(rows) == 1116, f"manifest rows {len(rows)} != 1116")
    check(sum(r["Scope"] == "baseGame" for r in rows) == 640, "base rows != 640")
    check(sum(r["Scope"] == "dlc1" for r in rows) == 476, "DLC1 rows != 476")
    check(sum(r["Group"] == "human" for r in rows) == 508, "human rows != 508")
    check(sum(r["Group"] == "mutant" for r in rows) == 608, "mutant rows != 608")
    keys = {(r["Scope"], r["Prototype"], r["FactionNode"], r["Rank"]) for r in rows}
    check(len(keys) == len(rows), "duplicate manifest rows")
    for r in rows:
        p = r["Prototype"]
        check(not p.startswith(("Guard", "[", "SIRCAA_", "MALACHITE_", "FW_"))
              and p not in ("Object68", "RestingLairDefault"), f"excluded prototype listed: {p}")
        check(r["Faction"] not in ("Scientists", "SIRCAA_Scientist", "MALACHITE_Scientist",
                                   "FW_Duty", "FW_Freedom", "Object68"), f"excluded faction listed: {r['Faction']}")
        check(r["SourceMax"].isdigit(), f"non-integer source {r}")
        timers = (r["InitialRespawnSeconds"], r["FullRespawnSeconds"], r["WipeRespawnSeconds"])
        check(timers in (("180.0", "480.0", "480.0"), ("6.0", "30.0", "30.0")), f"unexpected respawn timers {r}")
    check(sum((r["InitialRespawnSeconds"], r["FullRespawnSeconds"]) == ("180.0", "480.0") for r in rows) == 1104,
          "ordinary 180 / 480 / 480 s timers != 1104 entries")

    policy = (APP / "Services/LairPopulationPolicy.cs").read_text(encoding="utf-8")
    for token in ("ExpectedRows = 1116", "ExpectedBaseRows = 640", "ExpectedDlcRows = 476",
                  'HumanId = "alife.lairPopulation.human"', 'MutantId = "alife.lairPopulation.mutant"',
                  'RespawnTimeId = "alife.lairRespawnTime"',
                  "sourceSeconds <= 0.0 ? 0.0 : Math.Round(sourceSeconds * multiplier, 1, MidpointRounding.AwayFromZero)",
                  '"InitialSpawnQuantityRespawnTimeSeconds"', '"MaxSpawnQuantityRespawnTimeSeconds"',
                  '"WipeRespawnTimeoutSeconds"',
                  "source <= 0 ? 0 : Math.Max(1, (int)Math.Floor(source * multiplier + 0.5))"):
        check(token in policy, "policy drifted: " + token)
    writer = (APP / "Services/CfgPatchWriterService.LairPopulation.cs").read_text(encoding="utf-8")
    for token in ('"Preset", "PossibleInhabitantFactions", row.FactionNode,',
                  '"SpawnSettingsPerPlayerRanks", row.Rank', '"MaxSpawnQuantity"',
                  "IsScopeApplicable(row.Scope, hasDlc1)", "if (changed == 0)",
                  "foreach (var id in LairPopulationPolicy.PopulationIds)",
                  "LairPopulationPolicy.ScaleTimer(row.SourceTimers[t], multiplier)",
                  'value.ToString("0.0", CultureInfo.InvariantCulture)'):
        check(token in writer, "writer drifted: " + token)
    main_writer = (APP / "Services/CfgPatchWriterService.cs").read_text(encoding="utf-8")
    check("WriteLairPopulation(modifiedById, hasDlc1, documents, writtenParameters)" in main_writer,
          "WriteLairPopulation not called by Write()")
    check("supported.UnionWith(LairPopulationPolicy.ParameterIds);" in main_writer, "baseline support missing")
    csproj = (APP / "TrueCustomDifficulty.csproj").read_text(encoding="utf-8")
    check('LogicalName="TrueCustomDifficulty.Data.lair-population-manifest.csv"' in csproj, "manifest not embedded")
    baseline = json.loads((APP / "Data/cfg-writer-baseline.json").read_text(encoding="utf-8"))
    special = baseline["special"].get("lairPopulation", {})
    check(special.get("parameterIds") == list(IDS) + ["alife.lairRespawnTime"] and special.get("expectedRows") == 1116,
          "baseline special.lairPopulation drifted")
    check(not any(e["parameterId"].startswith("alife.lair") for e in baseline["editPoints"]),
          "lair controls have direct edit points")

    catalog = {p["id"]: p for p in json.loads((APP / "Data/parameters.json").read_text(encoding="utf-8"))["parameters"]}
    for pid in IDS:
        p = catalog.get(pid)
        check(p is not None, "catalog missing " + pid)
        if p:
            check(p["tab"] == "alife" and p["vanilla"] == {"All": 1}, pid + " tab / vanilla")
            check(p["slider"] == {"min": 0.25, "max": 3, "step": 0.05, "stepMode": "absolute"}, pid + " slider")
    check(catalog["alife.lairPopulation.human"].get("exposeInUi") is True, "human card must be the board anchor")
    check(catalog["alife.lairPopulation.mutant"].get("exposeInUi") is False, "mutant card is a board member")
    respawn = catalog.get("alife.lairRespawnTime", {})
    check(respawn.get("exposeInUi") is False and respawn.get("vanilla") == {"All": 1}
          and respawn.get("slider") == {"min": 0.25, "max": 4, "step": 0.05, "stepMode": "absolute"},
          "RESPAWN TIME catalog entry drifted")

    vm = (APP / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
    check('["alife.lairPopulation.human"] = "LairPopulation"' in vm
          and '["alife.lairPopulation.mutant"] = "LairPopulation"' in vm
          and '["alife.lairRespawnTime"] = "LairPopulation"' in vm
          and '["LairPopulation"] = "alife.lairPopulation.human"' in vm, "board family wiring")
    ui = (APP / "Controls/AggregateVisualControl.cs").read_text(encoding="utf-8")
    for token in ('case "LairPopulation":', '"LairPopulation" => LairPopulationBoardHeight(width)',
                  '"LairPopulation" => LairPopulationPolicy.ParameterIds', '"LAIR POPULATION"',
                  '"HUMAN CAMPS"', '"MUTANT NESTS"', '"RESPAWN TIME"', "LairPopulationPolicy.Example(",
                  "LairPopulationPolicy.ScaleTimer(LairPopulationPolicy.TypicalTimers[0], multiplier)",
                  "(3 + EconomyColumns(width) - 1) / EconomyColumns(width)"):
        check(token in ui, "board drifted: " + token)
    mvm = (APP / "ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
    check("LairPopulationPolicy.ParameterIds.Contains(p.Id)" in mvm, "hidden member has no view model")

    # Factory presets: every non-vanilla value must change at least one lair.
    lair = json.loads((ROOT / "tools/factory_preset_lair_population_2026_11_18.json").read_text(encoding="utf-8"))
    for path in sorted((APP / "Presets").glob("*.json")):
        values = json.loads(path.read_text(encoding="utf-8"))["values"]
        for pid, group in IDS.items():
            m = values[pid]["number"]
            check(abs(round((m - 0.25) / 0.05) * 0.05 + 0.25 - m) < 1e-9 and 0.25 <= m <= 3, f"{path.name} {pid} off grid")
            check(lair["values"][path.stem].get(pid, 1) == m, f"{path.name} {pid} != matrix")
            if m != 1:
                check(any(scale(int(r["SourceMax"]), m) != int(r["SourceMax"]) for r in rows if r["Group"] == group),
                      f"{path.name} {pid} changes nothing")
        respawn_time = values["alife.lairRespawnTime"]["number"]
        check(abs(round((respawn_time - 0.25) / 0.05) * 0.05 + 0.25 - respawn_time) < 1e-9 and 0.25 <= respawn_time <= 4,
              f"{path.name} respawn time off grid")
        check(lair["values"][path.stem].get("alife.lairRespawnTime", 1) == respawn_time, f"{path.name} respawn time != matrix")

    if args.base and args.dlc:
        with tempfile.TemporaryDirectory() as tmp:
            backup = MANIFEST.read_bytes()
            try:
                subprocess.run([sys.executable, str(ROOT / "tools/build_lair_population_manifest.py"),
                                args.base, args.dlc], check=True, capture_output=True)
                check(MANIFEST.read_bytes() == backup, "manifest does not match the game CFG")
            finally:
                MANIFEST.write_bytes(backup)

    if fails:
        for f in fails:
            print("FAIL", f)
        sys.exit(1)
    print("PASS lair population: 1116 eligible lair entries (640 base + 476 DLC1; 508 human / 608 mutant), "
          "RESPAWN TIME over 3 timers each, writer, board and 12 factory presets in step")


if __name__ == "__main__":
    main()
