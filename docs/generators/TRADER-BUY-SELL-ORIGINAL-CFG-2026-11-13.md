# True Custom Difficulty — Trader Buy/Sell Source Dossier

**Scope:** S.T.A.L.K.E.R. 2 original decoded CFG 2.0.5 + DLC1; extracted 2026-11-13.

**Source archives (SHA-256, exact match to prior TCD audit):**

- `STALKER2-GameData-CFG(2)(2).zip`: `7610b16218310c7345796ff26b1554c73e3d2ca775f6a50108a7406cb7f33da1`
- `STALKER2-DLC1-CFG(2)(2).zip`: `3c5cb53f1769619b510ae3780142d92639c083a4a755a8717a267710c3c849e6`

## Contents

- `trader-buy-limits-86.csv` — all 86 production `TradePrototype` SIDs (68 base + 18 DLC1), exact native `BuyLimitations` per `TradeGenerators[0]`, file and root line, purchase exclusion per observed `EItemType`, buy/sell modifiers, durability thresholds, money and refresh. `NOT_BLOCKED_BY_BUYLIMITATIONS` does **not** mean confirmed merchant acceptance of any specific item.
- `trader-overview-86.csv` — same 86 profiles with unique **reachable candidate** item SID counts for 13 item categories. `sell_XXX=0` means none observed through current analyzed `ItemGenerator` graph, not an engine-wide promise.
- `trader-offer-sids-86.csv` — one row per trader, listing **each exact reachable item SID** by category for quick inspection. `Chance=0` leaves are excluded; all other candidates are potential stock, not guaranteed current stock.
- `trader-offers-source-rows.csv` — 2,939 physical reachable source paths to 164 distinct item SIDs. Each entry has trader, generator/leaf scope and line, actual item SID, generator nesting, player rank eligibility, `Chance`/`Weight`, vanilla min/max count, and detection of zero-chance entries. Item availability is not guaranteed per visit.
- `trader-summary.json` — original hashes, source counts, six buy limitation patterns, native-category totals, warnings.
- `extract_trader_trade_inventory.py` — rerun the exact offline extraction against original decoded archives.

## Results: purchase exclusion patterns

The source has 92 TradePrototype roots (74 base, 18 DLC1). Six base root templates/tests/nonproduction records are excluded, yielding 86 production profiles. Across these: 53 have an explicit `BuyLimitations` list, 33 omit it.

| Production profiles | Exact vanilla blacklist (`EItemType::` prefix omitted) |
| ---: | --- |
| 33 | *No explicit list* |
| 13 | `MutantLoot` (bartenders) |
| 15 | `Weapon; Armor; Ammo; Grenade; Attach; MutantLoot` (medics) |
| 9 | `Weapon; Armor; MutantLoot` (generic NPC profiles) |
| 15 | `Consumable; Ammo; Grenade` (technicians, except Bolshak) |
| 1 | `Weapon; Armor; Ammo; Grenade; Attach; Consumable; Artifact` (`EgerTrader_TradePrototype`) |

`BolshakTechnician_TradePrototype` from DLC1 has no explicit blacklist (unlike the other 15 technicians). A general merchant's sale inventory and purchase blacklist are independent: `EgerTrader_TradePrototype` sells 40 different reachable items, including weapons and armor, even though it refuses to buy those categories from the player.

**Important:** `BuyLimitations` is an explicit rejection list, not a positive allowlist. A missing type does not prove every item of that kind is sellable by the player; durability, item tradeability, conditions, coupons and other engine rules can still apply.

## Source-verification checklist and notable contrasts

- The exact 86 `(scope, TradePrototype SID)` pairs extracted from the original files match **all 86** entries in TCD's pre-existing `trader-min-buy-condition-manifest.csv` (sorted FNV-1a64 `facc1d4194fc359c`, 68 base + 18 DLC1). None is missing.
- All production trade profiles have exactly one authored `TradeGenerators/[0]` rule in these original CFGs. The only observed multi-rule prototype is excluded test-only `TradeTest`.
- **General Trader vs Generic NPC:** all 9 `GeneralNPC_TradePrototype*` profiles blacklist Weapon, Armor and MutantLoot. The separately named `TraderNPC` and `RC_TraderNPC` have no such explicit blacklist.
- **Rostok:** `Bartender_Rostok_TradePrototype` has eight reachable weapon SIDs (its own T1 weapons link), despite bartender type. Bartender profiles blacklist only `MutantLoot` on the buying side.
- **Eger:** has 40 distinct reachable sell candidates, including weapons and armor, but its buying side explicitly refuses Weapon, Armor, Ammo, Grenade, Attach, Consumable and Artifact.
- **Bolshak DLC1 technician:** does not author `BuyLimitations`, despite the other 15 technicians blacklisting Consumable, Ammo and Grenade. His reachable sale pool includes weapons, ammo and medical supplies.
- These results are from original CFG **2.0.5**, not independently checked for 2.1 or a later installed game version.

## Stock caveats

- `PlayerRank` constraints are followed through nested generators. Rank annotations indicate which candidate branches are source-eligible, not which items will definitely generate.
- `Chance`, `Weight`, possible same-category generation, source scopes and vendor refresh still matter. Unresolved generator references: **zero** in this extraction.
- DLC trade profiles frequently link to base-game ItemGenerators. They remain included in the outputs with their proper DLC TradePrototype SID.
- `Chance=0` source entries stay in the detailed CSV with `direct_entry_state=ZERO_CHANCE`. They are **not** treated as available when computing `sell_...` category counts.
- Raw `EItemGenerationCategory::Artifact` also houses grenades, binoculars and `AVOG`, so we classify concrete item SIDs rather than use native generator categories blindly.
- The output is an original-source extraction, **not** confirmation of the mod's BPatch output or in-game regeneration behavior.

## Reproduce

```bash
python3 tools/extract_trader_trade_inventory.py \
  --base-zip '/path/to/STALKER2-GameData-CFG(2)(2).zip' \
  --dlc1-zip '/path/to/STALKER2-DLC1-CFG(2)(2).zip' \
  --out audit/trader-full-buy-sell
```

No game, user presets, or repository gameplay code was modified by this audit.
