#!/usr/bin/env python3
"""Audit exact authored weapon spawn recipes for allowlisted progression slots."""
import argparse
import csv
import re
from pathlib import Path

OPEN = re.compile(r'^\\s*([^:]+?)\\s*:\\s*struct\\.begin\\b')
CLOSE = re.compile(r'^\\s*struct\\.end\\s*$')

def blocks(lines):
    stack = []
    for i, line in enumerate(lines):
        match = OPEN.match(line)
        if match:
            stack.append((match.group(1).strip(), i))
        elif CLOSE.match(line):
            if not stack:
                raise ValueError(f'Unmatched struct.end at line {i+1}')
            name, start = stack.pop()
            yield tuple(n for n, _ in stack) + (name,), start, i + 1
    if stack:
        raise ValueError('Unclosed struct.begin blocks')

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--cfg', required=True, type=Path)
    p.add_argument('--manifest', default='src/TrueCustomDifficulty/Data/world-weapon-shuffle-manifest.csv', type=Path)
    args = p.parse_args()
    with args.manifest.open(newline='', encoding='utf-8-sig') as f:
        slots = list(csv.DictReader(f))
    if len(slots) != 14:
        raise ValueError(f'Expected 14 approved slots, found {len(slots)}')
    lines = args.cfg.read_text(encoding='utf-8-sig').splitlines()
    wanted = {(r['generator_sid'], *r['node_path'].split('/')): r for r in slots}
    found = {}
    for path, start, end in blocks(lines):
        if path not in wanted:
            continue
        if path in found:
            raise ValueError(f'Duplicate source block {path}')
        body = '\\n'.join(lines[start:end])
        matches = re.findall(r'^\\s*ItemPrototypeSID\\s*=\\s*(\\S+)', body, re.M)
        if len(matches) != 1 or matches[0] != wanted[path]['item_sid']:
            raise ValueError(f'Source item mismatch at {path}: {matches}')
        found[path] = {'upgrade': 'Upgrades : struct.begin' in body, 'attach': 'Attaches : struct.begin' in body, 'lines': end-start}
    missing = set(wanted) - set(found)
    if missing:
        raise ValueError(f'Missing source blocks: {sorted(missing)}')
    for path in sorted(found):
        r = wanted[path]
        info = found[path]
        print(f"{r['item_sid']},tier={r['progression_tier']},family={r['family']},upgrade={info['upgrade']},attach={info['attach']},recipe_lines={info['lines']},generator={r['generator_sid']}")
    print(f'[OK] validated {len(found)} complete source slot blocks; audit-only, no patch written')

if __name__ == '__main__':
    main()
