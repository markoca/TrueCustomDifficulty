using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Compact stamina-usage board.
///
/// One-time movement/melee costs share a relative-to-Base-Preset scale. Aiming is a
/// continuous recovery penalty. Exhaustion/recovery uses the actual absolute stamina
/// thresholds and direct recovery-effect values from the player/effect CFGs.
/// </summary>
public sealed class ActionCostsRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double DesiredHeight = 393.0;

    private const string AimParameterId = "stamina.aimRegenCoef";
    private const string SprintDisableThresholdId = "stamina.sprintDisableThreshold";
    private const string SprintRecoveryDelayId = "stamina.sprintRecoveryDelay";
    private const string LowRecoveryThresholdId = "stamina.lowRecoveryThreshold";
    private const string LowRecoveryStrengthId = "stamina.lowRecoveryStrength";
    private const string LowRecoveryDurationId = "stamina.lowRecoveryDuration";

    private static readonly ActionDefinition[] Actions =
    {
        new("stamina.sprintCost", "SPRINT", ActionGroup.Movement),
        new("stamina.jumpCost", "JUMP", ActionGroup.Movement),
        new("stamina.vaultCost", "VAULT", ActionGroup.Movement),
        new("stamina.meleeNormalCost", "NORMAL MELEE", ActionGroup.Melee),
        new("stamina.meleeStrongCost", "STRONG MELEE", ActionGroup.Melee),
        new("stamina.butstockCost", "BUTTSTOCK", ActionGroup.Melee),
    };

    private readonly ParameterViewModel?[] _actionParameters = new ParameterViewModel?[Actions.Length];
    private readonly List<InteractiveTarget> _targets = new();
    private readonly VisualStatusMessage _status;

    private ParameterViewModel? _aimParameter;
    private ParameterViewModel? _sprintDisableThreshold;
    private ParameterViewModel? _sprintRecoveryDelay;
    private ParameterViewModel? _lowRecoveryThreshold;
    private ParameterViewModel? _lowRecoveryStrength;
    private ParameterViewModel? _lowRecoveryDuration;

    private ParameterViewModel? _selectedParameter;
    private ParameterViewModel? _hoverParameter;
    private InteractiveTarget? _dragTarget;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(ActionCostsRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public ActionCostsRadar()
    {
        _status = new VisualStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = DesiredHeight;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 760.0 : Math.Max(560.0, availableSize.Width);
        return new Size(width, DesiredHeight);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((ActionCostsRadar)d).RebindParameters();

    private void RebindParameters()
    {
        foreach (var parameter in EnumerateBoundParameters())
        {
            if (parameter is not null)
                parameter.PropertyChanged -= Parameter_PropertyChanged;
        }

        Array.Clear(_actionParameters, 0, _actionParameters.Length);
        _aimParameter = null;
        _sprintDisableThreshold = null;
        _sprintRecoveryDelay = null;
        _lowRecoveryThreshold = null;
        _lowRecoveryStrength = null;
        _lowRecoveryDuration = null;
        _selectedParameter = null;
        _hoverParameter = null;
        _dragTarget = null;

        if (Parameters is not null)
        {
            var byId = Parameters
                .OfType<ParameterViewModel>()
                .ToDictionary(x => x.Id, StringComparer.Ordinal);

            for (var i = 0; i < Actions.Length; i++)
            {
                if (byId.TryGetValue(Actions[i].ParameterId, out var parameter))
                    _actionParameters[i] = parameter;
            }

            byId.TryGetValue(AimParameterId, out _aimParameter);
            byId.TryGetValue(SprintDisableThresholdId, out _sprintDisableThreshold);
            byId.TryGetValue(SprintRecoveryDelayId, out _sprintRecoveryDelay);
            byId.TryGetValue(LowRecoveryThresholdId, out _lowRecoveryThreshold);
            byId.TryGetValue(LowRecoveryStrengthId, out _lowRecoveryStrength);
            byId.TryGetValue(LowRecoveryDurationId, out _lowRecoveryDuration);

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
        foreach (var parameter in _actionParameters)
            yield return parameter;

        yield return _aimParameter;
        yield return _sprintDisableThreshold;
        yield return _sprintRecoveryDelay;
        yield return _lowRecoveryThreshold;
        yield return _lowRecoveryStrength;
        yield return _lowRecoveryDuration;
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

        DrawCenteredText(dc, "STAMINA USAGE", new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);

        var left = 18.0;
        var width = Math.Max(540.0, ActualWidth - 36.0);
        var gap = 12.0;
        var half = (width - gap) / 2.0;

        DrawActionGroup(
            dc,
            ActionGroup.Movement,
            "MOVEMENT",
            new Rect(left, 47.0, half, 132.0),
            text,
            muted,
            border,
            accent,
            panel);

        DrawActionGroup(
            dc,
            ActionGroup.Melee,
            "MELEE",
            new Rect(left + half + gap, 47.0, half, 132.0),
            text,
            muted,
            border,
            accent,
            panel);

        DrawAimPanel(
            dc,
            new Rect(left, 189.0, width, 54.0),
            text,
            muted,
            border,
            accent,
            panel);

        DrawExhaustionPanel(
            dc,
            new Rect(left, 255.0, width, 119.0),
            text,
            muted,
            border,
            soft,
            accent,
            panel);

        // Silent reset: no transient status text.
    }

    private void DrawActionGroup(
        DrawingContext dc,
        ActionGroup group,
        string title,
        Rect card,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.95), new Pen(border, 0.9), card, 4.0, 4.0);
        DrawText(dc, title, new Point(card.Left + 9.0, card.Top + 7.0), 9.3, muted, FontWeights.Bold);

        var indices = Enumerable.Range(0, Actions.Length)
            .Where(i => Actions[i].Group == group)
            .ToArray();

        for (var local = 0; local < indices.Length; local++)
        {
            var index = indices[local];
            var parameter = _actionParameters[index];
            if (parameter is null)
                continue;

            var top = card.Top + 27.0 + local * 34.0;
            var reference = parameter.ReferenceNumber;
            var relative = Math.Abs(reference) > Epsilon ? parameter.CurrentNumber / reference : 0.0;
            var modified = Math.Abs(parameter.CurrentNumber - reference) > Epsilon;

            DrawText(dc, Actions[index].Label, new Point(card.Left + 9.0, top), 9.2, text, FontWeights.Bold);
            DrawRightAlignedText(
                dc,
                $"{FormatRaw(parameter.CurrentNumber)} SP",
                new Point(card.Right - 9.0, top),
                9.2,
                modified ? accent : text,
                FontWeights.Bold);

            var track = new Rect(card.Left + 9.0, top + 20.0, card.Width - 18.0, 8.0);
            var y = track.Top + track.Height / 2.0;
            dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

            var currentX = track.Left + track.Width * Math.Clamp(relative / 2.0, 0.0, 1.0);
            var baseX = track.Left + track.Width * 0.5;

            dc.DrawLine(new Pen(muted, 1.0), new Point(baseX, y - 5.0), new Point(baseX, y + 5.0));
            dc.DrawLine(new Pen(accent, 3.0), new Point(track.Left, y), new Point(currentX, y));
            DrawHandle(dc, parameter, new Point(currentX, y), panel, accent);

            AddTarget(
                parameter,
                new Rect(track.Left - 6.0, track.Top - 5.0, track.Width + 12.0, track.Height + 10.0),
                track,
                MappingKind.RelativeToReference2X,
                0.0,
                2.0);
        }
    }

    private void DrawAimPanel(
        DrawingContext dc,
        Rect card,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        dc.DrawRoundedRectangle(
            IsSelected(_aimParameter) ? CloneWithOpacity(accent, 0.08) : CloneWithOpacity(panel, 0.95),
            new Pen(border, 0.9),
            card,
            4.0,
            4.0);

        DrawText(dc, "AIMING RECOVERY PENALTY", new Point(card.Left + 10.0, card.Top + 8.0), 9.4, text, FontWeights.Bold);

        if (_aimParameter is not { } parameter)
            return;

        DrawRightAlignedText(dc, $"{parameter.CurrentNumber * 100:0}%", new Point(card.Right - 10.0, card.Top + 7.0), 10.0,
            parameter.IsValueModifiedFromReference() ? accent : text, FontWeights.Bold);

        var track = new Rect(card.Left + 235.0, card.Top + 31.0, Math.Max(150.0, card.Width - 250.0), 10.0);
        var y = track.Top + track.Height / 2.0;
        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track);
        var baseX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, track);
        dc.DrawLine(new Pen(muted, 1.0), new Point(baseX, y - 6.0), new Point(baseX, y + 6.0));
        dc.DrawLine(new Pen(accent, 3.0), new Point(track.Left, y), new Point(currentX, y));
        DrawHandle(dc, parameter, new Point(currentX, y), panel, accent);

        AddTarget(
            parameter,
            new Rect(track.Left - 6.0, track.Top - 6.0, track.Width + 12.0, track.Height + 12.0),
            track,
            MappingKind.Absolute,
            parameter.Minimum,
            parameter.Maximum);
    }

    private void DrawExhaustionPanel(
        DrawingContext dc,
        Rect card,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.95), new Pen(border, 0.9), card, 4.0, 4.0);
        DrawText(dc, "EXHAUSTION & LOW-SP RECOVERY", new Point(card.Left + 10.0, card.Top + 7.0), 9.5, text, FontWeights.Bold);

        var thresholdTrack = new Rect(card.Left + 145.0, card.Top + 30.0, card.Width - 160.0, 38.0);
        var sprintY = thresholdTrack.Top + 7.0;
        var recoveryY = thresholdTrack.Top + 28.0;

        DrawText(dc, "SPRINT LOCK", new Point(card.Left + 10.0, sprintY - 6.0), 9.0, muted, FontWeights.Bold);
        DrawText(dc, "RECOVERY BELOW", new Point(card.Left + 10.0, recoveryY - 6.0), 9.0, muted, FontWeights.Bold);

        var zeroX = thresholdTrack.Left;
        var thirtyX = thresholdTrack.Left + thresholdTrack.Width * 0.5;
        var sixtyX = thresholdTrack.Right;
        dc.DrawLine(new Pen(soft, 1.0), new Point(zeroX, thresholdTrack.Top - 1.0), new Point(zeroX, thresholdTrack.Bottom + 1.0));
        dc.DrawLine(new Pen(soft, 1.0) { DashStyle = DashStyles.Dot }, new Point(thirtyX, thresholdTrack.Top - 1.0), new Point(thirtyX, thresholdTrack.Bottom + 1.0));
        dc.DrawLine(new Pen(soft, 1.0), new Point(sixtyX, thresholdTrack.Top - 1.0), new Point(sixtyX, thresholdTrack.Bottom + 1.0));

        DrawAbsoluteThresholdRow(
            dc,
            _sprintDisableThreshold,
            thresholdTrack,
            sprintY,
            0.0,
            60.0,
            value => $"{value:0.0} SP",
            text,
            muted,
            border,
            accent,
            panel);

        DrawAbsoluteThresholdRow(
            dc,
            _lowRecoveryThreshold,
            thresholdTrack,
            recoveryY,
            0.0,
            60.0,
            value => $"{value:0} SP",
            text,
            muted,
            border,
            accent,
            panel);

        var miniGap = 10.0;
        var miniLeft = card.Left + 10.0;
        var miniTop = card.Top + 76.0;
        var miniWidth = (card.Width - 20.0 - miniGap * 2.0) / 3.0;

        DrawMiniTrack(
            dc,
            _sprintRecoveryDelay,
            "RESTART DELAY",
            value => $"{value:0.0} s",
            new Rect(miniLeft, miniTop, miniWidth, 34.0),
            text,
            muted,
            border,
            accent,
            panel);

        DrawMiniTrack(
            dc,
            _lowRecoveryStrength,
            "RECOVERY BOOST",
            value => $"+{value:0.##} SP/s",
            new Rect(miniLeft + miniWidth + miniGap, miniTop, miniWidth, 34.0),
            text,
            muted,
            border,
            accent,
            panel);

        DrawMiniTrack(
            dc,
            _lowRecoveryDuration,
            "BOOST DURATION",
            value => $"{value:0.0} s",
            new Rect(miniLeft + (miniWidth + miniGap) * 2.0, miniTop, miniWidth, 34.0),
            text,
            muted,
            border,
            accent,
            panel);

    }

    private void DrawAbsoluteThresholdRow(
        DrawingContext dc,
        ParameterViewModel? parameter,
        Rect trackArea,
        double y,
        double visualMinimum,
        double visualMaximum,
        Func<double, string> formatter,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        if (parameter is null)
            return;

        var line = new Rect(trackArea.Left, y - 5.0, trackArea.Width, 10.0);
        dc.DrawLine(new Pen(border, 3.0), new Point(line.Left, y), new Point(line.Right, y));

        var currentX = ValueToX(parameter.CurrentNumber, visualMinimum, visualMaximum, line);
        var baseX = ValueToX(parameter.ReferenceNumber, visualMinimum, visualMaximum, line);
        dc.DrawLine(new Pen(muted, 1.0), new Point(baseX, y - 6.0), new Point(baseX, y + 6.0));
        dc.DrawLine(new Pen(accent, 2.8), new Point(line.Left, y), new Point(currentX, y));
        DrawHandle(dc, parameter, new Point(currentX, y), panel, accent);

        DrawRightAlignedText(dc, formatter(parameter.CurrentNumber), new Point(line.Right, y - 10.0), 9.0, parameter.IsValueModifiedFromReference() ? accent : text, FontWeights.Bold);

        var activeLeft = ValueToX(parameter.Minimum, visualMinimum, visualMaximum, line);
        var activeRight = ValueToX(parameter.Maximum, visualMinimum, visualMaximum, line);
        var activeMap = new Rect(activeLeft, line.Top, Math.Max(1.0, activeRight - activeLeft), line.Height);

        AddTarget(
            parameter,
            new Rect(activeMap.Left - 6.0, y - 9.0, activeMap.Width + 12.0, 18.0),
            activeMap,
            MappingKind.Absolute,
            parameter.Minimum,
            parameter.Maximum);
    }

    private void DrawMiniTrack(
        DrawingContext dc,
        ParameterViewModel? parameter,
        string label,
        Func<double, string> formatter,
        Rect rect,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        if (parameter is null)
            return;

        DrawText(dc, label, new Point(rect.Left, rect.Top), 9.0, muted, FontWeights.Bold);
        DrawRightAlignedText(dc, formatter(parameter.CurrentNumber), new Point(rect.Right, rect.Top), 9.0, IsSelected(parameter) ? accent : text, FontWeights.Bold);

        var track = new Rect(rect.Left, rect.Top + 21.0, rect.Width, 8.0);
        var y = track.Top + track.Height / 2.0;
        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track);
        var baseX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, track);
        dc.DrawLine(new Pen(muted, 1.0), new Point(baseX, y - 5.0), new Point(baseX, y + 5.0));
        dc.DrawLine(new Pen(accent, 2.8), new Point(track.Left, y), new Point(currentX, y));
        DrawHandle(dc, parameter, new Point(currentX, y), panel, accent);

        AddTarget(
            parameter,
            new Rect(track.Left - 5.0, track.Top - 6.0, track.Width + 10.0, track.Height + 12.0),
            track,
            MappingKind.Absolute,
            parameter.Minimum,
            parameter.Maximum);
    }

    private void DrawHandle(DrawingContext dc, ParameterViewModel parameter, Point point, Brush panel, Brush accent)
    {
        dc.DrawEllipse(
            panel,
            new Pen(accent, IsSelected(parameter) ? 2.4 : 1.4),
            point,
            5.5,
            5.5);
        dc.DrawEllipse(accent, null, point, 2.4, 2.4);
    }

    private bool IsSelected(ParameterViewModel? parameter)
        => parameter is not null && ReferenceEquals(parameter, _selectedParameter);

    private void AddTarget(
        ParameterViewModel parameter,
        Rect hitRect,
        Rect mapRect,
        MappingKind mapping,
        double visualMinimum,
        double visualMaximum)
    {
        if (!parameter.IsControlEnabled)
            return;

        _targets.Add(new InteractiveTarget(parameter, hitRect, mapRect, mapping, visualMinimum, visualMaximum));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var target = FindTarget(e.GetPosition(this));
        if (target is null)
            return;

        _selectedParameter = target.Parameter;
        _dragTarget = target;
        CaptureMouse();
        UpdateTarget(target, e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var point = e.GetPosition(this);
        var hover = FindTarget(point)?.Parameter;

        if (!ReferenceEquals(hover, _hoverParameter))
        {
            _hoverParameter = hover;
            InvalidateVisual();
        }

        Cursor = hover is null ? Cursors.Arrow : Cursors.Hand;

        if (_dragTarget is null || e.LeftButton != MouseButtonState.Pressed)
            return;

        UpdateTarget(_dragTarget, point);
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverParameter is null)
            return;

        _hoverParameter = null;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragTarget is null)
            return;

        _dragTarget = null;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);

        var target = FindTarget(e.GetPosition(this));
        if (target is null)
            return;

        _selectedParameter = target.Parameter;
        target.Parameter.ResetToReference();
        _status.ShowTransient($"{ShortLabel(target.Parameter)} reset to Base Preset.");
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.R)
        {
            foreach (var parameter in EnumerateBoundParameters())
                parameter?.ResetToReference();

            _status.ShowTransient("Stamina pressure board reset to Base Preset.");
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

        if (direction == 0 || _selectedParameter is null || !_selectedParameter.IsSlider)
            return;

        _selectedParameter.CurrentNumber = Math.Clamp(
            _selectedParameter.CurrentNumber + direction * _selectedParameter.TickFrequency,
            _selectedParameter.Minimum,
            _selectedParameter.Maximum);

        _status.Clear();
        InvalidateVisual();
        e.Handled = true;
    }

    private void UpdateTarget(InteractiveTarget target, Point point)
    {
        var ratio = Math.Clamp(
            (point.X - target.MapRect.Left) / Math.Max(1.0, target.MapRect.Width),
            0.0,
            1.0);

        double raw;
        if (target.Mapping == MappingKind.RelativeToReference2X)
            raw = target.Parameter.ReferenceNumber * ratio * 2.0;
        else
            raw = target.VisualMinimum + ratio * (target.VisualMaximum - target.VisualMinimum);

        SetSnapped(target.Parameter, raw);
        _status.Clear();
        InvalidateVisual();
    }

    private static void SetSnapped(ParameterViewModel parameter, double raw)
    {
        var tick = Math.Max(parameter.TickFrequency, Epsilon);
        var snapped = parameter.Minimum
            + Math.Round((raw - parameter.Minimum) / tick, MidpointRounding.AwayFromZero) * tick;

        parameter.CurrentNumber = Math.Clamp(snapped, parameter.Minimum, parameter.Maximum);
    }

    private InteractiveTarget? FindTarget(Point point)
    {
        for (var i = _targets.Count - 1; i >= 0; i--)
        {
            if (_targets[i].HitRect.Contains(point))
                return _targets[i];
        }

        return null;
    }

    private static string ShortLabel(ParameterViewModel parameter)
        => parameter.Label.Replace(" - ", " ").Trim();

    private static string FormatRaw(double value)
        => Math.Abs(value - Math.Round(value)) < Epsilon
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);

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

    private enum ActionGroup
    {
        Movement,
        Melee
    }

    private enum MappingKind
    {
        RelativeToReference2X,
        Absolute
    }

    private sealed record ActionDefinition(string ParameterId, string Label, ActionGroup Group);

    private sealed record InteractiveTarget(
        ParameterViewModel Parameter,
        Rect HitRect,
        Rect MapRect,
        MappingKind Mapping,
        double VisualMinimum,
        double VisualMaximum);
}
