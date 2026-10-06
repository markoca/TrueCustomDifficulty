# World Weapon Placement Audit

Version: 0.9.26.44

## Decision

The old **14-slot World Weapon Shuffle allowlist is not the full world-weapon placement surface**. It covers only a small set of already-proven weapon entries inside stash `ItemGenerator` nodes.

A direct audit of the raw base-game and DLC1 `SpawnActorPrototypes.cfg` files found:

| Surface | Base | DLC1 | Total |
|---|---:|---:|---:|
| All direct weapon `ESpawnType::Item` actors across all levels | 498 | 106 | 604 |
| Direct weapon actors on exact main `WorldMap_WP` | 313 | 90 | 403 |
| **Conservative SAFE_REGULAR_DIRECT** | 282 | 80 | **362** |
| Existing proven stash-generator weapon slots | 14 | 0 | 14 |
| **Total currently proven safe spawn slots** | **296** | **80** | **376** |

`SAFE_REGULAR_DIRECT` is deliberately conservative. A direct actor qualifies only when all of these are true:

1. `LevelName = WorldMap_WP` exactly;
2. `SpawnType = ESpawnType::Item`;
3. `SpawnOnStart = true`;
4. the current item is a regular weapon model, not a named unique/Guard/DLC-special variant;
5. the actor SID is not referenced by the audited quest CFG.

This gives **376 currently proven safe weapon spawn slots** when DLC1 is installed.

## Exact `WorldMap_WP` classification

| Classification | Base | DLC1 | Total |
|---|---:|---:|---:|
| SAFE_REGULAR_DIRECT | 282 | 80 | 362 |
| SPECIAL_WEAPON | 13 | 4 | 17 |
| UNIQUE_VARIANT | 9 | 0 | 9 |
| DLC_VARIANT_OR_SPECIAL | 0 | 3 | 3 |
| GUARD_VARIANT | 0 | 2 | 2 |
| SCRIPTED_OR_DEFERRED | 9 | 1 | 10 |
| QUEST_REFERENCED | 0 | 0 | 0 |

The exact main-world direct surface contains **403 weapon actors**; **362 (89.8%)** pass the conservative regular-world filter.

## Regular base-game weapon models

The previous generic replacement pool contained 34 fully tiered models. This audit adds two ordinary models that are clearly part of the normal weapon ecosystem but still lack a progression-tier mapping:

- `GunFora_ST` — Rifle — tier **UNMAPPED**.
- `GunPKP_MG` — Machine gun — tier **UNMAPPED**.

That gives **36 regular base-game weapon models** for a future one-per-model distribution system:

- `GunMark_SP` — DMR, T2; currently appears in 3 conservative safe direct slots
- `GunSKP_DMR` — DMR, T3; currently appears in 1 conservative safe direct slots
- `GunGP3A_DMR` — DMR, T4; currently appears in 2 conservative safe direct slots
- `GunPKP_MG` — MG, T3; currently appears in 6 conservative safe direct slots
- `GunPM_HG` — Pistol, T1; currently appears in 20 conservative safe direct slots
- `GunUDP_HG` — Pistol, T1; currently appears in 4 conservative safe direct slots
- `GunKora_HG` — Pistol, T2; currently appears in 7 conservative safe direct slots
- `GunM10_HG` — Pistol, T2; currently appears in 4 conservative safe direct slots
- `GunAPB_HG` — Pistol, T3; currently appears in 4 conservative safe direct slots
- `GunRhino_HG` — Pistol, T3; currently appears in 6 conservative safe direct slots
- `GunAK74_ST` — Rifle, T1; currently appears in 76 conservative safe direct slots
- `GunG37_ST` — Rifle, T2; currently appears in 6 conservative safe direct slots
- `GunGvintar_ST` — Rifle, T2; currently appears in 3 conservative safe direct slots
- `GunM16_ST` — Rifle, T2; currently appears in 10 conservative safe direct slots
- `GunArev_ST` — Rifle, T3; currently appears in 2 conservative safe direct slots
- `GunGrim_ST` — Rifle, T3; currently appears in 4 conservative safe direct slots
- `GunLavina_ST` — Rifle, T3; currently appears in 6 conservative safe direct slots
- `GunDnipro_ST` — Rifle, T4; currently appears in 8 conservative safe direct slots
- `GunKharod_ST` — Rifle, T4; currently appears in 9 conservative safe direct slots
- `GunFora_ST` — Rifle, T2; currently appears in 6 conservative safe direct slots
- `GunAKU_PP` — SMG, T1; currently appears in 44 conservative safe direct slots
- `GunViper_PP` — SMG, T1; currently appears in 25 conservative safe direct slots
- `GunBucket_PP` — SMG, T2; currently appears in 8 conservative safe direct slots
- `GunFora230_PP` — SMG, T2; currently appears in 3 conservative safe direct slots
- `GunIntegral_PP` — SMG, T3; currently appears in 7 conservative safe direct slots
- `GunZubr_PP` — SMG, T3; currently appears in 5 conservative safe direct slots
- `GunObrez_SG` — Shotgun, T1; currently appears in 9 conservative safe direct slots
- `GunTOZ_SG` — Shotgun, T1; currently appears in 10 conservative safe direct slots
- `GunM860_SG` — Shotgun, T2; currently appears in 15 conservative safe direct slots
- `GunSPSA_SG` — Shotgun, T3; currently appears in 15 conservative safe direct slots
- `GunD12_SG` — Shotgun, T4; currently appears in 3 conservative safe direct slots
- `GunRam2_SG` — Shotgun, T4; currently appears in 4 conservative safe direct slots
- `GunThreeLine_SP` — Sniper, T1; currently appears in 1 conservative safe direct slots
- `GunSVDM_SP` — Sniper, T2; currently appears in 15 conservative safe direct slots
- `GunM701_SP` — Sniper, T3; currently appears in 5 conservative safe direct slots
- `GunSVU_SP` — Sniper, T4; currently appears in 6 conservative safe direct slots

Two base models remain intentionally outside the regular pool:

- `GunRpg7_GL` — launcher/special.
- `GunGauss_SP` — special/endgame weapon.

Named unique derivatives remain excluded from the generic pool.

## Progression tier decisions for Fora and PKP

### `GunFora_ST` -> T2

Evidence from `ItemGeneratorPrototypes.cfg`:

- ordinary trader surfaces start exposing Fora at `ERank::Experienced`;
- ordinary NPC pools repeatedly place it beside known T2 rifles (`GunM16_ST`, `GunG37_ST`, `GunGvintar_ST`);
- its base `Cost = 15000`, between the existing T2 rifle reference values (M16 14,000; G37 17,000; Gvintar 21,000).

This is strong enough to lock **T2**.

### `GunPKP_MG` -> T3

Evidence from `ItemGeneratorPrototypes.cfg`:

- generic heavy-NPC progression first introduces PKP at `ERank::Veteran` in Military and Varta pools;
- Duty delays it to Master and Corpus uses Veteran/Master, which marks it as a late/high-tier heavy weapon rather than an early weapon;
- `Cost = 30000`, consistent with upper T3 / low T4 weapon economics;
- dedicated `*MachineGunnerWeaponNewbie/Experienced/Veteran/MasterGenerator` families contain PKP at every rank because PKP is the role-defining machine gun. Those role generators are therefore **not** evidence that the weapon itself is T1.

For world-distribution progression, **T3** is the most faithful mapping: it becomes eligible at the Veteran-style progression band without treating it as Master-only T4.

## DLC1 weapon models

DLC1 defines **12** additional weapon prototypes:

- `GunAK9_ST`
- `GunArevLight_AR`
- `GunClapper_SG`
- `GunDefender_SG`
- `GunDniproDuty_ST`
- `GunFallschirmjager_DMR`
- `GunHSG72_DMR`
- `GunKharodFreedom_ST`
- `GunSPSA_Sniper_SG`
- `GunSVU_CQB_SP`
- `GunTundra_DMR`
- `GunWarszawa_AR`

They are **not yet promoted into the generic replacement pool**. Their progression/uniqueness semantics are not generic enough in the current evidence; several are tied to named NPC/reward generators, while only three appear as direct main-world pickups.

## Quest/deferred safety

- Base exact-world actor SIDs referenced by `QuestNodePrototypes.cfg`: **2**.
- DLC1 exact-world actor SIDs referenced by `QuestNodePrototypes.cfg`: **1**.
- All such referenced actors are also deferred (`SpawnOnStart=false`) in this data, so the conservative filter excludes them.

## Consequence for the proposed one-per-weapon system

- **36** regular base weapon models.
- **296** proven safe slots in base game.
- **376** proven safe slots with DLC1 installed.

If all 36 regular base models are allowed to spawn at most once, only **36 / 296 = 12.2%** of base safe slots need to be occupied.
With DLC1 installed it becomes **36 / 376 = 9.6%**.

This is exactly the shape needed for the proposed design: many possible locations, a small number of actual weapon spawns, and no duplicate regular weapon model per build.

## Implementation implication

v0.9.26.44 replaces the old 14-slot replacement shuffle with the production **unique density distribution** writer.

The writer now uses all 282 safe base direct actors, 80 optional DLC1 direct actors, and the 14 proven generator slots. It does not transplant weapon SIDs between unrelated locations: for each selected weapon model it keeps one randomly chosen location where that model was already authored and removes the other eligible copies. Quest/unique/special/deferred placements remain untouched.

**No production gameplay behavior was changed by this audit.**

## Source provenance

- `STALKER2-GameData-CFG(2).zip` — SHA-256 `7610b16218310c7345796ff26b1554c73e3d2ca775f6a50108a7406cb7f33da1`
- `STALKER2-DLC1-CFG(2).zip` — SHA-256 `3c5cb53f1769619b510ae3780142d92639c083a4a755a8717a267710c3c849e6`

Detailed data:

- `docs/generators/world-weapon-direct-placement-audit.csv`
- `docs/generators/world-weapon-regular-pool-audit.csv`
