# Destructible Grenade Production Fan-Out

## Decision

`Destructible Grenade Quantity` has a closed, production-only scope and is implemented in v0.2.10.

The two vanilla leaf pools are deliberately not edited in place. The writer materializes TCD-owned copies and redirects only audited production destructible actors:

- `DestructibleStash_F1` -> `TCD_DestructibleStash_F1`
- `DestructibleStash_RGD` -> `TCD_DestructibleStash_RGD`

Each pool contains exactly one item and exposes only `Weight` plus `MinCount/MaxCount`:

| Pool | Item | Weight | Vanilla count | Production redirects |
|---|---|---:|---:|---:|
| `DestructibleStash_F1` | `GrenadeF1` | 1 | 1-2 | 101 |
| `DestructibleStash_RGD` | `GrenadeRGD5` | 1 | 1-2 | 81 |

Total production scope: **182 unique physical wooden ammo-crate actors**. Every audited row is `production_world`, uses `ERank::Newbie`, and directly references one of the two leaf pools. No unresolved or cross-context consumer exists in the audited grenade surface.

## Quantity contract

The UI exposes **50-200%** in **50%** steps. Counts use positive round-half-up scaling from the vanilla `1-2` range:

| UI | Generated count |
|---:|---:|
| 50% | 1-1 |
| 100% | 1-2 |
| 150% | 2-3 |
| 200% | 2-4 |

`Weight=1` and the physical crate's F1/RGD assignment are preserved. There is no local `Chance` field, so this surface does **not** justify a grenade frequency/chance control.

## Runtime contract

The embedded manifest `destructible-grenade-production-redirect-manifest.csv` rewrites exactly one field per production actor:

`SpawnActor root -> ItemGeneratorSettings.[0].PrototypeSID`

The control remains `runtime_test_required` until the generated CFG is built and verified in game.
## Final UI boundary — v0.2.22

The grenade destructible branch is now considered **closed for UI discovery**.

- Keep **Grenade Crate Quantity** as the only control for this branch.
- Do **not** add a Grenade Crate Frequency/Chance control: neither vanilla leaf exposes a local `Chance` field.
- Do **not** reinterpret `Weight=1` as frequency. Each leaf contains exactly one item, so changing that value would not be a proven crate-occurrence control.
- Do **not** merge this surface with Zombie Grenade Frequency. Zombie loadouts are a separate generator domain with different semantics and writer targets.

The audited consumer footprint is also closed: `DestructibleStash_F1` has 101 direct/propagated consumers and `DestructibleStash_RGD` has 81; every one is `destructible_world`. Both have zero audited ItemContainer, human NPC, zombie NPC, mutant, or corpse-override consumers.

The TCD clone/redirect design is therefore conservative rather than compensating for a known cross-context consumer: it keeps vanilla leaf definitions untouched and constrains runtime edits to the exact 182 audited production actors.

