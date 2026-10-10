#!/usr/bin/env python3
"""Verify the nine unique world pickups against original GameData/DLC1 CFG ZIPs.

This is a direct CFG topology/QuestNode literal-reference audit; it cannot
prove runtime Unreal pickup/quest behavior. No generated files are written.
"""
from __future__ import annotations

import argparse
import csv
from pathlib import Path
import sys
from zipfile import ZipFile


ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / "src/TrueCustomDifficulty/Data/loose-unique-weapon-shuffle-manifest.csv"
REGULAR = ROOT / "src/TrueCustomDifficulty/Data/world-weapon-direct-manifest.csv"
EXPECTED = {
    "09EEF8794F23BF1D373B6CA1CCC6F8DF": "Gun_Tank_MG",
    "13DF06544F7D4401F5B042A01A6BD1E4": "Gun_S15_AR",
    "61AF1E49404F5E1620F6EE86F5C5F644": "GunArevPrecise_AR",
    "665FA23B4FC7DE9B6C61BF9497B1C9A5": "Gun_SOFMOD_AR",
    "7ADA44EA4C856FDA87DE298DBF5C7656": "Gun_GStreet_HG",
    "90A7F3344C766CF84BE73F83A98F7026": "Gun_Texas_SG",
    "BEFFC269430B8A7C7CFE8EB2BDC5574A": "GunNightStalker_HG",
    "CB061545481621331D8258B730D562E1": "Gun_Cavalier_SR",
    "CBE52EF14A9E37B1699E58A51F766F76": "Gun_RatKiller_SMG",
}


def fail(message: str) -> None:
    raise ValueError(message)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", type=Path, required=True, help="STALKER2-GameData-CFG ZIP")
    parser.add_argument("--dlc", type=Path, required=True, help="STALKER2-DLC1-CFG ZIP")
    args = parser.parse_args()

    with MANIFEST.open(encoding="utf-8", newline="") as source:
        rows = list(csv.DictReader(source))
    found = {row["actorSid"]: row["itemSid"] for row in rows}
    if len(rows) != 9 or found != EXPECTED:
        fail("Source-locked nine-item manifest cardinality or item mapping drift")
    with REGULAR.open(encoding="utf-8", newline="") as source:
        regular = {row["actor_sid"] for row in csv.DictReader(source)}
    if regular.intersection(found):
        fail("Unique weapons overlap generic regular weapon pool")

    with ZipFile(args.base) as archive:
        actors = archive.read("GameData/SpawnActorPrototypes.cfg").decode("utf-8-sig")
        quest_base = archive.read("GameData/QuestNodePrototypes.cfg")
    with ZipFile(args.dlc) as archive:
        quest_dlc = archive.read("DLCGameData/DLC1/QuestNodePrototypes.cfg")

    for row in rows:
        sid = row["actorSid"]
        key = sid + " : struct.begin"
        start = actors.find(key)
        if start < 0 or actors.find(key, start + len(key)) >= 0:
            fail(f"Actor missing or duplicated in source: {sid}")
        end = actors.find("struct.end", start)
        if end < 0:
            fail(f"Unclosed actor source: {sid}")
        chunk = actors[start:end].splitlines()
        fields = {}
        for line in chunk[1:]:
            if "=" in line:
                name, value = line.strip().split("=", 1)
                fields[name.strip()] = value.strip()
        expected = {
            "SID": sid,
            "LevelName": row["levelName"],
            "SpawnOnStart": "true",
            "SpawnType": "ESpawnType::Item",
            "SpawnedPrototypeSID": row["itemSid"],
            "ItemSID": row["itemSid"],
            "MeshPath": row["meshPath"],
            "Durability": row["durability"],
            "DisablePhysicsAndCollision": row["disablePhysicsAndCollision"],
        }
        for field, value in expected.items():
            if fields.get(field) != value:
                fail(f"Original CFG drift: {sid} {field}: {fields.get(field)!r} != {value!r}")
        if row["scope"] != "baseGame" or row["sourceFile"] != "GameData/SpawnActorPrototypes.cfg":
            fail(f"Scope/source drift: {sid}")
        for token in (sid, row["itemSid"]):
            if token.encode() in quest_base or token.encode() in quest_dlc:
                fail(f"Direct QuestNode literal reference: {token}")
    print("PASS: 9/9 original actor identities, meshes, conditions, quest-reference gate")
    print("PASS: disjoint from regular world weapon shuffle")
    print("NOT RUN: actual Unreal BPatches, physics/collision and scripted pickup gameplay")
    return 0


if __name__ == "__main__":
    try:
        sys.exit(main())
    except (ValueError, KeyError, OSError) as exc:
        print(f"FAIL: {exc}", file=sys.stderr)
        sys.exit(1)
