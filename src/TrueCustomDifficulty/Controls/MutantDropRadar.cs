using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Fourteen-axis editor that aggregates all mutant-part drop chances into one readable radar.
/// Backing values remain the original ParameterViewModels so presets and writer routes stay unchanged.
/// </summary>
public sealed class MutantDropRadar : FrameworkElement
{
    private const double RadarInfoTextSize = 11.0;
    private const double RadarLegendTextSize = 11.0;
    private const double RadarAxisTextSize = 10.5;

    private const double MaximumValue = 1.0;
    private const double Step = 0.01;

    private static readonly AxisDefinition[] Axes =
    {
        new("loot.mutant.blindDogChance", "Blind Dog", -90),
        new("loot.mutant.bloodsuckerChance", "Bloodsucker", -64.286),
        new("loot.mutant.boarChance", "Boar", -38.571),
        new("loot.mutant.burerChance", "Burer", -12.857),
        new("loot.mutant.catChance", "Bayun", 12.857),
        new("loot.mutant.chimeraChance", "Chimera", 38.571),
        new("loot.mutant.controllerChance", "Controller", 64.286),
        new("loot.mutant.deerChance", "Deer", 90),
        new("loot.mutant.fleshChance", "Flesh", 115.714),
        new("loot.mutant.poltergeistChance", "Poltergeist", 141.429),
        new("loot.mutant.pseudodogChance", "Pseudodog", 167.143),
        new("loot.mutant.pseudogiantChance", "Pseudogiant", 192.857),
        new("loot.mutant.snorkChance", "Snork", 218.571),
        new("loot.mutant.tushkanChance", "Tushkan", 244.286),
    };

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Axes.Length];
    private int _selectedAxis;
    private int _dragAxis = -1;
    private int _hoverAxis = -1;
        private readonly Dictionary<int, Rect> _legendHitRects = new();
    private int _legendRecordingAxis = -1;

private string _statusText = string.Empty;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(MutantDropRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public MutantDropRadar()
    {
        Focusable = true;
        MinHeight = 530;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 720 : Math.Max(360, availableSize.Width);
        return new Size(width, 530);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MutantDropRadar)d).RebindParameters();

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
            var byId = Parameters.OfType<ParameterViewModel>().ToDictionary(x => x.Id, StringComparer.Ordinal);
            for (var i = 0; i < Axes.Length; i++)
            {
                if (!byId.TryGetValue(Axes[i].ParameterId, out var parameter))
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
            or nameof(ParameterViewModel.CurrentDisplay))
        {
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var textBrush = FindBrush("TextBrush", Brushes.White);
        var mutedBrush = FindBrush("RadarMutedTextBrush", Brushes.Gray);
        var borderBrush = FindBrush("BorderBrush", Brushes.DimGray);
        var accentBrush = FindBrush("AccentBrush", Brushes.Goldenrod);
        var panelBrush = FindBrush("PanelAltBrush", Brushes.Black);
        var technicalBrush = FindBrush("BorderSoftBrush", borderBrush);
        IndustrialChrome.DrawFrame(
            dc,
            new Rect(2, 2, Math.Max(0, ActualWidth - 4), Math.Max(0, ActualHeight - 4)),
            panelBrush,
            borderBrush,
            accentBrush,
            technicalBrush,
            8.0,
            accentEdge: true,
            technicalMarks: true);

        var (center, radius) = GetGeometry();
        if (radius <= 10)
            return;

        var neutralPen = new Pen(borderBrush, 1);
        var outerPen = new Pen(borderBrush, 1.2);

        DrawRing(dc, center, radius * 0.25, neutralPen);
        DrawRing(dc, center, radius * 0.50, neutralPen);
        DrawRing(dc, center, radius * 0.75, neutralPen);
        DrawRing(dc, center, radius, outerPen);

        for (var i = 0; i < Axes.Length; i++)
        {
            var unit = UnitVector(Axes[i].AngleDegrees);
            dc.DrawLine(neutralPen, center, center + unit * radius);
        }

        var values = GetValues();
        var points = new Point[Axes.Length];
        for (var i = 0; i < Axes.Length; i++)
        {
            var unit = UnitVector(Axes[i].AngleDegrees);
            points[i] = center + unit * (radius * values[i] / MaximumValue);
        }

        var polygon = BuildClosedGeometry(points);
        var fill = accentBrush.Clone();
        fill.Opacity = 0.16;
        dc.DrawGeometry(fill, new Pen(accentBrush, 2), polygon);

        for (var i = 0; i < points.Length; i++)
        {
            var hovered = i == _hoverAxis;
            if (hovered)
            {
                var outerGlow = accentBrush.Clone();
                outerGlow.Opacity = 0.12;
                dc.DrawEllipse(outerGlow, null, points[i], 14.0, 14.0);

                var innerGlow = accentBrush.Clone();
                innerGlow.Opacity = 0.28;
                dc.DrawEllipse(innerGlow, null, points[i], 10.0, 10.0);
            }

            var outer = i == _selectedAxis ? 8.5 : 6.0;
            var outlineWidth = i == _selectedAxis ? 2.4 : hovered ? 2.0 : 1.5;
            dc.DrawEllipse(panelBrush, new Pen(accentBrush, outlineWidth), points[i], outer, outer);
            dc.DrawEllipse(accentBrush, null, points[i], 3.2, 3.2);
        }

        DrawRingCaption(dc, "25%", center, radius * 0.25, mutedBrush);
        DrawRingCaption(dc, "50%", center, radius * 0.50, mutedBrush);
        DrawRingCaption(dc, "75%", center, radius * 0.75, mutedBrush);
        DrawRingCaption(dc, "100%", center, radius, mutedBrush);

        for (var i = 0; i < Axes.Length; i++)
        {
            var unit = UnitVector(Axes[i].AngleDegrees);
            var labelPoint = center + unit * (radius + 24);
            DrawAxisLabel(dc, Axes[i].Label, labelPoint, _selectedAxis == i ? accentBrush : textBrush);
        }

        DrawInfoHeader(dc,
            "Mutant part drop chance | higher = parts drop more often | a successful drop still gives 1 part.",
            mutedBrush);

        DrawLegend(dc, values, textBrush, mutedBrush, accentBrush);

        var instruction = string.IsNullOrWhiteSpace(_statusText)
            ? "Drag a point | Arrow keys adjust selected mutant | Right-click point/legend resets that mutant | R resets all"
            : _statusText;
        DrawCenteredText(dc, instruction, new Point(GetInfoRect().Left + GetInfoRect().Width / 2.0, ActualHeight - 18), RadarInfoTextSize,
            string.IsNullOrWhiteSpace(_statusText) ? mutedBrush : accentBrush, FontWeights.Normal);
    }

    private Rect GetInfoRect()
    {
        var left = Math.Max(ActualWidth * 0.48, 370.0);
        return new Rect(left, 76.0, Math.Max(250.0, ActualWidth - left - 16.0), Math.Max(300.0, ActualHeight - 120.0));
    }

    private void DrawInfoHeader(DrawingContext dc, string text, Brush brush)
    {
        var info = GetInfoRect();
        DrawClippedText(dc, text, new Rect(info.Left, 28.0, info.Width, 34.0), RadarInfoTextSize, brush, FontWeights.Normal);
    }

    private void DrawLegend(DrawingContext dc, double[] values, Brush textBrush, Brush mutedBrush, Brush accentBrush)
    {
        var info = GetInfoRect();
        DrawLegendColumn(dc, 0, Axes.Length, info, values, textBrush, mutedBrush, accentBrush);
    }

    private void DrawLegendColumn(DrawingContext dc, int start, int end, Rect bounds, double[] values, Brush textBrush, Brush mutedBrush, Brush accentBrush)
    {
        const double rowHeight = 28.0;
        var totalHeight = (end - start) * rowHeight;
        var y = bounds.Top + Math.Max(0.0, (bounds.Height - totalHeight) / 2.0);
        for (var i = start; i < end; i++)
        {
            var parameter = _parameters[i];
            if (parameter is null)
                continue;

            var isSelected = i == _selectedAxis;
            var hovered = i == _hoverAxis;
            var isModified = parameter.IsModified;
            BeginLegendHitArea(i);

DrawClippedText(dc, Axes[i].Label, new Rect(bounds.Left + 6, y + 6, bounds.Width * 0.44, 18), RadarLegendTextSize, isModified ? accentBrush : hovered ? textBrush : textBrush, FontWeights.SemiBold);

            var currentPercent = values[i] * 100.0;
            var vanillaPercent = TryGetVanillaValue(parameter, out var vanilla)
                ? vanilla * 100.0
                : currentPercent;
            var detail = $"{currentPercent:0.#}% drop chance | vanilla {vanillaPercent:0.#}%";
            DrawClippedText(dc, detail, new Rect(bounds.Left + (bounds.Width * 0.44), y + 6, bounds.Width * 0.56 - 6, 18), RadarLegendTextSize, isModified || isSelected ? accentBrush : hovered ? textBrush : mutedBrush, isModified || isSelected ? FontWeights.SemiBold : FontWeights.Normal);

            EndLegendHitArea();

            y += rowHeight;
        }
    }

    private void DrawAxisLabel(DrawingContext dc, string label, Point position, Brush brush)
    {
        var anchor = position.X < GetGeometry().Center.X - 18.0 ? TextAnchor.Right
            : position.X > GetGeometry().Center.X + 18.0 ? TextAnchor.Left
            : TextAnchor.Center;
        DrawAnchoredText(dc, label, position, RadarAxisTextSize, brush, FontWeights.SemiBold, anchor);
    }

    private void DrawRingCaption(DrawingContext dc, string text, Point center, double radius, Brush brush)
        => DrawText(dc, text, new Point(center.X + 6, center.Y - radius - 8), 8.9, brush, FontWeights.SemiBold);

    private void DrawRing(DrawingContext dc, Point center, double radius, Pen pen)
    {
        var points = Axes.Select(axis => center + UnitVector(axis.AngleDegrees) * radius).ToArray();
        dc.DrawGeometry(null, pen, BuildClosedGeometry(points));
    }

    private static StreamGeometry BuildClosedGeometry(IReadOnlyList<Point> points)
    {
        var geometry = new StreamGeometry();
        using var context = geometry.Open();
        context.BeginFigure(points[0], isFilled: true, isClosed: true);
        context.PolyLineTo(points.Skip(1).ToArray(), isStroked: true, isSmoothJoin: true);
        geometry.Freeze();
        return geometry;
    }

    private static bool TryGetVanillaValue(ParameterViewModel parameter, out double value)
    {
        value = 0.0;
        if (!parameter.Definition.Vanilla.TryGetValue("All", out var element)
            || element.ValueKind != System.Text.Json.JsonValueKind.Number)
        {
            return false;
        }

        value = element.GetDouble();
        return true;
    }

    private double[] GetValues()
    {
        var values = new double[Axes.Length];
        for (var i = 0; i < Axes.Length; i++)
            values[i] = Math.Clamp(_parameters[i]?.CurrentNumber ?? 0.0, 0.0, MaximumValue);
        return values;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var point = e.GetPosition(this);

        // The legend is informational for left-clicks. Consume the click here so
        // it cannot fall through to FindAxis(), which maps arbitrary positions
        // to the nearest radar axis and can otherwise push that value to max.
        if (FindLegendAxis(point) >= 0)
        {
            e.Handled = true;
            return;
        }

        var axis = FindAxis(point);
        if (axis < 0)
            return;

        _selectedAxis = axis;
        _dragAxis = axis;
        CaptureMouse();
        UpdateAxisFromPoint(axis, point);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var point = e.GetPosition(this);
        var hoverAxis = FindLegendAxis(point);
        if (hoverAxis < 0)
            hoverAxis = FindHandleAxis(point, 22.0);

        if (hoverAxis != _hoverAxis)
        {
            _hoverAxis = hoverAxis;
            InvalidateVisual();
        }
        Cursor = hoverAxis >= 0 ? Cursors.Hand : Cursors.Arrow;
        if (_dragAxis < 0 || e.LeftButton != MouseButtonState.Pressed)
            return;

        UpdateAxisFromPoint(_dragAxis, point);
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        if (_hoverAxis >= 0)
        {
            _hoverAxis = -1;
            InvalidateVisual();
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        var axis = FindLegendAxis(e.GetPosition(this));
        if (axis < 0 || e.Delta == 0)
            return;

        var parameter = _parameters[axis];
        if (parameter is null)
            return;

        var direction = e.Delta > 0 ? 1 : -1;
        _selectedAxis = axis;
        _hoverAxis = axis;
        parameter.CurrentNumber = Math.Clamp(parameter.CurrentNumber + (direction * Step), 0.0, MaximumValue);
        _statusText = string.Empty;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragAxis < 0)
            return;

        _dragAxis = -1;
        ReleaseMouseCapture();
        e.Handled = true;
    }


    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);
                var point = e.GetPosition(this);
        var axis = FindLegendAxis(point);
        if (axis < 0)
            axis = FindHandleAxis(point, 22.0);

        if (axis < 0)
            return;

        _selectedAxis = axis;
        var parameter = _parameters[axis];
        var vanilla = parameter is not null && TryGetVanillaValue(parameter, out var value) ? value : 0.0;
        if (parameter is not null)
            parameter.CurrentNumber = vanilla;

        _statusText = $"{Axes[axis].Label} reset to vanilla {vanilla * 100:0.#}%";
        InvalidateVisual();
        e.Handled = true;
    }
    private int FindLegendAxis(Point point)
    {
        foreach (var hit in _legendHitRects)
        {
            if (hit.Value.Contains(point))
                return hit.Key;
        }

        return -1;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.R)
        {
            foreach (var parameter in _parameters)
            {
                if (parameter is not null && TryGetVanillaValue(parameter, out var vanilla))
                    parameter.CurrentNumber = vanilla;
            }
            _statusText = string.Empty;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var delta = e.Key switch
        {
            Key.Up or Key.Right or Key.Add or Key.OemPlus => Step,
            Key.Down or Key.Left or Key.Subtract or Key.OemMinus => -Step,
            _ => 0.0
        };

        if (Math.Abs(delta) < 0.0001)
            return;

        var current = _parameters[_selectedAxis];
        if (current is null)
            return;

        current.CurrentNumber = Math.Clamp(current.CurrentNumber + delta, 0.0, MaximumValue);
        _statusText = string.Empty;
        InvalidateVisual();
        e.Handled = true;
    }

    private void UpdateAxisFromPoint(int axis, Point point)
    {
        var (center, radius) = GetGeometry();
        if (radius <= 0)
            return;

        var unit = UnitVector(Axes[axis].AngleDegrees);
        var relative = point - center;
        var projected = relative.X * unit.X + relative.Y * unit.Y;
        var value = Math.Clamp(projected / radius * MaximumValue, 0.0, MaximumValue);
        value = Math.Round(value / Step, MidpointRounding.AwayFromZero) * Step;

        var parameter = _parameters[axis];
        if (parameter is not null)
            parameter.CurrentNumber = value;

        _statusText = string.Empty;
        InvalidateVisual();
    }

    private int FindAxis(Point point)
        => FindHandleAxis(point, 14.0);

    private int FindHandleAxis(Point point, double maximumDistance)
    {
        var values = GetValues();
        var (center, radius) = GetGeometry();
        var best = -1;
        var bestDistance = maximumDistance;
        for (var i = 0; i < Axes.Length; i++)
        {
            var handle = center + UnitVector(Axes[i].AngleDegrees) * (radius * values[i] / MaximumValue);
            var distance = (point - handle).Length;
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }
        return best;
    }

    private (Point Center, double Radius) GetGeometry()
    {
        var radius = Math.Min(124.0, Math.Max(92.0, Math.Min(ActualWidth * 0.15, 124.0)));
        return (new Point(Math.Min(ActualWidth * 0.25, 260.0), ActualHeight / 2.0), radius);
    }

    private static Vector UnitVector(double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180.0;
        return new Vector(Math.Cos(radians), Math.Sin(radians));
    }

    private static Brush FindBrush(string key, Brush fallback)
        => Application.Current?.TryFindResource(key) as Brush ?? fallback;

    private void DrawCenteredText(DrawingContext dc, string text, Point center, double size, Brush brush, FontWeight weight)
    {
        var formatted = CreateText(text, size * 1.25, brush, weight);
        var x = Math.Clamp(center.X - formatted.Width / 2.0, 8.0, Math.Max(8.0, ActualWidth - formatted.Width - 8.0));
        dc.DrawText(formatted, new Point(x, center.Y - formatted.Height / 2.0));
    }

    private void DrawAnchoredText(DrawingContext dc, string text, Point anchor, double size, Brush brush, FontWeight weight, TextAnchor textAnchor)
    {
        var formatted = CreateText(text, size, brush, weight);
        double x = textAnchor switch
        {
            TextAnchor.Left => anchor.X,
            TextAnchor.Right => anchor.X - formatted.Width,
            _ => anchor.X - formatted.Width / 2.0
        };

        x = Math.Clamp(x, 8.0, Math.Max(8.0, ActualWidth - formatted.Width - 8.0));
        dc.DrawText(formatted, new Point(x, anchor.Y - formatted.Height / 2.0));
    }

    private void BeginLegendHitArea(int axis)
    {
        _legendRecordingAxis = axis;
        _legendHitRects.Remove(axis);
    }

    private void EndLegendHitArea()
        => _legendRecordingAxis = -1;

    private void IncludeLegendTextHit(Rect bounds, FormattedText formatted)
    {
        if (_legendRecordingAxis < 0)
            return;

        var textWidth = Math.Min(bounds.Width, formatted.WidthIncludingTrailingWhitespace);
        var textHeight = Math.Min(bounds.Height, formatted.Height);
        var textLeft = bounds.Left;
        var textRect = new Rect(
            textLeft - 3.0,
            bounds.Top - 2.0,
            Math.Max(1.0, textWidth + 6.0),
            Math.Max(1.0, textHeight + 4.0));

        if (_legendHitRects.TryGetValue(_legendRecordingAxis, out var current))
            _legendHitRects[_legendRecordingAxis] = Rect.Union(current, textRect);
        else
            _legendHitRects[_legendRecordingAxis] = textRect;
    }

    private void DrawClippedText(DrawingContext dc, string text, Rect bounds, double size, Brush brush, FontWeight weight)
    {
        var formatted = CreateText(text, size, brush, weight);
        IncludeLegendTextHit(bounds, formatted);
        formatted.MaxTextWidth = Math.Max(1.0, bounds.Width);
        formatted.MaxLineCount = 1;
        formatted.Trimming = TextTrimming.CharacterEllipsis;
        formatted.TextAlignment = TextAlignment.Left;
        dc.DrawText(formatted, new Point(bounds.Left, bounds.Top));
    }

    private void DrawText(DrawingContext dc, string text, Point point, double size, Brush brush, FontWeight weight)
        => dc.DrawText(CreateText(text, size, brush, weight), point);

    private FormattedText CreateText(string text, double size, Brush brush, FontWeight weight)
        => new(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            size, brush, VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private enum TextAnchor { Left, Center, Right }
    private sealed record AxisDefinition(string ParameterId, string Label, double AngleDegrees);
}
