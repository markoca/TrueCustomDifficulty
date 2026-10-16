# Stash Medical Composition and Quantity — source-backed contract (2026-10-16)

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

## UI: two focused source-true panels in Loot

1. **MEDICAL LOOT COMPOSITION** — compact **four-row shared-scale horizontal bar graph**: Bandages, Medkits, Army Medkits and Anti-Rad. Each selection-bias source has **0–3x** range, **0.25x** step, **1x native baseline**. The chart shows numeric value at right, labels on the left, and the same 0x/1x/2x/3x reference ruler for all four real source channels. A value of 0x removes that item from the eligible TCD production clone entry; it does not rewrite scripted/quest stash loot.
2. **MEDKIT QUANTITY** — one compact native-source **2×2 control**: Medkits MIN FLOOR / MAX CAP and Army Medkits MIN FLOOR / MAX CAP. Four independent **0 AUTO / 1–6 items** native integer controls. `AUTO` (0) preserves the **original per-entry MinCount or MaxCount**, not zero items. No invented default "1–2" is imposed across all tiers.

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

Given original `[sourceMin, sourceMax]`, `minFloor=0` means untouched source minimum, otherwise `min=max(sourceMin, minFloor)`. `maxCap=0` means untouched source maximum, otherwise `max=min(sourceMax, maxCap)`. If `min > max` **MAX CAP wins when set**, so `min=max`; otherwise the explicit MIN FLOOR raises the maximum to the requested minimum, so `max=min`. Each emitted entry satisfies **0 ≤ MinCount ≤ MaxCount** and keeps per-tier/branch behavior when both sliders are `AUTO`. In particular, **Medkit MAX CAP = 1** reduces existing 2–4 stacks to a maximum of 1 while not changing chances. It does not guarantee that a Medkit appears.

All source categories/rank/difficulty guards, existing count overrides on other item types, TCD gear modes and production-only manifests remain intact. A Rare medical-only edit must not silently apply the selected profile's *unmodified* Rare Stash Ammo Quantity: the writer now passes **1x** for ammo when that source is not modified.

## QA — expected static behavior and manual game observations

- [ ] Startup + UI: four compositional bars and 2×2 quantity controls display, use stable native WPF hover, clamp, keyboard, reset and independent modified-state colors; no overlapping or clipped cards at narrow width
- [ ] Both 1x/1x and AUTO/AUTO preserve exact native Medkit fields, while Medical 3x × Medkit 0x yields source zero on eligible Medkits without zeroing Bandages
- [ ] Medical 0x × Medkit 3x remains 0; Medkit 1x alone does not inflate `Chance=1`; 0.25x changes `Chance=0.4` to `0.1`
- [ ] Basic/Standard/Rare clone references and manifest routing stay 1,271/450/305; no quest/scripted/unresolved/test stash modification
- [ ] Medkit MAX CAP=1 clamps each existing `MaxCount` and `MinCount` to no more than 1; no impact on Bandage or ammo quantities
- [ ] Army Medkit changes only source branches that already contain `ArmyMedkit`; its count AUTO values preserve 1–2 in the relevant Rare Easy bonus
- [ ] MIN FLOOR=3, MAX CAP=AUTO raises insufficient maximum to 3; MIN FLOOR=3, MAX CAP=1 resolves to 1–1 (explicit cap wins)
- [ ] Build composition: Basic RGD-5 chance/quantity + MEDICAL global + item Medkit bias + Medkit cap + Rare coupon + ammunition + optional gear all coexist, without stale/duplicate generator patch overwrites
- [ ] Save/load all eight added sources, retain existing presets, and protect user-edited stock profiles against auto-migration
- [ ] Freshly populated in-game regular stash trials across game difficulties, player ranks and tiers verify the practical effect; **do not infer an in-game success or absolute spawn percentage solely from a CFG diff**

**Limits:** Smart Loot and authored loot not routed through the exact TCD production-clone manifests are not touched. The `EcoMedkit` SID remains covered by the *global* medical control where present, but it is **not** a new independent editor because it does not appear in the audited Basic/Standard/Rare template categories. This is intentional.
