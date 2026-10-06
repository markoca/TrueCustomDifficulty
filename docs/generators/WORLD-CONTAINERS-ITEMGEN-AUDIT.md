# World Containers / ItemGenerator Stashes — Deep Audit

This document audits the physical world-container ItemGenerator layer separately from SmartLoot. Runtime source is the supplied STALKER 2 2.0.5 CFG dump. Writer behavior is unchanged.

## Domain boundary

A physical `ESpawnType::ItemContainer` can carry `ItemGeneratorSettings`, `StashPrototypeSID` (SmartLoot), or both. These are independent loot layers and must not be collapsed into one "stash abundance" control.

```text
ItemContainer SpawnActor
  ├─ ItemGeneratorSettings -> ItemGeneratorPrototypes
  └─ StashPrototypeSID     -> StashPrototypes / SmartLoot
```

## Counts

| Metric | Count |
|---|---:|
| Mapped World Container / Stash generator roots | 996 |
| Per-rank ItemGenerator links to mapped world-container generators | 1596 |
| Physical containers using those generators | 1103 |
| Distinct directly referenced generator SIDs | 891 |
| Main-world candidate per-rank links | 1458 |
| Main-world candidate physical containers | 1005 |
| Distinct main-world candidate generator SIDs | 814 |
| Main-world candidate containers with ItemGenerator + SmartLoot | 770 |
| Main-world candidate containers with ItemGenerator only | 235 |
| Links on quest/scripted logic levels | 47 |
| Links on test/debug/demo maps | 55 |
| Mapped generator roots with no direct physical-container link | 105 |


## Production rule

For generic world-stash tuning, the strict physical surface is **main `WorldMap_WP` only**, plus generator policy `CANDIDATE — container/stash-only scope`. Quest/scripted logic levels and test/debug/demo maps are excluded even when they use `ESpawnType::ItemContainer`.

## Core GamePass pools

The three important shared parents are not safe global edit targets: `Cheap` reaches destructibles and quest content; `Common_Var1` reaches quest content; `Rare` reaches standard-human NPC content. A generic world-stash control must therefore target explicit world wrapper/instance rows or use clone/rewire.

| Pool | Impact contexts | Incoming composition | Incoming inheritance | Local rows | Chance | Weight | Diff-gated | Rank-gated | Decision |
|---|---|---:|---:|---:|---:|---:|---:|---:|---|
| `CNPPGamePass_Stash_ItemGenerator_Cheap` | — | 0 | 0 | 15 | 10 | 5 | 7 | 0 | REVIEW — no resolved runtime consumer |
| `CNPPGamePass_Stash_ItemGenerator_Rare` | item_container_world | 0 | 0 | 33 | 10 | 23 | 3 | 25 | CANDIDATE — container/stash-only scope |
| `GamePass_Stash_ItemGenerator_Cheap` | destructible_world, item_container_world, quest_scripted | 342 | 4 | 52 | 14 | 38 | 46 | 23 | EXCLUDE FROM GENERIC TUNING — scripted/quest |
| `GamePass_Stash_ItemGenerator_CheapNoFresh` | item_container_world | 0 | 0 | 35 | 10 | 25 | 29 | 14 | CANDIDATE — container/stash-only scope |
| `GamePass_Stash_ItemGenerator_Common_Var1` | item_container_world, quest_scripted | 240 | 4 | 39 | 14 | 25 | 14 | 28 | EXCLUDE FROM GENERIC TUNING — scripted/quest |
| `GamePass_Stash_ItemGenerator_Common_Var2` | item_container_world | 1 | 0 | 9 | 5 | 4 | 0 | 0 | CANDIDATE — container/stash-only scope |
| `GamePass_Stash_ItemGenerator_Rare` | human_npc_standard, item_container_world | 164 | 4 | 42 | 19 | 23 | 11 | 33 | REVIEW — shared across gameplay contexts |
| `Gamepass_Stash_ItemGenerator_Common` | item_container_world | 0 | 0 | 2 | 0 | 2 | 0 | 0 | CANDIDATE — container/stash-only scope |

## Main-world physical wiring

The strict main-world surface contains **1005 physical containers** and **1458 rank-specific generator links**. **770** of those containers also carry SmartLoot, so ItemGenerator edits affect only part of their final loot.

The exact actor/rank/generator/SmartLoot mapping is in `world-container-main-world-actors.csv` and the full row-level wiring is in `WORLD-CONTAINER-WIRING-CATALOG.md`.

## Control-safety conclusions

| Proposed concept | Verdict | Reason |
|---|---|---|
| Global edit of `GamePass_Stash_ItemGenerator_Cheap` | **NO** | Shared with world containers, destructibles and quest/scripted consumers. |
| Global edit of `GamePass_Stash_ItemGenerator_Common_Var1` | **NO** | Shared with world and quest/scripted consumers. |
| Global edit of `GamePass_Stash_ItemGenerator_Rare` | **NO** | Shared with world containers and standard-human NPC path. |
| Explicit main-world wrapper/instance allowlist | **STRUCTURALLY SAFE CANDIDATE** | Physical consumer scope is known and authored quest/test surfaces can be excluded. |
| One `stash abundance` slider across ItemGenerator + SmartLoot | **NO** | They are separate systems with different fields and selection semantics. |
| ItemGenerator `Weight` as percentage | **NO / runtime semantics required** | Weight is pool selection weight, not proven probability. |
| Explicit `Chance` rows on main-world-only wrappers | **RESEARCH CANDIDATE** | Structurally targetable, but final gameplay meaning still needs per-family validation. |
| Count-range scaling on explicit main-world-only rows | **RESEARCH CANDIDATE** | Exact rows can be targeted, but item/category policy must be specified. |

## Files

- `world-container-itemgen-wiring.csv` — all mapped physical container → rank → ItemGenerator links with map context and SmartLoot co-presence.
- `world-container-main-world-actors.csv` — strict main-world actor allowlist.
- `world-container-generator-classification.csv` — all 996 mapped generator roots with audit decision.
- `world-stash-core-pools.csv` — core reusable stash pool summary.
- `world-stash-core-pool-entries.csv` — exact raw/local rows for core GamePass stash pools.
- `WORLD-CONTAINER-WIRING-CATALOG.md` — human-readable full wiring table.
- `WORLD-STASH-CORE-POOL-CATALOG.md` — human-readable raw row catalog for core pools.
