# Health Control Deep Audit — v0.2.45

Runtime source of truth: **STALKER 2 CFG dump 2.0.5**.

## Result

All **16 normal Health-tab controls** were traced end to end through catalog metadata, vanilla values, slider semantics, concrete writer edit points and CFG targets.

- **13** controls are statically confirmed.
- **3** controls remain `runtime_test_required`: `health.maxHP`, `health.baseRegen`, `health.regenDelay`.
- **0** controls were removed or widened.
- The writer surface remains exactly **38 concrete Health edit points**.

## v0.2.45 correction — treatment magnitudes

Six direct-treatment controls previously exposed the engine's signed delta directly. The CFG correctly stores removal as a negative value (`-100` means subtract 100 points), but that made the UI slider direction counter-intuitive: moving from `-200` toward `0` weakened a control labelled *Removal*.

The UI now exposes a positive gameplay magnitude while the writer performs the sign conversion:

| Control | UI vanilla | UI range | CFG vanilla | Writer |
|---|---:|---:|---:|---|
| Bandage Bleeding Removal | 100 | 0–200 | -100 | `negateSet` |
| Medkit Bleeding Removal | 15 | 0–30 | -15 | `negateSet` |
| Army Medkit Bleeding Removal | 35 | 0–70 | -35 | `negateSet` |
| Scientific Medkit Bleeding Removal | 25 | 0–50 | -25 | `negateSet` |
| Scientific Medkit Radiation Removal | 60 | 0–120 | -60 | `negateSet` |
| Anti-Rad Radiation Removal | 100 | 0–200 | -100 | `negateSet` |

This changes UI representation only. At the vanilla marker the emitted CFG values are unchanged. Presets saved before v0.2.45 with negative values are migrated with `abs(value)` when loaded.

## Core health

| Control | CFG target | Status | Audit result |
|---|---|---|---|
| Player Max Health | `Player.VitalParams.MaxHP` + `HealthMechanics...FullHP.ConditionValue` | runtime test | Both 100-point values remain linked; broader engine assumptions about the HP scale are still runtime-gated. |
| Passive Health Recovery | `Player.VitalParams.RegenHP` | runtime test | Direct field, vanilla 0; tick/execution semantics remain runtime-gated. |
| Health Recovery Delay | `Player.VitalParams.RegenHPDelayTimeSeconds` | runtime test | Direct seconds field, vanilla 5; which recovery paths honor it remains runtime-gated. |
| Health Recovery Multiplier | `EnvironmentDifficulty.Regen_HP` | confirmed | Direct selected-difficulty multiplier; N/S/V/M = 1/1/0.75/0.5. |
| Low-Health Recovery Rate | `LowHealthRegenHP.ValueMin/ValueMax` | confirmed | Both endpoints stay linked; vanilla 1/1. |

## Healing Amount

Bandage, Medkit, Army Medkit and Scientific Medkit healing remain direct `ValueMin/ValueMax` controls with vanilla 20/70/85/100 respectively. No duration, charge, action-time or Master modifier field is silently included.


### Food Restores Health

A confirmed boolean control now owns exactly eight concrete assignments: `ValueMin` + `ValueMax` for `BreadHealing1`, `FreshBreadHealing2`, `CannedHealing2`, and `SausageHealing2`. OFF writes 0 HP; ON leaves vanilla values in force. Satiety is not part of the writer surface. Weird Kettle alternative effects are explicitly excluded from the target set.

## Hard boundaries retained

- `RegenHealthModifier` remains NO-UI until its layering is proven.
- Medical `Duration` / `Charges` remain NO-UI until runtime semantics are proven.
- `HPThresholdToKill` remains NO-UI.
- Medical `InventoryActionTime` remains intentionally outside difficulty tuning.
- NPC wounded-state regeneration remains outside the player Health domain.
- Master-only health controls remain on the Master tab and are not counted among these 16 normal Health controls.
