# P0 CI and stale-audit reconciliation — 2026-10-18 (main)

## Why the GitHub Actions run was red

Inspected Actions run [39157521121](https://github.com/markoca/TrueCustomDifficulty/actions/runs/39157521121) on commit `337aae53816fcb807b256c4dffb627169e380f82`, before this P0 commit. GitHub reports all **14 jobs failed** in approximately 13 seconds. The billing/timing endpoint reports **0 ms runner execution** for every Windows and Ubuntu job; job-steps API returns an empty array for sampled jobs, and log retrieval returns Azure `BlobNotFound` (HTTP 404). Consequently **no test execution is evidenced**, and it is not defensible to attribute the red workflow to C# or the stash algorithms. This may be a GitHub account/runner/actions-availability or provisioning failure, but the precise cause is **not established** from the accessible API. This repository change cannot repair an external runner-provisioning failure.

## Corrected source-level audit drift

The current catalog contains **281 distinct IDs: 179 visible and 102 hidden**, with **268 player-editable dimensions** (13 audit-only hidden). **39 Stashes IDs** have four UI families: Content 17, Medical 12, Extra Gear 7, Ammo Rewards 3; **35 Stashes entries are visible**. The 12 committed factory profiles each contain 281 values. Native direct writer remains 213 IDs / 1,230 concrete edit points; the **68 special-route IDs** include all 21 stash content/extra-gear IDs and all 12 medical controls.

- `tools/audit_final_cfg_mapping.py`: 281/268/68 cardinalities, exact special-stash and medical ID lists, production `WriteStashGearCoverage` and `WriteStashAuthoredWeaponRemovals` verification in their actual C# partials, factory preset membership. Historical 248-ID inventory is still checked *as an explicit 2026-10-15 snapshot*, not rewritten into a current count.
- `tools/audit_project_consistency.py`: 281 total, 179 visible/102 hidden, support 117 confirmed/62 runtime-test-required, slider/toggle 164/13, Stashes 39 total/35 visible, six Extra Gear checkbox dependencies, current medical quantity dual-rail and WEIGHT/CHANCE mode-selector contracts, current zero-items rendering. The 248 historical mapping ledger now excludes precisely the eight subsequently added tier Chance source IDs instead of losing historical coverage.
- `tools/audit_stash_content_gear.py`: all **seven** live Extra Gear controls, per-source Weight 0–3× and two 0–100% percentage sliders, current medical 2-way quantity rails and renderer method names, 281-source count and unchanged native parsing/actor safety checks.
- Workflow names updated to 281 / 268 (rather than superseded 268 / 255). Workflow now explicitly runs the current stash coverage and safe authored-weapon removal static checks as separate fail-closed CI gates, in addition to existing `audit_stashes_current.py`.

These are **audit/test maintenance changes**; gameplay code, WPF layout, saved preset values, protected stash rules and the 13-item approved weapon-removal allowlist are unchanged.

## Verification limits and recovery

The static checks require an actual Python execution and .NET compilation before being labelled **passing**. GitHub Actions cannot be declared green merely because audited code was pushed. If subsequent workflow runs still have 0-ms jobs, inspect GitHub repository **Actions** availability/usage/billing/runner state and contact GitHub support as needed; retry the exact workflow once external runner scheduling is restored. Do not lower assertions or disable failing tests to force a green status.

Run locally from repository root:

```bash
python3 tools/audit_ui_cfg_contract.py
python3 tools/audit_final_cfg_mapping.py --out audit/final-cfg-mapping
python3 tools/audit_stashes_mixer_ui.py
python3 tools/audit_stashes_current.py
python3 tools/audit_stash_content_gear.py
python3 tools/audit_stash_gear_coverage.py
python3 tools/audit_extra_stash_gear.py
python3 tools/audit_project_consistency.py
dotnet build tests/LootSpecialGeneratorsContract/LootSpecialGeneratorsContract.csproj -c Release -r win-x64
```

A static catalog assertion cannot establish in-game loot probability or a passing Windows/Wine runtime test.

---

# Stashes tab audit — 2026-10-18 (main)

## Baseline inspected

GitHub `main` HEAD at inspection: `5493e337c09ba58a9c63915c2a9805e1284e2651`, after adding fourth rank-only Attachments knob. Review covers 39 Stashes catalog parameters and their live WPF aggregate families, `MainViewModel`, parameter-family map, CFG writer routing, original stash safety inventories, 12 factory presets, docs UI locks and CI audit availability. No .NET/Wine/game execution was available during this source review.

## Summary of findings

| Severity | Finding | Disposition |
|---|---|---|
| High | `GetFamilyIds("StashGear")` omitted `loot.stashGearAttachmentWeight` although the renderer, MainViewModel and parameter-family catalog contained it. The fourth knob had no live WPF parameter binding/hit target. | Fixed: all seven ids listed in family binding; new static contract enforces exact match. |
| High | The 4→2 knob visual switches when **paint width <540 DIP**, but measured height switched at **host width <540 DIP**. Paint width is host minus 30 DIP, so host widths **540–569 DIP** drew two rows with 444 DIP, clipping lower Bonus Chance content. | Fixed: host `StashGearHeight(width)` breakpoint **570 DIP**; 612 DIP narrow, 444 DIP wide. |
| Medium | Baseline `stashContentAndGearProduction.parameterIds` listed Enabled + Coverage but not Weapons, Armor, Artifacts, Attachments and Bonus Chance. Dedicated C# writer still wrote these IDs and the generic missing-edit guard whitelisted them, hiding a central route-inventory drift. | Fixed: add five existing IDs to the existing special route group; audit requires 281/281 registered. |
| Medium | All 12 factory presets held only **271/281** current source parameters. Four medical Chance-scale controls and six bonus-gear controls were absent; behavior relied on missing-value fallback rather than complete shipped profiles. | Fixed: add missing ten untouched native values to each preset; leave every existing preset choice unchanged; audit enforces exact set and type. |
| Medium | Existing audit code still contains several obsolete snapshots: e.g. `tools/audit_project_consistency.py` expects 276 total, 29 Stashes, `tools/audit_final_cfg_mapping.py` expects 276, and `tools/audit_stash_content_gear.py` refers to removed `DrawStashQuantityDots` and old UI. These pre-existing checks must not be presented as authoritative current pass/fail evidence. | New independent `tools/audit_stashes_current.py` is the canonical Stashes-specific contract; update the wider historical audits separately. CI invokes the new audit. |
| CI/QA | GitHub Actions at inspected HEAD was marked failed; all 14 jobs returned failures within ~3 sec and logs/steps were not retrievable. | **Not verified**: no conclusion about source test result, build or actor loot RNG. User must run local build and game tests. |

## Stashes source census

| Visual group | Catalog IDs | Notes |
|---|---:|---|
| Stash Content Mix | 17 | 6 weight scalar IDs (Ammo/Grenades two hidden), native grenade Chance/Amount, Standard/Rare Chance fields and Rare coupon source |
| Medical Composition + Quantity | 12 | 4 Weight + 4 Chance-scale dials, 4 MIN/MAX source integer controls |
| Extra Stash Gear | 7 | Master checkbox, 0–100% build-time physical actor coverage, four independent 0–3× relative mixer knobs, one 0–100% shared native outer Bonus Chance |
| Stash Rewards | 3 | One visible three-tier ammo amount board, two aggregate backings |
| **Total** | **39** | **35 visible**, **4 hidden** |

The 4-knob UI is a single row at effective width ≥540 DIP, otherwise 2×2. Max row width 630 DIP. Only modified numeric values receive accent emphasis; no new full-width elements added.

## Game/CFG semantics verified from source, not gameplay

- **Actor coverage** selects an exact deterministic fraction from 800 approved regular base actors and 108 eligible DLC1 actors. Not a loot-roll percentage.
- Native actor branches receive one actor-private possible item from *one* selected mixer class per rank, not a guaranteed 1–2 extra inventory reward.
- Pool: 34 ordinary weapons, 39 suits, 69 standard non-Weird artifacts, plus 19 independently audited loose attachment generators by rank (T2=8/T3=7/T4=4). Attachment may have a second internal roll inside its original generator.
- The shared Bonus Chance writes the outer `PossibleItems.Chance` 0..1, not the physical stash spawn percentage.
- The master checkbox separately suppresses only 13 explicitly audited regular original weapon nodes from 39 cataloged weapon entries. Quest, unique, clue, protected, and eight unapproved DLC cases remain outside this operation. Opening pre-generated stash inventory in an old save cannot establish CFG behavior.
- Existing global Stash Content Weight and Medical native Chance are source-field controls, not stash-wide drop odds; the medical MIN/MAX coupling preserves explicit zero-items semantics.

## Verification steps

Current source-level contract: `python3 tools/audit_stashes_current.py`.
Legacy mixer check: `python3 tools/audit_stashes_mixer_ui.py` (some other old audits remain stale).
For native generation: build `tests/LootSpecialGeneratorsContract` and inspect base/DLC output for supported actors, then use Wine to review compact 4-/2-column layout (especially host widths ~550 and ~570 DIP) and test unopened eligible/protected in-game stash actors.

The source audit and GitHub push do not demonstrate a passing .NET build or gameplay acceptance.
