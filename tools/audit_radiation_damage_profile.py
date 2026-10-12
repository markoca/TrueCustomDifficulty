#!/usr/bin/env python3
"""Check radiation stage preview against independently audited vanilla CFG rows.

Includes a conditional one-charge-per-second HP model, not a proven in-game death timer.
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
assert "private const double RadiationSimNarrowHeight = 184.0;" in code
assert "private const double RadiationTimelineHeight = 202.0;" in code
assert "RadiationBoardHeight(double width)" in code
assert '"RadiationBalance" => RadiationBoardHeight' in code
assert "CFG EFFECT VALUES · NOT A DEATH TIMER" in code
assert "RADIATION KILLS IN" not in code, "No proven death countdown is available"
assert "RADIATION LEVELS" in code and "RADIATION DAMAGE STAGES" not in code
assert "private const double RadiationProfileHeight = 388.0;" in code
assert "foreach (var threshold in RadiationRingThresholds)" in code
assert "DrawRadiationRingSector(dc, center, radius, fromRad, toRad," in code
assert "ctx.ArcTo(" in code
assert '"SIMULATED"' in code and '"99 RAD · DEADLY"' in code
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


# Preview-only choices, scrub/playback and explicitly hypothetical HP-zero ETA cannot add CFG sources.
for token in (
    'private int _radField = 1;',
    'private int _radStart;',
    'private bool _radRecovery = true;',
    'private int RadiationSimHit(Point point)',
    'AdvanceRadiationSim(preview, 1);',
    'AdvanceRadiationSim(preview, Math.Sign(e.Delta));',
    '_radSimFocus = -1;',
    'ToolTip = "Model only: click or scroll to cycle · Right-click restores preview default.";',
    'AdvanceRadiationSim(_radSimFocus, simulationDirection);',
    'DrawRadiationSimulation(dc, text, muted, border, soft, accent, panel);',
    '"RADIATION SIMULATION"', '"RADIATION TIMELINE"',
    '"MODEL ONLY"', '"TO 99 RAD"', '"NO NET GAIN"',
    '"MODEL HP ZERO"', '"HYPOTHETICAL · NO HEALING"',
    '"ASSUME 1 CHARGE/s · ENGINE CADENCE UNVERIFIED"',
    '"ASSUME 1 CHARGE/s · DIRECT HAZARD EXCLUDED"',
    'field.PerSecond * buildUp - recovery',
    '"RadiationBalance" => RadiationBoardHeight(width)',
    'private double RadiationInitialRad =>',
    'private double RadiationCurrentRad =>',
    'private static double RadiationHpLoss(',
    'private static double RadiationHpZeroSeconds(',
    'private double RadiationModelMaxHp =>',
    'private bool RadiationRingHit(Point point)',
    'private void ScrubRadiationRing(Point point)',
    'private void ToggleRadiationPlayback()',
    'private void TickRadiationPlayback()',
    'private Rect RadiationPlaybackButton(int index)',
    'new Pen(accent, 4.5)',
    'StopRadiationPlayback();',
    'ResetRadiationPreview(clearCustomStart: true);',
    '"PLAY ×10"',
    '"PAUSE"',
    '"RESET"',
    'Math.Min(100.0,',
    'health.maxHP'
):
    assert token in code, "Simulation renderer/interaction drift: " + token

field_block = code.split("RadiationFields =", 1)[1].split("};", 1)[0]
field_defs = re.findall(r'\("([^"]+)",\s*([0-9]+(?:\.[0-9]+)?),\s*(true|false)\)', field_block)
expected_fields = [
    ("LIGHT", 1, False), ("MEDIUM", 3, False), ("STRONG", 6, False),
    ("DEADLY", 33, True), ("RADBLOCK", 50, True),
    ("CUSTOM", 1, False), ("TOPAZ", 1, False)
]
assert [(name, float(rate), hazard == "true") for name, rate, hazard in field_defs] == expected_fields
start_block = code.split("RadiationStarts =", 1)[1].split("};", 1)[0]
assert [float(v) for v in re.findall(r"[0-9]+(?:\.[0-9]+)?", start_block)] == [0,25,50,75]

# Mathematical regression for the explicitly ASSUMED constant-field model.
def hypothetical_to_99(rate, buildup, passive, initial):
    net = max(0, rate * max(0, buildup) - max(0, passive))
    return None if net == 0 else (99 - initial) / net
assert abs(hypothetical_to_99(3, 1, .05, 0) - 99/2.95) < 1e-9
assert abs(hypothetical_to_99(6, 1.5, .05, 50) - 49/8.95) < 1e-9
assert hypothetical_to_99(1, 0, .05, 0) is None

# Independent model regression: this is illustrative arithmetic, not engine validation.
from math import inf

stage_onsets = [1, 10, 20, 30, 40, 50, 60, 70, 80, 90, 99]
stage_dps = [float(value) for _, _, value in stages]

def model_damage_rate(rad_value, multiplier):
    active = [i for i, threshold in enumerate(stage_onsets) if rad_value >= threshold]
    return 0 if not active else stage_dps[active[-1]] * multiplier

def model_loss(start, elapsed, net, multiplier):
    if multiplier == 0 or elapsed <= 0:
        return 0
    rad_value = start
    remaining = elapsed
    loss = 0
    while remaining > 0:
        if net <= 0 or rad_value >= 100:
            return loss + model_damage_rate(rad_value, multiplier) * remaining
        boundary = next((v for v in stage_onsets + [100] if v > rad_value), 100)
        period = min(remaining, (boundary - rad_value) / net)
        loss += model_damage_rate(rad_value, multiplier) * period
        remaining -= period
        rad_value = boundary
    return loss

assert abs(model_loss(0, 2, 10, 1) - .48) < 1e-8  # 0–1: 0, 1–10: .18, 10–20: .30
assert abs(model_loss(99, 5, 0, 1) - 100) < 1e-8
assert abs(model_loss(90, 1, 0, 1) - 38.67) < 1e-8
assert model_loss(0, 999, 0, 1) == 0
assert model_loss(100, 999, 100, 0) == 0
assert stage_dps[-1] < stage_dps[-2]  # No invented monotonic Deadly damage curve.
assert '"RadiationBalance" => RadiationBoardHeight(width)' in code
assert '"survival.radiationDamage"' in code and '"survival.radiationRecovery"' in code

print("PASS: 11 original radiation damage stages, 22 exact writer leaves,")
print("      source-multiplier projection and distinct DEADLY source validated.")
print("      Interactive 0–100 RAD ring, scenario playback, conditional HP-zero model and timeline.")
print("NOT VERIFIED: engine cadence/falloff, direct-field hazards, protection or in-game death time; Wine visual acceptance.")
