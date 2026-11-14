#!/usr/bin/env python3
"""Static 2.1 generic-NPC rare trader-stock guard. No Unreal runtime assertions."""
import csv
import re
from collections import defaultdict
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src/TrueCustomDifficulty"
service = (SRC / "Services/CfgPatchWriterService.GenericNpcRareStock.cs").read_text(encoding="utf-8")
core = (SRC / "Services/CfgPatchWriterService.cs").read_text(encoding="utf-8")
remove = (SRC / "Services/CfgPatchWriterService.TraderSells.cs").read_text(encoding="utf-8")
contract = (ROOT / "tests/EconomyPatchContract/Program.cs").read_text(encoding="utf-8")
manifest = SRC / "Data/trader-sells-2.1-runtime.csv"

actual = defaultdict(lambda: {"roots": set(), "roles": set(), "scopes": set(), "slots": set()})
with manifest.open(encoding="utf-8", newline="") as f:
    for r in csv.DictReader(f):
        p = (r["trade_scope"], r["trade_sid"])
        if r["trader_type"] == "GENERIC_NPC_TRADER":
            actual[p]["roots"].add(r["root_stock_sid"])
            actual[p]["roles"].add(r["trader_type"])
            actual[p]["scopes"].add(r["root_generator_scope"])
            actual[p]["slots"].add(r["trade_generator_slot"])

pat = r'^\s*\("([^"]+)", "([^"]+)", ([12])\),$'
expected = re.findall(pat, service, re.MULTILINE)
assert len(expected) == 9 and len(set(e[0] for e in expected)) == 9
assert len(actual) == len(expected)
for sid, root, index in expected:
    profile = actual[("base", sid)]
    assert profile["roots"] == {root}, (sid, profile)
    assert profile["scopes"] == {"base"}
    assert profile["slots"] == {"0"}
    assert root.startswith("GeneralNPC_TradeItemGenerator")
    if sid in {"GeneralNPC_TradePrototype_Scientists", "GeneralNPC_TradePrototype_Spark"}:
        assert index == "1"
    else:
        assert index == "2"

assert "SellCloneSid(\"base\", p.Trade, 0, p.Root)" in service
assert "GetOrAddReferencedRoot(" in service
assert '"../ItemGeneratorPrototypes.cfg", p.Root' in service
assert '"ItemGeneratorPrototypeSID", clone' in service
assert 'WriteGenericNpcRareTraderStock(documents)' in core
assert 'AddPlainRoot(string prototype)' in core
assert 'WriteTraderSells(modifiedById, vm.TraderItemOverrides' in core
assert core.index('WriteTraderSells(modifiedById, vm.TraderItemOverrides') < core.index('WriteGenericNpcRareTraderStock(documents)')
assert 'TCD_GENERIC_NPC_RARE_STOCK' in service
assert 'GenericRareNormalChance = 0.02' in service
assert 'GenericRareSecondChoiceChance = 0.08' in service
assert 'mode.Equals("test", StringComparison.OrdinalIgnoreCase)' in service
assert '(RareBrokenPrimarySid, 58)' in service and '(RareBrokenPistolSid, 16)' in service
assert '(RareBetterPrimarySid, 8)' in service
assert '"0.02", "0.17"' in service and '"0.42", "0.68"' in service
assert 'RareGroup(NewRareRoot(generator, RareMasterSid), 0, "SubItemGenerator")' in service
assert 'RareReference(master, 0, RarePrimarySid, "Chance", "1")' in service
assert 'RareReference(master, 1, RareSecondarySid, "Chance",' in service
assert 'TestGenericNpcRareStock();' in contract
assert 'original = Environment.GetEnvironmentVariable(key)' in contract
assert 'Environment.SetEnvironmentVariable(key, original);' in contract
assert 'TraderBuysPolicy' not in service and 'CorpseSmartLoot' not in service.split("private static int WriteGenericNpcRareTraderStock",1)[1]
assert (ROOT / "docs/generators/GENERIC-NPC-RARE-TRADER-STOCK-21.md").exists()
for doc in ("UI-DESIGN-LOCK.md", "VISUAL-CONTROL-SYSTEM.md"):
    s = (ROOT / "docs" / doc).read_text(encoding="utf-8")
    assert "Generic NPC rare trader stock, **explicit test opt-in**" in s
print("PASS: nine exact 2.1 generic merchants, opt-in 100%/2% isolated rare branch, native weight+durability source contract")
