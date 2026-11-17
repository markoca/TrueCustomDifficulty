# STASH / LOOT consistency review — 2026-11-17

Scope: 89 `loot.*` catalog rows (44 visible), STASH LOOT MIX / ADJUST ODDS, MEDICAL COMPOSITION, STASH SMART LOOT, NPC LOOT help texts, factory presets.

## Verified consistent

* Every hidden `loot.*` id is owned by a board or family (no orphan hidden rows).
* "Vanilla:" claims in every loot rationale match the catalog vanilla values (Medkit/Army Medkit MAX: stored 0 = source cap shown as 5, stored -1 = 0 items; board maps both ways).
* All 12 factory presets keep the Standard/Rare ammo mirrors equal to AMMO AMOUNT.
* Help-text UI tokens resolve to drawn labels (scan of ParameterViewModel strings against Controls/XAML).

## Fixed

1. ADJUST ODDS: redundant `CHANCE` tag removed, header `100% = GAME ODDS`.
2. Standard/Rare ammo mirror texts: AMMO AMOUNT, `0.25x–2x`.
3. Standard/Rare Chance Scale texts: marked legacy compatibility-only.
4. NPC LOOT help: SMART LOOT instead of *Extra Corpse Loot*.
5. STASH SMART LOOT redraw inputs: stale Basic `stashMix*` cells removed.

## Open — needs a decision

Four hidden legacy ids are still written when an older user preset carries a non-1 value, but no board shows them:

| id | effect if set |
|---|---|
| `loot.stashMixAmmo` | thins ammo Weight picks in every tier |
| `loot.stashMixGrenades` | thins grenade Weight picks in every tier |
| `loot.standardStashChanceScale` | scales every Standard stash Chance |
| `loot.rareStashChanceScale` | scales every Rare stash Chance |

Options: (a) an **OLDER PRESET … CLEAR** notice on STASH LOOT MIX, same as TRADER STOCK; (b) reset them to 1 when a preset loads (silent migration); (c) leave as is.
