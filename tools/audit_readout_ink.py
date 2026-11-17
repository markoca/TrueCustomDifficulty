#!/usr/bin/env python3
"""Changed-value readouts gold only their numbers (ReadoutInk), never words/units."""
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CONTROLS = ROOT / "src/TrueCustomDifficulty/Controls"
ink = (CONTROLS / "ReadoutInk.cs").read_text(encoding="utf-8")
assert "ReadoutInk" in ink and "public static Brush Numbers(" in ink
errors = []
for path in sorted(CONTROLS.glob("*.cs")):
    src = path.read_text(encoding="utf-8")
    # Every text factory that receives a caller brush resolves ReadoutInk.
    for m in re.finditer(r"private FormattedText (CreateText|Text)\(string value, double size, Brush brush", src):
        body = src[m.start():src.find("\n    }", m.start())]
        if "ReadoutInk.Resolve(brush)" not in body or "ReadoutInk.Highlight(" not in body:
            errors.append(f"{path.name}: {m.group(1)} ignores ReadoutInk")
    # Multi-line form: "...IsValueModifiedFromReference()\n    ? FindBrush(\"AccentBrush\"...".
    for m in re.finditer(r"IsValueModifiedFromReference\(\)\s*\?\s*(?:accent|FindBrush\(\"AccentBrush\")", src):
        errors.append(f"{path.name}:{src.count(chr(10), 0, m.start()) + 1}: whole readout turns gold")
    # A whole changed readout painted in accent is the retired style.
    for n, line in enumerate(src.splitlines(), 1):
        if re.search(r"(?:[Mm]odified|[Cc]hanged|IsValueModifiedFromReference\(\)(?: == true)?)\s*\?\s*accent\s*:", line):
            errors.append(f"{path.name}:{n}: whole readout turns gold")
assert not errors, "\n".join(errors)
print("PASS: changed readouts gold only their numbers; words and units stay neutral")
