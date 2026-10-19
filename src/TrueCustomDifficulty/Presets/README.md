# True Custom Difficulty — Factory Presets

### 2026-10-19 — CURRENT 287-source role coverage (supersedes earlier factory snapshots)

The 12 official profiles still carry **all 287 source-backed parameter IDs** (3,444 serialized entries); **49 distinct real controls** were rebalanced in **445 actual value changes** versus the immediately preceding shipped stock profiles. Source: `tools/factory_profile_role_coverage_2026_10_19.json`, gate: `python3 tools/audit_factory_profile_role_coverage.py`. The earlier 248-ID tables below are HISTORICAL, not current numerical profile values. The diagnostic RC CFG Signal preset is separate and unchanged.

This pass tunes Bread/Sausage/Canned Food/Condensed Milk relief as actual HungerPoints removed; Hunger, Fatigue and Bleeding thresholds; Stamina exhaustion/low-recovery sources; three active Basic stash-category priorities; Medical priority, native Chance and MIN/MAX; Standard/Rare per-category source Chance; and Basic grenade quantity. **Chance scales per-entry authored odds, not a whole-stash guaranteed drop.** Priority changes source preference; MIN/MAX set authored floor/cap, not guaranteed exact counts. The already-curated world abundance, NPC resources, trader costs, overall hunger growth, HP healing and weapon economics remain intact. Do not adjust retired global stash Weight sources or integer-no-op A-Life dimensions to increase changed counts artificially.

**Extra Stash Gear (ON)** for Anomaly Runner, Mercenary, Mutant Hunter, Scavenger and Smuggler uses role-specific weapon/armor/artifact/attachment priorities and limited **8–12% eligible stash coverage** with **20–30% bonus candidate Chance**. To respect the live mutual-exclusion contract, **Shuffle Weapons is OFF** in precisely those five roles. Other seven roles retain Shuffle Weapons ON and Extra Stash Gear OFF. Dormant World Weapon Density is intentionally preserved for later manual toggling. No protected or quest stash entry list, source eligibility, original CFG writer, UI control catalog or game difficulty reference was changed.

`PresetDirectoryService.PreRoleCoverageFactoryBlobs` recognizes exact Git blob identities of the previous pristine 287-ID factory JSON files; startup migrates only byte-identical official copies, never modified factory-named or custom presets. Profiles remain balance hypotheses awaiting actual Windows/Wine and STALKER 2 in-game verification. The old 248-control rationale ledger stays available for provenance but is not the newest role value authority.


### 2026-10-15 — Opt-in RC CFG Signal preset (NOT A FACTORY ROLE)

One **diagnostic** profile `RC - CFG Signal` is embedded separately under `src/TrueCustomDifficulty/QA/` and seeded to the portable `Presets/` directory **only when absent**. It is intentionally excluded from the twelve official 248-source factory snapshots, factory SHA migrations, balance matrices and role audits. On load it resets all other Stalker source controls and applies exactly **13 extreme, individually testable CFG settings** (e.g. Starting Coupons 100000, Carry Weight 160, MaxSP/MaxHP 200, 100% Water instant stamina, 100% eligible Blind Dog loot Chance, and 3x source-backed Blind Dog A-Life cap). Player edits to the seeded diagnostic file are never overwritten. For source paths, per-setting A/B procedure, controlled saves and BPatch/gameplay evidence rules see `docs/RC-CFG-SIGNAL-TEST.md`. This is deliberately **not** a balanced playable difficulty and **not** proof of runtime effects.


**2026-10-15 — Canonical A-Life group-size profile mapping:** All 12 official factory roles retain 248 native sources and identical true human/mutant integer group caps. Only 39 source multipliers are replaced with the editor's canonical effective stops, across 216 unchanged group-size source outcomes. The per-role before/after matrix is `tools/factory_preset_alife_effective_mapping.json`; the native source ledger, role matrix, docs, and 12-file pristine migration hashes are synchronized. Modified or custom preset files are never overwritten. Local .NET/Wine/in-game verification remains pending.


**2026-10-15 — Final CFG-to-writer audit:** 12 × 248 source entries are preserved. **108 source-inert integer A-Life multipliers** were reset to real neutral 1×; their previous fractional values did not modify any `MaxCount` and could make the production writer reject the preset. Authored values, full per-source decisions and their source confidence levels are synchronized in `tools/factory_preset_full_source_audit.json`. The `PreFinalCfgAuditFactoryBlobs` migration only refreshes byte-identical old stock, never customized presets. [Full CFG report](../../../docs/FINAL-CFG-MAPPING-AUDIT-2026-10-15.md). No .NET/Wine/game execution is claimed from this static pass.

**2026-10-15 full-source audit:** 248 CFG-backed controls in each of 12 role profiles, **2,976 independently documented setting decisions** in `tools/factory_preset_full_source_audit.json`, source-normalized numeric values, differentiated major mutant HP classes and no unintended zero rare trophy chance. Existing consumable/editor overrides and slower hunger are preserved; `PreFullDeepAuditFactoryBlobs` upgrades only exact-byte stock files. This snapshot is not an in-game verified difficulty rating. Full report: `docs/FACTORY-PROFILE-FULL-AUDIT-2026-10-15.md`.

**2026-10-15 consumable cadence update:** All twelve stock profiles enable the serialized duration override, use slower hunger growth (0.007–0.014 points/s) and get longer, role-specific Water, Energy Drink, Vinca, Hercules and Psi Block effects. Field Medic alone additionally enables the medical-limit override for medkit bleeding treatment. All 248 native CFG controls remain independent; exact-byte pristine old factory files upgrade but user-edited/custom presets stay untouched. Source-of-truth: `tools/factory_preset_consumable_balance.json`. Publish/Wine/in-game effects remain unverified.

**2026-10-15 factory role deep rebalance:** 12 full 248-ID profiles, 78 audited source controls, 702 changed values. Strong profession identities cover combat, medical supplies, traversal, artifact/trophy economies, world scarcity, repair/wear and hazard exposure. `tools/factory_preset_role_rebalance.json` is the reproducible per-role delta matrix, with the full rationale in `docs/FACTORY-PROFILE-DEEP-AUDIT-2026-10-15.md`. Exact-byte prior factory presets auto-upgrade without replacing user-edited files; user-created profiles are safe. Build/Wine and real gameplay checks are pending.

### 2026-10-15 — 248-source factory presets and exact stock upgrade (CURRENT)

The two independent Water strength sources are both present in all **twelve shipped factory presets**: `survival.waterStaminaEffectStrength` (WaterStamina2 +25% Stamina type) and `survival.waterStaminaCostReduction` (WaterStaminaPerAction1 −10% SPDrain). Each new cost-cut entry inherits that factory profile's **previous shared Water strength** so the pre-split gameplay setting stays equivalent. The source catalog has **248** independent entries per factory profile, versus the previous 247. The existing role-specific 46-source scarcity balance values, other drink settings, durations, difficulty references and raw writer routes are untouched. `PresetDirectoryService` recognizes exactly the twelve pristine previously-shipped 247-source JSON blob hashes; it upgrades only those byte-identical stock files and never overwrites user-edited presets. User presets without the new cost key continue to load via the existing in-memory Water split migration.

## Current scarcity-first balance — 2026-10-14

The twelve role presets deliberately reduce vanilla's excess supplies through real loot categories, NPC/stash/trader availability, limited medical healing, sleep recovery and coherent economy/repair costs. Food never heals HP in these factory profiles; extra corpse smart-loot bonuses are OFF. Field Medic and Scavenger retain a small 10 HP bandage heal, others use bandages for bleeding control. The full source-backed tuning matrix lives in `tools/factory_preset_scarcity_balance.json`, described in `docs/FACTORY-PRESETS.md`. Previously shipped byte-identical factory JSON files automatically upgrade; edited/user presets stay untouched. This is preset data, not a new UI/CFG setting.

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
- Scavenger: Food Restores Health = OFF; Bandage Healing remains 10 HP

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

