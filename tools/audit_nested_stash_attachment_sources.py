#!/usr/bin/env python3
"""Read-only, fail-closed audit of ORIGINAL stash attachment singleton CFG.

No native loot probability or engine runtime behavior can be inferred here.
This is a developer tool; not part of the mod's customer/build runtime.
"""
import argparse
import csv
import hashlib
import re
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
POOL = ROOT / 'src/TrueCustomDifficulty/Data/stash-bonus-attachment-progressions.csv'
LEDGER = ROOT / 'docs/evidence/STASH-ATTACHMENT-ORIGINAL-SINGLETON-CFG-2026-10-17.csv'
PINNED_SOURCE_SHA256 = '4a06bc946eb9eaa7ff736421df7fd1bb6fe38fb91038bf5fffa005f680d64ac3'
PARENT_METADATA = '{refurl=../ItemGeneratorPrototypes.cfg;refkey=[0]}'
ENTRY = re.compile(r'^([^\s].*?)\s*:\s*struct\.begin\b(.*)$')
ASSIGN = re.compile(r'^(\S+)\s*=\s*(.*?)\s*$')


def load_expected(path=POOL):
    with path.open(encoding='utf-8', newline='') as f:
        rows = list(csv.DictReader(f))
    result = {}
    for row in rows:
        key = row['SourceGeneratorSID']
        if key in result or key != 'Stash_' + row['ItemSID']:
            raise ValueError('Duplicate or non-singleton name: ' + key)
        result[key] = row
    if len(result) != 19:
        raise ValueError(f'Expected 19 audited attachment sources, got {len(result)}')
    return result


def load_original(path):
    path = Path(path)
    suffix = '/GameData/ItemGeneratorPrototypes.cfg'
    if path.is_dir():
        candidates = [p for p in path.rglob('ItemGeneratorPrototypes.cfg')
                      if ('/' + p.as_posix()).endswith(suffix)]
        if len(candidates) != 1:
            raise ValueError(f'Expected exactly one GameData source CFG, found {len(candidates)}')
        return candidates[0].read_bytes(), str(candidates[0])
    if zipfile.is_zipfile(path):
        with zipfile.ZipFile(path) as archive:
            candidates = [n for n in archive.namelist()
                          if ('/' + n.replace('\\', '/')).endswith(suffix)]
            if len(candidates) != 1:
                raise ValueError(f'Expected exactly one GameData CFG in ZIP, found {len(candidates)}')
            return archive.read(candidates[0]), str(path) + ':' + candidates[0]
    if path.is_file() and path.name == 'ItemGeneratorPrototypes.cfg':
        return path.read_bytes(), str(path)
    raise ValueError('Pass the original GameData/ItemGeneratorPrototypes.cfg, a CFG directory, or a ZIP')


def parse_original(text, expected):
    """Validate authored singleton leaves and the raw referenced [0] parent.

    Parent raw defaults are evidence, NOT a claim about Unreal count behavior.
    """
    found = {}
    stack = []
    for lineno, original in enumerate(text.splitlines(), 1):
        line = original.strip()
        if not line or line.startswith('//'):
            continue
        if line == 'struct.end':
            if not stack:
                raise ValueError(f'Unmatched struct.end at line {lineno}')
            stack.pop()
            continue
        m = ENTRY.match(line)
        if m:
            name, metadata = m.groups()
            if not stack and (name in expected or name == '[0]'):
                if name in found:
                    raise ValueError('Duplicate original generator root ' + name)
                found[name] = {'root_line': lineno, 'metadata': metadata.strip(), 'fields': {}}
            stack.append(name)
            continue
        if stack and stack[0] in found:
            m = ASSIGN.match(line)
            if m:
                key, value = m.groups()
                path = '/'.join(stack[1:] + [key])
                fields = found[stack[0]]['fields']
                if path in fields:
                    raise ValueError(f'Duplicate original field {stack[0]}/{path}')
                fields[path] = value
    if stack:
        raise ValueError('Unbalanced original CFG (unclosed structures)')
    missing = set(expected) - set(found)
    if missing:
        raise ValueError('Missing original singleton roots: ' + ', '.join(sorted(missing)))
    if '[0]' not in found:
        raise ValueError('Missing referenced original [0] generator template')
    parent = found['[0]']['fields']
    prefix = 'ItemGenerator/[0]/PossibleItems/[0]/'
    if (parent.get(prefix + 'Chance') != '0.f'
            or parent.get(prefix + 'MinCount') != '0'
            or parent.get(prefix + 'MaxCount') != '0'
            or parent.get('ItemGenerator/[0]/Category') != 'EItemGenerationCategory::None'):
        raise ValueError('Original [0] parent template changed; review inheritance')
    result = []
    for root, row in expected.items():
        src = found[root]
        fields = src['fields']
        prefix = 'ItemGenerator/[0]/PossibleItems/[0]/'
        got = fields.get(prefix + 'ItemPrototypeSID')
        if got != row['ItemSID']:
            raise ValueError(f'{root}: expected inner ItemPrototypeSID {row["ItemSID"]}, got {got!r}')
        if (src['metadata'] != PARENT_METADATA
                or fields.get('SID') != root
                or fields.get('ItemGenerator/[0]/Category')
                    != 'EItemGenerationCategory::Attach'
                or fields.get(prefix + 'Chance') != '1'):
            raise ValueError(f'{root}: audited source parent/category/Chance changed')
        expected_fields = {
            'SID', 'ItemGenerator/[0]/Category',
            prefix + 'ItemPrototypeSID', prefix + 'Chance'
        }
        if set(fields) != expected_fields:
            raise ValueError(f'{root}: original singleton fields changed; review explicitly: '
                             f'{sorted(set(fields) ^ expected_fields)}')
        other_items = [p for p in fields if '/PossibleItems/[' in p
                       and not p.startswith(prefix)]
        if other_items:
            raise ValueError(f'{root}: additional authored PossibleItems paths: {other_items[:4]}')
        if any(p.endswith('/ItemGeneratorPrototypeSID') for p in fields):
            raise ValueError(f'{root}: nested sub-generator inside original singleton; manual source audit required')
        result.append({
            'SourceGeneratorSID': root,
            'ItemSID': row['ItemSID'],
            'Category': row['Category'],
            'ProgressionTier': row['ProgressionTier'],
            'RootLine': src['root_line'],
            'RootReferenceMetadata': src['metadata'],
            'AuthoredInnerChance': fields.get(prefix + 'Chance', 'NOT_AUTHORED'),
            'AuthoredInnerWeight': fields.get(prefix + 'Weight', 'NOT_AUTHORED'),
            'AuthoredMinCount': fields.get(prefix + 'MinCount', 'NOT_AUTHORED'),
            'AuthoredMaxCount': fields.get(prefix + 'MaxCount', 'NOT_AUTHORED'),
            'AuthoredCategory': fields.get('ItemGenerator/[0]/Category', 'NOT_AUTHORED'),
            'ParentReferenceKey': '[0]',
            'ParentMinCountRaw': parent[prefix + 'MinCount'],
            'ParentMaxCountRaw': parent[prefix + 'MaxCount'],
            'ParentChanceRaw': parent[prefix + 'Chance'],
            'EffectiveInheritedValues': 'PARENT_FIELDS_RESOLVED_RUNTIME_SEMANTICS_UNVERIFIED',
            'RuntimeDropVerified': 'NO',
        })
    return result


def verify_source_ledger(result, raw_source):
    """Only the exact reviewed original GameData CFG may certify the ledger."""
    digest = hashlib.sha256(raw_source).hexdigest()
    if digest != PINNED_SOURCE_SHA256:
        raise ValueError(f'Original CFG source SHA256 drifted: {digest}')
    with LEDGER.open(encoding='utf-8', newline='') as stream:
        ledger = {r['SourceGeneratorSID']: r for r in csv.DictReader(stream)}
    if len(ledger) != 19 or set(ledger) != {r['SourceGeneratorSID'] for r in result}:
        raise ValueError('Pinned original singleton evidence ledger inventory drifted')
    for row in result:
        evidence = ledger[row['SourceGeneratorSID']]
        comparisons = {
            'ItemSID': row['ItemSID'],
            'ProgressionTier': row['ProgressionTier'],
            'AttachmentKind': row['Category'],
            'OriginalLine': str(row['RootLine']),
            'OriginalCategory': row['AuthoredCategory'],
            'OriginalChance': row['AuthoredInnerChance'],
            'AuthoredMinCount': row['AuthoredMinCount'],
            'AuthoredMaxCount': row['AuthoredMaxCount'],
            'TemplateMinCountRaw': row['ParentMinCountRaw'],
            'TemplateMaxCountRaw': row['ParentMaxCountRaw'],
            'ReferenceKey': row['ParentReferenceKey'],
            'RuntimeVerified': 'NO',
        }
        for key, value in comparisons.items():
            if evidence[key] != value:
                raise ValueError(f'Pinned original CFG evidence mismatch: {row["SourceGeneratorSID"]}/{key}')
    print('PASS: pinned original GameData SHA256 and 19-row source evidence ledger')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path, help='Original decoded CFG file, directory or ZIP')
    parser.add_argument('--out', type=Path, help='Write reviewed authored leaf fields as a CSV')
    parser.add_argument('--verify-ledger', action='store_true',
                        help='Verify the exact reviewed original CFG SHA256 and 19-row evidence ledger')
    args = parser.parse_args()
    original, provenance = load_original(args.source)
    try:
        decoded = original.decode('utf-8-sig')
    except UnicodeDecodeError:
        decoded = original.decode('cp1251')
    result = parse_original(decoded, load_expected())
    if args.verify_ledger:
        verify_source_ledger(result, original)
    if args.out:
        args.out.parent.mkdir(parents=True, exist_ok=True)
        with args.out.open('w', encoding='utf-8', newline='') as f:
            writer = csv.DictWriter(f, fieldnames=list(result[0]))
            writer.writeheader()
            writer.writerows(result)
    print(f'PASS authored original singleton source structure: {len(result)} entries from {provenance}')
    for entry in result:
        print(f'{entry["SourceGeneratorSID"]}: authored Chance={entry["AuthoredInnerChance"]}; '
              f'counts={entry["AuthoredMinCount"]}/{entry["AuthoredMaxCount"]}; '
              f'inherited={entry["EffectiveInheritedValues"]}')
    print('NOT VERIFIED: Unreal runtime interpretation of the inherited raw 0 counts, '
          'nested SubItemGenerator execution, game inventory, or physical-stash probability')


if __name__ == '__main__':
    main()
