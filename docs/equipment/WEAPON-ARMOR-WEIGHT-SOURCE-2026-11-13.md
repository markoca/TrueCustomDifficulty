# Weapon & Armor Weight — Original Source Inventory (2026-11-13)

Two native Item Weight controls reuse the existing per-ItemPrototype `Weight` scaler: `equipment.weaponWeight` and `equipment.armorWeight`, 0–2×, 0.01× steps, original 1×. They edit **112 + 12** nonzero weapon prototypes and **86 + 8** nonzero armor/helmet prototypes (base game + DLC1). The only omitted nonzero weapon is `GunPM_PDA_Tutorial_HG` (tutorial source). Template roots and zero-weight weapons remain unchanged. Values are resolved by traversing actual `refkey` ancestors in the supplied decoded STALKER 2 2.0.5 CFGs (2026-10-17).

Inventory source rows are in `src/TrueCustomDifficulty/Data/equipment-weapon-armor-weight-source.csv`. The active `cfg-writer-baseline.json` has one `scale` point per row, `nodePath=[]`, `key=Weight`, and `sourceRaw` equal to the effective vanilla weight. This writes concrete child overrides even for inherited source weights, avoiding partial multiplier coverage.

The targeted source is `GameData/ItemPrototypes.cfg` (both categories), and DLC1 `DLCGameData/DLC1/ItemPrototypes.cfg` only when installed. A weight of 1× writes nothing, 0× makes each nonzero mapped item 0 kg, and 2× doubles each source weight. Other equipment effects, upgrades, protection, weapon wear, item cost, trade inventories, and world/stash spawns are unaffected; item mass itself can affect existing encumbrance rules. Category-wide type membership includes real `GuardGun*` weapon variants and NPC/quest suits by explicit original `EItemType::Weapon` / `EItemType::Armor` inheritance. Player inventory availability of those special variants is not asserted.

`ItemWeightBoard` now shows 4 compact source cards in a 2×2 bank at ≥672 DIP or a 4×1 bank below, retaining the original horizontal track affordances, contextual help, independent right-click reset, keyboard arrows and family R reset. The editor does not add another visual family. Source and BPatch output parity must be checked with the actual Windows writer tests, followed by in-game DLC and load-order acceptance.

## Source boundaries

| Category | Base | DLC1 | Combined |
| --- | ---: | ---: | ---: |
| Weapons | 112 | 12 | 124 |
| Armor + helmets | 86 | 8 | 94 |
| **Total** | **198** | **20** | **218** |

The baseline expands from 1230 to 1448 direct edit points. A prior audit of **113** base weapon definitions included the tutorial-only gun, intentionally excluded here. `MeleeStub`, weapon template prototypes, grenade-launcher data prototypes, `Gun_SkifGun_HG` and other original zero-weight sources are not assigned a new artificial weight. 