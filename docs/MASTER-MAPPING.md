# MASTER-MAPPING.md

## Activation thresholds remain vanilla

Master bundle activation thresholds are intentionally not exposed. Starvation, severe fatigue, radiation and low-health bundles enter when the game reaches their vanilla status-state conditions. The Master tab only controls whether the bundle is enabled and how strong its consequences are.

## Purpose

The **Master** tab contains dedicated Master-only survival/health mechanics bundles from `AdditionalMechanicsEffects`. Combat-facing Master overrides (mutant ability cooldowns and PSY combat) remain in Combat so players find them with the systems they affect. Ordinary difficulty scalars that simply have a Master value remain in their normal gameplay tabs.

Internal parameter IDs are retained for preset compatibility; therefore several IDs still begin with `advanced.`, `combat.`, `survival.` or `health.` even though their UI tab is now `master`.

## Bundle dependency behavior

Where the CFG has a parent mechanics bundle, the child values live in global effect prototypes and are dormant until that bundle is active. The UI keeps those children visible for inspection/rationale but disables their inputs and dims the card while the parent toggle is Off.

Player-facing penalty/loss sliders use positive magnitudes even where the engine stores the effect as a negative delta; the CFG writer applies the sign conversion.

## Low Health

| Control | CFG target / behavior | Dependency |
|---|---|---|
| Master Low-Health Mechanics | `GameData/DifficultyPrototypes.cfg` → `EnvironmentDifficulty.AdditionalMechanicsEffects` | `—` |
| Master Low-Health Recovery Penalty | `GameData/EffectPrototypes.cfg` → `ValueMin|ValueMax` | `advanced.lowHealthMechanics` |

## Medical Effect Modifier

| Control | CFG target / behavior | Dependency |
|---|---|---|
| Master Bandage Healing Modifier | `GameData/EffectPrototypes.cfg` → `EffectModifiers[EffectPrototypeSID=BandageHealing2].ValueMultiplier` | `advanced.masterEffectModifierMechanics` |
| Master Medkit Healing Modifier | `GameData/EffectPrototypes.cfg` → `EffectModifiers[EffectPrototypeSID=MedkitHealing3].ValueMultiplier` | `advanced.masterEffectModifierMechanics` |
| Master Army Medkit Healing Modifier | `GameData/EffectPrototypes.cfg` → `EffectModifiers[EffectPrototypeSID=ArmyMedkitHealing4].ValueMultiplier` | `advanced.masterEffectModifierMechanics` |
| Master Effect Modifier Mechanics | `GameData/DifficultyPrototypes.cfg` → `EnvironmentDifficulty.AdditionalMechanicsEffects` | `—` |

## Starvation

| Control | CFG target / behavior | Dependency |
|---|---|---|
| Master Starvation Penalties | `GameData/DifficultyPrototypes.cfg` → `EnvironmentDifficulty.AdditionalMechanicsEffects` | `—` |
| Starvation Stamina Drain | `GameData/EffectPrototypes.cfg` → `ValueMin|ValueMax` | `advanced.starvationMechanics` |
| Starvation Damage | `GameData/EffectPrototypes.cfg` → `ValueMin|ValueMax` | `advanced.starvationMechanics` |
| Starvation Damage Interval | `GameData/EffectPrototypes.cfg` → `TimePerChargeMin|TimePerChargeMax` | `advanced.starvationMechanics` |
| Starvation Penalty Delay | `GameData/EffectPrototypes.cfg` → `DelayMin|DelayMax`; `GameData/EffectPrototypes.cfg` → `DelayMin|DelayMax`; `GameData/EffectPrototypes.cfg` → `DelayMin|DelayMax` | `advanced.starvationMechanics` |
| Starvation Health Recovery Penalty | `GameData/EffectPrototypes.cfg` → `ValueMin|ValueMax` | `advanced.starvationMechanics` |

## Radiation

| Control | CFG target / behavior | Dependency |
|---|---|---|
| Master Radiation Penalty | `GameData/DifficultyPrototypes.cfg` → `EnvironmentDifficulty.AdditionalMechanicsEffects` | `—` |
| Master Radiation Stamina Drain | `GameData/EffectPrototypes.cfg` → `ValueMin|ValueMax` | `advanced.radiationMechanics` |

## Severe Fatigue

| Control | CFG target / behavior | Dependency |
|---|---|---|
| Master Severe-Fatigue Penalties | `GameData/DifficultyPrototypes.cfg` → `EnvironmentDifficulty.AdditionalMechanicsEffects` | `—` |
| Severe-Fatigue Max Stamina Loss | `GameData/EffectPrototypes.cfg` → `ValueMin|ValueMax` | `advanced.fatigueMechanics` |
| Severe-Fatigue Carry Capacity Loss | `GameData/EffectPrototypes.cfg` → `ValueMin|ValueMax`; `GameData/EffectPrototypes.cfg` → `ValueMin|ValueMax` | `advanced.fatigueMechanics` |
| Severe-Fatigue Recoil Penalty | `GameData/EffectPrototypes.cfg` → `ValueMin|ValueMax` | `advanced.fatigueMechanics` |
| Severe-Fatigue Aim Sway | `GameData/EffectPrototypes.cfg` → `ValueMin|ValueMax`; `GameData/EffectPrototypes.cfg` → `ValueMin|ValueMax` | `advanced.fatigueMechanics` |
| Severe-Fatigue Dispersion | `GameData/EffectPrototypes.cfg` → `ValueMin|ValueMax` | `advanced.fatigueMechanics` |

## Interpretation rule

- Novice / Stalker / Veteran neutral `100%` or `Off` values on Master override controls often represent **absence of an override block**, not an explicit vanilla field. These controls remain marked `runtime_test_required` where inserting an equivalent override into a non-Master base preset still needs validation.
- Master bundle toggles are discrete list-membership mechanics switches. Their child sliders do not replace the switch.
- Moving a control into this tab does not change its CFG target, vanilla values, or preset ID.

## UI bundle behavior

Master-only survival/health mechanics are presented as parent bundles. The parent toggle is authoritative:

- **OFF** — the bundle is inactive and its child controls are hidden from the list.
- **ON** — the bundle is active and its child controls become visible and editable.

This is a presentation change only. Existing parameter IDs, `requiresToggle` dependencies, preset serialization and CFG targets remain unchanged.

