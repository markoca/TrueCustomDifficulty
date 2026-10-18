> **2026-10-18 CURRENT MIXER:** A compact three-lane `WEAPONS / ARMOR / ARTIFACTS` 0–3x relative-weight mixer complements the master checkbox and 0–100% eligible-actors control. These are explicitly **build-time class weights**, not native Chance or Weight fields. Each source-audited, rank-gated selected actor receives at most **one possible** bonus item, chosen from the 142 regular candidate SIDs according to the category mix; the private generator uses real `PossibleItems[0].Chance = 0.5` and `MinCount = MaxCount = 1`. In-game appearance is not guaranteed. 0x excludes a class; all three 0x emits no extra generator; protection and deterministic selected-actor set remain unchanged. Earlier statements about guaranteed weapon category and optional second suit/artifact are superseded by this block. Audit & runtime confirmation are distinct.

> **2026-10-18 CURRENT BONUS POOL (supersedes the 34+4 pool below):** exactly 34 regular weapons, 39 normal player-facing suits and 69 ordinary artifacts in `gear-progression-ranks.csv`. Artifact rarity policy: Common=T1/Newbie (39), Uncommon=T2/Experienced (17), Rare=T3/Veteran (9), Epic=T4/Master (4); higher native player ranks may use lower tiers. Additional armor tiers are curated rather than claimed vanilla-derived. The six Weird/Arch artifacts and quest, fake, prolog, special/DLC-story items are excluded. The private per-actor generator emits one regular weapon plus **zero or one** additional suit **or** standard artifact (never both), and retains 1–2 candidate items. Existing original generator references, deterministic 800+108 actor percentage coverage, 13 removable authorized original weapons and two compact UI controls are unchanged. The new artifact category uses `EItemGenerationCategory::Artifact` without weapon or armor properties. In-game spawned artifact confirmation is still pending; native `Chance=1` is not a claimed physical stash probability.

# Extra Stash Gear: deterministic physical stash coverage (2026-10-18)

## Eligibility

The previously documented 803 regular source-approved base physical stash actors plus 112 DLC1-approved actors are only the *starting inventory*, never automatic permission to inject gear. The new actor-rank catalog checks original `ItemGeneratorSettings` source slot counts and `PlayerRank` rank branches and excludes empty generators and the `Gun_Trophy_AR` actor. **800 base ordinary physical stash actors + 108 DLC1 eligible = 908 with DLC**, or 800 without DLC. Quest, named-clue, protected, special and unclassified/unknown actor types are excluded. The original 39 weapon-containing stash entries remain a separate source inventory (13 removable on master ON, 26 protected/deferred).

## New controls

`loot.stashGearEnabled`: the existing checkbox, off by default. Independently removes the 13 specifically authorized regular original fixed weapon slots when ON; master OFF preserves them.

`loot.stashGearCoveragePercent`: the only gear frequency control, 0..100% stepping exactly 1%, off/default 0%, dependent on master. This is an **exact rounded fraction of physical ActorSIDs** picked *before* game generation, **not a new native item Chance** and not a randomized roll every time a stash is opened. For each installed content scope, candidate actor SIDs are ordered by 64-bit prefix of SHA256(`TCD-StashGear-2026-10-18|ACTOR|{scope}|{ActorSID}`); pick `floor((N*percent+50)/100)`. All selected stashes receive one or two extra authored source items; original base selection is independent of DLC1 installation.

## Native rank and item pool

Each chosen actor gets its own `TCD_ExtraGear_{ActorSID}_{RankCode}` generator, appended to the first unused `ItemGenerators` slot on its pre-existing native rank branch. Actor sources with all four ranked branches receive four **mutually rank-targeted** references, not four unconditional drops. No existing source item or generator is overwritten. Pool: **34 source-tied regular weapons** across T1(8), T2(10), T3(10), T4(6), plus **four** player-rank-specific suits (T1 Newbee Neutral; T2 Zorya Neutral; T3 SEVA Neutral; T4 Exoskeleton Neutral). Eligible item SIDs are limited to `ProgressionTier <= native PlayerRank`, and stable SHA256 values deterministically pick a weapon and optionally a suit. A selected stash's private generator contains 1 regular weapon plus 0/1 suit (50/50 hash parity), each with source count 1. Pistols use `WeaponPistol`, other weapons `WeaponPrimary`, suits `BodyArmor`. `Chance=1` is written for the possible items inside selected private generators, but engine category roll semantics/guarantees must be tested in-game before interpreting that as a 100% inventory spawn. Damage, weapon setup, ammo source and original attachments remain unchanged except the separately approved source weapon removals.

The supported item list is the original `gear-progression-ranks.csv`: T1 `GunPM_HG`, `GunUDP_HG`, `GunViper_PP`, `GunAKU_PP`, `GunAK74_ST`, `GunObrez_SG`, `GunTOZ_SG`, `GunThreeLine_SP`; T2 `GunM10_HG`, `GunKora_HG`, `GunBucket_PP`, `GunFora230_PP`, `GunM16_ST`, `GunG37_ST`, `GunGvintar_ST`, `GunM860_SG`, `GunMark_SP`, `GunSVDM_SP`; T3 `GunAPB_HG`, `GunRhino_HG`, `GunIntegral_PP`, `GunZubr_PP`, `GunGrim_ST`, `GunLavina_ST`, `GunArev_ST`, `GunSPSA_SG`, `GunSKP_DMR`, `GunM701_SP`; T4 `GunKharod_ST`, `GunDnipro_ST`, `GunD12_SG`, `GunRam2_SG`, `GunGP3A_DMR`, `GunSVU_SP`. Gauss/RPG, `GunFora_ST` (unverified tier), other suits (no independent tier source), quest and unique items are not eligible.

## Safety, audit and QA

No fixed map-region-to-rank assumption. No per-entry fabricated 2% / 7% / 15% chance. No shared generator modifications to all consumers. Runtime validates source actor manifests, rank/slot count, 106 protected base actors and 38 distinct gear SIDs. Only after all source data passes does it assign actor-specific generators and scoped BPatch redirects. The UI follows both locked documents and retains the existing compact checkbox/hit zones and Stamina Usage-style slider semantics. Run:
```bash
python3 tools/audit_stash_gear_coverage.py
python3 tools/audit_extra_stash_gear.py
python3 tools/audit_stash_content_gear.py
python3 tools/audit_stashes_mixer_ui.py
```

For native end-to-end CFG emission checks, run the LootSpecialGeneratorsContract .NET harness (including `VerifyStashGearCoverage`), then test previously ungenerated and unopened real physical stash actors in-game. Source-level checks cannot guarantee all gear appears in a live physical stash exactly as intended.
