import re,sys,collections
def parse(path):
    root=[]; stack=[('ROOT',root)]
    for raw in open(path,encoding='utf-8-sig'):
        line=raw.strip()
        if not line or line.startswith('//'): continue
        m=re.match(r'^(\S+)\s*:\s*struct\.begin',line)
        if m:
            node=(m.group(1),[]); stack[-1][1].append(node); stack.append(node); continue
        if line.startswith('struct.end'):
            stack.pop(); continue
        m=re.match(r'^(\S+)\s*=\s*(.*)$',line)
        if m: stack[-1][1].append((m.group(1),m.group(2).strip()))
    return root
def get(node,key):
    for k,v in node[1]:
        if k==key: return (k,v)
def kids(node): return [c for c in node[1] if isinstance(c[1],list)]
def val(node,key):
    for k,v in node[1]:
        if k==key and not isinstance(v,list): return v
def rows(path):
    out=[]
    for proto in parse(path):
        name=proto[0]
        preset=get(proto,'Preset')
        if not preset: continue
        pif=get(preset,'PossibleInhabitantFactions')
        if not pif: continue
        for fac in kids(pif):
            faction=val(fac,'Faction')
            ranks=get(fac,'SpawnSettingsPerPlayerRanks')
            if not ranks: continue
            for r in kids(ranks):
                mx=val(r,'MaxSpawnQuantity')
                arch=get(r,'SpawnSettingsPerArchetypes')
                mins=sum(int(float(val(a,'MinQuantityPerArchetype') or 0)) for a in kids(arch)) if arch else 0
                out.append(dict(proto=name,node=fac[0],faction=faction,rank=r[0],max=mx,minsum=mins,sid=val(proto,'SID')))
    return out
