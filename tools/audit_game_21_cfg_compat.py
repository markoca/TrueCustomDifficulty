#!/usr/bin/env python3
"""Audit TCD CFG writer compatibility against the supplied 2.0 and 2.1 dumps.

    python3 tools/audit_game_21_cfg_compat.py --old "/path/STALKER2-2.0 cfg dump.zip" --new "/path/STALKER2-2.1 cfg dump.zip" --json-out audit/21-compat.json

Both inputs are the original outer ZIPs containing GameData and DLC1 ZIPs.
Read-only: this script does not update any CFG mappings or manifests.
"""
from __future__ import annotations

import argparse
import csv
from decimal import Decimal, InvalidOperation
import io
import json
import re
import sys
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
BASELINE = ROOT / "src/TrueCustomDifficulty/Data/cfg-writer-baseline.json"
DATA = ROOT / "src/TrueCustomDifficulty/Data"
SID = re.compile(r"(?<![A-Za-z0-9])([0-9A-Fa-f]{32})(?![A-Za-z0-9])")
OPEN = re.compile(r"^\s*(.*?)\s*:\s*struct\.begin\b")
ASSIGN = re.compile(r"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$")
WATCH = ("StashPrototypeSID", "ItemGeneratorSettings",
         "EnableSmartLootIfPossible", "SpawnedPrototypeSID")


def inner_archives(path: Path):
    with zipfile.ZipFile(path) as outer:
        result = {}
        for entry in outer.namelist():
            if not entry.endswith(".zip"):
                continue
            z = zipfile.ZipFile(io.BytesIO(outer.read(entry)))
            for name in z.namelist():
                if name.endswith(".cfg"):
                    if name in result:
                        raise ValueError("Duplicate CFG: " + name)
                    result[name] = (z, name)
        if not result:
            raise ValueError("No inner CFG archives in: " + str(path))
        return result


def cfg_lines(src):
    z, name = src
    with z.open(name) as stream:
        for raw in stream:
            yield raw.decode("utf-8-sig", errors="replace").rstrip("\r\n")


def cfg_assignments(src):
    """Yield (prototype, nested nodes, key, source value) without losing array indices."""
    stack = []
    for line in cfg_lines(src):
        match = OPEN.match(line)
        if match:
            stack.append(match.group(1).strip())
            continue
        if line.strip().startswith("struct.end"):
            if stack:
                stack.pop()
            continue
        match = ASSIGN.match(line)
        if match and stack:
            yield stack[0], tuple(stack[1:]), match.group(1), match.group(2).strip()


def comparable(actual):
    value = actual.strip()
    if value.endswith("f") and len(value) > 1:
        value = value[:-1]
    try:
        return Decimal(value)
    except InvalidOperation:
        return value


def actor_tokens():
    owners = {}
    for path in sorted(DATA.glob("*.csv")):
        with path.open(encoding="utf-8-sig", newline="") as handle:
            for row in csv.reader(handle):
                for field in row:
                    for found in SID.findall(field):
                        owners.setdefault(found.upper(), set()).add(path.name)
    return owners


def actor_watch(src, wanted):
    """Index actual top-level actor roots and inspect only manifest-owned SIDs.

    SpawnActorPrototypes has deeply nested structs. A flat regex stopping at the
    first struct.end silently misses actor roots and can incorrectly PASS with
    zero matched actors. The root SID is a 32-hex token in column zero.
    """
    if not wanted:
        return {}
    z, name = src
    data = z.read(name)
    pattern = re.compile(rb"(?m)^([0-9A-Fa-f]{32})\s*:\s*struct\.begin[^\r\n]*")
    matches = list(pattern.finditer(data))
    actors = {}
    for i, match in enumerate(matches):
        sid = match.group(1).decode("ascii").upper()
        if sid not in wanted:
            continue
        stop = matches[i + 1].start() if i + 1 < len(matches) else len(data)
        body = data[match.end():stop].decode("utf-8", errors="replace")
        actors[sid] = tuple(
            line.strip() for line in body.splitlines()
            if any(word in line for word in WATCH)
        )
    return actors


def run(old_path: Path, new_path: Path):
    old = inner_archives(old_path)
    new = inner_archives(new_path)
    baseline = json.loads(BASELINE.read_text(encoding="utf-8"))
    points = baseline["editPoints"]
    changed = {
        name for name in old.keys() & new.keys()
        if old[name][0].getinfo(name).CRC != new[name][0].getinfo(name).CRC
        or old[name][0].getinfo(name).file_size != new[name][0].getinfo(name).file_size
    }
    removed = sorted(old.keys() - new.keys())
    added = sorted(new.keys() - old.keys())
    issues = []
    checked = 0

    # Unchanged CFG files cannot contain newly changed writer leaves.
    # For changed CFGs, compare each exact prototype/node/key and baseline sourceRaw.
    for filename in sorted({p["file"] for p in points} & changed):
        keys = {
            (p["prototype"], tuple(p["nodePath"]), p["key"])
            for p in points if p["file"] == filename
        }
        old_values = {
            key: value
            for prototype, path, name, value in cfg_assignments(old[filename])
            if (key := (prototype, path, name)) in keys
        }
        new_values = {
            key: value
            for prototype, path, name, value in cfg_assignments(new[filename])
            if (key := (prototype, path, name)) in keys
        }
        for point in (p for p in points if p["file"] == filename):
            key = (point["prototype"], tuple(point["nodePath"]), point["key"])
            before, after = old_values.get(key), new_values.get(key)
            checked += 1
            if before is None or after is None:
                issues.append(f"MISSING LEAF {filename}: {key}: old={before}, new={after}")
            elif comparable(before) != comparable(after):
                issues.append(f"CHANGED LEAF {filename}: {key}: {before} -> {after}")
            elif comparable(after) != comparable(str(point["sourceRaw"])):
                issues.append(f"BASELINE MISMATCH {filename}: {key}: "
                              f"2.1={after}, baseline={point['sourceRaw']}")
    for filename in sorted({p["file"] for p in points} & set(removed)):
        issues.append("REMOVED SOURCE FILE: " + filename)

    # Roaming guides (new in 2.1) are not the legacy paid hub-guide routes.
    roaming = []
    economy = "GameData/FastTravelEconomyPrototypes.cfg"
    if economy in new:
        roaming = [
            (root, key, value)
            for root, path, key, value in cfg_assignments(new[economy])
            if root == "[1]" and not path and key in ("SID", "GeneralCoefficient")
        ]
    if ("[1]", "SID", "ALifeGuideEconomy") not in roaming or (
        "[1]", "GeneralCoefficient", "1.3"
    ) not in roaming:
        issues.append("Unexpected 2.1 ALifeGuideEconomy source: " + repr(roaming))

    # Check the ACTUAL production CSV inventories, not guessed actor IDs.
    owners = actor_tokens()
    if not owners:
        issues.append("Actor manifest inventory is EMPTY: cannot audit stash/loot compatibility")
    actor_stats = {}
    for filename in (
        "GameData/SpawnActorPrototypes.cfg",
        "DLCGameData/DLC1/SpawnActorPrototypes.cfg",
    ):
        if filename not in old or filename not in new:
            issues.append("Missing actor CFG: " + filename)
            continue
        a = actor_watch(old[filename], owners)
        b = actor_watch(new[filename], owners)
        lost = sorted(a.keys() - b.keys())
        touched = sorted(k for k in a.keys() & b.keys() if a[k] != b[k])
        actor_stats[filename] = {
            "manifest_actor_roots_old": len(a),
            "manifest_actor_roots_new": len(b),
            "removed": lost,
            "changed_watched_fields": [
                {"actor": sid, "manifests": sorted(owners[sid]),
                 "before": a[sid], "after": b[sid]} for sid in touched
            ],
        }
        if not a or not b:
            issues.append(f"{filename}: ZERO matched manifest actors; audit evidence is invalid")
        if lost:
            issues.append(f"{filename}: {len(lost)} manifest actors REMOVED")
        if touched:
            issues.append(f"{filename}: {len(touched)} manifest actor(s) "
                          "have changed source/loot fields")

    return {
        "baseline_game_version": baseline["gameVersion"],
        "old_cfg_files": len(old), "new_cfg_files": len(new),
        "added": added, "removed": removed, "changed_file_count": len(changed),
        "direct_points": len(points),
        "direct_points_checked_in_changed_files": checked,
        "roaming_guide_economy": roaming,
        "manifest_actors": actor_stats, "issues": issues,
        "passed": not issues,
    }


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--old", type=Path, required=True)
    parser.add_argument("--new", type=Path, required=True)
    parser.add_argument("--json-out", type=Path)
    args = parser.parse_args()
    result = run(args.old, args.new)
    if args.json_out:
        args.json_out.parent.mkdir(parents=True, exist_ok=True)
        args.json_out.write_text(
            json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
        )
    print(json.dumps(
        {key: value for key, value in result.items() if key != "manifest_actors"},
        ensure_ascii=False, indent=2,
    ))
    for filename, record in result["manifest_actors"].items():
        print(f"{filename}: {record['manifest_actor_roots_old']} -> "
              f"{record['manifest_actor_roots_new']} manifest actor roots; "
              f"{len(record['removed'])} removed, "
              f"{len(record['changed_watched_fields'])} changed loot fields")
    return 0 if result["passed"] else 1


if __name__ == "__main__":
    sys.exit(main())
