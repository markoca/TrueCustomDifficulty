#!/usr/bin/env python3
"""Check radiation stage preview against independently audited vanilla CFG rows.

The source-value preview is NOT a runtime exposure/death-time simulation.
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
assert "RadiationProfileTop + RadiationProfileHeight + 12.0" in code
assert '"RadiationBalance" => RadiationBoardHeight' in code
assert "CFG EFFECT VALUES · NOT A DEATH TIMER" in code
assert "RADIATION KILLS IN" not in code, "No proven death countdown is available"
assert "RADIATION LEVELS" in code and "RADIATION DAMAGE STAGES" not in code
assert "private const double RadiationProfileHeight = 360.0;" in code
assert "foreach (var threshold in RadiationRingThresholds)" in code
assert "DrawRadiationRingSector(dc, center, radius, fromRad, toRad," in code
assert "ctx.ArcTo(" in code
assert "HP LOSS AT 1 RAD" in code and "99 RAD · DEADLY" in code
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

print("PASS: 11 original radiation damage stages, 22 exact writer leaves,")
print("      source-multiplier projection and distinct DEADLY source validated.")
print("      360-degree ring with exact audited 10–99 RAD thresholds.")
print("NOT VERIFIED: damage-event frequency, stage transitions, exposure or death ETA; Wine visual acceptance.")
