# True Custom Difficulty — Factory Presets

The application ships with twelve custom factory presets:

1. Immersive
2. Tactical
3. Scavenger
4. Mutant Hunter
5. Anomaly Runner
6. Hardcore
7. Hardcore+
8. Mercenary
9. Wanderer
10. Outcast

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

Rebuilt playstyle descriptions and verified 201-entry catalogue coverage are maintained in `docs/FACTORY-PRESETS.md`. Factory presets are curated starting points; loading one retains the usual Base difficulty and CFG writer behavior.


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
