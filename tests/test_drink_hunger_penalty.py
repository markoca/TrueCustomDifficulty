"""Independent drink hunger source contract (2.1)."""
import json
from pathlib import Path
from decimal import Decimal

ROOT = Path(__file__).resolve().parents[1] / "src" / "TrueCustomDifficulty"
ID = "survival.drinkHungerPenalty"
WATER = "survival.waterHungerRelief"
EXPECTED = {"EnegeticSatiety1": 10, "EnegeticSatiety2": 5,
            "BeerSatiety1": 10, "VodkaSatiety1": 10}
PROFILES = {
    "Anomaly Runner": [
        0.8,
        1.6
    ],
    "Field Medic": [
        1.4,
        0.8
    ],
    "Hardcore+": [
        0.2,
        3
    ],
    "Hardcore": [
        0.4,
        2.4
    ],
    "Immersive": [
        0.6,
        1.6
    ],
    "Mercenary": [
        0.8,
        1.4
    ],
    "Mutant Hunter": [
        0.6,
        1.8
    ],
    "Outcast": [
        0.4,
        2.6
    ],
    "Scavenger": [
        0.8,
        2
    ],
    "Smuggler": [
        1.2,
        1.2
    ],
    "Tactical": [
        0.8,
        1.8
    ],
    "Wanderer": [
        1,
        1.2
    ]
}

def test_drink_hunger_penalty():
    catalog = json.loads((ROOT / "Data/parameters.json").read_text())
    baseline = json.loads((ROOT / "Data/cfg-writer-baseline.json").read_text())
    for param_id, expected, operation in (
        (ID, EXPECTED, "scaleIntegerHalfUpPreserveZero"),
        (WATER, {"WaterSatiety1": -10}, "scale"),
    ):
        p = next(p for p in catalog["parameters"] if p["id"] == param_id)
        assert p["slider"] == {"min": 0, "max": 4, "step": 0.2}
        assert p["vanilla"]["All"] == 1
        assert {t["prototype"] for t in p["targets"]} == set(expected)
        assert all(t["operation"] == operation and t["path"] == "ValueMin|ValueMax" for t in p["targets"])
        points = [e for e in baseline["editPoints"] if e["parameterId"] == param_id]
        assert len(points) == 2 * len(expected)
        assert {(e["prototype"], e["key"], int(e["sourceRaw"])) for e in points} == {(sid, k, n) for sid, n in expected.items() for k in ("ValueMin", "ValueMax")}
        assert all(e["operation"] == operation and e["file"] == "GameData/EffectPrototypes.cfg" for e in points)
        for n in expected.values():
            for i in range(21):
                value = Decimal(n) * Decimal(i) / 5
                assert value == value.to_integral_value()
                assert value <= 0 if n < 0 else value >= 0
        for path in ("Controls/AggregateVisualControl.cs", "ViewModels/MainViewModel.cs", "ViewModels/ParameterViewModel.cs"):
            assert param_id in (ROOT / path).read_text()
    presets = list((ROOT / "Presets").glob("*.json"))
    assert len(presets) == len(PROFILES) == 12
    for path in presets:
        values = json.loads(path.read_text())["values"]
        water, penalty = PROFILES[path.stem]
        assert values[WATER] == {"kind": "slider", "number": water}
        assert values[ID] == {"kind": "slider", "number": penalty}
    ui = (ROOT / "Controls/AggregateVisualControl.cs").read_text()
    assert 'DrawMiniConsumableKnob(dc, waterHunger, "HUNGER RELIEF"' in ui
    assert 'DrawMiniConsumableKnob(dc, drinkHunger, "HUNGER PENALTY"' in ui
    assert 'DrawHungerReadout(reliefGroup, waterHunger, "Water", 10, true' in ui
    assert 'DrawHungerReadout(drinkHungerGroup, drinkHunger, "Limited Energy", 5, false' in ui

if __name__ == "__main__":
    test_drink_hunger_penalty()
    print("Independent drink hunger source contract passed")
