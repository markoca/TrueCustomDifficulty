# Non-Food Hunger Penalties — CFG audit (2.1)

Date: 2026-11-24. Status: CFG-confirmed; implementation deferred; runtime acceptance required.

## Source and scope

Primary source: `STALKER2-2.1 cfg dump(3).zip`, SHA-256 `d1f38f28effae06d5009cbf823e268e9fa2db42ab4fdd0f4de4a8b0ef08228a9`. Inspected base and DLC1 nested CFG archives. Enumerated positive explicit HungerPoints effect prototypes and traced item references, including alternate effect arrays. The engine may classify drinks as food; scope here means utility consumables with positive hunger values, not food satiety, medical effects, or environmental hunger.

## Exact standard targets

Source: `GameData/EffectPrototypes.cfg` (runtime GameLite/GameData). Each target is a top-level prototype inheriting `[0]` (Empty). Each has `Type = EEffectType::HungerPoints`, `Positive = EBeneficial::Negative`, and explicit matching ValueMin/ValueMax.

| Item SID | ItemPrototypes.cfg reference | Effect SID (writer root) | ValueMin | ValueMax |
|---|---|---|---:|---:|
| Energetic | EffectPrototypeSIDs[0] (line 16480) | EnegeticSatiety1 | 10 | 10 |
| Energetic_Limited | EffectPrototypeSIDs[0] (16546) | EnegeticSatiety2 | 5 | 5 |
| Beer | EffectPrototypeSIDs[1] (16817) | BeerSatiety1 | 10 | 10 |
| Vodka | EffectPrototypeSIDs[1] (16382) | VodkaSatiety1 | 10 | 10 |
| DvupalovVodka | EffectPrototypeSIDs[1] (18746) | VodkaSatiety1 | 10 | 10 |

The exact game spelling is EnegeticSatiety, not EnergeticSatiety. Limited inherits Energetic but overrides both effect arrays. DvupalovVodka explicitly shares the standard vodka effect: four distinct roots / eight scalar fields for five items. Do not create independent Magic Vodka controls unless explicitly reworking that shared effect chain. The source values are raw hunger points, not guaranteed final in-game percentage changes.

All four inherit Duration = 0.f, Charges = 0, DelayMin = DelayMax = 0.f. ShouldPauseByDialog = false is explicit for both energy effects and vodka; Beer inherits true. Preserve these and all other fields. Do not add duration edits for these magnitude-only effects.

## Weird Kettle alternative paths

Same EffectPrototypes.cfg and ValueMin/ValueMax pair; ItemPrototypes.cfg AlternativeEffectPrototypeSIDs selects these separate effects.

| Item SID | Alternative array index | Effect SID | ValueMin | ValueMax |
|---|---:|---|---:|---:|
| Energetic | 0 | Artifact_WeirdKettle_EnegeticSatiety | 10 | 10 |
| Energetic_Limited | 0 | Artifact_WeirdKettle_LimitedEnegeticSatiety | -100 | -100 |
| Beer | 0 | Artifact_WeirdKettle_BeerSatiety | 5 | 5 |
| Vodka | 1 | Artifact_WeirdKettle_VodkaSatiety | 10 | 10 |
| DvupalovVodka | 1 | Artifact_WeirdKettle_VodkaSatiety | 10 | 10 |

Three positive alternate roots (six fields) are potential opt-in fanout targets, but no product decision is made here. If a future relative multiplier includes alternate positive effects, preserve their own vanilla values, especially Beer (+5 rather than +10). Keep Limited's -100 untouched. The alternate Beer root has LocalizationSID = decrease_hunger despite positive +5 and Negative classification; do not infer sign from localization and do not change presentation fields in a magnitude-only writer.

Artifact_WeirdKettle_NegativeEffect_Satiety = +33/+33 is referenced by WeirdKettle_NegativeEffect_Satiety.ApplyExtraEffectPrototypeSIDs[0] in the same CFG (line 15553). It is a separate composite-effect path, not a standard drink effect-list target. Activation/frequency not established. Exclude it.

## Exclusions and difficulty interaction

- WaterSatiety1 = -10 and food/milk/ration hunger relief are out of scope.
- No additional positive HungerPoints effect wired to a vanilla medical item was found in the scanned base/DLC1 item data.
- Other positive roots are SleepHunger (+0.04), VolumeHungerIncreasePermanent (+5), VolumeHungerMax (+100), DLC1 SpatialBubble_HungerIncreasePermanent (+1). None are standard non-food consumable penalty targets.
- DLC1 provides no additional standard positive consumable hunger effect in this source scan.
- DifficultyPrototypes.cfg, EnvironmentDifficulty.Effect_Satiety: Easy/Medium/Hard/Stalker (Novice/Stalker/Veteran/Master) = 1/1/2/2.5. Do not modify this shared field: its runtime interaction with positive and negative hunger effects is not proven by CFG. Also leave passive Hunger Build-up and food relief controls alone.

## Deferred implementation contract

1. Proposed four independent controls (Energy Drink, Limited Energy Drink, Beer, Vodka); final layout, ranges, and whether alternate positive effects participate remain deferred.
2. Direct writer targets are the two existing numeric fields per root, not the item effect lists or parent [0]. Always write ValueMin = ValueMax. For a relative multiplier, 1x preserves each root's own baseline; 0x removes the raw positive effect; never permit a negative penalty accidentally. If using absolute points, retain the different vanilla defaults. Do not change other effects.
3. Test exact vanilla reset, zero, increased values, independent Energy/Limited, shared Vodka/Dvupalov behavior, unchanged food, medicine and artifact negative/relief targets, and unchanged existing duration, fatigue, drunkenness, stamina, radiation and passive hunger settings.
4. Validate actual consumption on all four PC difficulties with controlled starting hunger and no passive drift; separately test Weird Kettle equipped and unequipped and confirm whether Effect_Satiety modifies positive points. Do not label runtime-confirmed until tested.
5. No application code, UI, parameter catalog, CFG writer baseline, or 12 factory presets modified by this audit.

Related historical context: [Non-food/non-medicine consumable effects audit](NONFOOD-NONMED-CONSUMABLE-EFFECTS-AUDIT.md), [Survival mapping](SURVIVAL-MAPPING.md).

## 2026-11-24 — Shared Drink Hunger Penalty (current)

In Consumable Effects (`ConsumableMixer`), place one shared native mini-knob **HUNGER GAIN** (`survival.drinkHungerPenalty`, catalog label Hunger From Drinks) in its own **DRINKS** group between WATER and ENERGY DRINK. The group uses the identical rounded frame, fill, border and height as its neighbors. Keep the existing 270-degree compact dial, centered multiplier, hover, reference tick, modified-number gold, keyboard/wheel/right-click reset. Two small live readout rows below the dial show **Regular: +N pts** (Energy Drink, Beer, Vodka and equivalent positive variants; base 10) and **Limited / Kettle beer: +N pts** (base 5), computed using the same nonnegative integer half-up rule as the writer. Only changed digits turn gold. These are CFG hunger points, not difficulty-adjusted runtime measurements. On narrow widths keep WATER above the shared knob and ENERGY DRINK together on the next row; shared measurement matches rendering. Range 0–4x in 0.2x steps, vanilla 1x. The existing `scaleIntegerHalfUpPreserveZero` writer scales the seven positive roots (14 fields) documented in the [2.1 audit](NONFOOD-HUNGER-PENALTIES-AUDIT-2.1.md), preserving integer source format and allowing zero. Water, food, medicine, Weird Kettle relief/independent penalty, duration and passive hunger are unchanged. This supersedes the earlier deferred four-knob proposal; all twelve factory profiles start at neutral 1x to avoid unrequested rebalance. Runtime acceptance pending.

## 2026-11-24 — Independent water relief and energy/alcohol penalty (current)

Supersedes the previous shared drink hunger control. Consumable Effects uses two independently framed native dials between WATER and ENERGY DRINK: WATER / HUNGER RELIEF (`survival.waterHungerRelief`) and ENERGY / ALCOHOL / HUNGER PENALTY (`survival.drinkHungerPenalty`, retained for saved-profile compatibility). Both use 0–4x, 0.2x steps, 1x Vanilla, native hover/reset, centered multiplier and live signed raw hunger points with only modified digits in gold. Wide layouts use four adjacent frames; compact layouts keep water and relief on row one, penalty and energy on row two. Water scales only WaterSatiety1 ValueMin/ValueMax from -10 using signed `scale`; the exact step yields integral signed CFG output. The penalty scales only EnegeticSatiety1 (+10), EnegeticSatiety2 (+5), BeerSatiety1 (+10), VodkaSatiety1 (+10; shared with DvupalovVodka), using `scaleIntegerHalfUpPreserveZero`. All Weird Kettle alternate and composite effects, food, passive hunger, difficulty modifiers and other drink effects are excluded. 0x removes the selected effect; 1x preserves vanilla. Older user profiles retain their penalty setting; missing water defaults to Vanilla. Unmodified prior factory profiles migrate to the new role-specific settings; custom profiles are not overwritten. Raw CFG behavior is documented; game runtime acceptance remains pending.
