# Vanilla Preset Marker Audit

Source of truth: the fingerprinted runtime STALKER 2 2.0.5 CFG dump, with `GameData/DifficultyPrototypes.cfg` as the primary source for difficulty-dependent markers.

This audit checks whether the UI's single `VANILLA` marker is hiding a real Novice / Stalker / Veteran / Master difference.

## v0.1.63 result

Current catalog:

- visible controls: **167**;
- numeric sliders: **159**;
- toggles: **7**;
- discrete choice sliders: **1**;
- controls with one shared `All` vanilla baseline: **122**;
- controls carrying explicit Novice / Stalker / Veteran / Master baselines: **45**.

Every visible control belongs to one of those two vanilla-reference models; there are no unclassified marker baselines.

## Shared marker on selected difficulty fields

Only three shared-`All` controls directly patch the selected `DifficultyPrototypes.cfg` baseline:

| UI control | CFG field | Novice / Easy | Stalker / Medium | Veteran / Hard | Master / Stalker |
|---|---|---:|---:|---:|---:|
| Human Attack Frequency | `NPCCombatDifficulty.NPC_AttackCooldown` | 1.0 | 1.0 | 1.0 | 1.0 |
| Human Enemy Fire Accuracy | `NPCCombatDifficulty.HipAccuracyMultiplier` | 1.0 | 1.0 | 1.0 | 1.0 |
| Mutant Attack Frequency | `MutantCombatDifficulty.Mutant_AttackCooldown` | 1.0 | 1.0* | 1.0 | 1.0 |

`*` Medium does not write its own `MutantCombatDifficulty` block and inherits the neutral value `1.0` from its source chain.

These three controls should therefore keep a single shared `VANILLA` marker. They are editable because the fields can be overridden, not because vanilla uses them to distinguish difficulty presets.

For the two **Attack Frequency** controls, the table shows the vanilla raw cooldown field (`1.0`). The UI uses an intuitive frequency multiplier and writes its reciprocal: `200% frequency -> 0.50x cooldown`, `50% frequency -> 2.00x cooldown`.

## Explicit four-preset values that render at the same position

Some controls intentionally carry four explicit difficulty references even when the current runtime values happen to be identical. The Economy examples below are all `1.0` on all four vanilla baselines, so their four markers correctly overlap visually:

- Consumable Cost
- Ammo Cost
- Armor Cost
- Weapon Cost
- Artifact Cost
- Repair Cost

Keeping the four source values is still useful because a later game patch can diverge them without changing the data model.

## Global/shared controls

The remaining shared-`All` controls target global prototypes/variables rather than a selected difficulty prototype. Examples include player vitality values, individual consumable/effect values, anomaly base effects, many stamina values, carrying values and other global systems. Their source contains one shared vanilla value, so inventing four difficulty markers would be misleading.

The 13 aggregate Loot concepts removed in v0.1.62 are not part of this marker audit because they are no longer visible controls.

## Cross-checks

The v0.1.63 baseline audit independently verified:

- the four vanilla difficulty/economy tables across **34 CFG fields** with **0 mismatches**;
- **346** direct `set` vanilla-marker-to-writer-source comparisons with **0 mismatches** after correcting the stale Low Bleeding Damage marker from `0.33` to the runtime value `1.0`;
- **699/699** ordinary writer edit points against the exact runtime source path and value.

## Conclusion

There is currently no evidence that a shared `VANILLA` marker in the visible catalog is masking a real four-difficulty difference. Marker semantics remain separate from runtime behavior: a correct vanilla marker proves source alignment, not by itself the in-game effect of changing that control.
