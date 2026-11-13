#!/usr/bin/env python3
"""Build a source-validated, slot-aware 2.1 SELL runtime CSV.

Input: the original nested 2.1 CFG archive, 3,150-row slot CSV and reviewed
role reconciliation. Refuse ambiguous generator scopes and missing profiles.
"""
import csv
import io
import sys
import zipfile
from collections import Counter
from pathlib import Path
from extract_trader_21_graph import parse

COLUMNS = ('trade_scope,trade_sid,trader_type,root_stock_sid,item_sid,item_category,'
           'generator_sid,generator_scope,entry_cfg_path,direct_entry_state,'
           'nested_chain,outer_links,trade_generator_slot,trade_generator_condition,'
           'root_generator_scope').split(',')

def generator_scopes(archive):
    scopes = {}
    with zipfile.ZipFile(archive) as outer:
        for name in outer.namelist():
            scope = 'dlc1' if 'DLC1' in name else 'base'
            with zipfile.ZipFile(io.BytesIO(outer.read(name))) as inner:
                path = ('DLCGameData/DLC1/' if scope == 'dlc1' else 'GameData/') + 'ItemGeneratorPrototypes.cfg'
                for node in parse(inner.read(path).decode('utf-8-sig')):
                    sid = node['attrs'].get('SID',node['key'])
                    scopes.setdefault(sid,set()).add(scope)
    return scopes

def build(archive, slot_csv, roles_csv, output):
    scopes = generator_scopes(archive)
    with open(roles_csv, newline='', encoding='utf-8-sig') as f:
        role_rows = list(csv.DictReader(f))
    roles = {(r['scope'],r['trade_sid']):r['trader_type'] for r in role_rows}
    if len(roles)!=len(role_rows) or len(roles)!=90:
        raise ValueError('Expected 90 unique reviewed 2.1 trade role entries')
    with open(slot_csv, newline='', encoding='utf-8-sig') as f:
        rows=list(csv.DictReader(f))
    if len(rows)!=3150:raise ValueError('2.1 leaf census mismatch')
    output_rows=[]
    unresolved=set()
    for row in rows:
        trade_scope=row['trade_scope']
        trade_sid=row['trade_sid']
        root=row['root_sid']
        generator=row['generator_sid']
        scope_set=scopes.get(root,set())
        if len(scope_set)!=1:
            raise ValueError('Ambiguous or missing root generator scope: '+repr((trade_sid,root,scope_set)))
        generator_scopes_set=scopes.get(generator,set())
        if row['generator_scope'] not in generator_scopes_set:
            raise ValueError('Missing leaf generator scope: '+repr((generator,row['generator_scope'])))
        role=roles.get((trade_scope,trade_sid))
        if role is None:raise ValueError('Missing trade role mapping: '+trade_sid)
        if role=='UNRESOLVED':unresolved.add((trade_scope,trade_sid))
        chain=row['nested_chain'].split('>')
        if len(chain) not in (1,2) or chain[0]!=root or chain[-1]!=generator:
            raise ValueError('Invalid source generator chain: '+trade_sid)
        parent=row['parent_link_coordinates']
        if (len(chain)==1)!= (not parent):
            raise ValueError('Invalid source parent link: '+trade_sid)
        slot=int(row['trade_generator_slot'])
        if slot<0 or slot>1:raise ValueError('Unexpected source slot')
        output_rows.append(dict(zip(COLUMNS,[
            trade_scope,trade_sid,role,root,row['item_sid'],row['category'],
            generator,row['generator_scope'],row['entry_path'],
            'ZERO_CHANCE' if float(row['chance'] or 1)>0 is False else 'CANDIDATE',
            row['nested_chain'],parent,str(slot),row['trade_generator_condition'],
            next(iter(scope_set))
        ])))
    if len({(r['trade_scope'],r['trade_sid']) for r in output_rows})!=90:
        raise ValueError('2.1 stocked profile census mismatch')
    if Counter(int(r['trade_generator_slot']) for r in output_rows)!={0:3143,1:7}:
        raise ValueError('2.1 slot census mismatch')
    if unresolved:
        raise ValueError('Unreviewed 2.1 trade roles; refusing production manifest: '+repr(sorted(unresolved)))
    with open(output,'w',newline='',encoding='utf-8') as f:
        writer=csv.DictWriter(f,fieldnames=COLUMNS,lineterminator='\n')
        writer.writeheader()
        writer.writerows(output_rows)
    print('2.1 production-ready source manifest:',len(output_rows),'rows')

if __name__=='__main__':
    if len(sys.argv)!=5:
        sys.exit('Usage: build_trader_21_runtime.py SOURCE.zip SLOTS.csv ROLES.csv OUTPUT.csv')
    build(*sys.argv[1:])
