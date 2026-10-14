# Factory Presets

## 2026-10-14 — Abundance correction: all twelve profiles rebalanced (CURRENT)

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
