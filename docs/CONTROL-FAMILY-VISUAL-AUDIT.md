# True Custom Difficulty — Control-Family Visual Audit

**Source checkpoint:** 2026-10-15, cross-family style unification and slider numeral legibility. This is a source/UI-contract inventory, **not** proof of a successful local .NET publish or rendered Wine screenshots.

## Canonical visual families

| Family | Current owner | Shared appearance and behavior |
| --- | --- | --- |
| Ordinary source slider | `MainWindow.xaml` `ParameterCardTemplate` + `App.xaml` Slider | One WPF Track/Thumb template, compact parameter caption and current value, native snap steps, muted reference marker, editable value highlighted only when changed |
| Choice slider | Same WPF template with choice branch | Uses the native Slider template and proportional reference marks; keeps discrete choices and independent value conversion |
| Two-ended range slider | Same template, `RangeTrackCaptionStyle` and `RangeTrackValueStyle` | Identical MIN and MAX caption/value fonts, alignments and muted defaults; both keep unchanged bindings and existing change triggers |
| Toggle | Shared `App.xaml` CheckBox template and `BooleanCheckboxChrome.cs` for hand-drawn boards | Same geometric checkbox conventions; checked accent, restrained hover, no divergent custom whole-card glow |
| Rotary source knob | `KnobTypography.cs`, `AggregateVisualControl.DrawSourceKnob`, `AnomalyDamageRadar.DrawGlobalDamageKnob` | 10.8-DIP Bold upper caption, 10.8-DIP white SemiBold MIN/MAX endpoints raised closer to rim; current source yellow only when modified, reference tick muted; two-value knobs keep muted secondary line and gold changed-number only |
| Shared radar / dashboard / graph | `VisualControlTypography.cs`, native `*Radar`, `*Control` and `AggregateVisualControl` owners | 12.5-DIP Bold accent board title, shared 9.3-DIP Bold axis title and 9.2-DIP muted axis ticks **where the same axis function is present** |
| Native compact track/card heading | `VisualControlTypography.CardTitle` | 10.8-DIP Bold applied where identical heading space exists in Bleeding, drink cards, Mutant Hit Damage, dropped-weapon-condition range |
| Core Health / medical source values | `HealthTypography.cs` and native Health/Medical renderers | Board title inherits `VisualControlTypography.BoardTitle`; independently calibrated 11.4-DIP captions, 12.8-DIP source values and 10.4-DIP medical/health axes remain purpose-specific |
| Clickable reference markers | `VanillaMarkerBar.cs` | Muted until hover, hover turns white, native slider click-to-snap contract; not the same as editable current values |

## Board-title coverage

The native board/section renderer title token applies to exactly these twelve source files: `ActionCostsRadar.cs`, `BleedingCurveControl.cs`, `DrunkennessControl.cs`, `HumanGroupSizeRadar.cs`, `ItemPriceScalersRadar.cs`, `MutantGroupSizeRadar.cs`, `MutantHealthRadar.cs`, `MutantDropRadar.cs`, `StashAmmoRadar.cs`, `WorldLootRemovalRadar.cs`, `AnomalyDamageRadar.cs`, and `AggregateVisualControl.cs` (six board-title rendering paths). `MedicalHealingRadar.cs` is covered through `HealthTypography.Title`, which aliases `VisualControlTypography.BoardTitle`.

## Comparable axes and card titles

- Human Squad Size: shared chart axis and tick sizes.
- Item Value Layers: shared chart axis and tick sizes.
- World Loot Depletion: shared chart axis and tick sizes.
- Anomaly Damage: shared chart axis title size; independent control-specific numeric labels retain their distinct meaning.
- Aggregate/Fatigue: shared ordinary fatigue-axis tick size.
- Bleeding / Alcohol / Mutant Hit Damage / Dropped Weapon Condition: shared native compact card/track caption size where appropriate.

## Intentional exceptions: do not normalize geometry

- Native radial selectors (Radiation Balance, Drunkenness state display, specialized staging and survival gauges) are **not** copies of a 270-degree source knob.
- Medical Healing has independently colored Healing, Bleeding and Radiation axes; forcing one neutral color would remove information.
- Stamina, Bleeding and Fatigue timelines/graphs use geometry tied to actual native parameters and source ranges, not a generic equal-width slider template.
- Narrow-layout cards with small axis/phase labels keep their measured positions and font sizes when the common `CardTitle` role does not apply.
- The reference/Vanilla marker must remain quieter than the active source handle. Yellow is for changed numbers, not arbitrary entire labels.
- Source IDs, limits, precision, assignments, CFG output and presets must never be altered by visual parity passes.

## Acceptance checklist

1. Publish with `./publish.sh` on the user's system and launch with Wine.
2. At minimum 1024×576 and ordinary 1600×900, compare board heading sizes across Combat, Health, Stamina, Survival, Economy, Anomalies, Loot and Master.
3. Check the paired MIN/MAX range slider fonts, independent edits and native reference marker click-to-snap.
4. Compare 270-degree source-knob text with special Global Anomaly Damage; test modified and unchanged colors.
5. Inspect chart-axis titles and numerical graduation legibility, and ensure text is not clipped by card/axis boundaries.
6. Recheck both On/Off toggles, source-generated CFGs, preset load/save and automatic Build Difficulty deployment.
7. Run the active static consistency audit. **No Wine or .NET results are claimed in this source-only pass.**

## 2026-10-15 numeric readability and anomaly cleanup

- The WPF parameter current readout is 13.3 DIP; MIN/MAX range numerals 10.8 DIP, effective value readout 11.8 DIP. Binding and reference colors are unchanged.
- Mutant Health retains 14×58-DIP slider cards and 65-DIP row pitch, with 13-DIP multipliers, 12.5-DIP bold white actual HP amount/unit and gold 13.7-DIP changed HP digits only.
- Stamina, Mutant Group/Part, Human Squad, Loot, Bleeding, Drunkenness, Medical and Aggregate native sliders have selective ~0.8-DIP numeric size increases. Compact headings, axis text and rotary knob captions/limits are intentionally unchanged.
- Anomaly Damage removes three redundant helper captions and uses EFFECTIVE instead of FINAL; the same ten native bars move 58 DIP upward in a 548-DIP frame. The independent Global knob and bar hit mapping are unchanged.
- These are source-level checks; local .NET and Wine acceptance remains outstanding.
