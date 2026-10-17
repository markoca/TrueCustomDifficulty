#!/usr/bin/env python3
"""Map regular stash actors into pinned community geographic polygons."""
import argparse
import csv
import io
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'src/TrueCustomDifficulty/Data/stash-regular-actor-regions.csv'
POLYGONS = ROOT / 'tools/data/stalker2-community-regions.json'
TARGET = ROOT / 'src/TrueCustomDifficulty/Data/stash-regular-actor-map-regions.csv'
ALIASES = {"CityCenter": "prypiat", "CementFactory": "cement_factory", "LesserZone": "lesser_zone", "Yantar": "yantar", "Wild": "wild_island", "RedForest": "red_forest", "CoolingTowers": "cooling_towers", "BurntForest": "orbita", "Garbage": "garbage", "Backwater": "zaton", "Promzona": "iron_forest", "STCMalachite": "malachite", "Swamp": "swamp", "Jupiter": "jupiter", "Cordon": "cordon", "ChemicalPlant": "chemical_plant", "DugaRadar": "duga", "SIIRCA": "sircaa", "Rostok": "rostok"}

def contains(x, y, ring):
    hit = False
    j = len(ring) - 1
    for i, (ax, ay) in enumerate(ring):
        bx, by = ring[j]
        if (ay > y) != (by > y) and x < (bx - ax) * (y - ay) / (by - ay) + ax:
            hit = not hit
        j = i
    return hit

def generate():
    features = json.loads(POLYGONS.read_text(encoding='utf-8'))['features']
    with SOURCE.open(newline='', encoding='utf-8') as f:
        original = list(csv.DictReader(f))
    rows = []
    for row in original:
        x, y = float(row['X']) / 100, float(row['Y']) / 100
        names = [f['properties']['svgID'].lstrip('#') for f in features
                 if contains(x, y, f['geometry']['coordinates'][0])]
        region = names[0] if len(names) == 1 else 'UNRESOLVED'
        generator = row['Region']
        status = ('OUTSIDE_POLYGONS' if region == 'UNRESOLVED' else
                  'NEW_MAP_MATCH' if generator == 'UNRESOLVED' else
                  'AGREES_WITH_GENERATOR' if ALIASES.get(generator) == region else
                  'GENERATOR_MAP_CONFLICT')
        rows.append(dict(ActorSID=row['ActorSID'], MapRegion=region,
                         GeneratorRegion=generator, MatchStatus=status,
                         X=row['X'], Y=row['Y'], Z=row['Z']))
    return rows

if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('--check', action='store_true')
    args = p.parse_args()
    rows = generate()
    assert len(rows) == 831 and len({r['ActorSID'] for r in rows}) == 831
    buffer = io.StringIO()
    writer = csv.DictWriter(buffer, fieldnames=['ActorSID','MapRegion','GeneratorRegion','MatchStatus','X','Y','Z'], lineterminator=chr(10))
    writer.writeheader()
    writer.writerows(rows)
    if args.check:
        assert TARGET.read_text(encoding='utf-8') == buffer.getvalue(), 'Map region catalog drift'
        print('PASS: geographic catalog matches 831 actor rows')
    else:
        TARGET.write_text(buffer.getvalue(), encoding='utf-8')
        print('Wrote', TARGET)
