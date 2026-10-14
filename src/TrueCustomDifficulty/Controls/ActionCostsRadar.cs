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
    private const double UsageHeight = 187.0;
    private const double ExhaustionWideHeight = 330.0;
    private const double ExhaustionNarrowHeight = 450.0;
    private bool IsExhaustion => string.Equals(Mode, "Exhaustion", StringComparison.Ordinal);

    private const string AimParameterId = "stamina.aimRegenCoef";
    private const string MaxStaminaParameterId = "stamina.maxSP";
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
        new("stamina.meleeNormalCost", "QUICK MELEE", ActionGroup.Melee),
        new("stamina.meleeStrongCost", "STRONG MELEE", ActionGroup.Melee),
        new("stamina.butstockCost", "BUTTSTOCK", ActionGroup.Melee),
    };

    private readonly ParameterViewModel?[] _actionParameters = new ParameterViewModel?[Actions.Length];
    private readonly List<InteractiveTarget> _targets = new();
    private readonly VisualStatusMessage _status;

    private ParameterViewModel? _aimParameter;
    // Read-only visual ruler dependency. Never include in this board's R-reset group.
    private ParameterViewModel? _maximumStamina;
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

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode), typeof(string), typeof(ActionCostsRadar),
        new FrameworkPropertyMetadata("Usage",
            FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
            OnParametersChanged));

    public string Mode
    {
        get => (string)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public ActionCostsRadar()
    {
        _status = new VisualStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = 0.0;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 760.0 : Math.Max(560.0, availableSize.Width);
        return new Size(width, IsExhaustion
            ? width >= 780.0 ? ExhaustionWideHeight : ExhaustionNarrowHeight
            : UsageHeight);
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

        if (_maximumStamina is not null)
            _maximumStamina.PropertyChanged -= Parameter_PropertyChanged;

        Array.Clear(_actionParameters, 0, _actionParameters.Length);
        _aimParameter = null;
        _maximumStamina = null;
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
            if (IsExhaustion)
                byId.TryGetValue(MaxStaminaParameterId, out _maximumStamina);
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
            if (_maximumStamina is not null)
                _maximumStamina.PropertyChanged += Parameter_PropertyChanged;
        }

        InvalidateVisual();
    }

    private IEnumerable<ParameterViewModel?> EnumerateBoundParameters()
    {
        if (IsExhaustion)
        {
            yield return _aimParameter;
            yield return _sprintDisableThreshold;
            yield return _sprintRecoveryDelay;
            yield return _lowRecoveryThreshold;
            yield return _lowRecoveryStrength;
            yield return _lowRecoveryDuration;
        }
        else
        {
            foreach (var parameter in _actionParameters)
                yield return parameter;
        }
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

        DrawCenteredText(dc,
            IsExhaustion ? "EXHAUSTION & RECOVERY" : "STAMINA USAGE",
            new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);

        const double left = 18.0;
        var width = Math.Max(1.0, ActualWidth - 36.0);
        const double gap = 12.0;
        var half = (width - gap) / 2.0;

        if (IsExhaustion)
        {
            DrawExhaustionBoard(dc, left, width, text, muted, border, accent, panel);
        }
        else
        {
            DrawActionGroup(dc, ActionGroup.Movement, "MOVEMENT",
                new Rect(left, 48.0, half, 124.0),
                text, muted, border, accent, panel);
            DrawActionGroup(dc, ActionGroup.Melee, "MELEE",
                new Rect(left + half + gap, 48.0, half, 124.0),
                text, muted, border, accent, panel);
        }

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

            var top = card.Top + 25.0 + local * 31.0;
            var reference = parameter.ReferenceNumber;
            var relative = Math.Abs(reference) > Epsilon ? parameter.CurrentNumber / reference : 0.0;
            DrawText(dc, Actions[index].Label, new Point(card.Left + 9.0, top), 9.2,
                text, FontWeights.Bold);
            DrawSourceValue(dc, parameter,
                $"{FormatRaw(parameter.CurrentNumber)} STAMINA",
                new Point(card.Right - 9.0, top), 10.4, text, accent);

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


    // STAMINA THRESHOLDS: player-facing ruler starts at vanilla's 100 STAMINA,
    // grows with edited Max Stamina, and is never confused with the 60-point
    // native ceiling of the separate recovery-boost threshold.
    // Their hit lanes are separate above/below the ruler so
    // both remain independently draggable even when their values coincide.
    // The other four CFG sources occupy three distinct gameplay groups.
    private void DrawExhaustionBoard(
        DrawingContext dc, double left, double width,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        const double gap = 10.0;
        const double top = 48.0;
        var wide = ActualWidth >= 780.0;
        var zonesWidth = wide ? Math.Min(340.0, (width - gap) * 0.40) : width;
        var zonesHeight = wide ? 268.0 : 192.0;
        DrawStaminaZonesCard(dc,
            new Rect(left, top, zonesWidth, zonesHeight),
            text, muted, border, accent, panel);

        var rightX = wide ? left + zonesWidth + gap : left;
        var rightWidth = wide ? width - zonesWidth - gap : width;
        if (wide)
        {
            DrawRecoveryStateCard(dc, _aimParameter, "AIMING", "REGEN PENALTY",
                value => $"{value * 100.0:0.#}%",
                new Rect(rightX, top, rightWidth, 70.0),
                text, muted, border, accent, panel);
            DrawRecoveryStateCard(dc, _sprintRecoveryDelay, "AFTER EXHAUSTION", "REGEN DELAY",
                value => $"{value:0.0} s",
                new Rect(rightX, top + 80.0, rightWidth, 70.0),
                text, muted, border, accent, panel);
            DrawLowStaminaBoostCard(dc,
                new Rect(rightX, top + 160.0, rightWidth, 108.0),
                text, muted, border, accent, panel);
        }
        else
        {
            var half = (rightWidth - gap) / 2.0;
            var groupsTop = top + zonesHeight + gap;
            DrawRecoveryStateCard(dc, _aimParameter, "AIMING", "REGEN PENALTY",
                value => $"{value * 100.0:0.#}%",
                new Rect(rightX, groupsTop, half, 82.0),
                text, muted, border, accent, panel);
            DrawRecoveryStateCard(dc, _sprintRecoveryDelay, "AFTER EXHAUSTION", "REGEN DELAY",
                value => $"{value:0.0} s",
                new Rect(rightX + half + gap, groupsTop, half, 82.0),
                text, muted, border, accent, panel);
            DrawLowStaminaBoostCard(dc,
                new Rect(rightX, groupsTop + 92.0, rightWidth, 94.0),
                text, muted, border, accent, panel);
        }
    }

    private void DrawStaminaZonesCard(
        DrawingContext dc, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        DrawRecoveryFrame(dc, card, panel, border);
        DrawCenteredText(dc, "STAMINA THRESHOLDS",
            new Point(card.Left + card.Width / 2.0, card.Top + 16.0),
            10.2, text, FontWeights.Bold);

        if (_sprintDisableThreshold is null || _lowRecoveryThreshold is null)
            return;

        var compact = card.Height < 220.0;
        var firstLabelY = card.Top + (compact ? 34.0 : 46.0);
        var secondLabelY = card.Top + (compact ? 134.0 : 183.0);
        DrawZoneValue(dc, _sprintDisableThreshold, "SPRINT STOPS BELOW", firstLabelY,
            card, text, accent);
        DrawZoneValue(dc, _lowRecoveryThreshold, "BOOST STARTS BELOW", secondLabelY,
            card, text, accent);

        // One common STAMINA space with two skinny independent color traces. The
        // darker upper trace denotes sprint lock; the lower trace denotes
        // low-stamina recovery eligibility, NOT a required ordering constraint.
        var rail = new Rect(card.Left + 21.0,
            card.Top + (compact ? 88.0 : 119.0),
            Math.Max(1.0, card.Width - 42.0), 8.0);
        var y = rail.Top + rail.Height / 2.0;
        const double staminaMin = 0.0;
        // The original game uses 100 STAMINA, not 60. If Max Stamina is
        // raised past 100, extend this same absolute-unit ruler to that pool.
        // Below 100 retain a comparable reference scale; the editable native
        // limits remain Sprint 0..32.2 and Boost 1..60 independently.
        var staminaMax = Math.Max(100.0, _maximumStamina?.CurrentNumber ?? 100.0);
        var sprintX = ValueToX(_sprintDisableThreshold.CurrentNumber, staminaMin, staminaMax, rail);
        var boostX = ValueToX(_lowRecoveryThreshold.CurrentNumber, staminaMin, staminaMax, rail);
        var sprintReferenceX = ValueToX(_sprintDisableThreshold.ReferenceNumber, staminaMin, staminaMax, rail);
        var boostReferenceX = ValueToX(_lowRecoveryThreshold.ReferenceNumber, staminaMin, staminaMax, rail);

        dc.DrawLine(new Pen(border, 7.0),
            new Point(rail.Left, y), new Point(rail.Right, y));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.67), 2.5),
            new Point(rail.Left, y - 2.2), new Point(sprintX, y - 2.2));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.38), 2.5),
            new Point(rail.Left, y + 2.2), new Point(boostX, y + 2.2));

        dc.DrawLine(new Pen(CloneWithOpacity(muted, 0.85), 1.0),
            new Point(sprintReferenceX, y - 8.0), new Point(sprintReferenceX, y - 3.0));
        dc.DrawLine(new Pen(CloneWithOpacity(muted, 0.85), 1.0),
            new Point(boostReferenceX, y + 3.0), new Point(boostReferenceX, y + 8.0));

        // Two physical handles offset vertically around ONE ruler. Each lane
        // owns the exact native source range: sprint 0..32.2 and boost 1..60.
        var sprintHandle = new Point(sprintX, y - 13.0);
        var boostHandle = new Point(boostX, y + 13.0);
        dc.DrawLine(new Pen(accent, 1.2),
            new Point(sprintX, y - 10.0), new Point(sprintX, y));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.75), 1.2),
            new Point(boostX, y), new Point(boostX, y + 10.0));
        DrawHandle(dc, _sprintDisableThreshold, sprintHandle, panel, accent);
        DrawHandle(dc, _lowRecoveryThreshold, boostHandle, panel, accent);

        // Endpoints belong to the rail, not the distant bottom of the card.
        // Keep them symmetric and clear of both value labels at all widths.
        var axisLabelY = rail.Bottom + 11.0;
        DrawText(dc, "0 STAMINA", new Point(rail.Left, axisLabelY),
            9.2, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, $"{staminaMax:0} STAMINA",
            new Point(rail.Right, axisLabelY),
            9.2, muted, FontWeights.Normal);

        // Outside each source's native edit range the shared ruler is passive:
        // Sprint 0..32.2 and Boost 1..60, regardless of the displayed pool.
        var sprintMaxX = ValueToX(_sprintDisableThreshold.Maximum, staminaMin, staminaMax, rail);
        var boostMinX = ValueToX(_lowRecoveryThreshold.Minimum, staminaMin, staminaMax, rail);
        var boostMaxX = ValueToX(_lowRecoveryThreshold.Maximum, staminaMin, staminaMax, rail);
        AddTarget(_sprintDisableThreshold,
            new Rect(rail.Left - 5.0, y - 34.0,
                Math.Max(1.0, sprintMaxX - rail.Left) + 10.0, 32.0),
            rail, MappingKind.Absolute, staminaMin, staminaMax);
        AddTarget(_lowRecoveryThreshold,
            new Rect(boostMinX - 5.0, y + 2.0,
                Math.Max(1.0, boostMaxX - boostMinX) + 10.0, 32.0),
            rail, MappingKind.Absolute, staminaMin, staminaMax);
    }

    private void DrawZoneValue(
        DrawingContext dc, ParameterViewModel parameter, string title,
        double top, Rect card, Brush text, Brush accent)
    {
        var heading = CreateText(title, 9.8, text, FontWeights.Bold);
        var value = CreateSourceValueText(parameter,
            $"{FormatRaw(parameter.CurrentNumber)} STAMINA", 11.5, text, accent);
        heading.MaxTextWidth = Math.Max(1.0,
            card.Width - 24.0 - value.WidthIncludingTrailingWhitespace - 10.0);
        heading.MaxLineCount = 1;
        heading.Trimming = TextTrimming.CharacterEllipsis;
        dc.DrawText(heading, new Point(card.Left + 12.0, top));
        dc.DrawText(value, new Point(card.Right - 12.0
            - value.WidthIncludingTrailingWhitespace, top - 2.0));
    }

    private static void DrawRecoveryFrame(
        DrawingContext dc, Rect card, Brush panel, Brush border)
        => dc.DrawRoundedRectangle(panel, new Pen(border, 0.9),
            card, 4.0, 4.0);

    private void DrawRecoveryStateCard(
        DrawingContext dc, ParameterViewModel? parameter,
        string state, string controlLabel, Func<double, string> formatter, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        DrawRecoveryFrame(dc, card, panel, border);
        DrawText(dc, state, new Point(card.Left + 12.0, card.Top + 8.0),
            9.5, muted, FontWeights.Bold);
        DrawRecoverySourceTrack(dc, parameter, controlLabel, formatter,
            new Rect(card.Left + 12.0, card.Top + 26.0,
                Math.Max(1.0, card.Width - 24.0), 41.0),
            text, muted, border, accent, panel);
    }

    private void DrawLowStaminaBoostCard(
        DrawingContext dc, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        DrawRecoveryFrame(dc, card, panel, border);
        DrawText(dc, "LOW-STAMINA BOOST",
            new Point(card.Left + 12.0, card.Top + 9.0),
            10.0, text, FontWeights.Bold);
        const double gap = 14.0;
        var half = Math.Max(1.0, (card.Width - 24.0 - gap) / 2.0);
        DrawRecoverySourceTrack(dc, _lowRecoveryStrength, "STRENGTH",
            value => $"+{value:0.##} STAMINA/s",
            new Rect(card.Left + 12.0, card.Top + 33.0, half, 41.0),
            text, muted, border, accent, panel);
        DrawRecoverySourceTrack(dc, _lowRecoveryDuration, "DURATION",
            value => $"{value:0.0} s",
            new Rect(card.Left + 12.0 + half + gap, card.Top + 33.0, half, 41.0),
            text, muted, border, accent, panel);
    }

    // A source-native rail without redundant per-control frames. Every
    // numerical step, muted reference tick and editor hit target remains
    // directly backed by one unmodified ParameterViewModel and CFG mapping.
    private void DrawRecoverySourceTrack(
        DrawingContext dc, ParameterViewModel? parameter,
        string label, Func<double, string> formatter, Rect area,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        if (parameter is null)
            return;

        var current = CreateSourceValueText(parameter,
            formatter(parameter.CurrentNumber), 11.2, text, accent);
        var title = CreateText(label, 9.6, text, FontWeights.Bold);
        title.MaxTextWidth = Math.Max(1.0,
            area.Width - current.WidthIncludingTrailingWhitespace - 9.0);
        title.MaxLineCount = 1;
        title.Trimming = TextTrimming.CharacterEllipsis;
        dc.DrawText(title, new Point(area.Left, area.Top));
        dc.DrawText(current,
            new Point(area.Right - current.WidthIncludingTrailingWhitespace, area.Top - 2.0));

        var track = new Rect(area.Left, area.Top + 21.0,
            Math.Max(1.0, area.Width), 8.0);
        var y = track.Top + track.Height / 2.0;
        var nativeMin = parameter.Minimum;
        var nativeMax = parameter.Maximum;
        var currentX = ValueToX(parameter.CurrentNumber, nativeMin, nativeMax, track);
        var refX = ValueToX(parameter.ReferenceNumber, nativeMin, nativeMax, track);
        dc.DrawLine(new Pen(border, 3.5),
            new Point(track.Left, y), new Point(track.Right, y));
        dc.DrawLine(new Pen(accent, 3.3),
            new Point(track.Left, y), new Point(currentX, y));
        dc.DrawLine(new Pen(muted, 1.6),
            new Point(refX, y - 7.0), new Point(refX, y + 7.0));
        DrawHandle(dc, parameter, new Point(currentX, y), panel, accent);

        var minText = CreateText(formatter(nativeMin), 9.0, muted, FontWeights.Normal);
        var maxText = CreateText(formatter(nativeMax), 9.0, muted, FontWeights.Normal);
        var half = Math.Max(1.0, (track.Width - 10.0) / 2.0);
        minText.MaxTextWidth = half;
        maxText.MaxTextWidth = half;
        minText.MaxLineCount = 1;
        maxText.MaxLineCount = 1;
        dc.DrawText(minText, new Point(track.Left, area.Top + 30.0));
        dc.DrawText(maxText,
            new Point(track.Right - maxText.WidthIncludingTrailingWhitespace,
                area.Top + 30.0));

        AddTarget(parameter,
            new Rect(track.Left - 5.0, track.Top - 6.0,
                track.Width + 10.0, track.Height + 12.0),
            track, MappingKind.Absolute, nativeMin, nativeMax);
    }

    // Only the numeric token (including its +/-/%/x) becomes gold when changed.
    // STAMINA, STAMINA/s, s and other static units remain neutral.
    private void DrawSourceValue(
        DrawingContext dc, ParameterViewModel parameter, string value,
        Point right, double size, Brush text, Brush accent)
    {
        var formatted = CreateSourceValueText(parameter, value, size, text, accent);
        dc.DrawText(formatted,
            new Point(right.X - formatted.WidthIncludingTrailingWhitespace, right.Y));
    }

    private FormattedText CreateSourceValueText(
        ParameterViewModel parameter, string value, double size, Brush text, Brush accent)
    {
        var formatted = CreateText(value, size, text, FontWeights.Bold);
        if (parameter.IsValueModifiedFromReference())
        {
            foreach (System.Text.RegularExpressions.Match token in
                System.Text.RegularExpressions.Regex.Matches(
                    value, @"[+\-−]?\d+(?:\.\d+)?(?:[%x×])?"))
            {
                formatted.SetForegroundBrush(accent, token.Index, token.Length);
                formatted.SetFontSize(size + 1.2, token.Index, token.Length);
            }
        }
        return formatted;
    }

    private void DrawHandle(DrawingContext dc, ParameterViewModel parameter, Point point, Brush panel, Brush accent)
    {
        dc.DrawEllipse(
            panel,
            new Pen(accent, IsSelected(parameter) || ReferenceEquals(_hoverParameter, parameter) ? 2.4 : 1.4),
            point,
            5.5,
            5.5);
        ControlHoverGlow.Draw(dc, point, accent,
            ReferenceEquals(_hoverParameter, parameter));
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

            _status.ShowTransient(IsExhaustion
                ? "Exhaustion & Recovery reset to Base Preset."
                : "Stamina Usage reset to Base Preset.");
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
