#!/usr/bin/env python3
"""Factory Food/Drink box rank-aware source balance contract; static checks only."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src/TrueCustomDifficulty"
policy = json.loads((ROOT / "tools/factory_preset_food_drink_boxes_2026_11_23.json").read_text(encoding="utf-8"))
overweight = json.loads((ROOT / "tools/factory_preset_overweight_2026_11_23.json").read_text(encoding="utf-8"))
catalog = {p["id"]: p for p in json.loads((APP / "Data/parameters.json").read_text(encoding="utf-8"))["parameters"]}
baseline = json.loads((APP / "Data/cfg-writer-baseline.json").read_text(encoding="utf-8"))
roots, ranks = policy["rootIds"], policy["rankSuffixes"]
assert len(roots) == len(set(roots)) == 10
assert ranks == ["Experienced", "Veteran", "Master"]
ids = roots + [root + rank for rank in ranks for root in roots]
assert len(ids) == len(set(ids)) == 40 and set(ids) <= set(catalog)
assert len(catalog) == baseline["supportedParameterCount"] == 497
points = [p for p in baseline["editPoints"] if p["parameterId"] in ids]
assert len(points) == 40
assert len({(p["file"], p["prototype"], tuple(p["nodePath"]), p["key"]) for p in points}) == 40
for p in points:
    base = p["parameterId"] in roots
    assert p["operation"] == ("scale" if base else "set"), p
    assert p["key"] == ("Weight" if base else "Chance"), p
    assert p["file"] == "GameData/ItemGeneratorPrototypes.cfg", p

names = {p.stem for p in (APP / "Presets").glob("*.json")}
assert len(names) == 12 and names == set(policy["roles"]) == set(overweight["roles"])
signatures = set()
for name in sorted(names):
    role = policy["roles"][name]
    arrays = [role["newbieWeights"]] + [role["chances"][r] for r in ranks]
    assert len(arrays) == 4 and all(len(a) == 10 for a in arrays), name
    preset = json.loads((APP / "Presets" / (name + ".json")).read_text(encoding="utf-8"))
    assert len(preset["values"]) == len(catalog) and preset["gameVersion"] == "2.1", name
    assert len(role["intent"]) > 50, name
    for idx, vals in enumerate(arrays):
        assert vals[0] + vals[1] > 0, (name, idx)
        assert vals[0] == 0 or any(v > 0 for v in vals[2:6]), (name, idx)
        assert vals[1] == 0 or any(v > 0 for v in vals[6:10]), (name, idx)
        for k, root in enumerate(roots):
            pid = root + ranks[idx-1] if idx else root
            num = vals[k]
            meta = catalog[pid]["slider"]
            assert meta["min"] <= num <= meta["max"], (name, pid)
            step = meta["step"]
            assert abs((num - meta["min"]) / step - round((num - meta["min"]) / step)) < 1e-7, (name, pid)
            assert preset["values"][pid] == {"kind": "slider", "number": num}, (name, pid)
        if idx > 1:
            assert vals[0] >= arrays[idx-1][0] and vals[1] >= arrays[idx-1][1], name
    signature = tuple(tuple(a) for a in arrays)
    assert signature not in signatures, name
    signatures.add(signature)
    assert preset["values"]["stamina.maxCarryWeight"]["number"] == overweight["roles"][name]["maxCarryKg"]
    assert [preset["values"][pid]["number"] for pid in overweight["ids"]] == overweight["roles"][name]["ratios"]
assert policy["roles"]["Hardcore+"]["chances"]["Master"][0] <= .2
assert policy["roles"]["Outcast"]["chances"]["Master"][1] <= .2
assert policy["roles"]["Smuggler"]["chances"]["Master"][8] >= .9
assert policy["roles"]["Anomaly Runner"]["chances"]["Master"][9] >= .8
assert policy["roles"]["Scavenger"]["newbieWeights"][6] >= 1.5
print("PASS: 480 exact profile values, 40 existing CFG coordinates, native source grids")
print("PASS: all 12 role signatures unique, active branch safety and overweight roles preserved")
print("NOT TESTED: live WPF/Wine compilation and spawned game loot")
