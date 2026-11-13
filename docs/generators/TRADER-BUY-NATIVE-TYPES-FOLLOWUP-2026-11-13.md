**2026-11-13 verified follow-up:** [BPatch and durability source contract](TRADER-BUY-BPATCH-CONTRACT-2026-11-13.md) confirmed the 86 source profiles and 688 hypothetical category flips. The two original 0%-condition rows are now corrected in the production manifest; compiled writer/Unreal gameplay validation remains outstanding. This supersedes the earlier statement below that the manifest still needs reconciliation.

# Trader Buy Acceptance — Native Item Types and Consumables Follow-up (2026-11-13)

**Verdict: source-verified for broad native item types; Food / Drinks / Medical cannot be split via the existing `BuyLimitations` field.** No gameplay or UI controls implemented. No in-game behavior verified.

## Source and reproducibility

Independently parsed user-supplied original **STALKER 2 2.0.5** `GameData/TradePrototypes.cfg`, `GameData/ItemPrototypes.cfg` and DLC1 equivalents, plus the 2,939 exact source offer paths in the previous audit. The ZIPs match established SHA-256 signatures:

- Base ZIP: `7610b16218310c7345796ff26b1554c73e3d2ca775f6a50108a7406cb7f33da1`
- DLC1 ZIP: `3c5cb53f1769619b510ae3780142d92639c083a4a755a8717a267710c3c849e6`
- Base `TradePrototypes.cfg`: `c5115350d67d91a329b5922f0a51e9abe02f91075e5fbe24c1b3b4fa48f540eb`
- DLC1 `TradePrototypes.cfg`: `7a72b5dfc112226c92c081e49ca6c3877b7cf00cd541964d07113253d37e31a5`

Run **`python3 tools/audit_trader_buy_native_types.py --base-zip <base.zip> --dlc1-zip <dlc1.zip> --offers-csv docs/generators/trader-offers-source-rows.csv --out audit/trader-native-buy-types`** from the repository root. This is a source/identity audit; running it does not patch the game. It reconciles the existing **86-profile production manifest**, the prior independently extracted 86-profile `BuyLimitations` CSV, item-template inheritance, all 164 seller candidate item SIDs, and the 2,939 source offer paths. Generated CSVs below are included as a frozen same-version snapshot.

## 1. All food/drink/medical seller SIDs share a native type

| Gameplay grouping | Unique seller candidate item SIDs | Native effective item type | Observed `SortGroup` |
| --- | --- | --- | --- |
| Food | Bread, CannedFood, Milk, Sausage, DLC1 MonolithDryRation (5) | **`EItemType::Consumable`** | Hunger; DLC1 ration: Buff |
| Drinks | Beer, Energetic, Vodka, Water (4) | **`EItemType::Consumable`** | Radiations, Stamina |
| Medical | AntiRad, ArmyMedkit, Bandage, EcoMedkit, Hercules, Medkit (6) | **`EItemType::Consumable`** | Radiations, HP, Bleeding, Buff |

For base-game items, the item prototype inherits `Type` from `TemplateConsumable`; the DLC1 `MonolithDryRation` has an explicit `Type = EItemType::Consumable`. **`SortGroup` is not a `BuyLimitations` criterion.** Thus `BuyLimitations` can forbid all three classes together using `EItemType::Consumable`, but it **cannot** express “buy food but not medicine” or “buy drinks but not food” based on the verified native schema. An item-SID-specific buying hook, independent buy rules, or runtime code path would require separate proof and is **not approved**.

## 2. Native buy blacklist audit: every production seller

- **86** production `TradePrototype` roots: **68 base + 18 DLC1**. Every root has exactly one locally authored `TradeGenerators/[0]` in the original decoded CFG; no multiple-generator ambiguity for these targets.
- **53** author an explicit `BuyLimitations` struct; **33** do not. Absence means **not explicitly blocked by this list**, NOT proven that the merchant will accept a given item (condition, pricing, funds and other gameplay rules still matter).
- There are exactly **six** native blacklist patterns, including an empty list, consistent with the prior 86-profile extraction.

| Trader type | Profiles | Consumes blocked by explicit `BuyLimitations` | Observed policy |
| --- | ---: | ---: | --- |
| General Trader | 33 | **1** | Eger rejects Consumable and several equipment types; remaining 32 have no authored list |
| Medic | 15 | **0** | Rejects Weapon, Armor, Ammo, Grenade, Attach, MutantLoot; Consumable not listed |
| Technician | 16 | **15** | Fifteen reject Consumable, Ammo, Grenade; DLC1 Bolshak lacks an authored list |
| Bartender | 13 | **0** | Rejects MutantLoot; Consumable not listed |
| Generic NPC Trader | 9 | **0** | Rejects Weapon, Armor, MutantLoot; Consumable not listed |
| **Total** | **86** | **16** | **70** not blocked by this particular flag |

An explicit **Consumable** rejection necessarily applies equally to food, drinks and medicine under the verified source taxonomy. Example: a standard Technician does not selectively reject bandages while accepting bread through this native blacklist.

## 3. Native item-type coverage and source anomalies

The candidate seller inventory has **164 distinct item SIDs** across **253 scope/SID combinations**. After recursively resolving `refkey` references in the two primary `ItemPrototypes.cfg` roots, there were **zero mismatches** between our source stock categories and native `EItemType` for all *resolved* items. Native item types include `Weapon`, `Armor`, `Attach`, `Ammo`, `Artifact`, `Detector`, `Grenade`, `Consumable`, `NightVisionGoggles`, and `Binoculars`.

**Not source-resolved in the extracted two primary ItemPrototypes roots:** `A0919A`, `DutyArmor_3_U1`, and `DutyMask_1`. Their exact native item type cannot be claimed from this specific audit. Keep them explicitly unverified rather than inferring a type from stock categories. In addition, observed vanilla `BuyLimitations` only actually list eight `EItemType` values (Weapon, Armor, Ammo, Grenade, Attach, Consumable, MutantLoot, Artifact). **Detector, NightVisionGoggles and Binoculars were not seen in vanilla denial lists**, so future controls using those names require an explicit BPatch/in-game acceptance test. Broad item-type membership alone does not establish that the engine's buy-denial path supports every possible enum.

## 4. Interaction with pricing and durability: two critical exceptions

- The only observed production `ItemTypeBuyModifiers` entry in the base + DLC1 `TradePrototypes.cfg` files is **DLC1 BolshakTechnician**, `ItemType = EItemType::Info`, `Modifier = 1`. This is a **price modifier**, not a buy-denial or category filter. It does not permit distinguishing Food/Drinks/Medical.
- **SelmaTrader_TradePrototype** and **Trader_Soviet_Rostok_TradePrototype** explicitly author `WeaponSellMinDurability = 0.0f` and `ArmorSellMinDurability = 0.0f`. The existing TCD `trader-min-buy-condition-manifest.csv` records `vanilla = 0.4f` for all 86 profiles and therefore **does not reflect these two authored overrides**. This is a definite **source/manifest baseline mismatch**, not proof of a game runtime bug. A later writer change must reconcile it rather than silently redefining these traders' vanilla condition gates.
- **22** production profiles have one or both durability fields absent from their local original TradeGenerator; `0.4f` in the TCD manifest for those is a baseline/reference assumption rather than a source-authored value. This also requires effective-inheritance/runtime proof where relevant.
- Leave existing BuyModifier, SellModifier, discounts, item category prices, and condition gates **unchanged** while implementing any future `BuyLimitations` controls.

## 5. Proposed controls and acceptance gates

**Safe source-level candidate:** a compact **Trader Buys — by native category** matrix per General Trader, Medic, Technician, Bartender and Generic NPC Trader (e.g. Weapon, Armor, Attachments, Ammo, Grenades, Artifacts, Mutant Parts, all Consumables). Default behavior should preserve every profile's exact existing list. A checkmark cannot be interpreted as overriding a vanilla denial; an opt-in override needs explicit semantics.

**Do not offer as proven:** separate buy acceptance toggles for Food, Drinks and Medical; also do not assume standalone Detectors, Night Vision and Binoculars are supported by the buy-denial path because these enums were not used in vanilla `BuyLimitations`.

Before implementation: (1) re-audit condition baseline and the two authored 0% profiles, (2) construct the exact 86-profile buyer-role/blacklist writer allowlist, (3) test BPatch array addition/removal preserving unrelated types and sibling TradeGenerator fields, (4) run in-game buy acceptance / coupon balance checks at one trader of each type and DLC1, and (5) keep `Trader Sells` inventory independent from `Trader Buys` rules.

## Output references

- [`trader-native-buy-consumable-gates-86.csv`](trader-native-buy-consumable-gates-86.csv): full original-profile row, explicit blacklist and item-type/condition/price exceptions.
- [`trader-consumable-native-type-evidence.csv`](trader-consumable-native-type-evidence.csv): item SID × original base/DLC1 template chain and `SortGroup` evidence.
- [`trader-offered-item-native-types.csv`](trader-offered-item-native-types.csv): all 253 offered item SID/scope pairs, resolved native type or explicit unresolved case.
- [`trader-native-buy-audit-summary.json`](trader-native-buy-audit-summary.json): reproducible counts and archive SHA hashes.
- [`tools/audit_trader_buy_native_types.py`](../../tools/audit_trader_buy_native_types.py): offline fail-closed verifier.
