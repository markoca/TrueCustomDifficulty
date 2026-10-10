# True Custom Difficulty — Configuration Baseline

**Status:** Baseline v0.2 — audited against runtime 2.0.5  
**Primary source:** Runtime/game CFG dump from STALKER 2 2.0.5 with DLC content  
**Dump label:** `Stalker2 Config Files Dump 2.0.5 1653 2.0.5 2026-09-11T13-40Z`  
**Purpose:** Technical source-of-truth for True Custom Difficulty.

---

## 1. Scope and source precedence

This document records confirmed configuration structures, vanilla values, difficulty-specific values, and research/control surfaces discovered in the supplied CFG dumps. Reference/control-surface lists are not themselves a support contract; current support is defined by `parameters.json` plus `cfg-writer-baseline.json`.

The source precedence for future work is:

1. **Runtime/game + DLC CFG dump** — primary source of truth for actual game-side values.
2. **ZoneKit Survival CFG subset** — development/reference source only.
3. **Standalone ZoneKit root CFG files** — supplementary reference only.
4. Assumptions or inferred behavior must never override runtime CFG values without a runtime test.

The runtime dump contains **631 CFG files**:

| Source area | CFG files |
|---|---:|
| Base `GameData` | 485 |
| `DLCGameData/DLC1` | 118 |
| `DLCGameData/PreOrder` | 11 |
| `DLCGameData/Deluxe` | 10 |
| `DLCGameData/Ultimate` | 7 |

The supplied ZoneKit survival subset contains **372 CFG files**.

Several important files differ between the runtime dump and ZoneKit. `CoreVariables.cfg` is byte-identical in the compared copies, but `DifficultyPrototypes.cfg`, `EffectPrototypes.cfg`, `ItemGeneratorPrototypes.cfg`, `StashPrototypes.cfg`, `TradePrototypes.cfg`, `ObjSleepParamsPrototypes.cfg`, and `ObjWeightParamsPrototypes.cfg` differ. Therefore, runtime values in this document take precedence.

---

## 2. Evidence status vocabulary

This baseline separates source evidence, writer implementation, and runtime behavior. They are related, but they are not the same claim.

| Status | Meaning |
|---|---|
| **CFG CONFIRMED / CONFIRMED** | The field/value/structure is directly present in the runtime 2.0.5 CFG dump. This proves the source mapping, not the gameplay effect. Existing table rows may use the shorter `CONFIRMED`. |
| **WRITER-MAPPED** | The tool has a concrete deterministic output contract to the exact CFG file/root/node/key or a separately documented special writer. |
| **RUNTIME CONFIRMED** | The generated setting has also been tested in game and produced the intended player-visible behavior. |
| **RUNTIME TEST REQUIRED** | The CFG relationship and writer path are known, but the exact in-game semantics still need confirmation. |
| **DEFERRED / RESEARCH** | Related CFG data exists, but exposing one control would require an aggregate selector, transform, inheritance rule, or other design that is not yet sufficiently proven. |

`parameters.json` currently uses `confirmed` for CFG-confirmed/writer-mapped controls and `runtime_test_required` for structurally mapped controls whose gameplay semantics still need an in-game test. A `confirmed` catalog entry must therefore not be read as automatically runtime-confirmed.

---

## 3. Difficulty profile mapping

The internal PC difficulty SIDs map to the UI difficulty names as follows:

| Internal SID | UI difficulty |
|---|---|
| `Easy` | **Novice** |
| `Medium` | **Stalker** |
| `Hard` | **Veteran** |
| `Stalker` | **Master** |
| `Custom` | **Custom** |

This naming collision is important: the internal SID `Stalker` is the **Master** difficulty.

The runtime dump also contains Xbox-specific variants (`Easy_Xbox`, `Medium_Xbox`, `Hard_Xbox`, `Stalker_Xbox`) and a `Default` prototype. The baseline target for the tuner is the PC profile set unless explicitly expanded later.

---

## 4. Dedicated `Custom` prototype — not used by this tool

The runtime `DifficultyPrototypes.cfg` contains a dedicated `Custom` prototype with mostly neutral/default values.

True Custom Difficulty does **not** require the player to select that prototype. The player chooses one of the existing vanilla difficulties in-game, and the tool's **Base Preset** identifies which vanilla difficulty prototype receives the generated BPatch overrides:

- Novice -> `Easy`
- Stalker -> `Medium`
- Veteran -> `Hard`
- Master -> `Stalker`

The custom **preset** is therefore the saved collection of tool values, while the **Base Preset** is the vanilla game difficulty being overridden. The game continues to report and run that vanilla difficulty selection.

---

# 5. Vanilla difficulty reference table

These are the exact PC difficulty values found in `DifficultyPrototypes.cfg`.

## 5.1 NPC combat

| CFG field | Novice | Stalker | Veteran | Master |
|---|---:|---:|---:|---:|
| `NPC_HP` | 1.00 | 1.20 | 1.00 | 1.50 |
| `NPC_Weapon_BaseDamage` | 0.17 | 0.85 | 1.02 | 1.35 |
| `NPC_AttackCooldown` | 1.00 | 1.00 | 1.00 | 1.00 |
| `HipAccuracyMultiplier` | 1.00 | 1.00 | 1.00 | 1.00 |
| `PlayerWeapon_HeadshotMultiplier` | 1.00 | 1.00 | 1.00 | 2.00 |
| `Weapon_JammingMultiplier` | 1.00 | 1.00 | 1.00 | 1.50 |
| `AccumulatedDamageReductionCurveWeightMax` | 1.00 | 0.75 | 0.50 | 0.40 |
| `AccumulatedDamageReductionCurveWeightMin` | 1.00 | 0.33 | 0.15 | 0.12 |
| `AccumulatedDamageReductionIncludesHealedHealth` | false | false | true | true |

**Note:** Master also contains special PSY phantom overrides not represented by a simple global percentage.

## 5.2 Mutant combat

| CFG field | Novice | Stalker | Veteran | Master |
|---|---:|---:|---:|---:|
| `Mutant_BaseDamage` | 0.35 | 1.00 | 1.35 | 1.50 |


## 5.3 Environment / survival difficulty

| CFG field | Novice | Stalker | Veteran | Master |
|---|---:|---:|---:|---:|
| `Effect_Bleeding` | 0.25 | 1.00 | 1.00 | 1.50 |
| `Effect_Degen_Bleeding` | 1.00 | 1.00 | 1.00 | 1.00 |
| `Regen_HP` | 1.00 | 1.00 | 0.75 | 0.50 |
| `Radiation_AccumulationSpeed` | 0.60 | 1.00 | 1.50 | 1.85 |
| `Anomaly_Damage` | 0.30 | 1.00 | 1.45 | 1.75 |
| `Explosion_BaseDamage` | 0.25 | 1.00 | 1.00 | 1.20 |
| `Armor_Durability` | 1.35 | 1.00 | 0.90 | 0.60 |
| `Effect_Satiety` | 1.00 | 1.00 | 2.00 | 2.50 |
| `Effect_Sleepiness` | 1.00 | 1.00 | 1.00 | 1.00 |
| `Weapon_BaseDamage` | 1.30 | 1.00 | 0.75 | 0.75 |
| `Weather_Storm_Weight` | 1.00 | 1.00 | 1.00 | 1.25 |
| `Weather_Emission_Weight` | 1.00 | 1.00 | 1.00 | 1.25 |

> **Project scope:** weather/emission occurrence, weights, frequency and scheduling are reference-only and are not exposed as tuning controls. Only their gameplay effects may be tuned.

### Important semantic caution

`Effect_Satiety` is confirmed as a difficulty scalar, but the CFG dump alone does **not** prove exactly where the runtime applies that scalar. It must not yet be labeled “food effectiveness” in the UI. The safe label during development is the raw field name or a clearly marked interpretation until runtime-tested.

The same rule applies whenever a difficulty field is a high-level scalar whose application code is outside the CFG dump.

## 5.4 Economy difficulty

| CFG field | Novice | Stalker | Veteran | Master |
|---|---:|---:|---:|---:|
| `BuyCondition` | 1.00 | 1.00 | 0.65 | 0.55 |
| `SellCondition` | 1.00 | 1.00 | 1.25 | 1.65 |
| `Upgrade_Cost` | 1.00 | 1.00 | 1.25 | 1.50 |
| `Reward_MainLine_Money` | 1.35 | 1.00 | 0.75 | 0.55 |
| `Reward_SideLine_Money` | 1.35 | 1.00 | 0.75 | 0.55 |
| `Consumable_Cost` | 1.00 | 1.00 | 1.00 | 1.00 |
| `Ammo_Cost` | 1.00 | 1.00 | 1.00 | 1.00 |
| `Armor_Cost` | 1.00 | 1.00 | 1.00 | 1.00 |
| `Weapon_Cost` | 1.00 | 1.00 | 1.00 | 1.00 |
| `Artifact_Cost` | 1.00 | 1.00 | 1.00 | 1.00 |
| `Repair_Cost` | 1.00 | 1.00 | 1.00 | 1.00 |

The difficulty layer does **not** currently vary the listed item-cost multipliers or `Repair_Cost`; economy differences primarily come through buy/sell condition, upgrade costs, and quest rewards.

---

# 6. Player base survival state

Source: `GameData/ObjPrototypes.cfg`, `Player` prototype.

## 6.1 Vital maxima and base rates

| Property | Vanilla value | Status |
|---|---:|---|
| `MaxHP` | 100 | CONFIRMED |
| `MaxSP` | 100 | CONFIRMED |
| `MaxBleeding` | 100 | CONFIRMED |
| `MaxRadiation` | 100 | CONFIRMED |
| `MaxSleepinessPoints` | 100 | CONFIRMED |
| `MaxHungerPoints` | 100 | CONFIRMED |
| `MaxThirstPoints` | 100 | CONFIRMED |
| `MaxSuppressionPoints` | 100 | CONFIRMED |
| `RegenHungerPoints` | 0.015 | CONFIRMED |
| `RegenThirstPoints` | 0.0 | CONFIRMED |
| `DegenBleeding` | 0.3 | CONFIRMED |
| `DegenRadiation` | 0.05 | CONFIRMED |
| `DegenPsyPoints` | 1.0 | CONFIRMED |
| `DegenSuppressionPoints` | 33.0 | CONFIRMED |
| `RegenSleepinessPoints` | 0.01 | CONFIRMED |
| `RegenHP` | 0.0 | CONFIRMED |
| `RegenSP` | 5.0 | CONFIRMED |
| `RegenHPDelayTimeSeconds` | 5 | CONFIRMED |
| `RegenHealthModifier` | 1.0 | CONFIRMED |

The Player prototype explicitly applies these mechanics:

`HungerMechanics`, `BleedingMechanics`, `SleepMechanics`, `PsyMechanics`, `RadiationMechanics`, `HealthMechanics`, `DrunknessMechanics`, `PoppyFieldSleepinessMechanics`, `OverDrunknessMechanics`, `AdditionalRegenStaminaMechanics`, `SuppressionMechanics`, `FallHeightMechanics`, `EnergeticOveruseMechanics`, and `EnergeticToleranceMechanics`.

---

# 7. Stamina

## 7.1 Per-action base stamina costs

Source: `ObjPrototypes.cfg`, `Player.StaminaPerAction`.

| Action | Vanilla |
|---|---:|
| Sprint | 5.25 |
| Jump | 16.0 |
| Melee normal | 16.0 |
| Melee strong | 25.0 |
| Melee buttstock | 12.0 |
| Vault | 12.0 |
| Walk | 0 |
| Run | 0 |
| Jogging | 0 |
| Crouch | 0 |
| Low crouch | 0 |
| Climb | 0 |

## 7.2 Stamina regeneration state coefficients

Source: `CoreVariables.cfg`.

| State | Coefficient |
|---|---:|
| `SprintUnderRunSpeed` | -0.65 |
| `Sprint` | -1.40 |
| `Jogging` | 0 |
| `SlowRun` | 0 |
| `Run` | -0.65 |
| `Aim` | -0.25 |

## 7.3 Stamina disable threshold and low-SP recovery

The Player prototype contains a sprint threshold at `16.1` stamina with a `1` second regeneration delay for the sprint state.

`AdditionalRegenStaminaMechanics` separately switches into its low-SP branch below the `30` SP boundary and applies `IncreaseStaminaPoints` (`RegenStamina = 10`, `Duration = 4 s`).

## 7.4 Vault traversal

Runtime `Player.VaultingParams` defines:

| Field | Vanilla |
|---|---:|
| `MinObstacleHeight` | 70 |
| `MaxObstacleHeight` | 130 |

The tuner exposes `MaxObstacleHeight` only. Higher traversal reference markers are presentation aids and require runtime validation because vault success also depends on collision, landing and search parameters.

## 7.5 Water stamina penalties

`Player.WaterContactInfo` applies:

| Immersion | Threshold | Effect | Vanilla |
|---|---:|---|---:|
| Medium | 31 | `WaterMediumStaminaPenalty` | +100% SP drain |
| High | 76 | `WaterStrongStaminaPenalty` | +200% SP drain |
| Deadly | 126 | `WaterStrongStaminaPenalty` + `WaterDeadlyDamage` | +200% SP drain + separate death effect |

The stamina tuner changes the SP-drain strength only, not water-depth thresholds or deadly-water damage.

### Reference / control surfaces

| Candidate control | Source | Type |
|---|---|---|
| Maximum stamina | `MaxSP` | Direct pool-size slider |
| Base stamina regeneration | `RegenSP` | Direct raw-rate slider |
| Sprint cost | `StaminaPerAction.Sprint` | Direct raw-value slider |
| Jump cost | `StaminaPerAction.Jump` | Direct raw-value slider |
| Melee costs | `StaminaPerAction.*` | Direct raw-value sliders |
| Vault cost | `StaminaPerAction.Vault` | Direct raw-value slider |
| Sprint disable threshold/delay | `StaminaDisableThresholds[0]` | Direct threshold/time sliders |
| Low-SP recovery | `AdditionalRegenStaminaMechanics` + `IncreaseStaminaPoints` | Threshold / strength / duration sliders |
| Maximum vault height | `Player.VaultingParams.MaxObstacleHeight` | Direct raw-height slider |
| Water stamina drain | `WaterMediumStaminaPenalty` / `WaterStrongStaminaPenalty` | Direct percentage sliders |
| Sprint regen coefficient | `CoreVariables.StaminaRegenStateCoefs` | Direct coefficient slider |
| Run regen coefficient | same | Direct coefficient slider |
| Aim regen coefficient | same | Direct coefficient slider |

All four standard difficulties use the same base Player stamina values unless a separate effect modifies them.

---

# 8. Hunger

## 8.1 Base hunger accumulation

| Property | Value |
|---|---:|
| Maximum hunger points | 100 |
| `RegenHungerPoints` | 0.015 |

A higher HungerPoints value represents increasing hunger.

## 8.2 Hunger thresholds

Source: `EffectPrototypes.cfg`, `HungerMechanics`.

| State | Threshold |
|---|---:|
| Very low | 0 |
| Low | 17 |
| Medium | 33 |
| Strong | 83 |

## 8.3 Standard hunger penalties

| Hunger state | Effect | Vanilla value |
|---|---|---:|
| Low | Stamina regeneration modifier | 0% |
| Medium | Stamina regeneration modifier | -33% |
| Strong | Stamina regeneration modifier | -70% |

Medium and Strong hunger also apply `HungerNoSleep`.

## 8.4 Master-only additional hunger mechanics

Master adds `HungerNegativeMechanics`.

At HungerPoints `>= 83` the following are applied:

| Effect | Value |
|---|---:|
| `HungerNegativeStaminaPenalty` | +200% SP drain |
| `HungerNegativeDamage` | 4 damage |
| Damage interval | 10 seconds |
| Damage delay | 3 seconds |
| `HungerNegativeHealthRegeneration` | -100% health regeneration |

These mechanics are not enabled through the standard Novice/Stalker/Veteran profile `AdditionalMechanicsEffects`; they are explicitly part of Master.

### Reference / control surfaces

- Hunger accumulation rate
- Low/Medium/Strong thresholds
- Medium stamina regen penalty
- Strong stamina regen penalty
- Starvation stamina-drain toggle and amount
- Starvation damage toggle, damage amount, and interval
- Starvation health-regeneration penalty
- “Cannot sleep while hungry” behavior

---

# 9. Thirst

The runtime dump contains `MaxThirstPoints` and `RegenThirstPoints` in object prototypes, including the Player value:

- `MaxThirstPoints = 100`
- `RegenThirstPoints = 0.0`

A scan of the supplied runtime CFG dump found explicit `Thirst` text only in base/DLC object prototypes. No complete thirst mechanics effect chain comparable to Hunger/Sleep was found in the supplied CFGs.

**Status: DORMANT / INCOMPLETE**

The tuner should **not** expose a normal thirst slider in the first supported release unless a runtime implementation is added and tested.

---

# 10. Sleep and fatigue

## 10.1 Base sleepiness

| Property | Value |
|---|---:|
| Maximum sleepiness points | 100 |
| `RegenSleepinessPoints` | 0.01 |

## 10.2 Sleepiness mechanics

Source: `SleepMechanics`.

| State | Threshold |
|---|---:|
| Very low | 0 |
| Low | 50 |
| Medium | 60 |
| Strong | 80 |

The mechanics state starts at 20 and stops at 10.

Confirmed penalties include:

| State | Effect | Value |
|---|---|---:|
| Low | Aiming time | +15% |
| Medium | Aiming time | +25% |
| Strong | Aiming time | +40% |
| Medium | Reloading time | +15% |
| Strong | Reloading time | +18.75% |

Visual/audio fatigue effects are also attached to these states.

## 10.3 Sleeping rules

Source: `ObjSleepParamsPrototypes.cfg`, `DefaultSleepParams`.

| Property | Vanilla |
|---|---:|
| `AllowSleepThreshold` | 50 |
| `SleepHoursMultiplier` | 0.08 |
| `MinSleepHours` | 7 |
| `bAllowEmissionSleep` | false |

Sleep is blocked by the following effects:

- `RadiationNoSleep`
- `BleedingNoSleep`
- `PsyNoSleep`
- `HungerNoSleep`

## 10.4 Effects applied by sleep

Confirmed sleep effects:

| Effect | Vanilla |
|---|---:|
| Health restore | +300 |
| Stamina restore | +100% |
| Sleepiness restore | -100% |
| Bleeding restore | -100% |
| Radiation restore | -100% |
| Psy restore | -100% |
| Suppression restore | -100% |
| Sleep hunger effect | `0.04` |

The exact runtime interpretation/scaling of `SleepHunger = 0.04` together with sleep duration should be tested before exposing it as a user-facing percentage.

## 10.5 Master-only additional fatigue mechanics

Master enables `SleepinessNegativeMechanics`.

At SleepinessPoints `>= 50` the additional effects include:

| Effect | Vanilla |
|---|---:|
| Maximum stamina | -20% |
| Carry weight | -20% |
| Recoil | +55% |
| Dispersion | +0.5 |

Additional idle-sway penalties also exist.

### Reference / control surfaces

- Sleepiness accumulation rate
- Low/Medium/Strong fatigue thresholds
- Aiming penalties
- Reload penalties
- Minimum sleep hours
- Sleep eligibility threshold
- Sleep health/stamina restoration
- Sleep-clears-bleeding/radiation toggles
- Master-style fatigue penalties as independent toggles

---

# 11. Health and regeneration

## 11.1 Base Player health

| Property | Vanilla |
|---|---:|
| Max HP | 100 |
| Base `RegenHP` | 0 |
| HP regeneration delay | 5 seconds |
| Base regeneration modifier | 1.0 |

## 11.2 Low-health mechanics

`HealthMechanics` activates at HP `<= 30`.

The low-health state applies:

- low-health post-process
- low-health sound
- `LowHealthRegenHP`

`LowHealthRegenHP` has value `1`.

## 11.3 Difficulty health-regeneration scalar

| Difficulty | `Regen_HP` |
|---|---:|
| Novice | 1.00 |
| Stalker | 1.00 |
| Veteran | 0.75 |
| Master | 0.50 |

Master also adds `RegenHealthNegativeMechanics`.

At HP `<= 30`, `RegenHealthNegativeHealthRegeneration` applies an additional `-70%` regeneration effect.

### Reference / control surfaces

- Low-health threshold
- Low-health regeneration amount
- Difficulty health-regeneration scalar
- Master-style low-health regeneration penalty
- Base HP maximum — possible but should be treated separately from ordinary survival tuning

---

# 12. Bleeding

## 12.1 Base values

| Property | Vanilla |
|---|---:|
| Max bleeding | 100 |
| `DegenBleeding` | 0.3 |

## 12.2 Bleeding thresholds

| State | Threshold | Damage effect |
|---|---:|---:|
| Low | 0.1 | 1.0 |
| Medium | 50 | 1.5 |
| Strong | 75 | 2.0 |

The bleeding damage effects are configured with a one-second charge interval and ignore armor.

Medium and Strong bleeding also apply movement penalties and block sleep.

## 12.3 Difficulty scalar

| Difficulty | `Effect_Bleeding` |
|---|---:|
| Novice | 0.25 |
| Stalker | 1.00 |
| Veteran | 1.00 |
| Master | 1.50 |

`Effect_Degen_Bleeding` is `1.0` on all four standard difficulties.

### Reference / control surfaces

- Bleeding effect strength scalar
- Natural bleeding decay
- Low/Medium/Strong thresholds
- Damage per bleeding state
- Bleeding movement penalties
- Bleeding blocks sleep toggle

---

# 13. Radiation

## 13.1 Base Player radiation

| Property | Vanilla |
|---|---:|
| Max radiation | 100 |
| `DegenRadiation` | 0.05 |

## 13.2 Radiation mechanics thresholds

Confirmed `RadiationMechanics` thresholds:

| State | Threshold |
|---|---:|
| Light | 0.001 |
| Light 2 | 1 |
| Medium | 10 |
| Strong | 20 |
| Level 4 | 30 |
| Level 5 | 40 |
| Level 6 | 50 |
| Level 7 | 60 |
| Level 8 | 70 |
| Level 9 | 80 |
| Level 10 | 90 |
| Deadly | 99 |

## 13.3 Difficulty accumulation scalar

| Difficulty | `Radiation_AccumulationSpeed` |
|---|---:|
| Novice | 0.60 |
| Stalker | 1.00 |
| Veteran | 1.50 |
| Master | 1.85 |

Master adds `RadiationNegativeMechanics`; at radiation `>= 50`, `RadiationNegativeStaminaPenalty = +50%` increases stamina cost/drain by 50% while the penalty is active.

### Reference / control surfaces

- Radiation accumulation
- Natural radiation recovery
- Radiation thresholds
- Master-style stamina penalty
- Radiation sleep restriction
- Radiation damage effect levels

---

## 13.4 Anomaly damage layering used by the tuner

`EnvironmentDifficulty.Anomaly_Damage` is treated as the broad vanilla difficulty layer, while anomaly-specific effects retain their own base damage values in `EffectPrototypes.cfg`. The tuner therefore keeps these controls separate:

- **Global Anomaly Damage** — difficulty multiplier (`0.30 / 1.00 / 1.45 / 1.75` for Novice / Stalker / Veteran / Master).
- **Individual Base Damage** — the anomaly-specific raw effect value, such as Fire `60` or Chemical `30`.

The working model is `effective pre-protection damage ≈ base effect × global anomaly multiplier`. Editing both layers compounds the result. Because the CFG dump exposes the scalar and the base effects but not the engine implementation that combines them, final per-anomaly routing remains subject to runtime validation.

# 14. Carry weight and encumbrance

Source: `ObjWeightParamsPrototypes.cfg`, `DefaultWeightParams`.

| Property | Vanilla |
|---|---:|
| `MaxInventoryMass` | 80 |
| `InventoryPenaltyLessWeight` | 49.99 |

Encumbrance stages:

| Threshold | Stamina regeneration penalty | Movement penalty |
|---:|---:|---:|
| 50 | -10% | -15% |
| 60 | -20% | -25% |
| 70 | -35% | -50% |
| 80 | -50% | -50% plus movement restrictions |

The exact movement restrictions at the highest stages include jogging/blocking effects in addition to the speed modifier.

### Reference / control surfaces

- Max inventory mass
- Encumbrance thresholds
- Stamina penalties per stage
- Movement penalties per stage

---

# 15. Consumables

Consumable item prototypes reference effects rather than storing all gameplay values directly in the item definition. The tuner must therefore resolve:

`ItemPrototype -> EffectPrototypeSID -> Effect value`

rather than modifying only item files.

## 15.1 Food and hunger effects

| Item | Effect SID | HungerPoints effect |
|---|---|---:|
| Bread | `BreadSatiety2` | -20 |
| Sausage | `SausageSatiety2` | -30 |
| Canned food | `CannedSatiety3` | -50 |
| Water | `WaterSatiety1` | -10 |
| Energy drink | `EnegeticSatiety1` | +10 |
| Vodka | `VodkaSatiety1` | +10 |

Negative HungerPoints reduce hunger; positive values increase it.

Food also carries healing or other effects. Confirmed examples:

| Item | Health effect |
|---|---:|
| Bread | 8 |
| Sausage | 12 |
| Canned food | 18 |

## 15.2 Medical items

| Item | Health | Bleeding | Radiation |
|---|---:|---:|---:|
| Bandage | +20 | -100 | — |
| Medkit | +70 | -15 | — |
| Army medkit | +85 | -35 | — |
| Eco medkit | +100 | -25 | -60 |

Relevant effect SIDs include:

- `BandageHealing2`
- `BandageBleeding4`
- `MedkitHealing3`
- `MedkitBleeding2`
- `ArmyMedkitHealing4`
- `ArmyMedkitBleeding3`
- `EcoMedkitHealing4`
- `EcoMedkitBleeding2`
- `EcoMedkitAntirad3`

## 15.3 Water and energy drink

Confirmed effects include:

| Effect | Vanilla |
|---|---:|
| Water stamina | +25% over effect duration |
| Water instant stamina | +38% |
| Energy drink stamina regen | +200% for 45 s |
| Energy drink instant stamina | +50% |
| Energy drink sleepiness | -20 |

## 15.4 Vodka

Confirmed examples:

| Effect | Vanilla |
|---|---:|
| Radiation | -50 |
| SP drain penalty | +200% for 20 s |

### Reference / control surfaces

Any future grouped consumable control must use an explicitly enumerated family rather than assuming that a category-wide multiplier exists.

Possible controls:

- Medical healing effectiveness
- Bleeding treatment effectiveness
- Food hunger restoration
- Energy drink stamina effectiveness
- Antiradiation effectiveness

These require careful grouping so negative values (restoration) are scaled by magnitude rather than by sign mistakes.

---

# 16. Loot generation

## 16.1 Generator schema

`ItemGeneratorPrototypes.cfg` is the major base-game loot source. The runtime file is approximately 9.4 MB.

A generated item entry can contain:

- `ItemPrototypeSID`
- `ItemGeneratorPrototypeSID`
- `Weight`
- `Chance`
- `MinDurability`
- `MaxDurability`
- `MinCount`
- `MaxCount`
- `AmmoMinCount`
- `AmmoMaxCount`
- `bRequireWeapon`

The generator is grouped by `EItemGenerationCategory`.

Observed base-game category occurrences include:

| Category | Occurrences in generator CFG |
|---|---:|
| `WeaponPrimary` | 3979 |
| `SubItemGenerator` | 3361 |
| `BodyArmor` | 1742 |
| `Head` | 1295 |
| `Detector` | 850 |
| `Artifact` | 834 |
| `Consumable` | 778 |
| `Ammo` | 274 |
| `Junk` | 265 |
| `WeaponPistol` | 218 |
| `Attach` | 120 |
| `WeaponSecondary` | 103 |
| `Mask` | 75 |
| `NightVision` | 30 |
| `MutantLoot` | 15 |

These counts are structural occurrences, **not** expected drop rates.

## 16.2 Example generators

`SimpleFoodGenerator` includes Bread, Sausage, and CannedFood with weighted selection.

`SimpleMedkitGenerator` includes Medkit and Bandage with count ranges.

`PlayerItemGenerator` demonstrates ammo entries using explicit `Chance`, count ranges, and `bRequireWeapon`.

### Key conclusion

There is no confirmed single global `WorldLootMultiplier` in the inspected CFGs. Loot scarcity is produced by multiple independent dimensions: difficulty-gated branches, `Chance`, relative `Weight`, count ranges, weapon-associated ammo counts, and SmartLoot.

The tuner therefore must **not** model loot as one global abundance slider. `Chance`, `Weight`, and count ranges have different mathematics. In particular, multiplying every `Weight` in the same candidate pool by the same factor leaves the relative selection probability unchanged.

The source-aligned model is documented in `LOOT-MAPPING.md`. After the v0.1.62 strict audit, the visible Loot tab intentionally exposes only mappings that currently have a concrete writer contract:

- **Extra NPC Corpse Loot** — the difficulty `CorpseSmartLoot.GeneralNPC` list;
- **Expensive Stash Coupon Chance** — the explicit difficulty-gated coupon chance;
- **14 individual mutant-part drop chances**, including Bayun (runtime CFG identifiers remain `CatLootGenerator` / `CatLoot`).

The former NPC medical/food/utility, weapon-ammo/durability, loose-ammo and generic stash controls are **deferred research**, not current UI features. Their backing fields are real, but each control would require an aggregate selector or transform rather than a 1:1 property mapping.

---

# 17. Difficulty-specific corpse smart loot

Difficulty profiles alter `CorpseSmartLoot` for `GeneralNPC`:

| Difficulty | Additional `GeneralNPC` smart loot |
|---|---|
| Novice | `NPC_Medicine_Smart`, `NPC_Ammo_Smart` |
| Stalker | `NPC_Water_Smart` |
| Veteran | none |
| Master | none |

Examples from `StashPrototypes.cfg`:

- `NPC_Medicine_Smart` contains medicine loot pools by **Progression Rank** (`PlayerRank`), not by selected Game Difficulty.
- `NPC_Ammo_Smart` contains caliber-aware ammo pools and count ranges.
- `NPC_Water_Smart` provides Water with Progression-Rank-specific entries.

This is an important exception to the statement that “loot is the same on every difficulty.” General generator data may be shared, while the difficulty layer can still inject smart loot.

The v0.1.78 end-to-end control audit is in `docs/generators/SMARTLOOT-CORPSE-BONUS-AUDIT.md`. It confirms the visible `Extra NPC Corpse Loot` choice is a source-exact list replacement and does not rewrite SmartLoot package internals or ItemGenerator corpse inventory.

---

# 18. Stashes

`StashPrototypes.cfg` exposes a structured smart-loot model.

Relevant fields include:

- `Rank`
- `SmartLootParams`
- `MinSpawnChance`
- `MaxSpawnChance`
- `ItemSetCount`
- `ItemPrototypeSID`
- `MinCount`
- `MaxCount`
- `Weight`
- weapon caliber / ammo count fields

Supported groups include health/consumables, attachments, primary weapons, secondary weapons, pistols, grenades, and other smart-loot structures.

Example: `StashMedicine_Cheap` uses separate Medkit and Bandage entries with explicit set counts, spawn chances, and count ranges.

### Current tool model

The stash CFG is retained as source reference, but the tool does **not** currently expose generic stash-abundance, stash-medical chance/count, stash-ammo count, or a synthetic Novice medical-bonus toggle. The strict audit showed that these concepts span inherited `GamePass_Stash_ItemGenerator_*` bases and mix `Chance`, `Weight`, count ranges, and difficulty/rank gates.

The current direct stash-facing control is **Expensive Stash Coupon Chance**, whose difficulty-gated source values are explicitly mapped. Any broader stash control remains **DEFERRED / RESEARCH** until every target branch and transformation rule is explicitly enumerated.

---

# 19. Trade and economy

## 19.1 Base trader template

`BaseTraderNPC_Template` contains:

| Property | Vanilla |
|---|---:|
| `BuyModifier` | 0.3 |
| `SellModifier` | 1.0 |
| `WeaponSellMinDurability` | 0.4 |
| `ArmorSellMinDurability` | 0.4 |
| `bInfiniteMoney` | true |
| Refresh condition | `TradeRegenDaysPassed1` |

Rank-based buy modifiers:

| Rank condition | Modifier |
|---|---:|
| Experienced | 1.15 |
| Veteran | 1.20 |
| Master | 1.25 |

Rank-based sell modifiers:

| Rank condition | Modifier |
|---|---:|
| Experienced | 0.97 |
| Veteran | 0.95 |
| Master | 0.90 |

Not every trader uses exactly the base template values. Individual trader prototypes must be respected.

## 19.2 Generic trader example

`TraderNPC` uses:

| Property | Value |
|---|---:|
| Money | 20000 |
| Generator | `DynamicTraderItemGenerator` |
| Buy modifier | 0.5 |
| Sell modifier | 1.0 |
| Refresh | `TradeRegenDaysPassed7` |

## 19.3 Repair system

Source: `CoreVariables.cfg`.

| Property | Vanilla |
|---|---:|
| `ItemCostMinPercent` | 0.1 |
| `BaseRepairCostModifier` | 0.7 |

Repair reputation modifiers:

| Relation | Modifier |
|---|---:|
| Enemy | 2.0 |
| Disaffection | 1.5 |
| Neutral | 1.0 |
| Friend | 0.75 |

### Reference / control surfaces

- Trader buy-price economy
- Trader sell-price economy
- Trader stock abundance
- Trader refresh interval
- Repair cost
- Upgrade cost
- Main quest rewards
- Side quest rewards

The first two need careful naming because the raw `BuyModifier`/`SellModifier` direction may not correspond intuitively to “cheaper/more expensive” without runtime validation.

---

# 20. Equipment durability

## 20.1 Armor

Difficulty directly exposes `Armor_Durability`:

| Difficulty | Value |
|---|---:|
| Novice | 1.35 |
| Stalker | 1.00 |
| Veteran | 0.90 |
| Master | 0.60 |

The precise runtime interpretation should be verified before labeling the direction as “degradation speed” versus “durability retained.”

## 20.2 Weapons

Weapon configuration contains per-weapon/per-profile fields such as:

`DurabilityDamagePerShot`

The inspected weapon settings contain many different values rather than one universal number.

### Key conclusion

Weapon wear is **not** represented by one obvious global CFG scalar in the inspected data.

A “Weapon Degradation” slider is therefore an aggregate transform over relevant weapon settings unless another runtime/global multiplier is identified later.

**Status: DEFERRED / RESEARCH — no global weapon-wear control is currently exposed.**

---

# 21. DLC coverage

The runtime dump contains DLC configuration branches in addition to base `GameData`.

DLC1 specifically adds its own:

- `EffectPrototypes.cfg`
- `ItemGeneratorPrototypes.cfg`
- `ItemPrototypes.cfg`
- `ObjPrototypes.cfg`
- `TradePrototypes.cfg`
- weapon-data CFGs

DLC1 does **not** provide a separate `DifficultyPrototypes.cfg` in this dump.

Its prototypes commonly inherit from base-game prototypes using `refurl` / `refkey`.

### Design requirement

Any category-wide tuner operation for loot, effects, items, traders, or equipment must consider both:

`GameData/...`

and relevant:

`DLCGameData/...`

Otherwise the tool can produce a partially tuned game where DLC content ignores the selected survival settings.

PreOrder, Deluxe, and Ultimate also contribute item/weapon-related CFGs.

---

# 22. Current control model

The current application exposes only controls that have a concrete writer contract. Some are direct one-field edits; others are explicit special writers for known arrays/lists. Aggregate generator research is kept separate from the visible catalog.

## 22.1 Direct controls

Representative direct mappings include:

| Control | CFG target |
|---|---|
| Starting Coupons | `CoreVariables.DefaultConfig.PlayerStartingMoney` |
| Hunger build-up | `Player.VitalParams.RegenHungerPoints` |
| Sleepiness build-up | `Player.VitalParams.RegenSleepinessPoints` |
| Base stamina regen | `Player.VitalParams.RegenSP` |
| Sprint stamina cost | `Player.StaminaPerAction.Sprint` |
| Jump stamina cost | `Player.StaminaPerAction.Jump` |
| Carry capacity | `DefaultWeightParams.MaxInventoryMass` |
| Sleep threshold | `DefaultSleepParams.AllowSleepThreshold` |
| Minimum sleep | `DefaultSleepParams.MinSleepHours` |
| Radiation recovery | `Player.VitalParams.DegenRadiation` |
| Bleeding recovery | `Player.VitalParams.DegenBleeding` |
| Repair base cost | `CoreVariables.BaseRepairCostModifier` |

## 22.2 Difficulty scalar controls

The current difficulty layer uses the four selectable vanilla baselines only: Novice/`Easy`, Stalker/`Medium`, Veteran/`Hard`, and Master/`Stalker`. Directly mapped examples include NPC HP/damage, mutant damage, radiation accumulation, anomaly/explosion damage, armor durability, health regeneration, bleeding, player weapon damage, upgrade cost, quest rewards, and buy/sell-condition fields.

`Weather_Storm_Weight` and `Weather_Emission_Weight` remain documented as runtime reference values but are intentionally **not** exposed as controls.

## 22.3 Explicit structural/special writers

Some supported controls are not scalar fields but still have a deterministic, enumerated writer contract, including:

- Additional Mechanics list membership;
- Extra NPC Corpse Loot list selection;
- Master PSY override arrays;
- explicitly enumerated item/effect families already present in the writer baseline.

These are writer-mapped because the affected source nodes are known. Runtime-test status is tracked separately where behavior still needs confirmation.

## 22.4 Deferred aggregate/generator research

The following remain research topics rather than visible controls unless/until the complete target set and transform mathematics are proven:

- generic NPC/stash/loose-ammo abundance transforms;
- category-composition changes based on relative `Weight`;
- artifact / weapon / armor generator-wide transforms requiring exclusions;
- SmartLoot spawn-curve tuning;
- trader stock;
- a truly global weapon-degradation transform.

---

# 23. UI baseline rule: percentage + raw value + vanilla references

Every slider must show both the user's normalized setting and the actual CFG value.

## 23.1 Difficulty-dependent setting

Example:

```text
Radiation Accumulation

Current: 130%    Raw: 1.30x

Novice  0.60x
Stalker 1.00x
Veteran 1.50x
Master  1.85x
```

The slider should visually mark the four vanilla positions.

## 23.2 Shared/base setting

Example:

```text
Hunger Build-up

Current: 175%
Raw: 0.02625

Vanilla all difficulties: 0.015
```

Do not render four overlapping markers when all difficulties share the same raw value.

## 23.3 Toggle setting

Example:

```text
Starvation damage

Novice:  OFF
Stalker: OFF
Veteran: OFF
Master:  ON
```

Use a toggle or mode selector, not a fake percentage.

## 23.4 Raw values are authoritative

The displayed percentage is a tuner convenience.

The baseline reference must always preserve:

- exact raw CFG value
- source property
- source prototype
- applicable difficulty/profile

This prevents ambiguous “100%” values from losing their technical meaning.

---

# 24. Current visible coverage

The v0.1.63 catalog contains **167 visible controls**, and all 167 have a concrete writer contract. There are no visible writer-deferred controls.

The current UI covers Combat, Survival, Health, Stamina, Loot, Economy, Equipment, Environment and Advanced mechanics. Loot is deliberately narrow after the strict audit: **Extra NPC Corpse Loot**, **Expensive Stash Coupon Chance**, and **14 individual mutant-part drop chances**. Broader generator abstractions are kept out of the UI until their selectors and transforms are proven.

---

# 25. Baseline implementation principles

1. **Do not hardcode assumptions that the CFGs do not prove.**
2. **Preserve raw vanilla values for all four difficulties.**
3. **Do not collapse toggle mechanics into percentage sliders.**
4. **Treat aggregate loot controls as transformations, not fake global variables.**
5. **Resolve item effects through their referenced EffectPrototype SIDs.**
6. **Process DLC branches when a tuning category affects DLC content.**
7. **Keep the runtime game dump as the source of truth for values.**
8. **Treat ZoneKit values as secondary when they differ from runtime.**
9. **Record every generated change as `file -> prototype -> property -> old -> new`.**
10. **Keep support levels separate: CFG-confirmed/writer-mapped proves the generated mapping; only an in-game test makes a setting runtime-confirmed.**

---

# 26. Open questions / runtime validation queue

| Question | Status |
|---|---|
| Exact runtime semantics of `Effect_Satiety` | RUNTIME TEST REQUIRED |
| Exact runtime direction/meaning of `Armor_Durability` | RUNTIME TEST REQUIRED |
| Exact runtime direction of trader `BuyModifier` / `SellModifier` in all contexts | RUNTIME TEST REQUIRED |
| Best mathematically safe transformation for loot `Chance` vs `Weight` | DESIGN + TEST REQUIRED |
| Dedicated `Custom` prototype as player-facing target | OUT OF SCOPE — tool overrides the selected vanilla base preset instead |
| Whether difficulty values can be reloaded without restart/new game | RUNTIME TEST REQUIRED |
| Whether all item-generator changes apply to already-spawned containers/NPCs | RUNTIME TEST REQUIRED |
| Whether a complete player thirst mechanic exists outside supplied CFGs | NOT FOUND IN CFG DUMP |
| Whether there is a global weapon-wear scalar outside inspected CFGs | NOT FOUND YET |
| DLC-specific exceptions to global category transforms | REVIEW REQUIRED |

---

# 27. Primary CFG map

| System | Primary CFG(s) |
|---|---|
| Difficulty | `GameData/DifficultyPrototypes.cfg` |
| Player vitals / stamina | `GameData/ObjPrototypes.cfg` |
| Effects / mechanics | `GameData/EffectPrototypes.cfg` |
| Sleep | `GameData/ObjSleepParamsPrototypes.cfg` |
| Weight / encumbrance | `GameData/ObjWeightParamsPrototypes.cfg` |
| Global stamina / repair | `GameData/CoreVariables.cfg` |
| Items / consumables | `GameData/ItemPrototypes.cfg` |
| Loot generators | `GameData/ItemGeneratorPrototypes.cfg` |
| Containers | `GameData/ItemContainerPrototypes.cfg` |
| Stashes / smart loot | `GameData/StashPrototypes.cfg` |
| Corpses | `GameData/CorpsePrototypes.cfg`, `CorpseClueStashPrototypes.cfg` |
| Trade | `GameData/TradePrototypes.cfg` |
| Weapon wear | `GameData/WeaponData/CharacterWeaponSettingsPrototypes.cfg` plus weapon configs |
| DLC effects | `DLCGameData/DLC1/EffectPrototypes.cfg` |
| DLC loot | `DLCGameData/DLC1/ItemGeneratorPrototypes.cfg` |
| DLC items | `DLCGameData/DLC1/ItemPrototypes.cfg` |
| DLC objects | `DLCGameData/DLC1/ObjPrototypes.cfg` |
| DLC trade | `DLCGameData/DLC1/TradePrototypes.cfg` |

---

# 28. Baseline conclusion

The runtime configuration already contains a substantial survival framework:

- hunger with thresholds and stamina penalties
- sleeping/fatigue with progressive penalties
- bleeding and radiation mechanics
- low-health regeneration logic
- difficulty-specific survival scalars
- Master-only punitive mechanics
- structured smart loot and stash systems
- structured trader/economy systems
- explicit stamina and carrying parameters
- DLC-specific extensions that inherit from the base game

True Custom Difficulty therefore does **not** need to invent the core survival model. Its job is to expose, normalize, reference, and safely patch existing configuration values while preserving the original Novice/Stalker/Veteran/Master baselines as orientation points.

Thirst and aggregate generator systems remain outside the supported visible scope until their full source chain, transform semantics, and runtime behavior are sufficiently proven.

---

## 29. v0.2 baseline audit result

The v0.1.63 audit revalidated the baseline against the exact runtime 2.0.5 dump fingerprint recorded below. File counts remain **631 total CFGs** (`485 GameData`, `118 DLC1`, `11 PreOrder`, `10 Deluxe`, `7 Ultimate`).


Two baseline defects were corrected during this pass:

- `Low Bleeding Damage` now uses the actual runtime `LowBleedingDamage.ValueMin/ValueMax = 1.0` vanilla value instead of the stale UI value `0.33`.
- Fast-travel coupon edits now address the physical `[0]..[14]` roots in `FastTravelPrototypes.cfg`; guide SIDs are retained only as metadata. This makes the emitted BPatch root match the source CFG structure 1:1.

The 13 removed aggregate Loot concepts remain deferred and are not counted among the 167 visible controls.

---

## Appendix A — Source fingerprints

These fingerprints identify the exact supplied archives used for this baseline.

```text
Runtime game+DLC CFG dump SHA-256:
739ecb4a60e2d98181cc6b326894d184b76a09c29b94a66c18d7b0da2c37a83a

ZoneKit survival CFG subset SHA-256:
f536923459257496bfa68541fcb83b2b0e178b9346c026fbb9cad7bd11308ec2
```

---

## Appendix B — Baseline change policy

When a later game patch or ZoneKit update is introduced, do not silently replace this document's values.

The update process should be:

```text
old runtime dump
        ↓
CFG structural/value diff
        ↓
identify changed supported parameters
        ↓
update baseline version
        ↓
retest generated tuner output
```

A future baseline version should record the game build/dump label and archive fingerprint in the same way as this document.


## Additional Environment and Equipment controls (v0.1.6)

The runtime 2.0.5 dump also exposes several direct, confirmed tuning surfaces that are useful for True Custom Difficulty.

### Environment — specific anomaly base effects

These sit alongside the existing difficulty-level `EnvironmentDifficulty.Anomaly_Damage` scalar:

| Effect SID | Vanilla raw value | Meaning in tuner |
|---|---:|---|
| `ElectroAnomaly` | 65 | Electro anomaly base damage |
| `ChemicalDamage` | 30 | Chemical anomaly damage |
| `FireAnomalyDPS` | 60 | Fire anomaly damage per tick |
| `CarouselAnomaly` | 105 | Carousel anomaly direct damage |
| `ToxicCloudDamage` | 25 | Toxic Cloud damage per tick |
| `SteamAnomalyDPS` | 60 | Steam anomaly damage per tick |

These controls change effect strength only. They do not change anomaly placement, weather frequency, emission frequency, or scheduling.

### Equipment — global effect caps

> Reference only: these engine caps are **not exposed as True Custom Difficulty controls**. The Effect Caps UI block was removed in v0.9.26.44 because these limits are not useful player-facing difficulty controls.

`ObjEffectMaxParamsPrototypes.cfg`, `DefaultEffectMaxParamsSID`, defines these vanilla caps:

| Effect type | Vanilla max |
|---|---:|
| `ProtectionShock` | 90 |
| `ProtectionStrike` | 4.5 |
| `ProtectionBurn` | 90 |
| `ProtectionChemical` | 90 |
| `ProtectionPSY` | 90 |
| `ProtectionRadiation` | 85 |
| `RegenStamina` | 30 |
| `DegenBleeding` | 5 |
| `AdditionalInventoryWeight` | 140 |
| `PenaltyLessWeight` | 90 |

These are global caps on combined effects. They do not rewrite individual armor, artifact, or upgrade prototypes.
