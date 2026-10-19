# Incremental UI → production writer → CFG destination audit — 2026-10-19

## Scope and result

This is a **delta audit**, not a repetition of the existing source-verified 2026-10-15 pass. The frozen \`tools/final_cfg_mapping_inventory.json\` covers the **earlier 248 parameters**, including **213 direct source IDs and 1,230 concrete CFG leaf edit points**; that historical source audit remains valid only for its prior snapshot. The current catalog and embedded writer registry are **283 total IDs**: **213 direct** and **70 special**, giving **35 additional special-registered Stashes parameters** and **0 missing catalog-to-route registrations**. Of 283, **270 are editable dimensions** (including source-backed aggregate members); the prior audit-only list still has 13.

The current 35 added IDs all have source statuses \`runtime_test_required\`; a registered route or a static scan does **not** establish actual loot in the game. The new \`tools/audit_cfg_route_delta.py\` checks precisely the new 35 IDs against the frozen earlier source inventory, production method invocation, the actual generator/actor output destinations, branch gates, and both special route registries. It emits a per-ID CSV with \`--out audit/stashes-cfg-route-delta.csv\`. Existing native Stash, World Loot, Combat, Master, etc. audits remain responsible for their already-reviewed IDs.

| Added source group | Count | Actual writer entrypoints | Possible CFG source destinations |
| --- | ---: | --- | --- |
| Stash Content Priority | 6 | \`WriteCheapStashProduction\`, \`WriteMediumStashAmmoProduction\`, \`WriteExpensiveStashProduction\`, \`WriteWorldStashSmartLoot\` | \`GameData/ItemGeneratorPrototypes.cfg\`, \`GameData/StashPrototypes.cfg\`; approved actor redirects in base/DLC1 \`SpawnActorPrototypes.cfg\` |
| Stash Medical Priority / Chance / Quantity | 12 | The same three tier clone writers plus \`WriteWorldStashSmartLoot\`; additional direct/Cabin medical paths are already independently tested | The same possible generator, SmartLoot and approved actor/redirect destinations |
| Extra Stash Gear | 7 | \`WriteStashGearCoverage\`; master additionally \`WriteStashAuthoredWeaponRemovals\` | \`GameData/ItemGeneratorPrototypes.cfg\` (private \`TCD_ExtraGear_*\` definitions and 13 original-weapon removals), base/DLC1 \`SpawnActorPrototypes.cfg\` (private generator references) |
| Standard/Rare category Chance factors | 10 | \`WriteMediumStashAmmoProduction\` or \`WriteExpensiveStashProduction\` | \`GameData/ItemGeneratorPrototypes.cfg\` clone definitions plus manifest-approved physical actor redirects |

**Every group uses the ordinary production staging convention:** \`Stalker2/Content/GameLite/<source folder>/<PrototypeName>/<PrototypeName>_patch_TrueCustomDifficulty.cfg\` for prototype CFG files. Listed locations are **possible physical destinations**, not a claim that every single parameter always emits all of them. A setting identical to its preset reference may intentionally emit no patch; optional DLC1 routes depend on actual DLC availability. The audit keeps source provenance distinct from production emission and in-game behavior.

## Repaired audit gates (not gameplay changes)

- \`tools/audit_final_cfg_mapping.py\` expected a stale **68** special IDs and **268** editable dimensions even though its own live catalogue requirement was 283. The corrected counts are **70 special**, **270 editable**.
- \`tools/audit_stash_gear_coverage.py\` had a stale **281** count (two places); it now expects 283.
- \`tests/AllControlsPatchContract/Program.cs\` had stale hardcoded **268 total / 255 editable** IDs. It now probes the **283 / 270** live catalog and requires an actual \`GameData/ItemGeneratorPrototypes\` BPatch for Stashes categories rather than relying only on the writer's \`writtenParameters\` bookkeeping. During gear source probes it turns on the master and selects **1% real eligible stash coverage** so native bonus Chance/priorities have a physical target. Other 248 already-audited source controls are **not redesigned**.
- CI runs the incremental delta gate, and its two stale job captions now match the production test scope.

No parameters, user-facing UI layouts, CFG writer transformations, game chances, protection allowlists, or serialized factory preset values were altered by this audit.

## Acceptance and remaining boundaries

**Evidence established by repository inspection:** exact current baseline count 283, 1,230 direct edit points / 213 direct source IDs; production special registration coverage of 70/70; all 35 newly registered Stashes IDs have declared generator/actor target files, named invoked C# writer methods, and source-specific static routing/CFG-path contracts. There are no known orphaned catalog source IDs.

**Not established without execution:** that a Windows WPF build compiles; that \`tests/AllControlsPatchContract\` succeeds for each of the 270 dimensions; that an edited parameter produces a usable native BPatch in a particular difficulty, rank, DLC or previously opened stash; and that Unreal applies it in-game. In particular, Extra Gear build-time 1% selection, multiple native generator coexistence, array patch merge behavior, and source-authored chance/weight semantics still require executable CFG/game QA. These are **not** marked PASS by this static pass.

Reproduce after pulling \`main\`:

```bash
python3 tools/audit_final_cfg_mapping.py
python3 tools/audit_cfg_route_delta.py --out audit/stashes-cfg-route-delta.csv
python3 tools/audit_stashes_current.py
python3 tools/audit_stash_gear_coverage.py
dotnet run --project tests/AllControlsPatchContract/AllControlsPatchContract.csproj -c Release
```
