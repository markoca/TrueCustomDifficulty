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
        MinHeight = 326;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 740.0 : Math.Max(560.0, availableSize.Width);
        return new Size(width, 326.0);
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
            panel, border, accent, soft, 8.0,
            accentEdge: true, technicalMarks: true);

        _targets.Clear();
        _mutantPartsTarget = null;
        DrawCenteredText(dc, "ITEM VALUE LAYERS",
            new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);

        // Six real CFG sources. The parts lane edits item Cost rather than
        // a difficulty-profile category, but shares the same input grammar.
        const double gap = 12.0;
        const double rowPitch = 90.0;
        var cardWidth = (Math.Max(1.0, ActualWidth - 32.0) - gap) / 2.0;
        for (var index = 0; index < Categories.Length + 1; index++)
        {
            var isParts = index == Categories.Length;
            var parameter = isParts ? _mutantParts : _parameters[index];
            var label = isParts ? "MUTANT PARTS" : Categories[index].Label;
            var card = new Rect(16.0 + (index % 2) * (cardWidth + gap),
                48.0 + (index / 2) * rowPitch, cardWidth, 78.0);
            var selected = isParts ? _mutantPartsSelected
                : !_mutantPartsSelected && _selectedRow == index;
            var hovered = isParts ? _mutantPartsHovered
                : !_mutantPartsHovered && _hoverRow == index;
            var hit = DrawPriceCard(dc, parameter, label, card, selected, hovered,
                text, muted, border, accent, panel);
            if (hit is null)
                continue;
            if (isParts)
                _mutantPartsTarget = hit;
            else
                _targets[index] = hit;
        }
    }

    private HitTarget? DrawPriceCard(
        DrawingContext dc, ParameterViewModel? parameter, string label,
        Rect card, bool selected, bool hovered,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        dc.DrawRoundedRectangle(panel, new Pen(border, 0.9), card, 4.0, 4.0);
        dc.DrawText(CreateText(label, 10.0, text, FontWeights.Bold),
            new Point(card.Left + 12.0, card.Top + 9.0));

        if (parameter is null)
        {
            DrawText(dc, "PARAMETER UNAVAILABLE",
                new Point(card.Left + 12.0, card.Top + 31.0),
                9.0, muted, FontWeights.Normal);
            return null;
        }

        var modified = parameter.IsValueModifiedFromReference();
        DrawRightAlignedText(dc, $"{parameter.CurrentNumber:0.##}x",
            new Point(card.Right - 12.0, card.Top + 8.0),
            modified ? 12.2 : 11.0, modified ? accent : text, FontWeights.Bold);

        var track = new Rect(card.Left + 14.0, card.Top + 39.0,
            Math.Max(1.0, card.Width - 28.0), 12.0);
        var y = track.Top + track.Height / 2.0;
        var currentX = ValueToX(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, track);
        var baseX = ValueToX(parameter.ReferenceNumber,
            parameter.Minimum, parameter.Maximum, track);
        dc.DrawLine(new Pen(border, 3.0),
            new Point(track.Left, y), new Point(track.Right, y));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.70), 3.0),
            new Point(track.Left, y), new Point(currentX, y));
        dc.DrawLine(new Pen(muted, 1.8),
            new Point(baseX, y - 8.0), new Point(baseX, y + 8.0));

        // Never accent the whole card on hover; only the editable control point.
        if (hovered)
            dc.DrawEllipse(CloneWithOpacity(accent, 0.17), null,
                new Point(currentX, y), 12.0, 12.0);
        dc.DrawEllipse(panel, new Pen(accent,
                selected || hovered ? 2.5 : 1.5),
            new Point(currentX, y), 6.0, 6.0);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

        DrawText(dc, $"{parameter.Minimum:0.##}x",
            new Point(track.Left, card.Top + 58.0),
            9.0, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, $"{parameter.Maximum:0.##}x",
            new Point(track.Right, card.Top + 58.0),
            9.0, muted, FontWeights.Normal);

        return new HitTarget(
            new Rect(track.Left - 7.0, track.Top - 6.0,
                track.Width + 14.0, track.Height + 12.0),
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
}
