# Factory Presets

True Custom Difficulty ships with ten custom factory presets. They replace the old
factory preset files named after the four vanilla game difficulties.

The vanilla difficulties still remain the **reference/baseline model** used internally
by the tool. A factory preset therefore has its own `referenceDifficulty`, but the
preset displayed to the user is one of the seven custom profiles below.

| Factory preset | Reference difficulty | Intended player |
|---|---|---|
| Immersive | Stalker | Wants the Zone to feel believable and systemic without becoming punitive |
| Tactical | Veteran | Wants fast, lethal, skill-driven firefights |
| Scavenger | Veteran | Wants scarcity, carrying, maintenance and economy to drive play |
| Mutant Hunter | Veteran | Wants mutant encounters and trophy hunting to dominate the risk/reward loop |
| Anomaly Runner | Stalker | Wants traversal, anomalies, radiation and artifact exploration to dominate |
| Hardcore | Master | A resource-aware survivalist: avoid fights, preserve supplies, and plan every expedition |
| Hardcore+ | Master | A last-resort survivor in an almost stripped Zone, with little margin for mistakes |

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
**Complementary systems:** mutant-specific HP/cooldowns/damage, recoverable stamina, headshot damage, wear from expeditions and substantially enhanced mutant-part values. Unlike Hardcore, ordinary human firefights and trader access must not overshadow the hunting loop. Bandages stop bleeding but do not heal HP; food healing stays disabled.

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
