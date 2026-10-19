# TrueCustomDifficulty — segmented RC acceptance audit (2026-10-19)

## Rules and scope

Use **main** as the only source of truth. Each functional/UI correction must update source code, applicable catalog(s), tests and documentation in the **same commit**, then be pushed and verified on main before handing over local pull/build/Wine steps. Do not mistake an unexecuted CI check, static route, packaged binary or screenshot for an in-game gameplay PASS. Keep all previously signed-off RC copy decisions unless a source-proven contradiction is found.

The two mandatory UI contracts are [UI-DESIGN-LOCK.md](UI-DESIGN-LOCK.md) and [VISUAL-CONTROL-SYSTEM.md](VISUAL-CONTROL-SYSTEM.md). Actual WPF controls and their machine-readable catalog must always match those contracts. Prefer compact, discoverable control surfaces, no redundant explanatory strings or oversized full-width controls. Slider geometry/value positioning follows the Stamina Usage control; no special visual emphasis on Vanilla/Base numbers.

The [final RC copy pass](RC-FINAL-COPY-AUDIT-2026-10-19.md) already reviewed all **182 player-visible controls** across 11 tabs. This audit does not re-open that completed editorial work by default.

## Seven gates, in order

| Gate | Area | Acceptance evidence | State on 2026-10-19 |
| --- | --- | --- | --- |
| **01** | Repository, CI, build and test infrastructure | main SHA and workflow match; static scripts, Linux-to-Windows .NET 10 WPF build and Windows production-writer jobs actually execute and report PASS/FAIL with logs; publish integrity known | **IN PROGRESS / CI BLOCKED** |
| **02** | CFG source mapping and production BPatch writer | current 287-source inventory vs embedded baseline, exact field/root/scope/operation/default ownership, named special routes, no ineffective integer transforms; representative generated patches | NOT STARTED in this segmented pass |
| **03** | Loot, Stashes and generator safety | Weight is not mislabeled as probability; explicit Chance only where genuinely exposed; ordinary vs authored/quest/unique/DLC boundaries; protected actors untouched; per-domain real-writer fixtures | NOT STARTED |
| **04** | WPF/UI visual and control contracts | renderers vs two locked catalogs, compact knob/grid geometry, Stamina Usage slider positions, typography and modified-number state, responsive sizes, focused help text, keyboard/mouse behavior | NOT STARTED |
| **05** | Vanilla, factory and user presets | difficulty baseline correctness; no accidental changes after load/save/reset; all machine IDs and aggregate bindings survive round-trips; correct UI warning for source-neutral settings | NOT STARTED |
| **06** | Build Mod and portable publish | `publish.sh` produces fresh win-x64 binary with source receipt; user Presets/Output/tools preserved; repak V11 and retoc UE5.5 yield coherent .pak/.utoc/.ucas; errors fail safely | NOT STARTED |
| **07** | Wine + S.T.A.L.K.E.R. 2 runtime | local visual acceptance, game load and fresh/unopened world-actor tests, repeatable gameplay observations and recorded build/game version | NOT STARTED |

**Gate policy:** record **PASS**, **FAIL**, **BLOCKED**, or **NOT RUN** per concrete test, never inferred from code review. Gates 02–06 can accumulate independent evidence while hosted CI is blocked; gate 07 requires the user's running game. Later work must not retroactively describe unexecuted checks as successful.

## Gate 01 — initial evidence and findings

**Inspected main:** `45c3d691d9b5a9e91c013d670502726a165c675b` (message: “RC UI: remove redundant tab summary from parameter help pane”). Repo's default branch is `main` and connector reported push access. The documented `.github/workflows/static-audit.yml` triggers on push to main, PR and workflow dispatch, declares 14 jobs: one static Python audit, one Linux cross-build, and twelve Windows WPF writer-contract jobs.

**Hosted CI blocker:** [workflow run 39316871215](https://github.com/markoca/TrueCustomDifficulty/actions/runs/39316871215), for that exact SHA, reports 14/14 jobs `failure`. The `audit` job (ID `118280511281`) reports an empty `steps` array; its log retrieval returned a 404. The remaining jobs are likewise reported failed with no step records in the job list. Thus *none of these job failures is evidence of a test assertion or C# compile error*. The underlying hosted runner/account/workflow scheduling cause is **not established** by available logs. Do not change production code merely to turn these red badges green.

**Structural checks (inspected, not executed):** `global.json` selects .NET SDK 10; the app targets `net10.0-windows`, with WPF and Windows targeting enabled. `tests/Directory.Build.props` enables Windows targeting for 13 test projects on Linux; `publish.sh` cross-publishes win-x64, checks for an already running app and maintains portable user directories. These checks do not prove actual compilation, Wine behavior, or packaging.

### Remaining Gate 01 acceptance

1. Obtain an Actions execution where jobs run steps and have usable logs (or diagnose and document GitHub-side runner/account restrictions separately). Do not count an empty-step failure as application failure.
2. Independently run `python3 tools/pre_release.py static --output audit/pre-release/rc-static`, plus source/copy/UI audit scripts included in the workflow, recording actual exit codes and output.
3. Run `dotnet build tests/LootSpecialGeneratorsContract/LootSpecialGeneratorsContract.csproj -c Release -r win-x64` on Linux and the real Windows writer contracts through Windows CI or `./tests/run-wine.sh <ContractName>` as appropriate; record the executed scope. A cross-build alone does not pass writer output.
4. Run `./publish.sh` on the current checkout and verify `publish/win-x64/BUILD-INFO.txt` points to the same source commit; do not delete/recreate the portable publish directory.
5. Mark the gate PASS only when the required tests have actually passed; otherwise retain named BLOCKED/NOT RUN conditions.

### Current outcome

**Gate 01 is not complete.** CI is blocked before test execution; no independent local .NET build, Wine launch, generated BPatch fixtures, or in-game behavior was run by this review. No production code change is justified by the CI evidence currently available.

## Handoff and revision discipline

For every gate: source evidence → defects with affected paths/IDs → minimal code+test+documentation change on main → verify pushed SHA → local pull/build/test command block → record test results. Update this document with completed/blocked checks and evidence URLs without changing audit results retrospectively.
