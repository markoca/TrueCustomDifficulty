#!/usr/bin/env python3
"""Validate the source-backed base/DLC1 SID inventory without requiring CFG archives.

For a fresh extract run tools/generate_spawn_item_sids.py with both CFG ZIPs;
then rerun this audit. The count is intentionally not frozen so new dumps can differ.
"""
from pathlib import Path
import csv
import re
import sys

ROOT = Path(__file__).resolve().parents[1]
MD = ROOT / "docs/SPAWNABLE-ITEM-SIDS.md"
CSV = ROOT / "src/TrueCustomDifficulty/Data/spawnable-item-sids.csv"
assert MD.is_file() and CSV.is_file(), "Run generate_spawn_item_sids.py first"
with CSV.open(encoding="utf-8", newline="") as f:
    rows = list(csv.DictReader(f))
assert rows
categories = ('consumables','weapons','ammo','attachments','armor','binoculars','nvg','detectors','artifacts')
expected_types = {
    'consumables': {'Consumable', 'Grenade'}, 'weapons': {'Weapon'},
    'ammo': {'Ammo'}, 'attachments': {'Attach'}, 'armor': {'Armor'},
    'binoculars': {'Binoculars'}, 'nvg': {'NightVisionGoggles'},
    'detectors': {'Detector'}, 'artifacts': {'Artifact'}
}
mechanisms = {'generator', 'npc-pack', 'stash-smartloot', 'placed-item',
              'placed-actor-item', 'placed-attachment', 'artifact-spawner', 'script-item'}
assert len(rows) == len({r['sid'] for r in rows}), 'Duplicate item SIDs'
assert [categories.index(r['category']) for r in rows] == sorted(categories.index(r['category']) for r in rows), 'Rows not category sorted'
assert not any(r['sid'] in ('A762D', 'A762A', 'A762E') for r in rows), 'Cut 7.62x39 ammo returned'
assert all(r['native_type'] in expected_types[r['category']] for r in rows)
assert all(r['definition_origin'] in ('base','dlc1') for r in rows)
assert all(r['definition_cfg'].endswith('/ItemPrototypes.cfg') for r in rows)
assert all(set(r['spawn_mechanisms'].split(',')) <= mechanisms for r in rows)
assert all(r['spawn_cfg_paths'] and all(path.startswith(('GameData/','DLCGameData/DLC1/')) and path.endswith('.cfg')
                                        for path in r['spawn_cfg_paths'].split(';')) for r in rows)
text = MD.read_text(encoding='utf-8')
shortkind={'generator':'GEN','npc-pack':'PACK','stash-smartloot':'STASH','placed-item':'WORLD',
           'placed-actor-item':'WORLD','placed-attachment':'WORLD','artifact-spawner':'ART','script-item':'SCRIPT'}
for cat in categories:
    group = [r for r in rows if r['category']==cat]
    assert group, cat
    assert f'## {cat.title()} ({len(group)})' in text, cat
    for r in group:
        codes = ','.join(dict.fromkeys(shortkind[m] for m in r['spawn_mechanisms'].split(',')))
        assert f"| `{r['sid']}` | {r['definition_origin']} | {codes} |" in text, r['sid']
assert f'(total **{len(rows)}**)' in text
assert all(f'`{sid}`' in text for sid in ('A762D', 'A762A', 'A762E'))
for sid in ('Medkit','Bandage','GunAK74_ST','A918D','RU_ColimScope_1',
            'Newbee_Neutral_Armor','Binoculars_01','NVG_Gen1','Echo','EArtifactFlash'):
    assert any(r['sid']==sid for r in rows), sid
assert 'does **not** prove an item will spawn' in text
print('PASS: source-indexed, category-consistent SID inventory:',len(rows),'SIDs in',len(categories),'categories')
