#!/usr/bin/env python3
"""Army Medkit coverage contract (2026-11-17).

Every vanilla Army Medkit source the player meets in loot is visible and
editable:
* stashes: Rare Easy-only list, SmartLoot medicine (SMART column), direct
  entries -> ARMY MEDKITS odds / MIN / MAX (existing writers);
* breakable medical boxes -> ARMY MEDKIT MIX (loot.medicalBoxArmyMedkitWeight),
  three direct "scale" points on the ArmyMedkit Weight.
Optional TCD_BASE_DUMP re-checks the box Weights against the source.
"""
import json
import os
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src" / "TrueCustomDifficulty"
PID = "loot.medicalBoxArmyMedkitWeight"
EXPECTED = {"DestructibleStash_Medkit_Experienced": ("20", "4", "1"),
            "DestructibleStash_Medkit_Veteran": ("20", "10", "5"),
            "DestructibleStash_Medkit_Master": ("10", "10", "10")}
failures = []


def check(ok, label):
    print(("[PASS] " if ok else "[FAIL] ") + label)
    if not ok:
        failures.append(label)


catalog = {p["id"]: p for p in json.loads((APP / "Data/parameters.json").read_text(encoding="utf-8"))["parameters"]}
baseline = json.loads((APP / "Data/cfg-writer-baseline.json").read_text(encoding="utf-8"))
row = catalog.get(PID)
check(row is not None and row["group"] == "Breakable Containers" and row["vanilla"] == {"All": 1}
      and row["slider"] == {"min": 0, "max": 3, "step": 0.25, "stepMode": "absolute"}
      and row.get("exposeInUi") is False, "catalog: hidden AmmoDistribution member, 0-3x / 0.25, vanilla 1")
points = [p for p in baseline["editPoints"] if p["parameterId"] == PID]
check(len(points) == 3 and {p["prototype"]: p["sourceRaw"] for p in points}
      == {k: v[1] for k, v in EXPECTED.items()}
      and all(p["operation"] == "scale" and p["key"] == "Weight"
              and p["nodePath"] == ["ItemGenerator", "[0]", "PossibleItems", "[1]"]
              and p["file"] == "GameData/ItemGeneratorPrototypes.cfg" and p["scope"] == "baseGame"
              for p in points), "baseline: 3 scale points on the ArmyMedkit Weight")

policy = (APP / "Services/MedicalBoxPolicy.cs").read_text(encoding="utf-8")
for proto, (medkit, army, eco) in EXPECTED.items():
    check(f'"{proto}", {float(medkit):.1f}, {float(army):.1f}, {float(eco):.1f}' in policy,
          f"policy weights {proto}")
ui = (APP / "Controls/AggregateVisualControl.cs").read_text(encoding="utf-8")
odds = (APP / "Controls/AggregateVisualControl.StashOdds.cs").read_text(encoding="utf-8")
vm = (APP / "ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
pvm = (APP / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
check('DrawMedicalBoxMix(dc, geometry.MedicalBox' in ui and '"ARMY MEDKIT MIX"' in ui
      and '"MEDICAL BOXES"' in ui and "MedicalBoxPolicy.ArmyShare(" in ui, "UI: MEDICAL BOXES knob + per-rank share")
check(f'["{PID}"] = "AmmoDistribution"' in pvm and vm.count(f'"{PID}"') >= 3, "family wiring")
check('StashSmartMedicineOdds(current, sids)' in ui and '"SMART"' in ui
      and 'StashSmartMedicinePrototype = "StashMedicine_Smart"' in odds
      and "group.EffectiveSpawn(state)" in odds, "stash MEDS table: SMART column from writer endpoints")

dump = os.environ.get("TCD_BASE_DUMP")
if dump:
    text = (Path(dump) / "GameData/ItemGeneratorPrototypes.cfg").read_text(encoding="utf-8", errors="replace")
    for proto, want in EXPECTED.items():
        body = re.search(r"^" + proto + r" : struct\.begin.*?\n(.*?)^struct\.end", text, re.S | re.M).group(1)
        got = tuple(re.findall(r"ItemPrototypeSID = (\w+)\s+Weight = (\S+)", body))
        check(got == (("Medkit", want[0]), ("ArmyMedkit", want[1]), ("EcoMedkit", want[2])),
              f"dump {proto} {got}")
else:
    print("[SKIP] TCD_BASE_DUMP not set")

if failures:
    print(f"\n{len(failures)} failure(s)")
    sys.exit(1)
print("\nArmy Medkit coverage contract OK")
