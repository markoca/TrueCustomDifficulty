#!/usr/bin/env python3
"""Fail closed on source-exact 2.1 Trader Stock runtime and preset/UI boundaries."""
import csv
import hashlib
from collections import Counter, defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src/TrueCustomDifficulty"
PATH = SRC / "Data/trader-sells-2.1-runtime.csv"
EXPECTED = "9585c5b21bc13610dbc448650b2b053d582c852911782976076f5657f741e587"
COLUMNS = [
    "trade_scope", "trade_sid", "trader_type", "root_stock_sid", "item_sid",
    "item_category", "generator_sid", "generator_scope", "entry_cfg_path",
    "direct_entry_state", "nested_chain", "outer_links",
    "trade_generator_slot", "trade_generator_condition", "root_generator_scope"
]
raw = PATH.read_bytes()
assert hashlib.sha256(raw.replace(b"\r\n", b"\n")).hexdigest() == EXPECTED, "2.1 inventory hash drift"
with PATH.open(encoding="utf-8", newline="") as f:
    reader = csv.DictReader(f)
    assert reader.fieldnames == COLUMNS, "2.1 runtime schema drift"
    rows = list(reader)
assert len(rows) == 2939, "Expected 2939 playable native rows"
assert len({(r["trade_scope"], r["trade_sid"]) for r in rows}) == 86
assert len({(r["generator_scope"], r["generator_sid"]) for r in rows}) == 62
assert len({(r["generator_scope"], r["generator_sid"], r["entry_cfg_path"], r["item_sid"]) for r in rows}) == 608
assert Counter(r["generator_scope"] for r in rows)["dlc1"] == 113
assert Counter(r["direct_entry_state"] for r in rows) == {"CANDIDATE": 2933, "ZERO_CHANCE": 6}
assert Counter(r["trade_scope"] for r in rows)["dlc1"] > 0
assert {(r["trade_scope"], r["trade_sid"]) for r in rows if r["trade_scope"] == "dlc1"}.__len__() == 18
assert {(r["trade_scope"], r["trade_sid"]) for r in rows if r["trade_scope"] == "base"}.__len__() == 68

roles = {
    "GENERAL_TRADER": 33, "MEDIC": 15, "TECHNICIAN": 16,
    "BARTENDER": 13, "GENERIC_NPC_TRADER": 9,
}
profiles = defaultdict(list)
offers = defaultdict(set)
for r in rows:
    assert r["trade_generator_slot"] == "0"
    assert r["trade_generator_condition"] == "ConstTrue"
    assert r["trade_scope"] in {"base", "dlc1"}
    assert r["generator_scope"] in {"base", "dlc1"}
    assert r["root_generator_scope"] in {"base", "dlc1"}
    assert r["trader_type"] in roles
    assert r["direct_entry_state"] in {"CANDIDATE", "ZERO_CHANCE"}
    assert r["entry_cfg_path"].startswith("ItemGenerator/[")
    assert "/PossibleItems/[" in r["entry_cfg_path"]
    assert len(r["entry_cfg_path"].split("/")) == 4
    chain = r["nested_chain"].split(">")
    assert len(chain) in {1, 2}
    assert chain[0] == r["root_stock_sid"] and chain[-1] == r["generator_sid"]
    assert bool(r["outer_links"]) == (len(chain) == 2)
    if r["outer_links"]:
        assert all(link.startswith(r["root_stock_sid"] + ":ItemGenerator/[")
                   for link in r["outer_links"].split("|"))
    profiles[(r["trade_scope"], r["trade_sid"])].append(r)
    offers[(r["trader_type"], r["item_sid"])].add(r["item_category"])
assert Counter(group[0]["trader_type"] for group in profiles.values()) == roles
assert len(offers) == 260 and len({sid for _, sid in offers}) == 164

exceptions = {key: value for key, value in offers.items() if len(value) != 1}
assert exceptions == {("GENERAL_TRADER", "AVOG"): {"Artifacts", "Ammo"}}, exceptions
cross_scoped = {(r["trade_sid"], r["root_generator_scope"])
                for r in rows if r["trade_scope"] != r["root_generator_scope"]}
assert cross_scoped == {
    ("VezaMedic_TradePrototype", "base"),
    ("VezaMedicStage2_TradePrototype", "base"),
}, cross_scoped
suslov = [r for r in rows if r["trade_sid"] == "SuslovTraderSQ14_TradePrototype"
          and r["generator_sid"] == "Trader_T3_Ammo_ItemGenerator"]
assert len(suslov) == 22 and all(len(r["outer_links"].split("|")) == 2 for r in suslov)
assert not {("base", x) for x in ("AllTraderNPC", "BaseTraderNPC_Template", "BasicTrader", "TradeTest")} & set(profiles)

project = (SRC / "TrueCustomDifficulty.csproj").read_text()
writer = (SRC / "Services/CfgPatchWriterService.TraderSells.cs").read_text()
items = (SRC / "Services/TraderOffersPolicy.cs").read_text()
for name in ("trader-sells-2.1-runtime.csv", "9585C5B21BC13610DBC448650B2B053D582C852911782976076F5657F741E587"):
    assert name in (project + writer + items)
assert "trader-offers-source-rows.csv" not in project + writer + items, "Legacy 2.0.5 executable fallback"
assert "6B06E7F3B6B4844E4043F4F620F7B8A364ED7623728027315AD90E7D74C2E05F" not in writer + items
assert "var originalRootScope = first.RootGeneratorScope;" in writer
assert 'roleItemKeys.Contains(TraderOffersPolicy.Key(role.Id, r.ItemSid))' in writer
assert '"[" + first.TradeGeneratorSlot + "]"' in writer
assert 'r.OuterLinks.Split(\'|\',' in writer
assert 'fields[4] == "AVOG"' in items
print("PASS Trader Stock 2.1 SHA256/2939 rows/86 profiles/164 items/260 role-items/62 generators/608 coordinates; 2.0.5 SELL fallback absent")
