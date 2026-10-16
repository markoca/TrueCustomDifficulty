# Regular stash sources for all ordinary loot categories (2026-10-16)

**CURRENT 2026-10-16 — implemented after supplied CFG dump:** 991 SmartLoot item rows, 13 original world SmartLoot prototypes and 831 safe physical WorldMap_WP ItemContainer actor references from the user ZIP are now embedded and used for cloned per-actor SmartLoot production. 71 actors with Blueprint/Unique/Quest ItemGenerator references are excluded. The existing Stashes category and medical graphs drive both independent sources. No unique/quest source item rows are modified. Older notes below saying "no game patch" are historical. Runtime validation still required. [Current source/patch details](SMARTLOOT-PRODUCTION-EXTENSION-2026-10-16.md).


**Source:** tracked STALKER 2 v2.0.5 CFG row catalogs. **Deliverable:** `tools/regular-stash-source-inventory.json` plus `tools/audit_regular_stash_source_inventory.py`. **Game patch status: NONE added in this inventory commit.** Current Basic/Standard/Rare ItemGenerator stash tuning remains exactly as before. No unique/quest/single-instance items or actor patches are introduced.

## What we found

Normal stash rewards have two separate candidate production layers:

```text
Physical WorldMap_WP stash / ItemContainer
 ├─ ItemGeneratorSettings → ItemGeneratorPrototypes.cfg
 └─ StashPrototypeSID → StashPrototypes.cfg (SmartLoot)
```

**17 distinct GamePass/CNPP stash core and helper generator SIDs** with 245 exact **local** source rows, including two *nested-generator references* (not collectible items), and **13 world SmartLoot source prototypes** containing 991 exact item rows across 2,170 source-consumer edges (2,143 production-classified). **This is an inventory of source rows, not a count of separate stash containers, spawn probabilities, or total dynamically reachable gear.**

| Ordinary item class | Existing Stashes source control | ItemGenerator local rows | World SmartLoot item rows | Expected missing layer |
| --- | --- | ---: | ---: | --- |
| Ammo (regular calibers) | `loot.stashMixAmmo` | 106 | **944** | Seven ammo SmartLoot families |
| Medical (ordinary, including AntiRad) | `loot.stashMixMedical` + four individual biases | 50 | **15** | Two medical SmartLoot families; separately audited |
| Food (Bread/Sausage/CannedFood) | `loot.stashMixFood` | 34 | **14** | Two mixed food/drink SmartLoot families |
| Drinks (Water/Milk/Energetic/Vodka/Beer) | `loot.stashMixDrinks` | 31 | **15** | The same two mixed food/drink families |
| Grenades (RGD5/F1) | `loot.stashMixGrenades` | 5 | **3** | Two grenade SmartLoot families |
| Coupons (`Money*`) | `loot.stashMixCoupons` | 17 | **0** | GamePass source tiers already supported where redirected; no known SmartLoot coupon class |
| Nested links (noncollectible) | — | 2 | 0 | Review parent/child reachability, never display as an item |

**All numbers are raw/local source-row counts** within the two committed catalogs, not cross-corpus totals for every game asset. No source row in these catalogs is classified as a unique/quest item. The item set includes only recognized ordinary SIDs. The class mapping intentionally refuses unknown item SIDs rather than guessing that a newly appearing one is safe.

## 13 additional world SmartLoot families

The 13 tracked source prototypes are **currently not modified by TrueCustomDifficulty Stashes controls**. Production edge counts originate from `SMARTLOOT-WORLD-AUDIT.md`.

| Category | SmartLoot prototype SID | Local rows | Production consumer edges / all edges |
| --- | --- | ---: | ---: |
| Ammo | `Stash_Ammo_Smart_CommonRare` | 51 | **980 / 985** |
| Ammo | `Stash_Ammo_Smart_Cheap` | 99 | 46 / 46 |
| Ammo | `Stash_AmmoAll_Cheap` | 284 | 19 / 20 |
| Ammo | `Stash_AmmoNATO_Smart` | 151 | 9 / 12 |
| Ammo | `Stash_AmmoSNG_Smart` | 104 | 3 / 3 |
| Ammo | `Stash_AmmoSNG_Smart_MainLoot` | 104 | 2 / 2 |
| Ammo | `Stash_AmmoNATO_Smart_MainLoot` | 151 | 3 / 3 |
| Food + Drinks | `StashFood_Cheap` | 3 | 7 / 11 |
| Food + Drinks | `StashFood_Smart` | 26 | 4 / 5 |
| Grenades | `StashGrenade_Cheap` | 2 | 11 / 12 |
| Grenades | `StashGrenade_Smart` | 1 | 5 / 5 |
| Medical | `StashMedicine_Cheap` | 2 | 18 / 22 |
| Medical | `StashMedicine_Smart` | 13 | **1,036 / 1,044** |

The first two high-impact gaps are **`StashMedicine_Smart`** for previously observed Bandages/Medkits and **`Stash_Ammo_Smart_CommonRare`** for normal ammo. **5 prototypes** have only production consumers in the tracked *direct* edges; this is not a blanket guarantee of safe global replacement because indirect scripted consumers and engine effects require independent validation.

**Important SmartLoot source semantics:** `MinSpawnChance`/`MaxSpawnChance`, `ItemSetCount`, item `Weight`/`Chance` and `MinCount`/`MaxCount` are distinct. Particularly `*_MainLoot` can have original 0/0 spawn endpoints despite active production consumer edges. Do **not** blindly interpret endpoint values as drop percentages, multiply all weights equally and claim increased likelihood, or erase source ranks/caliber slots. A synchronized multiplier of every item in a weighted pool can have *zero relative-selection effect*.

## Other ItemGenerator cores needing coverage review

The **active TCD production-clone trio** is `GamePass_Stash_ItemGenerator_Cheap`, `GamePass_Stash_ItemGenerator_Common_Var1`, `GamePass_Stash_ItemGenerator_Rare`. Its production-specific source redirections remain working.

The additional normal-item source candidates include:

- `GamePass_Stash_ItemGenerator_CheapNoFresh` — food, drinks, ammo, medical, RGD5 and coupons; world-container source.
- `CNPPGamePass_Stash_ItemGenerator_Rare` — medical, ammo, food and RGD5; world-container source.
- `GamePass_Stash_ItemGenerator_Common_Var2` — ammo, food and drinks; container-world source.
- `CNPPGamePass_Stash_ItemGenerator_Cheap` — medical, food, drinks, ammo and grenades, **but no resolved active consumer** in the available audit.
- `Gamepass_Stash_ItemGenerator_Common` — nested references to Common Var1/Var2, not independently collectible items.
- Nine Pistol/Primary/Secondary Ammo Cheap/Common/Rare helper pools — regular ammo rows, but their exact *production external* consumer scope is not established by the included local raw-row catalog.

These are **not** automatically safe global patch roots. The inherited/common pools may reach NPC, quest or scripted contexts. Clone/redirection must be restricted to verified ordinary WorldMap_WP item containers.

## Explicitly excluded / left in their correct domain

No quest rewards, named/unique/one-off items, hand-placed pickups, loot from scripted scenes, special artifacts, unique weapons, plot blueprints or puzzle rewards are to be touched by this general stash feature. `NPC_Ammo_Smart`, `NPC_Medicine_Smart`, `NPC_Water_Smart` belong to NPC corpse bonuses. `StashMedicine_Corpse` and `StashVodka_Corpse` have no resolved world consumer; they are not normal production stash scope. Medical/ammo/food/grenade **breakable destructibles** remain in the separate Loot / Breakable Boxes domain, not silently folded into Stashes.

**Ordinary weapons and armor:** The current Stash Gear control adds standard rank-gated equipment via TCD-owned source rows. The 17 tracked GamePass core-pool table entries themselves do **not** show an independent native regular weapon/armor row. No new hypothetical weapon, armor, attachment, detector, artifact or unique gear pool should be inserted without its own exact source/consumer audit.

## Requirements before changing gameplay output

1. Recover/commit the **actual actor-level world SmartLoot consumer-edge manifest** from the v2.0.5 baseline (`smartloot-world-consumer-edges.csv`, exact source file + actor + StashPrototypeSID path + production/quest/test classification). The existing GitHub repository has summarized documentation but **not this required machine-readable manifest**. Counts alone cannot safely construct per-actor patches.
2. For mixed-context SmartLoot, clone the original `StashPrototypes.cfg` definition(s) in TCD space, preserve rank and item-set semantics, then redirect only allowlisted ordinary production actors. Patch only specifically validated fields from each existing Stash Content category; do not globally rewrite shared quest/test roots.
3. Add source-accurate generated-CFG regression tests covering all 13 prototypes, rank/slot identities, typical selected/unselected settings, old save profiles, and clean output at VANILLA defaults. Verify that a 0x setting actually suppresses item production in-game, not merely changes a weight.
4. For world-only ItemGenerator cores, build verified source-root template + production target manifest before rerouting. Keep all current TCD source/preset IDs unchanged; do **not** add duplicate sliders or redesign the locked Stashes charts.
5. Verify in-game on previously ungenerated *normal ItemContainers*, and identify the actual near-Richter actor SID to distinguish loose scripted pickups from two-layer stash generation.

## What is already added to the repo

The deterministic **regular stash source inventory JSON and its Python regression audit** are now versioned for all six existing standard categories. The JSON explicitly marks all 13 world SmartLoot families as `not_in_tcd_writer` and the three currently patched GamePass source families as `tcd_production_clone_active`, with no unsupported gameplay assertion. **This commit does not alter CFG writer behavior, player presets, runtime loot or WPF UI.** It is the source-backed foundation required for safe subsequent implementation.
