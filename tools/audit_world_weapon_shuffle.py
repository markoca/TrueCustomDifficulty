#!/usr/bin/env python3
import csv,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
slots=list(csv.DictReader(open(ROOT/'Data/world-weapon-shuffle-manifest.csv',encoding='utf8')))
pool=list(csv.DictReader(open(ROOT/'Data/world-weapon-shuffle-pool.csv',encoding='utf8')))
compatible=[]
for s in slots:
    alts=[p for p in pool if p['tier']==s['progression_tier'] and p['family']==s['family'] and p['item_sid']!=s['item_sid']]
    if alts: compatible.append((s,alts))
assert len(slots)==14, len(slots)
assert len(compatible)==8, len(compatible)
assert all(s['source']=='base' for s in slots)
assert all(s['node_path'].startswith('ItemGenerator/') and '/PossibleItems/' in s['node_path'] for s in slots)
assert all(s['item_sid'] not in {'Gun_Trophy_AR','GunRpg7_GL','GunGauss_SP'} for s in slots)
print('[OK] world weapon shuffle: 14 allowlisted regular SAFE_WORLD slots / 8 same-tier+family compatible')
for s,alts in compatible:
    print(f"  {s['generator_sid']}: {s['item_sid']} -> {', '.join(p['item_sid'] for p in alts)}")

# Structural contract gates added in v0.4.5
EXPECTED_ALLOWLISTED = 14
EXPECTED_COMPATIBLE = 8
assert len(slots) == EXPECTED_ALLOWLISTED, f'allowlist drift: {len(slots)} != {EXPECTED_ALLOWLISTED}'
compatible = [s for s in slots if any(p['tier']==s['progression_tier'] and p['family']==s['family'] and p['item_sid']!=s['item_sid'] for p in pool)]
assert len(compatible) == EXPECTED_COMPATIBLE, f'compatible-slot drift: {len(compatible)} != {EXPECTED_COMPATIBLE}'
for s in slots:
    path=s['node_path'].split('/')
    assert len(path) == 4 and path[0]=='ItemGenerator' and path[2]=='PossibleItems', f"unexpected node path: {s['generator_sid']} {s['node_path']}"
    assert path[1].startswith('[') and path[3].startswith('['), f"non-index node path: {s['generator_sid']} {s['node_path']}"
print('[OK] structural contract: manifest cardinality, compatibility cardinality, and node paths locked')

writer=(ROOT/'src/TrueCustomDifficulty/Services/CfgPatchWriterService.cs').read_text(encoding='utf8')
seed_start=writer.index('private static int BuildWorldWeaponSeed')
seed_end=writer.index('private static void SelectDeterministicPrefix', seed_start)
seed_block=writer[seed_start:seed_end]
assert '.Where(x => x.Id != "loot.worldWeaponShuffle")' in seed_block, 'shuffle amount must be excluded from seed'
assert 'SelectDeterministicPrefix(compatible, changeCount, rng)' in writer, 'prefix selection contract missing'
print('[OK] amount-independent seed + deterministic prefix guarantee nested 25/50/100 selection')
import math
# C# MidpointRounding.AwayFromZero is exact here because all products are integers.
expected_counts={0.25:2,0.50:4,1.00:8}
for fraction, expected in expected_counts.items():
    actual=int(math.floor(len(compatible)*fraction+0.5))
    assert actual==expected, f'{fraction:.0%}: {actual} != {expected}'
print('[OK] amount cardinality: 25%=2 / 50%=4 / 100%=8; prefix nesting locked')

# Writer-shape fixtures are generated from the canonical manifest/pool and must stay reproducible.
import subprocess
subprocess.run([sys.executable, str(ROOT/'tools/generate_world_weapon_shuffle_fixtures.py')], check=True, cwd=ROOT)
for pct, expected in [(25,2),(50,4),(100,8)]:
    text=(ROOT/f'tests/world-weapon-shuffle-fixtures/world-weapon-shuffle-{pct}.cfg').read_text(encoding='utf8')
    assert text.count('ItemPrototypeSID = ') == expected, (pct, text.count('ItemPrototypeSID = '))
    assert text.count('PossibleItems : struct.begin {bpatch}') == expected
    assert text.count('[0] : struct.begin\n') == expected
    assert 'Upgrades =' not in text and 'Attaches =' not in text
print('[OK] rendered fixture shape: replacement nodes are full children; parents are bpatch; no Upgrades/Attaches inherited')
