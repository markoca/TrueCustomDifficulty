#!/usr/bin/env python3
"""Validate and preview whole-recipe same-tier weapon reassignment from original CFG."""
import argparse
import csv
import hashlib
import random
import re
from collections import Counter, defaultdict
from pathlib import Path

DATA = Path(__file__).resolve().parents[1] / "src/TrueCustomDifficulty/Data"
OPEN = re.compile(r"^\s*([^:]+?)\s*:\s*struct\.begin\b")
CLOSE = re.compile(r"^\s*struct\.end\s*$")
ITEM = re.compile(r"^\s*ItemPrototypeSID\s*=\s*(\S+)\s*$", re.M)

def read_csv(name):
    with (DATA / name).open(newline="", encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))

def extract(lines, wanted):
    stack = []
    found = {}
    for i, line in enumerate(lines):
        m = OPEN.match(line)
        if m:
            stack.append((m.group(1).strip(), i))
        elif CLOSE.match(line):
            if not stack:
                raise ValueError(f"Unmatched struct.end at line {i+1}")
            path = tuple(x[0] for x in stack)
            _, start = stack.pop()
            if path in wanted:
                if path in found:
                    raise ValueError(f"Duplicate source path: {path}")
                block = lines[start:i+1]
                items = ITEM.findall("\n".join(block))
                if len(items) != 1 or items[0] != wanted[path]["item_sid"]:
                    raise ValueError(f"Item mismatch at {path}: {items}")
                found[path] = block
    if stack:
        raise ValueError("Unclosed source CFG block")
    missing = wanted.keys() - found.keys()
    if missing:
        raise ValueError(f"Missing {len(missing)} approved source blocks: {sorted(missing)}")
    return found

def plan(slots, seed):
    groups = defaultdict(list)
    for slot in slots:
        groups[(slot["progression_tier"], slot["family"])].append(slot)
    assignments = []
    for key in sorted(groups):
        rows = sorted(groups[key], key=lambda r: (r["generator_sid"], r["node_path"]))
        shuffled = list(rows)
        digest = hashlib.sha256((str(seed) + ":" + ":".join(key)).encode()).digest()
        random.Random(int.from_bytes(digest[:8], "big")).shuffle(shuffled)
        assignments.extend(zip(rows, shuffled))
    return assignments

def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument("--cfg", type=Path, required=True)
    ap.add_argument("--seed", default="42")
    ap.add_argument("--check", action="store_true")
    args = ap.parse_args()
    slots = read_csv("world-weapon-shuffle-manifest.csv")
    gear = {r["ItemSID"]: r for r in read_csv("gear-progression-ranks.csv")
            if r["ItemClass"] == "Weapon" and r["Confidence"] == "EXPLICIT_TRADER_TIER"}
    if len(slots) != 14:
        raise ValueError(f"Expected 14 allowlisted slots, got {len(slots)}")
    wanted = {}
    for slot in slots:
        item = gear.get(slot["item_sid"])
        if not item or (item["ProgressionTier"], item["ItemType"]) != (slot["progression_tier"], slot["family"]):
            raise ValueError(f"Unapproved or mistiered item {slot['item_sid']}")
        path = (slot["generator_sid"], *slot["node_path"].split("/"))
        if path in wanted:
            raise ValueError(f"Duplicate destination {path}")
        wanted[path] = slot
    lines = args.cfg.read_text(encoding="utf-8-sig").splitlines()
    recipes = extract(lines, wanted)
    assignments = plan(slots, args.seed)
    assert len(assignments) == len(slots)
    assert Counter(s["item_sid"] for s in slots) == Counter(src["item_sid"] for _, src in assignments)
    assert all((dest["progression_tier"], dest["family"]) == (src["progression_tier"], src["family"])
               for dest, src in assignments)
    if args.check:
        assert assignments == plan(slots, args.seed)
        print(f"[OK] {len(recipes)} verified whole recipes, strict tier/family, seeded and conserved")
        return
    print("# AUDIT-ONLY whole-recipe assignment; NOT a deployable CFG patch")
    for dest, src in sorted(assignments, key=lambda pair: (pair[0]["generator_sid"], pair[0]["node_path"])):
        if dest is src:
            continue
        source_path = (src["generator_sid"], *src["node_path"].split("/"))
        body = recipes[source_path]
        # A complete source block is copied as-is; this avoids SID-only leakage of
        # foreign Upgrades, Attaches and ammo fields. CFG patch syntax is deliberately
        # not emitted until a tested whole-node replacement mechanism is available.
        print(f"DEST {dest['generator_sid']} {dest['node_path']} <- SOURCE {src['generator_sid']} {src['node_path']}")
        for line in body:
            print(line)
        print("END_RECIPE")

if __name__ == "__main__":
    main()
