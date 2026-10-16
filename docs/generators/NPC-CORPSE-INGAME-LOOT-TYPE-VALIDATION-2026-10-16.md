**PRE-GAME SOURCE/WINDOWS CFG DIAGNOSTICS (2026-10-17):** [Production generator execution map and 13-source fixture suite](GENERIC-NPC-STASH-EXECUTION-AND-CFG-PARITY-2026-10-17.md) checks ordinary-human medical 0x/2x, separate corpse package states and selected/inactive 43-entry extra ammo source. These verify emitted CFG, not which items a live NPC will actually drop. Every manual NPC case remains NOT_RUN.

# Ordinary human NPC corpses — source-separated in-game loot validation

**Status: TEST PLAN ONLY; no in-game observations accepted.** This is the second primary target alongside [ordinary world stashes](STASH-INGAME-LOOT-TYPE-VALIDATION-2026-10-16.md).
The [32-case NPC matrix](npc-corpse-ingame-loot-test-matrix.csv) and [audit CLI](../../tools/audit_npc_corpse_ingame_test_matrix.py) document what to verify.
Neither a CFG line nor a manifest reference proves that a particular killed enemy drops an item.

## Scope: generic human enemies, not every searchable body

Choose ordinary repeatable living humans from the **33 active, production-only**
NPC consumable redirect links; sample CloseCombat, Heavy, Noon, Recon, Sniper and
Stormtrooper consumable source families (Noon is a faction-specific pool). The matrix pins one original loadout SID and its recorded
external-consumer archetype per role. These are **source archetypes, not physical
spawn actor GUIDs or confirmed world waypoints**. The tester must establish
the actual in-game NPC identity and clean generation state separately.

**Hard exclusions:** named/scripted characters, Prologue, quest rewards,
zombies, mutants, world containers, breakable crates, traders, special
CorpseStashSID dead-body overrides, and weapon-attached ammunition when
measuring loose or bonus corpse stacks. The 39 corpse-override generator
targets include 31 shared across other gameplay contexts. Never globally
edit them to force a corpse-only result.

## Three independent ordinary-corpse sources

1. **Standard human consumables.** Six GeneralNPC_Consumables role pools
   contain four native rank/difficulty gate blocks and 104 active item rows.
   The existing loot.npcMedicalChance writer clones all six pools to
   TCD_GeneralNPC_Consumables_*; changes **only 58 medical Chance rows**;
   redirects only **33** standard-human links; does not adjust food, water,
   alcohol or counts. Medical 0x is **not** a promise that a whole corpse
   contains no medicine when another source is active.
2. **Difficulty corpse SmartLoot selection.** Under
   GameData/DifficultyPrototypes.cfg, EconomyDifficulty.CorpseSmartLoot.GeneralNPC
   is selected by loot.npcCorpseBonus: Novice/internal Easy has
   NPC_Medicine_Smart + NPC_Ammo_Smart; Stalker/internal Medium has
   NPC_Water_Smart; Veteran/Hard and Master/internal Stalker have none.
   The control chooses a package list, not a numerical probability.
3. **Extra corpse bonus ammo counts.** loot.npcWeaponAmmoSupply scales 43
   MinCount/MaxCount source entries in the TCD_NPC_Ammo_Smart clone, retaining
   24 rank/caliber/Weight groups. The clone is emitted **only if** the
   current corpse bonus package list contains NPC_Ammo_Smart. With Water or
   None the saved multiplier has no active clone. This is not loaded weapon
   ammo or a generic NPC ItemGenerator ammo stack.

## Execution sequence

**Wave A: ordinary human medical (21 cases).** Use Veteran (its default
corpse bonus is None). For six verified consumable-pool families compare source
reference Medical 1x, Medical 0x and Medical 2x at fixed difficulty and rank.
Recon additionally repeats the 1x/0x/2x experiment on Master to cover the
native alternative difficulty branch. Each baseline is source-reference,
**not a guaranteed item**. Use independently newly generated NPCs rather
than reloading the same rolled corpse.

**Wave B: bonus package identity (5 cases).** On Novice inspect the original
Medicine+Ammo package, explicitly select None and Water, then verify the
Stalker-original Water and Veteran-original None negative/reference cases.
Record medical and ammo bonus items separately from standard consumables.
This does not prove item-level SmartLoot activation rates.

**Wave C: bonus ammo quantity (4 cases).** With Novice Medicine+Ammo active,
compare original 1x, 0x and 3x NPC_Ammo_Smart stack counts. On Stalker with
its original Water package, set ammo bonus 3x as a negative control: the
writer must not create an unused ammo clone. Record corpse inventory ammo
separately from loaded/carried weapon ammo and independent ItemGenerator
stacks, even if the resulting on-screen item name is identical.

**Wave D: cross-layer contradiction check (2 cases).** On Novice set regular
human Medical to 0x in BOTH runs. In the first also explicitly set the
corpse package to None; in the second retain Medicine+Ammo. This is a
deliberate **two-control comparison**, not a single-knob experiment.
A Medkit in the second case does not disprove the standard medical 0x
change. If source attribution is still ambiguous, require a separate
actor/source-scoped debug CFG patch; do not add a normal persisted UI knob.

Do not edit shared global GeneralNPC_Consumables or GamePass_Stash parents
to create a testing shortcut, and never expand testing into quests or
named/scripted source SIDs to make a generator easier to reach.

## Per-body evidence

For **each case and newly generated ordinary NPC/body** record: case_id,
game version, TCD commit and PAK/hash, map and location, actual NPC identity
or reliable source-archetype match, faction and role, native difficulty,
player progression rank, selected package list, every modified knob,
DLC/mod load order, save state indicating NPC/body not already generated,
expected and emitted CFG path/value, individual corpse loot SID/count,
carried weapon and its ammo **separately**, screenshot/log, outcome
PASS/FAIL/INCONCLUSIVE, and alternate producing sources.

Until actual bodies have been tested, all rows remain NOT_RUN.
Evidence for an accepted result must include tokens such as
evidence=...; save=...; NPC=... in the actual_in_game CSV field.
One kill, or multiple reloads of the same generated body, is not enough
to infer item-drop odds or claim a doubled in-game frequency.

0x acceptance requires an exact generated patch against the audited
production source and an independently generated physical NPC. If other
sources can still produce an item, mark result inconclusive until isolated
instead of weakening the writer assertion.

## Pre-game commands

Run after pulling the same repository commit:

- python3 tools/audit_stash_ingame_test_matrix.py
- python3 tools/audit_npc_corpse_ingame_test_matrix.py
- python3 tools/audit_stashes_mixer_ui.py
- ./tests/run-wine.sh LootSpecialGeneratorsContract

Python checks validate source provenance, planning and evidence formatting
only. Successful Wine/.NET writer tests validate output, not physical
corpse loot or engine RNG. In-game acceptance is separate and pending.
