using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Compact Bleeding System editor for the seven source-native bleeding controls.
/// Stage cards only display thresholds and damage; they are not selectors.
/// </summary>
public sealed class BleedingCurveControl : FrameworkElement
{
    private const string SeverityId = "survival.bleedingSeverity";
    private const string RecoveryId = "survival.bleedingRecovery";
    private const string LowDamageId = "survival.bleedingLowDamage";
    private const string MediumDamageId = "survival.bleedingMediumDamage";
    private const string SevereDamageId = "survival.bleedingStrongDamage";
    private const string MediumThresholdId = "survival.bleedingMediumThreshold";
    private const string SevereThresholdId = "survival.bleedingStrongThreshold";

    private const double CurveMaximumDamage = 4.0;
    private const double LowThreshold = 0.1;
    private const double DesiredHeight = 466.0;

    private ParameterViewModel? _severity;
    private ParameterViewModel? _recovery;
    private readonly ParameterViewModel?[] _damage = new ParameterViewModel?[3];
    private ParameterViewModel? _mediumThreshold;
    private ParameterViewModel? _severeThreshold;
    // Health from another tab is a passive comparison, not part of bleeding reset.
    private ParameterViewModel? _maxHealth;

    private Rect _severityTrackRect;
    private Rect _recoveryTrackRect;
    private Rect _plotRect;
    private readonly Rect[] _damageHitRects = new Rect[3];
    private Rect _mediumThresholdHitRect;
    private Rect _severeThresholdHitRect;

    private readonly List<(Rect Hit, double Value, ParameterViewModel Parameter)> _difficultyMarkerHits = new();
    private double? _hoverDifficultyValue;
    private DragTarget _hoverTarget = DragTarget.None;
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
        MinHeight = DesiredHeight;
        SnapsToDevicePixels = true;
        Cursor = Cursors.Arrow;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 760.0 : Math.Max(520.0, availableSize.Width);
        return new Size(width, DesiredHeight);
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
        if (_maxHealth is not null)
            _maxHealth.PropertyChanged -= Parameter_PropertyChanged;
        _maxHealth = null;

        _severity = null;
        _recovery = null;
        _mediumThreshold = null;
        _severeThreshold = null;
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

            byId.TryGetValue(MediumThresholdId, out _mediumThreshold);
            byId.TryGetValue(SevereThresholdId, out _severeThreshold);
            byId.TryGetValue("health.maxHP", out _maxHealth);

            foreach (var parameter in EnumerateBoundParameters())
            {
                if (parameter is not null)
                    parameter.PropertyChanged += Parameter_PropertyChanged;
            }
            if (_maxHealth is not null)
                _maxHealth.PropertyChanged += Parameter_PropertyChanged;
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
        yield return _mediumThreshold;
        yield return _severeThreshold;
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

        DrawCenteredText(dc, "BLEEDING SYSTEM", new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);
        DrawText(dc, "GLOBAL EFFECTS", new Point(16, 34), 8.8, muted, FontWeights.Bold);

        var gap = 10.0;
        var cardLeft = 16.0;
        var cardTop = 51.0;
        var totalCardWidth = Math.Max(420.0, ActualWidth - 32.0);
        var cardWidth = (totalCardWidth - gap) / 2.0;

        _difficultyMarkerHits.Clear();
        DrawCompactSlider(
            dc,
            new Rect(cardLeft, cardTop, cardWidth, 72.0),
            _severity,
            "BLEEDING SEVERITY",
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

        DrawText(dc, "BLEEDING DAMAGE PROFILE", new Point(16, 142),
            11.4, accent, FontWeights.Bold);
        DrawCenteredText(dc, "BLEEDING POINTS  /  0-100",
            new Point(ActualWidth / 2.0 + 14, 160), 10.2, text, FontWeights.Bold);
        // Rotate the Y-axis caption like the other profile charts; keep
        // the vertical tick numerals and plot bounds where they are.
        var hpTickLabel = CreateText("HP / TICK", 10.2, text, FontWeights.Bold);
        dc.PushTransform(new RotateTransform(-90.0, 16.0, 277.5));
        dc.DrawText(hpTickLabel,
            new Point(16.0 - hpTickLabel.WidthIncludingTrailingWhitespace / 2.0, 272.4));
        dc.Pop();

        _plotRect = new Rect(64.0, 185.0, Math.Max(360.0, ActualWidth - 89.0), 185.0);
        DrawCurve(dc, text, muted, border, softBorder, accent, panel);
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

        DrawText(dc, label, new Point(rect.Left + 10, rect.Top + 8),
            10.8, text, FontWeights.Bold);

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
            parameter.IsValueModifiedFromReference() ? 12.0 : 10.8,
            parameter.IsValueModifiedFromReference() ? accent : text,
            FontWeights.Bold);

        var trackLeft = rect.Left + 12.0;
        var trackRight = rect.Right - 12.0;
        var trackY = rect.Top + 42.0;
        trackHitRect = new Rect(trackLeft, trackY - 13.0, trackRight - trackLeft, 28.0);

        dc.DrawLine(new Pen(border, 3.0), new Point(trackLeft, trackY), new Point(trackRight, trackY));

        // Difficulty markers are meaningful only where the source actually differs
        // between vanilla difficulties (Bleeding Strength: N / S+V / M).
        var difficultyMarkers = parameter.Id == "survival.bleedingSeverity"
            ? parameter.VanillaMarkers
            : Array.Empty<VanillaMarkerViewModel>();
        foreach (var marker in parameter.VanillaMarkers)
        {
            var markerX = ValueToX(marker.Value, parameter.Minimum, parameter.Maximum, trackLeft, trackRight);
            dc.DrawLine(new Pen(muted, 1.0), new Point(markerX, trackY - 5), new Point(markerX, trackY + 5));
            if (difficultyMarkers.Count > 1)
            {
                DrawCenteredText(dc, marker.Label, new Point(markerX, trackY + 10.0),
                    8.3,
                    parameter.IsControlEnabled && _hoverDifficultyValue == marker.Value
                        ? Brushes.White : muted, FontWeights.SemiBold);
                if (parameter.IsControlEnabled)
                    _difficultyMarkerHits.Add((
                        new Rect(markerX - 21.0, trackY + 8.0, 42.0, 16.0),
                        marker.Value, parameter));
            }
        }

        var handleX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, trackLeft, trackRight);
        ControlHoverGlow.Draw(dc, new Point(handleX, trackY), accent,
            _hoverTarget == (parameter.Id == SeverityId ? DragTarget.Severity : DragTarget.Recovery));
        dc.DrawEllipse(accent, null, new Point(handleX, trackY), 5.0, 5.0);

        // On the difficulty-specific rail, reserve the lower lane for N, S/V, M
        // instead of colliding with the minimum and maximum captions.
        if (difficultyMarkers.Count <= 1)
        {
            DrawText(dc, FormatNumber(parameter.Minimum),
                new Point(trackLeft, rect.Bottom - 16.0), 8.5, muted, FontWeights.SemiBold);
            DrawRightAlignedText(dc, FormatNumber(parameter.Maximum),
                new Point(trackRight, rect.Bottom - 16.0), 8.5, muted, FontWeights.SemiBold);
        }
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

        // Graduated risk bands distinguish mild, moderate and heavy bleeding.
        // These colors are purely visual; authored CFG thresholds stay intact.
        var mediumStart = ThresholdToX(_mediumThreshold?.CurrentNumber ?? 50.0);
        var severeStart = ThresholdToX(_severeThreshold?.CurrentNumber ?? 75.0);
        void Band(double from, double to, double opacity)
        {
            if (to <= from) return;
            dc.DrawRectangle(CloneWithOpacity(accent, opacity), null,
                new Rect(from, _plotRect.Top + 1, to - from, _plotRect.Height - 2));
        }
        Band(_plotRect.Left + 1, mediumStart, .025);
        Band(mediumStart, severeStart, .055);
        Band(severeStart, _plotRect.Right - 1, .095);

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
                10.0,
                level == 0 ? text : muted,
                FontWeights.SemiBold);
        }

        for (var threshold = 0; threshold <= 100; threshold += 25)
        {
            var gridX = ThresholdToX(threshold);
            dc.DrawLine(
                new Pen(CloneWithOpacity(softBorder, threshold is 0 or 100 ? 0.8 : 0.45), 1.0)
                {
                    DashStyle = threshold is 0 or 100 ? DashStyles.Solid : DashStyles.Dot
                },
                new Point(gridX, _plotRect.Top),
                new Point(gridX, _plotRect.Bottom));
        }

        var currentThresholds = new[]
        {
            LowThreshold,
            _mediumThreshold?.CurrentNumber ?? 50.0,
            _severeThreshold?.CurrentNumber ?? 75.0
        };
        var referenceThresholds = new[]
        {
            LowThreshold,
            _mediumThreshold?.ReferenceNumber ?? 50.0,
            _severeThreshold?.ReferenceNumber ?? 75.0
        };

        for (var threshold = 0; threshold <= 100; threshold += 25)
            DrawCenteredText(dc, $"{threshold}", new Point(ThresholdToX(threshold),
                _plotRect.Bottom + 13), 10.0, muted, FontWeights.SemiBold);

        var x = currentThresholds.Select(ThresholdToX).ToArray();
        var vanillaX = referenceThresholds.Select(ThresholdToX).ToArray();

        var vanillaValues = new[]
        {
            GetSharedVanilla(_damage[0], 1.0),
            GetSharedVanilla(_damage[1], 1.5),
            GetSharedVanilla(_damage[2], 2.0)
        };

        var vanillaPoints = new[]
        {
            new Point(vanillaX[0], DamageToY(vanillaValues[0])),
            new Point(vanillaX[1], DamageToY(vanillaValues[1])),
            new Point(vanillaX[2], DamageToY(vanillaValues[2]))
        };

        // BLEEDING uses authored LOW / MEDIUM / SEVERE steps. A sloped
        // interpolating line suggests an effect that does not exist.
        void DrawStageCurve(Point[] values, Pen pen)
        {
            for (var i = 0; i < values.Length - 1; i++)
            {
                dc.DrawLine(pen, values[i],
                    new Point(values[i + 1].X, values[i].Y));
                dc.DrawLine(pen, new Point(values[i + 1].X, values[i].Y),
                    values[i + 1]);
            }
            dc.DrawLine(pen, values[^1],
                new Point(_plotRect.Right, values[^1].Y));
        }

        DrawStageCurve(vanillaPoints,
            new Pen(muted, 1.6) { DashStyle = DashStyles.Dash });

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
            for (var i = 1; i < points.Length; i++)
            {
                context.LineTo(new Point(points[i].X, points[i - 1].Y), true, false);
                context.LineTo(points[i], true, false);
            }
            context.LineTo(new Point(_plotRect.Right, points[^1].Y), true, false);
            context.LineTo(new Point(_plotRect.Right, _plotRect.Bottom), true, false);
        }
        fillGeometry.Freeze();

        var fill = accent.Clone();
        fill.Opacity = 0.15;
        dc.DrawGeometry(fill, null, fillGeometry);
        // Project each live stage point to the baseline to make its location
        // and damage independently readable at a glance.
        for (var i = 0; i < points.Length; i++)
            dc.DrawLine(new Pen(CloneWithOpacity(accent, .42), 1.1)
                    { DashStyle = DashStyles.Dot },
                new Point(points[i].X, points[i].Y),
                new Point(points[i].X, _plotRect.Bottom));
        DrawStageCurve(points, new Pen(accent, 2.8));

        var labels = new[] { "LOW", "MEDIUM", "SEVERE" };
        const double stageGap = 8.0;
        var stageBankWidth = Math.Min(618.0, Math.Max(1.0, ActualWidth - 32.0));
        var stageCardWidth = (stageBankWidth - 2.0 * stageGap) / 3.0;
        var stageBankLeft = (ActualWidth - stageBankWidth) / 2.0;
        for (var i = 0; i < 3; i++)
        {
            var selected = DamageTargetForIndex(i) == _selectedTarget;
            var radius = selected ? 7.0 : 5.5;

            ControlHoverGlow.Draw(dc, points[i], accent,
                _hoverTarget == DamageTargetForIndex(i));
            dc.DrawEllipse(panel, new Pen(accent, selected ? 2.2 : 1.5), points[i], radius, radius);
            dc.DrawEllipse(accent, null, points[i], 2.8, 2.8);

            _damageHitRects[i] = new Rect(x[i] - 38.0, _plotRect.Top, 76.0, _plotRect.Height - 10.0);

            var thresholdParameter = i switch
            {
                1 => _mediumThreshold,
                2 => _severeThreshold,
                _ => null
            };

            if (thresholdParameter is not null)
            {
                dc.DrawLine(new Pen(muted, 1.0), new Point(vanillaX[i], _plotRect.Bottom - 6.0), new Point(vanillaX[i], _plotRect.Bottom + 8.0));
                dc.DrawLine(new Pen(accent, IsSelectedThreshold(i) ? 2.8 : 2.0), new Point(x[i], _plotRect.Bottom - 8.0), new Point(x[i], _plotRect.Bottom + 9.0));
                ControlHoverGlow.Draw(dc, new Point(x[i], _plotRect.Bottom + 3.0), accent,
                    _hoverTarget == (i == 1 ? DragTarget.MediumThreshold : DragTarget.SevereThreshold));
                dc.DrawEllipse(panel, new Pen(accent, IsSelectedThreshold(i) ? 2.4 : 1.5), new Point(x[i], _plotRect.Bottom + 3.0), 5.5, 5.5);
            }

            // Read-only stage values. Only the original graph handles edit
            // HP/tick or Medium/Severe thresholds; cards have no hit targets.
            var card = new Rect(stageBankLeft + i * (stageCardWidth + stageGap),
                405.0, stageCardWidth, 44.0);
            dc.DrawRoundedRectangle(panel, new Pen(border, 1.0),
                card, 3.0, 3.0);
            DrawSourceNumber(dc,
                $"{labels[i]} AT {currentThresholds[i]:0.#} PTS",
                new Point(card.Left + card.Width / 2, card.Top + 5),
                10.1, text, accent, FontWeights.Bold,
                (i == 1 ? _mediumThreshold : i == 2 ? _severeThreshold : null)
                    ?.IsValueModifiedFromReference() == true);
            var pool = Math.Max(1.0, _maxHealth?.CurrentNumber ?? 100.0);
            DrawSourceNumber(dc,
                $"{currentValues[i]:0.##} HP/TICK · {100.0 * currentValues[i] / pool:0.#}% MAX HP",
                new Point(card.Left + card.Width / 2, card.Top + 25),
                9.0, text, accent, FontWeights.Bold,
                _damage[i]?.IsValueModifiedFromReference() == true,
                highlightDerivedLastNumber: true);
        }

        _mediumThresholdHitRect = new Rect(x[1] - 20.0, _plotRect.Bottom - 10.0, 40.0, 34.0);
        _severeThresholdHitRect = new Rect(x[2] - 20.0, _plotRect.Bottom - 10.0, 40.0, 34.0);

    }


    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var point = e.GetPosition(this);
        var marker = _difficultyMarkerHits.FirstOrDefault(m => m.Hit.Contains(point));
        if (marker.Parameter?.IsControlEnabled == true)
        {
            _selectedTarget = DragTarget.Severity;
            marker.Parameter.CurrentNumber = Snap(marker.Parameter, marker.Value);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

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

        if (_mediumThresholdHitRect.Contains(point))
        {
            _selectedTarget = DragTarget.MediumThreshold;
            _dragTarget = DragTarget.MediumThreshold;
            CaptureMouse();
            UpdateThresholdFromPoint(_mediumThreshold, point.X);
            e.Handled = true;
            return;
        }

        if (_severeThresholdHitRect.Contains(point))
        {
            _selectedTarget = DragTarget.SevereThreshold;
            _dragTarget = DragTarget.SevereThreshold;
            CaptureMouse();
            UpdateThresholdFromPoint(_severeThreshold, point.X);
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
        var marker = _difficultyMarkerHits.FirstOrDefault(m => m.Hit.Contains(point));
        var nextMarker = marker.Parameter?.IsControlEnabled == true
            ? (double?)marker.Value : null;
        if (_hoverDifficultyValue != nextMarker)
        {
            _hoverDifficultyValue = nextMarker;
            InvalidateVisual();
        }
        if (nextMarker is not null && _dragTarget == DragTarget.None)
        {
            Cursor = Cursors.Hand;
            return;
        }
        var interactive =
            _severityTrackRect.Contains(point) ||
            _recoveryTrackRect.Contains(point) ||
            _mediumThresholdHitRect.Contains(point) ||
            _severeThresholdHitRect.Contains(point) ||
            FindDamageColumn(point) >= 0;

        var hovered = _severityTrackRect.Contains(point) ? DragTarget.Severity
            : _recoveryTrackRect.Contains(point) ? DragTarget.Recovery
            : _mediumThresholdHitRect.Contains(point) ? DragTarget.MediumThreshold
            : _severeThresholdHitRect.Contains(point) ? DragTarget.SevereThreshold
            : FindDamageColumn(point) is var row && row >= 0
                ? DamageTargetForIndex(row) : DragTarget.None;
        if (_hoverTarget != hovered)
        {
            _hoverTarget = hovered;
            InvalidateVisual();
        }
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
            case DragTarget.MediumThreshold:
                UpdateThresholdFromPoint(_mediumThreshold, point.X);
                break;
            case DragTarget.SevereThreshold:
                UpdateThresholdFromPoint(_severeThreshold, point.X);
                break;
        }

        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverTarget = DragTarget.None;
        _hoverDifficultyValue = null;
        Cursor = Cursors.Arrow;
        InvalidateVisual();
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

        if (_mediumThresholdHitRect.Contains(point))
        {
            _mediumThreshold?.ResetToReference();
            _selectedTarget = DragTarget.MediumThreshold;
            EnforceThresholdOrder(_mediumThreshold);
            e.Handled = true;
            return;
        }

        if (_severeThresholdHitRect.Contains(point))
        {
            _severeThreshold?.ResetToReference();
            _selectedTarget = DragTarget.SevereThreshold;
            EnforceThresholdOrder(_severeThreshold);
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

        var thresholdSelected = _selectedTarget is DragTarget.MediumThreshold or DragTarget.SevereThreshold;
        var direction = e.Key switch
        {
            Key.Up or Key.Add or Key.OemPlus => 1,
            Key.Down or Key.Subtract or Key.OemMinus => -1,
            Key.Right when thresholdSelected => 1,
            Key.Left when thresholdSelected => -1,
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
        EnforceThresholdOrder(parameter);

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

    private void UpdateThresholdFromPoint(ParameterViewModel? parameter, double x)
    {
        if (parameter is null)
            return;

        var innerLeft = _plotRect.Left + 12.0;
        var innerRight = _plotRect.Right - 12.0;
        var ratio = Math.Clamp((x - innerLeft) / Math.Max(1.0, innerRight - innerLeft), 0.0, 1.0);
        var raw = parameter.Minimum + ratio * (parameter.Maximum - parameter.Minimum);
        parameter.CurrentNumber = Snap(parameter, raw);
        EnforceThresholdOrder(parameter);
        InvalidateVisual();
    }

    private void EnforceThresholdOrder(ParameterViewModel? moved)
    {
        if (_mediumThreshold is null || _severeThreshold is null || moved is null)
            return;

        if (ReferenceEquals(moved, _mediumThreshold) && _mediumThreshold.CurrentNumber > _severeThreshold.CurrentNumber)
            _severeThreshold.CurrentNumber = _mediumThreshold.CurrentNumber;
        else if (ReferenceEquals(moved, _severeThreshold) && _severeThreshold.CurrentNumber < _mediumThreshold.CurrentNumber)
            _mediumThreshold.CurrentNumber = _severeThreshold.CurrentNumber;
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
            DragTarget.MediumThreshold => _mediumThreshold,
            DragTarget.SevereThreshold => _severeThreshold,
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
            ThresholdToX(LowThreshold),
            ThresholdToX(_mediumThreshold?.CurrentNumber ?? 50.0),
            ThresholdToX(_severeThreshold?.CurrentNumber ?? 75.0)
        };

    private double ThresholdToX(double threshold)
    {
        var left = _plotRect.Left + 12.0;
        var right = _plotRect.Right - 12.0;
        return left + (right - left) * Math.Clamp(threshold / 100.0, 0.0, 1.0);
    }

    private bool IsSelectedThreshold(int index)
        => index switch
        {
            1 => _selectedTarget == DragTarget.MediumThreshold,
            2 => _selectedTarget == DragTarget.SevereThreshold,
            _ => false
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

    private static Brush CloneWithOpacity(Brush brush, double opacity)
    {
        var clone = brush.Clone();
        clone.Opacity = Math.Clamp(opacity, 0.0, 1.0);
        return clone;
    }

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

    private void DrawSourceNumber(DrawingContext dc, string value, Point point,
        double size, Brush text, Brush accent, FontWeight weight, bool modified,
        bool highlightDerivedLastNumber = false)
    {
        var ft = CreateText(value, size, text, weight);
        if (modified)
        {
            var number = System.Text.RegularExpressions.Regex.Match(
                value, @"[+\-−]?\d+(?:\.\d+)?(?:[%x×])?");
            if (number.Success)
            {
                ft.SetForegroundBrush(accent, number.Index, number.Length);
                ft.SetFontSize(Math.Max(9.0, size) + 1.2,
                    number.Index, number.Length);
            }
        }
        if (highlightDerivedLastNumber)
        {
            var numbers = System.Text.RegularExpressions.Regex.Matches(value,
                @"[+\-−]?\d+(?:\.\d+)?(?:[%x×])?");
            if (numbers.Count > 0)
            {
                var derived = numbers[numbers.Count - 1];
                ft.SetForegroundBrush(accent, derived.Index, derived.Length);
            }
        }
        dc.DrawText(ft, new Point(point.X - ft.WidthIncludingTrailingWhitespace / 2.0,
            point.Y));
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
        SevereDamage,
        MediumThreshold,
        SevereThreshold
    }
}
