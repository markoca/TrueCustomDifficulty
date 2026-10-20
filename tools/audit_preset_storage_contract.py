#!/usr/bin/env python3
"""Guard custom preset storage against Windows app-folder permission regressions."""
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
vm = (ROOT / "src/TrueCustomDifficulty/ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
storage = (ROOT / "src/TrueCustomDifficulty/Services/UserPresetStorageService.cs").read_text(encoding="utf-8")
preset = (ROOT / "src/TrueCustomDifficulty/Services/PresetService.cs").read_text(encoding="utf-8")

required_vm = [
    "private readonly UserPresetStorageService _userPresetStorage = new();",
    "InitialDirectory = _userPresetStorage.EnsureDirectory()",
    "var userItems = _userPresetStorage.List();",
    ".Where(item => !userNames.Contains(item.Name))",
    ".Concat(userItems)",
]
missing = [token for token in required_vm if token not in vm]
assert not missing, "User-preset UI/list wiring drifted: " + ", ".join(missing)
assert vm.count("InitialDirectory = _userPresetStorage.EnsureDirectory()") == 2,     "Save and Load dialogs must both default to per-user Presets"

required_storage = [
    "Environment.SpecialFolder.LocalApplicationData",
    '"TrueCustomDifficulty"',
    '"Presets"',
    'Directory.EnumerateFiles(_directory, "*.json"',
]
missing = [token for token in required_storage if token not in storage]
assert not missing, "User preset storage contract drifted: " + ", ".join(missing)

assert 'InitialDirectory = _presetDirectoryService.Ensure()' not in vm,     "Preset dialogs returned to the portable application Presets directory"
assert 'Guid.NewGuid().ToString("N") + ".tmp"' in preset,     "Preset save must use unique temp files"
assert "catch (UnauthorizedAccessException) { }" in preset,     "Preset temp cleanup must not mask the actual save error"

print("[PASS] custom presets default to per-user storage and remain merged into the picker")
