#!/usr/bin/env python3
"""Four source-native food hunger-relief UI/CFG invariants (static, not gameplay)."""
from __future__ import annotations

import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TrueCustomDifficulty"
DATA = SRC / "Data"
DOCS = ROOT / "docs"

FOOD = (
    ("survival.breadSatiety", "BreadSatiety2", 20, 40, "survival-food-satiety-1", 0, 10),
    ("survival.sausageSatiety", "SausageSatiety2", 30, 60, "survival-food-satiety-1", 1, 20),
    ("survival.cannedFoodSatiety", "CannedSatiety3", 50, 100, "survival-food-satiety-2", 0, 30),
    ("survival.milkSatiety", "MilkSatiety4", 100, 100, "survival-food-satiety-2", 1, 35),
)


def require(condition: bool, explanation: str) -> None:
    if not condition:
        raise AssertionError(explanation)


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def main() -> int:
    catalog = json.loads(read(DATA / "parameters.json"))
    baseline = json.loads(read(DATA / "cfg-writer-baseline.json"))
    params = {entry["id"]: entry for entry in catalog["parameters"]}
    edit_points = baseline["editPoints"]
    layout = read(SRC / "Infrastructure" / "ResponsiveParameterGridPanel.cs")
    vm = read(SRC / "ViewModels" / "MainViewModel.cs")
    writer = read(SRC / "Services" / "CfgPatchWriterService.cs")
    survival_test = read(ROOT / "tests" / "SurvivalPatchContract" / "Program.cs")
    factory_files = list((SRC / "Presets").glob("*.json"))

    require(len(catalog["parameters"]) == len(params) == 287, "source count / duplicate IDs")
    require(sum(p.get("exposeInUi", True) for p in params.values()) == 182,
            "current visible-source census")
    require(baseline["supportedParameterCount"] == 287 and len(edit_points) == 1238,
            "current full direct CFG-writer census")
    require("MaxHungerPoints = 100" in read(DOCS / "SURVIVAL-MAPPING.md"),
            "source documented hunger ceiling must remain 100")
    require('"negateSet" => FormatLikeSource(-parameter.CurrentNumber, point.SourceRaw)' in writer,
            "native negative HungerPoints writer transform unavailable")

    owned = set()
    for pid, proto, original, maximum, row, column, order in FOOD:
        p = params[pid]
        require(p["tab"] == "survival" and p["group"] == "Hunger"
                and p.get("exposeInUi") is True and p["control"] == "slider",
                pid + ": source not independently rendered under Hunger")
        require(p["valueKind"] == "raw" and p["display"]["primary"] == "raw"
                and p["display"]["unit"] == "pts",
                pid + ": number incorrectly represents a rate or multiplier")
        require(p["vanilla"] == {"All": original} and p["default"]["mode"] == "fixedVanilla",
                pid + ": positive UI vanilla/reference is incorrect")
        require(p["slider"] == {"min": 0, "max": maximum, "step": 5, "stepMode": "absolute"},
                pid + ": unsafe or non-native slider bounds")
        require(p["support"]["status"] == "runtime_test_required",
                pid + ": no gameplay test authorized confirmed status")
        require(p["targets"] == [{
            "scope": "baseGame", "file": "GameData/EffectPrototypes.cfg",
            "prototype": proto, "path": "ValueMin|ValueMax", "operation": "negateSet"
        }], pid + ": catalog source target drift")

        points = [e for e in edit_points if e["parameterId"] == pid]
        require(len(points) == 2 and {e["key"] for e in points} == {"ValueMin", "ValueMax"},
                pid + ": exactly two signed native leaves required")
        for entry in points:
            key = (entry["file"], entry["prototype"], tuple(entry["nodePath"]), entry["key"])
            require(key not in owned, pid + ": duplicate food CFG leaf")
            owned.add(key)
            require(entry["scope"] == "baseGame"
                    and entry["file"] == "GameData/EffectPrototypes.cfg"
                    and entry["prototype"] == proto and entry["nodePath"] == []
                    and entry["sourceRaw"] == str(-original)
                    and entry["operation"] == "negateSet"
                    and entry.get("difficultySid", "") == "",
                    pid + ": wrong source, signed value or difficulty")
        require(f'["{pid}"] = new("{row}", {column})' in layout,
                pid + ": compact two-column layout drift")
        require(f'"{pid}" => {order}' in vm,
                pid + ": Hunger card order must be explicit, not alphabetic fallback")
        require(f'"{pid}"' in survival_test, pid + ": absent native Windows writer coverage")

    require(len(owned) == 8, "only eight original food edit points are permitted")
    require(all("WeirdKettle" not in str(x) for x in owned),
            "alternative effects must remain independent")
    require('FoodSatietyBoundaryWriterContract();' in survival_test
            and '"Veteran-Four-Normal-Food-Effects"' in survival_test
            and 'CheckWriter(vm, new HashSet<string>(StringComparer.Ordinal) { id }' in survival_test,
            "missing zero/max, isolation or shared BPatch regression")
    require(len(factory_files) == 12, "expected 12 factory playstyle profiles")
    for preset_file in factory_files:
        profile = json.loads(read(preset_file))
        require(len(profile["values"]) == 287, f"{preset_file.name}: incomplete source keys")
        for pid, _, reference, _, _, _, _ in FOOD:
            require(profile["values"][pid] == {"kind": "slider", "number": reference},
                    f"{preset_file.name}: do not pre-tune food strength without a balance request")
    migration = read(SRC / "Services" / "PresetDirectoryService.cs")
    require("PreFoodSatietyFactoryBlobs" in migration
            and "IsUnmodifiedPreviousFactory" in migration,
            "preserve user-edited presets / exact pristine-stock migration")

    for lock_name in ("UI-DESIGN-LOCK.md", "VISUAL-CONTROL-SYSTEM.md"):
        lock = read(DOCS / lock_name)
        require("CURRENT: four native Food Hunger Relief sliders" in lock
                and "Condensed Milk 0–100" in lock
                and "alphabetic fallback" in lock,
                lock_name + ": implementation/design lock disagreement")
    for document in ("SURVIVAL-MAPPING.md", "SURVIVAL-CONTROL-DEEP-AUDIT.md"):
        text = read(DOCS / document)
        require("| `survival.milkSatiety` | `MilkSatiety4` | -100 / -100 | 100 pts | 5 pts |" in text,
                document + ": milk ceiling documentation drift")

    print("PASS: 4 individual food source sliders, 8 exact negative CFG leaves,")
    print("      0–100 hunger ceiling, 2×2 compact order, 12 neutral presets,")
    print("      production writer and boundary/merged-patch test hooks.")
    print("NOT RUN: game 2.0.5 original raw effect ZIP comparison, .NET/Wine and in-game testing.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
