using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// ALife Director mutant spawn-cap radar.
/// Each axis independently scales one mutant restriction across Newbie, Experienced, Veteran, and Master ranks while preserving authored zero caps.
/// </summary>
public sealed class MutantGroupSizeRadar : FrameworkElement
{
    private const double RadarInfoTextSize = 11.0;
    private const double RadarLegendTextSize = 11.0;
    private const double RadarAxisTextSize = 10.5;

    private const double MinimumMultiplier = 0.25;
    private const double MaximumMultiplier = 3.0;
    private const double VanillaMultiplier = 1.0;
    private const double RadarStep = 0.05;

    private static readonly AxisDefinition[] Axes =
    {
        new("alife.mutantSpawn.bayun", "Bayun", -90.000, new[] { 1.0, 1.0, 1.0, 2.0 }, "1/1/1/2", "Mostly solo; Master can allow a pair."),
        new("alife.mutantSpawn.blindDog", "Blind Dog", -64.286, new[] { 4.0, 6.0, 8.0, 12.0 }, "4/6/8/12", "Pack mutant; higher ALife ranks allow progressively larger packs."),
        new("alife.mutantSpawn.bloodsucker", "Bloodsucker", -38.571, new[] { 0.0, 1.0, 1.0, 1.0 }, "0/1/1/1", "Disabled for Newbie; capped at one for Experienced, Veteran and Master."),
        new("alife.mutantSpawn.boar", "Boar", -12.857, new[] { 2.0, 3.0, 5.0, 6.0 }, "2/3/5/6", "Common pack mutant; maximum mutants in the group rises steadily with ALife rank."),
        new("alife.mutantSpawn.burer", "Burer", 12.857, new[] { 1.0, 1.0, 1.0, 2.0 }, "1/1/1/2", "Mostly solo; Master can allow a pair."),
        new("alife.mutantSpawn.chimera", "Chimera", 38.571, new[] { 1.0, 1.0, 1.0, 1.0 }, "1/1/1/1", "Elite mutant; vanilla allows at most one mutant in the group on every ALife rank."),
        new("alife.mutantSpawn.controller", "Controller", 64.286, new[] { 0.0, 1.0, 1.0, 1.0 }, "0/1/1/1", "Disabled for Newbie; capped at one for Experienced, Veteran and Master."),
        new("alife.mutantSpawn.deer", "Deer", 90.000, new[] { 1.0, 1.0, 1.0, 1.0 }, "1/1/1/1", "Rare large mutant; vanilla allows at most one mutant in the group on every ALife rank."),
        new("alife.mutantSpawn.flesh", "Flesh", 115.714, new[] { 2.0, 3.0, 5.0, 6.0 }, "2/3/5/6", "Common herd mutant; maximum mutants in the group rises steadily with ALife rank."),
        new("alife.mutantSpawn.poltergeist", "Poltergeist", 141.429, new[] { 1.0, 1.0, 2.0, 2.0 }, "1/1/2/2", "Solo at lower ALife ranks; Veteran and Master can allow two."),
        new("alife.mutantSpawn.pseudodog", "Pseudodog", 167.143, new[] { 1.0, 1.0, 1.0, 1.0 }, "1/1/1/1", "Fast predator; vanilla allows at most one mutant in the group on every ALife rank."),
        new("alife.mutantSpawn.ratSwarm", "Rat Swarm", 192.857, new[] { 1.0, 1.0, 1.0, 1.0 }, "1/1/1/1", "One Director swarm unit at a time on every ALife rank."),
        new("alife.mutantSpawn.snork", "Snork", 218.571, new[] { 1.0, 3.0, 4.0, 6.0 }, "1/3/4/6", "Aggressive pack mutant; maximum mutants in the group grows strongly with ALife rank."),
        new("alife.mutantSpawn.tushkan", "Tushkan", 244.286, new[] { 6.0, 8.0, 12.0, 16.0 }, "6/8/12/16", "Swarm mutant; vanilla allows the largest encounter groups here."),
    };


    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Axes.Length];
    private int _selectedAxis;
    private int _dragAxis = -1;
    private int _hoverAxis = -1;
        private readonly Dictionary<int, Rect> _legendHitRects = new();
    private readonly Dictionary<int, Rect> _axisLabelHitRects = new();
    private int _legendRecordingAxis = -1;

private readonly RadarStatusMessage _status;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(MutantGroupSizeRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public MutantGroupSizeRadar()
    {
        _status = new RadarStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = 720;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 700 : Math.Max(400, availableSize.Width);
        return new Size(width, 720);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MutantGroupSizeRadar)d).RebindParameters();

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
            or nameof(ParameterViewModel.CurrentDisplay)
            or nameof(ParameterViewModel.TechnicalDisplay)
            or nameof(ParameterViewModel.IsModified))
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
        var vanillaPen = new Pen(accentBrush, 1.5) { DashStyle = DashStyles.Dash };

        DrawRing(dc, center, ValueRadius(0.5, radius), neutralPen);
        DrawRing(dc, center, ValueRadius(1.0, radius), vanillaPen);
        DrawRing(dc, center, ValueRadius(1.5, radius), neutralPen);
        DrawRing(dc, center, ValueRadius(2.0, radius), neutralPen);
        DrawRing(dc, center, ValueRadius(2.5, radius), neutralPen);
        DrawRing(dc, center, ValueRadius(3.0, radius), neutralPen);

        _axisLabelHitRects.Clear();
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
            points[i] = center + unit * ValueRadius(values[i], radius);
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

        DrawRingCaption(dc, "0.5x", center, ValueRadius(0.5, radius), mutedBrush);
        DrawRingCaption(dc, "1x", center, ValueRadius(1.0, radius), accentBrush);
        DrawRingCaption(dc, "1.5x", center, ValueRadius(1.5, radius), mutedBrush);
        DrawRingCaption(dc, "2x", center, ValueRadius(2.0, radius), mutedBrush);
        DrawRingCaption(dc, "2.5x", center, ValueRadius(2.5, radius), mutedBrush);
        DrawRingCaption(dc, "3x", center, ValueRadius(3.0, radius), mutedBrush);

        for (var i = 0; i < Axes.Length; i++)
        {
            var unit = UnitVector(Axes[i].AngleDegrees);
            var labelPoint = center + unit * (radius + 30);
            _axisLabelHitRects[i] = new Rect(labelPoint.X - 80.0, labelPoint.Y - 20.0, 160.0, 40.0);
            DrawAxisLabel(
                dc,
                Axes[i].Label,
                $"{values[i]:0.##}x",
                labelPoint,
                mutedBrush,
                i == _hoverAxis ? Brushes.White : i == _selectedAxis ? accentBrush : textBrush);
        }

        DrawLegend(dc, values, textBrush, mutedBrush, accentBrush);

        if (!string.IsNullOrWhiteSpace(_status.Text))
        {
            DrawCenteredText(
                dc,
                _status.Text,
                new Point(GetInfoRect().Left + GetInfoRect().Width / 2.0, ActualHeight - 18), RadarInfoTextSize,
                accentBrush,
                FontWeights.Normal);
        }
    }

    private Rect GetInfoRect()
    {
        var left = Math.Max(ActualWidth * 0.50, 400.0);
        return new Rect(left, 72.0, Math.Max(260.0, ActualWidth - left - 16.0), Math.Max(424.0, ActualHeight - 116.0));
    }

    private void DrawInfoHeader(DrawingContext dc, string text, Brush brush)
    {
        var info = GetInfoRect();
        DrawClippedText(dc, text, new Rect(info.Left, 28.0, info.Width, 34.0), RadarInfoTextSize, brush, FontWeights.Normal);
    }

    private void DrawLegend(
        DrawingContext dc,
        double[] values,
        Brush textBrush,
        Brush mutedBrush,
        Brush accentBrush)
    {
        var info = GetInfoRect();
        const double rowHeight = 31.0;
        var totalHeight = Axes.Length * rowHeight;
        var top = info.Top + Math.Max(0.0, (info.Height - totalHeight) / 2.0);
        var legendBounds = new Rect(info.Left, top, info.Width, totalHeight);

        for (var i = 0; i < Axes.Length; i++)
            DrawLegendRow(dc, i, legendBounds, i * rowHeight, values, textBrush, mutedBrush, accentBrush);
    }

    private void DrawLegendRow(
        DrawingContext dc,
        int axis,
        Rect bounds,
        double yOffset,
        double[] values,
        Brush textBrush,
        Brush mutedBrush,
        Brush accentBrush)
    {
        var parameter = _parameters[axis];
        if (parameter is null)
            return;

        var info = Axes[axis];
        var value = values[axis];
        var y = bounds.Top + yOffset;
        var modified = Math.Abs(value - VanillaMultiplier) > 0.000001;
        var hovered = axis == _hoverAxis;

        BeginLegendHitArea(axis);

        DrawClippedText(
            dc,
            info.Label,
            new Rect(bounds.Left + 6, y + 6, bounds.Width * 0.24, 18.0), RadarLegendTextSize, modified ? accentBrush : textBrush,
            FontWeights.SemiBold);

        DrawClippedText(
            dc,
            $"{value:0.##}x",
            new Rect(bounds.Left + bounds.Width * 0.24, y + 6, bounds.Width * 0.12, 18.0), RadarLegendTextSize, modified ? accentBrush : hovered ? textBrush : mutedBrush,
            modified ? FontWeights.SemiBold : FontWeights.Normal);

        DrawClippedText(
            dc,
            FormatEffectiveCaps(info, value),
            new Rect(bounds.Left + bounds.Width * 0.36, y + 6, bounds.Width * 0.64 - 6, 18.0), RadarLegendTextSize, modified ? accentBrush : hovered ? textBrush : mutedBrush,
            modified ? FontWeights.SemiBold : FontWeights.Normal);

        EndLegendHitArea();
    }

    private static string FormatEffectiveCaps(AxisDefinition info, double speciesMultiplier)
    {
        var values = info.AuthoredHp
            .Select(authoredCap => authoredCap <= 0.0
                ? 0.0
                : Math.Max(1.0, Math.Floor(authoredCap * speciesMultiplier + 0.5)))
            .Select(value => value.ToString("0", CultureInfo.InvariantCulture))
            .ToArray();

        if (info.ParameterId == "alife.mutantSpawn.ratSwarm")
        {
            var count = int.Parse(values[0], CultureInfo.InvariantCulture);
            return $"Up to {count} {(count == 1 ? "swarm" : "swarms")}";
        }

        return $"Up to {values[0]} Newbie | {values[1]} Experienced | {values[2]} Veteran | {values[3]} Master";
    }

    private double[] GetValues()
    {
        var values = new double[Axes.Length];

        for (var i = 0; i < Axes.Length; i++)
            values[i] = Math.Clamp(_parameters[i]?.CurrentNumber ?? VanillaMultiplier, MinimumMultiplier, MaximumMultiplier);

        return values;
    }

    private void SetValue(int axis, double value)
    {
        var parameter = _parameters[axis];
        if (parameter is null)
            return;

        value = Math.Clamp(value, parameter.Minimum, parameter.Maximum);
        value = Math.Round(value / RadarStep, MidpointRounding.AwayFromZero) * RadarStep;
        parameter.CurrentNumber = Math.Clamp(value, parameter.Minimum, parameter.Maximum);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var point = e.GetPosition(this);

        // The legend is informational for left-clicks. Consume the click here so
        // it cannot fall through to FindAxis(), which maps arbitrary positions
        // to the nearest radar axis and can otherwise push that value to max.
        if (FindLegendAxis(point) >= 0 || FindAxisLabelAxis(point) >= 0)
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
            hoverAxis = FindAxisLabelAxis(point);
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

        var point = e.GetPosition(this);
        var axis = FindLegendAxis(point);
        if (axis < 0)
            axis = FindAxisLabelAxis(point);
        if (axis < 0 || e.Delta == 0)
            return;

        var parameter = _parameters[axis];
        if (parameter is null)
            return;

        var direction = e.Delta > 0 ? 1 : -1;
        _selectedAxis = axis;
        _hoverAxis = axis;
        parameter.CurrentNumber = Math.Clamp(
            parameter.CurrentNumber + (direction * RadarStep),
            parameter.Minimum,
            parameter.Maximum);
        _status.Clear();
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
            axis = FindAxisLabelAxis(point);
        if (axis < 0)
            axis = FindHandleAxis(point, 22.0);

        if (axis < 0)
            return;

        _selectedAxis = axis;
        var parameter = _parameters[axis];

        if (parameter is not null)
        {
            parameter.CurrentNumber = VanillaMultiplier;
            _status.ShowTransient($"{Axes[axis].Label} reset to vanilla 1x.");
        }

        InvalidateVisual();
        e.Handled = true;
    }
    private int FindAxisLabelAxis(Point point)
    {
        foreach (var hit in _axisLabelHitRects)
        {
            if (hit.Value.Contains(point))
                return hit.Key;
        }

        return -1;
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
                    resetParameter.CurrentNumber = VanillaMultiplier;
            }

            _status.Clear();
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

        var parameter = _parameters[_selectedAxis];
        if (parameter is null)
            return;

        parameter.CurrentNumber = Math.Clamp(
            parameter.CurrentNumber + (direction * RadarStep),
            parameter.Minimum,
            parameter.Maximum);

        _status.Clear();
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
        var normalized = Math.Clamp(projected / radius, 0.0, 1.0);
        var value = MinimumMultiplier + normalized * (MaximumMultiplier - MinimumMultiplier);

        SetValue(axis, value);
        _status.Clear();
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
            var handle = center + UnitVector(Axes[i].AngleDegrees) * ValueRadius(values[i], radius);
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
        var radius = Math.Min(150.0, Math.Max(112.0, Math.Min(ActualWidth * 0.18, 150.0)));
        return (new Point(Math.Min(ActualWidth * 0.27, 270.0), ActualHeight / 2.0), radius);
    }

    private static double ValueRadius(double value, double radius)
        => radius * Math.Clamp((value - MinimumMultiplier) / (MaximumMultiplier - MinimumMultiplier), 0.0, 1.0);

    private void DrawAxisLabel(
        DrawingContext dc,
        string label,
        string value,
        Point position,
        Brush mutedBrush,
        Brush activeBrush)
    {
        var anchor = position.X < GetGeometry().Center.X - 20.0
            ? TextAnchor.Right
            : position.X > GetGeometry().Center.X + 20.0
                ? TextAnchor.Left
                : TextAnchor.Center;

        DrawAnchoredText(dc, label, new Point(position.X, position.Y - 5), RadarAxisTextSize, activeBrush, FontWeights.SemiBold, anchor);
        DrawAnchoredText(dc, value, new Point(position.X, position.Y + 7), RadarAxisTextSize, mutedBrush, FontWeights.Normal, anchor);
    }

    private void DrawRingCaption(DrawingContext dc, string text, Point center, double radius, Brush brush)
        => DrawText(dc, text, new Point(center.X + 6, center.Y - radius - 8), 8.6, brush, FontWeights.SemiBold);

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

    private static Vector UnitVector(double angleDegrees)
    {
        var radians = angleDegrees * Math.PI / 180.0;
        return new Vector(Math.Cos(radians), Math.Sin(radians));
    }

    private static Brush FindBrush(string key, Brush fallback)
        => Application.Current?.TryFindResource(key) as Brush ?? fallback;

    private void DrawCenteredText(DrawingContext dc, string text, Point center, double size, Brush brush, FontWeight weight)
    {
        var formatted = CreateText(text, size, brush, weight);
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

    private void DrawClippedText(DrawingContext dc, string text, Rect bounds, double size, Brush brush, FontWeight weight)
    {
        var formatted = CreateText(text, size, brush, weight);
        IncludeLegendTextHit(bounds, formatted);
        formatted.MaxTextWidth = Math.Max(1.0, bounds.Width);
        formatted.MaxLineCount = 1;
        formatted.Trimming = TextTrimming.None;
        formatted.TextAlignment = TextAlignment.Left;
        dc.DrawText(formatted, new Point(bounds.Left, bounds.Top));
    }

    private void DrawText(DrawingContext dc, string text, Point point, double size, Brush brush, FontWeight weight)
        => dc.DrawText(CreateText(text, size, brush, weight), point);

    private FormattedText CreateText(string text, double size, Brush brush, FontWeight weight)
    {
        var formatted = new FormattedText(

            text,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            size,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        var separatorBrush = FindBrush("AccentBrush", Brushes.Goldenrod);
        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '|')
                formatted.SetForegroundBrush(separatorBrush, i, 1);
        }

        return formatted;
    }

    private enum TextAnchor { Left, Center, Right }

    private sealed record AxisDefinition(
        string ParameterId,
        string Label,
        double AngleDegrees,
        double[] AuthoredHp,
        string VanillaLabel,
        string PlayerHint);
}
