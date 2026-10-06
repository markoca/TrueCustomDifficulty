# Standard Human NPC — Junk / Miscellaneous Deep Audit

**Source:** runtime STALKER 2 2.0.5 CFG dump.  
**Purpose:** determine whether the `EItemGenerationCategory::Junk` surface in standard-human loadouts represents tunable player loot or a gameplay/system marker.  
**Control-design status:** **DEEP AUDITED — EXCLUDE FROM LOOT TUNING.**

## Executive finding

In the standard-human NPC surface, `Junk` does **not** represent a miscellaneous player-loot pool. Every relevant `Junk` entry generates the same SID: `GuardQuestItem`. The item is an invisible, weightless `Info` item and is used by world contextual-action actors as an `ItemInInventory` precondition. It therefore functions as a guard/NPC interaction marker and must be preserved unchanged.

Do **not** create a generic `NPC Junk Drop Chance`, `NPC Misc Loot`, or `GuardQuestItem Chance` control from these rows. Do **not** scale `Junk` categories globally: elsewhere in the generator graph the same enum contains unrelated consumables, grenades, ammo, weapons, artifacts and other items.

## Item-prototype evidence

| Property | Runtime value | Meaning for control design |
| --- | --- | --- |
| `SID` | `GuardQuestItem` | The only `Junk` item in the standard-human guard surface. |
| `IsQuestItem` | `false` | Despite the name, it is not configured as a normal quest item. |
| `IsQuestItemPrototype` | `false` | Not a quest-item prototype. |
| `Invisible` | `true` | Not intended as visible inventory loot. |
| `Type` | `EItemType::Info` | System/info item, not normal equipment or consumable loot. |
| `RequireWeight` | `false` | No inventory-weight role. |
| `ShouldTriggerAnomalies` | `false` | No normal world-item anomaly interaction. |

Runtime definition: `GameData/ItemPrototypes.cfg`, `GuardQuestItem` block around lines 20565–20576 in the audited dump.

## Generator-surface summary

| Surface | Rows | Interpretation |
| --- | ---: | --- |
| Strict standard-human loadout definitions | 31 | Direct guard-loadout marker rows. |
| Shared definitions that also affect standard-human NPCs | 4 | Marker rows also used by another context; still not loot targets. |
| **Total standard-human-relevant marker rows** | **35** | All generate exactly one `GuardQuestItem`. |
| Named/scripted DLC1 generator rows using same marker | 9 | Confirms the SID is a broader interaction marker, not a standard-NPC loot commodity. |
| **All effective generator rows containing `GuardQuestItem`** | **44** | Base + DLC1. |

Every one of these generator rows uses `Chance = 1`, `MinCount = 1`, `MaxCount = 1`. The standard-human rows have no `Diff`, `PlayerRank`, or reputation gate.

## Standard-human-relevant generator rows

| Generator | Classification | Impact contexts | Chance | Count | CFG line |
| --- | --- | --- | ---: | ---: | ---: |
| `GuardNPC_Freedom_CloseCombat_ItemGenerator` | Shared / cross-context | human_npc_named, human_npc_standard | 1 | 1–1 | 47992 |
| `GuardNPC_Freedom_Recon_ItemGenerator` | Shared / cross-context | human_npc_named, human_npc_standard | 1 | 1–1 | 48102 |
| `GuardNPC_Freedom_Stormtrooper_ItemGenerator` | Shared / cross-context | human_npc_named, human_npc_standard | 1 | 1–1 | 48276 |
| `GuardNPC_Neutral_Recon_ItemGenerator` | Shared / cross-context | corpse_override, human_npc_standard | 1 | 1–1 | 46253 |
| `GuardNPC_Bandit_CloseCombat_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 46658 |
| `GuardNPC_Bandit_Heavy_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 46918 |
| `GuardNPC_Bandit_Recon_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 46762 |
| `GuardNPC_Bandit_Stormtrooper_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 46858 |
| `GuardNPC_Corpus_CloseCombat_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 49382 |
| `GuardNPC_Corpus_Heavy_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 49852 |
| `GuardNPC_Corpus_Recon_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 49492 |
| `GuardNPC_Corpus_Sniper_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 49778 |
| `GuardNPC_Corpus_Stormtrooper_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 49660 |
| `GuardNPC_Duty_CloseCombat_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 47516 |
| `GuardNPC_Duty_Heavy_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 47918 |
| `GuardNPC_Duty_Recon_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 47589 |
| `GuardNPC_Duty_Sniper_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 47858 |
| `GuardNPC_Duty_Stormtrooper_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 47740 |
| `GuardNPC_Freedom_Sniper_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 48421 |
| `GuardNPC_Monolith_CloseCombat_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 47008 |
| `GuardNPC_Monolith_Recon_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 47118 |
| `GuardNPC_Monolith_Sniper_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 47442 |
| `GuardNPC_Monolith_Stormtrooper_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 47331 |
| `GuardNPC_Neutral_CloseCombat_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 46046 |
| `GuardNPC_Neutral_Sniper_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 46399 |
| `GuardNPC_Neutral_Stormtrooper_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 46584 |
| `GuardNPC_Noon_CloseCombat_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 48998 |
| `GuardNPC_Noon_Recon_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 49093 |
| `GuardNPC_Noon_Sniper_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 49308 |
| `GuardNPC_Noon_Stormtrooper_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 49219 |
| `GuardNPC_Varta_CloseCombat_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 48495 |
| `GuardNPC_Varta_Heavy_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 48930 |
| `GuardNPC_Varta_Recon_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 48605 |
| `GuardNPC_Varta_Sniper_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 48871 |
| `GuardNPC_Varta_Stormtrooper_ItemGenerator` | Strict standard-human | human_npc_standard | 1 | 1–1 | 48756 |

### Strict standard-human prototypes without this marker

The marker is **not** a universal property of every standard-human generator. These strict standard-human prototypes do not contain a `GuardQuestItem` row:

- `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_AKU`
- `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_Obrez`
- `GeneralNPC_MIK_Armor`
- `GeneralNPC_MIK_CloseCombat_ItemGenerator`
- `GeneralNPC_MIK_Recon_ItemGenerator`
- `GeneralNPC_MIK_Sniper_ItemGenerator`
- `GeneralNPC_MIK_Stormtrooper_ItemGenerator`
- `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog`
- `GeneralNPC_Neutral_Sniper_ItemGeneratorCustom`
- `GeneralNPC_SIRCAA_Scientist_Recon_ItemGenerator`

This matches the observed naming/use pattern: the marker is concentrated in `GuardNPC_*` loadouts rather than being a generic inventory item for every human NPC.

## Runtime consumer evidence

`SpawnActorPrototypes.cfg` contains **40** uses of `GuardQuestItem`. All 40 are `ESpawnType::ContextualAction` actors and all 40 require `EContextualActionPreconditionType::ItemInInventory` with `ItemPrototypeSID = GuardQuestItem`.

| Contextual action | GuardQuestItem-gated actors |
| --- | ---: |
| `CA_sit_ground` | 10 |
| `CA_sit_chair` | 6 |
| `CA_stand` | 5 |
| `CA_sit_ground_detector` | 3 |
| `CA_sit_ground_squat` | 3 |
| `CA_lie_ground` | 2 |
| `CA_sit_barrel` | 2 |
| `CA_stand_againstwall_back` | 2 |
| `CA_stand_againstwall_right` | 2 |
| `CA_lie_ground_left` | 1 |
| `CA_sit_ground_weapon` | 1 |
| `CA_sit_table_junk_left_in` | 1 |
| `CA_stand_againstwall` | 1 |
| `CA_stand_barrel` | 1 |

### Complete contextual-action reference table

| Actor SID | Action | Spawn type | Precondition | Level | CFG line |
| --- | --- | --- | --- | --- | ---: |
| `023F5FDD46A165CEA0DED8991AFC1085` | `CA_sit_ground` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 931670 |
| `024B06E34F957D6935075B81A228A636` | `CA_sit_chair` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 932186 |
| `049D540F47A8501C2FD14AA260903CC5` | `CA_stand_againstwall` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 963381 |
| `062FDAC6405FFE64BBC30594725D17C2` | `CA_sit_chair` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 985122 |
| `0F2769BA4A249091A4B1A493F98BF7BB` | `CA_lie_ground` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 1110853 |
| `138B773C44A2F4275E70D199AD9D6A25` | `CA_stand` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 1166027 |
| `1695777F4D91C52B3B3D9289F23C9B0F` | `CA_sit_chair` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 1213171 |
| `17FEDB0D41622BE7832F8FBC2D99E6F7` | `CA_sit_ground` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 1233377 |
| `1B1464E54D7BF9F07AB3BE8534A7BA11` | `CA_sit_ground_squat` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 1277689 |
| `240688814BEB35148EB14DA0B0883CDB` | `CA_sit_ground_squat` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 1409157 |
| `26B9A21D426285C146597E8447C31939` | `CA_lie_ground` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 1449495 |
| `27E2C9A544646D0643FAACBEF91C1AB2` | `CA_sit_chair` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 1464808 |
| `2B0C64A84195943188B7F4A2D84F8EEC` | `CA_sit_barrel` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 1510779 |
| `2D99FB06449F60E2427C63AA701C5DA8` | `CA_sit_barrel` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 1549684 |
| `308E45914CE81BCC0878C2B69A6AFB0A` | `CA_sit_ground` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 1590552 |
| `328093C64D9E262A9087929F2F528515` | `CA_stand` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 1620388 |
| `4ECCDC1E45A5A6A6B4ACF0A74723BBB7` | `CA_sit_ground_detector` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 2007630 |
| `6787FB2342C7A1F7AB561D93EB4B501A` | `CA_sit_ground_detector` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 2352311 |
| `7A42088C472572FCBEAD8D903EA8939C` | `CA_lie_ground_left` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 2612247 |
| `7AF380654F25F9F35B32D49A67C0CB60` | `CA_sit_chair` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 2621850 |
| `80642A7F402BAAF7318CBFB52CCA98C6` | `CA_sit_ground` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 2696148 |
| `825D714C4283B80D0130D78DF0B037CD` | `CA_sit_ground_detector` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 2723313 |
| `86D9E9124B4E655A24816FB0E7BFE71B` | `CA_stand_againstwall_right` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 2787944 |
| `8F50FCEC4DA6A1CB953066A8985B98CA` | `CA_sit_ground` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 2911722 |
| `97E905DC45E79F9FFF4B7381BBAE6810` | `CA_stand_againstwall_back` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 3035217 |
| `B04064774D18753F3D07D2BDAF34EBE8` | `CA_stand_againstwall_back` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 3375512 |
| `B44FF262406743DA48D401B3AA05A020` | `CA_sit_ground` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 3433926 |
| `B80996C64FF9E1C7B407F39F60413758` | `CA_sit_ground` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 3482792 |
| `B8A5AA644A3D3CD427DD828515BB491C` | `CA_stand_againstwall_right` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 3492835 |
| `B8A8EB2E41D3F51CC41970B483065BA4` | `CA_sit_ground_squat` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 3492968 |
| `BD78F1DE4B44C1DBAAE14B85CD6B7E37` | `CA_stand_barrel` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 3562382 |
| `BE1A3EE34F7AF018FF5AE1A29B279EC7` | `CA_sit_table_junk_left_in` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 3572444 |
| `C093DF4940FFACA0862CCFB7C430B982` | `CA_sit_chair` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 3611818 |
| `C1D002364EA300EDE6BB158083E2C051` | `CA_stand` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 3628756 |
| `C93314954BAA8F69DF3D9FA519795E48` | `CA_sit_ground` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 3733882 |
| `CAE607A345BAF6E70616FEB1CD3D8B67` | `CA_sit_ground_weapon` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 3756982 |
| `CD275BCA4F6A882C092799B925641628` | `CA_stand` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 3785608 |
| `E0BF31004421D9E10448FE8DE5AF2B01` | `CA_sit_ground` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 4240500 |
| `E41854744B5CE688942F1FA26D366006` | `CA_sit_ground` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 4350097 |
| `E8DA442D45FB1085286C27AFDF0D363C` | `CA_stand` | `ESpawnType::ContextualAction` | `EContextualActionPreconditionType::ItemInInventory` | `WorldMap_WP` | 4420848 |

## Important warning about the `Junk` enum

Across the **entire** effective Item Generator graph, `EItemGenerationCategory::Junk` appears in **566 rows across 276 generators**, with **187 distinct direct item SIDs** (plus 1 nested-generator SID). Therefore the enum name is not a safe gameplay classifier by itself.

Examples of item SIDs observed under `Junk` elsewhere in the graph:

| Item SID | Effective `Junk` rows |
| --- | ---: |
| `Vodka` | 44 |
| `GuardQuestItem` | 44 |
| `Medkit` | 25 |
| `ArmyMedkit` | 16 |
| `GrenadeRGD5` | 16 |
| `Sausage` | 14 |
| `CannedFood` | 14 |
| `Bandage` | 13 |
| `Energetic` | 11 |
| `A545D` | 11 |
| `Bread` | 10 |
| `GrenadeF1` | 10 |
| `AntiRad` | 9 |
| `EcoMedkit` | 8 |
| `A012A` | 5 |
| `Water` | 5 |
| `Cinnamon` | 5 |
| `A918D` | 5 |
| `PSYBlocker` | 5 |
| `Hercules` | 5 |

That is why future controls must target an audited generator/item allowlist rather than `Category == Junk`.

## Control-safety decisions

| Candidate idea | 1:1 with intended gameplay concept? | Decision |
| --- | --- | --- |
| Standard NPC `Junk` drop chance | **No** | **Do not implement.** In this surface `Junk` is a hidden interaction marker, not player-facing junk loot. |
| `GuardQuestItem` chance/count | Technically direct fields, but wrong gameplay target | **Never expose as difficulty/loot tuning.** Preserve vanilla `1 / 1–1`. |
| Global `Junk` category multiplier | **No** | **Explicitly forbidden.** The enum contains many unrelated gameplay item types elsewhere. |
| Preserve marker while cloning/rewiring guard generators | **Yes** | **Required.** Any future NPC generator clone/rewire must carry this row unchanged where the source generator has it. |

## Audit decision

- **No player-facing control comes from this domain.**
- `GuardQuestItem` is treated as **system/interaction infrastructure**, not loot.
- Future standard-NPC generator rewrites must preserve the marker exactly when cloning a source loadout that contains it.
- Future category-wide logic must never assume `EItemGenerationCategory::Junk` means sellable junk or miscellaneous loot.
- Machine-readable companions: `npc-junk-standard-surface.csv`, `npc-junk-guardquest-generator-rows.csv`, `guardquest-contextual-action-preconditions.csv`.

