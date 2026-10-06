# v0.3.0 Production Implementation

v0.3.0 is the first release that treats the audited catalog/writer surface as the production application contract rather than as an expanding research surface.

## Production source of truth

The only C# application source tree is:

`src/TrueCustomDifficulty/`

`TrueCustomDifficulty.sln`, `publish.sh`, and `publish.ps1` all target that project. Historical root/nested C# mirrors were removed so there is no second implementation that can silently drift from the code that is actually published.

Canonical audit/runtime data is kept only under root `Data/`; standalone root JSON/CSV compatibility mirrors were removed in v0.3.1.

## Catalog-driven UI

The production UI is generated from the canonical embedded `parameters.json` catalog:

- 178 total catalog parameters
- 167 player-facing controls
- 11 hidden audit-only parameters
- 159 sliders
- 7 toggles
- 1 choice slider
- 9 tabs

No gameplay control is manually instantiated outside the catalog path.

## Writer integration

All 178 catalog IDs have a production writer route and `deferredParameterIds` is empty.

The writer now applies one additional production rule:

> A dependent control whose parent toggle is OFF is inactive and must not emit CFG output.

This matters for the 15 Master child sliders. Their custom values remain in memory and are preserved in presets, but they do not leak into the built mod while the owning Master mechanics bundle is disabled.

The footer Modified/Vanilla counts use the same active-control rule, so they describe the controls that can currently affect the build.

## Dependency UI behavior

When a Master parent toggle is switched OFF:

- its child controls are hidden;
- the hidden child values remain preserved for a later re-enable;
- the child values are excluded from CFG generation;
- if the side panel was showing a child that just became hidden, selection moves to the first visible control in the tab.

## Startup validation

The production catalog loader now rejects invalid application data before the UI starts. It validates:

- default Base Preset exists in `difficultyProfiles`;
- unique/valid tabs;
- every parameter belongs to a known tab;
- supported control types only (`slider`, `range`, `toggle`, `choiceSlider`);
- numeric slider vanilla values and slider bounds/step;
- boolean toggle vanilla values;
- valid production support status (`confirmed` or `runtime_test_required`);
- every `requiresToggle` points to a real visible toggle and cannot point to itself;
- existing slider-constraint validity checks.

## Runtime-test-required controls

The 30 visible controls still marked `runtime_test_required` remain fully implemented in the normal UI and production writer. The status means only that final engine behavior is awaiting later in-game observation; it does not mean the control is deferred or omitted from production.

## What v0.3.0 does not change

- no new gameplay controls;
- no removed gameplay controls;
- no CFG target remapping;
- no new runtime verifier/EXE workflow;
- no change to the v0.2.54 placement/provenance contract;
- no automatic mod install/uninstall lifecycle.

## v0.3.1 repository/runtime separation

Source audit tooling remains under repository `tools/`. Portable application output is now emitted under `publish/win-x64/`, whose own `tools/` directory is reserved for user-supplied `repak` and `retoc`. Runtime `Presets/` and `Output/` also live there, so publishing no longer repopulates the repository root with application artifacts.

## v0.3.2 Loot UI integration

The live catalog/writer surface is now **179 total parameters / 168 visible controls**. `loot.cheapStashGrenadeQuantity` shares the existing Cheap-stash clone/redirect writer with Ammo Quantity and RGD-5 Chance. The UI keeps Chance and Quantity as separate adjacent sliders and displays source-grounded ammo contents/count ranges for stash/crate quantity controls.
