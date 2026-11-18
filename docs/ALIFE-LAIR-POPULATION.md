# LAIR POPULATION (2026-11-18)

How many inhabitants a world lair can hold, on the A-Life tab next to GROUP SIZE.

| Control | ID | Scales |
|---|---|---|
| HUMAN CAMPS (*Human Lair Population*) | `alife.lairPopulation.human` | 508 faction/rank entries: 17 human factions, 288 base + 220 DLC1 |
| MUTANT NESTS (*Mutant Lair Population*) | `alife.lairPopulation.mutant` | 608 entries: 17 mutant and zombie species, 352 base + 256 DLC1 |

Range 0.25×–3× in steps of 0.05, vanilla 1×. Each eligible `MaxSpawnQuantity` (`LairPrototypes.cfg → <Lair>.Preset.PossibleInhabitantFactions.<Faction>.SpawnSettingsPerPlayerRanks.<Rank>`) is scaled with half-up rounding. A vanilla 0 stays 0, and a non-zero count never drops below 1, so no lair loses a faction. Rank and faction differences are kept.

The board shows one real lair by your rank under each card:
- `BANDIT CAMP BY RANK 6 / 6 / 7 / 8`
- `TUSHKAN NEST BY RANK 14 / 16 / 20 / 24`

## Group size vs lair population

| | File | Meaning |
|---|---|---|
| GROUP SIZE | `ALifeDirectorScenarioPrototypes.cfg` `MaxCount` | how many show up together in one A-Life encounter |
| LAIR POPULATION | `LairPrototypes.cfg` `MaxSpawnQuantity` | how many a lair can hold |

## Not edited

Each of these has a reason; `tools/build_lair_population_manifest.py` encodes the exclusions:

- the empty test prototype `[0]` and `RestingLairDefault`;
- settlement guard posts (`Guard*`), which defend bases and are not difficulty;
- friendly `Scientists` camps;
- story / special prototypes: `SIRCAA_*`, `MALACHITE_*`, `Object68`, `FW_*` (DLC1), including their factions.

Respawn timers (`…RespawnTimeSeconds`, `WipeRespawnTimeoutSeconds`), starting fill (`InitialSpawnQuantityPercent`) and the archetype mix (`MinQuantityPerArchetype`, `SpawnWeight`) are untouched. Vanilla already has entries where the archetype minimums exceed `MaxSpawnQuantity` (Freedom Newbie: 6 vs 7), so a lower cap is a state the game already handles.

## Code

- `Data/lair-population-manifest.csv` (embedded): 1116 rows (640 base + 476 DLC1), built from the 2.1 CFG dumps.
- `Services/LairPopulationPolicy.cs`: IDs, manifest loader with size and shape checks, `Scale`, and the board example.
- `Services/CfgPatchWriterService.LairPopulation.cs`: BPatch assignments. A value that rounds every lair back to vanilla is refused at build time.
- Board `LairPopulation` in `AggregateVisualControl` (two economy cards and an example line). Family wiring is in `ParameterViewModel` and `MainViewModel`.
- Baseline `special.lairPopulation`, census 417 / 211 visible.

## Factory presets

Absent from the table = 1×.

| Profile | Human | Mutant |
|---|---|---|
| Anomaly Runner | 0.75× | 0.85× |
| Hardcore | 1.25× | 1.15× |
| Hardcore+ | 1.5× | 1.4× |
| Mercenary | 1.25× | 1× |
| Mutant Hunter | 1× | 1.3× |
| Outcast | 1.15× | 1.1× |
| Scavenger | 0.75× | 1× |
| Tactical | 1.25× | 1× |
| Wanderer | 0.85× | 1× |
| Field Medic, Immersive, Smuggler | 1× | 1× |

Older stock preset files upgrade only when byte-identical (`PreLairPopulationFactoryBlobs`; `tools/factory_preset_lair_population_2026_11_18.json`).

## Gate

`tools/audit_lair_population.py` is in `pre_release.py`. With `--base` and `--dlc` it rebuilds the manifest from the game CFG and requires an exact match. That has passed against the 2.1 dumps.

## Not yet verified

The .NET build, a Wine run and in-game behaviour have not been checked: whether camps and nests actually fill to the new cap, and how fast they refill.
