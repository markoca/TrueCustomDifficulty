# STASHES — remaining safe-expansion candidate audit (2026-10-16)

## Decision and provenance

**Documentation-only follow-up. No new actor is approved for automatic redirection by this audit.** The existing Stashes UI graphs, their stable parameter/preset IDs, and the production writer stay unchanged. This document is a **candidate register**, not a patch allowlist or an in-game validation result.

- Uploaded archive: `STALKER2-GameData-CFG(20261016-152136).zip`.
- Exact SHA-256: `937b3fb4c74d1311b6e052c010aa0795073ccdc60d2ca6d8b5228ee3ce0b6083`. **Identical to** the earlier `STALKER2-GameData-CFG(20261016-142306).zip` recorded in [the active SmartLoot source contract](SMARTLOOT-PRODUCTION-EXTENSION-2026-10-16.md). A later filename/time stamp is **not** a newer dataset.
- Source paths: `GameData/SpawnActorPrototypes.cfg`, `GameData/StashPrototypes.cfg`, `GameData/ItemGeneratorPrototypes.cfg`, `GameData/QuestNodePrototypes.cfg` and `GameData/GlobalVariablePrototypes.cfg`.
- Actor filter: exact `LevelName=WorldMap_WP`, `SpawnType=ESpawnType::ItemContainer`. This is **not** the entire game's map/quest/corpse/destructible inventory.
- Authoritative existing patched-actor inputs: `src/TrueCustomDifficulty/Data/smartloot-regular-world-actor-manifest.csv`, `cheap-stash-production-redirect-manifest.csv`, `medium-stash-production-redirect-manifest.csv`, `expensive-stash-production-redirect-manifest.csv`. Preserve the current C# 803 actor no-clue gate in `CfgPatchWriterService.StashSmartLoot.cs`.

## Disjoint actor accounting

| Scope in this exact dump | Actors | Interpretation |
| --- | ---: | --- |
| Main-world ItemContainer actors | **1,141** | 902 with a nonempty `StashPrototypeSID`; 239 without one |
| With SmartLoot SID | **902** | 803 ordinary active + 28 named-clue deferred + 71 blueprint/unique/quest-generator excluded |
| **Active SmartLoot actor redirects** | **803** | In existing 831-row candidate manifest, `ClueVariableSID=EmptyInherited` |
| Named-clue SmartLoot candidates, not redirected | **28** | 26 `GDN_*`, 2 `Swamp_*`; no automatic safe expansion |
| Explicit blueprint/unique/quest-generator actors excluded | **71** | 5 of these also carry a named clue; keep all special-content paths isolated |
| Without SmartLoot SID | **239** | Not reachable by the SmartLoot actor redirection writer |
| Of those, already found in tier ItemGenerator production manifests | **227** | The individual ordinary-generator redirects are separate from any unique/blueprint reward |
| **No-SmartLoot actors outside those three production manifests** | **12** | 8 custom generators, 4 with no listed `PrototypeSID` |
| Total main-world named clues | **34** | 28 in the ordinary SmartLoot candidate class, 5 among 71 special exclusions, 1 in the no-SmartLoot group already represented by a tier production manifest |

The raw SmartLoot candidate manifest contains **831** actors, **not 831 active rewrites**. Subtracting 28 named-clue actors gives **803 active**. The 71 excluded actors are outside that 831-row manifest. The existing source catalog's **13 world SmartLoot prototypes / 991 item rows** and historical **2,143 production consumer edges** represent different objects and must not be added to actor totals.

The **40 newly deferred/reviewed actor IDs** with their **full 32-character IDs, exact source `ClueVariableSID`, original `StashPrototypeSID` list and `ItemGeneratorSettings[*].PrototypeSID` references** are preserved as machine-readable data in [`stash-safe-expansion-deferred-actors.csv`](stash-safe-expansion-deferred-actors.csv) (28 + 12 rows). The source manifest already preserves coordinates and DLC for the 28, and the uploaded ZIP remains the source for resolving the 12.

## The 28 named-clue SmartLoot actors — why not safe yet

All 28 have a named clue; **each named clue string occurs four times in `QuestNodePrototypes.cfg` and twice in `GlobalVariablePrototypes.cfg`** in the supplied dump. This is string-reference evidence of a quest/discovery dependency, not proof of a specific runtime reward mechanic. There are **26 GDN-linked** and **2 Swamp-linked** actor records; one named clue (`GDN_09_StashInCave`) is used on two different actors.

Nearly all list `StashMedicine_Smart,Stash_Ammo_Smart_CommonRare`; one actor (`2050CB3B4844A80A9404ACBC69DBD4F4`) instead lists `StashMedicine_Smart,Stash_Ammo_Smart_Cheap`. A material special-case source is actor `1F0D98894B52E72D537822A7F5C47856` (`Swamp_SunkenTugBoat_2_Stash`) which also references `Stash_GunFora_MagIncreased` through the *separate* ItemGenerator layer. Never discard or rewrite that special generator simply to tune medical/ammo SmartLoot.

**Disposition: WITHHOLD.** For any later actor-specific exception, audit its actual generator graph, the clue's quest/global-variable consumers, reward contents and generation timing; then add an explicit exact-actor allowlist with negative tests for all 71 special exclusions. A named clue should never be made generally patchable by relaxing the `ClueVariableSID` condition.

## The 12 without SmartLoot coverage

The 12 exact actors are listed in the same CSV. Their **four missing ItemGenerator PrototypeSID references** are:
`3BB3EB334159A776B18E07A7703C994F`,
`675FA71E486DE4619AD7BF848A634530`,
`79998780456B42B0144933B418D7C78E`, and
`AA40C6584BC050C9527D0C809835915B`.
An empty extracted `ItemGeneratorSettings[*].PrototypeSID` list does **not** prove an empty in-game inventory.

The other **eight** use source-authored custom generators rather than a covered tier-generator redirect:
- `SQ95_QSBIG_CartStash` and `SQ95_QSBIG_MainroomStash` — explicit SQ95 source names; treat as quest-associated unless independently proven ordinary.
- `CementPlant_IslandNearKopachi_Safe`, `StashZalissyaGrave_ItemGenerator` — hand-authored/special-name sources requiring contents review.
- `LesserZone_Rudnya-Veresnya_StashGenerator1_*`, `LesserZone_Scientists'Bunker_StashGenerator1_*`, `LesserZone_Barge_StashGenerator1_*`, `Promzona_OldPetrolStation_StashGenerator1_*` — location-specific generator sources. Names alone neither certify ordinary status nor support global root edits.

All 12 are **NO_SMARTLOOT_UNMAPPED**, not candidates for `StashPrototypeSID` modification. Review the native ItemGenerator definitions and complete consumers before considering their own clone-and-redirect manifest. Avoid modifying scripted/quest rewards, special weapons and blueprints.

One separate named-clue, no-SmartLoot actor `7FC60762441B2DD4D7E6E2B0E0E3E1D4` (`GDN_62_StashOnObservationArea`) is among the **227** ItemGenerator-manifested actors. It is *not* one of the 12. That manifest membership is not an assertion that arbitrary named-clue or quest-dependent contents are safe to change.

## Other source-root candidates (no new direct main-world actor)

In `ItemGeneratorSettings[*].PrototypeSID` for the **1,141 filtered actors**, the following each have **zero direct exact matches**:
`GamePass_Stash_ItemGenerator_CheapNoFresh`,
`GamePass_Stash_ItemGenerator_Common_Var2`,
`CNPPGamePass_Stash_ItemGenerator_Cheap`,
`CNPPGamePass_Stash_ItemGenerator_Rare`.

This **does not** prove they have no indirect parent/wrapper consumers or no use in other levels. Inspect the nested composition paths before any new generator-target proposal.

## Reproduction, migration and acceptance

1. Verify the ZIP checksum above and read the `WorldMap_WP` / `ESpawnType::ItemContainer` actors from `SpawnActorPrototypes.cfg`; preserve *entire ordered* `StashPrototypeSID` and `PrototypeSID` lists. Existing reproducer: `tools/audit_regular_stash_actor_dump.py`, with output CSV for 831 candidate and 71 special-exclusion actor records.
2. Compare the reproducer's 831 allowed rows to `src/TrueCustomDifficulty/Data/smartloot-regular-world-actor-manifest.csv`, then filter its `ClueVariableSID=EmptyInherited` subset and assert exactly **803**; compare the remainder to the **28** `NAMED_CLUE_SMARTLOOT` records in the new CSV.
3. Independently check all 239 actors with no SmartLoot against the **three** tier production redirect manifests, obtaining 227 manifest hits and 12 nonhits; compare the nonhits by full actor SID to `NO_SMARTLOOT_UNMAPPED` records in the new CSV.
4. For the 28, investigate `QuestNodePrototypes.cfg` and `GlobalVariablePrototypes.cfg`, and separately resolve the named generator contents from `ItemGeneratorPrototypes.cfg`. The quest/global string counts above are evidence, **not sufficient approval**.
5. For any approved future addition, commit updated machine-readable manifests + scoped clone/writer + generated-CFG negative tests + relevant UI/source documentation **together on `main`**. A proposed extra actor must be added by exact SID only. Validate an *ungenerated* in-game stash; static checks cannot certify loot changes in existing saves.

**No new safe expansion has been demonstrated by this identical source dump. No code, presets, UI controls or gameplay output change as a result of this audit.**
