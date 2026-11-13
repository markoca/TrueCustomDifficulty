#!/usr/bin/env python3
"""Static guard for slot-aware trader SELL clone and relink logic."""
from pathlib import Path
s=(Path(__file__).resolve().parents[1]/'src/TrueCustomDifficulty/Services/CfgPatchWriterService.TraderSells.cs').read_text()
checks=[
    'int TradeGeneratorSlot',
    'x[5], x[6], x[7], x[10], x[17], x[18], x[19], 0)',
    'rows.GroupBy(x => (x.TradeScope, x.TradeSid, x.TradeGeneratorSlot))',
    'source.GroupBy(r => (r.TradeScope, r.TradeSid, r.TradeGeneratorSlot))',
    'SellCloneSid(string scope, string tradeSid, int slot, string sourceSid)',
    'slot == 0',
    'first.TradeGeneratorSlot, first.RootSid',
    'first.TradeGeneratorSlot, t.GeneratorSid',
    '"[" + first.TradeGeneratorSlot + "]"',
]
for value in checks:
    assert value in s, 'Missing slot-aware writer invariant: '+value
assert 'first.TradeSid, new[] { "TradeGenerators", "[0]" }' not in s
print('Trader SELL slot-aware static source audit OK')
