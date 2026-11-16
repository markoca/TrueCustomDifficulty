# Stash loot: Weight vs Chance — the one rule (2026-11-16, CURRENT)

This is the authoritative definition used by the CFG writer, ODDS PER STASH, ADJUST ODDS and the Basic MIX preview. All of them call the same code (`StashEffectiveState.WeightListEdit` for Weight lists, `TierField` for Chance rows). `tools/audit_stash_presence.py` guards it.

## How the game reads an `ItemGenerator` list (`PossibleItems`)

| List type | How the game uses it | What "less" means |
|---|---|---|
| **Weight list** — every entry has `Weight` | Exactly **ONE entry** is picked per list, with probability `Weight / sum of Weights`. It never yields nothing unless an `Empty` entry is in the list. | Lowering one entry's Weight hands its share to the **other** entries. |
| **Chance list** — every entry has `Chance` | Every entry rolls on its own (`0` never, `1` always). | Lowering one Chance affects only that entry. |

Weight and Chance **never mix** in one list.

### Evidence

* **Our 2.1 dump scan** (`GameData/ItemGeneratorPrototypes.cfg`): of **13564** `PossibleItems` lists, only **3** mix Weight entries with Chance entries, all NPC grenade loadouts (`GuardNPC_Freedom_Stormtrooper`, `GuardNPC_Varta_Stormtrooper`, `GuardNPC_Noon_Recon`: RGD-5 `Chance 0.3` + F1 `Weight 1000`). Entries carrying *both* fields are the generator template defaults (`Weight = 4.f`, `Chance = 0.f`), not authored loot. The Basic / Standard / Rare tier templates (41 lists) and all **422** SmartLoot world groups have **0** mixed lists.
* **Community documentation** — *Desolation Patch Tutorial — Editing Generators* (Nexus Mods, S.T.A.L.K.E.R. 2 article 150): Weight "picks a random SINGLE item out of the pool"; without an empty entry such a generator "will ALWAYS return one item and never nothing"; the way to allow "nothing" is an entry with `ItemPrototypeSID` set to empty; Chance and Weight cannot be combined in one list ("a broken generator").
* **Vanilla `Empty`**: `ItemPrototypeSID = Empty` appears 56 times; 55 of them are `Weight` entries (NPC equipment slots meaning "no item"). No vanilla list puts `Empty` next to real items, so the mixed use below needs an in-game check (RELEASE-TESTING.md → *Stash presence*).

### What is NOT known from CFG alone

* The exact RNG of a Chance roll (assumed: independent uniform roll per entry).
* Whether every category of an `ItemGenerator` is always processed. Assumed yes (vanilla tier templates list several `Consumable` categories with `bAllowSameCategoryGeneration = true`). ODDS PER STASH is therefore labelled as approximate (≈).

## How the controls apply it (regular Basic / Standard / Rare stashes)

Every cell in **ADJUST ODDS** means: **keep X% of the game's own odds** for that kind in that tier, without changing the other kinds.

| Cell kind | List type | Writer action |
|---|---|---|
| Standard / Rare MEDS, FOOD, DRINKS, GRENADES, COUPONS; Rare AMMO (early-game rows) | Chance | Scales each authored `Chance` (Rare COUPONS and Basic GRENADES are a direct chance). |
| Basic MEDS / FOOD / DRINKS / COUPONS (`loot.stashMix*`) | Weight (Basic shared pick: milk, bread, sausage, canned food, bandage, sometimes a coupon; Basic water/energy list) | Scales that kind's Weight **and** puts the removed Weight into an appended `Empty` entry, so the list total is unchanged and the other kinds keep their odds. |
| Basic / Standard AMMO (presence sidecars), Rare AMMO (`loot.expensiveStashAmmoChance`) | Weight, ammo-only lists | Scales the whole list the same way; the removed share becomes `Empty`. |

`Empty` is written as `[n] { ItemPrototypeSID = Empty; Weight = removed }` after the source entries, only in the TCD tier template clones. At 100% nothing is appended. A legacy factor above 1× (old presets) leaves no `Empty` and only boosts that kind relative to the others.

## SmartLoot world stashes

A SmartLoot group spawns with a native chance between `MinSpawnChance` and `MaxSpawnChance` (player need) and then picks by Weight. For every **Weight** group the writer scales each item's Weight by its own factor k and both spawn endpoints by the kept share Σw·k / Σw (`StashEffectiveState.SmartKeptShare`). Item odds = spawn × share, so every item keeps exactly k of its game odds — the same rule, native fields only, no `Empty`.

## Medical items

Each medical item exists in Chance rows (Basic / Standard / Rare) and in Weight picks (Basic shared pick, SmartLoot). MEDICAL COMPOSITION therefore shows ONE control per item; its native Chance scale and Weight factor are mirrored so both list types keep the same share of game odds.

## Paths that still use plain relative Weight scaling

DLC1 regular stash rows (`StashDlc1`), Cabin and direct medical rows still scale Weight relatively (no `Empty`); they are outside ODDS PER STASH.
