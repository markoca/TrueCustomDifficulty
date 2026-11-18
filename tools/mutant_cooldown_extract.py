"""Own cooldown of every mutant ability in AbilityPrototypes.cfg (base + DLC1), refkey-resolved."""
import re,sys,collections,json
def parse(path):
    tops={}; order=[]
    stack=[]
    for raw in open(path,encoding='utf-8-sig',errors='replace'):
        line=raw.split('//')[0].rstrip() if '//' in raw and 'Script' not in raw else raw.rstrip()
        s=line.strip()
        if not s: continue
        m=re.match(r'^(\S+)\s*:\s*struct\.begin(.*)$',s)
        if m:
            node={'name':m.group(1),'kv':[],'kids':[],'ref':None}
            r=re.search(r'refkey=([^;}\s]+)',m.group(2))
            if r: node['ref']=r.group(1)
            if stack: stack[-1]['kids'].append(node)
            else: tops[node['name']]=node; order.append(node['name'])
            stack.append(node); continue
        if s.startswith('struct.end'):
            if stack: stack.pop(); continue
        m=re.match(r'^(\S+)\s*=\s*(.*)$',s)
        if m and stack: stack[-1]['kv'].append((m.group(1),m.group(2)))
    return tops,order
def v(n,k):
    for a,b in n['kv']:
        if a==k: return b
def kid(n,k):
    for c in n['kids']:
        if c['name']==k: return c
def resolve(tops,name,field,depth=0):
    n=tops.get(name)
    if not n or depth>10: return None
    if field=='CooldownTag':
        x=v(n,'CooldownTag')
        if x: return x
    else:
        x=kid(n,'TriggeredCooldowns')
        if x: return x
    if n['ref'] and n['ref']!='[0]': return resolve(tops,n['ref'],field,depth+1)
fams={'Pseudog':'pseudodog','Cat':'bayun','Bayun':'bayun','BlindDog':'blinddog','Blinddog':'blinddog','Bloodsucker':'bloodsucker','Boar':'boar','Burer':'burer','Chimera':'chimera','Controller':'controller','Deer':'deer','Flesh':'flesh','Poltergeist':'poltergeist','PseudoDog':'pseudodog','Pseudodog':'pseudodog','Pseudogiant':'pseudogiant','Snork':'snork','Tushkan':'tushkan'}
def extract(base_path, dlc_path):
  out=[]
  for src,path in [('base',base_path),('dlc',dlc_path)]:
      tops,order=parse(path)
      for name in order:
          if name.startswith('Cancel'): continue
          parts=re.split(r'_',name)
          fam=None
          for p in parts:
              for k,f in fams.items():
                  if p==k or p.startswith(k) and p[len(k):] in ('Summon','CombatSummon','RetreatSummon','Clone','Collar') : fam=f
              if fam: break
          if not fam:
              for k,f in sorted(fams.items(),key=lambda x:-len(x[0])):
                  if re.search(k,name): fam=f;break
          if not fam: continue
          tag=resolve(tops,name,'CooldownTag')
          tc=resolve(tops,name,'TC')
          if not tag or not tc: continue
          for e in tc['kids']:
              if v(e,'CooldownTag')==tag:
                  out.append(dict(src=src,fam=fam,ability=name,tag=tag.replace('Ability.Cooldown.',''),dur=float(v(e,'Duration').rstrip('f').rstrip('.') or 0)))
  return out


def agent_cooldown_tags(obj_path, ability_path, sid):
    """Own cooldown tags of every ability in an agent's ObjPrototypes ability list (refkey-resolved)."""
    objs, _ = parse(obj_path)
    abil, _ = parse(ability_path)

    def sids(name, depth=0):
        n = objs.get(name)
        if not n or depth > 8:
            return set()
        out = set()
        a = kid(n, 'Abilities')
        if a:
            out |= {v(c, 'SID') for c in a['kids'] if v(c, 'SID')}
        if n['ref'] and n['ref'] != '[0]':
            out |= sids(n['ref'], depth + 1)
        return out

    return {(resolve(abil, s, 'CooldownTag') or '').replace('Ability.Cooldown.', '') for s in sids(sid)} - {''}
