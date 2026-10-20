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
