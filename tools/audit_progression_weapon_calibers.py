#!/usr/bin/env python3
"""Build an evidence-only weapon SID -> caliber -> default ammo SID audit from extracted CFGs."""
import argparse
import csv
import re
import sys
from pathlib import Path

OPEN = re.compile(r'^\s*([^:]+?)\s*:\s*struct\.begin\b')
CLOSE = re.compile(r'^\s*struct\.end\s*$')
ASSIGN = re.compile(r'^\s*([A-Za-z][A-Za-z0-9_]*)\s*=\s*(.*?)\s*(?://.*)?$')
CALIBER = re.compile(r'^(?:EAmmoCaliber::)?([A-Za-z0-9_]+)$')

def cfg_blocks(file):
    lines = file.read_text(encoding='utf-8-sig').splitlines()
    stack = []
    for i, line in enumerate(lines):
        m = OPEN.match(line)
        if m:
            stack.append((m.group(1).strip(), i))
        elif CLOSE.match(line):
            if not stack:
                raise ValueError(f'{file}:{i+1}: unmatched struct.end')
            name, start = stack.pop()
            if len(stack) == 0:
                yield name, lines[start:i+1]
    if stack:
        raise ValueError(f'{file}: unclosed block')

def direct_properties(block):
    depth = 0
    out = {}
    for line in block[1:-1]:
        if OPEN.match(line):
            depth += 1
        elif CLOSE.match(line):
            depth -= 1
        elif depth == 0:
            m = ASSIGN.match(line)
            if m:
                out[m.group(1)] = m.group(2).strip()
    return out

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--weapon-cfg', required=True, type=Path, help='Extracted WeaponGeneralSetupPrototypes.cfg')
    p.add_argument('--ammo-cfg', required=True, type=Path, help='Extracted ItemPrototypes.cfg')
    p.add_argument('--output', type=Path, help='Optional audit CSV output (never a gameplay patch)')
    p.add_argument('--strict', action='store_true', help='Reject ambiguous/missing evidence')
    a = p.parse_args()
    weapons = {}
    for sid, block in cfg_blocks(a.weapon_cfg):
        props = direct_properties(block)
        caliber = props.get('AmmoCaliber')
        if not caliber:
            continue
        match = CALIBER.fullmatch(caliber)
        if not match:
            raise ValueError(f'Unexpected caliber {sid}: {caliber}')
        if sid in weapons:
            raise ValueError(f'Duplicate weapon setup SID {sid}')
        weapons[sid] = match.group(1)
    ammo = {}
    for sid, block in cfg_blocks(a.ammo_cfg):
        props = direct_properties(block)
        caliber = props.get('AmmoCaliber')
        if not caliber or props.get('AmmoType') != 'Default':
            continue
        match = CALIBER.fullmatch(caliber)
        if not match:
            raise ValueError(f'Unexpected ammo caliber {sid}: {caliber}')
        ammo.setdefault(match.group(1), []).append(sid)
    if not weapons:
        raise ValueError('No exact weapon AmmoCaliber assignments found')
    if not ammo:
        raise ValueError('No exact Default ammo caliber assignments found; check actual CFG property names')
    rows = []
    for sid, caliber in sorted(weapons.items()):
        candidates = sorted(set(ammo.get(caliber, [])))
        status = 'EXACT_SINGLE_DEFAULT' if len(candidates) == 1 else ('MISSING' if not candidates else 'AMBIGUOUS')
        rows.append({'weapon_setup_sid':sid, 'ammo_caliber':caliber, 'default_ammo_sid':candidates[0] if len(candidates)==1 else '', 'status':status, 'candidate_count':len(candidates)})
    if a.strict and any(r['status'] != 'EXACT_SINGLE_DEFAULT' for r in rows):
        raise ValueError('Unresolved default ammo mappings; no production assignment allowed')
    fields = ['weapon_setup_sid','ammo_caliber','default_ammo_sid','status','candidate_count']
    if a.output:
        a.output.parent.mkdir(parents=True, exist_ok=True)
        with a.output.open('w', newline='', encoding='utf-8') as f:
            writer = csv.DictWriter(f, fieldnames=fields)
            writer.writeheader()
            writer.writerows(rows)
    else:
        writer = csv.DictWriter(sys.stdout, fieldnames=fields)
        writer.writeheader()
        writer.writerows(rows)
    print(f'[AUDIT] {len(rows)} weapon setup entries, {sum(r["status"]=="EXACT_SINGLE_DEFAULT" for r in rows)} unique default-ammo mappings; not an item-SID mapping', file=sys.stderr)

if __name__ == '__main__':
    main()
