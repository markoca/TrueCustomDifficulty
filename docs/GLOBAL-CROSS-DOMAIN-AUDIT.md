# Global Cross-Domain Control Audit — v0.2.50

This audit treats the complete player-facing catalog as one surface instead of auditing tabs independently.

## UI surface

- Catalog parameters: **178**
- Visible gameplay controls: **167**
- Audit-only / hidden parameters: **11**
- Visible status split: **137 confirmed + 30 runtime-test-required**
- Control types: **159 sliders + 7 toggles + 1 choice slider**
- Duplicate player-facing labels: **0**
- Vanilla numeric values outside their current slider range: **0**
- Generator-backed controls: **27** = 16 confirmed + 11 runtime-test-required
- Non-generator visible controls: **140** = 121 confirmed + 19 runtime-test-required

Visible controls by tab:

| Tab | Controls |
|---|---:|
| Combat | 25 |
| Health | 15 |
| Stamina | 13 |
| Survival | 23 |
| Economy | 20 |
| Equipment | 8 |
| Master | 20 |
| Environment | 17 |
| Loot | 26 |
| **Total** | **167** |

The older **104** count remains valid only for the six-tab verification scope (Combat/Health/Stamina/Survival/Economy/Equipment). It is not the total UI count. `economy.medicStock` belongs to that six-tab count and is also one of the 27 generator-backed controls, so category totals must be treated as overlapping sets rather than added blindly.

## Hard writer-conflict check

Concrete writer targets were keyed by `(scope, file, prototype, nodePath, key)`. There are **0 collisions between different parameter IDs**. No two controls write the same concrete scalar/list field through independent writer routes.

The only intentional shared declared target is `EnvironmentDifficulty.AdditionalMechanicsEffects`. Five Master toggles use list-membership semantics, each with a distinct member:

- `advanced.lowHealthMechanics` → `RegenHealthNegativeMechanics`
- `advanced.starvationMechanics` → `HungerNegativeMechanics`
- `advanced.radiationMechanics` → `RadiationNegativeMechanics`
- `advanced.fatigueMechanics` → `SleepinessNegativeMechanics`
- `advanced.masterEffectModifierMechanics` → `MasterEffectModifierNegativeMechanics`

This is safe set-membership composition, not a target collision.

## Intentional cross-domain composition

Two cross-domain relationships are deliberate and must not be "deduplicated":

1. **Master mechanics toggles + child tuning sliders.** The toggle controls whether the mechanics bundle is active; the child sliders tune global effect prototypes. All **15** dependent sliders have a valid `requiresToggle` parent and are disabled in the UI while that parent is Off.
2. **Global Anomaly Damage + six base anomaly-damage controls.** The difficulty scalar and the individual EffectPrototype base damages are separate layers. The UI explicitly computes/displays the combined effective result for Electro, Chemical, Fire, Carousel, Toxic Cloud and Steam.

Shared parent CFG nodes such as `Player.VitalParams` and `DifficultyPrototypes.EnvironmentDifficulty` are only structural containers. Their child keys remain distinct, so they are not cross-control writer conflicts.

## v0.2.50 sign normalization

The global scan found the last four visible sliders still exposing engine-negative deltas even though their labels describe penalty/loss magnitude. They now store positive player-facing magnitudes and use `negateSet` when generating CFG:

| Control | UI vanilla | CFG vanilla |
|---|---:|---:|
| Master Low-Health Recovery Penalty | 70% | -70% |
| Starvation Health Recovery Penalty | 100% | -100% |
| Severe-Fatigue Max Stamina Loss | 20% | -20% |
| Severe-Fatigue Carry Capacity Loss | 20% | -20% |

Pre-v0.2.50 presets with negative values are migrated with absolute magnitude on load. This changes only UI representation; generated gameplay values remain identical.

## Decision

The current UI contains **167 distinct controls with no concrete writer collisions**. No control is removed in this release. The four Master signed-delta controls are normalized for consistent player-facing direction, and the complete UI/control accounting is now guarded as a release invariant.

## v0.3.2 loot-axis update

The catalog now contains **179 parameters / 168 visible controls / 11 hidden audit-only entries**. The added visible control is `loot.cheapStashGrenadeQuantity`, giving Cheap-stash RGD-5 a real independent Chance + Quantity pair. Current visible status split is **137 confirmed + 31 runtime-test-required**. Generator-backed visible controls are now **28**; non-generator visible controls remain 140.
