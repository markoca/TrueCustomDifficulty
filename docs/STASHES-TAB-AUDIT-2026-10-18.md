## 2026-10-19 — CURRENT RC player-facing labels (41 approved)

Authoritative labels for this release candidate supersede older terminology in historical sections below. 36 player-facing catalog labels, four WPF headings (STASH LOOT MIX, LOOT POOLS, BASIC · LOOT PREVIEW, HUMAN ENEMY COMBAT), and GEAR WEAR were approved, with catalog groups Item Values, Gear Wear, Damage by Anomaly and Human Squad Size aligned. All 283 parameter identities, CFG routes, values, preset schemas, and slider geometry are unchanged.

- `combat.npcHealth` → **Human Enemy HP**
- `combat.npcAttackCooldown` → **Enemy Attack Rate**
- `combat.playerWeaponDamage` → **Weapon Damage**
- `combat.playerHeadshotMultiplier` → **Headshot Damage**
- `combat.weaponJamming` → **Weapon Jam Rate**
- `combat.psySpawnCycleDelay` → **Phantom Spawn Interval**
- `survival.hungerRate` → **Hunger Rate**
- `survival.sleepinessRate` → **Fatigue Rate**
- `survival.sleepEligibilityThreshold` → **Sleep Threshold**
- `health.maxHP` → **Max Health**
- `health.baseRegen` → **Passive HP Regen**
- `health.regenDelay` → **Regen Delay**
- `stamina.maxSP` → **Max Stamina**
- `stamina.vaultMaxHeight` → **Vault Height**
- `stamina.regeneration` → **Stamina Regen**
- `loot.npcCorpseBonus` → **Extra Corpse Loot**
- `loot.npcWeaponAmmoSupply` → **Bonus Ammo Drops**
- `loot.destructibleAmmoQuantity` → **Ammo per Box**
- `loot.destructibleGrenadeQuantity` → **Grenades per Box**
- `loot.ammoBoxWeight918` → **Ammo Type Mix**
- `loot.stashGearCoveragePercent` → **Bonus Gear Coverage**
- `loot.expensiveStashMoneyChance` → **Rare Stash Coupon Chance**
- `economy.buyCondition` → **Prices Traders Pay**
- `economy.sellCondition` → **Prices You Pay**
- `economy.traderMinBuyCondition` → **Minimum Sellable Condition**
- `economy.itemValueFloor` → **Broken Gear Value**
- `economy.traderAmmoStock` → **Trader Ammo Supply**
- `economy.medicStock` → **Medic Supplies**
- `economy.consumableCost` → **Item Values**
- `economy.fastTravelCost` → **Guide Travel Fees**
- `equipment.weaponWearPerShot` → **Weapon Wear per Shot**
- `environment.electroAnomalyDamage` → **Damage by Anomaly**
- `environment.psyAnomalyIntensity` → **Psy Drain Strength**
- `alife.humanGroupSize.newbie` → **Human Squad Size**
- `advanced.radiationMechanics` → **Radiation Stamina Penalty**
- `survival.starvationStaminaDrain` → **Starvation Action Cost**

Trader buy-from-player = Prices Traders Pay; trader sell-to-player = Prices You Pay. Bonus Ammo Drops is bonus corpse ammo (not weapon-loaded ammo). Ammo Type Mix affects relative weights, not guaranteed drops. Bonus Gear Coverage selects eligible stash actors; Starvation Action Cost changes action stamina expense, not passive drain.

## 2026-10-19 — Local pull blocked by chmod +x on tracked launch scripts (resolved on main)

The earlier `publish.sh` and `run-latest.sh` were committed with Git mode **100644**, even though the documented local workflow executed `chmod +x` on both. On Linux, `chmod +x` changes the **tracked executable bit**, so Git treats the scripts as locally modified and will refuse `git pull` when an incoming main commit also changes those files. The blocked pull is **not evidence of stale .NET build caching**; it never updated the source checkout at all.

**Permanent Git fix:** both scripts now use Git tree mode **100755**, committed directly on `main`. Fresh clones and future pulls will have their executable bits already set. Do not ask users to run `chmod +x` for these scripts after syncing. Run the checked-in `./run-latest.sh`, which pulls `main`, publishes and checks the EXE/source fingerprints before Wine launch.

**One-time recovery for existing dirty working trees:** first inspect `git diff --summary -- publish.sh run-latest.sh` and `git diff -- publish.sh run-latest.sh`. To preserve any local edits instead of destroying them, use `git stash push -m "local script changes before main sync" -- publish.sh run-latest.sh`, then `git pull --ff-only origin main`, then `./run-latest.sh`. The stash stays safely saved for manual review; **do not automatically pop it**, because restoring old scripts could undo the main-only workflow. This is a user-side working-tree recovery; the GitHub connector cannot alter local disk state. No changes to UI controls, loot semantics, CFG writer or presets.

## 2026-10-19 — CURRENT: verified Wine launch for NATIVE-free Stashes cards

The active `DrawStashChanceTier` has **no `NATIVE` text or native-percent renderer**. Repeated screenshots showing the obsolete caption indicate that the displayed executable/window has not yet been proven to come from current `main`; they are not grounds to reintroduce or merely hide the same row a second time. **Use `bash run-latest.sh` from the project root** for the one-step `git pull --ff-only origin main` → clean publish → source/binary integrity check → Wine launch. A running old app blocks the workflow instead of being silently reused.

`publish.sh` writes `BUILD-INFO.txt` with `source_commit`, `exe_sha256`, `stash_tier_source_sha256` and `stash_chance_native_row=absent`. The runner verifies all four against the current checkout and published EXE, then checks the active C# renderer itself. If any check fails, Wine does **not** launch. The run prints the identities being tested; this is terminal-only build diagnosis, **not** a new player-facing control or label. `tools/audit_stashes_current.py` enforces this contract. No changes to loot probability, source IDs, CFG writer, presets, UI layout, or other visual families.

## 2026-10-19 — CURRENT: Standard / Rare NATIVE caption is forbidden in published builds

**The nine tier Chance cards must not contain a player-facing `NATIVE` caption or any native per-item Chance percentage/range.** The current renderer already removes both; the visible card consists solely of the category label, selected 0–100% value, editable rail, reference tick and small endpoints. Hover help explains relative percent-of-source semantics (with the Rare Coupons absolute exception), without exposing CFG diagnostics. No changes to per-entry Chance calculations, parameter IDs, presets or CFG writing.

A stale running Wine instance or an outdated `publish/win-x64/TrueCustomDifficulty.exe` can still display the old row even though `main` does not render it. `publish.sh` now rejects any literal `"NATIVE` in the dedicated tier renderer, and records `stash_chance_native_row=absent` beside `source_commit` and `exe_sha256` in `publish/win-x64/BUILD-INFO.txt`. The Stashes static audit independently checks the active renderer and the build guard. After a successful pull/publish, compare `git rev-parse --short HEAD` with `source_commit` and `sha256sum publish/win-x64/TrueCustomDifficulty.exe` with `exe_sha256`. Close old Wine instances before testing; never use a previous build as evidence of current UI.

## 2026-10-18 — P1 Extra Stash Gear: two distinct rank-gated bonus categories (LATEST)

**Native/build-time distinction:** `ELIGIBLE STASHES` (0–100%, whole 1% increments) deterministically selects exactly the rounded share of the **800 base + 108 available DLC1 eligible physical ActorSIDs**. Selection happens during mod build, not when the player opens the container. `BONUS CHANCE · ALL ITEMS` (0–100%, whole 1% increments, unchanged default 50%) writes the **real native `PossibleItems.Chance`** independently for each optional bonus candidate; the native engine may produce no reward. The controls are intentionally separate because they change different things; no fake overall stash drop odds.

**Hard authoring budget per active rank branch:** An eligible actor receives exactly one private `TCD_ExtraGear_{ActorSID}_{RankCode}` prototype per native player-rank branch when at least one Bonus Mix class is enabled. The builder deterministically selects up to **two distinct categories without replacement**, using the existing WEAPONS, ARMOR, ARTIFACTS, ATTACHMENTS **0–3× build-time relative weights**. Positive-weight, rank-valid classes are considered only (attachments T2+), so all-zero mix produces no added bonus generator; one valid nonzero class yields exactly one candidate, and two or more classes yield exactly two possible candidates. Each candidate has one `PossibleItems[0]` entry, `MinCount=MaxCount=1` for direct items, `bAllowSameCategoryGeneration=false`, and its own `Chance`. For attachments the existing source-audited singleton `SubItemGenerator` remains responsible for its original internal gate. Selection and SID picks are stable under the same actor/rank/configuration; original native rank branches are preserved, rather than globally mixing tiers. Quest, unique, protected, Weird/Gauss/RPG and unreviewed DLC sources remain excluded.

**Limit of the guarantee:** Code now enforces at most **two authored bonus candidates per active native rank branch**, not two *guaranteed physical drops*. The original separate source loot is untouched except the 13 independently authorized fixed regular weapon entries suppressed when the master checkbox is enabled (including Coverage 0%). Multiple native branches are source-audited by player rank, but engine behavior, additional mods, previously-generated saves, and possible nested singleton behavior are **not** proven by static analysis to obey a physical-inventory maximum. Do not advertise a hard absolute whole-container two-item guarantee without gameplay verification.

**UI and catalog lock:** Retain exactly seven Stash Gear IDs and controls: one master toggle, one compact coverage rail, four compact knob weights arranged 4/2 by width, and one compact native Bonus Chance rail. **No extra item-count slider, percentage duplication, full-width knob row, redundant descriptions, or Vanilla/Base emphasis.** Knob and slider hit targets, default values, input/ruler alignment and existing responsive `570 DIP` host-height breakpoint remain unchanged. Code/parameter catalog, rank inventory, 12 factory preset IDs and the named `StashGear` WPF family stay synchronized.

**Verification:** `tools/audit_stashes_current.py` and `tools/audit_stash_gear_coverage.py` now guard the 1–2-candidate logic and uniqueness. `tests/LootSpecialGeneratorsContract/StashGearCoverage.cs` checks default 2-class generation, the source item ranks, independent Chance=0.5 for two candidates, and single-class forced mixes. No claim that a GitHub push itself executes Python, .NET/Wine or native game loot rolls. This latest P1 section supersedes older one-candidate and old 1-weapon-plus-optional-armor descriptions further below.

# P0 CI and stale-audit reconciliation — 2026-10-18 (main)

## 2026-10-18 — LOCAL STASHES ROUTING AUDIT FIX (LATEST)

This maintenance pass is about the **project's source contracts**, not external CI infrastructure. The production baseline for Stashes has precisely **21 unique** `stashContentAndGearProduction.parameterIds`: six native content Weight scalers, seven Extra Gear controls (including `loot.stashGearEnabled` **once**), and eight independently authored Standard/Rare native Chance scalers. The source catalog has 281 total parameters, of which 39 belong to Stashes across four live UI families.

A previously shipped assertion in `tools/audit_stash_content_gear.py` erroneously expected the `loot.stashGearEnabled` master ID **twice** (`mix + gear + ["loot.stashGearEnabled"] + tierChance`). The **actual production registry is correct**, so the assertion guaranteed a false failure. The audit now requires exact ordered IDs, exactly 21 distinct members, and master cardinality one. The canonical `tools/audit_stashes_current.py` now makes the same independent full-route check and validates 12 unique medical native route IDs (four Weight, four Count, four Chance).

Project-only local static commands from repo root:

```bash
python3 tools/audit_stashes_current.py
python3 tools/audit_stash_content_gear.py
python3 tools/audit_stashes_mixer_ui.py
python3 tools/audit_extra_stash_gear.py
python3 tools/audit_stash_gear_coverage.py
```

This change does **not** modify gameplay, BPatch generation, UI coordinates, actor allowlists, rank pools, or factory presets; only auditing and its relevant documentation. No successful local Python execution, .NET compilation, Wine launch or native-game drop testing is asserted by a GitHub push. The older discussion below is retained for provenance, but does not supersede this current Stashes route contract.


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


## 2026-10-19 — Current Stashes audit contract reconciliation

The current Stashes catalogue contains **41 source parameters**, with **34 visible** and three groups: Stash Content, Stash Medical Composition, and Stash Gear. Ammo quantity is edited through the native `StashContentMix` panel; `StashAmmoRadar`, `IsStashAmmoAnchor` and `IsStashAmmoMember` are retired. The old `StashRewardProfile` / `DrawCompactStashRewards` path must not be reintroduced to satisfy an obsolete global audit assertion. The current `audit_stashes_current.py` contract is the regression authority for the active Stashes structure.


## 2026-10-19 — Compact Chance cards / tooltip contract

Standard (Medical / Drinks / Coupons) and Rare (Ammo / Medical / Food / Drinks / Grenades / Coupons) retain their nine source-backed Chance parameters and CFG writer semantics. The additional per-item `NATIVE` range label is now removed **only from the nine player-facing cards**; native CFG source values remain available to internal auditing. Cards have a 70-DIP body with a 78-DIP row pitch, 3/2 responsive columns and matching measured editor heights (785/1095 DIP). Hover offers a short warning that a relative 100% preserves original item Chance and does not guarantee a stash drop; Rare Coupons gets its separate absolute per-entry Chance clarification. `tools/audit_stashes_current.py` guards the revised geometry, tooltip wiring and removal of the old row. No source ID, probability, preset or CFG-generation change.
