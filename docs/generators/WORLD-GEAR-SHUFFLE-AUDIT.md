# World Gear Shuffle Audit

Status: **data/audit only — no UI or build mutation yet**.

## Purpose

Define the safe boundary for a future world weapon/armor shuffle without touching quest rewards, player presets, traders, NPC/zombie loadouts, or unproven stash consumers.

## Source of truth

- Base game: `GameData/ItemGeneratorPrototypes.cfg`
- Base item prototypes: `GameData/ItemPrototypes.cfg`
- DLC1: `DLCGameData/DLC1/ItemGeneratorPrototypes.cfg`

## Hard rules

1. Selection is **allowlist-only**. Never target all `*Stash*` generators or all `WeaponPrimary` / `WeaponSecondary` / `BodyArmor` categories globally.
2. `QUEST_REWARD`, `PRESET`, trader, NPC and zombie domains are excluded.
3. `UNCERTAIN` is excluded until a consumer relationship proves it is ordinary exploration/world loot.
4. Parse the actual `Category`; never classify gear by SID prefix (`Gun*` also includes attachments).
5. A weapon replacement is a complete spawn recipe problem: SID, compatible upgrades/attachments/ammo and related fields must be regenerated/validated. Never blindly replace only `ItemPrototypeSID` while retaining another weapon's attachments/upgrades.
6. Weapon and armor progression pools are separate.
7. Unique/special/launcher gear is quarantined from generic replacement pools unless explicitly approved.
8. Shuffle amount means the percentage of eligible SAFE_WORLD slots changed at build time; untouched slots remain vanilla. It is not a spawn/removal chance.
9. Seeded output should be deterministic for a built preset/playthrough.

## Classification

- `SAFE_WORLD`: proven ordinary exploration/location stash candidate; eligible for later shuffle work.
- `QUEST_REWARD`: MQ/SQ/EQ/ANCQ/QSBIG/story/reward semantics; never shuffled by default.
- `PRESET`: player gear/stash difficulty/loadout preset; never shuffled.
- `UNCERTAIN`: plausible world stash but consumer semantics not proven; excluded by default.

## Confirmed structural examples

`Garbage_DigSite_StashGenerator2_C59BE52D481BCA2E1EB1CE816B67EF71` contains a `WeaponPrimary` entry for `GunBucket_PP`, with its own durability, ammo, upgrade and attach recipe. This proves a future shuffle can target the gear entry independently from the stash's `GamePass_Stash_ItemGenerator_Rare` subgenerator.

`CityCenter_PolissyaHotel_StashGenerator3_07727ED446A8EB9D0F56B3ABB9542CAD` similarly contains a direct `WeaponPrimary` `GunRam2_SG` recipe in addition to its rare stash subgenerator.

## DLC1 boundary

DLC1 contains weapon/armor generator data but also dedicated player presets and quest/reward stash generators. DLC support therefore follows the same allowlist rule; DLC presence alone does not make an entry `SAFE_WORLD`.

Known DLC direct stash gear observed during the audit includes `DLC01_EQ00_Stash -> GunM16_ST`, `DLC01_SQ06_Stash -> GunGvintar_ST + HeavyAnomaly_Monolith_Armor`, `DLC01_ANCQ_IF_04_Stash -> GunGvintar_ST + Light_Military_Helmet`, `DLC01_EQ01_Stash -> Light_Mercenaries_Helmet`, `DLC01_EQ03_Stash -> GunG37_ST`, `DLC01_MQ03_Stash -> GunFora230_PP + Anomaly_Scientific_Armor`, and two `DLC01_MQ06` stash generators containing `GunMark_SP` / `GunRpg7_GL`. These remain quest/reward exclusions unless later consumer evidence proves otherwise.

## Planned control model (not implemented)

- `World Weapon Shuffle` toggle
- `Weapon Shuffle Amount` 0–100%
- optional `Cross-Class Weapon Shuffle` toggle
- `World Armor Shuffle` toggle
- `Armor Shuffle Amount` 0–100%

Default weapon behavior should prefer same progression tier + same weapon family. Cross-class mode may allow another regular weapon family in the same tier. Armor stays within its own progression tier.

## Implementation gate

Do **not** add UI/build mutation until `world-gear-shuffle-candidates.csv` has been reviewed and replacement pools are explicit. Generated CFG should preferably use minimal config patches rather than wholesale CFG replacement where the runtime/packaging path supports it.

## Extracted catalog status

The current GameData + DLC1 audit is intentionally conservative and category-aware.

- Total stash-named direct gear entries (`WeaponPrimary`, `WeaponSecondary`, `BodyArmor`): **106**
- Proven `SAFE_WORLD` direct gear slots: **14**
- `SAFE_WORLD` weapon slots: **13**
- `SAFE_WORLD` armor slots: **1**
- Unique SIDs in `SAFE_WORLD`: **10**
- DLC1 `SAFE_WORLD` gear slots proven by the supplied CFG dump: **0**

`Gun_Trophy_AR` is a `SAFE_WORLD` original spawn but is quarantined from the generic replacement pool because it is a special weapon variant. A slot being safe to modify does **not** imply that its original item is safe to use as a generic replacement elsewhere.

The replacement-pool CSV is therefore **PROVISIONAL** and evidence-backed only. It must not be consumed by the build until the pool has enough same-tier/same-family alternatives and the feature is explicitly approved.

### Implementation invariant

Discovery and classification are based on the actual generator category, never on an SID prefix. `Gun*` attachment SIDs must not be interpreted as weapons. Replacement of a weapon slot must rebuild a compatible recipe (weapon SID plus compatible ammo/upgrades/attachments) rather than blindly preserving the original recipe.

## Replacement-pool progression evidence

Weapon tiers are now derived from the game's explicit `Trader_T1_Guns_ItemGenerator` through `Trader_T4_Guns_ItemGenerator` placement where available. This supersedes earlier hand-estimated tiers based on price or perceived strength.

Current direct-template weapon prototype audit:

- 40 direct regular-template weapon definitions were inspected.
- 34 have explicit Trader T1-T4 progression evidence and are `CANDIDATE` replacement items.
- `GunFora_ST` remains `REVIEW` because no explicit `Trader_Tn` placement was found.
- `GunGauss_SP`, `GunRpg7_GL` and nonstandard launcher/data definitions are excluded from generic shuffle pools.
- Concrete-derived variants such as `Gun_Trophy_AR` remain excluded even when their world slot itself is `SAFE_WORLD`.

Important corrections from the evidence: `GunBucket_PP` and `GunM860_SG` are T2; `GunAPB_HG` is T3; `GunRam2_SG` is T4; `GunMark_SP` and `GunSVDM_SP` are T2.

Tier is an availability/progression class, not a power score. Cost and durability are retained as audit metadata only.

### Pool use rule

Default conservative replacement requires the same `family` and the same game-data-derived `tier`. Cross-family replacement, if later approved, may use the same tier only. No build code consumes these pools yet.

## Armor progression evidence

Armor does not expose an equivalent `Trader_T1..T4` hierarchy in the supplied generator data. The project therefore does not infer armor tiers from price.

Four direct neutral BodyArmor anchors are explicit in player gear-rank generators:

- `NewbieGearPC_Generator` -> `Newbee_Neutral_Armor` -> T1
- `ExperiencedGearPC_Generator` -> `Zorya_Neutral_Armor` -> T2
- `VeteranGearPC_Generator` -> `SEVA_Neutral_Armor` -> T3
- `MasterGearPC_Generator` -> `Exoskeleton_Neutral_Armor` -> T4

This corrects the earlier provisional `SEVA_Neutral_Armor = T4` estimate to **T3**.

Other direct `TemplateArmor` items remain `REVIEW` until faction/rank equivalence is proven from generator relationships. Similar cost is insufficient evidence.

## Faction armor rank expansion

The faction armor generators provide direct progression evidence through `PlayerRank = ERank::Newbie/Experienced/Veteran/Master`. Armor replacement tiers now use the earliest explicit rank at which a direct player-wearable `TemplateArmor` prototype appears. This expands the pool without using price as a proxy.

Neutral `*GearPC_Generator` anchors remain independent confirmation for Newbee/Zorya/SEVA/Exoskeleton neutral armor. NPC-only armor, templates, concrete-derived/story variants, and items without rank evidence remain excluded or REVIEW.
## Armor faction-rank sanity check

Faction `GeneralNPC_*_Armor` `PlayerRank` branches were tested as a possible tier source and **rejected for world-loot tiering**. They are valid evidence for NPC/faction loadout scaling, not a universal item-progression ladder.

Counterexamples are explicit in GameData: Scientist `Anomaly_Scientific_Armor` is present in a Newbie branch, while Monolith `Battle_Monolith_Armor` is present for Newbie/Experienced. Treating these as global T1 items would contradict the player gear-rank anchors and the intended stash progression model.

Production-safe armor tiers therefore remain limited to the four direct player gear anchors (Newbee/Zorya/SEVA/Exoskeleton Neutral). Faction rank values are retained only as audit metadata.
## Armor player-facing progression sanity result

A second pass across trader, stash and location occurrences did **not** reveal a universal armor progression axis equivalent to the weapon `Trader_T1..T4` generators. These occurrences are retained as evidence in `world-gear-armor-player-facing-evidence.csv`, but they do not promote REVIEW armors into generic replacement tiers.

Production-safe vanilla-derived armor tiers therefore remain limited to the four explicit `*GearPC_Generator` anchors. Generic armor shuffle stays implementation-gated; weapon shuffle can proceed independently.
## v0.4.3 weapon shuffle implementation

The first production writer path is implemented for **weapons only**. Armor remains audit-only.

- Player controls: `World Weapon Shuffle` and `Weapon Shuffle Amount`.
- Source surface: audited base-game `SAFE_WORLD` direct weapon slots only.
- Replacement rule: same vanilla-derived tier + same weapon family + different regular SID.
- Special/unique, quest/reward, preset, NPC and DLC1 quest slots remain excluded.
- The percentage is calculated over the **compatible subset**, not every audited weapon slot.
- Current compatible subset: **6 of 12** regular SAFE_WORLD weapon slots.
- Selection and replacement are deterministic for identical Base Preset + complete parameter state. This provides a stable build-time seed without adding a separate seed control to the normal UI.
- The concrete `PossibleItems[n]` weapon entry is replaced as a whole. Neutral spawn scalars (Chance/Weight, durability, ammo/count fields) are preserved when present; weapon-specific vanilla `Upgrades` and `Attaches` are intentionally not inherited.
- Cross-class shuffle is not implemented.
- Armor shuffle is not implemented.

This writer emits a minimal BPatch of `GameData/ItemGeneratorPrototypes.cfg`; the official Zone Kit patch contract supports child patching/replacement through patch files and `{bpatch}`.


## WeaponPistol coverage correction

The implementation manifest explicitly includes `WeaponPistol` SAFE_WORLD entries. `LesserZone_WastewaterTreatmentPlant_StashGenerator1_91D632284EAEDA16F6CD7098A676611F` and `ChemicalPlant_ChemicalTanks_StashGenerator6_8E3157704F88075AAB9F519551CDA57D` both spawn regular `GunUDP_HG` (T1 Pistol) and have `GunPM_HG` as a same-tier/same-family alternative. The production weapon manifest therefore contains 14 regular SAFE_WORLD weapon slots, of which 8 currently have at least one strict compatible replacement.
