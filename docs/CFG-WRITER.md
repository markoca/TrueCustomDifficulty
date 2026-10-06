# CFG BPatch Writer and Mod Build

## Purpose

v0.1.51 closes the player build pipeline from UI state to a finished STALKER 2 IoStore mod. The writer remains a **minimal BPatch generator**, not a full-CFG rewriter.

The current user-supplied base-game and DLC1 CFG dumps remain the development source of truth used to derive the embedded writer baseline. Players do not import those dumps.

## Player build flow

```text
UI / loaded JSON preset
        |
        v
CfgPatchWriterService
        |
        v
temporary BPatch CFG staging tree
        |
        v
user-supplied tools/repak.exe
        |
        v
CFG .pak (V11, mount ../../../)
        |
        +--> user-supplied tools/retoc.exe
                |
                v
        companion IoStore .utoc/.ucas (UE5_5)
        |
        v
Output/
├── TrueCustomDifficulty_P.pak
├── TrueCustomDifficulty_P.utoc
└── TrueCustomDifficulty_P.ucas
```

`Output/` contains **only the final triplet**. BPatch CFG files, build plans, logs and temporary packing files are not retained there.

## Temporary CFG staging

Only CFG files needed by modified controls are emitted. The writer never copies complete vanilla CFG files.

A prototype source such as:

```text
GameData/EffectPrototypes.cfg
```

is staged as:

```text
Stalker2/Content/GameLite/GameData/EffectPrototypes/
    EffectPrototypes_patch_TrueCustomDifficulty.cfg
```

with localized BPatch roots, for example:

```cfg
RazorAnomalyDamageDPSLow : struct.begin {bpatch}
    ValueMax = 50
    ValueMin = 40
struct.end
```

Nested existing structures use `{bpatch}` at every traversed struct level.

A singular/global CFG such as `GameData/CoreVariables.cfg` is staged in the same logical directory as:

```text
GameData/CoreVariables.cfg_patch_TrueCustomDifficulty
```

## Packing contract

A non-empty Build Mod requires **both** user-supplied executables:

```text
tools/repak.exe
tools/retoc.exe
```

The project never bundles or downloads them.

### 1. CFG PAK

The generated BPatch files are ordinary loose CFG files, so `repak` is used to place them in the actual `.pak` container:

```text
repak pack <temporary-staging> <temporary-packed>/TrueCustomDifficulty_P.pak --mount-point ../../../ --version V11
```

The staging tree already starts at `Stalker2/Content/...`, so the resulting archive paths resolve as `../../../Stalker2/Content/...`.

### 2. IoStore companions

`retoc` then creates the UE5.5 IoStore companion container:

```text
retoc to-zen <temporary-staging> <temporary-packed>/TrueCustomDifficulty_P.utoc --version UE5_5
```

For a CFG-only tree, retoc does not carry the CFG files into IoStore; they remain in the already-created `.pak`. retoc creates `.utoc/.ucas` and leaves the existing same-name `.pak` untouched.

The final expected triplet is:

```text
TrueCustomDifficulty_P.pak
TrueCustomDifficulty_P.utoc
TrueCustomDifficulty_P.ucas
```

All three must exist and be non-empty before the application publishes them to `Output/`.

## Writer coverage

### Runtime 2.0.5 baseline audit

For v0.1.63 the writer baseline was checked directly against the fingerprinted runtime 2.0.5 CFG dump:

- **699/699** ordinary edit points matched the exact source file, physical root, nested node path, key and `sourceRaw`;
- Fast Travel now patches the physical indexed roots `[0]..[14]` from `FastTravelPrototypes.cfg`; the guide SID is metadata, not the emitted root name;
- Additional Mechanics, Corpse Smart Loot, PSY override arrays and mutant cooldown-family source structures were separately cross-checked.

This is a structural/source audit. It does not turn every mapped control into an in-game runtime confirmation.

Current player catalog:

- 172 visible controls total
- 172 writer-supported controls
- 0 visible deferred controls
- 13 former aggregate Loot controls were removed from the visible catalog in v0.1.62 because they did not meet the strict 1:1/evidence requirement

Supported now:

- all `confirmed` controls
- direct `runtime_test_required` controls
- direct transforms: `set`, `scale`, `scaleNonZero`, retained-fraction delta
- Advanced Mechanics list membership
- NPC corpse smart-loot selection
- Psy override arrays
- Mutant cooldown-family arrays
- aggregate item-weight families (Ammo + Consumables)
- energy-drink sleepiness-relief effect family
- production-only Cheap stash ammo quantity and grenade chance through embedded redirect manifests
- production-only Medium stash ammo quantity through an embedded 450-assignment redirect manifest
- production-only Expensive stash ammo quantity through an embedded 305-assignment redirect manifest, with compatibility for the existing coupon-chance control
- production-only destructible ammo and grenade quantity through embedded redirect manifests

The former 13 aggregate Loot controls are no longer instantiated as player controls. Their IDs remain only in `parameters.json -> deferred` as research notes, so they cannot be saved, loaded, edited, or accidentally emitted by the runtime writer.

## Difficulty handling

| UI | Runtime SID |
| --- | --- |
| Novice | `Easy` |
| Stalker | `Medium` |
| Veteran | `Hard` |
| Master | `Stalker` |

The writer emits only the selected Base Preset's difficulty-specific edits. Some structures exist fully only on Master/Stalker in vanilla; where the lower vanilla difficulties need a complete local structure, the writer creates only that required localized subtree.

## DLC1

DLC1 targets are written only when the detected game installation contains the DLC1 `.pak/.utoc/.ucas` triplet.
