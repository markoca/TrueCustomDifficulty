**CURRENT 2026-10-16 — STASHES PHASE 2 SOURCE CONSUMER GRAPH:** The [5,007-row guarded source edge graph](stash-consumer-generator-edges.csv) and [implementation contract](STASH-CONSUMER-GRAPH-2026-10-16.md) preserve all blocked paths, 803 base / 112 DLC SmartLoot actor lists and observed non-authorizing source references. The graph does not modify existing writer guards or establish dynamic active CFG effects; run `python tools/audit_stash_consumer_graph.py`.

# Base world stash actor redirection protection (2026-10-16)

## Whole-container exclusion rule (confirmed 2026-10-17)

The regular Stashes controls must never edit a physical stash containing a quest or unique reward, **including that stash's otherwise ordinary ammo/medical/food/drink rows**. Named-clue and ambiguous mixed sources remain denied. All generator layers are treated as belonging to the same protected actor: tier redirections, shared/indirect wrapper edits, world SmartLoot redirection, and direct medical edits. DLC1 must use its exact no-clue regular actor allowlist; a generic-looking item or tier is not sufficient to approve a special stash.

The 2026-10-17 GameData CFG dump still extracts 1,141 WorldMap ItemContainers and exactly the same 106 protected actors by the existing named-clue/special-generator signals (72 special-only, 29 clue-only, 5 overlapping). For fail-closed defense-in-depth, the production SmartLoot actor loader now checks protected SID non-overlap; both base direct-medical loaders check the same denylist as well as their exact ordinary actor membership. DLC1 verifies blueprint/unique/quest signatures and remains restricted to its 112 exact approved actors. Static audit checks mutual exclusion and protection at all writer entry points. No new user controls or CFG transforms were introduced. The dump/manifest signals cannot prove every possible script-level quest dependency: unidentified sources remain deferred, not assumed safe.

## Root cause and production repair

The Linux Wine `LootSpecialGeneratorsContract` test reported
`smart-medical-zero: unique blueprint actor was rewritten`.
Actor `098E76F14ADD5CFF9C62508DB31013E3` has both regular
`GamePass_Stash_ItemGenerator_Rare` and authored
`Stash_Blueprint_APB_Upgrade_2` under `ItemGeneratorSettings`, as well as a regular SmartLoot list.
The SmartLoot writer already avoided touching this actor, but the **Rare tier production ItemGenerator redirect manifest** still pointed its `ItemGeneratorSettings.[0]` at `TCD_GamePass_Stash_ItemGenerator_Rare`. Thus it is unsafe to reason about protection only within the new SmartLoot writer.

The three production tier writers now call the common `IsApprovedStashTierRedirect` gate. the gate rejects **all 106 protected base-world actor IDs** for both direct `GameData/SpawnActorPrototypes.cfg` assignments and indirect `GameData/ItemGeneratorPrototypes.cfg` wrapper redirections, using each manifest row's `physical_container_sid` from [`base-stash-protected-actor-manifest.csv`](../../src/TrueCustomDifficulty/Data/base-stash-protected-actor-manifest.csv). It retains the previous **112 exact DLC1 ordinary allowlist** when writing `DLCGameData/DLC1/SpawnActorPrototypes.cfg`. Other independent/non-actor patch scopes remain unchanged; actor-associated wrapper generator paths are filtered too. This is *whole-actor* protection: it rejects regular tier subpool redirections on the same actor even if those specific pools contain generic items.

### Inventory extracted from the base CFG

Source archive: `STALKER2-GameData-CFG(20261016-152136).zip`, SHA-256 `937b3fb4c74d1311b6e052c010aa0795073ccdc60d2ca6d8b5228ee3ce0b6083`. Scope: **1,141 WorldMap_WP ItemContainers** extracted from `GameData/SpawnActorPrototypes.cfg`.

| Whole-actor protection category | Actors |
| --- | ---: |
| Named clue, no special ItemGenerator | 29 |
| Explicit Blueprint/Unique/Quest generator, no named clue | 72 |
| Both named clue and special generator | 5 |
| **Unique actors withheld from all stash tier redirects** | **106** |
| Distinct actors with special generator | 77 |
| Distinct actors with named clue | 34 |

The 71 special-generator actors excluded by the existing SmartLoot source audit form only the **subset with nonempty StashPrototypeSID**. Another six actors with special generators have **no SmartLoot reference** and would be missed by a SmartLoot-only denylist. Similarly, some named clues have no SmartLoot source. Protect all these in the tier ItemGenerator domain.

| Tier | Direct actor | Indirect ItemGenerator wrapper | Total excluded |
| --- | ---: | ---: | ---: |
| Basic / Cheap | 2 | 10 | 12 |
| Standard / Common Var1 | 17 | 26 | 43 |
| Rare / Expensive | 11 | 39 | 50 |
| **Total** | **30** | **75** | **105** |

The original `cheap/medium/expensive-stash-production-redirect-manifest.csv` files are retained as source inventories; 105 actor-associated assignments (30 direct and 75 indirect) are skipped by the writer at emission. The rest of eligible regular generator assignments and untouched special rewards are preserved. Existing `GameData/StashPrototypes.cfg` **reference clones** and the 803-base/112-DLC1 SmartLoot source adaptation are unchanged.

### 2026-10-16 — Linux audit assertion and nullable contract cleanup

The source gate in `CfgPatchWriterService.cs` already calls `IsApprovedStashTierRedirect(redirect.File, redirect.Prototype, redirect.PhysicalContainerSid)` in **all three** Cheap/Medium/Expensive writers. The Python static audit incorrectly checked an obsolete *two-argument* call signature, producing an `AssertionError` despite the physical-actor protection being present. Its assertion now checks the actual three-argument call exactly three times. This is a **test correction**, not a relaxation of the production protection or the 106 protected actors / 105 excluded redirects (30 direct, 75 indirect).

The Wine `LootSpecialGeneratorsContract` also compiled with `CS8602` at the SmartLoot actor lookup because its custom `Check(...)` function does not inform C# nullable flow analysis. The follow-on lookup uses `actors!` after the existing explicit guard. No output assertion or protected actor condition was removed. The user's latest log confirms cross-compilation succeeded; the Wine run printed only its startup line, so **the completed runtime result is still unknown**.

To recheck: `python tools/audit_base_stash_protection.py`, `dotnet build tests/LootSpecialGeneratorsContract/LootSpecialGeneratorsContract.csproj -c Release -r win-x64` and `./tests/run-wine.sh LootSpecialGeneratorsContract`.

## Repeatability and acceptance

- Complete machine-protected IDs (106 with exact reason): `src/TrueCustomDifficulty/Data/base-stash-protected-actor-manifest.csv`, embedded in the Windows binary; header `ActorSID,ProtectionReason`. The code rejects extra/missing/duplicated IDs and class count drift.
- Static audit: `python tools/audit_base_stash_protection.py` checks all three writer guard sites, source/manifest overlap (2+10 / 17+26 / 11+39), CSV integrity, embedded resource and generated-CFG tests. Optionally re-extract original actor records using `python tools/audit_base_stash_protection.py --source-zip "STALKER2-GameData-CFG(20261016-152136).zip"`.
- Windows-generated CFG regression: `./tests/run-wine.sh LootSpecialGeneratorsContract` on Linux, or `dotnet run --project tests/LootSpecialGeneratorsContract/LootSpecialGeneratorsContract.csproj -c Release` on Windows. The suite checks every protected SID has **no** actor paths in generated `GameData/SpawnActorPrototypes.cfg` for both direct tier and SmartLoot edits. It retains the previous named-clue and blueprint negative checks, and confirms 105 preexisting manifest entries are now filtered.
- **Do not turn a failed guard into a test-only exception or remove the protection assertion.** Require the emitted CFG to preserve special/quest/unique stash actors in all three tiers.
- Actual .NET/Wine and in-game stash behavior still require runtime acceptance; source/static checks alone do not prove Unreal/IoStore deployment behavior.

UI and preset compatibility: the same five compact STASHES graphs and existing `loot.*` source IDs. No UI editor or presets were added.
