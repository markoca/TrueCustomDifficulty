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

| Knob | Id | Range | Vanilla | Writes |
|---|---|---|---|---|
| WHEN STOCKED | `loot.stashSmartStockedChance` | 0–2× / 0.05 | 1× | every stash SmartLoot `MinSpawnChance` × k |
| WHEN SHORT | `loot.stashSmartShortChance` | 0–2× / 0.05 | 1× | every stash SmartLoot `MaxSpawnChance` × k |

Both run through the existing TCD stash SmartLoot clones (`WriteWorldStashSmartLoot`, actor
redirects incl. DLC1). Final endpoint = source × kept Weight share × (1 − supply reduction) × knob,
clamped 0..1; WHEN STOCKED is never above WHEN SHORT (`StashEffectiveState.SmartNeedEndpoints`, shared
with the previews). The in-dial line shows a typical set (10% / 80%); CHANCE A SET APPEARS shows the
average effective range of the MEDS and AMMO (main weapon) sets with vanilla ticks.

Examples: hardcore "no hand-outs" = WHEN SHORT 0.5×; "only helps when you need it" = WHEN STOCKED 0×.
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
hand-placed items. WHEN STOCKED / WHEN SHORT `requiresToggle` it: dimmed, values kept. The SMART
columns drop to "—". Corpse Smart Loot is switched off separately by Loot → NPC LOOT → SMART LOOT master; choosing
no BONUS PACKS also leaves NEED BOOST with nothing to shape.
