# Deep CFG / BPatch Audit — Phase 14B2: World Loot (2026-10-10)

## Scope

World Loot completes the **10 remaining specialized Loot input IDs** after Phase 14A (16 direct CFG IDs / 320 source paths) and Phase 14B1 (14 special generator/corpse IDs / 63 production tests).

Five source-backed sliders remove physical world actors: Ammo Boxes, Food & Drink Boxes, Medical Boxes, dedicated F1 Grenade Boxes, and loose world pickups. A separate five-input family comprises World Weapon Shuffle plus dependent Weapon Density, Attachment Shuffle, Detector Removal and Blueprint Shuffle.

All existing controls and 0.x version are unchanged. The production `CfgPatchWriterService.Write()` continues to use **source-level** `IsValueModifiedFromReference()` instead of aggregate UI `IsModified`; unmodified parent controls must not trigger unrelated fanout.

## Production contract C10 — physical removals

`tests/LootWorldRemovalsContract` runs **27** actual MainViewModel-to-production-writer scenarios: four untouched vanilla difficulties; five removal sliders × {base,DLC1} × {25%,100%}; plus three mixed-domain cases.

Expected source domain:
- Ammo Boxes: **1,315** physically audited actor IDs.
- Food & Drink Boxes: **1,208** actors.
- Medical Boxes: **868** actors.
- Ordinary loose world pickups: **7,233** actor IDs (**5,996 base + 1,237 DLC1**).
- Dedicated F1 Grenade Boxes: **101** actors (**61 base + 40 DLC1**, only `D_WoodenAmmoCrate_01` with `DestructibleStash_F1`).

Every actual generated removal file must map back to precisely one eligible source actor ID, path, level and category. The suite checks per-category removal cardinality using actual `MidpointRounding.AwayFromZero`, source manifest eligibility, independent base/DLC selection, disjoint domains, and **exactly one** `<ActorSID> : removenode` in each output. Tests intentionally never assume a deterministic RNG seed. No quest/reward/general-world-actor blanket removal is permitted.

## Production contract C11 — physical placement and permutations

`tests/LootWorldPlacementContract` runs **20** actual producer scenarios covering all four untouched vanilla difficulties, disabled-dependent density, each of the four independent world-effect toggles in base and DLC1, three weapon-density settings in each environment (0%, 50%, 100%) and three combinations.

Contracts verified independently of randomized choices:
- **Weapon shuffle and density:** 36 distinct regular weapons, 282 safe base direct actors + 80 DLC1 actors, 14 safe generator locations. Preserve pistol/primary slot category and ≤1 tier distance. For 296 base or 376 DLC-capable locations, retain exactly `36 + round((locations−36) × density)`; guarantee the normal source-model minimum copies; remove only eligible actor locations; change both direct item identity fields together; preserve authored ammo/durability/count mechanics in replacement generator items. No arbitrary weapons/quest spawns are eligible.
- **Attachments:** 39-item pool, 5 base direct + 57 base generator slots and 2 DLC1 direct + 3 DLC1 generator slots. Reassign only eligible slots with matching attachment category and at most one tier difference; preserve **exact multiset of actual original attachment IDs**, cloned `refurl/refkey` lineage, actor `PrototypeSID` redirects and both direct physical pickup identity fields. Never change traders/quest benches by globally editing vanilla generator roots.
- **Blueprints:** 77 world-only generator entries. Every blueprint SID remains present exactly once, **none stays in its original slot**, and emitted coupon `Chance` remains exactly 1. Every field belongs to an audited Stash_Blueprint entry.
- **Detectors:** only four active base direct pickups, three extra DLC1 direct pickups, plus **one DLC1 stash generator entry**, each with a scoped removal node. No unrelated detector/quest/trader override.

Both tests parse generated CFGs into physical source file, nested node path, exact leaf assignments, removal markers and template references. Their exact no-collateral assertions prohibit unrelated CFG changes and prohibit DLC1 actor output in a base-only installation. The shared source catalog and production manifests provide expected identities; a green test proves writer consistency, **not** the original world source provenance or Unreal gameplay.

```powershell
dotnet run --project tests/LootWorldRemovalsContract/LootWorldRemovalsContract.csproj -c Release -- --out audit/loot-world-removals-fixtures
dotnet run --project tests/LootWorldPlacementContract/LootWorldPlacementContract.csproj -c Release -- --out audit/loot-world-placement-fixtures
```

Registered in `tools/pre_release.py writer` and independent Windows jobs `loot-world-removals-windows` and `loot-world-placement-windows`. **As of test registration: not yet confirmed on same-SHA CI.** Do not mark the suites PASS until the corresponding jobs finish successfully.

## Independent original CFG and runtime gates

The 320-path Phase 14A original ZIP audit is not sufficient for world actor manifest provenance. In a later original-source pass, verify the actual actor IDs, `SpawnedPrototypeSID`, generator references, DLC scopes, rank/tier/category and physical levels against the supplied decoded GameData and DLC1 originals. Where the data is not in the original CFG, mark it unverifiable rather than inferring it. Randomized selection means tests must use invariant constraints, not fixed sample snapshots.

Still **NOT RUN**: original archive revalidation of all world manifest rows; actual Unreal file nesting and BPatch merge behavior; mod PAK/UTOC/UCAS packaging and load order; gameplay impact on crate counts, direct weapon placement, loot respawn, developer quest references and pickups. Original-source confirmation, writer output and in-game acceptance are distinct evidence layers.

See Phase 14A and 14B1 reports for other Loot families and their independently verified CI receipts.
