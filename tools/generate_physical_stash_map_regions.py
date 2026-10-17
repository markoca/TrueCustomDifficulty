#!/usr/bin/env python3
"""Join physical ItemContainers to pinned geographic polygons and regular stash SIDs."""
import argparse
import csv
import io
import json
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'src/TrueCustomDifficulty/Data/physical-stashes.csv'
REGULAR = ROOT / 'src/TrueCustomDifficulty/Data/stash-regular-actor-map-regions.csv'
GEO = ROOT / 'tools/data/stalker2-community-regions.json'
TARGET = ROOT / 'src/TrueCustomDifficulty/Data/physical-stash-map-regions.csv'
NEAR = {'3DC572304DD3E6EA9B6266A59F4A03BA': 'prypiat', '75B17AF54B2B381145BEFEB6A125FF0A': 'cordon'}
FIELDS = ['origin','actor_sid','container_sid','stash_prototype_sid','level_name','region','region_evidence','inventory_class','position_x','position_y','position_z','spawn_on_start']
def inside(x, y, ring):
    hit = False
    for i in range(len(ring)):
        ax, ay = ring[i]
        bx, by = ring[i - 1]
        if (ay > y) != (by > y) and x < (bx - ax) * (y - ay) / (by - ay) + ax:
            hit = not hit
    return hit

def generate():
    with SOURCE.open(newline='', encoding='utf-8') as f:
        actors = list(csv.DictReader(f))
    with REGULAR.open(newline='', encoding='utf-8') as f:
        regular = {r['ActorSID']: r for r in csv.DictReader(f)}
    features = json.loads(GEO.read_text(encoding='utf-8'))['features']
    result = []
    for a in actors:
        x, y = float(a['position_x']) / 100, float(a['position_y']) / 100
        names = [f['properties']['svgID'].lstrip('#') for f in features
                 if inside(x, y, f['geometry']['coordinates'][0])]
        region = names[0] if len(names) == 1 else 'UNRESOLVED'
        evidence = 'POLYGON' if len(names) == 1 else 'OUTSIDE_POLYGONS'
        if region == 'UNRESOLVED' and a['actor_sid'] in NEAR:
            region = NEAR[a['actor_sid']]
            evidence = 'NEAR_BORDER_INFERRED'
        cls = 'REGULAR_SMARTLOOT' if a['actor_sid'] in regular else 'OTHER_PLACED_CONTAINER'
        if cls == 'REGULAR_SMARTLOOT' and regular[a['actor_sid']]['MapRegion'] != region:
            raise ValueError('Region mismatch for ' + a['actor_sid'])
        result.append(dict(origin=a['origin'],actor_sid=a['actor_sid'],container_sid=a['container_sid'],
            stash_prototype_sid=a['stash_prototype_sid'],level_name=a['level_name'],region=region,
            region_evidence=evidence,inventory_class=cls,position_x=a['position_x'],
            position_y=a['position_y'],position_z=a['position_z'],spawn_on_start=a['spawn_on_start']))
    assert len(result) == 1569 and len({(r['origin'],r['actor_sid']) for r in result}) == 1569
    assert sum(r['inventory_class'] == 'REGULAR_SMARTLOOT' for r in result) == 831
    return result
if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('--check', action='store_true')
    args = p.parse_args()
    rows = generate()
    buffer = io.StringIO()
    writer = csv.DictWriter(buffer, fieldnames=FIELDS, lineterminator=chr(10))
    writer.writeheader()
    writer.writerows(rows)
    if args.check:
        assert TARGET.read_text(encoding='utf-8') == buffer.getvalue(), 'Physical regional catalog drift'
        print('PASS: 1569 physical actors, 831 regular actor joins')
    else:
        TARGET.write_text(buffer.getvalue(), encoding='utf-8')
        print('Wrote', TARGET)
