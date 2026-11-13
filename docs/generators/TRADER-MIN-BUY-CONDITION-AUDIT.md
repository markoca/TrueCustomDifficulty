# Trader Minimum Buy Condition Audit

## Decision

**IMPLEMENTED / RUNTIME TEST REQUIRED** as `economy.traderMinBuyCondition`.

The trade condition gate is separate from trader pricing and stock generation.

## Vanilla fields

`TradePrototypes.cfg` exposes the sell-condition threshold on the first trade generator:

- `TradeGenerators/[0]/WeaponSellMinDurability`
- `TradeGenerators/[0]/ArmorSellMinDurability`

The **global slider reference** is **0.40 / 40%**. This is **not** the source-authored vanilla value for every trader: `SelmaTrader_TradePrototype` and `Trader_Soviet_Rostok_TradePrototype` explicitly author **0.0f**, 62 other production profiles explicitly author **0.4f**, and 22 profiles have neither gate field locally authored.

`BaseTraderNPC_Template` records 0.4f for both, but does not override the two source-authored 0% dealer values. The production manifest now uses `vanilla=0.0f` for those exact two rows and keeps 0.4f as the reference for all other profiles; see [original BPatch/durability audit](TRADER-BUY-BPATCH-CONTRACT-2026-11-13.md).

## Production profile allowlist

The current extracted `TradePrototypes` consumer graph contains 90 unique trade-prototype consumer SIDs. Four non-production/template targets are excluded:

- `BaseTraderNPC_Template`
- `AllTraderNPC`
- `BasicTrader`
- `TradeTest`

The production allowlist therefore contains:

- **68 base-game profiles**
- **18 DLC1 profiles**
- **86 total production trade profiles**

The exact list is `trader-min-buy-condition-manifest.csv`.

## UI semantics

`Trader Minimum Buy Condition` is an absolute 0-100% slider.

- **0%** — mapped traders can accept completely broken weapons and armor.
- **40%** — neutral slider reference; no patch is emitted while unchanged, preserving the two original 0%-condition traders.
- **100%** — equipment must be at full condition to pass this gate.

A deliberately changed slider is an **absolute threshold**: it writes the selected value to both weapon and armor fields for all mapped traders, including the two vanilla 0% exceptions.

## Isolation

The writer changes only the two minimum durability gate fields. It does **not** change:

- `BuyModifier` / `SellModifier`
- difficulty `BuyCondition` / `SellCondition`
- item value / durability-price curve
- accepted item categories
- trader inventory or generators
- trader money
- refresh conditions
- reputation/rank discounts
- repair rules

Therefore a 0%-condition item may become sellable while still being worth very little or zero coupons depending on the existing durability-value and pricing layers.

## DLC handling

DLC1 profiles are emitted only when DLC1 is detected. The manifest contains all **18 DLC1 trade-prototype consumers** resolved by the current source graph, including the Bereza and Bolshak profiles.

## Runtime gate

The field names, vanilla reference and profile routing are source-grounded. Runtime verification is still required to confirm:

1. 0% permits red / 0%-condition equipment at representative base-game traders;
2. DLC1 traders obey the same generated threshold;
3. price calculation remains unchanged apart from the item becoming eligible for sale.
