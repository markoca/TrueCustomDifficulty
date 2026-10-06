# Corpse Override Generators — Deep Audit

This audit maps every `SpawnActorPrototypes.CorpseStashSID` edge and the complete ItemGenerator graph reachable from those corpse overrides. It does not add or change runtime controls.

## Evidence boundary

- A corpse override is an `ESpawnType::DeadBody` SpawnActor row with `CorpseStashSID` pointing at an **ItemGenerator SID**.
- `CorpseStashSID` is kept separate from `DifficultyPrototypes.EconomyDifficulty.CorpseSmartLoot`; they are different loot systems.
- Exact patch coordinates come from `generator-local-source-*`; effective/reachable tables are used only for blast-radius analysis.
- `Chance` and `Weight` are not treated as interchangeable mechanics.
- A target that is also used by living NPCs, zombies, quests, traders, mutants, or world containers is not approved for corpse-only target-definition edits.

## Result

| Metric | Count |
| --- | ---: |
| Physical `CorpseStashSID` DeadBody edges | 99 |
| Base-game SpawnActor edges | 57 |
| DLC1 SpawnActor edges | 42 |
| Distinct direct target ItemGenerator SIDs | 39 |
| Exact locally authored rows in those direct roots | 602 |
| Direct roots with **corpse-only** propagated impact | 8 |
| Direct roots shared with other gameplay contexts | 31 |
| All generator nodes touched by the propagated corpse graph | 137 |
| Corpse-only nodes in that full graph | 18 |
| Corpse-only internal dependencies | 10 |

`CorpseStashSID` is therefore a **per-corpse override hook**, not one common loot pool. The 99 physical edges fan into 39 direct generator roots, and 31 of those roots also affect at least one non-corpse gameplay context. A global “corpse generator loot amount/chance” scalar is not supported by this structure.

## The 8 corpse-only direct roots

Only eight direct target roots have propagated impact restricted to `corpse_override`. Even these are not one uniform mechanic: five are full faction/rank loadout roots, while three are bespoke/scripted corpse payloads.

| Generator | DeadBody refs | Local rows | Local structure | Reachable items | Decision |
| --- | ---: | ---: | --- | ---: | --- |
| `DutyNewbieItemGenerator` | 1 | 14 | BodyArmor, Head, SubItemGenerator | 150 | REVIEW — corpse-only root; local fields are isolated, but this is a composite loadout rather than one abundance scalar |
| `FreedomExperiencedItemGenerator` | 1 | 14 | BodyArmor, Head, SubItemGenerator | 158 | REVIEW — corpse-only root; local fields are isolated, but this is a composite loadout rather than one abundance scalar |
| `GarbageDetCenterCorpseItemGenerator` | 1 | 1 | Junk | 1 | EXCLUDE — scripted/key corpse payload; not generic loot |
| `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog_Medkit` | 1 | 6 | BodyArmor, Consumable, Head, WeaponPrimary | 6 | EXCLUDE — Prolog-specific authored corpse loadout |
| `MonolithVeteranItemGenerator` | 4 | 11 | BodyArmor, SubItemGenerator | 155 | REVIEW — corpse-only root; local fields are isolated, but this is a composite loadout rather than one abundance scalar |
| `ScientistExperiencedItemGenerator` | 1 | 11 | BodyArmor, SubItemGenerator | 165 | REVIEW — corpse-only root; local fields are isolated, but this is a composite loadout rather than one abundance scalar |
| `SparkVeteranItemGenerator` | 1 | 11 | BodyArmor, SubItemGenerator | 155 | REVIEW — corpse-only root; local fields are isolated, but this is a composite loadout rather than one abundance scalar |
| `YantarDeadBody_Spark_ItemGenerator` | 1 | 19 | Artifact, Detector, SubItemGenerator, WeaponPrimary | 31 | EXCLUDE — bespoke Yantar/Spark corpse loadout |

### What is actually isolated here

- `DutyNewbieItemGenerator`, `FreedomExperiencedItemGenerator`, `MonolithVeteranItemGenerator`, `ScientistExperiencedItemGenerator`, and `SparkVeteranItemGenerator` are corpse-only **roots**, so editing a field physically authored on those root rows can be corpse-scoped. However, they are composite loadouts: weapon-family selection uses `Weight`, while consumable/detector/artifact/attachment links use `Chance`, plus direct armor/head rows. That is not one honest abundance percentage.
- Their nested pool **definitions** must still be evaluated separately. Some roots call cross-context pools such as `AllAmmosNPCGenerator`, consumable, detector, artifact, or attachment families. Editing the root-local link is not the same blast radius as editing the shared target definition.
- `GarbageDetCenterCorpseItemGenerator` is a single scripted key payload (`Garbage_DetentionCenter_Key_Padlock`, `Chance=1`, count `1–1`) and must not become generic loot tuning.
- `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog_Medkit` is a Prolog-specific authored loadout with armor, fixed Medkit/Bandage counts, and a fixed AK-74 weapon/ammo setup.
- `YantarDeadBody_Spark_ItemGenerator` is a bespoke Yantar/Spark corpse loadout containing weapons plus shared pistol/armor/consumable/NVG pools, a grenade and an Echo detector.

## 10 corpse-only internal dependencies

These ten weapon-family helpers are only reached by the corpse-only roots in the supplied graph. They are structurally corpse-scoped dependencies, but their local rows are selection/condition/ammo structures rather than generic corpse abundance.

| Generator | Family | Fields | Direct items | Reachable items |
| --- | --- | --- | ---: | ---: |
| `ENCloseCombatWeaponExperiencedGenerator` | weapons | Weight, Durability, Ammo count | 4 | 4 |
| `ENMachineGunnerWeaponExperiencedGenerator` | weapons | Weight, Durability, Ammo count | 5 | 5 |
| `ENScoutWeaponExperiencedGenerator` | weapons | Weight, Durability, Ammo count | 3 | 3 |
| `ENSniperWeaponExperiencedGenerator` | weapons | Weight, Durability, Ammo count | 7 | 7 |
| `ENStormTrooperWeaponExperiencedGenerator` | weapons | Weight, Durability, Ammo count | 8 | 8 |
| `RUCloseCombatWeaponNewbieGenerator` | weapons | Weight, Durability, Ammo count | 3 | 3 |
| `RUMachineGunnerWeaponNewbieGenerator` | weapons | Weight, Durability, Ammo count | 4 | 4 |
| `RUScoutWeaponNewbieGenerator` | weapons | Weight, Durability, Ammo count | 2 | 2 |
| `RUSniperWeaponNewbieGenerator` | weapons | Weight, Durability, Ammo count | 5 | 5 |
| `RUStormTrooperWeaponNewbieGenerator` | weapons | Weight, Durability, Ammo count | 5 | 5 |

## All 39 direct corpse override targets

`Impact contexts` is the important safety column. Any row containing something besides `corpse_override` is a shared target definition and is excluded from corpse-only target edits unless a future feature intentionally wants every listed context.

| Generator | Refs | Consumers | Impact contexts | Content | Local rows | Reachable items | Verdict |
| --- | ---: | ---: | --- | --- | ---: | ---: | --- |
| `ARG01_QSBIG_Body1` | 1 | 1 | corpse_override, quest_scripted | mixed: ammo, artifacts, equipment, weapons | 14 | 20 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `BanditChernozem_ItemGenerator` | 1 | 1 | corpse_override, human_npc_named | mixed: armor, artifacts, equipment, weapons | 14 | 19 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `DLC01_MQ06_Zombie_Neutral_CloseCombat_ItemGenerator` | 1 | 1 | corpse_override, zombie_npc | mixed: armor, consumables, weapons | 27 | 19 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `DutyNewbieItemGenerator` | 1 | 1 | corpse_override | armor | 14 | 150 | REVIEW — corpse-only root; local fields are isolated, but this is a composite loadout rather than one abundance scalar |
| `EmptyItemGenerator` | 16 | 11 | corpse_override, human_npc_named, item_container_world, quest_scripted | empty/no items | 0 | 0 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `EmptyQuest` | 15 | 9 | corpse_override, quest_scripted, zombie_npc | empty/no items | 1 | 0 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `FreedomExperiencedItemGenerator` | 1 | 1 | corpse_override | armor | 14 | 158 | REVIEW — corpse-only root; local fields are isolated, but this is a composite loadout rather than one abundance scalar |
| `FreedomVeteranItemGenerator` | 2 | 1 | corpse_override, quest_scripted | armor | 11 | 155 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GarbageDetCenterCorpseItemGenerator` | 1 | 1 | corpse_override | junk | 1 | 1 | EXCLUDE — scripted/key corpse payload; not generic loot |
| `GeneralNPC_Bandit_Heavy_ItemGenerator` | 1 | 1 | corpse_override, human_npc_named, human_npc_standard, zombie_npc | mixed: artifacts, equipment, weapons | 16 | 21 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralNPC_Bandit_Stormtrooper_ItemGenerator` | 1 | 1 | corpse_override, human_npc_named, human_npc_standard, quest_scripted, zombie_npc | mixed: artifacts, equipment, weapons | 14 | 19 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralNPC_MALACHITE_Scientist_Recon_ItemGenerator` | 1 | 1 | corpse_override, human_npc_standard | mixed: armor, equipment, weapons | 4 | 9 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralNPC_Mercenaries_CloseCombat_ItemGenerator` | 1 | 1 | corpse_override, human_npc_named, human_npc_standard, quest_scripted, zombie_npc | mixed: artifacts, equipment, weapons | 15 | 20 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralNPC_Militaries_CloseCombat_ItemGenerator` | 3 | 2 | corpse_override, human_npc_named, human_npc_standard, zombie_npc | mixed: artifacts, equipment, weapons | 10 | 21 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralNPC_Militaries_Heavy_ItemGenerator` | 4 | 2 | corpse_override, human_npc_named, human_npc_standard, zombie_npc | mixed: artifacts, equipment, weapons | 22 | 25 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralNPC_Militaries_Recon_ItemGenerator` | 3 | 2 | corpse_override, human_npc_named, human_npc_standard, zombie_npc | mixed: artifacts, equipment, weapons | 20 | 24 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralNPC_Militaries_Sniper_ItemGenerator` | 4 | 2 | corpse_override, human_npc_named, human_npc_standard, zombie_npc | mixed: artifacts, equipment, weapons | 13 | 23 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralNPC_Militaries_Stormtrooper_ItemGenerator` | 2 | 2 | corpse_override, human_npc_named, human_npc_standard, zombie_npc | mixed: artifacts, equipment, weapons | 23 | 24 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog_Medkit` | 1 | 1 | corpse_override | mixed: armor, consumables, weapons | 6 | 6 | EXCLUDE — Prolog-specific authored corpse loadout |
| `GeneralNPC_Varta_Recon_ItemGenerator` | 2 | 2 | corpse_override, human_npc_named, human_npc_standard, zombie_npc | mixed: artifacts, equipment, weapons | 24 | 23 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralZombie_Militaries_CloseCombat_ItemGenerator` | 1 | 1 | corpse_override, zombie_npc | mixed: armor, artifacts, equipment, weapons | 10 | 21 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralZombie_Militaries_Heavy_ItemGenerator` | 1 | 1 | corpse_override, zombie_npc | mixed: armor, artifacts, equipment, weapons | 22 | 25 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralZombie_Militaries_Stormtrooper_ItemGenerator` | 1 | 1 | corpse_override, zombie_npc | mixed: armor, artifacts, equipment, weapons | 23 | 24 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralZombie_Neutral_CloseCombat_ItemGenerator` | 2 | 1 | corpse_override, human_npc_named, zombie_npc | mixed: armor, artifacts, equipment, weapons | 31 | 26 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralZombie_Neutral_Recon_ItemGenerator` | 1 | 1 | corpse_override, zombie_npc | mixed: armor, artifacts, equipment, weapons | 45 | 29 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GeneralZombie_Neutral_Stormtrooper_ItemGenerator` | 3 | 1 | corpse_override, zombie_npc | mixed: armor, artifacts, equipment, weapons | 35 | 31 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `GuardNPC_Neutral_Recon_ItemGenerator` | 1 | 1 | corpse_override, human_npc_standard | mixed: armor, artifacts, equipment, junk, weapons | 23 | 22 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `MercTesak_ItemGenerator` | 1 | 1 | corpse_override, human_npc_named | mixed: armor, artifacts, weapons | 13 | 152 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `MonolithVeteranItemGenerator` | 4 | 3 | corpse_override | armor | 11 | 155 | REVIEW — corpse-only root; local fields are isolated, but this is a composite loadout rather than one abundance scalar |
| `NeutralExperiencedItemGenerator` | 2 | 2 | corpse_override, quest_scripted | armor | 15 | 168 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `NeutralMasterItemGenerator` | 12 | 1 | corpse_override, item_container_world | armor | 11 | 157 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `NeutralNewbieItemGenerator` | 1 | 1 | corpse_override, quest_scripted | armor | 14 | 153 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `ScientistExperiencedItemGenerator` | 1 | 1 | corpse_override | armor | 11 | 165 | REVIEW — corpse-only root; local fields are isolated, but this is a composite loadout rather than one abundance scalar |
| `ScientistTaumin_ItemGenerator` | 1 | 1 | corpse_override, human_npc_named | mixed: armor, weapons | 12 | 159 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `ScientistsZitnik_ItemGenerator` | 1 | 1 | corpse_override, human_npc_named | mixed: armor, equipment, weapons | 5 | 13 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `SparkVeteranItemGenerator` | 1 | 1 | corpse_override | armor | 11 | 155 | REVIEW — corpse-only root; local fields are isolated, but this is a composite loadout rather than one abundance scalar |
| `VartaMasterItemGenerator` | 1 | 1 | corpse_override, human_npc_named | armor | 11 | 153 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |
| `YantarDeadBody_Spark_ItemGenerator` | 1 | 1 | corpse_override | mixed: armor, artifacts, attachments, equipment, weapons | 19 | 31 | EXCLUDE — bespoke Yantar/Spark corpse loadout |
| `cerep_0_ItemGenerator` | 1 | 1 | corpse_override, human_npc_named | mixed: armor, artifacts, equipment, weapons | 17 | 24 | EXCLUDE FOR CORPSE-ONLY EDITS — target definition is shared cross-context |

## Why shared roots are excluded

31 of the 39 direct targets are not corpse-only. Their propagated impact reaches combinations of living standard NPCs, named/scripted NPCs, zombies, quests, mutants, traders, and/or world containers. Editing the generator definition would therefore change those other consumers too.

This includes apparently attractive targets such as `NeutralMasterItemGenerator`, generic `GeneralNPC_*` loadouts, generic zombie loadouts, and several named/DLC generators. Their presence behind `CorpseStashSID` does not make the generator itself corpse-specific.

## Relationship to existing Extra NPC Corpse Loot control

The existing `Extra NPC Corpse Loot` control does **not** modify any of these 39 ItemGenerator roots. It edits `DifficultyPrototypes.EconomyDifficulty.CorpseSmartLoot.GeneralNPC`, which selects SmartLoot packages such as `NPC_Medicine_Smart`, `NPC_Ammo_Smart`, or `NPC_Water_Smart`. That bonus layer can coexist with a DeadBody `CorpseStashSID` override and must remain conceptually and technically separate.

## Control-safety conclusion

- **Do not add a global Corpse Override Loot slider.** There is no single 1:1 field or common generator representing this system.
- **Do not edit shared target definitions for corpse-only tuning.** 31/39 direct roots are cross-context.
- **Corpse-only root-local fields are eligible only for narrowly named, explicit controls** whose semantics match the exact field (`Weight`, `Chance`, durability, ammo count, fixed item count, etc.).
- **Bespoke/scripted corpse roots remain excluded** even when technically isolated.
- The difficulty SmartLoot corpse bonus remains a separate already-proven control path.

## Machine-readable artifacts

- `corpse-override-consumer-edges.csv` — all 99 physical DeadBody `CorpseStashSID` edges.
- `corpse-override-generator-inventory.csv` — all 39 direct target generators with consumers, local/reachable content, blast radius and decision.
- `corpse-override-local-source-entries.csv` — all exact locally authored rows in the 39 direct roots.
- `corpse-override-dependency-inventory.csv` — all 137 generator nodes whose propagated impact includes corpse overrides.
- `corpse-override-domain-summary.json` — compact counts and conclusions.
- `CORPSE-OVERRIDE-ENTRY-CATALOG.md` — human-readable exact local row catalog.
- `CORPSE-OVERRIDE-WIRING-CATALOG.md` — human-readable physical DeadBody wiring catalog.
