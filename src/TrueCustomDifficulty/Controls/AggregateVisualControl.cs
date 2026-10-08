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
    private readonly VisualStatusMessage _status;

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
        _status = new VisualStatusMessage(InvalidateVisual);
        Focusable = true;
        // Family heights vary: compact two-setting boards may be under 320px.
        // WPF must not inflate those boards to a stale global minimum.
        MinHeight = 0.0;
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
            case "EquipmentWearBalance":
                DrawEquipmentWearBalance(dc, text, muted, border, soft, accent, panel);
                break;
            case "PsyPhantomPressure":
                DrawPsyPhantomPressure(dc, text, muted, border, soft, accent, panel);
                break;
            case "HumanCombatProfile":
                DrawHumanCombatProfile(dc, text, muted, border, soft, accent, panel);
                break;
            case "PlayerWeaponProfile":
                DrawPlayerWeaponProfile(dc, text, muted, border, soft, accent, panel);
                break;
            case "CoreHealthDashboard":
                DrawCoreHealthDashboard(dc, text, muted, border, soft, accent, panel);
                break;
            case "SevereFatiguePenaltyBoard":
                DrawSevereFatiguePenaltyBoard(dc, text, muted, border, soft, accent, panel);
                break;
            case "MasterMedicalHealing":
                DrawMasterMedicalHealing(dc, text, muted, border, soft, accent, panel);
                break;
            case "MasterLowHealth":
            case "MasterRadiationPenalty":
                DrawMasterConditionalPenalty(dc, text, muted, border, soft, accent, panel);
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
        DrawHeader(dc, "FATIGUE PENALTIES", "stage timing on the real SleepinessPoints axis + combat penalty strength", text, muted, accent);

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

        DrawText(dc, "COMBAT PENALTIES", new Point(16.0, 140.0), 9.0, muted, FontWeights.Bold);
        DrawText(dc, "X = stage threshold   |   Y = penalty strength", new Point(142.0, 140.0), 9.0, muted, FontWeights.Normal);

        var plot = new Rect(58.0, 162.0, Math.Max(360.0, ActualWidth - 82.0), 174.0);
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
            DrawRightAlignedText(
                dc,
                $"{value * 100:0}%",
                new Point(plot.Left - 8.0, y - 7.0),
                9.0,
                muted,
                FontWeights.SemiBold);
        }

        for (var points = 0; points <= 100; points += 25)
        {
            var x = plot.Left + plot.Width * points / 100.0;
            var pen = new Pen(CloneWithOpacity(soft, points is 0 or 100 ? 0.8 : 0.5), 1.0)
            {
                DashStyle = points is 0 or 100 ? DashStyles.Solid : DashStyles.Dot
            };
            dc.DrawLine(pen, new Point(x, plot.Top), new Point(x, plot.Bottom));
        }

        var thresholds = new[]
        {
            P("survival.sleepLowThreshold"),
            P("survival.sleepMediumThreshold"),
            P("survival.sleepStrongThreshold")
        };
        var thresholdFallbacks = new[] { 50.0, 60.0, 80.0 };
        var thresholdCurrent = thresholds
            .Select((p, i) => p?.CurrentNumber ?? thresholdFallbacks[i])
            .ToArray();
        var thresholdReference = thresholds
            .Select((p, i) => p?.ReferenceNumber ?? thresholdFallbacks[i])
            .ToArray();

        double ThresholdToX(double value)
            => plot.Left + plot.Width * Math.Clamp(value / 100.0, 0.0, 1.0);

        var xs = thresholdCurrent.Select(ThresholdToX).ToArray();
        var vanillaXs = thresholdReference.Select(ThresholdToX).ToArray();

        var aim = new[]
        {
            P("survival.fatigueLowAimPenalty"),
            P("survival.fatigueMediumAimPenalty"),
            P("survival.fatigueStrongAimPenalty")
        };

        var reload = new[]
        {
            P("survival.fatigueMediumReloadPenalty"),
            P("survival.fatigueStrongReloadPenalty")
        };

        var aimCurrent = aim.Select(x => x?.CurrentNumber ?? 0.0).ToArray();
        var aimVanilla = aim.Select(x => x?.ReferenceNumber ?? 0.0).ToArray();
        var reloadCurrent = reload.Select(x => x?.CurrentNumber ?? 0.0).ToArray();
        var reloadVanilla = reload.Select(x => x?.ReferenceNumber ?? 0.0).ToArray();

        DrawCurveLine(
            dc,
            vanillaXs,
            aimVanilla,
            plot,
            0.0,
            yMaximum,
            new Pen(muted, 1.2) { DashStyle = DashStyles.Dash });

        DrawCurveLine(
            dc,
            xs,
            aimCurrent,
            plot,
            0.0,
            yMaximum,
            new Pen(accent, 2.2));

        var reloadXs = new[] { xs[1], xs[2] };
        var reloadVanillaXs = new[] { vanillaXs[1], vanillaXs[2] };

        DrawCurveLine(
            dc,
            reloadVanillaXs,
            reloadVanilla,
            plot,
            0.0,
            yMaximum,
            new Pen(soft, 1.2) { DashStyle = DashStyles.Dash });

        DrawCurveLine(
            dc,
            reloadXs,
            reloadCurrent,
            plot,
            0.0,
            yMaximum,
            new Pen(text, 1.7));

        var legendY = plot.Top + 10.0;
        dc.DrawLine(new Pen(accent, 2.2), new Point(plot.Left + 10.0, legendY + 4.0), new Point(plot.Left + 28.0, legendY + 4.0));
        dc.DrawEllipse(accent, null, new Point(plot.Left + 19.0, legendY + 4.0), 3.0, 3.0);
        DrawText(dc, "AIM", new Point(plot.Left + 34.0, legendY - 3.0), 9.0, accent, FontWeights.Bold);

        dc.DrawLine(new Pen(text, 1.7), new Point(plot.Left + 78.0, legendY + 4.0), new Point(plot.Left + 96.0, legendY + 4.0));
        dc.DrawRectangle(panel, new Pen(text, 1.3), new Rect(plot.Left + 84.0, legendY + 1.0, 7.0, 7.0));
        DrawText(dc, "RELOAD", new Point(plot.Left + 102.0, legendY - 3.0), 9.0, text, FontWeights.Bold);

        var vanillaLegendPen = new Pen(muted, 1.2) { DashStyle = DashStyles.Dash };
        dc.DrawLine(vanillaLegendPen, new Point(plot.Right - 88.0, legendY + 4.0), new Point(plot.Right - 68.0, legendY + 4.0));
        DrawText(dc, "BASE", new Point(plot.Right - 60.0, legendY - 3.0), 9.0, muted, FontWeights.SemiBold);

        var stageLabels = new[] { "LOW", "MEDIUM", "SEVERE" };

        for (var i = 0; i < 3; i++)
        {
            var thresholdParameter = thresholds[i];
            var stageX = xs[i];

            dc.DrawLine(
                new Pen(CloneWithOpacity(accent, 0.30), 1.0) { DashStyle = DashStyles.Dot },
                new Point(stageX, plot.Top + 25.0),
                new Point(stageX, plot.Bottom));

            if (thresholdParameter is not null)
            {
                var referenceX = vanillaXs[i];
                dc.DrawLine(new Pen(muted, 1.0), new Point(referenceX, plot.Bottom - 6.0), new Point(referenceX, plot.Bottom + 8.0));
                dc.DrawLine(new Pen(accent, IsSelected(thresholdParameter) ? 2.8 : 2.0), new Point(stageX, plot.Bottom - 8.0), new Point(stageX, plot.Bottom + 9.0));
                dc.DrawEllipse(panel, new Pen(accent, IsSelected(thresholdParameter) ? 2.4 : 1.5), new Point(stageX, plot.Bottom + 3.0), 5.5, 5.5);

                var thresholdMap = new Rect(plot.Left, plot.Bottom - 2.0, plot.Width, 22.0);
                AddHorizontalTarget(
                    thresholdParameter,
                    new Rect(stageX - 18.0, plot.Bottom - 10.0, 36.0, 32.0),
                    thresholdMap,
                    0.0,
                    100.0);
            }

            var aimPoint = new Point(stageX, ValueToY(aimCurrent[i], 0.0, yMaximum, plot));
            dc.DrawEllipse(panel, new Pen(accent, IsSelected(aim[i]) ? 2.4 : 1.6), aimPoint, 6.0, 6.0);
            dc.DrawEllipse(accent, null, aimPoint, 2.8, 2.8);

            if (aim[i] is { } aimParameter)
                AddVerticalPointTarget(aimParameter, aimPoint, plot, 0.0, yMaximum);

            if (i > 0)
            {
                var reloadIndex = i - 1;
                var reloadPoint = new Point(stageX, ValueToY(reloadCurrent[reloadIndex], 0.0, yMaximum, plot));

                dc.DrawRectangle(
                    panel,
                    new Pen(text, IsSelected(reload[reloadIndex]) ? 2.2 : 1.4),
                    new Rect(reloadPoint.X - 5.0, reloadPoint.Y - 5.0, 10.0, 10.0));

                if (reload[reloadIndex] is { } reloadParameter)
                    AddVerticalPointTarget(reloadParameter, reloadPoint, plot, 0.0, yMaximum);
            }

            DrawCenteredText(
                dc,
                $"{stageLabels[i]} {thresholdCurrent[i]:0}",
                new Point(stageX, plot.Bottom + 14.0),
                9.2,
                text,
                FontWeights.Bold);

            DrawCenteredText(
                dc,
                $"{aimCurrent[i] * 100:0}% AIM",
                new Point(stageX, plot.Bottom + 31.0),
                9.0,
                accent,
                FontWeights.Bold);

            var reloadLabel = i == 0
                ? "— RELOAD"
                : $"{reloadCurrent[i - 1] * 100:0.#}% RELOAD";

            DrawCenteredText(
                dc,
                reloadLabel,
                new Point(stageX, plot.Bottom + 46.0),
                9.0,
                i == 0 ? muted : text,
                i == 0 ? FontWeights.Normal : FontWeights.SemiBold);
        }
    }








    // Low-health and radiation are intentionally two compact, separately
    // editable conditional panels. Their toggle and sole child slider share
    // one surface, but they never change unrelated survival systems.
    private void DrawMasterConditionalPenalty(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        var lowHealth = Family == "MasterLowHealth";
        var toggle = P(lowHealth ? "advanced.lowHealthMechanics" : "advanced.radiationMechanics");
        var penalty = P(lowHealth ? "health.masterLowHealthPenalty" : "survival.radiationStaminaDrainPenalty");

        DrawText(dc, lowHealth ? "MASTER LOW HEALTH" : "MASTER RADIATION",
            new Point(17.0, 13.0), 11.5, accent, FontWeights.Bold);
        DrawText(dc,
            lowHealth
                ? "extra recovery loss at the authored 30 HP low-health boundary"
                : "extra action stamina cost in the heavy-radiation state",
            new Point(17.0, 32.0), 9.5, text, FontWeights.Normal);

        if (toggle is null || penalty is null)
        {
            DrawCenteredText(dc, "Master penalty data unavailable.",
                new Point(ActualWidth / 2.0, 110.0), 10.0, muted, FontWeights.SemiBold);
            return;
        }

        DrawTogglePill(dc, toggle,
            lowHealth ? "LOW-HEALTH BUNDLE" : "RADIATION BUNDLE",
            new Rect(17.0, 55.0, 251.0, 38.0),
            text, muted, border, accent, panel);
        DrawRightAlignedText(dc,
            $"BASE PRESET: {(toggle.ReferenceBoolean ? "ON" : "OFF")}",
            new Point(ActualWidth - 18.0, 66.0), 10.0, muted, FontWeights.SemiBold);

        var card = new Rect(17.0, 106.0, Math.Max(480.0, ActualWidth - 34.0), 86.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), card, 4.0, 4.0);

        var enabled = penalty.IsControlEnabled;
        DrawText(dc,
            lowHealth ? "HEALTH RECOVERY LOSS" : "EXTRA ACTION STAMINA COST",
            new Point(card.Left + 12.0, card.Top + 8.0), 10.4, text, FontWeights.Bold);
        DrawRightAlignedText(dc,
            $"{(lowHealth ? "-" : "+")}{penalty.CurrentNumber * 100:0}%  |  BASE {penalty.ReferenceNumber * 100:0}%",
            new Point(card.Right - 12.0, card.Top + 7.0),
            11.0, enabled ? accent : muted, FontWeights.Bold);

        var track = new Rect(card.Left + 16.0, card.Top + 32.0, card.Width - 32.0, 23.0);
        var y = track.Top + track.Height / 2.0;
        var currentX = ValueToX(penalty.CurrentNumber,
            penalty.Minimum, penalty.Maximum, track, reverse: false);
        var baseX = ValueToX(penalty.ReferenceNumber,
            penalty.Minimum, penalty.Maximum, track, reverse: false);

        dc.DrawLine(new Pen(soft, 4.0),
            new Point(track.Left, y), new Point(track.Right, y));
        if (enabled && currentX > track.Left)
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.66), 4.5),
                new Point(track.Left, y), new Point(currentX, y));

        dc.DrawLine(new Pen(muted, 1.9),
            new Point(baseX, y - 12.0), new Point(baseX, y + 12.0));
        dc.DrawEllipse(panel,
            new Pen(enabled ? accent : muted,
                enabled && IsSelected(penalty) ? 2.8 : 1.9),
            new Point(currentX, y), 7.2, 7.2);
        if (enabled)
            dc.DrawEllipse(accent, null, new Point(currentX, y), 2.7, 2.7);

        AddHorizontalTarget(penalty, track, track, penalty.Minimum, penalty.Maximum);
        DrawText(dc, "0%", new Point(track.Left, card.Bottom - 23.0),
            9.4, text, FontWeights.Normal);
        DrawRightAlignedText(dc, "100%", new Point(track.Right, card.Bottom - 23.0),
            9.4, text, FontWeights.Normal);

        DrawCenteredText(dc,
            enabled
                ? "GREY TICK = BASE PRESET  |  THRESHOLD UNCHANGED"
                : "BUNDLE OFF  |  VALUE PRESERVED  |  ENABLE TO EDIT",
            new Point(ActualWidth / 2.0, card.Bottom + 5.0),
            9.1, enabled ? text : muted, FontWeights.SemiBold);
    }

    // This is a comparison of exact authored healing ValueMultipliers,
    // not a computed amount of HP. The underlying medical item's own base heal
    // can be edited independently on the Health tab.
    private void DrawMasterMedicalHealing(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "MASTER CONSUMABLE PENALTIES",
            "healing retained after the Master modifier (100% = normal item heal)",
            text, muted, accent);

        var toggle = P("advanced.masterEffectModifierMechanics");
        var bandage = P("health.masterBandageModifier");
        var medkit = P("health.masterMedkitModifier");
        var army = P("health.masterArmyMedkitModifier");
        if (toggle is null || bandage is null || medkit is null || army is null)
        {
            DrawCenteredText(dc, "Medical effect data unavailable.",
                new Point(ActualWidth / 2.0, 165.0), 10.0, muted, FontWeights.SemiBold);
            return;
        }

        DrawTogglePill(dc, toggle, "MASTER EFFECT BUNDLE",
            new Rect(17.0, 55.0, Math.Min(257.0, ActualWidth * 0.51), 39.0),
            text, muted, border, accent, panel);
        DrawRightAlignedText(dc,
            toggle.CurrentBoolean ? "BUNDLE ACTIVE" : "BUNDLE DISABLED",
            new Point(ActualWidth - 18.0, 66.0), 10.2,
            toggle.CurrentBoolean ? accent : muted, FontWeights.Bold);

        DrawText(dc, "ITEM HEALING STRENGTH",
            new Point(20.0, 107.0), 9.7, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "0%   /   100% ORIGINAL HEAL   /   200%",
            new Point(ActualWidth - 20.0, 107.0),
            9.2, muted, FontWeights.SemiBold);

        var width = Math.Max(480.0, ActualWidth - 36.0);
        DrawMasterMedicalHealingRow(dc, bandage, "BANDAGE", new Rect(18.0, 130.0, width, 65.0),
            text, muted, border, soft, accent, panel);
        DrawMasterMedicalHealingRow(dc, medkit, "MEDKIT", new Rect(18.0, 205.0, width, 65.0),
            text, muted, border, soft, accent, panel);
        DrawMasterMedicalHealingRow(dc, army, "ARMY MEDKIT", new Rect(18.0, 280.0, width, 65.0),
            text, muted, border, soft, accent, panel);

        DrawCenteredText(dc,
            toggle.CurrentBoolean
                ? "GREY = BASE PRESET   |   100% = UNMODIFIED ITEM HEALING"
                : "OFF: SLIDERS LOCKED; VALUES KEPT   |   OTHER CONSUMABLE EFFECTS ALSO IN BUNDLE",
            new Point(ActualWidth / 2.0, 355.0),
            9.2, toggle.CurrentBoolean ? text : muted, FontWeights.SemiBold);
    }

    private void DrawMasterMedicalHealingRow(
        DrawingContext dc,
        ParameterViewModel parameter,
        string label,
        Rect card,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), card, 3.0, 3.0);
        var active = parameter.IsControlEnabled;
        DrawText(dc, label, new Point(card.Left + 11.0, card.Top + 8.0),
            10.3, text, FontWeights.Bold);
        DrawRightAlignedText(dc,
            $"{parameter.CurrentNumber * 100:0}%   |   BASE {parameter.ReferenceNumber * 100:0}%",
            new Point(card.Right - 12.0, card.Top + 7.0),
            10.5, active ? accent : muted, FontWeights.Bold);

        var track = new Rect(card.Left + 15.0, card.Top + 29.0,
            card.Width - 30.0, 24.0);
        var y = track.Top + track.Height / 2.0;
        var normalX = ValueToX(1.0, parameter.Minimum, parameter.Maximum, track, reverse: false);
        var baseX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, track, reverse: false);
        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track, reverse: false);

        dc.DrawLine(new Pen(soft, 4.0),
            new Point(track.Left, y), new Point(track.Right, y));
        if (active && currentX > track.Left)
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.65), 4.5),
                new Point(track.Left, y), new Point(currentX, y));

        // The 100%-normal reference is not generally the Master Base Preset.
        // Show it as a light guide and keep the true difficulty reference grey.
        dc.DrawLine(new Pen(CloneWithOpacity(text, 0.52), 1.0) { DashStyle = DashStyles.Dash },
            new Point(normalX, y - 10.0), new Point(normalX, y + 10.0));
        dc.DrawLine(new Pen(muted, 1.9),
            new Point(baseX, y - 12.0), new Point(baseX, y + 12.0));
        dc.DrawEllipse(panel, new Pen(active ? accent : muted, IsSelected(parameter) ? 2.5 : 1.8),
            new Point(currentX, y), 7.2, 7.2);
        if (active)
            dc.DrawEllipse(accent, null, new Point(currentX, y), 2.7, 2.7);

        AddHorizontalTarget(parameter, track, track, parameter.Minimum, parameter.Maximum);
    }

    // Master-only negative mechanics activation owns the six-member family.
    // All five sliders retain their original CFG-backed values even while off,
    // but their tracks are intentionally not interactive until enabled.
    private void DrawSevereFatiguePenaltyBoard(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        // Local heading typography: this dashboard contains five dense cards.
        // A stronger title/subtitle prevents the tiny-label look at 16:10.
        DrawText(dc, "MASTER SEVERE FATIGUE",
            new Point(17.0, 12.0), 12.0, accent, FontWeights.Bold);
        DrawText(dc, "extra penalties while the vanilla severe-fatigue state is active",
            new Point(17.0, 32.0), 9.7, text, FontWeights.Normal);

        var toggle = P("advanced.fatigueMechanics");
        if (toggle is null)
        {
            DrawCenteredText(dc, "Master fatigue data unavailable.",
                new Point(ActualWidth / 2.0, 175.0),
                10.0, muted, FontWeights.SemiBold);
            return;
        }

        DrawTogglePill(dc, toggle, "SEVERE-FATIGUE BUNDLE",
            new Rect(16.0, 55.0, Math.Min(247.0, ActualWidth * 0.51), 39.0),
            text, muted, border, accent, panel);
        DrawRightAlignedText(dc,
            toggle.CurrentBoolean ? "PENALTIES ACTIVE" : "PENALTIES DISABLED",
            new Point(ActualWidth - 18.0, 66.0),
            10.6, toggle.CurrentBoolean ? accent : muted, FontWeights.Bold);

        var width = Math.Max(480.0, ActualWidth - 32.0);
        var gap = 10.0;
        var half = (width - gap) / 2.0;

        DrawSevereFatiguePenaltyCard(dc, P("survival.severeFatigueMaxStamina"),
            "MAX STAMINA LOSS", new Rect(16.0, 113.0, half, 69.0),
            text, muted, border, soft, accent, panel);
        DrawSevereFatiguePenaltyCard(dc, P("survival.severeFatigueCarryWeight"),
            "CARRY CAPACITY LOSS", new Rect(16.0 + half + gap, 113.0, half, 69.0),
            text, muted, border, soft, accent, panel);
        DrawSevereFatiguePenaltyCard(dc, P("survival.severeFatigueRecoil"),
            "RECOIL INCREASE", new Rect(16.0, 191.0, half, 69.0),
            text, muted, border, soft, accent, panel);
        DrawSevereFatiguePenaltyCard(dc, P("survival.severeFatigueAimSway"),
            "AIM SWAY", new Rect(16.0 + half + gap, 191.0, half, 69.0),
            text, muted, border, soft, accent, panel);
        // Five actual penalties: center the last lane rather than adding a
        // decorative sixth card which looked like another control in the UI.
        DrawSevereFatiguePenaltyCard(dc, P("survival.severeFatigueDispersion"),
            "SHOT SPREAD", new Rect(16.0 + (width - half) / 2.0, 269.0, half, 69.0),
            text, muted, border, soft, accent, panel);

        DrawCenteredText(dc,
            toggle.CurrentBoolean
                ? "GOLD = CURRENT  |  GREY TICK = BASE  |  ACTIVATION THRESHOLD UNCHANGED"
                : "BUNDLE OFF  |  VALUES KEPT  |  ENABLE TO EDIT",
            new Point(ActualWidth / 2.0, 347.0),
            9.7, toggle.CurrentBoolean ? text : muted, FontWeights.SemiBold);
    }

    private void DrawSevereFatiguePenaltyCard(
        DrawingContext dc,
        ParameterViewModel? parameter,
        string label,
        Rect card,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), card, 3.0, 3.0);
        DrawText(dc, label, new Point(card.Left + 10.0, card.Top + 7.0),
            10.5, text, FontWeights.Bold);
        if (parameter is null)
            return;

        var enabled = parameter.IsControlEnabled;
        var isPercent = parameter.Id is "survival.severeFatigueMaxStamina"
            or "survival.severeFatigueCarryWeight"
            or "survival.severeFatigueRecoil";
        string Format(double value)
            => isPercent ? $"{value * 100:0}%"
                         : $"{value:0.##}x";

        // Keep the current value legible. Shorten the label for narrow cards;
        // the grey baseline tick stays present regardless of card width.
        var compact = card.Width < 325.0;
        DrawRightAlignedText(dc,
            compact
                ? Format(parameter.CurrentNumber)
                : $"{Format(parameter.CurrentNumber)}   /   BASE {Format(parameter.ReferenceNumber)}",
            new Point(card.Right - 10.0, card.Top + 6.0),
            11.2, enabled ? accent : muted, FontWeights.Bold);

        var track = new Rect(card.Left + 12.0, card.Top + 30.0,
            Math.Max(64.0, card.Width - 24.0), 21.0);
        var y = track.Top + track.Height / 2.0;
        dc.DrawLine(new Pen(soft, 4.0),
            new Point(track.Left, y), new Point(track.Right, y));
        var baseX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum,
            track, reverse: false);
        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum,
            track, reverse: false);

        // A visible, understated gold fill communicates the actual configured
        // magnitude even at vanilla. The previous "base-to-current delta only"
        // line completely disappeared whenever the values were equal.
        if (enabled && currentX > track.Left)
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.62), 4.0),
                new Point(track.Left, y), new Point(currentX, y));

        // Put the Base Preset tick behind the handle but extend it beyond the
        // ring so both remain distinguishable at identical values.
        dc.DrawLine(new Pen(muted, 1.8),
            new Point(baseX, y - 11.0), new Point(baseX, y + 11.0));
        dc.DrawEllipse(panel, new Pen(enabled ? accent : muted,
                enabled && IsSelected(parameter) ? 2.8 : 2.0),
            new Point(currentX, y), 7.2, 7.2);
        if (enabled)
            dc.DrawEllipse(accent, null, new Point(currentX, y), 2.8, 2.8);

        // AddHorizontalTarget performs the same IsControlEnabled guard used
        // by every other aggregate editor: OFF means no edits, not data loss.
        AddHorizontalTarget(parameter, track, track, parameter.Minimum, parameter.Maximum);
        DrawText(dc, Format(parameter.Minimum),
            new Point(track.Left, card.Bottom - 15.0),
            9.6, text, FontWeights.Normal);
        DrawRightAlignedText(dc, Format(parameter.Maximum),
            new Point(track.Right, card.Bottom - 15.0),
            9.6, text, FontWeights.Normal);
    }

    // This board only previews raw CFG settings. In particular RegenHP's
    // runtime tick cadence and which heal effects honor its delay are not
    // proven, so we deliberately do not draw an invented HP-over-time curve.
    private void DrawCoreHealthDashboard(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "CORE HEALTH",
            "health pool, configured regen delay and two independent recovery layers",
            text, muted, accent);

        var maxHP = P("health.maxHP");
        var delay = P("health.regenDelay");
        var passive = P("health.baseRegen");
        var effectScale = P("health.regenerationScalar");
        if (maxHP is null || delay is null || passive is null || effectScale is null)
        {
            DrawCenteredText(dc, "Core health data unavailable.",
                new Point(ActualWidth / 2.0, 177.0), 10.0, muted, FontWeights.SemiBold);
            return;
        }

        var width = Math.Max(480.0, ActualWidth - 32.0);
        var hpCard = new Rect(16.0, 54.0, width, 93.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), hpCard, 4.0, 4.0);
        DrawText(dc, "MAX HEALTH", new Point(hpCard.Left + 12.0, hpCard.Top + 10.0),
            10.0, text, FontWeights.Bold);
        DrawRightAlignedText(dc,
            $"{maxHP.CurrentNumber:0} HP  |  BASE {maxHP.ReferenceNumber:0} HP",
            new Point(hpCard.Right - 12.0, hpCard.Top + 8.0),
            10.5, accent, FontWeights.Bold);

        var hpTrack = new Rect(hpCard.Left + 16.0, hpCard.Top + 38.0,
            hpCard.Width - 32.0, 22.0);
        var hpY = hpTrack.Top + hpTrack.Height / 2.0;
        var hpBaseX = ValueToX(maxHP.ReferenceNumber, maxHP.Minimum, maxHP.Maximum, hpTrack, false);
        var hpCurrentX = ValueToX(maxHP.CurrentNumber, maxHP.Minimum, maxHP.Maximum, hpTrack, false);
        dc.DrawLine(new Pen(soft, 4.0), new Point(hpTrack.Left, hpY), new Point(hpTrack.Right, hpY));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.45), 5.0),
            new Point(Math.Min(hpCurrentX, hpBaseX), hpY),
            new Point(Math.Max(hpCurrentX, hpBaseX), hpY));
        dc.DrawLine(new Pen(muted, 2.0),
            new Point(hpBaseX, hpY - 10.0), new Point(hpBaseX, hpY + 10.0));
        dc.DrawEllipse(panel, new Pen(accent, IsSelected(maxHP) ? 2.8 : 1.8),
            new Point(hpCurrentX, hpY), 7.0, 7.0);
        dc.DrawEllipse(accent, null, new Point(hpCurrentX, hpY), 3.0, 3.0);
        AddHorizontalTarget(maxHP, hpTrack, hpTrack, maxHP.Minimum, maxHP.Maximum);
        DrawText(dc, $"{maxHP.Minimum:0} HP",
            new Point(hpTrack.Left, hpCard.Bottom - 20.0),
            9.0, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, $"{maxHP.Maximum:0} HP",
            new Point(hpTrack.Right, hpCard.Bottom - 20.0),
            9.0, muted, FontWeights.Normal);

        var delayCard = new Rect(16.0, 156.0, width, 98.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), delayCard, 4.0, 4.0);
        DrawText(dc, "CONFIGURED RECOVERY DELAY",
            new Point(delayCard.Left + 12.0, delayCard.Top + 9.0),
            9.6, text, FontWeights.Bold);
        DrawRightAlignedText(dc,
            $"{delay.CurrentNumber:0} s  |  BASE {delay.ReferenceNumber:0} s",
            new Point(delayCard.Right - 12.0, delayCard.Top + 8.0),
            10.2, accent, FontWeights.Bold);
        DrawText(dc, "Darker section = configured wait before compatible recovery",
            new Point(delayCard.Left + 12.0, delayCard.Top + 26.0),
            9.0, muted, FontWeights.Normal);

        var delayTrack = new Rect(delayCard.Left + 16.0, delayCard.Top + 50.0,
            delayCard.Width - 32.0, 22.0);
        var delayY = delayTrack.Top + delayTrack.Height / 2.0;
        var delayBaseX = ValueToX(delay.ReferenceNumber, delay.Minimum, delay.Maximum, delayTrack, false);
        var delayCurrentX = ValueToX(delay.CurrentNumber, delay.Minimum, delay.Maximum, delayTrack, false);
        dc.DrawLine(new Pen(soft, 4.0),
            new Point(delayTrack.Left, delayY), new Point(delayTrack.Right, delayY));
        if (delayCurrentX > delayTrack.Left)
            dc.DrawLine(new Pen(CloneWithOpacity(border, 0.85), 5.0),
                new Point(delayTrack.Left, delayY), new Point(delayCurrentX, delayY));
        dc.DrawLine(new Pen(muted, 2.0),
            new Point(delayBaseX, delayY - 9.0), new Point(delayBaseX, delayY + 9.0));
        dc.DrawEllipse(panel, new Pen(accent, IsSelected(delay) ? 2.8 : 1.8),
            new Point(delayCurrentX, delayY), 6.5, 6.5);
        dc.DrawEllipse(accent, null, new Point(delayCurrentX, delayY), 2.5, 2.5);
        AddHorizontalTarget(delay, delayTrack, delayTrack, delay.Minimum, delay.Maximum);
        DrawText(dc, "0 s", new Point(delayTrack.Left, delayCard.Bottom - 20.0),
            9.0, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, "10 s", new Point(delayTrack.Right, delayCard.Bottom - 20.0),
            9.0, muted, FontWeights.Normal);

        var gap = 10.0;
        var cardWidth = (width - gap) / 2.0;
        DrawHorizontalTrackCard(dc, passive, "PASSIVE REGEN",
            new Rect(16.0, 265.0, cardWidth, 72.0),
            text, muted, border, accent, panel);
        DrawHorizontalTrackCard(dc, effectScale, "RECOVERY EFFECT SCALE",
            new Rect(16.0 + cardWidth + gap, 265.0, cardWidth, 72.0),
            text, muted, border, accent, panel);

        DrawCenteredText(dc,
            "0 PASSIVE = VANILLA  |  EFFECT SCALE ALONE DOES NOT START REGEN",
            new Point(ActualWidth / 2.0, 345.0),
            9.0, text, FontWeights.SemiBold);
    }

    // Three source-exact tuning lanes: no invented headshot damage formula,
    // simulated hit chance, or hard-coded weapon-jam probability.
    private void DrawPlayerWeaponProfile(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "YOUR WEAPONS",
            "damage, headshot modifier and jamming stay three independent CFG settings",
            text, muted, accent);

        DrawPlayerWeaponLane(dc, P("combat.playerWeaponDamage"), "BASE HIT DAMAGE",
            new Rect(16.0, 57.0, Math.Max(480.0, ActualWidth - 32.0), 68.0),
            text, muted, border, soft, accent, panel);
        DrawPlayerWeaponLane(dc, P("combat.playerHeadshotMultiplier"), "HEADSHOT MULTIPLIER",
            new Rect(16.0, 134.0, Math.Max(480.0, ActualWidth - 32.0), 68.0),
            text, muted, border, soft, accent, panel);
        DrawPlayerWeaponLane(dc, P("combat.weaponJamming"), "JAM FREQUENCY",
            new Rect(16.0, 211.0, Math.Max(480.0, ActualWidth - 32.0), 68.0),
            text, muted, border, soft, accent, panel);

        DrawCenteredText(dc,
            "RIGHT = STRONGER HITS / HEADSHOTS  |  MORE JAMS ON THE THIRD TRACK",
            new Point(ActualWidth / 2.0, 290.0), 9.1, text, FontWeights.SemiBold);
    }

    private void DrawPlayerWeaponLane(
        DrawingContext dc,
        ParameterViewModel? parameter,
        string label,
        Rect card,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), card, 3.0, 3.0);
        DrawText(dc, label, new Point(card.Left + 11.0, card.Top + 7.0),
            9.5, text, FontWeights.SemiBold);
        if (parameter is null)
        {
            DrawRightAlignedText(dc, "MISSING CFG PARAMETER",
                new Point(card.Right - 11.0, card.Top + 7.0),
                9.5, muted, FontWeights.Normal);
            return;
        }

        DrawRightAlignedText(dc,
            $"{parameter.CurrentNumber:0.##}x   |   BASE {parameter.ReferenceNumber:0.##}x",
            new Point(card.Right - 11.0, card.Top + 7.0),
            10.0, accent, FontWeights.Bold);

        var track = new Rect(card.Left + 15.0, card.Top + 27.0,
            Math.Max(80.0, card.Width - 30.0), 20.0);
        var y = track.Top + track.Height / 2.0;
        var baselineX = ValueToX(parameter.ReferenceNumber,
            parameter.Minimum, parameter.Maximum, track, reverse: false);
        var currentX = ValueToX(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, track, reverse: false);

        dc.DrawLine(new Pen(soft, 3.5),
            new Point(track.Left, y), new Point(track.Right, y));
        if (Math.Abs(currentX - baselineX) > 0.5)
        {
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.7), 4.5),
                new Point(Math.Min(currentX, baselineX), y),
                new Point(Math.Max(currentX, baselineX), y));
        }

        dc.DrawLine(new Pen(muted, 1.8),
            new Point(baselineX, y - 9.0), new Point(baselineX, y + 9.0));
        dc.DrawEllipse(panel,
            new Pen(accent, IsSelected(parameter) ? 2.6 : 1.7),
            new Point(currentX, y), 6.4, 6.4);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

        // Native range and step are used for hit testing / dragging.
        AddHorizontalTarget(parameter, track, track, parameter.Minimum, parameter.Maximum);
        DrawText(dc, $"{parameter.Minimum:0.##}x",
            new Point(track.Left, card.Bottom - 16.0),
            9.0, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, $"{parameter.Maximum:0.##}x",
            new Point(track.Right, card.Bottom - 16.0),
            9.0, muted, FontWeights.Normal);
    }

    // Two independently editable native CFG axes plus two more distinct controls.
    // The crosshair is a visual pairing, not an effective DPS, TTK or hit-rate model.
    private void DrawHumanCombatProfile(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "HUMAN COMBAT PROFILE",
            "enemy toughness and gun damage, plus attack tempo and shot accuracy",
            text, muted, accent);

        var health = P("combat.npcHealth");
        var damage = P("combat.npcWeaponDamage");
        var frequency = P("combat.npcAttackCooldown");
        var accuracy = P("combat.npcHipAccuracy");
        if (health is null || damage is null || frequency is null || accuracy is null)
        {
            DrawCenteredText(dc, "Human combat data unavailable.",
                new Point(ActualWidth / 2.0, 165.0), 10.0, muted, FontWeights.SemiBold);
            return;
        }

        var compact = ActualWidth < 700.0;
        DrawText(dc,
            $"{(compact ? "HP" : "ENEMY HEALTH")}  {health.CurrentNumber:0.##}x  |  BASE {health.ReferenceNumber:0.##}x",
            new Point(18.0, 54.0), 9.8, accent, FontWeights.SemiBold);
        DrawRightAlignedText(dc,
            $"{(compact ? "GUN" : "GUN DAMAGE")}  {damage.CurrentNumber:0.##}x  |  BASE {damage.ReferenceNumber:0.##}x",
            new Point(ActualWidth - 18.0, 54.0), 9.8, accent, FontWeights.SemiBold);

        var plot = new Rect(78.0, 83.0, Math.Max(350.0, ActualWidth - 116.0), 135.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), plot, 4.0, 4.0);

        for (var tick = 0; tick <= 4; tick++)
        {
            var healthTick = health.Minimum + (health.Maximum - health.Minimum) * tick / 4.0;
            var damageTick = damage.Minimum + (damage.Maximum - damage.Minimum) * tick / 4.0;
            var gridX = ValueToX(healthTick, health.Minimum, health.Maximum, plot, reverse: false);
            var gridY = ValueToY(damageTick, damage.Minimum, damage.Maximum, plot);
            var gridPen = new Pen(CloneWithOpacity(soft, 0.5), 1.0) { DashStyle = DashStyles.Dot };
            dc.DrawLine(gridPen, new Point(gridX, plot.Top), new Point(gridX, plot.Bottom));
            dc.DrawLine(gridPen, new Point(plot.Left, gridY), new Point(plot.Right, gridY));

            if (tick is 0 or 2 or 4)
            {
                DrawCenteredText(dc, $"{healthTick:0.##}x",
                    new Point(gridX, plot.Bottom + 29.0), 9.3, text, FontWeights.Normal);
                DrawRightAlignedText(dc, $"{damageTick:0.##}x",
                    new Point(plot.Left - 29.0, gridY - 7.0), 9.3, text, FontWeights.Normal);
            }
        }

        var baseHealthX = ValueToX(health.ReferenceNumber, health.Minimum, health.Maximum, plot, reverse: false);
        var baseDamageY = ValueToY(damage.ReferenceNumber, damage.Minimum, damage.Maximum, plot);
        var healthX = ValueToX(health.CurrentNumber, health.Minimum, health.Maximum, plot, reverse: false);
        var damageY = ValueToY(damage.CurrentNumber, damage.Minimum, damage.Maximum, plot);
        var basePen = new Pen(CloneWithOpacity(muted, 0.65), 1.2) { DashStyle = DashStyles.Dash };
        dc.DrawLine(basePen, new Point(baseHealthX, plot.Top), new Point(baseHealthX, plot.Bottom));
        dc.DrawLine(basePen, new Point(plot.Left, baseDamageY), new Point(plot.Right, baseDamageY));
        dc.DrawEllipse(panel, new Pen(muted, 1.7),
            new Point(baseHealthX, baseDamageY), 6.0, 6.0);

        var currentPen = new Pen(CloneWithOpacity(accent, 0.65), 1.2) { DashStyle = DashStyles.Dot };
        dc.DrawLine(currentPen, new Point(healthX, plot.Top), new Point(healthX, plot.Bottom));
        dc.DrawLine(currentPen, new Point(plot.Left, damageY), new Point(plot.Right, damageY));
        dc.DrawEllipse(panel, new Pen(accent, 2.0),
            new Point(healthX, damageY), 6.5, 6.5);
        dc.DrawEllipse(accent, null, new Point(healthX, damageY), 2.8, 2.8);

        // Separate handles prevent accidental simultaneous changes when a user
        // clicks the crosshair or a blank part of the plotting area.
        var healthHandle = new Point(healthX, plot.Bottom + 10.0);
        dc.DrawEllipse(panel, new Pen(accent, IsSelected(health) ? 2.6 : 1.8),
            healthHandle, 7.0, 7.0);
        dc.DrawEllipse(accent, null, healthHandle, 2.5, 2.5);
        AddHorizontalTarget(health, new Rect(healthX - 17.0, plot.Bottom + 1.0, 34.0, 22.0),
            plot, health.Minimum, health.Maximum);

        var damageHandle = new Point(plot.Left - 11.0, damageY);
        dc.DrawRectangle(panel, new Pen(accent, IsSelected(damage) ? 2.6 : 1.8),
            new Rect(damageHandle.X - 6.0, damageHandle.Y - 6.0, 12.0, 12.0));
        AddVerticalTarget(damage, new Rect(plot.Left - 28.0, damageY - 14.0, 27.0, 28.0),
            plot, damage.Minimum, damage.Maximum);

        DrawCenteredText(dc,
            "TOUGHER ENEMIES →   |   MORE GUN DAMAGE ↑   |   GREY = BASE",
            new Point(ActualWidth / 2.0, 268.0), 9.3, text, FontWeights.SemiBold);

        // The frequency VM already inverts its raw cooldown in the CFG writer.
        // An accuracy multiplier is not a literal hit probability.
        var gap = 10.0;
        var cardWidth = (ActualWidth - 36.0 - gap) / 2.0;
        DrawHorizontalTrackCard(dc, frequency, "ATTACK FREQUENCY",
            new Rect(18.0, 287.0, cardWidth, 72.0),
            text, muted, border, accent, panel);
        DrawHorizontalTrackCard(dc, accuracy, "FIRE ACCURACY",
            new Rect(18.0 + cardWidth + gap, 287.0, cardWidth, 72.0),
            text, muted, border, accent, panel);
    }

    // Editable axes are deliberately independent. The gold crosshair is a
    // read-only summary of the two source-exact Psy spawn override multipliers.
    // Do not imply a simulated spawns-per-minute formula: base encounter timings
    // and the effective runtime paths are not fully exposed by the CFG dump.
    private void DrawPsyPhantomPressure(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "PSY PHANTOM PRESSURE",
            "spawn override: delay and phantom count, plus bleeding and gear damage",
            text, muted, accent);

        var wait = P("combat.psySpawnCycleDelay");
        var count = P("combat.psyPhantomCount");
        var wear = P("combat.psyDurabilityDamage");
        var bleed = P("combat.psyBleeding");
        if (wait is null || count is null || wear is null || bleed is null)
        {
            DrawCenteredText(dc, "Psy combat data unavailable.",
                new Point(ActualWidth / 2.0, 165.0), 10.0, muted, FontWeights.SemiBold);
            return;
        }

        DrawText(dc,
            $"CYCLE WAIT  {wait.CurrentNumber:0.##}x  |  BASE {wait.ReferenceNumber:0.##}x",
            new Point(18.0, 54.0), 9.8, accent, FontWeights.SemiBold);
        DrawRightAlignedText(dc,
            $"PHANTOM COUNT  {count.CurrentNumber:0.##}x  |  BASE {count.ReferenceNumber:0.##}x",
            new Point(ActualWidth - 18.0, 54.0), 9.8, accent, FontWeights.SemiBold);

        var plot = new Rect(78.0, 83.0, Math.Max(350.0, ActualWidth - 116.0), 135.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), plot, 4.0, 4.0);

        for (var tick = 0; tick <= 4; tick++)
        {
            var waitTick = wait.Minimum + (wait.Maximum - wait.Minimum) * tick / 4.0;
            var countTick = count.Minimum + (count.Maximum - count.Minimum) * tick / 4.0;
            var gridX = ValueToX(waitTick, wait.Minimum, wait.Maximum, plot, reverse: false);
            var gridY = ValueToY(countTick, count.Minimum, count.Maximum, plot);
            var gridPen = new Pen(CloneWithOpacity(soft, 0.5), 1.0) { DashStyle = DashStyles.Dot };
            dc.DrawLine(gridPen, new Point(gridX, plot.Top), new Point(gridX, plot.Bottom));
            dc.DrawLine(gridPen, new Point(plot.Left, gridY), new Point(plot.Right, gridY));

            if (tick is 0 or 2 or 4)
            {
                DrawCenteredText(dc, $"{waitTick:0.##}x", new Point(gridX, plot.Bottom + 29.0),
                    9.3, text, FontWeights.Normal);
                DrawRightAlignedText(dc, $"{countTick:0.##}x",
                    new Point(plot.Left - 29.0, gridY - 7.0), 9.3, text, FontWeights.Normal);
            }
        }

        var baseWaitX = ValueToX(wait.ReferenceNumber, wait.Minimum, wait.Maximum, plot, reverse: false);
        var baseCountY = ValueToY(count.ReferenceNumber, count.Minimum, count.Maximum, plot);
        var waitX = ValueToX(wait.CurrentNumber, wait.Minimum, wait.Maximum, plot, reverse: false);
        var countY = ValueToY(count.CurrentNumber, count.Minimum, count.Maximum, plot);

        var basePen = new Pen(CloneWithOpacity(muted, 0.65), 1.2) { DashStyle = DashStyles.Dash };
        dc.DrawLine(basePen, new Point(baseWaitX, plot.Top), new Point(baseWaitX, plot.Bottom));
        dc.DrawLine(basePen, new Point(plot.Left, baseCountY), new Point(plot.Right, baseCountY));
        dc.DrawEllipse(panel, new Pen(muted, 1.7),
            new Point(baseWaitX, baseCountY), 6.0, 6.0);

        var currentPen = new Pen(CloneWithOpacity(accent, 0.65), 1.2) { DashStyle = DashStyles.Dot };
        dc.DrawLine(currentPen, new Point(waitX, plot.Top), new Point(waitX, plot.Bottom));
        dc.DrawLine(currentPen, new Point(plot.Left, countY), new Point(plot.Right, countY));
        dc.DrawEllipse(panel, new Pen(accent, 2.0),
            new Point(waitX, countY), 6.5, 6.5);
        dc.DrawEllipse(accent, null, new Point(waitX, countY), 2.8, 2.8);

        // Only the bottom and left handles change parameters. Clicking inside
        // the plot (including the crosshair) never unexpectedly changes both.
        var waitHandle = new Point(waitX, plot.Bottom + 10.0);
        dc.DrawEllipse(panel, new Pen(accent, IsSelected(wait) ? 2.6 : 1.8),
            waitHandle, 7.0, 7.0);
        dc.DrawEllipse(accent, null, waitHandle, 2.5, 2.5);
        AddHorizontalTarget(wait, new Rect(waitX - 17.0, plot.Bottom + 1.0, 34.0, 22.0),
            plot, wait.Minimum, wait.Maximum);

        var countHandle = new Point(plot.Left - 11.0, countY);
        dc.DrawRectangle(panel, new Pen(accent, IsSelected(count) ? 2.6 : 1.8),
            new Rect(countHandle.X - 6.0, countHandle.Y - 6.0, 12.0, 12.0));
        AddVerticalTarget(count, new Rect(plot.Left - 28.0, countY - 14.0, 27.0, 28.0),
            plot, count.Minimum, count.Maximum);

        DrawCenteredText(dc,
            "LEFT = FASTER WAVES   |   UP = MORE PHANTOMS   |   GREY = BASE",
            new Point(ActualWidth / 2.0, 267.0), 9.3, text, FontWeights.SemiBold);

        var wearCardWidth = Math.Max(235.0, (ActualWidth - 46.0) * 0.60);
        var wearCard = new Rect(18.0, 286.0, wearCardWidth, 72.0);
        DrawHorizontalTrackCard(dc, wear, "PSY GEAR DAMAGE", wearCard,
            text, muted, border, accent, panel);

        var bleedRect = new Rect(wearCard.Right + 10.0, wearCard.Top + 17.0,
            Math.Max(90.0, ActualWidth - wearCard.Right - 28.0), 38.0);
        DrawTogglePill(dc, bleed,
            bleed.ReferenceBoolean ? "BLEED (BASE ON)" : "BLEED (BASE OFF)",
            bleedRect, text, muted, border, accent, panel);
    }

    // This is a two-axis editor, not a fabricated unified "condition lost %" formula.
    // X is the difficulty's armor-toughness scalar; Y is the relative per-shot
    // durability loss for each player weapon. The current point is read-only:
    // the independent axis handles are the only write surfaces.
    private void DrawEquipmentWearBalance(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(
            dc,
            "EQUIPMENT WEAR BALANCE",
            "armor toughness (X) vs player weapon wear per shot (Y)",
            text,
            muted,
            accent);

        var armor = P("equipment.armorDurability");
        var weapon = P("equipment.weaponWearPerShot");
        if (armor is null || weapon is null)
        {
            DrawCenteredText(dc, "Equipment wear data unavailable.", new Point(ActualWidth / 2.0, 150.0), 10.0, muted, FontWeights.SemiBold);
            return;
        }

        // Armor references depend on the chosen Base Preset (e.g. Veteran is
        // 0.9x), while player weapon wear has a shared 1x vanilla reference.
        // Show both exact CFG multipliers rather than suggesting 1x is always
        // vanilla for both axes. Short labels protect the minimum 520px layout.
        var compactLabels = ActualWidth < 690.0;
        var wearLabel = compactLabels ? "WEAPON" : "WEAPON WEAR";
        var armorLabel = compactLabels ? "ARMOR" : "ARMOR DURABILITY";
        DrawText(dc,
            $"{wearLabel}  {weapon.CurrentNumber:0.##}x  |  BASE {weapon.ReferenceNumber:0.##}x",
            new Point(18.0, 52.0), 10.5, accent, FontWeights.SemiBold);
        DrawRightAlignedText(dc,
            $"{armorLabel}  {armor.CurrentNumber:0.##}x  |  BASE {armor.ReferenceNumber:0.##}x",
            new Point(ActualWidth - 18.0, 52.0), 10.5, accent, FontWeights.SemiBold);

        var plot = new Rect(94.0, 81.0, Math.Max(350.0, ActualWidth - 132.0), 156.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), plot, 4.0, 4.0);

        for (var tick = 0; tick <= 4; tick++)
        {
            var armorTick = armor.Minimum + (armor.Maximum - armor.Minimum) * tick / 4.0;
            var armorTickX = ValueToX(armorTick, armor.Minimum, armor.Maximum, plot, reverse: false);
            var weaponTick = weapon.Minimum + (weapon.Maximum - weapon.Minimum) * tick / 4.0;
            var weaponTickY = ValueToY(weaponTick, weapon.Minimum, weapon.Maximum, plot);

            dc.DrawLine(new Pen(CloneWithOpacity(soft, 0.5), 1.0) { DashStyle = DashStyles.Dot },
                new Point(armorTickX, plot.Top), new Point(armorTickX, plot.Bottom));
            dc.DrawLine(new Pen(CloneWithOpacity(soft, 0.5), 1.0) { DashStyle = DashStyles.Dot },
                new Point(plot.Left, weaponTickY), new Point(plot.Right, weaponTickY));

            if (tick is 0 or 2 or 4)
            {
                DrawCenteredText(dc, $"{armorTick:0.##}", new Point(armorTickX, plot.Bottom + 29.0), 9.8, text, FontWeights.Normal);
                DrawRightAlignedText(dc, $"{weaponTick:0.##}", new Point(plot.Left - 29.0, weaponTickY - 7.0), 9.8, text, FontWeights.Normal);
            }
        }

        var armorX = ValueToX(armor.CurrentNumber, armor.Minimum, armor.Maximum, plot, reverse: false);
        var wearY = ValueToY(weapon.CurrentNumber, weapon.Minimum, weapon.Maximum, plot);
        var baseArmorX = ValueToX(armor.ReferenceNumber, armor.Minimum, armor.Maximum, plot, reverse: false);
        var baseWearY = ValueToY(weapon.ReferenceNumber, weapon.Minimum, weapon.Maximum, plot);

        var basePen = new Pen(CloneWithOpacity(muted, 0.6), 1.1) { DashStyle = DashStyles.Dash };
        dc.DrawLine(basePen, new Point(baseArmorX, plot.Top), new Point(baseArmorX, plot.Bottom));
        dc.DrawLine(basePen, new Point(plot.Left, baseWearY), new Point(plot.Right, baseWearY));
        dc.DrawEllipse(panel, new Pen(muted, 1.8), new Point(baseArmorX, baseWearY), 6.0, 6.0);

        var currentPen = new Pen(CloneWithOpacity(accent, 0.65), 1.2) { DashStyle = DashStyles.Dot };
        dc.DrawLine(currentPen, new Point(armorX, plot.Top), new Point(armorX, plot.Bottom));
        dc.DrawLine(currentPen, new Point(plot.Left, wearY), new Point(plot.Right, wearY));
        dc.DrawEllipse(panel, new Pen(accent, 2.0), new Point(armorX, wearY), 6.5, 6.5);
        dc.DrawEllipse(accent, null, new Point(armorX, wearY), 2.8, 2.8);

        // Explicit independent handles avoid a joint drag altering both CFG values.
        var armorHandle = new Point(armorX, plot.Bottom + 10.0);
        dc.DrawEllipse(panel, new Pen(accent, IsSelected(armor) ? 2.6 : 1.8), armorHandle, 7.0, 7.0);
        dc.DrawEllipse(accent, null, armorHandle, 2.5, 2.5);
        AddHorizontalTarget(armor, new Rect(armorX - 17.0, plot.Bottom + 1.0, 34.0, 22.0),
            plot, armor.Minimum, armor.Maximum);

        var weaponHandle = new Point(plot.Left - 11.0, wearY);
        dc.DrawRectangle(panel, new Pen(accent, IsSelected(weapon) ? 2.6 : 1.8),
            new Rect(weaponHandle.X - 6.0, weaponHandle.Y - 6.0, 12.0, 12.0));
        AddVerticalTarget(weapon, new Rect(plot.Left - 28.0, wearY - 14.0, 27.0, 28.0),
            plot, weapon.Minimum, weapon.Maximum);

        // Leave the status lane below y=290 clear for transient reset messages.
        DrawCenteredText(dc, "ARMOR → MORE DURABLE   |   WEAPON ↑ MORE WEAR / SHOT   |   GREY = BASE",
            new Point(ActualWidth / 2.0, 280.0), 9.5, text, FontWeights.SemiBold);
    }

    // The reputation curve is a read-only overview. Each modifier is adjusted on
    // its own full-width track, avoiding overlapping handles or text on the graph.
    // The global value-based constraint still owns HOSTILE >= DISLIKED >= NEUTRAL >= FRIENDLY.
    private void DrawRepairCurve(
        DrawingContext dc, Brush text, Brush muted, Brush border, Brush soft,
        Brush accent, Brush panel)
    {
        var hostile = P("economy.repairEnemy");
        var disliked = P("economy.repairDisaffection");
        var neutral = P("economy.repairNeutral");
        var friendly = P("economy.repairFriend");

        DrawText(dc, "REPAIR PRICE BY REPUTATION", new Point(17.0, 13.0),
            11.5, accent, FontWeights.Bold);
        DrawText(dc, "your standing with a technician's faction changes repair costs",
            new Point(17.0, 32.0), 9.5, text, FontWeights.Normal);

        if (hostile is null || disliked is null || neutral is null || friendly is null)
        {
            DrawCenteredText(dc, "Repair price controls unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var parameters = new[] { hostile, disliked, neutral, friendly };
        var labels = new[] { "HOSTILE", "DISLIKED", "NEUTRAL", "FRIENDLY" };
        var contentWidth = Math.Max(460.0, ActualWidth - 34.0);
        var overview = new Rect(17.0, 55.0, contentWidth, 164.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), overview, 4.0, 4.0);

        DrawText(dc, "REPUTATION REPAIR CURVE", new Point(29.0, 64.0),
            10.1, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "GOLD: CURRENT  |  DASHED: BASE PRESET",
            new Point(overview.Right - 12.0, 64.0), 9.0, muted, FontWeights.SemiBold);
        DrawText(dc, "1x = UNMODIFIED REPAIR PRICE  /  HIGHER = SURCHARGE",
            new Point(29.0, 81.0), 9.0, muted, FontWeights.Normal);

        // Shared plot is explanatory only; values remain editable in the four
        // separate source-range sliders underneath.
        var plot = new Rect(76.0, 101.0, Math.Max(360.0, ActualWidth - 114.0), 66.0);
        const double chartMin = 0.0;
        const double chartMax = 3.0;
        var xs = new[]
        {
            plot.Left + plot.Width * 0.10,
            plot.Left + plot.Width * 0.36,
            plot.Left + plot.Width * 0.64,
            plot.Left + plot.Width * 0.90
        };
        var live = parameters.Select(p => p.CurrentNumber).ToArray();
        var reference = parameters.Select(p => p.ReferenceNumber).ToArray();

        var baseY = ValueToY(1.0, chartMin, chartMax, plot);
        dc.DrawRectangle(CloneWithOpacity(muted, 0.06), null,
            new Rect(plot.Left, plot.Top, plot.Width, Math.Max(0.0, baseY - plot.Top)));
        dc.DrawRectangle(CloneWithOpacity(accent, 0.04), null,
            new Rect(plot.Left, baseY, plot.Width, Math.Max(0.0, plot.Bottom - baseY)));

        for (var value = 0; value <= 3; value++)
        {
            var y = ValueToY(value, chartMin, chartMax, plot);
            var pen = new Pen(value == 1 ? accent : soft, value == 1 ? 1.4 : 1.0);
            if (value != 1)
                pen.DashStyle = DashStyles.Dot;
            dc.DrawLine(pen, new Point(plot.Left, y), new Point(plot.Right, y));
            DrawRightAlignedText(dc, value == 1 ? "1x BASE" : $"{value}x",
                new Point(plot.Left - 9.0, y - 7.0),
                9.0, value == 1 ? accent : muted, FontWeights.SemiBold);
        }

        DrawCurveLine(dc, xs, reference, plot, chartMin, chartMax,
            new Pen(muted, 1.6) { DashStyle = DashStyles.Dash });
        DrawCurveLine(dc, xs, live, plot, chartMin, chartMax,
            new Pen(accent, 2.8));

        for (var i = 0; i < parameters.Length; i++)
        {
            var point = new Point(xs[i], ValueToY(live[i], chartMin, chartMax, plot));
            var referencePoint = new Point(xs[i],
                ValueToY(reference[i], chartMin, chartMax, plot));

            dc.DrawEllipse(panel,
                new Pen(muted, 1.4) { DashStyle = DashStyles.Dash },
                referencePoint, 5.0, 5.0);
            dc.DrawEllipse(panel,
                new Pen(accent, IsSelected(parameters[i]) ? 2.8 : 1.7),
                point, 6.5, 6.5);
            dc.DrawEllipse(accent, null, point, 2.6, 2.6);

            DrawCenteredText(dc, labels[i], new Point(xs[i], 174.0),
                9.2, text, FontWeights.Bold);
            DrawCenteredText(dc, $"{live[i]:0.##}x", new Point(xs[i], 191.0),
                10.1, accent, FontWeights.Bold);
        }

        string PriceHint(double multiplier)
        {
            var delta = (multiplier - 1.0) * 100.0;
            if (Math.Abs(delta) < 0.5)
                return "UNCHANGED BASE REPAIR LAYER";
            return delta > 0.0
                ? $"+{delta:0}% SURCHARGE VS 1x"
                : $"{-delta:0}% DISCOUNT VS 1x";
        }

        void DrawReputationTrack(ParameterViewModel parameter, string label, double top)
        {
            var card = new Rect(17.0, top, contentWidth, 75.0);
            dc.DrawRoundedRectangle(panel, new Pen(border, 1.0),
                card, 4.0, 4.0);
            DrawText(dc, label, new Point(card.Left + 12.0, top + 7.0),
                10.5, text, FontWeights.Bold);
            DrawRightAlignedText(dc,
                $"{parameter.CurrentNumber:0.##}x  |  BASE {parameter.ReferenceNumber:0.##}x",
                new Point(card.Right - 12.0, top + 7.0), 11.0,
                parameter.IsControlEnabled ? accent : muted, FontWeights.Bold);
            DrawText(dc, PriceHint(parameter.CurrentNumber),
                new Point(card.Left + 12.0, top + 24.0),
                9.1, muted, FontWeights.SemiBold);

            var track = new Rect(card.Left + 22.0, top + 40.0,
                card.Width - 44.0, 20.0);
            var y = track.Top + track.Height / 2.0;
            var currentX = ValueToX(parameter.CurrentNumber,
                parameter.Minimum, parameter.Maximum, track, reverse: false);
            var referenceX = ValueToX(parameter.ReferenceNumber,
                parameter.Minimum, parameter.Maximum, track, reverse: false);
            var unmodifiedX = ValueToX(1.0,
                parameter.Minimum, parameter.Maximum, track, reverse: false);

            dc.DrawLine(new Pen(soft, 4.0),
                new Point(track.Left, y), new Point(track.Right, y));
            if (parameter.IsControlEnabled)
                dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.72), 4.5),
                    new Point(track.Left, y), new Point(currentX, y));
            // Pale tick: unmodified 1x. Grey tick: selected Base Preset.
            dc.DrawLine(new Pen(soft, 1.3) { DashStyle = DashStyles.Dash },
                new Point(unmodifiedX, y - 12.0), new Point(unmodifiedX, y + 12.0));
            dc.DrawLine(new Pen(muted, 2.0),
                new Point(referenceX, y - 10.0), new Point(referenceX, y + 10.0));
            dc.DrawEllipse(panel,
                new Pen(parameter.IsControlEnabled ? accent : muted,
                    IsSelected(parameter) ? 2.8 : 1.9),
                new Point(currentX, y), 7.0, 7.0);
            if (parameter.IsControlEnabled)
                dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

            AddHorizontalTarget(parameter,
                new Rect(track.Left - 8.0, track.Top - 4.0,
                    track.Width + 16.0, track.Height + 8.0),
                track, parameter.Minimum, parameter.Maximum);
            DrawText(dc, $"{parameter.Minimum:0.##}x",
                new Point(track.Left, top + 61.0),
                9.1, muted, FontWeights.Normal);
            DrawRightAlignedText(dc, $"{parameter.Maximum:0.##}x",
                new Point(track.Right, top + 61.0),
                9.1, muted, FontWeights.Normal);
        }

        DrawReputationTrack(hostile, "HOSTILE / ENEMY", 231.0);
        DrawReputationTrack(disliked, "DISLIKED", 312.0);
        DrawReputationTrack(neutral, "NEUTRAL", 393.0);
        DrawReputationTrack(friendly, "FRIENDLY", 474.0);

        var monotonic = hostile.CurrentNumber + Epsilon >= disliked.CurrentNumber
            && disliked.CurrentNumber + Epsilon >= neutral.CurrentNumber
            && neutral.CurrentNumber + Epsilon >= friendly.CurrentNumber;
        DrawText(dc,
            monotonic
                ? "ORDER: HOSTILE >= DISLIKED >= NEUTRAL >= FRIENDLY"
                : "REPUTATION ORDER IS BEING NORMALIZED",
            new Point(20.0, 566.0), 9.2, monotonic ? muted : accent,
            FontWeights.SemiBold);
        DrawText(dc,
            "RIGHT-CLICK = RESET ONE  |  R = RESET ALL  |  PRICE MODIFIER, NOT FINAL REPAIR BILL",
            new Point(20.0, 585.0), 9.0, muted, FontWeights.Normal);
    }

    private void DrawBleedingTreatmentMixer(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        DrawHeader(
            dc,
            "BLEEDING TREATMENT",
            "each item is measured against its own vanilla bleeding-removal strength",
            text,
            muted,
            accent);

        var parameters = new[]
        {
            P("health.bandageBleeding"),
            P("health.medkitBleeding"),
            P("health.armyMedkitBleeding"),
            P("health.ecoMedkitBleeding")
        };
        var labels = new[] { "BANDAGE", "MEDKIT", "ARMY", "SCIENTIFIC" };

        var area = new Rect(24.0, 66.0, Math.Max(440.0, ActualWidth - 48.0), 286.0);
        var scaleWidth = 52.0;
        var faderArea = new Rect(
            area.Left + scaleWidth,
            area.Top,
            Math.Max(360.0, area.Width - scaleWidth),
            area.Height);

        var count = Math.Min(parameters.Length, labels.Length);
        var columnWidth = faderArea.Width / Math.Max(1, count);
        var trackTop = area.Top + 80.0;
        var trackBottom = area.Bottom - 40.0;
        var trackHeight = Math.Max(60.0, trackBottom - trackTop);

        // All four backing sliders are defined as 0..200% of their own vanilla value.
        // A shared normalized scale makes the mixer visually comparable without
        // pretending that the raw bleeding-removal values use the same units.
        var sharedMap = new Rect(faderArea.Left, trackTop, faderArea.Width, trackHeight);
        var vanillaY = ValueToY(1.0, 0.0, 2.0, sharedMap);

        var strongerFill = CloneWithOpacity(accent, 0.045);
        var weakerFill = CloneWithOpacity(muted, 0.035);

        dc.DrawRectangle(
            strongerFill,
            null,
            new Rect(faderArea.Left, trackTop, faderArea.Width, Math.Max(0.0, vanillaY - trackTop)));

        dc.DrawRectangle(
            weakerFill,
            null,
            new Rect(faderArea.Left, vanillaY, faderArea.Width, Math.Max(0.0, trackBottom - vanillaY)));

        DrawRightAlignedText(dc, "200%", new Point(area.Left + scaleWidth - 9.0, trackTop - 7.0), 8.0, muted, FontWeights.SemiBold);
        DrawRightAlignedText(dc, "100%", new Point(area.Left + scaleWidth - 9.0, vanillaY - 7.0), 8.2, accent, FontWeights.Bold);
        DrawRightAlignedText(dc, "0%", new Point(area.Left + scaleWidth - 9.0, trackBottom - 7.0), 8.0, muted, FontWeights.SemiBold);

        DrawRightAlignedText(dc, "STRONGER", new Point(area.Left + scaleWidth - 9.0, trackTop + 13.0), 7.2, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, "WEAKER", new Point(area.Left + scaleWidth - 9.0, trackBottom - 22.0), 7.2, muted, FontWeights.Normal);

        var vanillaPen = new Pen(accent, 1.25) { DashStyle = DashStyles.Dash };
        dc.DrawLine(vanillaPen, new Point(faderArea.Left, vanillaY), new Point(faderArea.Right, vanillaY));

        DrawText(dc, "VANILLA", new Point(faderArea.Left + 5.0, vanillaY - 18.0), 7.8, accent, FontWeights.Bold);

        for (var i = 0; i < count; i++)
        {
            if (parameters[i] is not { } parameter)
                continue;

            var centerX = faderArea.Left + columnWidth * (i + 0.5);
            var reference = Math.Abs(parameter.ReferenceNumber) > Epsilon
                ? parameter.ReferenceNumber
                : 1.0;

            var relativeMinimum = parameter.Minimum / reference;
            var relativeMaximum = parameter.Maximum / reference;
            var relativeCurrent = parameter.CurrentNumber / reference;

            DrawCenteredText(dc, labels[i], new Point(centerX, area.Top + 2.0), 9.0, text, FontWeights.Bold);
            DrawCenteredText(
                dc,
                $"{relativeCurrent * 100.0:0}% VANILLA",
                new Point(centerX, area.Top + 22.0),
                9.2,
                accent,
                FontWeights.Bold);

            DrawCenteredText(
                dc,
                $"{parameter.CurrentNumber:0.##} bleeding",
                new Point(centerX, area.Top + 39.0),
                7.9,
                muted,
                FontWeights.Normal);

            var mapRect = new Rect(centerX - 14.0, trackTop, 28.0, trackHeight);
            dc.DrawLine(new Pen(border, 4.0), new Point(centerX, trackTop), new Point(centerX, trackBottom));

            var currentY = ValueToY(relativeCurrent, relativeMinimum, relativeMaximum, mapRect);
            var thumb = new Rect(centerX - 14.0, currentY - 5.0, 28.0, 10.0);
            dc.DrawRoundedRectangle(
                panel,
                new Pen(accent, IsSelected(parameter) ? 2.5 : 1.7),
                thumb,
                2.0,
                2.0);
            dc.DrawRectangle(accent, null, new Rect(centerX - 9.0, currentY - 1.0, 18.0, 2.0));

            AddVerticalTarget(
                parameter,
                new Rect(
                    centerX - Math.Max(20.0, columnWidth * 0.36),
                    trackTop - 9.0,
                    Math.Max(40.0, columnWidth * 0.72),
                    trackHeight + 18.0),
                mapRect,
                parameter.Minimum,
                parameter.Maximum);

            DrawCenteredText(
                dc,
                $"vanilla {parameter.ReferenceNumber:0.##}",
                new Point(centerX, trackBottom + 14.0),
                7.7,
                muted,
                FontWeights.Normal);
        }

        DrawCenteredText(
            dc,
            "100% = VANILLA  •  drag to tune  •  right-click a fader to reset it",
            new Point(ActualWidth / 2.0, 366.0),
            8.2,
            muted,
            FontWeights.SemiBold);
    }

    private void DrawConsumableMixer(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        DrawHeader(
            dc,
            "CONSUMABLE EFFECT BOARD",
            "instant + sustained strength; timed effects share one real seconds axis",
            text,
            muted,
            accent);

        var stamina = P("survival.instantStaminaRestore");
        var waterStamina = P("survival.waterStaminaEffectStrength");
        var energyStamina = P("survival.energyDrinkStaminaRecoveryStrength");
        var antirad = P("survival.alcoholRadiationRemoval");

        var left = 22.0;
        var right = Math.Max(left + 460.0, ActualWidth - 22.0);
        var width = right - left;
        var gap = 12.0;
        var half = (width - gap) / 2.0;

        void DrawStrengthCard(
            ParameterViewModel? parameter,
            string title,
            string subtitle,
            IReadOnlyList<(string Label, double VanillaValue)> authored,
            string unit,
            Rect card)
        {
            dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.95), new Pen(border, 1.0), card, 4.0, 4.0);
            DrawText(dc, title, new Point(card.Left + 12.0, card.Top + 9.0), 8.9, text, FontWeights.Bold);
            DrawText(dc, subtitle, new Point(card.Left + 12.0, card.Top + 25.0), 7.5, muted, FontWeights.Normal);

            if (parameter is null)
                return;

            DrawRightAlignedText(
                dc,
                $"{parameter.CurrentNumber:0.##}x",
                new Point(card.Right - 12.0, card.Top + 8.0),
                10.0,
                accent,
                FontWeights.Bold);

            var maxAuthored = authored.Max(x => x.VanillaValue);
            var maxPreview = Math.Max(Epsilon, maxAuthored * parameter.Maximum);
            var barLeft = card.Left + 74.0;
            var barRight = card.Right - 14.0;
            var barWidth = Math.Max(80.0, barRight - barLeft);

            for (var i = 0; i < authored.Count; i++)
            {
                var item = authored[i];
                var y = card.Top + 48.0 + i * 21.0;
                var current = item.VanillaValue * parameter.CurrentNumber;
                var vanilla = item.VanillaValue;

                DrawRightAlignedText(dc, item.Label, new Point(barLeft - 8.0, y - 7.0), 7.6, muted, FontWeights.Bold);
                dc.DrawLine(new Pen(border, 4.0), new Point(barLeft, y), new Point(barRight, y));

                var vanillaX = barLeft + barWidth * Math.Clamp(vanilla / maxPreview, 0.0, 1.0);
                var currentX = barLeft + barWidth * Math.Clamp(current / maxPreview, 0.0, 1.0);

                dc.DrawLine(new Pen(muted, 1.0), new Point(vanillaX, y - 7.0), new Point(vanillaX, y + 7.0));
                dc.DrawLine(new Pen(accent, 4.0), new Point(barLeft, y), new Point(currentX, y));
                dc.DrawEllipse(accent, null, new Point(currentX, y), 4.5, 4.5);

                DrawRightAlignedText(
                    dc,
                    $"{current:0.#}{unit}",
                    new Point(barRight, y - 17.0),
                    7.6,
                    accent,
                    FontWeights.SemiBold);
            }

            var controlTrack = new Rect(card.Left + 14.0, card.Bottom - 22.0, card.Width - 28.0, 14.0);
            var controlY = controlTrack.Top + controlTrack.Height / 2.0;
            dc.DrawLine(new Pen(border, 3.0), new Point(controlTrack.Left, controlY), new Point(controlTrack.Right, controlY));

            var vanillaControlX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, controlTrack, reverse: false);
            var currentControlX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, controlTrack, reverse: false);

            dc.DrawLine(new Pen(muted, 1.1), new Point(vanillaControlX, controlY - 7.0), new Point(vanillaControlX, controlY + 7.0));
            dc.DrawEllipse(
                panel,
                new Pen(accent, IsSelected(parameter) ? 2.5 : 1.6),
                new Point(currentControlX, controlY),
                6.0,
                6.0);
            dc.DrawEllipse(accent, null, new Point(currentControlX, controlY), 2.8, 2.8);

            DrawText(dc, "0x", new Point(controlTrack.Left, controlY + 8.0), 7.2, muted, FontWeights.Normal);
            DrawCenteredText(dc, "VANILLA 1x", new Point(vanillaControlX, controlY + 8.0), 7.1, muted, FontWeights.SemiBold);
            DrawRightAlignedText(dc, "2x", new Point(controlTrack.Right, controlY + 8.0), 7.2, muted, FontWeights.Normal);

            AddHorizontalTarget(
                parameter,
                new Rect(controlTrack.Left - 7.0, controlTrack.Top - 5.0, controlTrack.Width + 14.0, controlTrack.Height + 10.0),
                controlTrack,
                parameter.Minimum,
                parameter.Maximum);
        }

        void DrawSustainedTrack(
            ParameterViewModel? parameter,
            string label,
            string preview,
            Rect card)
        {
            dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.78), new Pen(border, 0.9), card, 3.0, 3.0);
            DrawText(dc, label, new Point(card.Left + 9.0, card.Top + 5.0), 8.3, text, FontWeights.Bold);

            if (parameter is null)
                return;

            DrawRightAlignedText(
                dc,
                $"{parameter.CurrentNumber:0.##}x",
                new Point(card.Right - 9.0, card.Top + 4.0),
                8.8,
                accent,
                FontWeights.Bold);

            DrawText(dc, preview, new Point(card.Left + 9.0, card.Top + 18.0), 8.1, text, FontWeights.SemiBold);

            var track = new Rect(card.Left + 9.0, card.Bottom - 10.0, card.Width - 18.0, 8.0);
            var y = track.Top + track.Height / 2.0;
            dc.DrawLine(new Pen(border, 2.6), new Point(track.Left, y), new Point(track.Right, y));

            var vanillaX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, track, reverse: false);
            var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track, reverse: false);

            dc.DrawLine(new Pen(muted, 1.0), new Point(vanillaX, y - 5.0), new Point(vanillaX, y + 5.0));
            dc.DrawEllipse(
                panel,
                new Pen(accent, IsSelected(parameter) ? 2.4 : 1.5),
                new Point(currentX, y),
                5.2,
                5.2);
            dc.DrawEllipse(accent, null, new Point(currentX, y), 2.4, 2.4);

            AddHorizontalTarget(
                parameter,
                new Rect(track.Left - 6.0, track.Top - 6.0, track.Width + 12.0, track.Height + 12.0),
                track,
                parameter.Minimum,
                parameter.Maximum);
        }

        DrawStrengthCard(
            stamina,
            "INSTANT STAMINA BURST",
            "same multiplier preserves each drink's authored refill strength",
            new[]
            {
                ("WATER", 38.0),
                ("ENERGY", 50.0),
                ("LIMITED", 100.0)
            },
            "%",
            new Rect(left, 56.0, half, 130.0));

        DrawStrengthCard(
            antirad,
            "ALCOHOL RADIATION CLEANUP",
            "ordinary beer and vodka only — Magic Vodka is separate",
            new[]
            {
                ("BEER", 10.0),
                ("VODKA", 50.0)
            },
            " rad",
            new Rect(left + half + gap, 56.0, half, 130.0));

        DrawText(dc, "SUSTAINED STAMINA", new Point(left + 2.0, 194.0), 8.8, text, FontWeights.Bold);
        DrawText(dc, "0x–2x strength  •  grey tick = vanilla 1x  •  duration independent", new Point(left + 119.0, 194.0), 7.6, muted, FontWeights.Normal);

        DrawSustainedTrack(
            waterStamina,
            "WATER",
            waterStamina is null
                ? string.Empty
                : $"REGEN +{25.0 * waterStamina.CurrentNumber:0.#}%  |  ACTION COST -{10.0 * waterStamina.CurrentNumber:0.#}%",
            new Rect(left, 207.0, half, 40.0));

        DrawSustainedTrack(
            energyStamina,
            "ENERGY DRINK",
            energyStamina is null
                ? string.Empty
                : $"REGEN +{200.0 * energyStamina.CurrentNumber:0.#}%",
            new Rect(left + half + gap, 207.0, half, 40.0));

        DrawText(dc, "TIMED EFFECTS", new Point(left + 2.0, 253.0), 8.8, text, FontWeights.Bold);
        DrawText(
            dc,
            "shared real-seconds axis",
            new Point(left + 112.0, 253.0),
            7.5,
            muted,
            FontWeights.Normal);

        var plot = new Rect(left + 102.0, 282.0, width - 116.0, 136.0);
        const double timelineMaxSeconds = 270.0;

        double SecondsToX(double seconds)
            => plot.Left + plot.Width * Math.Clamp(seconds / timelineMaxSeconds, 0.0, 1.0);

        dc.DrawRoundedRectangle(
            CloneWithOpacity(panel, 0.95),
            new Pen(border, 1.0),
            new Rect(plot.Left - 10.0, plot.Top - 12.0, plot.Width + 20.0, plot.Height + 20.0),
            4.0,
            4.0);

        for (var seconds = 0; seconds <= 270; seconds += 30)
        {
            var x = SecondsToX(seconds);
            var pen = new Pen(seconds == 90 ? accent : border, seconds == 90 ? 1.1 : 0.8);
            if (seconds != 90)
                pen.DashStyle = DashStyles.Dot;

            dc.DrawLine(pen, new Point(x, plot.Top), new Point(x, plot.Bottom));
            DrawCenteredText(dc, $"{seconds}s", new Point(x, plot.Top - 19.0), 7.1, seconds == 90 ? accent : muted, FontWeights.SemiBold);
        }

        var durationRows = new[]
        {
            (
                Label: "WATER",
                Parameter: P("survival.waterEffectDuration"),
                Effects: new[] { ("REGEN", 5.0), ("COST", 30.0) }
            ),
            (
                Label: "ENERGY",
                Parameter: P("survival.energyDrinkEffectDuration"),
                Effects: new[] { ("MAIN", 45.0), ("ACTION", 15.0), ("SHORT", 5.0) }
            ),
            (
                Label: "VODKA",
                Parameter: P("survival.vodkaEffectDuration"),
                Effects: new[] { ("STAMINA PENALTY", 20.0) }
            ),
            (
                Label: "BEER",
                Parameter: P("survival.beerEffectDuration"),
                Effects: new[] { ("STAMINA PENALTY", 20.0) }
            ),
            (
                Label: "MAGIC",
                Parameter: P("survival.magicVodkaEffectDuration"),
                Effects: new[] { ("PSI PROTECTION", 90.0) }
            )
        };

        for (var row = 0; row < durationRows.Length; row++)
        {
            var item = durationRows[row];
            var y = plot.Top + 18.0 + row * 25.0;

            DrawRightAlignedText(dc, item.Label, new Point(plot.Left - 18.0, y - 8.0), 8.0, text, FontWeights.Bold);

            if (item.Parameter is not { } parameter)
                continue;

            var scale = parameter.CurrentNumber;
            var longestBase = item.Effects.Max(x => x.Item2);
            var longestCurrent = longestBase * scale;
            var longestX = SecondsToX(longestCurrent);

            dc.DrawLine(new Pen(border, 3.0), new Point(plot.Left, y), new Point(SecondsToX(longestBase * parameter.Maximum), y));

            for (var effectIndex = 0; effectIndex < item.Effects.Length; effectIndex++)
            {
                var effect = item.Effects[effectIndex];
                var effectSeconds = effect.Item2 * scale;
                var endX = SecondsToX(effectSeconds);
                var lineY = y + (effectIndex - (item.Effects.Length - 1) / 2.0) * 5.0;
                var opacity = Math.Max(0.35, 0.95 - effectIndex * 0.22);

                dc.DrawLine(
                    new Pen(CloneWithOpacity(accent, opacity), effectIndex == 0 ? 3.6 : 2.2),
                    new Point(plot.Left, lineY),
                    new Point(endX, lineY));

                dc.DrawEllipse(
                    CloneWithOpacity(accent, opacity),
                    null,
                    new Point(endX, lineY),
                    effectIndex == 0 ? 3.8 : 2.8,
                    effectIndex == 0 ? 3.8 : 2.8);

                if (effectIndex == 0 || item.Effects.Length == 1)
                {
                    DrawText(
                        dc,
                        $"{effect.Item1} {effectSeconds:0.#}s",
                        new Point(Math.Min(endX + 7.0, plot.Right - 92.0), lineY - 8.0),
                        7.2,
                        effectIndex == 0 ? accent : muted,
                        FontWeights.SemiBold);
                }
            }

            var vanillaLongestX = SecondsToX(longestBase);
            dc.DrawLine(new Pen(muted, 1.0), new Point(vanillaLongestX, y - 9.0), new Point(vanillaLongestX, y + 9.0));

            dc.DrawEllipse(
                panel,
                new Pen(accent, IsSelected(parameter) ? 2.4 : 1.5),
                new Point(longestX, y),
                5.8,
                5.8);
            dc.DrawEllipse(accent, null, new Point(longestX, y), 2.6, 2.6);

            DrawRightAlignedText(
                dc,
                $"{scale:0.##}x",
                new Point(plot.Right, y - 15.0),
                7.7,
                accent,
                FontWeights.Bold);

            var familyMaxSeconds = longestBase * parameter.Maximum;
            var familyMap = new Rect(
                plot.Left,
                y - 11.0,
                Math.Max(1.0, SecondsToX(familyMaxSeconds) - plot.Left),
                22.0);

            AddHorizontalTarget(
                parameter,
                new Rect(familyMap.Left - 7.0, familyMap.Top - 4.0, familyMap.Width + 14.0, familyMap.Height + 8.0),
                familyMap,
                parameter.Minimum,
                parameter.Maximum);
        }

        DrawCenteredText(
            dc,
            "GREY TICK = VANILLA LONGEST EFFECT   •   GOLD ENDPOINT = CURRENT LONGEST EFFECT",
            new Point(ActualWidth / 2.0, 438.0),
            7.5,
            muted,
            FontWeights.Normal);

        DrawCenteredText(
            dc,
            "DRUNKENNESS / OVERDRUNKENNESS ARE CONTROLLED SEPARATELY",
            new Point(ActualWidth / 2.0, 456.0),
            7.7,
            muted,
            FontWeights.SemiBold);
    }

    // Sleeping is four independent CFG settings, not a simulated fatigue-to-HP
    // formula. Keep the sleep gate and time requirement distinct from wake-up
    // effects, and give every setting its native-range horizontal editor.
    private void DrawSleepingDashboard(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var threshold = P("survival.sleepEligibilityThreshold");
        var minimumHours = P("survival.minimumSleepHours");
        var healthRestore = P("survival.sleepHealthRestore");
        var staminaRestore = P("survival.sleepStaminaRestore");

        DrawText(dc, "SLEEPING SYSTEM", new Point(17.0, 13.0),
            11.5, accent, FontWeights.Bold);
        DrawText(dc, "when you can sleep, how long you must rest and what you recover",
            new Point(17.0, 32.0), 9.5, text, FontWeights.Normal);

        if (threshold is null || minimumHours is null ||
            healthRestore is null || staminaRestore is null)
        {
            DrawCenteredText(dc, "Sleeping system data unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var width = Math.Max(460.0, ActualWidth - 34.0);

        // Both access controls operate on the authored sleep settings, not the
        // player's current in-game sleepiness or a forecast of actual sleep time.
        void DrawSleepTrack(
            ParameterViewModel parameter,
            string title,
            string explanation,
            string displayedValue,
            string leftLabel,
            string rightLabel,
            double top)
        {
            var card = new Rect(17.0, top, width, 82.0);
            dc.DrawRoundedRectangle(panel, new Pen(border, 1.0),
                card, 4.0, 4.0);

            DrawText(dc, title, new Point(card.Left + 12.0, top + 7.0),
                10.4, text, FontWeights.Bold);
            DrawRightAlignedText(dc, displayedValue,
                new Point(card.Right - 12.0, top + 7.0), 11.0,
                parameter.IsControlEnabled ? accent : muted, FontWeights.Bold);
            DrawText(dc, explanation, new Point(card.Left + 12.0, top + 26.0),
                9.4, muted, FontWeights.Normal);

            var track = new Rect(card.Left + 22.0, top + 44.0,
                card.Width - 44.0, 18.0);
            var y = track.Top + track.Height / 2.0;
            var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum,
                parameter.Maximum, track, reverse: false);
            var baseX = ValueToX(parameter.ReferenceNumber, parameter.Minimum,
                parameter.Maximum, track, reverse: false);

            dc.DrawLine(new Pen(border, 4.0),
                new Point(track.Left, y), new Point(track.Right, y));
            if (parameter.IsControlEnabled)
                dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.72), 4.5),
                    new Point(track.Left, y), new Point(currentX, y));

            // Base Preset marker is drawn behind the active gold handle even
            // when both are at the same coordinate.
            dc.DrawLine(new Pen(muted, 2.0),
                new Point(baseX, y - 11.0), new Point(baseX, y + 11.0));
            dc.DrawEllipse(panel,
                new Pen(parameter.IsControlEnabled ? accent : muted,
                    IsSelected(parameter) ? 2.8 : 1.9),
                new Point(currentX, y), 7.0, 7.0);
            if (parameter.IsControlEnabled)
                dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

            AddHorizontalTarget(parameter,
                new Rect(track.Left - 8.0, track.Top - 4.0,
                    track.Width + 16.0, track.Height + 8.0),
                track, parameter.Minimum, parameter.Maximum);
            DrawText(dc, leftLabel, new Point(track.Left, top + 65.0),
                9.1, muted, FontWeights.Normal);
            DrawRightAlignedText(dc, rightLabel, new Point(track.Right, top + 65.0),
                9.1, muted, FontWeights.Normal);
        }

        DrawSleepTrack(threshold,
            "SLEEP BECOMES AVAILABLE AT",
            "Required sleepiness points; this is a rule, not your live fatigue meter.",
            $"{threshold.CurrentNumber:0} pts  |  BASE {threshold.ReferenceNumber:0} pts",
            $"{threshold.Minimum:0} pts", $"{threshold.Maximum:0} pts", 55.0);

        DrawSleepTrack(minimumHours,
            "MINIMUM SLEEP",
            "Minimum hours allowed by the normal sleep configuration.",
            $"{minimumHours.CurrentNumber:0} h  |  BASE {minimumHours.ReferenceNumber:0} h",
            $"{minimumHours.Minimum:0} h", $"{minimumHours.Maximum:0} h", 148.0);

        DrawText(dc, "WHEN YOU WAKE UP", new Point(19.0, 248.0),
            10.5, accent, FontWeights.Bold);
        DrawRightAlignedText(dc, "RESTORE EFFECTS, NOT PASSIVE REGENERATION",
            new Point(ActualWidth - 19.0, 248.0),
            9.1, muted, FontWeights.SemiBold);

        DrawSleepTrack(healthRestore,
            "POST-SLEEP HEALTH RESTORE",
            "Direct health restoration; actual HP cannot exceed your health cap.",
            $"{healthRestore.CurrentNumber:0} HP  |  BASE {healthRestore.ReferenceNumber:0} HP",
            $"{healthRestore.Minimum:0} HP", $"{healthRestore.Maximum:0} HP", 272.0);

        DrawSleepTrack(staminaRestore,
            "POST-SLEEP STAMINA EFFECT",
            "Source effect strength; does not change your maximum stamina.",
            $"+{staminaRestore.CurrentNumber * 100:0}%  |  BASE +{staminaRestore.ReferenceNumber * 100:0}%",
            $"{staminaRestore.Minimum * 100:0}%", $"{staminaRestore.Maximum * 100:0}%", 365.0);

        DrawText(dc,
            "GREY TICK = BASE PRESET  |  RIGHT-CLICK = RESET ONE  |  R = RESET ALL",
            new Point(20.0, 470.0), 9.1, muted, FontWeights.SemiBold);
    }

    private void DrawRecoveryMeter(
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

        DrawRightAlignedText(dc, FriendlyValue(parameter), new Point(card.Right - 10.0, card.Top + 6.0), 10.0, accent, FontWeights.Bold);

        var track = new Rect(card.Left + 12.0, card.Top + 35.0, Math.Max(50.0, card.Width - 24.0), 20.0);
        var y = track.Top + track.Height / 2.0;
        dc.DrawLine(new Pen(border, 5.0), new Point(track.Left, y), new Point(track.Right, y));

        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track, reverse: false);
        dc.DrawLine(new Pen(accent, 5.0), new Point(track.Left, y), new Point(currentX, y));

        var vanillaX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, track, reverse: false);
        dc.DrawLine(new Pen(muted, 1.2), new Point(vanillaX, y - 8.0), new Point(vanillaX, y + 8.0));

        dc.DrawEllipse(panel, new Pen(accent, IsSelected(parameter) ? 2.4 : 1.6), new Point(currentX, y), 6.0, 6.0);
        AddHorizontalTarget(parameter, track, track, parameter.Minimum, parameter.Maximum);
    }

    // Separate the two trader price settings, item acceptance gate and broken-value
    // floor. Price settings are CFG multipliers, not complete shop-price formulas.
    private void DrawTradingDashboard(
        DrawingContext dc, Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        var traderBuys = P("economy.buyCondition");
        var traderSells = P("economy.sellCondition");
        var minCondition = P("economy.traderMinBuyCondition");
        var valueFloor = P("economy.itemValueFloor");

        DrawText(dc, "TRADING FLOW", new Point(17.0, 13.0),
            11.5, accent, FontWeights.Bold);
        DrawText(dc, "what you earn, what you pay and which damaged gear traders accept",
            new Point(17.0, 32.0), 9.5, text, FontWeights.Normal);

        if (traderBuys is null || traderSells is null ||
            minCondition is null || valueFloor is null)
        {
            DrawCenteredText(dc, "Trading controls unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var width = Math.Max(460.0, ActualWidth - 34.0);
        var overview = new Rect(17.0, 55.0, width, 66.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), overview, 4.0, 4.0);
        var midpoint = overview.Left + overview.Width / 2.0;
        dc.DrawLine(new Pen(border, 1.0),
            new Point(midpoint, overview.Top + 10.0),
            new Point(midpoint, overview.Bottom - 21.0));

        DrawText(dc, "YOU SELL  →  TRADER BUYS", new Point(overview.Left + 12.0, 64.0),
            10.1, accent, FontWeights.Bold);
        DrawText(dc, "YOU RECEIVE COUPONS", new Point(overview.Left + 12.0, 85.0),
            9.3, text, FontWeights.SemiBold);
        DrawText(dc, "TRADER SELLS  →  YOU BUY", new Point(midpoint + 12.0, 64.0),
            10.1, accent, FontWeights.Bold);
        DrawText(dc, "YOU SPEND COUPONS", new Point(midpoint + 12.0, 85.0),
            9.3, text, FontWeights.SemiBold);
        DrawText(dc, "Difficulty pricing values feed trader-specific shop rules.",
            new Point(overview.Left + 12.0, 103.0), 9.0, muted, FontWeights.Normal);

        void DrawPricingTrack(ParameterViewModel parameter, string label, string detail, double top)
        {
            var card = new Rect(17.0, top, width, 91.0);
            dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), card, 4.0, 4.0);
            DrawText(dc, label, new Point(card.Left + 12.0, top + 8.0),
                10.4, text, FontWeights.Bold);
            DrawRightAlignedText(dc,
                $"{parameter.CurrentNumber:0.##}x  |  BASE {parameter.ReferenceNumber:0.##}x",
                new Point(card.Right - 12.0, top + 7.0), 11.0,
                parameter.IsControlEnabled ? accent : muted, FontWeights.Bold);
            DrawText(dc, detail, new Point(card.Left + 12.0, top + 27.0),
                9.4, muted, FontWeights.Normal);

            var track = new Rect(card.Left + 22.0, top + 44.0, card.Width - 44.0, 22.0);
            var y = track.Top + track.Height / 2.0;
            var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum,
                parameter.Maximum, track, reverse: false);
            var baseX = ValueToX(parameter.ReferenceNumber, parameter.Minimum,
                parameter.Maximum, track, reverse: false);

            dc.DrawLine(new Pen(border, 4.0),
                new Point(track.Left, y), new Point(track.Right, y));
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.72), 4.5),
                new Point(track.Left, y), new Point(currentX, y));
            dc.DrawLine(new Pen(muted, 2.0),
                new Point(baseX, y - 11.0), new Point(baseX, y + 11.0));
            dc.DrawEllipse(panel, new Pen(accent,
                IsSelected(parameter) ? 2.8 : 1.9),
                new Point(currentX, y), 7.0, 7.0);
            dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

            AddHorizontalTarget(parameter,
                new Rect(track.Left - 8.0, track.Top - 4.0,
                    track.Width + 16.0, track.Height + 8.0),
                track, parameter.Minimum, parameter.Maximum);
            DrawText(dc, $"{parameter.Minimum:0.##}x",
                new Point(track.Left, top + 71.0), 9.1, muted, FontWeights.Normal);
            DrawRightAlignedText(dc, $"{parameter.Maximum:0.##}x",
                new Point(track.Right, top + 71.0), 9.1, muted, FontWeights.Normal);
        }

        DrawPricingTrack(traderBuys,
            "YOU SELL  /  TRADER PAYOUT",
            "Higher means a better trader-buying setting; not an exact payout.",
            132.0);
        DrawPricingTrack(traderSells,
            "YOU BUY  /  TRADER SHOP PRICE",
            "Higher means a pricier trader-selling setting; not an exact final price.",
            232.0);

        // The condition gate changes eligibility; price calculations are separate.
        var gate = new Rect(17.0, 333.0, width, 95.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), gate, 4.0, 4.0);
        DrawText(dc, "MINIMUM CONDITION TRADERS ACCEPT",
            new Point(gate.Left + 12.0, 341.0), 10.4, text, FontWeights.Bold);
        DrawRightAlignedText(dc,
            $"{minCondition.CurrentNumber * 100:0}%  |  BASE {minCondition.ReferenceNumber * 100:0}%",
            new Point(gate.Right - 12.0, 340.0), 11.0, accent, FontWeights.Bold);
        DrawText(dc, "Weapons and armor below this durability are refused.",
            new Point(gate.Left + 12.0, 360.0), 9.4, muted, FontWeights.Normal);

        var gateTrack = new Rect(gate.Left + 22.0, 380.0, gate.Width - 44.0, 22.0);
        var gateY = gateTrack.Top + gateTrack.Height / 2.0;
        var gateX = ValueToX(minCondition.CurrentNumber,
            minCondition.Minimum, minCondition.Maximum, gateTrack, reverse: false);
        var gateBaseX = ValueToX(minCondition.ReferenceNumber,
            minCondition.Minimum, minCondition.Maximum, gateTrack, reverse: false);
        dc.DrawLine(new Pen(border, 4.0),
            new Point(gateTrack.Left, gateY), new Point(gateTrack.Right, gateY));
        dc.DrawRectangle(CloneWithOpacity(muted, 0.16), null,
            new Rect(gateTrack.Left, gateY - 5.0,
                Math.Max(0.0, gateX - gateTrack.Left), 10.0));
        dc.DrawRectangle(CloneWithOpacity(accent, 0.19), null,
            new Rect(gateX, gateY - 5.0,
                Math.Max(0.0, gateTrack.Right - gateX), 10.0));
        dc.DrawLine(new Pen(muted, 2.0),
            new Point(gateBaseX, gateY - 11.0), new Point(gateBaseX, gateY + 11.0));
        dc.DrawEllipse(panel, new Pen(accent, IsSelected(minCondition) ? 2.8 : 1.9),
            new Point(gateX, gateY), 7.0, 7.0);
        dc.DrawEllipse(accent, null, new Point(gateX, gateY), 2.5, 2.5);
        AddHorizontalTarget(minCondition,
            new Rect(gateTrack.Left - 8.0, gateTrack.Top - 4.0,
                gateTrack.Width + 16.0, gateTrack.Height + 8.0),
            gateTrack, minCondition.Minimum, minCondition.Maximum);
        DrawText(dc, "0%  /  BROKEN", new Point(gateTrack.Left, 407.0),
            9.1, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, "100%  /  PRISTINE",
            new Point(gateTrack.Right, 407.0), 9.1, muted, FontWeights.Normal);

        // ItemCostMinPercent is a global durability-value layer, not a trader price.
        var floor = new Rect(17.0, 439.0, width, 95.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), floor, 4.0, 4.0);
        DrawText(dc, "ITEM VALUE AT 0% DURABILITY",
            new Point(floor.Left + 12.0, 447.0), 10.4, text, FontWeights.Bold);
        DrawRightAlignedText(dc,
            $"{valueFloor.CurrentNumber * 100:0}%  |  BASE {valueFloor.ReferenceNumber * 100:0}%",
            new Point(floor.Right - 12.0, 446.0), 11.0, accent, FontWeights.Bold);
        DrawText(dc, "Base value retained when broken; not the final sale price.",
            new Point(floor.Left + 12.0, 466.0), 9.4, muted, FontWeights.Normal);

        var floorTrack = new Rect(floor.Left + 22.0, 486.0, floor.Width - 44.0, 22.0);
        var floorY = floorTrack.Top + floorTrack.Height / 2.0;
        var floorX = ValueToX(valueFloor.CurrentNumber,
            valueFloor.Minimum, valueFloor.Maximum, floorTrack, reverse: false);
        var floorBaseX = ValueToX(valueFloor.ReferenceNumber,
            valueFloor.Minimum, valueFloor.Maximum, floorTrack, reverse: false);
        dc.DrawLine(new Pen(border, 4.0),
            new Point(floorTrack.Left, floorY), new Point(floorTrack.Right, floorY));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.72), 4.5),
            new Point(floorTrack.Left, floorY), new Point(floorX, floorY));
        dc.DrawLine(new Pen(muted, 2.0),
            new Point(floorBaseX, floorY - 11.0), new Point(floorBaseX, floorY + 11.0));
        dc.DrawEllipse(panel, new Pen(accent, IsSelected(valueFloor) ? 2.8 : 1.9),
            new Point(floorX, floorY), 7.0, 7.0);
        dc.DrawEllipse(accent, null, new Point(floorX, floorY), 2.5, 2.5);
        AddHorizontalTarget(valueFloor,
            new Rect(floorTrack.Left - 8.0, floorTrack.Top - 4.0,
                floorTrack.Width + 16.0, floorTrack.Height + 8.0),
            floorTrack, valueFloor.Minimum, valueFloor.Maximum);
        DrawText(dc, $"{valueFloor.Minimum * 100:0}%",
            new Point(floorTrack.Left, 513.0), 9.1, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, $"{valueFloor.Maximum * 100:0}%",
            new Point(floorTrack.Right, 513.0), 9.1, muted, FontWeights.Normal);

        DrawText(dc, "GREY TICK = BASE PRESET  |  RIGHT-CLICK = RESET ONE  |  R = RESET ALL",
            new Point(20.0, 550.0), 9.1, muted, FontWeights.SemiBold);
        DrawText(dc, "PRICE SETTINGS ARE DIFFICULTY SCALERS, NOT EXACT SHOP PAYOUTS",
            new Point(20.0, 567.0), 9.1, muted, FontWeights.Normal);
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
        DrawHeader(dc, "HUNGER STATE LADDER", "real HungerPoints thresholds + one shared stamina-recovery effect axis", text, muted, accent);

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

        var thresholds = new[]
        {
            P("survival.hungerLowThreshold"),
            P("survival.hungerMediumThreshold"),
            P("survival.hungerStrongThreshold")
        };
        var thresholdFallbacks = new[] { 17.0, 33.0, 83.0 };
        var thresholdLabels = new[] { "LOW", "MEDIUM", "SEVERE" };

        var thresholdTrack = new Rect(156.0, 136.0, Math.Max(300.0, ActualWidth - 184.0), 31.0);
        var thresholdY = thresholdTrack.Top + 13.0;
        DrawText(dc, "HUNGER POINTS", new Point(20.0, thresholdTrack.Top + 4.0), 9.0, muted, FontWeights.Bold);
        dc.DrawLine(new Pen(border, 3.0), new Point(thresholdTrack.Left, thresholdY), new Point(thresholdTrack.Right, thresholdY));

        for (var value = 0; value <= 100; value += 25)
        {
            var x = thresholdTrack.Left + thresholdTrack.Width * value / 100.0;
            dc.DrawLine(new Pen(soft, 1.0), new Point(x, thresholdY - 4.0), new Point(x, thresholdY + 4.0));
        }

        for (var i = 0; i < thresholds.Length; i++)
        {
            var parameter = thresholds[i];
            var current = parameter?.CurrentNumber ?? thresholdFallbacks[i];
            var reference = parameter?.ReferenceNumber ?? thresholdFallbacks[i];
            var x = thresholdTrack.Left + thresholdTrack.Width * Math.Clamp(current / 100.0, 0.0, 1.0);
            var referenceX = thresholdTrack.Left + thresholdTrack.Width * Math.Clamp(reference / 100.0, 0.0, 1.0);

            dc.DrawLine(new Pen(muted, 1.0), new Point(referenceX, thresholdY - 7.0), new Point(referenceX, thresholdY + 7.0));
            dc.DrawLine(new Pen(accent, parameter is not null && IsSelected(parameter) ? 2.8 : 2.0), new Point(x, thresholdY - 9.0), new Point(x, thresholdY + 9.0));
            dc.DrawEllipse(panel, new Pen(accent, parameter is not null && IsSelected(parameter) ? 2.4 : 1.5), new Point(x, thresholdY), 5.5, 5.5);

            DrawCenteredText(dc, $"{thresholdLabels[i]} {current:0}", new Point(x, thresholdY + 16.0), 9.0, i == 2 ? accent : text, FontWeights.Bold);

            if (parameter is not null)
            {
                AddHorizontalTarget(
                    parameter,
                    new Rect(x - 18.0, thresholdY - 13.0, 36.0, 34.0),
                    thresholdTrack,
                    0.0,
                    100.0);
            }
        }

        var axis = new Rect(218.0, 202.0, Math.Max(280.0, ActualWidth - 250.0), 118.0);
        const double effectMinimum = -1.0;
        const double effectMaximum = 0.4;

        double EffectToX(double effect)
        {
            var ratio = Math.Clamp(
                (effect - effectMinimum) / (effectMaximum - effectMinimum),
                0.0,
                1.0);

            return axis.Left + axis.Width * ratio;
        }

        var neutralX = EffectToX(0.0);

        dc.DrawRoundedRectangle(
            CloneWithOpacity(panel, 0.92),
            new Pen(border, 1.0),
            new Rect(axis.Left - 10.0, axis.Top - 26.0, axis.Width + 20.0, axis.Height + 42.0),
            4.0,
            4.0);

        var penaltyZone = new Rect(axis.Left, axis.Top - 4.0, Math.Max(0.0, neutralX - axis.Left), axis.Height + 8.0);
        var bonusZone = new Rect(neutralX, axis.Top - 4.0, Math.Max(0.0, axis.Right - neutralX), axis.Height + 8.0);

        dc.DrawRectangle(CloneWithOpacity(border, 0.10), null, penaltyZone);
        dc.DrawRectangle(CloneWithOpacity(accent, 0.06), null, bonusZone);

        DrawText(dc, "PENALTY", new Point(axis.Left, axis.Top - 21.0), 9.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "NEUTRAL", new Point(neutralX, axis.Top - 21.0), 9.0, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "BONUS", new Point(axis.Right, axis.Top - 21.0), 9.0, accent, FontWeights.Bold);

        dc.DrawLine(
            new Pen(soft, 1.0) { DashStyle = DashStyles.Dot },
            new Point(neutralX, axis.Top - 7.0),
            new Point(neutralX, axis.Bottom + 7.0));

        var stages = new[]
        {
            (Label: "WELL FED", Meaning: "stamina recovery bonus", Parameter: P("survival.wellFedStaminaRegen"), IsBonus: true),
            (Label: "MEDIUM", Meaning: "stamina recovery penalty + no sleep", Parameter: P("survival.hungerMediumStaminaRegen"), IsBonus: false),
            (Label: "SEVERE", Meaning: "stronger penalty + no sleep", Parameter: P("survival.hungerStrongStaminaRegen"), IsBonus: false)
        };

        for (var i = 0; i < stages.Length; i++)
        {
            var stage = stages[i];
            var rowY = axis.Top + 20.0 + i * 44.0;

            DrawText(dc, stage.Label, new Point(24.0, rowY - 12.0), 9.4, i == 0 ? accent : text, FontWeights.Bold);
            DrawText(dc, stage.Meaning, new Point(24.0, rowY + 3.0), 9.0, muted, FontWeights.Normal);

            if (stage.Parameter is not { } parameter)
                continue;

            dc.DrawLine(new Pen(border, 3.0), new Point(axis.Left, rowY), new Point(axis.Right, rowY));

            var currentEffect = stage.IsBonus ? parameter.CurrentNumber : -parameter.CurrentNumber;
            var referenceEffect = stage.IsBonus ? parameter.ReferenceNumber : -parameter.ReferenceNumber;
            var currentX = EffectToX(currentEffect);
            var referenceX = EffectToX(referenceEffect);

            dc.DrawLine(new Pen(muted, 1.0), new Point(referenceX, rowY - 7.0), new Point(referenceX, rowY + 7.0));
            dc.DrawLine(new Pen(accent, 3.0), new Point(neutralX, rowY), new Point(currentX, rowY));
            dc.DrawEllipse(panel, new Pen(accent, IsSelected(parameter) ? 2.5 : 1.6), new Point(currentX, rowY), 7.0, 7.0);
            dc.DrawEllipse(accent, null, new Point(currentX, rowY), 3.0, 3.0);

            DrawRightAlignedText(dc, FriendlyValue(parameter), new Point(axis.Right, rowY - 18.0), 9.0, accent, FontWeights.SemiBold);

            if (stage.IsBonus)
            {
                var maxX = EffectToX(parameter.Maximum);
                var map = new Rect(neutralX, rowY - 14.0, Math.Max(1.0, maxX - neutralX), 28.0);
                AddHorizontalTarget(
                    parameter,
                    new Rect(map.Left - 8.0, map.Top - 3.0, map.Width + 16.0, map.Height + 6.0),
                    map,
                    parameter.Minimum,
                    parameter.Maximum);
            }
            else
            {
                var maxPenaltyX = EffectToX(-parameter.Maximum);
                var map = new Rect(maxPenaltyX, rowY - 14.0, Math.Max(1.0, neutralX - maxPenaltyX), 28.0);
                AddHorizontalTarget(
                    parameter,
                    new Rect(map.Left - 8.0, map.Top - 3.0, map.Width + 16.0, map.Height + 6.0),
                    map,
                    parameter.Minimum,
                    parameter.Maximum,
                    reverse: true);
            }
        }

        DrawText(dc, "-100%", new Point(axis.Left, axis.Bottom + 11.0), 9.0, muted, FontWeights.Normal);
        DrawCenteredText(dc, "0%", new Point(neutralX, axis.Bottom + 11.0), 9.0, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, "+40%", new Point(axis.Right, axis.Bottom + 11.0), 9.0, muted, FontWeights.Normal);
    }

    // Each gameplay value is edited independently. The charge preview is deliberately
    // read-only: timing handles should never collide when the delay approaches zero.
    private void DrawStarvationTimeline(
        DrawingContext dc, Brush text, Brush muted, Brush border, Brush soft, Brush accent, Brush panel)
    {
        var toggle = P("advanced.starvationMechanics");
        var delay = P("survival.starvationPenaltyDelay");
        var interval = P("survival.starvationDamageInterval");
        var stamina = P("survival.starvationStaminaDrain");
        var damage = P("survival.starvationDamage");
        var recovery = P("survival.starvationHealthRegenPenalty");

        DrawText(dc, "MASTER STARVATION", new Point(17, 13), 11.5, accent, FontWeights.Bold);
        DrawText(dc, "extra punishment after severe hunger begins", new Point(17, 32),
            9.5, text, FontWeights.Normal);
        if (toggle is null || delay is null || interval is null ||
            stamina is null || damage is null || recovery is null)
        {
            DrawCenteredText(dc, "Master starvation data unavailable.",
                new Point(ActualWidth / 2, 140), 10, muted, FontWeights.Bold);
            return;
        }

        var active = toggle.CurrentBoolean;
        DrawTogglePill(dc, toggle, "STARVATION BUNDLE", new Rect(17, 55, 252, 38),
            text, muted, border, accent, panel);
        DrawRightAlignedText(dc, $"BASE PRESET: {(toggle.ReferenceBoolean ? "ON" : "OFF")}",
            new Point(ActualWidth - 18, 67), 10, muted, FontWeights.SemiBold);

        var fullWidth = Math.Max(480, ActualWidth - 34);
        var timingWidth = (fullWidth - 12) / 2;
        void DrawTiming(ParameterViewModel p, string label, double left)
        {
            var card = new Rect(left, 105, timingWidth, 84);
            dc.DrawRoundedRectangle(panel, new Pen(border, 1), card, 4, 4);
            DrawText(dc, label, new Point(left + 12, 114), 10.1, text, FontWeights.Bold);
            DrawRightAlignedText(dc, $"{p.CurrentNumber:0.#}s",
                new Point(card.Right - 12, 113), 11.4,
                p.IsControlEnabled ? accent : muted, FontWeights.Bold);
            var track = new Rect(left + 17, 142, timingWidth - 34, 19);
            var y = track.Top + track.Height / 2;
            var x = ValueToX(p.CurrentNumber, p.Minimum, p.Maximum, track, false);
            var baseX = ValueToX(p.ReferenceNumber, p.Minimum, p.Maximum, track, false);
            dc.DrawLine(new Pen(soft, 4), new Point(track.Left, y), new Point(track.Right, y));
            if (p.IsControlEnabled)
                dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.67), 4.5),
                    new Point(track.Left, y), new Point(x, y));
            dc.DrawLine(new Pen(muted, 2), new Point(baseX, y - 11), new Point(baseX, y + 11));
            dc.DrawEllipse(panel, new Pen(p.IsControlEnabled ? accent : muted,
                IsSelected(p) ? 2.8 : 1.9), new Point(x, y), 7, 7);
            if (p.IsControlEnabled)
                dc.DrawEllipse(accent, null, new Point(x, y), 2.5, 2.5);
            AddHorizontalTarget(p, track, track, p.Minimum, p.Maximum);
            DrawText(dc, $"{p.Minimum:0.#}s", new Point(track.Left, 167),
                9.3, muted, FontWeights.Normal);
            DrawRightAlignedText(dc, $"{p.Maximum:0.#}s", new Point(track.Right, 167),
                9.3, muted, FontWeights.Normal);
        }

        DrawTiming(delay, "PENALTY DELAY", 17);
        DrawTiming(interval, "DAMAGE INTERVAL", 29 + timingWidth);

        var preview = new Rect(17, 200, fullWidth, 84);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1), preview, 4, 4);
        DrawText(dc, "HP DAMAGE TICK PREVIEW", new Point(29, 208),
            10.2, text, FontWeights.Bold);
        var duration = Math.Max(30,
            Math.Ceiling((delay.CurrentNumber + 2 * interval.CurrentNumber) / 10) * 10);
        DrawRightAlignedText(dc, $"0-{duration:0}s  |  GOLD = HP LOSS",
            new Point(preview.Right - 12, 208), 9.2, muted, FontWeights.SemiBold);
        var axis = new Rect(preview.Left + 24, 233, preview.Width - 48, 22);
        var ay = axis.Top + axis.Height / 2;
        double At(double seconds) => axis.Left + axis.Width *
            Math.Clamp(seconds / duration, 0, 1);
        dc.DrawLine(new Pen(soft, 3), new Point(axis.Left, ay), new Point(axis.Right, ay));
        var startX = At(delay.CurrentNumber);
        dc.DrawLine(new Pen(muted, 2), new Point(startX, ay - 12),
            new Point(startX, ay + 12));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, active ? 0.7 : 0.25), 3),
            new Point(axis.Left, ay), new Point(startX, ay));
        for (var tick = 1; tick <= 3; tick++)
        {
            var seconds = delay.CurrentNumber + tick * interval.CurrentNumber;
            if (seconds > duration + Epsilon) break;
            var x = At(seconds);
            dc.DrawEllipse(panel, new Pen(active ? accent : muted, 2),
                new Point(x, ay), 6, 6);
            dc.DrawEllipse(active ? accent : muted, null, new Point(x, ay), 2.5, 2.5);
        }
        DrawText(dc, "0s", new Point(axis.Left, 258), 9.2, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, $"{duration:0}s", new Point(axis.Right, 258),
            9.2, muted, FontWeights.Normal);
        DrawCenteredText(dc, "GREY TICK = PENALTIES BEGIN",
            new Point(preview.Left + preview.Width / 2, 260),
            9.0, text, FontWeights.SemiBold);

        var pressure = damage.CurrentNumber / Math.Max(Epsilon, interval.CurrentNumber);
        var basePressure = damage.ReferenceNumber / Math.Max(Epsilon, interval.ReferenceNumber);
        DrawText(dc, $"DAMAGE PRESSURE  {pressure:0.00} HP/s  |  BASE {basePressure:0.00} HP/s",
            new Point(20, 295), 10, active ? text : muted, FontWeights.SemiBold);
        DrawRightAlignedText(dc, active ? "ACTIVE" : "OFF - VALUES STAGED",
            new Point(ActualWidth - 20, 295), 9.5,
            active ? accent : muted, FontWeights.Bold);

        // Three full-width editors replace the former cramped 3-column vertical faders.
        void DrawEffect(ParameterViewModel p, string label, string value,
            string minimumLabel, string maximumLabel, double top)
        {
            var card = new Rect(17, top, fullWidth, 57);
            dc.DrawRoundedRectangle(panel, new Pen(border, 1), card, 4, 4);
            DrawText(dc, label, new Point(card.Left + 12, top + 6),
                10.1, text, FontWeights.Bold);
            DrawRightAlignedText(dc, value, new Point(card.Right - 12, top + 6),
                10.5, p.IsControlEnabled ? accent : muted, FontWeights.Bold);
            var track = new Rect(card.Left + 17, top + 26, card.Width - 34, 17);
            var y = track.Top + track.Height / 2;
            var x = ValueToX(p.CurrentNumber, p.Minimum, p.Maximum, track, false);
            var baseX = ValueToX(p.ReferenceNumber, p.Minimum, p.Maximum, track, false);
            dc.DrawLine(new Pen(soft, 4), new Point(track.Left, y), new Point(track.Right, y));
            if (p.IsControlEnabled)
                dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.67), 4.5),
                    new Point(track.Left, y), new Point(x, y));
            dc.DrawLine(new Pen(muted, 2), new Point(baseX, y - 10), new Point(baseX, y + 10));
            dc.DrawEllipse(panel, new Pen(p.IsControlEnabled ? accent : muted,
                IsSelected(p) ? 2.8 : 1.9), new Point(x, y), 7, 7);
            if (p.IsControlEnabled)
                dc.DrawEllipse(accent, null, new Point(x, y), 2.5, 2.5);
            AddHorizontalTarget(p, track, track, p.Minimum, p.Maximum);
            DrawText(dc, minimumLabel, new Point(track.Left, top + 45),
                9, muted, FontWeights.Normal);
            DrawRightAlignedText(dc, maximumLabel, new Point(track.Right, top + 45),
                9, muted, FontWeights.Normal);
        }

        DrawEffect(stamina, "EXTRA ACTION STAMINA COST",
            $"+{stamina.CurrentNumber * 100:0}%  |  BASE +{stamina.ReferenceNumber * 100:0}%",
            "0%", "400%", 318);
        DrawEffect(damage, "HP LOST PER TICK",
            $"{damage.CurrentNumber:0.#} HP  |  BASE {damage.ReferenceNumber:0.#}",
            "0 HP", "8 HP", 380);
        DrawEffect(recovery, "HEALTH RECOVERY BLOCKED",
            $"{recovery.CurrentNumber * 100:0}%  |  BASE {recovery.ReferenceNumber * 100:0}%",
            "0%", "100%", 442);
    }

    // The summary band is deliberately read-only. MIN/MAX use separate full-width
    // tracks so identical or near-identical weights never produce overlapping handles.
    private void DrawRepeatedHitBand(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        var minimum = P("combat.accumulatedDamageWeightMin");
        var maximum = P("combat.accumulatedDamageWeightMax");
        var healedMemory = P("combat.accumulatedDamageIncludesHealedHealth");

        DrawText(dc, "REPEATED-HIT PROTECTION", new Point(17.0, 13.0),
            11.5, accent, FontWeights.Bold);
        DrawText(dc, "hidden protection during sustained incoming fire", new Point(17.0, 32.0),
            9.5, text, FontWeights.Normal);

        if (minimum is null || maximum is null || healedMemory is null)
        {
            DrawCenteredText(dc, "Repeated-hit protection data unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var contentWidth = Math.Max(460.0, ActualWidth - 34.0);
        var preview = new Rect(17.0, 56.0, contentWidth, 107.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), preview, 4.0, 4.0);
        DrawText(dc, "PROTECTION WINDOW", new Point(29.0, 66.0),
            10.0, text, FontWeights.Bold);
        DrawRightAlignedText(dc,
            $"NOW {minimum.CurrentNumber:0.00}-{maximum.CurrentNumber:0.00}  |  BASE {minimum.ReferenceNumber:0.00}-{maximum.ReferenceNumber:0.00}",
            new Point(preview.Right - 12.0, 66.0), 9.4, accent, FontWeights.SemiBold);

        var axis = new Rect(preview.Left + 79.0, 93.0, preview.Width - 98.0, 47.0);
        var currentY = 105.0;
        var referenceY = 134.0;
        double At(double value) => ValueToX(value, 0.0, 1.0, axis, reverse: false);

        DrawText(dc, "CURRENT", new Point(preview.Left + 12.0, currentY - 7.0),
            9.0, accent, FontWeights.Bold);
        DrawText(dc, "BASE", new Point(preview.Left + 12.0, referenceY - 7.0),
            9.0, muted, FontWeights.Bold);
        dc.DrawLine(new Pen(soft, 4.0), new Point(axis.Left, currentY),
            new Point(axis.Right, currentY));
        dc.DrawLine(new Pen(soft, 2.0), new Point(axis.Left, referenceY),
            new Point(axis.Right, referenceY));

        for (var i = 0; i <= 4; i++)
        {
            var x = At(i * 0.25);
            dc.DrawLine(new Pen(soft, 1.0), new Point(x, currentY - 8.0),
                new Point(x, referenceY + 8.0));
        }

        var liveMinX = At(minimum.CurrentNumber);
        var liveMaxX = At(maximum.CurrentNumber);
        var baseMinX = At(minimum.ReferenceNumber);
        var baseMaxX = At(maximum.ReferenceNumber);

        // The global value-based slider constraint maintains MIN <= MAX.
        // Math.Min/Max also keeps the preview safe while a preset is loading.
        var liveLeft = Math.Min(liveMinX, liveMaxX);
        var liveRight = Math.Max(liveMinX, liveMaxX);
        var baseLeft = Math.Min(baseMinX, baseMaxX);
        var baseRight = Math.Max(baseMinX, baseMaxX);
        dc.DrawRoundedRectangle(CloneWithOpacity(accent, 0.24), null,
            new Rect(liveLeft, currentY - 7.0, Math.Max(1.0, liveRight - liveLeft), 14.0),
            3.0, 3.0);
        dc.DrawLine(new Pen(accent, 3.0), new Point(liveLeft, currentY),
            new Point(liveRight, currentY));
        dc.DrawEllipse(accent, null, new Point(liveMinX, currentY), 4.5, 4.5);
        dc.DrawEllipse(accent, null, new Point(liveMaxX, currentY), 4.5, 4.5);

        var basePen = new Pen(muted, 2.0) { DashStyle = DashStyles.Dash };
        dc.DrawLine(basePen, new Point(baseLeft, referenceY),
            new Point(baseRight, referenceY));
        dc.DrawLine(new Pen(muted, 2.0), new Point(baseMinX, referenceY - 7.0),
            new Point(baseMinX, referenceY + 7.0));
        dc.DrawLine(new Pen(muted, 2.0), new Point(baseMaxX, referenceY - 7.0),
            new Point(baseMaxX, referenceY + 7.0));
        DrawText(dc, "0.00", new Point(axis.Left, 146.0), 9.0, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, "1.00", new Point(axis.Right, 146.0),
            9.0, muted, FontWeights.Normal);

        void DrawWeightTrack(ParameterViewModel parameter, string label, double top)
        {
            var card = new Rect(17.0, top, contentWidth, 75.0);
            dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), card, 4.0, 4.0);
            DrawText(dc, label, new Point(card.Left + 12.0, top + 9.0),
                10.2, text, FontWeights.Bold);
            DrawRightAlignedText(dc,
                $"{parameter.CurrentNumber:0.00}  |  BASE {parameter.ReferenceNumber:0.00}",
                new Point(card.Right - 12.0, top + 8.0), 11.0,
                parameter.IsControlEnabled ? accent : muted, FontWeights.Bold);

            var track = new Rect(card.Left + 22.0, top + 33.0, card.Width - 44.0, 19.0);
            var y = track.Top + track.Height / 2.0;
            var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum,
                parameter.Maximum, track, reverse: false);
            var referenceX = ValueToX(parameter.ReferenceNumber, parameter.Minimum,
                parameter.Maximum, track, reverse: false);

            dc.DrawLine(new Pen(soft, 4.0), new Point(track.Left, y),
                new Point(track.Right, y));
            if (parameter.IsControlEnabled)
                dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.72), 4.5),
                    new Point(track.Left, y), new Point(currentX, y));
            // Keep the Base Preset marker behind the handle, including when values match.
            dc.DrawLine(new Pen(muted, 2.0), new Point(referenceX, y - 11.0),
                new Point(referenceX, y + 11.0));
            dc.DrawEllipse(panel,
                new Pen(parameter.IsControlEnabled ? accent : muted,
                    IsSelected(parameter) ? 2.8 : 1.9),
                new Point(currentX, y), 7.0, 7.0);
            if (parameter.IsControlEnabled)
                dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

            AddHorizontalTarget(parameter, track, track, parameter.Minimum, parameter.Maximum);
            DrawText(dc, "0.00 - NO WEIGHT", new Point(track.Left, top + 56.0),
                8.9, muted, FontWeights.Normal);
            DrawRightAlignedText(dc, "1.00 - MAX WEIGHT",
                new Point(track.Right, top + 56.0), 8.9, muted, FontWeights.Normal);
        }

        DrawWeightTrack(minimum, "MINIMUM PROTECTION  /  WEAKER END", 174.0);
        DrawWeightTrack(maximum, "MAXIMUM PROTECTION  /  STRONGER END", 260.0);

        DrawTogglePill(dc, healedMemory, "HEALED DAMAGE STILL COUNTS",
            new Rect(17.0, 346.0, contentWidth, 40.0),
            text, muted, border, accent, panel);
        DrawText(dc,
            healedMemory.CurrentBoolean
                ? "ON: restored HP does not erase earlier hits from the protection tracker."
                : "OFF: healed damage is excluded from the protection tracker.",
            new Point(20.0, 393.0), 9.3, text, FontWeights.Normal);
        DrawRightAlignedText(dc, $"BASE PRESET: {(healedMemory.ReferenceBoolean ? "ON" : "OFF")}",
            new Point(ActualWidth - 19.0, 409.0), 9.1, muted, FontWeights.SemiBold);
        DrawText(dc, "CURVE WEIGHTS, NOT DAMAGE-REDUCTION %",
            new Point(20.0, 409.0), 8.8, muted, FontWeights.Normal);
    }

    // Radiation has three independent source units. Do not infer a net radiation
    // rate by subtracting a difficulty gain multiplier from a points/second removal.
    // The summary is read-only; each native-range track is edited independently.
    private void DrawRadiationBalance(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        var buildup = P("survival.radiationAccumulation");
        var recovery = P("survival.radiationRecovery");
        var damage = P("survival.radiationDamage");

        DrawText(dc, "RADIATION PRESSURE", new Point(17.0, 13.0),
            11.5, accent, FontWeights.Bold);
        DrawText(dc, "radiation gained, naturally cleared and converted to HP damage",
            new Point(17.0, 32.0), 9.5, text, FontWeights.Normal);

        if (buildup is null || recovery is null || damage is null)
        {
            DrawCenteredText(dc, "Radiation control data unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var contentWidth = Math.Max(460.0, ActualWidth - 34.0);
        var overview = new Rect(17.0, 55.0, contentWidth, 69.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), overview, 4.0, 4.0);

        var third = overview.Width / 3.0;
        var headings = new[] { "01  EXPOSURE", "02  CLEARANCE", "03  HP DAMAGE" };
        var notes = new[]
        {
            "incoming radiation gain",
            "passive removal / sec",
            "after harmful states"
        };
        for (var i = 0; i < 3; i++)
        {
            var x = overview.Left + third * i + 12.0;
            DrawText(dc, headings[i], new Point(x, 64.0), 9.5,
                i == 0 ? accent : text, FontWeights.Bold);
            DrawText(dc, notes[i], new Point(x, 84.0),
                9.0, muted, FontWeights.Normal);
            if (i != 0)
            {
                var dividerX = overview.Left + third * i;
                dc.DrawLine(new Pen(soft, 1.0), new Point(dividerX, 65.0),
                    new Point(dividerX, 99.0));
            }
        }

        DrawText(dc, "INDEPENDENT GAME SYSTEMS  /  RADIATION STATE THRESHOLDS STAY VANILLA",
            new Point(overview.Left + 12.0, 105.0),
            8.9, muted, FontWeights.SemiBold);

        // Three full-width tracks retain each parameter's real native min/max/step.
        // A grey Base Preset tick remains visible even when it coincides with gold.
        void DrawRadiationTrack(
            ParameterViewModel parameter,
            string label,
            string description,
            string currentAndBase,
            string minimumLabel,
            string maximumLabel,
            double top)
        {
            var card = new Rect(17.0, top, contentWidth, 96.0);
            dc.DrawRoundedRectangle(panel, new Pen(border, 1.0),
                card, 4.0, 4.0);

            DrawText(dc, label, new Point(card.Left + 12.0, top + 8.0),
                10.5, text, FontWeights.Bold);
            DrawRightAlignedText(dc, currentAndBase,
                new Point(card.Right - 12.0, top + 7.0),
                11.0, parameter.IsControlEnabled ? accent : muted, FontWeights.Bold);
            DrawText(dc, description, new Point(card.Left + 12.0, top + 27.0),
                9.4, muted, FontWeights.Normal);

            var track = new Rect(card.Left + 21.0, top + 48.0,
                card.Width - 42.0, 22.0);
            var y = track.Top + track.Height / 2.0;
            var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum,
                parameter.Maximum, track, reverse: false);
            var referenceX = ValueToX(parameter.ReferenceNumber, parameter.Minimum,
                parameter.Maximum, track, reverse: false);

            dc.DrawLine(new Pen(soft, 4.0),
                new Point(track.Left, y), new Point(track.Right, y));
            if (parameter.IsControlEnabled)
                dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.70), 4.5),
                    new Point(track.Left, y), new Point(currentX, y));

            dc.DrawLine(new Pen(muted, 2.0),
                new Point(referenceX, y - 11.0), new Point(referenceX, y + 11.0));
            dc.DrawEllipse(panel, new Pen(
                parameter.IsControlEnabled ? accent : muted,
                IsSelected(parameter) ? 2.8 : 1.9),
                new Point(currentX, y), 7.0, 7.0);
            if (parameter.IsControlEnabled)
                dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

            AddHorizontalTarget(parameter,
                new Rect(track.Left - 8.0, track.Top - 4.0,
                    track.Width + 16.0, track.Height + 8.0),
                track, parameter.Minimum, parameter.Maximum);

            DrawText(dc, minimumLabel, new Point(track.Left, top + 73.0),
                9.1, muted, FontWeights.Normal);
            DrawRightAlignedText(dc, maximumLabel, new Point(track.Right, top + 73.0),
                9.1, muted, FontWeights.Normal);
        }

        DrawRadiationTrack(buildup,
            "EXPOSURE ACCUMULATION",
            "Difficulty multiplier on incoming radiation; higher means faster build-up.",
            $"{buildup.CurrentNumber:0.##}x  |  BASE {buildup.ReferenceNumber:0.##}x",
            $"{buildup.Minimum:0.##}x", $"{buildup.Maximum:0.##}x", 137.0);

        DrawRadiationTrack(recovery,
            "NATURAL RADIATION CLEARANCE",
            "Absolute radiation removed per second; higher means faster clearing.",
            $"{recovery.CurrentNumber:0.00} pts/s  |  BASE {recovery.ReferenceNumber:0.00}",
            $"{recovery.Minimum:0.00} pts/s", $"{recovery.Maximum:0.00} pts/s", 244.0);

        DrawRadiationTrack(damage,
            "HEALTH DAMAGE FROM RADIATION",
            "Scales damage across all 11 harmful stages without moving state thresholds.",
            $"{damage.CurrentNumber:0.##}x  |  BASE {damage.ReferenceNumber:0.##}x",
            $"{damage.Minimum:0.##}x", $"{damage.Maximum:0.##}x", 351.0);

        DrawText(dc, "GREY TICK = BASE PRESET  |  RIGHT-CLICK = RESET ONE  |  R = RESET ALL",
            new Point(20.0, 458.0), 9.0, muted, FontWeights.SemiBold);
    }

    // The real SNG/NATO generator pools are distinct. Their previews show the
    // normalized composition *inside* each pool, never global drop probability.
    // The preview is read-only; every family has one independent full-width editor.
    private void DrawAmmoDistribution(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        var p918 = P("loot.ammoBoxWeight918");
        var p045 = P("loot.ammoBoxWeight045");
        var p545 = P("loot.ammoBoxWeight545");
        var p012 = P("loot.ammoBoxWeight012");

        DrawText(dc, "BREAKABLE BOX AMMO MIXER", new Point(17.0, 13.0),
            11.5, accent, FontWeights.Bold);
        DrawText(dc, "change which ammo families appear in boxes, not the number of rounds",
            new Point(17.0, 32.0), 9.5, text, FontWeights.Normal);

        if (p918 is null || p045 is null || p545 is null || p012 is null)
        {
            DrawCenteredText(dc, "Breakable ammo box data unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var w918 = Math.Max(0.0, p918.CurrentNumber);
        var w045 = Math.Max(0.0, p045.CurrentNumber);
        var w545 = Math.Max(0.0, p545.CurrentNumber);
        var w012 = Math.Max(0.0, p012.CurrentNumber);

        static double[] Normalize(IReadOnlyList<double> weights)
        {
            var total = weights.Sum();
            return total <= Epsilon
                ? weights.Select(_ => 0.0).ToArray()
                : weights.Select(weight => Math.Max(0.0, weight) / total).ToArray();
        }

        // The SNG pool has two distinct entries mapped to the same 9x18/9x19
        // control. NATO has one of each of its four existing authored entries.
        var sngShares = Normalize(new[] { 2.0 * w918, w545, w012 });
        var natoShares = Normalize(new[] { w045, w918, w545, w012 });
        var sngBaseShares = Normalize(new[] { 2.0, 1.0, 1.0 });
        var natoBaseShares = Normalize(new[] { 1.0, 1.0, 1.0, 1.0 });

        var width = Math.Max(460.0, ActualWidth - 34.0);
        var shades = new[] { 0.95, 0.72, 0.48, 0.29 };

        void DrawPool(
            string title,
            string sourceNote,
            IReadOnlyList<string> labels,
            IReadOnlyList<double> shares,
            IReadOnlyList<double> baseShares,
            IReadOnlyList<int> shadesByEntry,
            double top)
        {
            var card = new Rect(17.0, top, width, 107.0);
            dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), card, 4.0, 4.0);

            DrawText(dc, title, new Point(card.Left + 12.0, top + 7.0),
                10.4, text, FontWeights.Bold);
            DrawRightAlignedText(dc, sourceNote, new Point(card.Right - 12.0, top + 8.0),
                9.1, muted, FontWeights.SemiBold);

            var stack = new Rect(card.Left + 14.0, top + 30.0,
                card.Width - 28.0, 19.0);
            dc.DrawRoundedRectangle(CloneWithOpacity(border, 0.09),
                new Pen(border, 1.0), stack, 3.0, 3.0);

            var total = shares.Sum();
            if (total <= Epsilon)
            {
                DrawCenteredText(dc, "NO ACTIVE AMMO WEIGHT",
                    new Point(stack.Left + stack.Width / 2.0, stack.Top + 2.0),
                    9.5, accent, FontWeights.Bold);
            }
            else
            {
                var cursor = stack.Left;
                for (var i = 0; i < shares.Count; i++)
                {
                    // Keep the final visible segment precisely inside the bar,
                    // avoiding a one-pixel rounding gap at 100%.
                    var length = i == shares.Count - 1
                        ? Math.Max(0.0, stack.Right - cursor)
                        : Math.Max(0.0, stack.Width * shares[i]);

                    if (shares[i] > Epsilon && length > Epsilon)
                        dc.DrawRectangle(
                            CloneWithOpacity(accent, shades[shadesByEntry[i]]),
                            new Pen(panel, 0.8),
                            new Rect(cursor, stack.Top, length, stack.Height));
                    cursor += length;
                }
            }

            var columnWidth = (card.Width - 24.0) / shares.Count;
            for (var i = 0; i < shares.Count; i++)
            {
                var cx = card.Left + 12.0 + columnWidth * (i + 0.5);
                DrawCenteredText(dc, labels[i], new Point(cx, top + 55.0),
                    9.2, text, FontWeights.Bold);
                DrawCenteredText(dc, $"{shares[i] * 100.0:0}%",
                    new Point(cx, top + 70.0), 10.8, accent, FontWeights.Bold);
                DrawCenteredText(dc, $"BASE {baseShares[i] * 100.0:0}%",
                    new Point(cx, top + 89.0), 9.0, muted, FontWeights.Normal);
            }
        }

        DrawPool("SNG POOL", "A918D + A919D  /  A545D  /  A012D",
            new[] { "9x18 / 9x19", "5.45", "12 GAUGE" },
            sngShares, sngBaseShares, new[] { 0, 2, 3 }, 55.0);

        DrawPool("NATO POOL", "A045D  /  A919D  /  A556D  /  A012D",
            new[] { ".45 ACP", "9x19", "5.56", "12 GAUGE" },
            natoShares, natoBaseShares, new[] { 1, 0, 2, 3 }, 170.0);

        DrawText(dc, "AMMO FAMILY WEIGHTS", new Point(19.0, 286.0),
            10.0, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "1x = ORIGINAL WEIGHT 2  |  0x = REMOVE FROM SELECTION",
            new Point(ActualWidth - 19.0, 286.0), 8.9, muted, FontWeights.Normal);

        void DrawFamily(ParameterViewModel parameter, string label, string poolShare, double top)
        {
            var card = new Rect(17.0, top, width, 83.0);
            dc.DrawRoundedRectangle(panel, new Pen(border, 1.0),
                card, 4.0, 4.0);
            DrawText(dc, label, new Point(card.Left + 12.0, top + 6.0),
                10.3, text, FontWeights.Bold);
            DrawRightAlignedText(dc,
                $"{parameter.CurrentNumber:0.##}x  |  BASE {parameter.ReferenceNumber:0.##}x",
                new Point(card.Right - 12.0, top + 6.0), 11.0,
                parameter.IsControlEnabled ? accent : muted, FontWeights.Bold);
            DrawText(dc, poolShare, new Point(card.Left + 12.0, top + 24.0),
                9.3, muted, FontWeights.Normal);

            var track = new Rect(card.Left + 22.0, top + 44.0,
                card.Width - 44.0, 18.0);
            var y = track.Top + track.Height / 2.0;
            var currentX = ValueToX(parameter.CurrentNumber,
                parameter.Minimum, parameter.Maximum, track, reverse: false);
            var baseX = ValueToX(parameter.ReferenceNumber,
                parameter.Minimum, parameter.Maximum, track, reverse: false);

            dc.DrawLine(new Pen(border, 4.0), new Point(track.Left, y),
                new Point(track.Right, y));
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.72), 4.5),
                new Point(track.Left, y), new Point(currentX, y));
            dc.DrawLine(new Pen(muted, 2.0),
                new Point(baseX, y - 10.0), new Point(baseX, y + 10.0));
            dc.DrawEllipse(panel,
                new Pen(accent, IsSelected(parameter) ? 2.8 : 1.9),
                new Point(currentX, y), 7.0, 7.0);
            dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

            AddHorizontalTarget(parameter,
                new Rect(track.Left - 8.0, track.Top - 4.0,
                    track.Width + 16.0, track.Height + 8.0),
                track, parameter.Minimum, parameter.Maximum);
            DrawText(dc, $"{parameter.Minimum:0.##}x", new Point(track.Left, top + 65.0),
                9.0, muted, FontWeights.Normal);
            DrawRightAlignedText(dc, $"{parameter.Maximum:0.##}x",
                new Point(track.Right, top + 65.0), 9.0, muted, FontWeights.Normal);
        }

        DrawFamily(p918, "9x18 / 9x19 mm",
            $"SNG SHARE {sngShares[0] * 100:0}%   |   NATO SHARE {natoShares[1] * 100:0}%",
            309.0);
        DrawFamily(p045, ".45 ACP",
            $"SNG NOT PRESENT   |   NATO SHARE {natoShares[0] * 100:0}%",
            400.0);
        DrawFamily(p545, "5.45 / 5.56 mm",
            $"SNG SHARE {sngShares[1] * 100:0}%   |   NATO SHARE {natoShares[2] * 100:0}%",
            491.0);
        DrawFamily(p012, "12 GAUGE",
            $"SNG SHARE {sngShares[2] * 100:0}%   |   NATO SHARE {natoShares[3] * 100:0}%",
            582.0);

        DrawText(dc, "POOL % = SHARE WITHIN THAT POOL, NOT GLOBAL BOX DROP CHANCE",
            new Point(19.0, 682.0), 9.2, muted, FontWeights.SemiBold);
        DrawText(dc, "SNG MUST KEEP 9x18/19, 5.45/5.56 OR 12 GAUGE ABOVE 0x",
            new Point(19.0, 700.0), 9.2, accent, FontWeights.SemiBold);
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
        EnforceFamilyConstraints(target.Parameter);
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
        EnforceFamilyConstraints(_selectedParameter);

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
        var snapped = Snap(target.Parameter, raw);
        target.Parameter.CurrentNumber = snapped;

        EnforceFamilyConstraints(target.Parameter);

        _status.ClearTransient();
        InvalidateVisual();
    }

    private void EnforceFamilyConstraints(ParameterViewModel movedParameter)
    {
        EnforceAmmoPoolValidity(movedParameter);

        if (Family == "FatigueCurve")
        {
            EnforceOrderedTriplet(
                movedParameter,
                P("survival.sleepLowThreshold"),
                P("survival.sleepMediumThreshold"),
                P("survival.sleepStrongThreshold"));
        }
        else if (Family == "HungerLadder")
        {
            EnforceOrderedTriplet(
                movedParameter,
                P("survival.hungerLowThreshold"),
                P("survival.hungerMediumThreshold"),
                P("survival.hungerStrongThreshold"));
        }
    }

    private static void EnforceOrderedTriplet(
        ParameterViewModel movedParameter,
        ParameterViewModel? low,
        ParameterViewModel? medium,
        ParameterViewModel? high)
    {
        if (low is null || medium is null || high is null)
            return;

        if (ReferenceEquals(movedParameter, low))
        {
            if (medium.CurrentNumber < low.CurrentNumber)
                medium.CurrentNumber = low.CurrentNumber;
            if (high.CurrentNumber < medium.CurrentNumber)
                high.CurrentNumber = medium.CurrentNumber;
            return;
        }

        if (ReferenceEquals(movedParameter, medium))
        {
            if (low.CurrentNumber > medium.CurrentNumber)
                low.CurrentNumber = medium.CurrentNumber;
            if (high.CurrentNumber < medium.CurrentNumber)
                high.CurrentNumber = medium.CurrentNumber;
            return;
        }

        if (ReferenceEquals(movedParameter, high))
        {
            if (medium.CurrentNumber > high.CurrentNumber)
                medium.CurrentNumber = high.CurrentNumber;
            if (low.CurrentNumber > medium.CurrentNumber)
                low.CurrentNumber = medium.CurrentNumber;
        }
    }

    private void EnforceAmmoPoolValidity(ParameterViewModel movedParameter)
    {
        if (Family != "AmmoDistribution")
            return;

        var family918 = P("loot.ammoBoxWeight918");
        var family545 = P("loot.ammoBoxWeight545");
        var family012 = P("loot.ammoBoxWeight012");
        if (family918 is null || family545 is null || family012 is null)
            return;

        if (family918.CurrentNumber > Epsilon
            || family545.CurrentNumber > Epsilon
            || family012.CurrentNumber > Epsilon)
        {
            return;
        }

        // .45 ACP exists only in the NATO pool. The SNG pool therefore needs at least
        // one of its three shared families to retain a positive weight. If the user
        // tries to zero the final active SNG family, keep that actively edited family
        // at one visible tick above zero instead of silently changing another family.
        var fallback = ReferenceEquals(movedParameter, family918)
            ? family918
            : ReferenceEquals(movedParameter, family545)
                ? family545
                : ReferenceEquals(movedParameter, family012)
                    ? family012
                    : family918;

        var minimumPositive = Math.Max(fallback.TickFrequency, fallback.Minimum);
        if (minimumPositive <= Epsilon)
            minimumPositive = 0.25;

        fallback.CurrentNumber = Math.Clamp(minimumPositive, fallback.Minimum, fallback.Maximum);
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
            Math.Max(9.0, size),
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
            "survival.hungerLowThreshold" or
            "survival.hungerMediumThreshold" or
            "survival.hungerStrongThreshold" => $"{parameter.CurrentNumber:0} pts",
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
            "survival.sleepLowThreshold" or
            "survival.sleepMediumThreshold" or
            "survival.sleepStrongThreshold" => $"{parameter.CurrentNumber:0} pts",
            "survival.energyDrinkFatigueRelief" => $"{parameter.CurrentNumber:0.##}x",

            "survival.instantStaminaRestore" or
            "survival.waterStaminaEffectStrength" or
            "survival.energyDrinkStaminaRecoveryStrength" or
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
            "EquipmentWearBalance" => 320.0,
            "PsyPhantomPressure" => 390.0,
            "HumanCombatProfile" => 390.0,
            "PlayerWeaponProfile" => 330.0,
            "CoreHealthDashboard" => 385.0,
            "SevereFatiguePenaltyBoard" => 390.0,
            "MasterMedicalHealing" => 390.0,
            "MasterLowHealth" => 235.0,
            "MasterRadiationPenalty" => 235.0,
            "ConsumableMixer" => 475.0,
            "SleepingDashboard" => 525.0,
            "HungerLadder" => 380.0,
            "StarvationTimeline" => 532.0,
            "RepeatedHitBand" => 455.0,
            "RadiationBalance" => 490.0,
            "TradingDashboard" => 590.0,
            "RepairCurve" => 640.0,
            "AmmoDistribution" => 750.0,
            _ => 340.0
        };

    private static IReadOnlyList<string> GetFamilyIds(string family)
        => family switch
        {
            "FatigueCurve" => new[]
            {
                "survival.sleepinessRate",
                "survival.sleepLowThreshold",
                "survival.sleepMediumThreshold",
                "survival.sleepStrongThreshold",
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
            "EquipmentWearBalance" => new[]
            {
                "equipment.armorDurability",
                "equipment.weaponWearPerShot"
            },
            "PsyPhantomPressure" => new[]
            {
                "combat.psySpawnCycleDelay",
                "combat.psyPhantomCount",
                "combat.psyBleeding",
                "combat.psyDurabilityDamage"
            },
            "HumanCombatProfile" => new[]
            {
                "combat.npcHealth",
                "combat.npcWeaponDamage",
                "combat.npcAttackCooldown",
                "combat.npcHipAccuracy"
            },
            "PlayerWeaponProfile" => new[]
            {
                "combat.playerWeaponDamage",
                "combat.playerHeadshotMultiplier",
                "combat.weaponJamming"
            },
            "CoreHealthDashboard" => new[]
            {
                "health.maxHP",
                "health.baseRegen",
                "health.regenDelay",
                "health.regenerationScalar"
            },
            "SevereFatiguePenaltyBoard" => new[]
            {
                "advanced.fatigueMechanics",
                "survival.severeFatigueMaxStamina",
                "survival.severeFatigueCarryWeight",
                "survival.severeFatigueRecoil",
                "survival.severeFatigueAimSway",
                "survival.severeFatigueDispersion"
            },
            "MasterMedicalHealing" => new[]
            {
                "advanced.masterEffectModifierMechanics",
                "health.masterBandageModifier",
                "health.masterMedkitModifier",
                "health.masterArmyMedkitModifier"
            },
            "MasterLowHealth" => new[]
            {
                "advanced.lowHealthMechanics",
                "health.masterLowHealthPenalty"
            },
            "MasterRadiationPenalty" => new[]
            {
                "advanced.radiationMechanics",
                "survival.radiationStaminaDrainPenalty"
            },
            "ConsumableMixer" => new[]
            {
                "survival.instantStaminaRestore",
                "survival.waterStaminaEffectStrength",
                "survival.energyDrinkStaminaRecoveryStrength",
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
                "survival.hungerLowThreshold",
                "survival.hungerMediumThreshold",
                "survival.hungerStrongThreshold",
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
