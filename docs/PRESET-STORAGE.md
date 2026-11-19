# Preset storage — one authoritative JSON per factory role

The twelve official profiles are version-controlled under `src/TrueCustomDifficulty/Presets/<role>.json`. Each `git pull` updates these source files on `main`. Every successful `./publish.sh` copies the twelve JSON files byte-for-byte to `publish/win-x64/Presets/<role>.json`, together with the EXE and `BUILD-INFO.txt` in a rollback-capable transaction. The picker loads these root JSONs directly: **one file per official role**, no `Factory` subdirectory and no `(local)` role duplicate. The app also refreshes all twelve official files from its embedded resources at startup.

**Source-of-truth policy:** `main` owns the twelve official role names. Changes made only to an installed `Presets/<role>.json` file are overwritten on a successful publish or next application launch. To permanently tune a built-in profile, edit its tracked source JSON and commit/push to `main`. Saving from the app does **not** automatically push to GitHub. A separately named custom profile (e.g. `My Tactical.json`) is never overwritten, unless the user intentionally chooses its name for a save.

The portable `Presets` root inode, unrelated custom-named JSONs, its README, existing `tools` and `Output` remain intact. The obsolete generated `Presets/Factory` folder is retired after a successful publish; unknown files found there are preserved instead of deleted. A failed build or source validation keeps the previous EXE, official JSON files and receipt. `BUILD-INFO.txt` records `factory_preset_count=12` and `factory_presets_sha256`, which `run-latest.sh` verifies against both source and published files before launching Wine.

## Windows save behavior

Preset JSON is fully serialized in memory before the destination file is touched, then written directly using `File.WriteAllText`. Save and Load dialogs default to the portable `Presets` root, not `%LOCALAPPDATA%`. A permission probe checks whether the current Windows user can create new files; if needed, the app tries to grant that user Modify access to the portable Presets folder using Windows `icacls`. Permission errors are shown through a normal dialog.

## Verification

```bash
python3 tools/audit_preset_storage_contract.py
python3 tools/audit_publish_directory_contract.py
dotnet run --project tests/PresetSaveContract/PresetSaveContract.csproj
```
