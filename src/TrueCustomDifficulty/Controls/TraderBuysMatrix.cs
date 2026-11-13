using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// One trader/category table: each cell contains its independent BUY/SELL pair.
/// Native buy Consumables is shared; stock Medical/Food/Drinks are three SELL
/// sources in the SAME row. Neither native writer nor preset IDs are changed.
/// </summary>
public sealed class TraderBuysMatrix : UserControl
{
    private sealed record RowSpec(string Label, string? BuyId, string[] SellIds);

    // One category row, one trader cell, independent source-backed controls.
    // The three SELL consumables never imply three unsupported BUY categories.
    private static readonly RowSpec[] Rows =
    {
        new RowSpec("WEAPONS", "weapon", new[] { "weapons" }),
        new RowSpec("ARMOR", "armor", new[] { "armor" }),
        new RowSpec("ATTACHMENTS", "attach", new[] { "attachments" }),
        new RowSpec("AMMO", "ammo", new[] { "ammo" }),
        new RowSpec("GRENADES", "grenade", new[] { "grenades" }),
        new RowSpec("CONSUMABLES", "consumable", new[] { "medical", "food", "drinks" }),
        new RowSpec("ARTIFACTS", "artifact", new[] { "artifacts" }),
        new RowSpec("DETECTORS", null, new[] { "detectors" }),
        new RowSpec("MUTANT PARTS", "mutantLoot", Array.Empty<string>())
    };

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters), typeof(IEnumerable), typeof(TraderBuysMatrix),
        new PropertyMetadata(null, (sender, _) => ((TraderBuysMatrix)sender).Rebuild()));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public event EventHandler<AggregateParameterSelectedEventArgs>? ParameterSelected;

    private static Brush ThemeBrush(string key, Brush fallback) =>
        Application.Current?.TryFindResource(key) as Brush ?? fallback;

    private static TextBlock Caption(string label, double fontSize, Brush brush) => new()
    {
        Text = label,
        FontSize = fontSize,
        Foreground = brush,
        FontWeight = FontWeights.SemiBold,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        TextAlignment = TextAlignment.Center,
        TextWrapping = TextWrapping.NoWrap
    };

    // The unchanged 20-DIP application CheckBox is the actual editing surface.
    // Its individual border, rather than the shared category cell, shows edits.
    private FrameworkElement ActionToggle(ParameterViewModel? parameter,
        string tooltip, Brush muted, Brush accent)
    {
        if (parameter is null)
        {
            var absent = Caption("—", 10, muted);
            absent.Opacity = 0.42;
            absent.ToolTip = tooltip;
            return absent;
        }

        var halo = new Border
        {
            Width = 26,
            Height = 26,
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1),
            Background = Brushes.Transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = parameter
        };
        var style = new Style(typeof(Border));
        style.Setters.Add(new Setter(Border.BorderBrushProperty, Brushes.Transparent));
        style.Triggers.Add(new DataTrigger
        {
            Binding = new Binding(nameof(ParameterViewModel.IsOwnValueModified)),
            Value = true,
            Setters = { new Setter(Border.BorderBrushProperty, accent) }
        });
        halo.Style = style;

        var box = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = tooltip
        };
        box.SetBinding(ToggleButton.IsCheckedProperty,
            new Binding(nameof(ParameterViewModel.CurrentBoolean))
            {
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            });
        box.SetBinding(IsEnabledProperty,
            new Binding(nameof(ParameterViewModel.IsControlEnabled)));
        box.PreviewMouseLeftButtonDown += (_, _) =>
            ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(parameter));
        halo.Child = box;
        return halo;
    }

    private FrameworkElement SellerActions(RowSpec row, TraderBuysPolicy.Role role,
        IReadOnlyDictionary<string, ParameterViewModel> source,
        Brush muted, Brush accent)
    {
        if (row.SellIds.Length == 0)
            return ActionToggle(null, "No original stock category here.", muted, accent);

        if (row.SellIds.Length == 1)
        {
            var category = TraderSellsPolicy.Categories.Single(c => c.Id == row.SellIds[0]);
            var available = TraderSellsPolicy.IsAvailable(role, category);
            return ActionToggle(available ? source[TraderSellsPolicy.ParameterId(role, category)] : null,
                role.Label + " · " + category.Label +
                (available ? " · SELLS TO YOU. Checked keeps original offers; unchecked removes this stock."
                           : " · No original offers."),
                muted, accent);
        }

        // Only Consumables has multiple seller categories. A three-part
        // source-backed SELL cluster fits alongside its ONE native BUY toggle.
        var cluster = new UniformGrid { Columns = 3, VerticalAlignment = VerticalAlignment.Center };
        foreach (var id in row.SellIds)
        {
            var category = TraderSellsPolicy.Categories.Single(c => c.Id == id);
            var available = TraderSellsPolicy.IsAvailable(role, category);
            var caption = id switch { "medical" => "M", "food" => "F", "drinks" => "D", _ => "?" };
            var title = role.Label + " · SELLS " + category.Label.ToUpperInvariant();
            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var tag = Caption(caption, 8.5, muted);
            tag.ToolTip = title;
            stack.Children.Add(tag);
            stack.Children.Add(ActionToggle(
                available ? source[TraderSellsPolicy.ParameterId(role, category)] : null,
                title + (available
                    ? ". Checked = original stock; unchecked = remove this category."
                    : ". No original offers."),
                muted, accent));
            cluster.Children.Add(stack);
        }
        return cluster;
    }

    private Border TraderCategoryCell(RowSpec row, TraderBuysPolicy.Role role,
        IReadOnlyDictionary<string, ParameterViewModel> source,
        Brush panel, Brush border, Brush muted, Brush accent)
    {
        var pair = new Grid { SnapsToDevicePixels = true };
        pair.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
        pair.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        ParameterViewModel? buyer = null;
        if (row.BuyId is not null)
        {
            var category = TraderBuysPolicy.Categories.Single(c => c.Id == row.BuyId);
            buyer = source[TraderBuysPolicy.ParameterId(role, category)];
        }

        var buyerTip = role.Label + " · " + row.Label +
            (row.BuyId is null
                ? " · No separate native BUY category."
                : " · BUYS FROM YOU. Untouched = individual original trader rules." +
                  (row.Label == "CONSUMABLES" ? " Medical, food and drinks share this one BUY rule." : ""));
        var buyerControl = ActionToggle(buyer, buyerTip, muted, accent);
        Grid.SetColumn(buyerControl, 0);
        pair.Children.Add(buyerControl);

        var separator = new Border
        {
            Width = 1, Background = border, Opacity = 0.4,
            VerticalAlignment = VerticalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 5, 0, 5),
            IsHitTestVisible = false
        };
        Grid.SetColumn(separator, 1);
        pair.Children.Add(separator);

        var sellerControl = SellerActions(row, role, source, muted, accent);
        Grid.SetColumn(sellerControl, 1);
        pair.Children.Add(sellerControl);
        return new Border
        {
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1),
            BorderBrush = border,
            Background = panel,
            Padding = new Thickness(1, 0, 1, 0),
            Margin = new Thickness(2, 2, 2, 2),
            Child = pair,
            SnapsToDevicePixels = true
        };
    }

    private void Rebuild()
    {
        var source = Parameters?.OfType<ParameterViewModel>()
            .ToDictionary(p => p.Id, StringComparer.Ordinal);
        if (source is null) { Content = null; return; }
        var buyIds = TraderBuysPolicy.Roles.SelectMany(role =>
            TraderBuysPolicy.Categories.Select(category => TraderBuysPolicy.ParameterId(role, category)));
        if (buyIds.Concat(TraderSellsPolicy.ParameterIds).Any(id => !source.ContainsKey(id)))
        {
            Content = Caption("Trader controls are missing from the active catalog.", 12,
                ThemeBrush("TextBrush", Brushes.White));
            return;
        }

        var foreground = ThemeBrush("TextBrush", Brushes.White);
        var muted = ThemeBrush("RadarMutedTextBrush", Brushes.LightGray);
        var accent = ThemeBrush("AccentBrush", Brushes.Gold);
        var border = ThemeBrush("BorderSoftBrush", Brushes.DimGray);
        var panel = ThemeBrush("RecessedParameterRowBrush", Brushes.Black);
        var rowAlt = ThemeBrush("ParameterRowBrush", Brushes.Black);
        var main = new StackPanel { Margin = new Thickness(7, 7, 7, 6) };
        var title = Caption("TRADERS", 12, accent);
        title.HorizontalAlignment = HorizontalAlignment.Left;
        title.Margin = new Thickness(3, 0, 0, 4);
        main.Children.Add(title);

        var board = new Grid { SnapsToDevicePixels = true };
        board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(115) });
        foreach (var _ in TraderBuysPolicy.Roles)
            board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
        board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(19) });
        foreach (var row in Rows)
            board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(row.Label == "CONSUMABLES" ? 47 : 32) });

        for (var c = 0; c < TraderBuysPolicy.Roles.Count; c++)
        {
            var role = TraderBuysPolicy.Roles[c];
            var heading = Caption(role.Label.ToUpperInvariant(), 10, foreground);
            heading.TextTrimming = TextTrimming.CharacterEllipsis;
            heading.ToolTip = role.Label;
            Grid.SetRow(heading, 0);
            Grid.SetColumn(heading, c + 1);
            board.Children.Add(heading);

            // The two labels orient EVERY role/category pair; only five
            // category-cell borders exist across a row, not ten.
            var actions = new Grid { Margin = new Thickness(2, 0, 2, 0) };
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(42) });
            actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var buy = Caption("BUY", 9, muted);
            buy.ToolTip = "Merchant buys from you";
            actions.Children.Add(buy);
            var sell = Caption("SELL", 9, muted);
            sell.ToolTip = "Merchant sells to you";
            Grid.SetColumn(sell, 1);
            actions.Children.Add(sell);
            Grid.SetRow(actions, 1);
            Grid.SetColumn(actions, c + 1);
            board.Children.Add(actions);
        }

        for (var r = 0; r < Rows.Length; r++)
        {
            var row = Rows[r];
            if (r % 2 == 1)
            {
                var band = new Border { Background = rowAlt, Opacity = 0.48, IsHitTestVisible = false };
                Grid.SetRow(band, r + 2);
                Grid.SetColumnSpan(band, TraderBuysPolicy.Roles.Count + 1);
                board.Children.Add(band);
            }
            if (r == 5 || r == 6)
            {
                var rule = new Border
                {
                    BorderBrush = border, BorderThickness = new Thickness(0, 1, 0, 0),
                    Opacity = 0.75, VerticalAlignment = VerticalAlignment.Top,
                    IsHitTestVisible = false
                };
                Grid.SetRow(rule, r + 2);
                Grid.SetColumnSpan(rule, TraderBuysPolicy.Roles.Count + 1);
                board.Children.Add(rule);
            }
            var category = Caption(row.Label, 10, foreground);
            category.HorizontalAlignment = HorizontalAlignment.Left;
            category.Margin = new Thickness(5, 0, 0, 0);
            category.ToolTip = row.Label == "CONSUMABLES"
                ? "BUY covers all consumables. SELL uses M = Medical, F = Food, D = Drinks."
                : row.Label;
            Grid.SetRow(category, r + 2);
            Grid.SetColumn(category, 0);
            board.Children.Add(category);

            for (var c = 0; c < TraderBuysPolicy.Roles.Count; c++)
            {
                var role = TraderBuysPolicy.Roles[c];
                var cell = TraderCategoryCell(row, role, source, panel, border, muted, accent);
                Grid.SetRow(cell, r + 2);
                Grid.SetColumn(cell, c + 1);
                board.Children.Add(cell);
            }
        }

        main.Children.Add(board);
        Content = new Border
        {
            Background = ThemeBrush("PanelBrush", Brushes.Black),
            BorderBrush = border, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(2), MaxWidth = 820,
            Child = main
        };
    }
}
