# Visual Control System

True Custom Difficulty keeps every gameplay setting as its original parameter in the preset and CFG-writer graph, while related parameters may be edited through one aggregate visual surface.

The visual layer never invents replacement gameplay values. It edits the same live `ParameterViewModel` instances used by the ordinary parameter system.

## Compact readability contract

Semantic controls follow one density/readability baseline:

- The default application window is 1280×800 and the minimum is 1024×640, keeping the 16:10 layout.
- Large semantic controls are centered and capped at 920 px so maximizing the window does not stretch tracks across the whole screen.
- Custom-rendered text has a 9.0 minimum FontSize. Important values and labels use larger sizes.
- Detailed implementation caveats belong in the rationale/help panel; the control surface itself should show only the value, Base Preset reference and immediate gameplay meaning.
- Large entity families use two columns when that improves scanability instead of growing into very tall single-column dashboards.
- Grey ticks/markers carry Base Preset reference information so repeated micro-labels such as `VANILLA`, `0%` and `200%` do not need to be printed on every row.
- Compactness must never change backing parameter values, constraints, preset serialization or CFG-writer behavior.

## Shared interaction contract

- Left-click or drag a visible handle, point, meter or track to edit its backing value.
- Mouse wheel is not consumed by aggregate visual controls.
- Right-click an individual interactive element to restore only that backing parameter to the current Base Preset.
- Press `R` while a visual control has focus to restore the whole visual family to the current Base Preset.
- Vanilla/Base Preset values are shown with grey ticks, dashed curves, ghost envelopes or labelled reference markers.
- Preset serialization and CFG writer routes remain unchanged.

### WPF template visibility guard

When an aggregate family is anchored on a toggle (currently `advanced.fatigueMechanics`), the template must keep the `ControlRow` visible for its `AggregateVisualControl`. Ordinary toggle styling is therefore conditioned on **both** `IsToggle=True` and `IsVisualAggregateAnchor=False`. This prevents the later toggle trigger from hiding the entire visual board. The board itself contains the bundle toggle; no second ordinary checkbox should appear.

The static regression check is `python tools/verify_visual_anchor_triggers.py`. It catches a future unguarded toggle trigger and verifies that the visible toggle aggregate anchor and its six backing members remain mapped.

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
| Fatigue | Combat-penalty curves across LOW / MEDIUM / SEVERE, plus build-up and Energy Drink relief | Semantic |
| Equipment Wear | Two independent native CFG axes: armor toughness and player-weapon wear per shot, with current/base comparison | Semantic |
| Psy Phantom Pressure | Two independent phantom-cycle timing/count axes, plus gear damage and bleeding toggle | Semantic |
| Human Combat Profile | Native human health and gun-damage axes, plus independent attack-frequency and accuracy tracks | Semantic |
| Your Weapons | Three compact native-value lanes for base hit damage, headshot multiplier and weapon jamming | Semantic |
| Core Health | Actual max-HP rail, configured recovery-delay ruler, passive regen and compatible-effect scaling | Semantic |
| Master Severe Fatigue | On/off bundle plus five independently editable severe-fatigue penalties; inactive tracks preserve stored values | Semantic |
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

The old generic duration fader-bank helper is no longer used.

## Family mapping

### Fatigue Response

`AggregateVisualControl / FatigueCurve`

Independent system controls:

- Fatigue Build-up Rate.
- Energy Drink Fatigue Relief.

Combat response graph:

- Low / Medium / Severe stage thresholds on the real 0–100 SleepinessPoints X axis (vanilla 50 / 60 / 80).
- Low / Medium / Severe Aim-Time Penalty on the Y axis.
- Medium / Severe Reload-Time Penalty on the Y axis.

Threshold handles move horizontally while penalty handles move vertically. There is no separate Low-fatigue reload backing parameter, so the reload curve begins at Medium rather than inventing a synthetic Low point.

### Equipment Wear Balance

`AggregateVisualControl / EquipmentWearBalance`

- Horizontal axis = `equipment.armorDurability` (`EnvironmentDifficulty.Armor_Durability`); higher means tougher armor, but is **not** a proven inverse wear percentage.
- Vertical axis = `equipment.weaponWearPerShot`, scaling each nonzero `DurabilityDamagePerShot` in the existing 41 base + 11 DLC player-weapon routes; higher means faster weapon condition loss.
- Grey dashed lines/ghost point = the selected Base Preset; yellow lines/point = the current values.
- Drag only the bottom axis handle to edit armor; drag only the left axis handle to edit weapon wear. The two-axis summary point itself is not draggable.
- Right-click either handle to reset just that parameter; R resets the pair.
- Both parameters retain their distinct native ranges/steps, per-difficulty baselines, preset serialization and writer routes. The two item-weight sliders remain separate and unchanged.
- The visual does **not** claim a specific armor durability-to-wear formula and does **not** alter weapon jamming.

### Human Combat Profile

`AggregateVisualControl / HumanCombatProfile`

- X axis: `combat.npcHealth` (the selected difficulty's `NPC_HP` multiplier); farther right = tougher human enemies.
- Y axis: `combat.npcWeaponDamage` (the selected difficulty's `NPC_Weapon_BaseDamage` multiplier); higher = stronger firearm hits from human enemies.
- A grey ghost marker is the selected Base Preset; the yellow crosshair is the current pair. The crosshair is display-only; the bottom and left handles adjust one CFG value at a time.
- Below the plot, `combat.npcAttackCooldown` edits player-facing attack **frequency**, preserving the existing writer's reciprocal `NPC_AttackCooldown` conversion. `combat.npcHipAccuracy` edits the native accuracy multiplier.
- Both lower tracks retain their own vanilla reference ticks and their exact native range/step.
- These four independent controls do **not** produce a computed hit chance, attack DPS, or time-to-kill; the engine's complete combat model is not claimed.

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
- Four independent controls preserve their own ranges, steps, presets, and CFG routes. Gold handles are editable, grey ticks are Base Preset references. Right-click restores a single setting; R restores all four. No fabricated healing-rate curve or effective HP-per-second arithmetic is shown.

### Master Severe Fatigue

`AggregateVisualControl / SevereFatiguePenaltyBoard`

- Toggle: `advanced.fatigueMechanics`, the `SleepinessNegativeMechanics` membership of the selected difficulty's `EnvironmentDifficulty.AdditionalMechanicsEffects`. Master defaults ON; the other vanilla difficulties default OFF.
- MAX STAMINA LOSS: `survival.severeFatigueMaxStamina`, a 0–40% loss (vanilla 20%).
- CARRY CAPACITY LOSS: `survival.severeFatigueCarryWeight`, a 0–40% penalty applied to both existing carry-related effects (vanilla 20%).
- RECOIL INCREASE: `survival.severeFatigueRecoil`, a 0–110% increase (vanilla 55%).
- AIM SWAY: `survival.severeFatigueAimSway`, a 0x–0.8x source multiplier (vanilla 0.4x) mapped to both X and Y sway effects.
- SHOT SPREAD: `survival.severeFatigueDispersion`, a 0x–1x source multiplier (vanilla 0.5x).
- When OFF, the five penalty sliders retain their stored values but cannot be edited until the bundle is switched ON. Base Preset references remain visible in grey. The activation point is unchanged and ordinary Low/Medium fatigue mechanics are separate. Right-click restores a single enabled setting; R restores all six source values and the toggle.
- The panel edits these six existing CFG paths exclusively. It does not infer compounded spread/recoil formulas or introduce a new in-game difficulty.

### Bleeding Treatment

`AggregateVisualControl / BleedingTreatmentMixer`

- Bandage Bleeding Removal.
- Medkit Bleeding Removal.
- Army Medkit Bleeding Removal.
- Scientific Medkit Bleeding Removal.

Each item keeps its own raw bleeding-removal range, but the visual comparison is normalized to that item's vanilla value. The shared line at 100% therefore means vanilla strength for every treatment without pretending the raw values are identical.

### Consumable Effect Board

`AggregateVisualControl / ConsumableMixer`

Instant stamina preview:

- Water: 38% authored instant refill.
- Energy Drink: 50%.
- Limited Energy Drink: 100%.

One multiplier scales all three while preserving those authored differences.

Alcohol radiation-cleanup preview:

- Beer: 10 radiation points removed.
- Vodka: 50 radiation points removed.

Timed effects share one seconds axis:

- Water: 5 s stamina regeneration + 30 s stamina-cost effect.
- Energy Drink: 45 s / 15 s / 5 s authored stamina timings.
- Vodka: 20 s stamina-drain penalty.
- Beer: 20 s stamina-drain penalty.
- Magic Vodka: 90 s psi protection.

Each duration control scales its whole authored family while preserving the internal timing ratios. Drunkenness and OverDrunkenness are separate controls.

### Sleeping System

`AggregateVisualControl / SleepingDashboard`

- Sleep eligibility threshold.
- Minimum sleep duration.
- Post-sleep health restore.
- Post-sleep stamina restore.

The threshold is shown as a sleep gate, minimum sleep as a time ruler, and the two restore values as wake-up recovery meters.

### Hunger State Ladder

`AggregateVisualControl / HungerLadder`

- Hunger Build-up Rate.
- Low / Medium / Severe thresholds on the real 0–100 HungerPoints scale (vanilla 17 / 33 / 83).
- Well-Fed Stamina Recovery Bonus.
- Medium Hunger Stamina Recovery Penalty.
- Severe Hunger Stamina Recovery Penalty.

Medium and Severe retain the vanilla no-sleep state. Threshold timing and recovery-penalty magnitude are edited independently.

The three recovery effects share one semantic axis:

`PENALTY <- NEUTRAL -> BONUS`

Well Fed therefore reads to the bonus side while Hungry and Severe extend into the penalty side.

### Starvation Timeline

`AggregateVisualControl / StarvationTimeline`

- Master Starvation Penalties.
- Starvation Penalty Delay.
- Starvation Damage Interval.
- Starvation Stamina Drain.
- Starvation Damage.
- Starvation Health Recovery Penalty.

The timeline begins when severe hunger activates. It shows the grace period, first damage charge and later repeating charges over a real-time preview. The lower meters show the active penalty strengths.

`DAMAGE PRESSURE` is derived as:

`HP per tick / seconds per tick`

and is displayed only as an explanatory average health-pressure metric.

### Repeated-Hit Protection

`AggregateVisualControl / RepeatedHitBand`

- Repeated-Hit Protection Minimum.
- Repeated-Hit Protection Maximum.
- Healed Damage Still Counts.

The current gold envelope and dashed vanilla envelope are drawn on the same 0.00-1.00 curve-weight scale.

These values are hidden protection-strength weights, not literal damage-reduction percentages.

The central slider constraint preserves `MIN <= MAX`.

### Radiation Pressure

`AggregateVisualControl / RadiationBalance`

- Radiation Recovery.
- Radiation Accumulation.
- Radiation Damage.

Radiation Recovery is an absolute passive-removal rate in points/s. Radiation Accumulation is a difficulty scaler on incoming radiation. They therefore remain on separate native scales.

`RELATIVE TUNING BIAS` compares each setting only against its own Base Preset reference. It is not presented as a literal net radiation rate.

Radiation Damage separately scales health damage after the game has already entered harmful radiation states. Vanilla state thresholds are not moved.

### Trading Flow

`AggregateVisualControl / TradingDashboard`

- Trader Buy-From-You Pricing.
- Trader Sell-To-You Pricing.
- Trader Minimum Buy Condition.
- Item Value at 0% Durability.

The first two values are shown by transaction direction:

- `PLAYER -> TRADER`: the difficulty pricing setting used when traders buy from the player.
- `TRADER -> PLAYER`: the setting used when traders sell to the player.

`DIFFICULTY PRICING GAP` is only the difference between those two settings; it is not claimed to be the final shop-price ratio.

The condition gate shows which weapon/armor durability range is refused or accepted. The broken-item value floor is a separate durability-to-value layer.

### Repair Price Curve

`AggregateVisualControl / RepairCurve`

- Hostile.
- Disliked.
- Neutral.
- Friendly.

All four values share one repair-price graph.

`1x` means the unmodified base repair layer:

- above 1x = surcharge,
- below 1x = discount.

Current values are gold; vanilla is dashed.

The central slider constraint preserves:

`HOSTILE >= DISLIKED >= NEUTRAL >= FRIENDLY`

### Breakable Box Ammo Mixer

`AggregateVisualControl / AmmoDistribution`

Family controls:

- 9x18 / 9x19 mm.
- .45 ACP.
- 5.45 / 5.56 mm.
- 12 Gauge.

The control previews the two actual authored destructible-ammo pools separately.

SNG pool:

- A918D.
- A919D.
- A545D.
- A012D.

Because A918D and A919D share the same 9x18/9x19 family control and all four authored entries start at `Weight = 2`, vanilla normalized pool share is:

`50% 9x18/9x19 / 25% 5.45 / 25% 12 Gauge`

NATO pool:

- A045D.
- A919D.
- A556D.
- A012D.

Vanilla normalized pool share is:

`25% / 25% / 25% / 25%`

Pool percentages are normalized relative weight shares inside that pool, not global breakable-box spawn probabilities.

A family scaler of `1x` preserves authored `Weight = 2`; `0x` removes that family's existing entries from weighted selection. No new candidate ammo entries are created.

The SNG pool must keep at least one of 9x18/9x19, 5.45/5.56 or 12 Gauge above 0x. The CFG writer independently validates that every generated ammo pool retains at least one positive effective weight.

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
