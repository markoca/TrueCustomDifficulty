# Consumable medicine and item-effect CFG audit — 2026-10-11

## Inputs and methodology

User-supplied `STALKER2-GameData-CFG(8).zip`, specifically `GameData/ItemPrototypes.cfg` and `GameData/EffectPrototypes.cfg`. This is a **source mapping / UI candidate audit**, not a gameplay runtime test or permission to add controls. Affects **no** configuration, save or mod output in this audit. The archive has **17** top-level `{refkey=TemplateConsumable}` item definitions, including GuitarUsable (no authored effects).

Keep distinctions between (1) an item binding to one or more named effect prototype SIDs, (2) the prototype's **raw source type/value/duration**, and (3) the in-game resulting quantity. Do not present source 10 as “10 HP/s”, “10 psi protection points per second” or “10%” without runtime proof. For a shared item, do not count paired effects as additive stacks.

### Focused item-to-effect ownership

| UI / recognizable item | Actual item SID | Exact owned effect SID(s) | Raw effect kind | Vanilla raw strength(s) | Duration | Behavior / source caution |
|---|---|---|---|---|---|---|
| Vinca candidate — bleed medicine | `Cinnamon` | `CinnamonDegenBleeding` | `EEffectType::DegenBleeding` | `ValueMin = ValueMax = 10.0` | **180 s** | Positive ongoing bleed-recovery source; `DuplicationType = KeepNew`. The literal string `Vinca` **does not occur** in this supplied CFG: this is an item-name identification candidate, not a verified localization mapping. Confirm the rendered/localized name separately. |
| Psi Block | `PSYBlocker` | `PSYBlockerIncreaseRegen` | `EEffectType::DegenPsyPoints` | `10` | **60 s** | Positive psi-point degeneration/recovery source; `KeepNew`. Not guaranteed to be equivalent to general psi-damage resistance or a percentage-based armor stat. |
| Hercules | `Hercules` | `HerculesWeight` **and** `HerculesWeight_Penalty` | `AdditionalInventoryWeight` **and** `PenaltyLessWeight` | `20` **on both**, not summed | **300 s on both** | Paired related carrying-capacity / weight-penalty effects; both `KeepNew`, `ShouldPauseByDialog=false`. Exposing only one leaves mismatched rules. Does not rewrite the base `Player.VitalParams` carry limit. |

Source item bindings: `ItemPrototypes.cfg` has `Cinnamon`, `PSYBlocker` and `Hercules` in the Medicine category with corresponding `EffectPrototypeSIDs` entries. Source parameter definitions: `EffectPrototypes.cfg` has all four exact prototype entries. `Cinnamon` is sorted as a buff (priority 1), `Hercules` priority 2, `PSYBlocker` priority 3. The `ShouldShowEffects` list marks the paired Hercules secondary effect hidden from the vanilla inventory effect display, **not** absent from gameplay.

### Proposed player controls, **not yet implemented**

| Intended destination | Compact control label | Exact native source field(s) to write in a future implementation | Vanilla | Coupling / safeguards |
|---|---|---|---:|---|
| Health → medical effects | **BLEED RECOVERY STRENGTH** | `CinnamonDegenBleeding.ValueMin` and `ValueMax` | 10 | Both bounds change as a single value; never adjust bandage's instant bleed treatment. Native output unit not yet calibrated by runtime. |
| Health → medical effects | **BLEED RECOVERY DURATION** | `CinnamonDegenBleeding.Duration` | 180 s | Independent duration; maintain `KeepNew` / item SID. |
| Survival or Health → medical effects | **PSY RECOVERY STRENGTH** | `PSYBlockerIncreaseRegen.ValueMin` and `ValueMax` | 10 | Both bounds in lockstep. Avoid labeling as “PSY PROTECTION %”. |
| Same section | **PSY BLOCK DURATION** | `PSYBlockerIncreaseRegen.Duration` | 60 s | Native seconds, no new protection/resistance field. |
| Survival / carrying | **HERCULES CARRY BONUS** | `HerculesWeight.ValueMin/ValueMax` **and** `HerculesWeight_Penalty.ValueMin/ValueMax` | 20 per effect | **Atomic coupled writer** on all four scalar leaves; display **one** player knob, not two independent effect strengths; avoid misrepresenting +40 carry weight. |
| Same section | **HERCULES DURATION** | `HerculesWeight.Duration` **and** `HerculesWeight_Penalty.Duration` | 300 s each | Write both durations together. Never modify base maximum carry weight. |

Recommended visual grammar **if approved later**: three concise cards (Vinca/bleed, Psi Block, Hercules), each with one source-native knob for strength and one compact native-seconds duration slider; no extra explanation inside each control. This is a proposal only. Before implementation, choose validated editing ranges / step granularities, test value and duration mapping in the real production writer and confirm runtime effect scaling. Do not invent “number of pills” / resistance multipliers. No new UI catalog entry or serializer mapping has been added during this audit.

### Other consumables and duplicate-control screening

Full top-level item inventory from the supplied `TemplateConsumable` declarations:

| Category | Item SID | Representative authored primary effects | Existing TCD state / suggested disposition |
|---|---|---|---|
| Medicine | `Bandage`, `Medkit`, `ArmyMedkit`, `EcoMedkit`, `AntiRad` | health restoration, bleeding treatment, EcoMedkit radiation removal, AntiRad radiation removal | **Already represented** in `health.*` Medical Items and Radiation Removal; no duplicate row for those abilities. |
| Medicine / buff | `Cinnamon`, `PSYBlocker`, `Hercules` | bleed recovery (10/180s), psi recovery (10/60s), linked carry/weight-penalty pair (20/300s) | **New, source-backed candidates** above. |
| Drink | `Water`, `Energetic`, `Vodka`, `Beer` | stamina / stamina-action cost, duration, antirad, alcohol strength and other secondary effects | Significant existing **Survival Consumable Mixer** and **Health** controls. Do not reintroduce duplicate drink duration, instant stamina or alcohol radiation removal editors. |
| Food | `Bread`, `CannedFood`, `Sausage`, `Milk` | satiety `-20/-50/-30/-100`; Bread/CannedFood/Sausage health effects `8/18/12`; `RemoveDrunkness` `-10/-15/-15/-5` respectively | Potential secondary study only; the current request prioritizes the three medicines. Food satiety/health/drunkness are different effect families and should not be rolled into item strength without a new audit. |
| Other | `GuitarUsable` | no `EffectPrototypeSIDs` in its item block | No difficulty-value slider warranted from this item record. |

Additional source details: drink `Water` has `WaterStamina2` `Stamina` 25% / 5s, `WaterStaminaInstant` 38%, `WaterStaminaPerAction1` SPDrain -10% / 30s. `Energetic` has a 200% regen source / 45s and independent -30% / 15s, -100% / 5s action-cost effects; its other tolerance/overuse mechanics should be audited as a separate subsystem. `Vodka` and `Beer` both have individual `Drunkness`, antirad, hunger, stamina-cost and psi-point effects; current UI coverage must be compared by **prototype SID**, not simply by item display name.

### Safety and acceptance criteria for subsequent implementation

1. **Confirm the name** `Vinca` ↔ `Cinnamon` from the game's localized strings or actual UI. This CFG by itself proves `Cinnamon`, not `Vinca`.
2. Preserve `ValueMin == ValueMax` for these deterministic strengths, and link Hercules' two independent effect prototypes when editing magnitude or duration.
3. All three sources use `KeepNew`; do **not** claim that consuming a second item stacks effects additively.
4. Do not infer regenerated HP/s, bleed units/s, effective psi armor or base carry-weight points solely from raw prototype fields.
5. Check true CFG writer patch routes, source value normalization, reset/preview behavior and mod compatibility before exposing sliders.
6. Update `UI-DESIGN-LOCK.md` and `VISUAL-CONTROL-SYSTEM.md` with any subsequently approved controls, retaining compact/framed-free styles.
