# Final Generator Decision Matrix — v0.2.42

This is the canonical decision index for generator-related gameplay concepts. It does **not** replace the detailed evidence audits; it records the final lifecycle decision and points back to the source surface/reason captured by the decision layer.

## Release invariants

- **28** decision concepts, exactly one row per `control-candidate-triage.csv` concept.
- Current normal UI still contains **28 generator-backed controls** verified by `GENERATOR-CONTROL-END-TO-END-VERIFICATION.md`.
- `IMPLEMENTED_RUNTIME_TEST_REQUIRED` / `KEEP_EXISTING_RUNTIME_TEST_REQUIRED` means the control **is already implemented**, but runtime semantics are not yet promoted to proven.
- `NO_UI_*` is a closed decision, not an invitation to rediscover the control in later audits.
- `AUDIT_ONLY_*` preserves generator knowledge without creating a normal difficulty control.

## Lifecycle summary

| Lifecycle bucket | Concepts |
|---|---:|
| `AUDIT_ONLY_DOCUMENTED` | 4 |
| `CLOSED_NO_UI` | 15 |
| `IMPLEMENTED_RUNTIME_TEST_REQUIRED` | 6 |
| `IMPLEMENTED_STATIC_VERIFIED` | 3 |
| `PROMOTION_CANDIDATE_RUNTIME_PROOF` | 0 |

## Decision matrix

| Concept | Label | Decision | Lifecycle | UI controls |
|---|---|---|---|---:|
| `loot.npcCorpseBonus` | Extra NPC Corpse Loot | `KEEP_EXISTING` | `IMPLEMENTED_STATIC_VERIFIED` | 1 |
| `loot.expensiveStashMoneyChance` | Expensive Stash Money Chance | `KEEP_EXISTING` | `IMPLEMENTED_STATIC_VERIFIED` | 1 |
| `loot.mutant.*Chance` | 14 Mutant Part Drop Chances | `KEEP_EXISTING` | `IMPLEMENTED_STATIC_VERIFIED` | 14 |
| `loot.npcDetectorChance` | Human NPC Detector Chance | `NO_UI_NOT_CORE_DIFFICULTY` | `CLOSED_NO_UI` | 0 |
| `loot.zombieDetectorChance` | Zombie Detector Chance | `NO_UI_NO_OBSERVABLE_LOOT_EFFECT` | `CLOSED_NO_UI` | 0 |
| `loot.zombieWeaponCondition` | Zombie Weapon Condition | `AUDIT_ONLY_NO_UI` | `AUDIT_ONLY_DOCUMENTED` | 0 |
| `loot.zombieAttachedAmmo` | Zombie Weapon Ammo | `AUDIT_ONLY_NO_UI` | `AUDIT_ONLY_DOCUMENTED` | 0 |
| `loot.droppedWeaponCondition` | NPC Weapon Condition | `IMPLEMENTED_RUNTIME_TEST_REQUIRED` | `IMPLEMENTED_RUNTIME_TEST_REQUIRED` | 1 |
| `loot.npcWeaponAmmoSupply` | Human NPC Dropped Ammo Amount | `IMPLEMENTED_RUNTIME_TEST_REQUIRED` | `IMPLEMENTED_RUNTIME_TEST_REQUIRED` | 1 |
| `loot.grenadeChance` | Human NPC Grenade Chance | `NO_UI_MIXED_VANILLA_SEMANTICS` | `CLOSED_NO_UI` | 0 |
| `loot.npcMedicalChance` | NPC Medical Loot Chance | `KEEP_EXISTING_RUNTIME_TEST_REQUIRED` | `IMPLEMENTED_RUNTIME_TEST_REQUIRED` | 1 |
| `loot.npcFoodFrequency` | Human NPC Food Loot Frequency | `NO_UI_DIFFICULTY_NEUTRAL` | `CLOSED_NO_UI` | 0 |
| `loot.npcUtilityFrequency` | Human NPC Utility Loot Frequency | `NO_UI_NOT_CORE_DIFFICULTY` | `CLOSED_NO_UI` | 0 |
| `loot.looseAmmoQuantity` | Human NPC Loose Ammo Quantity | `NO_UI_GENERIC_TARGET` | `CLOSED_NO_UI` | 0 |
| `loot.standardNpcArmor` | Human NPC Armor Chance | `NO_UI_AS_PERCENTAGE` | `CLOSED_NO_UI` | 0 |
| `loot.standardNpcNVG` | Human NPC NVG Chance | `NO_UI_NO_REPEATABLE_PRODUCTION_SURFACE` | `CLOSED_NO_UI` | 0 |
| `economy.traderStock` | Trader Stock | `NO_UI_AGGREGATE_MIXED_SEMANTICS` | `CLOSED_NO_UI` | 0 |
| `economy.medicStock` | Medic Stock | `IMPLEMENTED_RUNTIME_TEST_REQUIRED` | `IMPLEMENTED_RUNTIME_TEST_REQUIRED` | 1 |
| `loot.worldStashAbundance` | World Stash Abundance | `NO_UI_SINGLE_SCALAR` | `CLOSED_NO_UI` | 0 |
| `loot.destructibleAmmoQuantity` | Ammo Crate Quantity | `KEEP_EXISTING_RUNTIME_TEST_REQUIRED` | `IMPLEMENTED_RUNTIME_TEST_REQUIRED` | 1 |
| `loot.smartGrenadeSpawnMax` | SmartLoot Grenade Spawn Endpoint | `AUDIT_ONLY_RUNTIME_SEMANTICS_UNKNOWN` | `AUDIT_ONLY_DOCUMENTED` | 0 |
| `loot.mutantElectrocollar` | Mutant Electrocollar Drop | `AUDIT_ONLY_WEIGHT` | `AUDIT_ONLY_DOCUMENTED` | 0 |
| `loot.corpseOverride` | Corpse Override Loot | `NO_UI_SINGLE_SCALAR` | `CLOSED_NO_UI` | 0 |
| `loot.npcJunkMisc` | Human NPC Junk / Misc Loot | `NO_UI_SYSTEM_MARKER` | `CLOSED_NO_UI` | 0 |
| `economy.traderAmmoStock` | Trader Ammo Stock Quantity | `NO_UI_INCOMPLETE_QUANTITY_SURFACE` | `CLOSED_NO_UI` | 0 |
| `economy.traderWeaponStock` | Trader Weapon Stock | `NO_UI_MIXED_AVAILABILITY_MODELS` | `CLOSED_NO_UI` | 0 |
| `economy.traderAttachmentStock` | Trader Attachment Stock | `NO_UI_MIXED_AVAILABILITY_QUANTITY_SURFACE` | `CLOSED_NO_UI` | 0 |

## UI projection note

The 28-row decision layer is concept-oriented, not slider-oriented. Five verified stash/destructible UI controls are represented by broader generator audit families rather than dedicated triage rows: `loot.cheapStashAmmoQuantity`, `loot.cheapStashGrenadeChance`, `loot.cheapStashGrenadeQuantity`, `loot.mediumStashAmmoQuantity`, `loot.destructibleGrenadeQuantity`, and `loot.expensiveStashAmmoQuantity`. Together with the triage-mapped controls, the current verified UI total is **28**.

## Runtime-unverified implemented controls

- `loot.droppedWeaponCondition` — production writer/UI enabled on the locked 121-row / 33-loadout surface.
- `loot.npcWeaponAmmoSupply` — production writer/UI enabled on the same locked surface; exact `AmmoMaxCount` / `AmmoMaxcount` spelling is preserved.
- `economy.medicStock` — production writer/UI enabled on the isolated 48-row Universal + Noon medic stock surface.

All three remain `RUNTIME_TEST_REQUIRED`; later verification uses vanilla CFG -> generated override CFG structural diff plus in-game observation.

## Cleanup rule

A detailed MD/CSV may be removed only if it is neither canonical nor required evidence for a live decision/guard. This matrix is an index of decisions, not permission to discard the evidence that makes those decisions reproducible.

## Blueprint Shuffle addition

`loot.blueprintShuffle` is an implemented runtime-gated generator control over **77 world-only blueprint placements**. It uses a fixed allowlist and a build-time one-to-one derangement; quest/scripted and unresolved blueprint generators are excluded. See `generators/BLUEPRINT-SHUFFLE-AUDIT.md`.

