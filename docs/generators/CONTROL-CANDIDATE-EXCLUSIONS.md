# Hard-excluded generator control candidates

This file is the authoritative human-readable policy for generator-backed control candidates that must **never** be exposed as generic UI tuning targets. It applies even when a CFG field is technically clean, isolated, or 1:1.

## Policy

- **TEST / DEBUG** — development maps, demo fixtures, balance/unit-test wiring, or generators with no production consumer path.
- **PROLOGUE** — a generator/root specifically authored for Prologue content.
- **ONE-TIME / STORY-SCRIPTED** — quest rewards, scripted payloads, unique corpses, named/bespoke encounters, boss/special-DLC payloads, and dedicated dependencies used only by such paths.

These categories are hard exclusions from UI/control design. They remain mapped and documented so future audits do not rediscover them.

### Important scope rule

A normal reusable production generator is **not** excluded just because one consumer happens to be in the Prologue or a test map. The generator itself must be specific to the excluded context. Example: the normal species `BloodsuckerLootGenerator` remains a valid species-level target even though a Prologue Bloodsucker consumes it; `..._Prolog` roots are excluded.

## Current register summary

- Exact excluded generator SIDs: **932**
- Prologue-specific: **10**
- Test/debug-only: **18**
- One-time/story-scripted: **904**
- Previously present in the strict candidate allowlist and now removed: **20**

Every exact SID and its reason is in `control-candidate-exclusions.csv`. The CSV is the exhaustive register; the tables below highlight the policy-sensitive sets that could otherwise be mistaken for safe candidates.

## Removed from the previous strict candidate allowlist

| Generator | Class | Mapped domain | Why excluded |
| --- | --- | --- | --- |
| `DLC01_Object68LootGenerator` | ONE_TIME_SCRIPTED | MUTANT LOOT | Bespoke boss/special-DLC mutant payload: not a repeatable generic mutant-part surface. |
| `JadedChimeraLootGenerator` | ONE_TIME_SCRIPTED | MUTANT LOOT | Bespoke boss/special-DLC mutant payload: not a repeatable generic mutant-part surface. |
| `RC_Faust_Mutant_ItemGenerator` | ONE_TIME_SCRIPTED | MUTANT LOOT | Bespoke boss/special-DLC mutant payload: not a repeatable generic mutant-part surface. |
| `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_AKU` | PROLOGUE | STANDARD HUMAN NPC LOADOUTS | Prologue-specific generator: intentionally excluded from UI/control design even when its fields are technically isolated or 1:1. |
| `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_Obrez` | PROLOGUE | STANDARD HUMAN NPC LOADOUTS | Prologue-specific generator: intentionally excluded from UI/control design even when its fields are technically isolated or 1:1. |
| `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog` | PROLOGUE | STANDARD HUMAN NPC LOADOUTS | Prologue-specific generator: intentionally excluded from UI/control design even when its fields are technically isolated or 1:1. |
| `StashProlog_ItemGenerator_1` | PROLOGUE | WORLD CONTAINERS / STASHES | Prologue-specific generator: intentionally excluded from UI/control design even when its fields are technically isolated or 1:1. |
| `DebugArtifactGenerator` | TEST_DEBUG | WORLD CONTAINERS / STASHES | Debug/demo generator: development fixture, not production gameplay. |
| `DestructibleStash_Ammo_NATO_Experienced` | TEST_DEBUG | WORLD DESTRUCTIBLES | Destructible generator is only confirmed in test/debug wiring and has no approved production destructible target surface. |
| `DestructibleStash_Ammo_NATO_Newbie` | TEST_DEBUG | WORLD DESTRUCTIBLES | Destructible generator is only confirmed in test/debug wiring and has no approved production destructible target surface. |
| `DestructibleStash_Ammo_NATO_Veteran` | TEST_DEBUG | WORLD DESTRUCTIBLES | Destructible generator is only confirmed in test/debug wiring and has no approved production destructible target surface. |
| `DestructibleStash_Ammo_SNG_Experienced` | TEST_DEBUG | WORLD DESTRUCTIBLES | Destructible generator is only confirmed in test/debug wiring and has no approved production destructible target surface. |
| `DestructibleStash_Ammo_SNG_Newbie` | TEST_DEBUG | WORLD DESTRUCTIBLES | Destructible generator is only confirmed in test/debug wiring and has no approved production destructible target surface. |
| `DestructibleStash_Ammo_SNG_Veteran` | TEST_DEBUG | WORLD DESTRUCTIBLES | Destructible generator is only confirmed in test/debug wiring and has no approved production destructible target surface. |
| `StashAKMedFood_Medium` | TEST_DEBUG | WORLD CONTAINERS / STASHES | Resolved physical consumers are test/debug only; no production-world consumer path exists. |
| `StashMP5MedFood_Medium` | TEST_DEBUG | WORLD CONTAINERS / STASHES | Resolved physical consumers are test/debug only; no production-world consumer path exists. |
| `StashNeutralArmorMedFood_Expensive` | TEST_DEBUG | WORLD CONTAINERS / STASHES | Resolved physical consumers are test/debug only; no production-world consumer path exists. |
| `StashNoonArmorMedFood_Expensive` | TEST_DEBUG | WORLD CONTAINERS / STASHES | Resolved physical consumers are test/debug only; no production-world consumer path exists. |
| `StashSaygaMedFood_Medium` | TEST_DEBUG | WORLD CONTAINERS / STASHES | Resolved physical consumers are test/debug only; no production-world consumer path exists. |
| `StashVartaArmorMedFood_Expensive` | TEST_DEBUG | WORLD CONTAINERS / STASHES | Resolved physical consumers are test/debug only; no production-world consumer path exists. |

## Prologue-specific roots

| Generator | Mapped domain | Content | Why excluded |
| --- | --- | --- | --- |
| `DestructibleStashProlog_AmmoA545D` | WORLD DESTRUCTIBLES | ammo | Prologue-specific generator: intentionally excluded from UI/control design even when its fields are technically isolated or 1:1. |
| `DestructibleStashProlog_Drink` | WORLD DESTRUCTIBLES | consumables | Prologue-specific generator: intentionally excluded from UI/control design even when its fields are technically isolated or 1:1. |
| `DestructibleStashProlog_Med` | WORLD DESTRUCTIBLES | consumables | Prologue-specific generator: intentionally excluded from UI/control design even when its fields are technically isolated or 1:1. |
| `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_AKU` | STANDARD HUMAN NPC LOADOUTS | mixed: artifacts, weapons | Prologue-specific generator: intentionally excluded from UI/control design even when its fields are technically isolated or 1:1. |
| `GeneralNPC_Bandit_Recon_ItemGenerator_Prolog_Obrez` | STANDARD HUMAN NPC LOADOUTS | mixed: artifacts, weapons | Prologue-specific generator: intentionally excluded from UI/control design even when its fields are technically isolated or 1:1. |
| `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog` | STANDARD HUMAN NPC LOADOUTS | mixed: ammo, armor, consumables | Prologue-specific generator: intentionally excluded from UI/control design even when its fields are technically isolated or 1:1. |
| `GeneralNPC_Neutral_CloseCombat_ItemGenerator_Prolog_Medkit` | CORPSE OVERRIDE GENERATORS | mixed: armor, consumables, weapons | Prologue-specific generator: intentionally excluded from UI/control design even when its fields are technically isolated or 1:1. |
| `StashProlog_ItemGenerator_1` | WORLD CONTAINERS / STASHES | mixed: ammo, consumables | Prologue-specific generator: intentionally excluded from UI/control design even when its fields are technically isolated or 1:1. |
| `StashProlog_ItemGenerator_2` | WORLD CONTAINERS / STASHES | consumables | Prologue-specific generator: intentionally excluded from UI/control design even when its fields are technically isolated or 1:1. |
| `StashProlog_ItemGenerator_3` | WORLD CONTAINERS / STASHES | mixed: ammo, consumables | Prologue-specific generator: intentionally excluded from UI/control design even when its fields are technically isolated or 1:1. |

## Test/debug-only roots

| Generator | Mapped domain | Evidence | Why excluded |
| --- | --- | --- | --- |
| `DebugArtifactGenerator` | WORLD CONTAINERS / STASHES | scope=base; test_debug_links=1; main_world_links=0; quest_level_links=0; other_production_links=0 | Debug/demo generator: development fixture, not production gameplay. |
| `DestructibleStash_Ammo_NATO_Experienced` | WORLD DESTRUCTIBLES | role=test/debug physical root; decision=EXCLUDE — no confirmed production destructible consumer | Destructible generator is only confirmed in test/debug wiring and has no approved production destructible target surface. |
| `DestructibleStash_Ammo_NATO_Master` | SHARED CROSS-CONTEXT POOLS | role=test/debug physical root; decision=EXCLUDE — no confirmed production destructible consumer | Destructible generator is only confirmed in test/debug wiring and has no approved production destructible target surface. |
| `DestructibleStash_Ammo_NATO_Newbie` | WORLD DESTRUCTIBLES | role=test/debug physical root; decision=EXCLUDE — no confirmed production destructible consumer | Destructible generator is only confirmed in test/debug wiring and has no approved production destructible target surface. |
| `DestructibleStash_Ammo_NATO_Veteran` | WORLD DESTRUCTIBLES | role=test/debug physical root; decision=EXCLUDE — no confirmed production destructible consumer | Destructible generator is only confirmed in test/debug wiring and has no approved production destructible target surface. |
| `DestructibleStash_Ammo_SNG_Experienced` | WORLD DESTRUCTIBLES | role=test/debug physical root; decision=EXCLUDE — no confirmed production destructible consumer | Destructible generator is only confirmed in test/debug wiring and has no approved production destructible target surface. |
| `DestructibleStash_Ammo_SNG_Master` | SHARED CROSS-CONTEXT POOLS | role=test/debug physical root; decision=EXCLUDE — no confirmed production destructible consumer | Destructible generator is only confirmed in test/debug wiring and has no approved production destructible target surface. |
| `DestructibleStash_Ammo_SNG_Newbie` | WORLD DESTRUCTIBLES | role=test/debug physical root; decision=EXCLUDE — no confirmed production destructible consumer | Destructible generator is only confirmed in test/debug wiring and has no approved production destructible target surface. |
| `DestructibleStash_Ammo_SNG_Veteran` | WORLD DESTRUCTIBLES | role=test/debug physical root; decision=EXCLUDE — no confirmed production destructible consumer | Destructible generator is only confirmed in test/debug wiring and has no approved production destructible target surface. |
| `MSDemoItemGenerator` | TEMPLATE / DEMO / EMPTY | scope=base | Debug/demo generator: development fixture, not production gameplay. |
| `MSDemoPlayerItemGenerator` | SHARED CROSS-CONTEXT POOLS | scope=base | Debug/demo generator: development fixture, not production gameplay. |
| `MSDemoSQ02IconGenerator` | TEMPLATE / DEMO / EMPTY | scope=base | Debug/demo generator: development fixture, not production gameplay. |
| `StashAKMedFood_Medium` | WORLD CONTAINERS / STASHES | test_debug_links=2; main_world_links=0; quest_level_links=0; other_production_links=0 | Resolved physical consumers are test/debug only; no production-world consumer path exists. |
| `StashMP5MedFood_Medium` | WORLD CONTAINERS / STASHES | test_debug_links=2; main_world_links=0; quest_level_links=0; other_production_links=0 | Resolved physical consumers are test/debug only; no production-world consumer path exists. |
| `StashNeutralArmorMedFood_Expensive` | WORLD CONTAINERS / STASHES | test_debug_links=2; main_world_links=0; quest_level_links=0; other_production_links=0 | Resolved physical consumers are test/debug only; no production-world consumer path exists. |
| `StashNoonArmorMedFood_Expensive` | WORLD CONTAINERS / STASHES | test_debug_links=2; main_world_links=0; quest_level_links=0; other_production_links=0 | Resolved physical consumers are test/debug only; no production-world consumer path exists. |
| `StashSaygaMedFood_Medium` | WORLD CONTAINERS / STASHES | test_debug_links=2; main_world_links=0; quest_level_links=0; other_production_links=0 | Resolved physical consumers are test/debug only; no production-world consumer path exists. |
| `StashVartaArmorMedFood_Expensive` | WORLD CONTAINERS / STASHES | test_debug_links=2; main_world_links=0; quest_level_links=0; other_production_links=0 | Resolved physical consumers are test/debug only; no production-world consumer path exists. |

## One-time / story-scripted special roots outside the generic quest group

The general `QUEST / REWARD / SCRIPTED` domain already contributes **878** exact generator SIDs to the hard-exclusion register. They are intentionally kept in the CSV instead of expanding this Markdown file by hundreds of rows. The additional bespoke roots below live in other mapped domains but are also hard-excluded because their actual use is unique/scripted.

| Generator | Mapped domain | Content/family | Why excluded |
| --- | --- | --- | --- |
| `C_E03_MQ06N_MeetingScar_cha_Zombie_01_ItemGenerator` | ZOMBIE NPC LOADOUTS | mixed: ammo, armor, consumables, weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `C_E03_MQ06N_MeetingScar_cha_Zombie_02_ItemGenerator` | ZOMBIE NPC LOADOUTS | mixed: ammo, armor, consumables, weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `DLC01_MQ06_Zombie_Bandit_CloseCombat_ItemGenerator` | ZOMBIE NPC LOADOUTS | mixed: armor, artifacts, consumables, weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `DLC01_MQ06_Zombie_Bandit_Heavy_ItemGenerator` | ZOMBIE NPC LOADOUTS | mixed: armor, consumables, weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `DLC01_MQ06_Zombie_Bandit_Recon_ItemGenerator` | ZOMBIE NPC LOADOUTS | mixed: armor, consumables, weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `DLC01_MQ06_Zombie_Bandit_Stormtrooper_ItemGenerator` | ZOMBIE NPC LOADOUTS | mixed: armor, consumables, weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `DLC01_MQ06_Zombie_Neutral_Recon_ItemGenerator` | ZOMBIE NPC LOADOUTS | mixed: armor, consumables, weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `DLC01_MQ06_Zombie_Neutral_Sniper_ItemGenerator` | ZOMBIE NPC LOADOUTS | mixed: armor, consumables, weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `DLC01_MQ06_Zombie_Neutral_Stormtrooper_ItemGenerator` | ZOMBIE NPC LOADOUTS | mixed: armor, consumables, weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `DLC01_Object68LootGenerator` | MUTANT LOOT | mutant_parts | Bespoke boss/special-DLC mutant payload: not a repeatable generic mutant-part surface. |
| `GarbageDetCenterCorpseItemGenerator` | CORPSE OVERRIDE GENERATORS | junk | Bespoke/scripted corpse payload: tied to one authored body/event rather than generic corpse loot. |
| `JadedChimeraLootGenerator` | MUTANT LOOT | mutant_parts | Bespoke boss/special-DLC mutant payload: not a repeatable generic mutant-part surface. |
| `KeyGeneratorRedForest` | ZOMBIE NPC LOADOUTS | junk | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `RC_Faust_Mutant_ItemGenerator` | MUTANT LOOT | mixed: armor, weapons | Bespoke boss/special-DLC mutant payload: not a repeatable generic mutant-part surface. |
| `RC_Military_Zombie_RedForest_ItemGenerator` | ZOMBIE NPC LOADOUTS | armor | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `RUCloseCombatWeaponExperiencedGenerator` | ZOMBIE NPC LOADOUTS | weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `RUMachineGunnerWeaponExperiencedGenerator` | ZOMBIE NPC LOADOUTS | weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `RUScoutWeaponExperiencedGenerator` | ZOMBIE NPC LOADOUTS | weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `RUSniperWeaponExperiencedGenerator` | ZOMBIE NPC LOADOUTS | weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `RUStormTrooperWeaponExperiencedGenerator` | ZOMBIE NPC LOADOUTS | weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `YantarDeadBody_Spark_ItemGenerator` | CORPSE OVERRIDE GENERATORS | mixed: armor, artifacts, attachments, equipment, weapons | Bespoke/scripted corpse payload: tied to one authored body/event rather than generic corpse loot. |
| `ZombieFoster_ItemGenerator` | ZOMBIE NPC LOADOUTS | mixed: armor, artifacts, equipment, weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `ZombieIgor_petrusko_ItemGenerator` | ZOMBIE NPC LOADOUTS | mixed: armor, equipment, weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `ZombieLevsa_ItemGenerator` | ZOMBIE NPC LOADOUTS | mixed: armor, artifacts, equipment, weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `ZombiePetkaBelak_ItemGenerator` | ZOMBIE NPC LOADOUTS | mixed: armor, artifacts, equipment, weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |
| `ZombieZombirovannyj_1_ItemGenerator` | ZOMBIE NPC LOADOUTS | mixed: armor, equipment, weapons | Named/scripted/bespoke zombie path: story-specific loadout or dedicated dependency, not generic zombie tuning. |

## Consequences for control design

1. A hard-excluded SID must not be added to `generator-target-allowlist.csv`, a writer target list, or a generic UI control.
2. If a future feature intentionally targets a specific quest/Prologue/test scenario, it must be designed as a separate explicit feature; it does not silently reuse the generic tuning surface.
3. Shared production generators stay eligible only when their intended production blast radius is acceptable and independently audited.
4. This policy does not remove source data or patch existing runtime behavior; it only constrains future control targeting.

## v0.1.81 consistency cleanup

The main strict target allowlist was already clean, but the older `generator-preferred-base-candidates.csv` still retained four now-hard-excluded rows. v0.1.81 removes `RC_Faust_Mutant_ItemGenerator`, `StashProlog_ItemGenerator_1`, `DLC01_Object68LootGenerator`, and `JadedChimeraLootGenerator` from that preferred-candidate convenience table as well. The production UI triage is in `CONTROL-CANDIDATE-TRIAGE.md`.
