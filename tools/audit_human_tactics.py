#!/usr/bin/env python3
"""HUMAN TACTICS + HUMAN AWARENESS contract (docs/combat/HUMAN-TACTICS-AND-AWARENESS.md).

Policy <-> embedded CombatSynchronization tables <-> catalog <-> writer baseline
<-> writer <-> UI. Optional: TCD_BASE_DUMP=<dump>/base checks the NPC weapon
inventory against the dump.
"""
from __future__ import annotations

import json
import os
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TrueCustomDifficulty"
failures: list[str] = []


def check(cond: bool, message: str) -> None:
    print(("[OK] " if cond else "[FAIL] ") + message)
    if not cond:
        failures.append(message)


read = lambda rel: (SRC / rel).read_text(encoding="utf-8")
policy = read("Services/HumanCombatPolicy.cs")
writer = read("Services/CfgPatchWriterService.MutantAggression.cs")
main_writer = read("Services/CfgPatchWriterService.cs")
ui = read("Controls/AggregateVisualControl.cs")
pvm = read("ViewModels/ParameterViewModel.cs")
mvm = read("ViewModels/MainViewModel.cs")
tab = read("ViewModels/TabViewModel.cs")
catalog = {r["id"]: r for r in json.loads(read("Data/parameters.json"))["parameters"]}
baseline = json.loads(read("Data/cfg-writer-baseline.json"))
tables = json.loads(read("Data/combat-synchronization-2.1.json"))["tables"]

# ---- tactics: policy vs embedded tables ------------------------------------------
controls = re.findall(
    r'new\((\w+), "([A-Z ]+)", "[^"]+",\s*new\[\] \{ (.*?) \},\s*(true|false), \((\d+), (\d+)\), \((\d+), (\d+)\)\)',
    policy, re.S)
check(len(controls) == 5, f"5 tactic controls ({len(controls)})")
num = lambda s: float(s.rstrip("f").rstrip("."))
for const, label, groups, halves, dmin, dmax, mmin, mmax in controls:
    pairs = re.findall(r'\("(\[\d+\])", "([\w.]+)"\)', groups)
    for sid, (lo, hi) in (("Default", (dmin, dmax)), ("CombatSynchronizerStalker", (mmin, mmax))):
        for rank in ("Newbie", "Experienced", "Veteran", "Master"):
            fg = tables[sid][rank]["children"]["FilterGroups"]["children"]
            for index, tag in pairs:
                check(fg[index]["children"]["AllowedTags"]["fields"]["[0]"] == tag,
                      f"{label}: {sid}.{rank}{index} is {tag}")
        first = tables[sid]["Newbie"]["children"]["FilterGroups"]["children"][pairs[0][0]]["fields"]
        check((num(first["CooldownMin"]), num(first["CooldownMax"])) == (float(lo), float(hi)),
              f"{label}: display seconds {sid} Newbie {lo}–{hi}")
    d = tables["Default"]["Newbie"]["children"]["FilterGroups"]["children"][pairs[0][0]]["fields"]
    m = tables["CombatSynchronizerStalker"]["Newbie"]["children"]["FilterGroups"]["children"][pairs[0][0]]["fields"]
    halved = num(d["CooldownMin"]) > num(m["CooldownMin"])
    check(halved == (halves == "true"), f"{label}: Master halves = {halves}")

ids = {"GrenadeRateId": "combat.npcGrenadeRate", "SuppressionRateId": "combat.npcSuppressionRate",
       "FlankRateId": "combat.npcFlankRate", "PushRateId": "combat.npcPushRate",
       "CamperPunishRateId": "combat.npcCamperPunishRate"}
for const, label, *_rest in controls:
    pid = ids[const]
    row = catalog.get(pid, {})
    halves = _rest[1] == "true"
    expected = {"Novice": 1, "Stalker": 1, "Veteran": 1, "Master": 2} if halves else {"All": 1}
    check(row.get("vanilla") == expected and row.get("group") == "Human Tactics", f"{pid}: vanilla / group")
    check(row.get("support", {}).get("status") == "runtime_test_required", f"{pid}: runtime_test_required")
entry = baseline["special"].get("humanTactics", {})
check(sorted(entry.get("parameterIds", [])) == sorted(ids.values()), "baseline special.humanTactics")
check("supported.UnionWith(HumanCombatPolicy.TacticIds);" in main_writer, "writer supports the tactic ids")
for token in ("private static int WriteCombatSynchronization(", "factors[index] = reference / rate;",
              "HumanCombatPolicy.Reference(c, difficultySid)", ".Concat(HumanCombatPolicy.Tactics.SelectMany(c => c.Groups))",
              "(ParseNumber(value) * factor)"):
    check(token in writer, f"combat sync writer token {token}")
check("writtenParameters.Add(MutantAggressionPolicy.SpecialAbilityRateId)" not in writer,
      "SPECIAL ABILITY RATE goes through the shared writer")

# ---- accuracy / awareness direct points ----------------------------------------------
pts = [p for p in baseline["editPoints"] if p["parameterId"] in
       ("combat.npcAccuracy", "combat.npcSightRange", "combat.npcHearingRange", "combat.npcAlertMemory")]
acc = [p for p in pts if p["parameterId"] == "combat.npcAccuracy"]
check(len(acc) == 77 and all(p["key"] == "DispersionRadius" and p["operation"] == "inverseScale"
                             and p["prototype"].endswith("_NPC") for p in acc), "accuracy: 77 NPC DispersionRadius inverseScale")
check(sum(p["scope"] == "dlc1" for p in acc) == 2, "accuracy: 2 DLC1 NPC weapons")
sight = [p for p in pts if p["parameterId"] == "combat.npcSightRange"]
check({(p["prototype"], p["key"], p["sourceRaw"]) for p in sight}
      == {("DefaultNPC", "CentralVisionDistance", "8000.f"), ("DefaultNPC", "PeripheralVisionDistance", "2500.f")},
      "sight: DefaultNPC 8000 / 2500")
hear = [p for p in pts if p["parameterId"] == "combat.npcHearingRange"]
check(len(hear) == 14 and all(p["prototype"] == "BaseHearingSensor" and p["operation"] == "scale" for p in hear),
      "hearing: 14 non-zero BaseHearingSensor distances")
mem = {(tuple(p["nodePath"]), p["key"], p["operation"]) for p in pts if p["parameterId"] == "combat.npcAlertMemory"}
check(mem == {((), "DefaultThreatValueFreezeTimeSeconds", "scale"), ((), "DefaultThreatValueLossPerSecond", "inverseScale"),
              (("Actions", "[3]"), "ThreatValueFreezeTimeSeconds", "scale"),
              (("Actions", "[3]"), "ThreatValueLossPerSecond", "inverseScale")}, "memory: freeze x m, loss / m")
for pid in ("combat.npcAccuracy", "combat.npcSightRange", "combat.npcHearingRange", "combat.npcAlertMemory"):
    row = catalog.get(pid, {})
    check(row.get("vanilla") == {"All": 1} and row.get("support", {}).get("status") == "runtime_test_required",
          f"{pid}: vanilla 1, runtime_test_required")

# ---- UI ----------------------------------------------------------------------------------
for token in ('case "HumanTactics":', 'case "HumanAwareness":', 'DrawHumanTacticsBoard(dc, text, muted, border, accent, panel)',
              'DrawHumanAwarenessRadar(dc, text, muted, border, accent, panel)',
              '"HumanAwareness" => HumanAwarenessHeight(width)', '"HumanTactics" => HumanCombatPolicy.TacticsBoardIds',
              '"HumanAwareness" => HumanCombatPolicy.AwarenessIds', "private static string CardReadout("):
    check(token in ui, f"UI token {token}")
check('["HumanTactics"] = "combat.npcGrenadeRate"' in pvm and '["HumanAwareness"] = "combat.npcSightRange"' in pvm,
      "family anchors")
check("HumanCombatPolicy.TacticsBoardIds.ToArray()" in mvm and "HumanCombatPolicy.AwarenessIds.ToArray()" in mvm,
      "board modified state")
check('"Human Tactics" => 2,' in tab and '"Human Awareness" => 3,' in tab, "combat section order")

dump = os.environ.get("TCD_BASE_DUMP")
if dump:
    text = (Path(dump) / "GameData" / "WeaponData" / "CharacterWeaponSettingsPrototypes.cfg").read_text(encoding="utf-8", errors="replace")
    npc = re.findall(r"^(\w+_NPC) : struct\.begin", text, re.M)
    check(len(npc) == 75 and {p["prototype"] for p in acc if p["scope"] == "baseGame"} == set(npc),
          f"dump: 75 base NPC weapon settings ({len(npc)})")

# Custom tactical visuals must be source-bound, interactive and measured.
tactics_ui = read("Controls/AggregateVisualControl.HumanTactics.cs")
for token in ("HumanTacticsHeight(double width)", "DrawHumanTacticsBoard(",
              "HumanCombatPolicy.Tactics", "HumanCombatPolicy.CooldownSeconds(",
              "HitKind.TacticalCooldown", "HitKind.TacticalSpread",
              "ReadoutInk.Numbers(accent, text)", "ControlHoverGlow.Draw(",
              "Snap(parameter,", "Math.Log(", "Math.Exp("):
    check(token in tactics_ui, f"tactics editor token {token}")
for token in ("HitKind.TacticalCooldown", "HitKind.TacticalSpread",
              "UpdateHumanTacticsTarget(target, point)",
              '"HumanTactics" => HumanTacticsHeight(width)'):
    check(token in ui, f"tactics input/measure token {token}")
check('var wide = width >= 750.0;' in
      read("Controls/AggregateVisualControl.HumanAwareness.cs"),
      "awareness responsive legend breakpoint")

if failures:
    print(f"\n{len(failures)} failure(s)")
    sys.exit(1)
radar = (ROOT / "src/TrueCustomDifficulty/Controls/AggregateVisualControl.HumanAwareness.cs").read_text(encoding="utf-8")
for token in ("AwarenessSightMeters = 80.0", "AwarenessSideMeters = 25.0", "AwarenessHearingMeters = 12.0",
              "AwarenessMemorySeconds = 30.0", "AwarenessHalfAngle = 85.0", "HitKind.RadarRadius", "HitKind.RadarArc",
              "CardValue(parameter, parameter.CurrentNumber)", "ReadoutInk.Numbers(accent, text)"):
    check(token in radar, f"radar token {token}")
print("\nHUMAN TACTICS / HUMAN AWARENESS contract OK")
