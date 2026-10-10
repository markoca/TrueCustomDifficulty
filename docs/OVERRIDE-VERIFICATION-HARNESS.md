# Override Verification — Current Scope

The historical `v0.2.52` override-planning harness is not part of the current source tree. Its archived scenario totals are not a production release gate. Do not treat older generated reference patches or counts as evidence for the present catalog.

For the current source-backed runtime-pending controls, use the active catalog and `cfg-writer-baseline.json`, along with `tools/audit_ui_cfg_contract.py`, `tools/audit_production_integration.py` and the production C# writer-contract suites listed in [Release Testing](RELEASE-TESTING.md).

The **four PSY Combat** controls retain the dedicated structural writer, targeting `SpawnPSYPhantoms` and `SpawnPSYNPC`. Master uses sparse index-aware updates to the existing PSY array; other difficulties create a two-effect array. Generated BPatch shape and values are covered by `tests/CombatPatchContract`; the real Unreal Engine loader and gameplay effects still require in-game tests.

The other runtime-pending routes (generator-backed loot, trader services, environment/equipment rules) remain covered by their domain-specific production contracts. Neither generated-CFG comparisons nor a local successful build automatically establish in-game acceptance.
