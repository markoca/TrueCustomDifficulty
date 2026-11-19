## 2026-11-19 — Factory presets synchronize on every successful publish

`git pull` updates the tracked source JSONs and `./publish.sh` synchronizes all 12 to `publish/win-x64/Presets/Factory` together with the new EXE and BUILD-INFO receipt. The main `Presets` directory and local edits are preserved; user-edited factory-named files remain selectable as `(local)`. Failed builds retain the previous EXE and matching Factory files.

## 2026-11-19 — Factory selection is independent from local copies

The twelve official dropdown roles read up-to-date factory JSONs from `Presets/Factory`. Modified root `Presets/<role>.json` files are preserved and separately listed as `<role> (local)`; identical duplicates are hidden. Use the official role entry for the latest Human Tactics/Awareness values. Load/Save still use the portable Presets root.

## 2026-11-19 — Distinct human enemy behavior for all factory roles

All 12 factory profiles now have fully authored HUMAN TACTICS / HUMAN AWARENESS settings: 12 native parameters per profile, **110 real changes**, with role-specific enemy coordination, accuracy, sight, hearing, search memory and fog/rain/storm concealment. `tools/factory_preset_human_roles_2026_11_19.json` holds the active matrix and rationale; `docs/FACTORY-PRESETS.md` compares all roles. Exact unedited former stock blobs upgrade automatically; customized profiles are preserved. No UI, CFG mapping, medical, stash or loose-item removal behavior changed in this pass.

## 2026-11-19 — Current source-specific world pickup removal policy

The 12 native profiles now use intentionally distinct **Remove Loose Detectors**, **Remove Loose Armor**, and **Remove Loose Artifacts** toggles. 19 checkbox changes were authored across 10 profiles; two already matched. Anomaly Runner/Wanderer retain accessible explorer detectors and fixed Weird finds; Scavenger retains loose armor; Smuggler retains fixed artifacts; Hardcore/Hardcore+/Outcast remove all three. Quest, trade, NPC loadout, reward-only and anomaly-field artifact sources are outside these operations. No new UI or CFG writers; authored values and roles: `tools/factory_preset_world_removal_policy_2026_11_19.json`, `docs/FACTORY-PRESETS.md`. Only pristine prior stock copies update automatically, never edited/user presets.

## 2026-11-19 — Current factory role diversification (84 sources)

12 complete native factory presets now make 363 actual value changes across 84 existing sourced control IDs. All preserve 452 stored settings, 5–10 s per-item HEAL TIME, Field Medic-only bandage HP, one artifact per spawn, compact UI control grammar and protected quest/unique loot. Generic stash reduction is now 15–25% by role, alongside reduced category odds and different professional supply paths. The source-to-role matrix is `tools/factory_preset_role_diversification_2026_11_19.json`; documented rationale and full profile comparison are in `docs/FACTORY-PRESETS.md`. Pristine installed factory copies upgrade without overwriting user-edited/custom presets; 2.1 runtime must still be tested.

## 2026-11-18 — Factory bandages treat bleeding, not HP

**11 of the 12 factory profiles have Bandage Healing = 0 HP.** Field Medic alone retains 10 HP as a deliberate medical-role exception. Scavenger's former 10 HP bonus is removed. All twelve keep their separate native bleeding removal; 5–10 s factory treatment durations and medkit healing are unchanged. The manual bandage healing control remains editable. Only an exact unchanged Scavenger factory copy is replaced on startup; customized/user presets stay untouched. Source policy and full audit: `tools/factory_preset_bandage_hp_policy_2026_11_18.json`, `docs/FACTORY-PRESETS.md`.

## 2026-11-18 — Healing time is part of every factory role

All twelve shipped profiles now use **5–10 seconds** for their four native item HEAL TIME values. Field Medic remains quickest; Hardcore+ and Outcast expose the player longest. Previous 1–4 s factory heals are superseded. The manual catalog range remains **1–10 s** with unchanged compact UI. Existing exact-byte stock copies upgrade automatically; customized factory-named and user presets are never overwritten. Full role matrix and source notes: `docs/FACTORY-PRESETS.md` and `tools/factory_preset_heal_time_2026_11_18.json`.

## 2026-11-18 — Second role pass: scarcity matters

All twelve shipped profiles now have source-backed, role-specific **Corpse Scarcity** and **Stash Supply Reduction** instead of the previous uniform zero. Extra supplies from stash Smart Loot are scaled down; if a role depends on medicines (Field Medic), paid ammunition (Mercenary and Mutant Hunter), body scavenging (Scavenger) or long journeys (Wanderer), it retains a modest useful source instead of completely eliminating one. Hardcore+ retains both Smart Loot masters OFF and its existing severe world-item removals. These edits never touch quest/unique loot or NPC equipped weapons. Actual changes by role are documented in `docs/FACTORY-PRESETS.md` and `tools/factory_preset_scarcity_refine_2026_11_18.json`. Unmodified factory files upgrade on restart; user-edited copies are never overwritten.

## 2026-11-18 — Role-tuned factory profiles

The twelve built-in profiles now have distinctive native mutant move pacing and supporting combat, weather, artifact, lair, trading and fall-damage choices. Examples: **Hardcore+** has extreme predators and slow resupply; **Hardcore** has dangerous but readable elite moves; **Mutant Hunter** brings frequent mutant encounters and signature attacks; **Tactical** emphasizes enemy flanking and suppression; **Mercenary** combines disciplined human combat with fast paid resupply; **Smuggler** rewards low-profile routes and rapid trading; **Scavenger** emphasizes scarcity and evasive survival; **Anomaly Runner** specializes in anomalies and psi hazards; **Field Medic** favors treatment and recovery; **Wanderer** favors exploration with uneven wildlife; **Immersive** follows plausible ecology; **Outcast** faces hostile patrols and a dangerous Zone. Exact changes and source references: `docs/FACTORY-PRESETS.md` and `tools/factory_preset_vibe_2026_11_18.json` in the repository. Locally edited profiles never get overwritten by a stock update.

## 2026-11-17 — CURRENT: thematic tuning of the newest controls

The controls added after the last factory pass (HUMAN TACTICS / HUMAN AWARENESS, MUTANT AGGRESSION, ARTIFACT HUNTING, STASH SMART LOOT, NPC SMART LOOT, REMOVE ARMORS / LOOSE ARTIFACTS, Army Medkit in medical boxes, HEAL TIME, weapon/armor weight) and the TRADER STOCK / RARE GEAR sidecars now carry role values. Source of truth: `tools/factory_preset_thematic_2026_11_17.json` (absent ids = reference-difficulty vanilla); gate: `tools/audit_factory_preset_coverage.py`.

| Role | Human tactics · accuracy | Awareness (sight · hearing · memory · fog/rain/storm) | Mutants (attack · ability · special) | Other |
|---|---|---|---|---|
| Tactical | 1.5/1.5/1.5/1.25/1.5 · 1.25 | 1.15 · 1.15 · 1.5 | — | smart .5/.75, heal 2–3 s |
| Mercenary | 1.25 all · 1.1 | 1.1 · 1.1 · 1.25 | — | smart .5/.8, RARE GEAR 3% |
| Hardcore | Master 2×, camper 1.5 · 1.15 | 1.2 · 1.2 · 1.5 · .75/.75/.65 | Master 2× | artifacts .8/.75, smart 0/.6 no grenades, Army Medkit .5×, heal 3–4 s, weight 1.1×, LOOSE ARTIFACTS ON, shopkeeper armor 75% |
| Hardcore+ | 2.5 all, camper 2 · 1.3 | 1.35 · 1.35 · 2 · .8/.8/.7 | 2.5 | artifacts .6/.5, stash + NPC smart loot OFF, REMOVE ARMORS + LOOSE ARTIFACTS ON, Army Medkit .25×, heal 5–6 s, weight 1.15×, armor/artifacts 50% and ammo 75% at traders |
| Immersive | — | 1.1 · 1.1 · 1.5 · .6/.6/.5 | — | smart .5/.8, heal 3–4 s |
| Smuggler | — | .85 · .85 · .75 · .6/.55/.45 | — | artifacts 1.2/1.25, smart .5/.75, weapon weight .9×, RARE GEAR 5% |
| Outcast | flank/push 1.25 · 1.1 | 1.1 · — · 1.5 | 1.25 | artifacts .8/1, smart 0/.5, REMOVE ARMORS ON, Army Medkit .5×, heal 3–4 s, shopkeeper weapons/armor 50% |
| Scavenger | — | .9 · .9 | — | RARE GEAR 3% |
| Mutant Hunter | — | — | 1.5 · 1.25 · 1.5 | smart .5/.8 |
| Wanderer | — | fog .65 | 1.25 | artifacts 1.2/1, smart .75/1, weight .85× |
| Field Medic | — | — | — | Army Medkit 1.5× |
| Anomaly Runner | — | — | — | artifacts 1.5/1.5, smart .75/1 |

SUPPLY REDUCTION (stash and corpse) stays 0 everywhere: scarcity is already set by the odds cells, boxes and loose loot, and would stack. Artifacts per spawn stay 1. `PresetDirectoryService.PreThematicFactoryBlobs` upgrades byte-identical stock copies; edited presets stay untouched.

## 2026-11-13 — Trader Sells factory preset extension

All 12 factory profiles now have **359 catalog entries**: the original 288, 40 independent Trader Buys cells, and 31 independent Trader Sells cells. The new seller cells are all ON/neutral, so **existing merchant stock remains unchanged** unless a player explicitly turns one OFF. No per-role merchant-category customization is introduced by default; reset and older preset import preserve the same vanilla behavior. See `docs/generators/TRADER-SELLS-IMPLEMENTATION-2026-11-13.md`.

## 2026-11-13 — Trader Buys matrix / factory profile source coverage

The 12 shipped profiles each contain **328 source IDs** (the historical 288 + 40 new `economy.traderBuys.*` boolean values). All new buyer cells are neutral and preserve each merchant's original buy limitations. The 2026-10-19 49-dimension preset tuning matrix remains a historical subset, not an instruction to limit full catalog coverage. See [`docs/generators/TRADER-BUYS-IMPLEMENTATION-2026-11-13.md`](../../../docs/generators/TRADER-BUYS-IMPLEMENTATION-2026-11-13.md).

## 2026-10-20 — RC candidate cleanup: diagnostic profile retired from app

The **RC - CFG Signal** diagnostic is no longer shipped, embedded or auto-created by the application. The preset picker contains the **12 official factory roles** plus any unrelated user profiles. During the first run after updating, the exact byte-for-byte stock diagnostic file is deleted. A user-edited diagnostic file is **moved without overwriting** into `Presets/Archived/` (with a numbered name if necessary), so it disappears from the picker without losing user changes. The 13-signal CFG contract remains in `tests/RCSignalPatchContract/Fixtures/RC-CFG-Signal.json`, embedded **only in the test assembly**, not in the published EXE. This is release-candidate cleanup, **not** proof of a passing post-change build, Wine UI test, game-load test or in-game effects.

# True Custom Difficulty — Factory Presets

### 2026-10-19 — HISTORICAL 287-source role coverage (supersedes earlier factory snapshots)

The 12 official profiles still carry **all 287 source-backed parameter IDs** (3,444 serialized entries); **49 distinct real controls** were rebalanced in **445 actual value changes** versus the immediately preceding shipped stock profiles. Source: `tools/factory_profile_role_coverage_2026_10_19.json`, gate: `python3 tools/audit_factory_profile_role_coverage.py`. The earlier 248-ID tables below are HISTORICAL, not current numerical profile values. The RC CFG Signal diagnostic is retired from the application as of 2026-10-20; its writer fixture is test-only.

This pass tunes Bread/Sausage/Canned Food/Condensed Milk relief as actual HungerPoints removed; Hunger, Fatigue and Bleeding thresholds; Stamina exhaustion/low-recovery sources; three active Basic stash-category priorities; Medical priority, native Chance and MIN/MAX; Standard/Rare per-category source Chance; and Basic grenade quantity. **Chance scales per-entry authored odds, not a whole-stash guaranteed drop.** Priority changes source preference; MIN/MAX set authored floor/cap, not guaranteed exact counts. The already-curated world abundance, NPC resources, trader costs, overall hunger growth, HP healing and weapon economics remain intact. Do not adjust retired global stash Weight sources or integer-no-op A-Life dimensions to increase changed counts artificially.

**Extra Stash Gear (ON)** for Anomaly Runner, Mercenary, Mutant Hunter, Scavenger and Smuggler uses role-specific weapon/armor/artifact/attachment priorities and limited **8–12% eligible stash coverage** with **20–30% bonus candidate Chance**. To respect the live mutual-exclusion contract, **Shuffle Weapons is OFF** in precisely those five roles. Other seven roles retain Shuffle Weapons ON and Extra Stash Gear OFF. Dormant World Weapon Density is intentionally preserved for later manual toggling. No protected or quest stash entry list, source eligibility, original CFG writer, UI control catalog or game difficulty reference was changed.

`PresetDirectoryService.PreRoleCoverageFactoryBlobs` recognizes exact Git blob identities of the previous pristine 287-ID factory JSON files; startup migrates only byte-identical official copies, never modified factory-named or custom presets. Profiles remain balance hypotheses awaiting actual Windows/Wine and STALKER 2 in-game verification. The old 248-control rationale ledger stays available for provenance but is not the newest role value authority.


### 2026-10-15 — Historical diagnostic RC CFG Signal preset (RETIRED)

Historical context: the application previously auto-seeded a separate 13-setting extreme CFG diagnostic. Since 2026-10-20 the profile is absent from published builds and the preset picker; equivalent inputs remain a **test-only fixture** for the production writer contract. The archived A/B and gameplay test procedure remains in `docs/RC-CFG-SIGNAL-TEST.md`. None of the twelve official factory roles is modified by this removal.


**2026-10-15 — Canonical A-Life group-size profile mapping:** All 12 official factory roles retain 248 native sources and identical true human/mutant integer group caps. Only 39 source multipliers are replaced with the editor's canonical effective stops, across 216 unchanged group-size source outcomes. The per-role before/after matrix is `tools/factory_preset_alife_effective_mapping.json`; the native source ledger, role matrix, docs, and 12-file pristine migration hashes are synchronized. Modified or custom preset files are never overwritten. Local .NET/Wine/in-game verification remains pending.


**2026-10-15 — Final CFG-to-writer audit:** 12 × 248 source entries are preserved. **108 source-inert integer A-Life multipliers** were reset to real neutral 1×; their previous fractional values did not modify any `MaxCount` and could make the production writer reject the preset. Authored values, full per-source decisions and their source confidence levels are synchronized in `tools/factory_preset_full_source_audit.json`. The `PreFinalCfgAuditFactoryBlobs` migration only refreshes byte-identical old stock, never customized presets. [Full CFG report](../../../docs/FINAL-CFG-MAPPING-AUDIT-2026-10-15.md). No .NET/Wine/game execution is claimed from this static pass.

**2026-10-15 full-source audit:** 248 CFG-backed controls in each of 12 role profiles, **2,976 independently documented setting decisions** in `tools/factory_preset_full_source_audit.json`, source-normalized numeric values, differentiated major mutant HP classes and no unintended zero rare trophy chance. Existing consumable/editor overrides and slower hunger are preserved; `PreFullDeepAuditFactoryBlobs` upgrades only exact-byte stock files. This snapshot is not an in-game verified difficulty rating. Full report: `docs/FACTORY-PROFILE-FULL-AUDIT-2026-10-15.md`.

**2026-10-15 consumable cadence update:** All twelve stock profiles enable the serialized duration override, use slower hunger growth (0.007–0.014 points/s) and get longer, role-specific Water, Energy Drink, Vinca, Hercules and Psi Block effects. Field Medic alone additionally enables the medical-limit override for medkit bleeding treatment. All 248 native CFG controls remain independent; exact-byte pristine old factory files upgrade but user-edited/custom presets stay untouched. Source-of-truth: `tools/factory_preset_consumable_balance.json`. Publish/Wine/in-game effects remain unverified.

**2026-10-15 factory role deep rebalance:** 12 full 248-ID profiles, 78 audited source controls, 702 changed values. Strong profession identities cover combat, medical supplies, traversal, artifact/trophy economies, world scarcity, repair/wear and hazard exposure. `tools/factory_preset_role_rebalance.json` is the reproducible per-role delta matrix, with the full rationale in `docs/FACTORY-PROFILE-DEEP-AUDIT-2026-10-15.md`. Exact-byte prior factory presets auto-upgrade without replacing user-edited files; user-created profiles are safe. Build/Wine and real gameplay checks are pending.

### 2026-10-15 — 248-source factory presets and exact stock upgrade (CURRENT)

The two independent Water strength sources are both present in all **twelve shipped factory presets**: `survival.waterStaminaEffectStrength` (WaterStamina2 +25% Stamina type) and `survival.waterStaminaCostReduction` (WaterStaminaPerAction1 −10% SPDrain). Each new cost-cut entry inherits that factory profile's **previous shared Water strength** so the pre-split gameplay setting stays equivalent. The source catalog has **248** independent entries per factory profile, versus the previous 247. The existing role-specific 46-source scarcity balance values, other drink settings, durations, difficulty references and raw writer routes are untouched. `PresetDirectoryService` recognizes exactly the twelve pristine previously-shipped 247-source JSON blob hashes; it upgrades only those byte-identical stock files and never overwrites user-edited presets. User presets without the new cost key continue to load via the existing in-memory Water split migration.

## Current scarcity-first balance — 2026-10-14

The twelve role presets deliberately reduce vanilla's excess supplies through real loot categories, NPC/stash/trader availability, limited medical healing, sleep recovery and coherent economy/repair costs. Food never heals HP in these factory profiles; extra corpse smart-loot bonuses are OFF. **Current 2026-11-18 correction:** Field Medic alone retains a 10 HP bandage heal; Scavenger and every other role use bandages only for bleeding control. The old two-role exception is superseded. The full source-backed tuning matrix lives in `tools/factory_preset_scarcity_balance.json`, described in `docs/FACTORY-PRESETS.md`. Previously shipped byte-identical factory JSON files automatically upgrade; edited/user presets stay untouched. This is preset data, not a new UI/CFG setting.

The application ships with twelve custom factory presets:

1. Anomaly Runner
2. Field Medic
3. Hardcore
4. Hardcore+
5. Immersive
6. Mercenary
7. Mutant Hunter
8. Outcast
9. Scavenger
10. Smuggler
11. Tactical
12. Wanderer

These presets replace the old Novice / Stalker / Veteran / Master preset files.

The game's vanilla difficulty selection is still the underlying reference model.
Each custom preset keeps its own `referenceDifficulty` internally and applies its
custom values onto that vanilla baseline.

Medical realism rules in the hard presets:
- Hardcore: Bandage Healing = 0; Food Restores Health = OFF
- Hardcore+: Bandage Healing = 0; Food Restores Health = OFF
- Mutant Hunter: Bandage Healing = 0; Food Restores Health = OFF
- Scavenger: Food Restores Health = OFF; Bandage Healing = 0 HP (current policy)

The Food Restores Health toggle only affects ordinary Bread, Fresh Bread,
Canned Food and Sausage. Weird Kettle alternative effects are intentionally untouched.

Rebuilt playstyle descriptions and verified 247-entry current catalogue coverage are maintained in `docs/FACTORY-PRESETS.md`. Factory presets are curated starting points; loading one retains the usual Base difficulty and CFG writer behavior.


## Factory profile upgrade (2026-10-09)

When the executable starts, it replaces a previously shipped factory preset **only if its complete local file still exactly matches the prior official factory JSON**. User-edited factory files, even those still using a factory filename, are preserved; custom preset filenames are never overwritten. Missing factory files are seeded normally. This is an intentional one-generation upgrade for the seven role-first profiles, not a universal forced reset. To adopt the new official balancing if an existing factory file was customized, save the customization under a separate name and remove the old factory-named file before restarting the application.


New role-focused presets:
- **Mercenary** (Veteran base): contract warrior, mobile and well equipped, profitable but dangerous gunfights.
- **Wanderer** (Stalker base): independent long-distance explorer, stamina and traversal, hazards and artifact value.
- **Outcast** (Veteran base): marginal survivor, scarce medicine and ammunition, expensive repairs and risky encounters.

All three include the full 201-value source mapping. The factory list now has ten profiles. Existing user presets and any personalized files remain untouched.


## Alphabetical factory presets — 2026-10-09

1. Anomaly Runner
2. Field Medic
3. Hardcore
4. Hardcore+
5. Immersive
6. Mercenary
7. Mutant Hunter
8. Outcast
9. Scavenger
10. Smuggler
11. Tactical
12. Wanderer

**Field Medic** (Stalker baseline): wound stabilisation, medicine, bleeding removal and sustained rescue expeditions; modest firepower.

**Smuggler** (Veteran baseline): black-market expeditions for valuable artifacts and trophies, fast retreats, gear wear and expensive supply chains.

Both new factory files contain all 201 existing preset keys, are embedded in the executable and only seeded when absent. Existing customized presets remain untouched.

The twelve role presets each include 219 CFG-backed values, now including 18 A-Life Director group-size settings (4 human rank + 14 mutant species). Previously shipped untouched factory JSONs are upgraded on launch; custom files are preserved.

## 2026-10-10 — independent Water / Energy short-duration keys

Every stock preset now includes `survival.waterRegenDuration = 5s`, `survival.energyFullCostDuration = 5s`, and `survival.energyPartialCostDuration = 15s`. Main drink-duration multipliers and these three real-seconds sliders are independent but constrained to actual source duration. The factory migration upgrades only byte-for-byte stock JSONs; user modifications are not overwritten. Loading a pre-change custom preset without the three fields derives the former short durations from its old main multiplier, clamped to the new vanilla floor and live main ceiling. New saves write the three values explicitly.

## Balance direction — immersion over vanilla abundance

Vanilla S.T.A.L.K.E.R. 2 provides plentiful loot and supplies. All factory presets, including Immersive, should be evaluated against the full acquisition/spending loop rather than preserving this generous baseline as the goal. Progression and scarcity must be role-specific, meaningful and playable, without artificial bullet sponges or unverified CFG interpretations. This is a design rule for future numerical rebalance, not a claim that all existing values have been re-audited. See `docs/FACTORY-PRESETS.md`, “Abundance-aware, immersion-first balance policy”.

## 2026-10-14 — Fully tuned current catalog

All 12 shipped profile files now include **247 parameter IDs** (34 more than the previous 213) with playstyle-specific tuning of the new controls and three existing child drink durations. Full tuning and validation contract: `docs/FACTORY-PRESETS.md` and `tools/factory_preset_expansion.json`. Previously installed, byte-for-byte official factory files upgrade automatically; edited or custom JSONs are never overwritten. All three short drink effects remain bound by their Water / Energy parent duration. Hidden, unverified source multipliers remain neutral.


**2026-11-18 — LAIR POPULATION:** every profile stores HUMAN CAMPS / MUTANT NESTS (`alife.lairPopulation.human` / `.mutant`). The values are Hardcore+ 1.5 / 1.4, Hardcore 1.25 / 1.15, Mutant Hunter 1 / 1.3, Mercenary and Tactical 1.25 / 1, Outcast 1.15 / 1.1, Anomaly Runner 0.75 / 0.85, Scavenger 0.75 / 1, Wanderer 0.85 / 1; the others stay at 1×. Every non-1 value changes real lair caps (`tools/audit_lair_population.py`). Stock files upgrade only when byte-identical.

**2026-11-18 — LAIR REFILL SPEED:** every profile stores `alife.lairRefillSpeed`. The values are Hardcore+ 1.5, Hardcore and Mutant Hunter 1.25, Mercenary and Tactical 1.15, Outcast 1.1, Anomaly Runner and Scavenger 0.85, Wanderer 0.75; the others stay at 1×.

**2026-11-18 — REFILL SPEED → LAIR RESPAWN TIME:** profiles now store `alife.lairRespawnTime` (higher = longer). The values are Hardcore+ 0.65, Hardcore and Mutant Hunter 0.8, Mercenary and Tactical 0.85, Outcast 0.9, Anomaly Runner and Scavenger 1.2, Wanderer 1.35; the others stay at 1×.

**2026-11-18 — ZONE WEATHER + TRADER RESTOCK:** every profile stores EMISSIONS, STORMS, EMISSION WARNING and RESTOCK EVERY. The non-vanilla values are Hardcore+ 1.5 / 1.5 / 40 s / 16 h, Hardcore 50 s / 12 h, Anomaly Runner emissions 1.25, Immersive storms 1.25, Wanderer emissions 1.15 and warning 75 s, Tactical storms 1.15, Scavenger 16 h, Outcast 12 h, Smuggler 6 h. Everything else follows the reference difficulty.

**2026-11-18 — SUMMON RATE:** every profile stores SUMMON RATE (Pseudodog phantoms, Bayun clones, Deer blind dogs). Non-vanilla: Hardcore+ and Mutant Hunter 1.5×, Outcast and Wanderer 1.25×. Pseudodog ABILITIES is its howl; stock Hardcore stores it at 1×.

**2026-11-18 — Mutant move knobs:** SUMMON RATE is retired. Every profile stores one knob per mutant move (68) and Deer ABILITIES, all 1× except the summon knobs: Hardcore+ and Mutant Hunter 1.5×, Outcast and Wanderer 1.25× (Pseudodog phantoms, Bayun clones, Deer and Controller dogs).

**2026-11-18 — Attack-only mutant knobs:** roars, growls, howls, dodges, teleports and shields have no control any more and stay vanilla. Every profile stores 54 attack knobs; Hardcore stores Bayun and Bloodsucker ABILITIES at 1×.

**2026-11-18 — No ABILITIES sliders:** each ability knob holds its own value (the old slider was folded in). 466 controls per profile.

**2026-11-18 — No mutant group sliders:** every attack and ability is its own knob (the old ATTACKS / ABILITIES slider values were folded in). 452 controls per profile.
