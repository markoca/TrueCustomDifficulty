# TRADER STOCK consistency review — 2026-11-17

Scope: the Economy TRADER STOCK board (`Controls/TraderBuysMatrix.cs`), the 31 `economy.traderSells.*` SELL ids with the partial-odds sidecar (`TraderSellOddsViewModel`), AMMO / MEDICAL SUPPLY (`economy.traderAmmoStock`, `economy.medicStock`), RARE GEAR, legacy BUY restrictions (`economy.traderBuys.*`) and `CfgPatchWriterService.TraderSells.cs`.

## Verified consistent

| Check | Result |
|---|---|
| SELL anchor is a `toggle` while texts speak in % | By design: 0% = toggle off, 5–95% = sidecar odds, 100% = vanilla; `CurrentDisplay` shows the %. |
| Role × category availability vs 2.1 runtime CSV | Matches the 86-profile union (general 10, medic 4, bartender 6, stalkers 5, technician 6). |
| Ammo profile count "39" | 31 shopkeepers + 5 stalkers + 2 bartenders + 1 technician (Bolshak) = 39. |
| Medical profile count "55" | 30 shopkeepers + 15 medics + 9 stalkers + 1 technician = 55. |
| Supply below 1x | Writer shrinks stacks (never below 1) and moves the remainder into the offer's Chance/Weight (`SupplyOddsFactor`). |
| Partial odds + supply | Multiplied per leaf; Weight offers get a native `Empty` pick. |
| Factory presets | None touch SELL / BUY ids or the odds sidecar. |

## Fixed

1. **AMMO / MEDICAL SUPPLY interaction text** contradicted the logic text ("does not alter availability chance" vs. "whatever they cannot lose comes off the offer's odds"). Rewritten to the real behaviour; the "TRADER STOCK odds sliders" pointer now names the per-type AMMO / MEDICAL rails.
2. **Legacy BUY labels** used internal role names (*General*, *Generic NPC*, anchor *Trader Buys*) while TRADER STOCK uses *Shopkeeper* / *Stalker*. Unified to **Shopkeeper Buys …** / **Stalker Buys …**; `tools/audit_rc_player_labels.py` pins the new anchor label.

## Open — needs a decision

**Technicians sell scopes and silencers.** The locked rule says "TECHNICIAN is not edited: only Bolshak (DLC1) sells items". The 2.1 source shows 15 base-game technician trade profiles (Chemical Plant, Asylum, Ikar, Backwater, Sultansk, Newbie Village, Malakhit, Cement Plant, Rostok, Yanov, Pripyat, Zalesie, Terricon, Power Plug, Varta) whose stock generators offer only Attachments (141 rows; e.g. EN_GoloScope_1 Chance 1.0, RU_Silen_2 Chance 0.1). Because the TECHNICIANS role has no chip, removing or thinning attachments at shopkeepers leaves every technician selling them untouched, and the NAMED TRADER STOCK note repeats the wrong statement.

Options: (a) add a **TECHNICIANS** chip (6 rails: Attachments for the 15 base technicians, plus Weapons/Ammo/Medical/Food/Drinks for Bolshak) — the writer already handles the role once it is in `EditorRoles`; (b) keep the role hidden and only correct the texts.
