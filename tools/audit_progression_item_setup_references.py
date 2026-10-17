#!/usr/bin/env python3
"""Audit explicit item->weapon-setup references in extracted ItemPrototypes CFG."""
import argparse
import csv
import re
from pathlib import Path
from collections import defaultdict

DATA = Path(__file__).resolve().parents[1] / "src/TrueCustomDifficulty/Data"
OPEN = re.compile(r'^\s*([^:]+?)\s*:\s*struct\.begin\b')
CLOSE = re.compile(r'^\s*struct\.end\s*$')
PROPERTY = re.compile(r'^\s*([A-Za-z][A-Za-z0-9_]*)\s*=\s*(.*?)\s*(?://.*)?$')
# Deliberately require exact field names, never infer by naming similarity.
SETUP_FIELDS = ("WeaponGeneralSetupSID", "WeaponGeneralSetupPrototypeSID", "WeaponSetupSID", "WeaponSetupPrototypeSID")

def blocks(path):
    lines = path.read_text(encoding="utf-8-sig").splitlines()
    stack = []
    for i, line in enumerate(lines):
        m = OPEN.match(line)
        if m:
            stack.append((m.group(1).strip(), i))
        elif CLOSE.match(line):
            if not stack:
                raise ValueError(f"Unmatched struct.end at {i+1}")
            sid, start = stack.pop()
            if not stack:
                yield sid, lines[start:i+1]
    if stack:
        raise ValueError("Unclosed CFG")

def direct(block):
    depth = 0
    props = defaultdict(list)
    for line in block[1:-1]:
        if OPEN.match(line):
            depth += 1
        elif CLOSE.match(line):
            depth -= 1
        elif depth == 0:
            m = PROPERTY.match(line)
            if m:
                props[m.group(1)].append(m.group(2).strip())
    return props

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--item-cfg", type=Path, required=True)
    p.add_argument("--weapon-cfg", type=Path, required=True)
    p.add_argument("--output", type=Path)
    p.add_argument("--strict", action="store_true")
    args = p.parse_args()
    with (DATA / "world-weapon-shuffle-manifest.csv").open(newline="", encoding="utf-8-sig") as f:
        slots = list(csv.DictReader(f))
    if len(slots) != 14:
        raise ValueError(f"Expected 14 allowlisted slots, got {len(slots)}")
    wanted = {s["item_sid"] for s in slots}
    setups = {sid for sid, _ in blocks(args.weapon_cfg)}
    items = defaultdict(list)
    for sid, body in blocks(args.item_cfg):
        if sid in wanted:
            items[sid].append(direct(body))
    rows = []
    for item in sorted(wanted):
        records = items.get(item, [])
        fields = [(key, value) for props in records for key in SETUP_FIELDS for value in props.get(key, [])]
        if len(records) != 1:
            status = "MISSING_OR_DUPLICATE_ITEM"
        elif len(fields) != 1:
            status = "NO_UNIQUE_DIRECT_SETUP_REFERENCE"
        elif fields[0][1] not in setups:
            status = "REFERENCED_SETUP_NOT_FOUND"
        else:
            status = "EXACT_DIRECT_REFERENCE"
        rows.append({"item_sid":item,
                     "weapon_setup_sid":fields[0][1] if len(fields)==1 else "",
                     "evidence_source":f"ItemPrototypes.cfg:{fields[0][0]}" if status=="EXACT_DIRECT_REFERENCE" else "",
                     "status":status})
    if args.strict and any(r["status"]!="EXACT_DIRECT_REFERENCE" for r in rows):
        raise ValueError("Not all item->setup references are directly proven; refusing strict output")
    fields = ["item_sid","weapon_setup_sid","evidence_source","status"]
    if args.output:
        args.output.parent.mkdir(parents=True, exist_ok=True)
        f = args.output.open("w", newline="", encoding="utf-8")
    else:
        import sys
        f = sys.stdout
    try:
        w = csv.DictWriter(f, fieldnames=fields)
        w.writeheader()
        w.writerows(rows)
    finally:
        if args.output:
            f.close()
    print(f"[AUDIT] {len(rows)} unique item SIDs; {sum(r['status']=='EXACT_DIRECT_REFERENCE' for r in rows)} exact direct setup references", flush=True)

if __name__ == "__main__":
    main()
