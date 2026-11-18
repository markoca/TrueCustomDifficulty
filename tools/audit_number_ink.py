#!/usr/bin/env python3
"""Changed values: only the NUMBERS turn gold and grow; words and units keep
their ink and size. Static gate over every board control and the XAML panel.

Rules
1. No readout picks a bigger font size because its value changed
   (`modified ? 14.2 : 13.0`): the size stays the base size and
   ReadoutInk grows the numbers only.
2. Every manual gold number range (SetForegroundBrush(accent …)) also grows
   (SetFontSize next to it, or ReadoutInk.Mark).
3. ReadoutInk.Highlight / Mark colour AND grow; NumberRuns does the same in XAML.
4. MainWindow.xaml never paints a whole value TextBlock gold or bigger on
   IsModified.
"""
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
APP = ROOT / "src/TrueCustomDifficulty"
CONTROLS = sorted((APP / "Controls").glob("*.cs"))
hits = []

SIZE_TERNARY = re.compile(
    r"\b[\w.()!]*(?:[Mm]odified|[Cc]hanged|IsEffectiveChange|IsValueModifiedFromReference\(\))[\w.()]*"
    r"\s*\?\s*\d+(?:\.\d+)?\s*:\s*\d+(?:\.\d+)?")
for f in CONTROLS:
    lines = f.read_text().splitlines()
    for n, line in enumerate(lines, 1):
        for m in SIZE_TERNARY.finditer(line):
            a, b = re.findall(r"\d+(?:\.\d+)?", m.group(0).split("?", 1)[1])[:2]
            if 8.0 <= float(b) < float(a) <= float(b) + 3.0:
                hits.append(f"{f.name}:{n}: whole text grows on change: {m.group(0)}")
        if re.search(r"(?:[Mm]odified|[Cc]hanged|FromReference\(\))\w*\)?\s*\?\s*\w+\s*\+\s*\d", line):
            hits.append(f"{f.name}:{n}: whole text grows on change (size + N)")
        if re.search(r"(?:[Mm]odified|[Cc]hanged|FromReference\(\)|IsEffectiveChange|> Epsilon)[^;]{0,40}\?\s*"
                     r"(?:accent|BrushResource\(\"AccentBrush\"[^)]*\)|FindBrush\(\"AccentBrush\"[^)]*\))\s*:", line) \
                and not re.search(r"Pen\(|CloneWithOpacity|Draw(?:Line|Ellipse|Rectangle|Geometry)|Chrome", line):
            hits.append(f"{f.name}:{n}: whole value painted gold; use ReadoutInk.Numbers")
        if "SetForegroundBrush(" in line and "separatorBrush" not in line and f.name != "ReadoutInk.cs":
            window = "\n".join(lines[max(0, n - 4):n + 3])
            if "SetFontSize(" not in window:
                hits.append(f"{f.name}:{n}: gold number does not grow (use ReadoutInk.Mark)")

ink = (APP / "Controls/ReadoutInk.cs").read_text()
for token in ("public const double NumberGrowth", "public static void Mark(",
              "formatted.SetFontSize(emSize + NumberGrowth"):
    if token not in ink:
        hits.append(f"ReadoutInk.cs: missing {token}")
runs = (APP / "Controls/NumberRuns.cs").read_text()
if "FontSize = block.FontSize + ReadoutInk.NumberGrowth" not in runs:
    hits.append("NumberRuns.cs: numbers must grow by ReadoutInk.NumberGrowth")

xaml = (APP / "MainWindow.xaml").read_text()
for name in ("CurrentValueText", "EffectiveValueText", "RangeMinValueText",
             "RangeMaxValueText", "PracticalQuantityText"):
    if re.search(rf'TargetName="{name}" Property="(Foreground|FontSize|FontWeight)"', xaml):
        hits.append(f"MainWindow.xaml: {name} changes as a whole on IsModified")
    tag = re.search(rf'<TextBlock x:Name="{name}"[^>]*>', xaml, re.S)
    if not tag or "controls:NumberRuns.Text=" not in tag.group(0):
        hits.append(f"MainWindow.xaml: {name} must use controls:NumberRuns")

if hits:
    for h in hits:
        print("FAIL", h)
    print(f"FAIL {len(hits)} number-ink hits")
    sys.exit(1)
print("PASS number ink: only numbers turn gold and grow; words, units and sizes stay put")
