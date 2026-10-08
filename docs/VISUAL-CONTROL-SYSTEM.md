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
