#!/usr/bin/env python3
"""STALKER 2.1 direct loose binocular/NVG removal contract; source-static only."""
import csv
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src/TrueCustomDifficulty"
ID = "loot.looseOpticsRemoved"
CATALOG = json.loads((APP / "Data/parameters.json").read_text(encoding="utf-8"))["parameters"]
BASELINE = json.loads((APP / "Data/cfg-writer-baseline.json").read_text(encoding="utf-8"))
assert len(CATALOG) == BASELINE["supportedParameterCount"] == 497
by_id = {row["id"]: row for row in CATALOG}
row = by_id[ID]
assert row["group"] == "World Loot" and row["control"] == "toggle"
assert row["vanilla"] == {"All": False}
assert row["support"]["status"] == "runtime_test_required"
assert row["exposeInUi"] is True
assert BASELINE["special"]["looseOpticsRemoved"] == {
    "parameterId": ID,
    "manifestResource": "TrueCustomDifficulty.Data.loose-optics-direct-manifest.csv",
    "actors": 17,
}
assert not any(p["parameterId"] == ID for p in BASELINE["editPoints"])
with (APP / "Data/loose-optics-direct-manifest.csv").open(encoding="utf-8", newline="") as stream:
    sources = list(csv.DictReader(stream))
assert len(sources) == 17
assert len({x["actor_sid"] for x in sources}) == 17
assert all(re.fullmatch("[A-F0-9]{32}", x["actor_sid"]) for x in sources)
assert sum(x["scope"] == "baseGame" for x in sources) == 15
assert sum(x["scope"] == "dlc1" for x in sources) == 2
assert sum(x["item_sid"].startswith("Binoculars_") for x in sources) == 13
assert sum(x["item_sid"].startswith("NVG_Gen") for x in sources) == 4
for x in sources:
    assert x["level_name"] == "WorldMap_WP"
    assert x["scope"] in ("baseGame", "dlc1")
    assert x["source_file"] == (
        "DLCGameData/DLC1/SpawnActorPrototypes.cfg"
        if x["scope"] == "dlc1" else "GameData/SpawnActorPrototypes.cfg")
    assert x["item_sid"] in (
        "Binoculars_01", "Binoculars_02", "Binoculars_03",
        "NVG_Gen1", "NVG_Gen2", "NVG_Gen3")
# Never remove the same physical actor through a second loot domain.
from itertools import chain
other = set()
for path in (APP / "Data").glob("*manifest.csv"):
    if path.name == "loose-optics-direct-manifest.csv":
        continue
    other |= set(re.findall(r"\b[0-9A-F]{32}\b", path.read_text(encoding="utf-8")))
assert not ({x["actor_sid"] for x in sources} & other)

part = (APP / "Services/CfgPatchWriterService.LooseOpticsRemovals.cs").read_text()
writer = (APP / "Services/CfgPatchWriterService.cs").read_text()
csproj = (APP / "TrueCustomDifficulty.csproj").read_text()
ui = (APP / "Controls/AggregateVisualControl.cs").read_text()
vm = (APP / "ViewModels/MainViewModel.cs").read_text()
pvm = (APP / "ViewModels/ParameterViewModel.cs").read_text()
assert "WriteLooseOpticsRemovals(" in writer and "supported.Add(LooseOpticsRemovedId);" in writer
assert "looseOpticsFiles" in writer and "+ looseOpticsFiles;" in writer
assert "rows.Count != 17" in part and ": removenode" in part
assert "IsScopeApplicable" in part
assert "Data\\loose-optics-direct-manifest.csv" in csproj
assert '(optics, "Binoculars / NVG")' in ui
assert "if (content >= 560.0)" in ui  # no clipped full-word caption on smaller widths
assert "Binocs / NVG" not in ui
assert "WorldRemoveCardHeight" in ui and "4.0 * WorldListRow" in ui
assert '"loot.looseOpticsRemoved"' in vm and '["loot.looseOpticsRemoved"] = "WorldLootLayout"' in pvm

presets = sorted((APP / "Presets").glob("*.json"))
assert len(presets) == 12
enabled = {"Hardcore", "Hardcore+", "Outcast"}
for path in presets:
    data = json.loads(path.read_text(encoding="utf-8"))
    assert len(data["values"]) == len(CATALOG)
    assert data["values"][ID] == {"kind": "toggle", "boolean": path.stem in enabled}
print("PASS: STALKER 2.1 audited 17 unique direct binocular/NVG actors, 15 base + 2 DLC1")
print("PASS: one independent toggle, 497 catalog sources, correct factory policy and writer/UI invariants")
print("NOT TESTED: Unreal or existing-save behavior")
