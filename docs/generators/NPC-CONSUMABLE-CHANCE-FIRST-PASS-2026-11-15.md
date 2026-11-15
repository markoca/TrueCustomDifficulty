# NPC Consumable Chance — First Implementation Pass

**Date:** 2026-11-15  
**Game source:** pinned S.T.A.L.K.E.R. 2.1 CFG writer baseline. Original raw source values are preserved in `npc-standard-consumable-original-chances-2.1.csv`.

## What shipped

The existing NPC LOOT & EQUIPMENT renderer has a compact row-level editor with three native-styled panels: **FOOD** (Bread, Sausage, Canned Food), **DRINKS** (Energy Drink, Beer, Vodka) and **MEDICAL** (Bandage, Medkit, Army Medkit, Anti-Rad).

Each SID row: small independent checkbox, label, 0–200% source-relative Chance rail in 5% steps, right-aligned changed-only yellow percentage. Unchecked is greyed out/default; its last percentage is remembered, but no patch is generated from the unchecked row. 100% is the neutral source reference; right-click active rail returns its multiplier to 100%, and Space/arrows/R work for the selected item. This is integrated inside the existing 836-DIP NPC frame (not a floating strip, a separate tab, or an additional 359-catalog parameter).

## Literal source evidence: no normalized baseline

The companion CSV holds **exactly 104** original item rows, one row for every pool/category/entry tuple, with unmodified original `ChanceRaw`, `ItemPrototypeSID`, `MinCountRaw`, `MaxCountRaw`, `DiffRaw`, `PlayerRankRaw` and original node index. Entries retain their source lexical spelling (e.g. `1` vs `1.0`) and all native gating, not flat percentages.

| SID | Source Chance rows | Source original Chance interval |
|---|---:|---:|
| Bread | 8 | 0.60–1.00 |
| Sausage | 8 | 0.60 |
| CannedFood | 10 | 0.50–1.00 |
| Energetic | 12 | 0.15–1.00 |
| Beer | 4 | 0.40–1.00 |
| Vodka | 4 | 0.10–0.50 |
| Bandage | 24 | 0.05–1.00 |
| Medkit | 20 | 0.05–1.00 |
| ArmyMedkit | 6 | 0.20–0.30 |
| AntiRad | 8 | 0.30–0.50 |

Total 26 Food + 20 Drinks + 58 Medical = 104 rows, across six original `GeneralNPC_Consumables_*` role pools, with exactly 33 repeatable-human source redirects. **Water is NOT a normal source row** here: it is in the optional difficulty-specific `NPC_Water_Smart` corpse package.

## CFG writer algorithm

- Emit no new NPC ItemGenerator patch when all source scalars are unchanged and the optional per-item checkboxes are OFF (or all enabled items are at 100% and the global Medical Drops scalar is unchanged).
- If any effective per-item change occurs, emit **one shared set of six `TCD_GeneralNPC_Consumables_*` clones**, preserving all 104 source item definitions and their native rank/difficulty gates, then redirect the exact existing 33 repeatable-human `SubItemGenerator` links. Never create duplicate clones or independent competing pools for the item subgroups.
- For each entry, resolve by **exact `ItemPrototypeSID`**. If an enabled per-item medical override exists, it **replaces** the current global `loot.npcMedicalChance` scalar for that SID; other medical SIDs keep global multiplier. Food and Drinks use their individual multiplier when enabled, and native 1x when disabled. This is never a multiplication of global and item sliders.
- Only when resolved multiplier differs from 1.0 is a numeric source Chance multiplied and clamped to 0..1; when 1.0, write the **literal original source token verbatim**, including the original `1` / `1.0` distinction. All other fields (`ItemPrototypeSID`, quantity, role membership, rank/difficulty, weights) are untouched.
- Fail closed if original pools are not exactly six, rows not 104, medical rows not 58, SID row counts differ from 24/20/6/8/8/8/10/12/4/4, or the existing medical entry's `sourceChanceRaw` differs from its actual original item field.
- Preserve the existing `loot.npcMedicalChance` global slider, `loot.npcCorpseBonus` packages, `loot.npcWeaponAmmoSupply`, 121 weapon condition rows and corpse MOD ITEMS sidecar independently. Do not patch original shared vanilla generator pool, quest/SID, zombie, trader or world-stash branches.
- Keep the fixed 359 native parameter catalog unchanged. Add `npcConsumableChances` as ten independent optional preset sidecars containing `enabled` and a 0..2 multiplier. Old presets without the field load all disabled at exactly 1.0; percentages survive unchecked toggles. Reset Loot/Reset All and changing Base Difficulty reset overrides to the source default, as do other native controls.

## Static and production tests

The new `VerifyNpcConsumableChanceFirstPass` in `tests/LootSpecialGeneratorsContract` exercises each SID at **0%, 50%, 100%, 200%**, checks all **104 literal/source-relative Chance results and original item fields**, verifies the **33 source-only redirects**, confirms disabled/no-op settings write no CFG, and tests mixed global Medical × individual Medical override precedence and preset serialization.

Use:

```bash
dotnet run --project tests/LootSpecialGeneratorsContract/LootSpecialGeneratorsContract.csproj -c Release
dotnet run --project tests/AllControlsPatchContract/AllControlsPatchContract.csproj -c Release
./publish.sh
```

These are authoring/build gates, not evidence of live gameplay. The actual in-game item-drop distribution must be sampled on newly generated standard human NPCs with other SmartLoot packages distinguished.

## Outstanding boundaries

This pass **does not** add a separate Water slider, per-NPC role selector, item-count editing, named NPCs, zombies or cross-context patching. In-game generation and Unreal BPatch merge acceptance remain unverified until tested with the actual game. Original authored Chance values can differ by source role, difficulty and progression rank, so an on-screen 200% multiplier is **not** a guarantee of a drop.
