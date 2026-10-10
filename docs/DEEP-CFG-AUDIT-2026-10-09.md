# Deep CFG audit — Phase 1: catalog/writer and Mutant Health source provenance

> **Important Phase 2 correction:** This Phase 1 report verified the numeric values for all sixteen Mutant Health prototypes but had **not** verified that the writer emitted them at the correct nested CFG location. Phase 2 found all 16 writer `nodePath` entries were empty despite the actual key being `VitalParams.MaxHP`. All 16 writer paths and 14 catalog target paths have since been corrected and source-verified. See [Phase 2: structural source paths](DEEP-CFG-AUDIT-PHASE2-2026-10-09.md). Do not interpret Phase 1 value parity as successful BPatch location validation.



**Date:** 2026-10-09
**Branch:** main
**Evidence:** original user-supplied current vanilla GameData and DLC1 decoded CFG archives, production parameter catalog and production writer baseline.

## Scope and evidence levels

This is **phase 1**, not a claim of final gameplay correctness.

1. **Catalog/writer route coverage**: every parameter ID must have a declared direct or explicit special writer route. Direct target ownership must be non-overlapping.
2. **Decoded CFG file existence**: every direct writer's referenced file must exist in the correct base/DLC1 input archive. File existence is not proof that a specific prototype and nested field match.
3. **Exact source value verification**: 14 Mutant Health parameters and all 16 concrete `VitalParams.MaxHP` targets were checked against the actual decoded vanilla `GameData/ObjPrototypes.cfg`. This includes the six rows missing from the previous writer baseline.
4. **Remaining work**: all other individual sourceRaw values and nested paths, inherited prototype semantics, special generator production manifests, DLC1 source overrides, difficulty-composition behavior, and game runtime effects still need their own evidence and tests.

### Source ZIP identity

| Archive | SHA-256 | CFG entries |
|---|---|---:|
| `STALKER2-GameData-CFG(4).zip` | `937b3fb4c74d1311b6e052c010aa0795073ccdc60d2ca6d8b5228ee3ce0b6083` | 485 |
| `STALKER2-DLC1-CFG(4).zip` | `93ccb9bcc9043f7c171d439b2c25c38f61311506bc33c0b926a16f526ccd6114` | 118 |

The hashes refer only to the supplied dumps, **not** every possible future game update. Regenerate and re-run when the source build changes.

## Confirmed catalog/writer defect — FIXED

The live `parameters.json` already contained six valid hidden, player-editable **Mutant Health** backings, but `cfg-writer-baseline.json` still contained only the original eight mutant families (10 concrete `MaxHP` points). This caused the writer's route graph to omit six current UI species. They were therefore not safely writable by the established direct CFG route despite their existing UI cards and confirmed catalog status.

| Parameter suffix | Actual vanilla prototype SID | `VitalParams.MaxHP` |
|---|---|---:|
| `blinddog` | `Blinddog` | 63 |
| `boar` | `Boar` | 300 |
| `flesh` | `Flesh` | 280 |
| `poltergeist` | `Poltergeist` | 400 |
| `snork` | `Snork` | 150 |
| `tushkan` | `Tushkan` | 19 |


The result is **14 mutant HP settings, 16 actual source `MaxHP` edit points**, compared with the former eight settings and ten edit points. All 16 were individually checked in the supplied base CFG (including Bloodsucker/MistBloodsucker and both Burer variants). `tools/audit_cfg_dump_provenance.py` reproduces this verification.

## Live catalogue and writer metrics (not the historical v0.9.26.51 snapshot)

| Tab | Total catalog | Editable dimensions | Visible anchors | Hidden active backings | Confirmed* | Runtime test required* | Direct CFG edit points |
|---|---:|---:|---:|---:|---:|---:|---:|
| Combat | 39 | 39 | 19 | 20 | 27 | 12 | 68 |
| Survival | 43 | 42 | 32 | 10 | 40 | 2 | 109 |
| Health | 16 | 16 | 13 | 3 | 13 | 3 | 38 |
| Stamina | 23 | 13 | 7 | 6 | 12 | 1 | 26 |
| Loot | 40 | 40 | 31 | 9 | 16 | 24 | 320 |
| Economy | 23 | 22 | 17 | 5 | 17 | 5 | 409 |
| Equipment | 4 | 4 | 4 | 0 | 3 | 1 | 113 |
| Anomalies (environment) | 17 | 17 | 7 | 10 | 17 | 0 | 42 |
| A-Life | 19 | 18 | 2 | 16 | 18 | 0 | 64 |
| Master | 20 | 20 | 20 | 0 | 20 | 0 | 35 |
| **Total** | **244** | **231** | **152** | **79** | **183** | **48** | **1,224** |

\* Catalog-declared support statuses on *active dimensions*, **not claims of in-game runtime testing**. Some confirmed source routes still need independent runtime characterization, and the `runtime_test_required` set must not be silently promoted to confirmed.

The remaining **13 intentionally non-UI** IDs are ten internal Stamina helper IDs, `economy.repairDifficultyCost`, `alife.mutantSpawn.generic`, and `survival.drunknessThresholdScale`. The last is explicitly documented by its own catalog entry as an **internal alcohol-stage threshold mapping**, withheld until the separate OverDrunkness chain is modeled; it is not currently referenced by a playable control. The 79 hidden active backings are edited through composites, not generic cards. Counts of *visible anchors* are catalog `exposeInUi` counts, not literal WPF-generated card count.

Writer totals following the correction:
- **244** parameter IDs in catalog and baseline `supportedParameterCount`.
- **1,224** direct edit points representing **202** distinct parameter IDs, with **zero direct concrete-target ownership collisions** by `(scope,file,prototype,nodePath,key)`.
- **13** distinct direct CFG input files, all present in the supplied archives under their declared base/DLC1 scope.
- Remaining parameters use named **special writers**, not direct editPoints; their own manifests and production semantics require subsequent separate checks.
- All 16 direct mutant `MaxHP` entries were source-verified as exact, not just checked by filename.

## Repaired test gates

- `tools/audit_production_integration.py` now locks the actual **244/152/92** catalog/visible/hidden split, real control-type counts, 10 tabs and 16 toggle dependencies.
- `tools/audit_ui_cfg_contract.py` now audits **231** editable dimensions and all **79** hidden composite-backed live references, replacing outdated **199/188** fixed assertions; it distinguishes 12 truly audit-only IDs from hidden-but-editable controls. Legacy cohort lists are not falsely presented as the complete active editor list.
- `tools/audit_project_consistency.py` replaces its old eight-mutant polar-radar assumptions with source-accurate current **14-species/16-MaxHP** tests, while retaining the eight independent cooldown-family expectation.
- `tools/audit_cfg_dump_provenance.py` is a reusable independent decoded-ZIP checker. It checks 13 direct input filenames and the exact 16 baseline/catalog/source Mutant HP triples. Other source fields are intentionally *not* marked validated by this script.

### Reproduce the source check

From the repository root (assuming the user's CFG output ZIPs):

```bash
python tools/audit_cfg_dump_provenance.py \
  --base-zip "$HOME/s2tools/cfg_dump/STALKER2-GameData-CFG.zip" \
  --dlc1-zip "$HOME/s2tools/cfg_dump/STALKER2-DLC1-CFG.zip"

python tools/audit_ui_cfg_contract.py
python tools/audit_production_integration.py
python tools/audit_project_consistency.py
```

The ZIP check was independently applied to the materialized user source archives on 2026-10-09. Full production Python suite and `.NET 10`/Wine builds were **not** executed in the GitHub connector environment. The commands above are acceptance checks, not already-claimed local passes. The archived filenames `(4)` used for this pass and the local extraction script's default ZIP names contain the same logical base/DLC1 scope.

## Remaining deep CFG audit work — prioritized

1. **SourceRaw/field-path audit (1,208 other direct edit points):** validate the exact prototype `SID`, nested node path and raw source value against both ZIPs. Split real mismatches from intentional inherited values, optional DLC1 overrides and version-specific differences.
2. **Special writer source manifests, especially Loot (24 runtime-test-required dimensions):** verify generator cloning, redirects, context eligibility, quest/story exclusion, independence of amount vs chance, and actual consumption paths. Do not mark file-existence as runtime-safe.
4. **Economy (5) / Health (3) / Survival (2) / Equipment (1) / Stamina (1):** audit exact scalar meaning, activation thresholds, stacked effects and cross-system interactions. In particular **Armor Durability** and **Equipment Bonus Caps** remain unresolved; bonus caps stay audit-only, not player-facing.
5. **Master and A-Life:** prove effective trigger/composition behavior, non-Master bundle insertion, and A-Life Director encounter caps versus lair population. Do not infer runtime spawn frequency from `MaxCount` fields.
6. **DLC1 and vanilla coverage:** confirm every targeted authored prototype for both scopes and negative-space safety (untouched quest/inherited/other DLC-only records), then execute UI + patch roundtrip + actual gameplay scenarios.

Do not bump to v1.0 on the strength of this partial audit. Keep source-provenance and runtime behavior separate in every follow-up report.
