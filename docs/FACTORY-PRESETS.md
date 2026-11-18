## 2026-11-18 — CURRENT: scarcity-first role refinement, 12-profile second pass

**Authoritative overlay:** `tools/factory_preset_scarcity_refine_2026_11_18.json`. This supersedes only the listed values from older `factory_preset_*` ledgers, while leaving their dated source decisions as historical evidence. The first 2026-11-18 role-vibe pass remains active everywhere else. **140 actual preset edits**, including **16 Smart Loot category toggles**, in the existing **452 native controls per role**. No new control IDs or baseline difficulty changes.

**Confirmed source gap:** before this pass, every profile had `loot.genericCorpseReduction=0` and `loot.genericStashReduction=0`, even where world item removal was 50–81%; most had stash Smart Loot actively giving resource-sensitive bonuses with high chances. The existing `loot.npcCorpseBonus` choice is **empty in all 12 roles**: toggling `npcSmartLootEnabled` would not itself suppress those already-absent optional bonus packs. We deliberately leave that dormant toggle alone instead of claiming a false scarcity effect. Native NPC medical Chance, regular stash category odds and actual generic-corpse/stash endpoints are separate and active.

| Profile | Generic corpse reduction | Generic stash reduction | Active stash smart bonus: stocked → low | Key scarcity identity and guardrail |
|---|---:|---:|---|---|
| Anomaly Runner | 10% | 10% | 0.30× → 0.60× | Less incidental ammo/food; keep better but finite artifact hunting; 10% bonus-gear coverage, 20% chance |
| Field Medic | 5% | 10% | 0.25× → 0.65× | Keep useful corpse medicines and a full 1× medic-trader stock, while reducing free food/ammo/grenades |
| Hardcore+ | 15% | 15% | OFF | Already removes 81% ordinary loose loot and has both Smart Loot masters OFF; small extra cuts, no universal wipe |
| Hardcore | 10% | 10% | 0× → 0.45× | Modest need-based emergency supply, without free stocked-bag loot or free food packs |
| Immersive | 10% | 15% | 0.20× → 0.50× | Sparse, plausible pickups and rare supplies; keep some organic food and medicine |
| Mercenary | 15% | 10% | 0.15× → 0.50× | Reward paid contracts instead of free stash ammo; traders retain 1× ammunition |
| Mutant Hunter | 10% | 10% | 0.20× → 0.60× | High ammo demand gets 0.75× trader ammo supply; fewer free grenades and premium gear |
| Outcast | 15% | 15% | 0× → 0.35× | Harsh subsistence, but rare emergency med/ammo chance persists |
| Scavenger | 5% | 15% | 0.10× → 0.45× | Bodies remain worth searching; scarce stash freebies, equipment is a rare prize |
| Smuggler | 20% | 10% | 0.15× → 0.50× | Black-market resupply, not corpse farming; no stash smart meds/grenades |
| Tactical | 15% | 10% | 0.15× → 0.50× | Human fighting consumes ammunition; keep 0.75× trader ammo but limit freebies |
| Wanderer | 10% | 10% | 0.25× → 0.65× | Traversal remains provisioned for travel, not automatically topped up with ammunition |

**Layered source effects, not a claimed final drop percentage.** The two generic scarcity masters proportionally reduce *eligible generic source Chance/endpoints* and are deliberately capped here at 5–20% (corpse) / 10–15% (stash), because the pre-existing per-item chances, Standard/Rare category odds, world physical removals, trader stock and Smart Loot odds are already reduced. Do not add these percentages together or interpret a 10% generic reduction as 10% fewer items in every saved game. Existing world actors may already have spawned. The NPC bonus choice, item attached ammo, unique/quest stashes, loot SIDs, equipped weapons, and quest rewards stay untouched.

**Per-role supply consistency:** `loot.stashSmartStockedChance` is never above its `stashSmartShortChance` counterpart, and needy-player chance remains nonzero for active Smart Loot masters; disabled Hardcore+ Smart Loot settings are not falsely edited. No disabled extra-gear settings were edited. The existing medical healing/repair/carry and scarcity-first economy choices remain except the explicit selective supply edits. All 12 roles retain **one artifact per eligible spawn** (`environment.artifactCount=1`) and **positive** medicine and ammunition paths. Scarcity in the game must still be verified with *fresh* generic corpses/stashes, random world actor builds and trader restocks; source writers alone cannot prove actual UE RNG.

**Validation:** `tools/audit_factory_preset_coverage.py` pins all 140 current overlay entries, 53 mutant move values/role, valid source ranges, enabled controls, Smart Loot hierarchy and all 12 old factory Git SHA-1 identities. `tools/audit_factory_profile_role_coverage.py` and `tools/audit_project_consistency.py` retain historical balance contracts while applying **only these explicit current overrides** where an old value differs. `AllControlsPatchContract` adds role-specific corpse/stash and Smart Loot assertions plus the existing 12-profile full 452-value load/save check. `PresetDirectoryService.PreScarcityRefineFactoryBlobs` upgrades only byte-identical old factory copies, never modified custom or factory-named presets. In-game effectiveness remains runtime-test-required.

## 2026-11-18 — CURRENT: 12 distinct factory vibes (452-control source-aware rework)

**Source of truth:** `tools/factory_preset_vibe_2026_11_18.json`, applied *after* the historical `factory_preset_*_2026_11_17/18.json` epoch snapshots. All **12 profiles** retain all **452** native catalog values, original base difficulty and player-preset JSON format. This pass makes **539 individual existing numeric-control edits**, including **437 actual changes** among the 636 per-species mutant move values (53 × 12). The 53 monster rate knobs were examined individually per role: some deliberately remain at their difficulty's source default, rather than forcing a cosmetic non-vanilla value. Rebalanced source targets include tactical human moves, special move pacing, artifact appearance and rarity (not per-spawn artifact count), weather/emission pressure, lair population/refill, trader restock and fall damage. All old per-role health/healing, loot category, weapon/economy and trader buy/sell decisions are retained; no retired or unavailable controls are reintroduced.

| Role | Identity | Signature mutant move | Distinct Zone/gameplay trade-off |
|---|---|---|---|
| Anomaly Runner | Anomaly/psi specialist | Poltergeist anomaly/throw 1.75×; ordinary wild dogs 0.75× | Faster trader restock 6 h; fall damage 75%; storms 1.15×; 55 s cover |
| Field Medic | Care and recovery | Blind Dog bites 0.75×; telekinetic and psi pressures remain | Special moves 0.75×; supplies refill 6 h; fall damage 80%; milder emissions |
| Hardcore+ | Extreme hostility | Bloodsucker leap and Burer disarm 3×; even minor fauna dangerous | Special moves 2.75×; 35 s cover; trader restock 20 h; no fall relief |
| Hardcore | Punishing but readable | Chimera long leap 2.75×; Pseudogiant shockwave 2.25× | Special moves 2.25×; storms 1.35×; trader restock 14 h |
| Immersive | Believable ecosystem | Bloodsucker leap 1.5×; flesh attacks 0.75× | Mature ecology: slower lair refill 1.15×, artifacts rarer, traders 10 h |
| Mercenary | Professional fire-and-maneuver | Burer disarm 1.75×; elite predators stronger | Humans use 1.5× grenades/flanks/suppression; resupply 5 h |
| Mutant Hunter | Dangerous trophy hunting | Pseudodog phantom summons 2.25×; predators 1.5–2× | Mutant lair density 1.5×, refill 0.75×, trader restock 6 h |
| Outcast | Hostile exile | Controller psy 1.75×, varied hard mutant moves | Enemy flanks/pushes 1.5×; traders 16 h; rare artifacts and bad weather |
| Scavenger | Cautious scarcity scavenging | Blind Dog running bite 0.5×, elite psi still dangerous | Less alert patrols, trader restock 18 h, fewer rare artifacts |
| Smuggler | Stealth courier | Controller psy 1.5×, common fauna slower | Enemy alert memory 0.65×, traders 4 h, softer falls; storm cover |
| Tactical | Disciplined combat | Sharp mutant rushes, moderate elite threats | Human flanks/suppression/grenades 1.75×, memory 1.75×, accuracy 1.3× |
| Wanderer | Lone Zone exploration | Poltergeist throws 1.75×, wildlife uneven | Fewer human lairs 0.8×; trader restock 12 h; falls 65%; rare luck 1.25× |

**Meaning of the balance:** A higher individual mutant **move rate** means a shorter cooldown between uses of that *specific* native move; a higher **lair respawn time multiplier** means longer waits for the same lair refill. Higher **artifact spawn chance** means more likely native artifact rolls, not extra artifacts per spawn; `environment.artifactCount=1` across all twelve roles. More intense Hardcore+ and Mutant Hunter encounters do not grant additional loot by themselves. Reduced fall damage is reserved for travel/courier/field recovery profiles; Hardcore remains at the native 100%.

**Migration and validation:** `PresetDirectoryService.PreVibeFactoryBlobs` contains the twelve exact Git-blob SHA-1 hashes of the immediately preceding stock profiles, so an untouched installed factory profile upgrades automatically. **User-edited factory-named profiles and custom profiles are never overwritten.** The `tools/audit_factory_preset_coverage.py` matrix audit validates every tuned value and all 53 monster rates per role; `AllControlsPatchContract` checks individual role signatures, a complete 452-setting load/save, and the existing 12-factory no-drift contract. Actual in-game behavior and AI cooldown semantics remain runtime-validation work, not proven by these static edits.

## 2026-11-18 — CURRENT: factory preset 0.05× fidelity for human HP and phantom waves

* **HUMAN ENEMY HP** (`combat.npcHealth`) and global **PHANTOM SPAWN INTERVAL** (`combat.psySpawnCycleDelay`) use the native `0.25×–2×` editor with **0.05× steps** rather than the old 0.10× grid. All twelve factory role profiles deliberately contain values such as 0.55× / 0.65× / 0.85× / 0.95×. Previously, loading rounded these values silently (e.g. 0.85× → 0.8×), changing actual gameplay. The catalog UI step and the preset normalizer now agree. No preset files, source CFG targets, role intent, value ranges, other combat controls or writers change.
* `AllControlsPatchContract.VerifyFactoryFineStepPresetValues` asserts 40 cross-difficulty 0.05× import/save cases and the exact catalog bounds; the existing twelve-factory-profile unchanged-load contract checks all original stored values. A static source assertion is not a substitute for the user's Windows/Wine test.

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

## 2026-11-17 — CURRENT: factory presets store every control

> **Counts here are a dated snapshot.** The live census is `tools/catalog_census.py` (2026-11-17: 406 catalog IDs, 202 visible, 204 hidden, 2,819 direct edit points).

* All 12 factory presets contain every catalog control (406 on 2026-11-17; 399 when this note was written). The 40 controls added on 2026-11-16 (MUTANT AGGRESSION 25, ARTIFACT HUNTING 4 — RESPAWN RATE retired 2026-11-17, HUMAN TACTICS / HUMAN AWARENESS 9, corpse/stash SUPPLY REDUCTION 2) are stored at the preset's reference-difficulty vanilla value (Master presets: 2× on the Master-only combat-sync rows), exactly what loading produced while they were missing. Nothing plays differently; thematic tuning of these controls comes later.
* `PresetDirectoryService.PreNewControlsFactoryBlobs` holds the previous stock hashes, so byte-identical installed factory copies upgrade; edited presets stay untouched.
* `tools/audit_factory_preset_coverage.py` checks full coverage, the stored vanilla values and the upgrade hashes.

## 2026-11-16 — CURRENT: factory presets match MEDICAL COMPOSITION (option a)

* All 12 factory presets now store each medical Weight factor (`loot.stashMedical{Item}Bias`) equal to its Chance scale, i.e. what loading already produced since one MEDICAL COMPOSITION cell drives both (0–100%). The old 1.25–1.75× Weight boosts (Field Medic, Anomaly Runner, Mutant Hunter, Scavenger, Wanderer, Immersive, Smuggler) were silently dropped on load; they are now gone from the files too. Field Medic's stash medicine is therefore vanilla (100%); its medical identity stays in healing, bleeding and trader/NPC supply.
* `PresetDirectoryService.PreMedicalMirrorFactoryBlobs` holds the previous stock hashes, so byte-identical installed factory copies upgrade; edited presets stay untouched. Older tuning matrices under `tools/factory_preset_*.json` still list the retired Weight values.

## 2026-10-20 — RC candidate cleanup: diagnostic profile retired from app

The **RC - CFG Signal** diagnostic is no longer shipped, embedded or auto-created by the application. The preset picker contains the **12 official factory roles** plus any unrelated user profiles. During the first run after updating, the exact byte-for-byte stock diagnostic file is deleted. A user-edited diagnostic file is **moved without overwriting** into `Presets/Archived/` (with a numbered name if necessary), so it disappears from the picker without losing user changes. The 13-signal CFG contract remains in `tests/RCSignalPatchContract/Fixtures/RC-CFG-Signal.json`, embedded **only in the test assembly**, not in the published EXE. This is release-candidate cleanup, **not** proof of a passing post-change build, Wine UI test, game-load test or in-game effects.

# Factory Presets

### 2026-10-19 — HISTORICAL 287-source role coverage (supersedes earlier factory snapshots)

The 12 official profiles still carry **all 287 source-backed parameter IDs** (3,444 serialized entries); **49 distinct real controls** were rebalanced in **445 actual value changes** versus the immediately preceding shipped stock profiles. Source: `tools/factory_profile_role_coverage_2026_10_19.json`, gate: `python3 tools/audit_factory_profile_role_coverage.py`. The earlier 248-ID tables below are HISTORICAL, not current numerical profile values. The RC CFG Signal diagnostic is retired from the application as of 2026-10-20; its writer fixture is test-only.

This pass tunes Bread/Sausage/Canned Food/Condensed Milk relief as actual HungerPoints removed; Hunger, Fatigue and Bleeding thresholds; Stamina exhaustion/low-recovery sources; three active Basic stash-category priorities; Medical priority, native Chance and MIN/MAX; Standard/Rare per-category source Chance; and Basic grenade quantity. **Chance scales per-entry authored odds, not a whole-stash guaranteed drop.** Priority changes source preference; MIN/MAX set authored floor/cap, not guaranteed exact counts. The already-curated world abundance, NPC resources, trader costs, overall hunger growth, HP healing and weapon economics remain intact. Do not adjust retired global stash Weight sources or integer-no-op A-Life dimensions to increase changed counts artificially.

**Extra Stash Gear (ON)** for Anomaly Runner, Mercenary, Mutant Hunter, Scavenger and Smuggler uses role-specific weapon/armor/artifact/attachment priorities and limited **8–12% eligible stash coverage** with **20–30% bonus candidate Chance**. To respect the live mutual-exclusion contract, **Shuffle Weapons is OFF** in precisely those five roles. Other seven roles retain Shuffle Weapons ON and Extra Stash Gear OFF. Dormant World Weapon Density is intentionally preserved for later manual toggling. No protected or quest stash entry list, source eligibility, original CFG writer, UI control catalog or game difficulty reference was changed.

`PresetDirectoryService.PreRoleCoverageFactoryBlobs` recognizes exact Git blob identities of the previous pristine 287-ID factory JSON files; startup migrates only byte-identical official copies, never modified factory-named or custom presets. Profiles remain balance hypotheses awaiting actual Windows/Wine and STALKER 2 in-game verification. The old 248-control rationale ledger stays available for provenance but is not the newest role value authority.


### 2026-10-17 — Current factory/audit reconciliation

The twelve official role presets remain v1 snapshots of 248 source-backed parameters; exact-stock migration protects edited factory files and all custom presets. The 2026-10-15 role-first rebalance is **HISTORICAL; SUPERSEDED** by the current full source-backed review and canonical A-Life mapping. RC CFG Signal is an opt-in diagnostic, not a thirteenth factory role. Static checks must not expect the retired role-first section to be labelled CURRENT. No factory values, migration rules or JSON schema change in this revision.

### 2026-10-15 — Historical diagnostic RC CFG Signal preset (RETIRED)

Historical context: the application previously auto-seeded a separate 13-setting extreme CFG diagnostic. Since 2026-10-20 the profile is absent from published builds and the preset picker; equivalent inputs remain a **test-only fixture** for the production writer contract. The archived A/B and gameplay test procedure remains in `docs/RC-CFG-SIGNAL-TEST.md`. None of the twelve official factory roles is modified by this removal.


### 2026-10-15 — Canonical A-Life factory profile mapping (CURRENT)

The existing A-Life integer-effective editor requires one genuine selectable multiplier per distinct set of authored `MaxCount` limits. All **12 factory profiles** now store those canonical stops: **39 multipliers are rewritten while 216/216 active A-Life source outputs stay exactly the same**. The 248 parameter IDs, four human rank controls, fourteen species controls, source-only neutral generic, role-specific intent, difficulty references and all other native values remain intact. The original/current per-profile values and actual rank limits are documented in [`tools/factory_preset_alife_effective_mapping.json`](../tools/factory_preset_alife_effective_mapping.json), with matching full-source ledger, role matrix and A-Life table. `PreEffectiveALifeFactoryBlobs` upgrades only byte-identical old stock factory profiles, never user-edited/custom profiles. No UI renderer/CFG writer change, or measured spawn-frequency claim.


### 2026-10-15 — Final CFG route / non-noop A-Life correction (HISTORICAL; SUPERSEDED)

The final source-to-production-writer audit found **108 ineffective A-Life group-size multipliers** across the twelve shipped stock profiles. Their decimal values differed from 1×, but integer `MaxCount` patch transformations produced exactly the original source values, potentially causing Build to reject a profile for lack of concrete output. All 108 affected values are now **genuine neutral 1×**; effective A-Life and the 247 other settings are preserved. Every revised profile still contains 248 native sources. The updated `tools/factory_preset_full_source_audit.json` records each reason; `tools/factory_preset_role_rebalance.json` stays synchronized. Pristine-only previous stock hashes are migrated by `PreFinalCfgAuditFactoryBlobs` and edited/custom profiles stay untouched. For per-source BPatch evidence and limitations see [the final CFG audit](FINAL-CFG-MAPPING-AUDIT-2026-10-15.md).

### 2026-10-15 — Full source-backed factory audit (CURRENT)

All twelve factory profiles have now been audited against the **248-entry native CFG source catalogue**, with **2,976 individual profile-setting decisions** and per-source evidence/role reasoning in `tools/factory_preset_full_source_audit.json`. Existing values were reauthored to match **actual UI normalization** instead of silently changing on preset load, and eight previously identical major mutant HP classes were differentiated by role. Rare positive trophy chances no longer accidentally load as zero; the newer matrix remains synchronized with older factory matrices. **197 confirmed / 51 runtime-pending** source mappings are labeled honestly; full .NET/Wine and in-game tests are pending. Only exact pristine preceding stock JSONs migrate; edited user profiles are preserved.

**Scope:** 12 factory role snapshots keep 248 native source IDs each, 5-second consumable Override Limits plus Field Medic's separate medical override, unchanged `referenceDifficulty`, and the slower hunger rate from the preceding rebalance. Every entry now has an explicit **native source purpose, CFG edit targets, evidence status, role-specific stored value, per-role tradeoff reason and reference comparator**. See [the full per-setting table](FACTORY-PROFILE-FULL-AUDIT-2026-10-15.md) and `tools/factory_preset_full_source_audit.json` for the per-role rationale. This is an authored balance hypothesis, not a claim about live spawn rates, drop frequencies or time-to-kill. The audit intentionally preserves shared stage thresholds, recovery limits and unverified neutral parameters where variation would be unjustified.

**The prior “role-first deep rebalance” and “consumable cadence” passages are historical context, superseded by this full-source pass.**

### 2026-10-15 — Consumable cadence, override and role differentiation (HISTORICAL; SUPERSEDED)

All 12 factory profiles explicitly save the existing `drinkDurationOverride` UI-only root flag. This permits **270s maximum drink effects** and **900s maximum Vinca/Hercules/Psi Block durations** without inventing a CFG parameter. Each profile retains **248 native CFG-backed controls** (preset format v1) and the same independent Water bonus/cost-cut sources and parent/child duration limits. **Field Medic** alone additionally enables the independent `medicalOverride` to allow much stronger source-backed medkit bleeding treatment. The source of truth is `tools/factory_preset_consumable_balance.json`; earlier expansion/scarcity/role matrix expectations remain synchronized.

Every preset now uses a **0.007–0.014 points/s hunger gain** (vanilla **0.015**), reducing repeated eating while keeping ordinary-food HP healing OFF, food scarcity, hunger thresholds and starvation rules unchanged. Role-specific Water and Energy Drink main durations cover **90–240s / 135–270s**, with matching source-native short effects; Vinca **350–750s**, Hercules **270–800s**, and Psi Block **200–850s**. Drink restoration amounts, stamina benefits, Vinca/Psi strength and Hercules load capacity also vary strongly by profession. These are configured effect duration and magnitude sources, **not measured hunger frequency or verified in-game effect stacking**.

The existing preset loader restores override mode **before** source values. `PreConsumableRebalanceFactoryBlobs` authorizes reseeding only *exact-byte pristine* previous factory files; modified factory files and user-created profiles are never overwritten. No UI geometry, interaction, title, CFG writer or native parameter was added/changed; Wine and game validation remain pending.

### 2026-10-15 — Role-first deep rebalance (HISTORICAL; SUPERSEDED)

All **twelve factory profiles** still store all **248 native CFG-backed catalog parameter entries**, while **78 audited control IDs** have been rebalanced across all ten tabs, resulting in **702 actual value changes**. The committed reproducible source is `tools/factory_preset_role_rebalance.json`, which is checked against every role JSON by `tools/audit_project_consistency.py`. Original 37-source expansion and 46-source scarcity matrices are synchronized wherever this pass changes their controls.

This is a **role and resource-loop rebalance**, not just scaling enemy HP: different specialists are exposed to different resource channels and failure costs. Source-specific world loot, NPC, stash, containers, traders, medical treatment, price multipliers, stamina, bleeding, radiation, anomaly severity, equipment, mutants and A-Life are balanced as complementary systems. Drastically nonvanilla values are intentional when sourced and bounded; labels on removal/density/price sliders do not imply total observed spawn, drop or income rates.

**Preserved:** original profile names and base reference difficulty, native CFG writers, save format v1, quest/unique-weapon placement guarantees, all 248 parameter IDs, UI controls and catalogs, independent Water bonus/cost-cut sources, native slider bounds and time relationships, finite treatment and ammunition even in Hardcore+. The known but unverified hidden `survival.drunknessThresholdScale` factory setting is neutralized to 1x rather than asserting a real gameplay effect.

**Migration:** `PresetDirectoryService.PreRoleRebalanceFactoryBlobs` allowlists the exact Git-blob SHA of each immediately preceding shipped **248-source** preset. Only byte-identical pristine installed factory files upgrade automatically; any edit to a stock name, even metadata only, prevents replacement. Custom/renamed presets remain untouched. Restart may be needed to seed the new stock profiles.

See `docs/FACTORY-PROFILE-DEEP-AUDIT-2026-10-15.md` for per-role decisions and the source-constrained comparison table. Static source validation is not real gameplay benchmarking; local publish/Wine and in-game testing remain necessary.

### 2026-10-15 — 248-source factory presets and exact stock upgrade (HISTORICAL; SUPERSEDED)

The two independent Water strength sources are both present in all **twelve shipped factory presets**: `survival.waterStaminaEffectStrength` (WaterStamina2 +25% Stamina type) and `survival.waterStaminaCostReduction` (WaterStaminaPerAction1 −10% SPDrain). Each new cost-cut entry inherits that factory profile's **previous shared Water strength** so the pre-split gameplay setting stays equivalent. The source catalog has **248** independent entries per factory profile, versus the previous 247. The existing role-specific 46-source scarcity balance values, other drink settings, durations, difficulty references and raw writer routes are untouched. `PresetDirectoryService` recognizes exactly the twelve pristine previously-shipped 247-source JSON blob hashes; it upgrades only those byte-identical stock files and never overwrites user-edited presets. User presets without the new cost key continue to load via the existing in-memory Water split migration.

## 2026-10-14 — Abundance correction: all twelve profiles rebalanced (HISTORICAL; SUPERSEDED)

**This numerical pass implements the governing abundance-aware policy below.** Each of the twelve embedded profiles has been rebuilt across **46 existing CFG-backed balance controls** (world sources, NPC/container loot, trader stock, healing, money, repairs, carry/wear and selected hazards). Independent tuning is authored in `tools/factory_preset_scarcity_balance.json`; there is no fake universal scarcity setting. All original 247 parameter IDs remain; the earlier 37-entry source-completion matrix and all UI-control contracts remain unchanged.

The following numbers are **configured source-specific controls**, not observed all-world percentages, live spawn statistics or demonstrated in-game drop rates. `Weapon density` applies only to compatible regular world-weapon locations; the removal sliders affect their own proven eligible categories.

| Factory profile | Loose world removed | Ammo boxes removed | Food boxes removed | Medical boxes removed | World weapon density kept | Sleep HP |
|---|---:|---:|---:|---:|---:|---:|
| Immersive | 48% | 45% | 38% | 50% | 70% | 60 HP |
| Tactical | 47% | 44% | 37% | 48% | 70% | 45 HP |
| Field Medic | 43% | 52% | 38% | 32% | 70% | 90 HP |
| Mutant Hunter | 52% | 50% | 40% | 48% | 70% | 50 HP |
| Anomaly Runner | 50% | 55% | 40% | 52% | 70% | 60 HP |
| Wanderer | 46% | 55% | 37% | 52% | 70% | 70 HP |
| Mercenary | 48% | 42% | 55% | 50% | 65% | 50 HP |
| Smuggler | 60% | 63% | 50% | 62% | 55% | 40 HP |
| Scavenger | 66% | 60% | 54% | 66% | 45% | 35 HP |
| Outcast | 75% | 78% | 65% | 78% | 30% | 25 HP |
| Hardcore | 72% | 76% | 64% | 76% | 35% | 25 HP |
| Hardcore+ | 84% | 86% | 78% | 86% | 20% | 15 HP |

**System decisions (all 12):**
- Ordinary food no longer restores HP. It remains a hunger resource. All optional NPC smart-loot corpse bonus packages are set to NONE, removing the extra water/medical-ammo source while leaving native NPC equipment and independently modeled corpse drops unchanged.
- Bandages prioritize bleeding control instead of easy health restoration: only **Field Medic** and **Scavenger** retain a small **10 HP** healing bonus; every other factory profile uses **0 HP**. Bleeding removal remains an independent source with real CFG semantics. Field Medic's earlier invalid 130-point bandage bleeding removal is corrected to the true 100-point cap.
- Sleep recovery is limited to **15–90 HP**, no longer an automatic 200-HP refill. Medkit potency, medical inventories and accessible treatment differ by role; Field Medic remains a relative medical specialist without unlimited medicine.
- Ammo acquisition is reduced through separate NPC, stash, crate and trader routes; weapon density is controlled independently of shuffling. The existing nine non-quest unique weapons remain subject to their existing shuffle contract, not arbitrary removal.
- Trader stock, mission income, resale values, consumable/ammo prices, weapon/armor costs, artifact/trophy prices, travel, wear and repairs are coordinated by profile. High-earning roles retain financial advantages, but cannot offset scarcity for free.
- Extreme modes reduce their earlier combined **92% crate/loose removal**, near-zero quest earnings and punishing damage/wear stack. Hardcore+ remains the most resource-limited, but avoids deliberately unreachable recovery. Enemy maximum-health multipliers and preset difficulty references are not inflated to fake challenge.
- Six previously out-of-range starvation tick intervals (12s beyond catalog 10s) are clamped to their actual editor ceiling. Current preset values must honor native source limits.
- Previous untouched factory files are upgraded by a new **exact-byte Git blob SHA** migration table in `PresetDirectoryService`. This includes the immediately preceding 247-value factory release. Modified factory files, custom filenames, and users' personal profiles are never silently overwritten.

**Acceptance:** regression checks validate exact 247 IDs/profile, authored 46-value role matrices, all source ranges and absolute-step grids in the new balance, food/NPC bonus/sleep invariants, parent drink durations, the existing 37-control expansion and Hardcore/Hardcore+ scarcity plus medical rules. These settings are *authored starting balance*, not a claim of measured game simulation; local compilation and gameplay verification are still required.

## 2026-10-14 — Abundance-aware, immersion-first balance policy (CURRENT)

**Binding balance direction for every future factory-profile pass:** vanilla S.T.A.L.K.E.R. 2 is already generous with available items and resources. **A vanilla-level supply of consumables, ammunition, weapons, money or healing must not automatically be considered balanced merely because it is the game's baseline.** Factory presets aim to make the Zone credible and consequential, not simply increase enemy hit points or multiply damage.

- **Start from the resource economy and gameplay loop, not isolated sliders.** Evaluate world pickups, crate/container yields, NPC loot, trader stock, mission rewards/coupons, artifact income, item availability, consumption, healing, equipment wear, repairs, carry limits and travel together. A reduction in one source may be cancelled by generosity in another.
- **Preserve meaningful scarcity in every profile, including Immersive.** Players should sometimes need to choose between carrying, selling, using, repairing or saving supplies; gathering equipment should not trivialize those decisions. The baseline direction is less effortless abundance than vanilla, with different degrees and bottlenecks per profile rather than a single global scarcity multiplier.
- **Keep each playstyle coherent.** Field Medic can have better treatment efficiency or access to medical supplies without infinite free healing; Tactical and Mercenary can be combat-ready without unlimited ammunition; Wanderer and Anomaly Runner can prioritize mobility and anti-radiation preparation without making exposure or supplies irrelevant; Scavenger/Outcast/Hardcore/Hardcore+ lean further into scarcity. Immersive aims at plausible survival economics, not a disguised Vanilla preset.
- **Maintain proportional risks and real recovery options.** The goal is tension and preparation, not arbitrary grind, unavoidable softlocks or bullet-sponge enemies. Avoid stacking extreme scarcity, high incoming damage, excessive wear and prohibitive costs without considering the complete feedback loop.
- **Tune grounded source-backed controls only.** Confirm the actual CFG effect, clamp and step for each control before choosing values; do not infer mechanics, treat an undocumented multiplicative knob as a drop-rate guarantee or alter hidden/unverified sources to compensate for scarcity.
- **Audit concrete totals and consequences before changing numbers.** Compare item acquisition and expenditure by source, run the full catalog/preset/CFG regression checks, retain unique weapon/quest guarantees and preserve custom user files. Treat balancing judgments as hypotheses until tested locally/in game.

**This section is a governing principle, not a claim that the currently shipped twelve profile numbers have already passed a complete economy-and-abundance re-audit.** The present profiles and their 247 CFG-backed values remain unchanged by this documentation-only decision. A subsequent dedicated rebalance must review the existing values against these criteria, document the rationale per playstyle, and update the preset JSONs and related regression contracts together on `main`.

## 2026-10-14 — Full current-control coverage and thematic rebalance (CURRENT)

All **12 factory profiles** now save exactly **247 catalog parameter values** instead of 213; the **34 missing source IDs** have been added. These cover six previously missing mutant-health species, Water/Energy instant stamina and buff magnitude/duration, Beer/Vodka radiation removal, Vinca/Hercules/Psi Block magnitudes and durations, alcohol effects, five movement-stamina states, repair-price source, loose unique-weapon shuffle and generic Director cap. The three already-present short drink durations are now tuned per role rather than always 5/5/15 seconds. **No new gameplay CFG IDs or UI control types were created.** Player-facing control positions, labels and Stamina Usage layout stay as catalogued.

The reproducible authored matrix is `tools/factory_preset_expansion.json` (group IDs and role-specific numeric/boolean vectors); the shipped JSON files contain full expanded values, not formulas. The consistency audit enforces exact **247/247** ID coverage, matrix parity, source bounds, hidden neutral values, beverage parent/child duration ceilings and existing hard-profile scarcity/medical guarantees. Every profile retains its original referenceDifficulty, preset identity, existing 213 values except for the three intentional short drink-duration adjustments, and native CFG writer behavior. The source-only hidden repair difficulty, generic mutant group size and unverified Drunkness stage thresholds remain **1×** in all profiles rather than inventing balance effects.

| Profile | Focus of newly completed controls |
|---|---|
| Immersive | Vanilla-like drinks, alcohol, medicine and movement-state effects; modest low-tier mutant variation |
| Tactical | More immediate energy restoration, slightly easier running stamina, efficient short energy-cost buffs |
| Scavenger | Tighter consumables, weaker medicine and movement recovery, variable low-tier mutants; unique loose weapon placements shuffled |
| Mutant Hunter | Stronger wildlife and psi mutants, longer energy benefits, dedicated anti-bleeding and expedition medicine |
| Anomaly Runner | Fast travel stamina, stronger Water/Energy effects, improved radiation removal, Hercules/Psi Block support and shuffled uniques |
| Hardcore | Scarcer, weaker consumables and treatment; harsher active-movement recovery and stronger mutant threats |
| Hardcore+ | Further restricted instant/sustained benefits, weaker treatment, harsher recovery penalties and more dangerous wildlife |
| Mercenary | Combat-mobility drinks, efficient energy buffs, moderate medical backup and mobile weapon-focused missions |
| Wanderer | Most sustained traversal benefit, longer Water/Energy windows, stronger load-carry and anti-radiation support; shuffled uniques |
| Outcast | Lower-quality medicine, depleted stamina recovery and a hazardous mutant environment |
| Smuggler | Long-haul cargo and radiation-removal benefits, moderate energy restoration and shuffled loose unique weapons |
| Field Medic | Strongest Vinca/Psi Block protection and prolonged treatment, with practical medical and radiation relief |

**Portable-install upgrade:** `PresetDirectoryService` now recognizes the twelve exact **213-entry factory-file Git blob hashes** from the immediately previous release. On startup it replaces only those byte-identical, unedited stock files with the latest embedded factory variants. User-modified factory files and custom-named JSONs are preserved. Users who customized a factory name can copy it under another name and delete the untouched factory filename to reseed the new official profile. No format revision; old custom presets remain loadable with missing values resolved by the loader.

Since 2026-10-12, Sleep's HEALTH RESTORED editor has a 100-HP tool default and an editable range of 0–200 HP (0x disables sleep health restoration). Existing curated factory presets retain their authored healing choices if within range; values previously above 200 HP are capped at 200 HP. The real vanilla source remains 300 HP.

True Custom Difficulty ships with twelve custom factory presets. They replace the old
factory preset files named after the four vanilla game difficulties.

The vanilla difficulties still remain the **reference/baseline model** used internally
by the tool. A factory preset therefore has its own `referenceDifficulty`, but the
preset displayed to the user is one of the twelve custom profiles below.

| Factory preset | Reference difficulty | Intended player |
|---|---|---|
| Anomaly Runner | Stalker | Wants traversal, anomalies, radiation and artifact exploration to dominate |
| Field Medic | Stalker | Wants treatment, bleeding management and stable recovery to sustain expeditions |
| Hardcore | Master | A resource-aware survivalist: avoid fights, preserve supplies, and plan every expedition |
| Hardcore+ | Master | A last-resort survivor in an almost stripped Zone, with little margin for mistakes |
| Immersive | Stalker | Wants the Zone to feel believable and systemic without becoming punitive |
| Mercenary | Veteran | Wants contract-driven gunfights with reliable mobility and equipment |
| Mutant Hunter | Veteran | Wants mutant encounters and trophy hunting to dominate the risk/reward loop |
| Outcast | Veteran | Wants to scrounge for survival with very limited access to safe supplies |
| Scavenger | Veteran | Wants scarcity, carrying, maintenance and economy to drive play |
| Smuggler | Veteran | Wants risky cargo runs, artifacts and black-market rewards |
| Tactical | Veteran | Wants fast, lethal, skill-driven firefights |
| Wanderer | Stalker | Wants long-range exploration with stamina, carry and hazard survival |

## Medical rules

The hard profiles intentionally separate wound treatment, food and healing:

- **Hardcore**: Bandage Healing = `0`; Food Restores Health = `OFF`
- **Hardcore+**: Bandage Healing = `0`; Food Restores Health = `OFF`
- **Mutant Hunter**: Bandage Healing = `0`; Food Restores Health = `OFF`
- **Scavenger**: Bandage Healing = `10`; Food Restores Health = `OFF`

The food-healing toggle only controls the four confirmed ordinary-food healing effects
for Bread, Fresh Bread, Canned Food and Sausage. Weird Kettle alternative effects are
outside that control and remain untouched.

## Migration from older builds

On startup, `PresetDirectoryService` removes only these exact retired factory filenames
from the portable runtime `Presets/` directory:

- `Novice.json`
- `Stalker.json`
- `Veteran.json`
- `Master.json`

It then seeds the seven current factory presets when they are missing. Other JSON preset
files in the directory are left untouched.

## 2026-10-09 — Role-first factory preset rebuild

All seven shipped profiles were redesigned as **playstyles**, not difficulty labels. Every file retains the same 201 source-backed entries and its pre-existing reference difficulty (Immersive/Anomaly Runner: Stalker; Tactical/Scavenger/Mutant Hunter: Veteran; Hardcore/Hardcore+: Master). The 604 numeric updates across seven profiles target complementary gameplay systems rather than unrelated slider extremes; unchanged values still participate in the preserved preset model.

### Immersive — the inhabitant
**Mindset:** Stay in the Zone for the world rather than a combat challenge. Sensible combat pressure; understandable ailments; useful food, medicine, inventory and loot without excessive abundance.
**Complementary systems:** modest combat and mutant threat; near-standard movement and healing; readable economy, natural weapon wear and world-loot density. Medical quality and persistence matter more than an artificial HP wall.

### Tactical — the firefight specialist
**Mindset:** Win by reconnaissance, cover, accuracy and quick target acquisition; a good shot must count on both sides.
**Complementary systems:** lower enemy HP, higher player weapon and headshot lethality, stronger enemy shots/attack pressure, limited healing, reasonable ammunition and swift maneuvering. Avoid extreme bullet sponges, and preserve usable stamina for repositioning.

### Scavenger — the resource accountant
**Mindset:** Survive by stripping the Zone for parts and deciding what is worth carrying or repairing; combat is a cost, not a goal.
**Complementary systems:** aggressively reduced world loose loot, medical/ammunition stocks and weapon density; worn equipment, inventory weight, limited carrying capacity, punishing trade/repair economics and weaker healing. Hunting remains a possible side-income but never effortless.

### Mutant Hunter — the professional tracker
**Mindset:** Accept uniquely dangerous mutant fights in exchange for trophy value; prepare, move and spend on specialist gear.
**Complementary systems:** mutant-specific health and damage, recoverable stamina, headshot damage, wear from expeditions and substantially enhanced mutant-part values. Unlike Hardcore, ordinary human firefights and trader access must not overshadow the hunting loop. Bandages stop bleeding but do not heal HP; food healing stays disabled.

### Anomaly Runner — the mobile explorer
**Mindset:** Plan routes, carry detectors, cross dangerous anomaly fields and trade artifacts; flee unnecessary firefights.
**Complementary systems:** stronger radiation/anomaly hazards, greater movement agility, larger stamina pool and recovery, more valuable artifacts, higher carrying room and travel expenses. Radiation recovery and medicine remain viable so traversal remains skill-based rather than a death sentence.

### Hardcore — the disciplined survivor
**Mindset:** Long-term survival through avoidance, planned supplies and careful maintenance; every battle is risky and costly.
**Complementary systems:** dangerous NPC/mutant combat, scarce ammo/medicine/loot, aggressive degradation, reduced HP and healing, severe exposure and high costs. Bandage healing zero and food healing disabled, while actual bleeding-removal controls still work.

### Hardcore+ — the last-resort survivor
**Mindset:** An almost stripped Zone; the player must avoid confrontation and scrounge for necessities, with little room for error.
**Complementary systems:** even thinner resources and vendor stock, higher wear, intense exposure and attack pressure, fragile health/stamina, limited carrying and punishing trade. Bandage and food healing remain disabled while dedicated bleeding and medical treatments retain their native mechanics.

### Preset integrity
- Existing 201 source entries per profile; no invented parameter IDs, DLC boss mapping or separate in-game Custom difficulty.
- Keep vanilla Base marker semantics, UI source mappings, writer contracts, reference difficulty and source-specific bounds.
- Numeric files were checked for legal catalog slider ranges (7 × 201 entries, zero numerical range violations). **Local .NET build and game runtime behavior have not been tested.**
- These are authored thematic recommendations rather than claims of measured in-game balance. Further gameplay testing may justify fine tuning.


## Factory profile upgrade (2026-10-09)

When the executable starts, it replaces a previously shipped factory preset **only if its complete local file still exactly matches the prior official factory JSON**. User-edited factory files, even those still using a factory filename, are preserved; custom preset filenames are never overwritten. Missing factory files are seeded normally. This is an intentional one-generation upgrade for the seven role-first profiles, not a universal forced reset. To adopt the new official balancing if an existing factory file was customized, save the customization under a separate name and remove the old factory-named file before restarting the application.


## Three additional factory profiles — 2026-10-09

Each of these new factory profiles contains all 201 existing supported preset values, derived and curated across combat, health, survival, mutant encounters, stamina, world loot, equipment, economy, hazards and recovery. All are authored gameplay styles, not new game difficulties. Existing baseline/reference semantics and writer contract are unchanged.

| Profile | Base difficulty | Player mindset | Complementary tuning |
|---|---|---|---|
| **Mercenary** | Veteran | Contract-driven professional who wins decisive firefights, maintains quality weapons and seeks profitable assignments | Lethal human combat, mobility and reliable stamina, respectable trader availability, boosted mission pay, sensible medical backup, moderate wear and world scarcity |
| **Wanderer** | Stalker | Long-distance nomad and explorer who avoids unnecessary fights but handles dangerous terrain | High mobility, vault reach and stamina, generous carry room, realistic hunger/fatigue, stronger hazards but viable recovery, valuable artifacts, balanced medical and moderate supplies |
| **Outcast** | Veteran | Isolated survivor without economic safety nets; salvages everything and must avoid protracted fights | Heavy supply depletion, weak medicine, no food healing, difficult trader prices, increased equipment deterioration, dangerous wildlife and exposure, restrictive carrying and stamina |

Each new profile was validated against its catalog slider ranges: **201 serialized values per preset; no numeric out-of-range entries**. These are authored balance targets, not measured in-game test results. Factory files are seeded only when absent, preserving existing personalized profiles.


### 2026-10-09 — Factory preset embedding fix

The three new JSON preset files (Mercenary, Wanderer, Outcast) must be explicitly included in `TrueCustomDifficulty.csproj` as embedded resources with `TrueCustomDifficulty.Presets.<Name>.json` logical names. `PresetDirectoryService.WriteEmbedded()` uses those exact manifest names during first-run factory seeding; a file present in the repository but absent from the embedded manifest causes startup failure. All ten factory JSON resources now have explicit entries. No gameplay parameters or UI layout changed.


## 2026-10-09 — Field Medic, Smuggler, and alphabetical sorting

All twelve factory profiles are listed alphabetically in the picker: Anomaly Runner, Field Medic, Hardcore, Hardcore+, Immersive, Mercenary, Mutant Hunter, Outcast, Scavenger, Smuggler, Tactical, Wanderer. Custom preset files remain untouched and still sort by name after the factory choices.

| New profile | Reference difficulty | Mindset | Complementary systems |
|---|---|---|---|
| **Field Medic** | Stalker | The responder who survives by controlling wounds and medical supplies, not overpowering foes | Strong HP healing, bandages, medkits, bleeding treatment, medical merchant stocks, affordable care, stable stamina, moderate combat and predictable hazards |
| **Smuggler** | Veteran | The opportunist who navigates risky routes to profit from rare finds, managing losses instead of winning every fight | Higher artifact and mutant-trophy value, speed and cargo capacity, sparse field resources and traders, steep supply and repair costs, faster equipment wear, dangerous but avoidable enemies |

Both are complete 201-entry presets using existing source IDs, source ranges and Base difficulty semantics. No new UI controls or game difficulty were introduced. Both JSONs are explicitly embedded into the executable to avoid missing-resource startup failures.


## 2026-10-09 — A-Life Director group-size tuning in all factory profiles

All twelve factory profiles now include **18 new values**: four rank-specific human group-size multipliers and fourteen species-specific mutant Director group-size multipliers, total **219 entries per preset**. The hidden `alife.mutantSpawn.generic` entry is intentionally omitted. All values use existing `0.25×–3×` catalog bounds and vanilla `1×` reference; they alter *Director group size*, **not** overall world/lair population, encounter frequency, or respawn speed. Keep species and rank independent.

| Profile | Human group tendency | Mutant group tendency |
|---|---|---|
| Anomaly Runner | Smaller, to favor traversal | Fewer elite/psy groups; lower environmental combat interruption |
| Field Medic | Near vanilla | Mostly vanilla-sized groups with some fewer high-threat encounters |
| Hardcore | Larger, escalating by rank | Larger packs and more elite groups |
| Hardcore+ | Very large and rank-escalating | Extremely challenging packs |
| Immersive | Vanilla | Slightly varied natural packs |
| Mercenary | Increasingly large high-rank human squads | Near vanilla |
| Mutant Hunter | Slightly smaller human groups | Larger common packs and dangerous elite groups |
| Outcast | Larger groups by rank | Dense common packs; more serious threats |
| Scavenger | Smaller human groups | More common packs, smaller elite groups |
| Smuggler | Near-standard human groups | More common packs with fewer elite groups |
| Tactical | More dangerous higher-rank human groups | Near-standard mutants |
| Wanderer | Slightly smaller human groups | More common wildlife packs, fewer elites |

Existing unmodified twelve-profile JSONs are automatically replaced when the app starts by matching their exact previous Git blob hash. Locally edited factory and user-named preset files are not overwritten. The UI remains unchanged; modified value numbers are still highlighted yellow only when different from vanilla. Gameplay runtime was not tested.

## 2026-11-18 — LAIR POPULATION

The 12 profiles now store `alife.lairPopulation.human` / `.mutant` (417 catalog controls each). The matrix is in `ALIFE-LAIR-POPULATION.md` and `tools/factory_preset_lair_population_2026_11_18.json`; stock files upgrade only when byte-identical (`PreLairPopulationFactoryBlobs`).

## 2026-11-18 — ZONE WEATHER + TRADER RESTOCK

The 12 profiles now store the four new controls (422 catalog controls each). The matrix is in `ZONE-WEATHER-AND-RESTOCK.md` and `tools/factory_preset_zone_weather_2026_11_18.json`; stock files upgrade only when byte-identical (`PreZoneWeatherFactoryBlobs`).

## 2026-11-18 — SUMMON RATE; Pseudodog ABILITIES is its howl

* Every profile stores SUMMON RATE. Non-vanilla: Hardcore+ 1.5×, Mutant Hunter 1.5×, Outcast 1.25×, Wanderer 1.25×; the rest 1×.
* Pseudodog ABILITIES now scales its howl only (phantoms are SUMMON RATE). Stock Hardcore stores 1× (was 2×, the old Master reference of a knob that did nothing).
* Untouched older stock copies upgrade by blob hash (`PreSummonRateFactoryBlobs`, `PrePseudodogAbilityFactoryBlobs`); edited copies are never overwritten.

## 2026-11-18 — Mutant move knobs replace SUMMON RATE

* Every profile stores the 68 move knobs and Deer ABILITIES (491 controls). All are 1× except the summon knobs, which keep SUMMON RATE's role values: Hardcore+ and Mutant Hunter 1.5×, Outcast and Wanderer 1.25× on Pseudodog PHANTOMS and RETREAT, Bayun CLONES, Deer DOGS and Controller DOGS.
* Untouched older stock copies upgrade by blob hash (`PreMoveKnobFactoryBlobs`); edited copies keep their values, and a stored SUMMON RATE is ignored.

## 2026-11-18 — Attack-only mutant knobs

* 474 controls per profile: roar, growl, howl, dodge, teleport and shield knobs and the Blind Dog, Chimera and Snork ABILITIES sliders are gone (their moves stay vanilla).
* Stock Hardcore stores Bayun and Bloodsucker ABILITIES at 1× (their Master vanilla is now 1×). Untouched older stock copies upgrade by blob hash (`PreAttackOnlyFactoryBlobs`).

## 2026-11-18 — ABILITIES sliders folded into their knobs

* 466 controls per profile. Each ability knob took over its old slider's value (knob × slider; at the slider's vanilla the Master 2× where the game halves that ability). Hardcore keeps vanilla Master pacing; Hardcore+, Mutant Hunter, Outcast and Wanderer keep their faster abilities (Hardcore+ clone, phantom and dog summons capped at 3×).
* Untouched older stock copies upgrade by blob hash (`PreAbilityKnobFactoryBlobs`).

## 2026-11-18 — ATTACKS sliders folded into their knobs

* 452 controls per profile. Each attack knob took over its family's ATTACKS slider (knob × slider; at the slider's vanilla the Master 2× where the game halves that attack). Untouched older stock copies upgrade by blob hash (`PreAttackKnobFactoryBlobs`).

## 2026-11-18 — Controller DOGS knob removed

* 451 controls per profile; the Controller dog summon (one-off `Controller_Deer` variant) stays vanilla. Untouched older stock copies upgrade by blob hash (`PreControllerDogsFactoryBlobs`).

## Save / load contract (2026-11-18)

SAVE writes every editor control, not only the changed ones. LOAD resets every control to the preset's base difficulty, then applies the saved values; older presets that lack a newer control get vanilla for it. `AllControlsPatchContract` checks a full SAVE → LOAD round trip at three difficulties. It also checks that all 12 factory presets load with no silent value change.
