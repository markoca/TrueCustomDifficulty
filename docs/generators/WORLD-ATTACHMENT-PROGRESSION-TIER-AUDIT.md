# World Attachment Progression Tier Audit

Version: 0.9.26.44 audit-only

## Result

The conservative world-attachment pool remains **39 models**.

Every safe-world attachment now has a proposed progression tier for future shuffle work:

- **T1:** 0
- **T2:** 18
- **T3:** 12
- **T4:** 9

No production attachment shuffle is enabled in this release.

## Evidence priority

Attachment progression is not represented by one clean global tier field, so the audit uses the strongest available evidence per attachment class.

For **optics and suppressors**, authored `Trader_Attachments_T2/T3/T4` membership is strong evidence, but world placement is allowed to override it when the trader grouping conflicts with actual safe-world availability.

For **magazines**, the strongest stable evidence is weapon-platform compatibility from `FittingWeaponsSIDs`. Increased and paired magazines are treated as upgrades and therefore never below T2; huge magazines never below T3. The earliest compatible regular weapon tier is used as the progression anchor.

For **underbarrel attachments**, fitting-weapon tiers, safe-world placement rank and cost are combined.

World actor `Rank` is treated as placement evidence, not as a universal item-tier field.

## Important conflict found

`EN_Silen_1` is listed in `Trader_Attachments_T4_ItemGenerator`, but its proven safe-world placements include Newbie and Experienced locations. For world-loot shuffle purposes it is therefore assigned **T2**, not T4, because preserving actual world availability is more important than mirroring the trader stock group.

## Proposed progression tiers

### Optics (5)

| SID | Tier | Confidence | Cost | Evidence |
|---|---:|---|---:|---|
| `EN_GoloScope_1` | T2 | HIGH | 3700 | explicit Trader_Attachments_T2 membership |
| `EN_X2Scope_1` | T2 | HIGH | 5000 | explicit Trader_Attachments_T2 membership |
| `RU_ColimScope_2` | T2 | HIGH | 2000 | explicit Trader_Attachments_T2 membership |
| `RU_X4Scope_1` | T3 | HIGH | 7500 | explicit Trader_Attachments_T3 membership |
| `EN_X4Scope_1` | T4 | HIGH | 7500 | explicit Trader_Attachments_T4 membership |

### Suppressors (4)

| SID | Tier | Confidence | Cost | Evidence |
|---|---:|---|---:|---|
| `EN_Silen_1` | T2 | MEDIUM | 3500 | safe-world placements are Newbie/Experienced despite Trader_Attachments_T4 membership; world progression takes priority |
| `RU_Silen_1` | T2 | HIGH | 2500 | explicit Trader_Attachments_T2 membership |
| `RU_Silen_3` | T3 | HIGH | 9000 | sniper suppressor; compatible T2-T4 weapons; world placements center on Veteran/Master |
| `EN_Silen_4` | T4 | HIGH | 12000 | high-cost sniper suppressor; Master world placement; compatible T2-T4 weapons |

### Magazines (28)

| SID | Tier | Confidence | Cost | Evidence |
|---|---:|---|---:|---|
| `GunAK74_MagIncreased` | T2 | MEDIUM | 4500 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunAK_MagPaired` | T2 | MEDIUM | 4500 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunBucket_MagIncreased` | T2 | MEDIUM | 4500 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunFora_MagIncreased` | T2 | MEDIUM | 9000 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunGP37_MagPaired` | T2 | MEDIUM | 5500 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunGvintar_MagIncreased` | T2 | MEDIUM | 7800 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunM10_MagIncreased` | T2 | MEDIUM | 3200 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunM16_MagIncreased` | T2 | MEDIUM | 5500 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunMark_MagIncreased` | T2 | MEDIUM | 7500 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunPM_MagIncreased` | T2 | MEDIUM | 3500 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunUDP_MagIncreased` | T2 | MEDIUM | 2500 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunViper_MagIncreased` | T2 | MEDIUM | 3700 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunAPB_MagIncreased` | T3 | MEDIUM | 4000 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunGP37_MagLarge` | T3 | MEDIUM | 7000 | earliest compatible weapon tier + HugeMag floor T3 |
| `GunGrim_MagIncreased` | T3 | MEDIUM | 7500 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunGrim_MagLarge` | T3 | MEDIUM | 7500 | earliest compatible weapon tier + HugeMag floor T3 |
| `GunIntegral_MagIncreased` | T3 | MEDIUM | 7800 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunLavina_MagIncreased` | T3 | MEDIUM | 8500 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunPKP_MagIncreased` | T3 | MEDIUM | 9500 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunPKP_MagLarge` | T3 | MEDIUM | 15000 | earliest compatible weapon tier + HugeMag floor T3 |
| `GunZubr_MagIncreased` | T3 | MEDIUM | 8300 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunD12_MagIncreased` | T4 | MEDIUM | 9000 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunD12_MagLarge` | T4 | MEDIUM | 9000 | earliest compatible weapon tier + HugeMag floor T3 |
| `GunD12_MagPaired` | T4 | MEDIUM | 7500 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunDnipro_MagPaired` | T4 | MEDIUM | 7500 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunGauss_MagIncreased` | T4 | HIGH | 19000 | special/endgame placement + cost; no regular-pool fitting weapon |
| `GunKharod_MagPaired` | T4 | MEDIUM | 7500 | earliest compatible weapon tier + upgrade-mag floor T2 |
| `GunSVU_MagIncreased` | T4 | MEDIUM | 10000 | earliest compatible weapon tier + upgrade-mag floor T2 |

### Underbarrels (2)

| SID | Tier | Confidence | Cost | Evidence |
|---|---:|---|---:|---|
| `RU_GLaunch_1` | T2 | MEDIUM | 12000 | AK74-compatible launcher; Experienced safe-world placement; high-cost upgrade |
| `EN_BuckLaunch_1` | T3 | MEDIUM | 9500 | compatible primarily with T2-T4 weapons; median compatible tier T3; high-cost upgrade |

## Compatibility implication for a future shuffle

The next implementation should use:

- attachment **category** as a hard boundary;
- item **progression tier** as the progression boundary;
- `FittingWeaponsSIDs` as the compatibility identity for the attachment itself;
- safe physical world placements only;
- no trader, bench, quest/reward, unique/special or internal component surfaces.

A practical first implementation should mirror the weapon shuffle rule and allow attachment movement only within the **same tier or ±1 tier**, while never crossing attachment categories.

The two shared world generators identified in v0.9.26.44 still require clone-and-redirect if their physical locations are to shuffle independently.

## Confidence

- **HIGH** — direct trader-tier/world-placement evidence is mutually consistent, or endgame placement is unambiguous.
- **MEDIUM** — tier is derived from weapon compatibility, upgrade type, cost and/or mixed placement evidence.

There are no unresolved entries, but the 30 MEDIUM-confidence rows should remain `runtime_test_required` if promoted into production.

## Evidence file

`docs/generators/world-attachment-progression-tier-audit.csv`
