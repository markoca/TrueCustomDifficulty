# Preset storage

True Custom Difficulty uses one portable preset location:

`<TrueCustomDifficulty>\Presets`

The 12 factory profiles, custom presets saved by the user, and legacy portable presets all live in that directory. **Save Preset** and **Load Preset** both default there, and the preset picker reads the same directory. Custom presets are not redirected to `%LOCALAPPDATA%`.

## Windows save behavior

Preset JSON is fully serialized in memory before the destination file is touched, then written directly with `File.WriteAllText`.

The previous implementation wrote a temporary file and used `File.Move(temp, target, overwrite: true)`. On some Windows folders that replacement operation can fail with:

`A required privilege is not held by the client.`

That privileged rename/replace path is no longer used for presets.

The application still requires ordinary write permission to its `Presets` directory. It does not request administrator rights and does not change filesystem ACLs.

## Verification

```bash
python3 tools/audit_preset_storage_contract.py
dotnet run --project tests/PresetSaveContract/PresetSaveContract.csproj
```
