# Difficulty Corpse SmartLoot — Deep Audit

This audit verifies the existing **Extra NPC Corpse Loot** control (`loot.npcCorpseBonus`) end to end. It does not add a new gameplay control and does not change runtime writer logic.

## Verdict

**DEEP AUDITED / EXACT 1:1 SET-LIST CONTROL.** The UI does not synthesize a generic corpse-loot multiplier. It selects one of the three exact source states observed at `DifficultyPrototypes.EconomyDifficulty.CorpseSmartLoot.GeneralNPC` and writes that list only to the selected vanilla difficulty prototype.

## Exact source surface

| Player difficulty | Runtime SID | Vanilla `GeneralNPC` SmartLoot list | Source edges |
| --- | --- | --- | ---: |
| Novice | `Easy` | `NPC_Medicine_Smart`, `NPC_Ammo_Smart` | 2 |
| Stalker | `Medium` | `NPC_Water_Smart` | 1 |
| Veteran | `Hard` | none | 0 |
| Master | `Stalker` | none | 0 |

The mapped runtime graph contains exactly **3** Difficulty corpse-SmartLoot edges, all for `GeneralNPC`. No other `CorpseSmartLoot` target or consumer class is present in the supplied graph.

## Control-state proof

| Slider state | Serialized value | Exact writer effect when modified | Vanilla marker |
| --- | --- | --- | --- |
| Medicine + Ammo | `NPC_Medicine_Smart, NPC_Ammo_Smart` | replace `CorpseSmartLoot` with a non-bpatch subtree containing `GeneralNPC = NPC_Medicine_Smart, NPC_Ammo_Smart` | Novice |
| Water | `NPC_Water_Smart` | replace `CorpseSmartLoot` with a non-bpatch subtree containing `GeneralNPC = NPC_Water_Smart` | Stalker |
| None | empty string | emit `CorpseSmartLoot =` so an existing/inherited corpse bonus is cleared | Veteran / Master |

The `choiceSlider` derives these three unique values directly from the four vanilla difficulty entries in `parameters.json`; Veteran and Master collapse to the same `None` choice. The control therefore has no fourth synthetic state.

## Package isolation

| SmartLoot package | Parent | Rank blocks | Group rows | Item rows | Unique items | Mapped consumers |
| --- | --- | ---: | ---: | ---: | ---: | ---: |
| `NPC_Medicine_Smart` | `empty` | 4 | 11 | 13 | 4 | 1 |
| `NPC_Ammo_Smart` | `empty` | 4 | 24 | 43 | 18 | 1 |
| `NPC_Water_Smart` | `empty` | 4 | 4 | 4 | 1 | 1 |

All three packages inherit only from the SmartLoot `empty` sentinel and have **no mapped non-difficulty consumers**. Selecting one here therefore does not alter world-container SmartLoot, destructible SmartLoot, trader stock, or any ItemGenerator definition.

Across the three packages there are **12 rank blocks, 39 exact group rows, 60 exact item rows, and 23 unique item SIDs**. The control changes none of those rows; it only chooses which package SID list is attached to human NPC corpses for the selected difficulty.

## Package semantics

- `NPC_Medicine_Smart`: 11 `ConsumablesParams` groups / 13 item rows across all four progression ranks; items are `Medkit`, `ArmyMedkit`, `Bandage`, and `Antirad`.
- `NPC_Ammo_Smart`: 24 `PrimaryWeaponParams` groups / 43 item rows across all four progression ranks; ammo eligibility is caliber-aware and uses `MainWeaponAmmoCount`, `PriorityCaliber`, item weights, and count ranges.
- `NPC_Water_Smart`: 4 `ConsumablesParams` groups / 4 item rows; each progression rank resolves to `Water`, count `1..1`.

The package internals are progression-rank-aware. Changing the slider does **not** flatten or rewrite those rank blocks.

## Separation from other corpse systems

This control does **not** edit:

- `SpawnActorPrototypes.CorpseStashSID` ItemGenerator overrides;
- standard human `GeneralNPC_*` ItemGenerator loadouts;
- shared NPC consumable pools;
- mutant loot generators;
- world `StashPrototypeSID` SmartLoot consumers.

That separation is structurally important: `CorpseStashSID` is an ItemGenerator reference, while this control is a Difficulty -> SmartLoot package-list reference. They can coexist on the same corpse without being the same loot layer.

## Writer safety check

`CfgPatchWriterService.WriteCorpseSmartLoot` is specialized rather than using the ordinary scalar writer. For a non-empty choice it removes any pending `CorpseSmartLoot` child/assignment and recreates the subtree without `{bpatch}`, which replaces that small optional subtree. For `None` it emits an empty `CorpseSmartLoot` assignment. This is required so choosing `None` can remove a vanilla/inherited corpse SmartLoot package instead of merely omitting a patch.

Only the selected Base Preset runtime SID is targeted. If the selected value equals that difficulty's vanilla value, the parameter is not modified and no corpse-SmartLoot patch is emitted.

## Control-safety conclusion

The existing control is approved as **1:1 at the set-list level**: each UI state corresponds to an exact source list state, and the writer changes only that list on one selected difficulty prototype. It should remain a fixed-choice slider, not be converted into a percentage/abundance slider.

## Machine-readable companions

- `difficulty-corpse-smartloot-edges.csv` — original three source edges.
- `smartloot-corpse-bonus-prototype-inventory.csv` — three package-level summaries.
- `smartloot-corpse-bonus-group-rows.csv` — all 39 exact package group rows.
- `smartloot-corpse-bonus-item-rows.csv` — all 60 exact package item rows.
- `smartloot-corpse-bonus-consumer-edges.csv` — all mapped consumers for these packages.
- `smartloot-corpse-bonus-control-states.csv` — exact UI/source/writer state mapping.
- `smartloot-corpse-bonus-domain-summary.json` — compact audit summary.
