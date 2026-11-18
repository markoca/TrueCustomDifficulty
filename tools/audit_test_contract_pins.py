#!/usr/bin/env python3
"""Static gate: catalog-wide counts pinned in the Windows C# contracts match the catalog.

The C# contracts only run on Windows / Wine; this keeps their catalog totals
and per-domain inventories from silently drifting while they are not run.
"""
import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src/TrueCustomDifficulty"
P = json.loads((APP / "Data/parameters.json").read_text(encoding="utf-8"))["parameters"]
E = json.loads((APP / "Data/cfg-writer-baseline.json").read_text(encoding="utf-8"))["editPoints"]
ids = [p["id"] for p in P]
pref = lambda s: sum(i.startswith(s) for i in ids)
pts = lambda s: sum(e["parameterId"].startswith(s) for e in E)

EXPECT = {
    "tests/ALifePatchContract/Program.cs": [f"catalogIds.Count == {pref('alife.')}",
                                            f"preset.Values.Count == {len(ids)} && Catalog.Parameters.Count == {len(ids)}",
                                            f"all.Length == {pts('alife.')}"],
    "tests/LootSpecialGeneratorsContract/GenericSupplyReduction.cs": [f"Catalog.Parameters.Count == {len(ids)}"],
    "tests/AllControlsPatchContract/Program.cs": [f"ids.Count == {len(ids)}"],
    "tests/EconomyPatchContract/Program.cs": [f"defs.Length == {pref('economy.')}", f"points.Length == {pts('economy.')}"],
    "tests/EnvironmentPatchContract/Program.cs": [f"ids.Length == {pref('environment.')}", f"points.Length == {pts('environment.')}"],
    "tests/EquipmentPatchContract/Program.cs": [f"points.Length == {pts('equipment.')}"],
    "tests/HealthStaminaPatchContract/Program.cs": [f"cohort.Length == {pref('health.') + pref('stamina.')}"],
    "tests/LootDirectPatchContract/Program.cs": [f"catalog.Count == {pref('loot.')}", f"source.Length == {pts('loot.')}",
                                                 f'p.Tab == "loot") == {sum(p["tab"] == "loot" for p in P)}',
                                                 f'p.Tab == "stashes") == {sum(p["tab"] == "stashes" for p in P)}'],
    "tests/LootWorldPlacementContract/Program.cs": [f"WriterBaseline.SupportedParameterCount=={len(ids)}"],
    "tests/SurvivalPatchContract/Program.cs": [f"cohort.Length == {pref('survival.')}", f"points.Length == {pts('survival.')}"],
}
bad = []
for f, needles in EXPECT.items():
    text = (ROOT / f).read_text(encoding="utf-8")
    for n in needles:
        if n not in text:
            bad.append(f"{f}: expected pin '{n}'")
if bad:
    print("\n".join("FAIL " + b for b in bad))
    sys.exit(1)
print(f"PASS C# contract pins: {sum(map(len, EXPECT.values()))} catalog / edit-point counts match ({len(ids)} controls)")
