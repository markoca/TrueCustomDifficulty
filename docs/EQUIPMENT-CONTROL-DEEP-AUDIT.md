# Equipment Control Deep Audit — v0.2.49

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

- **41** base-game `*_Player` entries;
- **40** non-zero `DurabilityDamagePerShot` values;
- one explicit zero: `UA_GLaunch_Player = 0.0`;
- **11** DLC1 non-zero player entries;
- **52** total writer edit points.

The additional base-game non-zero entry is `GunArevPrecise_AR_Player = 2.875`. This is an evidence/documentation correction only; the production writer was already targeting it.

## Deliberate boundaries

Protection caps remain outside the Equipment UI. Per-weapon jamming thresholds, arbitrary per-item weights, armor protection values, `LowDurabilityThreshold`, and `DurabilityRound` remain intentionally unexposed. `Armor Durability` remains runtime-gated because the direction and four difficulty values are proven, while the engine's exact scalar-to-condition-loss formula is not claimed.
