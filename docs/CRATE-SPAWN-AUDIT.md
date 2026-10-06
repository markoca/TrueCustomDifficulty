# Build-Time Crate Reduction Audit

## Decision

True Custom Difficulty treats physical crate population reduction as a **build-time random actor-removal** problem. The current production surface exposes three independent category toggles plus one shared percentage:

- `loot.reduceAmmoCrates` — Ammo / grenade-pool wooden ammo crates.
- `loot.reduceFoodDrinkCrates` — dedicated Food & Drink DSP crates.
- `loot.reduceMedicalCrates` — dedicated metal Medical crates.
- `loot.crateRemovalPercent` — percentage removed from every enabled category.

The controls remove physical `WorldMap_WP` actor nodes. They do **not** disable or rewrite the `ItemGenerator` prototypes that define surviving crate contents.

## CFG mechanism

Each selected actor is emitted under its original source scope:

- Base game: `GameData/SpawnActorPrototypes/WorldMap_WP/<ActorSID>.cfg`
- DLC1: `DLCGameData/DLC1/SpawnActorPrototypes/WorldMap_WP/<ActorSID>.cfg`

with:

```cfg
<ActorSID> : removenode
```

DLC1 rows participate only when DLC1 is detected by the normal build path.

## Random selection contract

For each enabled category independently:

`K = round(eligible_actor_count × removal_percentage)`

using midpoint-away-from-zero rounding. A partial Fisher-Yates selection driven by `RandomNumberGenerator.GetInt32` chooses `K` unique rows without replacement. A new subset is chosen on every Build.

At 50%:

| Category | Base only | With DLC1 |
|---|---:|---:|
| Ammo | 521 / 1,042 | **708 / 1,416** |
| Food & Drink | 550 / 1,100 | **604 / 1,208** |
| Medical | 355 / 709 | **434 / 868** |

`0%` is a valid no-op. `100%` removes every eligible actor in every enabled category.

## Base reference paks

The supplied FULL/HALF paks define and prove the base-game category populations. HALF paks are evidence that hardcoded subsets work; True Custom Difficulty does not reuse their exact 50% selections.

| Reference | FULL | HALF | HALF relation |
|---|---:|---:|---|
| Ammo | **1,042** | 517 | pure FULL subset (49.6%) |
| Food & Drink | **1,100** | 541 | pure FULL subset (49.2%) |
| Medical | **709** | 353 | pure FULL subset (49.8%) |

Reference SHA-256:

- Ammo FULL: `bf7b31699f10b26358bb0f0731e52b42a7aa24ccf52d577135a866141488f978`
- Ammo HALF: `a175c2c97bdd0ecfa877fe917e7e1271584ff2b2b05d87d93c14ef9f28ed56f2`
- Food FULL: `9f11332b84307c03b83b83513dea20934e3c675aac4f58307a457959f04fb938`
- Food HALF: `3cd91f197706ee88200087363eefa31c6b10d94307c4e15174d9501252093df4`
- Medical FULL: `5a00d8b912741285bf8463446202750c6b9099864844a77023301e34e49452f1`
- Medical HALF: `0d2e460a630f5476ce9e77b2c362c9772f6467c9d532b04fdc81ace5ac3c3b09`

## DLC1 extension rule

The base FULL lists were matched exactly against `GameData/SpawnActorPrototypes.cfg`. Each category then uses the **same exact physical-prototype + generator-family classifier** against `DLCGameData/DLC1/SpawnActorPrototypes.cfg`. This is not a name-only extrapolation.

### Ammo

The production World Ammo Boxes surface now resolves to 981 base `D_WoodenAmmoCrate_01` actors. DLC contributes another 334. Dedicated F1 boxes are excluded and owned by the separate F1 surface.

| Generator | Base | DLC1 | Total |
|---|---:|---:|---:|
| `DestructibleStash_AmmoSNG` | 831 | 151 | **982** |
| `DestructibleStash_AmmoNATO` | 102 | 150 | **252** |
| `DestructibleStash_RGD` | 48 | 33 | **81** |
| **Total** | **981** | **334** | **1,315** |

The Ammo category includes AmmoSNG, AmmoNATO and RGD wooden ammo boxes only. Dedicated `DestructibleStash_F1` actors are intentionally excluded and controlled by the independent F1 surface.

### Food & Drink

| Physical prototype | Base | DLC1 | Total |
|---|---:|---:|---:|
| `D_WoodenDSPCrate_01` | 320 | 15 | **335** |
| `D_WoodenDSPCrate_02` | 308 | 35 | **343** |
| `D_WoodenDSPCrate_03` | 472 | 58 | **530** |
| **Total** | **1,100** | **108** | **1,208** |

All rows use the audited `DestructibleStash_Food_Drink...` family. Other containers capable of yielding food/drink remain out of scope.

### Medical

The supplied FULL pak's **709/709** actor SIDs resolve to `WorldMap_WP` `D_MetallCrate_01` actors using `DestructibleStash_Med`. Applying that exact classifier to DLC1 finds **159** additional actors.

- Base: **709**
- DLC1: **159**
- Combined: **868**

Medical crate reduction is independent from Medic Stock, NPC Medical Loot Frequency and medical contents in surviving crates.

## Separation from crate contents

Physical crate population and crate contents remain separate systems:

- crate reduction removes physical world actors;
- `Ammo Crate Quantity` changes item counts in surviving ammo crates;
- Food/Drink generator composition is untouched;
- Medical generator composition is untouched;
- NPC/stash/trader loot systems are untouched.

Weapons and Armor world-spawn reduction remain out of scope until their reference files are promoted separately.
