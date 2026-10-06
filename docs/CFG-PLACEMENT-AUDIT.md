# CFG Placement Audit — v0.2.53

## Purpose

This release verifies only the thing that matters for static writer correctness: **each TCD parameter writes to the intended CFG location**.

There is deliberately **no EXE/Wine/headless verification workflow** in this release. The audit is entirely file-level:

`parameters.json target -> cfg-writer-baseline concrete route -> CfgPatchWriterService route family`

## Result

- Catalog parameters: **178 / 178 routed**
- Visible controls: **167**
- Hidden/audit-only parameters: **11**
- Ordinary direct-editPoint parameters: **153**
- Special writer-route parameters: **25**
- Concrete direct CFG edit points: **1370**
- Direct CFG files touched: **14**
- Cross-parameter direct field collisions: **0**
- Unsupported writer routes: **0**
- Result: **PASS**

For every one of the 1370 direct edit points, the audit verifies that the parameter declaration agrees with the writer baseline on **scope, CFG file, operation and field key**. The concrete baseline then fixes the exact prototype, node path and source value.

## Route split

| Writer route | Parameters |
|---|---:|
| `cheapStashAmmoCloneRedirect` | 1 |
| `cheapStashGrenadeCloneRedirect` | 1 |
| `destructibleAmmoCloneRedirect` | 1 |
| `destructibleGrenadeCloneRedirect` | 1 |
| `directEditPoints` | 153 |
| `expensiveStashAmmoCloneRedirect` | 1 |
| `listMembership` | 5 |
| `mediumStashCloneRedirect` | 1 |
| `mutantCooldownArray` | 8 |
| `npcMedicalCloneRedirect` | 1 |
| `psyArray` | 4 |
| `setList` | 1 |

## Special routes

The 25 non-direct parameters are not inferred from labels. They are explicitly tied to their dedicated production writer routes:

- 8 mutant cooldown array controls -> `GameData/DifficultyPrototypes.cfg`
- 4 PSY override controls -> `GameData/DifficultyPrototypes.cfg`
- 5 Master `AdditionalMechanicsEffects` list members -> `GameData/DifficultyPrototypes.cfg`
- 1 corpse smart-loot set-list control -> `GameData/DifficultyPrototypes.cfg`
- 7 generator clone/redirect controls -> their locked `ItemGeneratorPrototypes.cfg` clone roots plus the production redirect manifests

Clone/redirect routes may also patch consumer CFG files listed by their locked manifests. Those redirect assignments are intentional routing scaffolding; they do not create an additional gameplay control surface.

## Canonical evidence

`docs/cfg-placement/v0.2.53/` contains:

- `parameter-summary.csv` — one row for every catalog parameter
- `direct-targets.csv` — all 1370 concrete ordinary writer targets with file/prototype/node/key/operator
- `special-routes.csv` — all 25 dedicated special routes and their primary CFG/manifest
- `cfg-file-summary.csv` — direct edit-point totals by CFG file
- `summary.json` — machine-readable release gate

The canonical checker is `tools/audit_cfg_placement.py`.

## What this proves

This proves that the project has a deterministic, reviewable answer to **"which CFG field does this control write?"** for every parameter in the catalog. It does not claim that Unreal runtime behavior has been proven; controls still marked `runtime_test_required` keep that status until an in-game test is performed.
