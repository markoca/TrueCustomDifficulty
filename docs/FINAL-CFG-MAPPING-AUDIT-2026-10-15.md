# Final CFG mapping and BPatch writer audit — 2026-10-15

## Final assessment and evidence levels

The production source catalogue is **248 unique, source-backed parameter IDs**, of which **235 are player-editable dimensions** (148 directly exposed anchors plus 87 aggregate backings) and **13 are deliberately audit-only/hidden**. The editor-only `drinkDurationOverride` and `medicalOverride` flags are **not** fake CFG settings.

| Audit layer | Coverage | Meaning |
|---|---:|---|
| Catalogue → writer route | **248 / 248** | Every ID has an explicit baseline writer route; no deferred entries |
| Direct source writer | **213 IDs / 1,230 leaf edit points** | Exact file, physical root, nested path, key, operation, original `sourceRaw`, difficulty SID and DLC scope are present |
| Special writer | **35 IDs** | An explicitly named production method and source/manifests, not an assumed direct CFG scalar |
| Player-editable source IDs | **235** | 148 visible + 87 visual-aggregate backings; 13 additional catalogue IDs are deliberately hidden |
| Source evidence status | **197 confirmed / 51 runtime-test-required** | These are source-provenance classifications, not in-game pass/fail results |
| Duplicate ownership of direct leaves | **0** | No two independent parameters claim an identical concrete `(scope,file,prototype,nodePath,key)` |
| Unmapped catalogue IDs | **0** | No control is declared without a writer route |
| Factory source-inert integer alterations after correction | **0** | Every modified direct A-Life value now changes at least one real integer CFG source |
| .NET production writer execution | **Not run in this authoring session** | See the new all-controls contract and existing Windows contract suite |
| STALKER 2 runtime/Unreal BPatch merge | **Not run** | Real behavior and game build compatibility require a separate game acceptance pass |

**Conclusion:** The source and production-writer routing is complete. The source audit does **not** prove that the game recognizes all 248 patches or that every UI step has a meaningful impact. Source evidence and real in-game behavior must remain separate claims.

## Source-to-writer topology

```text
parameter catalogue (248 IDs; 235 editable)
    ├── 213 direct parameter IDs, 1,230 CFG edit points
    │     └── CfgPatchWriterService.TransformValue()
    │           → sourceRaw-preserving concrete transform
    │           → PatchDocument.AddAssignment(file/root/nodePath/key)
    │           → GameLite BPatch .cfg / cfg_patch
    └── 35 non-scalar / list / generator / actor / PSY source IDs
          └── named Write* method and audited manifest or prototype source
                → BPatch clone/list/node or actor .cfg_patch
                      → ModBuildService (repak V11 .pak + retoc UE5.5 .utoc/.ucas)
```

Direct operations implemented by the production dispatcher are `set`, `toggleSourceOrZero`, `negateSet`, `inverseScale`, `scale`, `scaleNonZero`, `scaleClamp01`, `setRangeBound`, `scaleIntegerHalfUp`, `scaleIntegerHalfUpPreserveZero` and `setDeltaFromRetainedFraction`. The audit checks each baseline direct operation is implemented, each leaf has a matching catalogue file/scope/operation and no direct leaf has overlapping parameter ownership.

Special methods include PSY overrides, Master mechanics list membership, NPC corpse loot and ammo, stash and breakable-generator cloning, world-item placement/removal, blueprint/detector/attachment shuffles, trader minimum condition and weapon distribution. **These remain special methods, never mislabeled as normal per-field scaling.**

### Counts by source domain

| Domain | Total | Direct | Special |
|---|---:|---:|---:|
| Combat | 30 | 26 | 4 |
| Survival | 53 | 53 | 0 |
| Health | 18 | 18 | 0 |
| Master | 20 | 15 | 5 |
| Stamina | 23 | 23 | 0 |
| Loot | 41 | 16 | 25 |
| Economy | 23 | 22 | 1 |
| Equipment | 4 | 4 | 0 |
| Environment | 17 | 17 | 0 |
| A-Life | 19 | 19 | 0 |
| **Total** | **248** | **213** | **35** |

The versioned [machine-readable inventory](../tools/final_cfg_mapping_inventory.json) records the **specific route, writer method, status, CFG file, scope, operation, representative original leaf, and declared catalogue targets for all 248 IDs**. The embedded `cfg-writer-baseline.json` is authoritative for all **1,230** concrete point identities. This report deliberately does not substitute the 248 labels for actual source paths.

## Actual defect: integer-scale A-Life placebo edits

The writer's A-Life `MaxCount` fields are **integers**, transformed by `scaleIntegerHalfUp` or `scaleIntegerHalfUpPreserveZero`. A slider of 0.95× over a vanilla limit of 1 still calculates 1; values of zero remain zero. This is **not** a meaningful CFG modification even when the multiplier appears changed.

The final audit found **108** such source-inert modifications across the twelve factory JSONs. A naive coverage check would mark all routes valid while production `Write()` could then throw `no concrete output` for those supposedly modified IDs. Every affected stock value has now been made exactly **1× neutral**, rather than inventing an exaggerated spawn/encounter-count change merely to make the patch nonempty. The existing intentional, effective A-Life modifications remain.

The production writer's failure message now distinguishes an ineffective integer source change and tells the user to choose a multiplier that actually alters an integer `MaxCount`, or reset to 1×. It does **not** silently write a false patch, does not fabricate spawn frequencies and does not change WPF control ranges. Edited and custom presets remain untouched: the pristine-stock migration allows only the exact Git blob hashes of previously shipped factory files.

**Important:** The same integer quantization still applies to manual settings. Not every 0.05× A-Life movement changes the final integer count. An ineffective custom setting is rejected transparently at Build instead of being misrepresented as an effective BPatch.

## Reproducible checks

The new repeatable, source-native gate runs with **only Python 3**:

```bash
python tools/audit_final_cfg_mapping.py --out audit/final-cfg-mapping
python tools/audit_ui_cfg_contract.py
python tools/audit_project_consistency.py
```

`audit_final_cfg_mapping.py` verifies all 248 declared source IDs, known direct transform cases, special dispatch methods, actual source file/scope/operation ownership, present special manifests, appropriate difficulty/DLC filtering and all twelve factory snapshots' **non-noop production direct outputs**. It emits `mapping.json` and `mapping.csv` under the chosen `--out` directory.

A **new Windows WPF contract project** `tests/AllControlsPatchContract/` probes the **real** `CfgPatchWriterService.Write` (not a Python reimplementation), attempting an effective changed state for **every one of the 235 editable source IDs** and checking that staged patch files are emitted. It has been added as an independent GitHub Actions job:

```powershell
dotnet run --project tests/AllControlsPatchContract/AllControlsPatchContract.csproj -c Release
```

Existing domain-specific production writer tests remain in `tests/*PatchContract/` and continue to check nested BPatch construction, generator fan-outs, Master/difficulty behavior and special physical world actors. The new exhaustive smoke is **additional**, not a replacement for these detailed semantic tests.

## Physical-source provenance and limitations

- `tools/audit_final_cfg_mapping.py` reads the **production embedded CFG baseline** and catalogue, not the user's live game installation. Its findings are **structural**.
- The historical original CFG evidence in `docs/AUDIT.md` covers **699/699** source leaves against a SHA-256-fingerprinted 2.0.5 dump. Later domain audits `docs/DEEP-CFG-AUDIT-PHASE*.md`, `docs/evidence/` and generator manifests provide additional source evidence. **This pass did not reacquire or independently reparse the full original 2.0.5 archives**, so do not equate 1,230 baseline paths with 1,230 newly source-verified runtime paths.
- The **51** `runtime_test_required` source IDs remain explicitly in that state. They are still routed to production writers, but their effective in-game behavior is not assumed.
- In particular, actor removals/randomized placements, cloned item generators, A-Life population caps, source `refurl` inheritance and DLC conditional routing require generated-file review and in-game checks. `sourceRaw` parity and presence of `struct.begin {bpatch}` cannot prove Unreal applies every patch.
- The prior GitHub Actions run for commit `31cfb962` failed before its job steps produced usable test logs; it cannot be cited as passing execution evidence. **No successful .NET/Wine/game result is claimed for this pass**.

## Acceptance criteria to declare game-runtime confidence

1. Run both static gates on a checked-out `main` with no errors.
2. Run the new all-controls Windows production writer job and all existing `*PatchContract` suites successfully, retaining CFG evidence.
3. Verify representative generated `GameData`, `DLC1`, list, nested-source, generator and actor paths in the packaged `.pak` (plus companion `.utoc/.ucas`).
4. Test actual build/install in Wine and load the resulting mod in **the same game build** as the fingerprinted 2.0.5 original CFG snapshot.
5. Run targeted in-game checks for the 51 runtime-pending controls, including negative/difficulty/dependent states and standalone special-writer behavior.

Until these steps pass, the defensible statement is **“248/248 mapped and production-writer-routed; factory no-op direct edits removed; live game effects partially unverified”**, not **“all 248 gameplay effects confirmed.”**
