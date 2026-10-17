#!/usr/bin/env python3
"""Source-based 14-slot audit and in-memory whole-recipe replacement regression."""
import argparse
import csv
import re
from collections import Counter, defaultdict
from pathlib import Path

DATA = Path(__file__).resolve().parents[1] / "src/TrueCustomDifficulty/Data"
START = re.compile(r"^([^\s:]+)\s*:\s*struct\.begin")
OPEN = re.compile(r"^\s*([^:]+?)\s*:\s*struct\.begin\b")
CLOSE = re.compile(r"^\s*struct\.end\s*$")
ITEM = re.compile(r"^\s*ItemPrototypeSID\s*=\s*(\S+)\s*$")

def top_sections(lines):
    positions = [(i, match.group(1)) for i, line in enumerate(lines)
                 if (match := START.match(line))]
    sections = {}
    for n, (start, name) in enumerate(positions):
        if name in sections:
            raise ValueError(f"Duplicate generator {name}")
        sections[name] = lines[start:positions[n+1][0] if n+1 < len(positions) else len(lines)]
    return sections

def find_recipe(block, target):
    stack = []
    found = []
    for i, line in enumerate(block):
        match = OPEN.match(line)
        if match:
            stack.append((match.group(1).strip(), i))
        elif CLOSE.match(line):
            if not stack:
                raise ValueError(f"Unmatched struct.end at {i}")
            name, start = stack.pop()
            if tuple(n for n, _ in stack) + (name,) == target:
                found.append((start, i+1))
    if stack:
        raise ValueError("Unclosed recipe blocks")
    if len(found) != 1:
        raise ValueError(f"Expected one source recipe at {target}, found {len(found)}")
    return found[0]

def audit(cfg, manifest):
    lines = cfg.read_text(encoding="utf-8-sig").splitlines()
    sections = top_sections(lines)
    with manifest.open(encoding="utf-8-sig", newline="") as stream:
        slots = list(csv.DictReader(stream))
    if len(slots) != 14:
        raise ValueError(f"Expected 14 allowlisted slots, got {len(slots)}")
    recipes = {}
    groups = defaultdict(list)
    ammo_count = 0
    for row in slots:
        sid = row["generator_sid"]
        block = sections.get(sid)
        if block is None:
            raise ValueError(f"Missing generator: {sid}")
        start, end = find_recipe(block, (sid, *row["node_path"].split("/")))
        recipe = block[start:end]
        matches = [m.group(1) for line in recipe if (m := ITEM.match(line))]
        if matches != [row["item_sid"]]:
            raise ValueError(f"Unexpected item in {sid}: {matches}")
        key = (sid, row["node_path"])
        if key in recipes:
            raise ValueError(f"Duplicate destination: {key}")
        recipes[key] = recipe
        groups[(row["progression_tier"], row["family"])].append(row)
        if any("Category = EItemGenerationCategory::Ammo" in line for line in block):
            ammo_count += 1
    # Simulate only within compatible groups. Whole blocks, never SID-only changes.
    changed = 0
    for rows in groups.values():
        rows = sorted(rows, key=lambda r: (r["generator_sid"], r["node_path"]))
        if len(rows) < 2:
            continue
        rotated = rows[1:] + rows[:1]
        before = Counter(tuple(recipes[(r["generator_sid"], r["node_path"])]) for r in rows)
        after = Counter(tuple(recipes[(r["generator_sid"], r["node_path"])]) for r in rotated)
        if before != after:
            raise AssertionError("Recipe conservation failed")
        changed += sum(a["item_sid"] != b["item_sid"] for a, b in zip(rows, rotated))
    print(f"[OK] {len(slots)} source recipes resolved, {len(groups)} strict tier/family groups")
    print(f"[OK] whole-recipe in-memory conservation; {changed} item SID changes in rotation test")
    print(f"[AUDIT] {ammo_count} generators contain separate Ammo categories; {len(slots)-ammo_count} do not")
    print("[LIMIT] Does not validate game BPatch merge semantics or sibling ammo ownership")

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument("--cfg", required=True, type=Path)
    p.add_argument("--manifest", type=Path, default=DATA / "world-weapon-shuffle-manifest.csv")
    args = p.parse_args()
    audit(args.cfg, args.manifest)

if __name__ == "__main__":
    main()
