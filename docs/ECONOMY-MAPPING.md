# Economy Mapping

Source of truth for this pass: runtime STALKER 2 CFG dump 2.0.5.

The Economy tab deliberately separates difficulty-profile multipliers from global CoreVariables values. It does not rewrite individual trader or upgrade prototypes unless a future aggregate transform is explicitly designed and tested.

## 1. Starting Coupons

### Starting Coupons

- File: `GameData/CoreVariables.cfg`
- Field: `PlayerStartingMoney`
- Vanilla: `0`

This is a player-initialization value. Changing it is intended for a new game; it is not a recurring coupon source and should not be expected to overwrite the coupon balance on an existing save.

The UI changes this value in 10-coupon increments. This is a presentation/input step only; the underlying CFG field remains the raw integer-like coupon value.

## 2. Trading

### Trader Buy-From-You Pricing

- File: `GameData/DifficultyPrototypes.cfg`
- Field: `EconomyDifficulty.BuyCondition`

| Difficulty | Value |
|---|---:|
| Novice | 1.00 |
| Stalker | 1.00 |
| Veteran | 0.65 |
| Master | 0.55 |

The CFG confirms the field and vanilla progression. This is the trader-buying-from-you side of the difficulty profile. It does not expose the complete final trader-price equation, while `TradePrototypes.cfg` separately contains trader-specific `BuyModifier` entries and discounts. For that reason the UI does not claim that 0.55 literally equals a final 55% payout.


### Trader Minimum Buy Condition

- File: `GameData/TradePrototypes.cfg` and DLC1 `DLCGameData/DLC1/TradePrototypes.cfg`
- Fields: `TradeGenerators/[0]/WeaponSellMinDurability` and `ArmorSellMinDurability`
- Vanilla threshold: `0.40` for both weapon and armor sell-condition gates
- UI: **Trader Minimum Buy Condition**, 0-100% in 1% steps

This field is the condition-acceptance gate, not a price multiplier. Setting it to 0% allows mapped traders to accept completely broken weapons and armor. The price of that damaged gear is still determined separately by durability-based item value, difficulty `BuyCondition`, trader-specific modifiers and other vanilla economy rules.

The current production allowlist contains 86 mapped production trade profiles: 68 base-game and 18 DLC1. Template/test profiles are excluded, and DLC1 targets are emitted only when DLC1 is installed.

### Trader Sell-To-You Pricing

- File: `GameData/DifficultyPrototypes.cfg`
- Field: `EconomyDifficulty.SellCondition`

| Difficulty | Value |
|---|---:|
| Novice | 1.00 |
| Stalker | 1.00 |
| Veteran | 1.25 |
| Master | 1.65 |

This is the trader-selling-to-you side of the difficulty profile. The same caution applies: the field is confirmed, while trader-specific `SellModifier` and discount layers remain separate, so 1.65 is not claimed to be the complete final shop-price formula.

### Item Value at 0% Durability

This is the minimum share of an item's base coupon value retained when condition reaches 0%.

- File: `GameData/CoreVariables.cfg`
- Field: `ItemCostMinPercent`
- Vanilla: `0.10`

The runtime CFG comment defines this as the percentage of base price retained by an item at zero durability. With vanilla `0.10`, the same comment gives:

- durability 0.0 -> 10% base value
- durability 0.5 -> 55% base value
- durability 1.0 -> 100% base value

The control therefore changes how strongly poor condition devalues an item before the other trade layers are applied.

## 3. Item Price Scalers

The following fields are real members of `EconomyDifficulty`:

| Field | Novice | Stalker | Veteran | Master |
|---|---:|---:|---:|---:|
| `Consumable_Cost` | 1.00 | 1.00 | 1.00 | 1.00 |
| `Ammo_Cost` | 1.00 | 1.00 | 1.00 | 1.00 |
| `Armor_Cost` | 1.00 | 1.00 | 1.00 | 1.00 |
| `Weapon_Cost` | 1.00 | 1.00 | 1.00 | 1.00 |
| `Artifact_Cost` | 1.00 | 1.00 | 1.00 | 1.00 |

Vanilla does not use these five fields to differentiate the four difficulty presets. They remain useful custom-difficulty controls because the fields already exist in the difficulty profile. The UI therefore shows a single shared `VANILLA` marker at 100%.

These fields are kept separate from the trader Buy/Sell condition scalars. The tool must not silently collapse the layers into one price percentage.

## 4. Services

### Upgrade Cost

- File: `GameData/DifficultyPrototypes.cfg`
- Field: `EconomyDifficulty.Upgrade_Cost`

| Difficulty | Value |
|---|---:|
| Novice | 1.00 |
| Stalker | 1.00 |
| Veteran | 1.25 |
| Master | 1.50 |

Individual upgrade prototypes retain their own `BaseCost`; the difficulty scalar changes the difficulty layer rather than rewriting every upgrade entry.

### Base Repair Price

- File: `GameData/CoreVariables.cfg`
- Field: `BaseRepairCostModifier`
- Vanilla: `0.70`

This is the single global repair-price control exposed in the normal UI. Higher values make repairs more expensive overall; lower values make them cheaper.

### Audit-only difficulty repair scalar

- File: `GameData/DifficultyPrototypes.cfg`
- Field: `EconomyDifficulty.Repair_Cost`
- Vanilla: `1.00` on Novice, Stalker, Veteran and Master

The field is real, but vanilla never uses it to distinguish difficulty presets. Exposing it beside `BaseRepairCostModifier` gives the user a second global repair-price multiplier for the same practical tuning goal. It therefore remains mapped for audit/writer validation but is intentionally hidden from the normal UI.

### Fast Travel Cost

- File: `GameData/FastTravelPrototypes.cfg`
- Physical roots: `[0]` through `[14]` (each root carries its guide `SID` as data)
- Path: all `Locations[*].RequiredMoney` values across those roots
- UI vanilla: `100%`

Runtime CFG contains **183** `RequiredMoney` assignments: **182 non-zero paid routes** plus one zero-valued template entry. The writer addresses the physical indexed roots directly; guide SIDs are retained only as audit metadata. The UI uses a single aggregate multiplier and the future writer applies `scaleNonZero`, preserving all relative route-price differences and the explicit zero.

Examples of vanilla route prices range from 500 to 6000 coupons. At 50%, every paid route is halved; at 200%, every paid route is doubled. At 0%, the mapped paid-route prices become zero.

Rank, reputation and global-variable discount coefficients remain separate vanilla layers and are not changed by this control.

## 5. Repair per Reputation

`CoreVariables.cfg` contains `ReputationRepairCostModifiers`:

| Relation | Vanilla modifier |
|---|---:|
| Enemy | 2.00 |
| Disaffection | 1.50 |
| Neutral | 1.00 |
| Friend | 0.75 |

These controls modify only their own relation tier. They do not change the player's current faction relationship itself.

The normal UI exposes two meaningful repair-price layers:

1. global **Base Repair Price** (`BaseRepairCostModifier`),
2. relation-specific repair modifier.

`EconomyDifficulty.Repair_Cost` remains documented as an audit-only third layer at its vanilla `1.00` baseline. The CFG dump confirms these fields exist, but this document does not invent an exact engine multiplication order where runtime code is not present.

## 6. Coupon Rewards

### Main quest coupon reward

- Field: `EconomyDifficulty.Reward_MainLine_Money`

### Side quest coupon reward

- Field: `EconomyDifficulty.Reward_SideLine_Money`

Both use the same vanilla values:

| Difficulty | Value |
|---|---:|
| Novice | 1.35 |
| Stalker | 1.00 |
| Veteran | 0.75 |
| Master | 0.55 |

They remain independent controls so a profile can change main-line and side-line income separately.

## 7. Intentionally Deferred

### Trader stock abundance

`TradePrototypes.cfg` references item generators and trader-specific stock logic. There is no single confirmed global abundance scalar. A safe control would require an explicit aggregate transform design over the relevant generators.

Status: `aggregate_design_required`.

### Trader refresh interval

Trader prototypes use different refresh-condition SIDs, including 8-hour, 1-day and 7-day conditions. Converting those discrete per-trader rules into one global slider would be an aggregate rewrite, not a direct CFG scalar.

Status: deferred until a safe mapping is designed.

### Trader-specific BuyModifier / SellModifier

Individual traders expose their own modifiers, limitations and discounts. They are deliberately not rewritten by the global Economy sliders in this pass. Difficulty `BuyCondition` / `SellCondition` remain the selected difficulty-profile layer.


## Mutant Parts Cost

Runtime CFG has no single mutant-part price multiplier. The UI's global `Mutant Parts Cost` control is an aggregate transform over explicit `Cost` fields for the 14 base-game mutant-part prototypes plus the DLC1 Object68 part when DLC1 is present. Relative trophy values are preserved. Trader pricing remains a separate layer. The 14 base prototypes were runtime-verified directly from the installed game `pakchunk0-Windows.pak`; examples include `BlinddogLoot = 1000`, `CatLoot = 8000`, `ChimeraLoot = 18000`, `PseudodogLoot = 2500`, and `PseudogiantLoot = 17000`.

## 2026-10-09 — Player-facing Economy presentation

The current GUI groups **22 existing source-bound settings** into six full-width sections: COUPONS & REWARDS (3), TRADING (4), TRADER STOCK (2), ITEM VALUE LAYERS (6), SERVICE PRICES (3), and REPAIR PRICE BY REPUTATION (4). All source variables above keep their own CFG semantics, native source ticks, vanilla difficulty references and preset storage. This is presentation-only; the global `EconomyDifficulty.Repair_Cost` field continues to be audit-only, without a second redundant editor.

Starting Coupons and the two distinct quest reward multipliers share one compact board but are **not added together**; starting coupons still apply only when a new game initializes. The five category-cost scalers share visual controls with Mutant Parts but remain logically separate from the per-item `Cost` transformations. Fast travel, upgrades and Base Repair Price each edit an independent source; the last remains distinct from the four repair-reputation modifiers. Stock quantities do not rewrite restock, rank or membership. Do not infer a final trader buy/sell formula from the editable raw scalers.

The grey reference tick reflects the chosen Base difficulty; values are highlighted only when actually modified. Historical source audits remain valid: this change adds no new CFG edit point and does not resurrect disallowed aggregate trader-stock controls.
