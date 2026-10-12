#!/usr/bin/env python3
"""Check radiation stage preview against independently audited vanilla CFG rows.

Checks the fixed-level HP-zero estimate (assumed 1 damage charge/s, not a proven game death timer).
Usage: python3 tools/audit_radiation_damage_profile.py
"""
import json
import re
from math import ceil
from pathlib import Path

root = Path(__file__).resolve().parents[1]
app = root / "src" / "TrueCustomDifficulty"
code = (app / "Controls" / "AggregateVisualControl.cs").read_text(encoding="utf-8")
baseline = json.loads((app / "Data" / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
catalog = json.loads((app / "Data" / "parameters.json").read_text(encoding="utf-8"))
rad = next(p for p in catalog["parameters"] if p["id"] == "survival.radiationDamage")
source = [p for p in baseline["editPoints"] if p["parameterId"] == rad["id"]]
profile = code.split("RadiationDamageStages =", 1)[1].split(";", 1)[0]
stages = re.findall(r'\("([^"]+)",\s*"([^"]+)",\s*([0-9]+(?:\.[0-9]+)?)\)', profile)

assert len(stages) == 11, "Exactly 11 confirmed radiation effect stages are required"
assert len(source) == 22, "Radiation writer must scale exactly 22 original min/max leaves"
assert [p["prototype"] for p in rad["targets"]] == [p for _, p, _ in stages]
assert [lab for lab, _, _ in stages] == [
    "LIGHT", "MED", "STRONG", "4", "5", "6", "7", "8", "9", "10", "DEADLY"
]
for label, prototype, number in stages:
    matched = [p for p in source if p["prototype"] == prototype]
    assert sorted(p["key"] for p in matched) == ["ValueMax", "ValueMin"], prototype
    assert all(p["operation"] == "scale" and p["sourceRaw"].rstrip("f") == matched[0]["sourceRaw"].rstrip("f")
               for p in matched), prototype
    assert abs(float(number) - float(matched[0]["sourceRaw"].rstrip("f"))) < 1e-9, prototype
assert float(stages[9][2]) > float(stages[10][2]), "DEADLY must remain visually distinct from L10"
assert "RadiationDamageStages[index].SourceDamage * damageScale" in code
assert "private const int RadiationHudSegments = 32;" in code
assert "private const int RadiationHudTickStep = 10;" in code
assert "private const double RadiationHudSegmentGap = 0.36;" in code
assert 'P("survival.radiationDamage")?.IsValueModifiedFromReference() ?? false' in code
assert "RadiationProfileTop + RadiationProfileHeight" in code
assert "private const double RadiationProfileHeight = 388.0;" in code
assert "private const double RadiationControlsNarrowHeight = 272.0;" in code
assert "RadiationBoardHeight(double width)" in code
assert '"RadiationBalance" => RadiationBoardHeight' in code
assert '"1–100 RAD"' in code
assert "RADIATION KILLS IN" not in code, "No proven death countdown is available"
assert "RADIATION LEVELS" in code and "RADIATION DAMAGE STAGES" not in code
assert "private const double RadiationProfileHeight = 388.0;" in code
assert "foreach (var threshold in RadiationRingThresholds)" in code
assert "DrawRadiationRingSector(dc, center, radius, first, last, inactive);" in code
assert "DrawRadiationRingSector(dc, center, radius, first, litEnd, active);" in code
assert "DrawRadiationHudSegments(dc, center, currentRad," in code
assert "DrawRadiationHudTrefoil(dc, center, CloneWithOpacity(text, 0.82));" in code
assert '"EST. HP ZERO · 1 TICK/S"' in code
assert "ctx.ArcTo(" in code
bounds = code.split("RadiationRingThresholds =", 1)[1].split("};", 1)[0]
splits = [float(x) for x in re.findall(r"[0-9]+(?:\.[0-9]+)?", bounds)]
assert splits == [10, 20, 30, 40, 50, 60, 70, 80, 90, 99], splits
assert abs(360 * splits[-1] / 100 - 356.4) < 1e-9
assert "for (var tick = 10; tick <= 100; tick += RadiationHudTickStep)" in code
assert "RadiationRingPoint(center, 128.0, tick)" in code
assert "tick == 100 ? accent : muted" in code
assert "Math.Clamp(Math.Round(angle * 100.0 / (Math.PI * 2.0)), 1.0, 100.0)" in code

assert "DrawSourceKnob(dc, P(source.Id), source.Label" in code
assert "rateReadout: source.Id == \"survival.radiationRecovery\"" in code
for factor in (0.0, 0.5, 1.0, 1.5, 2.0):
    for _, prototype, source_value in stages:
        expected = float(source_value) * factor
        points = [p for p in source if p["prototype"] == prototype]
        assert all(abs(float(p["sourceRaw"].rstrip("f")) * factor - expected) < 1e-9
                   for p in points)


# Preview-only controls must be bound to a fixed 1–100 RAD selection.
# Neither the selection nor a death estimate may create a new CFG writer target.
for token in (
    'private double _radLevel = 1.0;',
    'private double RadiationModelMaxHp =>',
    'private static int RadiationStageIndex(double rad)',
    'private double RadiationDamageAtLevel(double level)',
    'RadiationDamageStages[index].SourceDamage * damageScale',
    'private double RadiationTimeToZeroHp()',
    'RadiationModelMaxHp / damagePerAssumedCharge',
    'double.PositiveInfinity',
    'private void ScrubRadiationRing(Point point)',
    'private void SetRadiationFromRail(Point point)',
    'private Rect RadiationLevelRail()',
    'private bool RadiationRailHit(Point point)',
    'private void AdjustRadiationLevel(int step)',
    'Math.Clamp(Math.Round(1.0 + fraction * 99.0), 1.0, 100.0)',
    '_radRingCompleted = true;',
    'return distance >= 100.0 && distance <= 127.0;',
    'private const int RadiationHudSegments = 32;',
    'private const int RadiationHudTickStep = 10;',
    'private const double RadiationHudSegmentGap = 0.36;',
    'DrawRadiationHudSegments(dc, center, currentRad,',
    'DrawRadiationHudTrefoil(dc, center, CloneWithOpacity(text, 0.82));',
    'DrawRadiationRingSector(dc, center, radius, first, last, inactive);',
    'DrawRadiationRingSector(dc, center, radius, first, litEnd, active);',
    'var litEnd = Math.Min(last, rad);',
    'if (litEnd > first + Epsilon)',
    'private static Point RadiationHudPolar(',
    'private static void DrawRadiationHudTrefoil(',
    'new Pen(CloneWithOpacity(border, 0.9), 1.3)',
    'const double outer = 89.0;',
    'const double inner = 25.0;',
    'dc.DrawEllipse(symbol, null, center, 21.0, 21.0);',
    'area.Left + 22.0',
    'area.Width - 44.0',
    'DrawRadiationControls(dc, text, muted, border, soft, accent, panel);',
    'private void DrawRadiationControls(',
    '"RAD CONTROLS"', '"RADIATION LEVEL"',
    '"ACTIVE STAGE"', '"CFG DAMAGE / CHARGE"', '"EST. HP ZERO · 1 TICK/S"',
    '"FIXED RAD · NO HEALING OR PROTECTION"', '"FIXED RAD"',
    'var currentRad = _radLevel;',
    'var stageFrom = index <= 0 ? 1.0 : RadiationRingThresholds[index - 1];',
    'var stageTo = index == 10 ? 100.0 : RadiationRingThresholds[index] - 1.0;',
    'DrawRightAlignedText(dc, stageRange,',
    'private const double RadiationDialCenterY = 194.0;',
    'ring.Top + RadiationDialCenterY',
    'profile.Top + RadiationDialCenterY',
    '"EST. HP ZERO · 1 TICK/S"',
    'Math.Clamp(Math.Round(angle * 100.0 / (Math.PI * 2.0)), 1.0, 100.0)',
    '"RadiationBalance" => RadiationBoardHeight(width)',
    'health.maxHP'
):
    assert token in code, "Fixed-RAD control/source drift: " + token

# The old hypothetical FIELD/START/RECOVERY/TIMELINE model is removed.
for token in (
    'RADIATION SIMULATION', 'RADIATION TIMELINE', 'MODEL HP ZERO',
    'DrawRadiationSimulation(', 'RadiationFields =', 'RadiationStarts =',
    'RadiationNetGain(', 'RadiationHpLoss(', 'RadiationHpZeroSeconds(',
    'RadiationPlaybackButton(', 'RadiationPlaybackHit(', 'RadiationSimHit(',
    'ToggleRadiationPlayback(', 'TickRadiationPlayback(', 'StopRadiationPlayback(',
    'DispatcherTimer', '_radField', '_radStart', '_radRecovery', '_radPlaying',
    '_radElapsed', '_radSimFocus', 'PLAY ×10', 'TO 99 RAD', 'NO NET GAIN'
):
    assert token not in code, "Retired simulation returned: " + token

# Decorative HUD segmentation is a render only; the 10 exact game thresholds
# and integer 1..100 level selection do not snap to the 48 tooth boundaries.
segment_count = 32
gap = .36
pitch = 100.0 / segment_count
assert 0 < gap < pitch
def active_segment_lengths(rad_value):
    return [max(0.0, min((i + 1) * pitch - gap / 2, rad_value)
                - (i * pitch + gap / 2))
            for i in range(segment_count)]
assert len(active_segment_lengths(100)) == 32
assert sum(x > 0 for x in active_segment_lengths(100)) == 32
assert 0 < active_segment_lengths(1)[0] < pitch - gap
assert sum(x > 0 for x in active_segment_lengths(1)) == 1
assert sum(x > 0 for x in active_segment_lengths(50)) == 16
assert sum(x > 0 for x in active_segment_lengths(99)) == 32
assert sum(active_segment_lengths(99)) < sum(active_segment_lengths(100))
assert all(sum(active_segment_lengths(a)) < sum(active_segment_lengths(b))
           for a, b in ((1, 10), (10, 25), (25, 50), (50, 75), (75, 90), (90, 99), (99, 100)))
assert splits == [10,20,30,40,50,60,70,80,90,99]

# The HUD glyph is built as three vector blades and a hub (no external image),
# keeping actual pointer ring radius and panel hit rectangles unchanged.
glyph = code.split("private static void DrawRadiationHudTrefoil(", 1)[1].split(
    "// Native BUILD-UP", 1)[0]
assert "for (var blade = 0; blade < 3; blade++)" in glyph
assert "var angle = -150.0 + blade * 120.0;" in glyph
assert "ctx.ArcTo(" in glyph
assert "dc.DrawEllipse(symbol, null, center, 21.0, 21.0);" in glyph
assert "const double inner = 25.0;" in glyph
assert "const double outer = 89.0;" in glyph
assert "const double outer = 89.0;" in glyph
assert "const double inner = 17.0;" in glyph
assert "dc.DrawEllipse(shadow" not in glyph
assert "RadiationHudPolar(center, 28.0, angle)" not in glyph
assert "new Pen(accent, 13.0)" not in code
assert "DrawRadiationRingSector(dc, center, radius, 0.0, currentRad, accent);" not in code

# The ring stays the only circular input. No numeric thresholds or RAD
# number overlays its symbol; selected RAD is read in the adjacent card.
ring_renderer = code.split("private void DrawRadiationBalance(", 1)[1].split(
    "// Preview-only RAD selection.", 1)[0]
control_renderer = code.split("private void DrawRadiationControls(", 1)[1].split(
    "// Three native stash ammo quantities", 1)[0]
for token in (
    "RadiationStepButton(", "RadiationStepHit(",
    "new Point(center.X, center.Y + 43.0)",
    "new Point(center.X, center.Y + 68.0)",
    "RadiationRingPoint(center, tickLabelRadius, threshold)",
    "threshold.ToString(",
):
    assert token not in code, "Retired RAD step buttons or number overlays returned: " + token
assert "DrawCenteredText(dc," not in ring_renderer.split("for (var tick = 10; tick <= 100; tick += RadiationHudTickStep)", 1)[1].split(
    "DrawRadiationHudTrefoil(dc", 1)[0], "Numerical threshold overlay returned"
assert "currentRad.ToString(" not in ring_renderer, "RAD number returned to trefoil"
assert 'DrawCenteredText(dc, "RAD",' not in ring_renderer, "RAD label returned to glyph"
assert "DrawRadiationHudTrefoil(dc, center, CloneWithOpacity(text, 0.82));" in ring_renderer
assert "foreach (var threshold in RadiationRingThresholds)" not in ring_renderer
assert "tick == 100 ? accent : muted" in ring_renderer
assert "DrawText(dc, _radLevel.ToString(" in control_renderer
assert "RadiationStepButton(" not in control_renderer
assert "foreach (var step in" not in control_renderer
assert "new Point(rail.Left, area.Top" not in control_renderer
assert "new Point(rail.Right, area.Top" not in control_renderer
assert "private Rect RadiationLevelRail()" in code
assert "RadiationRailHit(point)" in code

# Fixed-radiation calculation has no build-up and no passive recovery input.
fixed_math = code.split('private double RadiationTimeToZeroHp()', 1)[1].split('private static string RadiationTime(', 1)[0]
assert 'Math.Ceiling(RadiationModelMaxHp / damagePerAssumedCharge)' in fixed_math
assert 'survival.radiationAccumulation' not in fixed_math
assert 'survival.radiationRecovery' not in fixed_math
assert 'survival.radiationDamage' in code

# Compare actual staged source amount across both sides of every boundary.
stage_onsets = [1.0, *splits]
stage_damages = [float(value) for _, _, value in stages]
def damage_at(level, multiplier):
    idx = sum(level >= start for start in stage_onsets) - 1
    return 0.0 if idx < 0 else stage_damages[idx] * multiplier

def hp_zero(level, hp, multiplier):
    damage = damage_at(level, multiplier)
    return None if damage <= 0 else ceil(hp / damage)

for level, expected in (
    (0, 0.0), (1, .2), (9, .2), (10, .3), (19, .3),
    (20, .48), (29, .48), (30, .816), (40, 1.468),
    (50, 2.2), (60, 4.18), (70, 8.37), (80, 17.58),
    (90, 38.67), (98, 38.67), (99, 20.0), (100, 20.0)
):
    assert abs(damage_at(level, 1) - expected) < 1e-9, level
    assert abs(damage_at(level, 1.5) - 1.5 * expected) < 1e-9, level
    assert damage_at(level, 0) == 0, level

assert abs(hp_zero(1, 100, 1) - 500.0) < 1e-9
assert hp_zero(50, 100, 1) == 46
assert hp_zero(90, 100, 1) == 3
assert abs(hp_zero(99, 100, 1) - 5.0) < 1e-9
assert hp_zero(99, 100, 2) == 3
assert hp_zero(0, 100, 1) is None
assert hp_zero(50, 100, 0) is None

# Ring selection follows a clockwise 0–360° geometry, but click/rail
# clamps the exposed chooser to 1–100, reserving 100 for completed full circle.
def angle_to_rad(clockwise_degrees):
    return max(1, min(100, int((clockwise_degrees % 360) / 3.6 + 0.5)))
assert angle_to_rad(0) == 1
assert angle_to_rad(36) == 10
assert angle_to_rad(90) == 25
assert angle_to_rad(180) == 50
assert angle_to_rad(270) == 75
assert angle_to_rad(356.4) == 99
assert angle_to_rad(359) == 100

print("PASS: 11 original radiation stages and 22 unchanged writer leaves;")
print("      32 HUD teeth, ticks every 10 RAD, separated trefoil and larger center;")
print("      original stage thresholds, 1:1 damage scaling and conditional fixed-RAD HP estimate.")
print("NOT VERIFIED: actual permanent-effect damage cadence, healing, armor or gameplay time to death.")
