#!/usr/bin/env python3
"""Fail-closed inventory for the 2.1 CFG migration boundary.

Default is an offline repo contract: a PASS certifies inventory consistency,
NOT 2.1 runtime migration. Pass both original outer ZIPs for source deltas.
"""
from __future__ import annotations

import argparse
import json
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src/TrueCustomDifficulty/Data"
EXPECTED = {
    "combat": 56, "survival": 153, "health": 47, "stamina": 26,
    "economy": 409, "equipment": 331, "environment": 42,
    "loot": 320, "alife": 64,
}
SELL_RESOURCE = "TrueCustomDifficulty.Data.trader-sells-2.1-runtime.csv"
CHANGED_DIRECT_FILES = {
    "GameData/CoreVariables.cfg",
    "DLCGameData/DLC1/ItemPrototypes.cfg",
}
NEW_21_FILES = {
    "GameData/FastTravelDestinationPrototypes.cfg",
    "GameData/FastTravelEconomyPrototypes.cfg",
}


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--old", type=Path, help="Original 2.0 outer CFG ZIP")
    ap.add_argument("--new", type=Path, help="Original 2.1 outer CFG ZIP")
    ap.add_argument("--json-out", type=Path)
    args = ap.parse_args()
    if bool(args.old) != bool(args.new):
        ap.error("--old and --new must be given together")

    baseline = json.loads((DATA / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
    catalog = json.loads((DATA / "parameters.json").read_text(encoding="utf-8"))
    points = baseline["editPoints"]
    ids = {p["id"] for p in catalog["parameters"]}
    actual = Counter(p["parameterId"].split(".", 1)[0] for p in points)
    assert dict(actual) == EXPECTED, f"Direct domain census drift: {actual}"
    assert len(points) == 1448 and len(ids) == 359
    assert baseline["supportedParameterCount"] == len(ids)
    assert baseline["gameVersion"] == catalog["source"]["gameVersion"] == "2.0.5", (
        "Reversioning requires fresh 2.1 direct + special source certification, "
        "not just a metadata label edit."
    )
    assert len(baseline["special"]) == 25
    sell = baseline["special"]["traderSells"]
    assert len(sell["parameterIds"]) == 31 and sell["expectedSourceRows"] == 2939
    assert sell["expectedProfiles"] == 86
    assert sell["manifestResource"] == SELL_RESOURCE, "Obsolete 2.0.5 SELL baseline resource"
    project = (ROOT / "src/TrueCustomDifficulty/TrueCustomDifficulty.csproj").read_text()
    assert "trader-sells-2.1-runtime.csv" in project
    assert "trader-offers-source-rows.csv" not in project
    changed = [p for p in points if p["file"] in CHANGED_DIRECT_FILES]
    changed_by_domain = Counter(p["parameterId"].split(".", 1)[0] for p in changed)
    assert len(changed) == 35 and dict(changed_by_domain) == {
        "stamina": 6, "economy": 8, "equipment": 21
    }, f"2.1 changed-file edit points drift: {changed_by_domain}"

    report = {
        "baselineStatus": "NOT_MIGRATED_GLOBAL_VERSION_2.0.5",
        "directRoutes": len(points),
        "unchangedFileRoutes": len(points) - len(changed),
        "changedFileRoutes": len(changed),
        "changedFileDomains": dict(changed_by_domain),
        "directByDomain": dict(actual),
        "specialGroups": len(baseline["special"]),
        "traderSellsStatus": "ACTIVATED_SOURCE_PINNED_2.1",
        "gameplayStatus": "NOT_TESTED",
        "sourceComparison": "NOT_RUN",
    }
    if args.old:
        from audit_game_21_cfg_compat import run
        result = run(args.old, args.new)
        assert (result["old_cfg_files"], result["new_cfg_files"],
                result["changed_file_count"], result["direct_points"],
                result["direct_points_checked_in_changed_files"]) == (
                603, 605, 22, 1448, 35), "2.1 archive or mapping census changed"
        assert set(result["added"]) == NEW_21_FILES and not result["removed"]
        report["sourceComparison"] = "PASS" if result["passed"] else "FAIL"
        report["sourceFindings"] = result["issues"]
        report["manifestActors"] = result["manifest_actors"]
    if args.json_out:
        args.json_out.parent.mkdir(parents=True, exist_ok=True)
        args.json_out.write_text(json.dumps(report, indent=2) + "\n", encoding="utf-8")
    print(json.dumps({k: v for k, v in report.items()
                      if k != "manifestActors"}, indent=2))
    print("PASS: current source inventory; full 2.1 migration is NOT certified"
          if report["sourceComparison"] != "FAIL" else "FAIL: original-source compatibility")
    return 1 if report["sourceComparison"] == "FAIL" else 0


if __name__ == "__main__":
    raise SystemExit(main())
