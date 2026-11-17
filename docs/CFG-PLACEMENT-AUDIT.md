# CFG Placement Audit — Current Source Contract

> **Retired 2026-11-17:** `tools/audit_cfg_placement.py` was a dated snapshot generator (v0.2.53, 236 parameters). Current gates: `tools/audit_final_cfg_mapping.py` (every catalog ID → production writer route), `tools/audit_project_consistency.py` and `python3 tools/pre_release.py static|cfg-routes|source`.

This report replaces older fixed catalog and special-route totals. All active catalog parameters must be traceable through `parameters.json` and the shipped `cfg-writer-baseline.json` to either a direct source edit point or an explicit production special writer.

As of 2026-10-10, the active source contract contains **236 parameters** and **1,220 direct CFG edit points**. The Combat portion is **30 parameter dimensions / 64 direct points**, with only four PSY-specific special writer routes. The exact current route populations must be obtained by running `python3 tools/audit_cfg_placement.py --help` and the full UI/CFG contract in `python3 tools/audit_ui_cfg_contract.py`; old CSV count snapshots are not release evidence.

The audit checks declared targets, concrete source scope, CFG path and operator, ensures that no two direct parameters own the same physical target and detects unsupported special routes. Original decoded CFG parity, BPatch merge semantics and in-game behavior are separate requirements. See [Release Testing](RELEASE-TESTING.md).
