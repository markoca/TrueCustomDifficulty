#!/usr/bin/env python3
"""Classify physical stash actors without granting new gameplay permissions."""
import argparse
import csv
import io
from pathlib import Path
ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / 'src/TrueCustomDifficulty/Data'
FIELDS = ['actor_sid','origin','inventory_class','safety_class','protection_reason','level_name','region','region_evidence','container_sid','stash_prototype_sid']
def read(name):
    with (DATA / name).open(newline='', encoding='utf-8') as f:
        return list(csv.DictReader(f))
def generate():
    actors = read('physical-stash-map-regions.csv')
    protected = {r['ActorSID']: r['ProtectionReason'] for r in read('base-stash-protected-actor-manifest.csv')}
    dlc = {r['ActorSID'] for r in read('dlc1-stash-eligible-actor-manifest.csv')}
    result = []
    for a in actors:
        sid = a['actor_sid']
        reason = ''
        if a['inventory_class'] == 'REGULAR_SMARTLOOT':
            cls = 'REGULAR_SMARTLOOT'
        elif sid in protected:
            cls = 'PROTECTED_BASE'
            reason = protected[sid]
        elif sid in dlc:
            cls = 'DLC1_ELIGIBLE_MANIFEST'
        elif not a['level_name'].startswith('WorldMap_WP'):
            cls = 'NON_WORLD_OR_TEST'
        else:
            cls = 'WORLD_UNCLASSIFIED'
        result.append(dict(actor_sid=sid,origin=a['origin'],inventory_class=a['inventory_class'],
            safety_class=cls,protection_reason=reason,level_name=a['level_name'],
            region=a['region'],region_evidence=a['region_evidence'],container_sid=a['container_sid'],
            stash_prototype_sid=a['stash_prototype_sid']))
    assert len(result) == 1569 and len({r['actor_sid'] for r in result}) == 1569
    expected = {'REGULAR_SMARTLOOT':831,'PROTECTED_BASE':78,'DLC1_ELIGIBLE_MANIFEST':112,
                'NON_WORLD_OR_TEST':67,'WORLD_UNCLASSIFIED':481}
    assert {k:sum(r['safety_class']==k for r in result) for k in expected} == expected
    return result
if __name__ == '__main__':
    p = argparse.ArgumentParser()
    p.add_argument('--check', action='store_true')
    args = p.parse_args()
    buf = io.StringIO()
    writer = csv.DictWriter(buf, fieldnames=FIELDS, lineterminator=chr(10))
    writer.writeheader()
    writer.writerows(generate())
    target = DATA / 'physical-stash-safety-catalog.csv'
    if args.check:
        assert target.read_text(encoding='utf-8') == buf.getvalue(), 'Safety catalog drift'
        print('PASS: 1569 physical actors classified with existing protection guards')
    else:
        target.write_text(buf.getvalue(), encoding='utf-8')
        print('Wrote', target)
