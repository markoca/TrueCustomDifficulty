# Project Consistency Audit — 9 October 2026

## Current, authoritative checks

The current application catalog defines **244 parameters**, **152 visible
anchors**, **231 effective player-editable dimensions**, **1,224 concrete
direct writer edit points**, and **43 special-writer parameter references**.
The latest user-side `audit_ui_cfg_contract.py` result reported **zero
direct route collisions** and complete 244/244 writer-route coverage.

The whole-project validator must check the **current code and manifests**.
Its former v0.2.x fixed assertions referred to 178 parameters, old radar
geometry, archived CSVs and 242 weapon-attached `AmmoMinCount/MaxCount`
edits since superseded by the separate `NPC_Ammo_Smart` corpse reward
clone. The old versions are NOT a permissible source of truth for current
displayed controls.

### Combat

- Mutant Health: 14 catalog scalers writing 16 `VitalParams.MaxHP` targets.
  **0.25–2.0x at 0.10x steps**, not the archived 0.01x radar contract.
  Novice/Stalker/Veteran reference **1x**; Master references **0.5x**.
- The special ability family remains **runtime-test-required**. Static
  presence of Master or Veteran override blocks does not establish
  in-game behavior, nor does a PASS prove their gameplay semantics.
- `Your Headshot Damage` is a multiplier, **not an additive extra
  damage bonus**. Catalog text was corrected; numeric CFG fields
  are unchanged.

### Current source/UI alignment

Current separate native boards were checked by registered class,
parameter ID set, native pointer/keyboard handlers and live VM backing,
**not** by deleted polar-axis literal text. Breakable Boxes belongs to
`AggregateVisualControl / AmmoDistribution`. Stash reward sources
remain compatible with their current visible family. Nine specialized
WPF classes continue to use historical `*Radar` class names where required
for XAML compatibility.

Survival now contains **32 visible controls** (30 source-confirmed,
2 runtime-test-required) and **93 direct edit points**. The consistency
audit must never shrink these back to historical 23/70 counts.

### Production data and presets

The 12 factory preset JSONs retain their current values. In particular,
Hardcore currently uses 0.79 for several removal percentages; Hardcore+
uses 0.92. The validator's pinned expectations were synchronized with
those **existing** files; the preset files themselves were not rewritten.

The catalog and project assembly metadata previously disagreed:
`parameters.json` identified `0.9.26.82`, whereas the project had
`0.9.26.52`. The project version was synchronized to the already-present
catalog version; no 1.0 release was created.

## Evidence status: do not overstate coverage

Historical archived CSV/suite data (old decision matrix, provenance,
placement, override test packs, old generator runtime test packers) are
**absent from the current repo**. Their checks are printed as
`[NOT RUN]` rather than incorrectly failing current runtime sources or
claiming old tests have passed. Active manifests and current writer
targets are still checked. This is not a replacement for full vanilla
CFG provenance validation or gameplay tests.

An independent static contract test passed in the supplied local log:
`python tools/audit_ui_cfg_contract.py` with 244/244 route coverage.
After pulling this commit, run:

```bash
python tools/audit_combat_contract.py
python tools/audit_ui_cfg_contract.py
python tools/audit_project_consistency.py
chmod +x publish.sh && ./publish.sh
WINEPREFIX="$HOME/.wine-s2" WINEDEBUG=-all wine "publish/win-x64/TrueCustomDifficulty.exe"
```

Do not claim the full project consistency audit, .NET publish or Wine
runtime acceptance passed until those steps have been performed locally.
For the 68 Combat direct edit-point comparison, execute
`tools/audit_cfg_source_values.py --parameter-prefix combat.`
against the actual decoded base/DLC1 CFG archives. The Master and
Veteran special-array effects require in-game validation.

## Automated CI guard

`.github/workflows/static-audit.yml` executes the three active Python
validators on changes to `main`, pull requests, and manual dispatch. This
prevents old NameError/path-regression failures from being declared fixed
without at least executing the complete static validator. GitHub Actions
status must be checked before claiming CI PASS. It does not perform .NET
publishing or prove runtime effects in S.T.A.L.K.E.R. 2.

The first GitHub Actions execution exposed an old stamina-audit dependency
on `_acr_ids`, whose original definition was located in a retired Action
Costs radar gate. The current source-backed seven-ID cohort is now declared
explicitly for the stamina audit so the validator does not depend on
historical geometry checks.

The CI runner also exposed a local-only root-file assumption: `prompt.md`
is not present in the workflow checkout. The validator now requires the seven
operational root files, allows the optional prompt when present, and still
fails on unexpected root files; it does not require local notes to build.

## Canonical pre-release tests

[Pre-release test runbook](RELEASE-TESTING.md) now tracks all three static validators, three checks against matching original GameData/DLC1 CFG archives, all 11 Windows production writer scenarios, CI evidence tied to the exact commit and manual Wine/gameplay acceptance. Run `python tools/pre_release.py list` and then select `static`, `source`, `writer` or `all`. The runner reports missing prerequisites as NOT RUN or a failure, never a synthetic PASS.

## 2026-10-15 — Responsive semantic slot cleanup

Compared the central `ResponsiveParameterGridPanel` map against all 247 source catalog parameter IDs and explicit `ParameterViewModel.IsControlVisible` rules. Removed 19 stale **layout-only** dictionary rows: 3 nonexistent legacy IDs, 6 explicitly filtered Combat PSY/repeated-hit entries and 10 `exposeInUi:false` backing children of live composite editors. All 16 supported backing-source CFG/preset records remain unchanged. The Stamina Exhaustion anchor `stamina.sprintDisableThreshold` is intentionally retained although catalog-hidden as a standalone editor; it is the actual visible family anchor. The four paired Economy layout anchors and responsive sizing logic are preserved. The audit checks all 104 surviving slot IDs, rejects nonexistent/filtered and unexpected hidden mappings, and requires the preserved visible anchors. Static source checks passed before pushing; .NET/WPF/Wine runtime acceptance remains pending.
