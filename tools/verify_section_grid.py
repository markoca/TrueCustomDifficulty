#!/usr/bin/env python3
"""Static WPF section-grid layout contract (runtime resizing still requires Wine)."""
from pathlib import Path
from xml.etree import ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
panel = (ROOT / "src/TrueCustomDifficulty/Infrastructure/ResponsiveSectionGridPanel.cs").read_text()
xaml = (ROOT / "src/TrueCustomDifficulty/MainWindow.xaml").read_text()
required = (
    '["Stashes"] = "Breakable Containers"',
    '["Human Enemies"] = "Mutants"',
    '["Your Weapons"] = "Explosives"',
    '["Trader Stock"] = "Item Price Scalers"',
    '["Equipment Wear"] = "Item Weight"',
    'private const double MinSectionWidth = 540.0;',
    'private const double ColumnGap = 16.0;',
    'i + 1 < elements.Count',
    'IsCompatiblePair(elements[i], elements[i + 1])',
    'CollectionViewGroup',
    'element.Measure(new Size(cardWidth, double.PositiveInfinity))',
    'row[i].Arrange(new Rect(i * (cardWidth + ColumnGap), y, cardWidth, rowHeight))',
)
for fragment in required:
    if fragment not in panel:
        raise SystemExit("[FAIL] section grid: " + fragment)
if '<infra:ResponsiveSectionGridPanel IsItemsHost="True"' not in xaml:
    raise SystemExit("[FAIL] section grid: XAML items host missing")
ET.fromstring(xaml)
for width in (800.0, 1095.0, 1096.0, 1200.0):
    paired = (width - 16.0) / 2.0 >= 540.0
    if paired != (width >= 1096.0):
        raise SystemExit("[FAIL] section grid: breakpoint drift")
print("[PASS] Section grid preserves ordering and safe two-column breakpoints")
