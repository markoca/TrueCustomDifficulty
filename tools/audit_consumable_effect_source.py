#!/usr/bin/env python3
"""Verify Water/Energy effect semantics against a decoded original GameData ZIP."""
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path
from zipfile import ZipFile

ROOT = Path(__file__).resolve().parents[1]
DATA = ROOT / 'src' / 'TrueCustomDifficulty' / 'Data'


def check(ok: bool, what: str) -> None:
    if not ok:
        raise AssertionError(what)


def block(source: str, name: str) -> tuple[str, str]:
    lines = source.splitlines()
    header = re.compile(r'^\s*' + re.escape(name) +
                        r'\s*:\s*struct\.begin(?:\s*\{refkey=([^}]+)\})?\s*$')
    hits = [(i, m.group(1) or '') for i, line in enumerate(lines)
            if (m := header.fullmatch(line))]
    check(len(hits) == 1, f'{name}: expected exactly one struct, got {len(hits)}')
    start, parent = hits[0]
    depth = 0
    for i in range(start, len(lines)):
        line = lines[i]
        if re.search(r'\bstruct\.begin\b', line):
            depth += 1
        if re.match(r'^\s*struct\.end\s*$', line):
            depth -= 1
            if depth == 0:
                return '\n'.join(lines[start:i + 1]), parent
    raise AssertionError(f'{name}: unterminated struct')


def field(source: str, key: str) -> str:
    hits = re.findall(r'^\s*' + re.escape(key) + r'\s*=\s*(.*?)\s*$',
                      source, re.MULTILINE)
    check(len(hits) == 1, f'{key}: expected one field, got {hits}')
    return hits[0]


def array(source: str, name: str) -> dict[int, str]:
    nested, _ = block(source, name)
    return {int(i): value for i, value in re.findall(
        r'^\s*\[(\d+)\]\s*=\s*([\w:]+)\s*$', nested, re.MULTILINE)}


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base-zip', type=Path, required=True)
    parser.add_argument('--dlc1-zip', type=Path, help='Accepted for pre-release runner; not used for base-game effects')
    parser.add_argument('--out', type=Path, help='Optional JSON evidence file')
    args = parser.parse_args()
    with ZipFile(args.base_zip) as zipfile:
        effects = zipfile.read('GameData/EffectPrototypes.cfg').decode('utf-8-sig')
        items = zipfile.read('GameData/ItemPrototypes.cfg').decode('utf-8-sig')

    expected = {
        'WaterStamina2': ('5.0', 'EEffectType::Stamina', '25%', ''),
        'WaterStaminaPerAction1': ('30.0', 'EEffectType::SPDrain', '-10%', ''),
        'EnergeticStamina': ('45.0', 'EEffectType::RegenStamina', '200%', ''),
        'EnergeticLimitedStamina': ('45.0', 'EEffectType::RegenStamina', '500%', ''),
        'EnergeticStaminaPerAction1': ('15.0', 'EEffectType::SPDrain', '-30%', ''),
        'EnergeticStaminaPerAction2': ('5.0', 'EEffectType::SPDrain', '-100%', 'EnergeticStaminaPerAction1'),
    }
    results: dict[str, dict[str, str]] = {}
    for sid, (duration, kind, magnitude, parent_expected) in expected.items():
        cfg, parent = block(effects, sid)
        check(parent == parent_expected or (parent_expected == '' and parent == '[0]'),
              f'{sid}: unexpected inheritance {parent!r}')
        # Variant PerAction2 inherits its SPDrain type and action roster from PerAction1.
        source_type = field(cfg, 'Type') if not parent_expected else field(block(effects, parent)[0], 'Type')
        check(source_type == kind, f'{sid}: type {source_type}, expected {kind}')
        for key, want in [('Duration', duration), ('ValueMin', magnitude), ('ValueMax', magnitude)]:
            check(field(cfg, key) == want, f'{sid}.{key}: unexpected CFG value')
        results[sid] = {'duration': duration, 'type': kind, 'magnitude': magnitude, 'refkey': parent}

    expected_actions = ('Walk', 'Run', 'Sprint', 'Jump', 'Vault',
                        'MeleeNormal', 'MeleeStrong', 'MeleeButstock')
    for sid in ('WaterStaminaPerAction1', 'EnergeticStaminaPerAction1'):
        cfg, _ = block(effects, sid)
        found = array(cfg, 'AffectedStaminaActionTypes')
        check(tuple(found[i] for i in sorted(found)) ==
              tuple('EStaminaAction::' + a for a in expected_actions),
              f'{sid}: action types drifted: {found}')
    child, _ = block(effects, 'EnergeticStaminaPerAction2')
    check('AffectedStaminaActionTypes' not in child,
          'PerAction2 should inherit the action list from PerAction1')

    water, _ = block(items, 'Water')
    energy, _ = block(items, 'Energetic')
    limited, parent = block(items, 'Energetic_Limited')
    check(parent == 'Energetic', 'Limited drink no longer inherits Energetic')
    w, e, l = [array(value, 'EffectPrototypeSIDs') for value in (water, energy, limited)]
    alt = array(limited, 'AlternativeEffectPrototypeSIDs')
    check(w.get(1) == 'WaterStamina2' and w.get(4) == 'WaterStaminaPerAction1',
          f'Water effect slots changed: {w}')
    check(e.get(1) == 'EnergeticStamina' and
          e.get(6) == 'EnergeticStaminaPerAction1' and
          e.get(9) == 'EnergeticStaminaPerAction2',
          f'Regular Energy effect slots changed: {e}')
    check(l.get(1) == 'EnergeticLimitedStamina' and
          l.get(9) == 'EnergeticStaminaPerAction2' and
          'EnergeticStaminaPerAction1' not in l.values(),
          f'Limited normal effect slots changed: {l}')
    check(alt.get(6) == 'EnergeticStaminaPerAction1',
          f'Limited Weird Kettle alternative effect changed: {alt}')

    baseline = json.loads((DATA / 'cfg-writer-baseline.json').read_text(encoding='utf-8'))
    for pid, names in {
        'survival.waterEffectDuration': ('WaterStamina2', 'WaterStaminaPerAction1'),
        'survival.energyDrinkEffectDuration': ('EnergeticStamina', 'EnergeticLimitedStamina',
                                               'EnergeticStaminaPerAction1', 'EnergeticStaminaPerAction2'),
    }.items():
        points = {e['prototype']: e for e in baseline['editPoints'] if e['parameterId'] == pid}
        check(set(points) == set(names), f'{pid}: unexpected/missing writer targets')
        for sid in names:
            p = points[sid]
            check(p['sourceRaw'] == results[sid]['duration'] and
                  p['key'] == 'Duration' and p['operation'] == 'scale' and
                  p['file'] == 'GameData/EffectPrototypes.cfg',
                  f'{pid}/{sid}: writer has changed independently of original source')

    report = {'status': 'PASS', 'source': str(args.base_zip),
              'effects': results, 'regular_energy_short_effects':
              ['EnergeticStaminaPerAction1', 'EnergeticStaminaPerAction2'],
              'limited_energy_short_effects': ['EnergeticStaminaPerAction2'],
              'limited_alternative_effects': ['EnergeticStaminaPerAction1'],
              'actions': list(expected_actions),
              'runtime_stacking_proven': False}
    if args.out:
        args.out.parent.mkdir(parents=True, exist_ok=True)
        args.out.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print('PASS: original Water / Energy effect identity, magnitude, inheritance,')
    print('      timing, action scope, regular/Limited membership and writer mapping.')
    print('NOT VERIFIED: UE runtime stacking/override semantics for concurrent SPDrain effects.')


if __name__ == '__main__':
    main()
