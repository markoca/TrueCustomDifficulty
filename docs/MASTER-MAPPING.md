# MASTER-MAPPING.md

### 2026-10-12 — Starvation TTK HUD (UI-only)

Replaced the old small `FULL HP → 0 IN...` sentence with a compact 2-row **STARVATION TTK** HUD: large centered derived `~T` (raw time to zero), right `K TICKS` / `FULL X HP`, left `RAW · NO HEALING`. The readout is hypothetical, not a guaranteed in-game kill time, and becomes muted/conditional if the parent mechanics toggle is OFF. Missing health data, zero direct tick damage or invalid timing suppress the countdown. No changes to `ceil(MaxHP / HP-per-tick)` or `delay + tickCount × interval`, five native CFG sources, independent health maximum, or preset/writer behavior. Preview height **176 DIP**, footer y+127 to +168. This supersedes the previous 152-DIP textual footer.

### 2026-10-12 — Starvation full-health raw damage estimate (UI-only)

The Master starvation preview observes **Health → Player Max Health** read-only across tabs and computes how long periodic *raw* starvation tick damage would take to exhaust a **full** configured HP bar: `ceil(maxHP / damagePerTick)` charges, after `delay + charges × interval` seconds from bundle activation. Example: 100 HP, 4 HP/charge, 2s delay and 2s interval → 25 ticks and 52s. It is explicitly labeled `RAW, NO HEALING` and is not an in-game prediction of death or actual effective damage. OFF-state is conditional; zero damage means no lethal countdown. The extra read-only health observation must not join Starvation's editable family or group reset. Timeline numbers move above the smaller axis dots and the preview grows from 124 to 152 DIP without changing any CFG mapping.

### 2026-10-11 — Starvation inactive-state readability; medicine candidates (current)

**Master → Starvation Penalties:** when the authored **Starvation Mechanics** parent is OFF, all five child source values **remain disabled/non-editable** and receive no editable gold track/handle or hover hit targets. Their **card captions and current numerical readouts** must nevertheless use neutral high-legibility text (`CloneWithOpacity(text, 0.75)`), not the much dimmer common `muted` brush that made the entire Starvation section appear blacked out compared with the other Master panels. Source rails, grey native references, parent toggle behavior, preset and CFG writes remain unchanged. Enabled and modified states continue to use the existing normal/changed-only accent convention. This is a presentation-only correction to `DrawMasterNativeCard` scoped to `StarvationTimeline`, not a forced activation of Master-only gameplay mechanics.

**Next consumables audit:** `docs/CONSUMABLE-MEDICINE-CFG-AUDIT-2026-10-11.md` records source-backed candidates from the provided GameData CFG: bleed-recovery medicine `Cinnamon` (possibly known as Vinca; localization name unverified), `PSYBlocker`, and the *two linked* Hercules carry-weight effects. Six compact editor concepts are **audit suggestions only**, not approved/rendered controls. No new sliders, knobs or writer definitions were introduced. Do not conflate new medicine effects with existing medical healing/radiation removal or drink editors.

### 2026-10-11 — Paired Master Radiation / Low Health penalty knobs (latest authoritative layout)

The **RADIATION PENALTY** (`MasterRadiationPenalty`, group `Radiation`) and **LOW HEALTH PENALTY** (`MasterLowHealth`, group `Low Health`) sections retain their **separate** original parent mechanics toggles, two independent source parameters and normal Master-section heading style. Their former horizontal penalty sliders are replaced by the **same frameless 270° `DrawSourceKnob` family** used throughout the application (native-step-aligned graduated arc, yellow current arc, muted source/reference tick, interactive angular dragging, wheel/keyboard and right-click per-source reset). Both knobs use a centered **204 × 181 DIP** maximum local footprint and source-native **0.01 steps over 0–1**; their visual endpoints and center readout use explicit **signed percentages**:

- **ACTION STAMINA COST** (`survival.radiationStaminaDrainPenalty`) displays **+0…+100%** while editing the exact native 0…1 modifier (original Master reference 0.5 = +50%).
- **HEALTH RECOVERY LOSS** (`health.masterLowHealthPenalty`) displays **−0…−100%** while editing the exact native 0…1 modifier (original Master reference 0.7 = −70%). The original 30-HP activation threshold is not altered.

These **two independent Master group sections sit beside each other in one responsive row** (Radiation left, Low Health right) when the section host is at least **740 DIP** wide, with **12 DIP** separation. Below 740 DIP they remain full-width **stacked in the same order** to prevent clipping. This pairing is restricted to those two adjacent group identities in `ResponsiveSectionGridPanel`; all unrelated top-level sections still occupy their own rows. Each conditional group's native height is **298 DIP**, including the centered section heading, unchanged **39-DIP** mechanics toggle at Y=52, and centered dial starting at Y=102. The pair is not one new CFG field or combined switch, and it must never occupy the entire row per knob when there is enough room.

**Behavior invariant:** Both existing toggles still gate their own knob's enabled styling and interaction (`IsControlEnabled`); their Source ID / `AdditionalMechanicsEffects` membership, selected difficulty default, native min/max/step, current/reference values, preset serialization and BPatch writer operations are unchanged. Only source-value **numbers** turn yellow on modification; static `+`/`−` and % units and subdued Base tick do not need separate emphasis. No redundant explanation/Vanilla value below the controls. This specification supersedes older 204-DIP standalone horizontal penalty sections and 440-DIP slider-bank directions.

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

