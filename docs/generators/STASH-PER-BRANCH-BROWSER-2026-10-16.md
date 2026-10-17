**STASH CONTENT MIX — GLOBAL ITEM-CATEGORY KNOBS (2026-10-17, supersedes expanded-by-default layout):** Six compact, always-visible global knobs — AMMO, MEDICAL, FOOD, DRINKS, GRENADES, COUPONS — use one shared source-category multiplier across matched Basic/Standard/Rare ItemGenerator and world SmartLoot entries, regardless of generator rank or family. Each knob affects **only its own item category**; it does not set separate values for any rank, difficulty or generator prototype. Medical item-detail factors combine only within Medical. One `StashEffectiveState.ItemBiasId`/`SmartItemBiasId` mapping now covers both producer families, including `AntiRad`/`Antirad`. The read-only, source-faithful 463-branch inspector is **collapsed by default** beneath the knobs: a small `SOURCE DETAILS` disclosure optionally expands the native group/Weight/Chance inspector without creating edit dimensions. Stash Content Mix height = **265 DIP collapsed** / **705 DIP expanded**, with browser starting at y265. Preset IDs, authoring gates, native-source boundaries and real writer numeric semantics are unchanged; do not infer a global cross-generator drop probability. Existing 26 Stashes IDs and catalogs remain 1:1 with the UI. Audits: `python tools/audit_stashes_mixer_ui.py`, `python tools/audit_stash_consumer_graph.py`, `python tools/audit_project_consistency.py`. Real .NET build/Wine/game confirmation remains pending. **This current rule takes precedence over older statements requiring the inspector to be always expanded at y237 / height 675.**

[Consumer graph correction](STASH-CONSUMER-GRAPH-2026-10-16.md): 5,013 physical source edges; four original wrapper redirect rows have multi-actor source rosters, including one protected Rostok wrapper. The browser remains driven by the single top-bar Base Preset difficulty and cannot treat blocked shared wrappers as editable or guaranteed loot.

**CURRENT 2026-10-17 — NATIVE BRANCH BASE PRESET SYNC:** Removed the redundant, independently editable DIFFICULTY stepper. ItemGenerator filters, visible group list and effective numeric bar previews now follow the app-wide Base Preset automatically; the RANK stepper remains centered. SmartLoot keeps only prototype + rank navigation. The only difficulty source is `MainViewModel.SelectedReferenceDifficulty`, not another UI state. This is UI/preview-only and changes no persistent Stashes keys or CFG writer behavior; local Wine visual confirmation is pending.

**PRE-COMPILE STASHES AUDIT (2026-10-16):** Source review of all five phases found and fixed a genuine missing C# method (`StashCategoryIndex` used by both the older stash previews and Phase 4 browser), a stale Phase 4 static assertion (`TuneCount` instead of the Phase 5 `SmartCount`), and a stale Phase 3 project-audit signature (`private` instead of the actual `public MedicalDisabled`). The browser now resolves all 46 distinct authored item SIDs through the two production category mappings, including SmartLoot `Antirad`; the Phase 5 executable harness checks the same coverage. Browser hover no longer forces Arrow over an underlying knob or drops the Hand cursor on repeated motion. Phase 1 inventory (463 branches/1,124 item rows) and Phase 2 source manifests (5,013 edges, 106 protected actors, 112 approved DLC1 actors) were independently cross-counted from the GitHub source and match their documented scopes. **This is source-level remediation only: no successful .NET build, Wine execution or game test is claimed.** Latest GitHub Actions run for `ab725bb` reported failure in all 14 jobs with zero reported steps (also observed in the preceding run); treat CI as an infrastructure/runner-or-account blocker, not as passing or informative executable acceptance. Phase 4 still intentionally lacks per-physical-actor activated/blocked consumer statuses in the browser, so it is a **native source browser**, not a live world-stash consumer inspector. Phase 5 remains **not runtime accepted**.

**CURRENT PHASE 5 2026-10-16:** [Native preview vs emitted CFG parity and runtime acceptance contract](STASH-PHASE-5-CFG-PARITY-AND-ACCEPTANCE-2026-10-16.md) is now implemented with shared SmartLoot count suppression; previously documented 'Phase 5 NOT YET DONE' is historical implementation-phase status. .NET/Wine/game execution is still pending.

# Stashes Phase 4 — native generator branch browser

**Status: source-browser UI implemented on main (2026-10-16); .NET/Wine and gameplay testing still required.**

## User-facing behavior

The existing six color-coded Stash Content Mix knobs remain one unboxed horizontal row, with their original `loot.*` IDs, snap, and changed-digits-only yellow conventions. Their old fixed examples are **replaced**, not stacked underneath, by one compact, read-only **NATIVE BRANCH** browser in the same family. The visible controls do not expand into 463 edit cards.

- The first strip selects **BASIC**, **STANDARD**, **RARE** or **SMARTLOOT**. ItemGenerator has only a centered independent **RANK** (Newbie/Experienced/Veteran/Master) stepper; its difficulty automatically follows the existing top **Base Preset** selector, with no duplicate DIFFICULTY control. SmartLoot retains **PROTOTYPE** (13 original SIDs) and **RANK**, then an exact native **GROUP** stepper. The selected subgroup preserves its full native path. Missing native gates indicate eligibility, never guaranteed physical spawns.
- Each exact group displays its original rows, including duplicate item SIDs, in source order. `Weight` groups show original → effective numeric Weight and only the effective **share of that one group's total Weight**. All-zero total explicitly says **NO WEIGHT TOTAL** / game fallback unverified; do not suggest a 0% physical drop guarantee.
- `Chance` groups show original → effective **per-entry** source Chance. Native chance fields are independent; there is no normalization to a fabricated 100% stash outcome.
- SmartLoot groups additionally show native and effective **MinSpawnChance / MaxSpawnChance / ItemSetCount**, applying the writer's *uniform-factor-only* rule; mixed item biases do not scale group spawn endpoints.
- Source-preview subscriptions include all Stash Rewards ammo/bonus Chance and grenade amount controls, and both Medical MIN/MAX range endpoints as **read-only linked inputs**. These inputs are never separately edited or reset by the Content Mix board. The same TierField method therefore sees live count as well as selection changes.
- The branch display uses the exact **`StashEffectiveState.TierField`** / **`SmartSelection`**, **`UniformSmartFactor`** and **`SmartSpawnEndpoint`** methods already used by the production writer. The shared WPF **`SelectedReferenceDifficulty`** dependency property binds directly to **`MainViewModel.SelectedReferenceDifficulty`**, as does the top bar. The single native mapping is **Novice → Easy, Stalker → Medium, Veteran → Hard, Master → Stalker**. The same mapped native difficulty filters source groups and is passed to Rare Coupons effective-value calculation. Changing the top Base Preset invalidates the preview and resets its currently filtered group index; neither rank nor prototype is reset. Existing Medical × detail and explicit MAX=-1 suppression apply.
- Selector arrows are clickable/hoverable. Keyboard: **Alt+Left/Right** changes branch, **Alt+Up/Down** changes rank, **Ctrl+Left/Right** changes prototype **only in SmartLoot**. There is no item-generator difficulty keyboard/arrow control; use the existing top Base Preset. Navigation does **not** create settings or modify CFG, presets, reset membership, source bindings or the selected source's native snap.

### Exact source authority

`StashBranchPreviewCatalog` builds the tier groups from embedded **`cfg-writer-baseline.json`**, and the SmartLoot groups from the production writer's already embedded **`smartloot-world-source-entries.csv`**. It does **not** read the Phase 1 documentation snapshot as a parallel runtime database. Runtime catalog startup validates **41 tier groups + 422 SmartLoot groups = 463**, and **1,124 item rows** (133 tier + 991 SmartLoot); source-schema drift fails closed. Source grouping is by original prototype, rank, SmartLoot group name and set index.

The browser intentionally does not present source eligibility as an activated actor or physically reachable stash. The Phase 2 guarded consumer graph remains the authority for **conditional/blocked** actor references; only a real generated CFG build can confirm which approved clones and consumer redirects were emitted. A selected source row is not an in-game drop probability. The six separate DLC1 CheapNoFresh entries, direct reward records and the 45 reserved gear entries remain outside this 463-branch scope.

## UI geometry and compatibility

- One maximum **778-DIP** centered board immediately below the original one-row Content Mix knobs; 420-DIP browser starts at y237, ending y657; Content Mix family measures **675 DIP** at both supported widths. No all-window-width controls, 463 cards, duplicate editors, Base/Vanilla emphasis or second source of numeric truth.
- Selector and preview labels use neutral white/muted text; **yellow remains reserved for changed source knob digits**, never read-only percentages/selection labels. Item bars inherit the established six-category source palette, and remain non-editable.
- Existing 26 persisted Stashes IDs (24 visible / 2 backing), presets, five source families, category/medical edits, producer/writer CFG semantics, static guard filters, R-family reset and other tab geometry are unchanged.
- Application and keyboard navigation must be checked under **Wine at wide and narrow widths**. Code inspection or static Python audit is not proof of render quality or a successful .NET compile.

## Verification

Source and UI synchronization:
```bash
python tools/audit_stashes_mixer_ui.py
python tools/audit_stash_generator_branches.py
python tools/audit_stash_consumer_graph.py
python tools/audit_project_consistency.py
```

Actual generated CFG parity (also Phase 3):
```bash
./tests/run-wine.sh LootSpecialGeneratorsContract
```

Manual Phase 4 acceptance: switch all four families and all four native rank labels; switch **all four top Base Preset difficulties** and confirm the ItemGenerator source group count, selected branch and Rare Coupons values update without a second in-board difficulty control (notably Stalker UI → Medium CFG, Master UI → Stalker CFG). Inspect SmartLoot prototype/rank navigation, duplicate-SID entries, Weight/Chance separation, numeric clamping, 0-weight warning and group spawn endpoints. Top difficulty selection retains its existing preset/CFG behavior; browsing itself must not edit difficulty, Stashes settings, presets or emitted CFG. In-game RNG/drop-rate verification belongs to Phase 5.
