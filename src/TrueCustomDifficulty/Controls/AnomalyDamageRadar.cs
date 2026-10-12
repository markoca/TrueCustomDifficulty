using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

public sealed class AnomalyDamageRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double DesiredHeight = 606.0;
    private const double KnobStartAngle = 135.0;
    private const double KnobSweepAngle = 270.0;
    private const double KnobRadius = 47.0;
    private const double MaxContentWidth = 756.0;
    private const double ColumnGap = 16.0;
    private const double RowHeight = 52.0;

    private static readonly AnomalyDefinition[] Anomalies =
    {
        new("environment.electroAnomalyDamage", "ELECTRO", false),
        new("environment.chemicalAnomalyDamage", "CHEMICAL", false),
        new("environment.fireAnomalyDamage", "FIRE", false),
        new("environment.carouselAnomalyDamage", "CAROUSEL", false),
        new("environment.toxicCloudDamage", "TOXIC CLOUD", false),
        new("environment.steamAnomalyDamage", "STEAM", false),
        new("environment.clickerHealthDamage", "CLICKER", false),
        new("environment.razorHealthDamage", "RAZOR", true),
        new("environment.soapBubbleHealthDamage", "SOAP BUBBLE", false),
        new("environment.fireballHealthDamage", "FIREBALL", false),
        new("environment.diamondHealthDamage", "DIAMOND", false),
    };

    // Each radar row edits its own parameter and selects that parameter's
    // precise rationale, not the first Electro aggregate anchor.
    public event EventHandler<AggregateParameterSelectedEventArgs>? ParameterSelected;

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Anomalies.Length];
    private ParameterViewModel? _globalDamage;
    private Rect _globalKnobHit = Rect.Empty;
    private bool _hoverGlobalKnob;
    private bool _dragGlobalKnob;
    private bool _selectedGlobalKnob;
    private readonly Dictionary<int, HitTarget> _targets = new();
    private readonly VisualStatusMessage _status;
    private int _selectedRow;
    private int _hoverRow = -1;
    private int _dragRow = -1;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(AnomalyDamageRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public AnomalyDamageRadar()
    {
        _status = new VisualStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = DesiredHeight;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 780.0 : Math.Max(580.0, availableSize.Width);
        return new Size(width, DesiredHeight);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AnomalyDamageRadar)d).RebindParameters();

    private void RebindParameters()
    {
        foreach (var parameter in _parameters)
        {
            if (parameter is not null)
                parameter.PropertyChanged -= Parameter_PropertyChanged;
        }
        if (_globalDamage is not null)
            _globalDamage.PropertyChanged -= Parameter_PropertyChanged;
        _globalDamage = null;

        Array.Clear(_parameters, 0, _parameters.Length);

        if (Parameters is not null)
        {
            var byId = Parameters.OfType<ParameterViewModel>().ToDictionary(x => x.Id, StringComparer.Ordinal);
            if (byId.TryGetValue("environment.anomalyDamage", out var global))
            {
                _globalDamage = global;
                global.PropertyChanged += Parameter_PropertyChanged;
            }
            for (var i = 0; i < Anomalies.Length; i++)
            {
                if (!byId.TryGetValue(Anomalies[i].ParameterId, out var parameter))
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
            or nameof(ParameterViewModel.EffectiveDisplay)
            or nameof(ParameterViewModel.EffectiveDamageValue)
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

        DrawCenteredText(dc, "ANOMALY DAMAGE",
            new Point(ActualWidth / 2.0, 21.0), 12.5, accent, FontWeights.Bold);

        // One shared frame for GLOBAL and all eleven native source sliders.
        // Global remains an independent 0..2.5x/0.01x difficulty source.
        DrawGlobalDamageKnob(dc, text, muted, border, accent, panel);
        // Gamer-first presentation: the global dial states its scope, while the
        // individual columns label their own final and editable base values.
        // No footer formula or separate technical instruction block.
        DrawCenteredText(dc, "ALL ANOMALIES", new Point(ActualWidth / 2.0, 248.0),
            11.0, text, FontWeights.Bold);
        DrawCenteredText(dc, "Scales damage across every anomaly",
            new Point(ActualWidth / 2.0, 269.0), 10.0, muted, FontWeights.Medium);
        DrawCenteredText(dc, "INDIVIDUAL DAMAGE  ·  BARS VS VANILLA",
            new Point(ActualWidth / 2.0, 296.0), 9.8, muted, FontWeights.SemiBold);

        // Individual handles edit source CFG values, not global difficulty
        // or the computed RESULT readout. Their 0..200% axis is *relative to
        // each source's vanilla*, never 0..200 hit points or global damage.
        const double top = 365.0, bottom = 526.0;
        const int columns = 11;
        var left = 53.0;
        var lane = (ActualWidth - left - 14.0) / columns;
        for (var tick = 0; tick <= 4; tick++)
        {
            var yTick = bottom - (bottom - top) * tick / 4.0;
            dc.DrawLine(new Pen(CloneWithOpacity(border, .40), .8),
                new Point(left, yTick), new Point(ActualWidth - 14.0, yTick));
            DrawRightAlignedText(dc, $"{tick * 50}%",
                new Point(left - 7.0, yTick - 6.0), 9.0, muted, FontWeights.SemiBold);
        }
        var axis = new Point(11.0, (top + bottom) / 2);
        dc.PushTransform(new RotateTransform(-90, axis.X, axis.Y));
        DrawCenteredText(dc, "% VANILLA", axis, 9.0, accent, FontWeights.Bold);
        dc.Pop();

        for (var i = 0; i < Anomalies.Length; i++)
        {
            if (_parameters[i] is not { } parameter) continue;
            var center = left + (i + .5) * lane;
            var reference = parameter.ReferenceNumber;
            var fraction = reference > Epsilon
                ? parameter.CurrentNumber / (2.0 * reference) : 0.0;
            var currentY = bottom - (bottom - top) * Math.Clamp(fraction, 0, 1);
            var referenceY = bottom - (bottom - top) * .5;
            var width = Math.Min(23.0, lane * .42);
            var barLeft = center - width / 2.0;
            dc.DrawRoundedRectangle(CloneWithOpacity(panel, .32),
                new Pen(CloneWithOpacity(border, .55), .8),
                new Rect(left + i * lane + 2, 306, lane - 4, 279), 4, 4);
            dc.DrawRectangle(CloneWithOpacity(border, .12), null,
                new Rect(barLeft, top, width, bottom - top));
            dc.DrawRectangle(CloneWithOpacity(accent, .72), null,
                new Rect(barLeft, currentY, width, Math.Max(.5, bottom - currentY)));
            dc.DrawLine(new Pen(CloneWithOpacity(muted, .68), 1.3),
                new Point(barLeft - 3, referenceY), new Point(barLeft + width + 3, referenceY));
            VerticalBarHandleChrome.Draw(dc, new Point(center, currentY),
                width, accent, _hoverRow == i);
            // Live combined effect: individual CFG strength times the global
            // Anomaly Damage difficulty multiplier. Keep the native value
            // visible in muted text because dragging still edits that source.
            var inherited = parameter.EffectiveDamageValue;
            var effectiveText = Anomalies[i].IsMultiplier
                ? $"{inherited:0.##}x" : $"{inherited:0.#}";
            DrawCenteredText(dc, "FINAL", new Point(center, 314), 7.4,
                muted, FontWeights.SemiBold);
            DrawCenteredText(dc, effectiveText, new Point(center, 326), 10.2,
                parameter.IsValueModifiedFromReference()
                    || _globalDamage?.IsValueModifiedFromReference() == true ? accent : text,
                FontWeights.Bold);
            var nativeText = Anomalies[i].IsMultiplier
                ? $"{parameter.CurrentNumber:0.##}x" : $"{parameter.CurrentNumber:0.#}";
            DrawCenteredText(dc, "BASE", new Point(center, 341), 7.4,
                muted, FontWeights.SemiBold);
            DrawCenteredText(dc, nativeText, new Point(center, 353), 9.2,
                parameter.IsValueModifiedFromReference() ? accent : muted,
                FontWeights.SemiBold);
            var words = Anomalies[i].Label.Split(' ');
            if (words.Length == 1)
                DrawCenteredText(dc, words[0], new Point(center, 550), 8.5,
                    text, FontWeights.Bold);
            else
            {
                DrawCenteredText(dc, words[0], new Point(center, 543), 8.5,
                    text, FontWeights.Bold);
                DrawCenteredText(dc, string.Join(" ", words.Skip(1)),
                    new Point(center, 559), 8.5, text, FontWeights.Bold);
            }
            _targets[i] = new HitTarget(
                new Rect(center - Math.Max(15, width / 2 + 5), top,
                    Math.Max(30, width + 10), bottom - top),
                new Rect(barLeft, top, width, bottom - top));
        }
    }

    private static Point KnobPoint(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180.0;
        return new Point(center.X + radius * Math.Cos(radians),
            center.Y + radius * Math.Sin(radians));
    }

    private static void DrawKnobArc(DrawingContext dc, Point center,
        double radius, double sweep, Pen pen)
    {
        if (sweep <= Epsilon)
            return;
        var figure = new PathFigure
        {
            StartPoint = KnobPoint(center, radius, KnobStartAngle),
            IsClosed = false,
            IsFilled = false
        };
        figure.Segments.Add(new ArcSegment(
            KnobPoint(center, radius, KnobStartAngle + sweep),
            new Size(radius, radius), 0.0, sweep > 180.0,
            SweepDirection.Clockwise, true));
        dc.DrawGeometry(null, pen, new PathGeometry(new[] { figure }));
    }

    // Native rotary marks, selected-difficulty Base marker and hit region.
    // Dense 0.01x inputs retain all steps; visible marks use a readable
    // subset of *real* source increments, not arbitrary graduations.
    private void DrawGlobalDamageKnob(DrawingContext dc, Brush text, Brush muted,
        Brush border, Brush accent, Brush panel)
    {
        _globalKnobHit = Rect.Empty;
        var p = _globalDamage;
        if (p is null)
            return;
        const double top = 47.0;
        var card = new Rect((ActualWidth - 194.0) / 2.0, top, 194.0, 181.0);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .40),
            new Pen(CloneWithOpacity(border, .7), 1.0), card, 4.0, 4.0);
        DrawCenteredText(dc, "GLOBAL MULTIPLIER", new Point(card.Left + 97.0, 65.0),
            10.0, text, FontWeights.Bold);

        var center = new Point(card.Left + 97.0, card.Top + 97.0);
        var range = Math.Max(Epsilon, p.Maximum - p.Minimum);
        var ratio = Math.Clamp((p.CurrentNumber - p.Minimum) / range, 0.0, 1.0);
        var reference = Math.Clamp((p.ReferenceNumber - p.Minimum) / range, 0.0, 1.0);
        var active = p.IsControlEnabled;
        var face = CloneWithOpacity(panel, .96);
        dc.DrawEllipse(face, new Pen(CloneWithOpacity(border, .75), 1.0),
            center, KnobRadius - 10.0, KnobRadius - 10.0);
        DrawKnobArc(dc, center, KnobRadius, KnobSweepAngle,
            new Pen(CloneWithOpacity(border, .9), 6.0));
        var step = Math.Max(Epsilon, p.TickFrequency);
        var intervals = (int)Math.Floor(range / step + .000001);
        var spacing = KnobRadius * KnobSweepAngle * Math.PI / 180.0
            / Math.Max(1.0, range / step);
        var stride = NativeTickStride(spacing);
        for (var idx = 0; idx <= intervals; idx += stride)
        {
            var value = p.Minimum + idx * step;
            var fraction = Math.Clamp((value - p.Minimum) / range, 0.0, 1.0);
            var major = idx == 0 || idx % (stride * 5) == 0;
            var degrees = KnobStartAngle + fraction * KnobSweepAngle;
            dc.DrawLine(new Pen(CloneWithOpacity(muted, major ? .75 : .42),
                    major ? 1.6 : 1.0),
                KnobPoint(center, KnobRadius + 5.0, degrees),
                KnobPoint(center, KnobRadius + (major ? 11.0 : 9.0), degrees));
        }
        if (intervals % stride != 0)
        {
            var degrees = KnobStartAngle +
                (p.Minimum + intervals * step - p.Minimum) / range * KnobSweepAngle;
            dc.DrawLine(new Pen(muted, 1.6),
                KnobPoint(center, KnobRadius + 5.0, degrees),
                KnobPoint(center, KnobRadius + 11.0, degrees));
        }
        if (ratio > Epsilon)
            DrawKnobArc(dc, center, KnobRadius, ratio * KnobSweepAngle,
                new Pen(CloneWithOpacity(active ? accent : muted, .95), 6.0));
        var baseAngle = KnobStartAngle + reference * KnobSweepAngle;
        dc.DrawLine(new Pen(muted, 2.4),
            KnobPoint(center, KnobRadius - 7.0, baseAngle),
            KnobPoint(center, KnobRadius + 6.0, baseAngle));
        var currentAngle = KnobStartAngle + ratio * KnobSweepAngle;
        dc.DrawLine(new Pen(_hoverGlobalKnob ? Brushes.White : active ? accent : muted, 2.5),
            KnobPoint(center, KnobRadius - 8.0, currentAngle),
            KnobPoint(center, KnobRadius + 3.0, currentAngle));
        // This helper already centers vertically by text height: use the dial
        // center directly rather than applying the generic top-edge offset.
        DrawCenteredText(dc, $"{p.CurrentNumber:0.##}x", new Point(center.X, center.Y),
            p.IsValueModifiedFromReference() ? 16.2 : 15.0,
            active && p.IsValueModifiedFromReference() ? accent : text, FontWeights.Bold);
        DrawCenteredText(dc, $"{p.Minimum:0.##}x",
            new Point(center.X - KnobRadius * .8, card.Bottom - 19.0),
            9.0, muted, FontWeights.SemiBold);
        DrawCenteredText(dc, $"{p.Maximum:0.##}x",
            new Point(center.X + KnobRadius * .8, card.Bottom - 19.0),
            9.0, muted, FontWeights.SemiBold);
        if (active)
            _globalKnobHit = new Rect(center.X - KnobRadius - 5.0,
                center.Y - KnobRadius - 5.0, 2 * (KnobRadius + 5.0),
                2 * (KnobRadius + 5.0));
    }

    private static int NativeTickStride(double pixelsPerStep)
    {
        const double minimumSpacing = 6.5;
        var required = minimumSpacing / Math.Max(Epsilon, pixelsPerStep);
        var scale = 1;
        while (scale < 100000 && required > 10.0 * scale)
            scale *= 10;
        if (required <= scale) return scale;
        if (required <= scale * 2) return scale * 2;
        if (required <= scale * 5) return scale * 5;
        return scale * 10;
    }

    private void UpdateGlobalFromPoint(Point point)
    {
        if (_globalDamage is not { } p || !p.IsControlEnabled)
            return;
        var center = new Point(ActualWidth / 2.0, 47.0 + 97.0);
        var dx = point.X - center.X;
        var dy = point.Y - center.Y;
        if (dx * dx + dy * dy < 400.0)
            return;
        var degrees = Math.Atan2(dy, dx) * 180.0 / Math.PI;
        var clockwise = (degrees - KnobStartAngle + 360.0) % 360.0;
        var fraction = clockwise > KnobSweepAngle
            ? (clockwise < 315.0 ? 1.0 : 0.0)
            : clockwise / KnobSweepAngle;
        var raw = p.Minimum + fraction * (p.Maximum - p.Minimum);
        SetGlobalDamage(raw);
    }

    private void SetGlobalDamage(double raw)
    {
        if (_globalDamage is not { } p || !p.IsControlEnabled)
            return;
        var step = Math.Max(Epsilon, p.TickFrequency);
        var snapped = p.Minimum + Math.Round(
            (raw - p.Minimum) / step, MidpointRounding.AwayFromZero) * step;
        p.CurrentNumber = Math.Clamp(snapped, p.Minimum, p.Maximum);
        _status.Clear();
        InvalidateVisual();
    }

    private Rect ContentRect()
    {
        var width = Math.Min(MaxContentWidth, Math.Max(1.0, ActualWidth - 32.0));
        return new Rect((ActualWidth - width) / 2.0, 0.0, width, ActualHeight);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var point = e.GetPosition(this);
        if (_globalKnobHit.Contains(point) && _globalDamage is { IsControlEnabled: true } global)
        {
            _selectedGlobalKnob = true;
            _dragGlobalKnob = true;
            ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(global));
            CaptureMouse();
            UpdateGlobalFromPoint(point);
            e.Handled = true;
            return;
        }

        var row = FindTarget(point);
        if (row < 0)
            return;

        _selectedGlobalKnob = false;
        _selectedRow = row;
        if (_parameters[row] is { } selected)
            ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(selected));
        _dragRow = row;
        CaptureMouse();
        UpdateFromPoint(row, e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var point = e.GetPosition(this);
        var globalHover = _globalKnobHit.Contains(point);
        if (globalHover != _hoverGlobalKnob)
        {
            _hoverGlobalKnob = globalHover;
            InvalidateVisual();
        }

        if (_dragGlobalKnob && e.LeftButton == MouseButtonState.Pressed)
        {
            Cursor = Cursors.Hand;
            UpdateGlobalFromPoint(point);
            e.Handled = true;
            return;
        }

        var hover = globalHover ? -1 : FindTarget(point);
        if (hover != _hoverRow)
        {
            _hoverRow = hover;
            InvalidateVisual();
        }
        Cursor = globalHover ? Cursors.Hand
            : hover >= 0 ? Cursors.SizeNS : Cursors.Arrow;
        ToolTip = globalHover
            ? "Scales damage for all anomalies. Turn or scroll in 0.01x steps; right-click resets global."
            : hover >= 0
                ? "FINAL = BASE × global (before protection). BASE is this anomaly's editable value; the bar compares it with vanilla. Right-click resets this anomaly."
                : null;

        if (_dragRow >= 0 && e.LeftButton == MouseButtonState.Pressed)
        {
            UpdateFromPoint(_dragRow, point);
            e.Handled = true;
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverRow = -1;
        _hoverGlobalKnob = false;
        ToolTip = null;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragGlobalKnob)
        {
            _dragGlobalKnob = false;
            ReleaseMouseCapture();
            e.Handled = true;
            return;
        }
        if (_dragRow < 0)
            return;

        _dragRow = -1;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);
        var point = e.GetPosition(this);
        if (_globalKnobHit.Contains(point) && _globalDamage is { IsControlEnabled: true } global)
        {
            _selectedGlobalKnob = true;
            ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(global));
            global.ResetToReference();
            _status.ShowTransient("Global anomaly multiplier reset to Base Preset.");
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        var row = FindTarget(point);
        if (row < 0 || _parameters[row] is not { } parameter)
            return;

        _selectedGlobalKnob = false;
        _selectedRow = row;
        ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(parameter));
        parameter.ResetToReference();
        _status.ShowTransient($"{Anomalies[row].Label} damage reset to Base Preset.");
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.R)
        {
            _globalDamage?.ResetToReference();
            foreach (var parameter in _parameters)
                parameter?.ResetToReference();

            _status.ShowTransient("Global and individual anomaly damage reset to Base Preset.");
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_selectedGlobalKnob && _globalDamage is { IsControlEnabled: true } global)
        {
            var globalStepCount = e.Key switch
            {
                Key.Up or Key.Right or Key.Add or Key.OemPlus => 1,
                Key.Down or Key.Left or Key.Subtract or Key.OemMinus => -1,
                Key.PageUp => 10,
                Key.PageDown => -10,
                _ => 0
            };
            if (e.Key == Key.Home)
                SetGlobalDamage(global.Minimum);
            else if (e.Key == Key.End)
                SetGlobalDamage(global.Maximum);
            else if (globalStepCount != 0)
                SetGlobalDamage(global.CurrentNumber + globalStepCount * global.TickFrequency);
            else
                return;
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

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!_globalKnobHit.Contains(e.GetPosition(this))
            || _globalDamage is not { IsControlEnabled: true } global)
            return;
        var direction = Math.Sign(e.Delta);
        if (direction == 0)
            return;
        Focus();
        _selectedGlobalKnob = true;
        ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(global));
        SetGlobalDamage(global.CurrentNumber + direction * global.TickFrequency);
        e.Handled = true;
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        _dragGlobalKnob = false;
        _dragRow = -1;
        InvalidateVisual();
    }

    private void UpdateFromPoint(int row, Point point)
    {
        if (!_targets.TryGetValue(row, out var target) || _parameters[row] is not { } parameter)
            return;

        var ratio = Math.Clamp((target.MapRect.Bottom - point.Y) / Math.Max(1.0, target.MapRect.Height), 0.0, 1.0);
        var raw = parameter.Minimum + ratio * (parameter.Maximum - parameter.Minimum);
        var tick = Math.Max(parameter.TickFrequency, Epsilon);
        var snapped = parameter.Minimum + Math.Round((raw - parameter.Minimum) / tick, MidpointRounding.AwayFromZero) * tick;

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

    private static string Compact(string value, int maxLength)
    {
        var compact = value.Replace(Environment.NewLine, " ").Trim();
        return compact.Length <= maxLength ? compact : compact[..Math.Max(1, maxLength - 1)] + "…";
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

    private sealed record AnomalyDefinition(string ParameterId, string Label, bool IsMultiplier);
    private sealed record HitTarget(Rect HitRect, Rect MapRect);
}
