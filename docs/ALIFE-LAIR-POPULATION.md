# LAIR POPULATION (2026-11-18)

## Current UI — Frameless lair population knobs (2026-11-18)

The A-Life LAIR POPULATION board uses **three frameless native 270° knobs** in a compact
centered row (at most 620 DIP wide), from left to right **HUMAN CAMPS · RESPAWN TIME ·
MUTANT NESTS**. RESPawn TIME is the larger center/hero knob, using the same
`prominent` dial sizing and raised caption as the central combat hero controls.
Native step sizes, editor controls, reset actions, reference ticks and CFG writers
remain unchanged.

* HUMAN CAMPS and MUTANT NESTS display **percent of native lair capacity**
  (25%–300%, 100% = the original source count). Counts are integer and rounded
  half-up in the writer, so a live four-rank example appears beneath each knob:
  BANDIT CAMP BY RANK / N 6 · E 6 · V 7 · M 8 and TUSHKAN NEST BY RANK /
  N 14 · E 16 · V 20 · M 24 at their source values. Only rank counts actually
  changed by rounding become yellow.
* RESPAWN TIME displays the representative **time until full** (8 min at 1×,
  2–32 min across 0.25×–4×) and the caption FULL AGAIN. The separate unframed
  preview says AFTER A FIGHT / HALF BACK IN 3 min / WIPED OUT: RESTARTS IN 8 min at 1× (two lines: a thinned-out lair is half full again after the first timer; a lair killed to the last one restarts after the wipe timer).
  The underlying three source timers still scale together, including the
  special shorter 6 / 30 / 30-second lairs; the central readout is a *typical*
  time, not a universal promise.
* The design adds no redundant Base/Vanilla emphasis, cards or extra controls.
  There is no change to native values, factory presets, manifest or game CFG.


How many inhabitants a world lair can hold, on the A-Life tab next to GROUP SIZE.

| Control | ID | Scales |
|---|---|---|
| HUMAN CAMPS (*Human Lair Population*) | `alife.lairPopulation.human` | 508 faction/rank entries: 17 human factions, 288 base + 220 DLC1 |
| MUTANT NESTS (*Mutant Lair Population*) | `alife.lairPopulation.mutant` | 608 entries: 17 mutant and zombie species, 352 base + 256 DLC1 |
| RESPAWN TIME (*Lair Respawn Time*) | `alife.lairRespawnTime` | the 3 respawn timers of all 1116 entries |

Range 0.25×–3× in steps of 0.05, vanilla 1×. Each eligible `MaxSpawnQuantity` (`LairPrototypes.cfg → <Lair>.Preset.PossibleInhabitantFactions.<Faction>.SpawnSettingsPerPlayerRanks.<Rank>`) is scaled with half-up rounding. A vanilla 0 stays 0, and a non-zero count never drops below 1, so no lair loses a faction. Rank and faction differences are kept.

RESPAWN TIME (0.25×–4×, vanilla 1×) multiplies `InitialSpawnQuantityRespawnTimeSeconds` (back to half full), `MaxSpawnQuantityRespawnTimeSeconds` (back to full) and `WipeRespawnTimeoutSeconds` (after the lair is wiped out). Each timer is rounded to 0.1 s and a 0 stays 0. Vanilla is 180 / 480 / 480 s in 1104 entries. The other 12 are Newbie entries (Diggers, IkarVarta, Sultan / Neutral bandits, zombie lairs) at 6 / 30 / 30 s, and they keep that ratio. The larger center dial displays full-refill time and the preview below shows the half-full and after-wipe timings.

The board shows one real lair by your rank under each population card:
- `BANDIT CAMP BY RANK 6 / 6 / 7 / 8`
- `TUSHKAN NEST BY RANK 14 / 16 / 20 / 24`

## Group size vs lair population

| | File | Meaning |
|---|---|---|
| GROUP SIZE | `ALifeDirectorScenarioPrototypes.cfg` `MaxCount` | how many show up together in one A-Life encounter |
| LAIR POPULATION | `LairPrototypes.cfg` `MaxSpawnQuantity` | how many a lair can hold |

## Not edited

Each of these has a reason; `tools/build_lair_population_manifest.py` encodes the exclusions:

- the empty test prototype `[0]` and `RestingLairDefault`;
- settlement guard posts (`Guard*`), which defend bases and are not difficulty;
- friendly `Scientists` camps;
- story / special prototypes: `SIRCAA_*`, `MALACHITE_*`, `Object68`, `FW_*` (DLC1), including their factions.

Starting fill (`InitialSpawnQuantityPercent`) and the archetype mix (`MinQuantityPerArchetype`, `SpawnWeight`) are untouched. Vanilla already has entries where the archetype minimums exceed `MaxSpawnQuantity` (Freedom Newbie: 6 vs 7), so a lower cap is a state the game already handles.

## Code

- `Data/lair-population-manifest.csv` (embedded): 1116 rows (640 base + 476 DLC1), built from the 2.1 CFG dumps.
- `Services/LairPopulationPolicy.cs`: IDs, manifest loader with size and shape checks, `Scale`, and the board example.
- `Services/CfgPatchWriterService.LairPopulation.cs`: BPatch assignments. A value that rounds every lair back to vanilla is refused at build time.
- Board `LairPopulation` in `AggregateVisualControl` (three frameless 270° knobs with one enlarged central dial and source-derived example lines). Family wiring is in `ParameterViewModel` and `MainViewModel`.
- Baseline `special.lairPopulation` (3 IDs), census 418 / 211 visible.

## Factory presets

Absent from the table = 1×.

| Profile | Human | Mutant |
|---|---|---|
| Anomaly Runner | 0.75× | 0.85× |
| Hardcore | 1.25× | 1.15× |
| Hardcore+ | 1.5× | 1.4× |
| Mercenary | 1.25× | 1× |
| Mutant Hunter | 1× | 1.3× |
| Outcast | 1.15× | 1.1× |
| Scavenger | 0.75× | 1× |
| Tactical | 1.25× | 1× |
| Wanderer | 0.85× | 1× |
| Field Medic, Immersive, Smuggler | 1× | 1× |

RESPAWN TIME (lower = lairs come back sooner): Hardcore+ 0.65×, Hardcore and Mutant Hunter 0.8×, Mercenary and Tactical 0.85×, Outcast 0.9×, Anomaly Runner and Scavenger 1.2×, Wanderer 1.35×, the rest 1×.

Older stock preset files upgrade only when byte-identical (`PreLairPopulationFactoryBlobs`, `PreLairRefillFactoryBlobs` for the first LAIR POPULATION stock and `PreLairRespawnTimeFactoryBlobs` for the short-lived REFILL SPEED stock; `tools/factory_preset_lair_population_2026_11_18.json`).

## Gate

`tools/audit_lair_population.py` is in `pre_release.py`. With `--base` and `--dlc` it rebuilds the manifest from the game CFG and requires an exact match. That has passed against the 2.1 dumps.

## Not yet verified

The .NET build, a Wine run and in-game behaviour have not been checked: whether camps and nests actually fill to the new cap, and whether the new refill times feel right.

## 2026-11-18 — REFILL SPEED renamed to RESPAWN TIME

The third card was first shipped as REFILL SPEED (`alife.lairRefillSpeed`, timer ÷ speed). It is now **RESPAWN TIME** (`alife.lairRespawnTime`, timer × multiplier), so the name and the direction match: higher = longer. Factory values were inverted onto the 0.05 grid (1.5 → 0.65, 1.25 → 0.8, 1.15 → 0.85, 1.1 → 0.9, 0.85 → 1.2, 0.75 → 1.35). A preset saved with the old key loads that card at vanilla 1×.
