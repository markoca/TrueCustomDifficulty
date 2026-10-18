# Source-backed regional location of ordinary physical stashes

**831 ordinary world SmartLoot actors** are linked by `ActorSID` to their original `X`/`Y` placement and regional outlines. The reproducible result is `src/TrueCustomDifficulty/Data/stash-regular-actor-regions.csv`.

The original actor coordinate data comes from `src/TrueCustomDifficulty/Data/smartloot-regular-world-actor-manifest.csv`. Region names and **22 geographic polygons** come from [thexii/stalker-map regions.json](https://github.com/thexii/stalker-map/blob/38042ac5b73bb7855adbd55b27a37c080ee10ee8/src/assets/data/hoc/regions.json) and its [English names](https://github.com/thexii/stalker-map/blob/38042ac5b73bb7855adbd55b27a37c080ee10ee8/src/assets/data/hoc/en.json), pinned to upstream commit `38042ac5b73bb7855adbd55b27a37c080ee10ee8`, **MIT-licensed**. Only XY polygon vertices, source keys and English regional names were vendored as `src/TrueCustomDifficulty/Data/stash-region-boundaries.json`. All coordinates are native world XY; no scaling, swapping or map image pixel approximations are applied.

The independent [MrCatOff/stalker-exporter](https://github.com/MrCatOff/stalker-exporter) documents a higher-fidelity way to regenerate 22 outlines from the game's `LevelBitmaps/T_Regions_Map` (2048² regional bitmap). The present catalog uses publicly published polygons, *not* a new raster trace from the user's installed game version, so locations at boundaries require additional verification.

**829/831** actors are inside exactly one published geographic polygon. The two remaining are outside, both close to a border. They stay `Region=UNRESOLVED`; `ClosestRegion` and `ClosestBoundaryDistanceUnits` are diagnostic hints, not confirmed regional assignments.

## Geographic region distribution

| Region | Ordinary world stash actors |
|---|---:|
| Garbage | 90 |
| Chemical Plant | 89 |
| Zaton | 86 |
| Lesser Zone | 72 |
| Rostok | 61 |
| Wild Island | 53 |
| Prypiat | 42 |
| Yantar | 41 |
| Burnt Forest | 40 |
| Malachite | 39 |
| Red Forest | 34 |
| Duga | 34 |
| Swamps | 28 |
| Cement Factory | 27 |
| Yaniv | 23 |
| Cooling Towers | 22 |
| Cordon | 22 |
| Jupiter | 20 |
| SIRCAA | 3 |
| Generators | 3 |
| UNRESOLVED | 2 |
| **Total** | **831** |

## Two boundary cases

| ActorSID | Closest region only | Distance in native world units | Generator |
|---|---|---:|---|
| `3DC572304DD3E6EA9B6266A59F4A03BA` | Prypiat | 101.078 | `Stash_Expensive_*` |
| `75B17AF54B2B381145BEFEB6A125FF0A` | Cordon | 266.348 | `Cordon_CampInTunnel_StashGenerator1_*` |

The distances are to the *simplified published polygon outline*, not a player walking distance. The native bitmap's region IDs or updated polygons are required before these should be considered verified.

## Corrected assumptions and evidence columns

The old CSV inferred `Region` from the `OriginalItemGeneratorSIDs` prefix. This was useful as an initial clue, but **not reliable geography**. In the new CSV:

- `Region` is assigned by point-in-polygon only; `RegionEvidence=POLYGON_INTERIOR` for 829 rows.
- `GeneratorRegionHint` preserves old inferred names separately, never overriding geometry.
- `HintAgreement=CONFLICT` flags **21** rows where the old generator name implies a different location than the published polygon. An example is `CityCenter_EnerhetykPalaceofCulture`, previously assumed Rostok but located in Prypiat.
- `Region=UNRESOLVED` / `RegionEvidence=OUTSIDE_PUBLISHED_POLYGON` flags the two edge cases. Do not treat a nearest-region hint as an actual region.
- Static inventory classification **does not** prove that an actor is spawned, accessible, enabled by quests, or safe to patch. This manifest only covers 831 ordinary SmartLoot actors; the broader original physical dump contains 1,569 ItemContainers, including quest, special, DLC and development entries.

## Regenerate / verify offline

```bash
python3 tools/generate_stash_actor_regions.py
python3 tools/generate_stash_actor_regions.py --check
python3 tools/audit_stash_actor_regions.py
python3 tools/pre_release.py static
```

The offline audit checks data consistency and regression sentinels. It does **not** simulate gameplay or check a compiled executable. No UI, drop chance, weight or runtime generator logic is modified in this work.
