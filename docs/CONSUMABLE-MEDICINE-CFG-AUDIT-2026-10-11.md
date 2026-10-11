# Consumable medicine and item-effect CFG audit — 2026-10-11

## Expanded duration editor limits (2026-10-11)

The existing `OVERRIDE LIMITS` editor-only preset switch is no longer drink-only. The three medicine durations join the existing five main drink and three Water/Energy short-effect editors. OFF keeps the actual native medicine slider caps: Vinca **540 seconds**, Hercules **900 seconds**, Psi Block **300 seconds**. ON allows all three medicine editors to reach **900 seconds**, independently of each other and drink values (Hercules already has a native maximum of 900s). The medicine ruler remains 0–900s (0/300/600/900 ticks) in both modes, while the distinct drink ruler remains at 0–270s. Disabling the override restores each native cap and clamps oversized values; the same saved `drinkDurationOverride` boolean is reused. There is no new source prototype or fake CFG toggle. Both Hercules effect-duration leaves remain bound to one current value. The gray `MEDICINE` caption is removed without deleting the medicine rows.


## Inputs and methodology

User-supplied `STALKER2-GameData-CFG(8).zip`, specifically `GameData/ItemPrototypes.cfg` and `GameData/EffectPrototypes.cfg`. This began as a **source mapping / UI candidate audit**. The six verified source controls have since been implemented in the catalog, production CFG writer and Survival UI. It is **not** an in-game runtime acceptance report. The archive has **17** top-level `{refkey=TemplateConsumable}` item definitions, including GuitarUsable (no authored effects).

Keep distinctions between (1) an item binding to one or more named effect prototype SIDs, (2) the prototype's **raw source type/value/duration**, and (3) the in-game resulting quantity. The game's consumable item descriptions corroborate Vinca **+10 bleeding regeneration**, Psi Block **+10 psi protection**, and Hercules **+20 kg** extra carry capacity. The UI displays these as source-native point/kg **amounts**, not abstract multipliers. Do not infer “10 HP/s”, “10 psi points per second”, effect-tick frequencies or percentages from this. For a shared item, do not count paired effects as additive stacks.

### Focused item-to-effect ownership

| UI / recognizable item | Actual item SID | Exact owned effect SID(s) | Raw effect kind | Vanilla raw strength(s) | Duration | Behavior / source caution |
|---|---|---|---|---|---|---|
| Vinca — bleed medicine | `Cinnamon` | `CinnamonDegenBleeding` | `EEffectType::DegenBleeding` | `ValueMin = ValueMax = 10.0` | **180 s** | Positive ongoing bleed-recovery source; `DuplicationType = KeepNew`. The literal string `Vinca` does not appear in this CFG; an independent item-ID source confirms that the displayed **Vinca** item uses the internal SID `Cinnamon` (see name cross-check). |
| Psi Block | `PSYBlocker` | `PSYBlockerIncreaseRegen` | `EEffectType::DegenPsyPoints` | `10` | **60 s** | Positive psi-point degeneration/recovery source; `KeepNew`. Not guaranteed to be equivalent to general psi-damage resistance or a percentage-based armor stat. |
| Hercules | `Hercules` | `HerculesWeight` **and** `HerculesWeight_Penalty` | `AdditionalInventoryWeight` **and** `PenaltyLessWeight` | `20` **on both**, not summed | **300 s on both** | Paired related carrying-capacity / weight-penalty effects; both `KeepNew`, `ShouldPauseByDialog=false`. Exposing only one leaves mismatched rules. Does not rewrite the base `Player.VitalParams` carry limit. |

Source item bindings: `ItemPrototypes.cfg` has `Cinnamon`, `PSYBlocker` and `Hercules` in the Medicine category with corresponding `EffectPrototypeSIDs` entries. Source parameter definitions: `EffectPrototypes.cfg` has all four exact prototype entries. `Cinnamon` is sorted as a buff (priority 1), `Hercules` priority 2, `PSYBlocker` priority 3. The `ShouldShowEffects` list marks the paired Hercules secondary effect hidden from the vanilla inventory effect display, **not** absent from gameplay.

### Name cross-check: Vinca = Cinnamon

The [Gamer Guides medicine item database](https://earth.gamerguides.com/stalker-2-heart-of-chornobyl/database/consumables/medicine) lists Vinca with creation/console ID `Cinnamon`, +10 bleeding regeneration and a 180-second duration. This independently corroborates the naming, while the supplied CFG remains the source for exact writer paths and values.

### Implemented player controls (2026-10-11)

| Intended destination | Compact control label | Exact native source field(s) written | Vanilla | Coupling / safeguards |
|---|---|---|---:|---|
| Survival → Consumable Effects | **VINCA BLEED REGEN (+10 BLEED PTS)** | `CinnamonDegenBleeding.ValueMin` and `ValueMax` | 10 | Both bounds change as a single value; never adjust bandage's instant bleed treatment. The in-game display specifies +10 bleeding regeneration; actual tick cadence remains unverified. |
| Health → medical effects | **BLEED RECOVERY DURATION** | `CinnamonDegenBleeding.Duration` | 180 s | Independent duration; maintain `KeepNew` / item SID. |
| Survival → Consumable Effects | **PSI BLOCK PROTECTION (+10 PSI PTS)** | `PSYBlockerIncreaseRegen.ValueMin` and `ValueMax` | 10 | Both bounds in lockstep. The game's visible effect is +10 psi protection; no % or per-second claim. |
| Same section | **PSY BLOCK DURATION** | `PSYBlockerIncreaseRegen.Duration` | 60 s | Native seconds, no new protection/resistance field. |
| Survival → Consumable Effects | **HERCULES CARRY (+20 KG)** | `HerculesWeight.ValueMin/ValueMax` **and** `HerculesWeight_Penalty.ValueMin/ValueMax` | 20 per effect | **Atomic coupled writer** on all four scalar leaves; display **one** player knob, not two independent effect strengths; avoid misrepresenting +40 carry weight. |
| Same section | **HERCULES DURATION** | `HerculesWeight.Duration` **and** `HerculesWeight_Penalty.Duration` | 300 s each | Write both durations together. Never modify base maximum carry weight. |

**Current implementation:** three frameless raw-strength knobs **below the shared Effect Duration graph** in the existing Consumable Mixer, replacing the redundant drink-stamina Live Effect Preview area. Vinca, Hercules and Psi Block each occupy an independent additional lane inside the **same** duration frame below its five existing drink lanes. Original drink lanes use 0–270s; medicine lanes share their own labeled 0–900s ruler so values remain physically honest and useful to edit. There are **no standalone medicine duration cards**. Vinca: 0–30 raw strength (step 1), 0–540s duration (step 5); Hercules: 0–60 / 0–900s; Psi Block: 0–30 / 0–300s. `MainViewModel` explicitly instantiates all six hidden aggregate members, fixing the previous empty-controls bug. Hercules updates both effect prototypes and deterministic `ValueMin/ValueMax` together; duration edits update both `Duration` fields. Do not invent pill counts, percent resistance or additive 40-point carry benefit. CFG/preset mappings have static tests; game runtime remains unverified.

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

### Game-facing effect amounts and evidence

- [Vinca item effect](https://earth.gamerguides.com/stalker-2-heart-of-chornobyl/database/consumables/medicine): game-facing +10 bleeding regeneration, 180-second duration.
- [Psi Block item effect](https://game8.co/games/STALKER-2-Heart-of-Chornobyl/archives/485928): game-facing +10 psi protection, 60-second duration.
- [Hercules carrying benefit](https://www.gamespot.com/gallery/stalker-2-increase-carry-weight-encumbrance-guide/2900-5967/): +20 kg carrying capacity, 300-second duration.
- These sources support the **amount/unit shown to players**. They do not establish internal regeneration/tick cadence. The exact CFG writer still uses the audited `ValueMin/ValueMax` fields.

### Runtime safety and acceptance criteria

1. **Preserve the player/internal naming distinction:** display **Vinca**; write the source item SID `Cinnamon` and effect SID `CinnamonDegenBleeding`. The name is externally cross-checked because the CFG by itself does not store the localized item label.
2. Preserve `ValueMin == ValueMax` for these deterministic strengths, and link Hercules' two independent effect prototypes when editing magnitude or duration.
3. All three sources use `KeepNew`; do **not** claim that consuming a second item stacks effects additively.
4. Do not infer regenerated HP/s, bleed units/s, effective psi armor or base carry-weight points solely from raw prototype fields.
5. Check true CFG writer patch routes, source value normalization, reset/preview behavior and mod compatibility before exposing sliders.
6. Update `UI-DESIGN-LOCK.md` and `VISUAL-CONTROL-SYSTEM.md` with any subsequently approved controls, retaining compact/framed-free styles.

## Production editor IDs

- `survival.vincaBleedRecovery`, `survival.vincaDuration`
- `survival.herculesCarryBonus`, `survival.herculesDuration`
- `survival.psiBlockRecovery`, `survival.psiBlockDuration`

All six appear in **Survival → Consumable Effects**, within the existing `ConsumableMixer` family. Magnitudes are direct `set` writes to raw effect `ValueMin` and `ValueMax`; durations are direct seconds `set` writes, preserving original `KeepNew`. The shared `OVERRIDE LIMITS` preset flag sets the medicine editor ceiling to 900s when ON and preserves their individual native caps when OFF; it does not add a CFG boolean. In-game acceptance is still pending.
