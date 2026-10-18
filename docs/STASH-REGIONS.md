# Ordinary physical stash actors — region labels

Source: `src/TrueCustomDifficulty/Data/smartloot-regular-world-actor-manifest.csv` (**831** ordinary SmartLoot actors). Generated file: `src/TrueCustomDifficulty/Data/stash-regular-actor-regions.csv`. Regenerate with `python tools/generate_stash_actor_regions.py`.

Region names are inferred from original `ItemGeneratorSID` prefixes, **not verified geographic polygons**. Newly recognized source aliases: `Wild` → Wild Island, `Backwater` → Zaton, `STCMalachite` → Malachite, `Swamp` → Swamps, `DugaRadar` → Duga, `SIIRCA` → SIRCAA. Existing provisional mappings `Promzona` → Chemical Plant and `CityCenter` → Rostok remain subject to map-boundary verification. `Districtno.1`, `Districtno.2`, generic `GamePass`, and ambiguous generators remain **UNRESOLVED**, rather than assigning unsupported geography. This classification does not cover all 1,569 physical ItemContainers and does not alter gameplay.

| Region | Actors |
|---|---:|
| UNRESOLVED | 207 |
| Chemical Plant | 86 |
| Garbage | 77 |
| Lesser Zone | 70 |
| Zaton | 52 |
| Wild Island | 51 |
| Yantar | 33 |
| Burnt Forest | 33 |
| Malachite | 33 |
| Red Forest | 31 |
| Cement Factory | 26 |
| Swamps | 26 |
| Duga | 25 |
| Cordon | 22 |
| Cooling Towers | 21 |
| Rostok | 18 |
| Jupiter | 17 |
| SIRCAA | 3 |
| **Total** | **831** |
