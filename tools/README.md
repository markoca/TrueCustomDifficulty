# External tools

This source directory contains first-party audit/test scripts. The published tools/ folder is for optional manually supplied external packer binaries.

True Custom Difficulty does **not** bundle or download third-party executables. Users supply them manually.

Published layout:

```text
TrueCustomDifficulty.exe
tools/
  repak.exe
  retoc.exe
```

## Required for Build Mod

A non-empty v0.1.51 build requires both tools:

- **repak** packs the generated BPatch CFG files into `TrueCustomDifficulty_P.pak` using PAK V11 and mount point `../../../`.
- **retoc** creates the matching UE5.5 `TrueCustomDifficulty_P.utoc` and `TrueCustomDifficulty_P.ucas` companion container.

The application does not download either executable. If one is missing, Build Mod stops with a clear error and does not publish a partial output.

The same tools may also be used manually for extraction/development/maintenance workflows, but those workflows are outside the normal player build path.

## Override verification harness

Override mapping evidence is documented in `../OVERRIDE-VERIFICATION-HARNESS.md`; there is no automated verification harness in this repository.

## WPF aggregate / toggle visibility regression

UI aggregate bindings are maintained directly in WPF source and the UI documentation.

## Mixer fader pilot regression

The mixer contract is described in the UI design documentation; publishing does not run any UI test scripts.

## CFG placement audit

`python tools/audit_cfg_placement.py --output docs/cfg-placement/v0.2.53` verifies all catalog parameters against their concrete CFG writer routes. It is intentionally static and does not launch the application.

## Pre-release test runner

Run `python tools/pre_release.py list` in the repository root to view the registered tests. The `static`, `source`, `writer` and `all` modes, their platform requirements, evidence reports and mandatory manual game checks are described in [Pre-release testing](../docs/RELEASE-TESTING.md). Test logs and fixtures are written to ignored `audit/pre-release/` and do not form part of the runtime release.

### Original CFG ancestry and special arrays

`audit_cfg_source_values.py` now checks physically declared source
`{refkey=...}` ancestor values, reporting `inherited_exact`
separately from directly authored fields. Wrong, missing or cyclical
ancestry is not silently accepted. The CI-backed
`test_cfg_source_inheritance.py` validates this behavior.
`audit_combat_source_structures.py` checks the exact 68 Combat
source routes plus 14 Master agents / 66 cooldown tags / 2 PSY
effects using actual supplied CFG ZIPs. These are source facts,
not game-engine BPatch runtime tests.

### Health/Stamina direct CFG source and real writer

`audit_health_stamina_source.py` compares 69 source edit points against the original decoded GameData/DLC1 CFG archives, including one `Player→[0]` inherited VaultingParams value. `tests/HealthStaminaPatchContract` executes 46 real WPF model-to-CFG writer scenarios on Windows CI and stores generated patch evidence. Both are registered in `pre_release.py`; neither runs the game.

C2 writer scenario breakdown: 41 live Health/Stamina parameter/difficulty cases plus five Master mechanics toggle full-list replacement cases = 46; five original Stamina speed-state CFG definitions are source-only and are not loaded into the app.

### Phase 9 Survival coverage

`tests/SurvivalPatchContract` checks 59 production C# ViewModel-to-BPatch outputs on Windows. `pre_release.py source` now includes the original-source check for all 139 `survival.*` edit points. Neither source static parity nor generated CFG data proves in-game behavior. Details: [Survival Phase 9](../docs/DEEP-CFG-AUDIT-PHASE9-SURVIVAL-2026-10-09.md).
## Original consumable effect source verification

`python3 tools/audit_consumable_effect_source.py --base-zip "/path/to/STALKER2-GameData-CFG.zip"`

This compares Energy Drink's 5/15/45-second effects (native -100%/-30% SPDrain and regeneration), the regular/Limited/Weird Kettle lists, inherited eight-action scope, and Water duration sources with the original decoded CFG and the production writer baseline. The tool is a source-and-structure check, **not** gameplay stacking proof. It also runs in the source phase of `tools/pre_release.py`.

