#!/usr/bin/env python3
"""Build a fail-closed 2.1 SELL staging manifest from the parent-link graph CSV."""
import csv
import sys
from collections import Counter

MEDICAL={'AntiRad','ArmyMedkit','Bandage','EcoMedkit','Hercules','Medkit'}
FOOD={'Bread','CannedFood','MonolithDryRation','Sausage'}
DRINKS={'Beer','Energetic','Milk','Vodka','Water'}
GRENADES={'GrenadeF1','GrenadeRGD5'}
NATIVE={'WeaponPrimary':'Weapons','WeaponPistol':'Weapons','WeaponSecondary':'Weapons',
        'Ammo':'Ammo','BodyArmor':'Armor','Head':'Armor','Attach':'Attachments',
        'Artifact':'Artifacts','Detector':'Detectors'}

def classify(row):
    item=row['item_sid']
    if item.startswith('Binoculars_'):return 'Binoculars'
    for name, items in [('Medical',MEDICAL),('Food',FOOD),('Drinks',DRINKS),('Grenades',GRENADES)]:
        if item in items:return name
    native=row['source_category'].removeprefix('EItemGenerationCategory::')
    if native=='Junk':
        if item.startswith('Gun'):return 'Weapons'
        if 'Armor' in item or 'Helmet' in item:return 'Armor'
    if native in NATIVE:return NATIVE[native]
    raise ValueError('Unclassified item: '+item+'/'+native)

def main(source,target):
    with open(source,newline='',encoding='utf-8-sig') as stream:
        rows=list(csv.DictReader(stream))
    fields=['trade_scope','trade_sid','root_sid','generator_scope','generator_sid',
            'entry_path','item_sid','category','source_category','chance','weight',
            'nested_chain','parent_link_coordinates','parent_link_count']
    if not rows or not (set(fields)-{'category'}).issubset(rows[0]):
        raise ValueError('Missing 2.1 source graph fields')
    counts=Counter()
    with open(target,'w',newline='',encoding='utf-8') as stream:
        out=csv.DictWriter(stream,fieldnames=fields)
        out.writeheader()
        for row in rows:
            chain=row['nested_chain'].split('>')
            slots=row['parent_link_coordinates'].split('|') if row['parent_link_coordinates'] else []
            if len(chain) not in (1,2) or chain[0]!=row['root_sid'] or chain[-1]!=row['generator_sid']:
                raise ValueError('Invalid nested generator chain')
            if len(slots)!=int(row['parent_link_count']) or len(set(slots))!=len(slots):
                raise ValueError('Invalid parent slot count or duplicate')
            if (len(chain)==1 and slots) or (len(chain)==2 and not slots):
                raise ValueError('Missing or unexpected parent reference')
            for slot in slots:
                x=slot.split(':')
                if len(x)!=4 or x[0]!=row['root_sid'] or not x[1].startswith('ItemGenerator/[') or not x[2].startswith('chance=') or not x[3].startswith('weight='):
                    raise ValueError('Invalid parent slot: '+slot)
            if len(row['entry_path'].split('/'))!=4:
                raise ValueError('Invalid leaf path')
            category=classify(row)
            out.writerow({**{k:row[k] for k in fields if k!='category'},'category':category})
            counts[category]+=1
    if sum(counts.values())!=3150 or counts['Binoculars']!=23:
        raise ValueError('2.1 census drift: '+str(counts))
    print('Validated:',dict(sorted(counts.items())))

if __name__=='__main__':
    if len(sys.argv)!=3:sys.exit('Usage: build_trader_21_manifest.py SOURCE.csv OUTPUT.csv')
    main(sys.argv[1],sys.argv[2])
