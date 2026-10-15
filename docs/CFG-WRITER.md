### 2026-10-16 — Build Difficulty: LocalizationWorkbench-inspired compact activity rail

The bottom command bar now has **one compact, bounded live build-progress rail** in the available gap **between Settings on the left and the profile selector/action buttons on the right**. **Collapsed when idle** (no reserved blank space); visible only from the start of actual asynchronous CFG/package work through final install/uninstall. It reuses the matching LocalizationWorkbench `UI/StalkerTheme.cs:StalkerProgressBar` rendering cues: flat 8-DIP graphite track, 1-DIP muted #3B4038 outline, 6-DIP warm-yellow #FFD438 to #F0C419 gradient fill, and a restrained live pulse. A small muted current-stage caption sits above the track. No full-width band, permanent status banner, numeric timer, or fabricated per-process percentage.

The six **completed-stage** values (0..6) are deliberately coarse: 0 **WRITING CFG**; 1 **PACKING PAK**; 2 **BUILDING IOSTORE**; 3 **VERIFYING PACKAGE**; 4 **PUBLISHING OUTPUT**; 5 **INSTALLING**; 6 **COMPLETE**. The no-mod case skips packing, reports **CLEARING OUTPUT**, then **INSTALLING** and remains subject to the same failure/error behavior. Because repak/retoc do not expose reliable granular percent, the bar never claims a stopwatch-derived completion fraction. Long-running packing shows a subtly pulsing current stage until the next actual milestone.

The build operation runs off the WPF dispatcher using `Task.Run` with `Progress<BuildProgressUpdate>` marshalled onto the dispatcher; editing, preset selection and Settings are disabled during the operation. Concurrent builds and closing mid-packaging are prevented. The rail collapses before the outcome dialog on both success and failure. The original `ModBuildService` packaging, validation, atomic Output replacement, `ModDeploymentService` atomic install, and vanilla/uninstall semantics remain unchanged. No CFG source, source ID, slider or gameplay mapping was modified.

**Verification:** `python tools/audit_build_progress_contract.py` (also in CI and pre-release static suite) checks XAML/footer placement, state binding, milestones and no fake percentage. **Manual Wine acceptance required:** start with a real modified preset and working repak/retoc; see live label changes over ~10 seconds, smoothly pulsing gold fill, no UI freezing, controls disabled, hidden immediately on completion, and existing success/error dialogs. Repeat with missing retoc/repak, a fully vanilla no-mod build, and a second-click attempt; verify no stale rail, no duplicate execution, and proper atomic install. At minimum 1024×576 and normal 1600×900 check the footer has no text clipping, shifted buttons or overlay.

# CFG BPatch Writer and Mod Build

## Final 2026-10-15 source-to-production BPatch mapping review

The current **2.0.5 catalog contains 248 source IDs** (235 player-editable) with **213 direct IDs, 1,230 concrete CFG leaves and 35 explicit special writer methods**. No unsupported/deferred IDs remain, and no two independent direct parameters own one concrete CFG leaf. `tools/audit_final_cfg_mapping.py` checks dispatch, file/root/key/operation and all twelve stock profile values for source-native integer no-ops. New `tests/AllControlsPatchContract` executes the actual production writer for all 235 editable IDs on Windows; it complements existing detailed domain writer tests and must pass before claiming emitted output fully verified. A-Life `scaleIntegerHalfUp*` values that quantize to vanilla never produce a patch: 108 ineffective stock edits were neutralized and custom ineffective edits now receive an explicit Build error. **None of this proves that STALKER 2 loads or applies every patch.** See [final mapping and BPatch audit](FINAL-CFG-MAPPING-AUDIT-2026-10-15.md).

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
