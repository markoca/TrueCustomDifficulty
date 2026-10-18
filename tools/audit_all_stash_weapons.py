#!/usr/bin/env python3
"""Rebuild and verify exact world ItemContainer weapon-source inventory.

Requires the original extracted base and DLC1 CFG archives. This finds literal
ItemPrototypeSID weapon-category references reachable from physical stash
ItemGeneratorSettings, following nested SubItemGenerators and cross-archive DLC
references. It does NOT prove runtime drops or resolve third-party generators.
"""
import argparse
import csv
import re
import zipfile
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "src/TrueCustomDifficulty/Data/stash-weapon-source-inventory.csv"
PATHS = {
    "base": ("GameData/ItemGeneratorPrototypes.cfg", "GameData/SpawnActorPrototypes.cfg"),
    "dlc1": ("DLCGameData/DLC1/ItemGeneratorPrototypes.cfg",
             "DLCGameData/DLC1/SpawnActorPrototypes.cfg"),
}
FIELDS = ("origin", "actor_sid", "level", "clue", "generator_sid",
          "category", "category_index", "item_index", "item_sid")
TOP = re.compile(r"(?m)^([^\s]+) : struct\.begin[^\r\n]*")
GROUP = re.compile(r"(?m)^      \[(\d+)\] : struct\.begin[^\r\n]*")
ITEM = re.compile(r"(?m)^            \[(\d+)\] : struct\.begin[^\r\n]*")
WEAPONS = {"WeaponPrimary", "WeaponPistol", "WeaponSecondary"}


def top_roots(source):
    starts = list(TOP.finditer(source))
    return {m.group(1): source[m.end():starts[i + 1].start()
            if i + 1 < len(starts) else len(source)]
            for i, m in enumerate(starts)}


def inner_generator(root):
    start = re.search(r"(?m)^   ItemGenerator : struct\.begin[^\r\n]*$", root)
    if not start:
        return ""
    depth, lines = 1, []
    for line in root[start.end():].splitlines(True):
        depth += bool(re.search(r":\s*struct\.begin\b", line))
        depth -= bool(re.search(r"^\s*struct\.end\s*$", line))
        if depth == 0:
            break
        lines.append(line)
    if depth != 0:
        raise ValueError("Malformed nested ItemGenerator CFG")
    return "".join(lines)


def generator_entries(text):
    source = inner_generator(text)
    found, follow = [], set()
    groups = list(GROUP.finditer(source))
    for gi, g in enumerate(groups):
        group = source[g.end():groups[gi + 1].start()
                       if gi + 1 < len(groups) else len(source)]
        typ = re.search(r"(?m)^\s*Category = EItemGenerationCategory::(\w+)", group)
        kind = typ.group(1) if typ else ""
        items = list(ITEM.finditer(group))
        for ii, item in enumerate(items):
            body = group[item.end():items[ii + 1].start()
                         if ii + 1 < len(items) else len(group)]
            sub = re.search(r"(?m)^\s{15}ItemGeneratorPrototypeSID\s*=\s*(\S+)", body)
            sid = re.search(r"(?m)^\s{15}ItemPrototypeSID\s*=\s*(\S+)", body)
            if kind == "SubItemGenerator" and sub and sub.group(1) != "empty":
                follow.add(sub.group(1))
            if kind in WEAPONS and sid and sid.group(1) != "empty":
                found.append((kind, g.group(1), item.group(1), sid.group(1)))
    return found, follow


def source_tables(path, source_paths):
    with zipfile.ZipFile(path) as archive:
        for name in source_paths:
            if name not in archive.namelist():
                raise ValueError("Original CFG ZIP missing: " + name)
        return [top_roots(archive.read(name).decode("utf-8-sig"))
                for name in source_paths]


def scan_actors(origin, actors, generators, definitions):
    results = []
    unknown = set()
    for actor, text in actors.items():
        if not re.fullmatch(r"[0-9A-F]{32}", actor):
            continue
        if not re.search(r"(?m)^\s*SpawnType = ESpawnType::ItemContainer\s*$", text):
            continue
        level = re.search(r"(?m)^\s*LevelName\s*=\s*(\S+)", text)
        if not level or not level.group(1).startswith("WorldMap_WP"):
            continue
        clue = re.search(r"(?m)^\s*ClueVariablePrototypeSID\s*=\s*(\S+)", text)
        if "   ItemGeneratorSettings : struct.begin" not in text:
            continue
        refs = re.findall(r"(?m)^\s{15}PrototypeSID\s*=\s*(\S+)",
                          text.split("   ItemGeneratorSettings : struct.begin", 1)[1])
        visited, pending = set(), list(refs)
        while pending:
            ref = pending.pop()
            if ref in visited or ref == "empty":
                continue
            visited.add(ref)
            if ref not in generators:
                unknown.add(ref)
                continue
            found, nested = definitions[ref]
            for cat, group, item, sid in found:
                results.append((origin, actor, level.group(1),
                                clue.group(1) if clue else "", ref,
                                cat, group, item, sid))
            pending.extend(nested)
    return results, unknown


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--game-zip", required=True, type=Path)
    parser.add_argument("--dlc-zip", required=True, type=Path)
    args = parser.parse_args()

    base, base_actors = source_tables(args.game_zip, PATHS["base"])
    dlc, dlc_actors = source_tables(args.dlc_zip, PATHS["dlc1"])
    if set(base) & set(dlc) != {"[0]"}:
        raise ValueError("Unexpected cross-archive generator name collision")
    generators = dict(base)
    generators.update(dlc)
    definitions = {sid: generator_entries(text) for sid, text in generators.items()}
    detected = []
    for origin, actors in (("base", base_actors), ("dlc1", dlc_actors)):
        items, unknown = scan_actors(origin, actors, generators, definitions)
        detected.extend(items)
        print(f"{origin}: {len(items)} authored weapon entries;"
              f" unresolved external refs: {len(unknown)}")
        if unknown:
            print("  Deferred external refs:", ", ".join(sorted(unknown)[:12]))
    with CATALOG.open(encoding="utf-8", newline="") as f:
        inventory = list(csv.DictReader(f))
    expected = [tuple(x[col] for col in FIELDS) for x in inventory]
    if len(expected) != len(set(expected)) or len(detected) != len(set(detected)):
        raise ValueError("Duplicate source entry identity")
    added = set(detected) - set(expected)
    lost = set(expected) - set(detected)
    counts = Counter(x["status"] for x in inventory)
    required = {"REMOVE_REGULAR": 13, "KEEP_QUEST": 16,
                "KEEP_UNIQUE_REVIEW": 1, "KEEP_PROTECTED": 1,
                "KEEP_DLC_SPECIAL_REVIEW": 8}
    if counts != required or len(expected) != 39 or added or lost:
        print("ADDED:", *sorted(added), sep="\n")
        print("MISSING:", *sorted(lost), sep="\n")
        raise SystemExit("FAIL: source or approved/protected classifications drifted")
    for item in inventory:
        if item["status"] == "REMOVE_REGULAR" and not (
            item["origin"] == "base" and item["level"] == "WorldMap_WP"
            and item["clue"] == "EmptyInherited" and item["category"] in WEAPONS):
            raise SystemExit("FAIL: quest/DLC/unclassified item marked removable")
    print("PASS: 39 source weapon entries, 13 removable, 26 protected or deferred")
    print("Scope: CFG-declared references; not a claim of guaranteed in-game loot")


if __name__ == "__main__":
    main()
