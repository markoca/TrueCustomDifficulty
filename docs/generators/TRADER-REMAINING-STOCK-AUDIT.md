# Remaining Trader Stock Domain Audit

## Decision

**No additional normal UI control is promoted.** After ammo, medic, weapons and attachments were audited separately, the remaining trader-stock categories do not expose another complete global 1:1 stock surface.

Two narrow surfaces are structurally clean enough to document — `BartenderCNPP_TradeItemGenerator` consumable counts and `RC_TraderNPC_ItemGenerator` junk counts — but both are special-purpose/localized stock models rather than general trader controls.

## Domain summary

| Domain | Direct rows | Roots | Count variants | Rows without count | Availability model | Rank-gated rows | Decision |
| --- | ---: | ---: | ---: | ---: | --- | ---: | --- |
| Consumable | 170 | 17 | 24 | 0 | Chance + Weight | 103 | NO GLOBAL UI — multiple stock families; general trader pool is cross-context |
| BodyArmor | 138 | 21 | 3 | 19 | Chance + Weight + direct/implicit | 133 | NO UI — mixed Chance/Weight/count/direct models across many trader roots |
| Artifact | 49 | 20 | 7 | 0 | Chance | 45 | NO UI — highly trader-specific Chance and count semantics |
| Detector | 15 | 13 | 2 | 0 | Chance | 14 | NO UI — sparse per-trader special stock |
| Junk | 16 | 1 | 1 | 0 | Chance | 0 | NO GLOBAL UI — one isolated RC trader model only |
| Head | 4 | 3 | 2 | 3 | Chance + Weight + direct/implicit | 3 | NO UI — tiny mixed special surface |

Full numeric summary: [`trader-remaining-stock-surface-summary.csv`](trader-remaining-stock-surface-summary.csv).

## General trader consumables

`Trader_Cosnsumables_ItemGenerator` itself is structurally attractive: **17 authored rows**, every row has `MinCount` and `MaxCount`, and the rows preserve vanilla `PlayerRank` progression. Most rows use `Chance=1`; ArmyMedkit/EcoMedkit rarity rows use lower Chance values that can remain untouched while counts are scaled.

However, the existing generator-domain graph classifies this pool as **SHARED CROSS-CONTEXT POOLS** with impact contexts `quest_scripted, trader_stock`. It is explicitly marked `EXCLUDE FROM GENERIC TUNING — scripted/quest`. Editing the target definition would therefore change more than trader stock.

The 29 trader-local links into this pool are isolated source edges, but scaling those links would change whether the entire consumables pool participates, not the amount of each consumable. That is not equivalent to stock amount.

**Decision: no `Trader Consumable Stock Amount` control.** The target's counts are clean, but its consumer boundary is not trader-only.

## Bartender consumables

`BartenderUniversal_TradeItemGenerator` also has clean rank-based consumable counts, but its consumer graph includes both trader-stock consumers and named human-NPC inventory consumers. It is therefore another cross-context target and cannot be safely edited for a trader-only control.

The DLC1 `BartenderCNPP_TradeItemGenerator` is different: it is a trader-only target with **29 consumable rows**, all with explicit count ranges and `Chance=1`. This is a technically clean amount surface for that specific CNPP bartender stock.

**Decision: document only, no normal UI.** A slider called `CNPP Bartender Consumable Stock Amount` would be source-exact, but it is too location/special-case specific for the normal difficulty UI and would not affect other bartenders.

## Armor

Armor stock spans many independently authored trader roots. Most rows use fixed `1/1` count and vary availability through `Chance`; other roots use `Weight`, omit count fields, or contain special rank splits. Increasing `1/1` counts would create duplicate armor copies rather than meaningfully broadening stock.

**Decision: no `Trader Armor Stock Amount` or global armor availability slider.**

## Artifacts and detectors

Artifacts and detectors are intentionally sparse, location-specific trader extras. Their count ranges and `Chance` values vary by trader and often represent special inventory identity rather than a common stock pool.

**Decision: no aggregate UI control.** Scaling them globally would erase authored trader identity and would combine unrelated special stock rules.

## Junk / RC trader

All **16** audited Junk rows live in `RC_TraderNPC_ItemGenerator`. Every row uses `MinCount=2, MaxCount=3`; 15 use `Chance=1.0` and one uses `Chance=0.0`.

This is a clean count surface, but it is one special trader model only.

**Decision: document only, no normal UI.** If an advanced per-location/special-trader page ever exists, `RC Trader Junk Amount` would be source-exact.

## Final trader-stock policy

The normal UI keeps only trader-stock controls that map to broad, complete and observable semantics:

- `Trader Ammo Stock Amount` — accepted, audited T1–T4 ammo quantity surface.
- `Medic Stock Amount` — accepted, isolated medic quantity surface.
- Weapons — closed no UI.
- Attachments — closed no UI.
- General consumables — closed no UI because the clean target pool is cross-context.
- Armor, artifacts, detectors, head gear — closed no UI because stock is trader-specific/mixed.
- CNPP bartender consumables and RC junk — exact but intentionally too narrow for normal UI.

This closes the current Trader Stock normal-UI search unless a future design intentionally introduces an **Advanced / Special Traders** section.
