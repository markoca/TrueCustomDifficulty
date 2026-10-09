# Equipment Mapping

**Current Equipment UI (2026-10-09):** Two source-accurate sections, **Equipment Wear** (`equipment.armorDurability` and `equipment.weaponWearPerShot`) and **Item Weight** (`equipment.ammoWeight` and `equipment.consumableWeight`). The former is a compact, two-axis plot with independent bottom-circle/left-square handles and read-only crossing; the latter consists of two compact horizontal Stamina Usage-style tracks, not mixer faders. All four real ranges/steps, CFG routes and native sources below are unchanged. Full UI anatomy is locked in `UI-DESIGN-LOCK.md` / `VISUAL-CONTROL-SYSTEM.md`.

**Equipment bonus cap decision:** historical `RegenStamina.MaxValue` (stamina recovery), `DegenBleeding.MaxValue` (bleeding recovery), `AdditionalInventoryWeight.MaxValue` (carry bonus) and `PenaltyLessWeight.MaxValue` (encumbrance relief) are **audit-only / no Equipment UI**. They set upper bounds, but their actual stacking, gameplay effects and interpretation are not proven. They were already absent from the current four-source `Data/parameters.json`; do not add mock sliders, calculate derived effects, or modify unrelated CFG outputs.

Source of truth for this pass: runtime STALKER 2 CFG dump 2.0.5.

The Equipment tab deliberately avoids protection-cap tuning. The protection caps previously removed from the UI remain excluded. This pass focuses on the confirmed difficulty armor-wear control, player-weapon wear, and confirmed item-weight controls.

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

The base-game runtime file contains 41 `*_Player` entries with explicit `DurabilityDamagePerShot`: 40 are non-zero and `UA_GLaunch_Player` is explicitly `0.0`. The DLC1 file contains 11 non-zero player entries. Values differ by weapon. Examples from the supplied runtime dump include:

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

Quest/special consumable variants that inherit ordinary food weights are resolved to their effective vanilla value and patched explicitly. The control changes only item `Weight`; healing, hunger, radiation and other consumable effects are not modified.



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
