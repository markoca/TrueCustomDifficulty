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
/// Compact 5x8 Trader Buys board. Binds to the real per-cell toggle VMs,
/// so presets and the production writer observe the exact same values.
/// No custom checkbox chrome: the global App.xaml CheckBox template owns
/// its 20-DIP size and subtle border-on-hover appearance.
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

    private static TextBlock Caption(string label, double fontSize, Brush brush)
        => new()
        {
            Text = label,
            FontSize = fontSize,
            Foreground = brush,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

    private void Rebuild()
    {
        var source = Parameters?.OfType<ParameterViewModel>().ToDictionary(p => p.Id, StringComparer.Ordinal);
        if (source is null)
        {
            Content = null;
            return;
        }
        var needed = TraderBuysPolicy.Roles
            .SelectMany(role => TraderBuysPolicy.Categories.Select(category =>
                TraderBuysPolicy.ParameterId(role, category))).ToArray();
        if (needed.Any(id => !source.ContainsKey(id)))
        {
            Content = Caption("Trader Buy controls are missing from the active catalog.", 12,
                ThemeBrush("TextBrush", Brushes.White));
            return;
        }

        var foreground = ThemeBrush("TextBrush", Brushes.White);
        var muted = ThemeBrush("RadarMutedTextBrush", Brushes.LightGray);
        var accent = ThemeBrush("AccentBrush", Brushes.Gold);
        var softBorder = ThemeBrush("BorderSoftBrush", Brushes.DimGray);
        var panel = ThemeBrush("RecessedParameterRowBrush", Brushes.Black);
        var main = new StackPanel { Margin = new Thickness(8, 9, 8, 7) };
        var title = Caption("TRADER BUYS", 13, accent);
        title.HorizontalAlignment = HorizontalAlignment.Left;
        title.Margin = new Thickness(3, 1, 0, 0);
        main.Children.Add(title);

        var subtitle = Caption("CHECKED: BUYS FROM YOU   •   UNCHANGED: ORIGINAL RULES",
            11, muted);
        subtitle.HorizontalAlignment = HorizontalAlignment.Left;
        subtitle.Margin = new Thickness(3, 2, 0, 9);
        main.Children.Add(subtitle);

        var board = new Grid { Margin = new Thickness(0), SnapsToDevicePixels = true };
        board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(132) });
        for (var c = 0; c < TraderBuysPolicy.Categories.Count; c++)
            board.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(46) });
        for (var r = 0; r < TraderBuysPolicy.Roles.Count; r++)
            board.RowDefinitions.Add(new RowDefinition { Height = new GridLength(42) });

        for (var c = 0; c < TraderBuysPolicy.Categories.Count; c++)
        {
            var category = TraderBuysPolicy.Categories[c];
            // Deliberate line breaks: WPF does not break long uppercase category
            // words reliably inside these eight equal grid columns.
            var headingText = category.Id switch
            {
                "attach" => "ATTACH-\\nMENTS",
                "grenade" => "GRE-\\nNADES",
                "artifact" => "ARTI-\\nFACTS",
                "consumable" => "CONSUM-\\nABLES",
                "mutantLoot" => "MUTANT\\nPARTS",
                _ => category.Label.ToUpperInvariant()
            };
            var heading = Caption(headingText.Replace("\\n", "\n"), 10.5, muted);
            heading.TextTrimming = TextTrimming.CharacterEllipsis;
            heading.ToolTip = category.Label;
            heading.Margin = new Thickness(2, 0, 2, 3);
            Grid.SetRow(heading, 0);
            Grid.SetColumn(heading, c + 1);
            board.Children.Add(heading);
        }

        for (var r = 0; r < TraderBuysPolicy.Roles.Count; r++)
        {
            var role = TraderBuysPolicy.Roles[r];
            var rowName = Caption(role.Label.ToUpperInvariant(), 10.7, foreground);
            rowName.HorizontalAlignment = HorizontalAlignment.Left;
            rowName.Margin = new Thickness(7, 0, 5, 0);
            Grid.SetRow(rowName, r + 1);
            Grid.SetColumn(rowName, 0);
            board.Children.Add(rowName);

            for (var c = 0; c < TraderBuysPolicy.Categories.Count; c++)
            {
                var category = TraderBuysPolicy.Categories[c];
                var parameter = source[TraderBuysPolicy.ParameterId(role, category)];
                var cell = new Border
                {
                    CornerRadius = new CornerRadius(3),
                    BorderThickness = new Thickness(1),
                    Background = panel,
                    Margin = new Thickness(2, 2, 2, 2),
                    DataContext = parameter,
                    SnapsToDevicePixels = true
                };
                var cellStyle = new Style(typeof(Border));
                cellStyle.Setters.Add(new Setter(Border.BorderBrushProperty, softBorder));
                cellStyle.Triggers.Add(new DataTrigger
                {
                    Binding = new Binding(nameof(ParameterViewModel.IsOwnValueModified)),
                    Value = true,
                    Setters = { new Setter(Border.BorderBrushProperty, accent) }
                });
                cell.Style = cellStyle;

                var box = new CheckBox
                {
                    DataContext = parameter,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    ToolTip = role.Label + " · " + category.Label +
                        ". Checked allows buying when explicitly overridden. " +
                        "Untouched cells preserve each merchant's original rules, including exceptions. " +
                        (category.NativeType == "Consumable" ?
                            "Food, drinks and medicine share this one game item type." : "")
                };
                box.SetBinding(ToggleButton.IsCheckedProperty, new Binding(nameof(ParameterViewModel.CurrentBoolean))
                {
                    Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                });
                box.SetBinding(IsEnabledProperty, new Binding(nameof(ParameterViewModel.IsControlEnabled)));
                box.PreviewMouseLeftButtonDown += (_, _) =>
                    ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(parameter));
                cell.Child = box;
                Grid.SetRow(cell, r + 1);
                Grid.SetColumn(cell, c + 1);
                board.Children.Add(cell);
            }
        }

        main.Children.Add(board);
        var footer = Caption(
            "CONSUMABLES = FOOD + DRINKS + MEDICAL   •   EGER / BOLSHAK KEEP UNIQUE RULES UNTIL CHANGED",
            10.5, muted);
        footer.Margin = new Thickness(3, 9, 3, 1);
        footer.HorizontalAlignment = HorizontalAlignment.Left;
        main.Children.Add(footer);

        Content = new Border
        {
            Background = ThemeBrush("PanelBrush", Brushes.Black),
            BorderBrush = softBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Child = main,
            Margin = new Thickness(2)
        };
    }
}
