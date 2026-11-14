#!/usr/bin/env python3
"""Fail-closed source and UI guard for category-only trader stock editor."""
from pathlib import Path
import json

root = Path(__file__).resolve().parents[1]
src = root / "src/TrueCustomDifficulty"
catalog = json.loads((src / "Data/parameters.json").read_text(encoding="utf-8"))
baseline = json.loads((src / "Data/cfg-writer-baseline.json").read_text(encoding="utf-8"))
rows = catalog["parameters"]
by = {p["id"]: p for p in rows}
assert len(by) == len(rows) == baseline["supportedParameterCount"] == 359
assert sum(p.get("exposeInUi", True) for p in rows) == 184
assert sum(p.get("exposeInUi") is False for p in rows) == 175
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
assert 'parameter.CurrentBoolean = cell.IsChecked == true' in ui
assert 'parameter.IsValueModifiedFromReference()' in ui
assert 'TraderItemOverrides' in ui
assert 'legacy item-specific exclusions' in ui
assert 'legacy BUY restrictions' in ui
# Technician has native CFG SELL routes but only exceptional game merchants
# actually open a stock shop. Suppress misleading role-wide Technician cards,
# without dropping existing preset/CFG writer parameters.
assert 'Roles.Where(role => role.Id != "technician").ToArray()' in policy
assert 'foreach (var role in TraderSellsPolicy.EditorRoles)' in ui
assert 'TraderSellsPolicy.EditorRoles.Single(r => r.Id == _roleId)' in ui
assert 'foreach (var role in TraderSellsPolicy.Roles)' not in ui
assert 'TraderSellsPolicy.ParameterIds.Any(id => !_source.ContainsKey(id))' in ui
assert 'TraderSellsPolicy.ParameterIds.Concat(' in ui
assert '["technician"] = new HashSet<string>(StringComparer.Ordinal)' in policy
assert 'Binding="{Binding IsTraderSellsAnchor}"' in xaml
assert '<controls:TraderBuysMatrix' in xaml
# The single SELL-anchored trader row owns the table and both native
# vertical knob banks. All five price/amount scalars remain in the catalog,
# presets and CFG writers, but their old standalone anchor rows are hidden.
assert '<infra:TraderEconomyPanel x:Name="TraderEconomyPanel"' in xaml
assert '<Setter TargetName="TraderEconomyPanel" Property="Visibility" Value="Visible" />' in xaml
assert xaml.count('VerticalKnobLayout="True"') == 2
assert 'Family="TraderStockBoard"' in xaml and 'Family="ServiceCostsBoard"' in xaml
assert 'TraderBuysMatrixControl" Property="Visibility" Value="Visible"' not in xaml
aggregate = (src / "Controls/AggregateVisualControl.cs").read_text(encoding="utf-8")
assert 'VerticalKnobLayout ? "STOCK AMOUNT" : "TRADER STOCK"' in aggregate
assert '"ServiceCostsBoard" => VerticalKnobLayout ? 608.0' in aggregate
assert '"TraderStockBoard" => VerticalKnobLayout ? 423.0' in aggregate
assert aggregate.count('new Rect(left, 44.0 + i * pitch, cellWidth, 181.0)') == 2
viewmodel = (src / "ViewModels/ParameterViewModel.cs").read_text(encoding="utf-8")
visible = viewmodel.split("public bool IsControlVisible =>", 1)[1].split("public string ToggleStateLabel", 1)[0]
assert '&& Id != "economy.traderAmmoStock"' in visible
assert '&& Id != "economy.fastTravelCost"' in visible
panel = (src / "Infrastructure/TraderEconomyPanel.cs").read_text(encoding="utf-8")
assert 'UseThreeColumns(width)' in panel
assert 'UseTwoKnobColumns(width)' in panel
assert 'tableWidth + 2 * KnobWidth + 2 * Gap' in panel
assert 'table.DesiredSize.Height + Gap + bankHeight' in panel
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
assert '<Setter TargetName="TraderBuysMatrixControl" Property="Visibility" Value="Visible" />' in sell_board
# The category-only UI does not bind the retired per-item list. The saved
# sidecar must continue to reach the writer through MainViewModel.
assert 'TraderItemOverrides' in (src / "ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
assert 'TraderSellsPolicy.ParameterIds' in (src / "ViewModels/MainViewModel.cs").read_text(encoding="utf-8")
assert 'WriteTraderSells(' in (src / "Services/CfgPatchWriterService.cs").read_text(encoding="utf-8")
assert 'roleItemKeys.Contains(TraderOffersPolicy.Key(role.Id, r.ItemSid))' in writer
assert 'roleChanges.Select(c => c.Category.Label)' in writer
assert 'Dlc1OnlyParameterIds' in policy
assert 'trader-sells-2.1-runtime.csv' in (src / "TrueCustomDifficulty.csproj").read_text(encoding="utf-8")
print("PASS: 359 source IDs, 40 legacy BUY flags, 31 source-backed SELL category toggles; category-only trader UI")
