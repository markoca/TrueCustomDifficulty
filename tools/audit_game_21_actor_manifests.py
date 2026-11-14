#!/usr/bin/env python3
"""Compare all production actor CSV references with the original 2.0->2.1 actor delta.

Requires BOTH actual outer game dump archives. No silent static-only PASS:
without them the script reports NOT RUN and returns a nonzero exit status.
This is source compatibility, not in-game or new-actor coverage.
"""
from __future__ import annotations
import argparse
import io
import re
import zipfile
from collections import Counter, defaultdict
from pathlib import Path

ROOT=Path(__file__).resolve().parents[1]
DATA=ROOT/'src/TrueCustomDifficulty/Data'
FILES={'base':('STALKER2-GameData-CFG.zip','GameData/SpawnActorPrototypes.cfg'),
       'dlc':('STALKER2-DLC1-CFG.zip','DLCGameData/DLC1/SpawnActorPrototypes.cfg')}
ROOTS=re.compile(rb'(?m)^([0-9A-Fa-f]{32})\s*:\s*struct\.begin[^\r\n]*')
HEX=re.compile(r'(?<![0-9A-Fa-f])([0-9A-Fa-f]{32})(?![0-9A-Fa-f])')
WATCH=('StashPrototypeSID','ItemGeneratorSettings','EnableSmartLootIfPossible','SpawnedPrototypeSID')

def actors(archive,scope):
    inner,filename=FILES[scope]
    with zipfile.ZipFile(archive) as outer:
        with zipfile.ZipFile(io.BytesIO(outer.read(inner))) as child:
            data=child.read(filename)
    starts=list(ROOTS.finditer(data));assert starts,scope+' has no actor roots'
    values={}
    for i,match in enumerate(starts):
        sid=match.group(1).decode('ascii').upper()
        end=starts[i+1].start() if i+1<len(starts) else len(data)
        lines=data[match.end():end].decode('utf-8',errors='replace').splitlines()
        payload=tuple(line.strip() for line in lines if any(k in line for k in WATCH))
        assert sid not in values,'Duplicate native actor '+sid
        values[sid]=payload
    return values

def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--old',required=True,type=Path)
    p.add_argument('--new',required=True,type=Path)
    args=p.parse_args()
    delta={}
    for scope in FILES:
        before=actors(args.old,scope);after=actors(args.new,scope)
        removed=before.keys()-after.keys()
        changed={k for k in before.keys()&after.keys() if before[k]!=after[k]}
        added=after.keys()-before.keys()
        assert (len(removed),len(changed),len(added))==(
            (16,87,205) if scope=='base' else (8,0,6)),scope+' source delta changed'
        delta[scope]={'removed':removed,'changed':changed,'added':added}
    dangerous=set().union(*(v['removed']|v['changed'] for v in delta.values()))
    files=sorted(DATA.glob('*.csv'))
    assert len(files)==46,'Production manifest count changed'
    conflicts=defaultdict(list)
    for path in files:
        ids=set(x.upper() for x in HEX.findall(path.read_text(encoding='utf-8-sig')))
        for sid in sorted(ids&dangerous):conflicts[path.name].append(sid)
    assert not conflicts,'Actor source changes intersect TCD production manifests: '+repr(dict(conflicts))
    print('PASS: 2.1 native actor roots: removed=24, watched-changed=87, new=211')
    print('PASS: 46 production CSVs: no changed/removed actor GUID intersection')
    print('NOT VERIFIED: newly-added actor eligibility, non-CSV route semantics, runtime BPatch/game')

if __name__=='__main__':main()
