using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// One compact merchant matrix with independent buying and stock switches.
/// The two source-backed CFG systems and every existing parameter ID remain separate.
/// </summary>
public sealed class TraderBuysMatrix : UserControl
{
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

    private static TextBlock Caption(string label, double size, Brush brush) => new()
    {
        Text = label, FontSize = size, Foreground = brush,
        FontWeight = FontWeights.SemiBold,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        TextAlignment = TextAlignment.Center,
        TextWrapping = TextWrapping.Wrap
    };

    private void AddCell(Grid board, int row, int col, ParameterViewModel? parameter,
        string role, string category, bool isBuying, Brush panel, Brush muted,
        Brush softBorder, Brush accent)
    {
        var cell = new Border
        {
            CornerRadius = new CornerRadius(3),
            BorderThickness = new Thickness(1),
            Background = panel,
            Margin = new Thickness(1, 1, 1, 1),
            SnapsToDevicePixels = true
        };
        var style = new Style(typeof(Border));
        style.Setters.Add(new Setter(Border.BorderBrushProperty, softBorder));
        if (parameter is not null)
        {
            cell.DataContext = parameter;
            style.Triggers.Add(new DataTrigger
            {
                Binding = new Binding(nameof(ParameterViewModel.IsOwnValueModified)),
                Value = true,
                Setters = { new Setter(Border.BorderBrushProperty, accent) }
            });
            var box = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = isBuying
                    ? role + " · " + category + " · BUYS FROM YOU. Untouched = original rules." +
                      (category == "CONSUMABLES" ? " Includes medical, food and drinks." : "")
                    : role + " · " + category + " · SELLS TO YOU. Checked = original offers; unchecked = remove stock."
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
            cell.Child = box;
        }
        else
        {
            var missing = Caption("—", 12, muted);
            missing.ToolTip = isBuying
                ? "This purchase category has no separate native buying rule."
                : "No original stock offer for this trader and category.";
            cell.Child = missing;
        }
        cell.Style = style;
        Grid.SetRow(cell, row);
        Grid.SetColumn(cell, col);
        board.Children.Add(cell);
    }

    private void Rebuild()
    {
        var source = Parameters?.OfType<ParameterViewModel>()
            .ToDictionary(p => p.Id, StringComparer.Ordinal);
        if (source is null) { Content = null; return; }
        var buys = TraderBuysPolicy.Roles
            .SelectMany(role => TraderBuysPolicy.Categories
                .Select(category => TraderBuysPolicy.ParameterId(role, category)));
        var needed = buys.Concat(TraderSellsPolicy.ParameterIds);
        if (needed.Any(id => !source.ContainsKey(id)))
        {
            Content = Caption("Trader controls are missing from the active catalog.", 12,
                ThemeBrush("TextBrush", Brushes.White));
            return;
        }

        var fg = ThemeBrush("TextBrush", Brushes.White);
        var muted = ThemeBrush("RadarMutedTextBrush", Brushes.LightGray);
        var accent = ThemeBrush("AccentBrush", Brushes.Gold);
        var softBorder = ThemeBrush("BorderSoftBrush", Brushes.DimGray);
        var panel = ThemeBrush("RecessedParameterRowBrush", Brushes.Black);
        var main = new StackPanel { Margin = new Thickness(5, 5, 5, 5) };
        var title = Caption("TRADERS", 12, accent);
        title.HorizontalAlignment = HorizontalAlignment.Left;
        title.Margin = new Thickness(3, 0, 0, 1);
        main.Children.Add(title);

        // "Consumables" is a single native buy type, while seller stock has
        // independent Medical/Food/Drinks. Do not invent three buy switches.
        var rows = new (string Label, string? BuyId, string? SellId)[]
        {
            ("WEAPONS", "weapon", "weapons"),
            ("ARMOR", "armor", "armor"),
            ("ATTACHMENTS", "attach", "attachments"),
            ("AMMO", "ammo", "ammo"),
            ("GRENADES", "grenade", "grenades"),
            ("ARTIFACTS", "artifact", "artifacts"),
            ("CONSUMABLES", "consumable", null),
            ("MEDICAL", null, "medical"),
            ("FOOD", null, "food"),
            ("DRINKS", null, "drinks"),
            ("DETECTORS", null, "detectors"),
            ("MUTANT PARTS", "mutantLoot", null)
        };
        var board = new Grid { SnapsToDevicePixels = true };
        board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(112) });
        foreach (var _ in TraderBuysPolicy.Roles)
        {
            board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        }
        board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(27) });
        board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(19) });
        foreach (var _ in rows)
            board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });

        for (var c = 0; c < TraderBuysPolicy.Roles.Count; c++)
        {
            var role = TraderBuysPolicy.Roles[c];
            var name = Caption(role.Label.ToUpperInvariant(), 10, fg);
            name.TextTrimming = TextTrimming.CharacterEllipsis;
            name.ToolTip = role.Label;
            Grid.SetRow(name, 0);
            Grid.SetColumn(name, c * 2 + 1);
            Grid.SetColumnSpan(name, 2);
            board.Children.Add(name);
            foreach (var isBuying in new[] { true, false })
            {
                var label = Caption(isBuying ? "BUY" : "SELL", 9.5, muted);
                label.ToolTip = isBuying ? "Trader buys from you" : "Trader sells to you";
                Grid.SetRow(label, 1);
                Grid.SetColumn(label, c * 2 + (isBuying ? 1 : 2));
                board.Children.Add(label);
            }
        }

        for (var r = 0; r < rows.Length; r++)
        {
            var row = rows[r];
            var name = Caption(row.Label, 10, fg);
            name.HorizontalAlignment = HorizontalAlignment.Left;
            name.Margin = new Thickness(5, 0, 0, 0);
            Grid.SetRow(name, r + 2);
            board.Children.Add(name);
            for (var c = 0; c < TraderBuysPolicy.Roles.Count; c++)
            {
                var role = TraderBuysPolicy.Roles[c];
                ParameterViewModel? buy = null;
                ParameterViewModel? sell = null;
                if (row.BuyId is not null)
                {
                    var category = TraderBuysPolicy.Categories.Single(x => x.Id == row.BuyId);
                    buy = source[TraderBuysPolicy.ParameterId(role, category)];
                }
                if (row.SellId is not null)
                {
                    var category = TraderSellsPolicy.Categories.Single(x => x.Id == row.SellId);
                    if (TraderSellsPolicy.IsAvailable(role, category))
                        sell = source[TraderSellsPolicy.ParameterId(role, category)];
                }
                AddCell(board, r + 2, c * 2 + 1, buy, role.Label, row.Label,
                    true, panel, muted, softBorder, accent);
                AddCell(board, r + 2, c * 2 + 2, sell, role.Label, row.Label,
                    false, panel, muted, softBorder, accent);
            }
        }
        main.Children.Add(board);
        Content = new Border
        {
            Background = ThemeBrush("PanelBrush", Brushes.Black),
            BorderBrush = softBorder, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4), Child = main,
            Margin = new Thickness(2), MaxWidth = 820
        };
    }
}
