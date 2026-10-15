#!/usr/bin/env python3
"""Source-level contract for the LocalizationWorkbench-style BUILD rail.

Only validates XAML/async wiring. It does not prove .NET/Wine rendering,
external repak/retoc execution, or correct in-game CFG application.
"""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src/TrueCustomDifficulty"
XAML = APP / "MainWindow.xaml"
VM = APP / "ViewModels/MainViewModel.cs"
BUILDER = APP / "Services/ModBuildService.cs"
MAIN_WINDOW = APP / "MainWindow.xaml.cs"
MODEL = APP / "Models/BuildProgressUpdate.cs"
w = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"

xaml = XAML.read_text(encoding="utf-8")
vm = VM.read_text(encoding="utf-8")
builder = BUILDER.read_text(encoding="utf-8")
window = MAIN_WINDOW.read_text(encoding="utf-8")
model = MODEL.read_text(encoding="utf-8")
root = ET.fromstring(xaml)  # catches malformed styles/closing tags

# Exactly one compact progress control in the footer, not over the cards,
# with Settings to its left and the profile selector to its right.
bars = list(root.iter(w + "ProgressBar"))
assert len(bars) == 1, f"expected exactly one build progress rail, got {len(bars)}"
assert bars[0].attrib.get("Value") == "{Binding BuildProgressStep, Mode=OneWay}", (
    "ProgressBar.Value defaults to TwoWay in WPF; binding to the read-only "
    "MainViewModel.BuildProgressStep must explicitly be OneWay"
)
assert bars[0].attrib.get("Style") == "{StaticResource WorkbenchBuildProgressStyle}"

footer = next((p for p in root.iter(w + "DockPanel")
               if p.attrib.get("LastChildFill") == "True"), None)
assert footer is not None
children = list(footer)
assert len(children) == 3 and [x.tag for x in children] == [
    w + "Button", w + "StackPanel", w + "Border",
]
assert children[0].attrib.get("Content") == "Settings"
assert children[1].find(w + "ComboBox") is not None
assert bars[0] in list(children[2].iter())
assert "IsBuilding" in ET.tostring(children[2], encoding="unicode")
assert "Collapsed" in ET.tostring(children[2], encoding="unicode")

for tag in ("Grid", "TabControl"):
    matching = [p for p in root.iter(w + tag)
                if p.attrib.get("Grid.Row") in ("1", "2")]
    # WPF Grid.Row is not namespace-prefixed.
    if not matching:
        matching = [p for p in root.iter(w + tag)
                    if p.attrib.get("{" +
                        "http://schemas.microsoft.com/winfx/2006/xaml/presentation" +
                        "}Grid.Row") in ("1", "2")]
    assert any(p.attrib.get("IsHitTestVisible") == "{Binding IsBuildIdle}"
               for p in matching), f"unlocked main editor region {tag}"
assert children[0].attrib.get("IsHitTestVisible") == "{Binding IsBuildIdle}"
assert children[1].attrib.get("IsHitTestVisible") == "{Binding IsBuildIdle}"
# Disabled WPF templates can paint white patches under Wine: never change
# IsEnabled on an entire tab/content subtree just to lock busy input.
assert 'IsEnabled="{Binding IsBuildIdle}"' not in xaml
assert "PreviewKeyDown += (_, e) =>" in window
assert "PreviewTextInput += (_, e) =>" in window
assert window.count("DataContext is MainViewModel { IsBuilding: true }") >= 4
assert "e.Handled = true;" in window
for color in ("#FFD438", "#F0C419"):
    assert color in xaml, f"missing LocalizationWorkbench gradient stop {color}"
assert 'x:Name="PART_Track"' in xaml and 'x:Name="PART_Indicator"' in xaml
assert 'RepeatBehavior="Forever"' in xaml, "no visible live-work pulse"

assert "record struct BuildProgressUpdate(int CompletedStages, string Phase)" in model
assert "IProgress<BuildProgressUpdate>? progress = null" in builder
for stage, label in [
    (1, "PACKING PAK"),
    (2, "BUILDING IOSTORE"),
    (3, "VERIFYING PACKAGE"),
    (4, "PUBLISHING OUTPUT"),
    (5, "INSTALLING"),
]:
    assert f'new BuildProgressUpdate({stage}, "{label}")' in builder, label
assert 'new BuildProgressUpdate(4, "CLEARING OUTPUT")' in builder
assert 'progress?.Report(new BuildProgressUpdate(5, "INSTALLING"));' in builder
assert "RunRepak(repakPath, stagingRoot, pakPath);" in builder
assert "RunRetoc(retocPath, stagingRoot, utocPath);" in builder
assert "CommitTriplet(outputDirectory, expected);" in builder

assert "private async Task BuildAsync()" in vm and "if (IsBuilding)" in vm
assert "new Progress<BuildProgressUpdate>" in vm
assert "await Task.Run(() =>" in vm
assert "_modBuildService.Build(this, _catalog, _gameInstall, progress)" in vm
assert "_modDeploymentService.Install(modsFolder!, result.Files)" in vm
assert "update.CompletedStages < BuildProgressStep" in vm
assert "IsBuilding = false;" in vm
assert 'BuildProgressLabel = "WRITING CFG";' in vm
assert 'BuildProgressLabel = "COMPLETE";' in vm
assert re.search(r'BuildCommand = new RelayCommand\(.*!IsBuilding\)', vm)
assert "Closing += (_, e) =>" in window and "e.Cancel = true;" in window

# The top-left radiation symbol has exactly the original three blades and
# center dot. Animation applies to the WHOLE Canvas; never mutate the
# independently authored 0/120/240-degree blade geometry or layout.
# LocalizationWorkbench/StalkerBrandMark: +10 degrees / 33 ms while busy,
# restore 0 on idle. WPF achieves the same 300deg/s with 1.2s/revolution.
logo_candidates = [element for element in root.iter(w + "Canvas")
                   if element.attrib.get("Width") == "34"
                   and element.attrib.get("Height") == "34"]
assert len(logo_candidates) == 1, "top-left 34x34 radiation canvas missing or duplicated"
logo = logo_candidates[0]
blades = [element for element in list(logo) if element.tag == w + "Path"]
centers = [element for element in list(logo) if element.tag == w + "Ellipse"]
assert len(blades) == 3 and len(centers) == 1, (
    "spinning mark must retain three original blades and the center dot")
angles = []
for blade in blades:
    transform = blade.find(w + "Path.RenderTransform/" + w + "RotateTransform")
    angles.append(int(transform.attrib["Angle"]) if transform is not None else 0)
assert angles == [0, 120, 240], "radiation shape blades changed"
assert centers[0].attrib.get("Width") == "7"
assert centers[0].attrib.get("Height") == "7"

spin = logo.find(w + "Canvas.RenderTransform/" + w + "RotateTransform")
assert spin is not None and spin.attrib == {
    "Angle": "0", "CenterX": "17", "CenterY": "17"
}, "the full icon must rotate around the existing geometric center (17,17)"
trigger = logo.find(w + "Canvas.Style/" + w + "Style/" +
                    w + "Style.Triggers/" + w + "DataTrigger")
assert trigger is not None
assert trigger.attrib.get("Binding") == "{Binding IsBuilding}"
assert trigger.attrib.get("Value") == "True"
start = trigger.find(w + "DataTrigger.EnterActions/" + w + "BeginStoryboard")
stop = trigger.find(w + "DataTrigger.ExitActions/" + w + "StopStoryboard")
assert start is not None and stop is not None
assert start.attrib.get("Name") == "RadiationBuildSpin"
assert stop.attrib.get("BeginStoryboardName") == start.attrib["Name"]
animation = start.find(w + "Storyboard/" + w + "DoubleAnimation")
assert animation is not None and animation.attrib.get("Storyboard.TargetProperty") == (
    "(UIElement.RenderTransform).(RotateTransform.Angle)")
assert animation.attrib.get("From") == "0"
assert animation.attrib.get("To") == "360"
assert animation.attrib.get("Duration") == "0:0:1.2"
assert animation.attrib.get("RepeatBehavior") == "Forever"
assert "IsBuilding = true;" in vm and "IsBuilding = false;" in vm
assert 'Value="{Binding BuildProgressStep, Mode=OneWay}"' in xaml

print("PASS: top-left radiation mark rotates clockwise at 300deg/s only "
      "during actual Build Difficulty and resets to its original idle angle.")
print("PASS: build rail is in Settings/profile gap, hidden when idle, styled like "
      "LocalizationWorkbench, stage-backed, async and editor-locked.")
print("NOT TESTED: WPF compile/Wine rotation, footer layout, repak/retoc execution.")
