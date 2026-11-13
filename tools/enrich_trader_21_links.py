#!/usr/bin/env python3
"""Attach exact 2.1 nested generator parent slots to the trader leaf graph."""
import csv
import io
import sys
import zipfile
from collections import Counter
from extract_trader_21_graph import parse

def read_generators(source):
    result={}
    with zipfile.ZipFile(source) as outer:
        for member in outer.namelist():
            scope='dlc1' if 'DLC1' in member else 'base'
            with zipfile.ZipFile(io.BytesIO(outer.read(member))) as inner:
                path=('DLCGameData/DLC1/' if scope=='dlc1' else 'GameData/')+'ItemGeneratorPrototypes.cfg'
                for root in parse(inner.read(path).decode('utf-8-sig')):
                    key=(scope,root['attrs'].get('SID',root['key']))
                    if key in result:raise ValueError('Duplicate generator '+str(key))
                    result[key]=root
    return result

def links(node):
    for group in node['children']:
        if group['key']!='ItemGenerator':continue
        for category in group['children']:
            for possible in category['children']:
                if possible['key']!='PossibleItems':continue
                for item in possible['children']:
                    a=item['attrs'];child=a.get('ItemGeneratorPrototypeSID','')
                    if child and child!='empty':
                        path=f'ItemGenerator/{category["key"]}/PossibleItems/{item["key"]}'
                        yield child,path,a.get('Chance',''),a.get('Weight','')

def enrich(source,graph,output):
    gens=read_generators(source)
    with open(graph,encoding='utf-8-sig',newline='') as stream:
        reader=csv.DictReader(stream)
        fields=list(reader.fieldnames or [])
        rows=list(reader)
    edges={}
    for (scope,sid),node in gens.items():
        for child,path,chance,weight in links(node):
            edges.setdefault((scope,sid,child),[]).append((path,chance,weight))
    for row in rows:
        chain=row['nested_chain'].split('>')
        if not chain or chain[0]!=row['root_sid'] or chain[-1]!=row['generator_sid']:
            raise ValueError('Invalid generator chain')
        if len(chain)>2:raise ValueError('Nested depth >2 unsupported')
        if len(chain)==1:parent=''
        else:
            scope=row['trade_scope'];root=chain[0];child=chain[1]
            if (scope,root) not in gens:scope='base'
            matches=edges.get((scope,root,child),[])
            if not matches:raise ValueError('Unresolved parent '+str((scope,root,child)))
            parent='|'.join(f'{root}:{path}:chance={chance}:weight={weight}'
                            for path,chance,weight in matches)
        row['parent_link_coordinates']=parent
        row['parent_link_count']=str(len(parent.split('|')) if parent else 0)
    if len(rows)!=3150:raise ValueError('2.1 leaf count mismatch')
    with open(output,'w',newline='',encoding='utf-8') as stream:
        writer=csv.DictWriter(stream,fieldnames=fields+['parent_link_coordinates','parent_link_count'])
        writer.writeheader();writer.writerows(rows)
    print('Parent slots:',dict(Counter(int(r['parent_link_count']) for r in rows)))

if __name__=='__main__':
    if len(sys.argv)!=4:
        sys.exit('Usage: enrich_trader_21_links.py SOURCE.zip GRAPH.csv OUTPUT.csv')
    enrich(*sys.argv[1:])
