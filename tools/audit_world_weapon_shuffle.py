#!/usr/bin/env python3
import csv,sys,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
slots=list(csv.DictReader(open(ROOT/'src/TrueCustomDifficulty/Data/world-weapon-shuffle-manifest.csv',encoding='utf8')))
pool=list(csv.DictReader(open(ROOT/'src/TrueCustomDifficulty/Data/world-weapon-shuffle-pool.csv',encoding='utf8')))
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
# Current writer (WriteWorldWeaponDistribution): SHUFFLE toggle + DENSITY over
# direct actors and these generator slots; the earlier seeded 25/50/100 prefix
# design and its rendered fixtures are retired.
block=writer[writer.index('private static (int EditCount, int FileCount) WriteWorldWeaponDistribution('):]
block=block[:block.index('\n    private static ', 10)]
for token in ('const string toggleId = "loot.worldWeaponShuffle";', 'const string densityId = "loot.worldWeaponDensity";',
              'LoadWorldWeaponSlots()', 'LoadWorldWeaponDirectSlots()', 'LoadWorldWeaponPool()'):
    assert token in block, token
print('[OK] current writer: SHUFFLE toggle + DENSITY over direct and generator slots')

# Gamer-facing text contract: rename the presentation, not the source setting.
ui=(ROOT/'src/TrueCustomDifficulty/Controls/AggregateVisualControl.cs').read_text(encoding='utf8')
params=json.loads((ROOT/'src/TrueCustomDifficulty/Data/parameters.json').read_text(encoding='utf8'))['parameters']
guns=[p for p in params if p['id']=='loot.worldWeaponDensity']
assert len(guns)==1
setting=guns[0]
assert setting['label']=='Guns to Find' and setting['display']['valueLabel']=='Guns to Find'
assert setting['requiresToggle']=='loot.worldWeaponShuffle'
assert setting['slider']=={'min': 0, 'max': 1, 'step': 0.05, 'stepMode': 'absolute'}
assert 'Even at 0%' in setting['description']
assert 'Quest, unique and NPC weapons are unaffected' in setting['rationale']
assert 'DrawText(dc, "GUNS TO FIND", new Point(left, rowTop + 2.0)' in ui
assert 'DrawText(dc, "Density", new Point(left, rowTop + 2.0)' not in ui
vm=(ROOT/'src/TrueCustomDifficulty/ViewModels/ParameterViewModel.cs').read_text(encoding='utf8')
assert '"loot.worldWeaponDensity" => $"{CurrentNumber * 100:0.#}% of possible spawns"' in vm
assert 'DrawRightAlignedText(dc, "OF POSSIBLE SPAWNS"' in ui
assert 'new Point(right, rowTop + 12.0), 7.4,' in ui
assert 'var value = $"{parameter.CurrentNumber * 100.0:0.#}%";' in ui
assert 'var track = new Rect(left + 2.0, rowTop + 22.0,' in ui
print('[OK] world gun supply UI: possible-spawns wording and compact native rail retained')
