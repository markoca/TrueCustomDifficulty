#!/usr/bin/env python3
"""Fail-closed static audit of multi-parent trader SELL relinking."""
from pathlib import Path
root=Path(__file__).resolve().parents[1]
writer=(root/'src/TrueCustomDifficulty/Services/CfgPatchWriterService.TraderSells.cs').read_text()
assert "SelectMany(r => r.OuterLinks.Split('|', StringSplitOptions.RemoveEmptyEntries))" in writer
assert ".Distinct(StringComparer.Ordinal)" in writer
assert 'rootDocument.AddAssignment(rootClone,' in writer
assert '"ItemGeneratorPrototypeSID", targetClone)' in writer
assert 'RegisterClone(targetClone, t.GeneratorScope, t.GeneratorSid)' in writer
assert "GetOrAddReferencedRoot(" in writer
assert 'segments.Length != 4' in writer
print("Trader SELL multi-parent static audit OK")
