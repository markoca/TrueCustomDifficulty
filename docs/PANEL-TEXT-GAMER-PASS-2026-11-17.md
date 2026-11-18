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

## Second pass (same day)

6. Say **vanilla**, not *the game's odds* / *GAME ODDS*: board captions `100% = VANILLA`, texts `Vanilla: 100%`, *keeps vanilla odds*.
7. The panel title is the catalog label, so the label must be the name the board draws (Shuffle Loose Weapons, Med Drops, Ammo Pack Size, Bonus Packs, Corpse Scarcity, `{Tier} {Category} Odds`, Basic Grenades Chance, Rare Coupons Chance, RGD-5 Amount, Ammo Amount, Army Medkit Mix).

## Third pass (same day)

8. WHAT IT DOES is a single paragraph.
9. `Vanilla:` format: `Vanilla: X.` if equal on all difficulties; otherwise `Vanilla: Novice a, Stalker b, Veteran c, Master d.` with equal neighbours merged (`Novice–Veteran`, `Novice and Stalker`).
10. No game file names, effect IDs, CFG fields or dev notes in any panel section.
11. Stage names follow the boards: LOW / MEDIUM / SEVERE.

## Fourth pass (same day)

12. A control that only works behind a master switch ends WHAT IT DOES with `Only works while X is ON.`
13. The panel title names the clicked control, even when its catalog label is the board name (`OwnPanelTitle`).
14. Board tokens and labels use the same words (REGEN STRENGTH, STAMINA LOSS, CARRY LOSS, SHOT SPREAD, MAX HP); short contextual tokens (NEWBIE, ELECTRO, WEAPONS) are fine when the board gives the context.

## Fifth pass (same day)

15. When a board's drawn name was explicitly approved (e.g. HEALING ITEM EFFICIENCY REDUCTION, STARVATION MECHANICS), the label follows the board, not the other way round.
16. NPC LOOT card texts follow rules 8–9 too; card texts must not describe retired controls.

## Sixth pass (same day)

17. Texts use the unit the control shows: × for multiplier readouts, % for percent readouts, plain numbers for raw values. A resulting real-world value may follow in brackets (`Vanilla: 1× (45 seconds)`).
18. Sibling readouts on one board use one unit (SEVERE FATIGUE: all signed %).
19. Panel texts write multipliers with `×`.

## Seventh pass (same day)

20. The `Vanilla:` value carries the readout's unit (pts/s, pts, s, HP/s, bleed pts, rad pts, stamina/s, % strength). Where a multiplier board also shows a real amount, give both: `Vanilla: 1× (500 HP)`.

## Eighth pass (same day)

21. Endpoints ("0 removes it", "2 doubles it") use the control's unit too (`0%`, `0×`), except raw-value controls.
22. Vanilla values are stated once, in WHAT IT DOES; HIGHER / LOWER explains direction only.

## Ninth pass (same day)

23. Any other control named in a panel text is written in CAPS (its label or board token).

## Tenth pass (2026-11-18)

24. Rule 17 also covers time readouts: HUMAN TACTICS speak in seconds, not ×.
25. Rule 23 also covers CONTROLS help; texts call a control what it is (bar, knob, slider, checkbox).

## Eleventh pass (2026-11-18)

26. The multiplier sign is `×` everywhere — readouts, ticks, panel texts, messages. Never the letter `x`.
27. Numeric ranges use an en dash (`9–12`).

## Twelfth pass (2026-11-18)

28. Units follow the number after a space (`45 s`, `12 m`, `40 kg`); compact clocks (`1h05m`) are the exception.
29. Separators: ` · ` between parts, `→` for before → after, en dash for ranges.

## Thirteenth pass (2026-11-18)

30. CONTROLS help ends with one reset sentence: `Right-click one <part> to reset it; R resets <scope>.`
31. Help must describe the control as it is now (raw BASE bars are not %; no notes about features that are not there).

## Fourteenth pass (2026-11-18)

32. Say what the player feels, not how the system is built: *less breathing room between attacks*, *you lose 4 HP every 10 s*, not *scales the waits of all the family's attacks* / *this bundle adds … on an interval*.
33. Don't explain what the card already shows (readouts, variant lists).

## Fifteenth pass (2026-11-18)

34. No system words in panel texts: *state*, *bundle*, *layer*, *component*, *actor-level*, *configured*. Describe the situation the player is in (*while you're severely tired*, *after you get hurt*).
35. `Vanilla:` comes right after the first sentence.

## Sixteenth pass (2026-11-18)

36. WORKS WITH opens with the scope (`Only …`) or the partner control in CAPS; no *This …* openers, no mouse tips, no *eligible*.

37. WORKS WITH is never empty; CAPS names must exist on screen; labels follow the same tech-speak rules.

## Since 2026-11-18

The rules above are consolidated in `docs/PANEL-TEXT-STYLE.md` and enforced by `tools/audit_panel_text_style.py`.
