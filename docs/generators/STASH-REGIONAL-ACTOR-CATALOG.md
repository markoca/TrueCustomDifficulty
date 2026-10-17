# Regional mapping of regular physical SmartLoot stashes

Source: `src/TrueCustomDifficulty/Data/smartloot-regular-world-actor-manifest.csv` on main. This is a **derived, conservative catalog of 831 regular SmartLoot actors**, not a verified regional classification of all 1,569 ItemContainer actors (these are different populations and cannot be equated without matching actor IDs).

The region label is taken from the original ItemGenerator SID prefix only if a single identifiable `*_StashGenerator` region prefix is present. It is **generator-naming evidence**, not a verified world-map polygon or proof that the actor is physically inside that region. Generic GamePass generators are `UNRESOLVED`. Actor SID and coordinates are preserved for future map-boundary verification. No gameplay/UI logic is modified.

**613 named by generator prefix; 218 unresolved.**

| Generator prefix (region-like label) | Actors |
|---|---:|
| `Backwater` | 52 |
| `BurntForest` | 33 |
| `CementFactory` | 26 |
| `ChemicalPlant` | 33 |
| `CityCenter` | 14 |
| `CoolingTowers` | 21 |
| `Cordon` | 22 |
| `DugaRadar` | 24 |
| `Garbage` | 77 |
| `Jupiter` | 17 |
| `LesserZone` | 65 |
| `Promzona` | 51 |
| `RedForest` | 31 |
| `Rostok` | 1 |
| `SIIRCA` | 3 |
| `STCMalachite` | 33 |
| `Swamp` | 26 |
| `UNRESOLVED` | 218 |
| `Wild` | 51 |
| `Yantar` | 33 |

See `src/TrueCustomDifficulty/Data/stash-regular-actor-regions.csv` for all 831 rows. Prefixes such as `Backwater`, `Promzona`, `Wild` and `CityCenter` are retained verbatim; translating them into player-facing map names requires a separate validated mapping.
