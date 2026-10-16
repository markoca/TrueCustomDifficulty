**PRE-COMPILE STASHES AUDIT (2026-10-16):** Source review of all five phases found and fixed a genuine missing C# method (`StashCategoryIndex` used by both the older stash previews and Phase 4 browser), a stale Phase 4 static assertion (`TuneCount` instead of the Phase 5 `SmartCount`), and a stale Phase 3 project-audit signature (`private` instead of the actual `public MedicalDisabled`). The browser now resolves all 46 distinct authored item SIDs through the two production category mappings, including SmartLoot `Antirad`; the Phase 5 executable harness checks the same coverage. Browser hover no longer forces Arrow over an underlying knob or drops the Hand cursor on repeated motion. Phase 1 inventory (463 branches/1,124 item rows) and Phase 2 source manifests (5,007 edges, 106 protected actors, 112 approved DLC1 actors) were independently cross-counted from the GitHub source and match their documented scopes. **This is source-level remediation only: no successful .NET build, Wine execution or game test is claimed.** Latest GitHub Actions run for `ab725bb` reported failure in all 14 jobs with zero reported steps (also observed in the preceding run); treat CI as an infrastructure/runner-or-account blocker, not as passing or informative executable acceptance. Phase 4 still intentionally lacks per-physical-actor activated/blocked consumer statuses in the browser, so it is a **native source browser**, not a live world-stash consumer inspector. Phase 5 remains **not runtime accepted**.

# Stashes Phase 5 — generated CFG parity and runtime acceptance gate

**Status, 2026-10-16:** test code and source/UI parity fixes implemented on GitHub `main`. **The Windows .NET build, tests under Wine, visual acceptance and game runtime have NOT been executed from this GitHub-only environment.** Phase 5 is **test implementation complete; execution/acceptance pending**. Do not claim whole-stash probabilities.

## What was reconciled

The Phase 4 native branch browser shows actual authored source groups, not whole-stash odds. Its original read-only SmartLoot count preview had a concrete writer mismatch: a zero `SmartFactor` disables `MinCount` and `MaxCount` for **every affected SmartLoot SID** (ammo, grenades, food, medical), not just Medkit and ArmyMedkit. The production writer already performed this zero-count suppression. `StashEffectiveState.SmartCount` now provides the **single** count function for the SmartLoot writer and the native browser, handling 0 suppression, medical floors/caps and unchanged non-medical counts. This preserves original emitted CFG semantics, not a new gameplay adjustment. Count readouts are on a separate compact line without duplicate old→new numbers when unchanged.

All 26 persistent Stashes `loot.*` IDs and existing presets, the six color-keyed content knobs, source-native Stamina Usage numeric conventions, normal editor/reset ownership, writer safety guards, four other Stashes visual families and 45 reserved gear entries remain intact. Native browser selection does not write user settings or mutate the game.

## Executable evidence

The integrated `tests/LootSpecialGeneratorsContract` runner now calls `VerifyStashBranchBrowserParity` in `StashBranchParity.cs`. It fails if the catalog loses any of **463 native groups, 1,124 original item rows, 19+410 Weight and 22+12 Chance groups**, duplicates item SID entries, or collapses separate native gate paths.

Pure shared-engine assertions prove that representative source Weight totals are **17.77** (native Basic / Easy-Medium / Experienced) and **43.77** after 3× Food, with 0 for an all-disabled same-group pool; the engine **fallback remains unknown**. It checks Easy/Medium/Hard/Stalker and rank gating, hard/native difficulty-scoped Rare Coupons bonus, RGD-5 Chance bonus multiplied before category bias and clamped at 1, SmartLoot mixed medical factors leaving native spawn endpoints unchanged, and uniform zero factors disabling counts/sets.

**Generated CFG scenarios** then use actual `MainViewModel`, the embedded baseline and `CfgPatchWriterService.Write` and parse the *real emitted CFG*:

| Case | Native facets | Additional scope |
|---|---|---|
| `food-3x` (Novice, no DLC) | 3× Food selection, original rank+gate boundaries, representative Weight | No DLC actor files |
| `all-category-zero` (Master, no DLC) | all six category biases to 0, per-item counts and zero SmartLoot spawn/sets where authored | No DLC actor files; whole-stash RNG **not** inferred |
| `hard-bonus-medical-dlc` (Veteran, DLC1 present) | bonus-before-category Chance, Rare difficulty, mixed Medkit/Army bias, Medkit MAX=-1, uniform/heterogeneous SmartLoot behavior | DLC1 positive allowlist; base protected actors |

For each case, test code compares every **emitted native ItemGenerator item** `Chance/Weight` + `MinCount/MaxCount` field in all three tier clones with the **same** `StashBranchPreview.EffectiveSelection/EffectiveCount` used by the UI. For **actually affected** SmartLoot groups it compares emitted item `Weight/Chance`, count endpoints, and emitted group `MinSpawnChance`, `MaxSpawnChance`, `ItemSetCount` against the shared effective engine; it does not demand nonexistent patches for untouched inherited fields. The test rejects protected base actor writes and DLC1 paths outside the 112-ID allowlist. Existing Phase 3 two-scenario parity, medical zero, six DLC CheapNoFresh and 803 base actor contracts remain active.

This checks **numeric source fields in generated CFG**, *not* whole-stash rates, loader acceptance, mod precedence, initial WorldMap respawn state, or the in-game RNG.

## Local commands, execution and acceptance

From repository root after pulling `main`:

```bash
python3 tools/audit_stash_generator_branches.py
python3 tools/audit_stash_consumer_graph.py
python3 tools/audit_stashes_mixer_ui.py
python3 tools/audit_project_consistency.py
./tests/run-wine.sh LootSpecialGeneratorsContract
./publish.sh
WINEPREFIX="$HOME/.wine-s2" WINEDEBUG=-all wine "./publish/win-x64/TrueCustomDifficulty.exe"
```

**Manual Wine/UI gate:** inspect Stashes Content Mix at wide and narrow panel widths. Switch Basic/Standard/Rare/SmartLoot and at least two prototypes; cycle four ranks and four difficulties; use clickable arrows and Alt/Ctrl navigation; verify selected native group path, Chance vs group-local Weight, source quantity suppression, no overflowing/clipped rows, source-knob changed yellow digits only, and no extra or stretched edit controls. Test R group reset and source knob right-click reset, saving/loading an existing preset, and that browser navigation alone does not change a single persisted setting or produce a CFG change.

**Generated CFG gate:** inspect the real test output and patch paths with and without DLC1; ensure no protected named-clue/unique/quest actors are redirected. **Game gate:** test newly generated ordinary eligible containers in STALKER 2 v2.0.5, log actual gameplay outcomes separately from native field comparisons; compare with DLC1 present/absent, avoid already-populated stashes, and independently verify Unreal source loader + spawn behavior. No assertion of empirically measured per-stash probabilities until proper in-game sampling.

If any compilation, static audit, emitted CFG, UI navigation, Wine or in-game validation fails, Phase 5 is **not accepted**; fix code and linked documentation directly on `main` with a new verified push. No assertion, expectation or protection threshold should be weakened to conceal a regression.
