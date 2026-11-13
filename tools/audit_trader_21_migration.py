#!/usr/bin/env python3
"""2.1-only Trader SELL migration gate: reconcile roles and source profiles.

Run with:
 python3 tools/audit_trader_21_migration.py docs/generators/trader-buy-limits-86.csv 2.1-staging.csv
This script never falls back to 2.0.5 data; the old inventory is read only to
report migration differences. Do not activate the 2.1 writer until every
unmatched profile has an explicit reviewed role and the writer tests pass.
"""
import csv
import sys
from collections import Counter, defaultdict

ROLES = {'GENERAL_TRADER','MEDIC','TECHNICIAN','BARTENDER','GENERIC_NPC_TRADER'}
CATEGORIES = {'Weapons','Armor','Attachments','Ammo','Medical','Food',
              'Drinks','Artifacts','Detectors','Grenades','Binoculars'}
def read(path):
    with open(path, encoding='utf-8-sig', newline='') as f:
        return list(csv.DictReader(f))

def main(old_path, new_path):
    old = read(old_path)
    new = read(new_path)
    if not old or not new:
        raise ValueError('Missing source data')
    roles = {}
    for row in old:
        key = row['scope'], row['trade_sid']
        role = row['trader_type']
        if role not in ROLES or (key in roles and roles[key] != role):
            raise ValueError('Invalid or ambiguous legacy role: '+str(key))
        roles[key] = role
    groups = defaultdict(list)
    for row in new:
        key = row['trade_scope'], row['trade_sid']
        groups[key].append(row)
        if row['category'] not in CATEGORIES:
            raise ValueError('Unrecognized 2.1 category: '+str(row))
        chain = row['nested_chain'].split('>')
        if len(chain) not in (1,2) or chain[0]!=row['root_sid'] or chain[-1]!=row['generator_sid']:
            raise ValueError('Broken 2.1 chain: '+str(key))
        links = row['parent_link_coordinates'].split('|') if row['parent_link_coordinates'] else []
        if len(links)!=int(row['parent_link_count']) or len(set(links))!=len(links):
            raise ValueError('Ambiguous 2.1 parent links: '+str(key))
        if (len(chain)==1 and links) or (len(chain)==2 and not links):
            raise ValueError('Invalid 2.1 parent topology: '+str(key))
    if len(new)!=3150 or len(groups)!=90:
        raise ValueError('Unexpected 2.1 leaf/profile census')
    inherited = sorted(groups.keys() & roles.keys())
    new_only = sorted(groups.keys() - roles.keys())
    legacy_only = sorted(roles.keys() - groups.keys())
    print('2.1 leaf rows:',len(new),'2.1 profiles with leaves:',len(groups))
    print('Existing role assignments available:',len(inherited))
    print('2.1 profiles requiring explicit role mapping:',len(new_only))
    for scope,sid in new_only:
        print('  UNMAPPED',scope,sid)
    print('Legacy profiles without 2.1 leaf rows:',len(legacy_only))
    for scope,sid in legacy_only:
        print('  LEGACY_ONLY',scope,sid)
    print('2.1 category counts:',dict(sorted(Counter(r['category'] for r in new).items())))
    if new_only:
        raise SystemExit('BLOCKED: review unmapped 2.1 roles before runtime migration')
    print('Role reconciliation passed; CFG writer and clone tests are still required')

if __name__=='__main__':
    if len(sys.argv)!=3:
        sys.exit('Usage: audit_trader_21_migration.py LEGACY_ROLE_CSV 2.1_STAGING_CSV')
    main(sys.argv[1],sys.argv[2])
