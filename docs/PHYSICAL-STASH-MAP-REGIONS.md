# Full physical ItemContainer regional inventory (GameData + DLC1)

**1,569** physically placed `ESpawnType::ItemContainer` actors from the 2026-10-17 GameData and DLC1 CFG dumps, joined by **ActorSID** with the existing **831** regular SmartLoot manifest entries. **738** additional ItemContainers are classified only as `OTHER_PLACED_CONTAINER` (not automatically ordinary stashes, safe to randomize, or even player-accessible).

The geographic labels are obtained from pinned third-party `tools/data/stalker2-community-regions.json` (original: https://github.com/joric/stalker/blob/main/data/regions.json), using `X/100, Y/100` and point-in-polygon. This is a community geographic approximation, not authoritative game-native region boundaries.

| Evidence | Actors |
|---|---:|
| Inside a single polygon (`POLYGON`) | 1465 |
| Near-border inference (`NEAR_BORDER_INFERRED`) | 2 |
| Outside available polygons (`OUTSIDE_POLYGONS`) | 102 |

Of the 102 outside actors, **36** have a `WorldMap_WP`-prefixed level, while **66** have other/blank levels, including test and dev maps. Outside actors remain **UNRESOLVED**; no nearest-region guess is made. The two previously audited regular stashes on polygon boundaries retain explicit inferred labels (`prypiat` and `cordon`).

**All 831** regular actors are matched to the earlier geographic catalog by ActorSID, and their regional labels agree. The other 738 are not yet protected/quest/DLC-safe classified; use the existing protected actor inventory before touching game logic. This is an **inventory-only** change.

## Regional counts (all ItemContainers, including test and quest actors)

| Geographic label | Count |
|---|---:|
| `cement_factory` | 49 |
| `chemical_plant` | 114 |
| `CNPP` | 67 |
| `cooling_towers` | 30 |
| `cordon` | 37 |
| `duga` | 62 |
| `garbage` | 151 |
| `generators` | 5 |
| `iron_forest` | 56 |
| `jupiter` | 47 |
| `lesser_zone` | 108 |
| `malachite` | 69 |
| `orbita` | 86 |
| `prypiat` | 114 |
| `red_forest` | 58 |
| `rostok` | 86 |
| `sircaa` | 16 |
| `swamp` | 33 |
| `UNRESOLVED` | 102 |
| `wild_island` | 64 |
| `yanov` | 40 |
| `yantar` | 67 |
| `zaton` | 108 |

## Files and reproducibility

- Source actor inventory: `src/TrueCustomDifficulty/Data/physical-stashes.csv` (preserves original CFG coordinates and LevelName).
- Derived regional inventory: `src/TrueCustomDifficulty/Data/physical-stash-map-regions.csv`.
- Generator and audit: `python3 tools/generate_physical_stash_map_regions.py --check` (use without `--check` to regenerate).
- Original extraction details: `docs/PHYSICAL-STASHES.md`.

**Limitations:** `OTHER_PLACED_CONTAINER` is a classification by *absence from the 831-row ordinary SmartLoot manifest*, not proof that a container is quest-specific. Polygon-edge assignments are explicitly tagged. Do not present all 1,569 objects as ordinary player stashes.
