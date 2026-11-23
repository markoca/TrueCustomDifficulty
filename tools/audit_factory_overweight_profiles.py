#!/usr/bin/env python3
"""Exact 12-role Overweight ratio and capacity gate (no game runtime simulation)."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src/TrueCustomDifficulty"
MATRIX = json.loads((ROOT / "tools/factory_preset_overweight_2026_11_23.json").read_text(encoding="utf-8"))
CATALOG = json.loads((APP / "Data/parameters.json").read_text(encoding="utf-8"))
BY_ID = {p["id"]: p for p in CATALOG["parameters"]}
PRESETS = sorted((APP / "Presets").glob("*.json"))
IDS = [
    "stamina.overweightLightThreshold",
    "stamina.overweightMediumThreshold",
    "stamina.overweightHeavyThreshold",
    "stamina.overweightCriticalThreshold",
]
assert MATRIX["ids"] == IDS and MATRIX["vanillaRatios"] == [0.625, 0.75, 0.875, 1]
assert MATRIX["gameVersion"] == CATALOG["source"]["gameVersion"] == "2.1"
assert len(PRESETS) == len(MATRIX["roles"]) == 12
assert {p.stem for p in PRESETS} == set(MATRIX["roles"])
assert len({tuple(role["ratios"]) for role in MATRIX["roles"].values()}) == 12
for id in IDS:
    source = BY_ID[id]
    assert source["control"] == "slider" and source["slider"]["stepMode"] == "absolute", id
    assert source["slider"]["min"] == .1 and source["slider"]["max"] == 1 and source["slider"]["step"] == .025, id

for path in PRESETS:
    preset = json.loads(path.read_text(encoding="utf-8"))
    role = MATRIX["roles"][path.stem]
    values = preset["values"]
    assert preset["name"] == path.stem and preset["gameVersion"] == "2.1"
    assert set(values) == set(BY_ID), path.name
    carry = values["stamina.maxCarryWeight"]
    assert carry == {"kind": "slider", "number": role["maxCarryKg"]}, path.name
    assert 20 <= carry["number"] <= 160 and (carry["number"] - 20) % 5 == 0, path.name
    ratios = role["ratios"]
    assert len(ratios) == 4 and role["intent"], path.name
    assert ratios != MATRIX["vanillaRatios"], path.name
    for index, id in enumerate(IDS):
        ratio = ratios[index]
        assert values[id] == {"kind": "slider", "number": ratio}, (path.name, id)
        assert .1 <= ratio <= 1 and abs((ratio - .1) / .025 - round((ratio - .1) / .025)) < 1e-8, (path.name, id)
        if index:
            assert ratio - ratios[index - 1] >= .025 - 1e-8, (path.name, id)
        kg = ratio * carry["number"]
        assert 0 < kg <= carry["number"] + 1e-9, (path.name, id)
    # Source effects do not change and all four stage fields are still serialized.
    assert len({ratios[i] * carry["number"] for i in range(4)}) == 4, path.name

writer = (APP / "Services/CfgPatchWriterService.cs").read_text(encoding="utf-8")
assert 'WriteOverweightThresholds(modifiedById, allById, documents, writtenParameters)' in writer
assert 'var kg = fractions.Select(fraction => maxCarry * fraction).ToArray()' in writer
assert '"WeightEffectParams", $"[{3 - i}]"' in writer
assert '"CriticalEffectStartUI", FormatLikeSource(kg[2], "70")' in writer
print("PASS: 12 role-specific Overweight profiles, 48 source ratios, unique 2.5%-grid stage ladders, unchanged capacities, and CFG writer mapping")
