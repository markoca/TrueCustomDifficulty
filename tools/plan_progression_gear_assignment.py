#!/usr/bin/env python3
"""Read-only deterministic same-tier/family weapon assignment preview."""
import argparse
import csv
import hashlib
import random
import sys
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / 'src' / 'TrueCustomDifficulty' / 'Data'

def load(name):
    with (ROOT / name).open(newline='', encoding='utf-8-sig') as f:
        return list(csv.DictReader(f))

def plan(seed):
    gear = {r['ItemSID']: r for r in load('gear-progression-ranks.csv') if r['ItemClass'] == 'Weapon' and r['Confidence'] == 'EXPLICIT_TRADER_TIER'}
    slots = load('world-weapon-shuffle-manifest.csv')
    assert len(slots) == 14, 'Source slot allowlist drifted'
    assert all(r['source'] == 'base' for r in slots), 'Unverified scope in slot allowlist'
    buckets = defaultdict(list)
    for r in slots:
        g = gear.get(r['item_sid'])
        assert g is not None, f'Unapproved source weapon: {r["item_sid"]}'
        assert (g['ProgressionTier'], g['ItemType']) == (r['progression_tier'], r['family']), f'Tier/family mismatch: {r["item_sid"]}'
        buckets[(r['progression_tier'], r['family'])].append(r)
    output = []
    for key in sorted(buckets):
        rows = sorted(buckets[key], key=lambda r: (r['generator_sid'], r['node_path']))
        items = [r['item_sid'] for r in rows]
        # Per-bucket stable seed independent of Python's salted hash().
        digest = hashlib.sha256((str(seed) + ':' + ':'.join(key)).encode()).digest()
        rng = random.Random(int.from_bytes(digest[:8], 'big'))
        rng.shuffle(items)
        assert Counter(items) == Counter(r['item_sid'] for r in rows)
        for slot, assigned in zip(rows, items):
            output.append({'generator_sid':slot['generator_sid'], 'node_path':slot['node_path'], 'tier':key[0], 'family':key[1], 'original_item_sid':slot['item_sid'], 'assigned_item_sid':assigned, 'changed':str(slot['item_sid'] != assigned).lower()})
    assert len(output) == len(slots)
    return sorted(output, key=lambda r: (r['generator_sid'], r['node_path']))

def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--seed', default='42')
    p.add_argument('--check', action='store_true', help='Verify determinism and conservation without printing rows')
    args = p.parse_args()
    rows = plan(args.seed)
    if args.check:
        assert rows == plan(args.seed)
        assert Counter(r['original_item_sid'] for r in rows) == Counter(r['assigned_item_sid'] for r in rows)
        assert all(r['tier'] in {'T1','T2','T3','T4'} for r in rows)
        print(f'[OK] progression gear planner: {len(rows)} allowlisted weapon slots, strict tier/family, deterministic and conserved')
        return
    fields = ['generator_sid','node_path','tier','family','original_item_sid','assigned_item_sid','changed']
    writer = csv.DictWriter(sys.stdout, fieldnames=fields, lineterminator='\\n')
    writer.writeheader()
    writer.writerows(rows)

if __name__ == '__main__':
    main()
