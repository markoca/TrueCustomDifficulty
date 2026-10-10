#!/usr/bin/env python3
"""Compare actual Combat CFG source values and special arrays against live writer inventory.

Requires decoded matching base/DLC1 ZIPs. All 64 Combat source routes must
exist directly or through an EXPLICIT {refkey=...} ancestor. The Master PSY override effects must match the writer manifest. This is source evidence, NOT a UE runtime/BPatch merge test.
"""
from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import re
import sys
import zipfile

from audit_cfg_source_values import check_point, read_tree, resolve_refkey_inheritance

ROOT = Path(__file__).resolve().parents[1]
BASELINE = ROOT / "src/TrueCustomDifficulty/Data/cfg-writer-baseline.json"


def child(node, name):
    found = [x for x in node.children if x.name == name]
    if len(found) != 1:
        raise ValueError(f"{node.name}.{name}: expected one struct, found {len(found)}")
    return found[0]


def inspect(base_zip, dlc_zip):
    baseline = json.loads(BASELINE.read_text(encoding="utf-8"))
    points = [p for p in baseline["editPoints"] if p["parameterId"].startswith("combat.")]
    if len(points) != 64:
        raise ValueError(f"expected 64 Combat source edit points, got {len(points)}")
    if any(p["scope"] != "baseGame" for p in points):
        raise ValueError("Combat source unexpectedly moved into DLC1; recheck original provenance")
    with zipfile.ZipFile(base_zip) as base, zipfile.ZipFile(dlc_zip) as dlc:
        # Input sanity only. All Combat direct source points belong to baseGame.
        if "DLCGameData/DLC1/ItemPrototypes.cfg" not in dlc.namelist():
            raise ValueError("DLC1 ZIP does not contain a decoded DLC1 CFG directory")
        sources = {}
        for filename in {p["file"] for p in points}:
            if filename not in base.namelist():
                raise ValueError(f"missing original base-game CFG file: {filename}")
            sources[filename] = base.read(filename).decode("utf-8-sig")

    trees = {filename: read_tree(content) for filename, content in sources.items()}
    results = []
    for point in points:
        direct = check_point(point, trees[point["file"]])
        original_lines = sources[point["file"]].splitlines()
        results.append(resolve_refkey_inheritance(point, trees[point["file"]],
                                                  original_lines, direct))
    failed = [r for r in results if r["status"] not in {"exact", "inherited_exact"}]
    roots = trees["GameData/DifficultyPrototypes.cfg"]
    master = [n for n in roots if n.name == "Stalker"]
    if len(master) != 1:
        raise ValueError("Master original CFG must have exactly one Stalker prototype")
    master = master[0]
    psy = child(child(master, "NPCCombatDifficulty"), "PsyPhantomNPCOverrides")
    psy_records = []
    if len(psy.children) != 2:
        raise ValueError("Master PSY source must contain exactly two records")
    for sid, spec in baseline["special"]["psyOverrides"].items():
        record = child(psy, spec["index"])
        expected = {
            "OverrideEffectPrototypeSID": sid,
            "PSYDelayBeforeNewSpawnCycleModifier": "0.5",
            "PsyPhantomNPCCountMultiplier": "2.0",
            "bPsyNPCApplyBleeding": "true",
            "PsyNPCDurabilityDamageMultiplier": "2.0",
        }
        if record.values != expected:
            raise ValueError(f"Master PSY effect {sid}: original values differ: {record.values}")
        psy_records.append({"effectSid": sid, "index": spec["index"], "status": "exact"})

    hashes = {p.name: hashlib.sha256(p.read_bytes()).hexdigest()
              for p in (base_zip, dlc_zip)}
    return {
        "sourceArchivesSha256": hashes,
        "sourceScope": "all 64 Combat routes are baseGame; DLC1 archive identity only",
        "directPointCount": len(results),
        "directStatuses": dict(sorted(Counter(p["status"] for p in results).items())),
        "directFailures": failed,
        "directRoutes": results,
        "masterPsyEffects": psy_records,
        "masterPsyEffectCount": len(psy_records),
        "runtimeCaveat": (
            "Medium has no local MutantCombatDifficulty section; explicit {refkey=Empty} "
            "provides its two source values. Whether a nested {bpatch} update "
            "merges correctly into this inherited section remains an in-game test."
        ),
        "interpretation": (
            "Original source and manifest parity only. Does not prove UE BPatch merging, "
            "in-game behavior, mod load order, or non-Combat writer values."
        ),
    }


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--base-zip", type=Path, required=True)
    p.add_argument("--dlc1-zip", type=Path, required=True)
    p.add_argument("--out", type=Path)
    a = p.parse_args()
    report = inspect(a.base_zip, a.dlc1_zip)
    if a.out:
        a.out.parent.mkdir(parents=True, exist_ok=True)
        a.out.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n",
                         encoding="utf-8")
    print(f"Combat source: {report['directPointCount']} direct routes {report['directStatuses']}")
    print(f"Master specials: {report['masterPsyEffectCount']} PSY override effects")
    for result in report["directRoutes"]:
        if result["status"] != "exact":
            print("  SOURCE INHERITANCE/FINDING", result["parameterId"],
                  result["prototype"], result["status"],
                  result.get("refkeyChain"), result.get("inheritedRaw"))
    print("NOT VERIFIED: in-game application of inherited Medium BPatch and special arrays")
    if report["directFailures"]:
        print("FAIL: original Combat source mismatch", file=sys.stderr)
        return 1
    print("PASS: original Combat CFG source provenance only")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (ValueError, KeyError, zipfile.BadZipFile) as exc:
        print("FAIL:", exc, file=sys.stderr)
        sys.exit(1)
