# Trader Buys — 5 × 8 merchant purchase matrix (2026-11-13)

**Implementation status: implemented in TCD source and production writer, game-runtime acceptance pending.** The control changes what merchants **BUY FROM THE PLAYER**. It does not modify what they sell, item prices, NPC inventories, medical stock or minimum weapon/armor buy condition.

## UI and native categories

One visible **Trader Buys** matrix card under Economy, eight native purchase categories across five seller roles. 40 independent boolean settings; 39 are non-standalone backing VMs and one is the visible anchor:

| Merchant role | Default restricted native categories | Profiles |
| --- | --- | ---: |
| General | none (Eger has separate exclusions) | 33 |
| Medic | Weapon, Armor, Ammo, Grenade, Attach, MutantLoot | 15 |
| Technician | Consumable, Ammo, Grenade (Bolshak DLC1 has no exclusions) | 16 |
| Bartender | MutantLoot | 13 |
| Generic NPC | Weapon, Armor, MutantLoot | 9 |
| **Total** | — | **86** (68 base, 18 DLC1) |

Columns: **Weapons (Weapon), Armor (Armor), Attachments (Attach), Ammo (Ammo), Grenades (Grenade), Artifacts (Artifact), Consumables (Consumable), Mutant Parts (MutantLoot)**. No unverified Detector/NVG/Binoculars buy categories. Food, Drinks and Medical cannot be separated in this native purchase filter because they share `EItemType::Consumable`.

Checked means buy/accept this category **when explicitly overridden**, unchecked means exclude it **when explicitly overridden**. A checkbox at its **untouched default** is not a forced acceptance: every trader keeps its exact source blacklist, including Eger and Bolshak. The role-level defaults show the common policy only. Once an individual cell changes relative to its default, this category applies to every seller of that role, including nonstandard traders. Individual traders may still refuse damaged/quest items or have insufficient money.

## Production source

`src/TrueCustomDifficulty/Data/trader-buy-policy-manifest.csv`: 86 source-exact trader profile IDs and raw `BuyLimitations` lists from original STALKER 2 CFG 2.0.5 and DLC1 sources (53 explicit lists, 33 not authored). Validate the 33 general seller groups, 15 medics, 16 technicians, 13 bartenders and nine generic NPC traders. Four prototype-only/test targets are excluded. The new manifest is embedded in the compiled application.

`src/TrueCustomDifficulty/Services/TraderBuysPolicy.cs`: canonical UI row, column, native type and 40 source ID catalog.

`src/TrueCustomDifficulty/Services/CfgPatchWriterService.TraderBuys.cs`: production source allowlist validation; applies only modified role/category IDs. It constructs the new list from the trader's **actual original** list, removes/adds the single changed enum, then compares the result with original. When identical, it emits no patch for that merchant. When changed, it uses `PatchDocument.AddReplacementNode` to replace **only** `TradeGenerators/[0]/BuyLimitations` without `{bpatch}` on that child, retaining `{bpatch}` on parent nodes; if the list becomes empty, it emits the child-only `removenode`. Any preserved blocked types keep their source order, newly blocked types append; all indices are dense. DLC1 profile patches are emitted only when DLC1 is detected.

It merges with the existing `economy.traderMinBuyCondition` writer on the same `TradeGenerators/[0]` parent without altering either field set. The two original explicitly authored 0% minimum-condition trader exceptions remain source-neutral until the global slider is deliberately edited.

## Presets, tests and verification

- 40 `economy.traderBuys.{role}.{category}` boolean IDs; one visible composite anchor, 39 backing IDs.
- Catalog: **328** parameters, **184** catalog-visible cards, **144** hidden parameters. Legacy frozen RC copy applies to its original 183 entries; new anchor copy separately reviewed.
- All 12 factory presets gain all 40 entries, neutral at source role defaults. Old player presets load absent values as defaults using existing preset migration semantics.
- `EconomyPatchContract`: new neutral test, five-role fanout test, combined buying+minimum-condition test, and original serializer shape test; total **43** Economy test scenarios, up from 40.
- `AllControlsPatchContract`: 311 player-mounted editable source IDs, up from 271; checks every category can cause a real staged edit.
- `tools/audit_trader_buys_implementation.py`: direct original source vs policy manifest / role/category/preset UI/writer route/source safety checks, also included in `tools/pre_release.py static`.

**Acceptance not claimed:** compiled .NET build / WPF layout visual behavior, generated mod install, Unreal BPatch semantics on live game, purchase eligibility, cross-mod replacement conflicts and DLC1 gameplay. Do not label this implementation game-tested until those pass. The implemented control is exclusively merchant BUY acceptance; the separately researched **Trader Sells** category suppression remains unimplemented.


## Verification against original 2.1 TradePrototypes (2026-11-14)

All 86 native BUY exclusion profiles were re-extracted from the 2.1 base+DLC1 source (53 explicit, 33 non-authored), and the signed source inventory exactly matches the embedded buy policy manifest. The two TradePrototypes CFG files are byte-for-byte identical to the provided 2.0 originals. The native 2.1 BUY verifier is `python3 tools/audit_trader_buy_21_source.py --source "/path/STALKER2-2.1 cfg dump.zip"`; its default CI mode pins the source-derived ledger and checks 2.1 SELL role parity. Durability thresholds: 64 physically authored pairs, 22 non-authored pairs that are assigned the historical 0.4f marker; do not call those 22 original authored defaults. No UI or active gameplay values have changed.
