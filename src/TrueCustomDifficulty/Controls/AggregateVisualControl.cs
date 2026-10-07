using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Reusable non-radar aggregate editor. Different Family values render the same backing
/// ParameterViewModels as curves, faders, dashboards, ladders, timelines, bands,
/// balance meters or distributions without changing preset/writer semantics.
/// </summary>
public sealed class AggregateVisualControl : FrameworkElement
{
    private const double Epsilon = 0.000001;

    private readonly Dictionary<string, ParameterViewModel> _parameters = new(StringComparer.Ordinal);
    private readonly List<HitTarget> _hitTargets = new();
    private readonly RadarStatusMessage _status;

    private HitTarget? _dragTarget;
    private ParameterViewModel? _selectedParameter;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(AggregateVisualControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnBindingChanged));

    public static readonly DependencyProperty FamilyProperty = DependencyProperty.Register(
        nameof(Family),
        typeof(string),
        typeof(AggregateVisualControl),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure, OnBindingChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public string Family
    {
        get => (string)GetValue(FamilyProperty);
        set => SetValue(FamilyProperty, value);
    }

    public AggregateVisualControl()
    {
        _status = new RadarStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = 320.0;
        SnapsToDevicePixels = true;
        Cursor = Cursors.Arrow;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 760.0 : Math.Max(520.0, availableSize.Width);
        return new Size(width, GetDesiredHeight(Family));
    }

    private static void OnBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AggregateVisualControl)d).RebindParameters();

    private void RebindParameters()
    {
        foreach (var parameter in _parameters.Values)
            parameter.PropertyChanged -= Parameter_PropertyChanged;

        _parameters.Clear();

        var familyIds = GetFamilyIds(Family);
        if (Parameters is not null && familyIds.Count > 0)
        {
            var wanted = new HashSet<string>(familyIds, StringComparer.Ordinal);

            foreach (var parameter in Parameters.OfType<ParameterViewModel>())
            {
                if (!wanted.Contains(parameter.Id))
                    continue;

                _parameters[parameter.Id] = parameter;
                parameter.PropertyChanged += Parameter_PropertyChanged;
            }
        }

        _dragTarget = null;
        _selectedParameter = null;
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void Parameter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ParameterViewModel.CurrentNumber)
            or nameof(ParameterViewModel.CurrentBoolean)
            or nameof(ParameterViewModel.CurrentDisplay)
            or nameof(ParameterViewModel.IsModified)
            or nameof(ParameterViewModel.IsControlEnabled))
        {
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        _hitTargets.Clear();

        var text = FindBrush("TextBrush", Brushes.White);
        var muted = FindBrush("RadarMutedTextBrush", Brushes.Gray);
        var border = FindBrush("BorderBrush", Brushes.DimGray);
        var soft = FindBrush("BorderSoftBrush", border);
        var accent = FindBrush("AccentBrush", Brushes.Goldenrod);
        var panel = FindBrush("PanelAltBrush", Brushes.Black);

        IndustrialChrome.DrawFrame(
            dc,
            new Rect(2, 2, Math.Max(0.0, ActualWidth - 4.0), Math.Max(0.0, ActualHeight - 4.0)),
            panel,
            border,
            accent,
            soft,
            8.0,
            accentEdge: true,
            technicalMarks: true);

        switch (Family)
        {
            case "FatigueCurve":
                DrawFatigueCurve(dc, text, muted, border, soft, accent, panel);
                break;
            case "BleedingTreatmentMixer":
                DrawBleedingTreatmentMixer(dc, text, muted, border, accent, panel);
                break;
            case "ConsumableMixer":
                DrawConsumableMixer(dc, text, muted, border, accent, panel);
                break;
            case "SleepingDashboard":
                DrawSleepingDashboard(dc, text, muted, border, accent, panel);
                break;
            case "HungerLadder":
                DrawHungerLadder(dc, text, muted, border, soft, accent, panel);
                break;
            case "StarvationTimeline":
                DrawStarvationTimeline(dc, text, muted, border, soft, accent, panel);
                break;
            case "RepeatedHitBand":
                DrawRepeatedHitBand(dc, text, muted, border, soft, accent, panel);
                break;
            case "RadiationBalance":
                DrawRadiationBalance(dc, text, muted, border, soft, accent, panel);
                break;
            case "TradingDashboard":
                DrawTradingDashboard(dc, text, muted, border, accent, panel);
                break;
            case "RepairCurve":
                DrawRepairCurve(dc, text, muted, border, soft, accent, panel);
                break;
            case "AmmoDistribution":
                DrawAmmoDistribution(dc, text, muted, border, soft, accent, panel);
                break;
            default:
                DrawHeader(dc, "VISUAL CONTROL", "No visual family is configured.", text, muted, accent);
                break;
        }

        if (!string.IsNullOrWhiteSpace(_status.Text))
        {
            DrawCenteredText(
                dc,
                _status.Text,
                new Point(ActualWidth / 2.0, Math.Max(18.0, ActualHeight - 21.0)),
                9.0,
                accent,
                FontWeights.SemiBold);
        }
    }

    private void DrawFatigueCurve(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "FATIGUE RESPONSE", "build-up + escalating aim / reload penalties", text, muted, accent);

        var gap = 10.0;
        var left = 16.0;
        var top = 52.0;
        var width = Math.Max(420.0, ActualWidth - 32.0);
        var cardWidth = (width - gap) / 2.0;

        DrawHorizontalTrackCard(
            dc,
            P("survival.sleepinessRate"),
            "FATIGUE BUILD-UP",
            new Rect(left, top, cardWidth, 72.0),
            text,
            muted,
            border,
            accent,
            panel);

        DrawHorizontalTrackCard(
            dc,
            P("survival.energyDrinkFatigueRelief"),
            "ENERGY DRINK RELIEF",
            new Rect(left + cardWidth + gap, top, cardWidth, 72.0),
            text,
            muted,
            border,
            accent,
            panel);

        var plot = new Rect(58.0, 158.0, Math.Max(360.0, ActualWidth - 82.0), 178.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), plot, 4.0, 4.0);

        const double yMaximum = 0.8;
        for (var i = 0; i <= 4; i++)
        {
            var value = i * 0.2;
            var y = ValueToY(value, 0.0, yMaximum, plot);
            var gridPen = new Pen(i == 0 ? border : soft, 1.0);
            if (i > 0)
                gridPen.DashStyle = DashStyles.Dot;

            dc.DrawLine(gridPen, new Point(plot.Left, y), new Point(plot.Right, y));
            DrawRightAlignedText(dc, $"{value * 100:0}%", new Point(plot.Left - 8.0, y - 7.0), 8.0, muted, FontWeights.SemiBold);
        }

        var xs = new[]
        {
            plot.Left + plot.Width * 0.16,
            plot.Left + plot.Width * 0.50,
            plot.Left + plot.Width * 0.84
        };

        var aim = new[]
        {
            P("survival.fatigueLowAimPenalty"),
            P("survival.fatigueMediumAimPenalty"),
            P("survival.fatigueStrongAimPenalty")
        };

        var reload = new[]
        {
            (ParameterViewModel?)null,
            P("survival.fatigueMediumReloadPenalty"),
            P("survival.fatigueStrongReloadPenalty")
        };

        var aimCurrent = aim.Select(x => x?.CurrentNumber ?? 0.0).ToArray();
        var aimVanilla = aim.Select(x => x?.ReferenceNumber ?? 0.0).ToArray();
        var reloadCurrent = new[] { 0.0, reload[1]?.CurrentNumber ?? 0.0, reload[2]?.CurrentNumber ?? 0.0 };
        var reloadVanilla = new[] { 0.0, reload[1]?.ReferenceNumber ?? 0.0, reload[2]?.ReferenceNumber ?? 0.0 };

        DrawCurveLine(dc, xs, aimVanilla, plot, 0.0, yMaximum, new Pen(muted, 1.2) { DashStyle = DashStyles.Dash });
        DrawCurveLine(dc, xs, reloadVanilla, plot, 0.0, yMaximum, new Pen(soft, 1.2) { DashStyle = DashStyles.Dash });
        DrawCurveLine(dc, xs, aimCurrent, plot, 0.0, yMaximum, new Pen(accent, 2.2));
        DrawCurveLine(dc, xs, reloadCurrent, plot, 0.0, yMaximum, new Pen(text, 1.7));

        DrawText(dc, "AIM", new Point(plot.Left + 8.0, plot.Top + 8.0), 8.5, accent, FontWeights.Bold);
        DrawText(dc, "RELOAD", new Point(plot.Left + 46.0, plot.Top + 8.0), 8.5, text, FontWeights.Bold);

        var stageLabels = new[] { "LOW", "MEDIUM", "SEVERE" };

        for (var i = 0; i < 3; i++)
        {
            var aimPoint = new Point(xs[i], ValueToY(aimCurrent[i], 0.0, yMaximum, plot));
            dc.DrawEllipse(panel, new Pen(accent, IsSelected(aim[i]) ? 2.4 : 1.6), aimPoint, 6.0, 6.0);
            dc.DrawEllipse(accent, null, aimPoint, 2.8, 2.8);

            if (aim[i] is { } aimParameter)
                AddVerticalPointTarget(aimParameter, aimPoint, plot, 0.0, yMaximum);

            if (i > 0)
            {
                var reloadPoint = new Point(xs[i], ValueToY(reloadCurrent[i], 0.0, yMaximum, plot));
                dc.DrawRectangle(panel, new Pen(text, IsSelected(reload[i]) ? 2.2 : 1.4), new Rect(reloadPoint.X - 5.0, reloadPoint.Y - 5.0, 10.0, 10.0));

                if (reload[i] is { } reloadParameter)
                    AddVerticalPointTarget(reloadParameter, reloadPoint, plot, 0.0, yMaximum);
            }

            DrawCenteredText(dc, stageLabels[i], new Point(xs[i], plot.Bottom + 10.0), 9.0, text, FontWeights.Bold);

            var aimLabel = $"{aimCurrent[i] * 100:0}% AIM";
            var reloadLabel = i == 0 ? "0% RELOAD" : $"{reloadCurrent[i] * 100:0.#}% RELOAD";
            DrawCenteredText(dc, aimLabel, new Point(xs[i], plot.Bottom + 27.0), 8.1, accent, FontWeights.SemiBold);
            DrawCenteredText(dc, reloadLabel, new Point(xs[i], plot.Bottom + 41.0), 7.8, muted, FontWeights.Normal);
        }
    }

    private void DrawRepairCurve(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "REPAIR PRICE BY REPUTATION", "shape the full reputation curve instead of four separate sliders", text, muted, accent);

        var plot = new Rect(58.0, 78.0, Math.Max(360.0, ActualWidth - 82.0), 190.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), plot, 4.0, 4.0);

        const double maximum = 3.0;
        for (var i = 0; i <= 3; i++)
        {
            var y = ValueToY(i, 0.0, maximum, plot);
            var pen = new Pen(i == 0 ? border : soft, 1.0);
            if (i > 0)
                pen.DashStyle = DashStyles.Dot;

            dc.DrawLine(pen, new Point(plot.Left, y), new Point(plot.Right, y));
            DrawRightAlignedText(dc, $"{i}x", new Point(plot.Left - 8.0, y - 7.0), 8.3, muted, FontWeights.SemiBold);
        }

        var parameters = new[]
        {
            P("economy.repairEnemy"),
            P("economy.repairDisaffection"),
            P("economy.repairNeutral"),
            P("economy.repairFriend")
        };

        var labels = new[] { "HOSTILE", "DISLIKED", "NEUTRAL", "FRIENDLY" };
        var xs = new[]
        {
            plot.Left + plot.Width * 0.10,
            plot.Left + plot.Width * 0.37,
            plot.Left + plot.Width * 0.64,
            plot.Left + plot.Width * 0.90
        };

        var current = parameters.Select(x => x?.CurrentNumber ?? 0.0).ToArray();
        var vanilla = parameters.Select(x => x?.ReferenceNumber ?? 0.0).ToArray();

        DrawCurveLine(dc, xs, vanilla, plot, 0.0, maximum, new Pen(muted, 1.4) { DashStyle = DashStyles.Dash });
        DrawCurveLine(dc, xs, current, plot, 0.0, maximum, new Pen(accent, 2.3));

        for (var i = 0; i < parameters.Length; i++)
        {
            var point = new Point(xs[i], ValueToY(current[i], 0.0, maximum, plot));
            dc.DrawEllipse(panel, new Pen(accent, IsSelected(parameters[i]) ? 2.5 : 1.6), point, 6.5, 6.5);
            dc.DrawEllipse(accent, null, point, 3.0, 3.0);

            if (parameters[i] is { } parameter)
            {
                var columnHit = new Rect(xs[i] - 42.0, plot.Top, 84.0, plot.Height);
                AddVerticalTarget(parameter, columnHit, plot, 0.0, maximum);
            }

            DrawCenteredText(dc, labels[i], new Point(xs[i], plot.Bottom + 12.0), 9.0, text, FontWeights.Bold);
            DrawCenteredText(dc, $"{current[i]:0.##}x", new Point(xs[i], plot.Bottom + 29.0), 9.0, accent, FontWeights.SemiBold);
        }

        DrawText(dc, "VANILLA", new Point(plot.Right - 56.0, plot.Top + 8.0), 8.0, muted, FontWeights.SemiBold);
    }

    private void DrawBleedingTreatmentMixer(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "BLEEDING TREATMENT MIXER", "compare treatment strength at a glance", text, muted, accent);

        var parameters = new[]
        {
            P("health.bandageBleeding"),
            P("health.medkitBleeding"),
            P("health.armyMedkitBleeding"),
            P("health.ecoMedkitBleeding")
        };
        var labels = new[] { "BANDAGE", "MEDKIT", "ARMY", "SCIENTIFIC" };

        DrawFaderBank(dc, parameters, labels, new Rect(24.0, 62.0, Math.Max(440.0, ActualWidth - 48.0), 270.0), text, muted, border, accent, panel, showRawAndMultiplier: true);
        DrawCenteredText(dc, "VANILLA MARKER = 1x", new Point(ActualWidth / 2.0, 350.0), 8.5, muted, FontWeights.SemiBold);
    }

    private void DrawConsumableMixer(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "CONSUMABLE EFFECTS MIXER", "effect strength above, duration faders below", text, muted, accent);

        var left = 16.0;
        var top = 52.0;
        var gap = 10.0;
        var width = Math.Max(420.0, ActualWidth - 32.0);
        var cardWidth = (width - gap) / 2.0;

        DrawHorizontalTrackCard(
            dc,
            P("survival.instantStaminaRestore"),
            "INSTANT STAMINA RESTORE",
            new Rect(left, top, cardWidth, 72.0),
            text,
            muted,
            border,
            accent,
            panel);

        DrawHorizontalTrackCard(
            dc,
            P("survival.alcoholRadiationRemoval"),
            "ALCOHOL RAD REMOVAL",
            new Rect(left + cardWidth + gap, top, cardWidth, 72.0),
            text,
            muted,
            border,
            accent,
            panel);

        var durationParameters = new[]
        {
            P("survival.waterEffectDuration"),
            P("survival.energyDrinkEffectDuration"),
            P("survival.vodkaEffectDuration"),
            P("survival.beerEffectDuration"),
            P("survival.magicVodkaEffectDuration")
        };
        var durationLabels = new[] { "WATER", "ENERGY", "VODKA", "BEER", "MAGIC" };

        DrawText(dc, "EFFECT DURATION", new Point(18.0, 143.0), 9.0, muted, FontWeights.Bold);
        DrawFaderBank(dc, durationParameters, durationLabels, new Rect(24.0, 160.0, Math.Max(440.0, ActualWidth - 48.0), 220.0), text, muted, border, accent, panel, showRawAndMultiplier: false);
    }

    private void DrawSleepingDashboard(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "SLEEPING SYSTEM", "when sleep becomes available and what it restores", text, muted, accent);

        var left = 16.0;
        var width = Math.Max(420.0, ActualWidth - 32.0);
        var gap = 10.0;
        var cardWidth = (width - gap) / 2.0;

        DrawHorizontalTrackCard(dc, P("survival.sleepEligibilityThreshold"), "SLEEP AVAILABLE AT", new Rect(left, 58.0, cardWidth, 88.0), text, muted, border, accent, panel);
        DrawHorizontalTrackCard(dc, P("survival.minimumSleepHours"), "MINIMUM SLEEP", new Rect(left + cardWidth + gap, 58.0, cardWidth, 88.0), text, muted, border, accent, panel);
        DrawHorizontalTrackCard(dc, P("survival.sleepHealthRestore"), "HEALTH RESTORED", new Rect(left, 164.0, cardWidth, 88.0), text, muted, border, accent, panel);
        DrawHorizontalTrackCard(dc, P("survival.sleepStaminaRestore"), "STAMINA RESTORED", new Rect(left + cardWidth + gap, 164.0, cardWidth, 88.0), text, muted, border, accent, panel);

        DrawCenteredText(dc, "ONE SLEEP CYCLE", new Point(ActualWidth / 2.0, 278.0), 8.5, muted, FontWeights.SemiBold);
    }

    private void DrawTradingDashboard(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "TRADING SYSTEM", "pricing, accepted condition and broken-item value", text, muted, accent);

        var left = 16.0;
        var width = Math.Max(420.0, ActualWidth - 32.0);
        var gap = 10.0;
        var cardWidth = (width - gap) / 2.0;

        DrawHorizontalTrackCard(dc, P("economy.buyCondition"), "TRADER BUYS FROM YOU", new Rect(left, 58.0, cardWidth, 88.0), text, muted, border, accent, panel);
        DrawHorizontalTrackCard(dc, P("economy.sellCondition"), "TRADER SELLS TO YOU", new Rect(left + cardWidth + gap, 58.0, cardWidth, 88.0), text, muted, border, accent, panel);
        DrawHorizontalTrackCard(dc, P("economy.traderMinBuyCondition"), "MINIMUM BUY CONDITION", new Rect(left, 164.0, cardWidth, 88.0), text, muted, border, accent, panel);
        DrawHorizontalTrackCard(dc, P("economy.itemValueFloor"), "VALUE AT 0% DURABILITY", new Rect(left + cardWidth + gap, 164.0, cardWidth, 88.0), text, muted, border, accent, panel);

        DrawCenteredText(dc, "BUY / SELL ARE PRICE SCALERS", new Point(ActualWidth / 2.0, 278.0), 8.4, muted, FontWeights.SemiBold);
    }

    private void DrawHungerLadder(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "HUNGER STATE LADDER", "build-up feeds into progressively harsher stamina states", text, muted, accent);

        DrawHorizontalTrackCard(
            dc,
            P("survival.hungerRate"),
            "HUNGER BUILD-UP",
            new Rect(16.0, 52.0, Math.Max(420.0, ActualWidth - 32.0), 72.0),
            text,
            muted,
            border,
            accent,
            panel);

        var ladderX = 42.0;
        var topY = 168.0;
        var rowGap = 66.0;
        dc.DrawLine(new Pen(soft, 2.0), new Point(ladderX, topY), new Point(ladderX, topY + rowGap * 2.0));

        var stages = new[]
        {
            ("WELL FED", "stamina recovery bonus", P("survival.wellFedStaminaRegen")),
            ("HUNGRY", "stamina recovery penalty", P("survival.hungerMediumStaminaRegen")),
            ("SEVERE", "stamina recovery penalty", P("survival.hungerStrongStaminaRegen"))
        };

        for (var i = 0; i < stages.Length; i++)
        {
            var y = topY + i * rowGap;
            dc.DrawEllipse(panel, new Pen(i == 0 ? accent : border, 2.0), new Point(ladderX, y), 6.0, 6.0);

            if (i < stages.Length - 1)
            {
                var arrowY = y + rowGap / 2.0;
                dc.DrawLine(new Pen(soft, 1.0), new Point(ladderX - 4.0, arrowY - 3.0), new Point(ladderX, arrowY + 2.0));
                dc.DrawLine(new Pen(soft, 1.0), new Point(ladderX + 4.0, arrowY - 3.0), new Point(ladderX, arrowY + 2.0));
            }

            DrawText(dc, stages[i].Item1, new Point(62.0, y - 17.0), 9.4, i == 0 ? accent : text, FontWeights.Bold);
            DrawText(dc, stages[i].Item2, new Point(62.0, y - 3.0), 8.0, muted, FontWeights.Normal);

            if (stages[i].Item3 is { } parameter)
            {
                DrawInlineHorizontalTrack(
                    dc,
                    parameter,
                    new Rect(250.0, y - 10.0, Math.Max(190.0, ActualWidth - 278.0), 26.0),
                    text,
                    muted,
                    border,
                    accent);
            }
        }
    }

    private void DrawStarvationTimeline(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "STARVATION TIMELINE", "onset, damage cadence and active penalties", text, muted, accent);

        var toggle = P("advanced.starvationMechanics");
        DrawTogglePill(dc, toggle, "MASTER STARVATION PENALTIES", new Rect(Math.Max(250.0, ActualWidth - 250.0), 43.0, 222.0, 34.0), text, muted, border, accent, panel);

        var delay = P("survival.starvationPenaltyDelay");
        var interval = P("survival.starvationDamageInterval");

        var lineLeft = 38.0;
        var lineRight = Math.Max(lineLeft + 380.0, ActualWidth - 30.0);
        var lineY = 119.0;
        var totalWidth = lineRight - lineLeft;
        var delayWidth = totalWidth * 0.34;
        var intervalWidth = totalWidth - delayWidth - 14.0;
        var intervalLeft = lineLeft + delayWidth + 14.0;

        DrawText(dc, "STARVING", new Point(lineLeft, 88.0), 8.5, muted, FontWeights.Bold);

        dc.DrawLine(new Pen(border, 3.0), new Point(lineLeft, lineY), new Point(lineLeft + delayWidth, lineY));
        dc.DrawLine(new Pen(border, 3.0), new Point(intervalLeft, lineY), new Point(lineRight, lineY));

        if (delay is not null)
        {
            var delayMap = new Rect(lineLeft, lineY - 13.0, delayWidth, 26.0);
            var delayX = ValueToX(delay.CurrentNumber, delay.Minimum, delay.Maximum, delayMap, reverse: false);
            var vanillaX = ValueToX(delay.ReferenceNumber, delay.Minimum, delay.Maximum, delayMap, reverse: false);

            dc.DrawLine(new Pen(muted, 1.0), new Point(vanillaX, lineY - 6.0), new Point(vanillaX, lineY + 6.0));
            dc.DrawEllipse(accent, null, new Point(delayX, lineY), 6.0, 6.0);
            DrawCenteredText(dc, "PENALTIES BEGIN", new Point(delayX, lineY + 17.0), 8.0, text, FontWeights.Bold);
            DrawCenteredText(dc, FriendlyValue(delay), new Point(delayX, lineY + 31.0), 8.6, accent, FontWeights.SemiBold);

            AddHorizontalTarget(delay, delayMap, delayMap, delay.Minimum, delay.Maximum);
        }

        if (interval is not null)
        {
            var intervalMap = new Rect(intervalLeft, lineY - 13.0, intervalWidth, 26.0);
            var intervalX = ValueToX(interval.CurrentNumber, interval.Minimum, interval.Maximum, intervalMap, reverse: false);
            var vanillaX = ValueToX(interval.ReferenceNumber, interval.Minimum, interval.Maximum, intervalMap, reverse: false);

            dc.DrawLine(new Pen(muted, 1.0), new Point(vanillaX, lineY - 6.0), new Point(vanillaX, lineY + 6.0));
            dc.DrawEllipse(accent, null, new Point(intervalX, lineY), 6.0, 6.0);
            DrawCenteredText(dc, "DAMAGE EVERY", new Point(intervalX, lineY + 17.0), 8.0, text, FontWeights.Bold);
            DrawCenteredText(dc, FriendlyValue(interval), new Point(intervalX, lineY + 31.0), 8.6, accent, FontWeights.SemiBold);

            AddHorizontalTarget(interval, intervalMap, intervalMap, interval.Minimum, interval.Maximum);
        }

        DrawText(dc, "ACTIVE WHILE STARVING", new Point(18.0, 176.0), 8.8, muted, FontWeights.Bold);

        var left = 16.0;
        var width = Math.Max(420.0, ActualWidth - 32.0);
        var gap = 10.0;
        var half = (width - gap) / 2.0;

        DrawHorizontalTrackCard(dc, P("survival.starvationStaminaDrain"), "STAMINA DRAIN", new Rect(left, 194.0, half, 76.0), text, muted, border, accent, panel);
        DrawHorizontalTrackCard(dc, P("survival.starvationDamage"), "DAMAGE PER TICK", new Rect(left + half + gap, 194.0, half, 76.0), text, muted, border, accent, panel);
        DrawHorizontalTrackCard(dc, P("survival.starvationHealthRegenPenalty"), "HEALTH RECOVERY LOSS", new Rect(left, 284.0, width, 76.0), text, muted, border, accent, panel);
    }

    private void DrawRepeatedHitBand(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "REPEATED-HIT PROTECTION BAND", "minimum and maximum protection as one editable range", text, muted, accent);

        var minimum = P("combat.accumulatedDamageWeightMin");
        var maximum = P("combat.accumulatedDamageWeightMax");
        var track = new Rect(52.0, 105.0, Math.Max(360.0, ActualWidth - 104.0), 34.0);
        var trackY = track.Top + track.Height / 2.0;

        dc.DrawLine(new Pen(border, 4.0), new Point(track.Left, trackY), new Point(track.Right, trackY));

        if (minimum is not null && maximum is not null)
        {
            var minX = ValueToX(minimum.CurrentNumber, 0.0, 1.0, track, reverse: false);
            var maxX = ValueToX(maximum.CurrentNumber, 0.0, 1.0, track, reverse: false);
            var bandLeft = Math.Min(minX, maxX);
            var bandRight = Math.Max(minX, maxX);

            var bandBrush = CloneWithOpacity(accent, 0.25);
            dc.DrawRectangle(bandBrush, null, new Rect(bandLeft, trackY - 7.0, Math.Max(1.0, bandRight - bandLeft), 14.0));
            dc.DrawLine(new Pen(accent, 3.0), new Point(bandLeft, trackY), new Point(bandRight, trackY));

            DrawReferenceTick(dc, minimum, track, trackY, muted);
            DrawReferenceTick(dc, maximum, track, trackY, muted);

            dc.DrawEllipse(panel, new Pen(accent, IsSelected(minimum) ? 2.5 : 1.6), new Point(minX, trackY), 7.0, 7.0);
            dc.DrawEllipse(accent, null, new Point(minX, trackY), 3.0, 3.0);

            dc.DrawEllipse(panel, new Pen(accent, IsSelected(maximum) ? 2.5 : 1.6), new Point(maxX, trackY), 7.0, 7.0);
            dc.DrawEllipse(accent, null, new Point(maxX, trackY), 3.0, 3.0);

            AddHorizontalTarget(minimum, new Rect(minX - 17.0, trackY - 19.0, 34.0, 38.0), track, 0.0, 1.0);
            AddHorizontalTarget(maximum, new Rect(maxX - 17.0, trackY - 19.0, 34.0, 38.0), track, 0.0, 1.0);

            DrawCenteredText(dc, $"MIN  {minimum.CurrentNumber * 100:0}%", new Point(minX, track.Bottom + 12.0), 9.0, text, FontWeights.Bold);
            DrawCenteredText(dc, $"MAX  {maximum.CurrentNumber * 100:0}%", new Point(maxX, track.Bottom + 30.0), 9.0, accent, FontWeights.Bold);
        }

        DrawText(dc, "0%", new Point(track.Left, trackY + 43.0), 8.0, muted, FontWeights.SemiBold);
        DrawRightAlignedText(dc, "100%", new Point(track.Right, trackY + 43.0), 8.0, muted, FontWeights.SemiBold);

        DrawTogglePill(
            dc,
            P("combat.accumulatedDamageIncludesHealedHealth"),
            "HEALED DAMAGE STILL COUNTS",
            new Rect(52.0, 208.0, Math.Max(300.0, ActualWidth - 104.0), 40.0),
            text,
            muted,
            border,
            accent,
            panel);
    }

    private void DrawRadiationBalance(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "RADIATION BALANCE", "natural recovery pushes left; radiation build-up pushes right", text, muted, accent);

        var recovery = P("survival.radiationRecovery");
        var buildup = P("survival.radiationAccumulation");

        var centerX = ActualWidth / 2.0;
        var y = 112.0;
        var outerLeft = 42.0;
        var outerRight = Math.Max(centerX + 170.0, ActualWidth - 42.0);
        var gap = 18.0;

        var leftMap = new Rect(outerLeft, y - 15.0, Math.Max(140.0, centerX - gap - outerLeft), 30.0);
        var rightMap = new Rect(centerX + gap, y - 15.0, Math.Max(140.0, outerRight - (centerX + gap)), 30.0);

        dc.DrawLine(new Pen(border, 4.0), new Point(leftMap.Left, y), new Point(leftMap.Right, y));
        dc.DrawLine(new Pen(border, 4.0), new Point(rightMap.Left, y), new Point(rightMap.Right, y));
        dc.DrawLine(new Pen(soft, 1.0), new Point(centerX, y - 26.0), new Point(centerX, y + 26.0));

        if (recovery is not null)
        {
            var x = ValueToX(recovery.CurrentNumber, recovery.Minimum, recovery.Maximum, leftMap, reverse: true);
            dc.DrawLine(new Pen(accent, 3.0), new Point(x, y), new Point(leftMap.Right, y));
            DrawReferenceTick(dc, recovery, leftMap, y, muted, reverse: true);
            dc.DrawEllipse(accent, null, new Point(x, y), 6.0, 6.0);
            AddHorizontalTarget(recovery, leftMap, leftMap, recovery.Minimum, recovery.Maximum, reverse: true);

            DrawText(dc, "RECOVERY", new Point(leftMap.Left, 70.0), 9.0, text, FontWeights.Bold);
            DrawText(dc, FriendlyValue(recovery), new Point(leftMap.Left, 86.0), 9.2, accent, FontWeights.SemiBold);
        }

        if (buildup is not null)
        {
            var x = ValueToX(buildup.CurrentNumber, buildup.Minimum, buildup.Maximum, rightMap, reverse: false);
            dc.DrawLine(new Pen(accent, 3.0), new Point(rightMap.Left, y), new Point(x, y));
            DrawReferenceTick(dc, buildup, rightMap, y, muted);
            dc.DrawEllipse(accent, null, new Point(x, y), 6.0, 6.0);
            AddHorizontalTarget(buildup, rightMap, rightMap, buildup.Minimum, buildup.Maximum);

            DrawRightAlignedText(dc, "BUILD-UP", new Point(rightMap.Right, 70.0), 9.0, text, FontWeights.Bold);
            DrawRightAlignedText(dc, FriendlyValue(buildup), new Point(rightMap.Right, 86.0), 9.2, accent, FontWeights.SemiBold);
        }

        DrawCenteredText(dc, "RECOVERY  ◄   BALANCE   ►  BUILD-UP", new Point(centerX, 145.0), 8.4, muted, FontWeights.SemiBold);

        DrawHorizontalTrackCard(
            dc,
            P("survival.radiationDamage"),
            "HEALTH DAMAGE WHILE IRRADIATED",
            new Rect(42.0, 184.0, Math.Max(360.0, ActualWidth - 84.0), 82.0),
            text,
            muted,
            border,
            accent,
            panel);
    }

    private void DrawAmmoDistribution(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "BREAKABLE BOX AMMO DISTRIBUTION", "stacked share preview + direct weight controls", text, muted, accent);

        var parameters = new[]
        {
            P("loot.ammoBoxWeight918"),
            P("loot.ammoBoxWeight045"),
            P("loot.ammoBoxWeight545"),
            P("loot.ammoBoxWeight012")
        };
        var labels = new[] { "9x18 / 9x19", ".45 ACP", "5.45 / 5.56", "12 GAUGE" };

        var weights = parameters.Select(x => Math.Max(0.0, x?.CurrentNumber ?? 0.0)).ToArray();
        var total = weights.Sum();
        var stack = new Rect(24.0, 70.0, Math.Max(430.0, ActualWidth - 48.0), 42.0);

        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), stack, 3.0, 3.0);

        if (total <= Epsilon)
        {
            DrawCenteredText(dc, "ALL WEIGHTS 0", new Point(stack.Left + stack.Width / 2.0, stack.Top + 12.0), 9.0, muted, FontWeights.Bold);
        }
        else
        {
            var opacities = new[] { 0.95, 0.72, 0.52, 0.32 };
            var cursor = stack.Left;

            for (var i = 0; i < weights.Length; i++)
            {
                var share = weights[i] / total;
                var segmentWidth = i == weights.Length - 1
                    ? stack.Right - cursor
                    : stack.Width * share;

                var segment = new Rect(cursor, stack.Top, Math.Max(0.0, segmentWidth), stack.Height);
                dc.DrawRectangle(CloneWithOpacity(accent, opacities[i]), null, segment);

                if (segment.Width >= 62.0)
                {
                    DrawCenteredText(dc, labels[i], new Point(segment.Left + segment.Width / 2.0, segment.Top + 8.0), 8.2, text, FontWeights.Bold);
                    DrawCenteredText(dc, $"{share * 100:0}%", new Point(segment.Left + segment.Width / 2.0, segment.Top + 23.0), 8.0, text, FontWeights.SemiBold);
                }

                cursor += segmentWidth;
            }
        }

        DrawText(dc, "RELATIVE WEIGHTS", new Point(24.0, 126.0), 8.6, muted, FontWeights.Bold);

        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i] is not { } parameter)
                continue;

            var rowY = 151.0 + i * 55.0;
            DrawText(dc, labels[i], new Point(26.0, rowY + 3.0), 9.0, text, FontWeights.Bold);
            DrawText(dc, $"{parameter.CurrentNumber:0.##}x", new Point(132.0, rowY + 3.0), 9.0, accent, FontWeights.Bold);

            DrawInlineHorizontalTrack(
                dc,
                parameter,
                new Rect(190.0, rowY - 2.0, Math.Max(240.0, ActualWidth - 218.0), 26.0),
                text,
                muted,
                border,
                accent);
        }

        DrawCenteredText(dc, "STACKED BAR IS A PREVIEW — WEIGHTS DO NOT NEED TO SUM TO 100%", new Point(ActualWidth / 2.0, 380.0), 8.0, muted, FontWeights.Normal);
    }

    private void DrawFaderBank(
        DrawingContext dc,
        IReadOnlyList<ParameterViewModel?> parameters,
        IReadOnlyList<string> labels,
        Rect area,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel,
        bool showRawAndMultiplier)
    {
        var count = Math.Min(parameters.Count, labels.Count);
        if (count == 0)
            return;

        var columnWidth = area.Width / count;

        for (var i = 0; i < count; i++)
        {
            if (parameters[i] is not { } parameter)
                continue;

            var centerX = area.Left + columnWidth * (i + 0.5);
            var trackTop = area.Top + 43.0;
            var trackBottom = area.Bottom - 48.0;
            var mapRect = new Rect(centerX - 14.0, trackTop, 28.0, Math.Max(30.0, trackBottom - trackTop));

            DrawCenteredText(dc, labels[i], new Point(centerX, area.Top + 3.0), 8.8, text, FontWeights.Bold);

            var valueText = showRawAndMultiplier && Math.Abs(parameter.ReferenceNumber) > Epsilon
                ? $"{parameter.CurrentNumber:0.##}  |  {parameter.CurrentNumber / parameter.ReferenceNumber:0.##}x"
                : FriendlyValue(parameter);

            DrawCenteredText(dc, valueText, new Point(centerX, area.Top + 20.0), 8.8, accent, FontWeights.SemiBold);

            dc.DrawLine(new Pen(border, 4.0), new Point(centerX, trackTop), new Point(centerX, trackBottom));

            var vanillaY = ValueToY(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, mapRect);
            dc.DrawLine(new Pen(muted, 1.2), new Point(centerX - 10.0, vanillaY), new Point(centerX + 10.0, vanillaY));

            var currentY = ValueToY(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, mapRect);
            var thumb = new Rect(centerX - 13.0, currentY - 5.0, 26.0, 10.0);
            dc.DrawRoundedRectangle(panel, new Pen(accent, IsSelected(parameter) ? 2.4 : 1.6), thumb, 2.0, 2.0);
            dc.DrawRectangle(accent, null, new Rect(centerX - 8.0, currentY - 1.0, 16.0, 2.0));

            AddVerticalTarget(
                parameter,
                new Rect(centerX - Math.Max(20.0, columnWidth * 0.35), trackTop - 8.0, Math.Max(40.0, columnWidth * 0.70), trackBottom - trackTop + 16.0),
                mapRect,
                parameter.Minimum,
                parameter.Maximum);

            DrawCenteredText(dc, FormatCompact(parameter.Minimum), new Point(centerX, trackBottom + 11.0), 7.8, muted, FontWeights.Normal);
            DrawCenteredText(dc, FormatCompact(parameter.Maximum), new Point(centerX, trackTop - 16.0), 7.8, muted, FontWeights.Normal);
        }
    }

    private void DrawHorizontalTrackCard(
        DrawingContext dc,
        ParameterViewModel? parameter,
        string label,
        Rect card,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), card, 3.0, 3.0);
        DrawText(dc, label, new Point(card.Left + 10.0, card.Top + 8.0), 8.8, muted, FontWeights.Bold);

        if (parameter is null)
            return;

        DrawRightAlignedText(dc, FriendlyValue(parameter), new Point(card.Right - 10.0, card.Top + 6.0), 10.2, accent, FontWeights.Bold);

        var track = new Rect(card.Left + 12.0, card.Top + 38.0, Math.Max(50.0, card.Width - 24.0), 28.0);
        var y = track.Top + track.Height / 2.0;

        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));
        DrawReferenceTick(dc, parameter, track, y, muted);

        var x = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track, reverse: false);
        dc.DrawEllipse(accent, null, new Point(x, y), 5.5, 5.5);

        AddHorizontalTarget(parameter, track, track, parameter.Minimum, parameter.Maximum);

        DrawText(dc, FormatCompact(parameter.Minimum), new Point(track.Left, card.Bottom - 14.0), 7.8, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, FormatCompact(parameter.Maximum), new Point(track.Right, card.Bottom - 14.0), 7.8, muted, FontWeights.Normal);
    }

    private void DrawInlineHorizontalTrack(
        DrawingContext dc,
        ParameterViewModel parameter,
        Rect track,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent)
    {
        var y = track.Top + track.Height / 2.0;
        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));
        DrawReferenceTick(dc, parameter, track, y, muted);

        var x = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track, reverse: false);
        dc.DrawEllipse(accent, null, new Point(x, y), 5.5, 5.5);

        AddHorizontalTarget(parameter, track, track, parameter.Minimum, parameter.Maximum);

        DrawRightAlignedText(dc, FriendlyValue(parameter), new Point(track.Right, track.Top - 13.0), 8.6, accent, FontWeights.SemiBold);
    }

    private void DrawTogglePill(
        DrawingContext dc,
        ParameterViewModel? parameter,
        string label,
        Rect rect,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        if (parameter is null)
            return;

        var active = parameter.CurrentBoolean;
        var fill = active ? CloneWithOpacity(accent, 0.18) : panel;
        dc.DrawRoundedRectangle(fill, new Pen(active ? accent : border, active ? 1.8 : 1.0), rect, 4.0, 4.0);

        DrawText(dc, label, new Point(rect.Left + 10.0, rect.Top + 10.0), 8.7, active ? text : muted, FontWeights.Bold);
        DrawRightAlignedText(dc, active ? "ON" : "OFF", new Point(rect.Right - 10.0, rect.Top + 9.0), 9.5, active ? accent : muted, FontWeights.Bold);

        AddToggleTarget(parameter, rect);
    }

    private void DrawHeader(
        DrawingContext dc,
        string title,
        string subtitle,
        Brush text,
        Brush muted,
        Brush accent)
    {
        DrawText(dc, title, new Point(16.0, 13.0), 10.5, accent, FontWeights.Bold);
        DrawText(dc, subtitle, new Point(16.0, 31.0), 8.5, muted, FontWeights.Normal);
    }

    private void DrawCurveLine(
        DrawingContext dc,
        IReadOnlyList<double> xs,
        IReadOnlyList<double> values,
        Rect plot,
        double minimum,
        double maximum,
        Pen pen)
    {
        var count = Math.Min(xs.Count, values.Count);
        for (var i = 1; i < count; i++)
        {
            dc.DrawLine(
                pen,
                new Point(xs[i - 1], ValueToY(values[i - 1], minimum, maximum, plot)),
                new Point(xs[i], ValueToY(values[i], minimum, maximum, plot)));
        }
    }

    private void DrawReferenceTick(
        DrawingContext dc,
        ParameterViewModel parameter,
        Rect mapRect,
        double y,
        Brush muted,
        bool reverse = false)
    {
        var x = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, mapRect, reverse);
        dc.DrawLine(new Pen(muted, 1.0), new Point(x, y - 6.0), new Point(x, y + 6.0));
    }

    private void AddHorizontalTarget(
        ParameterViewModel parameter,
        Rect hitRect,
        Rect mapRect,
        double visualMinimum,
        double visualMaximum,
        bool reverse = false)
    {
        if (!parameter.IsControlEnabled)
            return;

        _hitTargets.Add(new HitTarget(parameter, hitRect, mapRect, HitKind.Horizontal, visualMinimum, visualMaximum, reverse));
    }

    private void AddVerticalTarget(
        ParameterViewModel parameter,
        Rect hitRect,
        Rect mapRect,
        double visualMinimum,
        double visualMaximum)
    {
        if (!parameter.IsControlEnabled)
            return;

        _hitTargets.Add(new HitTarget(parameter, hitRect, mapRect, HitKind.Vertical, visualMinimum, visualMaximum, false));
    }

    private void AddVerticalPointTarget(
        ParameterViewModel parameter,
        Point point,
        Rect mapRect,
        double visualMinimum,
        double visualMaximum)
    {
        AddVerticalTarget(parameter, new Rect(point.X - 18.0, point.Y - 18.0, 36.0, 36.0), mapRect, visualMinimum, visualMaximum);
    }

    private void AddToggleTarget(ParameterViewModel parameter, Rect hitRect)
    {
        if (!parameter.IsControlEnabled)
            return;

        _hitTargets.Add(new HitTarget(parameter, hitRect, hitRect, HitKind.Toggle, 0.0, 1.0, false));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var point = e.GetPosition(this);
        var target = FindTarget(point);
        if (target is null)
            return;

        _selectedParameter = target.Parameter;

        if (target.Kind == HitKind.Toggle)
        {
            target.Parameter.CurrentBoolean = !target.Parameter.CurrentBoolean;
            _status.ClearTransient();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        _dragTarget = target;
        CaptureMouse();
        UpdateTarget(target, point);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var point = e.GetPosition(this);
        Cursor = FindTarget(point) is null ? Cursors.Arrow : Cursors.Hand;

        if (_dragTarget is null || e.LeftButton != MouseButtonState.Pressed)
            return;

        UpdateTarget(_dragTarget, point);
        e.Handled = true;
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
            foreach (var resetParameter in GetFamilyParameters())
                resetParameter.ResetToReference();

            _status.ShowTransient("Visual group reset to Base Preset.");
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_selectedParameter is null)
            return;

        if (_selectedParameter.IsToggle && e.Key == Key.Space)
        {
            _selectedParameter.CurrentBoolean = !_selectedParameter.CurrentBoolean;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (!_selectedParameter.IsSlider)
            return;

        var direction = e.Key switch
        {
            Key.Up or Key.Right or Key.Add or Key.OemPlus => 1,
            Key.Down or Key.Left or Key.Subtract or Key.OemMinus => -1,
            _ => 0
        };

        if (direction == 0)
            return;

        _selectedParameter.CurrentNumber = Math.Clamp(
            _selectedParameter.CurrentNumber + direction * _selectedParameter.TickFrequency,
            _selectedParameter.Minimum,
            _selectedParameter.Maximum);

        _status.ClearTransient();
        InvalidateVisual();
        e.Handled = true;
    }

    private void UpdateTarget(HitTarget target, Point point)
    {
        if (target.Kind == HitKind.Toggle)
            return;

        double ratio;

        if (target.Kind == HitKind.Horizontal)
        {
            ratio = Math.Clamp(
                (point.X - target.MapRect.Left) / Math.Max(1.0, target.MapRect.Width),
                0.0,
                1.0);

            if (target.Reverse)
                ratio = 1.0 - ratio;
        }
        else
        {
            ratio = Math.Clamp(
                (target.MapRect.Bottom - point.Y) / Math.Max(1.0, target.MapRect.Height),
                0.0,
                1.0);
        }

        var raw = target.VisualMinimum + ratio * (target.VisualMaximum - target.VisualMinimum);
        target.Parameter.CurrentNumber = Snap(target.Parameter, raw);
        _status.ClearTransient();
        InvalidateVisual();
    }

    private HitTarget? FindTarget(Point point)
    {
        for (var i = _hitTargets.Count - 1; i >= 0; i--)
        {
            if (_hitTargets[i].HitRect.Contains(point))
                return _hitTargets[i];
        }

        return null;
    }

    private IEnumerable<ParameterViewModel> GetFamilyParameters()
    {
        foreach (var id in GetFamilyIds(Family))
        {
            if (_parameters.TryGetValue(id, out var parameter))
                yield return parameter;
        }
    }

    private ParameterViewModel? P(string id)
        => _parameters.TryGetValue(id, out var parameter) ? parameter : null;

    private bool IsSelected(ParameterViewModel? parameter)
        => parameter is not null && ReferenceEquals(parameter, _selectedParameter);

    private static double Snap(ParameterViewModel parameter, double raw)
    {
        var step = Math.Max(0.0000001, parameter.TickFrequency);
        var snapped = parameter.Minimum +
                      Math.Round((raw - parameter.Minimum) / step, MidpointRounding.AwayFromZero) * step;

        return Math.Clamp(snapped, parameter.Minimum, parameter.Maximum);
    }

    private static double ValueToX(
        double value,
        double minimum,
        double maximum,
        Rect rect,
        bool reverse)
    {
        if (maximum <= minimum)
            return reverse ? rect.Right : rect.Left;

        var ratio = Math.Clamp((value - minimum) / (maximum - minimum), 0.0, 1.0);
        if (reverse)
            ratio = 1.0 - ratio;

        return rect.Left + rect.Width * ratio;
    }

    private static double ValueToY(double value, double minimum, double maximum, Rect rect)
    {
        if (maximum <= minimum)
            return rect.Bottom;

        var ratio = Math.Clamp((value - minimum) / (maximum - minimum), 0.0, 1.0);
        return rect.Bottom - rect.Height * ratio;
    }

    private static Brush CloneWithOpacity(Brush brush, double opacity)
    {
        var clone = brush.Clone();
        clone.Opacity = Math.Clamp(opacity, 0.0, 1.0);
        return clone;
    }

    private Brush FindBrush(string key, Brush fallback)
        => TryFindResource(key) as Brush ?? fallback;

    private FormattedText CreateText(string value, double size, Brush brush, FontWeight weight)
        => new(
            value,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            size,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private void DrawText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
        => dc.DrawText(CreateText(value, size, brush, weight), point);

    private void DrawRightAlignedText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
    {
        var formatted = CreateText(value, size, brush, weight);
        dc.DrawText(formatted, new Point(point.X - formatted.WidthIncludingTrailingWhitespace, point.Y));
    }

    private void DrawCenteredText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
    {
        var formatted = CreateText(value, size, brush, weight);
        dc.DrawText(formatted, new Point(point.X - formatted.WidthIncludingTrailingWhitespace / 2.0, point.Y));
    }

    private static string FriendlyValue(ParameterViewModel parameter)
    {
        return parameter.Id switch
        {
            "survival.sleepEligibilityThreshold" => $"{parameter.CurrentNumber:0} pts",
            "survival.minimumSleepHours" => $"{parameter.CurrentNumber:0} h",
            "survival.sleepHealthRestore" => $"{parameter.CurrentNumber:0} HP",
            "survival.sleepStaminaRestore" => $"{parameter.CurrentNumber * 100:0}%",

            "survival.hungerRate" => $"{parameter.CurrentNumber:0.000} pts/s",
            "survival.hungerMediumStaminaRegen" or
            "survival.hungerStrongStaminaRegen" or
            "survival.wellFedStaminaRegen" => $"{parameter.CurrentNumber * 100:0}%",

            "survival.starvationDamageInterval" or
            "survival.starvationPenaltyDelay" => $"{parameter.CurrentNumber:0.#} s",
            "survival.starvationDamage" => $"{parameter.CurrentNumber:0.#} dmg",
            "survival.starvationHealthRegenPenalty" => $"{parameter.CurrentNumber * 100:0}%",
            "survival.starvationStaminaDrain" => parameter.CurrentDisplay,

            "survival.radiationRecovery" => $"{parameter.CurrentNumber:0.00} pts/s",
            "survival.radiationAccumulation" or
            "survival.radiationDamage" => $"{parameter.CurrentNumber:0.##}x",

            "economy.buyCondition" or
            "economy.sellCondition" => $"{parameter.CurrentNumber:0.##}x",
            "economy.traderMinBuyCondition" or
            "economy.itemValueFloor" => $"{parameter.CurrentNumber * 100:0}%",

            "survival.sleepinessRate" => $"{parameter.CurrentNumber:0.000} pts/s",
            "survival.energyDrinkFatigueRelief" => $"{parameter.CurrentNumber:0.##}x",

            "survival.instantStaminaRestore" or
            "survival.alcoholRadiationRemoval" or
            "survival.waterEffectDuration" or
            "survival.energyDrinkEffectDuration" or
            "survival.vodkaEffectDuration" or
            "survival.beerEffectDuration" or
            "survival.magicVodkaEffectDuration" => $"{parameter.CurrentNumber:0.##}x",

            _ => parameter.CurrentDisplay
        };
    }

    private static string ShortLabel(ParameterViewModel parameter)
        => parameter.Label.Replace(" - ", " ").Trim();

    private static string FormatCompact(double value)
        => value.ToString(Math.Abs(value) < 1.0 ? "0.###" : "0.##", CultureInfo.InvariantCulture);

    private static double GetDesiredHeight(string family)
        => family switch
        {
            "FatigueCurve" => 420.0,
            "BleedingTreatmentMixer" => 385.0,
            "ConsumableMixer" => 420.0,
            "SleepingDashboard" => 310.0,
            "HungerLadder" => 380.0,
            "StarvationTimeline" => 395.0,
            "RepeatedHitBand" => 290.0,
            "RadiationBalance" => 300.0,
            "TradingDashboard" => 310.0,
            "RepairCurve" => 330.0,
            "AmmoDistribution" => 405.0,
            _ => 340.0
        };

    private static IReadOnlyList<string> GetFamilyIds(string family)
        => family switch
        {
            "FatigueCurve" => new[]
            {
                "survival.sleepinessRate",
                "survival.energyDrinkFatigueRelief",
                "survival.fatigueLowAimPenalty",
                "survival.fatigueMediumAimPenalty",
                "survival.fatigueStrongAimPenalty",
                "survival.fatigueMediumReloadPenalty",
                "survival.fatigueStrongReloadPenalty"
            },
            "BleedingTreatmentMixer" => new[]
            {
                "health.bandageBleeding",
                "health.medkitBleeding",
                "health.armyMedkitBleeding",
                "health.ecoMedkitBleeding"
            },
            "ConsumableMixer" => new[]
            {
                "survival.instantStaminaRestore",
                "survival.alcoholRadiationRemoval",
                "survival.waterEffectDuration",
                "survival.energyDrinkEffectDuration",
                "survival.vodkaEffectDuration",
                "survival.beerEffectDuration",
                "survival.magicVodkaEffectDuration"
            },
            "SleepingDashboard" => new[]
            {
                "survival.sleepEligibilityThreshold",
                "survival.minimumSleepHours",
                "survival.sleepHealthRestore",
                "survival.sleepStaminaRestore"
            },
            "HungerLadder" => new[]
            {
                "survival.hungerRate",
                "survival.hungerMediumStaminaRegen",
                "survival.hungerStrongStaminaRegen",
                "survival.wellFedStaminaRegen"
            },
            "StarvationTimeline" => new[]
            {
                "advanced.starvationMechanics",
                "survival.starvationStaminaDrain",
                "survival.starvationDamage",
                "survival.starvationDamageInterval",
                "survival.starvationPenaltyDelay",
                "survival.starvationHealthRegenPenalty"
            },
            "RepeatedHitBand" => new[]
            {
                "combat.accumulatedDamageWeightMax",
                "combat.accumulatedDamageWeightMin",
                "combat.accumulatedDamageIncludesHealedHealth"
            },
            "RadiationBalance" => new[]
            {
                "survival.radiationAccumulation",
                "survival.radiationRecovery",
                "survival.radiationDamage"
            },
            "TradingDashboard" => new[]
            {
                "economy.buyCondition",
                "economy.sellCondition",
                "economy.traderMinBuyCondition",
                "economy.itemValueFloor"
            },
            "RepairCurve" => new[]
            {
                "economy.repairEnemy",
                "economy.repairDisaffection",
                "economy.repairNeutral",
                "economy.repairFriend"
            },
            "AmmoDistribution" => new[]
            {
                "loot.ammoBoxWeight918",
                "loot.ammoBoxWeight045",
                "loot.ammoBoxWeight545",
                "loot.ammoBoxWeight012"
            },
            _ => Array.Empty<string>()
        };

    private enum HitKind
    {
        Horizontal,
        Vertical,
        Toggle
    }

    private sealed record HitTarget(
        ParameterViewModel Parameter,
        Rect HitRect,
        Rect MapRect,
        HitKind Kind,
        double VisualMinimum,
        double VisualMaximum,
        bool Reverse);
}
