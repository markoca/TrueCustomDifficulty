using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Category-only original trader stock editor. Native SELL toggles are the
/// backing model; legacy per-SID exclusions remain readable but not editable.
/// </summary>
public sealed class TraderBuysMatrix : UserControl
{
    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters), typeof(IEnumerable), typeof(TraderBuysMatrix),
        new PropertyMetadata(null, (o, _) => ((TraderBuysMatrix)o).Rebuild()));

    public static readonly DependencyProperty TraderItemOverridesProperty = DependencyProperty.Register(
        nameof(TraderItemOverrides), typeof(string), typeof(TraderBuysMatrix),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (o, _) => ((TraderBuysMatrix)o).Refresh()));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public string TraderItemOverrides
    {
        get => (string)GetValue(TraderItemOverridesProperty);
        set => SetValue(TraderItemOverridesProperty, value);
    }

    public event EventHandler<AggregateParameterSelectedEventArgs>? ParameterSelected;

    private Dictionary<string, ParameterViewModel> _source = new(StringComparer.Ordinal);
    private readonly List<ParameterViewModel> _observed = new();
    private readonly Dictionary<string, CheckBox> _cells = new(StringComparer.Ordinal);
    private TextBlock? _legacyNotice;
    private string _roleId = "general";
    private StackPanel? _roles;
    private StackPanel? _categories;

    private static Brush BrushFor(string name, Brush fallback) =>
        Application.Current?.TryFindResource(name) as Brush ?? fallback;

    private static TextBlock Text(string value, double size, Brush color) => new()
    {
        Text = value, FontSize = size, Foreground = color,
        FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis
    };

    private void Rebuild()
    {
        foreach (var p in _observed) p.PropertyChanged -= OnParameterChanged;
        _observed.Clear();
        _cells.Clear();
        if (Parameters is null) { Content = null; return; }
        _source = Parameters.OfType<ParameterViewModel>()
            .ToDictionary(p => p.Id, StringComparer.Ordinal);
        if (!_source.ContainsKey(TraderSellsPolicy.AnchorId) ||
            TraderSellsPolicy.ParameterIds.Any(id => !_source.ContainsKey(id)))
        {
            Content = Text("TRADER STOCK CATALOG MISSING", 11, Brushes.White);
            return;
        }
        foreach (var id in TraderSellsPolicy.ParameterIds.Concat(
            TraderBuysPolicy.Roles.SelectMany(r =>
                TraderBuysPolicy.Categories.Select(c => TraderBuysPolicy.ParameterId(r, c)))))
        {
            if (!_source.TryGetValue(id, out var p)) continue;
            p.PropertyChanged += OnParameterChanged;
            _observed.Add(p);
        }

        var accent = BrushFor("AccentBrush", Brushes.Gold);
        var fg = BrushFor("TextBrush", Brushes.White);
        var muted = BrushFor("RadarMutedTextBrush", Brushes.LightGray);
        var border = BrushFor("BorderSoftBrush", Brushes.DimGray);
        var root = new StackPanel { Margin = new Thickness(10) };
        root.Children.Add(Text("TRADER STOCK", 13, accent));
        root.Children.Add(new TextBlock
        {
            Text = "ON keeps original offers  ·  OFF removes the category",
            Foreground = muted, FontSize = 10, Margin = new Thickness(0, 4, 0, 10)
        });
        _roles = new StackPanel { Orientation = Orientation.Horizontal };
        root.Children.Add(_roles);
        _legacyNotice = Text("", 10, muted);
        _legacyNotice.TextWrapping = TextWrapping.Wrap;
        _legacyNotice.Margin = new Thickness(2, 8, 2, 3);
        root.Children.Add(_legacyNotice);
        _categories = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        root.Children.Add(_categories);
        Content = new Border
        {
            MaxWidth = 820, Margin = new Thickness(2), BorderBrush = border,
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4),
            Background = BrushFor("PanelBrush", Brushes.Black), Child = root
        };
        RenderRoles();
        RenderCategories();
    }

    private void RenderRoles()
    {
        if (_roles is null) return;
        _roles.Children.Clear();
        var accent = BrushFor("AccentBrush", Brushes.Gold);
        var fg = BrushFor("TextBrush", Brushes.White);
        var border = BrushFor("BorderSoftBrush", Brushes.DimGray);
        foreach (var role in TraderSellsPolicy.Roles)
        {
            var selected = role.Id == _roleId;
            var button = new Button
            {
                Content = role.Label.ToUpperInvariant(),
                Height = 29, FontSize = 10, Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 0, 4, 0),
                Foreground = selected ? accent : fg,
                BorderBrush = selected ? accent : border,
                BorderThickness = new Thickness(1)
            };
            button.Click += (_, _) =>
            {
                _roleId = role.Id;
                RenderRoles();
                RenderCategories();
            };
            _roles.Children.Add(button);
        }
    }

    private void RenderCategories()
    {
        if (_categories is null) return;
        _categories.Children.Clear();
        _cells.Clear();
        var role = TraderSellsPolicy.Roles.Single(r => r.Id == _roleId);
        var fg = BrushFor("TextBrush", Brushes.White);
        var muted = BrushFor("RadarMutedTextBrush", Brushes.LightGray);
        var accent = BrushFor("AccentBrush", Brushes.Gold);
        var border = BrushFor("BorderSoftBrush", Brushes.DimGray);
        var background = BrushFor("RecessedParameterRowBrush", Brushes.Black);

        foreach (var category in TraderSellsPolicy.Categories.Where(c =>
            TraderSellsPolicy.IsAvailable(role, c)))
        {
            var id = TraderSellsPolicy.ParameterId(role, category);
            var parameter = _source[id];
            var grid = new Grid { Height = 34 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(58) });
            var name = Text(category.Label.ToUpperInvariant(), 11, fg);
            name.Margin = new Thickness(10, 0, 0, 0);
            grid.Children.Add(name);
            var cell = new CheckBox
            {
                IsChecked = parameter.CurrentBoolean,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "ON: original " + category.Label + " stock. OFF: remove " +
                          category.Label + " from " + role.Label + " stock."
            };
            cell.Click += (_, _) =>
            {
                parameter.CurrentBoolean = cell.IsChecked == true;
                ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(parameter));
                Refresh();
            };
            var outline = new Border
            {
                BorderBrush = parameter.IsValueModifiedFromReference() ? accent : Brushes.Transparent,
                BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(3),
                Width = 28, Height = 27, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center, Child = cell
            };
            Grid.SetColumn(outline, 1);
            grid.Children.Add(outline);
            _categories.Children.Add(new Border
            {
                BorderBrush = border, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3), Background = background,
                Margin = new Thickness(1, 2, 1, 2), Child = grid
            });
            _cells[id] = cell;
        }
        Refresh();
    }

    private void OnParameterChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ParameterViewModel.CurrentBoolean) or
            nameof(ParameterViewModel.IsOwnValueModified)) Refresh();
    }

    private void Refresh()
    {
        foreach (var (id, checkbox) in _cells)
        {
            if (!_source.TryGetValue(id, out var parameter)) continue;
            checkbox.IsChecked = parameter.CurrentBoolean;
            if (checkbox.Parent is Border outline)
                outline.BorderBrush = parameter.IsValueModifiedFromReference()
                    ? BrushFor("AccentBrush", Brushes.Gold) : Brushes.Transparent;
        }
        if (_legacyNotice is null) return;
        var itemCount = TraderOffersPolicy.Decode(TraderItemOverrides).Count;
        var buyCount = TraderBuysPolicy.Roles.SelectMany(role =>
            TraderBuysPolicy.Categories.Select(cat => TraderBuysPolicy.ParameterId(role, cat)))
            .Count(id => _source.TryGetValue(id, out var p) && p.IsValueModifiedFromReference());
        var notes = new List<string>();
        if (itemCount > 0)
            notes.Add(itemCount + " legacy item-specific exclusions remain active from an older preset.");
        if (buyCount > 0)
            notes.Add(buyCount + " legacy BUY restrictions remain active from an older preset.");
        _legacyNotice.Text = string.Join("  ", notes);
        _legacyNotice.Visibility = notes.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
