# Preset JSON format

True Custom Difficulty presets are ordinary JSON files. They store **tool UI state**, not generated STALKER 2 CFG files. The internal JSON property `referenceDifficulty` is retained for backward compatibility; in the UI this is called **Base Preset**.

## UI-only duration range override (2026-10-10)

The optional preset-root boolean `"drinkDurationOverride": true/false` stores
whether **all eight independent drink-effect durations** can use the common
270-second maximum. It is **NOT** a `values` entry and it has no fake CFG writer
target. Missing metadata in older presets resolves to `false` (original
per-drink maxima). The loader restores the mode **before** numeric values,
allowing formerly constrained Water/Beer/Vodka values above their old cap to
round-trip. Changing the checkbox alone never creates a gameplay patch; it
only controls slider ranges and the short-effect parent limits. Turning it OFF
clamps values that exceed the restored native limits; to preserve a high-duration
preset, save it with Override ON before disabling.

## What is saved

Every preset stores the complete current state:

- selected vanilla reference difficulty;
- every slider value;
- both values of every range control;
- every toggle state;
- every enum selection;
- tool version and target game version metadata.

All controls are saved, including controls that are currently at their vanilla value. This makes loading deterministic and avoids depending on whatever state happened to be open before the preset was loaded.

## Example

```json
{
  "profileVersion": 1,
  "name": "hardcore",
  "toolVersion": "0.1.55",
  "gameVersion": "2.0.5",
  "referenceDifficulty": "Stalker",
  "drinkDurationOverride": false,
  "savedAtUtc": "2026-10-02T00:00:00+00:00",
  "values": {
    "combat.npcHealth": {
      "kind": "slider",
      "number": 1.25
    },
    "advanced.starvationMechanics": {
      "kind": "toggle",
      "boolean": true
    }
  }
}
```

The real file contains one entry for every current catalog parameter. Null fields are omitted.

## Loading behavior

1. The saved `referenceDifficulty` becomes the active vanilla baseline.
2. Every current control is reset to that baseline.
3. Matching preset IDs are applied.
4. Unknown IDs from older/newer catalogs are ignored.
5. Controls missing from an older preset stay at the selected vanilla baseline.
6. Numeric values are normalized/clamped by the current control rules.
7. A saved value whose `kind` no longer matches the current control type is ignored.
8. Invalid JSON, unsupported preset-format versions and unsupported reference difficulties are reported as load errors instead of crashing the application.

Preset format version `1` is the current format. Older pre-versioned presets remain loadable because a missing `profileVersion` defaults to `1`.
## Sleep HP range compatibility (2026-10-12)

`survival.sleepHealthRestore` now defaults to 100 HP with a 0–200 HP editor range (0x–2x relative to the *tool* baseline), including zero sleep healing. Presets store actual HP; old saved values above 200 are clamped to 200 during import and missing values start at 100. Original vanilla 300 HP is retained only as source provenance and writer comparison. No preset-format revision.

## Compatibility migrations

Preset files keep their original `toolVersion` so player-facing semantic migrations can preserve the same gameplay result when a control stops exposing an engine-internal representation.

- **pre-v0.1.96**: Human Attack Frequency raw wait multiplier are converted to intuitive frequency multipliers.
- **pre-v0.2.45**: negative bleeding/radiation treatment deltas are converted to positive removal magnitudes; the writer emits the negative CFG delta.
- **pre-v0.2.46**: negative `stamina.aimRegenCoef` values are converted to a positive **Aiming Stamina Recovery Penalty** magnitude; the writer emits the signed negative Aim-state coefficient.
- **pre-v0.2.47**: negative medium/severe hunger stamina-regeneration modifiers are converted to positive **Hunger Stamina Recovery Penalty** magnitudes; the writer emits the original signed negative CFG modifiers.
- **pre-v0.2.50**: negative Master low-health/starvation health-recovery and severe-fatigue stamina/carry deltas are converted to positive penalty/loss magnitudes; the writer preserves the original negative CFG values via `negateSet`.

These migrations change the stored UI representation, not the intended gameplay output. Newly saved presets use the current application version and already store the current semantics.

