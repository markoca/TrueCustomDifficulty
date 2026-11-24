"""Shared drink hunger penalty source contract (2.1)."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "src" / "TrueCustomDifficulty"
ID = "survival.drinkHungerPenalty"
OP = "scaleIntegerHalfUpPreserveZero"
EXPECTED = {"EnegeticSatiety1": 10, "EnegeticSatiety2": 5,
            "BeerSatiety1": 10, "VodkaSatiety1": 10,
            "Artifact_WeirdKettle_EnegeticSatiety": 10,
            "Artifact_WeirdKettle_BeerSatiety": 5,
            "Artifact_WeirdKettle_VodkaSatiety": 10}

def test_drink_hunger_penalty():
    catalog = json.loads((ROOT / "Data/parameters.json").read_text())
    p = next(p for p in catalog["parameters"] if p["id"] == ID)
    assert p["slider"] == {"min": 0, "max": 4, "step": 0.2}
    assert p["vanilla"]["All"] == 1
    assert {t["prototype"] for t in p["targets"]} == set(EXPECTED)
    assert all(t["operation"] == OP and t["path"] == "ValueMin|ValueMax" for t in p["targets"])
    baseline = json.loads((ROOT / "Data/cfg-writer-baseline.json").read_text())
    points = [e for e in baseline["editPoints"] if e["parameterId"] == ID]
    assert len(points) == 14
    assert {(e["prototype"], e["key"], int(e["sourceRaw"])) for e in points} == {(sid, k, n) for sid, n in EXPECTED.items() for k in ("ValueMin", "ValueMax")}
    assert all(e["operation"] == OP and e["file"] == "GameData/EffectPrototypes.cfg" for e in points)
    for n in EXPECTED.values():
        for i in range(21):
            assert (n * i) % 5 == 0
    presets = list((ROOT / "Presets").glob("*.json"))
    assert len(presets) == 12
    for path in presets:
        assert json.loads(path.read_text())["values"][ID] == {"kind": "slider", "number": 1}
    for path in ("Controls/AggregateVisualControl.cs", "ViewModels/MainViewModel.cs", "ViewModels/ParameterViewModel.cs"):
        assert ID in (ROOT / path).read_text()

if __name__ == "__main__":
    test_drink_hunger_penalty()
    print("Drink hunger penalty source contract passed")
