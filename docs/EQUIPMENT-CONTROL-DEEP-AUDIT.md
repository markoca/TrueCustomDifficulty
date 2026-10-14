# Equipment Control Deep Audit — v0.2.49

### 2026-10-14 — Equipment tab restored (CURRENT)

The separate **Equipment** tab is restored in JSON metadata and WPF grouping, after Economy / before Anomalies. Exactly four original `equipment.*` visible parameters now have `tab="equipment"`: two in Equipment Wear, two in Item Weight. Keep **3 confirmed + 1 runtime-test-required**, **113 unchanged writer points** (4 armor durability, 52 player weapon wear, 34 ammunition weight, 23 consumables weight) and all existing source provenance, difficulty values, native steps and presets. The stable-prefix audit additionally asserts tab ownership and group identity. Historical 2026-10-12 and 2026-10-11 Survival migration descriptions below no longer state current placement.

### 2026-10-12 — Current Equipment audit discovery after Survival tab migration

The four current `equipment.*` catalog parameter IDs remain `equipment.armorDurability`, `equipment.ammoWeight`, `equipment.consumableWeight`, and `equipment.weaponWearPerShot`, but their presentation `tab` is now `survival`. Project consistency audits must select the stable **parameter ID prefix**, not a retired `tab="equipment"` predicate: the old predicate returned no rows and crashed converting a missing vanilla difficulty value with `float(None)`. The corrected gate still requires 4 exposed controls, 3 confirmed + 1 runtime-test-required, exactly **113 edit points** (4 armor-durability + 34 ammo weight + 23 consumable weight + 52 weapon wear), and the source-matching armor difficulty values Novice 1.35 / Stalker 1 / Veteran 0.9 / Master 0.6. No source metadata, actual tab grouping, control, writer target, preset or vanilla values are altered. The old "expected 117" error text was obsolete and is corrected to 113.

### 2026-10-11 — Current UI placement, knobs, and alcohol ranges

- The separate **Equipment** tab is retired. Four original equipment source IDs are now presented inside **Survival**, preserving **Equipment Wear** (armor and weapon wear crosshair) and **Item Weight** (ammo and consumables source-native tracks) as independent groups, after the Bleeding controls. Original `equipment.armorDurability`, `equipment.weaponWearPerShot`, `equipment.ammoWeight`, `equipment.consumableWeight` remain unchanged in values, ranges, steps, CFG writer mappings and preset fields. Their `tab` metadata alone switches to `survival`.
- The shared 270° `DrawSourceKnob` renderer no longer draws rectangular background cards around rotary knobs anywhere. Existing dial arc, tick positions, units, hover/drag/wheel/keyboard and source-specific reset remain intact.
- Beer drink strength now has a native UI range **8–40** (1-point steps, vanilla 15); Vodka strength **15–80** (5-point steps, vanilla 30). Both are editable real `BeerDrunkness` / `VodkaDrunkness` effect values via the existing writer, not synthetic stage thresholds. The Drunkenness view takes slider bounds from the catalog.

This 2026-10-11 UI contract supersedes historical references to a separate Equipment tab and the prior Beer 30/Vodka 60 ceiling. The original mechanical/source audit remains valid.

> **Source archive evidence:** The 2026-10-09 uploaded GameData/DLC1 ZIPs match all 113 effective Equipment source values (103 physically authored exact, 10 explicitly inherited exact). Three of the inherited special consumables use `refurl=ConsumablePrototypes.cfg` with physically located parents in consolidated `ItemPrototypes.cfg`; see [Phase 11 evidence](evidence/EQUIPMENT-ORIGINAL-CFG-2026-10-09.json). Not in-game verified.

> **Phase 11 update (2026-10-09):** Executable current four-control source and writer gates are described in [Phase 11](DEEP-CFG-AUDIT-PHASE11-EQUIPMENT-2026-10-09.md). The live manifest has 113 points (101 base and 12 DLC1), not the historical eight-control 117. The phase's test results are tracked separately from original-source and gameplay acceptance.\n\n> **Current-surface note (2026-10-09):** This v0.2.49 eight-control audit is **historical**, not a statement about the current Equipment UI. The active `Data/parameters.json` contains four source controls: `equipment.armorDurability` (runtime-test-required), `equipment.weaponWearPerShot`, `equipment.ammoWeight` and `equipment.consumableWeight` (confirmed). The four bonus MaxValue fields listed in the old table (Stamina Recovery, Bleeding Recovery, Carry Weight Bonus and Encumbrance Relief) are now **audit-only**; their upper-limit function is known, but their actual player-facing behavior is insufficiently understood to expose as sliders. Preserve this audit record and the existing CFG mapping evidence, but do not recreate eight visible editors or claim the historical 117 edit-point number is the current four-control UI count. Current Equipment section layout: Equipment Wear dual-axis plot with Combat-parity circular X/Y/interactive center handles + two compact horizontal Item Weight sliders. See `UI-DESIGN-LOCK.md` and `VISUAL-CONTROL-SYSTEM.md`.

This release re-audits every normal **Equipment** control from player-facing metadata through the locked writer baseline to concrete CFG edit points. No new Equipment control is promoted and no existing control is removed.

## Result

- **8** normal Equipment controls.
- **7** statically confirmed.
- **1** remains `runtime_test_required`: `equipment.armorDurability`.
- **117** concrete writer edit points across the 8 controls.
- **0** invalid or partial normal-UI Equipment controls.

## Control-by-control contract

| Control | Status | Writer contract | Audit result |
|---|---|---|---|
| Armor Durability | runtime test required | 4 × `EnvironmentDifficulty.Armor_Durability` `set` | Vanilla 1.35 / 1.00 / 0.90 / 0.60 matches the difficulty CFG exactly; exact engine conversion to condition loss remains runtime-gated |
| Ammo Weight | confirmed | 34 × item `Weight` `scale` | 34 concrete non-template base-game ammo prototypes; relative vanilla weights preserved |
| Consumable Weight | confirmed | 23 × item `Weight` `scale` | 22 base-game Food/Medicine prototypes + DLC1 `MonolithDryRation`; relative vanilla weights preserved |
| Stamina Recovery Bonus Cap | confirmed | 1 × `RegenStamina.MaxValue` `set` | Direct equipment/effect stack cap; does not grant the effect |
| Bleeding Recovery Bonus Cap | confirmed | 1 × `DegenBleeding.MaxValue` `set` | Direct equipment/effect stack cap; does not grant the effect |
| Carry Weight Bonus Cap | confirmed | 1 × `AdditionalInventoryWeight.MaxValue` `set` | Direct equipment/effect stack cap; separate from base carry weight |
| Encumbrance Relief Cap | confirmed | 1 × `PenaltyLessWeight.MaxValue` `set` | Direct equipment/effect stack cap; does not grant relief by itself |
| Player Weapon Wear Per Shot | confirmed | 52 × `DurabilityDamagePerShot` `scaleNonZero` | 41 base-game player prototypes + 11 DLC1; 40 + 11 non-zero values scale, explicit zero remains zero |

## Correction in v0.2.49

The production writer already contained the current weapon-wear surface, but the Equipment mapping and catalog note still described the older count as **40 base-game entries / 39 non-zero**. The audited writer baseline contains:

- **41** base-game `*_Player` entries (40 directly authored including the zero + one `GunPKP_Korshunov_MG_Player` inherited from `GunPKP_MG_Player`);
- **40** non-zero effective `DurabilityDamagePerShot` values;
- one explicit zero: `UA_GLaunch_Player = 0.0`;
- **11** DLC1 non-zero player entries;
- **52** total writer edit points.

The additional base-game non-zero entry is `GunArevPrecise_AR_Player = 2.875`. This is an evidence/documentation correction only; the production writer was already targeting it.

## Deliberate boundaries

Protection caps remain outside the Equipment UI. Per-weapon jamming thresholds, arbitrary per-item weights, armor protection values, `LowDurabilityThreshold`, and `DurabilityRound` remain intentionally unexposed. `Armor Durability` remains runtime-gated because the direction and four difficulty values are proven, while the engine's exact scalar-to-condition-loss formula is not claimed.
