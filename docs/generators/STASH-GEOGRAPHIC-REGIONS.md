# Geographic regions of 831 regular physical stashes

Source: original `smartloot-regular-world-actor-manifest.csv`, generator-prefix catalog, and third-party [joric/stalker regions.json](https://github.com/joric/stalker/blob/main/data/regions.json). This is a **geographic estimate from a community polygon set**, not game-native `T_Regions_Map` validation. The SVG-derived polygons use a 100:1 world-to-map scale; `X/100, Y/100` is used for point-in-polygon. Preserve original generator labels for audit.

Results: **217** of 218 previously unresolved generator labels have a polygon region; **829** of 831 actors fall inside a single polygon; **2** are outside but assigned explicitly as near-border inferences. Of actors with both labels, **548 agree** under the explicit alias mapping and **64 disagree**. Conflicts are not automatically corrected; a named generator can refer to a different region from physical placement. All 831 records and statuses are in `src/TrueCustomDifficulty/Data/stash-regular-actor-map-regions.csv`.

Status counts: {"AGREES_WITH_GENERATOR":548,"NEW_MAP_MATCH":217,"GENERATOR_MAP_CONFLICT":64,"NEAR_BORDER_INFERRED":2}.

Near-border inferred (not polygon-confirmed): `3DC572304DD3E6EA9B6266A59F4A03BA` → **prypiat**, 11.32 map units from its boundary, generic stash generator; `75B17AF54B2B381145BEFEB6A125FF0A` → **cordon**, 12.01 map units from its boundary and original `Cordon_CampInTunnel_StashGenerator1`. Both assignments are hypotheses with an explicit `NEAR_BORDER_INFERRED` status, not confirmed in-game boundaries.

Example resolutions: `006CDB98…` and `00BF1382…` = `yanov`; `024158BA…` = `rostok`; `058E73D8…` River Port and `0AEF0961…` Laundry = `prypiat`; `0C68FFD2…` = `chemical_plant`.

## Limitations

The community polygon dataset may not perfectly reflect the current 2.0.5 game build, particularly along borders. Source `regions.json` is stored at `tools/data/stalker2-community-regions.json` to make regeneration reproducible. This mapping only covers the **831 ordinary SmartLoot actors**, not all 1,569 ItemContainer objects. It changes no game/UI behavior.
