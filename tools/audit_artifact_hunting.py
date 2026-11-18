#!/usr/bin/env python3
"""ARTIFACT HUNTING contract (docs/environment/ARTIFACT-HUNTING.md).

Policy <-> catalog <-> writer baseline <-> special writer <-> UI. Optional:
TCD_BASE_DUMP=<dump>/base also compares the spawner values with the 2.1 dump.
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


policy = (SRC / "Services" / "ArtifactSpawnPolicy.cs").read_text(encoding="utf-8")
special = (SRC / "Services" / "CfgPatchWriterService.Artifacts.cs").read_text(encoding="utf-8")
writer = (SRC / "Services" / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
ui = (SRC / "Controls" / "AggregateVisualControl.cs").read_text(encoding="utf-8")
pvm = (SRC / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")
mvm = (SRC / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
catalog = json.loads((SRC / "Data" / "parameters.json").read_text(encoding="utf-8"))["parameters"]
by_id = {r["id"]: r for r in catalog}
baseline = json.loads((SRC / "Data" / "cfg-writer-baseline.json").read_text(encoding="utf-8"))

IDS = ["environment.artifactSpawnChance", "environment.artifactRareLuck", "environment.artifactCount"]
RETIRED = "environment.artifactRespawnRate"  # 2026-11-17: refill is emission-driven, not cooldown
SPAWNERS = ["UniversalArtifactSpawner", "LesserZoneMagneticShortDistance"]
RANKS = ["Newbie", "Experienced", "Veteran", "Master"]
VANILLA_RARITY = [[80, 20, 0, 0], [50, 48, 1.9, 0.1], [30, 59, 10, 1], [10, 65, 20, 5]]

# ---- policy -------------------------------------------------------------------
for pid in IDS:
    check(f'"{pid}"' in policy, f"policy names {pid}")
check(all(f'"{s}"' in policy for s in SPAWNERS), "policy names both world spawners")
rows = re.findall(r"new\[\] \{ ([\d.]+), ([\d.]+), ([\d.]+), ([\d.]+) \}", policy)
check([[float(x) for x in r] for r in rows] == [[float(x) for x in r] for r in VANILLA_RARITY],
      "policy vanilla rarity split per zone rank")
check(all(abs(sum(r) - 100) < 1e-9 for r in VANILLA_RARITY), "each vanilla split sums to 100")


def rarity(rank: int, luck: float) -> list[float]:
    c, u, r, e = VANILLA_RARITY[rank]
    scaled = [c, u, r * luck, e * luck]
    total = sum(scaled)
    return [round(v * 100 / total, 2) for v in scaled]


check(rarity(0, 4) == [80, 20, 0, 0], "Newbie zone keeps 80/20/0/0 at any luck")
check(abs(sum(rarity(3, 4)) - 100) < 0.05 and rarity(3, 4)[2] > 40, "Master zone at 4x: rare share grows, sum 100")
check(abs(sum(rarity(3, 0.25)) - 100) < 0.05, "Master zone at 0.25x sums to 100")

# ---- catalog --------------------------------------------------------------------
for pid in IDS:
    row = by_id.get(pid)
    check(row is not None, f"catalog has {pid}")
    if not row:
        continue
    check(row["tab"] == "environment" and row["group"] == "Artifacts" and row.get("exposeInUi", True),
          f"{pid}: visible in Anomalies → Artifacts")
    check(row.get("vanilla") == {"All": 1} and row["default"]["mode"] == "fixedVanilla", f"{pid}: vanilla 1")
    check(row["support"]["status"] == "runtime_test_required", f"{pid}: runtime_test_required")
s = {pid: (by_id[pid]["slider"]["min"], by_id[pid]["slider"]["max"], by_id[pid]["slider"]["step"]) for pid in IDS}
check(s == {IDS[0]: (0, 4, 0.1), IDS[1]: (0.25, 4, 0.25), IDS[2]: (1, 3, 1)},
      f"slider ranges {s}")

# ---- writer baseline --------------------------------------------------------------
points = [p for p in baseline["editPoints"] if p["parameterId"].startswith("environment.artifact")]
check(len(points) == 24, f"24 direct artifact edit points ({len(points)})")
check(RETIRED not in by_id and RETIRED not in policy and RETIRED not in pvm
      and not any(p["key"] in ("MinCooldown", "MaxCooldown") for p in points),
      "RESPAWN RATE retired: no catalog row, policy id, help mapping or cooldown edit")
expected = {
    IDS[0]: ({"SpawnChanceBase", "SpawnChanceBonus"}, "scaleClamp100", {"25.f", "15.f"}),
    IDS[2]: ({"Count"}, "set", {"1"}),
}
for pid, (keys, op, raws) in expected.items():
    pts = [p for p in points if p["parameterId"] == pid]
    check(len(pts) == len(keys) * 8
          and {p["key"] for p in pts} == keys
          and {p["operation"] for p in pts} == {op}
          and {p["sourceRaw"] for p in pts} == raws
          and {p["prototype"] for p in pts} == set(SPAWNERS)
          and {p["nodePath"][0] for p in pts} == set(RANKS)
          and all(p["difficultySid"] == "" and p["file"] == "GameData/ArtifactSpawnerPrototypes.cfg" for p in pts),
          f"{pid}: {op} on {sorted(keys)} for 2 spawners x 4 zone ranks")
check(not [p for p in points if p["parameterId"] == IDS[1]], "rare luck has no direct points (special writer)")
entry = baseline["special"].get("artifactHunting", {})
check(entry.get("parameterId") == IDS[1] and entry.get("spawners") == SPAWNERS, "baseline special.artifactHunting")
check('"scaleClamp100" =>' in writer, "writer supports scaleClamp100")
check("WriteArtifactRareLuck(modifiedById, documents, writtenParameters)" in writer
      and "supported.Add(ArtifactSpawnPolicy.RareLuckId);" in writer, "main writer calls the rare-luck writer")
check("vanilla[2] + vanilla[3] <= 0.0" in special and "ArtifactSpawnPolicy.Rarity(rank, luck)" in special,
      "rare-luck writer skips Newbie and uses the shared policy split")

# ---- UI ------------------------------------------------------------------------------
for token in ('case "ArtifactHunting":', 'DrawArtifactHunting(', '"ARTIFACT HUNTING"', '"RARITY BY ZONE"',
              '"ArtifactHunting" => ArtifactHuntingHeight(width)', '"ArtifactHunting" => ArtifactSpawnPolicy.ParameterIds',
              'ArtifactSpawnPolicy.CardText(parameter.Id, value)',
              'ArtifactKnobBank(ActualWidth)', 'DrawSourceKnob(dc, parameter, controls[i].Label, knobCard,',
              'ArtifactSpawnPolicy.KnobPercentage(controls[i].Id, value)'):
    check(token in ui, f"UI token {token}")
artifact_ui = ui[ui.index('    private void DrawArtifactHunting('):
                 ui.index('    // This native-axis graph uses', ui.index('    private void DrawArtifactHunting('))]
check('dc.DrawTrackedRoundedRectangle(panel, new Pen(border, 0.9), knobCard' not in artifact_ui,
      "ARTIFACT HUNTING source knobs have no rectangular card frames")
check('"RARE+ ARTIFACT CHANCE"' in artifact_ui and '"BASE ODDS"' in artifact_ui
      and '"MASTER ZONES"' in artifact_ui, "artifact dials explain actual source-backed odds")
check('valueFormatter: isCount ? null' in artifact_ui
      and 'valueFormatter is not null ? valueFormatter(parameter.CurrentNumber)' in ui
      and 'valueFormatter is not null ? valueFormatter(parameter.Minimum)' in ui
      and 'valueFormatter is not null ? valueFormatter(dialMaximum)' in ui,
      "current, min and max all display effective percent with native rate editing")
check('public static string KnobPercentage(string id, double value)' in policy
      and 'RarePlusShare(3, value)' in policy and 'SpawnChance(value).Base' in policy,
      "effective percentages come from shared writer policy")
check(by_id[IDS[1]]["label"] == "Rare+ Artifact Chance", "catalog defines exact Rare+ terminology")
check(all(f'["{pid}"] = "ArtifactHunting"' in pvm for pid in IDS)
      and '["ArtifactHunting"] = "environment.artifactSpawnChance"' in pvm
      and '"ArtifactHunting" =>' in pvm, "family map, anchor and CONTROLS help")
check("ArtifactSpawnPolicy.SpawnChanceId,\n            ArtifactSpawnPolicy.ParameterIds.ToArray());" in mvm
      and '"Artifacts" => 400,' in mvm, "board modified state and Anomalies order")

# ---- optional dump ---------------------------------------------------------------------
dump = os.environ.get("TCD_BASE_DUMP")
if dump:
    text = (Path(dump) / "GameData" / "ArtifactSpawnerPrototypes.cfg").read_text(encoding="utf-8", errors="replace")
    for sid in SPAWNERS:
        block = re.search(r"^" + sid + r" : struct\.begin.*?\n(.*?)^struct\.end", text, re.S | re.M).group(1)
        for i, rank in enumerate(RANKS):
            rb = re.search(r"\n   " + rank + r" : struct\.begin\n(.*?)\n   struct\.end", block, re.S).group(1)
            got = [float(re.search(r"^\s+" + k + r" = ([\d.]+)", rb, re.M).group(1))
                   for k in ("Common", "Uncommon", "Rare", "Epic")]
            check(got == [float(x) for x in VANILLA_RARITY[i]], f"dump {sid}.{rank} rarity {got}")
    spawn = (Path(dump) / "GameData" / "SpawnActorPrototypes.cfg").read_text(encoding="utf-8", errors="replace")
    check(spawn.count("SpawnedPrototypeSID = UniversalArtifactSpawner") >= 73, "dump: Universal spawner is the world source")

if failures:
    print(f"\n{len(failures)} failure(s)")
    sys.exit(1)
print("\nARTIFACT HUNTING contract OK")
