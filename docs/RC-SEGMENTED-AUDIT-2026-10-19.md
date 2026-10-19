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


## Gate 01 — execution attempt 2 and environment limitations (2026-10-19)

- **GitHub rerun requested and accepted:** [latest main workflow run 39320831462](https://github.com/markoca/TrueCustomDifficulty/actions/runs/39320831462), SHA `e622a2923d485488d63fdcd6ccdcd424e103f8b2`, rerun attempt **2**. At the immediate status check, several jobs again reported `failure` while remaining jobs were queued; there was no test-step or compiler-output evidence to attribute those failures to source defects. **The rerun's final outcome must be checked again**.
- The predecessor run `39316871215` has **0 ms billable job runtime for all fourteen jobs** (12 Windows, 2 Ubuntu). Its `audit` job returned no recorded steps and its job-log endpoint returned 404. This is stronger evidence that hosted jobs never started, **but does not identify the exact account/runner/billing/policy cause**.
- **Local execution environment cannot verify Gate 01 here:** the available execution container has `git` but **no `dotnet` or `wine`**, no mounted repository checkout, and external GitHub DNS/clone is unavailable. Therefore `python tools/pre_release.py static`, Linux WPF cross-build, Windows/Wine writer contracts and `publish.sh` have **NOT RUN** here. GitHub connector source inspection is not a substitute for executing these checks.

### Isolation commands for the maintainer workstation

From the *current* main-branch checkout (with .NET 10/Wine present), run these independently, preserving the first failing output. Do not change original CFG or generated files on the basis of CI's zero-runtime badges:

```bash
cd /home/marko/s2tools/TrueCustomDifficulty/
git pull
git rev-parse HEAD
python3 tools/pre_release.py static --output audit/pre-release/rc-static
python3 tools/audit_final_cfg_mapping.py --out audit/final-cfg-mapping
python3 tools/audit_rc_final_copy.py
dotnet build tests/LootSpecialGeneratorsContract/LootSpecialGeneratorsContract.csproj -c Release -r win-x64
WINEPREFIX="$HOME/.wine-s2" WINEDEBUG=-all ./tests/run-wine.sh LootSpecialGeneratorsContract
```

**Disposition:** Gate 01 remains **BLOCKED / NOT RUN** for CI and real-binary contracts. No application-source correction or PASS is claimed. Next: inspect the final rerun jobs (step arrays/logs); use the first actual failing test if execution begins, or diagnose GitHub Actions administrative status outside code if zero-duration failure repeats.


## Gate 01 — independent source checks executed (2026-10-19)

This subsection records **checks actually executed against the connected GitHub main-branch file contents**, using a self-contained JavaScript comparison in the connector execution environment. It does **not** claim that any Python audit script, .NET build, WPF renderer, Windows production writer, Wine executable or game binary was run. These checks read source files directly through GitHub, parse embedded JSON/CSV and compare the catalog to the writer baseline and reviewed wording ledger.

**Actual executed scope:**

| Independent source-level assertion | Result |
| --- | --- |
| Parameter ID uniqueness/exposure: 287 unique, 182 visible, 105 non-UI | **PASS** |
| RC review CSV header, unique IDs, all 182 visible reviewed | **PASS** |
| All 182 frozen label/rationale/logic/interaction fields identical to current catalog | **PASS** |
| Both authoritative UI documentation files retain the 182-control freeze marker | **PASS** |
| CFG writer baseline version 2.0.5, 287 expected IDs, no deferred IDs | **PASS** |
| 1,238 direct CFG leaves across 217 source IDs | **PASS** |
| No orphan or multiply owned concrete CFG leaf; direct scope/file/operation matches parameter targets | **PASS** |
| Exactly 70 special IDs, disjoint from 217 direct IDs, total writer coverage 287/287 | **PASS** |
| All 21 named special production writer methods have method references in inspected main/partial service sources | **PASS** |
| All 12 embedded factory preset JSONs each contain exactly the 287 catalog IDs | **PASS** |
| Independently mirrored direct source-transform arithmetic from `tools/audit_final_cfg_mapping.py`: no modified-but-ineffective direct CFG values across all twelve factories | **PASS**, **0 inert** |
| The 11 allowed direct transform operation names are all recognized | **PASS** |
| All 20 special-writer embedded data resources present in the main tree | **PASS** |
| All 20 matching resources included by the WPF project file | **PASS** |
| Direct `Write` path, transform, assignment, output emission and missing-output guard are present in production source | **PASS** |

Factory modified-source evaluation used the exact operation semantics documented by the Python audit (including half-up integer rounding, zero preservation, difficulty SID filtering and parent-toggle suppression). Direct changed-source counts across the twelve factories: Immersive 164; Tactical 175; Field Medic 173; Mutant Hunter 184; Anomaly Runner 181; Wanderer 179; Mercenary 173; Smuggler 177; Scavenger 189; Outcast 192; Hardcore 197; Hardcore+ 205. These numbers are **static mapped source calculations**, not a count of CFGs produced by an executed C# writer. No preset in this test had an ineffective modified direct source.

**Actual hosted GitHub Actions attempt:** [run 39320831462](https://github.com/markoca/TrueCustomDifficulty/actions/runs/39320831462), `e622a2923d485488d63fdcd6ccdcd424e103f8b2`, failed on rerun attempt 2: all 14 jobs failed, none executed any steps, and GitHub reported 0 ms runtime on both platforms for all 28 job attempts (24 Windows, 4 Ubuntu). The underlying GitHub-side failure reason cannot be determined from these data. Treat **CI as BLOCKED, not tests FAIL**.

**Actual execution blockers:** tool container has no `dotnet`, no `wine`, does not mount the maintainer's `/home/marko/s2tools/TrueCustomDifficulty/` checkout, and cannot resolve GitHub DNS to clone. Hence the original Python audit suite, Linux cross-build, actual C# writer contracts and user-machine Wine/game test remain **NOT RUN** here. The independent source checks above provide bounded evidence but must not be substituted for those native executables.

**Gate 01 disposition:** **PARTIAL SOURCE PASS / BUILD & RUNTIME BLOCKED**. No production-code defect has been substantiated from the CI failures. When independent Windows/.NET execution is possible, record the concrete return codes/fixtures before changing this gate to PASS.

## Gate 02 — CFG writer semantic finding and correction (2026-10-19)

Source review confirms the 287/287 direct/special routing inventory, 217 direct IDs and 1,238 exact concrete edit points, 70 special IDs, disjoint target ownership and 20 embedded special resources. Production writer sources were inspected, but original source archives, .NET execution and STALKER 2 runtime have **not** been executed in this review.

**Confirmed source-level defect:** when Extra Stash Gear is enabled but Coverage rounds to 0%, non-default Bonus Mix weights or Bonus Chance cannot affect any private bonus generator. The writer previously unconditionally acknowledged changed gear controls as written. **Fix:** reject changed bonus tunings at 0% before recreating staging, list the controls to reset and keep the intentional master-only 13 authored reward removals unchanged. Positive coverage and explicit empty-mix mode remain untouched.

**Regression:** `tests/LootSpecialGeneratorsContract/StashGearCoverage.cs` now checks the 0%-case error, preservation of a pre-existing sentinel, and successful private generator creation after switching to Coverage 1%. `tools/audit_stash_gear_coverage.py` enforces the guard's presence. Runtime test execution still required; never present static inspection as .NET PASS.

**Gate 02:** partial source-level findings and fix committed; real C# writer/game confirmation **NOT RUN**.

## Gate 02 — inert Bonus Chance with all-zero Extra Gear mix (2026-10-19)

**Source-level finding:** a positive Coverage with **all four Extra Stash Gear class Weights set to zero** selects physical actors, but `ChooseStashGearClasses` returns no categories. `WriteStashGearCoverage` therefore emits no private item candidates, while the parent opt-in still emits 13 approved authored weapon reward removals. A separately modified Bonus Chance had no actual bonus item to influence, but `Write()` previously acknowledged it unconditionally.

**Correction on main:** reject **only changed Bonus Chance** in this exact zero-category case, before touching staging. Preserve all-zero Weight as a valid remove-only configuration if Bonus Chance is neutral; preserve nonzero class weights, normal Chance/Weight behavior, actor allowlists and game-native Chance. A new production writer regression checks pre-stage failure and remove-only output; static gate and domain documentation updated in the same commit.

**Verification:** code and catalog semantics independently inspected; .NET/Wine/game execution **NOT RUN**. Gate 02 remains **PARTIAL (source correction applied, executable acceptance pending)**.

## Gate 02 — NPC corpse bonus ammo: saved setting versus emitted source (2026-10-19)

**Confirmed accounting defect:** `loot.npcWeaponAmmoSupply` can retain a non-vanilla multiplier while `loot.npcCorpseBonus` selects Water or None. In this state the production `WriteNpcCorpseAmmoProduction` deliberately emits **no** `NPC_Ammo_Smart` clone or redirect, but previously recorded the dormant control as an applied writer parameter. This inflated `CfgWriteResult.ParameterCount` without any CFG work.

**Correction:** retain the editor and preset value and preserve successful Build Mod with Water/None; do not add the dormant ammo ID to `writtenParameters`, and narrowly exclude it from the missing-CFG-output error **only while the current corpse bonus choice lacks `NPC_Ammo_Smart`**. Medicine + Ammo still emits the original 43-row source clone and selected difficulty redirect; no loot, chances, UI controls or vanilla baselines changed.

**Regression:** the existing 63-scenario `LootSpecialGeneratorsContract` counts actually emitted parameters (including inactive Water/None case), and `GenericLootPreGame` asserts dormant Stalker/Water ammo contributes zero applied parameters while keeping its edited VM value. `tools/audit_final_cfg_mapping.py` adds a source structural guard. All files are updated in the same main commit. This is **source-level verified**, not executed .NET/Wine or gameplay proof.

**Scope note:** GitHub Actions execution is not a prerequisite for this segment by user decision. Native writer execution and in-game acceptance are separate verification items, not secretly treated as passed.
