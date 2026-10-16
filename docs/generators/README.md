# Generator map data

This directory is the machine-readable companion to [`../GENERATOR-MAP.md`](../GENERATOR-MAP.md).

Source baseline:

```text
STALKER 2 runtime/game + DLC CFG dump 2.0.5
SHA-256: 739ecb4a60e2d98181cc6b326894d184b76a09c29b94a66c18d7b0da2c37a83a
```

The runtime dump is treated as the source of truth for effective values. Inheritance metadata is retained separately because effective runtime exports can flatten parent fields into children. Item Generator tables contain **3,452 prototype rows / 3,451 distinct SIDs** because both base and DLC1 define an `empty` sentinel; `(scope, SID)` is the unique key.

## Human-readable catalogs

| File | Purpose |
| --- | --- |
| `../GENERATOR-CATALOG.md` | one row per Item Generator, including complete direct and recursively reachable item sets |
| `../GENERATOR-GAMEPLAY-GROUPS.md` | all 3,452 generators grouped by gameplay role, propagated impact context, full reachable items and target policy |
| `GENERATOR-TARGET-ALLOWLISTS.md` | human-readable strict single-domain candidate tables |
| `GENERATOR-CONSUMER-CATALOG.md` | all 13,647 external consumer edges with exact generator, context, consumer, file, line and key |
| `GENERATOR-BRANCH-CATALOG.md` | exact branch/entry table for every effective generator category and item/nested-generator row |
| `DLC1-STASH-REGULAR-PRODUCTION-2026-10-16.md` | Implemented DLC1 safe-source writer: 112 ordinary SmartLoot actors, six separate CheapNoFresh ItemGenerator redirects, 69 repaired Medium file paths and the unchanged UI/preset contract |
| `DLC1-STASH-GENERIC-ITEM-AUDIT-2026-10-16.md` | standalone DLC1 actor census and generic source/path triage; links exact 196-row actor/source CSV, notes separate DLC1 patch-file scope and source-manifest target-path hazards |
| `STASH-SAFE-EXPANSION-FOLLOWUP-2026-10-16.md` | follow-up actor-level audit from byte-identical 2026-10-16 dump: 803 active, 28 named-clue deferred, 71 special excluded, 227 ItemGenerator-manifested no-SmartLoot and 12 unresolved; links the 40-row deferred actor CSV |
| `SMARTLOOT-PRODUCTION-EXTENSION-2026-10-16.md` | exact 831-actor normal production SmartLoot redirect scope and 991 verbatim source item rows from user CFG dump, original rank/set/source semantics and 71 protected quest/unique/blueprint-linked actors; runtime validation still pending |
| `STASH-REGULAR-CATEGORIES-AUDIT-2026-10-16.md` | full tracked standard item class source inventory: 17 GamePass cores/245 rows, 13 SmartLoot roots/991 item rows, production/quest/test boundary, missing consumer manifest; does NOT change gameplay |
| `STASH-MEDICAL-ALL-SOURCE-AUDIT-2026-10-16.md` | consolidate 6 medical ItemGenerator stash cores, both untouched StashMedicine SmartLoot families, independent medical destructible pools and quest/NPC exclusions; separate verified source inventory from runtime effects |
| `STASH-BACKBONE-AUDIT.md` | deep audit of Cheap/Medium/Expensive world-stash tier wrappers and shared core pools, exact item/Chance/Weight/count structure, footprint, mod cross-check and control-surface verdicts |
| `CHEAP-STASH-CATEGORY-AUDIT.md` | Cheap-parent category deep dive: ammo/grenade/medical/food/coupons, count-vs-selection control verdicts, and production-only fan-out boundary |
| `CHEAP-STASH-AMMO-SCALING.md` | resolved 50–200% / 25 pp integer scaling policy for Cheap stash ammo Min/Max counts |
| `MEDIUM-STASH-AMMO-AUDIT.md` | Medium-stash ammo quantity source rows, production-only scope, scaling and runtime writer contract |
| `EXPENSIVE-STASH-AMMO-AUDIT.md` | Expensive-stash ammo quantity source rows, production-only scope, coupon compatibility and runtime writer contract |
| `NPC-CONSUMABLES-AUDIT.md` | deep audit of all `GeneralNPC_Consumables*` pools, exact branches/items, standard-human links, blast radius and control-safety verdicts |
| `NPC-CONSUMABLES-PRACTICAL-AUDIT.md` | practical repeatable-standard-human view: pool breadth, item sets, quantity-vs-frequency verdicts and the implemented 37-link TCD medical clone/redirect boundary |
| `NPC-WEAPONS-AMMO-AUDIT.md` | deep audit of direct standard-human weapons, attached ammo, and shared pistol-pool boundaries |
| `NPC-LOOSE-AMMO-AUDIT.md` | deep audit of standalone standard-human `Ammo`: one Prolog-specific direct row and one excluded shared Rare world-stash path |
| `NPC-LOOSE-AMMO-ENTRY-CATALOG.md` | exact 26-row standalone-ammo catalog reachable from the standard-human composition graph |
| `NPC-EQUIPMENT-ARTIFACTS-AUDIT.md` | deep audit of armor/helmet/detector/grenade/NVG surfaces |
| `NPC-JUNK-MISC-AUDIT.md` | proves standard-human `Junk` rows are `GuardQuestItem` interaction markers, not loot; includes all generator and contextual-action references |
| `ZOMBIE-LOADOUTS-AUDIT.md` | deep audit of all 114 zombie-domain generators; exact generic child boundaries, weapons/ammo, detector, grenade, consumable, shared-pool and Red Forest decisions |
| `GENERIC-ZOMBIE-GENERATOR-BASELINE.md` | corrected writer-facing baseline for all 93 generic zombie roots, with actual primary/pistol/weaponless source classes and the 91-row grenade boundary |
| `GENERIC-ZOMBIE-WEAPON-SOURCE-AUDIT.md` | corrected v0.2.16 weapon-source audit: 791 local primary rows, 85 pistol links to 12 shared pools / 71 pistol rows, and 2 weaponless roots |
| `ZOMBIE-ENTRY-CATALOG.md` | all 2,244 exact zombie-domain **local source rows**, including raw source anomalies and final target decision |
| `TRADER-STOCK-AUDIT.md` | deep audit of all trader stock generators, reusable pools, cross-context targets, rank progression, TradePrototype refresh wiring and control-safety decisions |
| `TRADER-ENTRY-CATALOG.md` | all 834 exact trader-domain raw/local source rows |
| `DESTRUCTIBLE-LOOT-AUDIT.md` | deep audit of production destructible physical wiring, food/drink, medical, ammo, grenade and SmartLoot boundaries |
| `DESTRUCTIBLE-ENTRY-CATALOG.md` | all 91 exact raw/local rows in the 40 defined `DestructibleStash*` generator roots |
| `DESTRUCTIBLE-WIRING-CATALOG.md` | every non-empty physical destructible ItemGenerator wiring row plus every destructible SmartLoot assignment |
| `MUTANT-LOOT-AUDIT.md` | strict audit of all mapped mutant generators and the 14 existing standard mutant-part controls |
| `MUTANT-ENTRY-CATALOG.md` | all exact local rows for the 19 mapped mutant-domain generators |
| `../SMARTLOOT-CATALOG.md` | one row per SmartLoot/Stash prototype with complete unique item set |
| `../SMARTLOOT-GAMEPLAY-GROUPS.md` | all 19 SmartLoot prototypes grouped into world-stash versus difficulty corpse-bonus domains with complete item sets and target policy |
| `SMARTLOOT-BRANCH-CATALOG.md` | exact SmartLoot rank/group/item table |
| `SMARTLOOT-WORLD-AUDIT.md` | deep audit of all 13 world-facing medical, food/drink, grenade and ammo SmartLoot prototypes, including physical consumer context and control-safety conclusions |
| `SMARTLOOT-WORLD-ENTRY-CATALOG.md` | complete exact group/item rows for the 13 audited world SmartLoot prototypes |
| `SMARTLOOT-WORLD-WIRING-CATALOG.md` | all 2,170 direct physical world-SmartLoot consumer edges with context and dual-layer status |
| `SMARTLOOT-CORPSE-BONUS-AUDIT.md` | end-to-end proof that `Extra NPC Corpse Loot` is an exact three-state Difficulty -> SmartLoot list control |
| `SMARTLOOT-CORPSE-BONUS-ENTRY-CATALOG.md` | every exact group/item row in the three selectable corpse-bonus SmartLoot packages |

- `stash-safe-expansion-deferred-actors.csv` — 40 full actor IDs and original generator/clue/SmartLoot source lists held back for future actor-by-actor implementation checks (never an active allowlist).

- `../../src/TrueCustomDifficulty/Data/dlc1-stash-eligible-actor-manifest.csv` — 112 exact DLC1 no-clue regular SmartLoot actor targets; only these may receive new DLC generic-source redirects.
- `../../src/TrueCustomDifficulty/Data/cheap-no-fresh-stash-item-entries.csv` — 35 original source rows for the DLC1-only `TCD_GamePass_Stash_ItemGenerator_CheapNoFresh` clone.

## Machine-readable files

- `../../tools/regular-stash-source-inventory.json` — versioned categorized source register (no in-game modification) verified by `../../tools/audit_regular_stash_source_inventory.py`.

| File | Purpose |
| --- | --- |
| `generator-local-source-inventory.csv` | **authoritative patch-target layer:** one row per physical generator root as locally authored in the runtime CFG |
| `generator-local-source-category-blocks.csv` | **authoritative patch-target layer:** all 14,993 locally authored category blocks before inheritance expansion |
| `generator-local-source-item-entries.csv` | **authoritative patch-target layer:** all 30,991 exact locally authored item/nested-generator rows and raw fields |
| `generator-map-summary.json` | compact aggregate counts for sanity checks and future diffs |
| `generator-group-summary.json` | aggregate gameplay-group / candidate / review / exclude counts |
| `generator-gameplay-groups.csv` | one row per generator with gameplay group, propagated impact contexts, graph role, full reachable items and target policy |
| `generator-consumer-contexts.csv` | every external consumer edge enriched with resolved gameplay context and consumer details |
| `generator-target-allowlist.csv` | strict single-domain candidate boundary set; does not imply every row should be patched |
| `generator-preferred-base-candidates.csv` | smaller reusable/base candidate set used to identify minimal edit points |
| `generic-zombie-generator-baseline.csv` | one row per generic zombie production root with local field coverage and control status |
| `generic-zombie-weapon-coverage-gaps.csv` | 12 generic zombie roots lacking child-local WeaponPrimary rows |
| `generic-zombie-parent-weapon-source-rows.csv` | 52 shared-parent weapon rows retained only as source material for future child-local overrides |
| `generic-zombie-grenade-targets.csv` | exact 91 existing generic-zombie grenade Chance targets |
| `ZOMBIE-GRENADE-FREQUENCY.md` | v0.2.17 implemented Zombie Grenade Frequency semantics and writer boundary |
| `zombie-grenade-frequency-summary.json` | machine-readable v0.2.17 target/scaling proof |
| `generic-zombie-baseline-summary.json` | compact generic-zombie baseline counts and decisions |
| `generator-inventory.csv` | one row per base/DLC1 Item Generator prototype; parent metadata plus local/effective category/item/ref counts |
| `generator-inheritance-edges.csv` | `refurl/refkey` child -> parent relationships |
| `generator-composition-edges.csv` | generator -> nested generator relationships through `ItemGeneratorPrototypeSID` |
| `generator-consumer-edges.csv` | external references from objects, trade, quests/actions, SpawnActor settings, `CorpseStashSID`, and the core default template |
| `generator-category-blocks.csv` | every effective Item Generator category block, including `Diff`, `PlayerRank`, and category metadata |
| `generator-item-entries.csv` | every effective direct item/nested-generator row and its exact fields |
| `stash-backbone-family-summary.csv` | Cheap/Medium/Expensive physical footprint and shared-parent summary |
| `stash-backbone-rank-wrapper-usage.csv` | all 12 tier/rank wrapper footprints |
| `stash-backbone-pool-blocks.csv` | exact local category blocks for the three core stash parents |
| `stash-backbone-pool-entries.csv` | exact local item rows for the three core stash parents |
| `stash-backbone-item-matrix.csv` | item membership and gate/mechanism matrix across stash tiers |
| `stash-backbone-control-surfaces.csv` | keep/review/reject register for potential stash controls |
| `improving-stashes-backbone-crosscheck.csv` | overlap between supplied Improving Stashes Loot mod and vanilla stash backbone roots |
| `stash-backbone-summary.json` | compact stash-backbone audit conclusions |
| `cheap-stash-category-entries.csv` | all 52 Cheap parent rows classified into ammo/grenade/medical/food-drink/coupons |
| `cheap-stash-category-summary.csv` | per-category Chance/Weight/count structure |
| `cheap-stash-control-surfaces.csv` | Cheap-stash control keep/defer/reject decisions |
| `cheap-stash-production-fanout.csv` | all 342 incoming Cheap-parent wrappers classified as production candidate, unresolved review, or scripted/quest exclusion |
| `cheap-stash-category-summary.json` | compact Cheap category/scope audit facts |
| `cheap-stash-ammo-scaling-table.csv` | exact integer Min/Max output for every Cheap ammo family at each supported quantity step |
| `npc-consumable-practical-pools.csv` | six active shared consumable pools with direct/propagated standard-human breadth and future TCD clone SID |
| `npc-consumable-practical-category-summary.csv` | category-level row/count/chance summary showing Medical as the only strong vanilla difficulty-sensitive group |
| `npc-consumable-standard-redirect-manifest.csv` | exact 37 active standard-human source links reserved for a future scope-isolated TCD consumable-pool redirect |
| `npc-consumable-control-surfaces-v0.2.13.csv` | practical keep/defer/no-UI decision table for NPC consumable control concepts |
| `npc-consumable-practical-summary.json` | compact v0.2.13 standard-human consumable audit sanity counts |
| `npc-loose-ammo-direct-entries.csv` | authoritative direct standard-human standalone-ammo row with consumer and final policy |
| `npc-loose-ammo-standard-paths.csv` | the only two standard-human routes to standalone `Ammo` rows |
| `npc-loose-ammo-shared-pool-entries.csv` | all 25 exact `Ammo` rows in shared `GamePass_Stash_ItemGenerator_Rare` |
| `npc-loose-ammo-shared-dependency-links.csv` | all incoming composition/inheritance fan-out links for the shared Rare pool |
| `npc-loose-ammo-shared-blast-radius.csv` | all resolved direct consumer edges affected by editing the shared Rare pool |
| `npc-loose-ammo-domain-summary.json` | compact standalone-ammo counts and no-generic-control verdict |
| `npc-consumable-pools.csv` | one row per `GeneralNPC_Consumables*` pool with usage/blast-radius counts and verdict |
| `npc-consumable-branches.csv` | every NPC-consumable branch item row with difficulty/rank gates, `Chance`, and count range |
| `npc-consumable-standard-consumers.csv` | every standard-human loadout link to a consumable pool, including edge `Chance/Weight` and concrete consumer |
| `npc-consumable-item-matrix.csv` | item-to-pool membership matrix for the 10 observed consumable item SIDs |
| `npc-junk-standard-surface.csv` | every standard-human-relevant `GuardQuestItem` generator row with impact scope and exact source fields |
| `npc-junk-guardquest-generator-rows.csv` | every effective base/DLC1 generator row containing `GuardQuestItem` |
| `guardquest-contextual-action-preconditions.csv` | all SpawnActor contextual actions gated by `GuardQuestItem` inventory presence |
| `spawn-loot-actors.csv` | loot-relevant SpawnActor rows showing physical spawn type plus Item Generator, SmartLoot, and corpse-generator wiring |
| `smartloot-inventory.csv` | all `StashPrototypes.cfg` SmartLoot prototypes |
| `smartloot-gameplay-groups.csv` | gameplay grouping, consumer summary, complete possible items and target policy for every SmartLoot prototype |
| `smartloot-inheritance-edges.csv` | SmartLoot inheritance relationships |
| `smartloot-rank-blocks.csv` | SmartLoot rank blocks |
| `smartloot-groups.csv` | SmartLoot parameter sets and group-level spawn/ammo/count fields |
| `smartloot-items.csv` | SmartLoot item rows with `Chance`, `Weight`, and count fields |
| `smartloot-consumer-edges.csv` | SpawnActor `StashPrototypeSID` plus Difficulty `CorpseSmartLoot` consumers |
| `smartloot-world-prototype-inventory.csv` | the 13 audited world prototypes with exact row counts, production fan-out, dual-layer counts, endpoint signatures and decisions |
| `smartloot-world-group-rows.csv` | all 422 exact group rows in the audited world SmartLoot domain |
| `smartloot-world-item-rows.csv` | all 991 exact item rows in the audited world SmartLoot domain |
| `smartloot-world-consumer-edges.csv` | all 2,170 direct physical consumer edges enriched with map context and dual ItemGenerator status |
| `smartloot-world-domain-summary.json` | compact world SmartLoot audit counts and safety conclusions |
| `difficulty-corpse-smartloot-edges.csv` | the three explicit GeneralNPC difficulty SmartLoot links |
| `smartloot-corpse-bonus-prototype-inventory.csv` | package-level summary for the three selectable corpse-bonus SmartLoot prototypes |
| `smartloot-corpse-bonus-group-rows.csv` | all 39 exact group rows across the three corpse-bonus packages |
| `smartloot-corpse-bonus-item-rows.csv` | all 60 exact item rows across the three corpse-bonus packages |
| `smartloot-corpse-bonus-consumer-edges.csv` | all three mapped external consumers of the corpse-bonus packages |
| `smartloot-corpse-bonus-control-states.csv` | exact choice-slider state -> source list -> writer-action mapping |
| `smartloot-corpse-bonus-domain-summary.json` | compact counts and 1:1-control verdict for the difficulty corpse-bonus domain |
| `zombie-loadout-generators.csv` | all 114 zombie-domain generators with family, exact local counts, consumer scope and refined target policy |
| `zombie-local-source-entries.csv` | all 2,244 exact local source rows belonging to zombie-domain generators |
| `zombie-generic-local-entries.csv` | the 1,882 exact local rows in the 93 generic base/CNPP zombie child loadouts |
| `zombie-generic-nested-links.csv` | all 355 local nested-generator references in generic zombie children, including mixed CNPP item+generator entries |
| `zombie-source-anomalies.csv` | 231 exact raw zombie rows flagged for mixed item/generator or unexpected weapon-like fields; preserved as source facts |
| `zombie-domain-summary.csv` | compact zombie family/category/decision counts |
| `trader-generator-inventory.csv` | all 96 trader-domain generator roots with role, consumers, local categories, reachable items and target policy |
| `trader-local-source-category-blocks.csv` | all 220 exact local trader category blocks |
| `trader-local-source-entries.csv` | all 834 exact local trader item/nested-generator rows |
| `trader-trade-prototype-links.csv` | all 83 mapped TradePrototype -> trader-generator links with refresh and buy/sell metadata |
| `trader-nested-links.csv` | every trader-local nested-generator edge with resolved target impact |
| `trader-nested-targets.csv` | target-level blast-radius summary for trader nested dependencies |
| `trader-core-tier-pool-entries.csv` | exact rows of the reusable trader-only T1–T4 gun/ammo/attachment and Soviet-ammo pools |
| `trader-domain-summary.json` | compact trader topology / gating / count summary for sanity checks and future diffs |
| `destructible-spawn-inventory.csv` | all physical destructible actors with authored map context and loot-layer flags |
| `destructible-itemgen-wiring.csv` | every non-empty physical destructible rank -> ItemGenerator link with context/family/policy |
| `destructible-production-wiring.csv` | strict `WorldMap_WP` subset of destructible ItemGenerator wiring |
| `destructible-smartloot-wiring.csv` | all destructible SmartLoot assignments kept as a separate loot layer |
| `destructible-generator-inventory.csv` | all defined `DestructibleStash*` roots plus unresolved/cross-system physical exceptions with blast-radius decision |
| `destructible-local-source-entries.csv` | all 91 authoritative raw/local `DestructibleStash*` rows |
| `destructible-domain-summary.json` | compact physical/generator/destructible-layer audit counts |
| `mutant-generator-inventory.csv` | all 19 mutant-domain roots with consumers, graph edges, fields, control link and decision |
| `mutant-local-source-entries.csv` | all 28 exact raw/local rows authored under the mapped mutant-domain roots |
| `mutant-consumer-edges.csv` | all direct runtime consumer edges for the mapped mutant generators |
| `mutant-control-audit.csv` | strict raw/local + writer-baseline check for the 14 visible standard mutant-part controls |
| `mutant-domain-summary.json` | compact mutant-domain counts and audit conclusions |

## Hard candidate exclusions

- `CONTROL-CANDIDATE-EXCLUSIONS.md` — policy and human-readable rationale for test/debug-only, Prologue-specific, and one-time/story-scripted targets.
- `control-candidate-exclusions.csv` — exhaustive SID-level exclusion register with one reason per generator.
- `control-candidate-exclusions-summary.json` — compact counts and allowlist-removal summary.

These files supersede any older structural-candidate classification for matching SIDs.

## Reading rules

- `generator-inheritance-edges.csv` and `generator-composition-edges.csv` are different graphs.
- `generator-local-source-*.csv` is the **authoritative layer for patch targets** because it records only fields physically authored under that CFG root.
- `generator-item-entries.csv` and `generator-category-blocks.csv` are **effective/inheritance-resolved analysis layers**; inherited fields may appear on a child even when they are not locally authored there. Use them for reachability/blast-radius analysis, not as patch coordinates.
- An inherited/effective row is not automatically a locally authored child override.
- `Diff` and `PlayerRank` are independent gates.
- `Weight` and lowercase `weight` are preserved separately because they are distinct source spellings.
- `AmmoMaxCount` and `AmmoMaxcount` are preserved separately for the same reason.
- `Empty`/`empty` sentinel values are kept where relevant instead of being silently resolved into real generators.
- unresolved consumer rows are deliberately retained. They identify limits of the supplied CFG graph; they are not automatically errors.

## Scope

This dataset maps CFG relationships. It does **not** claim exact engine probability semantics for `Chance`/`Weight`, nor does it turn aggregate generator groups into supported UI controls.

## Domain deep audits

- `NPC-CONSUMABLES-AUDIT.md` — standard-human consumable pool boundaries and shared-pool blast radius.
- `NPC-WEAPONS-AMMO-AUDIT.md` — standard-human direct weapon rows, attached-ammo fields, shared nested pistol pools and exact target policy.
- `NPC-LOOSE-AMMO-AUDIT.md` / `NPC-LOOSE-AMMO-ENTRY-CATALOG.md` — complete standalone-ammo audit; proves the only direct row is Prolog-specific and the only broader path uses a shared world-stash pool.
## Deep-audit domain files

- `NPC-CONSUMABLES-AUDIT.md` — standard-human consumable pools, branches and blast radius.
- `NPC-WEAPONS-AMMO-AUDIT.md` — direct standard-human weapon rows, attached ammo and shared pistol pools.
- `NPC-EQUIPMENT-ARTIFACTS-AUDIT.md` — armor/helmet/detector/grenade/NVG mapping, including the important fact that the standard-NPC CFG `Artifact` rows are grenades.

Equipment audit machine tables:

- `npc-equipment-direct-entries.csv`
- `npc-equipment-nested-links.csv`
- `npc-equipment-nested-pools.csv`
- `npc-equipment-nested-pool-entries.csv`
- `npc-equipment-item-matrix.csv`


- `NPC-JUNK-MISC-AUDIT.md` — standard-human `Junk` / `GuardQuestItem` system-marker audit.

- `ZOMBIE-LOADOUTS-AUDIT.md` / `ZOMBIE-ENTRY-CATALOG.md` — full zombie-domain deep audit and raw-local row catalog.

- `TRADER-STOCK-AUDIT.md` / `TRADER-ENTRY-CATALOG.md` — full trader-stock deep audit and exact raw/local source catalog.

- `DESTRUCTIBLE-LOOT-AUDIT.md` / `DESTRUCTIBLE-ENTRY-CATALOG.md` / `DESTRUCTIBLE-WIRING-CATALOG.md` — full world-destructible deep audit, raw/local generator rows, physical wiring and SmartLoot exceptions.

- `MUTANT-LOOT-AUDIT.md` / `MUTANT-ENTRY-CATALOG.md` — full mutant-domain audit, exact species-part controls, special-root exclusions and raw/local entries.

- `CORPSE-OVERRIDE-AUDIT.md` / `CORPSE-OVERRIDE-ENTRY-CATALOG.md` / `CORPSE-OVERRIDE-WIRING-CATALOG.md` — complete `CorpseStashSID` DeadBody override audit, physical wiring, exact root-local rows, propagated dependency graph and cross-context safety decisions.

- `SMARTLOOT-WORLD-AUDIT.md` / `SMARTLOOT-WORLD-ENTRY-CATALOG.md` / `SMARTLOOT-WORLD-WIRING-CATALOG.md` — complete world SmartLoot deep audit for medical, food/drink, grenade and ammo, including exact source rows and all physical consumers.

## Control design decision layer

- `CONTROL-CANDIDATE-EXCLUSIONS.md` / `control-candidate-exclusions.csv` — hard-excluded test/debug, Prologue and one-time/scripted SIDs.
- `CONTROL-CANDIDATE-TRIAGE.md` / `control-candidate-triage.csv` — production-only control concepts classified as keep, shortlist, defer, audit-only or no-UI.

## v0.2.15 production weapon audit

- `NPC-WEAPONS-AMMO-PRACTICAL-AUDIT.md`
- `npc-weapon-production-loadouts.csv`
- `npc-weapon-production-entries.csv`
- `npc-weapon-production-role-summary.csv`
- `npc-weapon-production-weapon-frequency.csv`
- `npc-weapon-production-exclusions.csv`
- `npc-weapon-production-control-surfaces.csv`
- `npc-weapon-production-summary.json`
