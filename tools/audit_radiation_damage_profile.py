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
assert '"1–100 RAD"' not in code
assert '"FIXED RAD"' not in code
assert "RADIATION KILLS IN" not in code, "No proven death countdown is available"
assert '"RADIATION LEVELS"' not in code and '"RAD CONTROLS"' not in code
assert '"RADIATION LEVEL"' not in code and '"RADIATION DAMAGE STAGES"' not in code
assert "private const double RadiationProfileHeight = 388.0;" in code
assert "foreach (var threshold in RadiationRingThresholds)" in code
assert "DrawRadiationRingSector(dc, center, radius, first, last, inactive);" in code
assert "DrawRadiationRingSector(dc, center, radius, first, litEnd, active);" in code
assert "DrawRadiationHudSegments(dc, center, currentRad," in code
assert "DrawRadiationHudTrefoil(dc, center, CloneWithOpacity(text, 0.82));" in code
assert '"DEAD AFTER"' in code and 'DrawStat("~" + RadiationTime(eta),' in code
assert '"DEATH ESTIMATE"' in code
assert '"MAX " + RadiationModelMaxHp.ToString(' not in code
assert rad["slider"] == {"min": 0, "max": 20, "step": 0.01}
assert '"DAMAGE PER TICK"' in code
assert '"1 TICK/S ASSUMED · NO HEALING/PROTECTION"' not in code
assert "ctx.ArcTo(" in code
bounds = code.split("RadiationRingThresholds =", 1)[1].split("};", 1)[0]
splits = [float(x) for x in re.findall(r"[0-9]+(?:\.[0-9]+)?", bounds)]
assert splits == [10, 20, 30, 40, 50, 60, 70, 80, 90, 99], splits
assert abs(360 * splits[-1] / 100 - 356.4) < 1e-9
assert "for (var tick = 10; tick <= 100; tick += RadiationHudTickStep)" in code
assert "RadiationRingPoint(center, 137.0, tick)" in code
assert "currentRad >= tick ? accent : muted" in code
assert "Math.Clamp(Math.Round(angle * 100.0 / (Math.PI * 2.0)), 0.0, 100.0)" in code

assert "DrawSourceKnob(dc, P(source.Id), source.Label" in code
assert "rateReadout: source.Id == \"survival.radiationRecovery\"" in code

# Left/hero/right knob ordering is deliberate: HP DAMAGE is larger and centered
# while three independent native parameter sources and 270-degree input remain.
knob_bank = code.split("private void DrawRadiationBalance(", 1)[1].split(
    "var profile = RadiationRingRect();", 1)[0]
knob_ids = re.findall(r'\("(survival\.radiation\w+)", "[A-Z -]+"\)', knob_bank)
assert knob_ids == [
    "survival.radiationAccumulation",
    "survival.radiationDamage",
    "survival.radiationRecovery",
], knob_ids
for token in (
    "const double gap = 12.0;",
    "const double centerExtra = 18.0;",
    "var sideWidth = (bankWidth - gap * 2.0 - centerExtra) / 3.0;",
    "var hero = i == 1;",
    "var knobWidth = sideWidth + (hero ? centerExtra : 0.0);",
    "new Rect(knobLeft, 49.0, knobWidth, hero ? 188.0 : 181.0)",
    "prominent: hero);",
):
    assert token in knob_bank, "Radiation central hero knob drifted: " + token
assert abs((2 * 188.0 + 206.0 + 2 * 12.0) - 606.0) < 1e-9
for factor in (0.0, 0.5, 1.0, 1.5, 2.0, 4.0, 6.0, 10.0, 20.0):
    for _, prototype, source_value in stages:
        expected = float(source_value) * factor
        points = [p for p in source if p["prototype"] == prototype]
        assert all(abs(float(p["sourceRaw"].rstrip("f")) * factor - expected) < 1e-9
                   for p in points)


# Ring-only preview now defaults to 0 RAD and covers 0–100.
# Neither the selection nor a death estimate may create a new CFG writer target.
for token in (
    'private double _radLevel = 0.0;',
    'private double RadiationModelMaxHp =>',
    'private static int RadiationStageIndex(double rad)',
    'private double RadiationDamageAtLevel(double level)',
    'RadiationDamageStages[index].SourceDamage * damageScale',
    'private double RadiationTimeToZeroHp()',
    'RadiationModelMaxHp / damagePerAssumedCharge',
    'double.PositiveInfinity',
    'private void ScrubRadiationRing(Point point)',
    'private void AdjustRadiationLevel(int step)',
    '_radRingCompleted = true;',
    'return distance >= 108.0 && distance <= 136.0;',
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
    'const double outer = 96.0;',
    'const double inner = 27.0;',
    'dc.DrawEllipse(symbol, null, center, 23.0, 23.0);',
    'DrawRadiationControls(dc, text, muted, soft, accent, panel);',
    'private void DrawRadiationControls(',
    '"RADS ACCUMULATED"', '"DAMAGE PER TICK"', '"DEAD AFTER"',
    'var currentRad = _radLevel;',
    'var stageFrom = index <= 0 ? 1.0 : RadiationRingThresholds[index - 1];',
    'var stageTo = index < 0 ? 0.0',
    'index == 10 ? 100.0 : RadiationRingThresholds[index] - 1.0;',
    'var level = index + 1;',
    'private const double RadiationDialCenterY = 183.0;',
    'private const double RadiationDialTrackRadius = 122.0;',
    'ring.Top + RadiationDialCenterY',
    'profile.Top + RadiationDialCenterY',
    'var hasDeathEstimate = double.IsFinite(eta);',
    'Math.Clamp(Math.Round(angle * 100.0 / (Math.PI * 2.0)), 0.0, 100.0)',
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
# and integer 0..100 level selection do not snap to the 32 tooth boundaries.
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
assert sum(x > 0 for x in active_segment_lengths(0)) == 0
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
assert "dc.DrawEllipse(symbol, null, center, 23.0, 23.0);" in glyph
assert "const double inner = 27.0;" in glyph
assert "const double outer = 96.0;" in glyph
# Read actual drawn geometry, not historical constants from older HUD versions.
# The 25-DIP vane root and 21-DIP hub must remain 4 DIP apart.
vane_inner = re.search(r"const double inner = ([0-9.]+);", glyph)
hub_match = re.search(
    r"dc\.DrawEllipse\(symbol, null, center, ([0-9.]+), ([0-9.]+)\);", glyph
)
assert vane_inner is not None and hub_match is not None, "Trefoil geometry missing"
assert float(vane_inner.group(1)) == 27.0, "Trefoil vane root drifted"
assert float(hub_match.group(1)) == float(hub_match.group(2)) == 23.0, (
    "Trefoil center radius drifted"
)
assert float(vane_inner.group(1)) - float(hub_match.group(1)) == 4.0, (
    "Trefoil vane-to-hub gap must be 4 DIP"
)
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
assert "DrawCenteredText(dc, currentRad.ToString(" not in ring_renderer
assert "profile.Top + 347.0" not in ring_renderer
assert "29.0, accent, FontWeights.Bold);" in ring_renderer
assert "if (currentRad > 0.0)" in ring_renderer
assert 'DrawCenteredText(dc, "RAD",' not in ring_renderer, "RAD label returned to glyph"
assert "DrawRadiationHudTrefoil(dc, center, CloneWithOpacity(text, 0.82));" in ring_renderer
assert "dc.DrawRoundedRectangle(panel, null, profile, 4.0, 4.0);" in ring_renderer
assert "dc.DrawRoundedRectangle(panel, null, area, 4.0, 4.0);" in control_renderer
assert "dc.DrawRoundedRectangle(panel, new Pen(border" not in ring_renderer
assert "dc.DrawRoundedRectangle(panel, new Pen(border" not in control_renderer
assert '"1–100 RAD"' not in ring_renderer
assert '"FIXED RAD"' not in control_renderer
assert '"DAMAGE PER TICK"' in control_renderer
assert 'var stageRange = index < 0 ? "0 RAD"' in control_renderer
assert 'var stageTo = index < 0 ? 0.0' in control_renderer, "0 RAD must not read thresholds[-1]"
assert '"DEAD AFTER"' in control_renderer and 'DrawStat("~" + RadiationTime(eta),' in control_renderer
assert '"DEATH ESTIMATE"' in control_renderer
assert '"MAX " + RadiationModelMaxHp.ToString(' not in control_renderer
# All three main figures match the bold 29-DIP RAD readout in hierarchy.
for token in (
    "var contentWidth = Math.Max(1.0, area.Width - 32.0);",
    "void DrawStat(string value, double y, double fontSize, Brush ink, FontWeight weight)",
    "CreateText(value, fontSize, ink, weight, contentWidth)",
    'DrawStat("RADS ACCUMULATED", wide ? 46.0 : 16.0,',
    'DrawStat(_radLevel.ToString("0", CultureInfo.InvariantCulture) + " RAD",',
    'wide ? 76.0 : 43.0, 29.0, accent, FontWeights.Bold);',
    'var level = index + 1;',
    'var levelPrefix = CreateText("LEVEL ", 13.5, muted,',
    'var levelNumber = CreateText(level.ToString(CultureInfo.InvariantCulture),',
    '22.0, accent, FontWeights.Bold, contentWidth);',
    'levelPosition.Y - 6.0));',
    'dc.DrawText(levelNumber, new Point(levelPosition.X + levelPrefix.Width + 12.0,',
    'DrawStat("DAMAGE PER TICK", wide ? 165.0 : 115.0,',
    "wide ? 192.0 : 140.0, 29.0,",
    'DrawStat(hasDeathEstimate ? "DEAD AFTER" : "DEATH ESTIMATE",',
    'wide ? 244.0 : 180.0, 11.5, muted, FontWeights.SemiBold);',
    'DrawStat("~" + RadiationTime(eta),',
    "wide ? 273.0 : 199.0, 29.0, accent, FontWeights.Bold);",
    'DrawStat("NO RAD DAMAGE", wide ? 286.0 : 215.0,',
    "25.0, text, FontWeights.Bold);",
):
    assert token in control_renderer, "Radiation stat typography/layout drift: " + token
assert "DrawRightAlignedText(dc, stageRange," not in control_renderer
assert control_renderer.count("11.5, muted, FontWeights.SemiBold);") == 3
assert 'if (hasDeathEstimate)' in control_renderer
assert 'var hasDeathEstimate = double.IsFinite(eta);' in control_renderer
assert '" TICKS"' not in control_renderer, "Do not show tick counts next to the elapsed time"
assert 'wide ? 315.0 : 237.0' not in control_renderer, "Duplicate white estimate returned"
assert control_renderer.count('DrawStat("~" + RadiationTime(eta),') == 1
assert '"DEAD AFTER " + eta.ToString' not in control_renderer
assert "foreach (var threshold in RadiationRingThresholds)" not in ring_renderer
assert "tick == 100 ? accent : muted" in ring_renderer
assert "DrawText(dc, _radLevel.ToString(" not in control_renderer
assert "RadiationStepButton(" not in control_renderer
assert "foreach (var step in" not in control_renderer
assert "var rail =" not in control_renderer
assert "levelX" not in control_renderer
for retired in ("RadiationLevelRail(", "RadiationRailHit(",
                "SetRadiationFromRail(", "_radLevelDragging"):
    assert retired not in code, "Retired RAD rail still present: " + retired

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
assert hp_zero(99, 100, 4) == 2
assert hp_zero(90, 100, 4) == 1
assert hp_zero(50, 100, 4) == 12
assert hp_zero(50, 100, 6) == 8
assert hp_zero(99, 100, 6) == 1
assert hp_zero(10, 100, 6) == 56
assert hp_zero(50, 100, 10) == 5
assert hp_zero(10, 100, 10) == 34
assert hp_zero(99, 100, 10) == 1
assert hp_zero(0, 100, 1) is None
assert hp_zero(50, 100, 0) is None

# Ring selection follows a clockwise 0–360° geometry, but click/rail
# maps 0–100, with completed ring drag holding 100 at the seam.
def angle_to_rad(clockwise_degrees):
    return max(0, min(100, int((clockwise_degrees % 360) / 3.6 + 0.5)))
assert angle_to_rad(0) == 0
assert angle_to_rad(36) == 10
assert angle_to_rad(90) == 25
assert angle_to_rad(180) == 50
assert angle_to_rad(270) == 75
assert angle_to_rad(356.4) == 99
assert angle_to_rad(359) == 100

print("PASS: 11 original radiation stages and 22 unchanged writer leaves;")
print("      enlarged 32-tooth ring-only 0–100 RAD and right-side RAD value;")
print("      hero-centered 20x HP DAMAGE and one highlighted estimated death time;")
print("      no obsolete slider, titles or footer; source damage stages unchanged;")
print("      original stage thresholds, 1:1 damage scaling and conditional fixed-RAD HP estimate.")
print("NOT VERIFIED: actual permanent-effect damage cadence, healing, armor or gameplay time to death.")
