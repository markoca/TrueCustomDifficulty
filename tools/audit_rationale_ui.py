#!/usr/bin/env python3
"""Static regression gate for the global right-side rationale panel contract.

Run from any directory: python3 tools/audit_rationale_ui.py
A PASS proves source wiring and family coverage, not Wine rendering.
"""
from pathlib import Path
import re
import sys


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/TrueCustomDifficulty"
VM = (SOURCE / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
TABS = (SOURCE / "ViewModels/TabViewModel.cs").read_text(encoding="utf-8")
XAML = (SOURCE / "MainWindow.xaml").read_text(encoding="utf-8")


def assert_that(ok: bool, description: str) -> None:
    if not ok:
        raise AssertionError(description)


def main() -> None:
    start = VM.index('public string RationaleDisplay =>')
    end = VM.index('public string Logic => Definition.Logic;', start)
    guidance = VM[start:end]
    assert_that('ControlHelp' not in guidance.split('public string ControlHelp')[0],
                'Gameplay WHAT IT DOES still concatenates control instructions')
    assert_that('return "CONTROLS"' not in guidance,
                'Instructions still inject an independent CONTROLS heading')
    assert_that('Environment.NewLine}{ControlHelp}' not in guidance,
                'Old inline control instructions remain')
    assert_that('public string ControlHelp' in guidance,
                'Dedicated control guidance is missing')
    assert_that('if (IsVisualAggregateMember)' in guidance,
                'Selected aggregate child lacks family-aware guidance')
    for kind in ('IsRange', 'IsChoiceSlider', 'IsToggle'):
        assert_that(f'if ({kind})' in guidance, f'No generic {kind} interaction guide')

    family_start = VM.index('VisualAggregateFamilyByParameterId')
    family_end = VM.index('VisualAggregateAnchorByFamily', family_start)
    family_map = VM[family_start:family_end]
    families = set(re.findall(r'=\s*"([A-Za-z][A-Za-z0-9]+)"', family_map))
    guide_begin = guidance.index('var instruction = VisualAggregateFamily switch')
    guide_end = guidance.index('return instruction + Environment.NewLine', guide_begin)
    guide_families = set(re.findall(r'"([A-Za-z][A-Za-z0-9]+)"\s*=>', guidance[guide_begin:guide_end]))
    assert_that(bool(families) and families <= guide_families,
                'Missing specific guide for visual families: ' + ', '.join(sorted(families-guide_families)))

    assert_that(XAML.count('Text="CONTROLS"') == 1,
                'Exactly one visible CONTROLS heading must exist')
    heading = XAML.index('Text="CONTROLS"')
    help_binding = XAML.index('Text="{Binding SelectedParameter.ControlHelp}"')
    muted_footer = XAML.index('Text="{Binding TabSummary}"')
    assert_that(XAML.index('Text="{Binding SelectedParameter.RationaleDisplay}"') < heading
                < help_binding < muted_footer,
                'CONTROLS must follow gameplay meaning and precede muted contextual footer')
    assert_that('Text="{Binding SliderGuide}"' not in XAML
                and 'public string SliderGuide' not in TABS,
                'Redundant per-tab mouse/keyboard instruction footer is still visible')
    for binding in ('SelectedParameter.HasValueMeaning', 'SelectedParameter.HasLogic',
                    'SelectedParameter.HasInteraction', 'SelectedParameter.HasEffectiveDamage'):
        assert_that(binding in XAML, 'Gameplay rationale panel lost ' + binding)

    print(f'PASS: global rationale order, one heading, {len(families)}'
          ' family-specific control guides, typed fallbacks, no redundant footer')
    print('NOT RUN: .NET compilation / Wine rendering / interaction acceptance')


if __name__ == "__main__":
    try:
        main()
    except (AssertionError, OSError, ValueError) as exc:
        print('FAIL:', exc, file=sys.stderr)
        sys.exit(1)
