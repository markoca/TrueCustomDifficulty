#!/usr/bin/env python3
"""Read-only, fail-closed audit of ORIGINAL stash attachment singleton CFG.

No native loot probability or engine runtime behavior can be inferred here.
This is a developer tool; not part of the mod's customer/build runtime.
"""
import argparse
import csv
import re
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
POOL = ROOT / 'src/TrueCustomDifficulty/Data/stash-bonus-attachment-progressions.csv'
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
    """Extract only explicitly authored leaf fields; do not resolve inheritance."""
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
            if not stack and name in expected:
                if name in found:
                    raise ValueError('Duplicate original generator root ' + name)
                found[name] = {'root_line': lineno, 'metadata': metadata.strip(), 'fields': {}}
            stack.append(name)
            continue
        if stack and stack[0] in expected:
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
    result = []
    for root, row in expected.items():
        src = found[root]
        fields = src['fields']
        prefix = 'ItemGenerator/[0]/PossibleItems/[0]/'
        got = fields.get(prefix + 'ItemPrototypeSID')
        if got != row['ItemSID']:
            raise ValueError(f'{root}: expected inner ItemPrototypeSID {row["ItemSID"]}, got {got!r}; inheritance must be resolved explicitly')
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
            'EffectiveInheritedValues': 'UNRESOLVED' if 'refurl' in src['metadata'] or 'refkey' in src['metadata'] else 'NOT_INFERRED',
            'RuntimeDropVerified': 'NO',
        })
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path, help='Original decoded CFG file, directory or ZIP')
    parser.add_argument('--out', type=Path, help='Write reviewed authored leaf fields as a CSV')
    args = parser.parse_args()
    original, provenance = load_original(args.source)
    try:
        decoded = original.decode('utf-8-sig')
    except UnicodeDecodeError:
        decoded = original.decode('cp1251')
    result = parse_original(decoded, load_expected())
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
    print('NOT VERIFIED: effective inherited values, native SubItemGenerator execution, gameplay inventory or probability')


if __name__ == '__main__':
    main()
