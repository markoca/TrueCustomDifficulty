# Visual Control System

True Custom Difficulty keeps every gameplay setting as its original parameter in the preset and CFG-writer graph, while related parameters may be edited through one aggregate visual surface.

**Mixer and responsive grid implementation must follow [UI-MIXER-GRID-SPEC.md](UI-MIXER-GRID-SPEC.md).** This document continues to describe existing non-mixer visual families and their established interaction contracts.

The visual layer never invents replacement gameplay values. It edits the same live `ParameterViewModel` instances used by the ordinary parameter system.

## Compact readability contract

Semantic controls follow one density/readability baseline:

- The default application window is **1600×900**, minimum **1024×576**, preserving **16:9**. The window fits itself to the available work area. Mixer/grid layout follows the authoritative [Mixer + Responsive Grid UI specification](UI-MIXER-GRID-SPEC.md).
- Large semantic controls are centered and capped at 920 px so maximizing the window does not stretch tracks across the whole screen.
- Custom-rendered text has a 9.0 minimum FontSize. Important values and labels use larger sizes.
- Shared aggregate title/subtitle typography uses 11.5/9.5 px; the shared renderer confines labels to the available horizontal viewport, **trims long labels on one line with an ellipsis**, and shifts centered labels away from clipped panel edges. This does not shrink below the 9 px minimum or change any numeric values.
- Shared Base Preset reference ticks are visually thicker and taller for legibility.
- Paired **72 px** source-value cards (Core Health, Human Combat, Psy Phantom Pressure) have a fixed hierarchy: title, current/Base Preset value, native-range track, and legible min/max. Titles and values occupy separate rows and are clipped **inside their own card**, not to the right edge of the full panel.
- Bundle toggle pills reserve a dedicated 52 px ON/OFF lane. Long captions ellipsize before that lane instead of painting over the current state.
- Equipment Wear and Human Combat crosshair panels show both independent axis captions and the current/Base Preset multipliers on separate, half-width rows above the plot. No crosshair drag semantics, sliders, or CFG values change.
- Detailed implementation caveats belong in the rationale/help panel; the control surface itself should show only the value, Base Preset reference and immediate gameplay meaning.
- Large entity families use multiple columns when that improves scanability instead of growing into very tall single-column dashboards.
- The Fatigue, Consumables and Hunger aggregate panels use a shared 82 px source-value card, with native-range sliders, 9.2+ px axis labels, 10+ px headings and a grey Base Preset reference tick. The heading and current/base values have separate lines and are clipped to their own card, not across neighboring columns.
- These panels dynamically reflow: ordinary editing cards use two columns at **700 px** aggregate width; Hunger and fatigue stage-threshold cards use up to three columns at **850 px** aggregate width. Narrower containers use fewer columns while retaining the original font sizes and hit targets.
- Their measured heights are calculated from the same wrapped row counts used by the renderer; no fixed 975/1080/748 px boards or bottom clipping at breakpoint changes.
- Grey ticks/markers carry Base Preset reference information so repeated micro-labels such as `VANILLA`, `0%` and `200%` do not need to be printed on every row.
- Compactness must never change backing parameter values, constraints, preset serialization or CFG-writer behavior.

## Shared interaction contract

- Left-click or drag a visible handle, point, meter or track to edit its backing value.
- Mouse wheel is not consumed by aggregate visual controls; the surrounding page continues to scroll.
- Hovered slider tracks show a subtle gold outline and a short source-parameter tooltip; draggable controls use horizontal/vertical resize cursors, while toggles use a hand cursor. Read-only graphs have no clickable hit target.
- A keyboard-selected slider retains its outline while the aggregate control has focus, even after the pointer leaves its track.
- Left/Right and Up/Down adjust the selected slider by its **catalog tick**. PageUp/PageDown move 10 ticks; Home/End select the source minimum/maximum. All keyboard edits use the same source-value snapping as dragging.
- Right-click an individual interactive element to restore only that backing parameter to the current Base Preset.
- Press `R` while a visual control has focus to restore the whole visual family to the current Base Preset.
- Vanilla/Base Preset values are shown with grey ticks, dashed curves, ghost envelopes or labelled reference markers.
- Preset serialization and CFG writer routes remain unchanged.

### WPF template visibility guard

When an aggregate family is anchored on a toggle (currently `advanced.fatigueMechanics`), the template must keep the `ControlRow` visible for its `AggregateVisualControl`. Ordinary toggle styling is therefore conditioned on **both** `IsToggle=True` and `IsVisualAggregateAnchor=False`. This prevents the later toggle trigger from hiding the entire visual board. The board itself contains the bundle toggle; no second ordinary checkbox should appear.

The WPF visual-anchor contract is maintained in the source and the UI design documentation.

### Constraint authority

Ordered gameplay relationships are owned by the central `SliderConstraintCoordinator`, not by individual visual controls.

The coordinator receives every `CurrentNumber` change, including changes made through aggregate visuals, and applies constraints using actual parameter/CFG values. The actively edited value has priority; neighbouring values move only as far as required.

Current ordered families include:

- Bleeding damage: `LOW <= MEDIUM <= SEVERE`.
- Bleeding stage thresholds: `MEDIUM <= SEVERE`; LOW stays fixed at the authored 0.1 boundary.
- Fatigue stage thresholds: `LOW <= MEDIUM <= SEVERE`.
- Fatigue aim penalty: `LOW <= MEDIUM <= SEVERE`.
- Fatigue reload penalty: `MEDIUM <= SEVERE`.
- Hunger stage thresholds: `LOW <= MEDIUM <= SEVERE`.
- Hunger stamina-recovery penalty: `HUNGRY <= SEVERE` by penalty magnitude.
- Repeated-hit protection: `MIN <= MAX`.
- Repair pricing: `FRIENDLY <= NEUTRAL <= DISLIKED <= HOSTILE`.
- Water stamina drain and melee stamina cost relationships.

Ammo-box pool validity is a separate family invariant rather than an ordering constraint. The UI prevents the SNG pool from reaching all-zero weights, and the CFG writer independently rejects any destructible ammo pool whose effective weights are all zero.

## Aggregate-family audit

| Family | Semantic visualization | Status |
| --- | --- | --- |
| Fatigue | Read-only LOW / MEDIUM / SEVERE stage preview and compact native-range combat-penalty/source tracks | Semantic |
| Equipment Wear | Two independent native CFG axes: armor toughness and player-weapon wear per shot, with current/base comparison | Semantic |
| Psy Phantom Pressure | Two independent phantom-cycle timing/count axes, plus gear damage and bleeding toggle | Semantic |
| Human Combat Profile | Native human health and gun-damage axes, plus independent attack-frequency and accuracy tracks | Semantic |
| Your Weapons | Three compact native-value lanes for base hit damage, headshot multiplier and weapon jamming | Semantic |
| Core Health | Max-HP rail, recovery-delay ruler, passive regen, difficulty effect scaling and a separate low-health recovery track | Semantic |
| Master Severe Fatigue | On/off bundle plus five independently editable severe-fatigue penalties; inactive tracks preserve stored values | Semantic |
| Master Medical Healing | Master consumable effect bundle toggle plus three source-exact healing-retention multipliers, on a common 0–200% axis | Semantic |
| Master Low Health | Compact bundle toggle and linked conditional health recovery loss, on a 0–100% native scale | Semantic |
| Master Radiation | Compact bundle toggle and linked additional action stamina cost, on a 0–100% native scale | Semantic |
| Bleeding Treatment | Shared normalized treatment-strength mixer against each item's own vanilla value | Semantic |
| Consumables | Authored instant-effect previews plus one real-seconds duration timeline | Semantic |
| Sleeping | Sleep gate, minimum sleep window and wake-up recovery | Semantic |
| Hunger | Shared stamina-recovery axis from penalty through neutral to bonus | Semantic |
| Starvation | Real-time grace period, repeating damage cadence and active penalty meters | Semantic |
| Repeated Hits | Current and vanilla protection envelopes on one curve-weight scale | Semantic |
| Radiation | Separate exposure/clearance controls, normalized tuning bias and downstream damage | Semantic |
| Trading | Transaction-direction pricing flow, condition gate and broken-item value floor | Semantic |
| Repair | Reputation-to-repair-price curve with surcharge/base/discount regions | Semantic |
| Ammo Distribution | Separate SNG/NATO pool composition previews plus family-weight editors | Semantic |
| Stash Reward Rolls | Three source-native vertical faders: Basic grenade chance, Basic grenade count, Rare coupon chance; Wine QA pending | Mixer pilot |
| Breakable Box Yields | Two vertical mixer faders for ammo and grenade counts; original 0.25x / 0.5x steps retained; Wine QA pending | Mixer pilot |
| World Loot Layout | Four independent reshuffle/removal toggles and a weapon-shuffle-dependent density track | Semantic |
| NPC Loot & Equipment | Discrete corpse-loot selector, independent medical/ammo tracks and MIN/MAX dropped-weapon condition | Semantic |
| Explosion Damage | Direct blast-damage multiplier and read-only Base Preset comparison band | Semantic |
| Physical Limits | Independent vault height (uu) / carry mass (kg) source tracks with original vault landmarks | Semantic |
| Trader Stock | Two responsive vertical faders for ammo and medic supply (0.25x native steps); Wine QA pending | Mixer pilot |
| Service Prices | Three source-bound vertical faders for guide, upgrade and base repair prices; Wine QA pending | Mixer pilot |
| Item Weight | Two independent native-value vertical weight faders; Wine QA pending | Mixer pilot |
| Anomaly Effects | Six native-value vertical mixer faders with responsive rows, clickable Base marks and per-channel rationale selection; Wine QA pending | Pilot |

The old generic duration fader-bank helper is no longer used.

## Family mapping

### Fatigue Response

`AggregateVisualControl / FatigueCurve`

The redesigned **Fatigue Penalties** panel separates actual SleepinessPoints progression, the three authored stage thresholds and the five combat penalties. The previous combined X/Y graph with overlapping drag targets is replaced by a **read-only stage preview** and ten native-range, independently editable sliders. The compact panel uses **two columns for build-up and other ordinary controls** (at least 700 px wide), with **up to three threshold and combat-penalty cards per row** at 850 px. On narrower screens, all controls wrap without shrinking text or editing targets.

- **Fatigue Build-up Rate**: `survival.sleepinessRate`, raw `VitalParams.RegenSleepinessPoints` (0–0.020 points/s, 0.001 step; vanilla 0.010).
- **Energy Drink Fatigue Relief**: `survival.energyDrinkFatigueRelief`, a 0–2x scaler (0.01 step, vanilla 1x) on the four already-authored energy-drink fatigue-removal effects. This changes their effect strength, not the effect duration.
- **Low / Medium / Severe Fatigue Thresholds**: `survival.sleepLowThreshold`, `survival.sleepMediumThreshold`, `survival.sleepStrongThreshold`. Each uses the real 0–100 SleepinessPoints scale in whole points; vanilla 50 / 60 / 80. The noninteractive stage preview shows colored progression, gold current threshold marks, grey Base Preset marks and three fixed-width live/base labels to avoid collisions.
- **Aim-Time Penalties**: Low `survival.fatigueLowAimPenalty` (0–30%, vanilla +15%); Medium `survival.fatigueMediumAimPenalty` (0–50%, vanilla +25%); Severe `survival.fatigueStrongAimPenalty` (0–80%, vanilla +40%). All three are independent 0.01-raw-step editors with their own CFG effect prototypes.
- **Reload-Time Penalties**: Medium `survival.fatigueMediumReloadPenalty` (0–30%, vanilla +15%, 0.01 step); Severe `survival.fatigueStrongReloadPenalty` (0–37.5%, vanilla +18.75%, 0.0025 raw step). The fractional vanilla value remains fully visible; it is not rounded to 19%.
  
There is **no Low-stage reload effect** in the authored CFG, so no fabricated sixth combat slider is exposed. The stage thresholds and combat penalty intensities remain separate independent controls. Base Preset reference values appear in grey behind gold current handles, with original source bounds at track endpoints.

The centralized value-based `LOW <= MEDIUM <= SEVERE` threshold constraint remains active, prioritizing the currently moved slider and adjusting neighbors minimally. Right-click resets one source setting, while `R` resets all ten family parameters. No new CFG writer mappings, preset fields, inferred formulas or gameplay semantics were introduced.

### Equipment Wear Balance

`AggregateVisualControl / EquipmentWearBalance`

- Horizontal axis = `equipment.armorDurability` (`EnvironmentDifficulty.Armor_Durability`); higher means tougher armor, but is **not** a proven inverse wear percentage.
- Vertical axis = `equipment.weaponWearPerShot`, scaling each nonzero `DurabilityDamagePerShot` in the existing 41 base + 11 DLC player-weapon routes; higher means faster weapon condition loss.
- Grey dashed lines/ghost point = the selected Base Preset; yellow lines/point = the current values.
- Drag the **bottom gold circle** to edit armor toughness or the **left square** to edit weapon wear. The gold crosshair intersection is a read-only summary; clicking it does not modify either CFG value. The on-panel legend now identifies these two separate handles.
- Right-click either handle to reset just that parameter; R resets the pair.
- Both axis headings and live/Base Preset values now occupy separate, width-bounded text lines, eliminating the left/right label collision at the minimum window width.
- Both parameters retain their distinct native ranges/steps, per-difficulty baselines, preset serialization and writer routes. The two item-weight sliders remain separate and unchanged.
- The visual does **not** claim a specific armor durability-to-wear formula and does **not** alter weapon jamming.

### Human Combat Profile

`AggregateVisualControl / HumanCombatProfile`

- X axis: `combat.npcHealth` (the selected difficulty's `NPC_HP` multiplier); farther right = tougher human enemies.
- Y axis: `combat.npcWeaponDamage` (the selected difficulty's `NPC_Weapon_BaseDamage` multiplier); higher = stronger firearm hits from human enemies.
- A grey ghost marker is the selected Base Preset; the yellow crosshair is the current pair. The crosshair is display-only; the **bottom circle** adjusts enemy health and the **left square** adjusts gun damage. The on-panel legend now names both handles explicitly.
- Below the plot, `combat.npcAttackCooldown` edits player-facing attack **frequency**, preserving the existing writer's reciprocal `NPC_AttackCooldown` conversion. `combat.npcHipAccuracy` edits the native accuracy multiplier.
- Each axis has its own two-line title and exact current/Base Preset value, with a strict left/right half-width text boundary so the two headings do not overlap.
- Both lower tracks use separate title/value lines, a gold editable handle, grey Base Preset marker, native range labels and the original step.
- These four independent controls do **not** produce a computed hit chance, attack DPS, or time-to-kill; the engine's complete combat model is not claimed.

### Psy Phantom Pressure

`AggregateVisualControl / PsyPhantomPressure`

- **Phantom Cycle Wait** (`combat.psySpawnCycleDelay`) is the horizontal source multiplier, edited only with the **bottom circular handle**.
- **Phantom Count** (`combat.psyPhantomCount`) is the vertical source multiplier, edited only with the **left square handle**.
- Both headings and current/Base Preset numbers occupy independent, width-bounded rows, matching the Human Combat and Equipment Wear graph style. The interior crosshair point and its guideline intersections are **read-only**, even when the current and base points overlap.
- **Psy Gear Damage** (`combat.psyDurabilityDamage`) remains its own native-range slider and **Psy Bleeding** (`combat.psyBleeding`) remains a separate ON/OFF bundle control. No artificial phantoms-per-minute estimate or joint-axis editing has been introduced.

### Your Weapons

`AggregateVisualControl / PlayerWeaponProfile`

Three directly adjustable and independent rows:

- `combat.playerWeaponDamage` tunes the selected difficulty's `EnvironmentDifficulty.Weapon_BaseDamage` (0.25x–2x). Its Base Preset reference can be 1.30x, 1x or 0.75x depending on the selected difficulty.
- `combat.playerHeadshotMultiplier` tunes `NPCCombatDifficulty.PlayerWeapon_HeadshotMultiplier` (0.5x–3x). The selected baseline is 1x for Novice/Stalker/Veteran and 2x for Master.
- `combat.weaponJamming` tunes `NPCCombatDifficulty.Weapon_JammingMultiplier` (0x–2x). The selected baseline is 1x for Novice/Stalker/Veteran and 1.5x for Master.

Each lane uses its own native absolute multiplier range and 0.01 step. The grey reference tick is the actual selected Base Preset, not a universal 1x. Gold handles edit only the specified CFG setting; the gold segment between base/current communicates a tuning delta, not computed effective damage or jam probability. Right-click resets a single lane, R resets the family. No damage-per-second, hit probability or headshot final-damage calculation is invented.

### Core Health

`AggregateVisualControl / CoreHealthDashboard`

- MAX HEALTH edits `health.maxHP` on its real 25–200 HP range. This updates both the player `VitalParams.MaxHP` and the `HealthMechanics.FullHP.ConditionValue` value, but other 100-HP gameplay assumptions remain runtime-test required.
- CONFIGURED RECOVERY DELAY edits `health.regenDelay` in the raw 0–10 second range. The shaded section represents the *configured delay*, not a proven HP recovery timeline: not every recovery source is confirmed to honor it.
- PASSIVE REGEN edits `health.baseRegen` (0–5 native `VitalParams.RegenHP`, 0.5 step). The source field's actual tick cadence and effect layering still need a runtime test, so it is not labeled as HP/s.
- RECOVERY EFFECT SCALE edits `health.regenerationScalar` (0x–2x, 0.01 step), the selected difficulty's `EnvironmentDifficulty.Regen_HP` multiplier. This changes compatible regeneration effects but does not create passive healing by itself.
- The paired PASSIVE REGEN and RECOVERY EFFECT SCALE cards now put the heading above the exact current/Base Preset value instead of trying to fit both on a single line. Both keep native ranges, grey reference markers, and independent draggable hit targets.
- Four independent controls preserve their own ranges, steps, presets, and CFG routes. Gold handles are editable, grey ticks are Base Preset references. Right-click restores a single setting; R restores all four. No fabricated healing-rate curve or effective HP-per-second arithmetic is shown.

### Master Severe Fatigue

`AggregateVisualControl / SevereFatiguePenaltyBoard`

- Toggle: `advanced.fatigueMechanics`, the `SleepinessNegativeMechanics` membership of the selected difficulty's `EnvironmentDifficulty.AdditionalMechanicsEffects`. Master defaults ON; the other vanilla difficulties default OFF.
- MAX STAMINA LOSS: `survival.severeFatigueMaxStamina`, a 0–40% loss (vanilla 20%).
- CARRY CAPACITY LOSS: `survival.severeFatigueCarryWeight`, a 0–40% penalty applied to both existing carry-related effects (vanilla 20%).
- RECOIL INCREASE: `survival.severeFatigueRecoil`, a 0–110% increase (vanilla 55%).
- AIM SWAY: `survival.severeFatigueAimSway`, a 0x–0.8x source multiplier (vanilla 0.4x) mapped to both X and Y sway effects.
- SHOT SPREAD: `survival.severeFatigueDispersion`, a 0x–1x source multiplier (vanilla 0.5x).
- The compact panel uses two capacity tracks, two handling tracks, and a centered final Shot Spread track. Each active track displays a gold fill from its actual minimum to the current value; the grey Base Preset tick remains visible even when current and reference values coincide. The five numeric values and axis endpoints use larger text to remain legible.
- When OFF, the five penalty sliders retain their stored values but cannot be edited until the bundle is switched ON. Base Preset references remain visible in grey. The activation point is unchanged and ordinary Low/Medium fatigue mechanics are separate. Right-click restores a single enabled setting; R restores all six source values and the toggle.
- The panel edits these six existing CFG paths exclusively. It does not infer compounded spread/recoil formulas or introduce a new in-game difficulty.

### Master Medical Healing

`AggregateVisualControl / MasterMedicalHealing`

- `advanced.masterEffectModifierMechanics` controls membership of `MasterEffectModifierNegativeMechanics` in the selected vanilla difficulty's `AdditionalMechanicsEffects`. It activates **more than just medical effects**, including bundled food, energy-drink and artifact behavior. The panel must never imply the toggle affects medkits alone.
- Bandage, Medkit and Army Medkit are direct `MasterEffectModifier.EffectModifiers[*].ValueMultiplier` scalars with distinct fixed vanilla references: 25%, 71% and 82%, respectively. They share a real 0–200% of **each item's normal healing** axis; 100% means the unmodified item healing strength, not the vanilla Master value.
- Golden fills show each currently configured healing-retention multiplier. The grey tick shows the item's vanilla source multiplier. The subtle dashed center marker shows 100% original healing for reference; it is not a separate parameter.
- Child tracks are disabled while the bundle toggle is off without discarding preset values. There is no invented final HP-restored calculation since base healing can be changed independently on the Health tab.
- Right-click an individual slider or switch resets that backing parameter; R resets the entire four-parameter family. No CFG writer or preset schema changes.

### Master Low Health and Radiation

`AggregateVisualControl / MasterLowHealth`

- Toggle `advanced.lowHealthMechanics` adds/removes `RegenHealthNegativeMechanics` on the selected Base Preset.
- Child slider `health.masterLowHealthPenalty` edits the source `RegenHealthNegativeHealthRegeneration.ValueMin/ValueMax` as a 0–100% recovery loss, with the vanilla negative 70% effect encoded by the writer.
- The authored Master activation threshold remains 30 HP. This control never changes ordinary low-health behavior or the selected Health-tab regeneration multiplier.
- OFF locks the slider without discarding its setting. Master defaults ON; lower vanilla difficulties default OFF.

`AggregateVisualControl / MasterRadiationPenalty`

- Toggle `advanced.radiationMechanics` adds/removes `RadiationNegativeMechanics` on the selected Base Preset.
- Child slider `survival.radiationStaminaDrainPenalty` edits `RadiationNegativeStaminaPenalty.ValueMin/ValueMax`, adding 0–100% to action stamina costs when the existing heavy-radiation condition applies. Vanilla is +50%.
- The radiation accumulation, passive removal, health damage and authored state thresholds do not change.
- OFF locks the slider without discarding its setting. Master defaults ON; lower vanilla difficulties default OFF.

Both panels use 235px height rather than the global 320px minimum: this is possible because the common WPF control and its XAML declaration no longer impose the old minimum. The other existing aggregate families retain their explicit measured heights (320px and above). Each toggle and its source-true child share one visual card; grey ticks show Base Preset reference values. Right-click one handle to reset it, and R resets its two-member family.

### Bleeding Treatment

`AggregateVisualControl / BleedingTreatmentMixer`

The redesigned board separates **read-only relative strength comparison** from the four **independent, full-width, raw bleeding-removal controls**. The previous narrow vertical faders are removed.

The upper comparison has a small, fixed-width column for **Bandage / Medkit / Army Medkit / Scientific Medkit**. Each column normalizes its current authored removal to that **specific item's own Base Preset value**, on a shared 0–200% relative scale. The grey midpoint tick marks 100% (the item's original strength). Current percentage and raw bleeding-removal points appear below each mini-bar without floating labels or draggable hit targets. **100% does not mean 100 bleeding points for every item.**

The four independently editable source values are:

- **Bandage**: `health.bandageBleeding`, 0–200 bleeding points in 5-point steps; vanilla 100. Writes `BandageBleeding4.ValueMin|ValueMax` with the original `negateSet` operation.
- **Medkit**: `health.medkitBleeding`, 0–30 points in 5-point steps; vanilla 15, `MedkitBleeding2`.
- **Army Medkit**: `health.armyMedkitBleeding`, 0–70 points in 5-point steps; vanilla 35, `ArmyMedkitBleeding3`.
- **Scientific Medkit**: `health.ecoMedkitBleeding`, 0–50 points in 5-point steps; vanilla 25, `EcoMedkitBleeding2`.

Each horizontal row uses the real raw slider min/max, current bleeding points, Base Preset bleeding points, and a percentage of that item's own vanilla reference. Gold handles and grey baseline ticks can be read independently. The stored numeric values remain **positive bleeding-removal magnitudes**; the existing CFG writer continues applying `negateSet`. This panel does **not** control HP healing or overwrite any medical-health-restore effect.

Right-click resets an individual slider; `R` resets the family of four. No changes to CFG bindings, preset schemas, original step frequency or healing/gameplay semantics.

### Consumable Effect Board

`AggregateVisualControl / ConsumableMixer`

The compact panel retains **nine independent native-range horizontal sliders**, reflowed into two source-value columns when at least 700 px wide (one column otherwise), while the five timed-effect editors use up to three columns at 850 px. It retains the **read-only instant-stamina/radiation previews** and the **real-seconds duration preview**. Every value remains selectable independently; the two-line compact cards prevent label/value collisions and do not shrink text or hit areas. Actual CFG targets and original slider steps remain unchanged.

**Immediate-effect preview:** current authored stamina refill shown separately for Water (vanilla 38%), Energy Drink (50%) and Limited Energy Drink (100%). The `survival.instantStaminaRestore` multiplier scales all three existing effects together (0–2x, step 0.05) without changing their proportions or effect durations. The adjacent alcohol preview displays Beer (10 radiation points removed) and ordinary Vodka (50 radiation points removed), controlled together by `survival.alcoholRadiationRemoval` (0–2x, step 0.05). Magic Vodka remains separate.

**Sustained stamina strength:** `survival.waterStaminaEffectStrength` scales Water's existing +25% stamina regeneration and −10% action-stamina cost effects together; `survival.energyDrinkStaminaRecoveryStrength` independently scales the ordinary Energy Drink's +200% sustained stamina regeneration. Both range from 0–2x in 0.05x steps. Neither strength control changes a timed effect's duration.

**Duration preview:** a five-row **read-only real-seconds overview (0–270 seconds)** shows the longest current effect for each drink as a gold line and its Base Preset reference as a grey tick. Its endpoints cannot be dragged; each actual duration is edited using a separate source-range slider in the responsive card grid below. Five authored duration families (0–3x, 0.05x step) are:

- **Water** `survival.waterEffectDuration`: 5-second stamina regeneration + 30-second stamina-cost effect.
- **Energy Drink** `survival.energyDrinkEffectDuration`: the original 45 / 15 / 5-second timed stamina-effect set, including the normal/Limited and per-action effects.
- **Vodka** `survival.vodkaEffectDuration`: original 20-second stamina-drain penalty.
- **Beer** `survival.beerEffectDuration`: original 20-second stamina-drain penalty.
- **Magic Vodka** `survival.magicVodkaEffectDuration`: original 90-second psi protection.

Each duration slider keeps the original relative durations **inside its own source family**, with the live scaled seconds visible in the summary below the grid (all five families) and in the preview for the longest effect. The control never invents new effect prototypes or silently combines unrelated modifiers. Gold handles show current multipliers; grey ticks and explicit text display the Base Preset reference, and the slider endpoints use the original catalog limits. Right-click resets the one selected slider; `R` resets all nine source settings.

**Drunkenness and OverDrunkenness are separate parameters and stay outside this board.** Neither alcoholic duration control changes how quickly the player becomes drunk; duration in this panel refers to its specifically authored stamina or psi effect.

### Sleeping System

`AggregateVisualControl / SleepingDashboard`

The redesigned panel separates **sleep access** from **post-sleep recovery** with four full-width horizontal tracks. All settings retain their existing catalog ranges, steps, Base Preset references and CFG target routes:

- **Sleep Becomes Available At** edits `survival.sleepEligibilityThreshold`, the actual `DefaultSleepParams.AllowSleepThreshold` in **sleepiness points** (0–100, 5-point steps, vanilla 50). This is a rule for when the player *may sleep*, not a live meter of the player's current sleepiness.
- **Minimum Sleep** edits `survival.minimumSleepHours`, the actual `DefaultSleepParams.MinSleepHours` in **hours** (1–14, 1-hour steps, vanilla 7).
- **Post-Sleep Health Restore** edits `survival.sleepHealthRestore`, the authored `SleepHealthRestore` effect's **HP amount** (0–600 HP, 5-HP steps, vanilla 300 HP). The amount is not an HP-per-second rate; restored health cannot exceed the player's health cap.
- **Post-Sleep Stamina Effect** edits `survival.sleepStaminaRestore`, the authored `SleepStaminaRestore` **Stamina effect strength** (0–2 raw effect units, displayed as 0–200%, 0.01 raw increments, vanilla +100%). This does not change the player's maximum stamina or stamina regeneration rules.

Each wide row displays the current value and Base Preset value in the appropriate units, with a gold handle and grey reference tick. Right-click an individual slider to reset only that source setting; `R` resets the four-member family. The original two narrow half-width recovery meters have been removed in favor of separate, readable recovery editors. No in-game sleep timing simulation, additional CFG fields or changed writer semantics have been introduced.

### Hunger State Ladder

`AggregateVisualControl / HungerLadder`

This panel now separates the **actual hunger build-up source**, a **read-only 0–100 HungerPoints stage preview**, three **native-range stage-threshold editors** and three **independent stamina-recovery effects**. No overlapping threshold handles remain on the overview: even when two thresholds have the same value, all three remain editable on distinct native-range sliders. When at least 850 px wide the three threshold cards sit in one row; the three recovery cards likewise share one row. At narrower widths, these cards wrap to two or one column.

- **Hunger Build-up**: `survival.hungerRate`, raw `VitalParams.RegenHungerPoints` (0–0.030 points/s, 0.001 step; vanilla 0.015).
- **Low / Medium / Severe Hunger Thresholds**: `survival.hungerLowThreshold`, `survival.hungerMediumThreshold`, `survival.hungerStrongThreshold`. Each uses the true 0–100 HungerPoints range, in whole points; vanilla 17 / 33 / 83. The read-only overview colors the four consecutive regions and shows fixed-position live/base legends, so labels never collide when thresholds converge.
- **Well-Fed Stamina Recovery Bonus**: `survival.wellFedStaminaRegen`, 0–40% bonus (1% step; vanilla +20%). The direct `VeryLowHungerStaminaRegeneration` effect remains a positive value.
- **Medium Hunger Recovery Loss**: `survival.hungerMediumStaminaRegen`, 0–66% loss (1% step; vanilla −33%).
- **Severe Hunger Recovery Loss**: `survival.hungerStrongStaminaRegen`, 0–100% loss (5% step; vanilla −70%).

The Medium and Severe effect values remain stored as **positive magnitudes** while their CFG writer uses the original `negateSet` operation; the UI explicitly displays them as negative stamina-recovery changes. The three recovery effects are independent of the threshold positions. Medium and Severe hunger retain their separate authored **no-sleep** state without creating any new sleep control.

Every slider displays its current value and actual Base Preset reference, with the source min/max, a gold editable handle and a grey Base Preset tick. The existing value-based `LOW <= MEDIUM <= SEVERE` constraint still prioritizes the actively edited threshold and minimally moves neighbors. Right-click resets the targeted slider; `R` resets all seven source parameters. No new CFG mapping, preset field, simulated time-to-starvation result or writer behavior has been introduced.

### Starvation Timeline

`AggregateVisualControl / StarvationTimeline`

- Master Starvation Penalties.
- Starvation Penalty Delay.
- Starvation Damage Interval.
- Starvation Stamina Drain.
- Starvation Damage.
- Starvation Health Recovery Penalty.

The Master-only bundle toggle gates all five numeric settings without discarding their staged values. Two separate, native-range horizontal timing sliders control the penalty delay (0–6s) and repeat interval (1–20s); their grey ticks show the Base Preset. The charge timeline is **read-only** and expands its seconds scale to keep repeated damage ticks visible without overlapping timing hit targets. Three full-width, horizontal effect strips edit extra stamina cost (0–400%), direct HP lost per tick (0–8 HP) and health recovery blocked (0–100%). Each preserves the original CFG target and shows the Base Preset mark. Right-click an active track to reset just that parameter; press R to reset the family.

`DAMAGE PRESSURE` is derived as:

`HP per tick / seconds per tick`

and is displayed only as an explanatory average health-pressure metric.

### Repeated-Hit Protection

`AggregateVisualControl / RepeatedHitBand`

- **Minimum Protection / Weaker End** edits `combat.accumulatedDamageWeightMin`.
- **Maximum Protection / Stronger End** edits `combat.accumulatedDamageWeightMax`.
- **Healed Damage Still Counts** edits `combat.accumulatedDamageIncludesHealedHealth`.

The wide, read-only **Protection Window** previews the two current golden endpoints and the two grey Base Preset endpoints on the same 0.00-1.00 source curve-weight axis. The title and live/Base Preset range now appear on **separate, panel-bounded text lines**, so they cannot collide at the minimum width. It is intentionally not draggable: when MIN and MAX are equal, there are no overlapping drag handles or ambiguous click targets. The two full-width horizontal sliders below it are independent editors, with current values, Base Preset numbers and grey reference ticks.

These source values are hidden protection-strength weights, **not literal damage-reduction percentages**. The catalog's value-based `MIN <= MAX` slider constraint is preserved, with the actively dragged value taking priority. The switch changes only whether healed damage remains in the repeated-hit tracker; it does not change the actual amount of healing.

Right-click on either slider or the switch resets that single source value to Base Preset. `R` resets all three settings. All three CFG mappings and preset schema remain unchanged.

### Radiation Pressure

`AggregateVisualControl / RadiationBalance`

Three independent, **full-width horizontal tracks** replace the old dual half-width meters and inferred relative-bias display:

- **Exposure Accumulation** edits `survival.radiationAccumulation` as the selected difficulty's incoming radiation **multiplier** (0–2.5x; base differs by difficulty).
- **Natural Radiation Clearance** edits `survival.radiationRecovery` as the Player's absolute passive radiation removal (0.00–0.10 points/s; base 0.05 points/s). More clearance means less radiation retained.
- **Health Damage from Radiation** edits `survival.radiationDamage` as a 0–2x scaler on the eleven existing harmful-radiation effect prototypes, without changing their relative strengths or state thresholds.

A compact, read-only **Exposure / Clearance / HP Damage** overview explains the three distinct game systems without pretending that the gain multiplier and the absolute removal rate can be subtracted into a net rate. Each overview column now clips its own caption and explanation before the next column begins. The former `RELATIVE TUNING BIAS` indicator is removed to avoid suggesting such a calculation.

Each editable slider uses its real catalog limits and step, shows current and Base Preset values on a **separate, wider value line below its title**, and keeps a grey reference tick behind the gold handle. The gameplay explanation remains on its own line; the lower editor track is moved slightly to maintain spacing within the existing card height. Right-click resets one slider; `R` resets all three. There are no new parameters or CFG-writer changes.

### Trading Flow

`AggregateVisualControl / TradingDashboard`

The redesigned Trading Flow surface separates four independently authored economy settings:

- **You Sell / Trader Payout** edits `economy.buyCondition` on its real 0–2x axis. This is the difficulty-profile `BuyCondition` used when a trader **buys from the player**, not an exact final payout.
- **You Buy / Trader Shop Price** edits `economy.sellCondition` on its real 0–2x axis. This is the difficulty-profile `SellCondition` used when a trader **sells to the player**, not the full shop-price formula.
- **Minimum Condition Traders Accept** edits `economy.traderMinBuyCondition` on a 0–100% durability gate: gear below the chosen threshold is refused. The source maps to the weapon and armor buy-eligibility gates of 86 audited production trading profiles, including DLC1 when installed. Vanilla is 40%.
- **Item Value at 0% Durability** edits `economy.itemValueFloor` on a 0–20% base-item-value axis. Vanilla is 10%. This changes the value retained by fully broken items independently from whether a trader agrees to buy them.

A compact read-only **You Sell → Trader Buys / Trader Sells → You Buy** overview establishes the direction of each transaction. Its two captions are bounded inside separate halves; even at the minimum width, one side cannot overwrite the other. All four values have their own full-width horizontal editor, with **distinct title, live/Base Preset value and gameplay-explanation rows** rather than overlapping labels at the top. The gold current handle, grey Base Preset tick and labelled source-range endpoints remain independent. The acceptance threshold uses neutral grey for refused condition and gold for accepted condition.

The former `DIFFICULTY PRICING GAP` display is removed. Subtracting two difficulty settings is not the player's actual shop markup; trader-specific modifiers, item condition and other vanilla trade rules affect the result. Right-click an editor to reset that parameter; `R` resets the four-member family. All existing CFG mappings, presets, steps and writer behavior remain unchanged.

### Repair Price Curve

`AggregateVisualControl / RepairCurve`

The panel combines a **read-only reputation curve** with four independently editable full-width horizontal sliders:

- **Hostile / Enemy** edits `economy.repairEnemy` (0–3x, vanilla 2x).
- **Disliked** edits `economy.repairDisaffection` (0–2x, vanilla 1.5x).
- **Neutral** edits `economy.repairNeutral` (0–2x, vanilla 1x).
- **Friendly** edits `economy.repairFriend` (0–2x, vanilla 0.75x).

The chart connects current values in gold and Base Preset values with a grey dashed line. Its title and legend have separate bounded half-width captions, and the 1x reference explanation occupies its own line. It has **no draggable hit targets**: the live handles are on the four wide tracks underneath, whose labels, exact live/Base Preset numbers and 1x-relative surcharge/discount hints have **three distinct clipped text lines**. Native bounds, grey Base Preset ticks and the separate **1x unmodified-price** reference remain visible. The chart uses a common 0–3x axis for accurate visual comparison; each slider retains its own original source range and 0.01 step.

`1x` means no reputation-specific change to the underlying repair price; values above 1x are surcharges and values below 1x are discounts. These are reputation modifiers, **not final repair bills**: base repair price and other underlying repair settings are unchanged.

The centralized value-based slider constraint remains `HOSTILE >= DISLIKED >= NEUTRAL >= FRIENDLY`. It prioritizes the actively edited setting and adjusts neighbors minimally. Right-click on a slider resets only its parameter to Base Preset; `R` resets the full four-member family. The control continues writing the four existing `ReputationRepairCostModifiers` entries without changing the CFG or preset schema.

### Breakable Box Ammo Mixer

`AggregateVisualControl / AmmoDistribution`

The redesigned panel has two **read-only, full-width distribution previews** and four **full-width editable family-weight tracks**. It retains the two real authored breakable-box ammo generators rather than simulating a single merged ammunition pool.

**SNG pool:** `A918D`, `A919D`, `A545D`, `A012D`. The first two entries are controlled together by the 9x18/9x19 slider, so the authored vanilla weights of 2 each produce normalized shares of **50% 9x18/9x19, 25% 5.45, 25% 12 Gauge**.

**NATO pool:** `A045D`, `A919D`, `A556D`, `A012D`. Their authored weights are all 2, so the vanilla normalized shares are **25% .45 ACP, 25% 9x19, 25% 5.56, 25% 12 Gauge**.

Each preview renders a proportional stacked bar and a separate, always-visible legend beneath it. Pool title and original generator-entry IDs have separate lines; every legend caption is clipped within its own family column rather than painting over adjacent labels. The legend includes the **current normalized percentage and vanilla percentage** for every authored candidate family, including families currently at 0%, without trying to squeeze text inside narrow bar segments. Pool order and source entry membership are preserved. **The percentage is a selection-weight share within that pool, not a global box spawn chance and not the number of bullets generated.**

The independent family editors are:

- **9x18 / 9x19 mm:** influences both SNG candidates (`A918D` and `A919D`) and the NATO `A919D` candidate.
- **.45 ACP:** changes NATO `A045D` only; does not add a .45 candidate to SNG.
- **5.45 / 5.56 mm:** controls existing SNG `A545D` and NATO `A556D` entries.
- **12 Gauge:** controls existing `A012D` entries in both pools.

All four tracks use their existing **0x–3x range in 0.25x steps**. At `1x` an authored `Weight = 2` entry remains unchanged; at `0x` that family's existing entries receive no positive selection weight. Each track shows its exact current multiplier, a grey Base Preset marker (`1x` in the unmodified source), its slider bounds and the current SNG/NATO shares. The **family title, current/Base Preset multiplier and per-pool share** now occupy separate, card-bounded rows; the horizontal track is kept below them.

The live SNG guard remains mandatory: at least one of the **9x18/9x19, 5.45/5.56 or 12 Gauge** family sliders must stay above `0x`. The editor corrects an attempted all-zero SNG selection, and the CFG writer retains its own non-empty-pool validation. The previews are not draggable; only the four family tracks create edit hit targets. Right-click resets one track, while `R` resets all four. No extra candidate entries, CFG routes, preset fields or spawn/quantity controls have been introduced.

## Non-aggregate visual controls

Dedicated semantic controls remain appropriate where a parameter family has its own gameplay model rather than belonging in the shared aggregate renderer.

Current specialized surfaces include:

- Bleeding damage curve with real Medium/Severe stage-threshold X handles (Low stays fixed at 0.1).
- Drunkenness threshold control.
- Stamina pressure board for action costs, aiming recovery, sprint lockout and emergency low-SP recovery.
- Medical healing output board.
- Item value layers board.
- Mutant health roster.
- Mutant special-ability pacing board.
- Human squad-cap ladder.
- Mutant encounter-cap matrix.
- Stash ammo tier/stack board.
- World-loot depletion board.
- Mutant part drop probability board.
- Individual anomaly damage table.

The semantic-control audit is complete: no polygon/ring radar renderer remains in `Controls/`. Several C# type names still end in `Radar` only as historical internal names used by existing XAML declarations; they no longer describe the rendered UI or interaction model. `RadarMutedTextBrush` is likewise only a legacy theme-resource key.

## Architecture

`ParameterViewModel.VisualAggregateFamily` maps backing parameter IDs to visual families and exposes exactly one visible anchor per family.

`AggregateVisualControl` receives the full live parameter collection plus the anchor's family name, binds only the required backing parameters and edits those instances directly.

`MainViewModel` assigns aggregate modified-state providers so the visible family is considered modified when any backing member differs from the current Base Preset.

`SliderConstraintCoordinator` subscribes to every numeric parameter change and remains the single authority for ordered slider relationships.

The CFG writer remains the final safety boundary for writer-specific invariants such as destructible-ammo pools requiring at least one positive effective weight.

No aggregate visual control changes the parameter catalog target, preset serialization format or CFG writer contract.

## Approved Human Combat UI-09 revision (2026-10-08)

- Both independent axis handles use one ring-and-dot glyph, matching hover/selection glow.
- Center crosshair is interactive: drag edits Health X and Gun Damage Y simultaneously, each using its native range and step; right-click center resets both to Base. Axis rings remain independently draggable.
- Attack Frequency capped at 2x; Fire Accuracy capped at 1x, with existing minimums and 0.01 steps retained.
- User explicitly approved these changes; other UI components remain locked.

## Approved Human Combat Profile layout (2026-10-08, UI-09)

**Status: LOCKED after implementation; runtime visual acceptance pending.** The user explicitly authorized this layout revision.

- Human Enemies uses a **square 2D plot** (width equals height), never the previous wide, shallow rectangle. The plot measures 180–280 DIPs per side based on the actual aggregate width. Position: 64 DIPs from the left, 91 DIPs from the top (extra left margin for unclipped Y-axis labels).
- The two editable graph axes remain **Enemy Health** (bottom circle / horizontal X) and **Gun Damage** (left square / vertical Y). The central crosshair is an **interactive joint handle**: drag to adjust both Enemy Health (X) and Gun Damage (Y), preserving each source's native bounds and steps; right-click it to reset both. The axis rings are independently draggable. Base indicators and empty plot background are noninteractive.
- The **Attack Frequency** and **Fire Accuracy** source controls appear as **two separate vertical mixer strips immediately to the right of the square graph**, each 128–140 DIPs wide; they use `DrawMixerStrip` with native ranges, steps, current/Base markers and source-specific rationale.
- The aggregate is **465 DIPs tall**. X-axis ticks sit 32 DIPs below the plot; Y-axis tick labels are right-aligned 12 DIPs to its left. Two concise explanation lines sit directly below the square plot, centered on the plot rather than the entire aggregate: `LEFT: DAMAGE · BOTTOM: HEALTH` at max(405, plot bottom + 56), and `DRAG CENTER: BOTH AXES` 18 DIPs lower. The left and bottom controls use the exact same ring-and-dot icon; no shape-specific legend wording is permitted. At the two-section grid breakpoint, a 540-DIP section can fit the 180-DIP square plot and both ≥128-DIP faders without horizontal overflow.
- In Combat, **Human Enemies + Mutants** are an explicitly approved adjacent two-column section pair. The existing global threshold remains 1096 DIPs of editor width; below that, section order is unchanged and they stack.
- Do not alter these sizes, placement, styling, interaction mechanics or pairing without fresh explicit user approval and UI Design Lock update. This arrangement changes **no gameplay semantics or source CFG values**.

### Human Combat hover visibility clarification — 2026-10-08

All three edit handles (bottom Health X, left Gun Damage Y, central joint X/Y) use the same gold ring/dot glyph. Hovering any draws an outer 20-DIP-radius translucent gold halo and a 12-DIP-radius emphasized ring; the central joint has a 36×36 DIP mouse target for more reliable interaction. Dragging the joint changes both native source parameters; right-click resets both with a dual-parameter status message. Empty plot background has no edit action. Attack Frequency remains limited to 2x and Fire Accuracy to 1x in `parameters.json`. This is explicitly approved UI-09 behavior and locked after implementation.

### Human Combat native runtime ceilings — 2026-10-08

Human Attack Frequency (`combat.npcAttackCooldown`) is limited to **2.00x**; Human Fire Accuracy (`combat.npcHipAccuracy`) is limited to **1.00x**. These bounds are enforced by both the embedded `parameters.json` catalog and `ParameterViewModel.Maximum` at runtime. The vertical faders, drag/key steps, normalized preset imports and CFG writer source values obey these runtime ceilings. This changes the frequency of attacks and shot accuracy respectively, **not the weapon's mechanical rate of fire**. No other sliders inherit either cap.

### Human Combat implementation vs. runtime screenshot (2026-10-08)

The intended source design has three identical gold circular ring-and-dot controls (Health bottom, Damage left, combined center), with a plot-centered two-line footer: `HEALTH ← BOTTOM | LEFT → DAMAGE`, then an accented `CENTER HANDLE: DRAG BOTH`. The user's reported screenshot displaying square/circle shapes and `POINT = PREVIEW` is **not** an approved baseline; it represents an older running artifact. A publish receipt `publish/win-x64/BUILD-INFO.txt` records source commit and binary SHA-256. This does not establish visual acceptance until confirmed in Wine.

### Human Combat runtime-verification requirement

The UI guide must reflect the **user-confirmed running UI**, not substitute source inspection for visible acceptance. A reported runtime showing 4x Attack Frequency, 2x Fire Accuracy, square Y glyph and `POINT = PREVIEW` is an **unaccepted stale UI**. `AggregateVisualControl.DrawHumanCombatProfile` explicitly asserts the 2x and 1x runtime maxima. To prove the actual EXE matches source, close all old TrueCustomDifficulty windows and invoke `./run-latest.sh`, which checks the publish receipt and starts the verified executable. Visual acceptance remains pending.


### Human Combat hover and labels (2026-10-08)

The two right-hand Human Combat strips have no surrounding rectangular frame in default, selected, or hovered states. Hover only adds a soft gold halo around the active fader handle; the rail, current gold value, grey Base marker, and actual parameter hit targets remain intact. Their headings are **ATTACK FREQUENCY** (NPC attack tempo, maximum 2x) and **SHOT ACCURACY** (human shot accuracy, maximum 1x). Below the plot, read **BOTTOM: ENEMY HEALTH | LEFT: GUN DAMAGE**, followed by **DRAG CENTER: BOTH VALUES**. The shared circular handle glyphs and their hover glow are preserved. Other mixer families retain their borders and labels.


### Human Combat — selection overlay and multiplier typography (2026-10-08)

No yellow rectangular hit-target/selection outline may be drawn around Human Combat's glowing handles or their two fader channels. Selection is communicated by the ring and halo only; hit testing and dragging remain active. The mixer-family current multiplier is set in 16.2 DIP type and scale endpoint multipliers in 11.3 DIP type. Mixer strips no longer print an explicit BASE multiplier caption; the grey reference tick remains available and clickable. Other aggregate labels omit redundant `BASE 1x` text but keep non-unit Base values. These are presentation-only changes and do not normalize the actual chosen difficulty reference to 1x.


### Human Combat axis labels (2026-10-08)

The Human Combat plot identifies its X axis as **ENEMY HEALTH →** centered immediately above the square plot, with the current multiplier beneath that title. The Y axis is identified by a rotated **GUN DAMAGE ↑** caption and its current multiplier in the left margin, physically attached to the vertical axis. Do not put these labels into independent left/right top panel cells. The panel no longer uses the “enemy toughness and gun damage, plus attack tempo and shot accuracy” subtitle. Preserve native parameters and faders.

### Human Combat axis headings — 2026-10-08

Keep ENEMY HEALTH and the current multiplier centered above the chart. The GUN DAMAGE caption alone is rotated vertically in the left outer margin; its current numeric multiplier is shown above the top-left Y-axis scale, separated from the rotated caption. Never use Unicode arrow glyphs in these labels because they can render as missing-glyph boxes in Wine. The interactive chart geometry is unchanged.

### Human Combat inline axis values (2026-10-08)

Draw the selected multiplier beside its axis title rather than on a different row: `ENEMY HEALTH` and its value share one horizontal line centered above the square; `GUN DAMAGE` and its value share one line that is rotated together along the left side. Use a small fixed gap, maintain title and value colors, and preserve the native axes and edit interactions.

### Human Combat grey fader endpoint typography (2026-10-08)

The Attack Frequency and Shot Accuracy grey minimum/maximum multiplier captions are 13.0 DIP. This applies only to Human Combat's frameless vertical mixer strips; other control families retain their established font sizes.

### Human Combat terminology — 2026-10-08

The vertical Human Combat axis label is **ENEMY GUN DAMAGE** (not GUN DAMAGE). Its multiplier remains inline with the rotated label; the footer names the left handle ENEMY GUN DAMAGE.

### Your Weapons + Human Combat layout (2026-10-08)

At widths supporting the existing 540-DIP-per-section responsive pairing, **Human Enemies** is directly left of **Your Weapons**. When narrower, they stack in that order. Your Weapons consists of four independent vertical source faders, displayed in responsive rows: WEAPON DAMAGE (`combat.playerWeaponDamage`), HEADSHOT DAMAGE (`combat.playerHeadshotMultiplier`), WEAPON JAMMING (`combat.weaponJamming`), EXPLOSION DAMAGE (`combat.explosionDamage`). The former explosion damage graph and redundant separate horizontal slider are retired from the active UI, not from catalog/CFG writing. Base reference ticks, numeric ranges, source steps and individual rationale remain available. The Mutants section follows the paired controls. This redesign supersedes the previous Human Enemies+Mutants and Your Weapons+Explosives pairing descriptions.

### Responsive combat section placement (2026-10-08)

The Human Enemies and Your Weapons sections form one 2x1 responsive row when usable grid width permits two 540-DIP-wide sections plus the 16-DIP gap (at least 1096 DIP). At smaller widths they are two stacked 1x1 sections. WPF section pairing must identify neighboring `GroupItem` entries using their `CollectionViewGroup` content, rather than relying solely on inherited DataContext.

### Fixed Combat columns (2026-10-08)

Human Enemies occupies the left column and Your Weapons the right column of a permanently paired 2x1 row, including when the viewport is narrower than the combined minimum. The section content maintains a minimum of 1096 DIP and offers horizontal scrolling when necessary; it does not reflow this particular pair into 1x1. Existing unrelated section pairings remain responsive.

### Combat fixed layout slots (2026-10-08)

Combat section row 1 is always **Human Enemies | Your Weapons**, two equal columns separated by 16 DIP. This relationship is defined by fixed first/second Combat catalog group positions, not automatic pairing or WPF group-name extraction. The Combat section panel reserves at least 1096 DIP, preserving each section's usable width; horizontal scrolling handles smaller viewports. The other Combat sections follow underneath, and other tabs do not inherit this fixed minimum.

### Combat group identity resolution (2026-10-08)

The actual WPF group enumeration may put Mutants between Human Enemies and Your Weapons. Read the group name from each GroupItem's bound Tag; for the Combat layout, arrange the Human Enemies and Your Weapons group elements first and together in a fixed 2-column row, with Mutants following. Never pair the first two raw WPF elements merely by index.

### Your Weapons fader alignment — 2026-10-08

The Combat Your Weapons section uses a fixed **2×2 mixer layout** (two columns, two rows), not generic automatic 3-column/4-column packing. The order is Weapon Damage | Headshot Damage on row one and Weapon Jamming | Explosion Damage on row two. All four faders share equal widths, centered within the section, with a consistent row gap and sufficient panel height. They retain their independent actual CFG ranges, steps and markers.

### 2026-10-08 — All sections fixed 1x1 (supersedes two-column designs)

**Explicit user instruction:** Every top-level section must occupy its own full-width row (1x1). No responsive or fixed side-by-side pairing is permitted, including Human Enemies / Your Weapons, Mutants, trader, equipment, loot and other catalog sections. `ResponsiveSectionGridPanel` now measures and arranges one GroupItem per row, preserving catalog visual order. Combat no longer reserves a 1096-DIP minimum, and horizontal scrolling is disabled again. The four faders **inside** Your Weapons retain their internal 2x2 arrangement; this change concerns top-level sections only. Earlier 2x1 instructions are superseded. No CFG or parameter mappings change. Wine acceptance pending.

### Combat visual update — 2026-10-08

HUMAN COMBAT PROFILE combines the health/damage X/Y graph and three frameless right-hand vertical faders: Attack Frequency, Shot Accuracy, and Explosion Damage. Explosion Damage is the original difficulty blast multiplier, **not** radius and not ordinary gun hit damage. YOUR WEAPONS now has three source-exact faders, excluding Explosion Damage. MUTANT HEALTH displays all eight mutant health multipliers as identical gold-ring vertical faders arranged across four columns and two rows, with grey clickable Base indicators, source-native min/max, 0.05 steps and derived HP below each name. No rectangular hover highlight. Preserve full-width, 1x1 top-level sections.

### Single-row faders — Mutant Health and Your Weapons (2026-10-08)

Mutant Health renders all eight equal-width vertical controls in **one row**, with 8-DIP gaps and compact long labels broken into two lines. Its minimum measured width is 760 DIP; it maintains vertical source ranges and ring-only hover feedback. Your Weapons renders its three independent source faders in one centered row (no second row), with the common mixer geometry. Each stays within its full-width top-level section, and the shorter content determines section height.

### Special Ability Cooldowns — single-row faders (2026-10-08)

Eight cooldown multipliers occupy one fixed horizontal row, matching Mutant Health's 8-DIP channel gaps, value typography and glowing circular handles. Each current number represents the cooldown **wait multiplier**; the smaller grey `x RETURN` value is the inverse rate (1 / wait). Drag vertically to edit the original source wait factor, not the displayed inverse rate. Grey reference markers and native bounds remain. No rectangular highlight or secondary horizontal slider.

### Special Ability Cooldowns layout (2026-10-08)

All eight cooldown multipliers now occupy one fixed horizontal row of compact vertical faders, matching Mutant Health. Each gold value and handle controls the original cooldown wait multiplier; the smaller grey `x RETURN` value is its inverse, not a second editable parameter. The grey Base reference tick, native ranges and steps, circular hover glow, individual right-click reset and R reset-all are preserved. Long mutant names wrap onto two lines without rectangular selection boxes.

### Combined Mutant Combat board (2026-10-08)

**GLOBAL COMBAT** is the first tier of the Mutant Combat section, consisting of two larger vertical faders for mutant hit damage and ordinary attack frequency. **SPECIAL ABILITY COOLDOWNS** is the second tier, consisting of eight compact, identical, one-row mutant-specific faders that edit cooldown *wait* values and show inverse `x RETURN`. These are separate CFG paths, not an overall combat pressure formula. Grey reference markers, native slider steps, circular gold hover-only indicators and per-setting reset are maintained. The combined section is one full-width top-level 1x1 block.

### Mutant Combat orthogonal slider directions (2026-10-08)

The top **GLOBAL COMBAT** zone uses two long **horizontal** sliders in separate rows: Damage Per Hit and Attack Frequency. Their values increase left to right; the gold ring glows on hover, a grey Base tick remains visible, and right-click restores the individual native CFG value. The lower SPECIAL ABILITY COOLDOWNS zone remains one horizontal bank of eight **vertical** faders, with wait multipliers increasing upward. Do not mix axis mapping between these zones.

### Corrected Global Combat positioning — 2026-10-08

The two horizontal sliders are **side by side**, not stacked: Damage Per Hit on the left, Attack Frequency on the right, equal-width columns separated by 24 DIP. The eight vertical cooldown faders remain underneath. Only their placement changes; mouse mapping and range semantics stay as before.

### Combat UI density and Psy graph — 2026-10-08

Mutant Combat's two horizontal global tracks share one row directly below the header, with the eight vertical ability cooldowns below after only the section label/subtitle. Keep vertical dead space minimal and preserve independent controls. Repeated Hit Protection is not a normal UI control (parameter state/mappings remain available internally). Psy Phantom Pressure follows Human Combat's square graph geometry, dotted crosshair and three circular handles: X=Cycle Wait, Y=Phantom Count, center=both. Gear Damage is a frameless vertical side fader, while Bleed retains its original boolean toggle. The Base marker represents the selected vanilla preset; this display does not derive spawns/minute.

### Locked Combat section order — 2026-10-08

The four visible Combat groups are ordered **Your Weapons → Human Enemies → Mutant Combat → Mutant Health**, one full-width card per row. The Psy Combat section and Repeated Hit Protection section are completely absent from the Combat view, including their headers; source CFG definitions remain available internally. Your Weapons uses one centered row of three equal-width faders, with two equal gaps rather than the former off-center bank calculation. Combat explanatory text must not mention repeated-hit protection as a visible control.

### Centered card title system (2026-10-08)

Remove duplicated section titles and decorative line separators above cards. Draw the title inside the actual control frame, centered in 12.5-DIP bold accent typography (and optional 9.5-DIP centered muted subtitle), for aggregate families and both dedicated mutant control families. The secondary Special Ability Cooldowns heading is centered and accent colored. Group templates reserve zero vertical height for unused section headers.

### Unified heading system across all tabs (2026-10-08)

Full-width 1x1 section cards and removal of duplicate section separators apply globally. Dedicated visual control titles are centered, bold, gold and 12.5 DIP, with centered 9.5-DIP grey explanatory subtitles, matching the Combat aggregate family. Gameplay ranges, interactions, visuals inside each specialized chart and CFG mapping stay unchanged. The beer/vodka drunkenness editor has no standalone top heading and retains its custom compact internal arrangement until its tab's individual redesign.

### Your Weapons horizontal layout (2026-10-08)

Three compact horizontal tracks occupy one row, left to right: Weapon Damage, Headshot Damage, Weapon Jamming. Each track controls the native parameter by dragging horizontally, with its own Base tick and right-click reset. Equal-width cells separated by 12 DIP, centered card heading, and 169-DIP overall height. Vertical faders are no longer used in Your Weapons.

### Hunger graph interaction and Your Weapons labels (2026-10-08)

Hunger State Ladder: keep the top Hunger Build-up slider and the existing HUNGER POINTS/STAGE PREVIEW. Replace both three-slider arrays with connected three-handle graphs. Stage thresholds use X=the native hunger points; their three levels are visual categories only, not extra config variables. Recovery handles use Y with an actual signed recovery axis: Well-Fed positive bonus versus Medium/Severe negative loss. Click or drag only the rings to edit one real parameter at a time; preserve source ordering constraints, native snapping, reference ghosts and reset behavior. The three Your Weapons horizontal sliders show live value only, not redundant `BASE 1` caption; reference ticks remain.

### Unified Hunger Effect Curve (2026-10-08)

The Hunger State Ladder has one interactive 2D graph beneath the existing Hunger Points preview. X maps thresholds in hunger points; Y maps signed stamina recovery (+ bonus, - penalty). LOW edits X only; MEDIUM and SEVERE are true 2D controls, each bound to its own native threshold and recovery-loss CFG values; WELL-FED is a separate Y-only bonus ring at zero hunger. Current connected effect curve in gold, Base in dashed grey. Right-click a MEDIUM/SEVERE ring resets both underlying values, with native snapping and existing low <= medium <= severe constraints. The previous separate Threshold and Recovery plots are superseded.

### Gamer-style hunger stage graph (2026-10-08)

Survival's Hunger State Ladder retains build-up control and threshold preview, with an updated **HUNGER SURVIVAL PROFILE**. Inside the profile the WELL-FED bonus has its own dedicated vertical channel, visually separate from the thresholds. A stepped, not diagonal, curve depicts discrete LOW, MEDIUM and SEVERE effects. LOW moves horizontally only; MEDIUM/SEVERE gold rings move in two dimensions (hunger onset X / negative stamina regeneration effect Y), with grey baseline markers and dashed baseline steps. Persistent state captions are in separate slots beneath the graph, not floating above handles. Intuitive instructions use WHEN and HOW MUCH. Keep original vanilla CFG semantics and threshold ordering.

### Interaction polish (2026-10-08)

The reference is Human Combat: no rectangular bounding-box selection overlays and no gold transient reset confirmation text. Gold ring handles and hover glow remain; underlying source reset behavior is unchanged. Shared mixer faders no longer draw selection card frames. Dedicated radar control reset messages are never rendered. Hunger Survival Profile removes the word BASE from its live value/preview/footer; grey reference ticks and dashed reference step profile stay visible.

### Hunger visual axis labeling (2026-10-08)

Show WELL-FED / STAMINA RECOVERY / BONUS directly above its vertical positive bonus rail, with UP = MORE RECOVERY below. Main chart captions: horizontal HUNGER POINTS (0–100) and vertical STAMINA RECOVERY LOSS in the dedicated left gutter. Avoid arrows that can render as missing glyphs in Wine; keep axis tick labels apart from the vertical caption. Native parameters and handle interactions remain untouched.

### Hunger axis tick typography (2026-10-08)

The Hunger Survival Profile uses 10-DIP semibold X/Y tick labels, emphasizes the 0/100 endpoints, and labels the Y-axis neutral state `0% NORMAL`. Keep accurate values on the axes (do not replace 100 with MAX). Remove the two redundant bottom instructional captions. Preserve handle behavior, stage markers, axes and game configuration mapping.

### Hunger Survival Profile — player-facing axis and stage captions (2026-10-08)

The Well-Fed bonus fader shows its live `+N% RECOVERY` value without the redundant `UP = MORE RECOVERY` explanation. Keep adequate physical separation between the vertically rotated `STAMINA RECOVERY LOSS` axis title and its numeric percent ticks; the profile plot starts 20 DIPs farther right, with its title 15 DIPs farther left relative to the plot. The vertical zero tick is simply `0%` rather than `0% NORMAL`. Stage summaries distinguish the onset point from the actual recovery reduction on two lines: `LOW AT N PTS` (no invented LOW penalty), `MEDIUM AT N PTS` / `-N% RECOVERY`, and `SEVERE AT N PTS` / `-N% RECOVERY`. This is labeling and spacing only: the native X/Y edit mapping, baseline ghosts, snapped source values and reset actions stay unchanged.

### Hunger stage footer — single-line readability update (2026-10-08)

The interactive Hunger Survival Profile retains its plot and Well-Fed bonus fader. Place the three stage readouts in separate left-aligned, full-width, single-line rows, at offsets +266, +286, and +306 DIPs below the 334-DIP frame top. All three rows are below X-axis ticks (+243 DIPs); none follows its handle's X coordinate. LOW displays `LOW AT N PTS`; MEDIUM and SEVERE explicitly display `MEDIUM AT N PTS | -N% STAMINA RECOVERY` and `SEVERE AT N PTS | -N% STAMINA RECOVERY`. The frame and measured aggregate height gain 40 DIPs. Preserve native parameter editing and other locked presentation unchanged.

### Hunger State Ladder — top-row Well-Fed and aligned full-width plot (2026-10-08)

HUNGER BUILD-UP (`survival.hungerRate`) and WELL-FED STAMINA RECOVERY (`survival.wellFedStaminaRegen`) are independent horizontal `DrawCompactTrack` editors in a shared two-column top row, each preserving its source range, Base tick, and drag/reset mapping. Well-Fed has no vertical slider within the graph. The HUNGER POINTS / STAGE PREVIEW remains an unchanged full-width read-only overview. HUNGER SURVIVAL PROFILE occupies the same outer panel width, with only a left gutter for recovery-loss ticks/title; it retains discrete gold steps and 2D MEDIUM/SEVERE drag rings. Its 294-DIP frame has a one-line stage footer at +259 DIPs, split into three independently clipped, evenly allocated horizontal zones: LOW left, MEDIUM center, SEVERE right; each caption updates with actual current threshold, and MEDIUM/SEVERE captions name the associated `-N% STAMINA RECOVERY`. No back-end or other UI contracts changed.

### Hunger preview/profile X-axis alignment (2026-10-08)

The preview and profile share identical 0–100 hunger-point X coordinates, not merely the same outer frame width. Preview axis starts at `preview.Left + 81` and spans `preview.Width - 111`; the lower profile derives its plot from that rectangle (`plot.Left = axis.Left - 2`, `plot.Width = axis.Width + 12`; internal profile `area` restores the same `axis.Left`/`axis.Width`). This deliberately reserves the Y-axis caption/tick gutter without shifting only the lower graph. All threshold markers, step transitions and ghost references therefore align vertically with the preview's threshold markers at any supported panel width. No gameplay or other controls are changed.

### Hunger header and Y-axis caption correction (2026-10-08)

The HUNGER STATE LADDER header has no explanatory subtitle. The rotated STAMINA RECOVERY LOSS text is aligned to the exact middle of the interior charting area, not the surrounding plot padding; its horizontal location is unchanged, preserving the gap to percentage tick labels. Preview/profile X-axis alignment, existing layout and native editing semantics remain intact.

### Hunger stage labels — consistent AT wording (2026-10-08)

All three stage footer captions include `AT` before the threshold value. LOW: `LOW AT N PTS`; MEDIUM: `MEDIUM AT N PTS | -N% STAMINA RECOVERY`; SEVERE: `SEVERE AT N PTS | -N% STAMINA RECOVERY`. This wording update does not affect the fixed three-zone layout or any interactions.

### Locked graph design archetype: Hunger Survival Profile (2026-10-08)

The confirmed Hunger graph is the permanent design reference for similar interactive stage-effect profiles. Its stable pattern is two native horizontal control tracks above a full-width read-only stage preview, exactly X-aligned with a full-width stepped graph; independent interactive gold rings, grey Base ghosts, clearly titled native X/Y axes with physically separate Y text/ticks, and LOW/MEDIUM/SEVERE one-line three-column dynamic captions. Preserve actual CFG values, snap, ordering, player-facing labels, reset behavior, and discrete authored effects. Different gameplay families must only reuse the visual grammar; their gameplay parameters/step heights must be audited separately.

### Fatigue profile: two native combat penalty plots (2026-10-08)

The FatigueCurve uses ten original parameters: `survival.sleepinessRate` build-up and `survival.energyDrinkFatigueRelief` as two top-row horizontal source tracks; `survival.sleepLowThreshold`, `survival.sleepMediumThreshold`, `survival.sleepStrongThreshold` for X thresholds (0–100 sleepiness points); `survival.fatigueLowAimPenalty`, `survival.fatigueMediumAimPenalty`, `survival.fatigueStrongAimPenalty` for native increased aim time, and `survival.fatigueMediumReloadPenalty`, `survival.fatigueStrongReloadPenalty` for native increased reload time. No LOW reload effect exists in CFG. The stage preview is read-only and shows gold current/grey base marker positions. The two curves render **discrete stairs** (not interpolations) on the shared sleepiness X axis, with actual source-family maxima setting each Y scale. Gold ring joints edit their own source threshold and one corresponding penalty; the two curves share thresholds. Right-click a joint resets those two values, R resets the entire visual family; low reload marker is threshold-only. The graphs reserve Y-title clearance, show real percentage labels and clipped one-line player-facing LOW/MEDIUM/SEVERE captions. These are distinct gameplay axes, not a combined/average fatigue score. Pending local Wine verification.

### Unified Fatigue Combat Profile — independent Y axes (2026-10-08)

The former two separate penalty graphs are superseded by a **single plot** with a shared 0–100 fatigue-point X axis and two separate **native independent Y scales**: gold AIM TIME on the left (0 to the maximum of the three original aim penalties) and white RELOAD TIME on the right (0 to the maximum of the two existing reload penalties). Both appear as discrete step curves, with matching Base ghosts. At LOW, only the gold aim penalty is authored; white reload stays zero until MEDIUM. Gold LOW/MEDIUM/SEVERE and white MEDIUM/SEVERE rings each edit the existing threshold X and corresponding native effect Y; the white and gold endpoints never represent a mathematically combined fatigue penalty. Pointer kind distinguishes two joint families even within the same plot, replacing fragile Y-position detection. Right-click each joint resets its own threshold and penalty, R resets the complete family. Top build-up and energy-drink tracks plus full-width stage preview remain as the approved Hunger-derived design. Graph height and control measurement have been shortened for the single panel. Pending Wine visual review.

### Fatigue combined graph — reload-only domain + correct dragging (2026-10-08)

The combined chart keeps the gold full-stage AIM curve. White RELOAD has no LOW effect: its visible step curve starts at MEDIUM's current native threshold and its grey ghost at Base MEDIUM. White percent tick marks use their own shortened reload vertical mapping (18 DIPs below the aim chart's top, 12 DIPs above its bottom), and a small right-inner secondary rail rather than a duplicate full-height Y caption. Its 0% is bottom and highest native reload slowdown is top. A reload joint maps pointer Y from its own reloadArea; both types of fatigue joint use `effect = ratioFromBottom * nativeFamilyMax` for intuitive upward-increases-penalty semantics. 34×34 hit rectangles and nearest-center arbitration resolve overlapping white/gold handles. The ring kind continues to distinguish aim versus reload effects and preserve targeted reset semantics. The Hunger design lock and all other source parameter mappings stay intact.

### Fatigue white reload scope: independent penalty controls (2026-10-08)

In the unified Fatigue graph, the gold aim handles own all LOW/MEDIUM/SEVERE shared fatigue thresholds (X) and aim slow-down values (Y). White reload handles are **Y-only** native penalty controls: MEDIUM maps to `survival.fatigueMediumReloadPenalty`, SEVERE maps to `survival.fatigueStrongReloadPenalty`; horizontal drag must not edit `survival.sleep*Threshold`, and selected-key adjustments also target the reload effect. White right-click resets only its own effect. The white step preview (and Base dashed ghost) is drawn **only from its MEDIUM threshold to its SEVERE threshold**, starting at MEDIUM's actual reload penalty and ending at the SEVERE step, without a fabricated 0–MEDIUM base segment or any line beyond SEVERE. The right-hand independent reload percentage ticks remain a guide to the white Y values. Gold stage thresholds and all original CFG mappings are preserved.

### Fatigue stage captions and vertical reload title (2026-10-08)

Fatigue's dynamic three-zone row uses `LOW AT N PTS | AIM +N%`, `MEDIUM AT N PTS | AIM +N% | RELOAD +N%`, and `SEVERE AT N PTS | AIM +N% | RELOAD +N%`, with a compact legend explicitly defining positive values as longer/slower aim or reload actions. The white axis title `RELOAD TIME` is rotated vertically at the **outer right margin**, centered against the shorter independent reload Y range. Its ticks, white curve and Y-only handles retain their native independent scale, restricted MEDIUM-to-SEVERE response and white reset behavior. The main plot rectangle, horizontal X values, preview alignment and locked Hunger archetype remain unchanged.

### Fatigue — final approved and locked (2026-10-08)

The user accepted the unified Fatigue Combat Profile: gold AIM stage graph and white independent, vertically titled right-hand RELOAD scale; gold handles control stage X thresholds while white handles change only native reload penalty Y. White response exists only MEDIUM–SEVERE; no unsupported LOW reload effect or additional white segments. Keep preview X alignment, separate native Y ranges, nearest-ring pointer behavior, grey Base references and concise gamer-facing percentages unchanged unless explicitly authorized. This is a design reference, not a required layout for other control families.

### Consumables Effect Board — gamer-facing effects (2026-10-08)

All nine independent source sliders survive. Four 0–2x strength tracks appear first: shared instant stamina refill (Water 38%, Energy 50%, Limited Energy 100% at 1x), Beer/Vodka radiation removed (10/50 points at 1x), Water sustained regen +25% and stamina action cost -10% at 1x, Energy sustained regen +200% at 1x. Three responsive read-only cards immediately explain the current resulting effects; these do not introduce extra parameter coupling or edit targets. A real-seconds timeline previews each drink's longest authored timed effect, with native grey Base markers. Five 0–3x duration tracks then edit Water's 5/30s regen/cost effects, Energy's 45/15/5s effect set, Vodka and Beer's 20s stamina penalties, and Magic Vodka's 90s psi protection separately. The panel explicitly distinguishes duration from strength and drunkenness, which is controlled elsewhere. Width-driven layout measurement now includes both strength rows, three preview cards and five duration controls. Keep native catalog snapping, Base resets, preset serialization and CFG mappings unchanged. Wine visual approval pending.

### 2026-10-08 — Consumables preview compile correction

Removed the duplicate local `previewTop` declaration in `DrawConsumableMixer` (CS0128). The seconds timeline now uses the single preview position derived from its redesigned title (`previewTitle + 23`), consistent with `CompactConsumableHeight`. No parameter, interaction, or approved UI layout was intentionally changed. Local publish/Wine verification pending.

### Consumables vertical compact fader layout (2026-10-08)

The nine independent editable ConsumableMixer tracks use a reusable compact 179-DIP vertical fader: two-line effect title, live native multiplier, gold vertical rail and knob, top max / bottom min labels, and side grey Base marker. Four strength faders precede the read-only instant/sustained/radiation statistics; five duration faders follow the retained read-only five-row real-seconds preview. Layout uses 3/4/5 columns by available width with measured height matched to the number of rows. Original source IDs, value bounds, ticks, reset semantics and all displayed effects remain. No Hunger/Fatigue modifications. Wine acceptance pending.

### Consumables approved revision: horizontal strengths, vertical duration faders (2026-10-08)

Supersedes the previous all-nine-faders arrangement. Four native 0–2x strength editors are compact horizontal source tracks; the five independent 0–3x Water/Energy/Vodka/Beer/Magic-Vodka duration editors remain compact vertical faders. The three effect-statistics cards and longest-effect seconds preview stay intact. Numeric value highlighting is conditional on `IsValueModifiedFromReference()` relative to the selected vanilla Base Preset: changed consumables values are yellow, unchanged values normal text; the color signal extends to effect readouts and current seconds. Remove the three bottom prose/explanation lines; explanation belongs to the existing rationale pane. Board height follows actual responsive rows and ends just below the five faders. No changes to CFG bindings, range/snapping, or other locked panels.

### 2026-10-08 — Precise highlighting inside Consumables readouts

User correction: in the three live Consumables statistics cards, highlight **only individual numerical values** that differ from their corresponding selected Base/Vanilla source values. Never color the whole statistics line: drink/effect names, separators, percentage signs, RAD units and unchanged numbers keep ordinary foreground color. For a mixed-effect line, Water's source modification and Energy's source modification are evaluated separately; one must not highlight the other's unaffected number. Upper horizontal strength sliders, lower vertical duration faders, effect values, and rationale remain otherwise unchanged. Local Wine QA pending.

### 2026-10-08 — Consumables warning cleanup

After the upper four controls returned to horizontal sliders, the local `FaderEnd` calculation in `DrawConsumableMixer` became unused (CS8321). Removed only that dead local helper. Duration faders, layout and all source/editor behavior remain unchanged. The user's previous publish succeeded with this warning; a fresh post-fix publish remains to be run locally.

### Locked Consumables and gamer-facing Drunk stages (2026-10-08)

Final Consumables design is **locked by user instruction**: top four compact horizontal effect-strength sliders; bottom five compact vertical effect-duration faders; three read-only live gameplay-statistic cards whose only modified **numeric substrings** turn yellow, not their labels/units; retained five-drink seconds timeline; no extra footer prose. The nine native parameter IDs, ranges, reset/CFG behavior are preserved.

DrunkennessControl keeps vanilla stage thresholds and existing beer/vodka strength controls. Visible stages use SOBER (<0.001), TIPSY (LOW 0.001), DRUNK (MEDIUM 30), WASTED (STRONG 80), with progression tiles, threshold reference cards and headline vocabulary updated consistently. This does not change intoxication math, the separate OverDrunkness 89 reference, or any source CFG mechanics.

### Sleep System simplified source dashboard (2026-10-08)

SleepingDashboard now reuses compact horizontal native tracks in two logical groups: access rules (`SLEEP UNLOCKS AT`, required fatigue points; `MINIMUM TIME ASLEEP`, minimum configured hours) and wake-up recovery (`HEALTH RESTORED` in HP, `STAMINA RESTORED` as authored effect percentage). Source identifiers remain `survival.sleepEligibilityThreshold`, `survival.minimumSleepHours`, `survival.sleepHealthRestore`, and `survival.sleepStaminaRestore`. Player-facing value text and range endpoints use meaningful points/hours/HP/% units; no BASE wording, redundant descriptions, or bottom legend. Grey source-reference ticks remain for orientation and right-click/R reset continues through the existing hit-target system. Grid is two columns at widths 700+ and one at narrower widths, with runtime height matching rendered rows. This is a UI-only redesign, with no changes to sleeping CFG effects or other locked boards. Local Wine validation pending.

### Radiation System — compact gameplay controls (2026-10-08)

RadiationBalance now presents three native horizontal sliders without the former 3-column prose overview, descriptive sentence under each value, or bottom control legend. The labels are RADIATION BUILD-UP (source multiplier, 0..native limits), NATURAL RAD CLEARANCE (native radiation points per second), and RADIATION HP DAMAGE (native damage multiplier). The three use the original `survival.radiationAccumulation`, `survival.radiationRecovery`, and `survival.radiationDamage` parameters and their actual catalog ranges and steps. Values show x or PTS/S appropriately; no BASE text, while grey Base reference ticks are retained for visual reference. The controls reflow with CompactStageColumns and measured height uses the same CompactGridEnd layout. No fake aggregate graph and no modification to threshold states or to any other approved board. Local runtime test pending.

### 2026-10-08 — Bleeding damage graph richer, native editing preserved

User-authorized BleedingCurveControl visual revamp: keep the two independent top bleeding-strength and natural-recovery sliders, and the existing native three damage plus two onset threshold parameters. The graph is taller (458-DIP control; plot y=185 height=185), with readable BLEEDING POINTS 0–100 X and HP/TICK Y captions, numeric X grid ticks, three gently shaded low/medium/severe exposure domains derived live from the editable medium/severe thresholds, a stronger gold live damage curve/fill, dashed vanilla reference, dotted live-value guides and a compact three-column stage readout at the bottom showing `STAGE AT N PTS` and `N HP / TICK`. Only damage readout numbers that differ from their source references are accented; current stage names remain unchanged. Stages have independent caption cells to avoid overlap even if thresholds converge. The LOW threshold remains fixed at 0.1; interactive damage drag, threshold sliders, threshold ordering, values and original CFG mappings are untouched. Existing other panels remain design-locked. Pending local build/Wine validation.

### 2026-10-08 — Core Health native controls with mixed visual grammar

User-authorized redesign of CoreHealthDashboard only, to avoid a panel of five similar sliders. The maximum HP parameter remains an interactive health-pool meter with source-exact native range and grey reference marker; recovery delay remains a separate interactive wait meter in real seconds. The three independent recovery sources (`health.baseRegen`, `health.regenerationScalar`, `health.lowHealthRegen`) are now compact **vertical faders** in a 3-column bank, showing current source value, its original min/max, a grey reference tick and a gold vertical handle. Do not combine or sum the three recovery mechanisms or suggest a fabricated HP/s formula. Recovery effects remain separate from passive and low-health rates. All five source IDs, native ranges, snapping, mouse/keyboard/reset routing, and CFG writer semantics remain intact. The gratuitous BASE string on both upper meters and the bottom tutorial line are removed. Panel height is adjusted for the new fader bank. Other approved panels, including locked Hunger, Fatigue and Consumables, remain unchanged. Local build/Wine inspection pending.

### 2026-10-08 — Core Health recovery channels horizontal

Explicit user correction supersedes the previous vertical recovery fader bank. In CoreHealthDashboard, all three independent recovery channels (passive healing, healing effects, low-HP recovery) use compact **horizontal** native source tracks in the existing three-column row, with Base text suppressed while reference ticks and native source values/reset behavior remain. Maximum HP and recovery-delay meters are unchanged. Removed vertical fader hit areas and updated panel height to 395 DIP. No other panels are affected. Local build/Wine visual verification pending.

### Medical Healing Output — interactive dosage panel, no preview (2026-10-08)

MedicalHealingRadar (legacy WPF control name) has a 390-DIP interactive direct-edit board. Top: `health.foodHealing` ON/OFF switch. The former standalone Food Restores Health row is collapsed using `IsMedicalHealingMember` and the existing card visibility contract, while `AllParameters` still supplies the real toggle to the integrated board. The medical aggregate-modified provider now includes the food Boolean and subscribes to `CurrentBoolean` changes.

Below: four direct source editor cards (Bandage, Medkit, Army Medkit and Scientific Medkit) in 2x2 layout, each showing current real HP, a relative healing percentage label, and exactly five clickable controls: minus / HALF / NORMAL / MAX / plus. Numeric HP changes accent yellow, static labels do not. The clicked actions change actual source values with native snapping, native range, and current-difficulty reference: HALF=0.5 reference, NORMAL=reference, MAX=native catalog maximum; plus/minus=one native tick. Keyboard arrow control and mouse right-click reset remain available. No separate read-only medical strength visualization, HP comparison bars, outcome simulation or preview is rendered. The food switch only changes the original food-healing Boolean; satiety and Weird Kettle are unaffected, and Scientific Medkit remains outside the separate Master medical penalty subset. Preserve the CFG writer, presets, other boards and separate Master parameters.

### 2026-10-08 — MedicalHealingRadar CS0136 compile correction

Resolved the C# CS0136 name-shadowing error in `OnKeyDown` by renaming the R-reset foreach variable from `p` to `medicalItem`, preserving all reset behavior and the separately declared selected parameter `p`. No UI, source mapping or editor behavior change. Fresh publish/Wine check pending.

### 2026-10-08 — Medical Healing UI correction: native horizontal sliders and tiny Food switch

The user rejected the five-step/button strip (-, HALF, NORMAL, MAX, +) and oversized Food toggle. That layout is **superseded**. Each of the four medical HP cards now has just its item label, live HP amount, one slim **horizontal** native-value draggable slider with a grey difficulty reference tick, and native endpoint labels; no step button presets, no second preview/comparison bar. Click and drag maps the real source parameter min/max with native snapping; mouse right-click resets one medicine, keyboard arrows change its native tick, and R resets the board. Modified HP values alone are accented. The Food Restores Health switch is now a small 52×23 DIP ON/OFF pill on the right of a 39-DIP header row; only the pill is clickable, not the entire large header. Food keeps its real CFG-backed Boolean, unchanged semantics and right-click reset. Overall MedicalHealingRadar board and XAML minimum height reduced from 390 to 304 DIP. Preserve the four native medicine parameters, presets and CFG writer, and all unrelated locked UI panels. Build/Wine review pending.

### 2026-10-08 — Final requested Medical Healing layout: four horizontal faders

User explicitly chose the simple alternative instead of another combined graph. Replace the 2x2 medical card grid with **four independent full-width horizontal fader lanes**, one each for Bandage, Medkit, Army Medkit and Scientific Medkit. Each lane contains a terse item name, its real HP value (yellow only if modified), a slim gold source-value rail/handle and an understated grey reference tick. No separate preview, graph, large boxed cards, preset buttons, prose, BASE strings or additional slider endpoints. A single compact Food Restores Health ON/OFF switch remains at the top. Preserve the native source parameters, snapping, pointer dragging, keyboard nudging, right-click/R reset, preset state and CFG writer. The horizontal rails span nearly the full board width; drag mapping uses the same geometry as rendering, not the earlier two-column mapping. MedicalHealingRadar measures 305 DIP; its XAML min-height matches. Other locked boards untouched. Local build/Wine verification pending.

### 2026-10-08 — Medical Healing compact 2×2 dosage layout (latest user decision)

Supersedes the earlier full-width four-lane Medical Healing design. Four independent native HP controls now occupy a compact, legible **2×2 grid**: Bandage / Medkit on the first row, Army Medkit / Scientific Medkit on the second. Each small medical cell presents a strong item label, prominent current HP readout (yellow only for modified values), a single native draggable horizontal HP fader, subtle five-part rail graduation for orientation, a grey selected-difficulty reference tick, and minimal actual native min/max markers. No extra comparison/preview bars, no preset buttons, no aggregate graph and no BASE prose. Hover/selection emphasize the directly editable rail, rather than introducing additional controls. Drag mapping is now derived from exactly the hit rectangle created during rendering, preserving accuracy across both columns and widths. A compact Food Restores Health ON/OFF pill remains at the top as the same real boolean. Native HP bounds/ticks, preset/CFG mapping, per-item right-click reset and keyboard editing remain unchanged. Medical control and XAML height set to 297 DIP. Other locked designs remain unchanged. User local build/Wine visual review pending.

### 2026-10-08 — Medical selection highlight and Bleeding Treatment copy refinement

Per explicit user request, Medical Healing's selected/hovered slider **does not acquire a yellow card outline, gold selection halo or thicker gold handle**. The normal active gold rail and gold numeric value only when modified remain; source interaction, keyboard focus and CFG behavior are unchanged. Bleeding Treatment no longer prints any `BASE` / `BASE PRESET` text, eliminates explanatory and reset footer prose, and shortens the live labels to treatment strength %, absolute `PTS`, and `BLEEDING REMOVED PER USE`. The reference markers remain visually present (subtle grey), and the read-only comparison, four native bleeding-removal tracks and actual source values stay intact. Pending local build/Wine review.

### Mutant Health — audited complete roster (2026-10-09)

MutantHealthRadar now renders fourteen individual MaxHP multiplier source tracks in **2 rows × 7 columns**, rather than eight in one row. New channels: Blinddog 63, Boar 300, Flesh 280, Poltergeist 400, Snork 150 and Tushkan 19 vanilla HP; prior Bloodsucker/Mist Bloodsucker, Chimera, Deer, Pseudogiant, Pseudodog, Burer and Bayun and Controller remain. All new family IDs follow `combat.mutantHealth.<name>` and map `scale` onto their audited `GameData/ObjPrototypes.cfg` `MaxHP` fields. Defaults are 1x, catalog min/max 0.25x–3x, tick 0.05x. Reference ticks, selected/draggable rails and original reset stay. Register all fourteen in ViewModel grouping and aggregate-modified tracking, with only the Bloodsucker ID acting as the visible anchor; do not introduce unsupported ability cooldowns or difficulty-specific MaxHP flags. Medical Healing and other existing user-locked boards stay unchanged. Build/Wine validation pending.

### Mutant health alphabetical 2× cap and ability cooldown limits (2026-10-09)

The fourteen Mutant Health faders are alphabetically ordered, seven per row. All catalog slider maxima and the runtime view-model limit are capped to 2× (minimum 0.25×; native 0.05× step). The existing eight specific Ability Cooldown families are already implemented, using real `AgentCooldownMultipliers` targets: Bayun, Bloodsucker, Burer, Chimera, Controller, Deer, Pseudodog, Pseudogiant. The six additional HP-only families have no proven special-ability cooldown entries and remain HP-only; no fabricated cooldown UI/source code. No changes to unrelated control families.

### 2026-10-09 — Source-aware numeric-only highlighting in compound captions

**General design rule:** where gamer-facing labels combine dynamic CFG numbers with explanatory words/units, never color the entire caption yellow simply because one source differs from its reference. Use a reusable `DrawCenteredNumericChanges` text-rendering helper that takes the ordered numeric tokens' individual modified-from-reference flags. Only the digits of each modified numeric token are accented; other digits, `LOW/MEDIUM/SEVERE`, `AT`, `PTS`, `STAMINA RECOVERY`, delimiters, minus and percent symbols remain normal. Do not automatically highlight numbers in static scales, axis limits or labels without a parameter association. Applied now to Hunger State Ladder's three stage-preview readouts and its three bottom Hunger Survival Profile stage captions, with independent change flags for each stage's threshold and recovery loss. Never infer modification from a combined sentence-wide Boolean. Existing Hunger geometry, lock, hit areas and native CFG values are unchanged. This helper is the standard for other affected compound text when updating those renderers, rather than indiscriminately recoloring literals. Pending local build/Wine verification.

### 2026-10-09 — Unified source-aware numeric highlighting across value captions

User requested that **all dynamic CFG-value captions** follow the Hunger rule: label words, units, signs, `%`, `x`, `HP`, `PTS`, separators, `BASE` and unmodified/reference numbers remain in their original neutral text colors. Only the digits of a value that differs from the parameter's selected reference receive accent yellow. Source changes must be checked **per parameter**, never by painting an entire mixed sentence. Static axes, scale limits, section headers and explanatory text do not represent changed source values and are not highlighted. Implemented a reusable formatted-text source-number renderer in `AggregateVisualControl` and applied it to the common compact/horizontal track components, Hunger stage preview/footers, Master medical and penalty readouts, Bleeding Treatment, Player Weapons, Bleeding curve stage captions, and Mutant Health live value/HP readouts. Existing custom Consumables statistic lines already color independent numeric values; no geometry, native CFG values, or locked board layouts were changed. Other specialized renderer captions should follow the same established pattern when touched; they must not be blanket-colored or falsely presented as changed without a source comparison. Local build/Wine verification pending.

### 2026-10-09 — Changed numeric value highlights include adjacent gameplay notation

Clarification from user: **when a dynamic numeric value differs from its selected Vanilla/Base reference, color the whole compact numeric token yellow**, including any immediately attached leading `+`, `-` (hyphen or mathematical minus `−`) and trailing `%`, `x` or `×`. Examples: `+20%`, `-33%`, `2x`, `1.5×`; keep `HP`, `PTS`, `RAD`, `STAMINA RECOVERY`, other words, separators and all unchanged/reference tokens in their normal color. This supersedes the earlier digits-only rule. Updated shared numeric-token regex renderers in AggregateVisualControl (Hunger captions and stage labels, compact/value tracks, Consumables readouts and other mapped usage), BleedingCurveControl and MutantHealthRadar. Compare each value with **its own** reference; no blanket line-wide gold coloring. No CFG source or layout changes. Compile/Wine validation pending.

### 2026-10-09 — Your Weapons 0.25× increments

The three existing Your Weapons source sliders — Base Hit Damage (`combat.playerWeaponDamage`), Headshot Multiplier (`combat.playerHeadshotMultiplier`) and Jam Frequency (`combat.weaponJamming`) — now have a native editor snap/tick of **0.25×** instead of 0.01×. Original min/max, CFG targets, reference markers and reset behavior are unchanged. This is a user-requested tuning precision change, not a layout redesign. Local compilation and Wine verification pending.

### 2026-10-09 — Combat numeric multiplier steps: 0.10x except Your Weapons

User-approved Combat-wide editing precision: all **34 other Combat multiplier sliders/faders** now have 0.10x steps, including NPC and mutant combat, all fourteen mutant-health faders, the eight special ability cooldowns, explosion damage and psy-related multiplier controls. The three **Your Weapons** controls (`combat.playerWeaponDamage`, `combat.playerHeadshotMultiplier`, `combat.weaponJamming`) retain the previously approved 0.25x steps. Booleans/non-numeric controls are not altered. No native source CFG target, authored minimum/maximum, layout, gameplay effect or preset format is changed. Set `stepMode: absolute` so actual editing honors the catalog's 0.10x step rather than a display-specific ratio fallback. Importantly, Combat's 0.10x grid is centered on **1.00x vanilla**, not 0.25x minimum, so it supports precisely 1.00x, 0.90x, 1.10x, etc. Dedicated Mutant Health and Ability Cooldown drag handlers use the same baseline-centered snapping. Out-of-step catalog boundary positions may still be used as bounds; current source values are clamped. Build/Wine QA pending.

### 2026-10-09 — Mutant Health 0.05× step restored

User explicitly reverted the recent Combat-wide 0.10× step **only for all 14 Mutant Health faders**. Their catalog sliders again use **0.05× increments** with original 0.25× minimum and 2× maximum. Native drag and keyboard adjustments read the catalog tick dynamically; the existing 1.00×-centered snap grid supports the full 0.25×–2× range in 0.05× increments. Other Combat multiplier controls retain 0.10×; the three Your Weapons controls retain 0.25×. No changes to layout, CFG mappings, presets, or gameplay values. Local build/Wine verification pending.

### 2026-10-09 — Footer removal and numeric emphasis refinement

User-authorized UI polish: remove the **bottom instructional legend/text** from Your Weapons (`AggregateVisualControl`'s player-weapon family), Mutant Combat (`SpecialAbilityCooldownRadar`) and Mutant Health (`MutantHealthRadar`). Do not remove controls, active value readouts or legends that are not those three panels. For source-aware modified-value highlighting, **accented numeric tokens (including attached + / - / % / x) are also rendered 1.2 DIP larger than the surrounding neutral label**, preserving per-source modified comparison, same label alignment and formatted-text layout. Applied in shared aggregate number-only helpers/track value labels and the Bleeding Curve and Mutant Health source-number renderers. Unmodified numbers and static scale/axis labels keep their original size. No native CFG steps, parameter values, visual control geometry or drag hit boxes change. Local build and Wine visual checks pending.

### 2026-10-09 — Mutant Combat wording cleanup

Remove the descriptive `GLOBAL COMBAT | damage per hit and normal attack frequency` subtitle beneath `MUTANT COMBAT`. Rename the first global source control display label from `Damage Per Hit` to **`Damage From Mutants`**. This changes only presentation text: keep `combat.mutantDamage`, the existing CFG mapping, value, control limits, position and interaction unchanged. The second global `Attack Frequency` control and all special-ability cooldown faders stay as before.

### 2026-10-09 — Your Weapons bottom legend removed; mutant cooldown rate clarified

The live Your Weapons board is `DrawYourWeaponsFaders`, not the older `DrawPlayerWeaponProfile`. Remove its **remaining bottom legend** (`GOLD = CURRENT | GREY TICK = BASE | RIGHT-CLICK TO RESET`) while leaving three horizontal weapon controls and the board heading intact. In `SpecialAbilityCooldownRadar`, replace the ambiguous secondary readout `N× RETURN` with **`N× ATTACK RATE`**, computed with the existing inverse of the authored ability cooldown multiplier (`1 / CurrentNumber`), and describe it as *more frequent special attacks, not regular attacks*. The underlying parameter remains a **cooldown multiplier**, not an independent attack speed or damage value. This changes UI text only; no cooldown inversion, CFG mapping, limits, editor steps, or reset semantics have changed. Keep these exact layouts as the current lock. Compile/Wine visual check pending.


### 2026-10-09 — Mutant Combat label implementation verified

The active `MutantCombatPressure` source control's `FirstLabel` is **DAMAGE FROM MUTANTS**, matching the previously authorized Mutant Combat wording contract. Only the displayed caption changed; `combat.mutantDamage`, slider geometry, ranges, editing, and reset semantics are unchanged.


### 2026-10-09 — Mutant Health subtitle removed

The Mutant Health card displays only its centered **MUTANT HEALTH** title: remove the `live MaxHP + direct multiplier` subtitle directly beneath it. Preserve the 14 editable faders, positions, source parameters, HP readouts, ranges and reset interactions; no gameplay change.


### 2026-10-09 — Mutant Health lower bound 0.05×

All fourteen Mutant Health multiplier controls now range from **0.05× to 2.00×**, with the existing **0.05× step**, 1.00× vanilla reference, and unchanged CFG mappings. The catalog slider minimum, runtime lower label and drag/keyboard bounds must agree; 0.25× is no longer the minimum. Existing values remain unchanged unless outside the new range.


### 2026-10-09 — Mutant Health two-row spacing

The seven-fader top row and seven-fader bottom row have a subtle **12-DIP vertical gap** instead of a 1-DIP gap. This changes only the second row's Y placement; individual fader height, width, order, hit targets, slider range/step, and outer card height stay unchanged.


### 2026-10-09 — Fatigue Combat Profile source-aware numeric emphasis and footer removal

The three stage captions (LOW, MEDIUM, SEVERE) are source-aware composite labels: fatigue-point threshold, aim penalty and (for MEDIUM/SEVERE) reload penalty each show the shared numeric-only gold emphasis and +1.2 DIP numeric size **only if that specific value differs from its selected Base reference**. Attached signs and % symbols follow their number; unchanged numbers, stage names and descriptive words stay neutral. The lower static legend (`GOLD = SLOWER AIM | WHITE = SLOWER RELOAD | HIGHER % = LONGER ACTION`) is removed. The X/Y axis scales, original aim/reload line colors, hit targets, drag geometry, source mappings and effect values are unchanged.


### 2026-10-09 — Bleeding System difficulty baseline markers

The **Bleeding Strength** slider displays labeled grey ticks at its actual vanilla values: **N** at **0.25×**, **S/V** at **1.00×** (shared by Stalker and Veteran), and **M** at **1.50×**. Labels are drawn below the rail instead of the min/max captions to avoid overlaps in the compact card. These correspond to `ParameterViewModel.VanillaMarkers`, not manufactured levels. **Natural Recovery** and all Bleeding Damage Profile stages have shared `All` references, so they do not receive separate per-difficulty labels. Existing active handles, drag hit boxes, controls, values and reset-to-base behavior remain unchanged.


### 2026-10-09 — Stamina Core and Movement Limits unified

The first Stamina card is **STAMINA & MOVEMENT** and combines four independent full-width horizontal sliders: **Stamina Capacity**, **Base Recovery Rate**, **Maximum Vault Height**, **Maximum Carry Weight**. The old separate Movement Limits card is hidden through shared `StaminaCore` aggregate membership and the original `stamina.maxSP` anchor remains first in the Stamina tab. Vault-height landmark labels (WAIST/EYES etc.) keep their exact values and clickable reset-to-landmark behavior. No subtitle or bottom explanatory footer. Numeric current/Base readouts use the common source-aware gold-only modified-number typography, with units and static words neutral. The existing four CFG bindings, reference markers, native bounds, edits, drag/right-click behavior, and preset serialization remain independent and unchanged.


### 2026-10-09 — Vault-height landmarks use body references only

In **Stamina & Movement → Maximum Vault Height**, show only authored body-reference labels (for example **WAIST**, **EYES**, **ABOVE EYES**) beneath the rail. Do **not** append raw Unreal-unit numbers or `uu` to those labels. Landmark positions and clickable snap targets retain the original source values; only the visible label text is simplified. The retired PhysicalLimits renderer follows the same caption rule for consistency.


### 2026-10-09 — Amok DLC1 boss mapping verification gate

Amok is excluded from the current fourteen Mutant Health faders. Do not invent a MaxHP override or add a UI channel until the DLC1 boss prototype, health path and three-phase/weak-point mechanics are verified against original CFG and runtime. See docs/combat/MUTANT-HEALTH-AUDIT.md. Existing 2 × 7 layout and gameplay remain unchanged.


### 2026-10-09 — Medical Healing combined card

Medical Healing uses one 400-DIP board: top row has the editable Bandage Healing horizontal rail left and Food Healing checkbox right; below, an interactive three-column shared 0–200 HP graph compares Medkit, Army Medkit and Scientific Medkit. Each bar is draggable vertically, with a small grey line at that item's actual vanilla reference (70/85/100 HP respectively). Current numeric HP values turn yellow and grow by 1.2 DIP only if changed from the selected Base preset; static HP and names remain neutral. The Bandage readout uses the same changed-numeric-only rule. No bottom explanatory text, duplicated per-medkit sliders or extra subtitle. Five original CFG-backed parameters, references, native discrete 5 HP steps, and reset interactions remain preserved. No game values are modified by this layout change.


### 2026-10-09 — Bleeding Treatment compact shared comparison

The Bleeding Treatment card is now 407 DIP tall. Top: a full-width editable Bandage bleeding-removal slider. Below: one interactive three-column chart for Medkit, Army Medkit, and Scientific Medkit on the same 0–70 bleeding-points display axis. Vanilla grey reference ticks are specific to each source (15, 35 and 25 pts), rather than normalized to 100%; the shared chart shows their real relative bleeding removal. Each column edits its original native CFG parameter with existing 5-point snapping and its own legal bounds. Only numeric values changed from Base turn gold and grow slightly, including associated +/-/%/x when present. No redundant preview, slider duplicates, explanatory footer or fabricated healing/HP units; this card is about bleeding removal, not health restored. Existing bandage and medkit CFG semantics remain unchanged.


### 2026-10-09 — Bandage joins the Bleeding Treatment chart

Bleeding Treatment now uses one interactive four-column chart for Bandage, Medkit, Army Medkit, and Scientific Medkit. A shared 0–200 bleeding-points scale compares actual values; each column has a grey vanilla/Base tick (100, 15, 35, 25 respectively), independently editable underlying CFG value, original legal range, and 5-point snapping. Remove the standalone Bandage slider, explanatory footer and duplicate preview. Numeric values follow changed-only yellow/enlarged styling; all other text stays neutral. Board height: 376 DIP.


### 2026-10-09 — Factory presets: role-first balancing

All seven factory profiles were rebuilt across combat, mutant health/cooldowns, environment, stamina/movement, health/medicine, economy, equipment wear and loot while retaining their 201 existing catalogue entries and source baseline difficulty. Do not infer new UI components or a new in-game difficulty from profile differences. Existing yellow-modified-number rule applies when values differ from the selected baseline. Profile-specific rationales and guardrails are described in `docs/FACTORY-PRESETS.md`.


### 2026-10-09 — Three additional factory role profiles

Add Mercenary, Wanderer and Outcast to the shipped Presets list (ten total). These are preset-data additions only: no new slider, UI card, difficulty mode or input behavior. All numeric changes remain subject to the existing yellow-only-when-modified number rule; baseline references are Veteran, Stalker and Veteran, respectively. See docs/FACTORY-PRESETS.md for authored playstyles.


### 2026-10-09 — Factory preset embedding fix

The three new JSON preset files (Mercenary, Wanderer, Outcast) must be explicitly included in `TrueCustomDifficulty.csproj` as embedded resources with `TrueCustomDifficulty.Presets.<Name>.json` logical names. `PresetDirectoryService.WriteEmbedded()` uses those exact manifest names during first-run factory seeding; a file present in the repository but absent from the embedded manifest causes startup failure. All ten factory JSON resources now have explicit entries. No gameplay parameters or UI layout changed.


### 2026-10-09 — Alphabetical profiles and two new roles

Twelve factory presets are ordered alphabetically, adding full 201-value Field Medic (Stalker Base) and Smuggler (Veteran Base) roles. Both are bundled as explicit .NET embedded resources and seeded on first run only when missing. UI and CFG editing rules, including yellow-only-changed-numbers, are unchanged. See docs/FACTORY-PRESETS.md.


### 2026-10-09 — A-Life factory preset depth

All twelve shipped factory presets now include the four human Director group size values and fourteen species-specific mutant Director group size values (219 entries total). The hidden generic mutant scaler remains excluded. Profiles preserve existing visual group radar controls and yellow-number-on-change behavior; A-Life Director group sizes are not conflated with lair population, encounter cadence or respawn. See docs/FACTORY-PRESETS.md.


### 2026-10-09 — Health tab compact boards

**Core Health** uses a compact two-column, three-row layout (302 DIP): Max Health, Recovery Delay, Passive Healing, Healing Effects and Low-HP Recovery. All five original CFG-backed native-range sliders remain editable, with subdued vanilla tick marks. Remove the old giant full-width HP/recovery-delay lanes, their min/max captions and redundant recovery heading. **Radiation Treatment** uses two side-by-side native-range sliders (Scientific Medkit and Anti-Rad) in a 149-DIP card, with no subtitle/footer. Preserve source values and changed-numeric-only gold highlight, neutral label/unit text, slider drag and resets. No catalog or gameplay values changed.


### 2026-10-09 — Compact Stamina & Movement

Unified Stamina & Movement is a 259-DIP two-column/two-row board: Stamina Capacity and Base Recovery on top, Maximum Vault Height and Maximum Carry Weight below. No repeated Base values in text: native horizontal sliders retain a grey vanilla/Base reference tick and modified numeric tokens alone turn gold and slightly larger. Vault retains compact, clickable body-height landmarks (WAIST / EYES / ABOVE EYES); no numeric UU landmark captions. All four authored catalog vanilla values are identical across difficulty presets (100 stamina, 5 regeneration, 130 vault height, 80 carry capacity), so do not invent or display per-difficulty differences. This is layout-only; native CFG values and sliders' independent reset/edit behavior do not change.


### 2026-10-09 — Vault landmark alignment correction

Within the two-column Stamina & Movement card, WAIST / EYES / ABOVE EYES captions must be positioned by measuring actual glyph width and centering on their respective track marker X positions. Clamp labels inside the card only at its edges (especially ABOVE EYES); hit targets follow the visible caption bounds. Never left-align caption glyphs inside a fixed-width box, as this visually misaligns labels and ticks. Preserve body-part-only captions, slider values and no redundant explanatory text.


### 2026-10-09 — Radiation Treatment binding fix

Scientific Medkit (`health.ecoMedkitAntirad`) and Anti-Rad (`health.antiRadRadiation`) are not individually exposed catalog rows. Both must be included in `MainViewModel`'s always-instantiated composite parameter ID set; otherwise `AllParameters` omits them and Radiation Treatment renders `CFG PARAMETER UNAVAILABLE`. Maintain both live backing VMs even when only one aggregate anchor is visible. The existing side-by-side 149-DIP layout, raw CFG semantics, vanilla ticks, independent interaction and modified-number highlighting remain unchanged.


### 2026-10-09 — Radiation Treatment delayed binding recovery

The compact Radiation Treatment board requires live `health.ecoMedkitAntirad` and `health.antiRadRadiation` VMs. AggregateVisualControl now rebinds on WPF Loaded and resolves a missing family member from the bound `Parameters` collection on demand; it caches and subscribes to the actual source VM only if that ID belongs to the active family. This addresses template/Window-relative binding timing without inventing CFG values or substituting fallback placeholders. Verify Scientific Medkit and Anti-Rad render editable values after fresh publish. If unavailable persists, inspect runtime binding diagnostics; do not claim gameplay verified.


### 2026-10-09 — Stamina Usage wording and clean presentation

Rename the Action Costs radar panel title from **STAMINA PRESSURE** to **STAMINA USAGE**. Keep the six independent movement/melee costs, aiming recovery control, sprint lock, low-SP recovery threshold, restart delay, recovery boost and boost duration. Omit the explanatory panel subtitle, repeated gold/Base legend, 'continuous stamina-regeneration slowdown', 'absolute stamina values' and duplicated percentages beside direct SP costs. Static action labels remain neutral; modified numeric values alone use the gold highlight; vanilla reference ticks stay visible on tracks. Bring rows closer together and size the board at 393 DIP, without changing backing source values, reference baselines or interaction mappings.


### 2026-10-09 — Aim stamina terminology

In Stamina Usage, display **AIM REGEN SLOWDOWN** instead of `AIMING RECOVERY PENALTY`. This names the gameplay effect precisely: aiming slows stamina regeneration, rather than directly spending stamina. Keep the original CFG setting, percent readout, reference tick, and modified-value coloring.


### 2026-10-09 — Cross-panel Stamina Usage visual consistency pass

Adopt Stamina Usage text economy for specialized slider dashboards without replacing meaning-bearing diagrams: short titles, no generic explanatory subtitle, no duplicated Base-vs-current prose where grey source ticks already show the reference, neutral static labels, and gold numeric readouts only when their parameter differs from its Base. Applied to Anomaly Damage, Human Group Size, Item Value Layers, Mutant Part Drops, Mutant Group Size, Stash Ammo, World Loot Depletion and Mutant Special Ability controls; removed redundant generic subtitles in aggregate editors. Preserve native control maps, original interactive charts, hit targets, baseline semantics and CFG values. Further per-panel geometry tuning must be tested in Wine; do not assume every specialized slider can be replaced by an ordinary horizontal track.


### 2026-10-09 — Specialized slider geometry pass (Stamina Usage alignment)

Retain original interactive ranges, native CFG backing IDs, vanilla/base ticks and panel-specific charts. Across Anomaly Damage, Mutant Part Drops, Stash Ammo, Human Group Size, Mutant Group Size, Item Value Layers and World Loot Depletion, use compact horizontal tracks with consistent approximately 10–12 DIP rails, 10 DIP side padding where applicable, neutral static names, and right-aligned current values that turn gold only when modified from Base. Remove duplicated conversion percentages on Anomaly Damage where the source value already labels the rail; show one chance percentage rather than repeating expected rolls in Mutant Drops. Stash Ammo keeps distinct sample-stack values alongside its shortened native slider, with no redundant tier subtitle. Human and Mutant Group Size preserve authored effective-cap references and species/rank breakdowns. World Loot preserves base/DLC1 population estimates and removal semantics. Do not flatten specialized radar/fader interactions into ordinary sliders; further per-panel geometry changes require local Wine visual verification. No source CFG values, preset values, or mappings changed.


### 2026-10-09 — Mutant cooldown fader numeric emphasis

Mutant special-ability cooldown faders retain their original vertical slider geometry and Base ticks. Current cooldown multiplier text stays neutral at Base, becoming gold and +1.2 DIP larger only after modification. Names remain neutral. Preserve the distinct cooldown and derived attack-frequency meaning.


### 2026-10-09 — Shared hover feedback alignment

Interactive hover styling is standardized across the Stamina Usage slider rail/handle controls, shared aggregate horizontal-track cards, aggregate toggle pills and specialized horizontal-row radar panels (anomaly damage, mutant drops, mutant group sizes, world loot, human squad caps, stash ammo and item prices). Hover uses subtle gold/Accent tinted card surface and/or name accent, with consistent stronger interactive handle outline; current numeric value stays gold **only** when modified from the selected Base Preset. Keep hovered styles transient and independent of value changes; keep clickable target geometry, keyboard and right-click reset behavior unchanged. Non-interactive explanations remain neutral. The existing specialized curved plots and vertical faders retain their interaction-specific hover and must not gain rectangular fake buttons. Local Windows/Wine visual testing remains required.


### 2026-10-09 — Hover target-only rule (user-approved correction)

For **all sliders**, hovering must not turn the whole card, track label, helper text or value text gold. Only the interactive slider **handle/control point** receives hover glow/outline. Current numeric text becomes gold only for an actual change from the selected Base Preset, never due solely to hover or selection. Specifically, the three bottom Stamina Usage mini-sliders keep their labels neutral while hovering; Radiation Treatment's two horizontal cards keep neutral panel/labels but their handles visibly respond to hover. Standard aggregate tracks, world-loot, anomaly, stash-ammo, mutant-drop, economy and A-Life slider row cards must not tint their entire surface gold on hover. Standalone toggle/buttons remain buttons and may retain their normal surface-hover feedback. Do not alter CFG mappings, native slider ranges, baseline markers or input hit targets.


### 2026-10-09 — Anomaly hover syntax correction

The Anomaly Damage row fill must use a syntactically valid two-branch conditional (`selected ? accent-tinted : neutral panel`). A prior hover-only cleanup accidentally left a third `:` branch and broke compilation (CS1003, lines 182–183); this has been corrected without changing slider targets, source CFG parameters, reset behavior or the handle-only hover policy.


### 2026-10-09 — Mutant Health grouped horizontal redesign

Mutant Health now uses the approved Stamina Usage presentation: fourteen independent, alphabetically ordered species multipliers in **two columns of seven compact horizontal cards** (58 DIP each, 65 DIP row pitch, 12 DIP column gap) within a 526-DIP board. Each card shows one neutral species label, current multiplier in a compact top-right value, small calculated HP readout and one editable horizontal rail. The source/base tick stays grey; only a modified multiplier turns gold (+1.2 DIP), and hover glow belongs **only to the handle**, not the entire card or its text. Mouse drag mapping is now horizontal, and original source IDs, exact 0.05x lower limits, slider tick snapping anchored at 1.00x, keyboard input, per-species right-click reset, group R reset and source CFG mappings remain intact. No large vertical faders or redundant explanatory text.

### 2026-10-09 — Mutant Health compact row value alignment

User-approved change for the fourteen grouped horizontal Mutant Health cards: display the neutral mutant name on the left, the editable source multiplier centered horizontally in the card's top row, and the derived actual HP right-aligned in that same top row. Compare both dynamic values to the selected Base reference using the source parameter's modification state. When modified, the complete multiplier token (including x) and **only the numeric HP amount** turn accent yellow and increase by 1.2 DIP; the HP unit, mutant label, card background and slider remain neutral. Keep the two-column/seven-row Stamina-style layout, horizontal rail, handle-only hover glow, 0.05x–2.00x range, 0.05x editing step, reference ticks, original CFG mappings and reset behavior. No extra subtitle/footer or UI family redesign.
