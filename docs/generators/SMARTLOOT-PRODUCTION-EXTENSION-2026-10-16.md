# Regular world stash SmartLoot implementation (2026-10-16)

## Evidence and scope

The user-supplied CFG archive `STALKER2-GameData-CFG(20261016-142306).zip` (SHA-256 `937b3fb4c74d1311b6e052c010aa0795073ccdc60d2ca6d8b5228ee3ce0b6083`) supplies direct source data for `GameData/StashPrototypes.cfg` and `GameData/SpawnActorPrototypes.cfg`. These are distinct from `ItemGeneratorPrototypes.cfg`; one ordinary stash can use both layers. The earlier source catalogs gave 2,143 SmartLoot production edges from a separate evidence snapshot; the **new uploaded dump** provides an independently extracted **902 WorldMap_WP ItemContainer actors**, of which 71 have blueprint/unique/quest generator references and were conservatively excluded. **831** ordinary actors form the strict candidate allowlist. This is not a count of unique stashes across every game map or script.

### Embedded exact sources

- `Data/smartloot-regular-world-actor-manifest.csv`: the 831 allowed real actor SIDs, their entire original ordered `StashPrototypeSID` list, unaffected `ItemGeneratorSettings` references, and provenance coordinates/clue/DLC. No actor with an explicitly Blueprint/Unique/Quest generator is included.
- `Data/smartloot-world-source-entries.csv`: all **991** original item rows in 13 world SmartLoot prototypes with literal rank/index/group/ItemPrototypeSID and source Chance/Weight/MinCount/MaxCount and group spawn endpoints. No NPC corpse, quest-only `StashMedicine_Corpse`, `StashVodka_Corpse` or the `empty` root is included.
- `tools/audit_regular_stash_actor_dump.py`: reproducible reader taking the ZIP plus explicit CSV output path; preserves actor SOURCE list order, skips anything not a main-world ItemContainer, refuses unknown StashPrototypes references, and removes actors with blueprint/unique/quest ItemGenerator references.

## Implemented writer

`CfgPatchWriterService.StashSmartLoot.cs` is a separate source-native adapter for the existing `Stashes > Stash Content` and `Medical Composition / Quantity` controls. It runs only when at least one of those existing source values differs from its active reference value. It does **not** add UI controls, rename IDs or modify the v1 preset format.

For every affected original SmartLoot prototype it creates a single `TCD_<SID>` reference clone inside `GameData/StashPrototypes.cfg`, retaining the original rank/group/caliber/item-set structure. It changes only original source-backed fields, then redirects **only the 831 allowlisted main-world actors** that reference a modified prototype by changing their `StashPrototypeSID` list in `GameData/SpawnActorPrototypes.cfg`. It preserves every other unmodified list member and the actor's original `ItemGeneratorSettings`, clue and reward mechanics; no shared original SmartLoot definition is globally overwritten.

The six original Stash Content groups cover regular AMMO, MEDICAL, FOOD, DRINKS, GRENADES and COUPONS. This SmartLoot corpus has no coupon item entries; coupons continue through the existing cloned ItemGenerator layer. Both food and drinks use their own SID classifications within the two mixed `StashFood_*` prototypes. All seven ammo source families, two medical, two food/drink and two grenade families are evaluated. Original item selection `Weight` and `Chance` fields receive the corresponding category or per-item factor. Where a source group's members share exactly the same factor, its existing `MinSpawnChance`/`MaxSpawnChance` are also scaled and clamped to [0,1]; an authored 0/0 pair remains 0/0, even on a live `*_MainLoot` source. **This is not proof that a percentage maps to actual stash occurrence rates.**

**0x hard exclusion** in the allowlisted SmartLoot family zeroes an eligible item's source Weight/Chance **and** MinCount/MaxCount. When all eligible members of the exact native group have the same zero factor, the native spawn endpoints and existing ItemSetCount are also zeroed. This avoids assuming engine fallback never selects an all-zero-weight pool. Medical item-specific Bandage, Medkit, ArmyMedkit and Antirad biases combine with general MEDICAL. Medkit/ArmyMedkit MIN FLOOR and MAX CAP also apply to their SmartLoot source items; MAX CAP `0 ITEMS` (serialized -1) has precedence over a positive MIN. VANILLA remains stored as 0.

## Limits and acceptance

- The actor allowlist covers only ordinary **WorldMap_WP ItemContainer** records in the supplied dump, not medical breakable/destructible crates, dialogue gifts, prologue quest/scripted levels, unique rewards, NPC corpses or one-time pickups.
- An ordinary physical stash outside the allowlist, or one whose contents were already generated before installation, is not guaranteed to change.
- These edits use source-native BPatch/reference clone conventions, but **WPF/.NET compilation, packaged UE5 patch load order and gameplay effectiveness still require runtime verification**. Static CFG metadata alone cannot prove that engine SmartLoot selection honors the new source fields in every circumstance.
- New regression scenarios in `LootSpecialGeneratorsContract` check zero-medical, Bandage-only zero, zero-ammo, zero-food and zero-grenade real generated CFG fields and that a known blueprint actor is never redirected.
- Don't conflate the older historic SmartLoot consumer numbers (2,143) with these **831 explicitly allowlisted actors** from the newly supplied dump. Group counts and source rows are distinct quantities.

## Verification

Run `python tools/audit_regular_stash_actor_dump.py "STALKER2-GameData-CFG(20261016-142306).zip" --output /tmp/allowlist.csv --excluded-output /tmp/excluded.csv`, compare with the embedded actor manifest using a CSV parser, then run `python tools/audit_regular_stash_source_inventory.py` and `python tools/audit_project_consistency.py`. On Windows execute the actual LootSpecialGeneratorsContract test and publish. In game, use **an ungenerated ordinary stash after installing the rebuilt mod** and verify MEDICAL=0x does not produce a Medkit or Bandage. Existing/quest/special-content exceptions are not a source-level writer bug.

