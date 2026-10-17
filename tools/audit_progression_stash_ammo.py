#!/usr/bin/env python3
"""Audit sibling ammo categories of the 14 allowlisted world weapon recipes.

This tool does not infer caliber compatibility or generate patches.
"""
import argparse
import csv
import re
from collections import defaultdict
from pathlib import Path

DATA = Path(__file__).resolve().parents[1] / "src/TrueCustomDifficulty/Data"
OPEN = re.compile(r"^\s*([^:]+?)\s*:\s*struct\.begin\b")
CLOSE = re.compile(r"^\s*struct\.end\s*$")
SID = re.compile(r"^\s*ItemPrototypeSID\s*=\s*(\S+)", re.M)

def read_manifest():
    with (DATA / "world-weapon-shuffle-manifest.csv").open(encoding="utf-8-sig", newline="") as f:
        rows = list(csv.DictReader(f))
    if len(rows) != 14:
        raise ValueError(f"Expected 14 slots, found {len(rows)}")
    return rows

def parse(lines):
    stack = []
    blocks = defaultdict(list)
    for i, line in enumerate(lines):
        match = OPEN.match(line)
        if match:
            stack.append((match.group(1).strip(), i))
        elif CLOSE.match(line):
            if not stack:
                raise ValueError(f"Unmatched struct.end at {i+1}")
            path = tuple(s for s, _ in stack)
            _, start = stack.pop()
            blocks[path].append((start, i+1))
    if stack:
        raise ValueError("Unclosed CFG block")
    return blocks

def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--cfg", required=True, type=Path)
    ap.add_argument("--strict", action="store_true", help="Fail when any generator has unclassified sibling category")
    args = ap.parse_args()
    rows = read_manifest()
    lines = args.cfg.read_text(encoding="utf-8-sig").splitlines()
    blocks = parse(lines)
    missing = []
    uncertain = []
    confirmed = 0
    for row in sorted(rows, key=lambda r: r["generator_sid"]):
        gen = row["generator_sid"]
        weapon = (gen, *row["node_path"].split("/"))
        matches = blocks.get(weapon, [])
        if len(matches) != 1:
            missing.append((gen, row["node_path"], len(matches)))
            continue
        start, end = matches[0]
        sids = SID.findall("\n".join(lines[start:end]))
        if sids != [row["item_sid"]]:
            raise ValueError(f"Weapon source mismatch: {gen} {sids}")
        # Categories at ItemGenerator/[n] across this generator, not just the weapon.
        category_paths = sorted(p for p in blocks if len(p) == 3 and p[0] == gen and p[1] == "ItemGenerator")
        ammo = []
        other = []
        unknown = []
        for p in category_paths:
            for s, e in blocks[p]:
                body = "\n".join(lines[s:e])
                category = re.search(r"^\s*(?:Category|ItemCategory)\s*=\s*(\S+)", body, re.M)
                label = category.group(1) if category else "UNKNOWN"
                item_sids = SID.findall(body)
                if label in {"Ammo", "EItemGeneratorCategory::Ammo"}:
                    ammo.extend(item_sids)
                elif label not in {"WeaponPrimary", "WeaponPistol", "EItemGeneratorCategory::WeaponPrimary", "EItemGeneratorCategory::WeaponPistol"}:
                    other.append(label)
                if label == "UNKNOWN":
                    unknown.append(p)
        print(f"{gen},weapon={row['item_sid']},tier={row['progression_tier']},ammo_sids={';'.join(ammo) or 'NONE'},other_categories={';'.join(other) or 'NONE'}")
        if other or not ammo or unknown:
            uncertain.append(gen)
        else:
            confirmed += 1
    if missing:
        raise ValueError(f"Unresolved weapon source blocks: {missing}")
    print(f"[AUDIT] {len(rows)} weapon slots inspected; {confirmed} category-only candidates; {len(uncertain)} generators require manual review")
    if args.strict and uncertain:
        raise ValueError(f"Unclassified/absent sibling ammo in {len(uncertain)} generators; no patch is safe")

if __name__ == "__main__":
    main()
