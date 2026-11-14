#!/usr/bin/env python3
"""Classify all 211 newly added S.T.A.L.K.E.R. 2.1 SpawnActor roots.

Read-only relative to game archives and project manifests. Only original
2.0 and 2.1 OUTER ZIPs are accepted. This checks source identity/coverage,
not game runtime behavior or whether new locations should join TCD pools.
"""
from __future__ import annotations
import argparse
import csv
import hashlib
import io
import re
import zipfile
from collections import Counter
from pathlib import Path

FILES = {
    "base": ("STALKER2-GameData-CFG.zip", "GameData/SpawnActorPrototypes.cfg"),
    "dlc1": ("STALKER2-DLC1-CFG.zip", "DLCGameData/DLC1/SpawnActorPrototypes.cfg"),
}
NEW_SOURCE_SHA = {
    "base": "3c6e753a385a36e0296ea967488fba35a1b0d935538c07526e4767867757ff99",
    "dlc1": "9ef3226a0f9926749a216214f1e4077694a50098b16b1896b804802e040e53bc",
}
NEW_ID_SHA = {
    "base": "fc61e1e7d716b3049c5bad20009e2f63e83ff19f7e40f155e66dcedf3c5d2348",
    "dlc1": "acaa99fb51120a90b3bbaf412c0764466bdff65a8de1b920be4e15b63789325c",
}
ROOTS = re.compile(rb"(?m)^([0-9A-Fa-f]{32})\s*:\s*struct\.begin[^\r\n]*")
FIELD = re.compile(r"^ {3}(?! )([A-Za-z0-9_]+)[ \t]*=[ \t]*(.*)$", re.MULTILINE)
EXPECTED = {
    "base": {
        "ESpawnType::FastTravelSecondaryPoint": 132,
        "ESpawnType::FastTravelPoint": 63,
        "ESpawnType::Trigger": 3,
        "ESpawnType::Obj": 3,
        "ESpawnType::ContextualAction": 2,
        "ESpawnType::ElectroAnomaly": 1,
        "ESpawnType::Squad": 1,
    },
    "dlc1": {
        "ESpawnType::Trigger": 3,
        "ESpawnType::ContextualAction": 2,
        "ESpawnType::FastTravelPoint": 1,
    },
}
COLUMNS = (
    "scope", "actor_sid", "classification", "spawn_type", "level_name",
    "spawned_prototype", "smartloot_flag", "stash_prototype", "item_generator",
)


def raw_cfg(outer_path: Path, scope: str) -> bytes:
    archive_name, cfg_name = FILES[scope]
    with zipfile.ZipFile(outer_path) as outer:
        with zipfile.ZipFile(io.BytesIO(outer.read(archive_name))) as inner:
            return inner.read(cfg_name)


def source_actor_blocks(data: bytes, selected: set[str] | None = None):
    positions = list(ROOTS.finditer(data))
    if not positions:
        raise ValueError("Missing original SpawnActor roots")
    seen = set()
    for i, match in enumerate(positions):
        sid = match.group(1).decode("ascii").upper()
        if sid in seen:
            raise ValueError("Duplicate SpawnActor SID: " + sid)
        seen.add(sid)
        if selected is not None and sid not in selected:
            continue
        stop = positions[i + 1].start() if i + 1 < len(positions) else len(data)
        body = data[match.end():stop].decode("utf-8-sig", errors="replace")
        yield sid, dict(FIELD.findall(body))


def classify(scope: str, sid: str, fields: dict[str, str]) -> dict[str, str]:
    kind = fields.get("SpawnType", "")
    level = fields.get("LevelName", "")
    proto = fields.get("SpawnedPrototypeSID", "")
    stash = fields.get("StashPrototypeSID", "")
    generator = fields.get("ItemGeneratorSettings", "")
    smartloot = fields.get("EnableSmartLootIfPossible", "")
    # Do not automatically include story NPCs even when SmartLoot is true.
    if stash or generator:
        raise ValueError("New 2.1 actor has an unreviewed stash/generator: " + sid)
    if kind in ("ESpawnType::FastTravelPoint", "ESpawnType::FastTravelSecondaryPoint"):
        if level != "WorldMap_WP" or proto or smartloot:
            raise ValueError("Unexpected travel actor contents: " + sid)
        category = "FAST_TRAVEL_ONLY"
    else:
        if kind == "ESpawnType::Obj" and proto != "Snork":
            raise ValueError("Unexpected new quest object: " + sid)
        if kind == "ESpawnType::ElectroAnomaly" and proto != "ElectroAnomaly":
            raise ValueError("Unexpected new anomaly: " + sid)
        if kind == "ESpawnType::Squad" and proto:
            raise ValueError("Unexpected new squad prototype: " + sid)
        if kind in ("ESpawnType::Trigger", "ESpawnType::ContextualAction") and proto:
            raise ValueError("Unexpected trigger/action prototype: " + sid)
        if kind not in EXPECTED[scope] or level == "":
            raise ValueError("Unknown newly added actor type or level: " + sid)
        category = "QUEST_OR_NON_LOOT_REVIEW"
        if kind in ("ESpawnType::Obj", "ESpawnType::Squad"):
            if scope != "base" or level != "WorldMap_WP/E11_MQ01_LogicLevel_WP" or smartloot != "true":
                raise ValueError("Quest NPC SmartLoot boundary changed: " + sid)
        elif smartloot:
            raise ValueError("Unexpected non-NPC SmartLoot: " + sid)
    return dict(scope=scope, actor_sid=sid, classification=category,
                spawn_type=kind, level_name=level, spawned_prototype=proto,
                smartloot_flag=smartloot, stash_prototype=stash,
                item_generator=generator)


def audit(old: Path, new: Path):
    records = []
    for scope in FILES:
        old_data = raw_cfg(old, scope)
        new_data = raw_cfg(new, scope)
        if hashlib.sha256(new_data).hexdigest() != NEW_SOURCE_SHA[scope]:
            raise ValueError("The supplied 2.1 actor CFG source has changed: " + scope)
        old_ids = {sid for sid, _ in source_actor_blocks(old_data)}
        new_ids = {sid for sid, _ in source_actor_blocks(new_data)}
        added = new_ids - old_ids
        expected_added = 205 if scope == "base" else 6
        if len(added) != expected_added:
            raise ValueError("Unexpected number of newly added 2.1 actors: " + scope)
        signature = hashlib.sha256(("\n".join(sorted(added)) + "\n").encode()).hexdigest()
        if signature != NEW_ID_SHA[scope]:
            raise ValueError("New actor SID census changed: " + scope)
        rows = [classify(scope, sid, fields) for sid, fields in
                source_actor_blocks(new_data, added)]
        kinds = Counter(x["spawn_type"] for x in rows)
        if kinds != Counter(EXPECTED[scope]):
            raise ValueError("2.1 new actor type census drifted: " + repr(kinds))
        records.extend(rows)
    categories = Counter(r["classification"] for r in records)
    if categories != {"FAST_TRAVEL_ONLY": 196, "QUEST_OR_NON_LOOT_REVIEW": 15}:
        raise ValueError("New 2.1 actor classification changed")
    if sum(r["smartloot_flag"] == "true" for r in records) != 4:
        raise ValueError("2.1 quest SmartLoot flags changed")
    if len(records) != len({(r["scope"], r["actor_sid"]) for r in records}) != 0:
        # Keep the bound explicit and prevent duplicate source coordinates.
        if len(records) != 211:
            raise ValueError("Unexpected classified actor count")
    else:
        raise ValueError("Duplicate or empty classified actors")
    return sorted(records, key=lambda x: (x["scope"], x["actor_sid"]))


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--old", required=True, type=Path)
    p.add_argument("--new", required=True, type=Path)
    p.add_argument("--out", type=Path)
    args = p.parse_args()
    result = audit(args.old, args.new)
    if args.out:
        args.out.parent.mkdir(parents=True, exist_ok=True)
        with args.out.open("w", encoding="utf-8", newline="") as f:
            writer = csv.DictWriter(f, fieldnames=COLUMNS)
            writer.writeheader()
            writer.writerows(result)
    print("PASS: 211 source-pinned 2.1 actors: 196 travel, 15 quest/non-loot")
    print("PASS: 0 new stash/generator sources; 4 quest-bound SmartLoot NPC/Squad actors")
    print("NOT VERIFIED: gameplay Fast Travel behavior or new item-generator eligibility")


if __name__ == "__main__":
    main()
