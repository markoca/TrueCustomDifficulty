# Economy Control Deep Audit — v0.2.48

This release re-audits every normal **Economy** control from the player-facing UI through the locked catalog and writer baseline to concrete CFG edit points. No new Economy control is promoted and no existing control is removed.

## Result

- **20** normal Economy controls.
- **17** statically confirmed.
- **3** remain `runtime_test_required`: `economy.buyCondition`, `economy.sellCondition`, and `economy.medicStock`.
- **341** concrete writer edit points across the 20 controls.
- **0** invalid or partial normal-UI Economy controls.
- `economy.repairDifficultyCost` remains audit-only (`exposeInUi=false`) because exposing it beside the global Base Repair Price would duplicate the same practical user goal.

## Control-by-control contract

| Control | Status | Writer contract | Audit result |
|---|---|---|---|
| Starting Coupons | confirmed | 1 × `PlayerStartingMoney` `set` | 1:1; new-game initialization only |
| Trader Buy-From-You Pricing | runtime test required | 4 × `EconomyDifficulty.BuyCondition` `set` | Vanilla values 1.00 / 1.00 / 0.65 / 0.55 match writer source exactly; final trader-price formula remains runtime-gated |
| Trader Sell-To-You Pricing | runtime test required | 4 × `EconomyDifficulty.SellCondition` `set` | Vanilla values 1.00 / 1.00 / 1.25 / 1.65 match writer source exactly; final trader-price formula remains runtime-gated |
| Trader Minimum Buy Condition | runtime test required | special writer: 86 profiles × weapon+armor durability gate | Direct `WeaponSellMinDurability` / `ArmorSellMinDurability`; vanilla 40%, 0% accepts broken gear |
| Item Value at 0% Durability | confirmed | 1 × `ItemCostMinPercent` `set` | Direct zero-condition value floor |
| Medic Stock Quantity | runtime test required | 96 × `MinCount/MaxCount` `scaleIntegerHalfUp` | Exactly 48 audited medic rows; Chance/rank/refresh untouched |
| Consumable Cost | confirmed | 4 × `Consumable_Cost` `set` | Direct difficulty-profile category scalar |
| Ammo Cost | confirmed | 4 × `Ammo_Cost` `set` | Direct difficulty-profile category scalar |
| Armor Cost | confirmed | 4 × `Armor_Cost` `set` | Direct difficulty-profile category scalar |
| Weapon Cost | confirmed | 4 × `Weapon_Cost` `set` | Direct difficulty-profile category scalar |
| Artifact Cost | confirmed | 4 × `Artifact_Cost` `set` | Direct difficulty-profile category scalar |
| Mutant Parts Cost | confirmed | 15 × item `Cost` `scale` | 14 base-game parts + DLC1 Object68; relative values preserved |
| Fast Travel Cost | confirmed | 183 × `RequiredMoney` `scaleNonZero` | 182 paid routes + one explicit zero; zero stays zero |
| Upgrade Cost | confirmed | 4 × `Upgrade_Cost` `set` | Direct difficulty-profile scalar |
| Base Repair Price | confirmed | 1 × `BaseRepairCostModifier` `set` | Single normal-UI global repair-price layer |
| Hostile Repair Price | confirmed | 1 × Enemy reputation modifier `set` | Direct relation-tier modifier |
| Disliked Repair Price | confirmed | 1 × Disaffection reputation modifier `set` | Direct relation-tier modifier |
| Neutral Repair Price | confirmed | 1 × Neutral reputation modifier `set` | Direct relation-tier modifier |
| Friendly Repair Price | confirmed | 1 × Friend reputation modifier `set` | Direct relation-tier modifier |
| Main Quest Coupon Reward | confirmed | 4 × `Reward_MainLine_Money` `set` | Direct difficulty-profile reward scalar |
| Side Quest Coupon Reward | confirmed | 4 × `Reward_SideLine_Money` `set` | Direct difficulty-profile reward scalar |

## Corrections in v0.2.48

The raw engine names `BuyCondition` and `SellCondition` are from the trader's side and the older labels **Trader Buy Pricing** / **Trader Sell Pricing** could be read in either direction. The player-facing labels are therefore clarified without changing IDs, presets, writer targets, ranges, or gameplay output:

- `economy.buyCondition` → **Trader Buy-From-You Pricing**.
- `economy.sellCondition` → **Trader Sell-To-You Pricing**.

Both controls remain runtime-gated because the fields and vanilla progression are proven, while the complete final shop-price equation is not claimed.

## Linked repair-price invariant

The existing linked constraint is value-based and remains correct:

`Friendly <= Neutral <= Disliked <= Hostile`

Vanilla values are `0.75 <= 1.00 <= 1.50 <= 2.00`. The actively moved slider keeps priority and adjacent linked values move only as required by the common slider-constraint coordinator.

## Deliberate NO-UI boundary

`EconomyDifficulty.Repair_Cost` remains mapped and writer-audited at vanilla `1.00` on every difficulty, but hidden from the normal UI. `BaseRepairCostModifier` is the single global repair-price control presented to the player; exposing both would create two overlapping global repair controls without adding a distinct player-facing tuning goal.

Trader stock aggregate controls also remain outside this audit except for the already-isolated **Medic Stock Quantity** surface. The closed trader ammo/weapon/attachment/global-stock decisions are unchanged.
