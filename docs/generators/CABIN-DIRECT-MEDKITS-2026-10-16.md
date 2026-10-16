# Richter's Cabin direct Medkits — regular stash coverage (2026-10-16)

## Reproduced in the 2.0.5 CFG dump

During **A Tough Awakening** (after Skif wakes and follows Richter to the small **Cabin** compound, before Zalissya), the player opened three ordinary containers and saw precisely **2 Medkits and no Bandages** despite MEDICAL=0x. This was **not DLC1 loot**.

The full source `GameData/SpawnActorPrototypes.cfg` and `GameData/ItemGeneratorPrototypes.cfg` identify **three ordinary no-clue WorldMap_WP actors**, all already in the existing 803-actor regular SmartLoot allowlist. Their independent producer paths:

| Source container | ActorSID | Native SmartLoot | Location-specific wrapper | Extra direct item |
| --- | --- | --- | --- | --- |
| BigSafe | `1C2FD1C141D6DA11E518758CC1CD36AE` | `StashMedicine_Smart` + common/rare ammo | `LesserZone_Cabin_StashGenerator1_...` | **Medkit** chance `1.0`, count `1..1` |
| Bag | `9BCFDFE947FE7F7D5F5451B253F8B68B` | `StashMedicine_Smart` + cheap ammo | `LesserZone_Cabin_StashGenerator2_...` | None |
| SmallSafe | `59FDC1CD4D0D094270C695A18D84373A` | `StashMedicine_Smart` + cheap ammo | `LesserZone_Cabin_StashGenerator3_...` | **Medkit** chance `1.0`, count `1..1` |

**Root cause:** TCD correctly tunes the three original SmartLoot lists and their regular Cheap subgenerator (category `ItemGenerator.[0]`), but the generator wrappers **1 and 3** each have an extra authored `Consumable` category at `ItemGenerator.[1].PossibleItems.[0]` with **100% guaranteed Medkit** independent of those ordinary sources. Exactly those two items remained in the player's three stash test.

### Exact generator field scope

The CFG dump uses subtly different source labels: the top-level root name is `LesserZone_Cabin_StashGenerator1_...` but its internal `SID` (and actor reference) is `LesserZone_Cabin _StashGenerator1_...` with a literal space before the underscore. The new manifest keeps **both** exact strings. Actor ID and root spelling are independently checked against the uploaded CFG source. No root `LesserZone_Cabin_StashGenerator4` actor was proven eligible, and it is not modified.

## Minimal source-native writer change

Added `CfgPatchWriterService.StashCabin.cs` and embedded two-row `Data/cabin-direct-medkit-item-entries.csv`. When **MEDICAL**, **Medkit Bias**, **Medkit MIN**, or **Medkit MAX** differs from the selected reference, the existing tuning functions apply to **only those two original physical location-specific wrappers**. The new writer emits only changed original source fields inside `GameData/ItemGeneratorPrototypes.cfg`:

`<CabinGeneratorRoot> / ItemGenerator / [1] / PossibleItems / [0] / Chance,MinCount,MaxCount`.

- MEDICAL=0x, Medkit Bias=0x, or Medkit MAX=0 ITEMS: `Chance=0.0`, `MinCount=0`, `MaxCount=0`, preserving the original `ItemPrototypeSID` and the rest of the wrapper.
- MEDICAL=0.5x: `Chance=0.5`; original one-item bounds unchanged.
- At default values: no Cabin-specific patch is emitted.
- Does not touch `ItemGenerator.[0]` (Cheap subpool), the three existing SmartLoot lists, any blueprint/quest actor, other items, or any actor `ItemGeneratorSettings` paths. The 106 protected base actors and 112 allowed DLC1 actors are unchanged. No new slider/preset/graph is introduced.

The source-backed fix is narrow on purpose. Other local wrapper generators, medical destructibles, NPC corpses, script rewards and fixed world pickups have independent semantics and are not silently retuned.

## Acceptance

- Reproduce static metadata with `python tools/audit_cabin_medkit_stash.py --source-zip "STALKER2-GameData-CFG(20261016-152136).zip"` (optional ZIP flag; without it verify project manifests).
- Generated-CFG tests: `./tests/run-wine.sh LootSpecialGeneratorsContract` on Linux. Cases cover MEDICAL=0x, Medkit Bias=0x, Medkit MAX=0 ITEMS, MEDICAL=0.5x, and a negative FOOD-only change. Confirm exact fields for both Cabin roots.
- After `./publish.sh`, inspect the emitted `GameData/ItemGeneratorPrototypes.cfg` patch. Test freshly generated containers in the Cabin on a game state in which they were never opened/generated. If loot was already rolled in the save, changing source generators does not retroactively delete those items.
- Static CFG patches cannot prove the game's runtime merge/loading, but the source now explains the observed exact 2/3 medical result without blaming unrelated DLC quest generators.

[Overall base stash safety](BASE-STASH-PROTECTED-ACTORS-2026-10-16.md) · [DLC1 regular production](DLC1-STASH-REGULAR-PRODUCTION-2026-10-16.md) · [Medical corpus](STASH-MEDICAL-ALL-SOURCE-AUDIT-2026-10-16.md).
