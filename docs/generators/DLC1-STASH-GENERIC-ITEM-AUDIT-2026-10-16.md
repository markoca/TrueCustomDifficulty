**CURRENT IMPLEMENTATION (2026-10-16):** The previous 196-actor CSV is a discovery register, not a writer manifest. [DLC1 safe writer contract](DLC1-STASH-REGULAR-PRODUCTION-2026-10-16.md) now approves only 112 ordinary DLC WorldMap actors, with 6 CheapNoFresh and 59 Medium ItemGenerator safe targets. The 69 historical Medium wrong-target-path rows documented below have now been corrected in the production manifest; 10 mixed/special Medium actors remain excluded by the runtime guard. The archived findings below remain source evidence and should not be interpreted as current missing code.

# DLC1 stash generic-item source map — CFG audit (2026-10-16)

## Scope and evidence

Source `STALKER2-DLC1-CFG(7).zip`, SHA-256 `93ccb9bcc9043f7c171d439b2c25c38f61311506bc33c0b926a16f526ccd6114`, combined for source resolution with the separate base `STALKER2-GameData-CFG(20261016-152136).zip`, SHA-256 `937b3fb4c74d1311b6e052c010aa0795073ccdc60d2ca6d8b5228ee3ce0b6083`.

Exact actor source: `DLCGameData/DLC1/SpawnActorPrototypes.cfg` and its `ESpawnType::ItemContainer` entries. DLC generator definitions: `DLCGameData/DLC1/ItemGeneratorPrototypes.cfg`. Base definitions: `GameData/ItemGeneratorPrototypes.cfg` and `GameData/StashPrototypes.cfg`. The base actor file contains **zero matching ActorSID roots** for the 196 DLC actor entries: the DLC actor records are a **separate file-scoped patch target**.

Complete actor-to-source machine catalog: [`dlc1-stash-generic-actors.csv`](dlc1-stash-generic-actors.csv). It includes **all 196 exact full actor SIDs**, their actual level, container type, clue, entire ordered SmartLoot and ItemGenerator SID lists, resolved possible generic category classes, mixed-authored flag, unresolved source status, and a non-authorizing triage label. It is a source inventory, never a live redirect manifest.

## Results

| DLC1 physical scope | Actor count |
| --- | ---: |
| All `ItemContainer` actors | **196** |
| In ordinary `WorldMap_WP` only | **179** |
| In separate quest/test-level paths | **17** |
| Any linked generic-item source | **175** |
| Ordinary `WorldMap_WP` with linked generic item source | **166** |
| No resolved generic-item source found | **21** |
| Actors with SmartLoot (any level) | **165** |
| Actors with ItemGenerator (any level) | **172** |
| Both source layers | **160** |
| SmartLoot-only | **5** |
| ItemGenerator-only | **12** |
| No authored source in these two actor fields | **19** |

Within the 179 direct `WorldMap_WP` actors, there are **161 SmartLoot**, **163 ItemGenerator**, **157 with both**, **4 SmartLoot-only**, **6 ItemGenerator-only**, and **12 without either**. No source reference implies no loot can ever be added at runtime.

The actor-class split from source triage is: **112** main-world ordinary-source *candidates*, **29** main-world special/mixed/custom generator candidates, **17** main-world DLC01_MQ06 paths (13 shared `DLC01_MQ06_Stash_ItemGenerator_Cheap` plus four individually named MQ06 pools), **8** main-world named-clue generic actor candidates, **9** separate-level generic actor candidates, and **21** total with no identified generic source (**13** of these on main WorldMap). These groups are mutually exclusive. **Zero** new actor is approved for patching by this audit.

## Definition of “generic item” here

**AMMO:** `A[0-9]*` source SIDs. **MEDICAL:** `Bandage`, `Medkit`, `ArmyMedkit`, `EcoMedkit`, `ScientificMedkit`, `AntiRad`, `Antirad`. **FOOD:** `Bread`, `Sausage`, `CannedFood`. **DRINKS:** `Vodka`, `Water`, `Milk`, `Energetic`, `Beer`. **GRENADES:** `GrenadeRGD5`, `GrenadeF1`. **COUPONS:** `Money*`. **BOOSTERS:** `Hercules`, `Cinnamon`, `PSYBlocker`.

Weapons, armor, attachments, artifacts, detectors, named one-off prizes and all other unknown SIDs remain **other-authored** rather than *automatically* labelled quest or unique. A special container may have both kinds of items. All category memberships mean **present in an authored possible item source**, *not* guaranteed runtime eligibility: original `Chance`, `Weight`, `MinCount`/`MaxCount`, group spawn endpoints, rank/difficulty gates and save generation history still govern the real outcome.

## Generator source map and missing coverage

There are **45 distinct ItemGenerator SIDs referenced by DLC1 ItemContainers**, of which **14** definitions are local to the DLC dump, including `DLC01_MQ06_Stash_ItemGenerator_Cheap`; others resolve to the base ItemGenerator CFG. DLC-local generator roots contain **86 local authored item rows** in this referenced subset. All 165 referenced SmartLoot sources resolve to base `StashPrototypes.cfg`, including the ordinary ammunition, medical and food families. `DLC01_MQ06_Stash_ItemGenerator_Cheap` contains 34 raw local rows across ammo, medical, food, drinks, grenades and money, but its MQ06 origin means **no generic patch approval**.

For the 112 main-world ordinary-source candidates, comparison by **full ActorSID** against current production tier manifests gives **102 indexed actors** (Basic **24**, Standard **59**, Rare **19**); **10** are not in those three ItemGenerator manifests, including **6** `CheapNoFresh` direct ItemGenerator users and **4** SmartLoot-only actors. These numbers are manifest *reference presence only*, not validated runtime effect.

**Critical path-scope caveat:** existing source-manifest rows are not sufficient proof of valid current patch targets. **All 69 DLC1 ActorSIDs registered in the Standard/Medium production redirect manifest** are recorded as targeting `GameData/SpawnActorPrototypes.cfg`, but the uploaded DLC ZIP places those exact actor roots in `DLCGameData/DLC1/SpawnActorPrototypes.cfg`. The other **62 matched DLC1 ActorSIDs** in the current Basic/Rare manifests already name the DLC1 file. These are all manifest hits (131 total, including 102 ordinary and 29 reviewed special/mixed actors), not a proof that the current patch actually works. **The precise 69 row path discrepancy is in [`dlc1-stash-target-path-mismatches.csv`](dlc1-stash-target-path-mismatches.csv)**, with ActorSID, tier, declared file, observed file and original generator. Example: `04D6AF3C427C5AAA381355A460EEFFF9` (`Common_Var1`). Before implementing, validate every manifest target against the current dump and review the writer semantics; do not blindly alter upstream source data. **Do not infer active working DLC writer coverage from the manifest-only number 102.**

The current SmartLoot production writer `CfgPatchWriterService.StashSmartLoot.cs` loads the 831 base actor manifest (803 active no-clue) and targets **`GameData/SpawnActorPrototypes.cfg`**, not the disjoint DLC1 actor file. Thus this separate DLC1 inventory **does not establish TCD SmartLoot rewrites for its 165 DLC actors**. Any DLC extension needs an actor-specific DLC path manifest and separate negative tests; clone source prototypes rather than changing shared original SmartLoot roots globally.

## Safety and follow-up

- Preserve the existing UI groups and stable stash IDs. Do not add new sliders just because a source is now inventoried.
- Exclude named clues, quest/test levels and mixed special generators from automatic redirects. `DLC01_MQ06_*`, `DLC01_SQ*`, `CNPPStash_*`, `IronForestStash_*`, `StashX19_*`, etc. require individual review.
- Never edit a `GamePass_Stash_*` root globally to pick up extra actors. Use source-true clone/redirection **only** after confirming the exact actor file path, generator `PrototypeSID` index and other rewards.
- The base source has `Stash_Stimulator_Master` with boosters and several auxiliary ammo/medkit pools used by DLC actors. Treat these as distinct sources; existing Stashes UI categories do not automatically represent all booster control semantics.
- `EmptyItemGenerator` and `NeutralMasterItemGenerator` are unresolved in the two examined ItemGenerator files. They may refer to runtime/default/higher-level sources; never convert “unresolved” into “no loot.”
- Re-read the DLC ZIP and compare exact 196 actor root IDs and ordered source SID lists before implementing. A source inventory is not evidence of generated CFG BPatch validity, pack precedence, or in-game effects on an ungenerated stash.

Related: [base SmartLoot source contract](SMARTLOOT-PRODUCTION-EXTENSION-2026-10-16.md), [base safe-expansion follow-up](STASH-SAFE-EXPANSION-FOLLOWUP-2026-10-16.md), [stash content/gear contract](../STASH-CONTENT-GEAR-IMPLEMENTATION.md). **Audit only; UI, presets, source writer and gameplay output unchanged.**
