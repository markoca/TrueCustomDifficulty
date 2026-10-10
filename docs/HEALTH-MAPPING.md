# Health CFG Mapping — v0.1.21

### 2026-10-10 — Medical Items: compact HEAL/BLEED row plus RAD removal bank (current authoritative layout)

**MEDICAL ITEMS** is a single 370-DIP-high frame containing **four closely paired HEAL/BLEED item clusters in one horizontal row on the left** (Bandage, Medkit, Army Medkit, Scientific Medkit) and **a fifth, dedicated RADIATION REMOVAL group on the right** (Scientific Medkit and Anti-Rad). Do **not** create or revive a separate Radiation Treatment card, aggregate anchor, or an additional row. The HEAL and BLEED columns keep their two real independent physical axes: vertical **HP** in gold (0–200) on the far left and vertical **PTS** in copper (0–100) between the medical pairs and the RAD bank. The narrower right-hand pair uses **teal** (`RadiationAccent`) for both bar-top handles and the **vertical RAD Y-axis title**, with its **own 0–200 RAD scale** (0/50/100/150/200). Units are not converted or normalized across these distinct physical axes. Food Healing stays an independent checkbox at upper right.

In each left item pair, the **HEAL caption below its vertical bar is now gold** to match the gold healing bar (the BLEED caption remains copper to match its secondary bar). Shrink medical pair spacing to `Min(23 DIP, laneWidth × 0.22)`, medical bar widths to `Min(39 DIP, laneWidth × 0.31)` HEAL and `Min(27 DIP, laneWidth × 0.21)` BLEED, and reserve `ActualWidth − 334 DIP` (minimum 270 DIP) for the four-item medical chart and a **compact right bank starting 55 DIP beyond that chart**. The existing four-item card order and on-card labels remain; on very narrow charts, stagger the BLEED numeric label down enough not to collide with HEAL numeric labels. Keep a single horizontal row throughout rather than stacking or spreading into a second large panel.

The radiation bank has **two full native vertical handles** with the same `VerticalBarHandleChrome` current-value cap / white-hover behavior as the medical bars: **Scientific Medkit 60 RAD reference, 0–120 RAD editable** and **Anti-Rad 100 RAD reference, 0–200 RAD editable**, both in **5-point steps**. They have separate actual numeric readouts and muted native reference/max ticks. Drag, keyboard arrows and right-click reset target only the selected source; `R` resets all eight HEAL/BLEED bars, both RAD bars and Food Healing. All **11 source parameters** (4 healing + 4 bleeding + 2 radiation removal + 1 Food Healing checkbox) live under the existing visible `health.bandageHealing` UI anchor. They remain **11 independently persisted CFG-backed controls**, with the original sign-converting `negateSet` radiation/bleeding patch writers unchanged. Modified-state tracking includes both radiation children. Supersede historical 4x2-only and detached Radiation Treatment UI contracts; this is visual regrouping, not gameplay coupling.

### 2026-10-10 — MEDICAL ITEMS: four HEAL/BLEED paired vertical controls (authoritative UI)

The Health tab shows **one full-width MEDICAL ITEMS frame**, replacing separate MEDICAL HEALING and BLEEDING TREATMENT cards. **Exactly four item clusters in one horizontal row**: BANDAGE, MEDKIT, ARMY MEDKIT, SCIENTIFIC MEDKIT. Every cluster contains **two independently editable vertical columns**: a **wider, gold HEAL** bar on the left (HP restored) and a **narrower, copper-colored BLEED** bar immediately to its right (bleeding points removed). Their **separate actual HP and PTS readouts** sit directly above the bars; small HEAL/BLEED captions distinguish the controls, and the item name sits underneath its pair. The two effects are visually grouped only; there is **no value linking or dependency**.

The compact board is **370 DIP high**, remains one row across all window widths, uses four equal item cells inside the existing max-920-DIP renderer, and has two clearly labeled simultaneous physical rulers: **HP 0–200 at the left** and **PTS 0–100 at the right**, marked in 50-HP / 25-PTS steps. These aligned Y coordinates do not imply the two measures are interchangeable. Healing true native maxima remain **40/140/170/200 HP** respectively; Bleeding true native maxima remain **100/30/70/50 PTS** respectively, with Bandage's max **1x (100 PTS)**. Both use native 5-point increments and **independent native-bounded hit zones**. Reference/native-max ticks remain muted gray; only genuinely changed numeric readouts receive the relevant color. Both bar types share `VerticalBarHandleChrome`: their horizontal top caps become white with subtle glow on hover and track their exact own value during dragging. Right-click resets the selected HEAL or BLEED source, arrow keys edit the last-selected bar, and `R` resets all four pairs plus Food Healing. The existing **Food Healing checkbox** is retained compactly at the upper-right inside this same frame.

The **single visible UI anchor is `health.bandageHealing`**. All eight original numeric backing parameters plus `health.foodHealing` stay live, individually persisted and handled by the existing CFG writer (including bleeding `negateSet` paths), and changes to any of them count as modification of the Medical Items card. The old `BleedingTreatmentMixer` UI family, duplicated standalone card and source renderer are retired. This supersedes older standalone Bleeding Treatment and Medkit Healing layouts elsewhere in the historical design documentation; preserve all source mappings and do not introduce eight disconnected or horizontally stacked sliders.

### 2026-10-10 — Current Bleeding Treatment UI scale correction (supersedes prior UI range)

**Bandage** now has a gameplay editor range of **0–100 PTS** in **5-point steps**, with its original **100 PTS = 1x** as the highest setting. The single shared Bleeding Treatment chart uses **0–100 PTS** vertically (0/25/50/75/100 axis ticks). Other source editors remain **Medkit 0–30**, **Army Medkit 0–70**, and **Scientific Medkit 0–50 PTS**. Each native hit map keeps its own max. Older Bandage presets above 100 are clamped by the existing ViewModel. The original game source, selected difficulty and source-exact negative `negateSet` CFG writer remain unchanged. Prior notes claiming 0–200 for this UI chart or Bandage slider describe the previous implementation and are superseded.

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

## 2026-10-09 — Health graphic UI mapping (no CFG writer changes)

Sixteen player-facing source parameters are still present: Core Health five (`maxHP`, `regenDelay`, `baseRegen`, `regenerationScalar`, `lowHealthRegen`), Medical Healing five (`bandageHealing`, `medkitHealing`, `armyMedkitHealing`, `ecoMedkitHealing`, `foodHealing`), Bleeding Treatment four (`bandageBleeding`, `medkitBleeding`, `armyMedkitBleeding`, `ecoMedkitBleeding`) and Radiation Treatment two (`ecoMedkitAntirad`, `antiRadRadiation`), all with prefix `health.`.

The 25–200 HP ruler and all three treatment charts are **direct editors of these existing sources**. Comparison axes use common units but do not overwrite per-source limits: Medkit HP 140, Army HP 170, Scientific HP 200; bleeding removal 100/30/70/50; Scientific radiation removal 120 and Anti-Rad removal 200. Vertical treatment bars have individually bounded hit regions; Radiation Scientific's excess 120–200 area is read-only grey. Five-point native amount steps remain. Current positive recovery/removal magnitudes stay normal Health UI numbers; the underlying sign inversion for CFG deltas remains handled by the existing writer. This is visual-only and does not add effects, durations, change other runtime fields, or touch the existing audited NO-UI boundaries.
