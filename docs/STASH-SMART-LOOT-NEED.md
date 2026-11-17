# SMART LOOT in stashes — visibility and NEED control (2026-11-17)

## How SmartLoot works (2.1 source, `GameData/StashPrototypes.cfg`)

Almost every regular stash carries SmartLoot sets on top of its Basic/Standard/Rare list:
`StashMedicine_Smart` (817 of 831 stashes), `Stash_Ammo_Smart_CommonRare` (766), and a few food /
grenade sets (2–3 stashes). Per player rank each set has groups with `MinSpawnChance` (you are well
stocked) → `MaxSpawnChance` (you are short), `ItemSetCount` (rolls) and Items (Weight or Chance,
Min/MaxCount). Ammo groups are one per caliber (`PriorityCaliber`); only the caliber of a weapon you
carry applies. How the engine measures "need" is not in the CFG.

## SMART columns

ODDS PER STASH (STASH LOOT MIX) and ODDS PER STASH · MEDS (MEDICAL COMPOSITION) have a fourth column
**SMART**: odds that the typical stash's medicine + ammo sets add at least one item of the row,
averaged over the four ranks, as `low–high%` (stocked → short). Ammo = for the caliber of your main
weapon (PrimaryWeaponParams groups averaged). Values come from the writer's own endpoints and
selections (`EffectiveSpawn` / `EffectiveSelection`), so every odds cell, SUPPLY REDUCTION and SMART
LOOT NEED move them. The ADJUST ODDS grid keeps the 4-column geometry; its 4th column is empty.
Model: each set roll spawns with the endpoint chance, then picks one entry by Weight. Estimate, not a
measured drop rate. (A first SMART column was removed on 2026-11-16; this one replaces it, without
hover text.)

## SMART LOOT NEED (Stashes → Stash Smart Loot)

The board's only checkbox and **STASH SMART LOOT** caption sit at the upper-left, using
the canonical checkbox chrome and whole-label hit target. The two centered knobs are
unframed (no individual rectangular cards); their 270° native arcs, values, reference
ticks and mouse/keyboard interactions are unchanged. The lower MEDS/AMMO chance-of-set
panel remains framed. The labels make clear that "stocked" and "low" refer to the
**player's** supplies and that these knobs change the odds of bonus loot, not item quantity.

| Knob | Id | Range | Vanilla | Writes |
|---|---|---|---|---|
| EXTRA LOOT WHEN STOCKED | `loot.stashSmartStockedChance` | 0–2× / 0.05 | 1× | stash SmartLoot `MinSpawnChance` × k when the player has plenty of supplies |
| EXTRA LOOT WHEN LOW | `loot.stashSmartShortChance` | 0–2× / 0.05 | 1× | stash SmartLoot `MaxSpawnChance` × k when the player runs low |

Both run through the existing TCD stash SmartLoot clones (`WriteWorldStashSmartLoot`, actor
redirects incl. DLC1). Final endpoint = source × kept Weight share × (1 − supply reduction) × knob,
clamped 0..1; WHEN STOCKED is never above WHEN SHORT (`StashEffectiveState.SmartNeedEndpoints`, shared
with the previews). The in-dial line shows a typical set (10% / 80%); CHANCE A SET APPEARS shows the
average effective range of the MEDS and AMMO (main weapon) sets with vanilla ticks.

Examples: hardcore "no hand-outs" = EXTRA LOOT WHEN LOW 0.5×; "only helps when you need it" = EXTRA LOOT WHEN STOCKED 0×.
Corpse smart loot is separate (Loot → NPC LOOT, NEED BOOST). Checks: `tools/audit_stash_smart_need.py`.

## Corpse NEED BOOST (single master)

Loot → NPC LOOT → SMART LOOT contains the only ON/OFF checkbox for corpse bonus packs,
NEED BOOST and AMMO PACK SIZE. NEED BOOST is a compact 0–2× dial with no separate checkbox.
The knob value is kept when the master is OFF and resumes when ON; 0× reads FLAT (flat 10%).
Legacy preset field `corpseSmartLootNeedBoostEnabled` is ignored and never saved again.

## SMART LOOT IN STASHES (master switch)

`loot.stashSmartLootEnabled` (toggle, vanilla ON, checkbox at the top of the STASH SMART LOOT board).
OFF sets `MinSpawnChance`, `MaxSpawnChance` and (where present) `ItemSetCount` of every stash SmartLoot
group to 0 in the TCD clones, so stashes hold only their normal Basic/Standard/Rare contents and
hand-placed items. Both EXTRA LOOT knobs `requiresToggle` it: dimmed, values kept. The SMART
columns drop to "—". Corpse Smart Loot is switched off separately by Loot → NPC LOOT → SMART LOOT master; choosing
no BONUS PACKS also leaves NEED BOOST with nothing to shape.

## Consistency review — loot vs smart loot (2026-11-17 evening)

**Source parity.** `Data/smartloot-world-source-entries.csv` was re-parsed against the 2.1
`GameData/StashPrototypes.cfg`: 13 stash prototypes, 991 item rows, every Weight / Chance / Min/MaxCount /
Min/MaxSpawnChance / ItemSetCount identical (0 diffs). Groups also carry `PriorityCaliber` and
`MainWeaponAmmoCount` (392 ammo groups), which TCD never edits. DLC1 has no StashPrototypes of its own.

**Who carries stash SmartLoot (WorldMap_WP):**

| Containers | Count | Covered by stash smart controls |
|---|---:|---|
| Ordinary base stashes (no clue) | 803 | yes |
| Ordinary DLC1 stashes (audited allowlist) | 112 | yes (with DLC1) |
| Protected base stashes (clue / special / quest) | 99 with SmartLoot | no, by design |
| DLC1 stashes outside the allowlist (5 clue, rest unaudited) | 49 | no, by design |
| Breakable wooden crates with `StashPrototypeSID` | 30 base + 2 DLC1 | no (they use the shared *_Cheap / *_Smart sets) |

**Control matrix (who moves SmartLoot):**

| Control | Tier lists | SmartLoot |
|---|---|---|
| SUPPLY REDUCTION | yes | yes (endpoints) |
| Basic MEDS / FOOD / DRINKS cells (`loot.stashMix*`) | Basic only | **yes** (Weight and Chance groups) |
| Standard / Rare cells | their tier | no |
| AMMO / GRENADES selection | Basic presence sidecar, Rare chance | only the hidden legacy `stashMixAmmo` / `stashMixGrenades` |
| Bandage / Medkit / Army Medkit / Anti-Rad item odds | all tiers | yes |
| Medkit / Army Medkit MIN / MAX | all tiers | yes |
| AMMO AMOUNT, RGD-5 AMOUNT | tier lists | **no** (SmartLoot ammo/grenade counts stay vanilla) |
| Smart loot switch, EXTRA LOOT WHEN STOCKED / LOW | no | yes |

Resolved for odds by SMART ODDS (below). SmartLoot counts still follow none of the AMOUNT controls.

## SMART ODDS — loot and smart loot fully separated (2026-11-17)

| Cell (STASH SMART LOOT → SMART ODDS) | Id | Items |
|---|---|---|
| MEDS | `loot.stashSmartMedsOdds` | Bandage, Medkit, Army Medkit, Eco Medkit, Anti-Rad |
| AMMO | `loot.stashSmartAmmoOdds` | every `A…` ammo SID (weapon-caliber groups) |
| FOOD & DRINKS | `loot.stashSmartFoodDrinksOdds` | Bread, Sausage, Canned Food, Water, Vodka, Energetic, Milk, Beer |
| GRENADES | `loot.stashSmartGrenadesOdds` | RGD-5, F1 |

Keep 0–100% of the game's odds (step 5%), vanilla 100%, `requiresToggle` the smart loot switch. Same
rule as the tier cells: Weight groups scale every Weight by X and the set's spawn endpoints by the kept
share; Chance rows scale their Chance. `StashEffectiveState.SmartCategoryId` / `SmartFactor` are shared
by the writer and every preview.

Now **only** these reach SmartLoot: SMART ODDS, the smart switch, EXTRA LOOT WHEN STOCKED / WHEN LOW,
SUPPLY REDUCTION, the per-item medical odds (Bandage…Anti-Rad) and Medkit / Army Medkit MIN/MAX. The
Basic MEDS / FOOD / DRINKS cells and the hidden legacy `stashMixAmmo` / `stashMixGrenades` change only
the Basic/Standard/Rare lists. A legacy preset that lowered a Basic cell therefore no longer lowers
smart loot. C# contracts updated (StashBranchParity, GenericLootPreGame, VerifyWorldStashSmartLoot,
VerifyStashEffectiveParity).
