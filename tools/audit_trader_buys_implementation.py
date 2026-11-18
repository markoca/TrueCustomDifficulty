#!/usr/bin/env python3
"""Fail-closed source and UI guard for category-only trader stock editor."""
from pathlib import Path
import json
import sys as _sys; _sys.path.insert(0, str(__import__("pathlib").Path(__file__).resolve().parent))
from catalog_census import CATALOG, VISIBLE, HIDDEN, EDIT_POINTS, DIRECT_IDS

root = Path(__file__).resolve().parents[1]
src = root / "src/TrueCustomDifficulty"
catalog = json.loads((src / "Data/parameters.json").read_text(encoding="utf-8"))
baseline = json.loads((src / "Data/cfg-writer-baseline.json").read_text(encoding="utf-8"))
rows = catalog["parameters"]
by = {p["id"]: p for p in rows}
assert len(by) == len(rows) == baseline["supportedParameterCount"] == CATALOG
assert sum(p.get("exposeInUi", True) for p in rows) == VISIBLE
assert sum(p.get("exposeInUi") is False for p in rows) == HIDDEN
buys = {p["id"] for p in rows if p["id"].startswith("economy.traderBuys.")}
sells = {p["id"] for p in rows if p["id"].startswith("economy.traderSells.")}
assert len(buys) == 40 and len(sells) == 31
assert {p["id"] for p in rows if p["id"] in buys and p.get("exposeInUi", True)} == set()
assert {p["id"] for p in rows if p["id"] in sells and p.get("exposeInUi", True)} == {"economy.traderSells.general.weapons"}
assert set(baseline["special"]["traderBuys"]["parameterIds"]) == buys
assert set(baseline["special"]["traderSells"]["parameterIds"]) == sells
for sid in buys | sells:
    assert by[sid]["control"] == "toggle"
    assert by[sid]["valueKind"] == "boolean"
    assert by[sid]["tab"] == "economy"
ui = (src / "Controls/TraderBuysMatrix.cs").read_text(encoding="utf-8")
xaml = (src / "MainWindow.xaml").read_text(encoding="utf-8")
writer = (src / "Services/CfgPatchWriterService.TraderSells.cs").read_text(encoding="utf-8")
policy = (src / "Services/TraderSellsPolicy.cs").read_text(encoding="utf-8")
assert 'TraderSellsPolicy.Categories.Where' in ui
assert 'TraderSellsPolicy.IsAvailable(role, c)' in ui
# One "keep X% of game odds" rail per category: 0% = SELL toggle off,
# 5–95% = TraderSellOdds sidecar, 100% = vanilla.
assert 'sell.CurrentBoolean = value > Epsilon;' in ui
assert 'SellOdds?.Set(rail.Id, value);' in ui
# Legacy restrictions from older presets are visible and clearable.
assert 'OLDER PRESET: ' in ui and '"CLEAR"' in ui and 'private void ClearLegacy()' in ui
assert 'TraderItemOverrides = string.Empty;' in ui
# TECHNICIAN (only Bolshak sells) has no rail and is never written.
assert 'Roles.Where(role => role.Id != "technician").ToArray();' in policy
assert 'var roles = TraderSellsPolicy.EditorRoles;' in ui
assert 'TraderSellsPolicy.EditableParameterIds.Contains(pair.Key)' in writer
assert 'var changed = TraderSellsPolicy.EditorRoles.SelectMany(' in writer
assert 'TraderSellsPolicy.ParameterIds\n            .Concat(' in ui.replace("\r", "")
assert '["technician"] = new HashSet<string>(StringComparer.Ordinal)' in policy
assert 'Binding="{Binding IsTraderSellsAnchor}"' in xaml
assert '<controls:TraderBuysMatrix' in xaml
# The single SELL-anchored trader row owns the table and two horizontal
# knob boards stacked on the right. The five native source controls remain
# hidden as standalone cards but retain their preset/CFG writer mappings.
assert '<infra:TraderEconomyPanel x:Name="TraderEconomyPanel"' in xaml
assert '<Setter TargetName="TraderEconomyPanel" Property="Visibility" Value="Visible" />' in xaml
assert 'VerticalKnobLayout="True"' not in xaml
# AMMO / MEDICAL SUPPLY are rails on the TRADER STOCK board (no knob bank).
assert 'Family="TraderStockBoard"' not in xaml and 'Family="ServiceCostsBoard"' in xaml
assert xaml.index('<controls:TraderBuysMatrix') < xaml.index('Family="ServiceCostsBoard"')
assert 'TraderBuysMatrixControl" Property="Visibility" Value="Visible"' not in xaml
aggregate = (src / "Controls/AggregateVisualControl.cs").read_text(encoding="utf-8")
assert '"STOCK AMOUNT"' not in aggregate and 'DrawTraderStockKnobs' not in aggregate
assert '"ServiceCostsBoard" => ServicePriceBoardHeight(width)' in aggregate
assert '"TraderStockBoard"' not in aggregate
assert 'var columns = ActualWidth >= 470.0 ? 3 : 2;' in aggregate
assert 'VerticalKnobLayout' not in aggregate
viewmodel = (src / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
visible = viewmodel.split("public bool IsControlVisible =>", 1)[1].split("public string ToggleStateLabel", 1)[0]
assert '&& !IsTraderSupply' in visible
assert '&& Id != "economy.fastTravelCost"' in visible
panel = (src / "Infrastructure/TraderEconomyPanel.cs").read_text(encoding="utf-8")
assert 'UseSideColumn(width)' in panel
assert 'private const double TableShare = 0.42;' in panel
assert 'var tableWidth = contentWidth * TableShare;' in panel
assert 'return (tableWidth, contentWidth - tableWidth);' in panel
assert 'tableWidth >= MinTableWidth && bankWidth >= MinBankWidth' in panel
assert 'var (tableWidth, bankWidth) = ColumnWidths(width);' in panel
assert 'services.Arrange(new Rect(right, 0.0, bankWidth' in panel
# 2026-11-18: TRADER RESTOCK sits under SERVICE PRICES in the right column.
assert 'restock.Arrange(new Rect(right, services.DesiredSize.Height + Gap, bankWidth' in panel
assert 'Family="TraderRestock"' in xaml
assert 'Math.Min(FallbackTableWidth, width)' in panel
assert 'Math.Min(FallbackBankWidth, width)' in panel
assert 'Family="ServiceCostsBoard"\n                                                                                                     HorizontalAlignment="Stretch"' in xaml
assert 'InternalChildren.Count != 3' in panel
assert 'table.DesiredSize.Height + Gap' in panel
# The legacy BUY source anchor must not leak a bare ON / wheel +/- card.
# SELL anchors the only visible stock editor; all 40 BUY toggles still write.
pvm = (src / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
visible = pvm.split("public bool IsControlVisible =>", 1)[1].split("public string ToggleStateLabel", 1)[0]
visual_anchors = pvm.split("public bool IsVisualControlAnchor =>", 1)[1].split("public bool HasEffectiveDamage", 1)[0]
assert "&& !IsTraderBuysMember" in visible
assert "(!IsTraderBuysMember || IsTraderBuysAnchor)" not in visible
assert "IsTraderBuysAnchor ||" not in visual_anchors
assert "IsTraderSellsAnchor ||" in visual_anchors
assert 'Binding="{Binding IsTraderBuysAnchor}"' not in xaml
sell_board = xaml.split('<DataTrigger Binding="{Binding IsTraderSellsAnchor}" Value="True">', 1)[1].split("</DataTrigger>", 1)[0]
assert '<Setter TargetName="TraderEconomyPanel" Property="Visibility" Value="Visible" />' in sell_board
# The category-only UI does not bind the retired per-item list. The saved
# sidecar must continue to reach the writer through MainViewModel.
assert 'TraderItemOverrides' in (src / "ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
assert 'TraderSellsPolicy.ParameterIds' in (src / "ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
assert 'WriteTraderSells(' in (src / "Services/CfgPatchWriterService.cs").read_text(encoding="utf-8")
assert 'roleItemKeys.Contains(TraderOffersPolicy.Key(role.Id, r.ItemSid))' in writer
assert 'roleChanges.Select(c => c.Category.Label)' in writer
assert 'Dlc1OnlyParameterIds' in policy
assert 'trader-sells-2.1-runtime.csv' in (src / "TrueCustomDifficulty.csproj").read_text(encoding="utf-8")
print(f"PASS: {CATALOG} source IDs, 40 legacy BUY flags, 31 source-backed SELL category toggles; category-only trader UI")
