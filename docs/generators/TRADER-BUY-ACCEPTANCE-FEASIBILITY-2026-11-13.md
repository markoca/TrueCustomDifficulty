**Implementation follow-up (2026-11-13):** the native eight-category `Trader Buys` × five-role matrix is now implemented in the source and production writer; [full design, exact manifest and pending in-game gates](TRADER-BUYS-IMPLEMENTATION-2026-11-13.md). This source audit predates implementation and retains its original scope/limitations.

**2026-11-13 original-source follow-up:** The original base + DLC1 CFG 2.0.5 `TradePrototypes.cfg` files and nested seller stock graph have since been independently extracted. See [TRADER-BUY-SELL-ORIGINAL-CFG-2026-11-13.md](TRADER-BUY-SELL-ORIGINAL-CFG-2026-11-13.md), [exact 86-profile buy blacklist CSV](trader-buy-limits-86.csv), and [full sale-item source-path ledger](trader-offers-source-rows.csv). This earlier feasibility report remains useful for the implementation contract, but its statement that no native source lists were extracted is superseded. The direct source audit found 53 explicit lists and 33 profiles with no authored `BuyLimitations` list. No new gameplay control or in-game acceptance proof is implied.

# Trader Buy Acceptance by Role — Feasibility Audit (2026-11-13)

**Verdict: YES, technically possible by `TradePrototypes.cfg` `TradeGenerators/[0]/BuyLimitations`, but NOT yet production mapped or implemented.** This is independent from what a trader offers for sale in `ItemGeneratorPrototypes.cfg`.

## Evidence sources

- Internal TCD: [Economy Mapping](../ECONOMY-MAPPING.md), [Trader Minimum Buy Condition](TRADER-MIN-BUY-CONDITION-AUDIT.md), [86-profile production TradePrototype manifest](../../src/TrueCustomDifficulty/Data/trader-min-buy-condition-manifest.csv), [role census](TRADER-ROLE-SCOPE-AUDIT-2026-11-13.md).
- External **source code example**, not part of TCD: [sdwvit/S2Mods SpecialistMerchants transformTradePrototypes.mts](https://github.com/sdwvit/S2Mods/blob/master/Mods/SpecialistMerchants/transformTradePrototypes.mts). Its published behavior is role-specific buy exclusions using `BuyLimitations`, plus rank/durability adjustments. The example adds exclusions into individual `TradeGenerators`; it demonstrates a plausible native mechanism, **not** exact source parity of TCD's current 2.0.5 dump.
- Independent [FairArtifactPrices technical notes](https://www.nexusmods.com/stalker2heartofchornobyl/mods/2860?tab=description) distinguish `ItemTypeBuyModifiers` (per-type **price**) from `BuyLimitations` (**acceptance**). They also warn that multiple `TradeGenerators` with the same true condition can supersede each other. Verify this for the current game build before writing.

## Native candidate blacklist categories

The external open-source example uses these `EItemType` names:

| Candidate native item type | TCD player label | Notes |
| --- | --- | --- |
| `EItemType::Weapon` | Weapons | Includes all eligible weapon items, not arbitrary individual weapon models |
| `EItemType::Armor` | Armor | Validate suits/helmets mapping in actual item definitions |
| `EItemType::Attach` | Attachments | Mounts, scopes, suppressors, etc. |
| `EItemType::Ammo` | Ammo | Validate launcher ammunition classification |
| `EItemType::Artifact` | Artifacts | Not necessarily same taxonomy as ItemGenerator `Artifact` category |
| `EItemType::Detector` | Detectors | Validate detection devices |
| `EItemType::Grenade` | Grenades | Separate from ammunition |
| `EItemType::Consumable` | Consumables | One broad category: cannot reliably express independent Food/Drinks/Medical buy toggles through this list alone |
| `EItemType::MutantLoot` | Mutant Parts | Separate from consumables and ordinary artifacts |
| `EItemType::NightVisionGoggles` | Night Vision | Verify equipment item taxonomy against original source |
| `EItemType::Other` | Other | May include binoculars and misc items; not safe as a precise binocular-only filter |

Do **not** claim all other enum item types are exhausted; `Info` is another type mentioned by a separate independent source and may need protection. Exact runtime type membership must be checked against same-version item prototypes and the game's effective behavior.

`BuyLimitations` is a **denial list**: for user-facing “Buys Weapons” ON/OFF the implementation needs to preserve all existing exclusions, insert/remove only the relevant `EItemType`, and treat OFF as explicit exclusion; ON means restore original vanilla eligibility, **not** forcibly override a vanilla blacklist. It is not a stock-selection `Chance` or quantity modifier.

## Integration contract with existing controls

| Mechanic | Current TCD | Invariant |
| --- | --- | --- |
| `TradeGenerators/[0]/WeaponSellMinDurability` | `economy.traderMinBuyCondition` | Changing category acceptance must **never** alter condition thresholds |
| `TradeGenerators/[0]/ArmorSellMinDurability` | `economy.traderMinBuyCondition` | Preserve unchanged fields and merge sibling BPatch changes safely |
| `BuyModifier`, `SellModifier` | Vanilla trade economics + TCD multipliers | No accidental price writes |
| `ItemTypeBuyModifiers` | Not exposed | Per-type **price**, not permission; do not use as acceptance |
| `BuyLimitations` | New candidate only | Preserve existing entries and original order, avoid list-replacement conflicts |
| `ItemGeneratorPrototypes.cfg` | Separate trader category SALE-STOCK proposal | Never change what a shop sells as a side effect of purchase eligibility |

The user-facing model could be a compact **Trader Buys** matrix: rows = General Trader, Medic, Technician, Bartender, Generic NPC Trader; columns = Weapons, Armor, Attachments, Ammo, Artifacts, Detectors, Grenades, Consumables, Mutant Parts, Night Vision, Other. Tooltips should not pretend separate food/drink/medical permissions exist natively.

## Major unsolved gates

1. **TradePrototype identity/coverage:** 86 existing minimum-buy-condition production profiles (68 base + 18 DLC1), 83 stock-consumer edges, 78 mapped merchant-facing stock roots. These counts represent different concepts. Build a 1:1 production **TradePrototype SID → actual seller role** allowlist. Do not extrapolate all role mappings from 96 item generators. Audit extra profiles including BerezaDLCTrader and bartender profiles not in the stock topology; preserve DLC story-stage profiles and template exclusions.
2. **Current native `BuyLimitations` inventory:** physically enumerate each production `TradePrototype` and effective inherited/explicit exclusion list from the matching original CFG dump. The existing min-durability CSV lists only durability fields; it **does not** prove exact `BuyLimitations` contents. Avoid synthesizing a replacement array based solely on a mod's sample.
3. **Modify lists safely:** validate indexed arrays, `{bpatch}` merge, adding/removing one category without losing other lists, and whether the native node supports targeted list element removals. Multiple unrelated TCD changes to `TradeGenerators/[0]` must merge into one correct output.
4. **Cross-role validity:** preserve explicit blacklist by seller type; special NPC templates, `DynamicTraderItemGenerator`, `RC_TraderNPC`, 2.0.5 DLC quest/stage roles and unnamed merchants need narrow decisions before user-facing claims.
5. **In-game acceptance:** test barter with weapons, suits/helmets, attachments, ammo, artifacts, mutant parts, and consumables for at least one merchant of each real role. Include rank, reputation, weapon condition and DLC1. A seller’s coupon balance may block a deal even when `BuyLimitations` allows the item.

## Preliminary answer

**We can likely control which merchant type BUYS broad native item types, and this is a more direct/complete engine mechanism than globally editing each seller's STOCK.** Do not implement until the exact original TradePrototype BuyLimitations source and consumer SID coverage are verified. Native consumables are grouped, so do not promise granular buy-only controls for Food vs Drinks vs Medical without additional evidence.

**No implementation in this audit**: no new parameters, no presets, no UI changes, no CFG writer edits, no test claims for the game runtime.
