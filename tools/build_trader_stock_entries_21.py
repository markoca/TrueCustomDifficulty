#!/usr/bin/env python3
"""Build the 2.1 trader stock entry-values manifest (stdlib only).

For every unique (generator scope, generator SID, entry path) of the locked
trader-sells-2.1-runtime.csv, record the source selection field (Chance or
Weight), its value, MinCount/MaxCount, and — for Weight entries — the list
size and Weight total, so the writer can scale odds/amounts in per-trader
clones and append a native Empty entry without reading the game files.

Usage: build_trader_stock_entries_21.py BASE_CFG_ZIP DLC1_CFG_ZIP RUNTIME_CSV OUTPUT_CSV
"""
import csv
import re
import sys
import zipfile

if len(sys.argv) != 5:
    sys.exit(__doc__)
base_zip, dlc_zip, runtime_csv, output = sys.argv[1:]
rx_start = re.compile(r'^\s*([^\s:]+)\s*:\s*struct\.begin')
rx_assign = re.compile(r'^\s*(\w+)\s*=\s*(.*?)\s*$')


def parse(text):
    roots, stack = [], []
    for line in text.splitlines():
        m = rx_start.match(line)
        if m:
            node = {'key': m.group(1), 'attrs': {}, 'children': []}
            (stack[-1]['children'] if stack else roots).append(node)
            stack.append(node)
        elif 'struct.end' in line:
            if stack:
                stack.pop()
        elif stack:
            a = rx_assign.match(line)
            if a:
                stack[-1]['attrs'][a.group(1)] = a.group(2)
    return roots


def load(path, member):
    with zipfile.ZipFile(path) as z:
        return {n['attrs'].get('SID', n['key']): n
                for n in parse(z.read(member).decode('utf-8-sig'))}


gens = {'base': load(base_zip, 'GameData/ItemGeneratorPrototypes.cfg'),
        'dlc1': load(dlc_zip, 'DLCGameData/DLC1/ItemGeneratorPrototypes.cfg')}


def child(node, key):
    return next(c for c in node['children'] if c['key'] == key)


keys = []
with open(runtime_csv, encoding='utf-8', newline='') as f:
    for r in csv.DictReader(f):
        key = (r['generator_scope'], r['generator_sid'], r['entry_cfg_path'])
        if key not in keys:
            keys.append(key)

rows = []
for scope, sid, path in keys:
    _, category, _, item = path.split('/')
    plist = child(child(child(gens[scope][sid], 'ItemGenerator'), category), 'PossibleItems')
    entry = child(plist, item)
    a = entry['attrs']
    field = 'Weight' if 'Weight' in a and 'Chance' not in a else 'Chance'
    weights = [c['attrs'].get('Weight', '') for c in plist['children']]
    total = sum(float(w.rstrip('f') or 0) for w in weights) if field == 'Weight' else 0.0
    rows.append([scope, sid, path, a['ItemPrototypeSID'], field, a[field],
                 a.get('MinCount', ''), a.get('MaxCount', ''),
                 len(plist['children']) if field == 'Weight' else '',
                 f'{total:g}' if field == 'Weight' else ''])

with open(output, 'w', encoding='utf-8', newline='') as f:
    w = csv.writer(f, lineterminator='\n')
    w.writerow(['generator_scope', 'generator_sid', 'entry_cfg_path', 'item_sid', 'field',
                'value', 'min_count', 'max_count', 'list_count', 'list_weight_total'])
    w.writerows(rows)
print(f'{len(rows)} trader stock entries -> {output}')
