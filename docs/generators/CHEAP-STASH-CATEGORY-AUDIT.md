# Cheap Stash Category Audit

This audit takes the **Cheap** world-stash backbone apart by player-facing loot class and asks a narrower question: **which controls can change a useful property without changing vanilla item-selection weights?**

It is still a source-structure audit, not gameplay telemetry. The parent generator is `GamePass_Stash_ItemGenerator_Cheap`.

## 1. Cheap pool in one table

| Loot class | Source rows | Selection model | Vanilla count structure | Control conclusion |
| --- | ---: | --- | --- | --- |
| Ammo | **3** | Weight only (`40 / 30 / 30`) | `2–3`, `5–7`, `2–3` | **Strong quantity candidate**; keep weights unchanged. |
| Grenade | **1** | `Chance=0.10` | fixed `1` | **Strong chance candidate**. |
| Medical | **12** | 6 Chance + 6 Weight | 8 fixed `1–1`; 4 variable | Count-only tuning is technically separable, but **defer** because UX/effect is uneven. |
| Food / Drink | **33** | 7 Chance + 26 Weight | 28 fixed `1–1` | **Do not add for now**; quantity has poor useful resolution and frequency is not one source semantic. |
| Coupons | **3** | `Weight=1.77` only | fixed `1` | **Not a Chance control**; leave audit-only. |

These five classes account for all **52** locally-authored Cheap-parent item rows.

## 2. Ammo is the cleanest quantity surface

The complete Cheap ammo block is all-difficulty and all-rank:

| Ammo | Weight | MinCount | MaxCount |
| --- | ---: | ---: | ---: |
| 9×18 (`A918D`) | 40 | 2 | 3 |
| 9×19 (`A919D`) | 30 | 5 | 7 |
| 12-gauge (`A012D`) | 30 | 2 | 3 |

A future **Cheap Stash Ammo Quantity** control can therefore change only `MinCount` / `MaxCount` while leaving the vanilla **40/30/30 selection balance exactly unchanged**.

This is materially different from an ammo *chance* control: there is no ammo `Chance` here. The source model is a weighted ammo-type choice plus an integer count range.

The integer scaling policy is now fixed in [`CHEAP-STASH-AMMO-SCALING.md`](CHEAP-STASH-AMMO-SCALING.md): **50–200%**, **25 pp steps**, each vanilla Min/Max endpoint scaled independently with positive **round-half-up**, then clamped to at least `1`. The control is therefore semantically/scaling-ready; only the production-only fan-out scope remains before implementation.

## 3. Grenade is the cleanest chance surface

Cheap grenades are one independent row:

```text
GrenadeRGD5
Chance   = 0.10
MinCount = 1
MaxCount = 1
Difficulty = all
PlayerRank = all
```

So **Cheap Stash Grenade Chance** is a real 1:1 source concept. Changing it does not touch ammo weights, consumable weights, coupon weights, or item counts.

## 4. Medical quantity is possible, but not attractive yet

Medical means only `Bandage` + `Medkit` in this Cheap parent. There are **12** source rows spread across **9 category blocks**:

- 6 are explicit `Chance` rows;
- 6 are weighted rows;
- 8 of 12 are fixed at `1–1` count;
- the remaining count ranges are `1–2` or `2–4`.

If we edit **only counts**, we can preserve every vanilla Chance and Weight exactly. So this is not structurally unsafe. The problem is UI quality: most rows have only one item, multiple difficulty/rank blocks can be active, and a simple percentage multiplier would have very coarse or non-uniform visible effect.

Verdict: **DEFER — technically possible count-only control, not a first-class UI candidate.**

## 5. Food / drink is even less suitable

The pool has **33** food/drink rows across 10 blocks. **28 of 33 are fixed `1–1`**. The selection model mixes 7 Chance rows with 26 Weight rows.

- A generic **Food/Drink Frequency** control would be false because it would have to reinterpret Chance and Weight as the same thing.
- A quantity-only control could preserve selection, but almost every source row is already exactly one item. Below 100% either becomes a no-op or requires zero-count semantics; above 100% jumps immediately to duplicate items.

Verdict: **DO NOT ADD FOR NOW.**

## 6. Cheap coupons are not a chance field

The Cheap coupon entries are:

```text
Experienced rank -> MoneyCommon  Weight=1.77
Veteran rank    -> MoneyRare    Weight=1.77
Master rank     -> MoneyEpic    Weight=1.77
```

They exist only on Novice/Stalker difficulty in these rank-gated weighted consumable pools. Newbie has **no coupon row**. There is no coupon `Chance` field here.

Therefore **Cheap Stash Coupon Chance** must not be added. A player-facing `Coupon Weight` control would expose an internal relative-selection primitive whose absolute probability is not proven, so it stays audit-only too.

## 7. Scope is the hard part, not the grenade/ammo fields

The shared parent itself is too broad to edit globally:

- **163 direct external consumers**: **128 item-container** + **35 quest/scripted**;
- **342 incoming `SubItemGenerator` composition references**;
- of those incoming wrappers, **298** are already classified as production item-container candidates, **38** have unresolved/no runtime consumer, and **6** are excluded scripted/quest paths;
- the four `Stash_Cheap_<rank>` wrappers themselves fan out to a mapped set of **214 physical roots** and even include one `D_WoodenBox_02` destructible path at lower ranks.

The direct-parent physical rows visible in `spawn-loot-actors.csv` are also layered: **126/126** carry a SmartLoot/Stash layer in addition to the ItemGenerator.

**Consequence:** do not patch `GamePass_Stash_ItemGenerator_Cheap` globally. That would alter excluded quest/scripted content and every nested consumer.

A future comprehensive Cheap-stash control should instead use a **production-only fan-out** strategy: create/materialize the tuned Cheap behavior only through confirmed normal-world container wrappers/consumers, while leaving quest/scripted, test and unresolved paths untouched. `cheap-stash-production-fanout.csv` is the first exact allowlist/review register for that job.

## 8. Control verdict

| Proposed control | Verdict |
| --- | --- |
| **Cheap Stash Grenade Chance** | **STRONG CANDIDATE**, once production-only scope fan-out is implemented. |
| **Cheap Stash Ammo Quantity** | **STRONG CANDIDATE — SCALING RESOLVED**, production-only fan-out still required. |
| Cheap Stash Medical Quantity | **DEFER** — technically clean count-only edit, weak/coarse UX. |
| Cheap Stash Food / Drink Quantity | **DO NOT ADD FOR NOW**. |
| Cheap Stash Medical Frequency | **REJECT** — mixed Chance/Weight. |
| Cheap Stash Food / Drink Frequency | **REJECT** — mixed Chance/Weight. |
| Cheap Stash Coupon Chance | **REJECT** — source is Weight, not Chance, and no Newbie row exists. |
| Cheap Stash Ammo Type Balance | **AUDIT ONLY** — loot-composition design, not normal difficulty tuning. |

## 9. Data files

- `cheap-stash-category-entries.csv` — all 52 Cheap parent rows, classified by player-facing loot class.
- `cheap-stash-category-summary.csv` — category counts and source mechanisms.
- `cheap-stash-control-surfaces.csv` — explicit keep/defer/reject register.
- `cheap-stash-production-fanout.csv` — all 342 incoming Cheap-parent composition wrappers with production/review/exclude classification.
- `cheap-stash-category-summary.json` — compact counts and scope facts.
- `CHEAP-STASH-AMMO-SCALING.md` — deterministic integer scaling policy for the future ammo-quantity control.
- `cheap-stash-ammo-scaling-table.csv` — exact 50–200% endpoint results for all three vanilla Cheap ammo ranges.

## 10. Boundary

This audit does **not** infer an absolute probability from `Weight`, and it does not merge the separate SmartLoot layer into ItemGenerator semantics. A future control must state whether it changes ItemGenerator content only or also intentionally changes SmartLoot.
