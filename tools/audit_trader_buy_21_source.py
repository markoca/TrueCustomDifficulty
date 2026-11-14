#!/usr/bin/env python3
"""Source-exact 2.1 native Trader BUY audit; game acceptance is separate."""
from __future__ import annotations
import argparse
import csv
import hashlib
import io
import re
import zipfile
from collections import Counter
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
DATA=ROOT/'src/TrueCustomDifficulty/Data'
BUY=DATA/'trader-buy-policy-manifest.csv'
MIN=DATA/'trader-min-buy-condition-manifest.csv'
SELL=DATA/'trader-sells-2.1-runtime.csv'
FILES={'base':'GameData/TradePrototypes.cfg','dlc1':'DLCGameData/DLC1/TradePrototypes.cfg'}
INNER={'base':'STALKER2-GameData-CFG.zip','dlc1':'STALKER2-DLC1-CFG.zip'}
SOURCE_SHA={'base':'c5115350d67d91a329b5922f0a51e9abe02f91075e5fbe24c1b3b4fa48f540eb',
            'dlc1':'7a72b5dfc112226c92c081e49ca6c3877b7cf00cd541964d07113253d37e31a5'}
BUY_LEDGER_SHA='51a68cb8ae6de4e15157fba5e2414241bc1954ac92e80fc23dca08eb403fed5f'
ROLE={'general':'GENERAL_TRADER','medic':'MEDIC','technician':'TECHNICIAN',
      'bartender':'BARTENDER','genericNpc':'GENERIC_NPC_TRADER'}
TYPES={'Weapon','Armor','Ammo','Grenade','Attach','Consumable','MutantLoot','Artifact'}
START=re.compile(r'^\s*(.*?)\s*:\s*struct\.begin')
FIELD=re.compile(r'^\s*([^=]+?)\s*=\s*(.*?)\s*$')

def read(path):
    with path.open(encoding='utf-8-sig',newline='') as f:return list(csv.DictReader(f))

def source_tree(raw):
    stack=[];result={};current=None
    for line in raw.decode('utf-8-sig').splitlines():
        m=START.match(line)
        if m:
            stack.append(m.group(1).strip())
            if len(stack)==1:
                current=stack[0]
                assert current not in result, 'Duplicate TradePrototype '+current
                result[current]={}
        elif line.strip()=='struct.end':
            assert stack, 'Unbalanced struct.end'
            stack.pop()
        elif stack:
            m=FIELD.match(line)
            if m:
                fields=result[current].setdefault(tuple(stack[1:]),{})
                key=m.group(1).strip()
                assert key not in fields, 'Duplicate source field '+current+':'+key
                fields[key]=m.group(2).strip()
    assert not stack, 'Unterminated CFG root'
    return result

def native_limits(profile):
    raw=profile.get(('TradeGenerators','[0]','BuyLimitations'))
    if raw is None:return 'NOT_AUTHORED',''
    result=[]
    for i in range(len(raw)):
        value=raw.get(f'[{i}]')
        assert value and value.startswith('EItemType::'), 'Sparse/invalid native BuyLimitations'
        name=value.removeprefix('EItemType::')
        assert name in TYPES and name not in result, 'Invalid or duplicate native buy type'
        result.append(name)
    assert result, 'Empty explicit native list needs separate proof'
    return 'EXPLICIT',';'.join(result)

def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source',type=Path,help='Original outer STALKER2-2.1 cfg dump.zip')
    args=parser.parse_args()
    buyers=read(BUY);minimum=read(MIN);sellers=read(SELL)
    assert len(buyers)==len(minimum)==86 and len(sellers)==2939
    assert Counter(x['scope'] for x in buyers)=={'base':68,'dlc1':18}
    assert Counter(x['role'] for x in buyers)=={'general':33,'medic':15,
                'technician':16,'bartender':13,'genericNpc':9}
    assert Counter(x['state'] for x in buyers)=={'EXPLICIT':53,'NOT_AUTHORED':33}
    key=lambda x:(x['scope'],x['prototype'])
    bmap={key(x):x for x in buyers};mmap={key(x):x for x in minimum}
    assert len(bmap)==len(mmap)==86 and set(bmap)==set(mmap)
    smap={}
    for row in sellers:
        k=row['trade_scope'],row['trade_sid']
        assert k not in smap or smap[k]==row['trader_type'], 'SELL role ambiguity'
        smap[k]=row['trader_type']
    assert len(smap)==86 and set(bmap)==set(smap), 'BUY/SELL scope coverage differs'
    for k,record in bmap.items():
        assert record['file']==FILES[k[0]] and ROLE[record['role']]==smap[k]
        types=record['blocked_types'].split(';') if record['blocked_types'] else []
        assert len(types)==len(set(types)) and set(types)<=TYPES
        assert (record['state']=='EXPLICIT')==bool(types)
    assert all(row['file']==FILES[k[0]] and row['node_path']=='TradeGenerators/[0]'
               and row['weapon_key']=='WeaponSellMinDurability'
               and row['armor_key']=='ArmorSellMinDurability'
               and row['vanilla'] in ('0.4f','0.0f') for k,row in mmap.items())
    assert {k for k,x in mmap.items() if x['vanilla']=='0.0f'}=={
        ('base','SelmaTrader_TradePrototype'),('base','Trader_Soviet_Rostok_TradePrototype')}
    payload='\n'.join(sorted('|'.join((x['scope'],x['file'],x['prototype'],
                                  x['state'],x['blocked_types'])) for x in buyers))+'\n'
    assert hashlib.sha256(payload.encode('utf-8')).hexdigest()==BUY_LEDGER_SHA, '2.1 BUY source ledger drift'
    if args.source:
        with zipfile.ZipFile(args.source) as outer:
            natives={}
            for scope in FILES:
                with zipfile.ZipFile(io.BytesIO(outer.read(INNER[scope]))) as inner:
                    raw=inner.read(FILES[scope])
                assert hashlib.sha256(raw).hexdigest()==SOURCE_SHA[scope], 'Original 2.1 source SHA drift'
                natives.update({(scope,sid):data for sid,data in source_tree(raw).items()})
        assert len(natives)==92 and len(bmap)==86, 'Unexpected 2.1 trade profile census'
        for k,row in bmap.items():
            assert k in natives, 'Missing 2.1 BUY profile '+str(k)
            profile=natives[k]
            assert ('TradeGenerators','[0]') in profile
            assert profile[('TradeGenerators','[0]')].get('ConditionSID')=='ConstTrue'
            assert native_limits(profile)==(row['state'],row['blocked_types']), 'Original BUY mismatch: '+str(k)
        unauthored=0
        for k,row in mmap.items():
            fields=natives[k][('TradeGenerators','[0]')]
            values=fields.get('WeaponSellMinDurability'),fields.get('ArmorSellMinDurability')
            if values==(None,None):
                assert row['vanilla']=='0.4f', 'Unexpected legacy fallback: '+str(k)
                unauthored+=1
            else:
                assert values==(row['vanilla'],row['vanilla']), 'Authored native durability differs: '+str(k)
        assert unauthored==22, 'Original un-authored durability count drift'
        print('PASS: 2.1 original trade CFG hashes, 86 physical BuyLimitations, 64 authored/22 un-authored durability')
    else:
        print('NOT RUN: raw 2.1 ZIP parity; use --source to check physical CFGs')
    print('PASS: 86 BUY/Sells role pairs, 53 exact native lists, pinned source ledger')
    print('NOT VERIFIED: gameplay acceptance or native default for 22 un-authored durability fields')
if __name__=='__main__':
    main()
