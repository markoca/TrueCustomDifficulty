# Human NPC grenade semantics audit

**Decision: NO UI / preserve mixed vanilla semantics.**

The standard-human direct equipment inventory contains **44 grenade rows across 20 loadout generators**. A global `Human NPC Grenade Chance` slider is not source-exact because the live repeatable production surface does not use one field semantics.

| Surface | Rows | Meaning | Decision |
|---|---:|---|---|
| Active `Chance=0.3` | 31 | direct grenade Chance | mapped, but not a complete all-NPC surface |
| Active `Chance=0.2` | 5 | direct grenade Chance | mapped, but not a complete all-NPC surface |
| Prologue `Chance=0` | 6 | explicitly disabled grenade rows | hard-excluded; never activate |
| Active `Weight=1000` | 2 | relative selection Weight, not Chance | normal repeatable production; preserve |

The two Weight-only rows are:

- `GuardNPC_Varta_Stormtrooper_ItemGenerator` -> `GrenadeF1`, Experienced/Veteran/Master, `Weight=1000`.
- `GuardNPC_Noon_Recon_ItemGenerator` -> `GrenadeF1`, Master, `Weight=1000`.

Both are ordinary repeatable GuardNPC loadouts. They cannot be removed by the project's Prologue/story/test hard-exclusion policy. Therefore a UI percentage that scales only the 36 active Chance rows would silently omit legitimate standard NPC grenade generation, while transforming Weight into a percentage would invent semantics.

## Final boundary

- Do **not** add `Human NPC Grenade Chance/Frequency` to the normal UI.
- Keep all 44 rows documented.
- Keep the six Prologue zero-Chance rows disabled.
- Do not normalize the two Weight-only rows into Chance rows.
- This decision is source-semantic, so no runtime test is required to justify the UI exclusion.

Source inventory: `npc-equipment-direct-entries.csv`.
