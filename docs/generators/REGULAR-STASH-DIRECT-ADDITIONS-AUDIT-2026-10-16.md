# Additional direct loot in otherwise ordinary world stash wrappers — source audit (2026-10-16)

**Audit only. No writer, UI or preset changes in this audit.** The two directly-authored Medkits at Richter's Cabin were fixed separately in [the Cabin implementation](CABIN-DIRECT-MEDKITS-2026-10-16.md).

## Evidence

Input: `STALKER2-GameData-CFG(20261016-152136).zip` SHA-256 `937b3fb4c74d1311b6e052c010aa0795073ccdc60d2ca6d8b5228ee3ce0b6083`, particularly `GameData/SpawnActorPrototypes.cfg` and `GameData/ItemGeneratorPrototypes.cfg`. Extracted physical actors are within the **803 source-audited, ordinary WorldMap_WP ItemContainers**: no named clue, no explicitly Blueprint/Unique/Quest ItemGenerator reference, and nonempty normal SmartLoot. Each selected actor uses a location-specific ItemGenerator wrapper containing both a regular `GamePass_Stash_ItemGenerator_Cheap / Common_Var1 / Rare` subgenerator and additional *directly authored* `ItemPrototypeSID` rows outside that regular core.

The full [machine-readable catalog](regular-stash-direct-additions-2026-10-16.csv) retains **80 extra item rows across 55 source roots / 55 physical actors**, including exact physical ActorSID, original root, actual SID, core, category index/type, item index, item prototype, Chance/Weight, count bounds and review status. Re-extraction from the supplied base ZIP reproduced exactly the same 80 records.

| Extra source-row class | Count | Status |
| --- | ---: | --- |
| Canonical medical entries | **12** | 8 physical actors; 2 Cabin Medkits already patched, **10 rows on 6 more actors** not tuned |
| Generic ammunition | **11** | Includes ammo-category `AVOG`, which does not use numeric-A item ID conventions |
| Grenade entries | **2** | Direct RGD-5 sources independent of Cheap core |
| Boosters | **9** | Hercules/PSYBlocker; no automatic MEDICAL classification |
| Lowercase `medkit` source SID | **1** | Case-sensitive source warning; not the canonical `Medkit` |
| Other authored rewards and equipment | **45** | Weapons/attachments and other authored entries; no blanket modification |
| **Total** | **80** | **55 actors**, including **22** with a recognized generic item or booster |

### Six additional regular WorldMap stashes with untuned directly-authored medical rewards

These ten entries all have **literal item Chance=1 and positive source counts**. The game still controls category selection and generator activation; the source chance is not proof that each stash always physically spawns the entry.

| World location | ActorSID | Direct extra medical source fields |
| --- | --- | --- |
| Rostok | `12AA126A43D685C7CD7179854E40C5E9` | ArmyMedkit ×5 |
| Wild Island / Azimuth Station | `717745A84684B392BF29D5B6C157B489` | EcoMedkit ×1, ArmyMedkit ×3, Medkit ×3, Bandage ×5 |
| Garbage / Factory | `89A6611B4CA53AFD52E20CBAC749B162` | ArmyMedkit ×5 |
| Garbage / Hydroelectric Station | `A781417E408DCEDA7F96D48D07C27D52` | ArmyMedkit ×3 |
| Promzona / Promin CMD Factory | `A5C617CC4B4C04CC52DAAA86A6AAFDBF` | EcoMedkit ×2 |
| Promzona / Army Warehouses | `BFD606894964956318B5519609B326B0` | EcoMedkit ×3, ArmyMedkit ×3 |

**Additional source spelling issue:** The last generator also contains `ItemPrototypeSID = medkit` (lowercase) with `Chance=1`, `MinCount=3`, `MaxCount=3`. This is **separate from the 12 canonical medical rows**. Do not assume that it spawns a canonical Medkit or silently normalize its case without confirming game item-resolution behavior; the existing stash medical SID mapper is case-sensitive.

## Other items, safeguards and DLC1

- **11 extra ammo and 2 RGD-5 rows** are written by location-specific generators outside the generic tier sources; they may require their own item-level patch allowlist for the existing ammo/grenade sliders. Nine booster rows are distinct from the current medical source controls.
- **45 authored nonclassified rows** (including weapons and attachments) can coexist with the ordinary loot within the very same generator. Never edit the entire root, its category, or the parent generic `GamePass_Stash` source globally to catch the extra items.
- Separately authored *no-normal-core* generators (e.g. `SQ95_QSBIG_*`, `StashZalissyaGrave_ItemGenerator`, `CementPlant_IslandNearKopachi_Safe`, `LesserZone_Barge_StashGenerator1` and `Promzona_OldPetrolStation_StashGenerator1`) can contain mundane loot but are **not** part of the safer 80-entry normal-core-plus-extra inventory. Hold these for quest/special review.
- In the **112 previously approved DLC1 ordinary WorldMap actors**, the separate DLC dump shows no equivalent extra DLC-local direct entry attached to a standard `GamePass_Stash` wrapper. The custom DLC1 MQ06/SQ/EQ/quest sources remain excluded; this is not a claim that DLC1 has no additional authored medical loot.
- As with the Cabin case, previously generated stash contents in a save do not retroactively change from source edits.

## Recommended next implementation (not performed)

Extend the same **exact actor+root+category+item field patch** method from the Cabin implementation to the six further medical actors, using the existing MEDICAL and Medkit/ArmyMedkit controls. For MEDICAL=0x, both item selection **and** MinCount/MaxCount should be zero on each eligible source row; preserve extra equipment, booster siblings, normal cheap/rare subgenerator and SmartLoot lists. Investigate lowercase `medkit` as an independent source-ID question. Classify ammo/grenade direct entries against their own existing slider contracts before inclusion. Add negative generated-CFG tests to ensure untouched other authored rows.

No code, presets or UI controls were changed in this audit.
