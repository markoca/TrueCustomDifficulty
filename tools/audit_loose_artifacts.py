#!/usr/bin/env python3
"""REMOVE LOOSE ARTIFACTS contract (2026-11-17).

Static: catalog row, baseline special entry, embedded manifest, writer guard,
UI row and help. Optional source check: TCD_BASE_DUMP / TCD_DLC1_DUMP point to
extracted CFG roots; every manifest actor must be a WorldMap_WP ArtifactSpawner
with SpawnOnStart true, its spawner a single fixed artifact, and no other CFG
may reference the actor (no quest use).
"""
import csv
import json
import os
import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src" / "TrueCustomDifficulty"
PID = "loot.looseArtifactsRemoved"
failures = []


def check(ok, label):
    print(("[PASS] " if ok else "[FAIL] ") + label)
    if not ok:
        failures.append(label)


catalog = {p["id"]: p for p in json.loads((APP / "Data/parameters.json").read_text(encoding="utf-8"))["parameters"]}
baseline = json.loads((APP / "Data/cfg-writer-baseline.json").read_text(encoding="utf-8"))
row = catalog.get(PID)
check(row is not None and row["control"] == "toggle" and row["group"] == "World Loot"
      and row["vanilla"] == {"All": False} and row.get("exposeInUi", True)
      and row["support"]["status"] == "runtime_test_required", "catalog toggle in World Loot, vanilla OFF")
check(baseline["special"].get("looseArtifactsRemoved", {}).get("parameterId") == PID,
      "baseline special.looseArtifactsRemoved")
check(not any(p["parameterId"] == PID for p in baseline["editPoints"]), "no direct edit points")

manifest = list(csv.DictReader((APP / "Data/loose-artifact-spawner-manifest.csv").open(encoding="utf-8")))
check(len(manifest) == 5 and sum(r["scope"] == "baseGame" for r in manifest) == 4
      and sum(r["scope"] == "dlc1" for r in manifest) == 1, "manifest 4 base + 1 DLC1")
check({r["spawner_sid"] for r in manifest} == {
    "WeirdKettleArtifactSpawner", "WeirdNutArtifactSpawner", "WeirdFlowerArtifactSpawner",
    "WeirdBallArtifactSpawner", "WaterDripArtifactSpawner"}, "manifest spawners are the fixed unique artifacts")
others = set()
for f in (APP / "Data").glob("*.csv"):
    if f.name != "loose-artifact-spawner-manifest.csv":
        others |= set(re.findall(r"\b[0-9A-F]{32}\b", f.read_text(encoding="utf-8")))
check(not ({r["actor_sid"] for r in manifest} & others), "actors disjoint from every other manifest")

csproj = (APP / "TrueCustomDifficulty.csproj").read_text(encoding="utf-8")
check("Data\\loose-artifact-spawner-manifest.csv" in csproj, "manifest embedded")
writer = (APP / "Services/CfgPatchWriterService.cs").read_text(encoding="utf-8")
part = (APP / "Services/CfgPatchWriterService.LooseArtifactRemovals.cs").read_text(encoding="utf-8")
check("WriteLooseArtifactRemovals(" in writer and "supported.Add(LooseArtifactsRemovedId);" in writer,
      "main writer calls it and counts it supported")
check("rows.Count != 5" in part and ": removenode" in part and "IsScopeApplicable" in part,
      "writer: inventory guard, removenode, DLC scope")
ui = (APP / "Controls/AggregateVisualControl.cs").read_text(encoding="utf-8")
check('(artifacts, "Loose Artifacts")' in ui, "REMOVE card row")
pvm = (APP / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
check(f'["{PID}"] = "WorldLootLayout"' in pvm, "family map")

for env, rel in (("TCD_BASE_DUMP", "GameData/SpawnActorPrototypes.cfg"),
                 ("TCD_DLC1_DUMP", "DLCGameData/DLC1/SpawnActorPrototypes.cfg")):
    root = os.environ.get(env)
    if not root:
        print(f"[SKIP] {env} not set")
        continue
    text = (Path(root) / rel).read_text(encoding="utf-8", errors="replace")
    for r in (x for x in manifest if x["source_file"] == rel):
        m = re.search(r"^" + r["actor_sid"] + r" : struct\.begin.*?\n(.*?)^struct\.end", text, re.S | re.M)
        body = m.group(1) if m else ""
        check("ESpawnType::ArtifactSpawner" in body and "LevelName = WorldMap_WP" in body
              and "SpawnOnStart = true" in body and f"SpawnedPrototypeSID = {r['spawner_sid']}" in body,
              f"dump actor {r['actor_sid']} {r['spawner_sid']}")
        refs = subprocess.run(["grep", "-rl", r["actor_sid"], root], capture_output=True, text=True).stdout.split()
        check(all(x.endswith("SpawnActorPrototypes.cfg") for x in refs), f"no quest reference {r['actor_sid']}")

if failures:
    print(f"\n{len(failures)} failure(s)")
    sys.exit(1)
print("\nREMOVE LOOSE ARTIFACTS contract OK")
