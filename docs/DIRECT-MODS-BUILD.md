# Direct ~mods build destination

As of 2026-10-20, **Build Difficulty has no persistent application Output directory**.

The pipeline validates the saved `~mods` path, writes CFG and runs repak/retoc under the OS temporary directory, validates the complete preset-named `.pak/.utoc/.ucas` triplet, and transactionally installs that temporary triplet directly into the selected `~mods` before cleanup.

Only root-level `zzzzz_TrueCustomDifficulty_*.pak/.utoc/.ucas` files are application-owned. Existing owned files are rollback-protected during replacement; unrelated mods are never touched. A build with no gameplay differences removes only those owned files.

A fresh publish creates `tools/` and `Presets/` beside the executable, but does **not** create `Output/`. An `Output` directory left by an older version is legacy data and is not read, written, cleared or used by the current build pipeline.

Verification:

```bash
python3 tools/audit_build_progress_contract.py
python3 tools/audit_project_consistency.py
python3 tools/audit_publish_directory_contract.py
bash tests/run-wine.sh DirectModsDeploymentContract
```
