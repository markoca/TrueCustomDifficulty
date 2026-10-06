# Destructible Ammo Production Fan-Out

## Decision

The production-only scope for **Destructible Ammo Quantity** is closed and the control has been implemented since v0.2.9.

The vanilla pools `DestructibleStash_AmmoSNG` and `DestructibleStash_AmmoNATO` must **not** be patched globally. They are shared dependencies: test/debug destructibles reference the same family, and Master wrappers also feed non-destructible DLC ItemContainers.

The safe model is selective redirection of confirmed production destructible actors to TCD-owned copies:

- `TCD_DestructibleStash_AmmoSNG`
- `TCD_DestructibleStash_AmmoNATO`

The TCD copies must preserve vanilla item-selection weights and modify only the four `MinCount` / `MaxCount` ammo ranges in each pool.

## Confirmed production scope

| Surface | SNG | NATO | Total |
|---|---:|---:|---:|
| Production redirect assignments | 983 | 252 | **1,235** |
| Normal `D_WoodenAmmoCrate_01` roots | 982 | 252 | **1,234** |
| Non-crate production exception | 1 | 0 | **1** |

All **1,235** confirmed production rows:

- are in `WorldMap_WP`;
- are classified `production_world`;
- use a direct `ItemGeneratorSettings` assignment;
- use `ERank::Newbie`;
- resolve directly to SNG or NATO, with no unresolved intermediate wrapper;
- have unique physical SpawnActor roots.

By source file:

- base game: **934** redirects (832 SNG + 102 NATO);
- DLC1: **301** redirects (151 SNG + 150 NATO).

### Production exception

One confirmed world actor (`3D2445C74FAC2F8ADD61DA905FD9C965`) spawns `M_WodenMovableChair_21` but directly uses `DestructibleStash_AmmoSNG`. It remains in the allowlist because the runtime wiring is explicit and production-world, but it is flagged in the CSV as a non-crate exception rather than being silently counted as a wooden ammo crate.

## Explicit exclusions

### Test/debug destructibles

There are **42** ammo-family test/debug wiring rows across **12** physical roots:

- 1 direct SNG + 1 direct NATO reference in `TestMap_Balance`;
- 40 rank-wrapper references in `TestMap_Vortex_WP1` (5 physical roots × 4 ranks × SNG/NATO families).

These remain vanilla under the project's hard test/debug exclusion policy.

### Non-destructible ItemContainer paths

Three DLC1 `Bag` ItemContainers use Master ammo wrappers:

- one uses both `DestructibleStash_Ammo_NATO_Master` and `DestructibleStash_Ammo_SNG_Master`;
- one uses NATO Master only;
- one uses SNG Master only.

That is four protected cross-context wrapper references. They are not targets of the destructible control and are the concrete reason the shared vanilla ammo roots/wrappers must not be edited globally.

## Redirect contract

Each production manifest row rewrites exactly one field:

`SpawnActor root -> ItemGeneratorSettings.[0].PrototypeSID`

from:

- `DestructibleStash_AmmoSNG` -> `TCD_DestructibleStash_AmmoSNG`, or
- `DestructibleStash_AmmoNATO` -> `TCD_DestructibleStash_AmmoNATO`.

No test map, non-destructible ItemContainer, wrapper generator, SmartLoot setting, ammo `Weight`, item SID, or unrelated SpawnActor field belongs to this control surface.

## Control readiness

`Destructible Ammo Quantity` is now:

- semantics: **READY**;
- item rows: **READY**;
- production scope: **READY**;
- redirect manifest: **READY**;
- writer/UI: **IMPLEMENTED SINCE v0.2.9**;
- runtime status: **RUNTIME TEST REQUIRED**.

The implementation uses two TCD-owned ammo roots and the audited 1,235 direct production redirects described below.

## Machine-readable files

- `destructible-ammo-production-allowlist.csv`
- `destructible-ammo-production-redirect-manifest.csv`
- `destructible-ammo-production-exclusions.csv`
- `destructible-ammo-production-fanout-summary.json`


## v0.2.9 implementation status

`Ammo Crate Quantity` now uses this manifest directly from an embedded runtime resource. The writer creates TCD-owned SNG/NATO leaf pools, scales only `MinCount/MaxCount`, preserves all vanilla `Weight=2` values, and redirects only the 1,235 production assignments listed here. The control remains `runtime_test_required` until confirmed in game.
