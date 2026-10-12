#!/usr/bin/env python3
"""Check radiation stage preview against independently audited vanilla CFG rows.

Checks the fixed-level HP-zero estimate (assumed 1 damage charge/s, not a proven game death timer).
Usage: python3 tools/audit_radiation_damage_profile.py
"""
import json
import re
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
assert "RadiationDamageStages.Max(s => s.SourceDamage)" in code
assert "stage.SourceDamage * multiplier" in code
assert "deadly.SourceDamage * multiplier" in code
assert 'damage.IsValueModifiedFromReference()' in code
assert "RadiationProfileTop + RadiationProfileHeight" in code
assert "private const double RadiationProfileHeight = 388.0;" in code
assert "private const double RadiationControlsNarrowHeight = 272.0;" in code
assert "RadiationBoardHeight(double width)" in code
assert '"RadiationBalance" => RadiationBoardHeight' in code
assert "CFG EFFECT VALUE · NOT VERIFIED HP/s" in code
assert "RADIATION KILLS IN" not in code, "No proven death countdown is available"
assert "RADIATION LEVELS" in code and "RADIATION DAMAGE STAGES" not in code
assert "private const double RadiationProfileHeight = 388.0;" in code
assert "foreach (var threshold in RadiationRingThresholds)" in code
assert "DrawRadiationRingSector(dc, center, radius, fromRad, toRad," in code
assert "ctx.ArcTo(" in code
assert '"SELECTED RAD"' in code and '"99 RAD · DEADLY' in code
bounds = code.split("RadiationRingThresholds =", 1)[1].split("};", 1)[0]
splits = [float(x) for x in re.findall(r"[0-9]+(?:\.[0-9]+)?", bounds)]
assert splits == [10, 20, 30, 40, 50, 60, 70, 80, 90, 99], splits
assert abs(360 * splits[-1] / 100 - 356.4) < 1e-9
assert "var toRad = i == 10 ? 100.0 : RadiationRingThresholds[i];" in code

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
    'private int RadiationStepHit(Point point)',
    'private bool RadiationRailHit(Point point)',
    'private void AdjustRadiationLevel(int step)',
    'Math.Clamp(Math.Round(1.0 + fraction * 99.0), 1.0, 100.0)',
    '_radRingCompleted = true;',
    'return distance >= 100.0 && distance <= 127.0;',
    'new Pen(accent, 13.0)',
    'DrawRadiationRingSector(dc, center, radius, 0.0, currentRad, accent);',
    'DrawRadiationControls(dc, text, muted, border, soft, accent, panel);',
    'private void DrawRadiationControls(',
    '"RAD CONTROLS"', '"RADIATION LEVEL"',
    '"ACTIVE STAGE"', '"CFG DAMAGE / CHARGE"', '"EST. HP ZERO"',
    '"ASSUME 1 CHARGE/s · NO HEALING"', '"FIXED RAD"',
    'var currentRad = _radLevel;',
    'var actualDamage = sourceDamage * multiplier;',
    '" CFG DAMAGE"',
    'stageIndex == 0 ? 1.0 : RadiationRingThresholds[stageIndex - 1]',
    'stageIndex == 10 ? 100.0 : RadiationRingThresholds[stageIndex] - 1.0',
    'double.IsNaN(nextThreshold) ? "FINAL STAGE"',
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

# Fixed-radiation calculation has no build-up and no passive recovery input.
fixed_math = code.split('private double RadiationTimeToZeroHp()', 1)[1].split('private static string RadiationTime(', 1)[0]
assert 'RadiationModelMaxHp / damagePerAssumedCharge' in fixed_math
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
    return None if damage <= 0 else hp / damage

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
assert abs(hp_zero(50, 100, 1) - 100.0 / 2.2) < 1e-9
assert abs(hp_zero(90, 100, 1) - 100.0 / 38.67) < 1e-9
assert abs(hp_zero(99, 100, 1) - 5.0) < 1e-9
assert abs(hp_zero(99, 100, 2) - 2.5) < 1e-9
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
print("      clockwise 1–100 RAD ring + compact rail/buttons, exact CFG damage and")
print("      fixed-RAD full-HP estimate (conditional one charge per second).")
print("NOT VERIFIED: actual permanent-effect damage cadence, healing, armor or gameplay time to death.")
