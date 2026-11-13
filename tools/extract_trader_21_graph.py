#!/usr/bin/env python3
"""Reproduce the 2.1 trader offer graph from a nested CFG dump (stdlib only)."""
import zipfile, io, re, csv, collections, sys
if len(sys.argv)!=3:
    sys.exit("Usage: extract_trader_21_graph.py SOURCE_ZIP OUTPUT_CSV")
source=sys.argv[1]
rxstart=re.compile(r'^\s*([^\s:]+)\s*:\s*struct\.begin')
rxassign=re.compile(r'^\s*([\w]+)\s*=\s*(.*?)\s*$')
def parse(text):
    roots=[];stack=[]
    for n,line in enumerate(text.splitlines(),1):
        m=rxstart.match(line)
        if m:
            node={'key':m.group(1),'attrs':{},'children':[],'line':n}
            if stack:stack[-1]['children'].append(node)
            else:roots.append(node)
            stack.append(node)
        elif 'struct.end' in line:
            if stack:stack.pop()
        elif stack:
            a=rxassign.match(line)
            if a:stack[-1]['attrs'][a.group(1)]=a.group(2)
    return roots
def descendants(n):
    yield n
    for c in n['children']:yield from descendants(c)
def sid(n):return n['attrs'].get('SID',n['key'])
trades={};gens={}
with zipfile.ZipFile(source) as z:
    for name in z.namelist():
        scope='dlc1' if 'DLC1' in name else 'base'
        with zipfile.ZipFile(io.BytesIO(z.read(name))) as inner:
            for filename,store in [('TradePrototypes.cfg',trades),('ItemGeneratorPrototypes.cfg',gens)]:
                path=('DLCGameData/DLC1/' if scope=='dlc1' else 'GameData/')+filename
                for n in parse(inner.read(path).decode('utf-8-sig')):
                    key=(scope,sid(n))
                    if key in store:raise ValueError('Duplicate scoped SID: '+str(key))
                    store[key]=n
rows=[];unresolved=[];cycles=[]
def locate(scope,key):
    if (scope,key) in gens:return scope,gens[(scope,key)]
    if ('base',key) in gens:return 'base',gens[('base',key)]
    return None,None
def walk(scope,trade,root,gen,chain):
    gs,g=locate(scope,gen)
    if g is None:
        unresolved.append((scope,trade,gen));return
    if (gs,gen) in chain:
        cycles.append((scope,trade,gen));return
    chain=chain+((gs,gen),)
    for group in g['children']:
        if group['key']!='ItemGenerator':continue
        for cat in group['children']:
            category=cat['attrs'].get('Category','')
            for pi in cat['children']:
                if pi['key']!='PossibleItems':continue
                for item in pi['children']:
                    a=item['attrs']
                    child=a.get('ItemGeneratorPrototypeSID','')
                    item_sid=a.get('ItemPrototypeSID','')
                    path=f'ItemGenerator/{cat["key"]}/PossibleItems/{item["key"]}'
                    if item_sid and item_sid!='empty':
                        rows.append([scope,trade,root,gs,gen,path,category,item_sid,
                                     a.get('Chance',''),a.get('Weight',''),
                                     '>'.join(k for _,k in chain),child])
                    if child and child!='empty':walk(gs,trade,root,child,chain)
for (scope,trade),n in trades.items():
    for node in descendants(n):
        root=node['attrs'].get('ItemGeneratorPrototypeSID')
        if root and root!='empty':walk(scope,trade,root,root,())
if len(trades)!=92 or len(gens)!=3452 or len(rows)!=3150 or unresolved or cycles:
    raise ValueError('2.1 graph census mismatch or broken generator references: '
                     +str((len(trades),len(gens),len(rows),unresolved[:3],cycles[:3])))
with open(sys.argv[2],'w',newline='',encoding='utf-8') as out:
    writer=csv.writer(out)
    writer.writerow(['trade_scope','trade_sid','root_sid','generator_scope','generator_sid',
                     'entry_path','source_category','item_sid','chance','weight',
                     'nested_chain','child_ref'])
    writer.writerows(rows)
print('Profiles:',len(trades),'Generators:',len(gens),'Leaf occurrences:',len(rows))
print('Source categories:',dict(collections.Counter(r[6] for r in rows)))
