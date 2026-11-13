#!/usr/bin/env python3
"""Join exact 2.1 TradeGenerators slots/conditions onto staged trader leaves."""
import csv, io, sys, zipfile
from pathlib import Path
from enrich_trader_21_links import parse

def run(source, staging, output):
    slots = {}
    with zipfile.ZipFile(source) as outer:
        for name in outer.namelist():
            scope = 'dlc1' if 'DLC1' in name else 'base'
            with zipfile.ZipFile(io.BytesIO(outer.read(name))) as inner:
                path = ('DLCGameData/DLC1/' if scope == 'dlc1' else 'GameData/')+'TradePrototypes.cfg'
                for trade in parse(inner.read(path).decode('utf-8-sig')):
                    sid = trade['attrs'].get('SID',trade['key'])
                    for block in trade['children']:
                        if block['key'] != 'TradeGenerators': continue
                        for slot in block['children']:
                            root = slot['attrs'].get('ItemGeneratorPrototypeSID','')
                            if not root: continue
                            key = slot['key']
                            if not (key.startswith('[') and key.endswith(']')):
                                raise ValueError('Invalid trade slot: '+key)
                            slots.setdefault((scope,sid,root),[]).append(
                                (int(key[1:-1]),slot['attrs'].get('ConditionSID','')))
    with open(staging,encoding='utf-8-sig',newline='') as f:
        reader=csv.DictReader(f);fields=list(reader.fieldnames or []);rows=list(reader)
    result=[]
    for row in rows:
        key=(row['trade_scope'],row['trade_sid'],row['root_sid'])
        matched=slots.get(key)
        if not matched: raise ValueError('Unmapped source trade slot: '+repr(key))
        for slot,condition in matched:
            result.append({**row,'trade_generator_slot':str(slot),
                           'trade_generator_condition':condition})
    if len(rows)!=3150 or len(result)!=3150:
        raise ValueError('2.1 source leaf count or duplicate root-slot mapping drift')
    counts={slot:sum(int(r['trade_generator_slot'])==slot for r in result)
            for slot in {int(r['trade_generator_slot']) for r in result}}
    if counts!={0:3143,1:7}:
        raise ValueError('2.1 source slot census drift: '+repr(counts))
    with open(output,'w',encoding='utf-8',newline='') as f:
        writer=csv.DictWriter(f,fieldnames=fields+[
            'trade_generator_slot','trade_generator_condition'])
        writer.writeheader();writer.writerows(result)
    print('2.1 trader slot census OK:',counts)

if __name__=='__main__':
    if len(sys.argv)!=4:
        sys.exit('Usage: enrich_trader_21_slots.py 2.1.zip staging.csv slot-manifest.csv')
    run(*sys.argv[1:])
