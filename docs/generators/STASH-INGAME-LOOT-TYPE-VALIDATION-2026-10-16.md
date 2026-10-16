# Stashes — controlled in-game loot-type validation campaign

**2026-10-16 | STATUS: NOT RUN on the current build.** Generic ordinary stashes are the primary scope; Richter Cabin and other bespoke direct generators are regression-only. This is an executable *human game-test protocol* and a source-pinned [45-case matrix](stash-ingame-loot-test-matrix.csv), **not** a claim that the game was tested by GitHub, Wine, or this document. Preparation precedes accepting Phases 3–5; Phases 1–2 remain source-level evidence. Do not claim 100% physical stash drops from a CFG `Chance=1` row.

## The actual earlier 100% medical finding — do not lose the evidence

The original user observation was **exactly two Medkits across three Cabin containers** during **A Tough Awakening** (after Richter, before Zalissya), even with `MEDICAL=0x`. In [Cabin source audit](CABIN-DIRECT-MEDKITS-2026-10-16.md), two independent direct **item-level** `Medkit Chance=1 / MinCount=1 / MaxCount=1` rows were traced *outside* the regular Cheap and `StashMedicine_Smart` paths. The 100% number is the **conditional item-row Chance**, NOT a measured 100% probability for an unopened physical container. The old mod PAK had no SmartLoot patch; later source changes addressed SmartLoot and then these direct Cabin Medkits. **None of that is a current in-game acceptance result.**

| Container | ActorSID | Original producers | Extra direct source |
| --- | --- | --- | --- |
| Cabin BigSafe | `1C2FD1C141D6DA11E518758CC1CD36AE` | local wrapper, Cheap, `StashMedicine_Smart`, ammo SmartLoot | `LesserZone_Cabin_StashGenerator1_...` + special internal SID with a space; Medkit at `ItemGenerator.[1].PossibleItems.[0]` |
| Cabin Bag | `9BCFDFE947FE7F7D5F5451B253F8B68B` | local wrapper, Cheap, `StashMedicine_Smart`, ammo SmartLoot | **none of the two** |
| Cabin SmallSafe | `59FDC1CD4D0D094270C695A18D84373A` | local wrapper, Cheap, `StashMedicine_Smart`, ammo SmartLoot | `LesserZone_Cabin_StashGenerator3_...` + special internal SID with a space; same item path |

Do **not** replace this 2-of-3 observed total with an unsupported claim that *all* medical contents were previously guaranteed or that all other medkits came from these rows.

## What exactly must be established, in order

1. **Source activation:** for each selected physical ActorSID, is the original item generator or SmartLoot `StashPrototypeSID` actually used for a newly populated world container? An edge in a manifest is only a candidate until observed.
2. **Type suppression:** in a **single-knob** `0x` build, are the selected source's item/selection/count fields actually suppressed and absent from fresh in-game generation? If an item remains, first check parallel SmartLoot, local direct items, separately authored rewards, world pickups, quest paths, mod load order and cached contents.
3. **Source isolation:** a mixed-layer container cannot show *which layer made a given Medkit/ammo* merely by looking at the final inventory. Individual *source* attribution requires a further actor-scoped debug patch that disables **only one source** at a time (tier vs SmartLoot vs direct). This capability is **NOT implemented in the main app**. Do not pretend that one global category knob isolates layers.
4. **Increased values:** `3x` changes authored `Weight` or clamped `Chance`, not a universal 3x physical-drop rate. Compare independent new containers with the same native rank/difficulty and source; preserve zero/native Chance and one-category Weight normalization. Low-spawn SmartLoot, especially `StashFood_Smart` with small native spawn endpoints, may rarely manifest even when patched correctly.
5. **Protected paths:** avoid named-clue, blueprint, quest, scripted and unique stashes in the normal control group; do not infer their eligibility from a nearby stash with the same appearance. A fixed world pickup or dialogue reward is NOT an ItemContainer drop.

## Practical game setup

- Use one exact game build (the tracked source baseline is **STALKER 2 v2.0.5**), same difficulty, player rank, DLC state, other mods, save starting point, and installed `TrueCustomDifficulty` output across comparisons. Log if they differ.
- Make separate **vanilla/reference**, **single change 0x**, **single change 3x**, and where stated **negative control** installs/runs. Reset all other Stashes and relevant Loot settings to their **selected difficulty reference** before changing the *one* named control. For Medkit `MAX=-1`, this is the intentional `0 ITEMS` sentinel, not `0x` bias.
- Back up a save **before the relevant container has been generated**, not merely before it was opened. The exact generation trigger (proximity, first discovery, quest activation, save loading, opening) is not established. If it may already be generated, use a clean new-game/save branch or a different demonstrably untouched actor. Reloading the same rolled inventory is **not** an independent RNG trial.
- Record current item counts **before opening**, everything obtained **after opening**, and whether the stash was previously visited/generated. Do not combine two containers' contents. Record a screenshot/video and the exact `ActorSID` tied to location, plus applied profile, built PAK name/hash if available, difficulty/rank/DLC/mod order, and native CFG outputs.
- An actor GUID is a **source-accurate target, not a documented in-game waypoint**. Coordinates in `smartloot-regular-world-actor-manifest.csv` are source world coordinates; locate visually/with your own mapping before calling it verified in-game.

### First wave — generic ordinary containers (35 cases)

This is the primary player-facing scope. All **35 generic rows come before**
the **10 regression-only direct/other cases** in
[stash-ingame-loot-test-matrix.csv](stash-ingame-loot-test-matrix.csv).

For **15 distinct original actor + producer + loot-category combinations**,
the matrix now contains an explicit untouched **BASE** row immediately
before the corresponding **ZERO** 0x test; eligible families with a
HIGH 3x test compare against the same baseline. The exact original
physical actor SID and source SID are pinned, but an SID alone is **not**
an in-game waypoint or proof that the source activates on encounter.

Order of ordinary tests:

1. Medical from Basic, Standard, Rare ItemGenerator and two independent
   Medical SmartLoot prototypes (five baseline/zero pairs).
2. Food and Drinks separately from generic ItemGenerator and SmartLoot;
   other categories remain at native values in each build.
3. Ammo from generic ItemGenerator and SmartLoot **on the same actor**.
   When both produce the same visible ammo SID, ordinary contents alone
   cannot establish which producer supplied it.
4. Grenades from original Basic tier Chance and two separate SmartLoot
   packages, without equating Chance with the probability of a whole stash.
5. Coupons from Rare original ItemGenerator groups, preserving native
   rank and difficulty gates. No separate coupon SmartLoot package is
   claimed among the inventoried world prototype sources.

The 3x value changes specific authored field values, not an implied 3x
physical whole-stash reward frequency. A 0x test is not independently
source-attributable on mixed-layer actors until an **actor-scoped debug
CFG isolation build** turns off exactly one producer. That diagnostic
patch is NOT a persistent UI/preset feature, and has not been built here.
Keep all original blueprint/quest/named-clue protections.

### Source-family references for generic tests

| Category | Native ItemGenerator physical source | Parallel SmartLoot source | Main risk |
| --- | --- | --- | --- |
| Medical | Basic 006CDB98..., Standard 12775ACA..., Rare 069FAF0C... | StashMedicine_Cheap 51AE018D..., StashMedicine_Smart 006CDB98... | Alternative medical producers |
| Food | Basic 006CDB98... | StashFood_Smart 0F395036... | SmartLoot activation group may be rare |
| Drinks | Basic 006CDB98... | Drinks groups within StashFood_Smart 0F395036... | Do not also change Food |
| Ammo | Basic 006CDB98... | Stash_Ammo_Smart_CommonRare on same actor | Same item SID from two sources |
| Grenades | Basic 006CDB98... | StashGrenade_Cheap 6BC070D3..., StashGrenade_Smart 0F395036... | Native Chance versus SmartLoot Weight |
| Coupons | Rare 069FAF0C... | None in current indexed SmartLoot sources | Difficulty/rank gates |

### Second wave — direct-source regression only (10 cases)

**Richter Cabin is a known 100% conditional item-row finding, not an
ordinary-stash frequency benchmark.** Keep it at the end, including
BigSafe, Bag and SmallSafe baselines, Medical 0x tests, the unrelated Food
0x negative control and Medkit 0 ITEMS cases:

1. Record reference contents separately for BigSafe, Bag and SmallSafe,
   starting from uncontaminated generation states; prior total of two
   Medkits across three containers is historical evidence only.
2. Reproduce the Medical=0 experiment, checking the two direct Chance=1
   Medkit rows and other independent generators for these three actors.
3. On BigSafe change Food=0 while preserving Medical: direct Medkit
   Chance/count must not change.
4. Separately test Medkit MAX=-1 on the direct BigSafe and SmallSafe
   rows. This disables that item source, not every other medical producer.
5. Keep the Rostok ArmyMedkit direct row as a **regression-only** source,
   not an example of generic stash mechanics.

A failure on Cabin/Rostok must be recorded, but it must not cause special
item-generator behavior to be generalized into the 35 ordinary-container
test cases.

### Observations — use one record per case and physical generation event

Keep the source matrix authoritative; report observed results in a separate file or message with:

```text
case_id:
game_version:
TCD_commit_and_PAK:
save_state_and_first-generation_evidence:
actor_sid / physical location:
difficulty / player rank / DLC / other mods:
single changed parameter and value:
expected native source and generated CFG fields:
items and individual counts found (or none):
screenshot/video/log reference:
PASS / FAIL / INCONCLUSIVE:
notes (possible alternate layer, inherited/protected actor, etc.):
```

A **PASS** for 0x needs both a correctly targeted CFG and a fresh, attributable producer. A **FAIL** is evidence of a mismatch only after identifying the producing source and confirming the test state; otherwise mark **INCONCLUSIVE**. For `3x` runs, record per-group exposure and results across multiple independent freshly generated instances (exploratory target: 20–30); **do not** turn that into a quantitative drop-probability guarantee or repeat a rolled save as a new trial. There is no established numeric pass/fail threshold for physical RNG rates.

## Static audit and acceptance boundary

```bash
python3 tools/audit_stash_ingame_test_matrix.py
python3 tools/audit_stash_generator_branches.py
python3 tools/audit_stash_consumer_graph.py
python3 tools/audit_stashes_mixer_ui.py
./tests/run-wine.sh LootSpecialGeneratorsContract
```

The first command validates that each matrix row maps to **exact real allowlisted source actor / native layer**, including original Cabin and Rostok direct item entries, that control values fit native UI ranges, and that no physical gameplay test is pre-marked as passed. The last command still needs actual .NET/Wine execution; a green static result is never game verification.

**Stop/go:** no new Phase 4/5 "complete" status until the five ordinary Medical source-family baseline/0x pairs have attributable generation results, plus the Cabin regression checks; no global *physical* drop odds before later independent samples. **Do not broaden protected actor redirects or modify vanilla shared generators to force tests to look successful.**
