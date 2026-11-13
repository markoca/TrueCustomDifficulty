using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Source-audited trader inventory editor. Pick a seller, filter native stock
/// items, and toggle individual item SIDs. No player-to-trader BUY matrix.
/// </summary>
public sealed class TraderBuysMatrix : UserControl
{
    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters), typeof(IEnumerable), typeof(TraderBuysMatrix),
        new PropertyMetadata(null, (o, _) => ((TraderBuysMatrix)o).Rebuild()));

    public static readonly DependencyProperty TraderItemOverridesProperty = DependencyProperty.Register(
        nameof(TraderItemOverrides), typeof(string), typeof(TraderBuysMatrix),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            (o, _) => ((TraderBuysMatrix)o).RefreshItems()));

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
    private readonly List<ParameterViewModel> _observedLegacy = new();
    private string _roleId = "general";
    private string _category = "ALL";
    private TextBox? _search;
    private ComboBox? _categories;
    private StackPanel? _rows;
    private TextBlock? _count;
    private StackPanel? _roleButtons;
    private Border? _legacyNotice;

    private static Brush ThemeBrush(string name, Brush fallback) =>
        Application.Current?.TryFindResource(name) as Brush ?? fallback;

    private static TextBlock Text(string value, double size, Brush color) => new()
    {
        Text = value, FontSize = size, FontWeight = FontWeights.SemiBold,
        Foreground = color, VerticalAlignment = VerticalAlignment.Center,
        TextTrimming = TextTrimming.CharacterEllipsis
    };

    private static string LabelForSid(string sid)
    {
        var name = sid.Replace('_', ' ');
        return Regex.Replace(name, @"(?<=[a-z])(?=[A-Z])", " ");
    }

    private void Rebuild()
    {
        foreach (var old in _observedLegacy)
            old.PropertyChanged -= LegacyChanged;
        _observedLegacy.Clear();

        if (Parameters is null) { Content = null; return; }
        _source = Parameters.OfType<ParameterViewModel>()
            .ToDictionary(x => x.Id, StringComparer.Ordinal);
        if (!_source.ContainsKey(TraderSellsPolicy.AnchorId)
            || TraderSellsPolicy.ParameterIds.Any(id => !_source.ContainsKey(id)))
        {
            Content = Text("Trader stock controls are missing from the catalog.",
                12, ThemeBrush("TextBrush", Brushes.White));
            return;
        }

        foreach (var id in TraderSellsPolicy.ParameterIds)
        {
            var parameter = _source[id];
            parameter.PropertyChanged += LegacyChanged;
            _observedLegacy.Add(parameter);
        }
        foreach (var role in TraderBuysPolicy.Roles)
        foreach (var category in TraderBuysPolicy.Categories)
        {
            var id = TraderBuysPolicy.ParameterId(role, category);
            if (_source.TryGetValue(id, out var buyer))
            {
                buyer.PropertyChanged += LegacyChanged;
                _observedLegacy.Add(buyer);
            }
        }

        var foreground = ThemeBrush("TextBrush", Brushes.White);
        var muted = ThemeBrush("RadarMutedTextBrush", Brushes.LightGray);
        var accent = ThemeBrush("AccentBrush", Brushes.Gold);
        var border = ThemeBrush("BorderSoftBrush", Brushes.DimGray);
        var panel = ThemeBrush("PanelBrush", Brushes.Black);
        var root = new StackPanel { Margin = new Thickness(10, 9, 10, 9) };

        var title = Text("TRADER STOCK", 13, accent);
        title.Margin = new Thickness(2, 0, 0, 7);
        root.Children.Add(title);

        _roleButtons = new StackPanel { Orientation = Orientation.Horizontal };
        root.Children.Add(_roleButtons);

        var tools = new Grid { Margin = new Thickness(1, 9, 1, 6) };
        tools.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(154) });
        tools.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        tools.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(78) });

        _categories = new ComboBox
        {
            Height = 29,
            Margin = new Thickness(0, 0, 6, 0),
            ToolTip = "Show a single original stock category"
        };
        _categories.Items.Add("ALL");
        foreach (var category in TraderOffersPolicy.Offers
            .Select(x => x.Category).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal))
            _categories.Items.Add(category);
        _categories.SelectedItem = _category;
        if (_categories.SelectedIndex < 0) _categories.SelectedIndex = 0;
        _categories.SelectionChanged += (_, _) =>
        {
            _category = _categories.SelectedItem as string ?? "ALL";
            RefreshItems();
        };
        tools.Children.Add(_categories);

        _search = new TextBox
        {
            Height = 29,
            FontSize = 11,
            VerticalContentAlignment = VerticalAlignment.Center,
            ToolTip = "Filter by item name or exact source item SID",
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(7, 0, 5, 0)
        };
        _search.TextChanged += (_, _) => RefreshItems();
        Grid.SetColumn(_search, 1);
        tools.Children.Add(_search);

        _count = Text("", 10, muted);
        _count.HorizontalAlignment = HorizontalAlignment.Right;
        Grid.SetColumn(_count, 2);
        tools.Children.Add(_count);
        root.Children.Add(tools);

        _legacyNotice = new Border
        {
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            Background = ThemeBrush("RecessedParameterRowBrush", Brushes.Black),
            CornerRadius = new CornerRadius(3),
            Margin = new Thickness(1, 0, 1, 6),
            Visibility = Visibility.Collapsed
        };
        root.Children.Add(_legacyNotice);

        _rows = new StackPanel();
        var scroll = new ScrollViewer
        {
            Content = _rows,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            MaxHeight = 365,
            Focusable = false
        };
        root.Children.Add(scroll);
        Content = new Border
        {
            Margin = new Thickness(2),
            MaxWidth = 820,
            BorderBrush = border,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Background = panel,
            Child = root
        };
        RebuildRoleButtons();
        RefreshItems();
    }

    private void LegacyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ParameterViewModel.CurrentBoolean) or
            nameof(ParameterViewModel.IsOwnValueModified))
            RefreshItems();
    }

    private void RebuildRoleButtons()
    {
        if (_roleButtons is null) return;
        _roleButtons.Children.Clear();
        var accent = ThemeBrush("AccentBrush", Brushes.Gold);
        var foreground = ThemeBrush("TextBrush", Brushes.White);
        var muted = ThemeBrush("RadarMutedTextBrush", Brushes.LightGray);
        var border = ThemeBrush("BorderSoftBrush", Brushes.DimGray);
        foreach (var role in TraderBuysPolicy.Roles)
        {
            var active = role.Id == _roleId;
            var button = new Button
            {
                Content = role.Label.ToUpperInvariant(),
                Height = 29,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = active ? accent : foreground,
                Background = ThemeBrush("RecessedParameterRowBrush", Brushes.Black),
                BorderBrush = active ? accent : border,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 0, 4, 0),
                ToolTip = "Edit items offered by " + role.Label
            };
            button.Click += (_, _) =>
            {
                if (_roleId == role.Id) return;
                _roleId = role.Id;
                RebuildRoleButtons();
                RefreshItems();
            };
            _roleButtons.Children.Add(button);
        }
    }

    private bool CategoryDisabled(TraderOffersPolicy.Offer offer)
    {
        var role = TraderSellsPolicy.Roles.Single(x => x.Id == offer.RoleId);
        var category = TraderSellsPolicy.Categories.FirstOrDefault(x =>
            x.Label == offer.Category);
        if (category is null || !TraderSellsPolicy.IsAvailable(role, category))
            return false;
        return !_source[TraderSellsPolicy.ParameterId(role, category)].CurrentBoolean;
    }

    private void SetItem(TraderOffersPolicy.Offer offer, bool enabled)
    {
        var disabled = TraderOffersPolicy.Decode(TraderItemOverrides)
            .ToHashSet(StringComparer.Ordinal);
        var key = TraderOffersPolicy.Key(offer.RoleId, offer.ItemSid);
        if (!enabled)
        {
            disabled.Add(key);
        }
        else
        {
            // Old presets can have a role-wide stock-category OFF. Convert that
            // one legacy ban into individual exclusions before enabling one item.
            if (CategoryDisabled(offer))
            {
                foreach (var sibling in TraderOffersPolicy.Offers.Where(x =>
                    x.RoleId == offer.RoleId && x.Category == offer.Category))
                    disabled.Add(TraderOffersPolicy.Key(sibling.RoleId, sibling.ItemSid));
                var role = TraderSellsPolicy.Roles.Single(x => x.Id == offer.RoleId);
                var category = TraderSellsPolicy.Categories.Single(x => x.Label == offer.Category);
                _source[TraderSellsPolicy.ParameterId(role, category)].CurrentBoolean = true;
            }
            disabled.Remove(key);
        }
        TraderItemOverrides = TraderOffersPolicy.Encode(disabled);
        if (_source.TryGetValue(TraderSellsPolicy.AnchorId, out var anchor))
            ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(anchor));
    }

    private void UpdateLegacyNotice()
    {
        if (_legacyNotice is null) return;
        var modified = TraderBuysPolicy.Roles.SelectMany(role =>
                TraderBuysPolicy.Categories.Select(category =>
                    TraderBuysPolicy.ParameterId(role, category)))
            .Where(id => _source.TryGetValue(id, out var parameter) &&
                         parameter.IsValueModifiedFromReference()).ToArray();
        _legacyNotice.Visibility = modified.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (modified.Length == 0) return;

        var muted = ThemeBrush("RadarMutedTextBrush", Brushes.LightGray);
        var accent = ThemeBrush("AccentBrush", Brushes.Gold);
        var line = new DockPanel { Margin = new Thickness(6, 2, 6, 2) };
        var label = Text("OLD PRESET: BUY RULES STILL ACTIVE", 9.5, muted);
        DockPanel.SetDock(label, Dock.Left);
        line.Children.Add(label);
        var reset = new Button
        {
            Content = "RESET BUY RULES",
            HorizontalAlignment = HorizontalAlignment.Right,
            Foreground = accent,
            FontSize = 9,
            Margin = new Thickness(12, 0, 0, 0),
            Padding = new Thickness(5, 2, 5, 2)
        };
        reset.Click += (_, _) =>
        {
            foreach (var id in modified)
                _source[id].ResetToReference();
            RefreshItems();
        };
        DockPanel.SetDock(reset, Dock.Right);
        line.Children.Add(reset);
        _legacyNotice.Child = line;
    }

    private void RefreshItems()
    {
        if (_rows is null || _count is null || _source.Count == 0) return;
        var disabled = TraderOffersPolicy.Decode(TraderItemOverrides);
        var query = _search?.Text.Trim() ?? string.Empty;
        var offers = TraderOffersPolicy.Offers.Where(x =>
            x.RoleId == _roleId &&
            (_category == "ALL" || x.Category == _category) &&
            (query.Length == 0 || x.ItemSid.Contains(query, StringComparison.OrdinalIgnoreCase) ||
             LabelForSid(x.ItemSid).Contains(query, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        _rows.Children.Clear();
        UpdateLegacyNotice();
        var fg = ThemeBrush("TextBrush", Brushes.White);
        var muted = ThemeBrush("RadarMutedTextBrush", Brushes.LightGray);
        var accent = ThemeBrush("AccentBrush", Brushes.Gold);
        var border = ThemeBrush("BorderSoftBrush", Brushes.DimGray);
        var panel = ThemeBrush("RecessedParameterRowBrush", Brushes.Black);
        var shown = 0;
        var available = 0;
        string? lastCategory = null;
        foreach (var offer in offers)
        {
            if (_category == "ALL" && lastCategory != offer.Category)
            {
                var heading = Text(offer.Category.ToUpperInvariant(), 9.5, muted);
                heading.Margin = new Thickness(6, 9, 0, 3);
                _rows.Children.Add(heading);
                lastCategory = offer.Category;
            }
            var key = TraderOffersPolicy.Key(offer.RoleId, offer.ItemSid);
            var checkedState = !disabled.Contains(key) && !CategoryDisabled(offer);
            if (checkedState && !offer.ZeroChanceOnly) available++;
            shown++;

            var grid = new Grid { Height = 31 };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(48) });
            var name = Text(LabelForSid(offer.ItemSid), 10.8, checkedState ? fg : muted);
            name.Margin = new Thickness(7, 0, 3, 0);
            name.ToolTip = offer.ItemSid + " · " + offer.Category +
                (offer.Dlc1Only ? " · DLC1 stock only" : "") +
                (offer.ZeroChanceOnly ? " · Original chance is zero" : "");
            grid.Children.Add(name);
            if (offer.Dlc1Only || offer.ZeroChanceOnly)
            {
                var hint = Text(offer.ZeroChanceOnly ? "0%" : "DLC", 9, muted);
                hint.HorizontalAlignment = HorizontalAlignment.Right;
                hint.Margin = new Thickness(0, 0, 2, 0);
                Grid.SetColumn(hint, 0);
                grid.Children.Add(hint);
            }

            var outline = new Border
            {
                BorderThickness = new Thickness(1),
                BorderBrush = checkedState ? Brushes.Transparent : accent,
                CornerRadius = new CornerRadius(3),
                Width = 27, Height = 27,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            var box = new CheckBox
            {
                IsChecked = checkedState,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = "Trader " + _roleId + " offers " + offer.ItemSid +
                          ". Uncheck to remove this item from the original stock generator."
            };
            box.Click += (_, _) => SetItem(offer, box.IsChecked == true);
            outline.Child = box;
            Grid.SetColumn(outline, 1);
            grid.Children.Add(outline);

            var row = new Border
            {
                BorderBrush = border, BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3), Background = panel,
                Margin = new Thickness(2, 1, 2, 1),
                Child = grid
            };
            _rows.Children.Add(row);
        }

        if (shown == 0)
        {
            var none = Text("NO MATCHING SOURCE ITEMS", 10.5, muted);
            none.Margin = new Thickness(8, 18, 8, 16);
            _rows.Children.Add(none);
        }
        _count.Text = available + " / " + shown;
        _count.ToolTip = "Items retained in the filtered original stock list. Original 0%-chance entries are not counted as available.";
    }
}
