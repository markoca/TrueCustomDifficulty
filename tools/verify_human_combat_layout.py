#!/usr/bin/env python3
"""Static layout lock contract for the explicitly approved human combat redesign."""
from pathlib import Path
from xml.etree import ElementTree as ET
ROOT = Path(__file__).resolve().parents[1]
import json
SRC = ROOT / "src" / "TrueCustomDifficulty"
visual = (SRC / "Controls" / "AggregateVisualControl.cs").read_text()
grid = (SRC / "Infrastructure" / "ResponsiveSectionGridPanel.cs").read_text()
help_text = (SRC / "ViewModels" / "ParameterViewModel.cs").read_text()
xaml = (SRC / "MainWindow.xaml").read_text()
start = visual.index("    private void DrawHumanCombatProfile(")
end = visual.index("    // Editable axes are deliberately independent.", start)
human = visual[start:end]
required = (
    'var plotSide = Math.Min(280.0, Math.Max(180.0, ActualWidth - 360.0));',
    'var plot = new Rect(64.0, 91.0, plotSide, plotSide);',
    'DrawMixerStrip(dc, frequency, "ATTACK", "FREQUENCY",',
    'DrawMixerStrip(dc, accuracy, "FIRE", "ACCURACY",',
    'new Rect(faderLeft, 83.0, faderWidth, MixerStripHeight)',
    'new Rect(faderLeft + faderWidth + 10.0, 83.0, faderWidth, MixerStripHeight)',
    'AddHorizontalTarget(health,',
    'AddVerticalTarget(damage,',
    'HitKind.HumanJoint, health.Minimum, health.Maximum',
    'DrawHumanAxisHandle(dc, healthHandle,',
    'DrawHumanAxisHandle(dc, damageHandle,',
    'new Point(plot.Left - 12.0, gridY - 7.0)',
    'new Point(gridX, plot.Bottom + 32.0)',
    'Math.Max(422.0, plot.Bottom + 72.0)',
)
for fragment in required:
    if fragment not in human:
        raise SystemExit("[FAIL] human square/faders: " + fragment)
if "DrawHorizontalTrackCard(dc, frequency" in human or "DrawHorizontalTrackCard(dc, accuracy" in human:
    raise SystemExit("[FAIL] human: old horizontal controls remain")
if '"HumanCombatProfile" => 465.0' not in visual:
    raise SystemExit("[FAIL] human: incorrect aggregate height")
if 'or "HumanCombatProfile")' not in visual:
    raise SystemExit("[FAIL] human: per-fader rationale selection not wired")
if '["Human Enemies"] = "Mutants"' not in grid:
    raise SystemExit("[FAIL] human: missing explicitly approved adjacent section pairing")
if '"HumanCombatProfile" => "The square graph' not in help_text:
    raise SystemExit("[FAIL] human: rationale not updated")
catalog = {p["id"]: p for p in json.loads((SRC / "Data" / "parameters.json").read_text())["parameters"]}
assert catalog["combat.npcAttackCooldown"]["slider"]["max"] == 2
assert catalog["combat.npcHipAccuracy"]["slider"]["max"] == 1
assert 'HitKind.HumanJoint => Cursors.SizeAll' in visual
assert 'P("combat.npcWeaponDamage")?.ResetToReference();' in visual
ET.fromstring(xaml)
# At both width extremes, the square and the two 128+ DIP fader hit regions fit.
for board_width in (540.0, 600.0, 920.0):
    square = min(280.0, max(180.0, board_width - 360.0))
    right = 64.0 + square
    strip = min(140.0, max(128.0, (board_width - right - 32.0) / 2.0 - 5.0))
    last = right + 14.0 + strip + 10.0 + strip
    if last > board_width - 12.0:
        raise SystemExit(f"[FAIL] human: clipping at {board_width} DIP")
print("[PASS] Square Human Combat graph, vertical faders, rationale and Mutants pairing")
