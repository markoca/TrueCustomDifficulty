#!/usr/bin/env python3
"""Rebuild conservative stash region labels from the committed actor manifest."""
import csv
from collections import Counter
from pathlib import Path
import re
ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'src/TrueCustomDifficulty/Data/smartloot-regular-world-actor-manifest.csv'
TARGET = ROOT / 'src/TrueCustomDifficulty/Data/stash-regular-actor-regions.csv'
PATTERN = re.compile(r'(?:^|,)([A-Za-z][A-Za-z0-9]*)_[A-Za-z0-9_]*StashGenerator')
def generate():
    with SOURCE.open(newline='', encoding='utf-8') as stream:
        rows = list(csv.DictReader(stream))
    result = []
    for row in rows:
        names = set(PATTERN.findall(row['OriginalItemGeneratorSIDs']))
        region = next(iter(names)) if len(names) == 1 else 'UNRESOLVED'
        result.append({'ActorSID': row['ActorSID'], 'Region': region,
                       'RegionEvidence': 'GENERATOR_SID_PREFIX' if region != 'UNRESOLVED' else 'NO_EXPLICIT_REGION',
                       'OriginalItemGeneratorSIDs': row['OriginalItemGeneratorSIDs'],
                       'X': row['X'], 'Y': row['Y'], 'Z': row['Z']})
    return result
if __name__ == '__main__':
    import argparse, io, sys
    p = argparse.ArgumentParser()
    p.add_argument('--check', action='store_true')
    args = p.parse_args()
    rows = generate()
    if len(rows) != 831 or len({r['ActorSID'] for r in rows}) != 831:
        sys.exit('Unexpected regular stash inventory count or duplicate actor SID')
    buf = io.StringIO(newline='')
    writer = csv.DictWriter(buf, fieldnames=['ActorSID','Region','RegionEvidence','OriginalItemGeneratorSIDs','X','Y','Z'], lineterminator='\n')
    writer.writeheader()
    writer.writerows(rows)
    text = buf.getvalue()
    if args.check:
        if TARGET.read_text(encoding='utf-8') != text:
            sys.exit('Regional stash catalog drift; run generator')
        print('PASS: 831 actor rows, regional CSV matches source')
    else:
        TARGET.write_text(text, encoding='utf-8')
        print('Wrote', TARGET)
