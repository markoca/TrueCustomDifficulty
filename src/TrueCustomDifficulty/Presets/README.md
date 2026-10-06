# True Custom Difficulty — Factory Presets

The application ships with seven custom factory presets:

1. Immersive
2. Tactical
3. Scavenger
4. Mutant Hunter
5. Anomaly Runner
6. Hardcore
7. Hardcore+

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
