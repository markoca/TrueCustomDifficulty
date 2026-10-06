# Health CFG Mapping — v0.1.21

Runtime source of truth: **STALKER 2 CFG dump 2.0.5**.

## Scope

The Health tab deliberately separates direct HP mechanics from Survival-state logic. Bleeding-state damage, radiation accumulation and starvation/fatigue consequences remain in Survival; this tab owns the player HP pool, health regeneration, low-health mechanics and direct medical-item effects.

## Core Health

| UI control | CFG target | Vanilla | Notes |
|---|---|---:|---|
| Player Max Health | `ObjPrototypes.cfg -> Player.VitalParams.MaxHP` | 100 HP | Also keeps `HealthMechanics.FullHP.ConditionValue` aligned. Broader assumptions about the 100-point health scale require runtime testing. |
| Passive Health Recovery | `Player.VitalParams.RegenHP` | 0 | Vanilla has no ordinary actor-level passive HP regeneration from this field. |
| Health Recovery Delay | `Player.VitalParams.RegenHPDelayTimeSeconds` | 5 s | Exact effect-source coverage requires runtime testing. |
| Health Recovery Multiplier | `DifficultyPrototypes.cfg -> EnvironmentDifficulty.Regen_HP` | N 1.00 / S 1.00 / V 0.75 / M 0.50 | Multiplies compatible health-regeneration effects; it does not create healing on its own. |

## Standard vs Master low-health mechanics

These are two distinct CFG mechanics blocks. Vanilla happens to place both at **30 HP**, but their activation thresholds are intentionally kept vanilla and are not exposed in the UI.

### Standard `HealthMechanics`

At `HP <= 30`, vanilla applies:
- `LowHealthPostProcess`
- `LowHealthSoundPlay`
- `LowHealthRegenHP` (`ValueMin = ValueMax = 1`) — exposed as **Low-Health Recovery Rate**

`FullHP.ConditionValue = 100` stops the low-health sound path.

### Master `RegenHealthNegativeMechanics`

Only Master includes `RegenHealthNegativeMechanics` in `EnvironmentDifficulty.AdditionalMechanicsEffects`. Its own threshold is `HP <= 30`, where it applies `RegenHealthNegativeHealthRegeneration = -70%`.

The Health tab exposes the Master activation toggle and penalty magnitude, while both standard and Master low-health activation points stay at their vanilla values.

## Healing Amount

| Item/effect | Base HP | MasterEffectModifier | Effective with untouched Master modifier |
|---|---:|---:|---:|
| Bandage / `BandageHealing2` | 20 | ×0.25 | 5.0 HP |
| Medkit / `MedkitHealing3` | 70 | ×0.71 | 49.7 HP |
| Army Medkit / `ArmyMedkitHealing4` | 85 | ×0.82 | 69.7 HP |
| Scientific Medkit / `EcoMedkitHealing4` | 100 | no matching entry in runtime `MasterEffectModifier` | 100 HP before other layers |

The three Master multipliers are children of `MasterEffectModifier`. They are dormant unless **Master → Master Effect Modifier Mechanics** is enabled. The same Master prototype also modifies food, energetic and Weird Kettle effects. Those Master multipliers remain separate from the direct food-healing switch below.

### Food Restores Health

The normal food healing path is independent from satiety. The toggle is vanilla-ON and maps only these direct health effects:

| Food effect | Vanilla HP | OFF output |
|---|---:|---:|
| `BreadHealing1` | 8 | 0 |
| `FreshBreadHealing2` | 15 | 0 |
| `CannedHealing2` | 18 | 0 |
| `SausageHealing2` | 12 | 0 |

The writer changes `ValueMin` and `ValueMax` only. Food hunger/satiety effects are not changed. `Artifact_WeirdKettle_BreadHealing`, `Artifact_WeirdKettle_CannedHealing`, `Artifact_WeirdKettle_SausageHealing`, `Artifact_WeirdKettle_MilkHealing`, and every other Weird Kettle alternative effect are explicitly outside this switch.

## Bleeding treatment

Direct item effects are independent from passive bleeding recovery and bleeding-state damage:

| Effect | Vanilla raw value |
|---|---:|
| `BandageBleeding4` | -100 |
| `MedkitBleeding2` | -15 |
| `ArmyMedkitBleeding3` | -35 |
| `EcoMedkitBleeding2` | -25 |

Negative values subtract BleedingPoints.

## Radiation treatment

| Effect | Vanilla raw value |
|---|---:|
| `EcoMedkitAntirad3` | -60 |
| `Antirad4` | -100 |

Negative values subtract RadiationPoints. These direct treatments do not alter radiation accumulation or passive recovery.

## Intentionally not exposed

- Medical-item `InventoryActionTime`: intentionally not exposed. The user does not want difficulty tuning to alter Bandage/Medkit/Anti-Rad use timing.
- NPC wounded-state variables in `CoreVariables.cfg` (`WoundedStateHealthRegen`, `ChanceToGetHealOverTimeWhenWounded`, etc.): they are not the player's normal health/difficulty system.
- `HPThresholdToKill`: global low-level death handling is not treated as a difficulty control.
- `RegenHealthModifier = 1.0`: not exposed yet because its exact layering against `EnvironmentDifficulty.Regen_HP` and effect-based `RegenHealth` is not proven by the CFG alone.
- Effect `Duration` / `Charges` for medkits and bandages: the fields are present, but their engine execution semantics should be runtime-tested before being turned into player-facing difficulty sliders.

## v0.2.45 UI sign boundary

Bleeding/radiation treatment rows above document the **raw CFG values**. The normal UI now presents their positive removal magnitude (for example Bandage `100` removal), and the writer emits the corresponding negative CFG delta (`-100`) with `negateSet`. This keeps stronger treatment moving to the right while preserving the original engine value at vanilla.
