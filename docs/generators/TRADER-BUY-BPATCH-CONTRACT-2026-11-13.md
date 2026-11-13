# Trader Buys — BPatch and Vanilla Durability Audit (2026-11-13)

**Verdict: original-source and hypothetical patch-shape PASS; compiled .NET writer, actual Unreal parser, multi-mod conflicts and game runtime NOT VERIFIED.** No new Trader Buys UI/control is implemented.

## Scope and reproducibility

Original CFG 2.0.5 source identities:

| Source | SHA-256 |
| --- | --- |
| Base ZIP | `7610b16218310c7345796ff26b1554c73e3d2ca775f6a50108a7406cb7f33da1` |
| DLC1 ZIP | `3c5cb53f1769619b510ae3780142d92639c083a4a755a8717a267710c3c849e6` |
| Base GameData/TradePrototypes.cfg | `c5115350d67d91a329b5922f0a51e9abe02f91075e5fbe24c1b3b4fa48f540eb` |
| DLC1 DLCGameData/DLC1/TradePrototypes.cfg | `7a72b5dfc112226c92c081e49ca6c3877b7cf00cd541964d07113253d37e31a5` |

Offline verification was executed over these two archives with the frozen 86-profile source manifest and buy blacklist CSV. The committed reproducible gate is `tools/audit_trader_buy_bpatch_contract.py`, importing the existing original-source parser `tools/extract_trader_trade_inventory.py`.

Run from repository root:

~~~bash
python3 tools/audit_trader_buy_bpatch_contract.py \
  --base-zip "/path/to/STALKER2-GameData-CFG(2)(2).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(2)(2).zip" \
  --out audit/trader-buy-bpatch
~~~

Exactly **86** profiles: **68 base, 18 DLC1**. Each has a concrete, locally authored `TradeGenerators/[0]` node. Of these, **53** have explicit `BuyLimitations` and **33** have none locally. All explicit arrays use dense ordinal scalar fields `[0]`, `[1]` etc. The eight types observed in original denial lists are Weapon, Armor, Ammo, Grenade, Attach, Consumable, MutantLoot and Artifact. Unlisted means not denied by **this** filter, not guaranteed acceptance.

## Candidate patch shape

Official [Zone Kit Config patching](https://zonekit-support.stalker2.com/en-US/kb/article/172/how-to-patch-contents-of-a-cfg-file) explains nested `{bpatch}` parent merge, child replacement, and `removenode` under a patch parent.

To deny Consumables at Asylum Medic while retaining all existing bans, the prospective **child-only** patch is:

~~~cfg
Asylum_Medic_TradePrototype : struct.begin {bpatch}
    TradeGenerators : struct.begin {bpatch}
        [0] : struct.begin {bpatch}
            BuyLimitations : struct.begin
                [0] = EItemType::Weapon
                [1] = EItemType::Armor
                [2] = EItemType::Ammo
                [3] = EItemType::Grenade
                [4] = EItemType::Attach
                [5] = EItemType::MutantLoot
                [6] = EItemType::Consumable
            struct.end
        struct.end
    struct.end
struct.end
~~~

All ancestor nodes use `{bpatch}`, but the `BuyLimitations` child **does not**. That causes a replacement of **this child only**, not a replacement of the full `TradeGenerator` or `TradePrototype`. This is important for preserving sibling source data such as price modifiers, SID links and minimum durability.

If the resulting list becomes empty, the **candidate** is `BuyLimitations : removenode` under the same three `{bpatch}` ancestors. Do not silently leave an empty nested array or remove scalar `[i]` fields: scalar-removal behavior and index compaction were not verified in the Unreal runtime.

The current production C# `CfgPatchWriterService.PatchDocument` already exposes `AddReplacementNode` and `AddRemovalNode` helpers that serialize this prospective form. The new C# Economy contract exercises those **actual helper methods via reflection** to validate the full child replacement/removal outline, while also emitting sibling durability assignments into the same parent. **The C# contract was not run in this environment** because .NET and Wine are not installed.

### Original-source simulation results

| Static probe | Count |
| --- | ---: |
| Source trade profiles read | 86 |
| One-category toggle probes | **688** (86 × 8) |
| Adds to source denial list | 506 |
| Removals from source denial list | 182 |
| Resulting empty denial lists | 13 |
| Mutated unrelated sibling fields in the isolated tree model | **0** |

These are **Python model checks** of source rows and BPatch-shape representations, not an Unreal parser. The repo audit command writes representative proposed CFG snippets and a JSON summary. No output patch was installed or loaded in game.

The prospective strategy is **not conflict-free across mods**: another mod may edit the same `BuyLimitations` child, in which case list replacement can override its additions. Verify conflict logs, precedence and composition under the real engine before shipping; do not claim universal compatibility. Also, native types Detector/NightVisionGoggles/Binoculars were not present in the 53 original lists and remain unproven as acceptance-filter enum values.

## Minimum buy condition — original 0% exceptions

The two actual source-authored production exceptions are:

| TradePrototype SID | WeaponSellMinDurability | ArmorSellMinDurability | Original CFG root |
| --- | --- | --- | --- |
| `SelmaTrader_TradePrototype` | `0.0f` | `0.0f` | Base line ~3076 |
| `Trader_Soviet_Rostok_TradePrototype` | `0.0f` | `0.0f` | Base line ~2175 |

Among 86, **62** explicitly author 0.4f/0.4f, **two** explicitly author 0.0f/0.0f, and **22** do not locally author either field. Do **not** claim those 22 have source-authored 40% thresholds; effective inheritance/runtime validation remains separate.

The old production `trader-min-buy-condition-manifest.csv` incorrectly recorded 0.4f in all 86 rows. The two special rows have now been corrected to **0.0f**, preserving all 86 source targets and the seven-column schema.

The existing `economy.traderMinBuyCondition` slider has an absolute 40% UI reference value. While unchanged it emits **no patch**, so the two original 0% profiles remain untouched. When deliberately changed, its absolute threshold is written to all 86 mapped production targets (with DLC1 detection), including those two. In current production `WriteTraderMinBuyCondition`, the manifest raw vanilla value controls **numeric formatting**, not the assigned threshold, so the correction preserves edited outputs. A new regression assertion in `tests/EconomyPatchContract/Program.cs` checks both 0% entries and unchanged reference behavior.

## Remaining test gates (not claimed as PASS)

1. Run the Economy production C# contract with .NET/Wine: the new serializer shape scenario and existing 39 scenarios (expected total **40**) must pass, including no other TradeGenerator fields in the emitted patch.
2. Inspect BPatch application and warnings with the real game/Zone Kit, including removing a sole denial category and adding a new `BuyLimitations` child to a trader who did not originally have one.
3. Validate interaction with other installed mods editing the same child, inherited source configs, and DLC1 trade profiles.
4. Check buy eligibility and price at one real General, Medic, Technician, Bartender, Generic NPC and DLC1 trader, and at both 0%-condition sellers. Negative/positive tests must account for wallet, rank and quest state.

**No new gameplay controls, writer purchase-policy route, presets or UI catalog changes are authorized by this source-only verification.**
