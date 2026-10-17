## STASHES PLAYER-FACING LOOT STATUS (CURRENT) — 2026-10-17

**The six global item-type knobs remain the only Stash Content Mix edits.** Knob values represent a multiplier applied to the original native *selection* field, NOT an overall chance to find that item in a stash. `1×` = ORIGINAL source selection, `0.1–0.9×` = REDUCED native selection (not a proportional real-world drop-frequency claim), `0×` = EXCLUDED from the mod-controlled eligible generators. The original selection can be a relative Weight or independent Chance, with no fabricated single global probability. Do not claim a positive multiplier guarantees appearance or that `0×` removes items from quest/scripted/protected sources.

**What the player sees:** each original one-row colored knob gains a short neutral `ORIGINAL`, `REDUCED`, `EXCLUDED` (or MEDICAL `PARTIAL`/`ADJUSTED`) status; beneath is one bounded always-visible read-only **WHAT CAN APPEAR** panel with six item-kind descriptions and dynamically recomputed eligibility/status. Descriptions are source-backed: AMMO supported ammunition, MEDICAL bandages/medkits/army medkits/anti-rad, FOOD bread/sausage/canned food, DRINKS water/energy drink/beer/milk/vodka, GRENADES RGD-5/F1, COUPONS native coupon bundles. MEDICAL explicitly lists affected subtypes as `EXCLUDED: ...` when medical composition or MAX=0-items settings disable them; its global knob remains independent, with no new persisted IDs. Status copy states `CAN APPEAR` or `STILL POSSIBLE` only as eligibility, never as promised physical loot. Footer explains possible is not guaranteed and `OFF` applies only to mod-controlled stashes.

**Retire all developer/source counters from player UI:** `GLOBAL SOURCE COVERAGE`, `ITEMGEN`, `WORLD`, `WEIGHT`, `CHANCE`, entry counts and W/C abbreviations; keep original 463-group/1,124-row inventories only for source validation and executable CFG parity. Panel shows two columns of three source-aligned categories when outer width ≥670 DIP (panel height 290 DIP; family measured 545 DIP for width ≥700), otherwise one column of six (panel height 500 DIP; family measured 755 DIP). Starts y237 and remains ≤778 DIP wide, with small category swatches, compact neutral typography, no extra editing dimension, no extra rank/branch/drop-rate controls. All original 26 Stashes IDs, 0–1× / 0.10× global snapping, native eligibility/quest safety, Weight/Chance writer transforms and catalog/UI synchronization are preserved. Runtime/Wine and game acceptance are not claimed. Older global-source-coverage proposals below are historical, **not current UI**.

## STASHES GLOBAL ITEM-CATEGORY SOURCE COVERAGE (CURRENT) — 2026-10-17

**One global knob per item type, never per generator branch or rank.** The six persisted Stash Content Mix controls (AMMO, MEDICAL, FOOD, DRINKS, GRENADES, COUPONS) each apply a single identical 0–1× multiplier in 0.10× increments to *all eligible matching source item SIDs*, irrespective of Basic/Standard/Rare ItemGenerator tier, SmartLoot prototype, native branch, player rank or difficulty. Supported guarded direct/DLC1 stash paths share that category mapping. Existing native rank/difficulty eligibility gates remain, as do protected actor/quest constraints; the global knob does **not** create absent items or guarantee physical spawns. Original Weight and Chance fields keep distinct native semantics. Medical detail effects remain MEDICAL-only and there is no global combined loot probability.

**Visible compact UI:** six unboxed knobs on one row, then an always-visible noninteractive **GLOBAL SOURCE COVERAGE** panel, **not** a Native Branch browser. Six category readouts each show original authored Weight/Chance item-row counts and ItemGenerator/SmartLoot counts **across every native rank/group/prototype**, with no generator, branch, rank, difficulty, prototype, group or disclosure selectors. Counts are from the audited 463 original groups / 1,124 item rows; supported guarded direct/DLC1 overrides are **not included in these inventory counts**. No normalized six-way percentages or invented drop rates. Bounded 778-DIP width, summary starts at y237; total measured family height = **520 DIP for widths ≥700 DIP**, **665 DIP narrower**. Source keys and 26 persisted Stashes IDs remain unchanged. Color/size hierarchy, white neutral copy and gold changed *digits only* follow the locked visual system. The old Native Branch UI below in historical sections is retired from the player-facing screen; its source catalog is kept for backend CFG parity only. Real .NET/Wine/gameplay acceptance remains pending.

**CURRENT STASH CONTENT MIX DECISION (2026-10-17):** Use the existing native-field hybrid rather than a chance-only conversion: the original inventory contains **429 Weight groups and 34 Chance groups**. Six global item-category knobs use **0–1× / 0.10× steps**, adjusting only their own matched source entries across tiers, ranks, and SmartLoot while preserving each original group's selection logic. Native per-branch details are always visible (bounded width 778 DIP, browser y237, 675-DIP family height), with no disclosure button. Existing 26 IDs, source writer logic, safety gates and grouped read-only preview remain intact. Catalogs: [UI-DESIGN-LOCK](../UI-DESIGN-LOCK.md) and [VISUAL-CONTROL-SYSTEM](../VISUAL-CONTROL-SYSTEM.md). .NET/Wine/game acceptance is pending. Older phase-specific UI designs further below are retained as historical rationale.

[Revalidated consumer graph](STASH-CONSUMER-GRAPH-2026-10-16.md) now expands four pipe-separated multi-actor wrapper sources into 10 physical edges; 5,013 edges total. One Rostok wrapper is blocked entirely because a protected actor shares the patch assignment. No branch count, rank gate, or item Chance/Weight inference changed.

**GENERIC STASH AND NPC CORPSE PRE-GAME GATE (2026-10-16):** [Generic source-first readiness](GENERIC-LOOT-PRE-GAME-TEST-READINESS-2026-10-16.md) prioritizes 35 original ordinary-stash actor/source tests (including 15 new untouched baselines) plus 32 generic human corpse tests. The earlier cabin direct Medkit and Rostok special cases remain 10 last-wave regressions in the [45-case stash matrix](stash-ingame-loot-test-matrix.csv). Normal corpse consumable clones and difficulty-selected SmartLoot are distinct layers; no actor spawn or item probability is proven by source manifests. Both matrices remain NOT_RUN. Do not broaden production source edits without evidence.

**PRE-COMPILE STASHES AUDIT (2026-10-16):** Source review of all five phases found and fixed a genuine missing C# method (`StashCategoryIndex` used by both the older stash previews and Phase 4 browser), a stale Phase 4 static assertion (`TuneCount` instead of the Phase 5 `SmartCount`), and a stale Phase 3 project-audit signature (`private` instead of the actual `public MedicalDisabled`). The browser now resolves all 46 distinct authored item SIDs through the two production category mappings, including SmartLoot `Antirad`; the Phase 5 executable harness checks the same coverage. Browser hover no longer forces Arrow over an underlying knob or drops the Hand cursor on repeated motion. Phase 1 inventory (463 branches/1,124 item rows) and Phase 2 source manifests (5,013 edges, 106 protected actors, 112 approved DLC1 actors) were independently cross-counted from the GitHub source and match their documented scopes. **This is source-level remediation only: no successful .NET build, Wine execution or game test is claimed.** Latest GitHub Actions run for `ab725bb` reported failure in all 14 jobs with zero reported steps (also observed in the preceding run); treat CI as an infrastructure/runner-or-account blocker, not as passing or informative executable acceptance. Phase 4 still intentionally lacks per-physical-actor activated/blocked consumer statuses in the browser, so it is a **native source browser**, not a live world-stash consumer inspector. Phase 5 remains **not runtime accepted**.

**CURRENT PHASE 5 2026-10-16:** [Native preview vs emitted CFG parity and runtime acceptance contract](STASH-PHASE-5-CFG-PARITY-AND-ACCEPTANCE-2026-10-16.md) is now implemented with shared SmartLoot count suppression; previously documented 'Phase 5 NOT YET DONE' is historical implementation-phase status. .NET/Wine/game execution is still pending.

**CURRENT PHASE 4 2026-10-16:** [Native per-branch source browser](STASH-PER-BRANCH-BROWSER-2026-10-16.md) is now implemented in the Content Mix UI, with 463 runtime-derived, read-only branch paths and correct rank/difficulty selectors. Native writer/preview parity execution and Wine UI tests still pending; do not treat source eligibility as a generated CFG or physical stash outcome. This status supersedes the earlier phases below.

**CURRENT 2026-10-16 — STASHES PHASE 3 SHARED EFFECTIVE CFG:** [Phase 3 shared effective CFG calculator](STASH-EFFECTIVE-CFG-CALCULATION-2026-10-16.md) tracks one numeric source-to-field calculator now called by tier and SmartLoot writers and existing Stashes source examples. Basic RGD-5/Rare Coupons bonus Chance ordering and Medical zero-count are shared. The new generated CFG contract has not yet been executed on Wine, and dynamic per-actor activation remains conditional. Phase 4 per-branch browser is implemented; runtime verification is pending.

# Stashes — generator-branch mixer architecture and source inventory

**Status:** **Phase 1 native source inventory, Phase 2 guarded consumer graph and Phase 3 shared effective-field calculator implemented** (2026-10-16). See [5,013 source consumer edges and guard details](STASH-CONSUMER-GRAPH-2026-10-16.md). **Shared field calculation is now implemented in writers and current sample previews, but its generated-CFG regression has not run in this GitHub-only session; dynamic per-actor patch activation is still separate. The read-only per-branch selector is now implemented (Phase 4); per-branch editability is NOT implemented.** Existing Stashes UI and CFG writers remain current; source eligibility is not an actual generated/drop outcome.

**Authority:** original STALKER 2 v2.0.5 CFG evidence already extracted into this repository, with GameData input references below. The embedded writer baseline and SmartLoot row catalog are the current **source records**, not the game engine's source code. Runtime mechanics and probabilities remain subject to independent testing.

## Why this design is necessary

Previous six-way Stashes preview normalized independent UI multipliers as if they formed one pool. That produced six artificial **16.7%** shares at `1×`. Replacing the sum with hand-picked original `Weight` pools and separate `Chance` samples corrected those *particular source examples*, **but the current UI still does not show the full effective CFG output**. In particular, the Basic RGD-5 and Rare Coupons samples do not yet compose the distinct **Stash Rewards** Chance controls with the global category knobs. An example source `Chance=0.4` is not proof that 40% of opened stashes grant that item.

The agreed architecture is **native-generator branches first; one shared source-to-effective-state calculation for the preview and patch writer**. The mixer displays independently gated source groups, not a universal six-category loot probability. Generic category knobs can remain as global *inputs* and source groups can be inspected individually.

## Authoritative source layers and chain

```text
SpawnActorPrototypes.cfg ordinary approved physical container
   ├─ ItemGeneratorSettings → ItemGeneratorPrototypes.cfg
   │    → Basic / Standard / Rare production source generator
   │    → ItemGenerator.[categoryIndex].PossibleItems.[itemIndex]
   │    → native Category, Diff and PlayerRank gates
   │    → Weight OR Chance per branch; MinCount / MaxCount per item
   │
   └─ StashPrototypeSID → StashPrototypes.cfg world SmartLoot
        → original SmartLoot prototype
        → ItemGenerators.[rankIndex].SmartLootParams.<group>.[groupIndex]
        → Items.[itemIndex], Weight OR Chance
        → MinSpawnChance / MaxSpawnChance / ItemSetCount
```

These two layers **may coexist on one stash actor**. A link to one does not imply it is the only drop mechanism. The existing writer uses reference clones and narrowly scoped consumer redirects; it is not a global overwrite of original generator definitions. The rank/difficulty filters are authored eligibility conditions, not numeric probability factors. Do not flatten an actor's multi-layer route, different groups or different ranks into one weighted list.

**Snapshot, generated by source-only audit:** [`stash-generator-branches.csv`](stash-generator-branches.csv).

**Regeneration / hard drift check:** `python tools/audit_stash_generator_branches.py` from repository root. The script reconstructs the snapshot *from the same embedded authored inputs* and fails if names, membership, branch boundaries or fields drift. Intentional source upgrades: inspect CFG provenance, run `python tools/audit_stash_generator_branches.py --write`, review the diff and update this document. The snapshot is **review material**, not an additional source of truth or a runtime-calculation table. No duplicate calculation may be implemented against it.

### Exact source inventory (game 2.0.5)

| Source layer | Native source groups | Weight groups | Chance groups | Authored item rows |
|---|---:|---:|---:|---:|
| Basic `GamePass_Stash_ItemGenerator_Cheap` | 12 | 8 | 4 | 52 |
| Standard `GamePass_Stash_ItemGenerator_Common_Var1` | 10 | 4 | 6 | 39 |
| Rare `GamePass_Stash_ItemGenerator_Rare` | 19 | 7 | 12 | 42 |
| World SmartLoot: 13 `StashPrototypes.cfg` prototypes | 422 | 410 | 12 | 991 |
| **Total** | **463** | **429** | **34** | **1,124** |

The 463 count is of **distinct catalogued source-group paths**, **not** 463 independent spawn events or a statement of active consumer reachability. None of these 463 groups mixes native `Weight` and `Chance` **inside the same source-group item list** in the current embedded inputs. The native group may nevertheless have separate outer spawn/gate rules. Repeated `ItemPrototypeSID` occurrences in a single group stay repeated; never deduplicate them when summing weights.

Source paths in the snapshot use:
- ItemGenerator: `GameData/ItemGeneratorPrototypes.cfg` → source SID + `ItemGenerator.[i].PossibleItems`; record `EItemGenerationCategory`, `Diff` and `PlayerRank` from category fields.
- SmartLoot: `GameData/StashPrototypes.cfg` → original prototype SID + `ItemGenerators.[rank].SmartLootParams.<Group>.[set].Items`; record native rank index and `MinSpawnChance`/`MaxSpawnChance`/`ItemSetCount` fields (when present).

**Provenance:**
- `src/TrueCustomDifficulty/Data/cfg-writer-baseline.json` → `special.cheapStashProduction.templateCategories`, `special.mediumStashAmmoProduction.templateCategories`, `special.expensiveStashProduction.templateCategories`.
- `src/TrueCustomDifficulty/Data/smartloot-world-source-entries.csv` → 991 item records; writer consumes the same embedded resource in `CfgPatchWriterService.StashSmartLoot.cs`.
- `CfgPatchWriterService.StashTuning.cs` owns category/medical multipliers, medical count suppression and TCD tier customization; `CfgPatchWriterService.cs` writes Basic/Standard/Rare ItemGenerator clones.
- `cheap-stash-production-redirect-manifest.csv` (1,271 source redirect rows), `medium-stash-production-redirect-manifest.csv` (450), `expensive-stash-production-redirect-manifest.csv` (305) contain source edge references. Protected/unsafe routes are filtered by the **writer**. These raw manifest numbers are **not** an approved active stash count.
- `smartloot-regular-world-actor-manifest.csv` inventories 831 candidates; **803 ordinary no-clue base actors** are eligible after named-clue protection, not every candidate. DLC1 can use **112 separately approved** ordinary actor routes only when DLC1 is present. See [SmartLoot source protection](SMARTLOOT-PRODUCTION-EXTENSION-2026-10-16.md) and [protected base actors](BASE-STASH-PROTECTED-ACTORS-2026-10-16.md).

Other direct item entries, quest rewards, unique/blueprint stashes, DLC-only generator families, boosters, crate loot and already generated inventories **must not silently join** this 463-branch index. They require separate explicit source approval and evidence before preview claims coverage.

## What the mixer may truthfully show

**Selected native Weight branch:** show each *actual item row* or a clearly labeled derived category rollup **only for that precise `PossibleItems`/SmartLoot group**. If all eligible members of the group use native `Weight`, apply exactly the writer's effective factors to each source `Weight`, then normalize by the **sum inside that same branch**. A zero total is an *empty/undefined engine-selection case*, not automatic 0% output guarantees. Display source and effective weights and show rank, difficulty, generator, category/group indices. Never normalize across different categories with independent group rolls.

**Selected native Chance branch:** show original and effective **per-entry Chance** alongside other values on *that same native branch*. Apply author-defined bonus Chance controls before global category/medical multipliers, with the writer's clamping and special count suppression; `0` and `1` are meaningful source values. Do **not** sum Chance fields to 100%, divide them into a stacked probability pie or translate directly into the rate of entire stashes.

**SmartLoot branch:** show original group/Set path, rank, item `Weight`/`Chance`, `MinSpawnChance`, `MaxSpawnChance`, `ItemSetCount` and any item-count overrides. The current SmartLoot writer changes group spawn endpoints only if every eligible item in that group has the same effective factor. The effective display must match that rule; do not automatically scale endpoints for heterogeneous biases.

**Generator/consumer context:** distinguish *source unchanged*, *changed in a TCD clone*, *selected by an approved consumer redirect*, and *not applicable* (filtered/quest/unique/DLC disabled). A branch's presence in source data is not evidence that a particular physical stash consumes it.

**Cross-control examples that must be composed by the shared engine:**
1. Basic `GrenadeRGD5`: original native `Chance=0.1`; optional `loot.cheapStashGrenadeChance` overrides the raw source; `loot.stashMixGrenades` then scales the effective value. The current sampled UI ignores the first component; this is **known parity gap**.
2. Rare `MoneyCommon` (and other ranks): original `Chance` and difficulty-gated `loot.expensiveStashMoneyChance` override; then `loot.stashMixCoupons`. The current sampled UI ignores the bonus control and selected difficulty; **known parity gap**.
3. Global `loot.stashMixMedical` × Bandage/Medkit/ArmyMedkit/AntiRad detail bias, plus Medkit/ArmyMedkit `MIN/MAX` and special `MAX=-1 = 0 ITEMS`; preserve original count floors/caps, explicit zero and native group selection.
4. Original source `Chance=1` under `3×` remains clamped to 1; original zero stays zero. For `Weight` preserve native arithmetic without presenting a global chance. Item group or SmartLoot zero rules must not be inferred beyond the writer's explicit suppression.

## Implementation phases (locked order)

1. **Source graph and branch inventory — PHASE 1: inventoried.** Check 41 ItemGenerator + 422 SmartLoot branch keys; retain every original item entry, `Weight` vs `Chance`, numeric source values, `Diff`/`PlayerRank`, SmartLoot rank/group/set boundaries and spawn fields. Generated snapshot and deterministic audit live in this commit. This does **not** certify per-actor reachability.
2. **Consumer graph and static safety gate — PHASE 2: SOURCE-GRAPH COMPLETE.** [Checked 5,013 source edges](STASH-CONSUMER-GRAPH-2026-10-16.md) join tier and SmartLoot original actor sources to the Phase 1 generator families/branch keys and retain all blocked/indirect/DLC references without reauthorizing them. Six approved DLC CheapNoFresh actors are clearly flagged outside the 463-index. **Dynamic per-build patch/affected-prototype activation, the full transitive graph of unindexed local wrappers and separately authored direct item rewards are NOT covered by this phase** and require Phase 3 or a separate source-approved extension.
3. **Shared source-to-effective-field calculator — PHASE 3 CODE IMPLEMENTED, GENERATED-CFG TEST PENDING.** [Implementation and acceptance contract](STASH-EFFECTIVE-CFG-CALCULATION-2026-10-16.md): `StashEffectiveState` is now called by all three ItemGenerator native tier writers, SmartLoot item/group output, and existing read-only sample previews; the separate direct/DLC medical tuning wrappers also delegate to it. Original fields, bonus-then-category order, reference clones, medical suppression, rank/difficulty-gated Rare Coupons and source-formatted values are preserved. New `VerifyStashEffectiveParity` asserts predicted values against actual generated CFG leaves, but .NET/Wine execution is **not confirmed**. Full per-actor dynamic output activation, DLC CheapNoFresh's dedicated bonus preparation and branch chooser UI are separate concerns.
4. **Per-branch mixer UI — CODE IMPLEMENTED, RUNTIME TEST PENDING.** Compact generator selector (`ItemGenerator` tier / `SmartLoot` prototype), actual branch selector (rank, difficulty, group), readable Weight or Chance visualization and optional per-item details. Keep single-row six color-keyed knobs for global category tuning, Stamina Usage yellow modified-digit conventions, no redundant tiles/whole-width controls; follow `docs/UI-DESIGN-LOCK.md` and `docs/VISUAL-CONTROL-SYSTEM.md`. Do not create 463 knobs/cards or assume all branches are active. Changes to per-branch **editability** would add new persistable settings, change presets/writer/catalog and need separate explicit design approval.
5. **Parity and runtime acceptance — NOT YET DONE.** Unit tests for representative Weight/Chance, 1×/0×/3×, all-zero pools, rank/difficulty gating, mixed Basic bonus + category, Rare coupon + category, medical suppression, group endpoint update, DLC/no-DLC and protected actors. Compare every previewed field against actual emitted CFG (not merely source JSON). Build `net10.0-windows`, run Wine controls at normal/narrow width, verify untouched quest/unique output and newly generated unpopulated stash behavior in game. **No game-probability proof until measured in game.**

## Acceptance invariants

- One native branch ID = one exact source generator SID + category/rank/group/set path; do not merge across branch gates or source layers. All 1,124 source item rows remain traceable.
- `Weight` charts normalize only homogeneous **native** groups. `Chance` values are direct, source-field percentages and separate rolls. Never label either as all-stash loot odds.
- Display and writer consume the same effective-value snapshot, preserving current reference/override semantics. A preview that omits Stash Rewards bonus controls must explicitly identify itself as partial rather than output parity.
- The protection filter is evaluated on consumer edges, *not* implied by presence of a source branch in the inventory.
- Existing **26 Stashes persisted parameter IDs**, factory/user presets, written CFG semantics and all source guards remain unchanged in Phase 1. New UI bindings must not be presented as implemented before a code+docs+tests commit.
- Every phase lands directly on `main` as **one verified code + relevant docs commit**, with source provenance and audits; builds/game effects must be reported separately from static review.

## Phases 1–2 source audit validation

```bash
python tools/audit_stash_generator_branches.py
python tools/audit_stash_consumer_graph.py
python tools/audit_stashes_mixer_ui.py
```

The first audit proves native branch source inventory equality; the second proves eligible/blocked physical source joins reproduce the writer-manifest guards, not dynamic emission. The existing mixer audit is a separate regression check for current UI, not proof of future shared output parity. **A 1:1 match to the source groups becomes a 1:1 match to generated CFG only after Phase 3 parity tests**; neither claim proves actual game-world drop probabilities.
