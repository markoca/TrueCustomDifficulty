The [5,013-edge consumer graph](STASH-CONSUMER-GRAPH-2026-10-16.md) now expands every compound physical actor list and blocks an entire shared tier wrapper if any contained actor is protected. The special Rostok wrapper is excluded from generic patch output; pre-game cases remain NOT_RUN.

**SOURCE-EXECUTION AND CFG DIAGNOSTIC UPDATE (2026-10-17):** [13 isolated category/bonus output scenarios, explicit original source mapping and real production-writer BPatch field checks](GENERIC-NPC-STASH-EXECUTION-AND-CFG-PARITY-2026-10-17.md) are now wired to LootSpecialGeneratorsContract. They verify 58 NPC medical Chance rows/46 unaffected consumable rows, 33 production redirects, 43 corpse bonus ammo counts, corpse set-list activation, independent tier versus SmartLoot stash fields and cross-domain safety. Export via `./tests/run-wine.sh LootSpecialGeneratorsContract --out audit/generic-loot-fixtures` on local Wine/.NET. No source-level test proves game spawn activation; 77 original manual cases remain NOT_RUN.

# Generic world stashes + ordinary human corpses — pre-game test readiness

**Scope locked before the first physical game test. Source matrix preparation
is not in-game acceptance.** All cases start NOT_RUN.

## What is the priority?

1. Generic/ordinary world ItemContainers, preserving Basic/Standard/Rare
   ItemGenerator and independent world SmartLoot source layers.
2. Lootable corpses of ordinary repeatable human NPCs, preserving standard
   human consumable generators, difficulty-selected corpse SmartLoot packages
   and bonus ammo stacks.
3. Richter's Cabin two historically observed direct Chance=1 Medkits and
   a separately authored Rostok medical row remain **regressions only**.

This plan deliberately does not expand the normal UI, preset schema, loot
writer or actor allowlists. Quest, named/scripted, unique, prologue, zombies,
mutants, world pickups and loot crates are NOT generic targets.

## Exact planned case counts

| Domain | Source-approved plan | Cases | Current game result |
| --- | --- | ---: | --- |
| Ordinary generic ItemContainers | 15 exact actor + source + category baseline pairs plus zero/high variants | **35** | All NOT_RUN |
| Cabin/Rostok direct-source regressions | Recorded historic two-Medkit issue and non-medical negative control | **10** | All NOT_RUN |
| Ordinary human medical | 6 original role archetypes at Veteran plus Recon Master rank/difficulty gate, each with baseline/0x/2x | **21** | All NOT_RUN |
| Corpse SmartLoot list | Novice Medicine+Ammo / None / Water plus Stalker Water and Veteran None reference | **5** | All NOT_RUN |
| Corpse bonus ammo counts | Novice active bonus 1x/0x/3x plus Stalker Water inactive 3x negative | **4** | All NOT_RUN |
| Cross-layer NPC medical | Novice standard medical 0x with corpse package None versus Medicine+Ammo | **2** | All NOT_RUN |
| **Total** | **45 stash + 32 NPC** | **77** | **77 NOT_RUN** |

## Game sample selection constraints

- The ordinary stash matrix uses physical ActorSID values from the audited
  regular-world actor manifest and its exact ItemGenerator/SmartLoot redirect
  provenance. That is a **candidate physical source**, not a confirmed
  accessible waypoint or proof that both branches roll every visit.
- The ordinary NPC matrix uses exact standard-human archetype and loadout
  source strings from the **33-link repeatable-human-only** medical redirect
  manifest. These are *not individual physical NPC GUIDs*: identify the
  actual ordinary NPC/archetype in-game before marking PASS.
- The 99 DeadBody CorpseStashSID links are not generic corpse SmartLoot;
  31 of the 39 direct target generators leak into other contexts and are
  excluded from broad edits. Source-only evidence cannot collapse them.
- For Medical=0 on a normal human, explicitly account for
  NPC_Medicine_Smart separately. On Novice it is part of the default
  difficulty corpse bonus. On Veteran/Master the vanilla bonus list is
  empty, hence those difficulties anchor isolated standard consumable
  tests. Never call absence/appearance of all medical items proven by
  a single generator edit.
- For ordinary stashes, source Chance and native group Weight, count,
  spawn-group endpoints and first-time generation are distinct gates;
  the full physical drop probability is not inferred from any one field.
- Preserve a truly fresh pre-generation save and test the same game build,
  difficulty, progression rank, DLC and mod order across comparisons.
  Record observed items individually per container or killed NPC;
  do not pool an actor with its alternate source family.
- Do not mislabel reloads of a previously generated container/NPC as
  independent random trials.

## Files and acceptance gates

Primary [stash protocol](STASH-INGAME-LOOT-TYPE-VALIDATION-2026-10-16.md)
and [45-case stash CSV](stash-ingame-loot-test-matrix.csv).

Primary [ordinary NPC protocol](NPC-CORPSE-INGAME-LOOT-TYPE-VALIDATION-2026-10-16.md)
and [32-case NPC CSV](npc-corpse-ingame-loot-test-matrix.csv).

Run from repository root:

    python3 tools/audit_stash_ingame_test_matrix.py
    python3 tools/audit_npc_corpse_ingame_test_matrix.py
    python3 tools/audit_stash_generator_branches.py
    python3 tools/audit_stash_consumer_graph.py
    python3 tools/audit_project_consistency.py
    ./tests/run-wine.sh LootSpecialGeneratorsContract

The first five are source/static checks, if executed; the sixth generates
and tests real CFG output under Wine, if successful. None validates
unobserved in-game RNG. Record independent game observations with
identity, exact source/prototype, applied PAK, untouched generation
state, per-item count, save ID and screenshot/log evidence.

**Stop:** No new whole-stash probabilities, generic corpse 0% guarantees
or Phase 5 gameplay acceptance claims until enough attributable newly
generated physical observations exist. Missing producer attribution is
INCONCLUSIVE, not a reason to alter protected/shared sources.
