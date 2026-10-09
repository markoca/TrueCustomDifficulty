# Deep CFG / BPatch Audit — Phase 12: Environment / Anomalies (2026-10-09)

## Current production wiring

The current `environment.*` catalog has **17** live source-backed parameters, all
loaded into the actual application ViewModel. **Seven** are presented as normal
visual editors/anchors. Ten hidden members are the individually editable
backings of the Anomaly radar; they remain real writer inputs, not retired controls.

All **42 direct CFG paths** are base-game scoped, with no environment-scoped
DLC1 edit points in the current writer baseline:

| File | Original fields | Operations |
| --- | ---: | --- |
| `GameData/DifficultyPrototypes.cfg` | 4 | `set`, `EnvironmentDifficulty.Anomaly_Damage` for the four difficulty SIDs |
| `GameData/EffectPrototypes.cfg` | 38 | paired `ValueMin`/`ValueMax` across 19 concrete effect prototypes: `set`, `scale`, `setDeltaFromRetainedFraction` |

Important semantics: global `Anomaly_Damage` is difficulty-scoped; individual
effect values are authored independently. Flycatcher **movement** and **turn**
controls expose retained movement fractions, not the native negative effect
deltas: `CFG = current retained fraction - 1.0`. The original movement
value is `-50%` and yaw/pitch are both `-0.7`. The writer must preserve
the original percent formatting and must write both yaw/pitch fields.
Razor health and bleeding have independent low/high damage tiers.
`PsyAnomalyIncreaseRegen = -1.7` must remain negative under scaling.

## Independent original source archive gate (B9) — PASS

Compared the current 42-path source manifest to the two user-supplied
original decoded ZIP archives with exact prototype identity, nested path,
field name and raw source value verification:

| Result | Count |
| --- | ---: |
| Literal `sourceRaw` exact matches | **42** |
| Inherited or numerically reformatted | 0 |
| Missing prototypes/fields/value mismatches | **0** |

Source archive hashes:
- Base ZIP `7610b16218310c7345796ff26b1554c73e3d2ca775f6a50108a7406cb7f33da1`.
- DLC1 ZIP `3c5cb53f1769619b510ae3780142d92639c083a4a755a8717a267710c3c849e6`.

The two referenced base-game CFG files are physically present in the ZIP;
their source-file hashes and per-parameter counts are retained in
[evidence/ENVIRONMENT-ORIGINAL-CFG-2026-10-09.json](evidence/ENVIRONMENT-ORIGINAL-CFG-2026-10-09.json).
This proves only the uploaded decoded source dump / manifest contract.
There are no DLC1-scoped Environment writer points to verify, but this does
NOT establish that DLC1 has no additional anomaly gameplay systems.

Repeat on future source dumps, always with matching archive and manifest
versions:

```bash
python tools/audit_cfg_source_values.py \
  --base-zip "/path/to/STALKER2-GameData-CFG(2)(2).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(2)(2).zip" \
  --parameter-prefix environment. \
  --out audit/environment-source-values.json --strict
```

This original source check is registered in `tools/pre_release.py source`.

## Production BPatch writer contract (C6)

`tests/EnvironmentPatchContract` instantiates the real application
`MainViewModel` and executes `CfgPatchWriterService.Write`. The runner
parses the emitted CFG, checking every leaf, value, nested `{bpatch}`
tag, output file path, count and unexpected collateral output.

**90 independent scenarios** are registered:
- 4 vanilla no-output baselines (Novice, Stalker, Veteran, Master);
- 68 standalone tests (17 live backing parameters × 4 difficulties);
- 12 min/max boundary checks on global damage, Flycatcher move/turn,
  negative PSY, Diamond bleeding and Razor health;
- 6 two-control scenarios that exercise global/individual layering, shared
  EffectPrototypes patch output and source-only aggregate backing members.

Run on Windows / .NET 10:

```powershell
dotnet run --project tests/EnvironmentPatchContract/EnvironmentPatchContract.csproj -c Release -- --out audit/environment-patch-fixtures
```

CI job `environment-writer-windows` retains actual CFG output evidence.
**Registration alone is not a PASS.** Record the same-SHA CI result
separately after completion.

## Runtime / user acceptance — NOT RUN

Confirm actual Unreal BPatch loading, preset baseline/difficulty SID, global
and individual damage stacking, all individual radar source controls, signed
Flycatcher move/yaw/pitch effects, Razor low/high damage and bleeding,
negative PSY regen effect, and that protection/equipment/corrosion stays
independent. Mod PAK/UTOC/UCAS load order is another acceptance boundary.
No UI, values, gameplay balancing, design-lock or project version changes
were requested or made in this phase.
