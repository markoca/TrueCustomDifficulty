# Factory Presets

True Custom Difficulty ships with seven custom factory presets. They replace the old
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
| Hardcore | Master | Wants an 80%-scarcity, dirty survival profile where basic resources are never guaranteed |
| Hardcore+ | Master | Wants a nearly stripped Zone: extreme scarcity, fragile gear and almost no economic safety net |

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
