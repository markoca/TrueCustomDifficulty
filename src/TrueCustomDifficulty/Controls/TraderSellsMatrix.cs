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
/// Native merchant stock suppression per seller role. Does not conflate
/// player-to-trader BuyLimitations with trader-to-player generated offers.
/// </summary>
public sealed class TraderSellsMatrix : UserControl
{
    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters), typeof(IEnumerable), typeof(TraderSellsMatrix),
        new PropertyMetadata(null, (sender, _) => ((TraderSellsMatrix)sender).Rebuild()));
    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }
    public event EventHandler<AggregateParameterSelectedEventArgs>? ParameterSelected;
    private static Brush ThemeBrush(string key, Brush fallback) =>
        Application.Current?.TryFindResource(key) as Brush ?? fallback;

    private static TextBlock Caption(string text, double size, Brush brush)
        => new()
        {
            Text = text, FontSize = size, Foreground = brush,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

    private void Rebuild()
    {
        var source = Parameters?.OfType<ParameterViewModel>().ToDictionary(p => p.Id, StringComparer.Ordinal);
        if (source is null) { Content = null; return; }
        if (TraderSellsPolicy.ParameterIds.Any(id => !source.ContainsKey(id)))
        {
            Content = Caption("Trader Sells controls are missing from the catalog.", 12,
                ThemeBrush("TextBrush", Brushes.White));
            return;
        }
        var fg = ThemeBrush("TextBrush", Brushes.White);
        var muted = ThemeBrush("RadarMutedTextBrush", Brushes.LightGray);
        var accent = ThemeBrush("AccentBrush", Brushes.Gold);
        var border = ThemeBrush("BorderSoftBrush", Brushes.DimGray);
        var panel = ThemeBrush("RecessedParameterRowBrush", Brushes.Black);
        var main = new StackPanel { Margin = new Thickness(8, 9, 8, 7) };

        var title = Caption("TRADER SELLS", 13, accent);
        title.HorizontalAlignment = HorizontalAlignment.Left;
        main.Children.Add(title);
        var subtitle = Caption("CHECKED = ORIGINAL STOCK   •   UNCHECKED = REMOVE CATEGORY", 10.5, muted);
        subtitle.HorizontalAlignment = HorizontalAlignment.Left;
        subtitle.Margin = new Thickness(2, 2, 0, 8);
        main.Children.Add(subtitle);

        var board = new Grid { SnapsToDevicePixels = true };
        board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(123) });
        foreach (var _ in TraderSellsPolicy.Categories)
            board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(49) });
        foreach (var _ in TraderSellsPolicy.Roles)
            board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(40) });

        for (var col = 0; col < TraderSellsPolicy.Categories.Count; col++)
        {
            var category = TraderSellsPolicy.Categories[col];
            var caption = category.Id switch
            {
                "weapons" => "WEAPONS",
                "armor" => "ARMOR",
                "attachments" => "ATTACH-\nMENTS",
                "ammo" => "AMMO",
                "medical" => "MEDICAL",
                "food" => "FOOD",
                "drinks" => "DRINKS",
                "artifacts" => "ARTI-\nFACTS",
                "detectors" => "DETEC-\nTORS",
                "grenades" => "GRE-\nNADES",
                _ => category.Label.ToUpperInvariant()
            };
            var heading = Caption(caption, 10, muted);
            heading.TextTrimming = TextTrimming.CharacterEllipsis;
            heading.Margin = new Thickness(2, 0, 2, 1);
            heading.ToolTip = category.Label;
            Grid.SetRow(heading, 0); Grid.SetColumn(heading, col + 1);
            board.Children.Add(heading);
        }

        for (var row = 0; row < TraderSellsPolicy.Roles.Count; row++)
        {
            var role = TraderSellsPolicy.Roles[row];
            var heading = Caption(role.Label.ToUpperInvariant(), 10.5, fg);
            heading.HorizontalAlignment = HorizontalAlignment.Left;
            heading.Margin = new Thickness(5, 0, 2, 0);
            Grid.SetRow(heading, row + 1); Grid.SetColumn(heading, 0);
            board.Children.Add(heading);

            for (var col = 0; col < TraderSellsPolicy.Categories.Count; col++)
            {
                var category = TraderSellsPolicy.Categories[col];
                var available = TraderSellsPolicy.IsAvailable(role, category);
                var cell = new Border
                {
                    CornerRadius = new CornerRadius(3),
                    BorderThickness = new Thickness(1),
                    Background = panel,
                    Margin = new Thickness(2),
                    SnapsToDevicePixels = true
                };
                var style = new Style(typeof(Border));
                style.Setters.Add(new Setter(Border.BorderBrushProperty, border));
                if (available)
                {
                    var parameter = source[TraderSellsPolicy.ParameterId(role, category)];
                    cell.DataContext = parameter;
                    style.Triggers.Add(new DataTrigger
                    {
                        Binding = new Binding(nameof(ParameterViewModel.IsOwnValueModified)),
                        Value = true,
                        Setters = { new Setter(Border.BorderBrushProperty, accent) }
                    });
                    var check = new CheckBox
                    {
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        ToolTip = role.Label + " · " + category.Label +
                            ". OFF removes this category from this merchant type on stock regeneration. " +
                            "ON keeps original random offers and rank limits."
                    };
                    check.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(ParameterViewModel.CurrentBoolean))
                    {
                        Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    });
                    check.SetBinding(IsEnabledProperty, new Binding(nameof(ParameterViewModel.IsControlEnabled)));
                    check.PreviewMouseLeftButtonDown += (_, _) =>
                        ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(parameter));
                    cell.Child = check;
                }
                else
                {
                    var missing = Caption("—", 14, muted);
                    missing.ToolTip = "No original offers in this category for this merchant type.";
                    cell.Child = missing;
                }
                cell.Style = style;
                Grid.SetRow(cell, row + 1); Grid.SetColumn(cell, col + 1);
                board.Children.Add(cell);
            }
        }
        main.Children.Add(board);
        var footer = Caption(
            "— = NO ORIGINAL OFFER   •   RANK, CHANCE AND REFRESH RULES ARE UNCHANGED",
            10.2, muted);
        footer.Margin = new Thickness(2, 9, 2, 0);
        footer.HorizontalAlignment = HorizontalAlignment.Left;
        main.Children.Add(footer);

        Content = new Border
        {
            Background = ThemeBrush("PanelBrush", Brushes.Black),
            BorderBrush = border, BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4), Child = main,
            Margin = new Thickness(2)
        };
    }
}
