# Phase 8 — Health and Stamina CFG / BPatch audit (2026-10-09)

## Scope and original sources

Verified against the **actual user-supplied matching decoded archives**
`STALKER2-GameData-CFG(2).zip` and `STALKER2-DLC1-CFG(2).zip`.
This is a provenance check against genuine original CFG text, **not**
a claim about in-game runtime behavior.

| Domain | Direct points | Current backing parameters | Original source outcome |
| --- | ---: | ---: | --- |
| Health | 43 | 20 | 43 physically present and exact |
| Stamina | 26 | 23 | 25 physically present and exact, **1 source-inherited exact** |
| **Total** | **69** | **43** | **68 direct exact + 1 inherited exact** |

The inherited route is `stamina.vaultMaxHeight`:
`GameData/ObjPrototypes.cfg` defines
`Player : struct.begin {refkey=[0]}`; its local `VaultingParams`
section is absent. The original `[0].VaultingParams` contains
`MaxObstacleHeight = 130`. The catalog/writer source reference is
therefore correct **via original inheritance**. It remains unknown
whether a `Player.VaultingParams : struct.begin {bpatch}` targeting
this missing-but-inherited subsection is applied correctly by Unreal.

Other source families are directly present: Health `Player.VitalParams`,
`HealthMechanics`, `EffectPrototypes` healing/bleeding/radiation;
Stamina `Player.StaminaPerAction`, `CoreVariables.DefaultConfig` indexed
stamina states, `ObjWeightParamsPrototypes` and water effect fields.
Health includes both source-backed `MaxHP` edits and all eight
`foodHealing` range values. The original raw percent, float-suffix
and negative values were compared *as written*, not guessed from
UI labels.

The repeatable source verifier is:

```bash
python tools/audit_health_stamina_source.py \
  --base-zip "/path/to/STALKER2-GameData-CFG(2).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(2).zip" \
  --out audit/health-stamina-original-source.json
```

The standard `tools/pre_release.py source` now includes this scope.

## Actual app-to-writer test coverage

`tests/HealthStaminaPatchContract` executes the **production**
`MainViewModel`, each real bound `ParameterViewModel` and the
`CfgPatchWriterService`. It changes every source-backed Health/Stamina
parameter, including the internal editors hidden under their native
grouped UI panels, and checks the generated CFG text.

- **38 live Health/Stamina parameters** (20 Health + 18 Stamina), including
  **4 Master-dependent Health modifiers** tested on Master where
  their `advanced` parent toggles are already enabled.
- `health.regenerationScalar` is separately checked on
  Novice/Stalker/Veteran/Master, for **41 executable scenarios**.
- Strict path/section assertion for every generated leaf: prototype
  SID, nested `{bpatch}` flags, exact numeric transformation (set,
  negateSet, toggleSourceOrZero), destination file, edit count and
  no unrelated extra fields, sections or CFG files.
- Health and Stamina original sources cover **69 concrete leaf fields**,
  and the imported data continues to be independently verified
  against decoded vanilla ZIPs.

The Windows CI job `health-stamina-writer-windows` saves exact generated
patches to artifact `health-stamina-patch-contract`.

**Important runtime boundary:** these are actual .NET writer tests,
not the game. Verify the inherited Player vaulting section, effective
Stamina costs and recovery, health regeneration, treatment and food
healing in S.T.A.L.K.E.R. 2 before claiming gameplay confirmation.
The three zero-outcome/multiplier behaviors and parent-toggle
gating remain included in emitted-patch checks, but effects in
game require separate controlled acceptance.

## Release and local commands

```bash
cd /home/marko/s2tools/TrueCustomDifficulty/
git pull

# Existing three static guards.
python tools/pre_release.py static

# Original Health + Stamina CFG archive comparison.
python tools/audit_health_stamina_source.py \
  --base-zip "/path/to/STALKER2-GameData-CFG(2).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(2).zip"

# Full original-source pre-release inventory.
python tools/pre_release.py source \
  --base-zip "/path/to/STALKER2-GameData-CFG(2).zip" \
  --dlc1-zip "/path/to/STALKER2-DLC1-CFG(2).zip"
```

On Windows / .NET 10:

```powershell
python tools/pre_release.py writer
```

On Linux inspect the **matching GitHub Actions SHA** for both Windows
writer jobs; `all` returns `INCOMPLETE` on Linux, never a synthetic PASS.
No v1.0 release, UI changes or gameplay balance changes are included.

## Runtime acceptance evidence (manual)

| Scenario | Result |
| --- | --- |
| Inherited `Player.VaultingParams.MaxObstacleHeight` applies correctly | NOT RUN |
| Health MaxHP and FullHP effect condition agree after real mod load | NOT RUN |
| Healing, bleeding treatment and radiation treatment actually respond | NOT RUN |
| Native Stamina Action Costs and indexed regeneration coefs work | NOT RUN |
| Water drain and Food Healing switch affect intended gameplay only | NOT RUN |
| Master-dependent modifier behavior with parent enabled / disabled | NOT RUN |

The first Windows harness execution caught a **test path-normalization bug**: root-level effect values were represented with a duplicated `/` delimiter in the expected string, despite their correct generated CFG paths. The test was corrected to omit the empty intermediate path; production writer and its CFG values are unchanged.

## Phase 8 regression: Master parent false-positive list replacement

The first real Windows writer execution reproduced a genuine production
issue: changing one subordinate Master medical effect (for example
`health.masterArmyMedkitModifier`) also made the grouped
`advanced.masterEffectModifierMechanics` checkbox appear modified,
although its actual ON/OFF value had not changed. The original
`WriteAdditionalMechanics` looked only for membership in the
aggregate `modifiedById` list and consequently rewrote the entire
`AdditionalMechanicsEffects` section (seven total edits / two
parameter IDs instead of the expected one nested effect multiplier).

The production writer now checks each **parent toggle's actual
`IsValueModifiedFromReference()`** before regenerating the list.
A child-only edit therefore writes its own BPatch field and does not
recreate an unrelated master mechanics list; genuinely changed parent
toggles still follow the original full-list replacement path.

This targeted correction leaves UI coloring, enabled-state behavior,
preset values, and all gameplay default numbers unchanged. Generated
source structure remains distinct from verified gameplay behavior.

## Phase 8: catalog vs live application graph

The first extended Windows execution exposed that the **43 catalog
parameters are not all active app controls**. The 5 Stamina
internal-speed-state source definitions below are intentionally
**excluded from MainViewModel.AllParameters**:

- `stamina.sprintUnderRunSpeedRegenCoef`
- `stamina.joggingRegenCoef`
- `stamina.slowRunRegenCoef`
- `stamina.sprintRegenCoef`
- `stamina.runRegenCoef`

They remain legitimate, verified original CFG paths in the manifest
but are **not writable through the current app**. The harness now pins
this exact intentional exclusion to fail on unexpected catalog/UI
regressions, and executes the **38 live loaded Health/Stamina
parameters**, plus three extra Health regeneration difficulty baselines
(**41 live writer scenarios**). No new UI controls were added.
The source provenance check still covers all 69 direct source fields:
its scope is original source correctness, not a claim of active UI
control coverage.

This distinction is essential to release reports: never label the
five internal-only Stamina values as functioning app-to-BPatch routes.
