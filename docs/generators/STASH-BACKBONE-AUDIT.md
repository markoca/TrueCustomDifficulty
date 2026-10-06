# World Stash Backbone Deep Audit

This audit narrows the generator map to the three vanilla stash tiers that dominate ordinary world stashes: **Cheap**, **Medium**, and **Expensive**. It is a source-structure audit, not telemetry: physical/reference footprint tells us how broadly a generator is wired, not how many times a particular player will open it.

## 1. The important topology

Each physical stash can carry rank-specific generator settings. The four visible tier wrappers are pure inherited wrappers around one shared core pool:

```text
Stash_Cheap_Newbie / Experienced / Veteran / Master
    -> GamePass_Stash_ItemGenerator_Cheap

Stash_Medium_Newbie / Experienced / Veteran / Master
    -> GamePass_Stash_ItemGenerator_Common_Var1

Stash_Expensive_Newbie / Experienced / Veteran / Master
    -> GamePass_Stash_ItemGenerator_Rare
```

The wrapper rank and the generator's own `PlayerRank` gates are separate pieces of data. Game difficulty is another independent axis (`Easy -> Novice`, `Medium -> Stalker`, `Hard -> Veteran`, internal `Stalker -> Master`).

**Writer rule for future controls:** for stash-only tuning, prefer/materialize the `Stash_<tier>_<rank>` wrappers. Do **not** edit the shared `GamePass_*` parent blindly. `GamePass_Stash_ItemGenerator_Rare`, for example, is also reachable from a standard-human NPC path, while the rank wrappers are the stash interception surface already used by the existing **Expensive Stash Coupon Chance** writer.

## 2. How broad are the three tiers?

| Tier | Shared parent | Main-world physical sources in mapped wrapper set | Local parent rows | Unique offered SIDs |
|---|---|---:|---:|---:|
| **Cheap** | `GamePass_Stash_ItemGenerator_Cheap` | 211 | 52 | 16 |
| **Medium** | `GamePass_Stash_ItemGenerator_Common_Var1` | 35 | 39 | 15 |
| **Expensive** | `GamePass_Stash_ItemGenerator_Rare` | 17 | 42 | 21 |

Cheap is by far the most common ordinary stash tier in the mapped world. Medium and Expensive are progressively less common, but use broader/higher-grade ammo and reward pools.

## 3. Cheap stash pool

Reachable vanilla item SIDs: **A012D, A918D, A919D, Bandage, Bread, CannedFood, Energetic, GrenadeRGD5, Medkit, Milk, MoneyCommon, MoneyEpic, MoneyRare, Sausage, Vodka, Water**.

Important structure:

- base ammo is a weighted choice among `A918D`, `A919D`, and `A012D`; counts differ by ammo family;
- food/medical logic changes by difficulty and uses **both** `Chance` and `Weight`;
- rank progression introduces coupon tiers through **Weight=1.77**, not through a clean coupon `Chance`;
- `GrenadeRGD5` is a narrow explicit `Chance=0.10`, count 1 row;
- therefore a generic **Cheap Stash Loot %** or **Cheap Stash Coupon Chance** would be semantically false.

| Block | Category | Gate | Item SID | Selection field | Count | CFG line |
|---|---|---|---|---|---|---:|
| `[0]` | Consumable | Novice, Stalker | `Bandage` | Chance 0.4 | 1–2 | `64312` |
| `[0]` | Consumable | Novice, Stalker | `Bread` | Chance 0 | 1–2 | `64318` |
| `[0]` | Consumable | Novice, Stalker | `Sausage` | Chance 0 | 1–2 | `64324` |
| `[0]` | Consumable | Novice, Stalker | `Vodka` | Chance 0.5 | 1–1 | `64330` |
| `[0]` | Consumable | Novice, Stalker | `Medkit` | Chance 0.4 | 1–1 | `64336` |
| `[1]` | Consumable | Veteran | `Milk` | Weight 1 | 1–1 | `64349` |
| `[1]` | Consumable | Veteran | `Bread` | Weight 5 | 1–1 | `64355` |
| `[1]` | Consumable | Veteran | `Sausage` | Weight 5 | 1–1 | `64361` |
| `[1]` | Consumable | Veteran | `CannedFood` | Weight 3 | 1–1 | `64367` |
| `[1]` | Consumable | Veteran | `Bandage` | Weight 2 | 1–1 | `64373` |
| `[2]` | Consumable | All difficulties / ranks | `Energetic` | Weight 1 | 1–1 | `64385` |
| `[2]` | Consumable | All difficulties / ranks | `Water` | Weight 4 | 1–1 | `64391` |
| `[3]` | Ammo | All difficulties / ranks | `A918D` | Weight 40 | 2–3 | `64403` |
| `[3]` | Ammo | All difficulties / ranks | `A919D` | Weight 30 | 5–7 | `64409` |
| `[3]` | Ammo | All difficulties / ranks | `A012D` | Weight 30 | 2–3 | `64415` |
| `[4]` | Consumable | Novice | `Bandage` | Chance 1 | 2–4 | `64428` |
| `[4]` | Consumable | Novice | `Vodka` | Chance 1 | 0–1 | `64434` |
| `[4]` | Consumable | Novice | `Medkit` | Chance 1 | 1–2 | `64440` |
| `[5]` | Artifact | All difficulties / ranks | `GrenadeRGD5` | Chance 0.1 | 1–1 | `64451` |
| `[6]` | Consumable | Veteran | `Bandage` | Chance 0.2 | 1–2 | `64464` |
| `[6]` | Consumable | Veteran | `Bread` | Chance 0 | 1–2 | `64470` |
| `[6]` | Consumable | Veteran | `Sausage` | Chance 0 | 1–2 | `64476` |
| `[6]` | Consumable | Veteran | `Vodka` | Chance 0.3 | 1–1 | `64482` |
| `[6]` | Consumable | Veteran | `Medkit` | Chance 0.3 | 1–1 | `64488` |
| `[7]` | Consumable | Novice, Stalker + Experienced | `Milk` | Weight 1 | 1–1 | `64502` |
| `[7]` | Consumable | Novice, Stalker + Experienced | `Bread` | Weight 5 | 1–1 | `64508` |
| `[7]` | Consumable | Novice, Stalker + Experienced | `Sausage` | Weight 5 | 1–1 | `64514` |
| `[7]` | Consumable | Novice, Stalker + Experienced | `CannedFood` | Weight 3 | 1–1 | `64520` |
| `[7]` | Consumable | Novice, Stalker + Experienced | `Bandage` | Weight 2 | 1–1 | `64526` |
| `[7]` | Consumable | Novice, Stalker + Experienced | `MoneyCommon` | Weight 1.77f | 1–1 | `64532` |
| `[8]` | Consumable | Novice, Stalker + Veteran | `Milk` | Weight 1 | 1–1 | `64546` |
| `[8]` | Consumable | Novice, Stalker + Veteran | `Bread` | Weight 5 | 1–1 | `64552` |
| `[8]` | Consumable | Novice, Stalker + Veteran | `Sausage` | Weight 5 | 1–1 | `64558` |
| `[8]` | Consumable | Novice, Stalker + Veteran | `CannedFood` | Weight 3 | 1–1 | `64564` |
| `[8]` | Consumable | Novice, Stalker + Veteran | `Bandage` | Weight 2 | 1–1 | `64570` |
| `[8]` | Consumable | Novice, Stalker + Veteran | `MoneyRare` | Weight 1.77f | 1–1 | `64576` |
| `[9]` | Consumable | Novice, Stalker + Master | `Milk` | Weight 1 | 1–1 | `64590` |
| `[9]` | Consumable | Novice, Stalker + Master | `Bread` | Weight 5 | 1–1 | `64596` |
| `[9]` | Consumable | Novice, Stalker + Master | `Sausage` | Weight 5 | 1–1 | `64602` |
| `[9]` | Consumable | Novice, Stalker + Master | `CannedFood` | Weight 3 | 1–1 | `64608` |
| `[9]` | Consumable | Novice, Stalker + Master | `Bandage` | Weight 2 | 1–1 | `64614` |
| `[9]` | Consumable | Novice, Stalker + Master | `MoneyEpic` | Weight 1.77f | 1–1 | `64620` |
| `[10]` | Consumable | Novice, Stalker + Newbie | `Milk` | Weight 1 | 1–1 | `64634` |
| `[10]` | Consumable | Novice, Stalker + Newbie | `Bread` | Weight 5 | 1–1 | `64640` |
| `[10]` | Consumable | Novice, Stalker + Newbie | `Sausage` | Weight 5 | 1–1 | `64646` |
| `[10]` | Consumable | Novice, Stalker + Newbie | `CannedFood` | Weight 3 | 1–1 | `64652` |
| `[10]` | Consumable | Novice, Stalker + Newbie | `Bandage` | Weight 2 | 1–1 | `64658` |
| `[11]` | Consumable | Master | `Milk` | Weight 1 | 1–1 | `64671` |
| `[11]` | Consumable | Master | `Bread` | Weight 5 | 1–1 | `64677` |
| `[11]` | Consumable | Master | `Sausage` | Weight 5 | 1–1 | `64683` |
| `[11]` | Consumable | Master | `CannedFood` | Weight 3 | 1–1 | `64689` |
| `[11]` | Consumable | Master | `Bandage` | Weight 0.8f | 1–1 | `64695` |

## 4. Medium stash pool

Reachable vanilla item SIDs: **A012D, A045D, A545D, A556D, A762SniperD, A762SniperS, A919D, A939D, Bandage, Energetic, Medkit, MoneyCommon, MoneyEpic, MoneyRare, Vodka**.

Important structure:

- consumables are mostly explicit `Chance` rows;
- ammo expands by progression rank from basic pistol/rifle/shotgun families into .45, 5.56, sniper and 9x39 families;
- coupon rows are a clean `Chance=0.10`, but exist only for **Experienced/Veteran/Master** and only on **Novice/Stalker** difficulty; there is no equivalent Newbie coupon row;
- the source contains `A919D` twice in the Newbie ammo block; this is preserved as a vanilla source fact, not silently deduplicated.

| Block | Category | Gate | Item SID | Selection field | Count | CFG line |
|---|---|---|---|---|---|---:|
| `[0]` | Consumable | Novice, Stalker, Veteran | `Bandage` | Chance 1 | 1–3 | `64731` |
| `[0]` | Consumable | Novice, Stalker, Veteran | `Medkit` | Chance 1 | 1–2 | `64737` |
| `[0]` | Consumable | Novice, Stalker, Veteran | `Vodka` | Chance 1 | 1–1 | `64743` |
| `[0]` | Consumable | Novice, Stalker, Veteran | `Energetic` | Chance 0.5 | 1–2 | `64749` |
| `[1]` | Ammo | Newbie | `A919D` | Weight 5 | 18–24 | `64762` |
| `[1]` | Ammo | Newbie | `A545D` | Weight 5 | 18–24 | `64768` |
| `[1]` | Ammo | Newbie | `A919D` | Weight 5 | 18–24 | `64774` |
| `[1]` | Ammo | Newbie | `A012D` | Weight 5 | 4–6 | `64780` |
| `[1]` | Ammo | Newbie | `A045D` | Weight 3 | 30–45 | `64786` |
| `[2]` | Consumable | Novice | `Bandage` | Chance 1 | 4–8 | `64799` |
| `[2]` | Consumable | Novice | `Vodka` | Chance 1 | 1–1 | `64805` |
| `[2]` | Consumable | Novice | `Medkit` | Chance 1 | 2–4 | `64811` |
| `[3]` | Ammo | Experienced | `A919D` | Weight 5 | 18–24 | `64824` |
| `[3]` | Ammo | Experienced | `A545D` | Weight 5 | 18–24 | `64830` |
| `[3]` | Ammo | Experienced | `A012D` | Weight 5 | 4–6 | `64836` |
| `[3]` | Ammo | Experienced | `A045D` | Weight 3 | 30–45 | `64842` |
| `[3]` | Ammo | Experienced | `A556D` | Weight 5 | 18–24 | `64848` |
| `[4]` | Ammo | Veteran | `A919D` | Weight 5 | 18–24 | `64861` |
| `[4]` | Ammo | Veteran | `A545D` | Weight 5 | 18–24 | `64867` |
| `[4]` | Ammo | Veteran | `A762SniperS` | Weight 5 | 25–30 | `64873` |
| `[4]` | Ammo | Veteran | `A012D` | Weight 3 | 4–6 | `64879` |
| `[4]` | Ammo | Veteran | `A045D` | Weight 3 | 30–45 | `64885` |
| `[4]` | Ammo | Veteran | `A556D` | Weight 5 | 18–24 | `64891` |
| `[4]` | Ammo | Veteran | `A762SniperD` | Weight 1 | 45–55 | `64897` |
| `[5]` | Ammo | Master | `A919D` | Weight 5 | 18–24 | `64910` |
| `[5]` | Ammo | Master | `A545D` | Weight 5 | 18–24 | `64916` |
| `[5]` | Ammo | Master | `A762SniperS` | Weight 5 | 5–10 | `64922` |
| `[5]` | Ammo | Master | `A012D` | Weight 3 | 4–6 | `64928` |
| `[5]` | Ammo | Master | `A045D` | Weight 3 | 30–45 | `64934` |
| `[5]` | Ammo | Master | `A556D` | Weight 5 | 18–24 | `64940` |
| `[5]` | Ammo | Master | `A762SniperD` | Weight 1 | 45–55 | `64946` |
| `[5]` | Ammo | Master | `A939D` | Weight 5 | 18–24 | `64952` |
| `[6]` | Consumable | Novice, Stalker + Experienced | `MoneyCommon` | Chance 0.1f | 1–1 | `64966` |
| `[7]` | Consumable | Novice, Stalker + Veteran | `MoneyRare` | Chance 0.1f | 1–1 | `64980` |
| `[8]` | Consumable | Novice, Stalker + Master | `MoneyEpic` | Chance 0.1f | 1–1 | `64994` |
| `[9]` | Consumable | Master | `Bandage` | Chance 0.4 | 1–2 | `65006` |
| `[9]` | Consumable | Master | `Medkit` | Chance 0.4 | 1–1 | `65012` |
| `[9]` | Consumable | Master | `Vodka` | Chance 1 | 1–1 | `65018` |
| `[9]` | Consumable | Master | `Energetic` | Chance 0.5 | 1–2 | `65024` |

## 5. Expensive stash pool

Reachable vanilla item SIDs: **A012A, A012E, A045A, A545A, A556A, A762SniperA, A762SniperD, A918A, A919A, A939A, AntiRad, ArmyMedkit, Bandage, CannedFood, Energetic, GrenadeRGD5, Medkit, MoneyCommon, MoneyEpic, MoneyLegendary, MoneyRare**.

Important structure:

- core medical/food items (`Bandage`, `Medkit`, `CannedFood`, `AntiRad`, `Energetic`) are guaranteed-Chance rows in the parent model;
- Easy/Novice difficulty adds extra medical rows including `ArmyMedkit`;
- ammo is strongly progression-rank gated and uses both `Chance` and `Weight`;
- `GrenadeRGD5` is `Chance=1`, count 1–2;
- coupons are the cleanest stash-currency system: each progression rank selects `MoneyCommon/Rare/Epic/Legendary`, with `Chance=0.20` on Novice/Stalker and `0.10` on Veteran/Master. This is why **Expensive Stash Coupon Chance** is a valid existing control.

| Block | Category | Gate | Item SID | Selection field | Count | CFG line |
|---|---|---|---|---|---|---:|
| `[0]` | Consumable | All difficulties / ranks | `Bandage` | Chance 1 | 2–4 | `65110` |
| `[0]` | Consumable | All difficulties / ranks | `Medkit` | Chance 1 | 2–3 | `65116` |
| `[0]` | Consumable | All difficulties / ranks | `CannedFood` | Chance 1 | 1–3 | `65122` |
| `[0]` | Consumable | All difficulties / ranks | `AntiRad` | Chance 1 | 1–1 | `65128` |
| `[0]` | Consumable | All difficulties / ranks | `Energetic` | Chance 1 | 1–2 | `65134` |
| `[1]` | Ammo | Newbie | `A918A` | Chance 1 | 20–25 | `65147` |
| `[1]` | Ammo | Newbie | `A012A` | Chance 0.5 | 8–12 | `65153` |
| `[2]` | Ammo | Newbie | `A545A` | Weight 1 | 55–65 | `65166` |
| `[2]` | Ammo | Newbie | `A919A` | Weight 2 | 55–65 | `65172` |
| `[3]` | Artifact | All difficulties / ranks | `GrenadeRGD5` | Chance 1 | 1–2 | `65183` |
| `[4]` | Consumable | Novice | `Medkit` | Chance 1 | 2–4 | `65196` |
| `[4]` | Consumable | Novice | `AntiRad` | Chance 1 | 1–1 | `65202` |
| `[4]` | Consumable | Novice | `ArmyMedkit` | Chance 1 | 1–2 | `65208` |
| `[5]` | Ammo | Experienced | `A918A` | Weight 5 | 20–25 | `65221` |
| `[5]` | Ammo | Experienced | `A012A` | Weight 3 | 8–12 | `65227` |
| `[5]` | Ammo | Experienced | `A045A` | Weight 2 | 30–40 | `65233` |
| `[6]` | Ammo | Experienced | `A545A` | Weight 2 | 55–65 | `65246` |
| `[6]` | Ammo | Experienced | `A919A` | Weight 1 | 55–65 | `65252` |
| `[6]` | Ammo | Experienced | `A556A` | Weight 2 | 55–65 | `65258` |
| `[7]` | Ammo | Veteran | `A762SniperA` | Weight 2 | 10–15 | `65271` |
| `[7]` | Ammo | Veteran | `A012E` | Weight 3 | 8–12 | `65277` |
| `[7]` | Ammo | Veteran | `A045A` | Weight 2 | 30–40 | `65283` |
| `[8]` | Ammo | Veteran | `A545A` | Weight 2 | 55–65 | `65296` |
| `[8]` | Ammo | Veteran | `A919A` | Weight 1 | 55–65 | `65302` |
| `[8]` | Ammo | Veteran | `A556A` | Weight 2 | 55–65 | `65308` |
| `[8]` | Ammo | Veteran | `A762SniperD` | Weight 2 | 55–65 | `65314` |
| `[9]` | Ammo | Master | `A762SniperA` | Weight 2 | 10–15 | `65327` |
| `[9]` | Ammo | Master | `A012E` | Weight 3 | 8–12 | `65333` |
| `[9]` | Ammo | Master | `A045A` | Weight 2 | 30–40 | `65339` |
| `[9]` | Ammo | Master | `A939A` | Weight 2 | 30–40 | `65345` |
| `[10]` | Ammo | Master | `A545A` | Weight 2 | 55–65 | `65358` |
| `[10]` | Ammo | Master | `A919A` | Weight 1 | 55–65 | `65364` |
| `[10]` | Ammo | Master | `A556A` | Weight 2 | 55–65 | `65370` |
| `[10]` | Ammo | Master | `A762SniperD` | Weight 2 | 55–65 | `65376` |
| `[11]` | Consumable | Novice, Stalker + Newbie | `MoneyCommon` | Chance 0.2f | 1–1 | `65390` |
| `[12]` | Consumable | Novice, Stalker + Experienced | `MoneyRare` | Chance 0.2f | 1–1 | `65404` |
| `[13]` | Consumable | Novice, Stalker + Veteran | `MoneyEpic` | Chance 0.2f | 1–1 | `65418` |
| `[14]` | Consumable | Novice, Stalker + Master | `MoneyLegendary` | Chance 0.2f | 1–1 | `65432` |
| `[15]` | Consumable | Veteran, Master + Newbie | `MoneyCommon` | Chance 0.1f | 1–1 | `65446` |
| `[16]` | Consumable | Veteran, Master + Experienced | `MoneyRare` | Chance 0.1f | 1–1 | `65460` |
| `[17]` | Consumable | Veteran, Master + Veteran | `MoneyEpic` | Chance 0.1f | 1–1 | `65474` |
| `[18]` | Consumable | Veteran, Master + Master | `MoneyLegendary` | Chance 0.1f | 1–1 | `65488` |

## 6. What is actually safe to expose?

| Surface | Verdict | Why |
|---|---|---|
| Expensive Stash Coupon Chance | **KEEP EXISTING** | Exact per-rank Chance rows; existing writer already targets `Stash_Expensive_*` wrappers instead of the shared Rare parent. |
| Cheap Stash Grenade Chance | **IMPLEMENTED / RUNTIME TEST REQUIRED** | One RGD-5 Chance row (`0.10`, count 1); v0.2.6 uses the audited production-only Cheap fan-out. |
| Medium Stash Coupon Chance | **REVIEW LATER** | Clean 10% Chance, but not present for Newbie and only active on Novice/Stalker. |
| Expensive Stash Grenade Chance | **REVIEW LATER** | Structurally precise, but vanilla chance is 100%; changing it would turn a guaranteed component into an optional one. |
| Per-tier ammo quantity | **IMPLEMENTED / RUNTIME TEST REQUIRED** | Cheap, Medium and Expensive use integer Min/Max scaling through production-only TCD-owned generator fan-out; vanilla ammo selection fields stay unchanged. |
| Per-tier medical/food quantity | **REVIEW LATER** | Overlapping difficulty blocks and mixed Chance/Weight make one simple scalar ambiguous. |
| All Stash Loot Chance / Quantity | **DO NOT ADD** | No single source field represents it. The three pools mix Chance, Weight and Count in different ways. |

## 7. Improving Stashes Loot cross-check

The supplied `Improving_Stashes_Loot_P.pak` independently overrides the same high-leverage stash roots: all 12 `Stash_Cheap/Medium/Expensive_<rank>` wrappers plus `GamePass_Stash_ItemGenerator_Cheap`, `...Common_Var1`, `...Rare`, CNPP variants and `CheapNoFresh`. In the supplied mod those roots are redirected into the mod's own `ISL_MasterGenerator` graph.

That is useful **as an interception-point cross-check**, not as proof of vanilla probability semantics. The mod intentionally replaces vanilla tier behavior, so its balance values are not copied into True Custom Difficulty.

## 8. Data files

- `stash-backbone-family-summary.csv` — tier footprint and pool summary.
- `stash-backbone-rank-wrapper-usage.csv` — all 12 rank wrapper footprints.
- `stash-backbone-pool-blocks.csv` — exact local category blocks for the three shared parents.
- `stash-backbone-pool-entries.csv` — exact 133 local item rows with gates and raw Chance/Weight/Count fields.
- `stash-backbone-item-matrix.csv` — item membership and conditions across Cheap/Medium/Expensive.
- `stash-backbone-control-surfaces.csv` — explicit keep/review/reject decision register.
- `improving-stashes-backbone-crosscheck.csv` — supplied-mod overlap.
- `stash-backbone-summary.json` — compact machine-readable conclusions.

## 9. Boundary

This audit does **not** claim an engine formula converting `Weight` into absolute probability and does not treat `Weight` as interchangeable with `Chance`. It also does not merge the separate SmartLoot layer into ItemGenerator results: many physical stashes have both layers, so any future stash control must state which layer it changes.
