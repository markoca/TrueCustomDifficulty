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
            case "EquipmentWearBalance":
                DrawEquipmentWearBalance(dc, text, muted, border, soft, accent, panel);
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

        DrawText(dc, $"WEAPON WEAR   {weapon.CurrentNumber:0.##}x", new Point(18.0, 52.0), 10.0, accent, FontWeights.SemiBold);
        DrawRightAlignedText(dc, $"ARMOR TOUGHNESS   {armor.CurrentNumber:0.##}x", new Point(ActualWidth - 18.0, 52.0), 10.0, accent, FontWeights.SemiBold);

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
                DrawCenteredText(dc, $"{armorTick:0.##}", new Point(armorTickX, plot.Bottom + 29.0), 9.0, muted, FontWeights.Normal);
                DrawRightAlignedText(dc, $"{weaponTick:0.##}", new Point(plot.Left - 29.0, weaponTickY - 7.0), 9.0, muted, FontWeights.Normal);
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
        DrawCenteredText(dc, "BOTTOM: ARMOR (TOUGHER RIGHT)   |   LEFT: WEAPON (MORE WEAR UP)   |   GREY = BASE",
            new Point(ActualWidth / 2.0, 280.0), 9.0, muted, FontWeights.SemiBold);
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
        DrawHeader(
            dc,
            "REPAIR PRICE BY REPUTATION",
            "better faction relations should progressively reduce the repair-price multiplier",
            text,
            muted,
            accent);

        var plot = new Rect(68.0, 82.0, Math.Max(380.0, ActualWidth - 104.0), 214.0);
        const double minimum = 0.0;
        const double maximum = 3.0;
        const double basePrice = 1.0;

        dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.95), new Pen(border, 1.0), plot, 4.0, 4.0);

        var baseY = ValueToY(basePrice, minimum, maximum, plot);
        var surchargeFill = CloneWithOpacity(border, 0.08);
        var discountFill = CloneWithOpacity(accent, 0.055);

        dc.DrawRectangle(
            surchargeFill,
            null,
            new Rect(plot.Left, plot.Top, plot.Width, Math.Max(0.0, baseY - plot.Top)));

        dc.DrawRectangle(
            discountFill,
            null,
            new Rect(plot.Left, baseY, plot.Width, Math.Max(0.0, plot.Bottom - baseY)));

        for (var i = 0; i <= 3; i++)
        {
            var y = ValueToY(i, minimum, maximum, plot);
            var pen = new Pen(i == 1 ? accent : soft, i == 1 ? 1.4 : 1.0);
            if (i != 1)
                pen.DashStyle = DashStyles.Dot;

            dc.DrawLine(pen, new Point(plot.Left, y), new Point(plot.Right, y));
            DrawRightAlignedText(
                dc,
                i == 1 ? "1x BASE" : $"{i}x",
                new Point(plot.Left - 9.0, y - 7.0),
                8.2,
                i == 1 ? accent : muted,
                i == 1 ? FontWeights.Bold : FontWeights.SemiBold);
        }

        DrawText(dc, "SURCHARGE", new Point(plot.Left + 8.0, plot.Top + 7.0), 7.8, muted, FontWeights.Bold);
        DrawText(dc, "DISCOUNT", new Point(plot.Left + 8.0, baseY + 8.0), 7.8, accent, FontWeights.Bold);
        DrawRightAlignedText(dc, "REPUTATION IMPROVES →", new Point(plot.Right - 8.0, plot.Top + 7.0), 7.8, muted, FontWeights.Bold);

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

        DrawCurveLine(
            dc,
            xs,
            vanilla,
            plot,
            minimum,
            maximum,
            new Pen(muted, 1.5) { DashStyle = DashStyles.Dash });

        DrawCurveLine(
            dc,
            xs,
            current,
            plot,
            minimum,
            maximum,
            new Pen(accent, 2.6));

        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i] is not { } parameter)
                continue;

            var currentPoint = new Point(xs[i], ValueToY(current[i], minimum, maximum, plot));
            var vanillaPoint = new Point(xs[i], ValueToY(vanilla[i], minimum, maximum, plot));

            // A stem to the 1x base line makes the actual gameplay meaning visible:
            // above 1x is an added repair-price surcharge, below 1x is a discount.
            dc.DrawLine(
                new Pen(CloneWithOpacity(accent, 0.55), 2.0),
                new Point(xs[i], baseY),
                currentPoint);

            dc.DrawEllipse(
                panel,
                new Pen(muted, 1.2) { DashStyle = DashStyles.Dash },
                vanillaPoint,
                5.0,
                5.0);

            dc.DrawEllipse(
                panel,
                new Pen(accent, IsSelected(parameter) ? 2.8 : 1.7),
                currentPoint,
                7.0,
                7.0);
            dc.DrawEllipse(accent, null, currentPoint, 3.2, 3.2);

            var deltaPercent = (current[i] - 1.0) * 100.0;
            var deltaLabel = Math.Abs(deltaPercent) <= 0.5
                ? "BASE PRICE"
                : deltaPercent > 0.0
                    ? $"+{deltaPercent:0}% SURCHARGE"
                    : $"{deltaPercent:0}% DISCOUNT";

            DrawCenteredText(
                dc,
                $"{current[i]:0.##}x",
                new Point(xs[i], currentPoint.Y - 21.0),
                9.0,
                accent,
                FontWeights.Bold);

            DrawCenteredText(
                dc,
                deltaLabel,
                new Point(xs[i], currentPoint.Y + 11.0),
                7.4,
                current[i] <= 1.0 + Epsilon ? accent : muted,
                FontWeights.SemiBold);

            var columnHit = new Rect(xs[i] - 46.0, plot.Top, 92.0, plot.Height);
            AddVerticalTarget(parameter, columnHit, plot, minimum, maximum);

            DrawCenteredText(dc, labels[i], new Point(xs[i], plot.Bottom + 12.0), 9.0, text, FontWeights.Bold);
            DrawCenteredText(
                dc,
                $"vanilla {vanilla[i]:0.##}x",
                new Point(xs[i], plot.Bottom + 29.0),
                7.7,
                muted,
                FontWeights.Normal);
        }

        var monotonic = current[0] + Epsilon >= current[1]
            && current[1] + Epsilon >= current[2]
            && current[2] + Epsilon >= current[3];

        DrawCenteredText(
            dc,
            monotonic
                ? "HOSTILE ≥ DISLIKED ≥ NEUTRAL ≥ FRIENDLY"
                : "REPUTATION CURVE ORDER WILL BE NORMALIZED WHILE EDITING",
            new Point(ActualWidth / 2.0, 326.0),
            8.0,
            monotonic ? muted : accent,
            FontWeights.SemiBold);

        DrawCenteredText(
            dc,
            "GOLD = CURRENT   •   DASHED = VANILLA   •   1x = UNMODIFIED BASE REPAIR LAYER",
            new Point(ActualWidth / 2.0, 346.0),
            7.7,
            muted,
            FontWeights.Normal);
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

    private void DrawSleepingDashboard(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "SLEEPING SYSTEM", "sleep gate, minimum rest window and wake-up recovery", text, muted, accent);

        var threshold = P("survival.sleepEligibilityThreshold");
        var minimumHours = P("survival.minimumSleepHours");
        var healthRestore = P("survival.sleepHealthRestore");
        var staminaRestore = P("survival.sleepStaminaRestore");

        var left = 16.0;
        var width = Math.Max(420.0, ActualWidth - 32.0);

        // Sleep gate: this is not player runtime fatigue. It is the configured
        // sleepiness threshold at which normal sleep becomes available.
        var gateCard = new Rect(left, 56.0, width, 92.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), gateCard, 3.0, 3.0);
        DrawText(dc, "SLEEP GATE", new Point(gateCard.Left + 12.0, gateCard.Top + 9.0), 9.0, text, FontWeights.Bold);
        DrawText(dc, "how tired you must be before normal sleep is allowed", new Point(gateCard.Left + 12.0, gateCard.Top + 25.0), 8.0, muted, FontWeights.Normal);

        if (threshold is not null)
        {
            DrawRightAlignedText(dc, FriendlyValue(threshold), new Point(gateCard.Right - 12.0, gateCard.Top + 8.0), 10.4, accent, FontWeights.Bold);

            var gateTrack = new Rect(gateCard.Left + 16.0, gateCard.Top + 52.0, gateCard.Width - 32.0, 22.0);
            var gateY = gateTrack.Top + gateTrack.Height / 2.0;
            dc.DrawLine(new Pen(border, 4.0), new Point(gateTrack.Left, gateY), new Point(gateTrack.Right, gateY));

            var vanillaX = ValueToX(threshold.ReferenceNumber, threshold.Minimum, threshold.Maximum, gateTrack, reverse: false);
            dc.DrawLine(new Pen(muted, 1.2), new Point(vanillaX, gateY - 8.0), new Point(vanillaX, gateY + 8.0));
            DrawCenteredText(dc, "VANILLA", new Point(vanillaX, gateY + 10.0), 7.3, muted, FontWeights.SemiBold);

            var currentX = ValueToX(threshold.CurrentNumber, threshold.Minimum, threshold.Maximum, gateTrack, reverse: false);
            dc.DrawEllipse(accent, null, new Point(currentX, gateY), 6.0, 6.0);

            DrawText(dc, "EARLIER", new Point(gateTrack.Left, gateY + 10.0), 7.4, muted, FontWeights.SemiBold);
            DrawRightAlignedText(dc, "MORE TIRED", new Point(gateTrack.Right, gateY + 10.0), 7.4, muted, FontWeights.SemiBold);
            AddHorizontalTarget(threshold, gateTrack, gateTrack, threshold.Minimum, threshold.Maximum);
        }

        // Minimum rest window shown as a clock-like ruler rather than another generic card.
        var timeCard = new Rect(left, 160.0, width, 88.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), timeCard, 3.0, 3.0);
        DrawText(dc, "MINIMUM SLEEP WINDOW", new Point(timeCard.Left + 12.0, timeCard.Top + 9.0), 9.0, text, FontWeights.Bold);
        DrawText(dc, "shortest normal sleep cycle", new Point(timeCard.Left + 12.0, timeCard.Top + 25.0), 8.0, muted, FontWeights.Normal);

        if (minimumHours is not null)
        {
            DrawRightAlignedText(dc, FriendlyValue(minimumHours), new Point(timeCard.Right - 12.0, timeCard.Top + 8.0), 10.4, accent, FontWeights.Bold);

            var timeTrack = new Rect(timeCard.Left + 16.0, timeCard.Top + 49.0, timeCard.Width - 32.0, 22.0);
            var timeY = timeTrack.Top + timeTrack.Height / 2.0;
            dc.DrawLine(new Pen(border, 4.0), new Point(timeTrack.Left, timeY), new Point(timeTrack.Right, timeY));

            var vanillaX = ValueToX(minimumHours.ReferenceNumber, minimumHours.Minimum, minimumHours.Maximum, timeTrack, reverse: false);
            dc.DrawLine(new Pen(muted, 1.2), new Point(vanillaX, timeY - 8.0), new Point(vanillaX, timeY + 8.0));

            var currentX = ValueToX(minimumHours.CurrentNumber, minimumHours.Minimum, minimumHours.Maximum, timeTrack, reverse: false);
            dc.DrawLine(new Pen(accent, 4.0), new Point(timeTrack.Left, timeY), new Point(currentX, timeY));
            dc.DrawEllipse(accent, null, new Point(currentX, timeY), 6.0, 6.0);

            DrawText(dc, $"{minimumHours.Minimum:0}H", new Point(timeTrack.Left, timeY + 10.0), 7.4, muted, FontWeights.SemiBold);
            DrawCenteredText(dc, "VANILLA", new Point(vanillaX, timeY + 10.0), 7.3, muted, FontWeights.SemiBold);
            DrawRightAlignedText(dc, $"{minimumHours.Maximum:0}H", new Point(timeTrack.Right, timeY + 10.0), 7.4, muted, FontWeights.SemiBold);
            AddHorizontalTarget(minimumHours, timeTrack, timeTrack, minimumHours.Minimum, minimumHours.Maximum);
        }

        DrawText(dc, "WAKE-UP RECOVERY", new Point(left + 2.0, 266.0), 9.0, text, FontWeights.Bold);
        DrawText(dc, "direct restore applied after normal sleep", new Point(left + 2.0, 282.0), 8.0, muted, FontWeights.Normal);

        var gap = 10.0;
        var recoveryWidth = (width - gap) / 2.0;
        DrawRecoveryMeter(
            dc,
            healthRestore,
            "HEALTH",
            new Rect(left, 302.0, recoveryWidth, 66.0),
            text,
            muted,
            border,
            accent,
            panel);

        DrawRecoveryMeter(
            dc,
            staminaRestore,
            "STAMINA",
            new Rect(left + recoveryWidth + gap, 302.0, recoveryWidth, 66.0),
            text,
            muted,
            border,
            accent,
            panel);
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

    private void DrawTradingDashboard(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        DrawHeader(
            dc,
            "TRADING FLOW",
            "what traders pay, what they charge, what damaged gear they accept and what broken gear is worth",
            text,
            muted,
            accent);

        var traderBuys = P("economy.buyCondition");
        var traderSells = P("economy.sellCondition");
        var minCondition = P("economy.traderMinBuyCondition");
        var valueFloor = P("economy.itemValueFloor");

        var left = 24.0;
        var right = Math.Max(left + 450.0, ActualWidth - 24.0);
        var width = right - left;
        var gap = 14.0;
        var half = (width - gap) / 2.0;

        void DrawPricingSide(
            ParameterViewModel? parameter,
            string title,
            string subtitle,
            string leftLabel,
            string rightLabel,
            Rect card)
        {
            dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.95), new Pen(border, 1.0), card, 4.0, 4.0);
            DrawText(dc, title, new Point(card.Left + 12.0, card.Top + 9.0), 9.0, text, FontWeights.Bold);
            DrawText(dc, subtitle, new Point(card.Left + 12.0, card.Top + 25.0), 7.8, muted, FontWeights.Normal);

            DrawText(dc, leftLabel, new Point(card.Left + 12.0, card.Top + 47.0), 7.6, muted, FontWeights.Bold);
            DrawRightAlignedText(dc, rightLabel, new Point(card.Right - 12.0, card.Top + 47.0), 7.6, accent, FontWeights.Bold);

            if (parameter is null)
                return;

            DrawRightAlignedText(
                dc,
                FriendlyValue(parameter),
                new Point(card.Right - 12.0, card.Top + 8.0),
                10.0,
                accent,
                FontWeights.Bold);

            var track = new Rect(card.Left + 18.0, card.Top + 69.0, card.Width - 36.0, 24.0);
            var y = track.Top + track.Height / 2.0;
            dc.DrawLine(new Pen(border, 4.0), new Point(track.Left, y), new Point(track.Right, y));

            var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track, reverse: false);
            var vanillaX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, track, reverse: false);

            dc.DrawLine(new Pen(accent, 4.0), new Point(track.Left, y), new Point(currentX, y));
            dc.DrawLine(new Pen(muted, 1.2), new Point(vanillaX, y - 8.0), new Point(vanillaX, y + 8.0));

            dc.DrawEllipse(
                panel,
                new Pen(accent, IsSelected(parameter) ? 2.6 : 1.6),
                new Point(currentX, y),
                7.0,
                7.0);
            dc.DrawEllipse(accent, null, new Point(currentX, y), 3.0, 3.0);

            DrawText(dc, "0x", new Point(track.Left, y + 10.0), 7.4, muted, FontWeights.Normal);
            DrawCenteredText(dc, "BASE", new Point(vanillaX, y + 10.0), 7.3, muted, FontWeights.SemiBold);
            DrawRightAlignedText(dc, "2x", new Point(track.Right, y + 10.0), 7.4, muted, FontWeights.Normal);

            AddHorizontalTarget(
                parameter,
                new Rect(track.Left - 8.0, track.Top - 5.0, track.Width + 16.0, track.Height + 10.0),
                track,
                parameter.Minimum,
                parameter.Maximum);
        }

        DrawPricingSide(
            traderBuys,
            "PLAYER  →  TRADER",
            "trader buys your item",
            "LOWER PAYOUT SETTING",
            "HIGHER PAYOUT SETTING",
            new Rect(left, 58.0, half, 112.0));

        DrawPricingSide(
            traderSells,
            "TRADER  →  PLAYER",
            "trader sells an item to you",
            "LOWER SHOP SETTING",
            "HIGHER SHOP SETTING",
            new Rect(left + half + gap, 58.0, half, 112.0));

        var currentGap = traderBuys is not null && traderSells is not null
            ? traderSells.CurrentNumber - traderBuys.CurrentNumber
            : 0.0;
        var baseGap = traderBuys is not null && traderSells is not null
            ? traderSells.ReferenceNumber - traderBuys.ReferenceNumber
            : 0.0;

        var gapCard = new Rect(left, 184.0, width, 54.0);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.94), new Pen(border, 1.0), gapCard, 4.0, 4.0);
        DrawText(dc, "DIFFICULTY PRICING GAP", new Point(gapCard.Left + 12.0, gapCard.Top + 8.0), 8.4, muted, FontWeights.Bold);
        DrawText(
            dc,
            "sell-to-you setting minus buy-from-you setting — NOT the final shop-price ratio",
            new Point(gapCard.Left + 12.0, gapCard.Top + 24.0),
            7.5,
            muted,
            FontWeights.Normal);
        DrawRightAlignedText(
            dc,
            $"{currentGap:+0.00;-0.00;0.00}   |   BASE {baseGap:+0.00;-0.00;0.00}",
            new Point(gapCard.Right - 12.0, gapCard.Top + 8.0),
            9.0,
            Math.Abs(currentGap - baseGap) > Epsilon ? accent : muted,
            FontWeights.Bold);

        var gateCard = new Rect(left, 253.0, width, 91.0);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.95), new Pen(border, 1.0), gateCard, 4.0, 4.0);
        DrawText(dc, "TRADER CONDITION GATE", new Point(gateCard.Left + 12.0, gateCard.Top + 9.0), 8.8, text, FontWeights.Bold);
        DrawText(dc, "weapons / armor below the threshold are refused", new Point(gateCard.Left + 12.0, gateCard.Top + 25.0), 7.7, muted, FontWeights.Normal);

        if (minCondition is not null)
        {
            DrawRightAlignedText(
                dc,
                $"{minCondition.CurrentNumber * 100:0}% MIN CONDITION",
                new Point(gateCard.Right - 12.0, gateCard.Top + 8.0),
                9.5,
                accent,
                FontWeights.Bold);

            var gateTrack = new Rect(gateCard.Left + 20.0, gateCard.Top + 54.0, gateCard.Width - 40.0, 24.0);
            var gateY = gateTrack.Top + gateTrack.Height / 2.0;
            var currentX = ValueToX(minCondition.CurrentNumber, 0.0, 1.0, gateTrack, reverse: false);
            var vanillaX = ValueToX(minCondition.ReferenceNumber, 0.0, 1.0, gateTrack, reverse: false);

            dc.DrawLine(new Pen(border, 4.0), new Point(gateTrack.Left, gateY), new Point(gateTrack.Right, gateY));

            dc.DrawRectangle(
                CloneWithOpacity(border, 0.16),
                null,
                new Rect(gateTrack.Left, gateY - 6.0, Math.Max(1.0, currentX - gateTrack.Left), 12.0));
            dc.DrawRectangle(
                CloneWithOpacity(accent, 0.16),
                null,
                new Rect(currentX, gateY - 6.0, Math.Max(1.0, gateTrack.Right - currentX), 12.0));

            dc.DrawLine(new Pen(muted, 1.2), new Point(vanillaX, gateY - 10.0), new Point(vanillaX, gateY + 10.0));
            dc.DrawLine(new Pen(accent, 2.4), new Point(currentX, gateY - 12.0), new Point(currentX, gateY + 12.0));
            dc.DrawEllipse(panel, new Pen(accent, IsSelected(minCondition) ? 2.6 : 1.6), new Point(currentX, gateY), 7.0, 7.0);

            DrawText(dc, "REFUSED", new Point(gateTrack.Left, gateY + 11.0), 7.5, muted, FontWeights.Bold);
            DrawRightAlignedText(dc, "ACCEPTED", new Point(gateTrack.Right, gateY + 11.0), 7.5, accent, FontWeights.Bold);
            DrawCenteredText(dc, "VANILLA 40%", new Point(vanillaX, gateY + 11.0), 7.2, muted, FontWeights.SemiBold);

            AddHorizontalTarget(
                minCondition,
                new Rect(gateTrack.Left - 8.0, gateTrack.Top - 5.0, gateTrack.Width + 16.0, gateTrack.Height + 10.0),
                gateTrack,
                0.0,
                1.0);
        }

        var valueCard = new Rect(left, 360.0, width, 78.0);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.95), new Pen(border, 1.0), valueCard, 4.0, 4.0);
        DrawText(dc, "BROKEN-ITEM VALUE FLOOR", new Point(valueCard.Left + 12.0, valueCard.Top + 9.0), 8.8, text, FontWeights.Bold);
        DrawText(dc, "base value retained at exactly 0% durability", new Point(valueCard.Left + 12.0, valueCard.Top + 25.0), 7.7, muted, FontWeights.Normal);

        if (valueFloor is not null)
        {
            DrawRightAlignedText(
                dc,
                $"{valueFloor.CurrentNumber * 100:0}% OF BASE VALUE",
                new Point(valueCard.Right - 12.0, valueCard.Top + 8.0),
                9.5,
                accent,
                FontWeights.Bold);

            var floorTrack = new Rect(valueCard.Left + 20.0, valueCard.Top + 49.0, valueCard.Width - 40.0, 20.0);
            var floorY = floorTrack.Top + floorTrack.Height / 2.0;
            dc.DrawLine(new Pen(border, 4.0), new Point(floorTrack.Left, floorY), new Point(floorTrack.Right, floorY));

            var currentX = ValueToX(valueFloor.CurrentNumber, valueFloor.Minimum, valueFloor.Maximum, floorTrack, reverse: false);
            var vanillaX = ValueToX(valueFloor.ReferenceNumber, valueFloor.Minimum, valueFloor.Maximum, floorTrack, reverse: false);

            dc.DrawLine(new Pen(accent, 4.0), new Point(floorTrack.Left, floorY), new Point(currentX, floorY));
            dc.DrawLine(new Pen(muted, 1.2), new Point(vanillaX, floorY - 8.0), new Point(vanillaX, floorY + 8.0));

            dc.DrawEllipse(
                panel,
                new Pen(accent, IsSelected(valueFloor) ? 2.6 : 1.6),
                new Point(currentX, floorY),
                7.0,
                7.0);
            dc.DrawEllipse(accent, null, new Point(currentX, floorY), 3.0, 3.0);

            DrawText(dc, "0%", new Point(floorTrack.Left, floorY + 9.0), 7.3, muted, FontWeights.Normal);
            DrawCenteredText(dc, "VANILLA 10%", new Point(vanillaX, floorY + 9.0), 7.2, muted, FontWeights.SemiBold);
            DrawRightAlignedText(dc, "20%", new Point(floorTrack.Right, floorY + 9.0), 7.3, muted, FontWeights.Normal);

            AddHorizontalTarget(
                valueFloor,
                new Rect(floorTrack.Left - 8.0, floorTrack.Top - 5.0, floorTrack.Width + 16.0, floorTrack.Height + 10.0),
                floorTrack,
                valueFloor.Minimum,
                valueFloor.Maximum);
        }

        DrawCenteredText(
            dc,
            "PRICING SETTINGS STILL PASS THROUGH TRADER-SPECIFIC MODIFIERS AND OTHER VANILLA SHOP RULES",
            new Point(ActualWidth / 2.0, 459.0),
            7.6,
            muted,
            FontWeights.Normal);
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

    private void DrawStarvationTimeline(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "STARVATION TIMELINE", "see the grace period, repeating damage cadence and active penalties together", text, muted, accent);

        var toggle = P("advanced.starvationMechanics");
        var delay = P("survival.starvationPenaltyDelay");
        var interval = P("survival.starvationDamageInterval");
        var staminaDrain = P("survival.starvationStaminaDrain");
        var damage = P("survival.starvationDamage");
        var healthRegen = P("survival.starvationHealthRegenPenalty");

        DrawTogglePill(
            dc,
            toggle,
            "MASTER STARVATION PENALTIES",
            new Rect(Math.Max(250.0, ActualWidth - 250.0), 43.0, 222.0, 34.0),
            text,
            muted,
            border,
            accent,
            panel);

        var timeline = new Rect(42.0, 103.0, Math.Max(390.0, ActualWidth - 84.0), 62.0);
        var timelineY = timeline.Top + 28.0;
        const double previewSeconds = 50.0;

        double TimeToX(double seconds)
            => timeline.Left + timeline.Width * Math.Clamp(seconds / previewSeconds, 0.0, 1.0);

        dc.DrawRoundedRectangle(
            CloneWithOpacity(panel, 0.92),
            new Pen(border, 1.0),
            new Rect(timeline.Left - 10.0, timeline.Top - 14.0, timeline.Width + 20.0, timeline.Height + 29.0),
            4.0,
            4.0);

        dc.DrawLine(new Pen(border, 3.0), new Point(timeline.Left, timelineY), new Point(timeline.Right, timelineY));

        for (var second = 0; second <= 50; second += 10)
        {
            var tickX = TimeToX(second);
            dc.DrawLine(new Pen(soft, 1.0), new Point(tickX, timelineY - 6.0), new Point(tickX, timelineY + 6.0));
            DrawCenteredText(dc, $"{second}s", new Point(tickX, timelineY + 13.0), 7.6, muted, FontWeights.Normal);
        }

        DrawText(dc, "SEVERE HUNGER", new Point(timeline.Left, timeline.Top - 8.0), 8.2, text, FontWeights.Bold);
        dc.DrawEllipse(accent, null, new Point(timeline.Left, timelineY), 5.5, 5.5);

        if (delay is not null)
        {
            var delayX = TimeToX(delay.CurrentNumber);
            var delayVanillaX = TimeToX(delay.ReferenceNumber);

            dc.DrawRectangle(
                CloneWithOpacity(accent, 0.09),
                null,
                new Rect(timeline.Left, timelineY - 7.0, Math.Max(1.0, delayX - timeline.Left), 14.0));

            dc.DrawLine(new Pen(muted, 1.0), new Point(delayVanillaX, timelineY - 12.0), new Point(delayVanillaX, timelineY + 12.0));
            dc.DrawLine(new Pen(accent, 2.0), new Point(delayX, timelineY - 15.0), new Point(delayX, timelineY + 15.0));
            dc.DrawEllipse(panel, new Pen(accent, IsSelected(delay) ? 2.5 : 1.6), new Point(delayX, timelineY), 7.0, 7.0);

            DrawCenteredText(dc, "PENALTIES START", new Point(delayX, timeline.Top - 8.0), 8.0, accent, FontWeights.Bold);
            DrawCenteredText(dc, FriendlyValue(delay), new Point(delayX, timeline.Bottom + 21.0), 8.4, accent, FontWeights.SemiBold);

            var delayMap = new Rect(
                TimeToX(delay.Minimum),
                timelineY - 15.0,
                Math.Max(1.0, TimeToX(delay.Maximum) - TimeToX(delay.Minimum)),
                30.0);

            AddHorizontalTarget(
                delay,
                new Rect(delayMap.Left - 8.0, delayMap.Top - 4.0, delayMap.Width + 16.0, delayMap.Height + 8.0),
                delayMap,
                delay.Minimum,
                delay.Maximum);
        }

        if (delay is not null && interval is not null)
        {
            var firstTickTime = delay.CurrentNumber + interval.CurrentNumber;
            var firstTickX = TimeToX(firstTickTime);

            var intervalVanillaTime = delay.CurrentNumber + interval.ReferenceNumber;
            var intervalVanillaX = TimeToX(intervalVanillaTime);

            dc.DrawLine(new Pen(muted, 1.0), new Point(intervalVanillaX, timelineY - 11.0), new Point(intervalVanillaX, timelineY + 11.0));
            dc.DrawEllipse(panel, new Pen(accent, IsSelected(interval) ? 2.5 : 1.6), new Point(firstTickX, timelineY), 7.0, 7.0);
            dc.DrawEllipse(accent, null, new Point(firstTickX, timelineY), 3.0, 3.0);

            DrawCenteredText(dc, "1ST DAMAGE", new Point(firstTickX, timeline.Top + 2.0), 7.8, text, FontWeights.Bold);
            DrawCenteredText(dc, $"EVERY {interval.CurrentNumber:0}s", new Point(firstTickX, timeline.Bottom + 21.0), 8.4, accent, FontWeights.SemiBold);

            // Preview the next starvation charges on a real time axis. These are
            // visual-only markers; the first charge handle edits the interval.
            for (var tick = 2; tick <= 8; tick++)
            {
                var tickTime = delay.CurrentNumber + interval.CurrentNumber * tick;
                if (tickTime > previewSeconds + Epsilon)
                    break;

                var tickX = TimeToX(tickTime);
                dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.55), 1.4), new Point(tickX, timelineY - 8.0), new Point(tickX, timelineY + 8.0));
                dc.DrawEllipse(CloneWithOpacity(accent, 0.60), null, new Point(tickX, timelineY), 3.2, 3.2);
            }

            var intervalStartX = TimeToX(delay.CurrentNumber + interval.Minimum);
            var intervalEndX = TimeToX(delay.CurrentNumber + interval.Maximum);
            var intervalMap = new Rect(
                intervalStartX,
                timelineY - 15.0,
                Math.Max(1.0, intervalEndX - intervalStartX),
                30.0);

            AddHorizontalTarget(
                interval,
                new Rect(intervalMap.Left - 8.0, intervalMap.Top - 4.0, intervalMap.Width + 16.0, intervalMap.Height + 8.0),
                intervalMap,
                interval.Minimum,
                interval.Maximum);
        }

        var bundleOn = toggle?.CurrentBoolean == true;
        var pressure = damage is not null && interval is not null && interval.CurrentNumber > Epsilon
            ? damage.CurrentNumber / interval.CurrentNumber
            : 0.0;
        var vanillaPressure = damage is not null && interval is not null && interval.ReferenceNumber > Epsilon
            ? damage.ReferenceNumber / interval.ReferenceNumber
            : 0.0;

        DrawText(
            dc,
            bundleOn ? "ACTIVE PENALTIES" : "BUNDLE OFF — VALUES ARE STAGED",
            new Point(22.0, 206.0),
            8.8,
            bundleOn ? text : muted,
            FontWeights.Bold);

        DrawRightAlignedText(
            dc,
            $"DAMAGE PRESSURE  {pressure:0.00} HP/s   |   VANILLA {vanillaPressure:0.00} HP/s",
            new Point(Math.Max(420.0, ActualWidth - 22.0), 206.0),
            8.3,
            pressure > vanillaPressure + Epsilon ? accent : muted,
            FontWeights.SemiBold);

        var meterArea = new Rect(24.0, 232.0, Math.Max(430.0, ActualWidth - 48.0), 148.0);
        var meterGap = 12.0;
        var meterWidth = (meterArea.Width - meterGap * 2.0) / 3.0;

        var meters = new[]
        {
            (Parameter: staminaDrain, Label: "ACTION STAMINA COST", Footer: "extra cost"),
            (Parameter: damage, Label: "HP PER TICK", Footer: "direct damage"),
            (Parameter: healthRegen, Label: "HEALTH RECOVERY LOSS", Footer: "regen suppressed")
        };

        for (var i = 0; i < meters.Length; i++)
        {
            var meter = meters[i];
            var card = new Rect(
                meterArea.Left + i * (meterWidth + meterGap),
                meterArea.Top,
                meterWidth,
                meterArea.Height);

            dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.94), new Pen(border, 1.0), card, 4.0, 4.0);
            DrawCenteredText(dc, meter.Label, new Point(card.Left + card.Width / 2.0, card.Top + 10.0), 8.2, muted, FontWeights.Bold);

            if (meter.Parameter is not { } parameter)
                continue;

            DrawCenteredText(
                dc,
                FriendlyValue(parameter),
                new Point(card.Left + card.Width / 2.0, card.Top + 29.0),
                10.0,
                accent,
                FontWeights.Bold);

            var bar = new Rect(
                card.Left + Math.Max(22.0, card.Width * 0.28),
                card.Top + 53.0,
                Math.Max(22.0, card.Width * 0.44),
                62.0);

            dc.DrawRoundedRectangle(CloneWithOpacity(border, 0.16), new Pen(border, 1.0), bar, 3.0, 3.0);

            var currentY = ValueToY(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, bar);
            var referenceY = ValueToY(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, bar);

            dc.DrawRectangle(
                CloneWithOpacity(accent, bundleOn ? 0.28 : 0.12),
                null,
                new Rect(bar.Left, currentY, bar.Width, Math.Max(1.0, bar.Bottom - currentY)));

            dc.DrawLine(
                new Pen(muted, 1.2),
                new Point(bar.Left - 4.0, referenceY),
                new Point(bar.Right + 4.0, referenceY));

            dc.DrawLine(
                new Pen(accent, IsSelected(parameter) ? 3.0 : 2.0),
                new Point(bar.Left - 3.0, currentY),
                new Point(bar.Right + 3.0, currentY));

            AddVerticalTarget(
                parameter,
                new Rect(card.Left + 8.0, bar.Top - 6.0, card.Width - 16.0, bar.Height + 12.0),
                bar,
                parameter.Minimum,
                parameter.Maximum);

            DrawCenteredText(dc, meter.Footer, new Point(card.Left + card.Width / 2.0, card.Bottom - 21.0), 7.8, muted, FontWeights.Normal);
        }

        DrawCenteredText(
            dc,
            "VANILLA MARKERS ARE THE THIN GREY LINES — DRAG THE GOLD HANDLES / METERS",
            new Point(ActualWidth / 2.0, 404.0),
            7.8,
            muted,
            FontWeights.Normal);
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
        DrawHeader(
            dc,
            "REPEATED-HIT PROTECTION",
            "the game varies hidden protection strength between a weaker and stronger end",
            text,
            muted,
            accent);

        var minimum = P("combat.accumulatedDamageWeightMin");
        var maximum = P("combat.accumulatedDamageWeightMax");
        var healedMemory = P("combat.accumulatedDamageIncludesHealedHealth");

        var axis = new Rect(58.0, 104.0, Math.Max(380.0, ActualWidth - 116.0), 62.0);
        var currentY = axis.Top + 21.0;
        var vanillaY = axis.Top + 47.0;

        dc.DrawRoundedRectangle(
            CloneWithOpacity(panel, 0.94),
            new Pen(border, 1.0),
            new Rect(axis.Left - 12.0, axis.Top - 28.0, axis.Width + 24.0, axis.Height + 58.0),
            4.0,
            4.0);

        DrawText(dc, "WEAKER", new Point(axis.Left, axis.Top - 22.0), 8.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "PROTECTION STRENGTH", new Point(axis.Left + axis.Width / 2.0, axis.Top - 22.0), 8.2, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "STRONGER", new Point(axis.Right, axis.Top - 22.0), 8.0, accent, FontWeights.Bold);

        dc.DrawLine(new Pen(border, 3.0), new Point(axis.Left, currentY), new Point(axis.Right, currentY));
        dc.DrawLine(new Pen(soft, 2.0), new Point(axis.Left, vanillaY), new Point(axis.Right, vanillaY));

        for (var i = 0; i <= 4; i++)
        {
            var value = i * 0.25;
            var x = ValueToX(value, 0.0, 1.0, axis, reverse: false);

            dc.DrawLine(new Pen(soft, 1.0), new Point(x, currentY - 7.0), new Point(x, vanillaY + 7.0));
            DrawCenteredText(dc, value.ToString("0.00", CultureInfo.InvariantCulture), new Point(x, axis.Bottom + 11.0), 7.6, muted, FontWeights.Normal);
        }

        DrawRightAlignedText(dc, "CURRENT", new Point(axis.Left - 16.0, currentY - 7.0), 7.8, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "VANILLA", new Point(axis.Left - 16.0, vanillaY - 7.0), 7.8, muted, FontWeights.Bold);

        if (minimum is not null && maximum is not null)
        {
            var minX = ValueToX(minimum.CurrentNumber, 0.0, 1.0, axis, reverse: false);
            var maxX = ValueToX(maximum.CurrentNumber, 0.0, 1.0, axis, reverse: false);
            var refMinX = ValueToX(minimum.ReferenceNumber, 0.0, 1.0, axis, reverse: false);
            var refMaxX = ValueToX(maximum.ReferenceNumber, 0.0, 1.0, axis, reverse: false);

            var bandLeft = Math.Min(minX, maxX);
            var bandRight = Math.Max(minX, maxX);
            var refBandLeft = Math.Min(refMinX, refMaxX);
            var refBandRight = Math.Max(refMinX, refMaxX);

            dc.DrawRoundedRectangle(
                CloneWithOpacity(accent, 0.22),
                null,
                new Rect(bandLeft, currentY - 8.0, Math.Max(1.0, bandRight - bandLeft), 16.0),
                3.0,
                3.0);
            dc.DrawLine(new Pen(accent, 3.0), new Point(bandLeft, currentY), new Point(bandRight, currentY));

            var vanillaPen = new Pen(muted, 1.4) { DashStyle = DashStyles.Dash };
            dc.DrawRoundedRectangle(
                CloneWithOpacity(muted, 0.08),
                vanillaPen,
                new Rect(refBandLeft, vanillaY - 6.0, Math.Max(1.0, refBandRight - refBandLeft), 12.0),
                2.0,
                2.0);

            dc.DrawLine(new Pen(muted, 1.0), new Point(refMinX, vanillaY - 9.0), new Point(refMinX, vanillaY + 9.0));
            dc.DrawLine(new Pen(muted, 1.0), new Point(refMaxX, vanillaY - 9.0), new Point(refMaxX, vanillaY + 9.0));

            dc.DrawEllipse(
                panel,
                new Pen(accent, IsSelected(minimum) ? 2.8 : 1.7),
                new Point(minX, currentY),
                8.0,
                8.0);
            dc.DrawEllipse(accent, null, new Point(minX, currentY), 3.2, 3.2);

            dc.DrawEllipse(
                panel,
                new Pen(accent, IsSelected(maximum) ? 2.8 : 1.7),
                new Point(maxX, currentY),
                8.0,
                8.0);
            dc.DrawEllipse(accent, null, new Point(maxX, currentY), 3.2, 3.2);

            DrawCenteredText(
                dc,
                $"MIN  {minimum.CurrentNumber:0.00}",
                new Point(minX, currentY - 26.0),
                8.6,
                text,
                FontWeights.Bold);

            DrawCenteredText(
                dc,
                $"MAX  {maximum.CurrentNumber:0.00}",
                new Point(maxX, currentY + 13.0),
                8.6,
                accent,
                FontWeights.Bold);

            DrawCenteredText(
                dc,
                $"{minimum.ReferenceNumber:0.00} → {maximum.ReferenceNumber:0.00}",
                new Point((refMinX + refMaxX) / 2.0, vanillaY + 10.0),
                7.6,
                muted,
                FontWeights.SemiBold);

            var minMap = new Rect(axis.Left, currentY - 17.0, axis.Width, 34.0);
            var maxMap = new Rect(axis.Left, currentY - 17.0, axis.Width, 34.0);

            AddHorizontalTarget(
                minimum,
                new Rect(minX - 24.0, currentY - 26.0, 48.0, 26.0),
                minMap,
                0.0,
                1.0);

            AddHorizontalTarget(
                maximum,
                new Rect(maxX - 24.0, currentY, 48.0, 26.0),
                maxMap,
                0.0,
                1.0);

            var midpoint = (minimum.CurrentNumber + maximum.CurrentNumber) / 2.0;
            var width = Math.Max(0.0, maximum.CurrentNumber - minimum.CurrentNumber);

            DrawCenteredText(
                dc,
                $"ENVELOPE CENTER {midpoint:0.00}   |   SPAN {width:0.00}",
                new Point(ActualWidth / 2.0, 204.0),
                8.2,
                muted,
                FontWeights.SemiBold);
        }

        DrawCenteredText(
            dc,
            "0.00–1.00 ARE CURVE WEIGHTS, NOT LITERAL DAMAGE-REDUCTION PERCENTAGES",
            new Point(ActualWidth / 2.0, 226.0),
            7.8,
            muted,
            FontWeights.Normal);

        DrawTogglePill(
            dc,
            healedMemory,
            "HEALED DAMAGE REMAINS IN PROTECTION MEMORY",
            new Rect(52.0, 252.0, Math.Max(360.0, ActualWidth - 104.0), 42.0),
            text,
            muted,
            border,
            accent,
            panel);

        DrawCenteredText(
            dc,
            healedMemory?.CurrentBoolean == true
                ? "ON: HEALING RESTORES HP, BUT EARLIER HITS STILL COUNT TOWARD REPEATED-HIT PROTECTION"
                : "OFF: HEALED DAMAGE NO LONGER STAYS IN THE REPEATED-HIT TRACKER",
            new Point(ActualWidth / 2.0, 307.0),
            7.8,
            healedMemory?.CurrentBoolean == true ? accent : muted,
            FontWeights.SemiBold);
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
        DrawHeader(
            dc,
            "RADIATION PRESSURE",
            "exposure gain, natural clearance and downstream health damage are separate systems",
            text,
            muted,
            accent);

        var buildup = P("survival.radiationAccumulation");
        var recovery = P("survival.radiationRecovery");
        var damage = P("survival.radiationDamage");

        var left = 34.0;
        var right = Math.Max(left + 440.0, ActualWidth - 34.0);
        var centerX = (left + right) / 2.0;
        var gap = 22.0;
        var flowY = 112.0;

        var recoveryMap = new Rect(left, flowY - 16.0, Math.Max(150.0, centerX - gap - left), 32.0);
        var buildupMap = new Rect(centerX + gap, flowY - 16.0, Math.Max(150.0, right - (centerX + gap)), 32.0);

        dc.DrawRoundedRectangle(
            CloneWithOpacity(panel, 0.94),
            new Pen(border, 1.0),
            new Rect(left - 12.0, 62.0, right - left + 24.0, 105.0),
            4.0,
            4.0);

        dc.DrawLine(new Pen(soft, 1.0), new Point(centerX, 76.0), new Point(centerX, 153.0));

        DrawText(dc, "NATURAL CLEARANCE", new Point(recoveryMap.Left, 72.0), 8.7, text, FontWeights.Bold);
        DrawText(dc, "absolute passive removal", new Point(recoveryMap.Left, 87.0), 7.7, muted, FontWeights.Normal);

        DrawRightAlignedText(dc, "EXPOSURE GAIN", new Point(buildupMap.Right, 72.0), 8.7, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "difficulty scaler on incoming radiation", new Point(buildupMap.Right, 87.0), 7.7, muted, FontWeights.Normal);

        dc.DrawLine(new Pen(border, 4.0), new Point(recoveryMap.Left, flowY), new Point(recoveryMap.Right, flowY));
        dc.DrawLine(new Pen(border, 4.0), new Point(buildupMap.Left, flowY), new Point(buildupMap.Right, flowY));

        if (recovery is not null)
        {
            var currentX = ValueToX(recovery.CurrentNumber, recovery.Minimum, recovery.Maximum, recoveryMap, reverse: true);
            var vanillaX = ValueToX(recovery.ReferenceNumber, recovery.Minimum, recovery.Maximum, recoveryMap, reverse: true);

            dc.DrawLine(new Pen(accent, 4.0), new Point(currentX, flowY), new Point(recoveryMap.Right, flowY));
            dc.DrawLine(new Pen(muted, 1.2), new Point(vanillaX, flowY - 9.0), new Point(vanillaX, flowY + 9.0));
            dc.DrawEllipse(
                panel,
                new Pen(accent, IsSelected(recovery) ? 2.6 : 1.6),
                new Point(currentX, flowY),
                7.0,
                7.0);
            dc.DrawEllipse(accent, null, new Point(currentX, flowY), 3.0, 3.0);

            DrawText(dc, FriendlyValue(recovery), new Point(recoveryMap.Left, 132.0), 9.2, accent, FontWeights.Bold);

            var recoveryFactor = Math.Abs(recovery.ReferenceNumber) > Epsilon
                ? recovery.CurrentNumber / recovery.ReferenceNumber
                : 0.0;
            DrawRightAlignedText(
                dc,
                $"{recoveryFactor:0.##}x BASE",
                new Point(recoveryMap.Right, 132.0),
                8.2,
                muted,
                FontWeights.SemiBold);

            AddHorizontalTarget(
                recovery,
                new Rect(recoveryMap.Left - 8.0, recoveryMap.Top - 5.0, recoveryMap.Width + 16.0, recoveryMap.Height + 10.0),
                recoveryMap,
                recovery.Minimum,
                recovery.Maximum,
                reverse: true);
        }

        if (buildup is not null)
        {
            var currentX = ValueToX(buildup.CurrentNumber, buildup.Minimum, buildup.Maximum, buildupMap, reverse: false);
            var vanillaX = ValueToX(buildup.ReferenceNumber, buildup.Minimum, buildup.Maximum, buildupMap, reverse: false);

            dc.DrawLine(new Pen(accent, 4.0), new Point(buildupMap.Left, flowY), new Point(currentX, flowY));
            dc.DrawLine(new Pen(muted, 1.2), new Point(vanillaX, flowY - 9.0), new Point(vanillaX, flowY + 9.0));
            dc.DrawEllipse(
                panel,
                new Pen(accent, IsSelected(buildup) ? 2.6 : 1.6),
                new Point(currentX, flowY),
                7.0,
                7.0);
            dc.DrawEllipse(accent, null, new Point(currentX, flowY), 3.0, 3.0);

            DrawText(dc, $"{buildup.CurrentNumber:0.##}x gain", new Point(buildupMap.Left, 132.0), 9.2, accent, FontWeights.Bold);

            var buildupFactor = Math.Abs(buildup.ReferenceNumber) > Epsilon
                ? buildup.CurrentNumber / buildup.ReferenceNumber
                : 0.0;
            DrawRightAlignedText(
                dc,
                $"{buildupFactor:0.##}x BASE",
                new Point(buildupMap.Right, 132.0),
                8.2,
                muted,
                FontWeights.SemiBold);

            AddHorizontalTarget(
                buildup,
                new Rect(buildupMap.Left - 8.0, buildupMap.Top - 5.0, buildupMap.Width + 16.0, buildupMap.Height + 10.0),
                buildupMap,
                buildup.Minimum,
                buildup.Maximum);
        }

        var recoveryRelative = recovery is not null && Math.Abs(recovery.ReferenceNumber) > Epsilon
            ? recovery.CurrentNumber / recovery.ReferenceNumber
            : 0.0;
        var buildupRelative = buildup is not null && Math.Abs(buildup.ReferenceNumber) > Epsilon
            ? buildup.CurrentNumber / buildup.ReferenceNumber
            : 0.0;
        var relativeBias = buildupRelative - recoveryRelative;

        var biasCard = new Rect(left, 183.0, right - left, 76.0);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.94), new Pen(border, 1.0), biasCard, 4.0, 4.0);

        DrawText(dc, "RELATIVE TUNING BIAS", new Point(biasCard.Left + 12.0, biasCard.Top + 9.0), 8.6, muted, FontWeights.Bold);
        DrawRightAlignedText(
            dc,
            "NORMALIZED TO BASE PRESET — NOT A LITERAL NET RAD/S",
            new Point(biasCard.Right - 12.0, biasCard.Top + 9.0),
            7.5,
            muted,
            FontWeights.Normal);

        var biasAxis = new Rect(biasCard.Left + 24.0, biasCard.Top + 42.0, biasCard.Width - 48.0, 20.0);
        var biasY = biasAxis.Top + biasAxis.Height / 2.0;
        var biasCenterX = biasAxis.Left + biasAxis.Width / 2.0;
        dc.DrawLine(new Pen(border, 3.0), new Point(biasAxis.Left, biasY), new Point(biasAxis.Right, biasY));
        dc.DrawLine(new Pen(soft, 1.0), new Point(biasCenterX, biasY - 11.0), new Point(biasCenterX, biasY + 11.0));

        var biasMagnitude = Math.Clamp(Math.Abs(relativeBias) / 3.0, 0.0, 1.0);
        var biasX = relativeBias >= 0.0
            ? biasCenterX + (biasAxis.Width / 2.0) * biasMagnitude
            : biasCenterX - (biasAxis.Width / 2.0) * biasMagnitude;

        dc.DrawLine(new Pen(accent, 4.0), new Point(biasCenterX, biasY), new Point(biasX, biasY));
        dc.DrawEllipse(accent, null, new Point(biasX, biasY), 5.5, 5.5);

        DrawText(dc, "MORE CLEARANCE", new Point(biasAxis.Left, biasY - 24.0), 7.6, muted, FontWeights.Bold);
        DrawCenteredText(dc, "BASELINE", new Point(biasCenterX, biasY - 24.0), 7.6, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "MORE BUILD-UP", new Point(biasAxis.Right, biasY - 24.0), 7.6, accent, FontWeights.Bold);

        var biasLabel = Math.Abs(relativeBias) <= 0.05
            ? "BASE-PRESET BALANCE"
            : relativeBias > 0.0
                ? $"{relativeBias:+0.00;-0.00;0.00} RELATIVE PRESSURE"
                : $"{relativeBias:+0.00;-0.00;0.00} RELATIVE PRESSURE";

        DrawCenteredText(
            dc,
            biasLabel,
            new Point(biasCenterX, biasY + 12.0),
            8.4,
            Math.Abs(relativeBias) <= 0.05 ? muted : accent,
            FontWeights.SemiBold);

        var damageCard = new Rect(left, 278.0, right - left, 88.0);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.94), new Pen(border, 1.0), damageCard, 4.0, 4.0);

        DrawText(dc, "HEALTH DAMAGE AFTER RADIATION BECOMES HARMFUL", new Point(damageCard.Left + 12.0, damageCard.Top + 9.0), 8.5, text, FontWeights.Bold);
        DrawRightAlignedText(
            dc,
            "RADIATION STATE THRESHOLDS STAY VANILLA",
            new Point(damageCard.Right - 12.0, damageCard.Top + 9.0),
            7.7,
            muted,
            FontWeights.Normal);

        if (damage is not null)
        {
            DrawRightAlignedText(
                dc,
                FriendlyValue(damage),
                new Point(damageCard.Right - 12.0, damageCard.Top + 28.0),
                10.0,
                accent,
                FontWeights.Bold);

            var damageTrack = new Rect(damageCard.Left + 18.0, damageCard.Top + 50.0, damageCard.Width - 36.0, 24.0);
            var damageY = damageTrack.Top + damageTrack.Height / 2.0;

            dc.DrawLine(new Pen(border, 4.0), new Point(damageTrack.Left, damageY), new Point(damageTrack.Right, damageY));

            var currentX = ValueToX(damage.CurrentNumber, damage.Minimum, damage.Maximum, damageTrack, reverse: false);
            var vanillaX = ValueToX(damage.ReferenceNumber, damage.Minimum, damage.Maximum, damageTrack, reverse: false);

            dc.DrawLine(new Pen(accent, 4.0), new Point(damageTrack.Left, damageY), new Point(currentX, damageY));
            dc.DrawLine(new Pen(muted, 1.2), new Point(vanillaX, damageY - 9.0), new Point(vanillaX, damageY + 9.0));

            dc.DrawEllipse(
                panel,
                new Pen(accent, IsSelected(damage) ? 2.6 : 1.6),
                new Point(currentX, damageY),
                7.0,
                7.0);
            dc.DrawEllipse(accent, null, new Point(currentX, damageY), 3.0, 3.0);

            DrawText(dc, "0x", new Point(damageTrack.Left, damageY + 10.0), 7.5, muted, FontWeights.Normal);
            DrawCenteredText(dc, "VANILLA 1x", new Point(vanillaX, damageY + 10.0), 7.5, muted, FontWeights.SemiBold);
            DrawRightAlignedText(dc, "2x", new Point(damageTrack.Right, damageY + 10.0), 7.5, muted, FontWeights.Normal);

            AddHorizontalTarget(
                damage,
                new Rect(damageTrack.Left - 8.0, damageTrack.Top - 5.0, damageTrack.Width + 16.0, damageTrack.Height + 10.0),
                damageTrack,
                damage.Minimum,
                damage.Maximum);
        }

        DrawCenteredText(
            dc,
            "GREY MARKERS = BASE PRESET / VANILLA REFERENCES",
            new Point(ActualWidth / 2.0, 389.0),
            7.7,
            muted,
            FontWeights.Normal);
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
        DrawHeader(
            dc,
            "BREAKABLE BOX AMMO MIXER",
            "relative selection weights inside the two authored ammo pools",
            text,
            muted,
            accent);

        var p918 = P("loot.ammoBoxWeight918");
        var p045 = P("loot.ammoBoxWeight045");
        var p545 = P("loot.ammoBoxWeight545");
        var p012 = P("loot.ammoBoxWeight012");

        var parameters = new[] { p918, p045, p545, p012 };
        var labels = new[] { "9x18 / 9x19", ".45 ACP", "5.45 / 5.56", "12 GAUGE" };
        var opacities = new[] { 0.95, 0.74, 0.54, 0.34 };

        var w918 = Math.Max(0.0, p918?.CurrentNumber ?? 0.0);
        var w045 = Math.Max(0.0, p045?.CurrentNumber ?? 0.0);
        var w545 = Math.Max(0.0, p545?.CurrentNumber ?? 0.0);
        var w012 = Math.Max(0.0, p012?.CurrentNumber ?? 0.0);

        // SNG has two entries driven by the 9x18/9x19 family (A918D + A919D),
        // one 5.45 entry and one 12 Gauge entry. NATO has one entry from each family.
        var sngRaw = new[] { 2.0 * w918, w545, w012 };
        var natoRaw = new[] { w045, w918, w545, w012 };

        double[] Normalize(IReadOnlyList<double> raw)
        {
            var total = raw.Sum();
            if (total <= Epsilon)
                return raw.Select(_ => 0.0).ToArray();

            return raw.Select(value => value / total).ToArray();
        }

        var sngShares = Normalize(sngRaw);
        var natoShares = Normalize(natoRaw);

        void DrawPool(
            string title,
            string authored,
            IReadOnlyList<string> poolLabels,
            IReadOnlyList<double> shares,
            IReadOnlyList<int> familyOpacityIndices,
            Rect card,
            string vanillaSummary)
        {
            dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.95), new Pen(border, 1.0), card, 4.0, 4.0);
            DrawText(dc, title, new Point(card.Left + 12.0, card.Top + 8.0), 8.9, text, FontWeights.Bold);
            DrawRightAlignedText(
                dc,
                $"VANILLA {vanillaSummary}",
                new Point(card.Right - 12.0, card.Top + 8.0),
                7.6,
                muted,
                FontWeights.SemiBold);

            var stack = new Rect(card.Left + 14.0, card.Top + 31.0, card.Width - 28.0, 36.0);
            dc.DrawRoundedRectangle(CloneWithOpacity(border, 0.08), new Pen(border, 1.0), stack, 3.0, 3.0);

            var total = shares.Sum();
            if (total <= Epsilon)
            {
                DrawCenteredText(
                    dc,
                    "NO ACTIVE WEIGHT",
                    new Point(stack.Left + stack.Width / 2.0, stack.Top + 11.0),
                    8.4,
                    muted,
                    FontWeights.Bold);
                return;
            }

            var cursor = stack.Left;
            for (var i = 0; i < shares.Count; i++)
            {
                var share = shares[i];
                var segmentWidth = i == shares.Count - 1
                    ? stack.Right - cursor
                    : stack.Width * share;

                var segment = new Rect(cursor, stack.Top, Math.Max(0.0, segmentWidth), stack.Height);
                var opacityIndex = familyOpacityIndices[i];
                dc.DrawRectangle(CloneWithOpacity(accent, opacities[opacityIndex]), null, segment);

                if (segment.Width >= 72.0)
                {
                    DrawCenteredText(
                        dc,
                        poolLabels[i],
                        new Point(segment.Left + segment.Width / 2.0, segment.Top + 7.0),
                        7.7,
                        text,
                        FontWeights.Bold);
                    DrawCenteredText(
                        dc,
                        $"{share * 100:0}%",
                        new Point(segment.Left + segment.Width / 2.0, segment.Top + 21.0),
                        8.1,
                        text,
                        FontWeights.SemiBold);
                }
                else if (segment.Width >= 34.0)
                {
                    DrawCenteredText(
                        dc,
                        $"{share * 100:0}%",
                        new Point(segment.Left + segment.Width / 2.0, segment.Top + 13.0),
                        7.5,
                        text,
                        FontWeights.Bold);
                }

                cursor += segmentWidth;
            }
        }

        var poolLeft = 24.0;
        var poolWidth = Math.Max(440.0, ActualWidth - 48.0);

        DrawPool(
            "SNG POOL",
            "A918D + A919D  |  A545D  |  A012D",
            new[] { "9x18/19", "5.45", "12G" },
            sngShares,
            new[] { 0, 2, 3 },
            new Rect(poolLeft, 58.0, poolWidth, 76.0),
            "50 / 25 / 25");

        DrawPool(
            "NATO POOL",
            "A045D  |  A919D  |  A556D  |  A012D",
            new[] { ".45", "9x19", "5.56", "12G" },
            natoShares,
            new[] { 1, 0, 2, 3 },
            new Rect(poolLeft, 142.0, poolWidth, 76.0),
            "25 / 25 / 25 / 25");

        DrawText(dc, "FAMILY WEIGHTS", new Point(24.0, 232.0), 9.0, muted, FontWeights.Bold);
        DrawRightAlignedText(
            dc,
            "1x = authored Weight 2   •   0x = remove existing family entries from weighted selection",
            new Point(Math.Max(430.0, ActualWidth - 24.0), 232.0),
            7.5,
            muted,
            FontWeights.Normal);

        for (var i = 0; i < parameters.Length; i++)
        {
            if (parameters[i] is not { } parameter)
                continue;

            var rowY = 258.0 + i * 44.0;
            DrawText(dc, labels[i], new Point(26.0, rowY + 3.0), 8.9, text, FontWeights.Bold);
            DrawText(dc, $"{parameter.CurrentNumber:0.##}x", new Point(128.0, rowY + 3.0), 9.0, accent, FontWeights.Bold);

            string shareText;
            if (i == 0)
                shareText = $"SNG {sngShares[0] * 100:0}%  |  NATO {natoShares[1] * 100:0}%";
            else if (i == 1)
                shareText = $"SNG —  |  NATO {natoShares[0] * 100:0}%";
            else if (i == 2)
                shareText = $"SNG {sngShares[1] * 100:0}%  |  NATO {natoShares[2] * 100:0}%";
            else
                shareText = $"SNG {sngShares[2] * 100:0}%  |  NATO {natoShares[3] * 100:0}%";

            DrawText(dc, shareText, new Point(180.0, rowY + 3.0), 7.8, muted, FontWeights.SemiBold);

            DrawInlineHorizontalTrack(
                dc,
                parameter,
                new Rect(330.0, rowY - 2.0, Math.Max(150.0, ActualWidth - 358.0), 26.0),
                text,
                muted,
                border,
                accent);
        }

        DrawCenteredText(
            dc,
            "POOL % = NORMALIZED WEIGHT SHARE INSIDE THAT POOL, NOT GLOBAL BOX DROP CHANCE",
            new Point(ActualWidth / 2.0, 430.0),
            9.0,
            muted,
            FontWeights.Normal);

        DrawCenteredText(
            dc,
            "SNG MUST KEEP AT LEAST ONE OF 9x18/19, 5.45/5.56 OR 12 GAUGE ABOVE 0x",
            new Point(ActualWidth / 2.0, 449.0),
            9.0,
            accent,
            FontWeights.SemiBold);
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
            "ConsumableMixer" => 475.0,
            "SleepingDashboard" => 390.0,
            "HungerLadder" => 380.0,
            "StarvationTimeline" => 430.0,
            "RepeatedHitBand" => 335.0,
            "RadiationBalance" => 415.0,
            "TradingDashboard" => 485.0,
            "RepairCurve" => 370.0,
            "AmmoDistribution" => 470.0,
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
