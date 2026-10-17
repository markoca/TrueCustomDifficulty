#!/usr/bin/env python3
"""Audit direct and explicitly inherited item->weapon setup references."""
import argparse
import csv
import re
import sys
from collections import defaultdict
from pathlib import Path

DATA = Path(__file__).resolve().parents[1] / "src/TrueCustomDifficulty/Data"
OPEN = re.compile(r'^\s*([^:]+?)\s*:\s*struct\.begin\b(.*)$')
CLOSE = re.compile(r'^\s*struct\.end\s*$')
PARENT = re.compile(r'\bref\s*:\s*([A-Za-z0-9_]+)\b')
PROPERTY = re.compile(r'^\s*([A-Za-z][A-Za-z0-9_]*)\s*=\s*(.*?)\s*(?://.*)?$')
SETUP_FIELDS = ("WeaponGeneralSetupSID", "WeaponGeneralSetupPrototypeSID",
                "WeaponSetupSID", "WeaponSetupPrototypeSID")


def blocks(path):
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    stack = []
    for index, line in enumerate(lines):
        match = OPEN.match(line)
        if match:
            refs = PARENT.findall(match.group(2))
            if len(refs) > 1:
                raise ValueError(f"{path}:{index+1}: ambiguous parent")
            stack.append((match.group(1).strip(), index, refs[0] if refs else None))
        elif CLOSE.match(line):
            if not stack:
                raise ValueError(f"{path}:{index+1}: unmatched struct.end")
            sid, start, parent = stack.pop()
            if not stack:
                yield sid, lines[start:index+1], parent
    if stack:
        raise ValueError(f"{path}: unclosed struct")


def direct(block):
    depth = 0
    props = defaultdict(list)
    for line in block[1:-1]:
        if OPEN.match(line):
            depth += 1
        elif CLOSE.match(line):
            depth -= 1
        elif depth == 0:
            match = PROPERTY.match(line)
            if match:
                props[match.group(1)].append(match.group(2).strip())
    return props


def resolve(item, items, setups):
    seen = set()
    chain = []
    current = item
    while current:
        if current in seen:
            return None, "INHERITANCE_CYCLE", chain
        seen.add(current)
        records = items.get(current, [])
        if len(records) != 1:
            return None, "MISSING_OR_DUPLICATE_PROTOTYPE", chain
        props, parent = records[0]
        chain.append(current)
        references = [(key, value) for key in SETUP_FIELDS
                      for value in props.get(key, [])]
        if len(references) > 1:
            return None, "AMBIGUOUS_SETUP_REFERENCE", chain
        if references:
            key, value = references[0]
            if value not in setups:
                return None, "REFERENCED_SETUP_NOT_FOUND", chain
            status = "EXACT_DIRECT_REFERENCE" if len(chain) == 1 else "EXACT_INHERITED_REFERENCE"
            return (key, value), status, chain
        current = parent
    return None, "NO_SETUP_REFERENCE_IN_CHAIN", chain


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--item-cfg", type=Path, required=True)
    parser.add_argument("--weapon-cfg", type=Path, required=True)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--strict", action="store_true")
    args = parser.parse_args()
    with (DATA / "world-weapon-shuffle-manifest.csv").open(newline="", encoding="utf-8-sig") as file:
        slots = list(csv.DictReader(file))
    if len(slots) != 14:
        raise ValueError(f"Expected 14 slots, got {len(slots)}")
    wanted = {row["item_sid"] for row in slots}
    setups = {sid for sid, _, _ in blocks(args.weapon_cfg)}
    items = defaultdict(list)
    for sid, body, parent in blocks(args.item_cfg):
        items[sid].append((direct(body), parent))
    rows = []
    for item in sorted(wanted):
        reference, status, chain = resolve(item, items, setups)
        rows.append({
            "item_sid": item,
            "weapon_setup_sid": reference[1] if reference else "",
            "evidence_source": ("ItemPrototypes.cfg:" + reference[0] + ":" +
                                "->".join(chain)) if reference else "",
            "status": status,
        })
    fields = ["item_sid", "weapon_setup_sid", "evidence_source", "status"]
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        with args.output.open("w", newline="", encoding="utf-8") as file:
            writer = csv.DictWriter(file, fieldnames=fields)
            writer.writeheader()
            writer.writerows(rows)
    else:
        writer = csv.DictWriter(sys.stdout, fieldnames=fields)
        writer.writeheader()
        writer.writerows(rows)
    proven = sum(row["status"] in {"EXACT_DIRECT_REFERENCE", "EXACT_INHERITED_REFERENCE"} for row in rows)
    print(f"[AUDIT] {len(rows)} unique item SIDs; {proven} proven direct/inherited references", file=sys.stderr)
    if args.strict and proven != len(rows):
        raise ValueError("Unresolved setup references; strict audit failed")


if __name__ == "__main__":
    main()
