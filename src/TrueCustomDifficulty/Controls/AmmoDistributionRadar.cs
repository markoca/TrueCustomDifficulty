using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Four-axis editor for grouped vanilla ammo-family weights used by dedicated breakable ammo boxes.
/// The backing values remain ordinary ParameterViewModels so presets/writer logic stay catalog-driven.
/// This control never adds new PossibleItems entries.
/// </summary>
public sealed class AmmoDistributionRadar : FrameworkElement
{
    private const double RadarInfoTextSize = 11.0;
    private const double RadarLegendTextSize = 11.0;
    private const double RadarAxisTextSize = 10.5;

    private const double MaximumValue = 3.0;
    private const double Step = 0.25;

    private static readonly AxisDefinition[] Axes =
    {
        new("loot.ammoBoxWeight918", "9×18 / 9×19 mm", -90),
        new("loot.ammoBoxWeight545", "5.45 / 5.56 mm", 0),
        new("loot.ammoBoxWeight045", ".45 ACP", 90),
        new("loot.ammoBoxWeight012", "12 Gauge", 180),
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
        typeof(AmmoDistributionRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public AmmoDistributionRadar()
    {
        Focusable = true;
        MinHeight = 500;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 560 : Math.Max(320, availableSize.Width);
        return new Size(width, 500);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AmmoDistributionRadar)d).RebindParameters();

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

        var (center, radius) = GetGeometry();
        if (radius <= 10)
            return;

        // Subtle local panel only behind the radar drawing area.
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

        var neutralPen = new Pen(borderBrush, 1);
        var vanillaPen = new Pen(accentBrush, 1.6) { DashStyle = DashStyles.Dash };

        DrawRing(dc, center, radius / 3.0, vanillaPen); // 1x vanilla ring
        DrawRing(dc, center, radius * 2.0 / 3.0, neutralPen); // 2x
        DrawRing(dc, center, radius, neutralPen); // 3x

        for (var i = 0; i < Axes.Length; i++)
        {
            var unit = UnitVector(Axes[i].AngleDegrees);
            var end = center + unit * radius;
            dc.DrawLine(neutralPen, center, end);
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

        var handleRadius = 6.0;
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

            var outer = i == _selectedAxis ? 9.0 : handleRadius;
            var outlineWidth = i == _selectedAxis ? 2.4 : hovered ? 2.0 : 1.5;
            dc.DrawEllipse(panelBrush, new Pen(accentBrush, outlineWidth), points[i], outer, outer);
            dc.DrawEllipse(accentBrush, null, points[i], 3.2, 3.2);
        }

        DrawRingCaption(dc, "1x = VANILLA", center, radius / 3.0, accentBrush);
        DrawRingCaption(dc, "2x", center, radius * 2.0 / 3.0, mutedBrush);
        DrawRingCaption(dc, "3x", center, radius, mutedBrush);

        var variantDetails = CalculateVariantDetails(values);
        for (var i = 0; i < Axes.Length; i++)
        {
            var unit = UnitVector(Axes[i].AngleDegrees);
            var labelPoint = center + unit * (radius + 32);
            DrawAxisLabel(dc, Axes[i].Label, $"{values[i]:0.##}x",
                labelPoint, mutedBrush, i == _selectedAxis ? accentBrush : textBrush);
        }

        DrawDetailsPanel(dc, values, variantDetails, textBrush, mutedBrush, accentBrush);

        var info = GetInfoRect();
        DrawClippedText(
            dc,
            "A range means that caliber exists in more than one vanilla ammo-box pool.",
            new Rect(info.Left, ActualHeight - 68, info.Width, 20), RadarInfoTextSize,
            mutedBrush,
            FontWeights.Normal);

        var instruction = string.IsNullOrWhiteSpace(_statusText)
            ? "Drag a point | Arrow keys adjust selected family | Right-click point/legend resets that family | R resets all"
            : _statusText;
        DrawCenteredText(dc, instruction, new Point(GetInfoRect().Left + GetInfoRect().Width / 2.0, ActualHeight - 28), RadarInfoTextSize, string.IsNullOrWhiteSpace(_statusText) ? mutedBrush : accentBrush, FontWeights.Normal);
    }

    private void DrawAxisLabel(
        DrawingContext dc,
        string label,
        string multiplier,
        Point position,
        Brush mutedBrush,
        Brush activeBrush)
    {
        var radarCenter = GetGeometry().Center;
        var anchor = position.X < radarCenter.X - 28.0
            ? TextAnchor.Right
            : position.X > radarCenter.X + 28.0
                ? TextAnchor.Left
                : TextAnchor.Center;

        DrawAnchoredText(dc, label, new Point(position.X, position.Y - 6), RadarAxisTextSize, activeBrush, FontWeights.SemiBold, anchor);
        DrawAnchoredText(dc, multiplier, new Point(position.X, position.Y + 8), RadarAxisTextSize, mutedBrush, FontWeights.Normal, anchor);
    }

    private Rect GetInfoRect()
    {
        var left = Math.Max(ActualWidth * 0.48, 370.0);
        return new Rect(left, 48.0, Math.Max(220.0, ActualWidth - left - 16.0), Math.Max(220.0, ActualHeight - 126.0));
    }

    private void DrawDetailsPanel(
        DrawingContext dc,
        IReadOnlyList<double> values,
        IReadOnlyList<string>[] details,
        Brush textBrush,
        Brush mutedBrush,
        Brush accentBrush)
    {
        var info = GetInfoRect();
        DrawClippedText(
            dc,
            "AMMO BOX MIX | higher = this family appears more often",
            new Rect(info.Left, 18, info.Width, 18), RadarInfoTextSize, textBrush,
            FontWeights.SemiBold);

        var rowHeights = details.Select(detail => 28.0 + (detail.Count * 18.0)).ToArray();
        var totalHeight = rowHeights.Sum();
        var y = info.Top + Math.Max(0.0, (info.Height - totalHeight) / 2.0);

        for (var i = 0; i < Axes.Length; i++)
        {
            var hovered = i == _hoverAxis;
            var parameter = _parameters[i];
            var modified = parameter?.IsModified ?? false;
            BeginLegendHitArea(i);

            DrawClippedText(
                dc,
                $"{Axes[i].Label} | {values[i]:0.##}x weight",
                new Rect(info.Left + 6, y + 5, info.Width - 12, 17), RadarLegendTextSize, modified ? accentBrush : hovered ? textBrush : textBrush,
                FontWeights.SemiBold);

            var detailY = y + 24;
            foreach (var detail in details[i])
            {
                DrawClippedText(
                    dc,
                    detail,
                    new Rect(info.Left + 18, detailY, info.Width - 24, 16), RadarLegendTextSize, modified ? accentBrush : hovered ? textBrush : mutedBrush,
                    FontWeights.Normal);
                detailY += 18;
            }

            EndLegendHitArea();
            y += rowHeights[i];
        }
    }

    private void DrawRingCaption(DrawingContext dc, string text, Point center, double radius, Brush brush)
    {
        DrawText(dc, text, new Point(center.X + 7, center.Y - radius - 8), 9.3, brush, FontWeights.SemiBold);
    }

    private void DrawRing(DrawingContext dc, Point center, double radius, Pen pen)
    {
        var points = Axes.Select(axis => center + UnitVector(axis.AngleDegrees) * radius).ToArray();
        dc.DrawGeometry(null, pen, BuildClosedGeometry(points));
    }

    private static StreamGeometry BuildClosedGeometry(IReadOnlyList<Point> points)
    {
        var geometry = new StreamGeometry();
        using (var context = geometry.Open())
        {
            context.BeginFigure(points[0], isFilled: true, isClosed: true);
            context.PolyLineTo(points.Skip(1).ToArray(), isStroked: true, isSmoothJoin: true);
        }
        geometry.Freeze();
        return geometry;
    }

    private double[] GetValues()
    {
        var values = new double[Axes.Length];
        for (var i = 0; i < Axes.Length; i++)
            values[i] = Math.Clamp(_parameters[i]?.CurrentNumber ?? 1.0, 0.0, MaximumValue);
        return values;
    }

    private static IReadOnlyList<string>[] CalculateVariantDetails(IReadOnlyList<double> values)
    {
        // Vanilla destructible-ammo membership:
        // pool A: A918D, A919D, A545D, A012D
        // pool B: A045D, A919D, A556D, A012D
        //
        // Player-facing rows use one consistent metric: the weighted spawn share
        // across every vanilla breakable-ammo pool in which that concrete ammo exists.
        // One-pool ammo shows one percentage; two-pool ammo shows a min-max range.
        var nine = values[0];
        var five = values[1];
        var fortyFive = values[2];
        var twelve = values[3];

        var poolA = (2.0 * nine) + five + twelve;
        var poolB = nine + five + fortyFive + twelve;

        static double Share(double value, double sum) => sum > 0.0 ? value / sum * 100.0 : 0.0;
        static string P(double value) => $"{value:0.#}%";
        static string Range(double a, double b)
        {
            var lo = Math.Min(a, b);
            var hi = Math.Max(a, b);
            return Math.Abs(hi - lo) < 0.05 ? P(lo) : $"{lo:0.#}-{hi:0.#}%";
        }

        var nine18 = Share(nine, poolA);
        var nine19A = Share(nine, poolA);
        var nine19B = Share(nine, poolB);
        var five45 = Share(five, poolA);
        var five56 = Share(five, poolB);
        var fortyFiveShare = Share(fortyFive, poolB);
        var twelveA = Share(twelve, poolA);
        var twelveB = Share(twelve, poolB);

        return new IReadOnlyList<string>[]
        {
            new[]
            {
                $"9×18 current box share: {P(nine18)}",
                $"9×19 current box share: {Range(nine19A, nine19B)}",
            },
            new[]
            {
                $"5.45×39 current box share: {P(five45)}",
                $"5.56×45 current box share: {P(five56)}",
            },
            new[]
            {
                $".45 ACP current box share: {P(fortyFiveShare)}",
            },
            new[]
            {
                $"12×70 current box share: {Range(twelveA, twelveB)}",
            },
        };
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
        TrySetAxis(axis, parameter.CurrentNumber + (direction * Step));
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
            axis = FindHandleAxis(point, 26.0);

        if (axis < 0)
            return;

        _selectedAxis = axis;
        var parameter = _parameters[axis];
        if (parameter is not null)
            parameter.CurrentNumber = 1.0;

        _statusText = $"{Axes[axis].Label} reset to 1x.";
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
            foreach (var resetParameter in _parameters)
            {
                if (resetParameter is not null)
                    resetParameter.CurrentNumber = 1.0;
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
        if (Math.Abs(delta) < 0.001)
            return;

        var parameter = _parameters[_selectedAxis];
        if (parameter is null)
            return;

        TrySetAxis(_selectedAxis, parameter.CurrentNumber + delta);
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
        TrySetAxis(axis, value);
    }

    private void TrySetAxis(int axis, double value)
    {
        var parameter = _parameters[axis];
        if (parameter is null)
            return;

        value = Math.Clamp(value, 0.0, MaximumValue);
        var proposed = GetValues();
        proposed[axis] = value;

        var eastSum = (2.0 * proposed[0]) + proposed[1] + proposed[3];
        var westSum = proposed[0] + proposed[1] + proposed[2] + proposed[3];
        if (eastSum <= 0.0 || westSum <= 0.0)
        {
            _statusText = "At least one vanilla ammo family in each box pool must remain above 0x.";
            InvalidateVisual();
            return;
        }

        _statusText = string.Empty;
        parameter.CurrentNumber = value;
        InvalidateVisual();
    }

    private int FindHandleAxis(Point point, double maximumDistance)
    {
        var (center, radius) = GetGeometry();
        if (radius <= 0)
            return -1;

        var values = GetValues();
        var bestHandle = -1;
        var bestDistance = double.MaxValue;
        for (var i = 0; i < Axes.Length; i++)
        {
            var handle = center + UnitVector(Axes[i].AngleDegrees) * (radius * values[i] / MaximumValue);
            var distance = (handle - point).Length;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestHandle = i;
            }
        }

        return bestDistance <= maximumDistance ? bestHandle : -1;
    }

    private int FindAxis(Point point)
        => FindHandleAxis(point, 14.0);

    private (Point Center, double Radius) GetGeometry()
    {
        var radius = Math.Min(132.0, Math.Max(98.0, Math.Min(ActualWidth * 0.16, 132.0)));
        return (new Point(Math.Min(ActualWidth * 0.25, 270.0), ActualHeight / 2.0), radius);
    }

    private static Vector UnitVector(double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180.0;
        return new Vector(Math.Cos(radians), Math.Sin(radians));
    }

    private static Brush FindBrush(string key, Brush fallback)
        => Application.Current?.TryFindResource(key) as Brush ?? fallback;

    private void DrawCenteredText(
        DrawingContext dc,
        string text,
        Point center,
        double size,
        Brush brush,
        FontWeight weight)
    {
        var formatted = CreateText(text, size * 1.25, brush, weight);
        var x = Math.Clamp(center.X - formatted.Width / 2.0, 8.0, Math.Max(8.0, ActualWidth - formatted.Width - 8.0));
        dc.DrawText(formatted, new Point(x, center.Y - formatted.Height / 2.0));
    }

    private void DrawAnchoredText(
        DrawingContext dc,
        string text,
        Point anchor,
        double size,
        Brush brush,
        FontWeight weight,
        TextAnchor textAnchor)
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

    private void DrawClippedText(
        DrawingContext dc,
        string text,
        Rect bounds,
        double size,
        Brush brush,
        FontWeight weight)
    {
        var formatted = CreateText(text, size, brush, weight);
        IncludeLegendTextHit(bounds, formatted);
        formatted.MaxTextWidth = Math.Max(1.0, bounds.Width);
        formatted.MaxLineCount = 1;
        formatted.Trimming = TextTrimming.CharacterEllipsis;
        formatted.TextAlignment = TextAlignment.Left;
        dc.DrawText(formatted, new Point(bounds.Left, bounds.Top));
    }

    private void DrawText(
        DrawingContext dc,
        string text,
        Point point,
        double size,
        Brush brush,
        FontWeight weight)
    {
        dc.DrawText(CreateText(text, size, brush, weight), point);
    }

    private FormattedText CreateText(string text, double size, Brush brush, FontWeight weight)
        => new(
            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            size,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private enum TextAnchor { Left, Center, Right }

    private sealed record AxisDefinition(string ParameterId, string Label, double AngleDegrees);
}
