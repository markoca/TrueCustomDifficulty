#!/usr/bin/env python3
"""Trader Sells 2.0.5 source, UI, private-clone routing and preset gate.

Static evidence ONLY: this cannot compile WPF, execute the native BPatch engine,
or prove the trader's inventory actually regenerates in-game.
"""
import csv
import hashlib
import json
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src/TrueCustomDifficulty"
DATA = SRC / "Data"
OFFERS = ROOT / "docs/generators/trader-offers-source-rows.csv"
assert hashlib.sha256(OFFERS.read_bytes().replace(b"\r\n", b"\n")).hexdigest() == (
    "6b06e7f3b6b4844e4043f4f620f7b8a364ed7623728027315ad90e7d74c2e05f"
), "Original 2.0.5 seller offer source content changed; reextract before writing."
with OFFERS.open(newline="", encoding="utf-8") as file:
    reader = csv.DictReader(file)
    assert reader.fieldnames == [
        "trade_scope", "trade_sid", "trader_type", "root_stock_sid", "item_sid",
        "item_category", "generator_sid", "generator_scope",
        "generator_cfg_line", "entry_cfg_line", "entry_cfg_path",
        "generation_native_category", "player_ranks", "chance", "weight",
        "min_count", "max_count", "direct_entry_state", "nested_chain",
        "outer_links"
    ]
    rows = list(reader)
assert len(rows) == 2939
assert Counter(x["generator_scope"] for x in rows) == {"base": 2826, "dlc1": 113}
assert len({(x["trade_scope"], x["trade_sid"]) for x in rows}) == 86
assert len({(x["generator_scope"], x["generator_sid"]) for x in rows}) == 62
assert len({(x["generator_scope"], x["generator_sid"], x["entry_cfg_path"], x["item_sid"])
            for x in rows}) == 608
assert sum(x["direct_entry_state"] == "ZERO_CHANCE" for x in rows) == 1
assert set(x["item_category"] for x in rows) == {
    "Weapons", "Armor", "Attachments", "Ammo", "Medical",
    "Food", "Drinks", "Artifacts", "Detectors", "Grenades",
    "NightVision", "Binoculars"
}

active = {
    "general": ("GENERAL_TRADER", ("weapons", "armor", "attachments", "ammo",
        "medical", "food", "drinks", "artifacts", "detectors", "grenades")),
    "medic": ("MEDIC", ("medical", "drinks", "artifacts", "detectors")),
    "technician": ("TECHNICIAN", ("weapons", "attachments", "ammo",
        "medical", "food", "drinks")),
    "bartender": ("BARTENDER", ("weapons", "ammo", "food", "drinks",
        "artifacts", "detectors")),
    "genericNpc": ("GENERIC_NPC_TRADER", ("ammo", "medical", "food",
        "drinks", "grenades")),
}
catalog = json.loads((DATA / "parameters.json").read_text(encoding="utf-8"))
base = json.loads((DATA / "cfg-writer-baseline.json").read_text(encoding="utf-8"))
by = {p["id"]: p for p in catalog["parameters"]}
assert len(by) == len(catalog["parameters"]) == base["supportedParameterCount"] == 359
assert sum(p.get("exposeInUi", True) for p in by.values()) == 184
assert len([p for p in by.values() if not p.get("exposeInUi", True)]) == 175
sells = {f"economy.traderSells.{role}.{category}"
         for role, (_, categories) in active.items() for category in categories}
assert len(sells) == 31
assert {p["id"] for p in by.values() if p["id"].startswith("economy.traderSells.")} == sells
for id in sells:
    p = by[id]
    assert p["control"] == "toggle" and p["valueKind"] == "boolean"
    assert p["vanilla"] == {"All": True} and p["tab"] == "economy"
    assert p["group"] == ("Traders" if id == "economy.traderSells.general.weapons" else "Trader Sells") and p["support"]["status"] == "runtime_test_required"
    assert p.get("exposeInUi") is (id == "economy.traderSells.general.weapons")
assert len(base["special"]["traderSells"]["parameterIds"]) == 31
assert set(base["special"]["traderSells"]["parameterIds"]) == sells
assert base["special"]["traderSells"]["manifestResource"] == (
    "TrueCustomDifficulty.Data.trader-offers-source-rows.csv")
assert base["special"]["traderSells"]["expectedProfiles"] == 86
assert base["special"]["traderSells"]["expectedSourceRows"] == 2939

profiles = defaultdict(list)
for row in rows:
    key = row["trade_scope"], row["trade_sid"]
    profiles[key].append(row)
    chain = row["nested_chain"].split(">")
    assert len(chain) in (1, 2)
    assert chain[0] == row["root_stock_sid"] and chain[-1] == row["generator_sid"]
    assert bool(row["outer_links"]) == (len(chain) == 2)
    assert row["entry_cfg_path"].startswith("ItemGenerator/[")
    assert "/PossibleItems/[" in row["entry_cfg_path"]
    assert len(row["entry_cfg_path"].split("/")) == 4
    if row["outer_links"]:
        parts = row["outer_links"].split(":")
        assert len(parts) == 4 and parts[0] == row["root_stock_sid"]
        assert parts[1].startswith("ItemGenerator/[")
        assert parts[2].startswith("chance=") and parts[3].startswith("weight=")
assert Counter(x[0] for x in profiles) == {"base": 68, "dlc1": 18}
assert Counter(profiles[x][0]["trader_type"] for x in profiles) == {
    "GENERAL_TRADER": 33, "MEDIC": 15, "TECHNICIAN": 16,
    "BARTENDER": 13, "GENERIC_NPC_TRADER": 9
}
for profile in profiles.values():
    assert len({(x["root_stock_sid"], x["trader_type"]) for x in profile}) == 1

dlc_only = set()
for role, (native, categories) in active.items():
    for cat in categories:
        wanted = [r for r in rows if r["trader_type"] == native
                  and r["item_category"] == cat.title()]
        assert wanted, (role, cat, "inert no-source UI cell")
        if {r["trade_scope"] for r in wanted} == {"dlc1"}:
            dlc_only.add(f"economy.traderSells.{role}.{cat}")
assert dlc_only == {
    f"economy.traderSells.technician.{cat}"
    for cat in ("weapons", "ammo", "medical", "food", "drinks")
}
# In particular, two cross-context consumable pools are cloned, never edited;
# a NATO ammo generator shared with quest scripts is cloned per merchant too.
for source in ("Trader_Cosnsumables_ItemGenerator",
               "BartenderUniversal_TradeItemGenerator",
               "Trader_NatoAmmo_ItemGenerator"):
    assert any(x["generator_sid"] == source for x in rows), source

preset_files = sorted((SRC / "Presets").glob("*.json"))
assert len(preset_files) == 12
for path in preset_files:
    preset = json.loads(path.read_text(encoding="utf-8"))
    assert set(preset["values"]) == set(by), path.name
    assert all(preset["values"][id] == {"kind": "toggle", "boolean": True}
               for id in sells), path.name

def read(path):
    return (SRC / path).read_text(encoding="utf-8")
source = {
    "policy": read("Services/TraderSellsPolicy.cs"),
    "control": read("Controls/TraderBuysMatrix.cs"),
    "writer": read("Services/CfgPatchWriterService.TraderSells.cs"),
    "dispatch": read("Services/CfgPatchWriterService.cs"),
    "vm": read("ViewModels/MainViewModel.cs"),
    "parameter": read("ViewModels/ParameterViewModel.cs"),
    "xaml": read("MainWindow.xaml"),
    "proj": read("TrueCustomDifficulty.csproj")
}
checks = {
    "policy": ("ParameterIds", "Dlc1OnlyParameterIds", "IsAvailable"),
    "control": ("TraderSellsPolicy.IsAvailable", "TraderOffersPolicy.Offers",
                "TraderItemOverrides", "SetItem(offer, box.IsChecked == true)"),
    "writer": ("SHA256.HashData", "GetOrAddReferencedRoot", "SellCloneSid(",
               'AddRemovalNode(targetClone, path[..^1], path[^1])',
               '"ItemGeneratorPrototypeSID", rootClone',
               '"ItemGeneratorPrototypeSID", targetClone',
               "LoadTraderSellOffers", "2939", "608"),
    "dispatch": ("WriteTraderSells(modifiedById", "baseline.Special.TraderSells",
                 "dormantSavedOnly.UnionWith(TraderSellsPolicy.Dlc1OnlyParameterIds)"),
    "vm": ("TraderSellsPolicy.ParameterIds", "TraderSellsPolicy.AnchorId",
           "traderSellsParameterIds.Contains(p.Id)"),
    "parameter": ("IsTraderSellsMember", "IsTraderSellsAnchor"),
    "xaml": ('<controls:TraderBuysMatrix x:Name="TraderBuysMatrixControl"',
             'Binding="{Binding IsTraderSellsAnchor}"'),
    "proj": ("trader-offers-source-rows.csv", "EmbeddedResource")
}
for k, needles in checks.items():
    for needle in needles:
        assert needle in source[k], (k, needle)
# Original stock is now edited per exact SID, with all legacy category
# presets preserved. Source `TraderOffersPolicy` is backed by the same embedded 2.0.5 CSV.
assert 'TraderOffersPolicy.Offers' in source["control"]
assert 'TraderSellsPolicy.IsAvailable(role, category)' in source["control"]
assert 'TraderItemOverrides' in source["control"]
assert 'roleItemKeys.Contains(TraderOffersPolicy.Key(role.Id, r.ItemSid))' in source["writer"]
assert 'disabledItems = TraderOffersPolicy.Decode(traderItemOverrides)' in source["writer"]
assert '<controls:TraderSellsMatrix' not in source["xaml"]
assert 'TraderSellsMatrixControl' not in source["xaml"]
assert 'GetDocument(documents, SellGeneratorFile(t.GeneratorScope))' in source["writer"]
assert 'GetDocument(documents, SellTradeFile(first.TradeScope))' in source["writer"]
assert 'rootDocument.AddRemovalNode(first.RootSid' not in source["writer"]
assert 'document.AddRemovalNode(t.GeneratorSid' not in source["writer"]

print("PASS: 359 source IDs, 184 visible, 175 hidden, 31 legacy stock-category source parameters and individual item SID exclusions")
print("PASS: 2939 original stock paths, 86 merchant profiles, 608 physical entries, nested links")
print("PASS: no inert source combinations, five DLC1-only cells, shared source clone isolation")
print("PASS: 12 neutral presets and source-backed UI → BPatch writer linkage")
print("NOT VERIFIED: compiled C# writer, Wine UI, Unreal CFG inheritance and live trader restock")
