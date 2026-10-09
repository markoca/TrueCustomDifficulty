#!/usr/bin/env python3
"""Check the current direct CFG writer inputs against user-extracted CFG ZIPs.

Phase 1 source audit (2026-10-09). This is intentionally strict about
file existence and the complete 14-species / 16-prototype Mutant Health
MaxHP surface; it does NOT claim full runtime validation for the remaining
1,208 direct edit points or any special/manifests writer.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src" / "TrueCustomDifficulty" / "Data"
HP_PREFIX = "combat.mutantHealth."
EXPECTED_HEALTH = {
    "combat.mutantHealth.bloodsucker": {"Bloodsucker": "500", "MistBloodsucker": "1000"},
    "combat.mutantHealth.chimera": {"Chimera": "1400"},
    "combat.mutantHealth.deer": {"Deer": "600"},
    "combat.mutantHealth.pseudogiant": {"Pseudogiant": "2500"},
    "combat.mutantHealth.pseudodog": {"PseudoDog": "300"},
    "combat.mutantHealth.burer": {"Burer": "400", "Burer_ShootingSpecial": "400"},
    "combat.mutantHealth.bayun": {"Bayun": "400"},
    "combat.mutantHealth.controller": {"Controller": "500"},
    "combat.mutantHealth.blinddog": {"Blinddog": "63"},
    "combat.mutantHealth.boar": {"Boar": "300"},
    "combat.mutantHealth.flesh": {"Flesh": "280"},
    "combat.mutantHealth.poltergeist": {"Poltergeist": "400"},
    "combat.mutantHealth.snork": {"Snork": "150"},
    "combat.mutantHealth.tushkan": {"Tushkan": "19"},
}


def die(message: str) -> None:
    raise SystemExit("[FAIL] " + message)


def find_max_hp(text: str, prototype: str) -> str:
    # CFG top-level prototypes use unindented NAME : struct.begin and finish
    # at the next unindented struct.end. VitalParams.MaxHP is indented.
    starts = list(re.finditer(
        r"^" + re.escape(prototype) + r"\s*:\s*struct\.begin[^\n]*\n",
        text,
        re.MULTILINE,
    ))
    if len(starts) != 1:
        die(f"{prototype}: expected exactly one top-level prototype, found {len(starts)}")
    end = text.find("\nstruct.end", starts[0].end())
    if end < 0:
        die(f"{prototype}: missing top-level struct.end")
    body = text[starts[0].end():end]
    hp = re.findall(r"(?m)^\s+MaxHP\s*=\s*(\S+)", body)
    if len(hp) != 1:
        die(f"{prototype}: expected one VitalParams.MaxHP, got {hp}")
    return hp[0]


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-zip", type=Path, required=True,
                        help="STALKER2-GameData-CFG.zip (user's current game dump)")
    parser.add_argument("--dlc1-zip", type=Path, required=True,
                        help="STALKER2-DLC1-CFG.zip (matching DLC1 dump)")
    args = parser.parse_args()

    catalog = json.loads((DATA / "parameters.json").read_text(encoding="utf-8"))
    baseline = json.loads((DATA / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
    catalog_ids = {p["id"] for p in catalog["parameters"]}
    if baseline["supportedParameterCount"] != len(catalog_ids):
        die("writer supportedParameterCount does not match live catalog")
    if len(catalog_ids) != len(catalog["parameters"]):
        die("duplicate catalog parameter IDs")

    points = baseline["editPoints"]
    hp_points = [p for p in points if p["parameterId"].startswith(HP_PREFIX)]
    if len(hp_points) != 16:
        die(f"expected sixteen real mutant MaxHP edit points; found {len(hp_points)}")

    with zipfile.ZipFile(args.base_zip) as base_zip, zipfile.ZipFile(args.dlc1_zip) as dlc1_zip:
        source_archives = {"baseGame": base_zip, "dlc1": dlc1_zip}
        # All direct writer input files MUST be physically present in the
        # appropriate archive. This is file-level proof, not key-level proof.
        source_files = {(p["scope"], p["file"]) for p in points}
        for scope, filename in sorted(source_files):
            archive = source_archives.get(scope)
            if archive is None:
                die(f"unknown direct input scope: {scope}")
            if filename not in archive.namelist():
                die(f"missing direct writer input in {scope} archive: {filename}")

        text = base_zip.read("GameData/ObjPrototypes.cfg").decode("utf-8-sig")
        actual: dict[str, dict[str, str]] = {}
        for point in hp_points:
            pid = point["parameterId"]
            if (point["scope"], point["file"], point["key"], point["operation"]) != (
                "baseGame", "GameData/ObjPrototypes.cfg", "MaxHP", "scale"
            ):
                die(f"{pid}: incorrect direct MaxHP writer mapping")
            if point.get("nodePath") != ["VitalParams"]:
                die(f"{pid}: expected nested VitalParams target")
            prototype = point["prototype"]
            if prototype in actual.setdefault(pid, {}):
                die(f"{pid}: duplicate edit target for {prototype}")
            raw = find_max_hp(text, prototype)
            if raw != point["sourceRaw"]:
                die(f"{pid}/{prototype}: CFG MaxHP={raw}, baseline sourceRaw={point['sourceRaw']}")
            actual[pid][prototype] = raw

        if actual != EXPECTED_HEALTH:
            die(f"14-mutant health route membership/source values drifted: {actual}")
        for p in catalog["parameters"]:
            if p["id"] not in EXPECTED_HEALTH:
                continue
            targets = p.get("targets") or []
            written = {x["prototype"] for x in targets
                       if x["scope"] == "baseGame"
                       and x["file"] == "GameData/ObjPrototypes.cfg"
                       and x["path"] == "VitalParams.MaxHP"
                       and x["operation"] == "scale"}
            if written != set(EXPECTED_HEALTH[p["id"]]):
                die(f"{p['id']}: catalog targets differ from concrete baseline")

    print(f"[OK] direct writer input files present in correct archives: {len(source_files)}")
    print(f"[OK] mutant-health families in active catalog: {len(EXPECTED_HEALTH)}")
    print(f"[OK] exact source-verified MaxHP edit points: {len(hp_points)}")
    print(f"[OK] writer baseline supported count: {baseline['supportedParameterCount']}")
    print("PASS — source provenance (file existence + Mutant Health MaxHP)")
    print("NOTE — other direct fields, special writers and runtime effects are NOT yet validated.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
