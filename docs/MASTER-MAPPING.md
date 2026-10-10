# MASTER-MAPPING.md

## Master UI presentation — final tab redesign (2026-10-09)

**Current five-section order:** STARVATION PENALTIES, SEVERE FATIGUE, RADIATION PENALTY, LOW HEALTH PENALTY, CONSUMABLE EFFECT PENALTIES, using existing groups `Starvation`, `Severe Fatigue`, `Radiation`, `Low Health`, `Medical Effect Modifier`. This is UI-only; no group or parameter ID has changed in the catalog and all **5 mechanics toggles + 15 child values** remain connected to the audited CFG targets and 15 dependency edges below.

The shared Master card uses one native horizontal source track, the selected difficulty's grey baseline tick, source-min/source-max labels, changed-only larger gold numeric value, and common handle-only hover glow. Child controls are muted and uneditable with their custom preset values preserved while the parent mechanic is OFF; the parent universal square checkbox itself stays editable. With sufficient width 2 columns are centered (each card capped at 370 DIP); narrow viewports use one capped 440-DIP column. The Starvation panel keeps two timing sources and a **read-only timeline of exactly three repeated damage tick positions** after the configured grace delay; this is a visual timing illustration, not the engine runtime health-loss/damage-reduction model. Under that timeline: extra stamina cost, per-tick damage and health regeneration loss. Severe Fatigue retains the five individual magnitude controls in 2×3. Radiation and Low Health have one native conditional source each. Consumable Effects retains three separate healing multipliers and a distinct muted 100%-original-heal reference: 25% Bandage, 71% Medkit and 82% Army Medkit are **authored vanilla Master source values**, not effective HP healed after other item changes. The consumable bundle also affects other non-medical consumable modifiers; unchanged.

This layout change is **not** the deeper CFG audit; its findings and unresolved non-Master override behavior remain governed by this source map and later validation. UI lock details are in `UI-DESIGN-LOCK.md` and `VISUAL-CONTROL-SYSTEM.md`.

## Activation thresholds remain vanilla

Master bundle activation thresholds are intentionally not exposed. Starvation, severe fatigue, radiation and low-health bundles enter when the game reaches their vanilla status-state conditions. The Master tab only controls whether the bundle is enabled and how strong its consequences are.

## Purpose

The **Master** tab contains dedicated Master-only survival/health mechanics bundles from `AdditionalMechanicsEffects`. Combat-facing PSY overrides remain in Combat so players find them with the systems they affect. Ordinary difficulty scalars that simply have a Master value remain in their normal gameplay tabs.

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

