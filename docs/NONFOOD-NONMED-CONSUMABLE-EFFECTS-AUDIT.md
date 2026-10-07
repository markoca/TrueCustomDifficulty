# Non-Food / Non-Medicine Consumable Effects Audit

## Scope

This audit covers **all consumable items that are neither food nor medicine**.

Included families currently evidenced by the runtime-derived baseline and generator audits:

- Water
- Energy Drink / `Energetic`
- Vodka
- Beer
- any additional base-game or DLC consumable discovered through `ItemPrototypes.cfg -> EffectPrototypeSID` traversal that is not food or medicine

Explicitly excluded:

- food: Bread, Fresh Bread, Sausage, Canned Food, dry rations and equivalent food-family items
- medicine: Bandage, Medkit, Army Medkit, Scientific/Eco Medkit, Anti-Rad and equivalent medical-family items

Environmental water-contact mechanics are outside this audit. `WaterMediumStaminaPenalty` and `WaterStrongStaminaPenalty` describe movement through water, not drinking Water.

## Source contract

Runtime CFG remains the source of truth.

```text
ItemPrototype
    -> EffectPrototypeSID
        -> EffectPrototype values / duration / charge semantics
            -> player mechanics consuming those effects
```

Do not invent one global "consumable effectiveness" scalar. Every supported control must own an explicitly enumerated target family.

## Water

Confirmed baseline effects:

| Effect | Vanilla |
|---|---:|
| HungerPoints | -10 |
| timed stamina effect | +25% over effect duration |
| instant stamina | +38% |

Known effect SID:

- `WaterSatiety1` = `-10 HungerPoints`

Negative HungerPoints reduce hunger.

The instant and timed stamina effects must remain separate until the source chain proves one intentional shared scalar.

## Energy Drink / Energetic

Confirmed baseline effects:

| Effect | Vanilla |
|---|---:|
| HungerPoints | +10 |
| stamina regeneration | +200% for 45 s |
| instant stamina | +50% |
| sleepiness/fatigue | -20 |

Known effect families:

- `EnegeticSatiety1` = `+10 HungerPoints`
- `EnergeticSleepiness`
- `Artifact_WeirdKettle_LimitedEnergeticSleepiness`
- `EnergeticsPoppyFieldSleepiness`
- `EnergeticsLimitedPoppyFieldSleepiness`

The existing `survival.energyDrinkFatigueRelief` control owns only the fatigue/sleepiness family. It must not absorb stamina, hunger, tolerance or overuse effects.

The Player mechanics graph also contains:

- `EnergeticOveruseMechanics`
- `EnergeticToleranceMechanics`

These are mandatory parts of this audit before overdose/tolerance controls are considered.

## Vodka

Confirmed baseline effects:

| Effect | Vanilla |
|---|---:|
| HungerPoints | +10 |
| radiation | -50 |
| stamina/SP drain penalty | +200% for 20 s |

Known effect SID:

- `VodkaSatiety1` = `+10 HungerPoints`

The Player mechanics graph also contains:

- `DrunknessMechanics`
- `OverDrunknessMechanics`

Radiation relief, stamina-drain penalty, drunkenness buildup and over-drunkness consequences must be treated as separate effect domains.

## Beer

Beer is confirmed as an active consumable in the runtime generator graph, including `GeneralNPC_Consumables_CloseCombat`.

The current baseline does not yet contain a source-exact Beer `ItemPrototype -> EffectPrototypeSID` chain.

Rules:

- Beer remains mandatory audit scope.
- Do not infer Beer values from Vodka.
- Do not expose Beer controls until its actual EffectPrototype chain is recovered.

## Relevant shared mechanics

The Player prototype explicitly includes:

- `DrunknessMechanics`
- `OverDrunknessMechanics`
- `EnergeticOveruseMechanics`
- `EnergeticToleranceMechanics`
- `AdditionalRegenStaminaMechanics` where a consumable effect actually feeds it

Effect/localization metadata also exposes concepts for:

- Water stamina
- Water stamina regeneration
- Energetic stamina regeneration
- Energetic tolerance buildup
- Energetic overdose buildup
- overdose removal

These names are discovery evidence only. Normal UI still requires a concrete runtime CFG prototype/path/value chain.

## Candidate player-facing controls

These are audit candidates, not implementation approval.

| Family | Candidate | Status |
|---|---|---|
| Water | Instant Stamina Restore | value confirmed; exact catalog target required |
| Water | Stamina Recovery Boost | value confirmed; duration/source target required |
| Energy Drink | Instant Stamina Restore | value confirmed; exact catalog target required |
| Energy Drink | Stamina Recovery Boost | +200% / 45 s confirmed; exact target required |
| Energy Drink | Fatigue Relief | already implemented and confirmed |
| Energy Drink | Hunger Impact | +10 HungerPoints confirmed |
| Energy Drink | Tolerance Build-up | mechanics exist; complete chain required |
| Energy Drink | Overuse / Overdose Build-up | mechanics exist; complete chain required |
| Vodka | Radiation Removal | -50 confirmed; exact catalog target required |
| Vodka | Stamina Drain Penalty | +200% / 20 s confirmed; exact target required |
| Vodka | Hunger Impact | +10 HungerPoints confirmed |
| Vodka | Drunkenness | mechanics exist; complete chain required |
| Vodka | Over-Drunkenness | mechanics exist; complete chain required |
| Beer | all gameplay effects | raw item/effect chain required |

## Design rules

1. Do not merge direct and timed effects merely because they belong to the same item.
2. Do not mix bottled Water with environmental water-contact controls.
3. Do not fold Energy Drink stamina effects into Fatigue Relief.
4. Do not assume Beer is weaker Vodka.
5. Preserve effect sign semantics:
   - negative HungerPoints = hunger reduction
   - positive HungerPoints = increased hunger
   - negative Radiation = radiation removal
6. Treat duration independently from magnitude unless the source intentionally couples them.
7. Tolerance / overdose / drunkenness controls require their complete threshold, accumulation, recovery and consequence chains.
8. Trace DLC consumables through DLC ItemPrototypes and EffectPrototypes instead of assuming base inheritance.

## Raw-CFG completion checklist

Before adding controls, enumerate:

- every base-game and DLC non-food/non-med consumable ItemPrototype
- every referenced EffectPrototype SID per item
- `ValueMin/ValueMax`
- `DurationMin/DurationMax`
- `DelayMin/DelayMax`
- charges / intervals where present
- affected vital/stat type
- tolerance/overuse/drunkenness thresholds, accumulation and recovery
- MasterEffectModifier entries affecting these consumables
- DLC-specific overrides

The final table must be source-exact:

```text
Item -> Effect SID -> field -> vanilla value -> unit -> duration
     -> special mechanic -> proposed UI -> status
```

No new control should be promoted until its row is source-exact.
