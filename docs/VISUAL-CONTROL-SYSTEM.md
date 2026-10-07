# Visual Control System

True Custom Difficulty keeps every gameplay setting as its original parameter in the preset and writer graph, but related parameters may be edited through one aggregate visual surface.

The visual layer must never invent a replacement gameplay value. It only edits the same backing `ParameterViewModel` instances that the ordinary sliders would edit.

## Shared interaction contract

- Left click on an unambiguous track sets the value.
- Left-drag adjusts the selected handle, point or fader.
- Mouse wheel is not consumed by aggregate visual controls.
- Right-click an individual interactive element to restore only that backing parameter to the current Base Preset.
- Press `R` while the visual control has focus to restore the whole visual family to the current Base Preset.
- Existing slider constraints remain authoritative. Visual controls do not maintain a second copy of ordering or cascade rules.
- Vanilla/Base Preset values are drawn as reference ticks, markers or dashed curves where the visualization supports them.
- Presets and CFG writer routes remain unchanged.

## Control types

| Type | Purpose | Current family |
| --- | --- | --- |
| Curve | Ordered values whose shape/progression matters | Bleeding Damage, Fatigue Response, Repair Price by Reputation |
| Mixer / Fader Bank | Parallel comparable values | Bleeding Treatment, Consumable Effect Durations |
| System Dashboard | Different controls that form one gameplay system | Sleeping, Trading |
| Threshold Ladder | Ordered gameplay states and their consequences | Hunger |
| Timeline | Delays, cadence and consequences | Starvation |
| Band / Range | Minimum and maximum boundaries on one scale | Repeated-Hit Protection |
| Balance | Opposing or counteracting system forces | Radiation |
| Distribution / Stacked Bar | Relative composition plus direct weights | Breakable Box Ammo Distribution |
| Radar | Independent axes that form a gameplay profile | Existing radar families |

## Family mapping

### Bleeding System

`BleedingCurveControl`

- Bleeding Effect Strength
- Natural Bleeding Recovery
- Low Bleeding Damage
- Medium Bleeding Damage
- Severe Bleeding Damage

Damage points use the existing `LOW <= MEDIUM <= SEVERE` constraint.

### Fatigue Response

`AggregateVisualControl / FatigueCurve`

Compact tracks:
- Fatigue Build-up Rate
- Energy Drink Fatigue Relief

Curves:
- Low / Medium / Severe Aim-Time Penalty
- Medium / Severe Reload-Time Penalty

The existing fatigue severity constraints remain authoritative.

### Bleeding Treatment Mixer

`AggregateVisualControl / BleedingTreatmentMixer`

- Bandage Bleeding Removal
- Medkit Bleeding Removal
- Army Medkit Bleeding Removal
- Scientific Medkit Bleeding Removal

Each fader keeps its raw removal value while the visual position is directly comparable because each catalog range spans 0x to 2x its vanilla value.

### Consumable Effects Mixer

`AggregateVisualControl / ConsumableMixer`

Compact tracks:
- Instant Stamina Restore
- Alcohol Radiation Removal

Duration faders:
- Water
- Energy Drink
- Vodka
- Beer
- Magic Vodka

### Sleeping Dashboard

`AggregateVisualControl / SleepingDashboard`

- Sleep Becomes Available At
- Minimum Sleep
- Post-Sleep Health Restore
- Post-Sleep Stamina Restore

### Hunger State Ladder

`AggregateVisualControl / HungerLadder`

- Hunger Build-up Rate
- Well-Fed Stamina Recovery Bonus
- Hunger Stamina Recovery Penalty
- Severe Hunger Stamina Recovery Penalty

### Starvation Timeline

`AggregateVisualControl / StarvationTimeline`

- Master Starvation Penalties
- Starvation Penalty Delay
- Starvation Damage Interval
- Starvation Stamina Drain
- Starvation Damage
- Starvation Health Recovery Penalty

The aggregate anchor remains visible while the master rule is off so the rule can be enabled from the same visual system.

### Repeated-Hit Protection Band

`AggregateVisualControl / RepeatedHitBand`

- Repeated-Hit Protection Minimum
- Repeated-Hit Protection Maximum
- Healed Damage Still Counts

The existing `MIN <= MAX` constraint remains authoritative.

### Radiation Balance

`AggregateVisualControl / RadiationBalance`

- Radiation Recovery
- Radiation Accumulation
- Radiation Damage

Recovery and accumulation are visualized as opposing forces but remain independent raw parameters.

### Trading Dashboard

`AggregateVisualControl / TradingDashboard`

- Trader Buy-From-You Pricing
- Trader Sell-To-You Pricing
- Trader Minimum Buy Condition
- Item Value at 0% Durability

### Repair Price Curve

`AggregateVisualControl / RepairCurve`

- Hostile
- Disliked
- Neutral
- Friendly

The existing reputation-price ordering constraint remains authoritative.

### Breakable Box Ammo Distribution

`AggregateVisualControl / AmmoDistribution`

- 9x18 / 9x19 mm
- .45 ACP
- 5.45 / 5.56 mm
- 12 Gauge

The stacked bar is a normalized preview of the current relative weights. The underlying values remain independent weights and do not need to sum to 100%.

## Architecture

`ParameterViewModel.VisualAggregateFamily` maps backing parameter IDs to visual families and exposes exactly one visible anchor per family.

`AggregateVisualControl` receives the full live parameter collection plus the anchor's family name, binds only the required backing parameters and edits those instances directly.

`MainViewModel` assigns an aggregate modified-state provider to each visual anchor so the family is considered modified when any backing value differs from the current Base Preset.

No aggregate visual control changes the parameter catalog target, preset serialization format or CFG writer contract.
