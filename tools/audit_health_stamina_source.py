#!/usr/bin/env python3
"""Validate 69 Health/Stamina direct CFG source routes against original ZIPs.

Provenance-only: exactly 68 directly authored fields plus one value inherited
through Player : struct.begin {refkey=[0]}. Not a UE runtime/BPatch test.
"""
from __future__ import annotations

import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import sys
import zipfile

from audit_cfg_source_values import (
    read_tree, check_point, resolve_refkey_inheritance,
)

ROOT = Path(__file__).resolve().parents[1]
BASELINE = ROOT / "src/TrueCustomDifficulty/Data/cfg-writer-baseline.json"


def run(base_zip: Path, dlc_zip: Path) -> dict:
    baseline = json.loads(BASELINE.read_text(encoding="utf-8"))
    points = [
        p for p in baseline["editPoints"]
        if p["parameterId"].startswith(("health.", "stamina."))
    ]
    by_domain = Counter(p["parameterId"].split(".")[0] for p in points)
    if dict(by_domain) != {"health": 43, "stamina": 26}:
        raise ValueError(f"Health/Stamina direct inventory drifted: {dict(by_domain)}")
    if any(p["scope"] != "baseGame" for p in points):
        raise ValueError("Health/Stamina direct CFG source scope drifted from baseGame")
    vault = [p for p in points if p["parameterId"] == "stamina.vaultMaxHeight"]
    if len(vault) != 1 or vault[0]["prototype"] != "Player" or (
        vault[0]["nodePath"], vault[0]["key"], vault[0]["sourceRaw"]
    ) != (["VaultingParams"], "MaxObstacleHeight", "130"):
        raise ValueError("VaultingParams original-source contract drifted")

    with zipfile.ZipFile(base_zip) as base, zipfile.ZipFile(dlc_zip) as dlc:
        # Ensure both real original archives were supplied; all these
        # particular direct fields are in the base-game ZIP.
        if "DLCGameData/DLC1/ItemPrototypes.cfg" not in dlc.namelist():
            raise ValueError("DLC1 archive lacks DLCGameData/DLC1/ItemPrototypes.cfg")
        source_files = sorted({p["file"] for p in points})
        missing_files = sorted(set(source_files) - set(base.namelist()))
        if missing_files:
            raise ValueError(f"original GameData CFG files missing: {missing_files}")
        text = {name: base.read(name).decode("utf-8-sig") for name in source_files}

    originals = {path: read_tree(payload) for path, payload in text.items()}
    results = []
    for point in points:
        original = originals[point["file"]]
        direct = check_point(point, original)
        record = resolve_refkey_inheritance(
            point, original, text[point["file"]].splitlines(), direct
        )
        results.append(record)

    totals = Counter(record["status"] for record in results)
    failures = [r for r in results
                if r["status"] not in {"exact", "inherited_exact"}]
    vault_result = [r for r in results if r["parameterId"] == "stamina.vaultMaxHeight"]
    if len(vault_result) != 1 or vault_result[0]["status"] != "inherited_exact" or (
        vault_result[0].get("inheritedFrom"), vault_result[0].get("refkeyChain")
    ) != ("[0]", ["[0]"]):
        failures.append({
            "parameterId": "stamina.vaultMaxHeight",
            "reason": "Player VaultingParams must be physically traced to [0]"
        })
    if totals.get("exact") != 68 or totals.get("inherited_exact") != 1:
        failures.append({
            "reason": "Expected 68 direct authored matches + 1 inherited exact",
            "observedStatuses": dict(totals)
        })

    def sha256(path: Path) -> str:
        with path.open("rb") as fd:
            return hashlib.file_digest(fd, "sha256").hexdigest()

    return {
        "scope": "Health + Stamina original direct CFG source parity",
        "archiveSHA256": {
            "baseGame": sha256(base_zip),
            "dlc1": sha256(dlc_zip),
        },
        "total": len(points),
        "byDomain": dict(by_domain),
        "statusCounts": dict(sorted(totals.items())),
        "findings": failures,
        "sourceRoutes": results,
        "runtimeNotVerified": [
            "Player.VaultingParams nested BPatch into refkey [0]",
            "Actual in-game changes to regeneration, healing and stamina",
            "External repak/retoc packaging and game installation"
        ],
        "interpretation": "Source ancestry provenance only; not a game runtime pass."
    }


def main() -> int:
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--base-zip", required=True, type=Path)
    p.add_argument("--dlc1-zip", required=True, type=Path)
    p.add_argument("--out", type=Path)
    args = p.parse_args()
    try:
        report = run(args.base_zip, args.dlc1_zip)
    except (ValueError, KeyError, OSError, zipfile.BadZipFile) as exc:
        print("FAIL: invalid Health/Stamina original CFG evidence:", exc, file=sys.stderr)
        return 1
    if args.out:
        args.out.parent.mkdir(parents=True, exist_ok=True)
        args.out.write_text(json.dumps(report, indent=2, ensure_ascii=False) + "\n",
                            encoding="utf-8")
    print(f"Original CFG: Health {report['byDomain']['health']} + "
          f"Stamina {report['byDomain']['stamina']} = {report['total']}")
    print("Source statuses:", report["statusCounts"])
    for f in report["findings"][:25]:
        print("  INVESTIGATE", f)
    if report["findings"]:
        print("FAIL: original CFG sources require triage")
        return 1
    print("PASS: 68 original direct matches; VaultingParams inherited from [0]")
    print("NOT RUN: in-game VaultingParams BPatch merge and gameplay acceptance")
    return 0


if __name__ == "__main__":
    sys.exit(main())
