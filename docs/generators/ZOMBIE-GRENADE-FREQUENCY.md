# Zombie Grenade Frequency — v0.2.17

**Status:** IMPLEMENTED / RUNTIME TEST REQUIRED

This control scales only the explicit `Chance` field on the existing generic-zombie grenade rows. It does **not** create grenade branches where vanilla has none and it does not touch weapon, detector, consumable, durability or attached-ammo fields.

## Source surface

- 93 generic zombie roots total.
- 91 roots contain exactly one local grenade row.
- 71 rows generate `GrenadeRGD5`; 20 generate `GrenadeF1`.
- 89 rows use `Chance=0.20`.
- 2 Scientist Stormtrooper rows use `Chance=0.40`.
- 2 Scientist-Recon roots have no grenade branch and remain untouched.
- 4 grenade rows contain additional weapon-like source fields; those fields are preserved verbatim.

## UI semantics

`Zombie Grenade Frequency` is a relative control because vanilla does not use one flat chance across every generic zombie.

- `0%` -> existing grenade Chance becomes `0`.
- `100%` -> exact vanilla.
- `250%` -> `0.20` rows become `0.50`; `0.40` rows become `1.00`.

Range: **0-250%**, step **5%**. The upper bound is deliberately 250% so even the largest vanilla source Chance cannot exceed 1.0.

## Writer model

The writer uses the ordinary concrete edit-point path. Each row is patched directly in its existing generic-zombie generator:

`<root>.ItemGenerator[category].PossibleItems[0].Chance`

Operation: `source Chance × UI multiplier`.

No TCD clone or redirect is required because the 91 target rows are already child-local zombie-only fields.
