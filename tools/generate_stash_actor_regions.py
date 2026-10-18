#!/usr/bin/env python3
"""Rebuild region-labelled ordinary physical stash inventory from tracked source manifest."""
import csv
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
SOURCE=ROOT/'src/TrueCustomDifficulty/Data/smartloot-regular-world-actor-manifest.csv'
OUTPUT=ROOT/'src/TrueCustomDifficulty/Data/stash-regular-actor-regions.csv'
ALIASES={"LesserZone":"Lesser Zone","Cordon":"Cordon","Garbage":"Garbage","WildIsland":"Wild Island","Wild":"Wild Island","Zaton":"Zaton","Backwater":"Zaton","RedForest":"Red Forest","BurntForest":"Burnt Forest","Yantar":"Yantar","CementFactory":"Cement Factory","CoolingTowers":"Cooling Towers","ChemicalPlant":"Chemical Plant","Promzona":"Chemical Plant","Malachite":"Malachite","STCMalachite":"Malachite","Swamps":"Swamps","Swamp":"Swamps","Duga":"Duga","DugaRadar":"Duga","Jupiter":"Jupiter","Rostok":"Rostok","SIRCAA":"SIRCAA","SIIRCA":"SIRCAA","CityCenter":"Rostok"}
FIELDS=['ActorSID','Region','RegionEvidence','OriginalItemGeneratorSIDs','DLC','X','Y','Z']

def main():
    with SOURCE.open(newline='',encoding='utf-8') as f: source=list(csv.DictReader(f))
    rows=[]
    for actor in source:
        labels={ALIASES[p.split('_',1)[0]] for p in actor['OriginalItemGeneratorSIDs'].split(',') if p.split('_',1)[0] in ALIASES}
        region=next(iter(labels)) if len(labels)==1 else 'UNRESOLVED'
        rows.append(dict(ActorSID=actor['ActorSID'],Region=region,RegionEvidence='GENERATOR_SID_PREFIX' if region!='UNRESOLVED' else 'NO_EXPLICIT_REGION_PREFIX',OriginalItemGeneratorSIDs=actor['OriginalItemGeneratorSIDs'],DLC=actor['DLC'],X=actor['X'],Y=actor['Y'],Z=actor['Z']))
    import io
    buffer=io.StringIO(newline='')
    writer=csv.DictWriter(buffer,fieldnames=FIELDS,lineterminator='\n');writer.writeheader();writer.writerows(rows)
    data=buffer.getvalue()
    if OUTPUT.exists() and OUTPUT.read_text(encoding='utf-8')==data: print(f'PASS: {len(rows)} actors')
    else: OUTPUT.write_text(data,encoding='utf-8');print(f'WROTE: {len(rows)} actors')
if __name__=='__main__':main()
