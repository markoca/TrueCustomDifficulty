using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Physical world-loot population depletion board.
///
/// The historical *Radar class name is kept for XAML compatibility. Every row uses
/// the same 0-100% removal semantics and shows the audited base-game / DLC-inclusive
/// populations with approximate removed and remaining actor counts.
/// </summary>
public sealed class WorldLootRemovalRadar : FrameworkElement
{
    // Shared white card hover (CardHoverTracker): cards drawn by the
    // renderer are recorded and the hovered one is outlined on top.
    private readonly CardHoverTracker _cardHover = new();

    protected override void OnRender(DrawingContext dc)
    {
        using (_cardHover.BeginRender())
            RenderCardContent(dc);
        _cardHover.Finish(Array.Empty<Rect>(), RenderSize);
        _cardHover.DrawHover(dc);
    }

    private const double Epsilon = 0.000001;
    private const int ModLane = 5;
    // Shared vertical 0–100% physical ruler.

    private static readonly PopulationDefinition[] Populations =
    {
        new("loot.worldAmmoCratesRemoved", "AMMO BOXES", 1042, 1416, "ammo / grenade box surface"),
        new("loot.worldF1GrenadeBoxes", "F1 GRENADE BOXES", 61, 101, "dedicated F1 boxes; Ammo Boxes can further reduce survivors"),
        new("loot.worldFoodDrinkCratesRemoved", "FOOD & DRINK BOXES", 1100, 1208, "physical food / drink boxes"),
        new("loot.worldMedicalCratesRemoved", "MEDICAL BOXES", 709, 868, "physical medical boxes"),
        new("loot.looseWorldLootRemoved", "LOOSE WORLD LOOT", 5996, 7233, "eligible loose ammo / consumables / grenades"),
    };

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Populations.Length];
    private readonly Dictionary<int, HitTarget> _targets = new();
    private readonly VisualStatusMessage _status;

    private int _selectedRow;
    private int _hoverRow = -1;
    private int _dragRow = -1;
    private Rect _modCheckbox = Rect.Empty;

    // Optional mod item Chance modifier, not an extra native CFG source.
    public static readonly DependencyProperty ModItemsEnabledProperty = DependencyProperty.Register(
        nameof(ModItemsEnabled), typeof(bool), typeof(WorldLootRemovalRadar),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender
            | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public bool ModItemsEnabled
    {
        get => (bool)GetValue(ModItemsEnabledProperty);
        set => SetCurrentValue(ModItemsEnabledProperty, value);
    }

    // Lock optional mod editing during the same build phase as the former WPF strip.
    public static readonly DependencyProperty ModItemsEditorAvailableProperty = DependencyProperty.Register(
        nameof(ModItemsEditorAvailable), typeof(bool), typeof(WorldLootRemovalRadar),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));
    public bool ModItemsEditorAvailable
    {
        get => (bool)GetValue(ModItemsEditorAvailableProperty);
        set => SetCurrentValue(ModItemsEditorAvailableProperty, value);
    }

    public static readonly DependencyProperty ModItemsRemovalProperty = DependencyProperty.Register(
        nameof(ModItemsRemoval), typeof(double), typeof(WorldLootRemovalRadar),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender
            | FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
    public double ModItemsRemoval
    {
        get => (double)GetValue(ModItemsRemovalProperty);
        set => SetCurrentValue(ModItemsRemovalProperty, Math.Round(Math.Clamp(value, 0.0, 1.0), 2));
    }

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(WorldLootRemovalRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public WorldLootRemovalRadar()
    {
        _status = new VisualStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = 0;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 780.0 : Math.Max(600.0, availableSize.Width);
        return new Size(width, 330.0);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((WorldLootRemovalRadar)d).RebindParameters();

    private void RebindParameters()
    {
        foreach (var parameter in _parameters)
        {
            if (parameter is not null)
                parameter.PropertyChanged -= Parameter_PropertyChanged;
        }

        Array.Clear(_parameters, 0, _parameters.Length);

        if (Parameters is not null)
        {
            var byId = Parameters
                .OfType<ParameterViewModel>()
                .ToDictionary(x => x.Id, StringComparer.Ordinal);

            for (var i = 0; i < Populations.Length; i++)
            {
                if (!byId.TryGetValue(Populations[i].ParameterId, out var parameter))
                    continue;

                _parameters[i] = parameter;
                parameter.PropertyChanged += Parameter_PropertyChanged;
            }
        }

        InvalidateVisual();
    }

    private void Parameter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ParameterViewModel.CurrentNumber)
            or nameof(ParameterViewModel.IsModified)
            or nameof(ParameterViewModel.CurrentDisplay)
            or nameof(ParameterViewModel.ReferenceNumber))
        {
            InvalidateVisual();
        }
    }

    private void RenderCardContent(DrawingContext dc)
    {
        base.OnRender(dc);

        var text = FindBrush("TextBrush", Brushes.White);
        var muted = FindBrush("RadarMutedTextBrush", Brushes.Gray);
        var border = FindBrush("BorderBrush", Brushes.DimGray);
        var soft = FindBrush("BorderSoftBrush", border);
        var accent = FindBrush("AccentBrush", Brushes.Goldenrod);
        var panel = FindBrush("PanelAltBrush", Brushes.Black);

        IndustrialChrome.DrawFrame(
            dc,
            new Rect(2.0, 2.0, Math.Max(0.0, ActualWidth - 4.0), Math.Max(0.0, ActualHeight - 4.0)),
            panel,
            border,
            accent,
            soft,
            8.0,
            accentEdge: true,
            technicalMarks: true);

        _targets.Clear();
        _modCheckbox = Rect.Empty;

        DrawCenteredText(dc, "WORLD LOOT DEPLETION", new Point(ActualWidth / 2.0, 21.0),
            VisualControlTypography.BoardTitle, accent, FontWeights.Bold);


        // Five native controls and one independent mod-item control share a ruler.
        const double plotTop = 111.0;
        const double plotBottom = 274.0;
        var left = 62.0;
        var lane = (ActualWidth - left - 19.0) / (Populations.Length + 1);
        for (var step = 0; step <= 4; step++)
        {
            var y = plotBottom - (plotBottom - plotTop) * step / 4.0;
            dc.DrawLine(new Pen(CloneWithOpacity(border, .43), .8),
                new Point(left, y), new Point(ActualWidth - 19, y));
            DrawRightAlignedText(dc, $"{step * 25}%",
                new Point(left - 9, y - 6), VisualControlTypography.AxisTick, muted, FontWeights.SemiBold);
        }
        var axis = new Point(12, (plotTop + plotBottom) / 2);
        dc.PushTransform(new RotateTransform(-90, axis.X, axis.Y));
        DrawCenteredText(dc, "REMOVED", axis, VisualControlTypography.AxisTitle, accent, FontWeights.Bold);
        dc.Pop();

        for (var i = 0; i < Populations.Length; i++)
        {
            var parameter = _parameters[i];
            if (parameter is null) continue;
            var center = left + (i + .5) * lane;
            dc.DrawTrackedRoundedRectangle(CloneWithOpacity(panel, .33),
                new Pen(CloneWithOpacity(border, .64), .8),
                new Rect(left + i * lane + 3, 69, lane - 6, 251), 4, 4);
            var current = Math.Clamp(parameter.CurrentNumber, 0, 1);
            var y = plotBottom - (plotBottom - plotTop) * current;
            var referenceY = plotBottom - (plotBottom - plotTop) *
                Math.Clamp(parameter.ReferenceNumber, 0, 1);
            var barWidth = Math.Min(39, lane * .37);
            var barLeft = center - barWidth / 2;
            dc.DrawRectangle(CloneWithOpacity(border, .12), null,
                new Rect(barLeft, plotTop, barWidth, plotBottom - plotTop));
            dc.DrawRectangle(CloneWithOpacity(accent, parameter.IsControlEnabled ? .72 : .22),
                null, new Rect(barLeft, y, barWidth, Math.Max(.5, plotBottom - y)));
            dc.DrawLine(new Pen(CloneWithOpacity(muted, .68), 1.35),
                new Point(barLeft - 5, referenceY),
                new Point(barLeft + barWidth + 5, referenceY));
            VerticalBarHandleChrome.Draw(dc, new Point(center, y), barWidth,
                parameter.IsControlEnabled ? accent : muted,
                _hoverRow == i && parameter.IsControlEnabled);
            DrawCenteredText(dc, $"{current * 100:0}%",
                new Point(center, 88), 11.3,
                parameter.IsValueModifiedFromReference() ? accent : text,
                FontWeights.Bold);
            var words = Populations[i].Label.Split(' ');
            if (words.Length <= 2)
                DrawCenteredText(dc, Populations[i].Label,
                    new Point(center, 299), 9, text, FontWeights.Bold);
            else
            {
                DrawCenteredText(dc, string.Join(" ", words.Take(2)),
                    new Point(center, 291), 9, text, FontWeights.Bold);
                DrawCenteredText(dc, string.Join(" ", words.Skip(2)),
                    new Point(center, 305), 9, text, FontWeights.Bold);
            }
            _targets[i] = new HitTarget(
                new Rect(center - Math.Max(16, barWidth / 2 + 6), plotTop,
                    Math.Max(32, barWidth + 12), plotBottom - plotTop),
                new Rect(center - barWidth / 2, plotTop, barWidth, plotBottom - plotTop));
        }

        // Sixth full-size vertical lane, with the same chrome as native controls.
        var modCenter = left + (ModLane + .5) * lane;
        dc.DrawTrackedRoundedRectangle(CloneWithOpacity(panel, .33),
            new Pen(CloneWithOpacity(border, .64), .8),
            new Rect(left + ModLane * lane + 3, 69, lane - 6, 251), 4, 4);
        var enabled = ModItemsEnabled && ModItemsEditorAvailable && IsEnabled;
        var fraction = Math.Clamp(ModItemsRemoval, 0.0, 1.0);
        var modY = plotBottom - (plotBottom - plotTop) * fraction;
        var modWidth = Math.Min(39, lane * .37);
        var modLeft = modCenter - modWidth / 2;
        dc.DrawRectangle(CloneWithOpacity(border, .12), null,
            new Rect(modLeft, plotTop, modWidth, plotBottom - plotTop));
        dc.DrawRectangle(CloneWithOpacity(enabled ? accent : muted, enabled ? .72 : .15),
            null, new Rect(modLeft, modY, modWidth, Math.Max(.5, plotBottom - modY)));
        VerticalBarHandleChrome.Draw(dc, new Point(modCenter, modY), modWidth,
            enabled ? accent : muted, _hoverRow == ModLane && enabled);
        DrawCenteredText(dc, $"{fraction * 100.0:0}%",
            new Point(modCenter, 88), 11.3,
            enabled && fraction > Epsilon ? accent : enabled ? text : muted, FontWeights.Bold);
        _modCheckbox = new Rect(modCenter - 52.0, 290.0, 18.0, 18.0);
        BooleanCheckboxChrome.Draw(dc, _modCheckbox, ModItemsEnabled, IsEnabled && ModItemsEditorAvailable,
            _hoverRow == ModLane, panel, border, accent);
        DrawCenteredText(dc, "MOD ITEMS", new Point(modCenter + 14.0, 299.0),
            9.0, enabled ? text : muted, FontWeights.Bold);
        _targets[ModLane] = new HitTarget(
            new Rect(modCenter - Math.Max(16, modWidth / 2 + 6), plotTop,
                Math.Max(32, modWidth + 12), plotBottom - plotTop),
            new Rect(modLeft, plotTop, modWidth, plotBottom - plotTop));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var point = e.GetPosition(this);
        if (IsEnabled && ModItemsEditorAvailable && _modCheckbox.Contains(point))
        {
            _selectedRow = ModLane;
            ModItemsEnabled = !ModItemsEnabled;
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        var row = FindTarget(point);
        if (row < 0 || !IsEnabled || !ModItemsEditorAvailable || (row == ModLane && !ModItemsEnabled))
            return;

        _selectedRow = row;
        _dragRow = row;
        CaptureMouse();
        UpdateFromPoint(row, e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_cardHover.UpdateHover(e.GetPosition(this))) InvalidateVisual();
        base.OnMouseMove(e);

        var point = e.GetPosition(this);
        var hover = _modCheckbox.Contains(point) ? ModLane : FindTarget(point);

        if (hover != _hoverRow)
        {
            _hoverRow = hover;
            InvalidateVisual();
        }

        Cursor = _modCheckbox.Contains(point) && IsEnabled && ModItemsEditorAvailable ? Cursors.Hand
            : hover >= 0 && (hover != ModLane || ModItemsEnabled) && IsEnabled && ModItemsEditorAvailable
                ? Cursors.SizeNS : Cursors.Arrow;

        if (_dragRow < 0 || e.LeftButton != MouseButtonState.Pressed)
            return;

        UpdateFromPoint(_dragRow, point);
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        if (_cardHover.ClearHover()) InvalidateVisual();
        base.OnMouseLeave(e);

        if (_hoverRow < 0)
            return;

        _hoverRow = -1;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_dragRow < 0)
            return;

        _dragRow = -1;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);

        var row = FindTarget(e.GetPosition(this));
        if (row < 0 || !IsEnabled || !ModItemsEditorAvailable)
            return;
        if (row == ModLane)
        {
            if (!ModItemsEnabled) return;
            _selectedRow = ModLane;
            ModItemsRemoval = 0.0;
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        if (_parameters[row] is not { } parameter)
            return;

        _selectedRow = row;
        parameter.ResetToReference();
        _status.ShowTransient($"{Populations[row].Label} removal reset to Base Preset.");
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (_selectedRow == ModLane)
        {
            if (e.Key == Key.Space && IsEnabled && ModItemsEditorAvailable)
            {
                ModItemsEnabled = !ModItemsEnabled;
                e.Handled = true;
                return;
            }
            if (e.Key == Key.R && IsEnabled && ModItemsEditorAvailable)
            {
                ModItemsRemoval = 0.0;
                e.Handled = true;
                return;
            }
            var step = e.Key switch
            {
                Key.Up or Key.Right or Key.Add or Key.OemPlus => 1,
                Key.Down or Key.Left or Key.Subtract or Key.OemMinus => -1,
                _ => 0
            };
            if (step != 0 && ModItemsEnabled && IsEnabled && ModItemsEditorAvailable)
            {
                ModItemsRemoval = Math.Clamp(ModItemsRemoval + step * 0.01, 0.0, 1.0);
                e.Handled = true;
            }
            return;
        }

        if (e.Key == Key.R)
        {
            foreach (var parameter in _parameters)
                parameter?.ResetToReference();

            _status.ShowTransient("World loot removal reset to Base Preset.");
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var direction = e.Key switch
        {
            Key.Up or Key.Right or Key.Add or Key.OemPlus => 1,
            Key.Down or Key.Left or Key.Subtract or Key.OemMinus => -1,
            _ => 0
        };

        if (direction == 0 || _parameters[_selectedRow] is not { } selected)
            return;

        selected.CurrentNumber = Math.Clamp(
            selected.CurrentNumber + direction * selected.TickFrequency,
            selected.Minimum,
            selected.Maximum);

        _status.Clear();
        InvalidateVisual();
        e.Handled = true;
    }

    private void UpdateFromPoint(int row, Point point)
    {
        if (!_targets.TryGetValue(row, out var target))
            return;
        if (row == ModLane)
        {
            if (!ModItemsEnabled || !IsEnabled || !ModItemsEditorAvailable) return;
            var modRatio = Math.Clamp(
                (target.MapRect.Bottom - point.Y) / Math.Max(1.0, target.MapRect.Height),
                0.0, 1.0);
            ModItemsRemoval = Math.Round(modRatio, 2, MidpointRounding.AwayFromZero);
            InvalidateVisual();
            return;
        }
        if (_parameters[row] is not { } parameter)
            return;

        var ratio = Math.Clamp(
            (target.MapRect.Bottom - point.Y) / Math.Max(1.0, target.MapRect.Height),
            0.0,
            1.0);

        var raw = parameter.Minimum + ratio * (parameter.Maximum - parameter.Minimum);
        var tick = Math.Max(parameter.TickFrequency, Epsilon);
        var snapped = parameter.Minimum
            + Math.Round((raw - parameter.Minimum) / tick, MidpointRounding.AwayFromZero) * tick;

        parameter.CurrentNumber = Math.Clamp(snapped, parameter.Minimum, parameter.Maximum);
        _status.Clear();
        InvalidateVisual();
    }

    private int FindTarget(Point point)
    {
        foreach (var pair in _targets)
        {
            if (pair.Value.HitRect.Contains(point))
                return pair.Key;
        }

        return -1;
    }

    private static Brush FindBrush(string key, Brush fallback)
        => Application.Current?.TryFindResource(key) as Brush ?? fallback;

    private static Brush CloneWithOpacity(Brush brush, double opacity)
    {
        var clone = brush.Clone();
        clone.Opacity = Math.Clamp(opacity, 0.0, 1.0);
        return clone;
    }

    private void DrawText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
        => dc.DrawText(CreateText(value, size, brush, weight), point);

    private void DrawCenteredText(DrawingContext dc, string value, Point center, double size, Brush brush, FontWeight weight)
    {
        var formatted = CreateText(value, size, brush, weight);
        dc.DrawText(formatted, new Point(center.X - formatted.Width / 2.0, center.Y - formatted.Height / 2.0));
    }

    private void DrawRightAlignedText(DrawingContext dc, string value, Point right, double size, Brush brush, FontWeight weight)
    {
        var formatted = CreateText(value, size, brush, weight);
        dc.DrawText(formatted, new Point(right.X - formatted.Width, right.Y));
    }

    private FormattedText CreateText(string value, double size, Brush brush, FontWeight weight)
    {
        var formatted = new FormattedText(
            value,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            Math.Max(9.0, size),
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        var separatorBrush = FindBrush("AccentBrush", Brushes.Goldenrod);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '|')
                formatted.SetForegroundBrush(separatorBrush, i, 1);
        }

        return formatted;
    }

    private sealed record PopulationDefinition(
        string ParameterId,
        string Label,
        int BasePopulation,
        int DlcInclusivePopulation,
        string Note);

    private sealed record HitTarget(Rect HitRect, Rect MapRect);
}
