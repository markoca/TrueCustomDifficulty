## 2026-10-18 — EXTRA STASH GEAR: ONE ACTOR-COVERAGE PERCENT SLIDER (CURRENT)

**Player-facing layout:** the **EXTRA STASH GEAR** family now has **exactly two live controls**: one compact `ENABLE EXTRA GEAR` checkbox (`loot.stashGearEnabled`, off by default), and one compact, disabled-while-off `ELIGIBLE STASHES` slider (`loot.stashGearCoveragePercent`, **0–100% in exact 1% steps**, default 0%). One shared narrow width (max 630 DIP), one slider rail with left 0% / right 100%, source-like Stamina Usage label/value placement; family height 184 DIP. Remove all six BASIC/STANDARD/RARE weapons/armor OFF/RARE/MOD/FREQ controls from the catalog, VM aggregate, renderer, emitted CFG writer and audits. No new faux chance, no full-width controls, no Vanilla/Base value prominence or verbose in-control copy. Old six-ID preset inputs are no longer supported controls.

**Actual denominator:** the existing 803 approved base-game `REGULAR_SMARTLOOT` actor IDs and 112 DLC1 manifest-approved actor IDs undergo the additional **native rank/generator validation**. Exclude two empty-generator base actors, four empty-generator DLC actors, and **the potentially unique `Gun_Trophy_AR` source actor**: exactly **800 base** and **108 DLC1** physical stash actors are eligible for *extra gear*. Quest, special, clue-bearing, unknown and source-protected actors never enter the percentage. The denominator is 800 for base-only installs; 800+108 for installed DLC1. **For each content scope independently**, choose `(eligibleCount × percentage + 50) / 100` rounded down in integer arithmetic, after ordering actors by a SHA256 hash of the stable, exact ActorSID. This yields an exact, reproducible, monotonic **physical actor coverage fraction at build time**, not an in-game drop Chance. Examples: 0%=0/0, 1%=8 base+1 DLC, 50%=400+54, 100%=800+108. Installing DLC does not reshuffle the previously selected base physical actors.

**Gear and count:** each selected actor receives a **private additional ItemGenerator prototype** referenced in a newly appended native `ItemGeneratorSettings[*].ItemGenerators` slot. This does **not** replace existing native generators or globally modify a common gear pool. Its exact *native rank branch* is reused to ensure source-audited `Newbie/Experienced/Veteran/Master` player progression rather than guessed region levels: one deterministic weapon selected from all normal tier ≤ branch rank and optionally one armor suit selected from tier ≤ rank, each `MinCount=MaxCount=1`. The 34 normal weapon SIDs and four native player-gear armor anchor SIDs are taken from `gear-progression-ranks.csv`; Gauss, RPG, quest, special, unique and item definitions without explicit tiers are excluded. For two selectable categories, the game engine's final generator behavior must still be verified in-game; never claim guaranteed real drops or an unproven physical stash hard cap across multiple generators. PlayerRank selection is per native actor rank or native `Newbie/Experienced/Veteran/Master` branch, NOT the region.

**Original weapon removal remains independent of coverage**: checkbox ON still removes only the previously audited 13 regular authored original weapon `PossibleItems` entries; even 0% coverage does not undo this previous explicit removal choice. Checkbox OFF removes none and adds no gear. The other 26 quest/unique/protected/deferred original source entries are never removed, and the World Weapon Shuffle conflict check remains enforced before patch generation. Source-wide 39-entry catalog remains authoritative separately.

**Implementation/source parity:** `src/TrueCustomDifficulty/Data/stash-gear-actor-ranks.csv` freezes 915 class-approved physical ActorSIDs and their native rank branch/slot counts; `src/TrueCustomDifficulty/Data/gear-progression-ranks.csv` contains 38 candidate items. The writer validates every source actor against the existing smartloot base/DLC manifests and all 106 protected actors, then selects the 800/108 eligible subset and emits actor-local extra generators. `tools/audit_stash_gear_coverage.py` tests all 101 percentage settings and selected actor counts; `tests/LootSpecialGeneratorsContract/StashGearCoverage.cs` checks the production writer's actual BPatch structure including 0%, 1%, 50%, 100%, base and DLC. This is source and static verification, **not claimed successful local .NET/Wine/in-game acceptance**. Supersedes all earlier six-control Extra Gear designs below.

# Every documented weapon-containing world stash — original CFG source inventory (2026-10-18)

Primary machine-readable inventory: [39 rows, each with origin/physical ActorSID/level/clue/generator/category/item/status/reason](../../src/TrueCustomDifficulty/Data/stash-weapon-source-inventory.csv).

## What the source confirms

- Original base GameData: **25** weapon category item entries across ordinary/quest world stash actors.
- Original DLC1: **14** such entries (six defined in DLC CFG, **eight referencing generators defined in base GameData**).
- **39 rows / 38 physical actors**, of which **13 independently validated ordinary base-world stash weapon entries** are approved for deletion when Extra Stash Gear is enabled.
- **26 exclusions**: 16 quest-associated, one special/protected STC Malachite, one possibly unique Trophy AR, eight unapproved DLC1 special/cross-archive generator actors.
- `GameData/StashPrototypes.cfg` declares no direct `Gun*` source items. Native procedural SmartLoot can still generate things from other layers; source row presence does not mean a guaranteed spawn.
- The 13 approved source entries preserve weapon category context but remove only their exact original `PossibleItems/[0]` node, not the stash actor, ammo, non-weapon sibling categories or other mods' content.

## Fail-closed policy

The existing 13-entry removal manifest is NOT expanded merely because a generator contains a weapon. Quest labels, native `ClueVariableSID`, actor location in a quest sublevel, unique identity and validated physical source allowlists determine approval. DLC1's eight cross-archive references are intentionally withheld. Even a source with no explicit clue may be quest-owned (e.g. MQ/SQ/ANCQ source names); an empty clue alone is never proof of safety. The existing 106 protected base ActorSIDs are always excluded. The generator only removes the 13 independently audited regular entries; source inventory and writer checks reject status/identity drift.

## Reproducing from archives

```bash
python3 tools/audit_all_stash_weapons.py \
  --game-zip "/path/to/STALKER2-GameData-CFG(20261017-181645).zip" \
  --dlc-zip "/path/to/STALKER2-DLC1-CFG(10).zip"
python3 tools/audit_extra_stash_gear.py
```

First command scans exact CFG original source roots and physical actors, resolving DLC→GameData generator references; emits a failure if new/removed source weapon entries are not reviewed in the checked-in inventory. The scanner reports unresolved external generator references rather than pretending to establish runtime absence of weapons. Second checks the frozen manifest and source-to-writer safety contract without requiring ZIPs.

**Limits:** no experimental evaluation of a physical inventory spawn, no guarantee on already generated stash contents, no claim that these rows exhaust dynamic script-injected weapons or third-party mods. To approve any of eight DLC1 pending entries later, audit their quest and prototype references, source-specific actors and dependencies, update both catalogs and writer allowlist together, and add generated-CFG negative tests.
