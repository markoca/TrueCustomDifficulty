# Trader Weapon Stock Audit

## Decision

**CLOSED NO UI — mixed availability models.** Do not expose a global `Trader Weapon Stock Amount`, `Trader Weapon Availability`, or `Weapon Stock Quantity` control.

The trader weapon domain does not have one homogeneous field that means "how many weapons traders stock". The same gameplay outcome is assembled from per-item `Chance`, parent-pool link `Chance`, `Weight`-based selection, optional count fields, rank gates, and weapon payload state.

## Exact audited surface

- **94 direct weapon rows across 11 trader-local roots.**
- **34 rows** belong to the reusable trader-only `Trader_T1_Guns`..`Trader_T4_Guns` pools.
- **51/94** rows explicitly use `MinCount=1, MaxCount=1`.
- **43/94** rows have no count pair at all.
- **63** rows use item-level `Chance`; **31** use `Weight`; **0** are direct/implicit rows with neither.
- **34** weapon rows carry an attached-ammo payload.

Full row evidence: [`trader-weapon-stock-surface.csv`](trader-weapon-stock-surface.csv).

## Core T1–T4 pools

The four reusable tier pools are structurally isolated to trader stock, but even these pools are not an `Amount` surface. Every row is one weapon, and availability is expressed mainly through `Chance`.

| Pool | Rows | Item Chance values | Count | Durability | Trader wrapper links | Wrapper Chance values |
| --- | ---: | --- | --- | --- | ---: | --- |
| `Trader_T1_Guns_ItemGenerator` | 8 | 1 | 1/1 | 1.0 | 14 | 0.3 / 0.5 / 1 |
| `Trader_T2_Guns_ItemGenerator` | 10 | 0.3 / 0.5 / 1 | 1/1 | 1.0 | 21 | 0.3 / 0.5 / 0.75 / 1 |
| `Trader_T3_Guns_ItemGenerator` | 10 | 0.3 / 0.5 / 1 | 1/1 | 1.0 | 15 | 0.33 / 0.5 / 1 |
| `Trader_T4_Guns_ItemGenerator` | 6 | 0.3 / 0.7 / 1 | 1/1 | 1.0 | 5 | 1 |

### Why the core pools still cannot be called global weapon stock

Changing only the T1–T4 item `Chance` values would affect reusable tier-pool weapon availability, but it would leave direct trader weapon rows untouched. Changing the parent link `Chance` would instead decide whether an entire tier pool participates for a given trader. Those are two different mechanics.

Several concrete traders bypass or supplement the shared pools with their own direct weapon rows. Separately, `HimzavodTrader_ItemGenerator` and `DynamicTraderItemGenerator` use `Weight`, not `Chance`, so a probability multiplier cannot represent their selection model.

## Per-root semantics

| Root | Weapon rows | Availability model | Explicit 1/1 count | No count pair | Attached-ammo rows |
| --- | ---: | --- | ---: | ---: | ---: |
| `DynamicTraderItemGenerator` | 3 | Weight | 0 | 3 | 3 |
| `HimzavodTrader_ItemGenerator` | 28 | Weight | 0 | 28 | 28 |
| `RC_TraderNPC_ItemGenerator` | 3 | Chance | 3 | 0 | 3 |
| `RostokTraderNato_TradeItemGenerator` | 7 | Chance | 7 | 0 | 0 |
| `RostokTraderSoviet_TradeItemGenerator` | 7 | Chance | 7 | 0 | 0 |
| `TraderTerikon_TradeItemGenerator` | 7 | Chance | 0 | 7 | 0 |
| `TraderZalesie_TradeItemGenerator` | 5 | Chance | 0 | 5 | 0 |
| `Trader_T1_Guns_ItemGenerator` | 8 | Chance | 8 | 0 | 0 |
| `Trader_T2_Guns_ItemGenerator` | 10 | Chance | 10 | 0 | 0 |
| `Trader_T3_Guns_ItemGenerator` | 10 | Chance | 10 | 0 | 0 |
| `Trader_T4_Guns_ItemGenerator` | 6 | Chance | 6 | 0 | 0 |

## Payload fields are independent

Weapon stock entries also carry fields that must not be folded into an availability/amount slider:

- `MinDurability` / `MaxDurability` — condition of stocked/generated weapon.
- `AmmoMinCount` / `AmmoMaxCount` — attached ammunition payload on the weapon entry where present.
- `PlayerRank` — progression/unlock gating.
- parent `SubItemGenerator` `Chance` / `Weight` — whether or how strongly an entire pool participates.
- TradePrototype refresh conditions — when stock refreshes; this is a separate system.

Scaling any of these together would create a synthetic behavior that does not correspond to one vanilla mechanic.

## Candidate mechanisms considered

| Proposed control | Result | Reason |
| --- | --- | --- |
| `Trader Weapon Stock Amount` | **REJECT** | 43/94 weapon rows have no count pair; where count exists it is overwhelmingly fixed 1/1 and raising it would create duplicate copies rather than broader stock. |
| `Trader Weapon Availability` over all weapon rows | **REJECT** | Chance-based and Weight-based roots are semantically different; some rows have neither field. |
| Scale T1–T4 item Chance only | **TECHNICALLY POSSIBLE, NO UI** | Safe trader-only target, but affects only shared core pools and not direct/special trader weapon models. The name would need to explicitly say it is only core tier-pool availability. |
| Scale T1–T4 parent link Chance | **TECHNICALLY POSSIBLE, NO UI** | Changes whole-tier participation per trader rather than per-weapon availability; this is a distinct mechanic and would be confusing as generic stock. |
| Weapon durability / attached ammo | **SEPARATE DOMAIN** | Payload state, not stock amount or availability. |

## Final policy

Preserve vanilla trader weapon-stock semantics. No normal UI control is added.

If we ever expose a trader-weapon control, it must name one exact mechanism and scope, for example **Core Trader Tier-Pool Weapon Availability**, and its description must clearly state that direct and Weight-based trader weapon models are unaffected. It must never be presented as global weapon stock.
