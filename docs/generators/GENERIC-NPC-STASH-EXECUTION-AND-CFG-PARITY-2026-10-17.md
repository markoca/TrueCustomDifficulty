**C# build blocker fix (2026-10-17):** Local build reported CS1628 in StashEffectiveState.UniformSmartFactor (lambda captured the out factor parameter), fixed by capturing an ordinary local while preserving its exact comparison and out assignment. Local build also reported CS0246 in AggregateVisualControl's invalid-tier guard, fixed by importing System.IO for InvalidDataException. Neither edit modifies category calculation, gameplay tuning, presets, generator source references or UI layout. Pending actual local .NET/Wine recompile; previous CI jobs ended before running steps.

# Ordinary NPC loot and generic world stashes — generator execution / CFG parity gate

**State: original CFG/source linkage and automated production-writer diagnostics.
Not an Unreal runtime simulator; no game PASS.**
Source evidence: the original STALKER 2 2.0.5 dump already catalogued in
this repository; production source manifests; cfg-writer-baseline.json;
[ordinary human corpse matrix](npc-corpse-ingame-loot-test-matrix.csv);
[generic stash matrix](stash-ingame-loot-test-matrix.csv).
No changed regular app UI, gameplay writer, persisted parameter, vanilla
shared source root, physical protected allowlist or game version is implied.

## NPC — source to ordinary lootable human corpse

| Source layer | Native input | TCD action on production only | Does NOT establish |
| --- | --- | --- | --- |
| Standard human loadout | 33 repeatable human ItemGenerator roots linked from original NPC archetypes | Preserve native weapon/armor, rank and role; change only audited links to TCD-owned consumable clones when medical is modified | Inventory is generated specifically at death |
| Six consumable pools | GeneralNPC_Consumables_CloseCombat / Heavy / Noon / Recon / Sniper / Stormtrooper: 104 item rows total; 58 medical Chance rows | loot.npcMedicalChance: clone all six pools; clamp only the 58 medical Chance rows to source × selected 0..2; retain all counts and other 46 rows | 0x eliminates bonus medicine from another source |
| Corpse bonus package list | DifficultyPrototypes.EconomyDifficulty.CorpseSmartLoot.GeneralNPC | loot.npcCorpseBonus: Novice = Medicine+Ammo, Stalker = Water, Veteran/Master = None; exact replacement only on selected difficulty | Real roll odds from selecting the package |
| Corpse extra ammo | StashPrototypes.NPC_Ammo_Smart: 24 groups, 43 stack count entries | loot.npcWeaponAmmoSupply 0..3 scales MinCount/MaxCount in TCD_NPC_Ammo_Smart clone **only when package includes NPC_Ammo_Smart** | Changes weapon-attached ammo, weapon selection or all loose ammo |
| Direct NPC equipment / grenades | 121 production weapon rows and mixed grenade Chance/Weight sources | Preserve independent selection; weapon condition remains an already existing separate control | All generated ammo/gear must be visible on a looted corpse |
| CorpseStashSID | 99 DeadBody links to 39 roots, of which 31 have cross-context dependencies | **Out of scope.** No broad edits; bespoke/scripted and named dead-body overrides stay protected | Same system as difficulty CorpseSmartLoot |

**One independently testable contradiction**: on Novice,
loot.npcMedicalChance=0 leaves the vanilla Medicine+Ammo corpse SmartLoot
package selected unless loot.npcCorpseBonus is separately changed. Therefore
a Medkit on an ordinary corpse does not disprove the consumable clone
transform. The fixtures include medical 0x both **with and without** that
parallel package, but they do not guarantee physical drops.

**NPC primary base difficulty for medical**: Veteran (internal Hard), where
CorpseSmartLoot.GeneralNPC is originally absent. Native Master (internal
Stalker) is the additional medical rank/difficulty-gate test. Never
confuse the user-facing difficulty Stalker with internal EGameDifficulty::Stalker.

## Generic stashes — source to ordinary ItemContainer

Physical WorldMap_WP ItemContainer
→ ItemGeneratorSettings (Basic/Standard/Rare rank wrappers)
AND/OR StashPrototypeSID (independent SmartLoot group list)
→ actual item rolls (game engine; not proven by CFG topology).

| Source layer | Original model | Scope and controls | Nonclaims |
| --- | --- | --- | --- |
| Tier ItemGenerator | GamePass_Stash_ItemGenerator_Cheap, Common_Var1, Rare: 41 authored source groups, 133 row entries | Production-only TCD reference clones + safe wrapper/actor redirects; six content category knobs | One selected group Weight = probability of entire stash |
| World SmartLoot | 13 original StashPrototypes, 422 source groups, 991 item entries | Original rank/group/ItemSetCount and per-group chance endpoints; 803 approved no-clue base actor candidates, optional 112 DLC1 allowlisted actors | Every mapped branch activates on every visit |
| Physical actor | 831 inventoried candidate base actors, of which 28 additionally have named clues; whole protected-base roster is 106 | Preserve ItemGeneratorSettings and StashPrototypeSID independent original sources and redirects; never rewrite special/quest/blueprint actors | Actor SID or static source edge means measured physical drop |
| Direct authored items | Cabin 2 direct conditional Chance=1 Medkits and other special rows | Keep last-wave regression cases only | Generic stash generator behavior |

**Important separation:** independent Chance entries do not form one
normalized 100% distribution. Weight is normalized only within its own
eligible source group. SmartLoot group MinSpawnChance/MaxSpawnChance,
ItemSetCount and engine/first-visit rules are separate. All-zero groups
can have engine fallback behavior unknown to the static writer, even
when explicit selection and counts are zero.

## Executable CFG diagnostics (production writer, independent actual output)

The new GenericLootPreGame.cs partial is called by the existing
LootSpecialGeneratorsContract entrypoint **after** current tests. It
constructs a fresh MainViewModel for each test, sets one existing control
(except the intentional combined medical+corpse-bonus NONE case),
invokes the real CfgPatchWriterService.Write, reads the emitted CFG,
and fails on mismatched exact source rows or cross-domain leaks.

### 13 isolated field-level scenarios

| Scenario name | Difficulty | Expected emitted source |
| --- | --- | --- |
| npc-med-veteran-zero | Veteran | Six consumable clones; 58 medical Chance = 0, 46 unrelated item rows and counts original; 33 exact redirects |
| npc-med-veteran-high | Veteran | Same source medical Chance = min(source × 2, 1); unrelated rows unchanged |
| npc-med-novice-zero-bonus-active | Novice | Medical clone 0x; original Medicine+Ammo difficulty package **left active**, no difficulty-list patch |
| npc-med-novice-zero-corpse-none | Novice | Medical clone 0x and explicit empty CorpseSmartLoot subtree |
| npc-ammo-novice-zero | Novice | 43 bonus ammo entries MinCount = MaxCount = 0; only ammo clone selected |
| npc-ammo-novice-high | Novice | 43 bonus ammo entries ×3, integer half-up; package list still includes Medicine |
| npc-ammo-stalker-water-inactive | Stalker | Saved ammo 3x produces **no** TCD_NPC_Ammo_Smart clone when Water package selected |
| stash-medical-zero | Veteran | Native Basic/Standard/Rare medical tier rows disabled; at least one parallel SmartLoot row selection/count disabled |
| stash-ammo-zero | Veteran | Native ammo tier and independent SmartLoot selection/count disabled |
| stash-food-zero | Veteran | Native food tier and independent SmartLoot selection/count disabled |
| stash-drinks-zero | Veteran | Native drink tier and independent SmartLoot selection/count disabled |
| stash-grenades-zero | Veteran | Native grenade tier and independent SmartLoot selection/count disabled |
| stash-coupons-zero | Veteran | Native Rare coupon rows disabled; **no** coupon SmartLoot prototype invented |

The harness compares against the retained **original** baseline values
for NPC Chance/count and ammo stack counts. Stash numeric comparisons use
the production preview's shared StashEffectiveState against **actual**
emitted native source fields, so this validates writer/preview *parity*,
not independent mathematical truth of the engine.

**Safeguards:** all affected NPC consumable clone templates and all 33
source consumer redirects are checked; production-only roots stay cloned;
no original shared consumable root is overwritten. NPC medical cannot
edit world actors, world SmartLoot or corpse package without an explicit
corpse setting. Stash controls cannot modify NPC consumables or
difficulty CorpseSmartLoot; base 106 protected stash actors are checked.
Only base-game/no-DLC fixture paths are written in these scenarios.

### Running / exporting inspectable source-specific fixtures

On Linux in the repo:

    ./tests/run-wine.sh LootSpecialGeneratorsContract

To retain **the entire suite's generated CFG fixture tree** in a
separate directory (the 13 relevant subdirectories are under
TCD_GenericLoot_PreGame):

    ./tests/run-wine.sh LootSpecialGeneratorsContract --out audit/generic-loot-fixtures

The runner preserves its executable Git file mode, cross-compiles the test to win-x64 and runs it under Wine;
requires the project's .NET 10 SDK and Wine. It does not modify the
main program's published executable. Inspect real output at, for example:

    audit/generic-loot-fixtures/TCD_GenericLoot_PreGame/npc-med-veteran-zero/Stalker2/Content/GameLite/

    audit/generic-loot-fixtures/TCD_GenericLoot_PreGame/stash-medical-zero/Stalker2/Content/GameLite/

Run static provenance checks independently:

    python3 tools/audit_npc_corpse_ingame_test_matrix.py
    python3 tools/audit_stash_ingame_test_matrix.py
    python3 tools/audit_stash_generator_branches.py
    python3 tools/audit_stash_consumer_graph.py

The diagnostic files are **not individually isolated physical actor
patches and not packaged installable PAKs**. Each controlled profile
targets the normal safe production scope of its one parameter, not one
specific NPC/body or one physical stash. If a fresh in-game result needs
attribution between parallel sources, an additional actor-scoped debug
patch must be designed and independently checked first.

**Acceptance:** successful local compiler, all source/static validators
and emitted-patch tests, then fresh individual real-world ordinary human
NPC/body or world stash generation with unmodified game difficulty/rank
and recorded evidence. No currently unrun game case is marked PASS.
