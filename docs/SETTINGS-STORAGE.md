# Settings storage and Windows permissions

The game destination selected under **Settings** is a validated, existing `~mods` folder. This is separate from the application's `Presets`, `Output`, and repack tools.

## Storage policy (2026-10-20)

1. **Writable portable install:** save `user-settings.json` beside `TrueCustomDifficulty.exe`, using a temporary file and atomic replacement.
2. **Windows Access Denied on the portable settings write:** save the same JSON in `%LOCALAPPDATA%\\TrueCustomDifficulty\\user-settings.json` instead. Do **not** request elevation or change filesystem ACLs.
3. **Fallback already exists:** that per-user file takes precedence for reads and subsequent saves. An older portable JSON is not silently overwritten or deleted. Removing the per-user fallback manually restores portable-file loading.
4. **No fallback available or writable:** surface the actual error in Settings. Invalid `~mods` paths, malformed JSON, full disks, and other I/O failures are not silently accepted.
5. This changes only the settings-file destination. Preset edits, Output generation and installation into the game's `~mods` still require appropriate filesystem permissions; the app does not elevate itself.

A Windows user with a protected `C:\\TrueCustomDifficulty` directory can therefore save the game path without administrator privileges. For a fully portable and writable install, move the entire application to a user-writable directory instead.

## Contract test

```bash
dotnet run --project tests/SettingsPersistenceContract/SettingsPersistenceContract.csproj
```

The cross-platform .NET 10 contract links the real production settings service and verifies portable saves, JSON round-trip, denied-write fallback, fallback priority, corruption errors, invalid game path rejection and missing LocalAppData behavior. It injects an Access Denied failure at the write boundary so tests do not depend on Windows ACLs or an administrator account.
