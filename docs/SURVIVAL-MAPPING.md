# Survival CFG Mapping — v0.2.47

### 2026-10-12 — Radiation 360-degree threshold dial (presentation only; CURRENT)

Replaces the read-only Radiation Damage Stages horizontal strip with a full circular 0–100 RAD breakdown, split at original RadiationMechanics boundaries 10,20,30,40,50,60,70,80,90,99. The angular positions are proportional to real RAD values; the final 99–100 wedge spans 3.6 degrees. The original 1-RAD first-damage onset is stated in the dial center rather than adding a requested split. All 11 original damage prototypes remain represented: the first ten values appear inside their correct radial ranges and 99+ Deadly damage appears in a footer to avoid clipping. The stage magnitude values still scale exclusively from survival.radiationDamage (the same 22 writer targets). This is NOT a current radiation level indicator, HP-per-second calculation, or kill countdown, and is independent of RadBlockFieldDamage=9000.

The original BUILD-UP, RECOVERY (PTS/S), and HP DAMAGE knobs, their CFG mappings, native editor inputs and preset output remain unchanged; the new passive dial has zero added hit targets. RadiationBalance height becomes 613 DIP. UI catalogs and radiation stage audit lock the full-circle layout.

### 2026-10-12 — Full original Radiation CFG audit (SOURCE VERIFIED; UI UNCHANGED)

See [RADIATION-CFG-DEEP-AUDIT-2026-10-12.md](RADIATION-CFG-DEEP-AUDIT-2026-10-12.md) for source-exact GameData/DLC1 audit: **11 normal damage levels / 22 writer leaves**, thresholds **1–99 RAD** with 0.001 visual onset, field presets **1, 3, 6, 33, 50 RAD/s** plus Custom and Topaz, inherited default effect **TimePerCharge=1s** (runtime repeat cadence NOT proven), and separate **RadBlockFieldDamage 9000 with explicit 2.7s charge interval**. Full radiation treatment / armor / quest source domains are cataloged. UI remains **three source-native knobs and the read-only damage-value stage strip**; no extra knobs, CFG writer changes or invented death ETA.

### 2026-10-12 — Radiation Damage Stages CFG-faithful display (current)

Original `GameData/EffectPrototypes.cfg` baseline independently verifies **11 radiation-damage effect prototypes** with **22 ValueMin/ValueMax scale targets**. Their original values in catalog target order are `Light=0.2`, `Medium=0.3`, `Strong=0.48`, `L4=0.816`, `L5=1.468`, `L6=2.2`, `L7=4.18`, `L8=8.37`, `L9=17.58`, `L10=38.67`, and separate `Deadly=20`. `survival.radiationDamage` has a writer-proven aggregate **scale** operation on all pairs; its new read-only chart applies the same multiplier for truthful source-effect values, with no extra CFG write. `survival.radiationAccumulation` edits difficulty `Radiation_AccumulationSpeed` scalar only; `survival.radiationRecovery` edits `Player.VitalParams.DegenRadiation` in points/second. Their interaction **cannot establish an exposure-to-death duration** without validated level thresholds, encounter exposure input/cadence and all active HP mitigation/recovery effects. Consequently **no RADIATION KILLS IN countdown** is implemented. The source-backed display is a compact horizontal level profile (10 staged effects plus distinct DEADLY effect), explicitly not a time axis, positioned under the original three editable knobs. Validation: `python3 tools/audit_radiation_damage_profile.py`, local WPF publish/Wine visual review pending.

### 2026-10-12 — Beer 3x radiation removal (current)

`health.beerRadiationRemoval` extends from **0–20** to **0–30 RAD** (0x–3x the source's 10-RAD value). With 5-RAD steps, the existing negative-sign `negateSet` writer emits **-30** for BeerAntirad1's both source bounds at maximum. Other RAD columns and their ruler are unchanged.

## Core-style Hunger and Fatigue starting controls (2026-10-11)

The two top sliders in **Hunger** and **Fatigue / Sleepiness** now reuse exactly the native two-card `DrawCoreResourceRuler` renderer from Core Health/Core Stamina. Hunger has **HUNGER RATE** (`survival.hungerRate`, 0.015 pts/s original, 0–0.030, 0.001 step) and **WELL-FED RECOVERY** (`survival.wellFedStaminaRegen`, original +20%, 0–40%, 1%-point steps). Fatigue has **FATIGUE RATE** (`survival.sleepinessRate`, 0.010 pts/s original, 0–0.020, 0.001 step) and **ENERGY FATIGUE RELIEF** (`survival.energyDrinkFatigueRelief`, original 1x, 0–2x, 0.01 step). The percentage display for Well-Fed Recovery multiplies the raw 0.20 by 100 for display only; its CFG source remains untouched. The controls use core-style neutral reference ticks, native endpoints, numeric-only changed highlighting and identical 105-DIP row geometry. Stage previews/graphs remain independent and are shifted by the exact same 19-DIP delta as the measured aggregate height. No new CFG, preset, stacking or gameplay calculations.


## Consumable duration override and strength controls (2026-10-11)

**WATER + ENERGY REFILL** scales immediate stamina refill from water, energy drink and limited energy drink; **WATER REGEN + COST CUT** controls WaterStamina2 regeneration and WaterStaminaPerAction1 lower stamina usage together; **ENERGY STAMINA REGEN** edits only EnergeticStamina sustained regeneration. These three are genuine source-backed 0–2x strength multipliers. The muted `MEDICINE` chart heading is removed, leaving individual medicine lane names and seconds-scale ticks.

The existing preset-only `drinkDurationOverride` now applies to five drink duration lanes, three short Water/Energy duration tracks, **and Vinca/Hercules/Psi Block**. OFF retains medicine caps **540/900/300s**; ON lets all three independently reach **900s (15 min)**, with Hercules already at that native cap. The medicine chart ruler remains 0–900s in both modes while the drink ruler remains 0–270s. ON preserves current values; OFF clamps only those outside native caps. The original medicine CFG `Duration` edits, dual Hercules coupling, preset serialization, source units and all strength magnitudes remain unchanged.


## Source-backed medicine effects (2026-10-11)

**Player-visible native amounts:** Vinca **+10 BLEED PTS** (bleeding regeneration), Hercules **+20 KG** (additional carry capacity; both coupled CFG effects have raw 20, never added), and Psi Block **+10 PSI PTS** (in-game psi protection). Three compact knobs show a centered positive point/kg amount and smaller neutral unit beneath, not percentages/multipliers/per-second rates. These native amount units are reflected by `CurrentDisplay` and the parameter catalog; the actual regeneration/tick cadence of Vinca and Psi Block still requires game-runtime confirmation.

**Survival → Consumable Effects** now includes Vinca (`survival.vincaBleedRecovery` 10, `survival.vincaDuration` 180s; `CinnamonDegenBleeding`), Hercules (`survival.herculesCarryBonus` 20, `survival.herculesDuration` 300s; both `HerculesWeight` and `HerculesWeight_Penalty`), and Psi Block (`survival.psiBlockRecovery` 10, `survival.psiBlockDuration` 60s; `PSYBlockerIncreaseRegen`). Each has one compact raw-strength knob **below the shared duration frame**, where its independently draggable seconds duration is one of three added medicine lanes (five drinks + three medicine effects). The old Live Effect Preview for drink stamina is removed entirely. The frame has a separately calibrated 0–900s medicine ruler; drink lanes retain their own 0–270s ruler. The medicine parameters must be mounted by `consumableAggregateParameterIds` in the live ViewModel, not merely present in the catalog. Every magnitude writes deterministic `ValueMin/ValueMax` together; Hercules edits synchronize **both** effect prototypes rather than summing them. No duplicate medicine heals, drink durations, or base carry-weight controls are introduced. Source/CFG mappings verified; game-runtime scaling remains to be accepted.


### 2026-10-12 — Sleep HP editor default and range (current)

`survival.sleepHealthRestore` still writes the real `SleepHealthRestore.ValueMin/ValueMax` CFG source (vanilla **300 HP**). The *tool* intentionally starts and resets at **100 HP**, with a **0–200 HP** range in steps of **5 HP**. Selecting 0 HP means no health restored by this sleep effect. The displayed **0x–2x** labels scale against 100 HP, not against vanilla. A fresh preset writes 100 HP to both fields; presets saved with more than 200 HP are clamped to 200 on load. The other stamina sleep effect is unchanged.

### 2026-10-11 — Sleep wake-up percent and HP interpretation (current)

`SleepingDashboard`'s **HEALTH RESTORED** edits the original `SleepHealthRestore` **300 HP** amount per sleep completion; it is not 300 HP/s, and the player's maximum-HP cap remains in effect. **STAMINA RESTORE STRENGTH** edits the original `SleepStaminaRestore` strength (vanilla **+100%**, raw `1.0`), which describes the magnitude of the native wake-up stamina effect, **not** extra +100 stamina points, a maximum-SP boost or SP/s. Source CFG and these mappings do not alone establish the runtime combination/refill formula. Both remain independent preset and CFG fields.

### 2026-10-10 — Beer and Vodka independent RAD removal (latest binding contract)

Within the **single MEDICAL ITEMS card** the right-hand RADIATION REMOVAL bank now has **four compact vertical sliders on one row**: **SCIENTIFIC MEDKIT**, **ANTI-RAD**, **BEER** and **VODKA**. There is no second-row, duplicate standalone Radiation Treatment card, or separate consumable alcohol antirad slider. The RAD bank is given additional horizontal space by tightening the four gold HEAL / copper BLEED pairs on the left; at least 820 DIP board width is reserved so all 12 numeric columns remain one row without clipping. Each column uses the shared horizontal cap/white-on-hover and independent source-native drag/keyboard/right-click/R reset; the RAD bank keeps a separate teal 0–200 vertical scale, with numbers not conflated with HP or bleeding points.

**Actual original CFG prototypes:** `BeerAntirad1.ValueMin/ValueMax=-10/-10`; `VodkaAntirad3.ValueMin/ValueMax=-50/-50`. New independent positive-unit control IDs: `health.beerRadiationRemoval` (vanilla **10 RAD**, **0–30 RAD**, 5-RAD native steps) and `health.vodkaRadiationRemoval` (vanilla **50 RAD**, **0–100 RAD**, 5-RAD native steps). Scientific Medkit remains 60 RAD/120 max; Anti-Rad 100 RAD/200 max. Both new controls write exclusively to their own source prototypes via the existing positive-to-negative `negateSet` operation; neither affects intoxication, drink duration, alcohol tolerance or the other drink. The previous `survival.alcoholRadiationRemoval` shared 0–2x parameter and its common Beer/Vodka CFG ownership have been retired from the catalog/Consumable Effects UI. **Preset compatibility:** when an old preset still contains the shared multiplier and lacks new individual drink values, load converts it to Beer `10 × old multiplier` and Vodka `50 × old multiplier`, retaining even fractional authored RAD amounts; new presets persist the two independent positive-unit values. Thus default 1x restores 10/50 without changing how the original game works.

The existing Medical Items anchor `health.bandageHealing` reflects changes in every HEAL/BLEED/RAD source plus Food Healing, but production CFG writing remains strictly per-source. No new effect, source ID collision or duplicate writer output is allowed. This current specification overrides historical shared-alcohol and two-RAD-bank UI descriptions.

This document records the runtime CFG basis for the Survival tab. Runtime game dump 2.0.5 is the source of truth.

## Negative-status onset policy

The app does **not** expose the Low / Medium / Severe Hunger, Fatigue, Bleeding, Master Starvation, Master Severe-Fatigue, or Master Radiation activation thresholds. Those values remain at their vanilla runtime settings. The UI only tunes build-up rates and the consequences that apply once the game has entered the corresponding state.

## Design boundary

Survival owns state build-up, sleep eligibility/recovery, and the strength of survival-state consequences. Status-state transition thresholds are intentionally kept vanilla and are no longer exposed as tuning controls, because moving the onset of a negative state separately from the state itself can create inconsistent behavior. Direct medical-item healing remains under Health. Base stamina regeneration/action costs remain under Stamina. Weather/emission scheduling remains out of scope.

Thirst remains deferred: `MaxThirstPoints` and `RegenThirstPoints` exist, but the supplied runtime CFG dump does not expose a complete active thirst mechanics chain comparable to Hunger/Sleep/Radiation/Bleeding.

## Hunger

Player base:

- `ObjPrototypes.cfg -> Player.VitalParams.RegenHungerPoints = 0.015`
- `MaxHungerPoints = 100`

`HungerMechanics` vanilla state boundaries (reference only; not exposed in UI):

- Very low: `0`
- Low: `17`
- Medium: `33`
- Strong: `83`

State effects:

- Very low: `VeryLowHungerStaminaRegeneration = +20%`
- Low: stamina-regeneration modifier `0%`
- Medium: stamina-regeneration modifier `-33%`, plus `HungerNoSleep`
- Strong: stamina-regeneration modifier `-70%`, plus `HungerNoSleep`

The normal UI exposes these two negative CFG modifiers as positive player-facing **penalty magnitudes**: vanilla `33%` and `70%`. The writer uses `negateSet`, so the generated CFG remains `-33%` / `-70%`. This keeps the slider direction intuitive: farther right always means a stronger hunger penalty.

Difficulty scalar:

- `EnvironmentDifficulty.Effect_Satiety`: Novice `1.0`, Stalker `1.0`, Veteran `2.0`, Master `2.5`
- The CFG confirms the scalar and values but not the final runtime direction for every food/satiety interaction. It remains runtime-test-required.

## Master starvation bundle

Master adds `HungerNegativeMechanics` through `EnvironmentDifficulty.AdditionalMechanicsEffects`.

Vanilla bundle activation point (reference only; not exposed in UI):

- Hunger `83`

Child effects:

- `HungerNegativeStaminaPenalty`: `SPDrain = +200%`, delay `3 s`
- `HungerNegativeDamage`: damage `4`, one charge every `10 s`, delay `3 s`
- `HungerNegativeHealthRegeneration`: `RegenHealth = -100%`, delay `3 s`

The bundle toggle and child values are separate controls. The effect prototypes exist globally, but they are dormant unless the bundle is active.

## Fatigue / sleepiness

Player base:

- `RegenSleepinessPoints = 0.01`
- `MaxSleepinessPoints = 100`

`SleepMechanics` vanilla state boundaries (reference only; not exposed in UI):

- Low: `50`
- Medium: `60`
- Strong: `80`

Standard penalties:

- Low aiming time: `+15%`
- Medium aiming time: `+25%`
- Strong aiming time: `+40%`
- Medium reload time: `+15%`
- Strong reload time: `+18.75%`

These are standard fatigue effects and are independent from the Master-only severe-fatigue bundle.

### Energy-drink fatigue relief

`GameData/EffectPrototypes.cfg` contains direct energy-drink sleepiness relief:

- `EnergeticSleepiness`: `ValueMin/ValueMax = -20`
- `Artifact_WeirdKettle_LimitedEnergeticSleepiness`: `-100`
- `EnergeticsPoppyFieldSleepiness`: `-3.5`
- `EnergeticsLimitedPoppyFieldSleepiness`: `-5.0`

The **Energy Drink Fatigue Relief** slider scales those values together while preserving their vanilla relative strengths. It does not alter `Player.VitalParams.RegenSleepinessPoints`; passive fatigue accumulation remains controlled by **Fatigue Build-up Rate**.

## Sleeping

`DefaultSleepParams`:

- `AllowSleepThreshold = 50`
- `SleepHoursMultiplier = 0.08`
- `MinSleepHours = 7`
- `bAllowEmissionSleep = false`

Vanilla sleep can be blocked by:

- `RadiationNoSleep`
- `BleedingNoSleep`
- `PsyNoSleep`
- `HungerNoSleep`

Direct post-sleep effects exposed in the UI:

- `SleepHealthRestore = 300`
- `SleepStaminaRestore = +100%`

Other vanilla sleep effects remain mapped but not exposed yet:

- bleeding removal `-100%`
- radiation removal `-100%`
- PSY removal `-100%`
- suppression removal `-100%`
- `SleepHunger = 0.04` — runtime semantics need further validation before exposing it.

`bAllowEmissionSleep` remains vanilla and is not exposed in the UI.

## Master severe-fatigue bundle

Master adds `SleepinessNegativeMechanics`.

Vanilla bundle activation point (reference only; not exposed in UI):

- Sleepiness `50`

Child effects:

- max stamina `-20%`
- additional inventory weight `-20%`
- penalty-less-weight effect `-20%`
- recoil `+55%`
- idle sway X `0.4`
- idle sway Y `0.4`
- dispersion `0.5`

The exact player-facing conversion of raw sway/dispersion values is engine-side, so the UI presents those raw values and does not invent degree/accuracy percentages.

## Independent short consumable-effect durations (2026-10-10)

Verified source targets in `GameData/EffectPrototypes.cfg` remain the same physical `Duration` fields:

| New source control | Original Duration | Absolute editable limit |
|---|---:|---|
| `survival.waterRegenDuration` → `WaterStamina2` | 5 s | 5 s to `30 × waterEffectDuration` |
| `survival.energyFullCostDuration` → `EnergeticStaminaPerAction2` | 5 s | 5 s to `45 × energyDrinkEffectDuration` |
| `survival.energyPartialCostDuration` → `EnergeticStaminaPerAction1` | 15 s | 15 s to `45 × energyDrinkEffectDuration` |

The parent duration sliders own only the longest native effects now; all short durations have separate `set` writer routes with no overlap. Main Water cannot go below 5/30× and Energy cannot go below 15/45× without contradicting the newly required short-effect vanilla minima. No new effects are added, and magnitude/stacking are not changed. See [Energy source audit](ENERGY-DRINK-EFFECT-SOURCE-AUDIT-2026-10-10.md).

## Radiation

Difficulty accumulation scalar:

- Novice `0.60`
- Stalker `1.00`
- Veteran `1.50`
- Master `1.85`

Player passive recovery:

- `Player.VitalParams.DegenRadiation = 0.05`

Master additionally enables `RadiationNegativeMechanics`:

- vanilla activation point: `50` radiation (reference only; not exposed in UI)
- `RadiationNegativeStaminaPenalty = +50% SPDrain` — this is a 50% increase in stamina cost/drain, not a stamina gain.

This extra bundle does not replace the normal multi-level radiation state machine; it adds a stamina consequence on top.

## Bleeding

Difficulty scalar:

- Novice `0.25`
- Stalker `1.00`
- Veteran `1.00`
- Master `1.50`

Player passive recovery:

- `Player.VitalParams.DegenBleeding = 0.3`

`BleedingMechanics` vanilla state boundaries (reference only; not exposed in UI):

- Low: `0.1`
- Medium: `50`
- Strong: `75`

Runtime 2.0.5 direct damage effects:

- `LowBleedingDamage = 1.0`
- `MediumBleedingDamage = 1.5` with one-second charge interval
- `StrongBleedingDamage = 2.0` with one-second charge interval

Medium/Strong states also include movement and no-sleep effects. Those are intentionally not exposed in v0.1.19 because the raw velocity-change semantics are less player-friendly and deserve separate runtime validation.

## Master-only difficulty membership

`DifficultyPrototypes.cfg` enables these extra survival bundles on Master through `EnvironmentDifficulty.AdditionalMechanicsEffects`:

- `HungerNegativeMechanics`
- `RadiationNegativeMechanics`
- `SleepinessNegativeMechanics`
- `RegenHealthNegativeMechanics`

These Master-only bundles are configured in the **Master** tab. Their activation boundaries stay vanilla; only the bundle switches and penalty strengths are exposed.


## v0.1.32 cleanup

`Effect_Satiety` and `SleepHoursMultiplier` remain documented as runtime fields but are no longer exposed as player controls. The tuner uses `Hunger Build-up Rate` as its single hunger-speed control.

### Radiation Damage

The UI exposes one aggregate `Radiation Damage` control. It scales the `ValueMin/ValueMax` values of `LightRadiationDamage`, `MediumRadiationDamage`, `StrongRadiationDamage`, `RadiationLevel4Damage` through `RadiationLevel10Damage`, and `DeadlyRadiationDamage`. Radiation-state thresholds are intentionally left vanilla.


## v0.2.47 Survival deep-audit corrections

- Revalidated all 23 normal Survival-tab controls against the locked runtime/writer baseline: 23 confirmed controls and 70 concrete edit points.
- `Hunger Stamina Recovery Penalty` and `Severe Hunger Stamina Recovery Penalty` now expose positive penalty magnitudes (`33%` / `70%` vanilla) while `negateSet` writes the original `-33%` / `-70%` CFG modifiers.
- The linked hunger-severity constraint now follows the player-facing values directly: medium penalty <= severe penalty.
- Corrected the stale mapping-only `0.33` Low Bleeding line to the already-audited runtime value `1.0`; the catalog/writer mapping was already correct.
- Thirst, state activation thresholds, `SleepHunger`, emission-sleep permission and ambiguous movement-state effects remain outside the normal Survival UI.
