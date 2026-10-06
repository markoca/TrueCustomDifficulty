# Generator Control End-to-End Verification — v0.2.42

This is the release gate for every **generator-backed control currently exposed in the normal UI**. It verifies the complete static chain:

`UI parameter -> writer route -> CFG field/operation -> isolated generator production surface -> consumer meaning`

A statically exact mapping is not automatically a runtime proof. Controls whose CFG mapping is exact but whose observed in-game effect has not yet been demonstrated remain `RUNTIME_TEST_REQUIRED`.

## Result

| Result | Controls |
|---|---:|
| `STATIC_1_TO_1_VERIFIED` | **16** |
| `STATIC_VERIFIED_RUNTIME_TEST_REQUIRED` | **11** |
| `INVALID_OR_PARTIAL_UI_CONTROL` | **0** |
| **Total generator-backed UI controls** | **28** |

No current generator-backed UI label was found to promise a broader gameplay domain than its writer actually owns. Production exclusions (quest/scripted/test/unresolved/shared paths) are deliberate scope boundaries, not silent partial coverage.

## Control matrix

| Parameter | UI label | Writer / source surface | Static verdict | Runtime gate |
|---|---|---|---|---|
| `loot.npcCorpseBonus` | Extra NPC Corpse Loot | Exact `DifficultyPrototypes.CorpseSmartLoot.GeneralNPC` set/list state | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.npcMedicalChance` | NPC Medical Loot Chance | 33 repeatable redirects -> 6 TCD pools -> 58 medical `Chance` rows | `STATIC_VERIFIED_RUNTIME_TEST_REQUIRED` | in-game |
| `loot.npcWeaponAmmoSupply` | NPC Dropped Ammo Amount | 121 direct weapon rows / 33 repeatable loadouts; `AmmoMinCount` + exact source max-key spelling | `STATIC_VERIFIED_RUNTIME_TEST_REQUIRED` | in-game + CFG diff |
| `loot.droppedWeaponCondition` | NPC Weapon Condition | Same 121/33 isolated surface; `MinDurability/MaxDurability` only | `STATIC_VERIFIED_RUNTIME_TEST_REQUIRED` | in-game + CFG diff |
| `loot.cheapStashAmmoQuantity` | Basic Stash Ammo Quantity | 1,271 production redirects; ammo `MinCount/MaxCount` only | `STATIC_VERIFIED_RUNTIME_TEST_REQUIRED` | in-game |
| `loot.cheapStashGrenadeChance` | Basic Stash Grenade Chance | Same 1,271 production boundary; one existing `GrenadeRGD5.Chance` field in TCD-owned template | `STATIC_VERIFIED_RUNTIME_TEST_REQUIRED` | in-game |
| `loot.cheapStashGrenadeQuantity` | Basic Stash RGD-5 Quantity | Same 1,271 production boundary; exact `GrenadeRGD5.MinCount/MaxCount` in TCD-owned template | `STATIC_VERIFIED_RUNTIME_TEST_REQUIRED` | in-game |
| `loot.mediumStashAmmoQuantity` | Standard Stash Ammo Quantity | 450 production redirects; 25 ammo count rows in complete TCD-owned Medium templates | `STATIC_VERIFIED_RUNTIME_TEST_REQUIRED` | in-game |
| `loot.destructibleAmmoQuantity` | Ammo Crate Quantity | 1,235 production actors (983 SNG / 252 NATO); TCD-owned ammo leaf counts only | `STATIC_VERIFIED_RUNTIME_TEST_REQUIRED` | in-game |
| `loot.destructibleGrenadeQuantity` | Grenade Crate Quantity | 182 production actors (101 F1 / 81 RGD5); TCD-owned leaf `MinCount/MaxCount` only | `STATIC_VERIFIED_RUNTIME_TEST_REQUIRED` | in-game |
| `loot.expensiveStashAmmoQuantity` | Rare Stash Ammo Quantity | 305 production redirects; ammo counts only in complete TCD-owned Expensive templates | `STATIC_VERIFIED_RUNTIME_TEST_REQUIRED` | in-game |
| `economy.medicStock` | Medic Stock Quantity | 48 direct rows: 25 Universal + 23 Noon; `MinCount/MaxCount` only | `STATIC_VERIFIED_RUNTIME_TEST_REQUIRED` | refreshed trader stock + CFG diff |
| `loot.expensiveStashMoneyChance` | Rare Stash Coupon Chance | 64 explicit difficulty-gated coupon `Chance` edit points; preserved/materialized when Expensive Ammo clone path is active | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.mutant.blindDogChance` | Blind Dog Part Drop Chance | one local single-item `Chance` row | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.mutant.bloodsuckerChance` | Bloodsucker Part Drop Chance | one local single-item `Chance` row | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.mutant.boarChance` | Boar Part Drop Chance | one local single-item `Chance` row | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.mutant.burerChance` | Burer Part Drop Chance | one local single-item `Chance` row | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.mutant.catChance` | Bayun Part Drop Chance | one local `CatLootGenerator/CatLoot` Chance row; legacy internal ID retained for preset compatibility | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.mutant.chimeraChance` | Chimera Part Drop Chance | one local single-item `Chance` row | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.mutant.controllerChance` | Controller Part Drop Chance | one local single-item `Chance` row | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.mutant.deerChance` | Deer Part Drop Chance | one local single-item `Chance` row | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.mutant.fleshChance` | Flesh Part Drop Chance | one local single-item `Chance` row | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.mutant.poltergeistChance` | Poltergeist Part Drop Chance | one local single-item `Chance` row | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.mutant.pseudodogChance` | Pseudodog Part Drop Chance | one local single-item `Chance` row | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.mutant.pseudogiantChance` | Pseudogiant Part Drop Chance | one local single-item `Chance` row | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.mutant.tushkanChance` | Tushkan Part Drop Chance | one local single-item `Chance` row | `STATIC_1_TO_1_VERIFIED` | none |
| `loot.mutant.snorkChance` | Snork Part Drop Chance | one local single-item `Chance` row | `STATIC_1_TO_1_VERIFIED` | none |

## Important boundaries

- `loot.mutant.catChance` is intentionally **not renamed internally**. The UI uses **Bayun**, while the legacy parameter ID and raw CFG `Cat*` identifiers remain compatibility contracts for existing presets/source data.
- Cheap/Medium/Rare stash controls operate only on confirmed normal-world production fan-out. Quest/scripted/test and unresolved wrappers remain vanilla by design.
- Destructible Ammo/Grenade controls use production-only clone/redirect surfaces; shared vanilla leaf pools are not globally edited.
- Human NPC Medical edits only medical `Chance` rows in six TCD-owned consumable clones and only redirects the 33 repeatable production loadouts. MIK/Mykluha and Prologue are excluded.
- Zombie Grenade Frequency scales each existing source Chance relative to its own vanilla value. It does not create a grenade branch for the two generic roots that have none.
- Rare Stash Coupon Chance is independent from ammo quantity. When the ammo control causes TCD-owned Expensive templates to be emitted, the selected coupon Chance is carried into those templates so the two controls compose instead of bypassing each other.

## Release decision

**KEEP all 28 current generator-backed UI controls.**

There is no static evidence for removing or broadening any of them. The eleven runtime-gated controls must not be upgraded to runtime-proven status until their in-game tests pass. The three former runtime-proof candidates (NPC weapon condition, NPC weapon ammo supply and Medic Stock Quantity) are now implemented in the normal UI/writer but remain runtime-unverified. Trader aggregates, zombie weapon condition/ammo and other closed/audit-only concepts remain outside the normal UI.

## Blueprint Shuffle

`loot.blueprintShuffle` uses an explicit 77-row world-only allowlist. Each row maps one unique `Stash_Blueprint_*` generator to one unique blueprint item and one unique physical world consumer. The writer performs a build-time derangement without replacement, so the eligible blueprint set and slot count remain unchanged while every included blueprint moves away from its vanilla location.

Quest blueprint `Blueprint_FaustPsyResist_Quest_1_1` and the two unresolved `SciSEVA_Scientific` stash generators are excluded. Runtime smoke test remains required.

## World Weapon Density

- `loot.worldWeaponShuffle` is the master toggle.
- `loot.worldWeaponDensity` is a runtime-gated child slider.
- 36 regular weapon models are always included in the distribution contract.
- Safe surface: 296 base locations / 376 with DLC1.
- Compatibility: pistol->pistol, primary->primary, progression tier ±1 maximum.
- Exact max-flow allocation guarantees the minimum copies of every model before density fill.
- Base guarantee scales from once to at most 6 times each; DLC1 from once to at most 8 times each.
- Quest/scripted, unique, Guard, Gauss and RPG placements remain excluded.
- Status: `runtime_test_required`.
