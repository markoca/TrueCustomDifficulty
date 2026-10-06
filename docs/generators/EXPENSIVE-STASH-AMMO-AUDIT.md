# Expensive Stash Ammo Quantity — deep audit, production scope and runtime implementation

This audit defines the safe runtime surface for **Expensive Stash Ammo Quantity**. The control changes only integer ammo `MinCount/MaxCount`; every vanilla ammo `Chance` / `Weight`, progression-rank gate, medical/food entry, guaranteed grenade and coupon rule is preserved.

## Source ammo surface

- Shared parent: `GamePass_Stash_ItemGenerator_Rare`
- Ammo rows: **25** across Newbie / Experienced / Veteran / Master progression blocks.
- Full audited parent template: **19 category blocks / 42 item rows**.
- Coupon rows: **8**, kept compatible with the existing **Expensive Stash Coupon Chance** control.

## Confirmed production-only scope

| Production branch | Physical normal-world containers | Redirect assignments |
|---|---:|---:|
| Rank-based `Stash_Expensive_<rank>` usage | **17** | **68** |
| Location-specific production wrappers nesting `GamePass_Stash_ItemGenerator_Rare` | **163** through **162 wrapper roots** | **162** |
| Direct `WorldMap_WP` references to the shared Rare parent | **75** (37 base + 38 DLC1) | **75** |
| **Total** | **255 unique physical containers** | **305** |

The three production branches are disjoint.

## Protected surfaces

The production manifest intentionally excludes:

- all `TestMap_Balance` and `TestMap_Vortex_WP1` rank-wrapper references;
- the `SQ103_LogicLevel_WP` rank-wrapper reference;
- **7** direct Rare-parent references in quest logic levels;
- `GeneralNPC_Neutral_Sniper_ItemGeneratorCustom`, because it is a standard-human NPC path sharing the Rare parent;
- `Yanov_Lisovyi_StashGenerator3_E95C28CC46F62A1DFBDE56BC9B2B66EF`, because no runtime consumer is resolved for it.

## Runtime implementation

The writer creates five complete TCD-owned roots:

- `TCD_GamePass_Stash_ItemGenerator_Rare`
- `TCD_Stash_Expensive_Newbie`
- `TCD_Stash_Expensive_Experienced`
- `TCD_Stash_Expensive_Veteran`
- `TCD_Stash_Expensive_Master`

Each root materializes the full audited vanilla Rare template and changes only the 25 ammo count rows. The shared vanilla Rare parent is never globally patched.

When **Expensive Stash Coupon Chance** is also modified, the selected difficulty's coupon Chance is additionally materialized into the TCD-owned roots. This prevents the ammo redirect from bypassing the existing coupon control. Coupon-only builds continue to use the existing audited vanilla-wrapper edit points.

## Scaling

Use **50–200% in 25% steps**, positive round-half-up per `MinCount` and `MaxCount`, clamped to at least 1.

## Status

`SEMANTICS: READY` · `SCALING: READY` · `PRODUCTION SCOPE: READY` · `WRITER/UI: IMPLEMENTED (runtime test required)`
