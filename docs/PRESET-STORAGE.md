# Custom preset storage

Custom True Custom Difficulty presets are mutable user data.

On Windows, **Save Preset** and **Load Preset** default to:

`%LOCALAPPDATA%\TrueCustomDifficulty\Presets`

The application-side `Presets` directory remains the source for the 12 shipped factory profiles and for legacy portable user JSONs that may already exist there. Both locations are merged into the preset picker. If the same preset name exists in both places, the per-user copy is shown.

This prevents a protected or read-only application directory from blocking Save Preset. It also keeps the program portable enough to read older preset files without requiring administrator privileges.

The JSON schema and gameplay semantics are unchanged.

Verification:

```bash
python3 tools/audit_preset_storage_contract.py
dotnet run --project tests/UserPresetStorageContract/UserPresetStorageContract.csproj
```
