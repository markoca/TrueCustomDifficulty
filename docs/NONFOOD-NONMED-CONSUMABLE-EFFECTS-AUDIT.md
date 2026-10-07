# Non-Food / Non-Medicine Consumable Effects Audit

## Scope

This audit covers the **utility consumables** requested for True Custom Difficulty while deliberately excluding normal food and medicine.

The scope is semantic, not based only on the engine's `ConsumableType` enum. The engine classifies several utility drinks as `Food`, so using that enum literally would incorrectly throw Water, Beer, Vodka and Energy Drinks out of this audit.

### Included

- Water
- Energy Drink / `Energetic`
- Limited Edition Energy Drink / `Energetic_Limited`
- Beer
- Vodka
- quest/special utility drink variants such as `DvupalovVodka` ("Magic Vodka")

### Excluded by user request

Food:
- Bread
- Fresh Bread
- Sausage
- Canned Food
- Spoiled Canned Food
- Condensed Milk
- DLC dry-ration / meal-family items such as `MonolithDryRation`

Medicine / medicinal drugs:
- Bandage
- Medkit
- Army Medkit
- Scientific/Eco Medkit
- Anti-Rad
- Hercules
- Vinca / `Cinnamon`
- PSI-Block / `PSYBlocker`

Environmental water-contact mechanics are also outside this audit. `WaterMediumStaminaPenalty` and `WaterStrongStaminaPenalty` describe movement through water, not drinking bottled Water.

## Evidence policy

Primary source of truth remains the runtime/game + DLC CFG dump 2.0.5 already used by the project baseline.

Evidence grades used below:

- **2.0.5 CONFIRMED** - value/semantics already captured from the project's runtime-derived 2.0.5 baseline.
- **2.0 CROSS-CHECK** - current 2.0-era mod/config evidence independently exposes the same vanilla SID/value/duration.
- **DISCOVERY** - an older extracted stats/config source exposes the item-to-effect SID wiring, useful for completing the map but not sufficient by itself to overwrite a 2.0.5 value.
- **RUNTIME TEST** - CFG target exists, but the complete runtime interaction is not yet safe enough for normal UI.

Consumable effects must be resolved as:

```text
ItemPrototype
    -> EffectPrototypeSID
        -> ValueMin / ValueMax
        -> Duration / Charges / Delay
        -> EEffectType
        -> any mechanics that accumulate or react to the effect
```

Do not invent one global "Consumable Effectiveness" scalar.

---


# Test implementation

The current runtime-test implementation exposes:

- `survival.instantStaminaRestore`
- `survival.alcoholRadiationRemoval`
- `survival.waterEffectDuration`
- `survival.energyDrinkEffectDuration`
- `survival.vodkaEffectDuration`
- `survival.beerEffectDuration`
- `survival.magicVodkaEffectDuration`
- `survival.beerDrunknessStrength`
- `survival.vodkaDrunknessStrength`

`survival.drunknessThresholdScale` remains in the catalog only as a hidden source/legacy mapping. The player-facing alcohol editor keeps the vanilla 20 / 40 / 60 thresholds fixed.

Alcohol drunkenness duration remains deliberately excluded. Beer/Vodka drunkenness **strength** is now exposed as a direct raw `ValueMin/ValueMax` value in 1-point Drunkness steps and remains marked `runtime_test_required` because those two raw effect rows were cross-checked outside the original 2.0.5 writer-baseline capture.

---

# Selected implementation priorities

The current player-facing priority is intentionally narrow:

1. **Instant Stamina**
2. **Alcohol Radiation Removal**
3. **Effect Duration for every in-scope utility consumable**

## Instant Stamina

Expose direct instant-stamina strength only for consumables that actually have an instant stamina effect:

- Water -> `WaterStaminaInstant`
- Energy Drink -> `EnergeticStaminaInstant3`
- Limited Edition Energy Drink -> `EnergeticLimitedStaminaInstant4`

This control domain changes the immediate stamina refill only. It must not change timed stamina regeneration, fatigue relief, hunger, tolerance or overdose mechanics.

## Alcohol Radiation Removal

Expose the direct radiation-removal effects of normal alcohol:

- Beer -> `BeerAntirad1`
- Vodka -> `VodkaAntirad3`

Prefer one family multiplier that preserves the authored Beer/Vodka strength ratio unless the final UI deliberately presents them as separate item controls.

Magic/Dvupalov Vodka is excluded from this radiation-removal control because its normal effect chain does not use the standard Vodka radiation-removal effect.

## Effect Duration

Duration is a separate tuning domain from effect magnitude.

A consumable duration control must scale only concrete `Duration` fields belonging to that consumable. Instant effects such as instant stamina and direct radiation removal have no duration and are therefore unaffected.

Desired family coverage:

- **Water Effect Duration**
  - `WaterStamina2`
  - `WaterStaminaPerAction1`
  - other Water timed effects only when source-exact

- **Energy Drink Effect Duration**
  - `EnergeticStamina`
  - `EnergeticStaminaPerAction1`
  - Limited Edition timed effects when their exact 2.0.5 rows are locked

- **Vodka Effect Duration**
  - `VodkaStaminaPenalty`
  - `VodkaDrunkness` only if the complete drunkness interaction is intentionally included

- **Beer Effect Duration**
  - `BeerStaminaPenalty`
  - `BeerDrunkness` only if the complete drunkness interaction is intentionally included

- **Magic Vodka Effect Duration**
  - `MagicVodkaPSYProtection`
  - `MagicVodkaDrunkness` only if intentionally included

A single per-consumable duration multiplier is preferred over exposing one slider for every internal timed effect. This keeps the UI readable while preserving the original relative durations inside each item.

## Safety rule for alcohol duration

Do not automatically scale drunkenness duration together with other alcohol effects until `DrunknessMechanics` / `OverDrunknessMechanics` has been fully source-audited. Until then, the safe alcohol-duration target is the direct stamina-penalty duration; drunkenness duration remains deferred.

---

# 1. Water

## Item effect chain

Discovery wiring:

```text
Water
  WaterSatiety1
  WaterStamina2
  RemoveDrunkness5
  WaterStaminaInstant
  WaterStaminaPerAction1
```

Weird Kettle alternative wiring also exists for Water and must be treated as a separate context rather than silently overwritten.

## Confirmed / cross-checked values

| Effect | SID | Vanilla | Evidence | UI suitability |
|---|---|---:|---|---|
| hunger change | `WaterSatiety1` | -10 HungerPoints | 2.0.5 CONFIRMED | direct |
| stamina regeneration | `WaterStamina2` | +25% | 2.0.5 CONFIRMED | direct |
| stamina regen duration | `WaterStamina2` | 5 s | 2.0 CROSS-CHECK | direct |
| instant stamina | `WaterStaminaInstant` | +38% | 2.0.5 CONFIRMED + 2.0 CROSS-CHECK | direct |
| stamina cost reduction | `WaterStaminaPerAction1` | 10% reduction | 2.0 CROSS-CHECK | direct |
| stamina-cost duration | `WaterStaminaPerAction1` | 30 s | 2.0 CROSS-CHECK | direct |
| sobering | `RemoveDrunkness5` | direct drunkness removal effect | DISCOVERY | source value still needs 2.0.5 raw confirmation |

Negative HungerPoints reduce hunger.

### Verdict

Water has enough direct structure for player-facing tuning. Its effects must not be collapsed into one number because it has:
- instant stamina,
- short stamina regeneration,
- longer stamina-cost reduction,
- hunger change,
- sobering.

Recommended normal controls:
- **Water Instant Stamina**
- **Water Stamina Recovery**
- **Water Stamina Recovery Duration**
- **Water Stamina Cost Reduction**
- **Water Stamina Cost Duration**

Hunger and sobering are lower-priority because they overlap Survival/alcohol state semantics.

---

# 2. Energy Drink

## Item effect chain

Discovery wiring:

```text
Energetic
  EnegeticSatiety1
  EnergeticStamina
  EnergeticStaminaInstant3
  EnergeticsPoppyFieldSleepiness
  EnergeticSleepiness
  EnergyDrinkPostProcess
  EnergeticStaminaPerAction1
```

Alternative Weird Kettle effects exist for the same item. The current `survival.energyDrinkFatigueRelief` control already intentionally includes the relevant Weird Kettle/Poppy Field fatigue variants.

## Confirmed / cross-checked values

| Effect | SID | Vanilla | Evidence | UI suitability |
|---|---|---:|---|---|
| hunger change | `EnegeticSatiety1` | +10 HungerPoints | 2.0.5 CONFIRMED | direct |
| stamina regeneration | `EnergeticStamina` | +200% | 2.0.5 CONFIRMED + 2.0 CROSS-CHECK | direct |
| stamina regen duration | `EnergeticStamina` | 45 s | 2.0.5 CONFIRMED + 2.0 CROSS-CHECK | direct |
| instant stamina | `EnergeticStaminaInstant3` | +50% | 2.0.5 CONFIRMED | direct |
| fatigue/sleepiness relief | `EnergeticSleepiness` family | -20 normal effect | 2.0.5 CONFIRMED | already exposed |
| stamina-per-action effect | `EnergeticStaminaPerAction1` | weak stamina-cost effect | DISCOVERY | magnitude needs raw 2.0.5 confirmation |
| stamina-per-action duration | `EnergeticStaminaPerAction1` | 15 s | 2.0 CROSS-CHECK | duration is direct |
| post-process | `EnergyDrinkPostProcess` | presentation effect | DISCOVERY | NO-UI |

### Existing control boundary

`survival.energyDrinkFatigueRelief` owns only fatigue/sleepiness relief. It must **not** absorb stamina, hunger, tolerance or overdose behavior.

### Recommended direct controls

- **Energy Drink Stamina Recovery**
- **Energy Drink Stamina Recovery Duration**
- **Energy Drink Instant Stamina**
- keep existing **Energy Drink Fatigue Relief**

Do not expose `EnergyDrinkPostProcess`.

---

# 3. Limited Edition Energy Drink

## Item effect chain

Discovery wiring:

```text
Energetic_Limited
  EnegeticSatiety2
  EnergeticLimitedStamina
  EnergeticLimitedStaminaInstant4
  EnergeticsLimitedPoppyFieldSleepiness
  EnergyDrinkPostProcess
```

The item inherits from the normal Energy Drink prototype but overrides its effect list.

Alternative Weird Kettle effects additionally include:
- Limited Energetic satiety
- stronger stamina regeneration
- stronger instant stamina
- strong sleepiness removal
- temporary carry-weight / penalty-less-weight effects

Those Weird Kettle variants are artifact-driven alternative effects and should not be silently folded into the normal Limited Edition values.

## Current status

The 2.0.5 baseline confirms the Limited/Poppy fatigue family exists, but it does not yet contain a complete source-exact numeric row for every Limited Edition stamina effect.

2.0-era cross-check confirms:
- normal Limited Edition stamina-regeneration duration remains based on the Energy Drink family and is tunable,
- this is a distinct item/effect family and must not be assumed equal to normal `Energetic`.

### Verdict

Promote only after the exact 2.0.5 `ValueMin/ValueMax` for:
- `EnegeticSatiety2`
- `EnergeticLimitedStamina`
- `EnergeticLimitedStaminaInstant4`

are recovered from raw CFG.

Status: **source path confirmed, exact-value completion required**.

---

# 4. Vodka

## Item effect chain

Discovery wiring:

```text
Vodka
  VodkaAntirad3
  VodkaSatiety1
  VodkaStaminaPenalty
  VodkaDrunkness
  VodkaPSYInstaDecrease
```

Weird Kettle alternatives exist for radiation, satiety and drunkness.

## Confirmed / cross-checked values

| Effect | SID | Vanilla | Evidence | UI suitability |
|---|---|---:|---|---|
| radiation removal | `VodkaAntirad3` | -50 Radiation | 2.0.5 CONFIRMED | direct |
| hunger change | `VodkaSatiety1` | +10 HungerPoints | 2.0.5 CONFIRMED | direct |
| stamina drain penalty | `VodkaStaminaPenalty` | +200% | 2.0.5 CONFIRMED | direct |
| stamina penalty duration | `VodkaStaminaPenalty` | 20 s | 2.0.5 CONFIRMED | direct |
| drunkness | `VodkaDrunkness` | strength 30 in vanilla-era source | 2.0 CROSS-CHECK / raw 2.0.5 still desirable | direct value, mechanics interaction requires care |
| psi relief | `VodkaPSYInstaDecrease` | direct instant psi decrease | DISCOVERY | raw 2.0.5 magnitude required |

### Verdict

Safe normal candidates:
- **Vodka Radiation Removal**
- **Vodka Stamina Drain Penalty**
- **Vodka Stamina Penalty Duration**

Drunkenness strength is writable, but exposing it should happen only after the `DrunknessMechanics -> OverDrunknessMechanics` thresholds and accumulation/recovery chain are fully documented.

---

# 5. Beer

## Item effect chain

Discovery wiring:

```text
Beer
  BeerAntirad1
  BeerSatiety1
  BeerStaminaPenalty
  BeerDrunkness
  BeerPSYInstaDecrease
```

Weird Kettle alternatives exist for all five domains plus a possible sleepiness negative effect.

## Confirmed / cross-checked values

| Effect | SID | Vanilla | Evidence | UI suitability |
|---|---|---:|---|---|
| radiation removal | `BeerAntirad1` | -10 Radiation | 2.0 CROSS-CHECK | direct |
| hunger change | `BeerSatiety1` | weak increase; exact 2.0.5 value pending | DISCOVERY | direct after raw confirmation |
| stamina drain penalty | `BeerStaminaPenalty` | 150% family value seen in extracted effect data | DISCOVERY | direct after 2.0.5 confirmation |
| drunkness | `BeerDrunkness` | strength 15 in vanilla-era source | 2.0 CROSS-CHECK / raw confirmation desirable | mechanics interaction |
| psi relief | `BeerPSYInstaDecrease` | direct instant psi decrease | DISCOVERY | exact magnitude pending |

### Verdict

Beer is no longer an unknown item chain. The actual SIDs are mapped.

The first Beer control that is clean enough to promote once the 2.0.5 raw row is copied into the catalog is:
- **Beer Radiation Removal**

Stamina penalty and drunkness are also promising but should be promoted only with their current exact duration/value rows.

---

# 6. Magic Vodka / Dvupalov Vodka

`DvupalovVodka` is a real quest consumable and is in scope because the request is to cover all non-food/non-med utility consumables, not only generic loot items.

Discovery wiring:

```text
DvupalovVodka
  MagicVodkaDrunkness
  VodkaSatiety1
  MagicVodkaPSYInstaDecrease
  MagicVodkaPSYProtection
```

Important difference from normal Vodka:
- it does **not** use normal Vodka radiation removal,
- it is primarily an intoxication + psi utility item.

Current cross-checks:
- `MagicVodkaPSYProtection` vanilla duration: 90 s
- player-facing psi protection: +2
- shared `VodkaSatiety1` hunger increase
- intoxication is stronger than ordinary alcohol in the item presentation

### Verdict

Potential controls:
- **Magic Vodka Psi Protection**
- **Magic Vodka Psi Protection Duration**
- **Magic Vodka Drunkenness**

Because this is a quest/special consumable, these controls are lower priority than normal Water/Energy/Vodka/Beer tuning.

---

# 7. Energy Drink tolerance and overdose

The Player mechanics graph explicitly contains:

- `EnergeticToleranceMechanics`
- `EnergeticOveruseMechanics`

Localization/effect metadata exposes concepts corresponding to:
- stimulant tolerance buildup,
- stimulant overdose buildup,
- overdose removal.

This proves that Energy Drink usage has a deeper state machine beyond its direct stamina/fatigue effects.

However, normal UI exposure requires the full chain:

```text
drink effect
 -> tolerance / overuse point delta
 -> thresholds
 -> decay / removal
 -> warning state
 -> gameplay consequence(s)
```

Until all of those are source-exact, do **not** expose:
- tolerance threshold,
- overdose threshold,
- overdose buildup,
- overdose recovery,
- overdose consequence strength

as standalone sliders.

Status: **RUNTIME / COMPLETE-CHAIN AUDIT REQUIRED**.

---

# Drunkenness threshold visualizer

The test UI now uses one source-exact threshold scaler:

- `survival.drunknessThresholdScale`

It scales the authored `DrunknessMechanics` thresholds together:

- Medium: 20
- Strong: 40
- Very Strong: 60

The control presents those thresholds primarily as a Beer-based alcohol-tolerance scale using Beer = 15 Drunkness and Vodka = 30 Drunkness. Vanilla therefore reads as 4 beers or 2 vodkas to Very Strong when drinks are stacked before natural sober-up. The normal slider surface is 1–6 beers with 4 marked VANILLA.

Beer is the default reference. The BEER/VODKA selector is presentation-only; it does not edit the drink effect strengths. A separate BACK-TO-BACK PREVIEW selects a simulated drink count and whitens only the effect text for the currently active drunkness tier.

---

# 8. Alcohol drunkness and over-drunkness

The Player mechanics graph explicitly contains:

- `DrunknessMechanics`
- `OverDrunknessMechanics`

Vodka and Beer both feed direct drunkness effects. Water and food can also remove some drunkness through `RemoveDrunkness*` effects.

Therefore a naïve "Alcohol Strength" slider can have non-local consequences if the over-drunkness thresholds are crossed more frequently.

Safe first-stage tuning:
- radiation removal
- stamina penalty magnitude
- stamina penalty duration

Current controlled surface:
- fixed vanilla player thresholds: Medium 20 / Strong 40 / Very Strong 60
- direct Beer strength through `BeerDrunkness.ValueMin/ValueMax`
- direct Vodka strength through `VodkaDrunkness.ValueMin/ValueMax`
- the drinks-to-Very-Strong UI is derived from the selected drink strength rather than from a shared tolerance scalar

Still deferred:
- drunkness duration
- over-drunkness threshold
- over-drunkness consequences
- sobering/removal magnitude

---

# 9. Master difficulty interaction

The existing `Master Consumable Effect Penalties` system is a separate layer from the base consumable EffectPrototypes.

Rule for future controls:

```text
base consumable effect
    x any active MasterEffectModifier entry
    = effective Master result
```

No new control should silently edit both the base effect and a Master modifier unless the UI explicitly presents that as one combined control.

The existing Energy Drink Fatigue Relief and future drink controls should therefore remain base-effect controls unless a concrete Master modifier target is intentionally added.

---

# 10. Recommended UI surface

The cleanest player-facing design is **not** one global Consumable Effectiveness slider.

Recommended group: **Consumable Effects** under Survival.

### Ready / nearly ready

Water:
- Instant Stamina
- Stamina Recovery
- Stamina Recovery Duration
- Stamina Cost Reduction
- Stamina Cost Duration

Energy Drink:
- Stamina Recovery
- Stamina Recovery Duration
- Instant Stamina
- existing Fatigue Relief

Vodka:
- Radiation Removal
- Stamina Drain Penalty
- Stamina Penalty Duration

Beer:
- Radiation Removal once the 2.0.5 row is copied into the catalog

### Do not expose yet

- Energy Drink tolerance/overdose
- Beer/Vodka drunkenness controls
- alcohol over-drunkness
- sobering strength
- Beer/Vodka psi-relief magnitude
- Limited Edition exact values until raw 2.0.5 values are recovered
- Weird Kettle negative-effect chance
- purely visual post-process effects

---

# 11. Priority implementation set

If the goal is useful difficulty tuning with minimum speculative behavior, implement in this order:

1. **Water Stamina Cost Reduction**
2. **Water Stamina Cost Duration**
3. **Water Instant Stamina**
4. **Water Stamina Recovery**
5. **Energy Drink Stamina Recovery**
6. **Energy Drink Stamina Recovery Duration**
7. **Energy Drink Instant Stamina**
8. **Vodka Radiation Removal**
9. **Vodka Stamina Drain Penalty**
10. **Vodka Stamina Penalty Duration**
11. **Beer Radiation Removal** after exact 2.0.5 value row is locked
12. keep existing **Energy Drink Fatigue Relief**

These are direct, understandable effects and do not require inventing a high-level aggregate mechanic.

---

# 12. Remaining raw-CFG completion checklist

To close the audit completely, recover/copy the exact 2.0.5 rows for:

- `WaterStamina2`
- `WaterStaminaInstant`
- `WaterStaminaPerAction1`
- `RemoveDrunkness5`
- `EnergeticStamina`
- `EnergeticStaminaInstant3`
- `EnergeticStaminaPerAction1`
- `EnegeticSatiety2`
- `EnergeticLimitedStamina`
- `EnergeticLimitedStaminaInstant4`
- `BeerAntirad1`
- `BeerSatiety1`
- `BeerStaminaPenalty`
- `BeerDrunkness`
- `BeerPSYInstaDecrease`
- `VodkaDrunkness`
- `VodkaPSYInstaDecrease`
- `MagicVodkaDrunkness`
- `MagicVodkaPSYInstaDecrease`
- `MagicVodkaPSYProtection`

For each row capture:

```text
SID
EEffectType
ValueMin / ValueMax
Duration
Charges
Delay
DuplicationType
Positive
AffectedStaminaActionTypes (when applicable)
ApplyExtraEffectPrototypeSIDs
```

After that, the remaining direct controls can be promoted with the same 1:1 standard used elsewhere in the project.
