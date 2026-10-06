# Architecture

## Player runtime flow

```text
embedded parameters.json
        |
        v
ParameterCatalogService
        |
        v
MainViewModel / ParameterViewModel
        |
        +--> PresetService
        |
        +--> GameInstallLocatorService
        |
        +--> ModBuildService
                |
                +--> CfgPatchWriterService
                |       +--> embedded cfg-writer-baseline.json
                |       +--> temporary minimal BPatch CFG tree
                |
                +--> ExternalToolsService
                        +--> tools/repak(.exe)
                        +--> tools/retoc(.exe)
                |
                +--> Output/TrueCustomDifficulty_P.{pak,utoc,ucas}
```

The player runtime does not import CFG dumps, resolve an imported CFG tree, or generate a separate build-plan file. Those were development-stage mechanisms and were removed in v0.1.61 after the concrete writer baseline became the runtime source for patch emission.

## Base Preset semantics

The tool does not introduce an `EGameDifficulty::Custom` runtime difficulty. The player selects one of the game's normal difficulties, and the tool applies the preset to that same vanilla baseline:

```text
Novice  -> Easy
Stalker -> Medium
Veteran -> Hard
Master  -> Stalker
```

Only values changed from the selected baseline are emitted.

## Writer contract

`parameters.json` defines player-facing controls and semantic targets. `cfg-writer-baseline.json` contains the concrete edit points used by the runtime writer.

At build time the writer validates that every current parameter ID is exactly one of:

1. covered by a concrete writer path; or
2. explicitly listed as deferred.

It also rejects unknown IDs, supported/deferred overlap, and a mismatched supported-parameter count. This prevents a UI control from silently existing without a known writer status.

## Support states

- `confirmed`: mapping is confirmed from source CFG data.
- `runtime_test_required`: mapping and writer path exist, but exact player-facing behavior still benefits from an in-game test.
- `design_required`: a safe aggregate writer transform has not yet been implemented. Modified controls in this state block Build rather than being skipped.


## Development generator map

Generator research is intentionally outside the player runtime path. `docs/GENERATOR-MAP.md` and `docs/generators/*.csv` map the runtime 2.0.5 Item Generator and SmartLoot graphs so future controls can be designed from an explicit target set. The application does not load these CSV files at runtime.

The map keeps these relations separate:

```text
refurl/refkey                    -> generator inheritance
ItemGeneratorPrototypeSID       -> generator composition
Obj/Trade/Quest/SpawnActor refs -> external Item Generator consumers
StashPrototypeSID               -> SmartLoot consumers
Difficulty.CorpseSmartLoot      -> difficulty SmartLoot overlay
```

No writer behavior changes merely because a relation appears in the research map.

## Packing

The writer creates a temporary minimal BPatch tree. `repak` owns the CFG `.pak`; `retoc` creates the `.utoc/.ucas` companions. Temporary work is deleted after the operation. The project never bundles or downloads either executable.
