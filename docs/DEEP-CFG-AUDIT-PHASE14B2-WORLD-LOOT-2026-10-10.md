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

Registered in `tools/pre_release.py writer` and independent Windows jobs `loot-world-removals-windows` and `loot-world-placement-windows`. **VERIFIED: C10 27/27 PASS and C11 20/20 PASS on the same
production commit `fb59fb36b14a7e8d8593aaf0dcc67e5cdf8736d1`.**
GitHub Actions run `38003085576` executed both Windows jobs,
`loot-world-removals-windows` and `loot-world-placement-windows`,
against the actual application writer. The former retained compact
per-scenario SHA-256 actor identity receipts and representative physical
CFG files instead of archiving tens of thousands of identical tiny files;
**all** generated files were checked before the remaining examples were
pruned. These results prove the manifest-to-production-writer contract
only, not original ZIP fidelity or the game's Unreal loading behavior.

## Phase 14B2 original decoded source verification — INDEPENDENT PASS (2026-10-10)

An independent read of **both uploaded original GameData/DLC1 ZIP archives**
parsed **152,378 physical actor roots** (130,057 base + 22,321 DLC1).
Every eligible production world-loot source domain was compared against
its original physical CFG identity; for complete physical-domain
comparisons, the selected actor SID, level, original source scope,
SpawnedPrototypeSID, generator family and/or item identity were normalized
and independently sorted. Manifest-side and original-side **row counts
and 64-bit FNV-1a signatures match exactly** (with explicit source
eligibility filters rather than inferring eligibility from identical
counts alone). Original ZIP and individual source file SHA-256 digests
are retained in the [original ZIP source evidence](evidence/LOOT-WORLD-ORIGINAL-CFG-2026-10-10.json).

| Original source family | Exact eligible source records | Original result |
| --- | ---: | --- |
| Ammo/RGD boxes (never F1) | 1,315 | PASS |
| Food/Drink boxes | 1,208 | PASS |
| Medical boxes | 868 | PASS |
| Dedicated F1 boxes | 101 | PASS |
| Loose dynamic packs | 5,395 | PASS |
| Loose static world items | 1,838 | PASS |
| Direct world weapon actors | 362 | PASS (10 non-safe source candidates excluded) |
| Direct attachment actors | 7 | PASS |
| World-only blueprint generator rows | 77 | PASS (two other original blueprint roots excluded) |
| Direct detector actors | 7 | PASS (one other original Gilka actor excluded) |
| Source item-generator leaf fingerprints | **131 unique** | **131/131 PASS** after correcting 15 misattributions |
| Original actor generator redirect field fingerprints | **58 unique** | **58/58 PASS** |

Explicit exclusions are source-backed: original **M_WodenMovableChair_21**
actor `3D2445C74FAC2F8ADD61DA905FD9C965` uses
`DestructibleStash_AmmoSNG` but is a **chair, not an ammo box**;
the separate F1 and ordinary ammo/RGD populations have no overlapping
physical actor IDs. Two original `Stash_Blueprint_SciSEVA_Scientific_Armor_Upgrade_1/2`
generator rows are not part of the 77 world-only blueprint shuffle
pool. Ordinary world-weapon manifest intentionally excludes ten
otherwise eligible-looking physical pickups; no quest/named character
locations were added to fill those slots.

### Critical production writer defect found and fixed

The original source had **15 misclassified values in six world weapon
stash generator rows**. These `Chance`, `MinCount`, `MaxCount`
values were physically authored under nested `Upgrades` or `Attaches`
substructures, *not* at the weapon `PossibleItems/[0]` leaf.
The previous writer performed `AddReplacementNode` for the entire
indexed item and copied these values to the wrong level, unintentionally
**dropping vanilla nested upgrade and attachment reward structures**.

The production `CfgPatchWriterService` now generates a **minimal
indexed `{bpatch}` assignment for `ItemPrototypeSID` only**.
This preserves the original item's Chance/Weight/durability/ammo
fields and nested upgrade/attachment subtrees through field-level
merging rather than replacing the complete item structure.
The six manifest rows have had exactly **15 incorrectly attributed
direct item fields cleared**. The production World Loot placement
contract now asserts that edited generator items contain only the
`ItemPrototypeSID` BPatch leaf, and explicitly locks the six
originally nested-only source records against accidental reinstatement.

The updated production writer **has not obtained same-SHA Windows CI
verification**: after the previous 47/47 green run, the current GitHub
Actions attempts failed before any runner step (zero steps on all jobs).
Do **not** carry the older 20/20 placement PASS forward to the repaired
writer's new commit. Once runners execute, repeat the full Windows
writer matrix and inspect emitted CFG fixtures.

### Remaining gates

The exact registered `tools/audit_loot_world_source.py` strict original
archive CLI remains available via `python tools/pre_release.py source`
and must be run against matching user ZIPs before release; the
original-proof evidence above comes from an **independently executed
decoded-source scanner**, not that exact in-repo gate. Original source
evidence establishes physical source values and eligibility, not
Unreal `{bpatch}` behavior or actual gameplay. Indexed patch merging,
PAK acceptance/load order and in-game world placement must still be
tested.

## Independent original CFG and runtime gates

The 320-path Phase 14A original ZIP audit is not sufficient for world actor manifest provenance. In a later original-source pass, verify the actual actor IDs, `SpawnedPrototypeSID`, generator references, DLC scopes, rank/tier/category and physical levels against the supplied decoded GameData and DLC1 originals. Where the data is not in the original CFG, mark it unverifiable rather than inferring it. Randomized selection means tests must use invariant constraints, not fixed sample snapshots.

A new strict original decoded CFG gate,
`tools/audit_loot_world_source.py`, verifies every source row's
actual physical actor identity and selected generator assignments
directly against both original ZIP archives, using memory-limited
streamed original root extraction. It is registered in the pre-release
source runner. **Run this exact repository gate with the original ZIPs before release; the independent original-source audit above has already passed.**

Still **NOT RUN**: the exact in-repository original-source CLI; actual Unreal file nesting and BPatch merge behavior; mod PAK/UTOC/UCAS packaging and load order; gameplay impact on crate counts, direct weapon placement, loot respawn, developer quest references and pickups. Original-source confirmation, writer output and in-game acceptance are distinct evidence layers.

See Phase 14A and 14B1 reports for other Loot families and their independently verified CI receipts.
