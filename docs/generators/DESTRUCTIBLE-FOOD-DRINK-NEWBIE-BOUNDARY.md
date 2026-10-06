# Destructible Food/Drink Newbie — unresolved production boundary

## Decision

`DestructibleStash_Food_Drink_Newbie` is a **real production dependency**, but its generator definition is absent from the supplied CFG corpus. It is therefore a **hard writer boundary**: do not expose it as a UI target, do not synthesize its missing definition, and do not redirect its consumers until the actual vanilla definition or runtime behavior is independently proven.

## Proven physical footprint

The supplied `SpawnActorPrototypes.cfg` wiring contains exactly **20 production-world references** to this SID. All 20 are:

- map: `WorldMap_WP`;
- rank: `ERank::Newbie`;
- context: `production_world`;
- destructible wooden-box actors.

Prototype distribution:

| Spawned prototype | Production references |
| --- | ---: |
| `D_WoodenBox_01` | 4 |
| `D_WoodenBox_02` | 7 |
| `D_WoodenBox_03` | 6 |
| `D_WoodenBox_04` | 3 |
| **Total** | **20** |

The complete actor list is stored in `destructible-food-drink-newbie-production-refs.csv`.

## What is not proven

The supplied generator definitions contain no block for `DestructibleStash_Food_Drink_Newbie`. Therefore the project cannot prove any of the following for this SID:

- direct items;
- nested generators;
- `Weight`/`Chance` semantics;
- `MinCount`/`MaxCount` semantics;
- equivalence to `DestructibleStash_Food_Drink` or to the Experienced/Veteran/Master wrappers.

The family name is not evidence of identical implementation. The known base wrapper uses `Weight`, while the known rank-specific wrappers use `Chance`; copying either shape into the missing Newbie definition would be fabrication.

## UI/writer consequence

This boundary reinforces the existing decisions:

- **Food / Drink Frequency:** no generic UI control. The known family already mixes Weight and Chance semantics, and the production Newbie wrapper is unresolved.
- **Food / Drink Quantity:** deferred. The resolved leaf rows also mix fixed `1–1` and `1–2` counts, so a generic quantity percentage would not be consistently meaningful.
- **Runtime writer:** must not target or redirect `DestructibleStash_Food_Drink_Newbie` with current evidence.

## Reopen condition

Revisit only if one of these becomes available:

1. the actual vanilla CFG definition for `DestructibleStash_Food_Drink_Newbie`; or
2. runtime evidence that establishes the exact generator shape and semantics.

Until then, the unresolved state is intentional, not an audit gap.
