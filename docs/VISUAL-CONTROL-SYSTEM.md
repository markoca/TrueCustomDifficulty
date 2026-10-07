# Visual Control System

True Custom Difficulty keeps every gameplay setting as its original parameter in the preset and CFG-writer graph, while related parameters may be edited through one aggregate visual surface.

The visual layer never invents replacement gameplay values. It edits the same live `ParameterViewModel` instances used by the ordinary parameter system.

## Shared interaction contract

- Left-click or drag a visible handle, point, meter or track to edit its backing value.
- Mouse wheel is not consumed by aggregate visual controls.
- Right-click an individual interactive element to restore only that backing parameter to the current Base Preset.
- Press `R` while a visual control has focus to restore the whole visual family to the current Base Preset.
- Vanilla/Base Preset values are shown with grey ticks, dashed curves, ghost envelopes or labelled reference markers.
- Preset serialization and CFG writer routes remain unchanged.

### Constraint authority

Ordered gameplay relationships are owned by the central `SliderConstraintCoordinator`, not by individual visual controls.

The coordinator receives every `CurrentNumber` change, including changes made through aggregate visuals, and applies constraints using actual parameter/CFG values. The actively edited value has priority; neighbouring values move only as far as required.

Current ordered families include:

- Bleeding damage: `LOW <= MEDIUM <= SEVERE`.
- Fatigue aim penalty: `LOW <= MEDIUM <= SEVERE`.
- Fatigue reload penalty: `MEDIUM <= SEVERE`.
- Hunger stamina-recovery penalty: `HUNGRY <= SEVERE` by penalty magnitude.
- Repeated-hit protection: `MIN <= MAX`.
- Repair pricing: `FRIENDLY <= NEUTRAL <= DISLIKED <= HOSTILE`.
- Water stamina drain and melee stamina cost relationships.

Ammo-box pool validity is a separate family invariant rather than an ordering constraint. The UI prevents the SNG pool from reaching all-zero weights, and the CFG writer independently rejects any destructible ammo pool whose effective weights are all zero.

## Aggregate-family audit

| Family | Semantic visualization | Status |
| --- | --- | --- |
| Fatigue | Combat-penalty curves across LOW / MEDIUM / SEVERE, plus build-up and Energy Drink relief | Semantic |
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

- Low / Medium / Severe Aim-Time Penalty.
- Medium / Severe Reload-Time Penalty.

There is no separate Low-fatigue reload backing parameter, so the reload curve begins at Medium rather than inventing a synthetic Low point.

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
- Well-Fed Stamina Recovery Bonus.
- Hunger Stamina Recovery Penalty.
- Severe Hunger Stamina Recovery Penalty.

The three state effects share one semantic axis:

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

Dedicated radar/curve controls remain appropriate where the values are independent axes or use a specialized domain visualization, including:

- Bleeding damage curve.
- Drunkenness.
- Action costs.
- Medical healing output board.
- Item value layers board.
- Mutant health.
- Special ability cooldowns.
- Human/mutant group sizes.
- Stash ammo.
- World-loot removal.
- Anomaly damage.
- Other existing radar families.

## Architecture

`ParameterViewModel.VisualAggregateFamily` maps backing parameter IDs to visual families and exposes exactly one visible anchor per family.

`AggregateVisualControl` receives the full live parameter collection plus the anchor's family name, binds only the required backing parameters and edits those instances directly.

`MainViewModel` assigns aggregate modified-state providers so the visible family is considered modified when any backing member differs from the current Base Preset.

`SliderConstraintCoordinator` subscribes to every numeric parameter change and remains the single authority for ordered slider relationships.

The CFG writer remains the final safety boundary for writer-specific invariants such as destructible-ammo pools requiring at least one positive effective weight.

No aggregate visual control changes the parameter catalog target, preset serialization format or CFG writer contract.
