## 2026-10-18 — CURRENT: SHARED BONUS CHANCE EDITOR

`Extra Stash Gear` has **six** live controls: opt-in toggle, 0–100% `Eligible Stashes` actor coverage, the three independent `WEAPONS / ARMOR / ARTIFACTS` build-time relative mix weights (0–3× in 0.1× steps), and **one** `BONUS CHANCE · ALL ITEMS` 0–100% native item Chance slider in exact 1% steps. It is placed below the mixer, no per-category chance controls. The default is **50%** and preserves the previous fixed `Chance = 0.5`, including loading old presets without this new key.

The CFG writer converts the selected integer percent to an invariant decimal `0..1` and assigns the **same** `ItemGenerator[0].PossibleItems[0].Chance` to each private additional candidate, irrespective of whether the stable mix selected a rank-eligible **weapon, armor or ordinary artifact**. Examples: `0% -> Chance = 0`, `1% -> 0.01`, `37% -> 0.37`, `50% -> 0.5`, `100% -> 1`. `MinCount = MaxCount = 1` remains unchanged. The weight mixer determines only which candidate class is authored into an actor-specific generator; `Eligible Stashes` determines only which ordinary physical actor IDs get one. Neither of those values modifies the native Chance. **A native CFG Chance is not an empirically validated whole-physical-stash drop rate**; native category/game generation, unopened stash acceptance and higher-level rules require in-game validation. 100% is a valid deliberately user-selected source Chance, not a guarantee about unrelated native generator stages.

All 800 approved base plus 108 eligible DLC1 actors, quest/Weird/unique exclusions, existing native rank branches and protected original source paths are unchanged. The master OFF disables all dependent sliders. Section remains compact with max 630 DIP and Stamina Usage aligned rails, now **444 DIP** tall. Catalog contains 280 parameters; audits check family parity and generated CFG default plus class-forced 0/37/100% output. Source-level changes and updated test code do not assert a successful local .NET/Wine/game verification.

---

## 2026-10-18 — CURRENT: THREE-WAY EXTRA STASH BONUS MIXER

`Extra Stash Gear` now has five compact controls: one master toggle, existing exact 0–100% `Eligible Stashes` **build-time actor selection**, and three **independent 0–3x, 0.1x** distribution Weight rails for `Weapons`, `Armor` and `Artifacts`. Default mix is 1x / 1x / 1x. A 0x weight excludes that category from the new bonus generator; all zero values create no bonus (the separate opt-in 13 authored weapon removal still applies). These are **design weights for actor/rank category assignment at build time**, NOT native `PossibleItems.Weight` fields or promised item drop percentages. They are compared per selected physical ActorSID and native rank branch using an independent deterministic SHA256 mixer, then a rank-eligible SID is chosen within the class. Across selected actors each eligible 142-item SID can be selected, but none is guaranteed.

Each selected physical actor/rank receives **at most one new bonus item candidate**: a source-validated regular weapon, one of 39 normal suits, or one of 69 standard artifacts. Six Weird/Arch artifacts plus quest, fake, prolog, NPC/unique/special items remain excluded. Its private real CFG `PossibleItems[0].Chance = 0.5` is an **explicitly authored chance** (not measured whole-physical-stash odds); `MinCount = MaxCount = 1`. This means a selected actor has a possible bonus, not a guaranteed physical item. The final engine treatment of the `Chance` field still requires unopened-stash in-game confirmation; no RNG or probability semantics are fabricated in the visual mixer. No new shared pools, no quest stash edits. Eligible actors 800 base + 108 DLC and original source removal behavior unchanged.

Implementation: `CfgPatchWriterService.StashGearCoverage.cs`; parameter count 279, StashGear aggregate and parameter-family mapping; current locked UI consists of compact toggle, Stamina Usage-style coverage slider and three compact aligned weight rails, section height 379 DIP. QA: `tools/audit_stash_gear_coverage.py`, `tools/audit_stashes_mixer_ui.py`, and generated CFG contract `VerifyStashGearCoverage` including category-only 0x exclusions. This is source-level evidence; a local .NET build, Wine UI inspection and actual game RNG remain to be run.

---

## 2026-10-18 — CURRENT: EXPANDED STASH BONUS POOL

The authoritative runtime `gear-progression-ranks.csv` contains **142 unique regular items**: 34 weapons, 39 ordinary player-facing armor suits, 69 standard anomaly artifacts. T1–T4 map to native `Newbie/Experienced/Veteran/Master` player-rank branches. The UI remains the same two compact controls; no chance sliders or additional controls are introduced.

| Tier / player rank | Weapons | Suits | Standard artifacts |
|---|---:|---:|---:|
| T1 Newbie | 8 | 7 | 39 common |
| T2 Experienced | 10 | 10 | 17 uncommon |
| T3 Veteran | 10 | 13 | 9 rare |
| T4 Master | 6 | 9 | 4 epic |
| **Total** | **34** | **39** | **69** |

The existing four original suits retain their direct player-rank source anchors. Another 35 normal, spawn-referenced, non-NPC suits have explicitly **curated stash tiers**, not claimed universal vanilla armor ranks. The standard artifact pool uses published item SID/rarity mapping and the gameplay progression policy Common→T1, Uncommon→T2, Rare→T3, Epic→T4; this does not assert native rarity-derived drop chances. This is a **design mapping**, not a newly discovered global CFG progression contract.

**Excluded:** all six Weird/Arch `AArtifactWeird*` items, fake/prolog/quest, DLC story and special artifacts; NPC-only armor, helmets, preinstalled special suits, and Gauss/RPG/unique weapon variants. Standard artifact SIDs are `CArtifact*`, `EArtifact*`, `FArtifact*`, `GArtifact*` and independently checked against the authoritative `spawnable-item-sids.csv` inventory.

For each eligible actor's native rank, select one weapon, then optionally **one suit OR one ordinary artifact**, never both, from all tier ≤ rank options. Selection uses deterministic actor-specific hashes. Item `Chance=1` belongs to the private generator; it is **not a guaranteed physical drop rate**. Max two authored extra item candidates per branch. Artifacts receive native `Artifact` category without equipment durability/ammo properties. Exact engine loot-roll behavior remains subject to unopened-stash testing in game.

The source manifest now carries `STASH_BONUS_POOL` usage, the 142-entry loader validates counts and special exclusions, and the production CFG output contract checks tier/class boundaries. This heading supersedes historical 34+4-only passages below.

---

## 2026-10-18 — EXTRA STASH GEAR: ONE ACTOR-COVERAGE PERCENT SLIDER (CURRENT)

**Player-facing layout:** the **EXTRA STASH GEAR** family now has **exactly two live controls**: one compact `ENABLE EXTRA GEAR` checkbox (`loot.stashGearEnabled`, off by default), and one compact, disabled-while-off `ELIGIBLE STASHES` slider (`loot.stashGearCoveragePercent`, **0–100% in exact 1% steps**, default 0%). One shared narrow width (max 630 DIP), one slider rail with left 0% / right 100%, source-like Stamina Usage label/value placement; family height 184 DIP. Remove all six BASIC/STANDARD/RARE weapons/armor OFF/RARE/MOD/FREQ controls from the catalog, VM aggregate, renderer, emitted CFG writer and audits. No new faux chance, no full-width controls, no Vanilla/Base value prominence or verbose in-control copy. Old six-ID preset inputs are no longer supported controls.

**Actual denominator:** the existing 803 approved base-game `REGULAR_SMARTLOOT` actor IDs and 112 DLC1 manifest-approved actor IDs undergo the additional **native rank/generator validation**. Exclude two empty-generator base actors, four empty-generator DLC actors, and **the potentially unique `Gun_Trophy_AR` source actor**: exactly **800 base** and **108 DLC1** physical stash actors are eligible for *extra gear*. Quest, special, clue-bearing, unknown and source-protected actors never enter the percentage. The denominator is 800 for base-only installs; 800+108 for installed DLC1. **For each content scope independently**, choose `(eligibleCount × percentage + 50) / 100` rounded down in integer arithmetic, after ordering actors by a SHA256 hash of the stable, exact ActorSID. This yields an exact, reproducible, monotonic **physical actor coverage fraction at build time**, not an in-game drop Chance. Examples: 0%=0/0, 1%=8 base+1 DLC, 50%=400+54, 100%=800+108. Installing DLC does not reshuffle the previously selected base physical actors.

**Gear and count:** each selected actor receives a **private additional ItemGenerator prototype** referenced in a newly appended native `ItemGeneratorSettings[*].ItemGenerators` slot. This does **not** replace existing native generators or globally modify a common gear pool. Its exact *native rank branch* is reused to ensure source-audited `Newbie/Experienced/Veteran/Master` player progression rather than guessed region levels: one deterministic weapon selected from all normal tier ≤ branch rank and optionally one armor suit selected from tier ≤ rank, each `MinCount=MaxCount=1`. The 34 normal weapon SIDs and four native player-gear armor anchor SIDs are taken from `gear-progression-ranks.csv`; Gauss, RPG, quest, special, unique and item definitions without explicit tiers are excluded. For two selectable categories, the game engine's final generator behavior must still be verified in-game; never claim guaranteed real drops or an unproven physical stash hard cap across multiple generators. PlayerRank selection is per native actor rank or native `Newbie/Experienced/Veteran/Master` branch, NOT the region.

**Original weapon removal remains independent of coverage**: checkbox ON still removes only the previously audited 13 regular authored original weapon `PossibleItems` entries; even 0% coverage does not undo this previous explicit removal choice. Checkbox OFF removes none and adds no gear. The other 26 quest/unique/protected/deferred original source entries are never removed, and the World Weapon Shuffle conflict check remains enforced before patch generation. Source-wide 39-entry catalog remains authoritative separately.

**Implementation/source parity:** `src/TrueCustomDifficulty/Data/stash-gear-actor-ranks.csv` freezes 915 class-approved physical ActorSIDs and their native rank branch/slot counts; `src/TrueCustomDifficulty/Data/gear-progression-ranks.csv` contains 38 candidate items. The writer validates every source actor against the existing smartloot base/DLC manifests and all 106 protected actors, then selects the 800/108 eligible subset and emits actor-local extra generators. `tools/audit_stash_gear_coverage.py` tests all 101 percentage settings and selected actor counts; `tests/LootSpecialGeneratorsContract/StashGearCoverage.cs` checks the production writer's actual BPatch structure including 0%, 1%, 50%, 100%, base and DLC. This is source and static verification, **not claimed successful local .NET/Wine/in-game acceptance**. Supersedes all earlier six-control Extra Gear designs below.

# Player-facing gear progression rank catalog

This is a **reference-only**, source-grounded crosswalk of player progression ranks and equipment tiers. It is **not** a region-to-rank mapping, does not claim each map region has a single rank, and changes no loot/shuffle behavior.

| Tier | Rank | Weapons | Armor anchors |
|---|---|---:|---:|
| T1 | Newbie | 8 | 1 |
| T2 | Experienced | 10 | 1 |
| T3 | Veteran | 10 | 1 |
| T4 | Master | 6 | 1 |

The **34** regular weapon SIDs use the existing documented `Trader_T1..T4_Guns_ItemGenerator` evidence in `WORLD-GEAR-WEAPON-TIER-MATRIX.md`. The **4** armor SIDs are explicit `NewbieGearPC`, `ExperiencedGearPC`, `VeteranGearPC`, and `MasterGearPC` anchors from `WORLD-GEAR-ARMOR-TIER-MATRIX.md`. The CSV records source and confidence for every entry.

**Excluded / review:** `GunFora_ST` lacks explicit tier; Gauss, RPG and unique variants are special; other armor models have no validated global tier and must not be assigned based solely on price, NPC faction rank, or trader presence. The existing `WORLD-GEAR-ARMOR-PLAYER-FACING-AUDIT.md` explains this limitation.

**Regional progression:** no verified `region → PlayerRank` contract has been established from the supplied game CFG. Region difficulty, local NPC rank and item tier must remain separate until actual source evidence links them. This CSV must not be consumed as an automatic region-based loot distribution table.

Source file: `src/TrueCustomDifficulty/Data/gear-progression-ranks.csv`. This is an audit artifact only; no UI, runtime or generator changes.
