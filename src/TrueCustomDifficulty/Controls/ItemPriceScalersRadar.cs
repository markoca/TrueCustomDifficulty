using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Item-value category board.
///
/// The historical *Radar class name is kept for XAML compatibility. Five rows edit
/// EconomyDifficulty category-cost multipliers; mutant parts are shown separately
/// because that control directly scales authored ItemPrototypes Cost values.
/// </summary>
public sealed class ItemPriceScalersRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double RowHeight = 38.0;

    private static readonly PriceDefinition[] Categories =
    {
        new("economy.consumableCost", "CONSUMABLES"),
        new("economy.ammoCost", "AMMO"),
        new("economy.armorCost", "ARMOR"),
        new("economy.weaponCost", "WEAPONS"),
        new("economy.artifactCost", "ARTIFACTS"),
    };

    private const string MutantPartParameterId = "economy.mutantPartValue";

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Categories.Length];
    private ParameterViewModel? _mutantParts;
    private readonly Dictionary<int, HitTarget> _targets = new();
    private HitTarget? _mutantPartsTarget;
    private readonly VisualStatusMessage _status;

    private int _selectedRow;
    private int _hoverRow = -1;
    private int _dragRow = -1;
    private bool _mutantPartsSelected;
    private bool _mutantPartsHovered;
    private bool _dragMutantParts;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(ItemPriceScalersRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public ItemPriceScalersRadar()
    {
        _status = new VisualStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = 390;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 740.0 : Math.Max(560.0, availableSize.Width);
        return new Size(width, 390.0);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((ItemPriceScalersRadar)d).RebindParameters();

    private void RebindParameters()
    {
        foreach (var parameter in _parameters)
        {
            if (parameter is not null)
                parameter.PropertyChanged -= Parameter_PropertyChanged;
        }

        if (_mutantParts is not null)
            _mutantParts.PropertyChanged -= Parameter_PropertyChanged;

        Array.Clear(_parameters, 0, _parameters.Length);
        _mutantParts = null;

        if (Parameters is not null)
        {
            var byId = Parameters
                .OfType<ParameterViewModel>()
                .ToDictionary(x => x.Id, StringComparer.Ordinal);

            for (var i = 0; i < Categories.Length; i++)
            {
                if (!byId.TryGetValue(Categories[i].ParameterId, out var parameter))
                    continue;

                _parameters[i] = parameter;
                parameter.PropertyChanged += Parameter_PropertyChanged;
            }

            if (byId.TryGetValue(MutantPartParameterId, out var mutantParts))
            {
                _mutantParts = mutantParts;
                mutantParts.PropertyChanged += Parameter_PropertyChanged;
            }
        }

        InvalidateVisual();
    }

    private void Parameter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ParameterViewModel.CurrentNumber)
            or nameof(ParameterViewModel.CurrentDisplay)
            or nameof(ParameterViewModel.IsModified)
            or nameof(ParameterViewModel.ReferenceNumber))
        {
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext dc)
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
        _mutantPartsTarget = null;

        DrawCenteredText(dc, "ITEM VALUE LAYERS", new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);
        DrawCenteredText(dc, "base-value layers before trader buy / sell pricing", new Point(ActualWidth / 2.0, 39.0),
            9.5, muted, FontWeights.Normal);

        var layout = GetLayout();

        DrawText(dc, "CATEGORY COSTS", new Point(18.0, 59.0), 9.0, muted, FontWeights.Bold);
        DrawRightAlignedText(
            dc,
            "0x = zero category-cost layer   •   1x = vanilla   •   2x = double",
            new Point(Math.Max(470.0, ActualWidth - 18.0), 59.0),
            7.4,
            muted,
            FontWeights.Normal);

        DrawText(dc, "CATEGORY", new Point(layout.LabelLeft, 77.0), 9.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "VALUE", new Point(layout.ValueCenter, 84.0), 7.8, muted, FontWeights.Bold);
        DrawCenteredText(dc, "CHEAPER  ←  CATEGORY VALUE  →  COSTLIER", new Point(layout.TrackLeft + layout.TrackWidth / 2.0, 84.0), 7.7, muted, FontWeights.Bold);

        dc.DrawLine(new Pen(border, 1.0), new Point(14.0, 95.0), new Point(Math.Max(14.0, ActualWidth - 14.0), 95.0));

        for (var row = 0; row < Categories.Length; row++)
            DrawCategoryRow(dc, row, 102.0 + row * RowHeight, layout, text, muted, border, accent, panel);

        DrawMutantPartsPanel(dc, 296.0, text, muted, border, accent, panel);

        // No transient yellow reset notifications.
    }

    private void DrawCategoryRow(
        DrawingContext dc,
        int row,
        double top,
        Layout layout,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var parameter = _parameters[row];
        if (parameter is null)
            return;

        var info = Categories[row];
        var selected = !_mutantPartsSelected && row == _selectedRow;
        var hovered = !_mutantPartsHovered && row == _hoverRow;
        var modified = Math.Abs(parameter.CurrentNumber - parameter.ReferenceNumber) > Epsilon;

        var rowRect = new Rect(14.0, top, Math.Max(1.0, ActualWidth - 28.0), RowHeight - 4.0);
        dc.DrawRoundedRectangle(
            selected
                ? CloneWithOpacity(accent, 0.08)
                : hovered
                    ? CloneWithOpacity(border, 0.10)
                    : CloneWithOpacity(panel, 0.01),
            new Pen(CloneWithOpacity(border, 0.45), 0.8),
            rowRect,
            3.0,
            3.0);

        DrawText(
            dc,
            info.Label,
            new Point(layout.LabelLeft, top + 7.0),
            8.7,
            modified ? accent : text,
            FontWeights.Bold);

        DrawCenteredText(
            dc,
            $"{parameter.CurrentNumber:0.##}x",
            new Point(layout.ValueCenter, top + 13.0),
            9.5,
            modified ? accent : text,
            FontWeights.Bold);

        var track = new Rect(layout.TrackLeft, top + 23.0, layout.TrackWidth, 9.0);
        var y = track.Top + track.Height / 2.0;
        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track);
        var baseX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, track);

        dc.DrawLine(new Pen(muted, 1.1), new Point(baseX, y - 8.0), new Point(baseX, y + 8.0));
        dc.DrawLine(new Pen(accent, 3.1), new Point(track.Left, y), new Point(currentX, y));

        dc.DrawEllipse(
            panel,
            new Pen(accent, selected ? 2.5 : 1.5),
            new Point(currentX, y),
            6.0,
            6.0);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.7, 2.7);


        _targets[row] = new HitTarget(
            new Rect(track.Left - 7.0, track.Top - 6.0, track.Width + 14.0, track.Height + 12.0),
            track);
    }

    private void DrawMutantPartsPanel(
        DrawingContext dc,
        double top,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var parameter = _mutantParts;
        if (parameter is null)
            return;

        var card = new Rect(18.0, top, Math.Max(1.0, ActualWidth - 36.0), 70.0);
        dc.DrawRoundedRectangle(
            _mutantPartsSelected
                ? CloneWithOpacity(accent, 0.08)
                : _mutantPartsHovered
                    ? CloneWithOpacity(border, 0.10)
                    : CloneWithOpacity(panel, 0.04),
            new Pen(border, 0.9),
            card,
            3.0,
            3.0);

        DrawText(dc, "MUTANT PARTS", new Point(card.Left + 10.0, card.Top + 8.0), 8.8, text, FontWeights.Bold);
        DrawText(
            dc,
            "direct base-value scale",
            new Point(card.Left + 10.0, card.Top + 24.0),
            9.0,
            muted,
            FontWeights.Normal);

        DrawRightAlignedText(
            dc,
            $"{parameter.CurrentNumber:0.##}x",
            new Point(card.Right - 12.0, card.Top + 8.0),
            10.0,
            accent,
            FontWeights.Bold);

        var blindDogValue = 1000.0 * parameter.CurrentNumber;
        var chimeraValue = 18000.0 * parameter.CurrentNumber;

        DrawText(
            dc,
            $"BLIND DOG  {blindDogValue:0} coupons",
            new Point(card.Left + 165.0, card.Top + 24.0),
            7.6,
            muted,
            FontWeights.SemiBold);

        DrawText(
            dc,
            $"CHIMERA  {chimeraValue:0} coupons",
            new Point(card.Left + 340.0, card.Top + 24.0),
            7.6,
            accent,
            FontWeights.SemiBold);

        var track = new Rect(card.Left + 14.0, card.Top + 48.0, card.Width - 28.0, 12.0);
        var y = track.Top + track.Height / 2.0;

        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track);
        var baseX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, track);

        dc.DrawLine(new Pen(muted, 1.1), new Point(baseX, y - 8.0), new Point(baseX, y + 8.0));
        dc.DrawLine(new Pen(accent, 3.2), new Point(track.Left, y), new Point(currentX, y));

        dc.DrawEllipse(
            panel,
            new Pen(accent, _mutantPartsSelected ? 2.5 : 1.5),
            new Point(currentX, y),
            6.0,
            6.0);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.7, 2.7);


        _mutantPartsTarget = new HitTarget(
            new Rect(track.Left - 7.0, track.Top - 6.0, track.Width + 14.0, track.Height + 12.0),
            track);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var point = e.GetPosition(this);

        if (_mutantPartsTarget is not null && _mutantPartsTarget.HitRect.Contains(point))
        {
            _mutantPartsSelected = true;
            _dragMutantParts = true;
            _dragRow = -1;
            CaptureMouse();
            UpdateMutantPartsFromPoint(point);
            e.Handled = true;
            return;
        }

        var row = FindTarget(point);
        if (row < 0)
            return;

        _mutantPartsSelected = false;
        _selectedRow = row;
        _dragRow = row;
        _dragMutantParts = false;
        CaptureMouse();
        UpdateFromPoint(row, point);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var point = e.GetPosition(this);
        var mutantHover = _mutantPartsTarget is not null && _mutantPartsTarget.HitRect.Contains(point);
        var hover = mutantHover ? -1 : FindTarget(point);

        if (mutantHover != _mutantPartsHovered || hover != _hoverRow)
        {
            _mutantPartsHovered = mutantHover;
            _hoverRow = hover;
            InvalidateVisual();
        }

        Cursor = mutantHover || hover >= 0 ? Cursors.Hand : Cursors.Arrow;

        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        if (_dragMutantParts)
        {
            UpdateMutantPartsFromPoint(point);
            e.Handled = true;
            return;
        }

        if (_dragRow >= 0)
        {
            UpdateFromPoint(_dragRow, point);
            e.Handled = true;
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        if (!_mutantPartsHovered && _hoverRow < 0)
            return;

        _mutantPartsHovered = false;
        _hoverRow = -1;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_dragRow < 0 && !_dragMutantParts)
            return;

        _dragRow = -1;
        _dragMutantParts = false;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);

        var point = e.GetPosition(this);

        if (_mutantPartsTarget is not null
            && _mutantPartsTarget.HitRect.Contains(point)
            && _mutantParts is not null)
        {
            _mutantPartsSelected = true;
            _mutantParts.ResetToReference();
            _status.ShowTransient("Mutant Parts value reset to Base Preset.");
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var row = FindTarget(point);
        if (row < 0 || _parameters[row] is not { } parameter)
            return;

        _mutantPartsSelected = false;
        _selectedRow = row;
        parameter.ResetToReference();
        _status.ShowTransient($"{Categories[row].Label} value reset to Base Preset.");
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.R)
        {
            foreach (var parameter in _parameters)
                parameter?.ResetToReference();

            _mutantParts?.ResetToReference();
            _status.ShowTransient("Item value layers reset to Base Preset.");
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

        if (direction == 0)
            return;

        var selected = _mutantPartsSelected ? _mutantParts : _parameters[_selectedRow];
        if (selected is null)
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
        if (!_targets.TryGetValue(row, out var target) || _parameters[row] is not { } parameter)
            return;

        UpdateParameterFromPoint(parameter, target.MapRect, point);
    }

    private void UpdateMutantPartsFromPoint(Point point)
    {
        if (_mutantPartsTarget is null || _mutantParts is null)
            return;

        UpdateParameterFromPoint(_mutantParts, _mutantPartsTarget.MapRect, point);
    }

    private void UpdateParameterFromPoint(ParameterViewModel parameter, Rect map, Point point)
    {
        var ratio = Math.Clamp((point.X - map.Left) / Math.Max(1.0, map.Width), 0.0, 1.0);
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

    private Layout GetLayout()
    {
        var usable = Math.Max(520.0, ActualWidth - 40.0);
        var labelWidth = Math.Clamp(usable * 0.20, 125.0, 155.0);
        var valueWidth = 78.0;
        var fixedWidth = labelWidth + valueWidth + 30.0;
        var trackWidth = Math.Max(230.0, usable - fixedWidth);

        var left = 22.0;
        var valueCenter = left + labelWidth + valueWidth / 2.0;
        var trackLeft = left + labelWidth + valueWidth + 18.0;

        return new Layout(left, valueCenter, trackLeft, trackWidth);
    }

    private static double ValueToX(double value, double minimum, double maximum, Rect track)
    {
        if (maximum <= minimum + Epsilon)
            return track.Left;

        var ratio = Math.Clamp((value - minimum) / (maximum - minimum), 0.0, 1.0);
        return track.Left + track.Width * ratio;
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

    private sealed record PriceDefinition(string ParameterId, string Label);
    private sealed record HitTarget(Rect HitRect, Rect MapRect);
    private sealed record Layout(double LabelLeft, double ValueCenter, double TrackLeft, double TrackWidth);
}
