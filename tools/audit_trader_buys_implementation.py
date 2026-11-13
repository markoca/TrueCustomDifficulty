#!/usr/bin/env python3
"""Fail-closed static 5x8 Trader Buys implementation/source guard.

Does NOT compile WPF or execute STALKER 2. The authoritative compiled writer
and actual game must still be tested through the C# and Wine contracts.
"""
from __future__ import annotations
import csv
import json
from collections import Counter
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src/TrueCustomDifficulty"
DATA = SRC / "Data"
ROLES = {
    "general": ("GENERAL_TRADER", 33, ()),
    "medic": ("MEDIC", 15, ("Weapon", "Armor", "Ammo", "Grenade", "Attach", "MutantLoot")),
    "technician": ("TECHNICIAN", 16, ("Consumable", "Ammo", "Grenade")),
    "bartender": ("BARTENDER", 13, ("MutantLoot",)),
    "genericNpc": ("GENERIC_NPC_TRADER", 9, ("Weapon", "Armor", "MutantLoot")),
}
CATEGORIES = {
    "weapon": "Weapon",
    "armor": "Armor",
    "attach": "Attach",
    "ammo": "Ammo",
    "grenade": "Grenade",
    "artifact": "Artifact",
    "consumable": "Consumable",
    "mutantLoot": "MutantLoot",
}
ids = {f"economy.traderBuys.{r}.{c}" for r in ROLES for c in CATEGORIES}
catalog = json.loads((DATA / "parameters.json").read_text(encoding="utf-8"))
base = json.loads((DATA / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
all_rows = catalog["parameters"]
by = {p["id"]: p for p in all_rows}
assert len(all_rows) == len(by) == base["supportedParameterCount"] == 359
assert len(ids) == 40 and set(by).intersection(ids) == ids
assert sum(p.get("exposeInUi", True) for p in all_rows) == 185
assert sum(p.get("exposeInUi") is False for p in all_rows) == 174
assert {p["id"] for p in all_rows if p.get("exposeInUi", True) and p["id"] in ids} == {
    "economy.traderBuys.general.weapon"}
for role, (_, _, denied) in ROLES.items():
    for cat, native in CATEGORIES.items():
        p = by[f"economy.traderBuys.{role}.{cat}"]
        assert p["control"] == "toggle" and p["valueKind"] == "boolean"
        assert p["group"] == "Trader Buys" and p["tab"] == "economy"
        assert p["vanilla"] == {"All": native not in denied}
        assert p["targets"][0]["itemType"] == "EItemType::" + native
        assert p["support"]["status"] == "runtime_test_required"
spec = base["special"]["traderBuys"]
assert spec["expectedProfiles"] == 86
assert spec["manifestResource"] == "TrueCustomDifficulty.Data.trader-buy-policy-manifest.csv"
assert len(spec["parameterIds"]) == 40 and set(spec["parameterIds"]) == ids

with (DATA / "trader-buy-policy-manifest.csv").open(newline="", encoding="utf-8") as f:
    reader = csv.DictReader(f)
    assert reader.fieldnames == ["scope", "file", "prototype", "role", "state", "blocked_types"]
    manifest = list(reader)
with (ROOT / "docs/generators/trader-buy-limits-86.csv").open(newline="", encoding="utf-8") as f:
    native = list(csv.DictReader(f))
assert len(manifest) == len(native) == 86
src = {(r["scope"], r["trade_sid"]): r for r in native}
assert len(src) == 86
seen = set()
for p in manifest:
    key = p["scope"], p["prototype"]
    assert key not in seen and key in src
    seen.add(key)
    n = src[key]
    assert p["scope"] in ("base", "dlc1")
    assert p["file"] == n["source_file"]
    assert p["state"] == n["buy_limitations_state"]
    assert p["blocked_types"] == n["buy_limitations"]
    assert ROLES[p["role"]][0] == n["trader_type"]
    blocked = p["blocked_types"].split(";") if p["blocked_types"] else []
    assert len(blocked) == len(set(blocked))
    assert all(t in CATEGORIES.values() for t in blocked)
    assert (p["state"] == "EXPLICIT") == bool(blocked)
roles = Counter(r["role"] for r in manifest)
assert all(roles[r] == count for r, (_, count, _) in ROLES.items())
assert Counter(r["scope"] for r in manifest) == {"base": 68, "dlc1": 18}
assert sum(r["state"] == "EXPLICIT" for r in manifest) == 53
eger = next(r for r in manifest if r["prototype"] == "EgerTrader_TradePrototype")
bolshak = next(r for r in manifest if r["prototype"] == "BolshakTechnician_TradePrototype")
assert eger["role"] == "general" and "Consumable" in eger["blocked_types"]
assert bolshak["scope"] == "dlc1" and bolshak["state"] == "NOT_AUTHORED"

preset_dir = SRC / "Presets"
presets = sorted(preset_dir.glob("*.json"))
assert len(presets) == 12
for preset in presets:
    data = json.loads(preset.read_text(encoding="utf-8"))
    assert set(data["values"]) == set(by), preset.name
    for pid in ids:
        assert data["values"][pid] == {"kind": "toggle",
                                       "boolean": by[pid]["vanilla"]["All"]}, (preset.name, pid)

v = (SRC / "ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
p = (SRC / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
x = (SRC / "MainWindow.xaml").read_text(encoding="utf-8")
matrix = (SRC / "Controls/TraderBuysMatrix.cs").read_text(encoding="utf-8")
policy = (SRC / "Services/TraderBuysPolicy.cs").read_text(encoding="utf-8")
writer = (SRC / "Services/CfgPatchWriterService.cs").read_text(encoding="utf-8")
implementation = (SRC / "Services/CfgPatchWriterService.TraderBuys.cs").read_text(encoding="utf-8")
project = (SRC / "TrueCustomDifficulty.csproj").read_text(encoding="utf-8")
must = [
    ("role matrix", "TraderBuysPolicy.Roles", v),
    ("backing instances", "traderBuysParameterIds.Contains(p.Id)", v),
    ("aggregate anchor", "TraderBuysPolicy.AnchorId", v),
    ("visible backing collapse", "(!IsTraderBuysMember || IsTraderBuysAnchor)", p),
    ("matrix xaml", '<controls:TraderBuysMatrix x:Name="TraderBuysMatrixControl"', x),
    ("matrix trigger", 'Binding="{Binding IsTraderBuysAnchor}"', x),
    ("checkbox", "ToggleButton.IsCheckedProperty", matrix),
    ("cell-local state", "nameof(ParameterViewModel.IsOwnValueModified)", matrix),
    ("local notification", "OnPropertyChanged(nameof(IsOwnValueModified))", p),
    ("two-line heading", '"CONSUM-\\\\nABLES"', matrix),
    ("policy rows", "new Role(", policy),
    ("production dispatch", "WriteTraderBuys(modifiedById, hasDlc1", writer),
    ("production coverage", "baseline.Special.TraderBuys.ExpectedProfiles != 86", writer),
    ("source manifest", "TraderBuysResource", implementation),
    ("source eligibility", "IsScopeApplicable(row.Scope, hasDlc1)", implementation),
    ("source exclusion", "TraderBuysPolicy.ParameterId(role, category)", implementation),
    ("BPatch replacement", 'doc.AddReplacementNode(row.Prototype, path, "BuyLimitations")', implementation),
    ("BPatch removal", 'doc.AddRemovalNode(row.Prototype, path, "BuyLimitations")', implementation),
    ("manifest embedded", "TrueCustomDifficulty.Data.trader-buy-policy-manifest.csv", project),
]
for label, needle, content in must:
    assert needle in content, (label, needle)
assert "TradeGenerators" in implementation and "ItemGeneratorPrototypes.cfg" not in implementation

print("[PASS] 359 catalog IDs / 185 visible / 174 hidden, 40 native Trader Buys cells")
print("[PASS] 86 exact original buy-policy source entries, roles, DLC1, Eger/Bolshak")
print("[PASS] 12 neutral factory presets, real WPF bindings, BPatch source router")
print("NOT VERIFIED: actual C# build, emitted .NET writer, Wine layout, Unreal gameplay")
