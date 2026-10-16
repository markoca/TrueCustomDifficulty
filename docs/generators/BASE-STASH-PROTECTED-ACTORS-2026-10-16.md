# Base world stash actor redirection protection (2026-10-16)

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

## Repeatability and acceptance

- Complete machine-protected IDs (106 with exact reason): `src/TrueCustomDifficulty/Data/base-stash-protected-actor-manifest.csv`, embedded in the Windows binary; header `ActorSID,ProtectionReason`. The code rejects extra/missing/duplicated IDs and class count drift.
- Static audit: `python tools/audit_base_stash_protection.py` checks all three writer guard sites, source/manifest overlap (2+10 / 17+26 / 11+39), CSV integrity, embedded resource and generated-CFG tests. Optionally re-extract original actor records using `python tools/audit_base_stash_protection.py --source-zip "STALKER2-GameData-CFG(20261016-152136).zip"`.
- Windows-generated CFG regression: `./tests/run-wine.sh LootSpecialGeneratorsContract` on Linux, or `dotnet run --project tests/LootSpecialGeneratorsContract/LootSpecialGeneratorsContract.csproj -c Release` on Windows. The suite checks every protected SID has **no** actor paths in generated `GameData/SpawnActorPrototypes.cfg` for both direct tier and SmartLoot edits. It retains the previous named-clue and blueprint negative checks, and confirms 105 preexisting manifest entries are now filtered.
- **Do not turn a failed guard into a test-only exception or remove the protection assertion.** Require the emitted CFG to preserve special/quest/unique stash actors in all three tiers.
- Actual .NET/Wine and in-game stash behavior still require runtime acceptance; source/static checks alone do not prove Unreal/IoStore deployment behavior.

UI and preset compatibility: the same five compact STASHES graphs and existing `loot.*` source IDs. No UI editor or presets were added.
