# Equipment Mapping

### 2026-10-11 — Frameless knobs, Equipment in Survival, expanded drink strengths (current)

**All rotary knobs:** The common `AggregateVisualControl.DrawSourceKnob` renders every native 270° knob without a rectangular card fill or border. Preserve dial face, selected/hover highlighting, native source-grid ticks, left/right limits, current value, neutral reference marker, pointer/wheel/keyboard interaction and right-click reset. Applies to Your Weapons, Radiation System, Consumable Effects, Ammo Family Weights, Economy, Trader Stock and all other consumers of this renderer. Do not add extra frames, filler text or full-width control backgrounds.

**Equipment now lives in Survival:** Remove the separate Equipment navigation tab. Under Survival place the two original groups after Bleeding: **Equipment Wear** keeps its compact two-axis original armor durability (`equipment.armorDurability`) and player weapon wear per shot (`equipment.weaponWearPerShot`) editor; **Item Weight** keeps the two original source-native weight controls (`equipment.ammoWeight`, `equipment.consumableWeight`). Only the catalog UI tab assignment for those four changes from `equipment` to `survival`. Preserve source IDs, values, order within each group, steps, reference handling, CFG targets, writer operations and preset compatibility. No duplicate equipment tab and no duplicate controls.

**Alcohol Tolerance:** Beer Strength (`survival.beerDrunknessStrength`) now spans **8–40** in **1** point increments (vanilla **15**); Vodka Strength (`survival.vodkaDrunknessStrength`) spans **15–80** in **5** point increments (vanilla **30**). The native catalog controls both real editing bounds and the DrunkennessControl preview/labels. These maxima are user tuning limits only; original DrunknessMechanics stage thresholds and the beer/vodka CFG effect targets remain unchanged.

This active design supersedes historical rectangular knob banks, Equipment tab navigation and 30/60 drink strength ceilings.

**Original Equipment UI (2026-10-09; now hosted in Survival):** Two source-accurate sections, **Equipment Wear** (`equipment.armorDurability` and `equipment.weaponWearPerShot`) and **Item Weight** (`equipment.ammoWeight` and `equipment.consumableWeight`). The former is a compact, two-axis plot exactly matching Combat's three circular handles: bottom X controls armor toughness, left Y controls per-shot weapon wear, and the now-interactive center ring edits both real source values by X/Y drag; the latter consists of two compact horizontal Stamina Usage-style tracks, not mixer faders. All four real ranges/steps, CFG routes and native sources below are unchanged. Full UI anatomy is locked in `UI-DESIGN-LOCK.md` / `VISUAL-CONTROL-SYSTEM.md`.

**Equipment bonus cap decision:** historical `RegenStamina.MaxValue` (stamina recovery), `DegenBleeding.MaxValue` (bleeding recovery), `AdditionalInventoryWeight.MaxValue` (carry bonus) and `PenaltyLessWeight.MaxValue` (encumbrance relief) are **audit-only / no Equipment UI**. They set upper bounds, but their actual stacking, gameplay effects and interpretation are not proven. They were already absent from the current four-source `Data/parameters.json`; do not add mock sliders, calculate derived effects, or modify unrelated CFG outputs.

Source of truth for this pass: runtime STALKER 2 CFG dump 2.0.5.

The relocated Equipment controls in Survival deliberately avoid protection-cap tuning. The protection caps previously removed from the UI remain excluded. This pass focuses on the confirmed difficulty armor-wear control, player-weapon wear, and confirmed item-weight controls.

## 1. Armor Wear

### Armor Difficulty Durability Scalar

- File: `GameData/DifficultyPrototypes.cfg`
- Field: `EnvironmentDifficulty.Armor_Durability`

| Difficulty | Value |
|---|---:|
| Novice | 1.35 |
| Stalker | 1.00 |
| Veteran | 0.90 |
| Master | 0.60 |

This is the existing difficulty-profile armor durability layer. The vanilla direction is clear: higher values are used on easier difficulties and make armor hold condition better; lower values are used on harder difficulties and make armor wear more readily. Stalker `1.00` is the neutral reference.

The dump does **not** expose the exact engine conversion from this scalar to condition loss per hit, so the UI does not call `1.35` “35% slower wear” or `0.60` “40% faster wear.” Treat it as a durability/toughness scalar, not a literal wear-speed percentage. It also does not change armor protection stats.


## 2. Weapon Wear

### Player Weapon Wear Per Shot

Weapons do not use one universal player weapon-wear scalar. They expose individual values in:

- `GameData/WeaponData/CharacterWeaponSettingsPrototypes.cfg`
- `DLCGameData/DLC1/WeaponData/CharacterWeaponSettingsPrototypes.cfg`

Field:

`DurabilityDamagePerShot`

The base-game runtime file has 41 mapped `*_Player` entries with effective `DurabilityDamagePerShot`: 40 are non-zero and `UA_GLaunch_Player` is explicitly `0.0`. Of the 41, one (`GunPKP_Korshunov_MG_Player`) inherits its value from `GunPKP_MG_Player` through an explicit `{refkey=...}` chain. The DLC1 file contains 11 directly authored non-zero player entries. Values differ by weapon. Examples from the supplied runtime dump include:

- `GunUDP_HG_Player = 3.3`
- `GunAPB_HG_Player = 4.76`
- `GunViper_PP_Player = 0.69`
- `GunRhino_HG_Player = 6.67`
- `GunArevPrecise_AR_Player = 2.875`

The Equipment control is therefore an aggregate relative transform rather than a replacement value:

- 100% -> preserve every weapon's native `DurabilityDamagePerShot`
- 50% -> multiply every non-zero player value by 0.5
- 200% -> multiply every non-zero player value by 2.0
- zero-valued player entries remain zero

Example:

`GunUDP_HG_Player 3.3` becomes `1.65` at 50% or `6.6` at 200%.

This preserves the game's weapon-to-weapon wear differences.

### Relationship to jamming

The Combat tab's `Weapon Jamming` control remains a separate difficulty multiplier. Weapon wear affects how quickly a weapon reaches poor condition; it does not replace the jamming multiplier or rewrite each weapon's jamming thresholds.

Per-weapon `MinJamDurabilityThreshold` / `MaxJamDurabilityThreshold` values are intentionally not exposed in this pass because they vary by weapon and would require a more explicit aggregate design.



## 3. Item Weight

Source:

`GameData/ItemPrototypes.cfg`

DLC1 consumable source:

`DLCGameData/DLC1/ItemPrototypes.cfg`

### Ammo Weight

There is no single global ammo-weight scalar. The tool therefore resolves each concrete ammo prototype to its effective vanilla `Weight` (including inherited values) and scales all of them together.

Current runtime dump coverage:

- 34 concrete non-template base-game ammo prototypes with non-zero effective weight
- no additional DLC1 ammo family in the supplied DLC1 dump

The writer emits an explicit BPatch `Weight` for every mapped ammo prototype so inherited variants follow the same percentage reliably.

### Consumable Weight

The same aggregate approach is used for Food/Medicine consumables:

- 22 base-game Food/Medicine consumable prototypes with non-zero effective weight
- DLC1 `MonolithDryRation = 0.15`

Quest/special consumable variants that inherit ordinary food weights are resolved to their effective vanilla value and patched explicitly. The original archive audit confirmed `DvupalovVodka`, `EQ08_FreshBread` and `EQ82_konserva` use `refurl=ConsumablePrototypes.cfg` with an explicit `refkey`; their parents are physically present in the original consolidated `GameData/ItemPrototypes.cfg` and were resolved by exact source ancestry, not guessed. The control changes only item `Weight`; healing, hunger, radiation and other consumable effects are not modified.



## 5. Intentionally Excluded

The following protection caps remain intentionally absent from the Equipment UI:

- `ProtectionShock`
- `ProtectionStrike`
- `ProtectionBurn`
- `ProtectionChemical`
- `ProtectionPSY`
- `ProtectionRadiation`

They were previously removed by design and are not reintroduced by this pass.

Also left unexposed:

- `LowDurabilityThreshold = 5`
- `DurabilityRound = 0.5`
- per-weapon jamming durability thresholds
- per-item armor/weapon weights beyond the dedicated aggregate Ammo/Consumable weight controls
- armor protection values
- artifact protection caps

Those fields either do not represent a clean difficulty control or require a separate aggregate design before they can be presented honestly in the UI.
