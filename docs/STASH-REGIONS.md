# Ordinary physical stash actors — region labels

Source: `src/TrueCustomDifficulty/Data/smartloot-regular-world-actor-manifest.csv` (831 ordinary SmartLoot world actors). Output: `src/TrueCustomDifficulty/Data/stash-regular-actor-regions.csv`.

Region is inferred **only** from an explicit original ItemGeneratorSID prefix. `UNRESOLVED` means no reliable prefix. These labels are not proof of geometric location and do not cover the full 1,569 ItemContainer population. No UI or gameplay changes. `Promzona` and `ChemicalPlant` share a provisional Chemical Plant label; `CityCenter` is provisionally Rostok; both mappings require validation against world-region boundaries.

| Region | Count |
|---|---:|
| UNRESOLVED | 397 |
| Chemical Plant | 86 |
| Garbage | 77 |
| Lesser Zone | 70 |
| Yantar | 33 |
| Burnt Forest | 33 |
| Red Forest | 31 |
| Cement Factory | 26 |
| Cordon | 22 |
| Cooling Towers | 21 |
| Rostok | 18 |
| Jupiter | 17 |
| **Total** | **831** |

Reproduce with `python tools/generate_stash_actor_regions.py`.
