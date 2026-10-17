# DLC1 geographic gap audit — 2026-10-17

## Verified DLC regions and evidence

GSC confirms that **Cost of Hope** introduced **Iron Forest** and **Chornobyl Nuclear Power Plant (CNPP)**: https://www.stalker2.com/uk/news/cost-of-hope-update-2-0-everything-to-know . This confirms *region names*, not actor-to-region assignments.

The pinned community GeoJSON contains polygons named `iron_forest` and `CNPP`, but its boundaries do **not** cover every DLC1 actor in the CFG dump. The region polygons cannot be treated as authoritative DLC1 coverage.

## Exact uncovered DLC1 actors

The 1,569-row physical catalog has **20 DLC1 actors outside available polygons**:

- **18** on `WorldMap_WP`, X in **595061.972162–643129.557397**, Y in **275798.180876–321379.571483**. Their original stash prototypes contain SmartLoot references, but no available region marker establishes Iron Forest versus CNPP.
- **2** on `Dev_TestMap_DLC1`, outside the gameplay world map; do not infer any world region from these coordinates.

The per-actor inventory is `src/TrueCustomDifficulty/Data/dlc1-outside-region-actors.csv`. `DlcRegion=UNRESOLVED` and `RegionConfidence=NOT_CONFIRMED` are intentional: no proximity guess is silently promoted to a fact.

## Implications

- **Do not** expand geographic polygons by drawing around the 18 actors; that would invent map boundaries.
- **Do not** change the existing DLC1 eligible manifest, quest-protection rules, loot generators, UI, or patch writer based on this audit.
- Source `origin=dlc1` proves DLC1 dump provenance, **not** which of the two DLC regions physically contains an actor.
- Next source needed: actual DLC1 `RegionMarker` / `T_Regions_Map` game-native region definitions or another validated current-build map coordinate overlay.

This audit documents the gap and leaves the 102 currently outside-polygon actors untouched.
