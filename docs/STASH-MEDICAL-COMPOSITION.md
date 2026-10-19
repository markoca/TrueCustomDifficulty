## 2026-10-19 — CURRENT: Medkit MIN/MAX shared two-handle range (Dropped Weapon Condition parity)

Medkits and Army Medkits now use ONE shared, continuous, source-backed 0–5 MIN/MAX range rail each, exactly the same two-endpoint interaction pattern as Loot → DROPPED WEAPON CONDITION. Each compact bounded 122-DIP card has fixed center-aligned separate MIN and MAX readouts, one colored interval between the handles, and 0/5 lower axis endpoints. Retire the former two independent half-width rails. The two native source IDs remain independent and their existing MIN<=MAX automatic coupling is unchanged. Mouse hit-test chooses the nearest endpoint; when handles coincide or nearly overlap, they visibly separate vertically by 7 DIP and remain independently selectable in 2D. The 38-DIP hit zone and active pointer capture mirror the NPC condition range. Wheel, keyboard/Home/End, source-specific right-click and family R reset remain supported; changed numbers alone highlight yellow.

MIN 0 preserves original source floor and 1–5 raises it; MAX 0 disables that item (stored legacy -1), MAX 1–4 caps the native maximum, MAX 5 preserves the original maximum (stored legacy 0). Legacy stored 6 survives until directly edited, without false VANILLA/AUTO labels. Same native patch routes, quest protections and preset state; no new drop probability. Quantity card Y start 473 wide / 782 narrow, card height 122, narrow row pitch 132, total medical panel measured 609 wide / 1050 narrow DIP including bottom padding. Renderer, catalog, audit and UI locks agree; previously documented half-rail geometry is superseded.

## 2026-10-19 — Medical quantity direction correction (current)

The Medkit and Army Medkit 98-DIP cards now each show two compact, separate **left-to-right 0..5** sliders: MIN 0 → 5 and MAX 0 → 5. This corrects the earlier reversed MIN rail (5 → 0) that displayed a misleading left-hand 5 beside the initial MIN=0 and MAX=5 readouts. Endpoint rendering and hit mapping use the same forward direction. Native floor/cap meanings, stored MAX off/uncapped compatibility, and pair constraints do not change. No VANILLA/AUTO labels return. See the current UI lock docs.

## 2026-10-19 — CURRENT Stashes numeric medical bounds and player-facing Priority

Every player-facing Stashes **Weight** label is now **Priority**: `LOOT PRIORITY · ALL TIERS`, `LOOT PRIORITY · NATIVE WEIGHTS`, `ANTI-RAD · PRIORITY`, `BONUS PRIORITY`, and `PRIORITY SHARE`. The underlying `Weight` CFG property is NOT renamed or replaced. Priority describes relative selection preference inside the actual authored native candidate pool, not a physical-stash probability. The read-only Extra Gear priority-share strip normalizes build-time class weights but does not calculate the final two distinct picks or Chance. Native Chance controls remain independent. The separate yellow modified-state mark beside Anti-Rad OFF/ON is removed; selected/hovered boundaries stay visible.

Medkit and Army Medkit **MIN/MAX** rails show only **integer 0–5** in readouts and endpoint labels. **MIN 0** leaves the authored minimum alone; MIN 1–5 raises its floor. **MAX 0** excludes that medical item (stored legacy -1, writer writes both counts 0). **MAX 1–4** caps its authored maximum; **MAX 5** leaves the original maximum intact (stored legacy 0). As before, these are floor/cap controls and do **not** guarantee exact item counts or raise a lower original source cap. The stored -1/0/1–6 schema and old presets retain their meanings; legacy 6 is visually clamped at the editor's 5 end until changed, without silently rewriting stored values. MAX edits below MIN lower MIN; MIN edits above MAX raise MAX. The same mapping works with mouse click/drag, wheel and keyboard, and right-click reset. On-screen numeric 0–5 replaces VANILLA/AUTO and removes the misleading middle sentinel without changing progression/quest protection. Responsive compact geometry unchanged. Catalog labels, WPF code, shared static audits and both authoritative UI documents synchronized. Compile/Wine and gameplay checks must be run locally.

**CURRENT 2026-10-16 — MEDICAL PREVIEW SOURCE CORRECTION:** The old four-way normalization of medical bias multipliers has been removed. The four editable medical bias bars now show four read-only original Chance examples from explicit Basic/Rare ItemGenerator categories, with global Medical and per-item bias applied and Chance clamped, separately per source. These are not comparable whole-stash drop odds; original Medkit/ArmyMedkit MAX=-1 suppression remains intact. [Current UI lock](UI-DESIGN-LOCK.md).

**Current visual override (2026-10-16):** MEDICAL COMPOSITION now uses four editable 0–3× horizontal bars plus a read-only normalized relative bias preview; MEDICAL QUANTITY uses two compact independent MIN/MAX dot-selector cards, not dials. The MAX special first stop **0 ITEMS = stored -1**, the next stop **AUTO = stored 0**; neither is a measured drop chance. Previous A+C knob descriptions are historical. See [current UI lock](UI-DESIGN-LOCK.md).

**CURRENT 2026-10-16 — A+C STASHES MEDICAL DETAIL:** Four independent small native bias knobs replace the horizontal composition graph; two compact MIN/MAX knob pair cards replace the older dot plots. Both remain directly below the always-visible Content Mix and highlight the selected real source in the rationale pane. MAX `0 ITEMS` still persists -1, source AUTO still persists 0. See [the current A+C visual lock](UI-DESIGN-LOCK.md); historical bar/dot layout below is superseded, not the source semantics.

**CURRENT 2026-10-16 — 12 native direct medical entries covered:** Two ordinary Richter Cabin direct Medkits are already covered; the same existing medical controls now additionally tune ten canonically named Medkit/ArmyMedkit/EcoMedkit/Bandage rows in six other regular stash wrappers. Nine Hercules/PSYBlocker booster rows and lowercase `medkit` remain out of this medical source scope, and 45 weapon/gear rows are explicitly reserved for a later randomized-gear feature. [Exact 22-row consumable inventory](generators/REGULAR-STASH-DIRECT-CONSUMABLES-2026-10-16.md) · [reserved gear queue](generators/STASH-GEAR-UNIQUE-RANDOMIZATION-BACKLOG.md).

**Additional ordinary-world direct-item audit (2026-10-16):** The original CFG contains [ten more canonical MEDICAL source rows on six location-specific stash wrappers](generators/REGULAR-STASH-DIRECT-ADDITIONS-AUDIT-2026-10-16.md) beyond the two already fixed Cabin Medkits. These are **not yet written by TCD's medical slider**. The same audit maps 11 direct ammo, two grenades and nine booster rows; a lowercase `medkit` SID requires separate resolution. Actor/item-level gating is required before safely expanding.

# Stash Medical Composition and Quantity — source-backed contract (2026-10-16)

## CURRENT 2026-10-16 — medical 0x source suppression regression

A reproduced saved-profile configuration has global MEDICAL=0x and each Bandage/Medkit/ArmyMedkit/AntiRad bias=0x while its quantity limits remain VANILLA. This is a valid setup: VANILLA only preserves source counts when the item is otherwise eligible, and must never re-enable items disabled by 0x.

Previous production-clone generation zeroed Chance or Weight but left MinCount and MaxCount positive; behavior of an all-zero weighted pool is not established by static CFG data. The writer now sets both selection and count fields to zero for excluded Bandage, Medkit, ArmyMedkit, AntiRad, and EcoMedkit entries. Global MEDICAL=0x disables all those audited medical SIDs; item-specific 0x disables only that item. MAX CAP=0 ITEMS (-1) remains a separate suppression for Medkit and ArmyMedkit and overrides a positive MIN FLOOR.

This is defensive CFG generation, not proof of the root cause of an observed in-game drop. Scope is still TCD-owned, redirected Basic/Standard/Rare regular production generators; previously-filled stashes, quest/scripted/unique/test/unresolved and separate CNPP generator pools, and other mod overrides are outside confirmed coverage. Saving a JSON preset alone does not install a mod: Build Difficulty creates and deploys .pak/.utoc/.ucas to the configured ~mods directory. Use a fresh ungenerated stash for manual runtime verification. LootSpecialGeneratorsContract now includes three generated-CFG zero-medical acceptance scenarios.


### 2026-10-16 — MIN FLOOR versus explicit 0 ITEMS precedence

**VANILLA** is the native source-bound preservation mode: the source can define different Medkit/ArmyMedkit stack counts by difficulty, tier and rank. On either MIN or MAX, stored `0` means VANILLA and never means "force no item". The new visible MAX CAP `0 ITEMS` point stores **-1**, distinct from VANILLA. For **MIN FLOOR = 1** (or any 1–6) and **MAX CAP = 0 ITEMS**, explicit zero wins: generated **MinCount = 0**, **MaxCount = 0**, and the applicable item entry **Chance/Weight = 0**, leaving no Medkit/ArmyMedkit from that eligible TCD production generator. No assertion is made about unrelated medical types or authored/quest stashes. If MAX is VANILLA and MIN is 1, MIN remains a floor rather than suppressing the item; if MIN is VANILLA and MAX is 1, source counts are capped at one. In-game validation of suppression remains required.


## CURRENT 2026-10-16 — MAX CAP now supports actual 0 ITEMS

Medkit and Army Medkit quantity are editable in one 2x2 dot graph. MIN FLOOR has native 0=VANILLA and 1..6 item stops. MAX CAP has a separate 0 ITEMS point (stored -1), historical 0=VANILLA and 1..6 caps. Selecting 0 ITEMS zeros both native MinCount and MaxCount and also zeros the eligible Medkit or ArmyMedkit item source Chance/Weight in production-only clone generators. Positive MAX only limits counts. This avoids relying solely on how the engine handles a selected zero-count entry. Normal VANILLA keeps source values; older saved preset MAX 0 values remain VANILLA. Use general Medical 0x to suppress all eligible medical items instead of just the two Medkit types. This is a source-level guarantee; unopened stash gameplay validation is still pending. Earlier MAX 0..6-only text below is historical.


**Status:** Implemented in TrueCustomDifficulty source; game runtime validation still required. Source version **2.0.5** (project's 2026-10-16 ItemGeneratorPrototypes CFG baseline). These are *production-only TCD-owned generator clone* edits, not blanket replacements of all game stashes.

## Why a single MEDICAL multiplier is insufficient

The existing `loot.stashMixMedical` multiplies **existing** Medkit/Bandage/ArmyMedkit/AntiRad `Chance` or `Weight` together. Many Medkit rows already use `Chance=1`, so **MEDICAL=3x does not give three times as many Medkits**. Increasing all items in one `Weight` category together leaves their relative ratios unchanged. Decreasing the global MEDICAL category to 0x should suppress *eligible TCD production-generator medical entries*, but manually-authored one-off/quest stash rewards are unaffected and may still contain medical items.

Audited source examples:

| Tier, branch | Item | Native selection | Native stack |
|---|---|---|---|
| Basic normal, Easy/Medium | Medkit | Chance 0.4 | 1 |
| Basic Easy bonus | Medkit | Chance 1 | 1–2 |
| Standard main | Medkit | Chance 1 | 1–2 |
| Standard Easy bonus | Medkit | Chance 1 | 2–4 |
| Rare main | Medkit | Chance 1 | 2–3 |
| Rare Easy bonus | ArmyMedkit | Chance 1 | 1–2 |

`Chance` is a particular source entry's selection field, **not a verified whole-stash drop percentage**. `Weight` represents relative choice only among that source group's applicable candidate items. Source categories can vary by difficulty, player rank and stash tier.

## UI: two focused source-true panels in Stashes

1. **MEDICAL LOOT COMPOSITION** — compact **four-row shared-scale horizontal bar graph**: Bandages, Medkits, Army Medkits and Anti-Rad. Each selection-bias source has **0–3x** range, **0.25x** step, **1x native baseline**. The chart shows numeric value at right, labels on the left, and the same 0x/1x/2x/3x reference ruler for all four real source channels. A value of 0x removes that item from the eligible TCD production clone entry; it does not rewrite scripted/quest stash loot.
2. **MEDKIT QUANTITY** — one compact native-source **2×2 control**: Medkits MIN FLOOR / MAX CAP and Army Medkits MIN FLOOR / MAX CAP. Four independent **0 VANILLA / 1–6 items** native integer controls. `VANILLA` (0) preserves the **original per-entry MinCount or MaxCount**, not zero items. No invented default "1–2" is imposed across all tiers.

All eight sources have separate preset state, independent native pointer + keyboard edits, right-click reset of just that source, **R** resetting the aggregate family, changed-number-only gold typography, and existing reference marker behavior. The original STASH CONTENT chart and EXTRA STASH GEAR remain intact.

## Writer ownership and exact composition

Selection changes use the **same existing** Basic / Standard / Rare production clones and the original 1,271 / 450 / 305 audited redirect manifests. No separate patch writer can overwrite their ammo, grenade or coupon changes. New separate selection factors are:

| ID | Only matching ItemPrototypeSID |
|---|---|
| `loot.stashMedicalBandageBias` | `Bandage` |
| `loot.stashMedicalMedkitBias` | `Medkit` |
| `loot.stashMedicalArmyMedkitBias` | `ArmyMedkit` |
| `loot.stashMedicalAntiRadBias` | `AntiRad` |

The effective entry multiplier is `loot.stashMixMedical × loot.stashMedical<Item>Bias`. Only a source field that is **already present** is transformed. `Weight' = max(0, source Weight × effective multiplier)`; `Chance' = clamp(source Chance × effective multiplier, 0, 1)`. At both neutral 1x, emit the original raw source field with its original suffix/precision. A source-zero selection stays zero. No new Medkits, Army Medkits or Anti-Rad prototypes are injected into tiers/ranks/difficulty branches where absent.

Count controls (two per item):
- `loot.stashMedkitMinCount` / `loot.stashMedkitMaxCount`
- `loot.stashArmyMedkitMinCount` / `loot.stashArmyMedkitMaxCount`

Given original `[sourceMin, sourceMax]`, `minFloor=0` means untouched source minimum, otherwise `min=max(sourceMin, minFloor)`. `maxCap=0` means untouched source maximum, otherwise `max=min(sourceMax, maxCap)`. If `min > max` **MAX CAP wins when set**, so `min=max`; otherwise the explicit MIN FLOOR raises the maximum to the requested minimum, so `max=min`. Each emitted entry satisfies **0 ≤ MinCount ≤ MaxCount** and keeps per-tier/branch behavior when both sliders are `VANILLA`. In particular, **Medkit MAX CAP = 1** reduces existing 2–4 stacks to a maximum of 1 while not changing chances. It does not guarantee that a Medkit appears.

All source categories/rank/difficulty guards, existing count overrides on other item types, TCD gear modes and production-only manifests remain intact. A Rare medical-only edit must not silently apply the selected profile's *unmodified* Rare Stash Ammo Quantity: the writer now passes **1x** for ammo when that source is not modified.

## QA — expected static behavior and manual game observations

- [ ] Startup + UI: four compositional bars and 2×2 quantity controls display, use stable native WPF hover, clamp, keyboard, reset and independent modified-state colors; no overlapping or clipped cards at narrow width
- [ ] Both 1x/1x and VANILLA/VANILLA preserve exact native Medkit fields, while Medical 3x × Medkit 0x yields source zero on eligible Medkits without zeroing Bandages
- [ ] Medical 0x × Medkit 3x remains 0; Medkit 1x alone does not inflate `Chance=1`; 0.25x changes `Chance=0.4` to `0.1`
- [ ] Basic/Standard/Rare clone references and manifest routing stay 1,271/450/305; no quest/scripted/unresolved/test stash modification
- [ ] Medkit MAX CAP=1 clamps each existing `MaxCount` and `MinCount` to no more than 1; no impact on Bandage or ammo quantities
- [ ] Army Medkit changes only source branches that already contain `ArmyMedkit`; its count VANILLA values preserve 1–2 in the relevant Rare Easy bonus
- [ ] MIN FLOOR=3, MAX CAP=VANILLA raises insufficient maximum to 3; MIN FLOOR=3, MAX CAP=1 resolves to 1–1 (explicit cap wins)
- [ ] Build composition: Basic RGD-5 chance/quantity + MEDICAL global + item Medkit bias + Medkit cap + Rare coupon + ammunition + optional gear all coexist, without stale/duplicate generator patch overwrites
- [ ] Save/load all eight added sources, retain existing presets, and protect user-edited stock profiles against auto-migration
- [ ] Freshly populated in-game regular stash trials across game difficulties, player ranks and tiers verify the practical effect; **do not infer an in-game success or absolute spawn percentage solely from a CFG diff**

**Limits:** Smart Loot and authored loot not routed through the exact TCD production-clone manifests are not touched. The `EcoMedkit` SID remains covered by the *global* medical control where present, but it is **not** a new independent editor because it does not appear in the audited Basic/Standard/Rare template categories. This is intentional.
