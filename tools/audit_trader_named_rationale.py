#!/usr/bin/env python3
"""Verify Trader Stock rationale named NPC categories against active 2.1 SELL CSV.

Source-only audit: does not compile Windows/WPF or prove in-game availability.
"""
import csv
import re
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/TrueCustomDifficulty"
vm = (SOURCE / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
rationale = (SOURCE / "Services/TraderStockRationale.cs").read_text(encoding="utf-8")
catalog = (SOURCE / "Data/parameters.json").read_text(encoding="utf-8")
xaml = (SOURCE / "MainWindow.xaml").read_text(encoding="utf-8")
assert "IsTraderSellsMember" in vm.split("public string RationaleDisplay =>", 1)[1].split("public string ControlHelp", 1)[0]
assert "TraderStockRationale.NamedCategories" in vm
assert 'SelectedParameter.RationaleDisplay' in xaml
assert '"id": "economy.traderSells.general.weapons"' in catalog
assert 'NamedProfiles.Select(p => p.Name + " — " + p.Categories)' in rationale
assert "Binoculars are source-listed but have no category switch" in rationale
assert "technicians have no Trader Stock tab" in rationale

source = defaultdict(set)
with (SOURCE / "Data/trader-sells-2.1-runtime.csv").open(encoding="utf-8", newline="") as handle:
    for row in csv.DictReader(handle):
        source[(row["trade_scope"], row["trade_sid"])].add(row["item_category"])
order = "Weapons Armor Attachments Ammo Medical Food Drinks Artifacts Detectors Grenades Binoculars".split()
pattern = r'^\s*\("([^"]+)", "(base|dlc1)", "([^"]+)", "([^"]+)"\),$'
entries = re.findall(pattern, rationale, re.MULTILINE)
assert len(entries) == 21, ("named profiles", len(entries))
assert len({(scope, sid) for _, scope, sid, _ in entries}) == len(entries)
for label, scope, sid, cats in entries:
    expected = ", ".join(c for c in order if c in source[(scope, sid)])
    assert expected and cats == expected, (label, sid, cats, expected)

for doc in ("UI-DESIGN-LOCK.md", "VISUAL-CONTROL-SYSTEM.md"):
    assert "CURRENT Trader Stock rationale: named 2.1 trader inventories" in (ROOT / "docs" / doc).read_text(encoding="utf-8")
named_doc = (ROOT / "docs/generators/TRADER-NAMED-PROFILES-2026-11-13.md").read_text(encoding="utf-8")
for label, scope, sid, cats in entries:
    assert f"| {label} | {scope} | `{sid}` | {cats} |" in named_doc

print("PASS: 21 source-mapped 2.1 NPC/TradeProfile stages and exact candidate stock categories in every Trader Stock rationale")
