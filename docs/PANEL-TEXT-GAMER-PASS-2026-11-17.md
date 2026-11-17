# Panel text gamer pass — 2026-11-17

The right-hand panel shows four sections for a selected control: **WHAT IT DOES** (rationale), **HIGHER / LOWER** (logic), **WITH OTHER SETTINGS** (interaction) and **CONTROLS** (help). All four are player-facing.

## Rules used

1. No engine words: native, CFG, writer, SID, MinCount/MaxCount, DispersionRadius, HearingDistanceCoef, authored, generator/branch, SmartLoot package. Say what the player sees instead (stack size, spread, odds, bonus pack).
2. Name other controls exactly as the UI draws them (caps for board tokens, e.g. GUNS TO FIND, PRICES YOU PAY).
3. One `Vanilla: X` per rationale, same form everywhere.
4. Don't repeat the same caveat in two sections.
5. CONTROLS help says what the hands do, in one or two sentences.

## Method

* Scan of rationale / logic / interaction for engine words (all ids except legacy Trader Buys and compatibility-only rows).
* Scan of capitalized control references against catalog labels and drawn UI tokens; every stale name was replaced by the current one.
* `tools/pre_release.py` and every static audit stay green.
