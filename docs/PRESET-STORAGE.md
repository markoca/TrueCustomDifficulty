## 2026-11-19 — Pull + publish refreshes factory profiles

Run `git pull` on `main` to update the 12 version-controlled JSON sources under `src/TrueCustomDifficulty/Presets/`. Every **successful** `./publish.sh` copies the exact twelve JSONs into `publish/win-x64/Presets/Factory/` **in the same rollback-capable transaction as the EXE**. This happens at build time, before launching the app. The build receipt `publish/win-x64/BUILD-INFO.txt` records `factory_preset_count=12` and `factory_presets_sha256`, providing a common source signature with the EXE build.

The portable `Presets` root and its inode remain unchanged; custom files, manually edited `Presets/<role>.json`, `README.md`, `tools`, and prior `Output` are not deleted. The app-managed `Presets/Factory` subtree alone is replaced, so **do not store personal preset edits inside Factory**. Edited root versions remain available as `<role> (local)` in the picker. If compilation, source validation or publishing fails, the previously working EXE, factory profiles and build receipt are preserved. The app also refreshes managed profiles from embedded resources at startup, keeping the built-in copies consistent.

Validation: `python3 tools/audit_publish_directory_contract.py` exercises fake publishes, updates to tracked factory sources, protected local presets, rollback and missing-source rejection. A real .NET 10/Wine build and live UI check remain separate.

# Preset storage

True Custom Difficulty uses one portable preset location:

`<TrueCustomDifficulty>\Presets`

The 12 factory profiles, custom presets saved by the user, and legacy portable presets all live in that directory. **Save Preset** and **Load Preset** both default there, and the preset picker reads the same directory. Custom presets are not redirected to `%LOCALAPPDATA%`.

## Windows save behavior

Preset JSON is fully serialized in memory before the destination file is touched, then written directly with `File.WriteAllText`.

The previous implementation wrote a temporary file and used `File.Move(temp, target, overwrite: true)`. On some Windows folders that replacement operation can fail with:

`A required privilege is not held by the client.`

That privileged rename/replace path is no longer used for presets.

Before opening **Save Preset**, the application now verifies that it can create a brand-new file in `Presets`. This is intentionally different from overwriting an existing JSON: Windows can allow file writes while denying **Create files / write data** on the directory itself.

If creation is denied, True Custom Difficulty grants the current Windows user **Modify** permission on the portable `Presets` directory only, with inheritance for files/subfolders. It first tries the ACL repair without elevation; if Windows refuses the ACL change, it invokes Windows' built-in `%SystemRoot%\System32\icacls.exe` through a one-time UAC approval. **Nothing needs to be installed or shipped with the app.** Permission-tool, UAC, Wine-compatibility and account-resolution failures are caught and shown as a normal error dialog; they must never crash the application. It does not grant permissions on the whole application directory, the game directory, or other user files.

## Verification

```bash
python3 tools/audit_preset_storage_contract.py
dotnet run --project tests/PresetSaveContract/PresetSaveContract.csproj
```

## 2026-11-19 — Factory picker source correction

Official factory roles in the dropdown use current EXE-embedded JSONs mirrored to `Presets/Factory/<role>.json`, refreshed on startup. Root `Presets/<role>.json` remains under the existing exact-blob migration: any locally edited copy is preserved and shown separately as `<role> (local)`, not silently substituted for the updated factory. Identical duplicates are hidden in the picker. Save/Load dialogs continue to default to the portable root `Presets` folder; custom-named profiles remain unchanged. The managed Factory subfolder is not a user save destination. This addresses Human Tactics/Awareness values appearing unchanged when changing profile without altering the catalog, source CFG, or gameplay values.
