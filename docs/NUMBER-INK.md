# Number ink: only numbers change (2026-11-18)

When a value differs from the vanilla reference, **only its numbers** change. They turn gold and are drawn `ReadoutInk.NumberGrowth` (1.2 DIP) bigger. Words, units, signs-as-words, `%`, `×` and `s` keep their normal colour and size. The text around a number never grows.

## How

| Where | Use |
|---|---|
| Board controls (`CreateText` / `Text`) | Pass `ReadoutInk.Numbers(accent, text)` as the brush and the **base** size. `ReadoutInk.Highlight` colours and grows the numbers. |
| A manual number range | `ReadoutInk.Mark(formatted, accent, index, length, baseSize)`. Never a bare `SetForegroundBrush(accent …)`. |
| XAML value text (`MainWindow.xaml`) | `controls:NumberRuns.Text` + `NumberRuns.Highlight="{Binding IsModified}"` + `NumberRuns.Accent`. Never a trigger that sets the whole TextBlock's Foreground, FontSize or FontWeight. |

Manual number regexes match `[+\-−]?\d+(?:\.\d+)?`. Units (`%`, `x`, `×`) are no longer part of the gold range.

## What changed

* 26 size ternaries (`modified ? 14.2 : 13.0`) were removed. Those readouts used to enlarge the whole text, units included. This covered coupons, the card readout, Flycatcher X/Y, weapon condition MIN/MAX, Master cards, the mixer, the native dials (signed %, Hercules kg, %), repair price, drink durations, sleep hours, artifact luck, the mutant drop %, trader buy cells, bleeding, drunkenness, anomaly damage, human tactics and awareness, stash branch and NPC consumables.
* 10 manual gold ranges now grow as well (`ReadoutInk.Mark`).
* XAML: CURRENT, EFFECTIVE, RANGE MIN/MAX and the example line no longer turn wholly gold or 13.7 bold. Only their numbers change.

Gate: `tools/audit_number_ink.py` (in `pre_release.py`).
