# NPC Consumable Chance — Literal CFG Chance (S.T.A.L.K.E.R. 2.1)

**Updated:** 2026-11-15
**Source of truth:** `src/TrueCustomDifficulty/Data/cfg-writer-baseline.json` and `npc-standard-consumable-original-chances-2.1.csv`.

## Controls

The existing **NPC LOOT & EQUIPMENT** board contains exactly ten compact SID-specific optional controls, grouped into **FOOD** (Bread, Sausage, Canned Food), **DRINKS** (Energy Drink, Beer, Vodka) and **MEDICAL** (Bandage, Medkit, Army Medkit, Anti-Rad). No new catalog parameter, independent tab, full-width editor or extra redundant Vanilla/Base labels.

- **Unchecked (default)**: muted **CFG** readout; no SID-specific override; all native `Chance` literals stay intact, except that the existing independent Medical Drops global control still applies to unchecked medical entries.
- **Checked**: the slider reads an **absolute 0–100% chance** in **1% steps**; `Chance = selectedPercent / 100`. For example, choosing **37%** writes `Chance = 0.37`, regardless of whether that particular entry originally had 0.05 or 1.0. **0%** writes 0; **100%** writes 1, guaranteeing the entry's Chance roll **when that entry is eligible and its generator is evaluated**.
- A SID absent from an NPC role/rank pool is not added there. A full NPC drop is still subject to NPC spawn, generator invocation and separate loot pools; 100% is **not** a blanket guarantee that every human corpse has the item.
- The little checkbox only enables the override; disabling retains the last slider value. Right-click on a row/rail or **R** while focused restores the source CFG by disabling its override; **Space** toggles the checkbox; arrows adjust 1%, **Home** is 0 and **End** is 100. Only checked changes are highlighted. Board measures 720 DIP with the same compact 3-column geometry.

## Exact original source

The 2.1 source CSV holds **104** item Chance rows across **six** `GeneralNPC_Consumables_*` role pools, four gated `Diff × PlayerRank` templates per pool and 33 scoped human-source redirects. Same SID can have distinct original chances:

| SID | Rows | Native Chance range |
| --- | ---: | ---: |
| Bread | 8 | 60–100% |
| Sausage | 8 | 60% |
| CannedFood | 10 | 50–100% |
| Energetic | 12 | 15–100% |
| Beer | 4 | 40–100% |
| Vodka | 4 | 10–50% |
| Bandage | 24 | 5–100% |
| Medkit | 20 | 5–100% |
| ArmyMedkit | 6 | 20–30% |
| AntiRad | 8 | 30–50% |

These are **source-entry** probabilities, not measured per-corpse frequencies. `Water` is absent from this standard consumable set; its optional `NPC_Water_Smart` corpse package is independent. Additional `NPC_Medicine_Smart` items likewise remain independently possible.

## Writer / interactions / precedence

The writer reuses one shared six-pool `TCD_GeneralNPC_Consumables_*` clone set and the **same 33 standard-human redirects** when a relevant checked absolute Chance or changed global Medical Drops control warrants it. Within each source row:

- An enabled new SID override **replaces** the original `Chance` and any existing global medical scalar; it is never multiplied by them.
- An unchecked medical SID inherits the existing `loot.npcMedicalChance` multiplier. Unchecked food/drinks preserve their literal source `Chance`.
- The original raw field spelling (including `1` vs `1.0`) stays byte-for-byte for unchanged Chance rows; all source non-Chance fields (`Diff`, `PlayerRank`, `MinCount`, `MaxCount`, IDs, weights) are kept unchanged.
- Existing corpse SmartLoot, weapons, world, traders, zombies, quest/named NPCs and mod-item sidecars are **not** patched by these ten controls.
- A checked SID triggers exactly one logical editor record; multiple checks still generate only one shared clone/redirect fanout. All disabled with no global medical change produces no new generator.

## Preset compatibility

New presets save `npcConsumableChances[SID].enabled` plus **`chancePercent`** (0..100, 1% steps). A new checked control always has absolute semantics. Old presets with only **`multiplier`** continue to load and write the original 0..2× per-row scaling behavior **without silently changing balance**. Legacy active controls display **`×`** rather than falsely advertising an absolute percent. Moving the rail / using arrows switches that item to the new literal `chancePercent` format. Presets without item sidecars default all items to unchecked. Reset current Loot/reset all restores CFG mode.

## Validation boundary

`tests/LootSpecialGeneratorsContract/NpcConsumableChanceFirstPass.cs` checks all ten SIDs at 0%, 1%, 37%, 50% and 100%; all 104 Chance leaves, unchanged non-Chance fields and exactly 33 redirects; mixed global-medical override precedence; unchecked/no-op behavior; new-preset roundtrip; and old 0.5× compatibility.

```bash
dotnet run --project tests/LootSpecialGeneratorsContract/LootSpecialGeneratorsContract.csproj -c Release
dotnet run --project tests/AllControlsPatchContract/AllControlsPatchContract.csproj -c Release
./publish.sh
```

**Runtime qualification:** writer/compiler tests do not prove in-game per-corpse frequencies. Observe newly spawned ordinary human NPCs with source-role/rank and separate SmartLoot distributions distinguished.


### NPC SmartLoot board integration (2026-11-15)

The optional standard consumable Chance editors now sit at Y=553 in the compact 720-DIP NPC board. The 98-DIP ammo preview begins at Y=238; the 115-DIP condition card at Y=348; the centered 420-DIP-capped 68-DIP MOD ITEMS card at Y=474. The independent top **CORPSE SMART LOOT** three-chip selector, **BONUS AMMO QTY** (bonus stack quantity, only for Medicine + Ammo), and **STANDARD MED** (source Chance multiplier, independent of SmartLoot) remain separate controls with original writer IDs. See [source-audited package semantics](SMARTLOOT-CORPSE-BONUS-AUDIT.md).
