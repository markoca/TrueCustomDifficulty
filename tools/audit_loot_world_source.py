#!/usr/bin/env python3
"""Check every physical World Loot manifest actor and placement against original decoded CFG ZIPs.

Streaming selected-root parser: original SpawnActorPrototypes is ~179 MB.
Checks actual authoring, not just the writer's embedded CSV counts. False
positives are findings, never silently accepted. Does not run Unreal/gameplay.
"""
from __future__ import annotations
import argparse
from collections import Counter, defaultdict
import csv
import hashlib
import io
import json
from pathlib import Path
import sys
import zipfile

from audit_cfg_source_values import START, END, CfgNode, read_tree

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / "src" / "TrueCustomDifficulty" / "Data"
BASE_ACTORS = "GameData/SpawnActorPrototypes.cfg"
DLC_ACTORS = "DLCGameData/DLC1/SpawnActorPrototypes.cfg"
ITEM_GENERATORS = "GameData/ItemGeneratorPrototypes.cfg"


def csv_rows(name: str) -> list[dict[str, str]]:
    with (DATA / name).open(newline="", encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


def zip_sha(path: Path) -> str:
    h = hashlib.sha256()
    with path.open("rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest()


def find_child(parent: CfgNode, path: list[str]) -> CfgNode:
    node = parent
    for name in path:
        matches = [c for c in node.children if c.name == name]
        if len(matches) != 1:
            raise ValueError("missing/ambiguous nested node " + "/".join(path))
        node = matches[0]
    return node


def all_values(root: CfgNode, key: str):
    result = [root.values[key]] if key in root.values else []
    for c in root.children:
        result.extend(all_values(c, key))
    return result


def scan_roots(archive: zipfile.ZipFile, member: str,
               wanted: set[str]) -> dict[str, CfgNode]:
    if member not in archive.namelist():
        raise ValueError("Original decoded archive missing " + member)
    found: dict[str, CfgNode] = {}
    depth = 0
    capture = False
    root_name = ""
    collected: list[str] = []
    with io.TextIOWrapper(archive.open(member), encoding="utf-8-sig", errors="strict") as file:
        for line in file:
            start = START.match(line)
            close = END.match(line)
            if depth == 0:
                if not start:
                    continue
                depth = 1
                root_name = start.group(1).strip()
                capture = root_name in wanted
                if capture:
                    collected = [line]
                continue
            if start:
                depth += 1
            elif close:
                depth -= 1
            if capture:
                collected.append(line)
                if depth == 0:
                    parsed = read_tree("".join(collected))
                    if len(parsed) != 1 or parsed[0].name != root_name or root_name in found:
                        raise ValueError("Original root identity duplicate/ambiguous " + root_name)
                    found[root_name] = parsed[0]
                    capture = False
                    collected = []
    if depth:
        raise ValueError("Incomplete original cfg file: " + member)
    return found


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base-zip", type=Path, required=True)
    parser.add_argument("--dlc1-zip", type=Path, required=True)
    parser.add_argument("--out", type=Path)
    args = parser.parse_args()
    # All expected IDs originate from repository manifests, but values
    # are verified independently against the actual decoded ZIP contents.
    request: dict[str, dict[str, list[tuple[str, str, tuple[str, ...], str, str]]]] = defaultdict(
        lambda: defaultdict(list))
    # tuple: domain, property, nested path, expected value, provenance.
    count = Counter()
    findings = []
    def add(file: str, sid: str, domain: str, prop: str,
            expected: str, path=(), provenance=""):
        if not sid or not expected:
            findings.append({"domain": domain, "error": "blank source SID/value",
                             "file": file, "sid": sid, "property": prop})
            return
        request[file][sid].append((domain, prop, tuple(path), expected, provenance))
        count[domain] += 1

    def actor_file(scope: str) -> str:
        return DLC_ACTORS if scope.lower() in ("dlc1", "dlc") else BASE_ACTORS

    # All physical actors: category-accurate source identity and generator
    # membership, not merely the presence of the actor SID in original data.
    crate = csv_rows("crate-spawn-production-manifest.csv")
    loose = csv_rows("loose-world-loot-manifest.csv")
    f1 = csv_rows("world-f1-grenade-box-manifest.csv")
    w_direct = csv_rows("world-weapon-direct-manifest.csv")
    a_direct = csv_rows("world-attachment-direct-manifest.csv")
    d_direct = csv_rows("world-detector-direct-manifest.csv")
    b_rows = csv_rows("blueprint-shuffle-manifest.csv")
    w_generators = csv_rows("world-weapon-shuffle-manifest.csv")
    a_generators = csv_rows("world-attachment-generator-manifest.csv")
    d_stash = csv_rows("world-detector-stash-manifest.csv")
    expected_size = dict(crate=3391, loose=7233, f1=101, weapon_direct=362,
                         attachment_direct=7, detector_direct=7,
                         blueprint=77, weapon_generator=14,
                         attachment_generator=60, detector_stash=1)
    actual_size = dict(crate=len(crate), loose=len(loose), f1=len(f1),
                       weapon_direct=len(w_direct), attachment_direct=len(a_direct),
                       detector_direct=len(d_direct), blueprint=len(b_rows),
                       weapon_generator=len(w_generators),
                       attachment_generator=len(a_generators), detector_stash=len(d_stash))
    if actual_size != expected_size:
        findings.append({"domain": "manifest", "error": "source inventory drift",
                         "expected": expected_size, "actual": actual_size})

    for row in crate:
        file = actor_file(row["scope"])
        sid = row["actorSid"]
        add(file, sid, "crate", "SID", sid)
        add(file, sid, "crate", "LevelName", row["levelName"])
        add(file, sid, "crate", "SpawnedPrototypeSID", row["spawnedPrototypeSid"])
        # Any category-specific physical actor generator assignment
        # must identify the generator family, not a quest/reward override.
        add(file, sid, "crate", "PrototypeSID*", row["generatorFamily"])
    for row in loose:
        file = actor_file(row["scope"])
        sid = row["actorSid"]
        add(file, sid, "loose", "SID", sid)
        add(file, sid, "loose", "LevelName", row["levelName"])
        if row["kind"] == "dynamic":
            add(file, sid, "loose", "SpawnType", "ESpawnType::PackOfItems")
            add(file, sid, "loose", "PackOfItemsPrototypeSID", row["itemOrPackSid"])
        else:
            add(file, sid, "loose", "ItemSID", row["itemOrPackSid"])
    for row in f1:
        file = actor_file(row["scope"])
        sid = row["actor_sid"]
        add(file, sid, "f1", "SID", sid)
        add(file, sid, "f1", "LevelName", row["level_name"])
        add(file, sid, "f1", "SpawnedPrototypeSID", row["spawned_prototype_sid"])
        add(file, sid, "f1", "PrototypeSID*", row["generator_sid"])
    for row in w_direct:
        file = row["source_file"]
        sid = row["actor_sid"]
        add(file, sid, "weapon_direct", "SID", sid)
        add(file, sid, "weapon_direct", "LevelName", row["level_name"])
        add(file, sid, "weapon_direct", "SpawnedPrototypeSID", row["item_sid"])
        add(file, sid, "weapon_direct", "ItemSID", row["item_sid"])
    for row in a_direct:
        file = row["source_file"]
        sid = row["actor_sid"]
        add(file, sid, "attachment_direct", "SID", sid)
        add(file, sid, "attachment_direct", "LevelName", row["level_name"])
        add(file, sid, "attachment_direct", "SpawnedPrototypeSID", row["item_sid"])
        add(file, sid, "attachment_direct", "ItemSID", row["item_sid"])
    for row in d_direct:
        file = row["source_file"]
        sid = row["actor_sid"]
        add(file, sid, "detector_direct", "SID", sid)
        add(file, sid, "detector_direct", "SpawnedPrototypeSID", row["item_sid"])
        add(file, sid, "detector_direct", "ItemSID", row["item_sid"])

    for row in b_rows:
        add(ITEM_GENERATORS, row["generator_sid"], "blueprint",
            "ItemPrototypeSID", row["item_sid"], row["node_path"].split("/"))
        # Verify every world placement actor used by the blueprint source
        # exists, but don't assume it has a direct generator reference; some
        # use a wrapper via StashPrototypeSID.
        sid = row["world_source_id"]
        file = row["source_file"]
        add(file, sid, "blueprint_actor", "SID", sid)
    for row in w_generators:
        sid = row["generator_sid"]
        base_path = tuple(row["node_path"].split("/"))
        add(ITEM_GENERATORS, sid, "weapon_generator", "ItemPrototypeSID",
            row["item_sid"], base_path)
        for field, csv_col in (("Chance", "chance"), ("Weight", "weight"),
                                ("MinDurability", "mindurability"),
                                ("MaxDurability", "maxdurability"),
                                ("AmmoMinCount", "ammomincount"),
                                ("AmmoMaxCount", "ammomaxcount"),
                                ("MinCount", "mincount"), ("MaxCount", "maxcount")):
            if row[csv_col]:
                add(ITEM_GENERATORS, sid, "weapon_generator", field,
                    row[csv_col], base_path)
    for row in a_generators:
        sid = row["actor_sid"]
        file = row["actor_file"]
        add(file, sid, "attachment_generator_actor", "SID", sid)
        for path in row["redirect_node_paths"].split(";"):
            add(file, sid, "attachment_generator_actor", "PrototypeSID",
                row["generator_sid"], path.split("/"))
        add(ITEM_GENERATORS, row["generator_sid"], "attachment_generator",
            "ItemPrototypeSID", row["item_sid"], row["entry_node_path"].split("/"))
    for row in d_stash:
        add(row["file"], row["generator_sid"], "detector_stash",
            "ItemPrototypeSID", row["item_sid"], row["node_path"].split("/"))

    source_meta = []
    scanned = Counter()
    status = Counter()
    archives = {"base": zipfile.ZipFile(args.base_zip), "dlc1": zipfile.ZipFile(args.dlc1_zip)}
    try:
        for member, roots in sorted(request.items()):
            archive = archives["dlc1" if member.startswith("DLCGameData/") else "base"]
            info = archive.getinfo(member) if member in archive.namelist() else None
            if info:
                source_meta.append({"member": member, "bytes": info.file_size, "crc32": f"{info.CRC:08x}",
                                    "physicalPrototypesRequested": len(roots)})
                extracted = scan_roots(archive, member, set(roots))
            else:
                extracted = {}
            for sid, checks in roots.items():
                root = extracted.get(sid)
                if root is None:
                    findings.append({"file": member, "prototype": sid, "error": "missing physical original prototype",
                                     "tests": len(checks)})
                    status["missing_prototype"] += len(checks)
                    continue
                for domain, prop, nested, expected, origin in checks:
                    try:
                        node = find_child(root, list(nested))
                        actual = (all_values(node, "PrototypeSID") if prop == "PrototypeSID*"
                                  else [node.values.get(prop, "")])
                        if expected not in actual:
                            raise ValueError("expected " + expected + ", got " + str(actual))
                        status["exact"] += 1
                    except ValueError as exc:
                        findings.append({"file": member, "prototype": sid,
                                         "domain": domain, "path": "/".join(nested),
                                         "property": prop, "error": str(exc)})
                        status["mismatch"] += 1
                    scanned[domain] += 1
    finally:
        for archive in archives.values():
            archive.close()

    report = {"phase": "14B2 physical World Loot original source",
              "status": "PASS" if not findings else "FAIL",
              "originalZipSha256": {"base": zip_sha(args.base_zip),
                                    "dlc1": zip_sha(args.dlc1_zip)},
              "originalSourceMembers": source_meta,
              "manifestRowCounts": actual_size,
              "comparedOriginalFields": dict(status),
              "checkedByDomain": dict(scanned),
              "findings": findings,
              "limits": ["Audits original physical actor and ItemGenerator source metadata, not game runtime",
                         "World tier/pool classification and random selection eligibility still require BPatch contract + manual runtime",
                         "Original archive versions must match the production CSV manifests"]}
    if args.out:
        args.out.parent.mkdir(parents=True, exist_ok=True)
        args.out.write_text(json.dumps(report, ensure_ascii=False, indent=2)+"\n", encoding="utf-8")
    print("World Loot original CFG:", report["status"], "physical checks:", dict(status),
          "manifest rows:", sum(actual_size.values()))
    for item in findings[:20]:
        print("  INVESTIGATE:", json.dumps(item, ensure_ascii=False))
    return int(bool(findings))


if __name__ == "__main__":
    sys.exit(main())
