import argparse, re, zipfile, collections, json, pathlib, csv
parser = argparse.ArgumentParser(description='Generate definition-backed, spawn-referenced item SID inventory from GameData and DLC1 CFG ZIP dumps')
parser.add_argument('--game', required=True, help='GameData CFG ZIP')
parser.add_argument('--dlc', required=True, help='DLC1 CFG ZIP')
parser.add_argument('--output', default=str(pathlib.Path(__file__).resolve().parents[1] / 'docs' / 'SPAWNABLE-ITEM-SIDS.md'), help='Markdown catalog path')
parser.add_argument('--csv', default=str(pathlib.Path(__file__).resolve().parents[1] / 'src' / 'TrueCustomDifficulty' / 'Data' / 'spawnable-item-sids.csv'), help='Source-indexed CSV output path')
args=parser.parse_args()
GAME=args.game; DLC=args.dlc
zs={'base':zipfile.ZipFile(GAME),'dlc1':zipfile.ZipFile(DLC)}
# Definition-aware and source-cited CFG reference inventory. Inheritance follows ItemPrototype refkey chains,
# including refkey from the opposite archive through external DLC refurl.
item_defs={}; by_archive={}
top_re=re.compile(r'^([^\s:]+)\s*:\s*struct\.begin([^\n]*)$',re.M)
for origin,z in zs.items():
 fn=next(n for n in z.namelist() if n.endswith('/ItemPrototypes.cfg'))
 s=z.read(fn).decode('utf-8-sig',errors='replace')
 matches=list(top_re.finditer(s)); defs={}
 for i,m in enumerate(matches):
  block=s[m.end():matches[i+1].start() if i+1<len(matches) else len(s)]
  sid=re.search(r'^   SID\s*=\s*([^\s]+)',block,re.M)
  sid=sid.group(1) if sid else m.group(1)
  if sid.lower() in ('empty','none'):continue
  parent=re.search(r'\brefkey=([^};]+)',m.group(2))
  typ=re.search(r'^   Type\s*=\s*EItemType::(\w+)',block,re.M)
  cal=re.search(r'^   Caliber\s*=\s*EAmmoCaliber::(\w+)',block,re.M)
  defs[sid]={'sid':sid,'base':m.group(1),'parent':parent.group(1) if parent else '', 'type':typ.group(1) if typ else '', 'caliber':cal.group(1) if cal else '', 'origin':origin,'definition':fn}
 by_archive[origin]=defs
 item_defs.update(defs)

def field(sid,field):
 d=item_defs.get(sid);visited=set()
 while d and d['sid'] not in visited:
  visited.add(d['sid'])
  if d[field]:return d[field]
  d=item_defs.get(d['parent'])
 return ''

def category(sid):
 typ=field(sid,'type')
 return {'Consumable':'consumables','Grenade':'consumables','Weapon':'weapons','Ammo':'ammo','Attach':'attachments','Armor':'armor','Binoculars':'binoculars','NightVisionGoggles':'nvg','Detector':'detectors','Artifact':'artifacts'}.get(typ,'')


# Evidence indexed per SID, a set of concrete CFG paths and strong spawn-related references.
evidence=collections.defaultdict(lambda:collections.defaultdict(set))
item_ids=set(item_defs)

# A registry of intended item-bearing CFG fields, never incidental fitting weapon, upgrades, references or prototypes.
keys={
 'ItemPrototypeSID':'item-generator',
 'ItemSID':'placed-item',
 'ArtifactItemSID':'artifact-spawner',
 'SpawnedPrototypeSID':'placed-actor-item',
 'AttachmentSID':'placed-attachment',
 'PSYControllerArtifactItemSID':'artifact-spawner',
}
# Some scripts and NPCs use direct bare SID syntax; treat only strongly typed fields.
ref_re=re.compile(r'^\s*('+ '|'.join(keys) +r')\s*=\s*([^\r\n]+)',re.M)
for origin,z in zs.items():
 for name in z.namelist():
  if not name.endswith('.cfg') or name.endswith('/ItemPrototypes.cfg'):continue
  # Item generators, all inventory layouts, stash generators, placed scene items, scripted items.
  if name.endswith('SpawnActorPrototypes.cfg') or name.endswith('ItemGeneratorPrototypes.cfg') or name.endswith('PackOfItemsGroupPrototypes.cfg') or name.endswith('StashPrototypes.cfg') or name.endswith('ArtifactSpawnerPrototypes.cfg') or name.startswith(('GameData/Scripts/','DLCGameData/DLC1/Scripts/')) or name.endswith('NPCPrototypes.cfg'):
   content=z.read(name).decode('utf-8-sig',errors='replace')
   for m in ref_re.finditer(content):
    key,value=m.groups()
    for token in re.findall(r'[A-Za-z0-9_]+',value.strip()):
     if token not in item_ids:continue
     # Only real item-related references; avoid treating entire NPC/etc prototypes as item evidence.
     if key=='SpawnedPrototypeSID' and 'SpawnActorPrototypes.cfg' not in name:continue
     if key=='ItemPrototypeSID':
      if 'ItemGeneratorPrototypes.cfg' in name:kind='generator'
      elif 'PackOfItemsGroupPrototypes.cfg' in name:kind='npc-pack'
      elif 'StashPrototypes.cfg' in name:kind='stash-smartloot'
      elif 'SpawnActorPrototypes.cfg' in name:kind='placed-item'
      else:kind='script-item'
     elif key=='ItemSID' and 'SpawnActorPrototypes.cfg' in name:kind='placed-item'
     elif key=='SpawnedPrototypeSID':kind='placed-actor-item'
     elif key=='AttachmentSID':kind='placed-attachment'
     else:kind=keys[key]
     evidence[token][kind].add(name)
   if name.endswith('ArtifactSpawnerPrototypes.cfg'):
    # list entries are direct artifact spawn choices; enum settings cannot match EItemType::Artifact.
    for val in re.findall(r'^\s*\[\d+\]\s*=\s*([A-Za-z_][\w]*)\s*$',content,re.M):
     if val in item_ids and category(val)=='artifacts':evidence[val]['artifact-spawner'].add(name)

all_referenced={sid:ms for sid,ms in evidence.items() if category(sid)}
# Exclusion 7.62x39, not 7.62x51 NATO or 7.62x54 sniper
excluded=sorted(x for x in item_defs if category(x)=='ammo' and field(x,'caliber')=='A762')
rows=[]
for sid,mechanisms in all_referenced.items():
 if sid in excluded:continue
 rows.append({'sid':sid,'category':category(sid),'type':field(sid,'type'),'origin':item_defs[sid]['origin'], 'mechanisms':sorted(mechanisms),'sources':sorted(set().union(*mechanisms.values())),'definition':item_defs[sid]['definition']})
rows.sort(key=lambda r:(['consumables','weapons','ammo','attachments','armor','binoculars','nvg','detectors','artifacts'].index(r['category']),r['sid'].casefold()))
output={'archive_names':[pathlib.Path(GAME).name,pathlib.Path(DLC).name], 'method':'Confirmed source CFG reference in generator, stash, npc pack, actor placement, artifact spawner or scripted item. This is configured spawn eligibility, not proof of runtime occurrence.', 'excluded_ammo_76239':excluded,'counts':dict(collections.Counter(r['category'] for r in rows)), 'items':rows}

# Compact markdown useful inventory and evidence: separate source mechanism codes, and origin.
shortkind={'generator':'GEN','npc-pack':'PACK','stash-smartloot':'STASH','placed-item':'WORLD','placed-actor-item':'WORLD','placed-attachment':'WORLD','artifact-spawner':'ART','script-item':'SCRIPT'}
head=['# Spawn-referenced Item SIDs — GameData + DLC1','',
 'Source: GameData and DLC1 CFG dumps (2026-10-17). This inventory means an ItemPrototype definition has at least one configured spawn-related reference; it does **not** prove an item will spawn in a particular playthrough. Sources may be gated by player rank, quest, difficulty, conditions, rarity, or disabled entries. Unreferenced templates and mere upgrade/fitting SIDs are excluded.','',
 'Excluded as cut content: **7.62×39** `'+ '`, `'.join(excluded)+'`. The source includes these definitions even if some have no direct spawn reference. 7.62×51 NATO and 7.62×54 sniper ammunition remain included.','',
 'Evidence: `GEN` = item generator, `PACK` = pack-of-items, `STASH` = SmartLoot stash, `WORLD` = world/actor placement (including attachments), `ART` = artifact spawner, `SCRIPT` = script item. `base` and `dlc1` identify the defining ItemPrototypes file, not necessarily the spawning context. Full source CFG paths and native types are retained in `src/TrueCustomDifficulty/Data/spawnable-item-sids.csv`. Extractor: `tools/generate_spawn_item_sids.py`.','',
 'Counts: '+', '.join(f'{k} **{v}**' for k,v in output['counts'].items())+f' (total **{len(rows)}**).','']
for cat in ['consumables','weapons','ammo','attachments','armor','binoculars','nvg','detectors','artifacts']:
 group=[r for r in rows if r['category']==cat]
 head.extend([f'## {cat.title()} ({len(group)})','', '| SID | Definition | Spawn reference |','|---|---|---|'])
 for r in group:
  mechs=','.join(dict.fromkeys(shortkind[m] for m in r['mechanisms']))
  head.append(f"| `{r['sid']}` | {r['origin']} | {mechs} |")
 head.append('')
md_path=pathlib.Path(args.output);md_path.parent.mkdir(parents=True, exist_ok=True)
md_path.write_text('\n'.join(head)+'\n',encoding='utf-8')
csv_path=pathlib.Path(args.csv);csv_path.parent.mkdir(parents=True, exist_ok=True)
with csv_path.open('w',encoding='utf-8',newline='') as out:
 writer=csv.writer(out,lineterminator='\n')
 writer.writerow(['category','sid','native_type','definition_origin','definition_cfg','spawn_mechanisms','spawn_cfg_paths'])
 for r in rows:
  writer.writerow([r['category'],r['sid'],r['type'],r['origin'],r['definition'],','.join(r['mechanisms']),';'.join(r['sources'])])
print(f'generated {len(rows)} SIDs: {md_path}, {csv_path}')

