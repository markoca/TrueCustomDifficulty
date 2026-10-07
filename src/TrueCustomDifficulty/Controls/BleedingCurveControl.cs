using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Aggregate editor for the five player-facing Bleeding controls.
/// Global bleeding strength and natural recovery stay as compact tracks while
/// Low / Medium / Severe damage are edited as one absolute damage-per-tick curve.
/// </summary>
public sealed class BleedingCurveControl : FrameworkElement
{
    private const string SeverityId = "survival.bleedingSeverity";
    private const string RecoveryId = "survival.bleedingRecovery";
    private const string LowDamageId = "survival.bleedingLowDamage";
    private const string MediumDamageId = "survival.bleedingMediumDamage";
    private const string SevereDamageId = "survival.bleedingStrongDamage";

    private const double CurveMaximumDamage = 4.0;

    private ParameterViewModel? _severity;
    private ParameterViewModel? _recovery;
    private readonly ParameterViewModel?[] _damage = new ParameterViewModel?[3];

    private Rect _severityTrackRect;
    private Rect _recoveryTrackRect;
    private Rect _plotRect;
    private readonly Rect[] _damageHitRects = new Rect[3];

    private DragTarget _dragTarget = DragTarget.None;
    private DragTarget _selectedTarget = DragTarget.LowDamage;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(BleedingCurveControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public BleedingCurveControl()
    {
        Focusable = true;
        MinHeight = 380;
        SnapsToDevicePixels = true;
        Cursor = Cursors.Arrow;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 760.0 : Math.Max(520.0, availableSize.Width);
        return new Size(width, 380.0);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((BleedingCurveControl)d).RebindParameters();

    private void RebindParameters()
    {
        foreach (var parameter in EnumerateBoundParameters())
        {
            if (parameter is not null)
                parameter.PropertyChanged -= Parameter_PropertyChanged;
        }

        _severity = null;
        _recovery = null;
        Array.Clear(_damage, 0, _damage.Length);

        if (Parameters is not null)
        {
            var byId = Parameters
                .OfType<ParameterViewModel>()
                .ToDictionary(x => x.Id, StringComparer.Ordinal);

            byId.TryGetValue(SeverityId, out _severity);
            byId.TryGetValue(RecoveryId, out _recovery);

            if (byId.TryGetValue(LowDamageId, out var lowDamage))
                _damage[0] = lowDamage;
            if (byId.TryGetValue(MediumDamageId, out var mediumDamage))
                _damage[1] = mediumDamage;
            if (byId.TryGetValue(SevereDamageId, out var severeDamage))
                _damage[2] = severeDamage;

            foreach (var parameter in EnumerateBoundParameters())
            {
                if (parameter is not null)
                    parameter.PropertyChanged += Parameter_PropertyChanged;
            }
        }

        InvalidateVisual();
    }

    private IEnumerable<ParameterViewModel?> EnumerateBoundParameters()
    {
        yield return _severity;
        yield return _recovery;
        yield return _damage[0];
        yield return _damage[1];
        yield return _damage[2];
    }

    private void Parameter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ParameterViewModel.CurrentNumber)
            or nameof(ParameterViewModel.CurrentDisplay)
            or nameof(ParameterViewModel.IsModified))
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
        var softBorder = FindBrush("BorderSoftBrush", border);
        var accent = FindBrush("AccentBrush", Brushes.Goldenrod);
        var panel = FindBrush("PanelAltBrush", Brushes.Black);

        IndustrialChrome.DrawFrame(
            dc,
            new Rect(2, 2, Math.Max(0.0, ActualWidth - 4), Math.Max(0.0, ActualHeight - 4)),
            panel,
            border,
            accent,
            softBorder,
            8.0,
            accentEdge: true,
            technicalMarks: true);

        DrawText(dc, "BLEEDING SYSTEM", new Point(16, 14), 10.5, accent, FontWeights.Bold);
        DrawText(dc, "GLOBAL EFFECTS", new Point(16, 34), 8.8, muted, FontWeights.Bold);

        var gap = 10.0;
        var cardLeft = 16.0;
        var cardTop = 51.0;
        var totalCardWidth = Math.Max(420.0, ActualWidth - 32.0);
        var cardWidth = (totalCardWidth - gap) / 2.0;

        DrawCompactSlider(
            dc,
            new Rect(cardLeft, cardTop, cardWidth, 72.0),
            _severity,
            "BLEEDING STRENGTH",
            text,
            muted,
            border,
            accent,
            panel,
            out _severityTrackRect);

        DrawCompactSlider(
            dc,
            new Rect(cardLeft + cardWidth + gap, cardTop, cardWidth, 72.0),
            _recovery,
            "NATURAL RECOVERY",
            text,
            muted,
            border,
            accent,
            panel,
            out _recoveryTrackRect);

        DrawText(dc, "DAMAGE PER BLEEDING TICK", new Point(16, 140), 9.5, muted, FontWeights.Bold);
        DrawText(dc, "drag the curve points", new Point(176, 140), 8.6, muted, FontWeights.Normal);

        _plotRect = new Rect(58.0, 164.0, Math.Max(360.0, ActualWidth - 82.0), 142.0);
        DrawCurve(dc, text, muted, border, softBorder, accent, panel);

        DrawCenteredText(
            dc,
            "LOW  ≤  MEDIUM  ≤  SEVERE",
            new Point(_plotRect.Left + _plotRect.Width / 2.0, 346.0),
            8.7,
            muted,
            FontWeights.SemiBold);
    }

    private void DrawCompactSlider(
        DrawingContext dc,
        Rect rect,
        ParameterViewModel? parameter,
        string label,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel,
        out Rect trackHitRect)
    {
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), rect, 3.0, 3.0);

        DrawText(dc, label, new Point(rect.Left + 10, rect.Top + 8), 9.2, muted, FontWeights.Bold);

        if (parameter is null)
        {
            DrawRightAlignedText(dc, "-", new Point(rect.Right - 10, rect.Top + 7), 10.5, text, FontWeights.Bold);
            trackHitRect = Rect.Empty;
            return;
        }

        DrawRightAlignedText(
            dc,
            parameter.CurrentDisplay,
            new Point(rect.Right - 10, rect.Top + 6),
            10.8,
            accent,
            FontWeights.Bold);

        var trackLeft = rect.Left + 12.0;
        var trackRight = rect.Right - 12.0;
        var trackY = rect.Top + 42.0;
        trackHitRect = new Rect(trackLeft, trackY - 13.0, trackRight - trackLeft, 28.0);

        dc.DrawLine(new Pen(border, 3.0), new Point(trackLeft, trackY), new Point(trackRight, trackY));

        foreach (var marker in parameter.VanillaMarkers)
        {
            var markerX = ValueToX(marker.Value, parameter.Minimum, parameter.Maximum, trackLeft, trackRight);
            dc.DrawLine(new Pen(muted, 1.0), new Point(markerX, trackY - 5), new Point(markerX, trackY + 5));
        }

        var handleX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, trackLeft, trackRight);
        dc.DrawEllipse(accent, null, new Point(handleX, trackY), 5.0, 5.0);

        DrawText(
            dc,
            FormatNumber(parameter.Minimum),
            new Point(trackLeft, rect.Bottom - 16.0),
            8.5,
            muted,
            FontWeights.SemiBold);

        DrawRightAlignedText(
            dc,
            FormatNumber(parameter.Maximum),
            new Point(trackRight, rect.Bottom - 16.0),
            8.5,
            muted,
            FontWeights.SemiBold);
    }

    private void DrawCurve(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush softBorder,
        Brush accent,
        Brush panel)
    {
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), _plotRect, 4.0, 4.0);

        for (var level = 0; level <= 4; level++)
        {
            var y = DamageToY(level);
            var pen = new Pen(level == 0 ? border : softBorder, 1.0);
            if (level > 0)
                pen.DashStyle = DashStyles.Dot;

            dc.DrawLine(pen, new Point(_plotRect.Left, y), new Point(_plotRect.Right, y));
            DrawRightAlignedText(
                dc,
                level.ToString(CultureInfo.InvariantCulture),
                new Point(_plotRect.Left - 9.0, y - 7.0),
                8.5,
                level == 0 ? text : muted,
                FontWeights.SemiBold);
        }

        var x = GetCurveXPositions();

        var vanillaValues = new[]
        {
            GetSharedVanilla(_damage[0], 1.0),
            GetSharedVanilla(_damage[1], 1.5),
            GetSharedVanilla(_damage[2], 2.0)
        };

        var vanillaPoints = new[]
        {
            new Point(x[0], DamageToY(vanillaValues[0])),
            new Point(x[1], DamageToY(vanillaValues[1])),
            new Point(x[2], DamageToY(vanillaValues[2]))
        };

        var vanillaPen = new Pen(muted, 1.3) { DashStyle = DashStyles.Dash };
        DrawPolyline(dc, vanillaPoints, vanillaPen);

        var currentValues = new[]
        {
            _damage[0]?.CurrentNumber ?? vanillaValues[0],
            _damage[1]?.CurrentNumber ?? vanillaValues[1],
            _damage[2]?.CurrentNumber ?? vanillaValues[2]
        };

        var points = new[]
        {
            new Point(x[0], DamageToY(currentValues[0])),
            new Point(x[1], DamageToY(currentValues[1])),
            new Point(x[2], DamageToY(currentValues[2]))
        };

        var fillGeometry = new StreamGeometry();
        using (var context = fillGeometry.Open())
        {
            context.BeginFigure(new Point(points[0].X, _plotRect.Bottom), true, true);
            context.LineTo(points[0], true, false);
            context.LineTo(points[1], true, false);
            context.LineTo(points[2], true, false);
            context.LineTo(new Point(points[2].X, _plotRect.Bottom), true, false);
        }
        fillGeometry.Freeze();

        var fill = accent.Clone();
        fill.Opacity = 0.10;
        dc.DrawGeometry(fill, null, fillGeometry);
        DrawPolyline(dc, points, new Pen(accent, 2.2));

        var labels = new[] { "LOW", "MEDIUM", "SEVERE" };
        for (var i = 0; i < 3; i++)
        {
            var selected = DamageTargetForIndex(i) == _selectedTarget;
            var radius = selected ? 7.0 : 5.5;

            dc.DrawEllipse(panel, new Pen(accent, selected ? 2.2 : 1.5), points[i], radius, radius);
            dc.DrawEllipse(accent, null, points[i], 2.8, 2.8);

            _damageHitRects[i] = new Rect(x[i] - 48.0, _plotRect.Top, 96.0, _plotRect.Height);

            DrawCenteredText(
                dc,
                labels[i],
                new Point(x[i], _plotRect.Bottom + 9.0),
                9.0,
                text,
                FontWeights.Bold);

            DrawCenteredText(
                dc,
                $"{currentValues[i]:0.0}",
                new Point(x[i], _plotRect.Bottom + 25.0),
                9.8,
                accent,
                FontWeights.Bold);
        }

        DrawText(
            dc,
            "VANILLA",
            new Point(_plotRect.Right - 58.0, _plotRect.Top + 7.0),
            8.0,
            muted,
            FontWeights.SemiBold);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var point = e.GetPosition(this);

        if (_severityTrackRect.Contains(point))
        {
            _selectedTarget = DragTarget.Severity;
            _dragTarget = DragTarget.Severity;
            CaptureMouse();
            UpdateCompactSlider(_severity, _severityTrackRect, point.X);
            e.Handled = true;
            return;
        }

        if (_recoveryTrackRect.Contains(point))
        {
            _selectedTarget = DragTarget.Recovery;
            _dragTarget = DragTarget.Recovery;
            CaptureMouse();
            UpdateCompactSlider(_recovery, _recoveryTrackRect, point.X);
            e.Handled = true;
            return;
        }

        var damageIndex = FindDamageColumn(point);
        if (damageIndex < 0)
            return;

        _selectedTarget = DamageTargetForIndex(damageIndex);
        _dragTarget = _selectedTarget;
        CaptureMouse();
        UpdateDamageFromPoint(damageIndex, point.Y);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var point = e.GetPosition(this);
        var interactive =
            _severityTrackRect.Contains(point) ||
            _recoveryTrackRect.Contains(point) ||
            FindDamageColumn(point) >= 0;

        Cursor = interactive ? Cursors.Hand : Cursors.Arrow;

        if (_dragTarget == DragTarget.None || e.LeftButton != MouseButtonState.Pressed)
            return;

        switch (_dragTarget)
        {
            case DragTarget.Severity:
                UpdateCompactSlider(_severity, _severityTrackRect, point.X);
                break;
            case DragTarget.Recovery:
                UpdateCompactSlider(_recovery, _recoveryTrackRect, point.X);
                break;
            case DragTarget.LowDamage:
            case DragTarget.MediumDamage:
            case DragTarget.SevereDamage:
                UpdateDamageFromPoint(DamageIndexForTarget(_dragTarget), point.Y);
                break;
        }

        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_dragTarget == DragTarget.None)
            return;

        _dragTarget = DragTarget.None;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);

        var point = e.GetPosition(this);

        if (_severityTrackRect.Contains(point))
        {
            _severity?.ResetToReference();
            _selectedTarget = DragTarget.Severity;
            e.Handled = true;
            return;
        }

        if (_recoveryTrackRect.Contains(point))
        {
            _recovery?.ResetToReference();
            _selectedTarget = DragTarget.Recovery;
            e.Handled = true;
            return;
        }

        var damageIndex = FindDamageColumn(point);
        if (damageIndex < 0)
            return;

        _damage[damageIndex]?.ResetToReference();
        _selectedTarget = DamageTargetForIndex(damageIndex);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.R)
        {
            foreach (var resetParameter in EnumerateBoundParameters())
                resetParameter?.ResetToReference();

            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_selectedTarget is DragTarget.LowDamage or DragTarget.MediumDamage or DragTarget.SevereDamage)
        {
            if (e.Key == Key.Left)
            {
                var next = Math.Max(0, DamageIndexForTarget(_selectedTarget) - 1);
                _selectedTarget = DamageTargetForIndex(next);
                InvalidateVisual();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Right)
            {
                var next = Math.Min(2, DamageIndexForTarget(_selectedTarget) + 1);
                _selectedTarget = DamageTargetForIndex(next);
                InvalidateVisual();
                e.Handled = true;
                return;
            }
        }

        var direction = e.Key switch
        {
            Key.Up or Key.Add or Key.OemPlus => 1,
            Key.Down or Key.Subtract or Key.OemMinus => -1,
            _ => 0
        };

        if (direction == 0)
            return;

        var parameter = ParameterForTarget(_selectedTarget);
        if (parameter is null)
            return;

        parameter.CurrentNumber = Math.Clamp(
            parameter.CurrentNumber + direction * parameter.TickFrequency,
            parameter.Minimum,
            parameter.Maximum);

        InvalidateVisual();
        e.Handled = true;
    }

    private void UpdateCompactSlider(ParameterViewModel? parameter, Rect hitRect, double x)
    {
        if (parameter is null || hitRect.IsEmpty)
            return;

        var ratio = Math.Clamp((x - hitRect.Left) / Math.Max(1.0, hitRect.Width), 0.0, 1.0);
        var raw = parameter.Minimum + ratio * (parameter.Maximum - parameter.Minimum);
        parameter.CurrentNumber = Snap(parameter, raw);
        InvalidateVisual();
    }

    private void UpdateDamageFromPoint(int index, double y)
    {
        if (index is < 0 or > 2 || _damage[index] is not { } parameter)
            return;

        var ratio = Math.Clamp((_plotRect.Bottom - y) / Math.Max(1.0, _plotRect.Height), 0.0, 1.0);
        var raw = ratio * CurveMaximumDamage;
        parameter.CurrentNumber = Snap(parameter, Math.Clamp(raw, parameter.Minimum, parameter.Maximum));
        InvalidateVisual();
    }

    private static double Snap(ParameterViewModel parameter, double raw)
    {
        var step = Math.Max(0.0000001, parameter.TickFrequency);
        var snapped = parameter.Minimum +
                      Math.Round((raw - parameter.Minimum) / step, MidpointRounding.AwayFromZero) * step;
        return Math.Clamp(snapped, parameter.Minimum, parameter.Maximum);
    }

    private int FindDamageColumn(Point point)
    {
        if (!_plotRect.Contains(point))
            return -1;

        for (var i = 0; i < _damageHitRects.Length; i++)
        {
            if (_damageHitRects[i].Contains(point))
                return i;
        }

        var x = GetCurveXPositions();
        var nearest = 0;
        var nearestDistance = Math.Abs(point.X - x[0]);
        for (var i = 1; i < x.Length; i++)
        {
            var distance = Math.Abs(point.X - x[i]);
            if (distance < nearestDistance)
            {
                nearest = i;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private ParameterViewModel? ParameterForTarget(DragTarget target)
        => target switch
        {
            DragTarget.Severity => _severity,
            DragTarget.Recovery => _recovery,
            DragTarget.LowDamage => _damage[0],
            DragTarget.MediumDamage => _damage[1],
            DragTarget.SevereDamage => _damage[2],
            _ => null
        };

    private static DragTarget DamageTargetForIndex(int index)
        => index switch
        {
            0 => DragTarget.LowDamage,
            1 => DragTarget.MediumDamage,
            _ => DragTarget.SevereDamage
        };

    private static int DamageIndexForTarget(DragTarget target)
        => target switch
        {
            DragTarget.LowDamage => 0,
            DragTarget.MediumDamage => 1,
            DragTarget.SevereDamage => 2,
            _ => -1
        };

    private double[] GetCurveXPositions()
        => new[]
        {
            _plotRect.Left + _plotRect.Width * 0.14,
            _plotRect.Left + _plotRect.Width * 0.50,
            _plotRect.Left + _plotRect.Width * 0.86
        };

    private double DamageToY(double damage)
    {
        var normalized = Math.Clamp(damage / CurveMaximumDamage, 0.0, 1.0);
        return _plotRect.Bottom - normalized * _plotRect.Height;
    }

    private static double GetSharedVanilla(ParameterViewModel? parameter, double fallback)
    {
        if (parameter is null)
            return fallback;

        var marker = parameter.VanillaMarkers.FirstOrDefault(x => x.Label == "VANILLA");
        return marker?.Value ?? parameter.VanillaMarkers.FirstOrDefault()?.Value ?? fallback;
    }

    private static double ValueToX(double value, double minimum, double maximum, double left, double right)
    {
        if (maximum <= minimum)
            return left;

        var ratio = Math.Clamp((value - minimum) / (maximum - minimum), 0.0, 1.0);
        return left + (right - left) * ratio;
    }

    private static void DrawPolyline(DrawingContext dc, IReadOnlyList<Point> points, Pen pen)
    {
        for (var i = 1; i < points.Count; i++)
            dc.DrawLine(pen, points[i - 1], points[i]);
    }

    private Brush FindBrush(string key, Brush fallback)
        => TryFindResource(key) as Brush ?? fallback;

    private FormattedText CreateText(string value, double size, Brush brush, FontWeight weight)
        => new(
            value,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            Math.Max(9.0, size),
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private void DrawText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
        => dc.DrawText(CreateText(value, size, brush, weight), point);

    private void DrawRightAlignedText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
    {
        var ft = CreateText(value, size, brush, weight);
        dc.DrawText(ft, new Point(point.X - ft.WidthIncludingTrailingWhitespace, point.Y));
    }

    private void DrawCenteredText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
    {
        var ft = CreateText(value, size, brush, weight);
        dc.DrawText(ft, new Point(point.X - ft.WidthIncludingTrailingWhitespace / 2.0, point.Y));
    }

    private static string FormatNumber(double value)
        => value.ToString(Math.Abs(value) < 1.0 ? "0.##" : "0.#", CultureInfo.InvariantCulture);

    private enum DragTarget
    {
        None,
        Severity,
        Recovery,
        LowDamage,
        MediumDamage,
        SevereDamage
    }
}
