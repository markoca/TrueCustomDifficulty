# ARTIFACT HUNTING (2026-11-16)

Anomalies tab → **ARTIFACT HUNTING** board: four compact cards and a read-only RARITY BY ZONE strip.

| Card | Id | Range | Vanilla | CFG |
|---|---|---|---|---|
| RESPAWN RATE | `environment.artifactRespawnRate` | 0.25x–4x / 0.25 | 1x | `MinCooldown`, `MaxCooldown` (5400) ÷ rate (`inverseScale`) |
| SPAWN CHANCE | `environment.artifactSpawnChance` | 0x–4x / 0.1 | 1x (25% +15%) | `SpawnChanceBase` 25, `SpawnChanceBonus` 15 × factor, capped at 100 (`scaleClamp100`) |
| RARE LUCK | `environment.artifactRareLuck` | 0.25x–4x / 0.25 | 1x | `RarityChance`: Rare and Epic × k, all four renormalised to 100 (special writer) |
| PER SPAWN | `environment.artifactCount` | 1–3 | 1 | `Count` (`set`) |

All four are `runtime_test_required` and independent of the selected difficulty.

## Source (S.T.A.L.K.E.R. 2 2.1 base dump)

* `GameData/ArtifactSpawnerPrototypes.cfg` has 98 spawners, but the world's anomaly fields use
  **`UniversalArtifactSpawner`** (73 world spawn actors in `SpawnActorPrototypes.cfg`) and its child
  **`LesserZoneMagneticShortDistance`** (1, radius 800). Only these two are edited.
* Per-artifact spawners (Eye, Battery, …) appear only on test maps; quest spawners (`Quest*`),
  unique ones (`Weird*`, DLC1 `WaterDrip`) and `SQ13_Soul` are **never touched**.
* Both edited spawners have four blocks `Newbie / Experienced / Veteran / Master`. These are the
  **area rank** of the anomaly field, not the game difficulty. Count, cooldown and chance are equal in
  all four; the rarity split differs:

| Zone rank | Common | Uncommon | Rare | Epic |
|---|---:|---:|---:|---:|
| Newbie | 80 | 20 | 0 | 0 |
| Experienced | 50 | 48 | 1.9 | 0.1 |
| Veteran | 30 | 59 | 10 | 1 |
| Master | 10 | 65 | 20 | 5 |

* RARE LUCK keeps Newbie areas without rare artifacts (as in the game). Example Master at 2x:
  Rare 32 / Epic 8 (40% rare+, was 25%).
* The cooldown unit is not documented in the CFG, so RESPAWN RATE is shown as a rate, never a time.
* `CoreVariables.ArtifactSpawnPityStructs` (luck growth per failed attempt) is not changed.
* `ArtifactSpawnPolicy` holds the ids, spawners and vanilla split; the writer and the board use the
  same `Rarity()` so the strip shows what is written.

## Checks

* `tools/audit_artifact_hunting.py` (with `TCD_BASE_DUMP=<dump>/base` also compares the dump).
* `tools/audit_cfg_source_values.py --parameter-prefix environment.artifact` → 40/40 exact.
* Factory presets do not store these ids yet; they load at 1x.

**Card texts (2026-11-17):** `fields regrow at game pace` / `2× faster` / `4× slower`; `artifact chance 25% (+15% bonus)` (`0% · fields stay empty` at 0); `25% rare finds in Master zones`; `1 artifact per spawn`. Track ends show the figure at each slider limit.
