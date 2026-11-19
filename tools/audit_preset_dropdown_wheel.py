#!/usr/bin/env python3
"""Guard hover-wheel profile selection without changing other WPF wheel controls."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/TrueCustomDifficulty/MainWindow.xaml"
CODE = ROOT / "src/TrueCustomDifficulty/MainWindow.xaml.cs"
VM = ROOT / "src/TrueCustomDifficulty/ViewModels/MainViewModel.cs"

root = ET.parse(XAML).getroot()
ns = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
pickers = [node for node in root.iter(f"{ns}ComboBox")
           if node.get("ItemsSource") == "{Binding AvailablePresets}"]
assert len(pickers) == 1, "Exactly one canonical profile dropdown must exist"
picker = pickers[0]
assert picker.get("Width") == "190", "Do not expand the compact profile picker"
assert picker.get("SelectedItem") == "{Binding SelectedPreset, Mode=TwoWay}"
assert picker.get("PreviewMouseWheel") == "PresetComboBox_PreviewMouseWheel"

code = CODE.read_text(encoding="utf-8")
match = re.search(
    r"private void PresetComboBox_PreviewMouseWheel\(object sender, MouseWheelEventArgs e\)"
    r"\s*\{(?P<body>.*?)\n    \}",
    code, re.S,
)
assert match, "Profile wheel handler missing"
body = match.group("body")
for required in (
    "sender is not ComboBox picker",
    "picker.IsDropDownOpen",
    "picker.Items.Count == 0",
    "e.Delta == 0",
    "e.Delta > 0 ? -1 : 1",
    "Math.Clamp(",
    "picker.SelectedIndex = next;",
    "e.Handled = true;",
):
    assert required in body, "Preset wheel lacks scoped behavior: " + required
assert body.index("picker.IsDropDownOpen") < body.index("picker.SelectedIndex = next;"), (
    "Open dropdown must keep native list scrolling"
)
assert body.index("picker.SelectedIndex = next;") < body.index("e.Handled = true;")
assert "LoadPresetFromPath(" not in body, "Reuse existing TwoWay SelectedPreset loading"
assert 'PreviewMouseWheel="CurrentValueBox_PreviewMouseWheel"' in XAML.read_text(
    encoding="utf-8"
), "Existing numeric field wheel must not be removed"

view_model = VM.read_text(encoding="utf-8")
assert "LoadPresetFromPath(value.Path);" in view_model, (
    "SelectedPreset changes must immediately load the target profile"
)
assert "if (!SetProperty(ref _selectedPreset, value) || value is null || _suppressPresetSelection)" in view_model
assert 'IsHitTestVisible="{Binding IsBuildIdle}"' in XAML.read_text(encoding="utf-8")
print("PASS: hovered closed profile picker cycles one entry up/down, clamps boundaries")
print("PASS: open popup scrolling, value-box wheels and full profile loading remain intact")
