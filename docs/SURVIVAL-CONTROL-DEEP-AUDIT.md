# Survival Control Deep Audit — v0.2.47

This release revalidates every normal Survival-tab control against the locked runtime CFG/writer baseline. The audit follows the same chain used for Combat, Health and Stamina:

`UI meaning -> vanilla value -> slider range/direction -> writer operation -> concrete CFG edit points`

## Result

- **23** normal Survival controls audited.
- **23 confirmed**, **0 runtime-test-required**, **0 invalid/partial**.
- **70** concrete writer edit points.
- No new Survival control was promoted and no existing control was removed.
- Two hunger stamina-recovery controls were normalized from engine-signed values to positive player-facing penalty magnitudes.
- One stale documentation value (`LowBleedingDamage = 0.33`) was corrected to the already-audited runtime value `1.0`; catalog/writer gameplay output was already correct.

## Control matrix

| Control ID | Player-facing control | Vanilla/reference | Writer operation | Edit points | Verdict |
|---|---|---:|---|---:|---|
| `survival.hungerRate` | Hunger Build-up Rate | `0.015` | `set` | 1 | Confirmed |
| `survival.hungerMediumStaminaRegen` | Hunger Stamina Recovery Penalty | `33%` | `negateSet` | 2 | Confirmed |
| `survival.hungerStrongStaminaRegen` | Severe Hunger Stamina Recovery Penalty | `70%` | `negateSet` | 2 | Confirmed |
| `survival.sleepinessRate` | Fatigue Build-up Rate | `0.01` | `set` | 1 | Confirmed |
| `survival.energyDrinkFatigueRelief` | Energy Drink Fatigue Relief | `100%` scale | `scale` | 8 | Confirmed |
| `survival.sleepEligibilityThreshold` | Sleep Becomes Available At | `50` | `set` | 1 | Confirmed |
| `survival.minimumSleepHours` | Minimum Sleep | `7 h` | `set` | 1 | Confirmed |
| `survival.radiationAccumulation` | Radiation Accumulation | `0.60 / 1.00 / 1.50 / 1.85` | `set` | 4 | Confirmed |
| `survival.radiationRecovery` | Radiation Recovery | `0.05` | `set` | 1 | Confirmed |
| `survival.radiationDamage` | Radiation Damage | `100%` scale | `scale` | 22 | Confirmed |
| `survival.bleedingSeverity` | Bleeding Effect Strength | `0.25 / 1.00 / 1.00 / 1.50` | `set` | 4 | Confirmed |
| `survival.bleedingRecovery` | Natural Bleeding Recovery | `0.3` | `set` | 1 | Confirmed |
| `survival.wellFedStaminaRegen` | Well-Fed Stamina Recovery Bonus | `20%` | `set` | 2 | Confirmed |
| `survival.fatigueLowAimPenalty` | Low Fatigue Aim-Time Penalty | `15%` | `set` | 2 | Confirmed |
| `survival.fatigueMediumAimPenalty` | Medium Fatigue Aim-Time Penalty | `25%` | `set` | 2 | Confirmed |
| `survival.fatigueStrongAimPenalty` | Severe Fatigue Aim-Time Penalty | `40%` | `set` | 2 | Confirmed |
| `survival.fatigueMediumReloadPenalty` | Medium Fatigue Reload-Time Penalty | `15%` | `set` | 2 | Confirmed |
| `survival.fatigueStrongReloadPenalty` | Severe Fatigue Reload-Time Penalty | `18.75%` | `set` | 2 | Confirmed |
| `survival.sleepHealthRestore` | Post-Sleep Health Restore | `300` | `set` | 2 | Confirmed |
| `survival.sleepStaminaRestore` | Post-Sleep Stamina Restore | `100%` | `set` | 2 | Confirmed |
| `survival.bleedingLowDamage` | Low Bleeding Damage | `1.0` | `set` | 2 | Confirmed |
| `survival.bleedingMediumDamage` | Medium Bleeding Damage | `1.5` | `set` | 2 | Confirmed |
| `survival.bleedingStrongDamage` | Severe Bleeding Damage | `2.0` | `set` | 2 | Confirmed |

The four values shown for difficulty-dependent controls are Novice / Stalker / Veteran / Master.

## Signed hunger-recovery boundary

Runtime `EffectPrototypes.cfg` stores the standard hunger recovery modifiers as negative deltas:

- `MediumHungerStaminaRegeneration.ValueMin/ValueMax = -33%`
- `StrongHungerStaminaRegeneration.ValueMin/ValueMax = -70%`

Exposing those raw signs made the old sliders run backwards from a player's point of view: moving right toward zero weakened the penalty even though the control described hunger recovery impact.

v0.2.47 uses positive penalty magnitudes instead:

- **Hunger Stamina Recovery Penalty**: vanilla `33%`, range `0..66%`.
- **Severe Hunger Stamina Recovery Penalty**: vanilla `70%`, range `0..100%`.
- Writer operation: `negateSet`, so `33% -> -33%` and `70% -> -70%` in the CFG.
- Pre-v0.2.47 presets containing negative values are migrated with `abs(value)` on load, preserving the same generated gameplay value.

The linked constraint is now expressed in the same player-facing value space:

`Hunger Stamina Recovery Penalty <= Severe Hunger Stamina Recovery Penalty`

The actively moved slider remains authoritative and the neighboring slider only moves as much as necessary to preserve that relation.

## Other linked Survival controls

The existing value-based constraints remain valid:

- `Low Bleeding Damage <= Medium Bleeding Damage <= Severe Bleeding Damage`
- `Low Fatigue Aim-Time Penalty <= Medium Fatigue Aim-Time Penalty <= Severe Fatigue Aim-Time Penalty`
- `Medium Fatigue Reload-Time Penalty <= Severe Fatigue Reload-Time Penalty`

These relationships use actual control/CFG values rather than percentages of each slider's vanilla baseline.

## Low Bleeding documentation correction

`CONFIG-BASELINE.md` already records the v0.1.63 runtime re-audit proving:

`LowBleedingDamage.ValueMin = 1.0`

`LowBleedingDamage.ValueMax = 1.0`

The catalog and writer baseline have used `1.0` since that correction. `SURVIVAL-MAPPING.md` still contained an older `0.33` line; v0.2.47 corrects that documentation-only drift. No gameplay writer value changed for Low Bleeding Damage in this release.

## Deliberate Survival boundaries

The deep audit does not turn every survival-related CFG field into a slider. These remain outside normal Survival UI:

- thirst, because the supplied CFG evidence still does not establish a complete active thirst mechanics chain;
- hunger/fatigue/bleeding/radiation state activation thresholds;
- `SleepHunger`, whose player-facing runtime semantics remain insufficiently proven;
- `bAllowEmissionSleep`;
- ambiguous bleeding movement-speed effects;
- Master-only starvation, severe-fatigue and radiation bundles, which remain in the Master tab.

This is intentional scope control, not missing writer coverage.
