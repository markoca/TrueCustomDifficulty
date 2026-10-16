**PHASE 5 FOLLOW-UP 2026-10-16:** [Generated CFG parity suite and safety acceptance](STASH-PHASE-5-CFG-PARITY-AND-ACCEPTANCE-2026-10-16.md) now asserts emitted native tier and conditionally affected SmartLoot fields against the source browser as well as existing protected 106-base-actor and 112-DLC1 gates. Source consumer statuses still mean conditional eligibility only, not active or guaranteed item-spawn routes. Tests have not yet been run under Wine in this session.

**CURRENT 2026-10-16 — STASHES PHASE 3 SHARED EFFECTIVE CFG:** [Phase 3 shared effective CFG calculator](STASH-EFFECTIVE-CFG-CALCULATION-2026-10-16.md) tracks one numeric source-to-field calculator now called by tier and SmartLoot writers and existing Stashes source examples. Basic RGD-5/Rare Coupons bonus Chance ordering and Medical zero-count are shared. The new generated CFG contract has not yet been executed on Wine, and dynamic per-actor activation remains conditional. Phase 4 per-branch selector is not built.

# Stashes phase 2 — protected consumer graph and native branch join

**Status: source-level consumer graph IMPLEMENTED (2026-10-16), writer/runtime parity NOT claimed.** Phase 1 native branch source inventory remains canonical: [native branch architecture](STASH-GENERATOR-BRANCH-ARCHITECTURE-2026-10-16.md), [463 native source branches](stash-generator-branches.csv). This phase adds [a checked-in consumer edge graph](stash-consumer-generator-edges.csv) and [regenerating / checking code](../../tools/audit_stash_consumer_graph.py) directly from the **same manifests used by the CFG writers**.

The graph is a normalized *relation*, **not a simulated loot output**. Query by physical actor ID to join an actor's source references with actual generator group paths; do not expand every physical actor into thousands of duplicate edit knobs. Scope is the current repository's **three tier redirection manifests, audited base/DLC1 world SmartLoot actor manifests, six separate DLC CheapNoFresh actors, and their observed original ItemGenerator references**. Other globally authored, protected, direct, scripted, already generated or one-off rewards are **not automatically inside this relation**.

## Source graph counts

| Edge type | Rows | Writer guard | Interpretation |
|---|---:|---|---|
| Tier redirect — eligible | **1,892** | Tier parameter changed + exact redirect gate + DLC1 if applicable | Approved original source edge; not proof that a specific native group was rolled |
| Tier redirect — blocked protected actor | **105** | `IsApprovedStashTierRedirect` denies 106-ID base protected roster | **Never** treat even an indirect wrapper row as editable |
| Tier redirect — blocked DLC1 not allowlisted | **29** | DLC actor must appear in 112-ID allowlist | Manifest entry retained as evidence but **not written** |
| SmartLoot prototype reference — eligible | **1,816** | Clue-free base actors, approved DLC1 list, writer must detect affected prototype; DLC installation required for DLC rows | Source-level prototype reference (not item roll) |
| SmartLoot prototype reference — blocked named clue | **56** | 28 of 831 base actors have non-empty ClueVariableSID, often 2 original prototypes each | Reference remains auditable but source clone may **not** redirect these actors |
| Separate DLC1 CheapNoFresh | **6** | DLC1 + dedicated writer condition + exact six-actor allowlist | **Not part of 463 phase-1 source branches**; needs separate original source-index extension if supported in the mixer |
| Original ItemGenerator source reference — observed only | **1,103** | No write permission inferred | Parallel original source references, including local custom wrappers, independent/unknown entries and six CheapNoFresh refs |
| **Total source evidence edges** | **5,007** | All statuses retained | Multiple references per actor are intentional; **not** 5,007 unique containers |

All 2,026 tier manifest rows (Basic 1,271, Standard 450, Rare 305) are retained, including the **105** protected rows (Basic 12, Standard 43, Rare 50), with physical-container SID + per-row `edit_prototype`, file/path and original/clone SID. The **29 DLC rejections** arise from 10 Standard and 19 Rare DLC rows not in the writer's 112-actor allowlist. The 1,872 SmartLoot reference rows arise from 831 original base actors (1,656 ordered prototype references) plus 112 approved DLC1 actors (216 references). Base SmartLoot status is **1,600 eligible + 56 denied**, and DLC1 contributes 216 eligible references, with the whole-actor `ClueVariableSID` rule retained.

Counts above are writer-*guard* summaries derived from the exact manifests. **ELIGIBLE_CONDITIONAL is not ACTIVE.** The three tier writers run only when the relevant setting is modified, and SmartLoot clones/consumer redirects run **only for prototypes whose source groups are affected** by actual modifications; vanilla profiles write no new SmartLoot redirection. DLC1 edges require detected DLC1. The actor list and source group list do not by themselves prove the runtime engine ever generates those items.

## Relationship keys and resolution

The checked CSV records `source_scope`, `physical_actor_sid`, `source_layer`, `edge_kind`, `patch_file`, `edit_prototype`, `edit_path`, `source_reference_sid`, `canonical_generator_sid`, `planned_clone_sid`, `rank_gate`, `source_group_layer`, `branch_count`, `guard_status`, `guard_reason`, `applicability`, and `consumer_provenance`.

- **ItemGenerator:** a direct `ItemGeneratorSettings.[n].PrototypeSID` references the GamePass generator (Basic/Standard/Rare) or rank wrapper; a `LOCATION_WRAPPER_SUBGENERATOR` is an *indirect* reference inside `GameData/ItemGeneratorPrototypes.cfg`, associated with its physical container SID. Keep the source wrapper `edit_prototype` and the physical actor SID distinct. The four `Stash_Cheap/Medium/Expensive_{Newbie,Experienced,Veteran,Master}` wrapper SIDs resolve to their **tier source generator** but retain the source wrapper name and actor-bound rank so eligibility isn't widened.
- **SmartLoot:** each `OriginalStashPrototypeSIDs` ordered token references a distinct `GameData/StashPrototypes.cfg` prototype. A clone is `TCD_<original SID>` only when the prototype is affected and the specific actor is allowlisted. The join to Phase 1 is **(`SmartLoot`, `canonical_generator_sid`)** → `ItemGenerators.[rank].SmartLootParams.<Group>.[set].Items`, retaining rank/set and native Weight/Chance group.
- **ItemGenerator native branches:** join **(`ItemGenerator`, `canonical_generator_sid`)** → each original `ItemGenerator.[i].PossibleItems`, including source difficulty/rank/Category gates and original Weight or Chance. A consumer's broad tier/wrapper edge does **not** mean every difficulty/rank branch is applicable simultaneously.
- **Observed only:** all original `OriginalItemGeneratorSIDs` are preserved independently as **non-authorizing** source observations. A local wrapper not found in the source inventory remains unresolved; do **not** invent a transitive route or upgrade its status because its name resembles a generic generator.
- **DLC CheapNoFresh:** six approved actor entries are separately recognized with zero native branch join count. The *dedicated* DLC1 writer consumes its own 35 original source rows and must be added to the source branch inventory in a later separately verified extension. Excluding it from the 463 count is intentional and explicit, not a dropped actor.

**Important:** a row with `branch_count > 0` means the source generator can join to that many **authored** branch definitions. It is not a count of reachable branches for the current rank/difficulty/game state, a count of patches, nor a count of individual spawn events.

## Guard evidence and limitations

- The writer gates the three tier manifests via `IsApprovedStashTierRedirect(file, actorSid, physicalContainerSid)`, which rejects the [106 protected base actor IDs](../../src/TrueCustomDifficulty/Data/base-stash-protected-actor-manifest.csv), both direct and indirect, and gates **DLC actor-file references** to the [112 exact eligible DLC1 actors](../../src/TrueCustomDifficulty/Data/dlc1-stash-eligible-actor-manifest.csv). Protection applies to the physical actor even if a wrapper's local `ItemGeneratorPrototypes.cfg` SID looks harmless.
- Base SmartLoot writer uses `LoadSmartStashActors()` → 831 audited candidates → **803 no-clue** actors; a separate 112-actor allowlist is used if DLC1 is enabled. It copies the set of **affected** original source prototypes to `TCD_` names before rewriting only corresponding actor `StashPrototypeSID` entries. A named clue blocks all its SmartLoot references.
- A single physical container can carry **both ItemGenerator and SmartLoot**, and can additionally have manually authored direct item generator entries. Count the paths separately; never merge the two source group/Weight/Chance domains into a single probability bar.
- The source roster omits intentionally excluded quest/blueprint/unique cases not approved into these manifests, along with unindexed direct medical/Cabin/CNPP/other DLC generator sources. **Absence from the CSV means not inventoried by this scoped graph, not known absent in the game**.
- The graph records current writer **static** gate semantics. It does not predict which generators actually get cloned for a given preset or which patched CFG fields are emitted; that requires the **shared effective-state engine + generated-CFG parity** in Phase 3. Nor does it prove in-game drop outcomes. `Chance` is native per-entry Chance, and `Weight` is comparable only within the exact native group.

## Reproducibility and actor drill-down

At repository root:

```bash
python tools/audit_stash_generator_branches.py
python tools/audit_stash_consumer_graph.py
python tools/audit_stashes_mixer_ui.py
```

On intentional source manifest changes: review the source and writer guards, then run `python tools/audit_stash_consumer_graph.py --write` and inspect the CSV diff; the ordinary run fails if the checked-in relation drifts.

Inspect one physical actor and its source evidence:

```bash
python tools/audit_stash_consumer_graph.py --actor 006CDB9847D04582966F109A979D242A
python tools/audit_stash_consumer_graph.py --actor 006CDB9847D04582966F109A979D242A --expand-branches
```

The expansion joins the edge with actual Phase 1 native group paths, source Weight/Chance group kind and gates, excluding observation-only edges from a second expansion (they are already represented as source evidence). **It never marks a branch dynamically active.**

## Acceptance and handoff to Phase 3

- [x] Preserve all 463 Phase 1 source groups and 1,124 item rows; resolve all approved direct tier and SmartLoot references to exact generator family/group definitions.
- [x] Preserve original actor SID, path and original ordered prototype list, including indirect local wrappers and parallel ItemGenerator + SmartLoot.
- [x] Record 105 protected tier manifest edges and 29 denied DLC tier manifest edges as blocked, not writable.
- [x] Preserve 28 protected named-clue base SmartLoot actors as **56 blocked prototype references**, and honor 112 DLC1 positive allowlist.
- [x] Flag six additional DLC1 CheapNoFresh entries as approved but **not part of Phase 1 native group index**.
- [x] Reproducible source-to-graph CSV and actor expansion CLI, integrated into existing Stashes audit.
- [ ] **Phase 3:** share one source→effective CFG transformation between writer and UI; distinguish actual per-build affected prototypes/parameters and verify against emitted CFG under each rank/difficulty and DLC presence.
- [ ] .NET/Wine/in-game validation; this commit changes **no production code, source writer output, UI controls or persistent parameters**.
