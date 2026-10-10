# Health Control Deep Audit — v0.2.45

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
| Bandage Bleeding Removal | 100 | 0–100 (max 1x) | -100 | `negateSet` |
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

## 2026-10-09 — Display-vs-source reconciliation for Health redesign

The approved Health redesign maps to all 16 previously accepted source controls. Source-by-source native steps, bounds, difficulty references, sign normalization and producer/writer domains are **unchanged**. The large Max Health editor stays bounded 25–200 HP, step 5. The per-medkit source maxima (140/170/200 healing HP), bleeding-treatment maxima (100/30/70/50 removed points) and radiation-treatment maxima (120/200 removed points) are now visible as *real physical chart boundaries*: unlike an unbounded shared graph, clicking outside one item's native range no longer modifies that item. Each source retains native 5-point snapping and individual reference reset.

The current fixed 0–100 shared Bleeding Treatment chart is purely a comparison coordinate system, never a new 200-point maximum for every source. Signed treatment CFG deltas remain a separate writer concern. Food Healing remains a boolean checkbox and the two low-health/rate channels remain independent. No synthetic total effective healing, time simulation or runtime-derived regeneration claim is introduced. UI heights in order: Core Health 348, Medical Healing 400, Bleeding Treatment 376, Radiation Treatment 250 DIP. Native geometry and CFG mappings must be checked with the local Wine build.
