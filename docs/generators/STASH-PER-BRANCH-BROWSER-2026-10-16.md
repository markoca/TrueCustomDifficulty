# Stashes Phase 4 — native generator branch browser

**Status: source-browser UI implemented on main (2026-10-16); .NET/Wine and gameplay testing still required.**

## User-facing behavior

The existing six color-coded Stash Content Mix knobs remain one unboxed horizontal row, with their original `loot.*` IDs, snap, and changed-digits-only yellow conventions. Their old fixed examples are **replaced**, not stacked underneath, by one compact, read-only **NATIVE BRANCH** browser in the same family. The visible controls do not expand into 463 edit cards.

- The first strip selects **BASIC**, **STANDARD**, **RARE** or **SMARTLOOT**. ItemGenerator has native **DIFFICULTY** (Easy/Medium/Hard/Stalker) and **RANK** (Newbie/Experienced/Veteran/Master); SmartLoot has **PROTOTYPE** (13 original SIDs) and **RANK**, then an exact native **GROUP** stepper. The selected subgroup always preserves its full native path. A missing native rank/difficulty gate means eligible for any selected value; it does not mean a guaranteed spawn.
- Each exact group displays its original rows, including duplicate item SIDs, in source order. `Weight` groups show original → effective numeric Weight and only the effective **share of that one group's total Weight**. All-zero total explicitly says **NO WEIGHT TOTAL** / game fallback unverified; do not suggest a 0% physical drop guarantee.
- `Chance` groups show original → effective **per-entry** source Chance. Native chance fields are independent; there is no normalization to a fabricated 100% stash outcome.
- SmartLoot groups additionally show native and effective **MinSpawnChance / MaxSpawnChance / ItemSetCount**, applying the writer's *uniform-factor-only* rule; mixed item biases do not scale group spawn endpoints.
- The branch display uses the exact **`StashEffectiveState.TierField`** / **`SmartSelection`**, **`UniformSmartFactor`** and **`SmartSpawnEndpoint`** methods already used by the production writer; the correct selected native difficulty is passed for Rare Coupons overrides. Existing Medical × detail and explicit MAX=-1 suppression apply.
- Selector arrows are clickable/hoverable. Keyboard: **Alt+Left/Right** changes branch, **Alt+Up/Down** changes rank, **Ctrl+Left/Right** changes difficulty (ItemGenerator) or prototype (SmartLoot). Navigation does **not** create settings or modify CFG, presets, reset membership, source bindings or the selected source's native snap.

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

Manual Phase 4 acceptance: switch all four families, all four native rank labels and all difficulty values, browse several SmartLoot prototypes and duplicate-SID group entries, verify Weight and Chance separation, test global Medical+Bandage/RGD-5 bonus+Grenades/Rare Coupons+Coupons with EASY vs HARD, verify numeric clamping and 0-weight warning, inspect group spawn endpoints, and check no Stashes settings/presets/CFG change merely by navigating. In-game RNG/drop-rate verification belongs to Phase 5.
