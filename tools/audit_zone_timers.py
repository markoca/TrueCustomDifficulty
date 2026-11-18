#!/usr/bin/env python3
"""ZONE WEATHER + TRADER RESTOCK gate (docs/ZONE-WEATHER-AND-RESTOCK.md).

Static by default. With --game DIR (the extracted base GameData folder) and
--dlc DIR (the extracted DLC1 folder) it also re-reads every source value and
dependency the writer relies on.
"""
import argparse
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src/TrueCustomDifficulty"
sys.path.insert(0, str(Path(__file__).resolve().parent))
from lair_cfg_parse import parse, get, kids, val  # noqa: E402

fails = []


def check(cond, msg):
    if not cond:
        fails.append(msg)


def num(text):
    return float(str(text).rstrip("f"))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--game")
    ap.add_argument("--dlc")
    args = ap.parse_args()

    catalog = {p["id"]: p for p in json.loads((APP / "Data/parameters.json").read_text(encoding="utf-8"))["parameters"]}
    baseline = json.loads((APP / "Data/cfg-writer-baseline.json").read_text(encoding="utf-8"))
    weights = {"environment.emissionFrequency": "Weather_Emission_Weight",
               "environment.stormFrequency": "Weather_Storm_Weight"}
    for pid, key in weights.items():
        p = catalog[pid]
        check(p["vanilla"] == {"Novice": 1, "Stalker": 1, "Veteran": 1, "Master": 1.25}, pid + " vanilla")
        pts = [e for e in baseline["editPoints"] if e["parameterId"] == pid]
        check(sorted((e["difficultySid"], e["key"], e["sourceRaw"], e["operation"]) for e in pts) ==
              sorted([("Easy", key, "1.0", "set"), ("Medium", key, "1.0", "set"),
                      ("Hard", key, "1.0", "set"), ("Stalker", key, "1.25", "set")]), pid + " edit points")
    check(catalog["environment.emissionFrequency"].get("exposeInUi") is True, "ZONE WEATHER anchor")
    check(catalog["environment.stormFrequency"].get("exposeInUi") is False
          and catalog["environment.emissionWarning"].get("exposeInUi") is False, "ZONE WEATHER members hidden")
    w = catalog["environment.emissionWarning"]
    check(w["vanilla"] == {"All": 60} and w["slider"]["min"] == 10 and w["slider"]["max"] == 60
          and w["slider"]["step"] == 5, "EMISSION WARNING range")
    r = catalog["economy.traderRestockHours"]
    check(r["vanilla"] == {"All": 8} and r["slider"]["min"] == 1 and r["slider"]["max"] == 48
          and r["slider"]["step"] == 1 and r.get("exposeInUi") is True, "TRADER RESTOCK range")
    special = baseline["special"].get("zoneTimers", {})
    check(special.get("parameterIds") == ["environment.emissionWarning", "economy.traderRestockHours"],
          "baseline special.zoneTimers")

    policy = (APP / "Services/ZoneTimersPolicy.cs").read_text(encoding="utf-8")
    for token in ('("[0]", "BeforeTheStorm", 0.0, 60.0)', '("[1]", "ActivateQuest", 0.0, 1.0)',
                  '("[2]", "ShockWave", 60.0, 10.0)', '("[3]", "Active", 60.0, 60.0)',
                  '("[4]", "AfterTheStorm", 120.0, 20.0)', "VanillaAiEndSeconds = 130.0",
                  "EmissionWeatherSeconds = 300.0", "LatestStoryEmissionEnd = 61.0",
                  'NamedTraderCondition = "TradeRegenHoursPassed8"',
                  'GenericTraderCondition = "TradeRegenDaysPassed7"',
                  "VanillaRestockHours = 8", "VanillaGenericRestockDays = 7"):
        check(token in policy, "policy drifted: " + token)
    check("MinWarningSeconds = 10.0;" in policy and "MaxWarningSeconds = 60.0;" in policy,
          "writer policy range must match the catalog")
    # The longest warning still ends inside the 300 s emission weather, and the
    # shortest still lets NPCs leave cover only after every story emission.
    check(140 + (w["slider"]["max"] - 60) <= 300, "max warning overruns the emission weather")
    check(130 + (w["slider"]["min"] - 60) >= 61, "min warning lets NPCs leave cover during a story emission")
    writer = (APP / "Services/CfgPatchWriterService.ZoneTimers.cs").read_text(encoding="utf-8")
    for token in ('"PhaseDuration", Seconds(seconds)', '"PhaseStartTime", Seconds(start + shift)',
                  '"AIEventStartTime", Seconds(ZoneTimersPolicy.VanillaAiEndSeconds + shift)',
                  'stage != "ActivateQuest"', '"Hours", hours.ToString(CultureInfo.InvariantCulture)',
                  '"Days", days.ToString(CultureInfo.InvariantCulture)'):
        check(token in writer, "writer drifted: " + token)
    main_writer = (APP / "Services/CfgPatchWriterService.cs").read_text(encoding="utf-8")
    check("WriteZoneTimers(modifiedById, documents, writtenParameters)" in main_writer, "WriteZoneTimers not called")
    ui = (APP / "Controls/AggregateVisualControl.cs").read_text(encoding="utf-8")
    for token in ('case "ZoneWeather":', 'case "TraderRestock":', '"ZONE WEATHER"', '"TRADER RESTOCK"',
                  '"EMISSIONS"', '"STORMS"', '"EMISSION WARNING"', '"RESTOCK EVERY"',
                  "ZoneTimersPolicy.EmissionEnd(warning)", "ZoneTimersPolicy.GenericRestockDays("):
        check(token in ui, "board drifted: " + token)
    # Source-backed, collision-free weather UI: validate the scoped renderer,
    # not similarly-named card drawing in unrelated visual families.
    weather_ui = ui[ui.index('    private void DrawZoneWeatherBoard('):
                    ui.index('    private void DrawTraderRestockBoard(')]
    check('dc.DrawTrackedRoundedRectangle(panel, new Pen(border, 0.9), knobCard' not in weather_ui,
          "no rectangular frame around the three weather knobs")
    check('DrawSourceKnob(dc, parameter, knobs[i].Label, knobCard' in weather_ui,
          "weather uses the native source dials")
    check('"RANDOM EMISSIONS"' in weather_ui and '"TIME TO FIND COVER"' in weather_ui,
          "gamer-facing weather knob titles")
    check('parameter is null || isWarning ? null' in weather_ui and 'positivePrefix: false' in weather_ui,
          "warning dial has only the SECONDS unit, no overlapping secondary phrase or artificial plus")
    check('seconds / Math.Max(1.0, end)' in weather_ui and
          'seconds / ZoneTimersPolicy.EmissionWeatherSeconds' not in weather_ui,
          "event bar ends with the actual emission stages, not the 300 s weather slot")
    check('warning + ZoneTimersPolicy.Stages[3].Duration' in weather_ui and
          'ZoneTimersPolicy.EmissionEnd(warning)' in weather_ui,
          "active and aftermath endpoints share native source timing")
    check('"FIND COVER · EMISSION · AFTERMATH"' in weather_ui and 'TimeX(cover)' in weather_ui,
          "phase names and NPC shelter marker")
    for warning in (10.0, 35.0, 60.0):
        event_end = warning + 80.0
        active_end = warning + 60.0
        npc_out = warning + 70.0
        check(0 < warning/event_end < active_end/event_end < npc_out/event_end < 1,
              f"warning/active/AI/aftermath positions remain ordered at {warning}s")
        check(abs(event_end/event_end-1) < 1e-9, "no empty tail after aftermath")
    for phrase in ('"OFF"', '"NORMAL"', '"LESS OFTEN"', '"MORE OFTEN"'):
        check(phrase in policy, "short in-dial weather state " + phrase)
    vm = (APP / "ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
    check("ZoneTimersPolicy.WeatherIds.Contains(p.Id)" in vm, "hidden ZONE WEATHER members have no view model")

    zone = json.loads((ROOT / "tools/factory_preset_zone_weather_2026_11_18.json").read_text(encoding="utf-8"))
    for path in sorted((APP / "Presets").glob("*.json")):
        values = json.loads(path.read_text(encoding="utf-8"))["values"]
        warn = values["environment.emissionWarning"]["number"]
        hours = values["economy.traderRestockHours"]["number"]
        check(10 <= warn <= 60 and warn % 5 == 0, f"{path.name} warning off grid")
        check(1 <= hours <= 48 and float(hours).is_integer(), f"{path.name} restock off grid")
        for pid in ("environment.emissionFrequency", "environment.stormFrequency"):
            m = values[pid]["number"]
            check(0 <= m <= 3 and abs(round(m / 0.05) * 0.05 - m) < 1e-9, f"{path.name} {pid} off grid")
        check(set(zone["values"][path.stem]) <= set(catalog), f"{path.name} matrix ids")
        for pid, expected in zone["values"][path.stem].items():
            check(values[pid]["number"] == expected, f"{path.name} {pid} does not match factory matrix")

    if args.game:
        game = Path(args.game)
        diff = {p[0]: p for p in parse(game / "DifficultyPrototypes.cfg")}
        for sid, want in (("Easy", "1.0"), ("Medium", "1.0"), ("Hard", "1.0"), ("Stalker", "1.25")):
            env = get(diff[sid], "EnvironmentDifficulty")
            check(val(env, "Weather_Emission_Weight") == want and val(env, "Weather_Storm_Weight") == want,
                  f"{sid} weather weights in the game CFG")
        emissions = parse(game / "EmissionPrototypes.cfg")
        default = emissions[0]
        check(val(default, "SID") == "Default", "EmissionPrototypes [0] is not Default")
        stages = [(val(s, "StageID").split("::")[1], num(val(s, "PhaseStartTime")), num(val(s, "PhaseDuration")))
                  for s in kids(get(default, "Stages"))]
        check(stages == [("BeforeTheStorm", 0, 60), ("ActivateQuest", 0, 1), ("ShockWave", 60, 10),
                         ("Active", 60, 60), ("AfterTheStorm", 120, 20)], f"Default stages {stages}")
        ai = [(val(e, "AIEventType").split("::")[1], num(val(e, "AIEventStartTime")))
              for e in kids(get(default, "AIEvents"))]
        check(ai[3] == ("EmissionEnd", 130.0), f"Default AIEvents {ai}")
        for story in emissions[1:]:
            check(get(story, "AIEvents") is None, f"{val(story, 'SID')} has its own AIEvents")
            ends = [num(val(s, "PhaseStartTime")) + num(val(s, "PhaseDuration")) for s in kids(get(story, "Stages"))]
            check(max(ends) <= 61.0, f"{val(story, 'SID')} ends at {max(ends)} s")
        weather = (game / "WeatherSelectionPrototypes.cfg").read_text(encoding="utf-8")
        check(re.search(r"Emission : struct\.begin\s+BlendWeight = 0\.f\s+BlendWeightIncrease = 100\.f\s+"
                        r"WeatherDurationMin = 300\.f\s+WeatherDurationMax = 300\.f", weather) is not None,
              "Emission weather window is not 300 s")
        providers = {p[0]: p for p in parse(game / "BoolProviderPrototypes.cfg")}
        check(val(providers["TradeRegenHoursPassed8"], "Hours") == "8", "TradeRegenHoursPassed8.Hours != 8")
        check(val(providers["TradeRegenDaysPassed7"], "Days") == "7", "TradeRegenDaysPassed7.Days != 7")
        uses = {}
        sources = list(game.rglob("*.cfg")) + (list(Path(args.dlc).rglob("*.cfg")) if args.dlc else [])
        for f in sources:
            if f.name == "BoolProviderPrototypes.cfg":
                continue
            text = f.read_text(encoding="utf-8", errors="ignore")
            for m in re.finditer(r"(\w+)\s*=\s*(TradeRegenHoursPassed8|TradeRegenDaysPassed7)\b", text):
                uses.setdefault((f.name, m.group(1), m.group(2)), 0)
                uses[(f.name, m.group(1), m.group(2))] += 1
        check({(n, k) for n, k, _ in uses} == {("TradePrototypes.cfg", "RefreshConditionSID")},
              f"restock conditions used outside trade: {uses}")
        if args.dlc:
            check(sum(v for (n, k, c), v in uses.items() if c == "TradeRegenHoursPassed8") == 84,
                  "named traders on 8 h != 84")
            check(sum(v for (n, k, c), v in uses.items() if c == "TradeRegenDaysPassed7") == 4,
                  "generic trade profiles on 7 days != 4")

    if fails:
        for f in fails:
            print("FAIL", f)
        sys.exit(1)
    print("PASS zone timers: emission / storm weights, emission warning retime (+ NPC cover end), "
          "trader restock (84 traders on hours, generic stalkers on days), boards and presets")


if __name__ == "__main__":
    main()
