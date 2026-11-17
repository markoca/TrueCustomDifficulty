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
