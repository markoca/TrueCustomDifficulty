#!/usr/bin/env python3
"""Strict original CFG 2.0.5 trader buy-type/consumable audit (source only).

Requires both vanilla archives. Reads the existing 86-profile exact manifest,
TradePrototypes and ItemPrototypes, and checks every offer SID in the existing
source-graph CSV. Does not make or validate a BPatch or game-runtime changes.
"""
from __future__ import annotations

import argparse
import csv
import hashlib
import json
from collections import Counter, defaultdict
from pathlib import Path
import sys
import zipfile

from extract_trader_trade_inventory import (
    BASE_ZIP_SHA, DLC_ZIP_SHA, MEDICAL, FOOD, DRINKS,
    c, field, parse_cfg, sha256, sorted_nodes,
)

ROOT = Path(__file__).resolve().parents[1]
MANIFEST = ROOT / 'src/TrueCustomDifficulty/Data/trader-min-buy-condition-manifest.csv'
EXPECTED_TYPES = (
    'Weapon', 'Armor', 'Ammo', 'Grenade', 'Attach', 'Consumable',
    'MutantLoot', 'Artifact'
)
SELL_CATEGORY_TYPES = {
    'Weapons': 'EItemType::Weapon',
    'Armor': 'EItemType::Armor',
    'Attachments': 'EItemType::Attach',
    'Ammo': 'EItemType::Ammo',
    'Medical': 'EItemType::Consumable',
    'Food': 'EItemType::Consumable',
    'Drinks': 'EItemType::Consumable',
    'Artifacts': 'EItemType::Artifact',
    'Detectors': 'EItemType::Detector',
    'Grenades': 'EItemType::Grenade',
    'NightVision': 'EItemType::NightVisionGoggles',
    'Binoculars': 'EItemType::Binoculars',
}
FILES = {
    'base': {
        'trade': 'GameData/TradePrototypes.cfg',
        'items': 'GameData/ItemPrototypes.cfg',
    },
    'dlc1': {
        'trade': 'DLCGameData/DLC1/TradePrototypes.cfg',
        'items': 'DLCGameData/DLC1/ItemPrototypes.cfg',
    },
}


def save_csv(path, rows, keys):
    path.parent.mkdir(exist_ok=True, parents=True)
    with path.open('w', newline='', encoding='utf-8') as f:
        writer = csv.DictWriter(f, fieldnames=keys, lineterminator='\n')
        writer.writeheader()
        writer.writerows(rows)


def template_chain(scope, sid, item_roots):
    root = item_roots[scope].get(sid) or item_roots['base'].get(sid)
    if not root:
        return 'UNRESOLVED', '', '', '', 'ITEM_PROTOTYPE_NOT_FOUND_IN_EXTRACTED_ROOT'
    start=root
    seen=set()
    chain=[]
    currscope=scope if sid in item_roots[scope] else 'base'
    while root:
        key=(currscope,root['key'])
        if key in seen:
            raise ValueError(f'Cyclic template ref {sid}: {key}')
        seen.add(key)
        chain.append(currscope+':'+root['key'])
        if field(root,'Type'):
            return field(root,'Type'), field(start,'SortGroup'), ';'.join(chain), str(start['line']), 'RESOLVED'
        p=root['attrs'].get('refkey','')
        if not p:
            return 'UNRESOLVED',field(start,'SortGroup'),';'.join(chain),str(start['line']),'MISSING_PARENT_TYPE'
        if p in item_roots[currscope]:
            root=item_roots[currscope][p]
        elif p in item_roots['base']:
            root=item_roots['base'][p]
            currscope='base'
        else:
            return 'UNRESOLVED',field(start,'SortGroup'),';'.join(chain),str(start['line']),'PARENT_NOT_IN_EXTRACTED_ROOT'
    raise RuntimeError(sid)


def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base-zip',type=Path,required=True)
    parser.add_argument('--dlc1-zip',type=Path,required=True)
    parser.add_argument('--offers-csv',type=Path,required=True)
    parser.add_argument('--buy-csv',type=Path,default=ROOT/'docs/generators/trader-buy-limits-86.csv')
    parser.add_argument('--manifest',type=Path,default=MANIFEST)
    parser.add_argument('--out',type=Path,required=True)
    args=parser.parse_args(argv)
    checks=[('base',args.base_zip,BASE_ZIP_SHA),('dlc1',args.dlc1_zip,DLC_ZIP_SHA)]
    trade_roots={};item_roots={};provenance={}
    for scope,path,expected_hash in checks:
        actual=sha256(path)
        if actual!=expected_hash:
            raise ValueError(f'{scope} archive SHA256 drift: {actual}')
        with zipfile.ZipFile(path) as z:
            tdata=z.read(FILES[scope]['trade'])
            idata=z.read(FILES[scope]['items'])
        trade_roots[scope],_ = parse_cfg(tdata,FILES[scope]['trade'])
        item_roots[scope],_ = parse_cfg(idata,FILES[scope]['items'])
        provenance[scope]={
            'archive_sha256':actual,
            'trade_cfg_sha256':hashlib.sha256(tdata).hexdigest(),
            'item_cfg_sha256':hashlib.sha256(idata).hexdigest(),
            'trade_root_count':len(trade_roots[scope]),
            'item_root_count':len(item_roots[scope]),
        }
    with args.manifest.open(newline='',encoding='utf-8-sig') as f:
        manifest=list(csv.DictReader(f))
    if len(manifest)!=86:
        raise ValueError('Exactly 86 production buyer profiles required')
    unique={(r['scope'],r['prototype']) for r in manifest}
    if len(unique)!=86 or sum(s=='base' for s,_ in unique)!=68:
        raise ValueError('Production buyer manifest scope/key drift')
    with args.offers_csv.open(newline='',encoding='utf-8') as f:
        offers=list(csv.DictReader(f))
    if len(offers)!=2939 or len({r['item_sid'] for r in offers})!=164:
        raise ValueError('Expected source-graph seller census 2939 paths / 164 distinct item SIDs')
    if {(r['trade_scope'],r['trade_sid']) for r in offers} - unique:
        raise ValueError('Source seller paths have unknown trade profiles')
    with args.buy_csv.open(newline='',encoding='utf-8-sig') as f:
        previous_buy = {(r['scope'],r['trade_sid']): r for r in csv.DictReader(f)}
    if set(previous_buy)!=unique:
        raise ValueError('Previous 86-profile buy source ledger differs from manifest')
    groups={**{k:'Medical' for k in MEDICAL},**{k:'Food' for k in FOOD},**{k:'Drinks' for k in DRINKS}}
    if len(groups)!=15:
        raise ValueError('Expected 15 exact consumable SID groups')
    buys=[];limitation_patterns=Counter(); anomalies=[]
    for row in manifest:
        scope,sid=row['scope'],row['prototype']
        root=trade_roots[scope].get(sid)
        if not root:raise ValueError(f'{scope}:{sid}: no root')
        trade_generators=sorted_nodes(c(root,'TradeGenerators'))
        if len(trade_generators)!=1 or trade_generators[0]['key']!='[0]':
            raise ValueError(f'{scope}:{sid} multi TradeGenerator requires separate semantics')
        g=trade_generators[0]
        lim=c(g,'BuyLimitations')
        native=[]
        if lim:
            if lim['children']:raise ValueError(f'{scope}:{sid} nested blacklist entries')
            for ix,item in enumerate(sorted(lim['fields'].items(),key=lambda a:int(a[0].strip('[]')))):
                key, val=item
                if key!=f'[{ix}]' or not val.startswith('EItemType::'):
                    raise ValueError(f'{scope}:{sid}: irregular blacklist index or enum {key} {val}')
                v=val[len('EItemType::'):]
                if v not in EXPECTED_TYPES:raise ValueError(f'Unknown type in list {scope}:{sid}:{v}')
                native.append(v)
        if len(native)!=len(set(native)):
            raise ValueError(f'{scope}:{sid} duplicated blacklist type')
        limitation_patterns[';'.join(native) or '(none)']+=1
        prior=previous_buy[(scope,sid)]
        if prior['buy_limitations']!=';'.join(native) or prior['buy_limitations_state']!=('EXPLICIT' if lim else 'NOT_AUTHORED'):
            raise ValueError(f'Original BuyLimitations changed vs prior audit at {scope}:{sid}')
        inherited_from=root['attrs'].get('refkey','')
        if inherited_from and not c(root,'TradeGenerators'):
            raise ValueError(f'{scope}:{sid}: trade list inherited; effective tree not merged')
        columns={
            'scope':scope,'trade_sid':sid,'trader_type':prior['trader_type'],
            'root_cfg_line':root['line'],'trade_generator_cfg_line':g['line'],
            'refkey':inherited_from,
            'buy_limitations_state':'EXPLICIT' if lim else 'NOT_AUTHORED',
            'native_buy_limitations':';'.join(native),
            'native_type_count':len(native),
            'native_consumable_buy_gate':'BLOCKED' if 'Consumable' in native else 'NOT_EXPLICITLY_BLOCKED',
            'food_buy_gate':'BLOCKED' if 'Consumable' in native else 'NOT_EXPLICITLY_BLOCKED',
            'drinks_buy_gate':'BLOCKED' if 'Consumable' in native else 'NOT_EXPLICITLY_BLOCKED',
            'medical_buy_gate':'BLOCKED' if 'Consumable' in native else 'NOT_EXPLICITLY_BLOCKED',
            'weapon_condition_raw':field(g,'WeaponSellMinDurability'),
            'armor_condition_raw':field(g,'ArmorSellMinDurability'),
            'buy_modifier_raw':field(g,'BuyModifier'),
            'sell_modifier_raw':field(g,'SellModifier'),
            'buy_limitations_native_node_line':str(lim['line']) if lim else '',
            'item_type_buy_modifier_entries': ';'.join(field(m,'ItemType')+':'+field(m,'Modifier') for m in sorted_nodes(c(g,'ItemTypeBuyModifiers'))),
        }
        if any(v and v!=row['vanilla'] for v in (columns['weapon_condition_raw'],columns['armor_condition_raw'])):
            anomalies.append('explicit durability differs from manifest baseline at '+scope+':'+sid)
        buys.append(columns)
    if len(buys)!=86 or sum(x['buy_limitations_state']=='EXPLICIT' for x in buys)!=53:
        raise ValueError('86 buyer count / 53 list count drift')
    if any(r['food_buy_gate'] != r['medical_buy_gate'] or r['food_buy_gate'] != r['drinks_buy_gate'] for r in buys):
        raise ValueError('Consumable family inconsistent with EItemType')
    if sum(r['native_consumable_buy_gate']=='BLOCKED' for r in buys)!=16:
        raise ValueError('Unexpected number of consumables-blocking traders')
    # A source type is a property of an ItemPrototype, NOT the item-generator category.
    found={};summary=[];nonmatching=[]
    for sid in sorted({r['item_sid'] for r in offers}):
        scopes={r['trade_scope'] for r in offers if r['item_sid']==sid}
        itemcategory={r['item_category'] for r in offers if r['item_sid']==sid}
        if len(itemcategory)!=1:raise ValueError(f'Inventory-category classification differs per SID {sid}')
        category=next(iter(itemcategory))
        for scope in sorted(scopes):
            typ,sortgroup,chain,line,status=template_chain(scope,sid,item_roots)
            entry={
                'scope':scope,'item_sid':sid,'sell_stock_category':category,
                'native_item_type':typ,'sort_group':sortgroup,
                'item_cfg_line':line,'template_chain':chain,'source_resolution':status,
            }
            summary.append(entry)
            found[(scope,sid)]=entry
            wanted=SELL_CATEGORY_TYPES.get(category)
            if status=='RESOLVED' and typ!=wanted:
                nonmatching.append({**entry,'expected':wanted})
    if nonmatching:
        raise ValueError('Confirmed item-type classification mismatch '+repr(nonmatching[:8]))
    for sid,group in groups.items():
        rowset=[r for r in summary if r['item_sid']==sid]
        if not rowset:raise ValueError('missing stock consumable '+sid)
        if any(r['native_item_type'] != 'EItemType::Consumable' or r['source_resolution']!='RESOLVED' for r in rowset):
            raise ValueError(f'{sid} not native Consumable')
    unresolved=sorted({r['item_sid'] for r in summary if r['source_resolution']!='RESOLVED'})
    if unresolved != ['A0919A','DutyArmor_3_U1','DutyMask_1']:
        raise ValueError('Unresolved item-prototype sources changed: '+repr(unresolved))
    if sorted(x['trade_sid'] for x in buys if x['item_type_buy_modifier_entries'])!=['BolshakTechnician_TradePrototype']:
        raise ValueError('ItemTypeBuyModifiers production profile source drift')
    if next(x for x in buys if x['trade_sid']=='BolshakTechnician_TradePrototype')['item_type_buy_modifier_entries']!='EItemType::Info:1':
        raise ValueError('Bolshak Info price type/amount changed')
    if sorted(x['trade_sid'] for x in buys if x['weapon_condition_raw']=='0.0f' or x['armor_condition_raw']=='0.0f') != ['SelmaTrader_TradePrototype','Trader_Soviet_Rostok_TradePrototype']:
        raise ValueError('Vanilla zero-min-condition source exception changed')
    bytype=Counter((r['trader_type'],r['native_consumable_buy_gate']) for r in buys)
    args.out.mkdir(parents=True,exist_ok=True)
    save_csv(args.out/'trader-native-buy-consumable-gates-86.csv',buys,list(buys[0]))
    save_csv(args.out/'trader-offered-item-native-types.csv',summary,list(summary[0]))
    consumer=[r for r in summary if r['item_sid'] in groups]
    consumer=sorted(consumer,key=lambda a:(groups[a['item_sid']],a['item_sid'],a['scope']))
    for row in consumer:row['consumable_group']=groups[row['item_sid']]
    save_csv(args.out/'trader-consumable-native-type-evidence.csv',consumer,['scope','item_sid','consumable_group','native_item_type','sort_group','item_cfg_line','template_chain','source_resolution','sell_stock_category'])
    result={
        'provenance':provenance,
        'production_profiles':len(buys),
        'trade_generators_per_profile':'one [0] each',
        'explicit_buy_lists':sum(r['buy_limitations_state']=='EXPLICIT' for r in buys),
        'missing_buy_lists':sum(r['buy_limitations_state']=='NOT_AUTHORED' for r in buys),
        'limitation_patterns':dict(sorted(limitation_patterns.items())),
        'consumable_blocked_traders':sum(r['native_consumable_buy_gate']=='BLOCKED' for r in buys),
        'consumable_gate_by_role':{f'{k[0]}:{k[1]}':v for k,v in sorted(bytype.items())},
        'consumable_sid_count_by_group':dict(Counter(groups.values())),
        'consumable_inherited_type':'EItemType::Consumable (all 15 candidate SIDs)',
        'offered_unique_sids':164,
        'unique_item_sid_scope_pairs':len(summary),
        'item_prototype_not_found_in_extracted_files':unresolved,
        'item_type_mismatches_for_resolved_items':nonmatching,
        'durability_explicit_vs_manifest_anomalies':anomalies,
        'durability_fields_missing_in_native_cfg':sum(not r['weapon_condition_raw'] or not r['armor_condition_raw'] for r in buys),
        'durability_explicit_zero_overrides': sorted(x['trade_sid'] for x in buys if x['weapon_condition_raw']=='0.0f' or x['armor_condition_raw']=='0.0f'),
        'item_type_buy_modifier_profiles':[{ 'scope':x['scope'], 'trade_sid':x['trade_sid'], 'entries':x['item_type_buy_modifier_entries']} for x in buys if x['item_type_buy_modifier_entries']],
        'limitations_model':'native blacklist applies to broad EItemType, not SortGroup or item SID; no proof of gameplay in runtime',
    }
    (args.out/'trader-native-buy-audit-summary.json').write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    print(json.dumps(result,indent=2,ensure_ascii=False))
    if sorted(anomalies)!=['explicit durability differs from manifest baseline at base:SelmaTrader_TradePrototype','explicit durability differs from manifest baseline at base:Trader_Soviet_Rostok_TradePrototype']:
        raise ValueError('Durability manifest/source differences changed: '+repr(anomalies))
    return 0

if __name__=='__main__':
    sys.exit(main())
