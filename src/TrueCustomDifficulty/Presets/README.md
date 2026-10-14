# True Custom Difficulty — Factory Presets

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

