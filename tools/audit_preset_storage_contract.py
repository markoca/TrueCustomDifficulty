#!/usr/bin/env python3
"""Guard the single portable Presets directory and Windows-safe save path."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
vm = (ROOT / "src/TrueCustomDifficulty/ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
preset = (ROOT / "src/TrueCustomDifficulty/Services/PresetService.cs").read_text(encoding="utf-8")
storage_path = ROOT / "src/TrueCustomDifficulty/Services/UserPresetStorageService.cs"

assert "_userPresetStorage" not in vm, "LocalAppData user-preset storage returned to MainViewModel"
assert "UserPresetStorageService" not in vm, "LocalAppData user-preset service returned to MainViewModel"
assert vm.count("InitialDirectory = _presetDirectoryService.Ensure()") == 2,     "Save and Load dialogs must both default to the portable Presets directory"
assert "var items = _presetDirectoryService.List();" in vm,     "Preset picker must read the same portable Presets directory"
assert not storage_path.exists(), "Obsolete LocalAppData preset service still exists"

assert "File.WriteAllText(fullPath, json);" in preset,     "Preset save must write the completed JSON directly to the selected target"
assert "File.Move(tempPath, fullPath, overwrite: true)" not in preset,     "Privileged Windows temp-file replacement path returned"
assert 'Guid.NewGuid().ToString("N") + ".tmp"' not in preset,     "Preset save unexpectedly creates temporary rename files"

print("[PASS] factory and custom presets use one portable Presets directory")
