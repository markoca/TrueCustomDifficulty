#!/usr/bin/env python3
"""Regenerate strict ordinary WorldMap stash SmartLoot actor candidates from CFG ZIP.

Produces an audit CSV, not a live game patch; requires only Python 3 stdlib.
"""
import argparse
import collections
import csv
import re
import zipfile
from pathlib import Path

ACTORS = "GameData/SpawnActorPrototypes.cfg"
SMART = "GameData/StashPrototypes.cfg"
TOP_ACTOR = re.compile(rb"(?m)^([0-9A-F]{32}) : struct\.begin")
TOP_SMART = re.compile(rb"(?m)^([^\s]+) : struct\.begin")
SPECIAL = ("Blueprint", "Unique", "Quest")


def source_field(actor, key):
    m = re.search(rb"(?m)^   " + key.encode()
                  + rb"[ \t]*=[ \t]*([^\r\n]*)", actor)
    return m.group(1).decode("utf-8").strip() if m else ""


def inventory(archive):
    with zipfile.ZipFile(archive) as z:
        content = z.read(ACTORS)
        stash = z.read(SMART)
    prototypes = {m.group(1).decode() for m in TOP_SMART.finditer(stash)}
    roots = list(TOP_ACTOR.finditer(content))
    allowed, excluded = [], []
    usage = collections.Counter()
    for i, root in enumerate(roots):
        part = content[root.start():roots[i + 1].start()
                       if i + 1 < len(roots) else len(content)]
        raw = source_field(part, "StashPrototypeSID")
        if not raw or raw == "Empty":
            continue
        sids = [sid.strip() for sid in raw.split(",") if sid.strip()]
        missing = set(sids) - prototypes
        if missing:
            raise ValueError(f"Undefined SmartLoot SID on {root.group(1).decode()}: {missing}")
        if (source_field(part, "LevelName") != "WorldMap_WP"
            or source_field(part, "SpawnType") != "ESpawnType::ItemContainer"):
            continue
        gens = [s.decode().strip() for s in re.findall(
            rb"(?m)^\s+PrototypeSID[ \t]*=[ \t]*([^\r\n]*)", part)]
        row = [
            root.group(1).decode(), raw, ",".join(gens),
            source_field(part, "ClueVariablePrototypeSID"),
            source_field(part, "DLC"),
            source_field(part, "PositionX"),
            source_field(part, "PositionY"),
            source_field(part, "PositionZ"),
        ]
        if any(tag in gen for gen in gens for tag in SPECIAL):
            excluded.append(row)
            continue
        allowed.append(row)
        usage.update(sids)
    return sorted(allowed), sorted(excluded), usage


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("zip", type=Path)
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument("--excluded-output", type=Path)
    args = parser.parse_args()
    approved, rejected, edges = inventory(args.zip)
    header = ["ActorSID", "OriginalStashPrototypeSIDs", "OriginalItemGeneratorSIDs",
              "ClueVariableSID", "DLC", "X", "Y", "Z"]
    for path, rows in ((args.output, approved), (args.excluded_output, rejected)):
        if path is None:
            continue
        path.parent.mkdir(parents=True, exist_ok=True)
        with path.open("w", encoding="utf-8", newline="") as f:
            writer = csv.writer(f, lineterminator="\n")
            writer.writerow(header)
            writer.writerows(rows)
    print("Main-world SmartLoot ItemContainers:", len(approved) + len(rejected))
    print("Ordinary allowlist:", len(approved))
    print("Excluded blueprint/unique/quest-linked actors:", len(rejected))
    for sid, n in edges.most_common():
        print(" ", sid, n)
    print("No game patch written; actor-level output requires runtime verification.")


if __name__ == "__main__":
    main()
