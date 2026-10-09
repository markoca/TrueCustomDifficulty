# Deep CFG / BPatch Audit — Phase 14A: Loot Direct Sources (2026-10-10)

## Why Loot is split into 14A and 14B

The current Loot catalog has **40 parameters**, but the production
direct BPatch baseline has only **16 parameter IDs / 320 points**:

| Direct family | Active IDs | Direct source points | What changes |
| --- | ---: | ---: | --- |
| Mutant part drop chance | 14 | 14 | One eligible mutant-generator `Chance` per family; original one-part count remains unchanged |
| Rare Stash coupon chance | 1 | 64 | Four difficulty SID groups and four progression-rank coupon tiers under `Stash_Expensive_*` aliases |
| NPC dropped weapon condition | 1 | 242 | `MinDurability`/`MaxDurability` on 121 weapon entries across 33 repeatable-human roots |
| **Total** | **16** | **320** | All 320 entries refer to base-game `GameData/ItemGeneratorPrototypes.cfg` |

**The other 24 Loot catalog IDs use specialized production writers.**
This audit **does not** validate their behavior: stash ammo quantity
and grenade production, NPC medical and corpse bonus/ammo, breakable
box generators/distribution, world-loot removal, world weapon/attachment/
blueprint shuffles, detector removal and their DLC1 fanouts remain the
separate **Phase 14B** audit. A `320/320` direct source result is NOT
a claim that all of Loot is correct.

## Independent original archived source audit — 320/320

The uploaded original decoded ZIPs were independently parsed.
Every selected prototype, nested source path and numeric original
value was compared with the current production direct source map.
The original ItemGeneratorPrototypes file SHA-256 is
`4a06bc946eb9eaa7ff736421df7fd1bb6fe38fb91038bf5fffa005f680d64ac3`
(9,101,641 bytes).

| Physical provenance | Entries | Result |
| --- | ---: | --- |
| Directly authored original `Chance` / durability fields | **256** | Exact |
| Rare Stash inherited `Chance` through verified physical parent | **64** | Inherited exact |
| Missing/ambiguous fields, raw mismatches | **0** | No findings |

The 64 `Stash_Expensive_Newbie/Experienced/Veteran/Master` targets
are **not directly authored** under their child prototypes. Every
alias explicitly contains
`{refurl=ItemGeneratorPrototypes/Gamepass_ItemGenerators.cfg;refkey=GamePass_Stash_ItemGenerator_Rare}`
and only a SID. The actual original
`GamePass_Stash_ItemGenerator_Rare` prototype and coupon fields
are physically present in the same **consolidated decoded**
`GameData/ItemGeneratorPrototypes.cfg`. This is source provenance,
not a demonstrated Unreal cross-file `{bpatch}` merge.

For Easy/Medium the canonical `ItemGenerator/[11..14]/PossibleItems/[0]/Chance`
is `0.2f`; for Hard/Stalker the four rows are `[15..18]` at `0.1f`.
Their actual `ItemPrototypeSID` progression is
`MoneyCommon, MoneyRare, MoneyEpic, MoneyLegendary`.
These eight physical parent fields are reused by four aliases and
their difficulty-gated writer edits; there are **64 effective
direct-mapping points**, but not 64 separately authored literal
fields.

The NPC durability source has **121 exact weapon entries in 33
generic/repeatable-human roots**. All except one have 25%–50%
original condition. The sole deliberate source exception is
`GuardNPC_Corpus_Stormtrooper_ItemGenerator` at
`ItemGenerator/[0]/PossibleItems/[1]`: 35%–45%.
The vanilla default MUST generate no patch, preserving that outlier.
When a user changes the range, the same player-selected MIN/MAX
must be explicitly written for all eligible weapon rows. Quest,
Prologue, zombie and named-character generators are not reclassified
as generic NPC equipment.

All 14 direct mutant generators use `MutantLoot` `Chance` and
retain `MinCount=MaxCount=1`. A drop probability change must
not change the number of parts per successful drop.

The hashes of the original archives and scoped source file,
and the domain and status counts are retained in
[evidence/LOOT-DIRECT-ORIGINAL-CFG-2026-10-10.json](evidence/LOOT-DIRECT-ORIGINAL-CFG-2026-10-10.json).
The independent original read includes a complete 320-record
per-field report; source gate scripts below reproduce it.

## Two executable original-source gates

The standard source gate compares all 320 original raw values,
including only the specifically allowlisted, physically verified
Gamepass flattened-refurl ancestry:

```bash
python tools/audit_cfg_source_values.py \
  --base-zip "/path/to/STALKER2-GameData-CFG(2)(2).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(2)(2).zip" \
  --parameter-prefix loot. --out audit/loot-direct-source-values.json --strict

python tools/audit_loot_direct_source.py \
  --base-zip "/path/to/STALKER2-GameData-CFG(2)(2).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(2)(2).zip" \
  --out audit/loot-direct-source-structures.json
```

The dedicated structural gate additionally verifies that the 242
durability paths genuinely refer to weapon item generator categories,
that all 14 mutant rewards remain one-part rolls, and that the
coupon `ItemPrototypeSID` progression and wrapper ancestry are
unchanged. Both are registered under `tools/pre_release.py source`.
The source gate has a negative regression: wrong inherited raw value
or missing/foreign Gamepass reference must fail closed.

## Real ViewModel-to-BPatch writer contract

`tests/LootDirectPatchContract` runs the actual `MainViewModel`
and production `CfgPatchWriterService.Write`, then parses the
generated CFG and checks exact root/array paths, all nested
`{bpatch}` flags, emitted counts and numerically correct fields.

**117 scheduled writer scenarios:**
- 4 vanilla no-output cases on Novice, Stalker, Veteran, Master;
- 14 mutant sources × four difficulties = 56;
- 14 mutant sources × two explicit edge tests = 28;
- Rare Stash coupon OFF/100% × four difficulties = 8;
- NPC weapon condition: min-only, max-only, both and 0–100% extremes
  × four difficulties = 16;
- 5 combined tests for multi-mutant, coupon, condition and
  cross-control merging into one shared output CFG.

This direct test suite intentionally avoids triggering the other 24
special-only Loot features. Those require additional production
fixtures and gameplay eligibility tests, not a broad PASS from
the direct manifest.

```powershell
dotnet run --project tests/LootDirectPatchContract/LootDirectPatchContract.csproj -c Release -- --out audit/loot-direct-patch-fixtures
```

The `loot-direct-writer-windows` job runs under .NET 10 on Windows.
**Status at writing: registered, result pending same-SHA CI.** No
claims of 117/117 PASS until an actual green job is recorded.

## In-game and engine acceptance — NOT RUN

Source identity and a writer's generated text do not establish that
Unreal merges edits into refkey-backed Rare Stash children.
Verify coupon chance in-game across all four difficulties, the
NPC condition range (including the vanilla outlier), and each mutant
part drop independently. Verify there is no effect on quest/reward
stashes, story NPCs or other special generators.

No UI, design lock, gameplay balancing or project version was changed.
