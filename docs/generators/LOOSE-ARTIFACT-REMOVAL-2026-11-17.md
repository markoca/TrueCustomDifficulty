# REMOVE LOOSE ARTIFACTS (2026-11-17)

Loot → World Loot → WORLD ITEM SPAWNS → **REMOVE** card (Loose Detectors, Loose Armor), third row **Loose Artifacts**
(`loot.looseArtifactsRemoved`, toggle, vanilla OFF, `runtime_test_required`).

## What it removes

The only fixed, non-quest artifact spots outside anomaly fields in the 2.1 source:

| Scope | Actor (SpawnActorPrototypes.cfg, WorldMap_WP) | Spawner |
|---|---|---|
| base | `484C79DF466E41A9462B50AE199636B9` | `WeirdKettleArtifactSpawner` |
| base | `62335D2342E2923A5CB965AE10DCA65F` | `WeirdNutArtifactSpawner` |
| base | `8AC5265746E9AA168D40DFB3CAFB68B9` | `WeirdFlowerArtifactSpawner` |
| base | `DE39BD774826AFC717945986E2D03255` | `WeirdBallArtifactSpawner` |
| DLC1 | `516E3E644ED344BEC05E9D893F589928` | `WaterDripArtifactSpawner` |

Each actor is `ESpawnType::ArtifactSpawner`, `SpawnOnStart = true`, and no other CFG references it.
Each spawner gives one fixed artifact (Count 1, cooldown 0, chance 100).

## Never touched

* Anomaly fields: `UniversalArtifactSpawner` (69 + 4 base, 6 DLC1) and `LesserZoneMagneticShortDistance`
  (see ARTIFACT HUNTING).
* `Quest*` spawners, the DLC1 WaterDrip actors used by quests (`C251EED1…`, `0D49F551…`, `52AFA7CD…`),
  and test/demo maps.
* World artifact `Item` actors are all `SpawnOnStart = false` (quest-spawned); no item generator or
  stash pack contains an artifact, so there is nothing else "loose" to remove.

## Writer

`CfgPatchWriterService.LooseArtifactRemovals.cs` reads the embedded
`Data/loose-artifact-spawner-manifest.csv`, checks the 4 + 1 inventory, and writes one
`<actor> : removenode` patch per actor (DLC1 row only when DLC1 is present), the same route as
REMOVE World Armor. An artifact already spawned in an existing save may stay; best on a new game.

## Checks

`tools/audit_loose_artifacts.py` (static; with `TCD_BASE_DUMP` / `TCD_DLC1_DUMP` it re-verifies each
actor against the source and that no quest references it).
