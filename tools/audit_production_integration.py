#!/usr/bin/env python3
"""Production integration audit for True Custom Difficulty.

Checks the live catalog/UI/writer contract rather than gameplay provenance:
- every player-facing catalog control is rendered by the generic UI path;
- dependency parents are valid visible toggles;
- all catalog parameter IDs have a production writer route;
- disabled dependent controls are excluded from production CFG output;
- canonical solution/publish scripts build only src/TrueCustomDifficulty.
"""
from __future__ import annotations

import argparse
import csv
import json
import sys
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "TrueCustomDifficulty"
CATALOG = SRC / "Data" / "parameters.json"
BASELINE = SRC / "Data" / "cfg-writer-baseline.json"

# Locked to the current source catalog. Update only after a
# provenance-backed change to the catalog and writer baseline in the SAME commit.
EXPECTED_VISIBLE = 150
EXPECTED_HIDDEN = 86
EXPECTED_TOTAL = 236
EXPECTED_CONTROLS = {"slider": 136, "toggle": 12, "choiceSlider": 1, "range": 1}
EXPECTED_TAB_COUNTS = {
    "combat": 17,
    "health": 13,
    "stamina": 7,
    "survival": 32,
    "economy": 17,
    "equipment": 4,
    "master": 20,
    "environment": 7,
    "loot": 31,
    "alife": 2,
}
EXPECTED_DEPENDENCIES = 16


def die(msg: str) -> None:
    raise SystemExit(f"FAIL: {msg}")


def load_json(path: Path):
    return json.loads(path.read_text(encoding="utf-8"))


def supported_ids(baseline: dict) -> set[str]:
    result = {p["parameterId"] for p in baseline.get("editPoints", [])}
    special = baseline.get("special", {})
    result.update(x["parameterId"] for x in special.get("listMembership", []))
    result.update(x["parameterId"] for x in special.get("setList", []))
    result.update(special.get("psyParameterKeys", {}).keys())

    for section, keys in {
        "cheapStashProduction": ("ammoQuantityParameterId", "grenadeChanceParameterId", "grenadeQuantityParameterId"),
        "mediumStashAmmoProduction": ("parameterId",),
        "expensiveStashProduction": ("ammoQuantityParameterId", "couponChanceParameterId"),
        "npcMedicalLootProduction": ("parameterId",),
        "traderMinBuyCondition": ("parameterId",),
        "destructibleAmmoProduction": ("parameterId",),
        "destructibleGrenadeProduction": ("parameterId",),
    }.items():
        data = special.get(section, {})
        for key in keys:
            value = data.get(key)
            if value:
                result.add(value)

    result.update(
        value for value in (special.get("destructibleAmmoProduction", {}).get("weightParameterIds", {}) or {}).values()
        if value
    )

    crate_spawn = special.get("crateSpawnProduction", {}) or {}
    for row in crate_spawn.get("categories", []):
        value = row.get("parameterId")
        if value:
            result.add(value)
    value = crate_spawn.get("removalPercentParameterId")
    if value:
        result.add(value)

    # Direct special writer routes that intentionally do not use generic editPoints.
    result.update({
        "loot.worldWeaponShuffle",
        "loot.worldWeaponDensity",
        "loot.worldAttachmentShuffle",
        "loot.worldDetectors",
        "loot.worldF1GrenadeBoxes",
        "loot.blueprintShuffle",
        "loot.looseWorldLootRemoved",
    })
    return result


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", type=Path, help="Optional evidence output directory")
    args = parser.parse_args()

    catalog = load_json(CATALOG)
    baseline = load_json(BASELINE)
    params = catalog["parameters"]
    by_id = {p["id"]: p for p in params}
    visible = [p for p in params if p.get("exposeInUi", True)]
    hidden = [p for p in params if not p.get("exposeInUi", True)]

    if len(params) != EXPECTED_TOTAL:
        die(f"catalog count {len(params)} != {EXPECTED_TOTAL}")
    if len(visible) != EXPECTED_VISIBLE or len(hidden) != EXPECTED_HIDDEN:
        die(f"visibility split is {len(visible)} visible / {len(hidden)} hidden")

    controls = Counter(p["control"] for p in visible)
    if dict(controls) != EXPECTED_CONTROLS:
        die(f"visible control split drifted: {dict(controls)}")

    tab_counts = Counter(p["tab"] for p in visible)
    if dict(tab_counts) != EXPECTED_TAB_COUNTS:
        die(f"visible tab counts drifted: {dict(tab_counts)}")

    dependencies = [p for p in visible if p.get("requiresToggle")]
    if len(dependencies) != EXPECTED_DEPENDENCIES:
        die(f"dependency count {len(dependencies)} != {EXPECTED_DEPENDENCIES}")
    for child in dependencies:
        parent = by_id.get(child["requiresToggle"])
        if not parent:
            die(f"{child['id']} requires missing {child['requiresToggle']}")
        if parent["control"] != "toggle" or not parent.get("exposeInUi", True):
            die(f"{child['id']} requires non-visible/non-toggle parent {parent['id']}")
        if child["tab"] != parent["tab"]:
            die(f"dependency crosses tabs: {child['id']} -> {parent['id']}")

    supported = supported_ids(baseline)
    catalog_ids = {p["id"] for p in params}
    if baseline.get("deferredParameterIds"):
        die(f"production writer still has deferred IDs: {baseline['deferredParameterIds']}")
    if supported != catalog_ids:
        missing = sorted(catalog_ids - supported)
        extra = sorted(supported - catalog_ids)
        die(f"writer route mismatch; missing={missing}, extra={extra}")

    main_vm = (SRC / "ViewModels" / "MainViewModel.cs").read_text(encoding="utf-8")
    param_vm = (SRC / "ViewModels" / "ParameterViewModel.cs").read_text(encoding="utf-8")
    tab_vm = (SRC / "ViewModels" / "TabViewModel.cs").read_text(encoding="utf-8")
    writer = (SRC / "Services" / "CfgPatchWriterService.cs").read_text(encoding="utf-8")
    catalog_service = (SRC / "Services" / "ParameterCatalogService.cs").read_text(encoding="utf-8")
    xaml = (SRC / "MainWindow.xaml").read_text(encoding="utf-8")

    required_source_contracts = {
        "catalog-driven visible controls": ".Where(p => p.ExposeInUi" in main_vm,
        "ammo distribution aggregate radar": "AmmoDistributionRadarControl" in xaml and "ammoDistributionParameterIds" in main_vm,
        "catalog-driven tabs": "foreach (var tab in catalog.Tabs.OrderBy(t => t.Order))" in main_vm,
        "active-only writer": "p.IsModified && p.IsControlEnabled" in writer,
        "visible selection repair": "EnsureVisibleSelection" in main_vm and "public void EnsureVisibleSelection()" in tab_vm,
        "dependency visibility notification": "OnPropertyChanged(nameof(IsControlVisible));" in param_vm,
        "startup parameter-shape validation": "ValidateParameterShape(parameter);" in catalog_service,
        "startup dependency validation": "requires unknown toggle" in catalog_service,
        "slider rendering": 'Binding IsSlider' in xaml and 'Binding CurrentNumber' in xaml,
        "toggle rendering": 'Binding IsToggle' in xaml and 'Binding CurrentBoolean' in xaml,
        "choice rendering": 'Binding IsChoiceSlider' in xaml and 'Binding ChoiceIndex' in xaml,
        "range rendering": 'Binding IsRange' in xaml and 'Binding CurrentMinNumber' in xaml and 'Binding CurrentMaxNumber' in xaml,
        "dependency visibility rendering": 'Binding IsControlVisible' in xaml,
    }
    failed = [name for name, ok in required_source_contracts.items() if not ok]
    if failed:
        die("missing production source contracts: " + ", ".join(failed))

    canonical = r"src\TrueCustomDifficulty\TrueCustomDifficulty.csproj"
    sln = (ROOT / "TrueCustomDifficulty.sln").read_text(encoding="utf-8")
    ps1 = (ROOT / "publish.ps1").read_text(encoding="utf-8")
    sh = (ROOT / "publish.sh").read_text(encoding="utf-8")
    if canonical not in sln:
        die("solution does not target canonical src project")
    if 'src\\TrueCustomDifficulty\\TrueCustomDifficulty.csproj' not in ps1:
        die("publish.ps1 does not target canonical src project")
    if 'src/TrueCustomDifficulty/TrueCustomDifficulty.csproj' not in sh:
        die("publish.sh does not target canonical src project")

    legacy_source = []
    for path in ROOT.rglob("*"):
        if not path.is_file() or path.is_relative_to(SRC):
            continue
        if path.suffix.lower() in {".cs", ".xaml", ".csproj"}:
            legacy_source.append(path.relative_to(ROOT).as_posix())
    if legacy_source:
        die("legacy C#/XAML project mirrors remain outside canonical src tree: " + ", ".join(sorted(legacy_source)))

    rows = []
    for p in params:
        rows.append({
            "parameterId": p["id"],
            "tab": p["tab"],
            "control": p["control"],
            "exposeInUi": str(p.get("exposeInUi", True)).lower(),
            "supportStatus": p.get("support", {}).get("status", ""),
            "requiresToggle": p.get("requiresToggle", ""),
            "writerRouted": "true" if p["id"] in supported else "false",
        })

    summary = {
        "version": catalog["app"]["version"],
        "catalogParameters": len(params),
        "visibleControls": len(visible),
        "hiddenAuditOnly": len(hidden),
        "visibleControlTypes": dict(sorted(controls.items())),
        "visibleTabCounts": dict(sorted(tab_counts.items())),
        "dependentControls": len(dependencies),
        "writerRouted": len(supported),
        "deferredWriterParameters": len(baseline.get("deferredParameterIds", [])),
        "productionDependencySuppression": True,
        "canonicalBuildProject": "src/TrueCustomDifficulty/TrueCustomDifficulty.csproj",
        "status": "PASS",
    }

    if args.out:
        out = args.out
        out.mkdir(parents=True, exist_ok=True)
        (out / "summary.json").write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")
        with (out / "controls.csv").open("w", encoding="utf-8", newline="") as f:
            w = csv.DictWriter(f, fieldnames=rows[0].keys())
            w.writeheader(); w.writerows(rows)
        dep_rows = [
            {"child": p["id"], "parentToggle": p["requiresToggle"], "tab": p["tab"]}
            for p in dependencies
        ]
        with (out / "dependencies.csv").open("w", encoding="utf-8", newline="") as f:
            w = csv.DictWriter(f, fieldnames=["child", "parentToggle", "tab"])
            w.writeheader(); w.writerows(dep_rows)
        (out / "README.md").write_text(
            "# Production integration evidence\n\n"
            "Generated by `tools/audit_production_integration.py`. The evidence locks the live "
            "catalog-to-UI-to-writer integration for the production application.\n",
            encoding="utf-8",
        )

    print("True Custom Difficulty — production integration audit")
    print(f"[OK] catalog: {len(params)} parameters = {len(visible)} visible + {len(hidden)} hidden audit-only")
    print(f"[OK] visible controls: {controls.get('slider', 0)} sliders + {controls.get('toggle', 0)} toggles + {controls.get('choiceSlider', 0)} choice slider + {controls.get('range', 0)} range")
    print(f"[OK] tabs: {len(tab_counts)} / all visible controls assigned")
    print(f"[OK] dependencies: {len(dependencies)} child controls -> valid visible toggle parents")
    print(f"[OK] writer routes: {len(supported)}/{len(params)}; deferred=0")
    print("[OK] disabled dependent controls are excluded from production CFG output")
    print("[OK] canonical solution/publish project: src/TrueCustomDifficulty")
    print("PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
