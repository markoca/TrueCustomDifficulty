#!/usr/bin/env python3
"""Static guard for slot-aware trader SELL clone and relink logic."""
from pathlib import Path
s=(Path(__file__).resolve().parents[1]/'src/TrueCustomDifficulty/Services/CfgPatchWriterService.TraderSells.cs').read_text()
checks=[
    'int TradeGeneratorSlot, string RootGeneratorScope',
    'var originalRootScope = first.RootGeneratorScope;',
    'profile.Any(x => x.RootGeneratorScope != first.RootGeneratorScope)',
    'rootScope.Any(x => x != originalRootScope)',
    'int.Parse(x[12], System.Globalization.CultureInfo.InvariantCulture), x[14]',
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
assert 'Legacy 2.0.5 fallback' not in s
assert '6B06E7F3B6B4844E4043F4F620F7B8A364ED7623728027315AD90E7D74C2E05F' not in s
print('Trader SELL slot-aware static source audit OK')
