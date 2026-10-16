# Preset JSON format

## CURRENT 2026-10-16 — Stashes graphs and backward-compatible zero-item caps

The two medical MAX CAP sources accept an additional -1 value that represents 0 ITEMS in the graphical UI. Existing v1 profile 0 remains AUTO; the 12 factory JSONs are unchanged. Save/Load serializes the same source IDs and the new -1 when explicitly chosen. The writer suppresses that Medkit/ArmyMedkit entry via native count and chance/weight zeroing. Stash Rewards is a six-row native bar graph and Medical Quantity is a four-cell dot graph; no preset format bump or parameter rename.


## 2026-10-16 — Stashes navigation does not change preset format

The 26 source-backed stash parameters remain in the unchanged v1 JSON values dictionary with their historical loot.* IDs. Only catalog tab membership moves to Stashes beside Loot. Save/Load still uses the same all-parameter serialization and 12 factory presets; existing custom profiles need no migration. The documented UI redesign is PROPOSED ONLY and makes no schema change.

### 2026-10-15 — Canonical A-Life factory values (CURRENT)

All 12 embedded factory v1 JSON presets preserve their **248** native source entries; only **39** A-Life multipliers are re-expressed as canonical integer-effective editor stops without changing any of the **216** real rank/species group-size limits. Their 1x neutral source, reference difficulties, UI-only override flags and unrelated values remain intact. Earlier user presets load through the A-Life integer-effective value normalization without changing the JSON schema. Only byte-identical old official factory files are automatically upgraded via `PreEffectiveALifeFactoryBlobs`; edited factory-name files and custom presets are protected. See `tools/factory_preset_alife_effective_mapping.json`.


### 2026-10-15 — Full 248-source factory audit (CURRENT)

Factory profiles stay **v1** with exactly 248 `values` entries and the existing `drinkDurationOverride`/`medicalOverride` root UI-only flags. Each profile-setting value and its source-specific reason is recorded in `tools/factory_preset_full_source_audit.json` and summarized in `docs/FACTORY-PROFILE-FULL-AUDIT-2026-10-15.md`. Source-native UI step normalization (including 5-percentage-point chance steps and 5-second override duration steps) is pre-applied to stock JSONs, so load no longer silently changes those values. Stock migration uses exact `PreFullDeepAuditFactoryBlobs` hashes; user-created/edited profiles are preserved. No new gameplay CFG parameter or serializer version is introduced.

### 2026-10-15 — Consumable cadence, override and role differentiation (HISTORICAL; SUPERSEDED)

All twelve embedded factory profiles now serialize `"drinkDurationOverride": true` **at preset root**, never in `values`, and the Field Medic alone serializes `"medicalOverride": true` for stronger bleeding treatments. These are *UI-only editor-ceiling flags* restored before numerical values; neither creates a new game CFG option. Profiles still contain **248 native entries**, original `referenceDifficulty` and v1 format. A missing override in a custom or old preset remains false by default. Exact Git-blob migration of the previous stock JSONs occurs through `PreConsumableRebalanceFactoryBlobs` and never overwrites user edits. Consult `tools/factory_preset_consumable_balance.json` for individual role values and `docs/FACTORY-PRESETS.md` for the cadence rationale.

### 2026-10-15 — Role-first deep rebalance (HISTORICAL; SUPERSEDED)

The current **12 stock profiles** are still **format v1** snapshots with all **248 source-backed IDs**. The 2026-10-15 role-first matrix rebalances 78 source IDs and 702 stored values, with no schema revision or new CFG target. The reference difficulty and prior `savedAtUtc` metadata fields are preserved for compatibility and do not indicate the rebalance date. See `tools/factory_preset_role_rebalance.json` and `docs/FACTORY-PROFILE-DEEP-AUDIT-2026-10-15.md`.

The startup `PresetDirectoryService` also matches **twelve exact Git-blob identities** of the immediately previous 248-key factory release (`PreRoleRebalanceFactoryBlobs`). Only untouched installed factory files are replaced; named user-edited presets and all custom presets are protected. Older migratable factory identities continue to work. Users wishing to adopt the new balance while retaining personalized changes should save their custom preset under another filename, then reseed a pristine factory file. In-game experience and compilation are not verified by the static audit.

## 2026-10-14 — Abundance-aware factory preset revision

Factory preset format **remains version 1** and each stock file retains all **247** native parameter values and its original `referenceDifficulty`. The most recent rebalance changes source-backed numbers and one existing NPC bonus choice, not CFG semantics or serialization fields. The 46-control authored patch and rationale are in `tools/factory_preset_scarcity_balance.json`; regression tests compare it to each embedded JSON.

The installer adds the exact Git blob SHA identities of the previous **247-key stock factories** to its migration whitelist. Only a *byte-for-byte identical* official prior factory file is replaced. Edits under stock names and unrelated user-created profiles are preserved. Older user presets missing later IDs remain loadable using the same baseline/default fallback as before.

## 2026-10-14 — Complete factory-state snapshots

All twelve embedded factory profiles now include every one of the **247 current catalog parameters**, including previously absent 34 source controls. The existing three Water/Energy short-effect duration entries may be non-vanilla, subject to the source-native main-duration ceiling. The factory matrix is `tools/factory_preset_expansion.json`, checked against every embedded JSON.

Unmodified **213-key** factory files from the preceding release are automatically upgraded by exact Git-blob hash comparison at startup. A factory-named file with any user edit is **not overwritten**. Loading an older custom preset with missing IDs is still supported: missing fields resolve to their respective baseline/editor defaults; saving records the current complete state. The optional `drinkDurationOverride` remains a root UI-only field, not an invented gameplay parameter, and the format remains version `1`.

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

