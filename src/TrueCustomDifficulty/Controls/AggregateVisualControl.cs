using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
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
    // Read-only cross-tab input to Starvation's hypothetical full-HP timeline.
    // Deliberately excluded from the Starvation family's editable/reset members.
    private ParameterViewModel? _starvationMaxHp;
    private readonly List<HitTarget> _hitTargets = new();
    private readonly VisualStatusMessage _status;
    private bool _fatigueReloadInteraction;

    private HitTarget? _dragTarget;
    private HitTarget? _hoverTarget;
    private ParameterViewModel? _selectedParameter;
    // Distinguish the two handles that edit one range-valued source parameter.
    private HitKind? _selectedHitKind;

    // The aggregate anchor owns the visible row, but each mixer channel must
    // activate its own parameter rationale in the parent view model.
    public event EventHandler<AggregateParameterSelectedEventArgs>? ParameterSelected;

    private void RaiseParameterSelected(ParameterViewModel parameter)
    {
        if (Family is "AnomalyGlobalDamage" or "FlycatcherSlowdown" or "AnomalySpecialEffects" or "ServiceCostsBoard"
            or "CouponFlow" or "TraderStockBoard" or "ItemWeightBoard" or "EquipmentWearBalance" or "BreakableBoxYields"
            or "StashRewardProfile" or "NpcLootLoadout" or "HumanCombatProfile" or "PlayerWeaponProfile"
            or "StarvationTimeline" or "SevereFatiguePenaltyBoard"
            or "TradingDashboard" or "RepairCurve" or "SleepingDashboard"
            or "RadiationBalance" or "StaminaCore" or "ConsumableMixer"
            or "MasterLowHealth" or "MasterRadiationPenalty" or "MasterMedicalHealing")
            ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(parameter));
    }

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
        ToolTipService.SetInitialShowDelay(this, 550);
        ToolTipService.SetShowDuration(this, 10000);
        Loaded += (_, _) => RebindParameters();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        // The paired Economy dashboard cards must measure at their actual host
        // width rather than demanding the unrelated 520-DIP plot minimum.
        var compactBoard = Family is "CouponFlow" or "TradingDashboard"
            or "ServiceCostsBoard" or "RepairCurve"
            or "MasterLowHealth" or "MasterRadiationPenalty";
        var width = double.IsInfinity(availableSize.Width) ? 760.0
            : Math.Max(compactBoard ? 280.0 : 520.0, availableSize.Width);
        return new Size(width, GetDesiredHeight(Family, width));
    }

    private static void OnBindingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AggregateVisualControl)d).RebindParameters();

    private void RebindParameters()
    {
        foreach (var parameter in _parameters.Values)
            parameter.PropertyChanged -= Parameter_PropertyChanged;
        if (_starvationMaxHp is not null)
            _starvationMaxHp.PropertyChanged -= Parameter_PropertyChanged;
        _starvationMaxHp = null;

        _parameters.Clear();

        // Observe Max HP without adding it to GetFamilyIds: group reset (R)
        // must never reset the Health tab from the Starvation panel.
        if (Family == "StarvationTimeline" && Parameters is not null)
        {
            _starvationMaxHp = Parameters.OfType<ParameterViewModel>()
                .FirstOrDefault(p => p.Id == "health.maxHP");
            if (_starvationMaxHp is not null)
                _starvationMaxHp.PropertyChanged += Parameter_PropertyChanged;
        }

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
        _hoverTarget = null;
        ToolTip = null;
        Cursor = Cursors.Arrow;
        _selectedParameter = null;
        _selectedHitKind = null;
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void Parameter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ParameterViewModel.CurrentNumber)
            or nameof(ParameterViewModel.TickFrequency)
            or nameof(ParameterViewModel.CurrentBoolean)
            or nameof(ParameterViewModel.DrinkDurationOverrideEnabled)
            or nameof(ParameterViewModel.Maximum)
            or nameof(ParameterViewModel.CurrentText)
            or nameof(ParameterViewModel.CurrentMinNumber)
            or nameof(ParameterViewModel.CurrentMaxNumber)
            or nameof(ParameterViewModel.ReferenceChoiceIndex)
            or nameof(ParameterViewModel.ReferenceNumber)
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
            case "BlastDamageBoard":
                DrawBlastDamageBoard(dc, text, muted, border, accent, panel);
                break;
            case "TraderStockBoard":
                DrawTraderStockKnobs(dc, text, muted, border, accent, panel);
                break;
            case "ServiceCostsBoard":
                DrawServicePriceKnobs(dc, text, muted, border, accent, panel);
                break;
            case "CouponFlow":
                DrawCouponFlow(dc, text, muted, border, accent, panel);
                break;
            case "ItemWeightBoard":
                DrawEquipmentWeightBoard(dc, text, muted, border, accent, panel);
                break;
            case "AnomalyGlobalDamage":
                DrawAnomalyGlobalDamage(dc, text, muted, border, accent, panel);
                break;
            case "FlycatcherSlowdown":
                DrawFlycatcherSlowdown(dc, text, muted, border, soft, accent, panel);
                break;
            case "AnomalySpecialEffects":
                DrawAnomalySpecialEffects(dc, text, muted, border, accent, panel);
                break;
            case "NpcLootLoadout":
                DrawNpcLootLoadout(dc, text, muted, border, accent, panel);
                break;
            case "WorldLootLayout":
                DrawWorldLootLayout(dc, text, muted, border, accent, panel);
                break;
            case "StashRewardProfile":
                DrawCompactStashRewards(dc, text, muted, border, accent, panel);
                break;
            case "BreakableBoxYields":
                // Retired family: its two sources are now drawn by AmmoDistribution.
                break;
            case "MutantCombatPressure":
            case "StaminaCore":
                DrawStaminaMovementBoard(dc, text, muted, border, accent, panel);
                break;
            case "WaterStaminaPressure":
                DrawWaterStaminaBoard(dc, text, muted, border, accent, panel);
                break;
            case "PhysicalLimits":
                DrawPairedSystemBoard(dc, text, muted, border, accent, panel);
                break;
            case "FatigueCurve":
                DrawFatigueCurve(dc, text, muted, border, soft, accent, panel);
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
                DrawYourWeaponsKnobs(dc, text, muted, border, accent, panel);
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
                DrawRepairCurve(dc, text, muted, border, accent, panel);
                break;
            case "AmmoDistribution":
                DrawAmmoDistribution(dc, text, muted, border, soft, accent, panel);
                break;
            default:
                DrawHeader(dc, "VISUAL CONTROL", "No visual family is configured.", text, muted, accent);
                break;
        }

        // One canonical overlay owns hover feedback for all ordinary native
        // horizontal / vertical / range / choice handles and switch indicators.
        // Graph joint handles call the same helper at their derived coordinates.
        DrawHoveredHandle(dc, accent);
    }





    private void DrawHoveredHandle(DrawingContext dc, Brush accent)
    {
        var target = _hoverTarget;
        if (target is null || !target.Parameter.IsControlEnabled)
            return;

        var source = target.Parameter;
        // All three Equipment rings draw their own matching Combat-style halo.
        // The generic map-center glow would be in the wrong position here.
        if (Family is "EquipmentWearBalance" or "FlycatcherSlowdown"
            or "AnomalyGlobalDamage" or "AnomalySpecialEffects" or "PlayerWeaponProfile")
            return;
        if (Family == "HumanCombatProfile" && target.Kind == HitKind.Knob)
            return;
        // Crosshair X/Y handles already draw the exact shared glow at their
        // own axes. Avoid a second overlay from their broad plot map rect.
        if (Family is "HumanCombatProfile" or "PsyPhantomPressure"
            && target.Kind is HitKind.Horizontal or HitKind.Vertical
            && target.MapRect.Width > 100.0 && target.MapRect.Height > 100.0)
            return;
        // The Hunger LOW control already draws its own glow at (lowX, Y(0)).
        // Its horizontal hit target maps X across the tall 2D graph: the
        // generic overlay would place a second ring at the plot's mid-height.
        // Keep only the real LOW handle glow, as with the other Hunger joints.
        if (Family == "HungerLadder"
            && target.Kind == HitKind.Horizontal
            && source.Id == "survival.hungerLowThreshold")
            return;
        if (Family == "SleepingDashboard"
            && target.Kind == HitKind.Horizontal
            && source.Id == "survival.minimumSleepHours")
            return;
        // Landmark captions snap to exact authored values. Their tiny fixed
        // mapping rectangles are not actual handles, so suppress the
        // generic selection glow on the caption.
        if (Family == "StaminaCore"
            && target.Kind == HitKind.Horizontal
            && source.Id == "stamina.vaultMaxHeight"
            && Math.Abs(target.VisualMaximum - target.VisualMinimum) < Epsilon)
            return;
        if (target.Kind == HitKind.Vertical
            && Family is ("SevereFatiguePenaltyBoard" or "RadiationBalance"
                or "StashRewardProfile" or "RepairCurve"))
            return;
        if (Family == "TradingDashboard"
            && target.Kind == HitKind.Horizontal
            && target.Parameter.Id is ("economy.buyCondition" or "economy.sellCondition"))
            return;
        var range = Math.Max(0.000001, target.VisualMaximum - target.VisualMinimum);
        // The two NPC condition handles share one rail except when they
        // nearly overlap, where both real centers are separated by 7 DIP.
        // Hover glow must follow the rendered center, not the track center.
        var conditionSplit = Family == "NpcLootLoadout" && source.IsRange
            && Math.Abs(ValueToX(source.CurrentMinNumber, target.VisualMinimum,
                    target.VisualMaximum, target.MapRect, false)
                - ValueToX(source.CurrentMaxNumber, target.VisualMinimum,
                    target.VisualMaximum, target.MapRect, false)) < 16.0;
        var conditionOffset = conditionSplit ? 7.0 : 0.0;
        var center = target.Kind switch
        {
            HitKind.Horizontal => new Point(
                ValueToX(source.CurrentNumber, target.VisualMinimum,
                    target.VisualMaximum, target.MapRect, target.Reverse),
                target.MapRect.Top + target.MapRect.Height / 2.0),
            HitKind.Vertical => new Point(
                target.MapRect.Left + target.MapRect.Width / 2.0,
                ValueToY(source.CurrentNumber, target.VisualMinimum,
                    target.VisualMaximum, target.MapRect)),
            HitKind.RangeMin => new Point(
                ValueToX(source.CurrentMinNumber, target.VisualMinimum,
                    target.VisualMaximum, target.MapRect, false),
                target.MapRect.Top + target.MapRect.Height / 2.0
                    - conditionOffset),
            HitKind.RangeMax => new Point(
                ValueToX(source.CurrentMaxNumber, target.VisualMinimum,
                    target.VisualMaximum, target.MapRect, false),
                target.MapRect.Top + target.MapRect.Height / 2.0
                    + conditionOffset),
            HitKind.Choice when Family == "NpcLootLoadout" =>
                new Point(double.NaN, double.NaN), // chip hover uses chip border
            HitKind.Choice => new Point(
                target.MapRect.Left + target.MapRect.Width *
                    Math.Clamp((source.ChoiceIndex - target.VisualMinimum) / range, 0.0, 1.0),
                target.MapRect.Top + target.MapRect.Height / 2.0),
            // Hovering the reference/Base marker must remain unobtrusive.
            _ => new Point(double.NaN, double.NaN)
        };

        if (double.IsFinite(center.X) && double.IsFinite(center.Y))
            ControlHoverGlow.Draw(dc, center, accent, hovered: true);
    }

    // The original difficulty multiplier is the only editable source value.
    // The second band compares it to the selected Base Preset: it is not
    // simulated explosion damage, radius or an extra clickable control.
    private void DrawBlastDamageBoard(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "EXPLOSION DAMAGE",
            "",
            text, muted, accent);

        var damage = P("combat.explosionDamage");
        if (damage is null)
        {
            DrawCenteredText(dc, "Explosion difficulty data unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var width = Math.Max(1.0, ActualWidth - 32.0);
        DrawHorizontalTrackCard(dc, damage, "BLAST DAMAGE MULTIPLIER",
            new Rect(16.0, 56.0, width, 72.0),
            text, muted, border, accent, panel, pairedValueFormatting: true);

        var preview = new Rect(16.0, 141.0, width, 103.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), preview, 4.0, 4.0);
        DrawText(dc, "DIFFICULTY REFERENCE / READ-ONLY",
            new Point(28.0, 150.0), 9.8, text, FontWeights.Bold);

        var band = new Rect(preview.Left + 18.0, 194.0,
            Math.Max(1.0, preview.Width - 36.0), 18.0);
        var y = band.Top + band.Height / 2.0;
        var selectedX = ValueToX(damage.CurrentNumber,
            damage.Minimum, damage.Maximum, band, false);
        var baseX = ValueToX(damage.ReferenceNumber,
            damage.Minimum, damage.Maximum, band, false);
        dc.DrawLine(new Pen(border, 5.0),
            new Point(band.Left, y), new Point(band.Right, y));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.70), 5.0),
            new Point(Math.Min(selectedX, baseX), y),
            new Point(Math.Max(selectedX, baseX), y));
        dc.DrawLine(new Pen(muted, 2.2),
            new Point(baseX, y - 10.0), new Point(baseX, y + 10.0));
        dc.DrawEllipse(accent, null, new Point(selectedX, y), 5.0, 5.0);
        DrawText(dc, "0x", new Point(band.Left, 218.0),
            9.0, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, "2x", new Point(band.Right, 218.0),
            9.0, muted, FontWeights.Normal);
    }

    // Anomalies are four independent source-exact panels: global strength,
    // eleven individual damage scalers (the dedicated radar), a 2D Flycatcher,
    // and three special-effect sources. No six-strip mixer remains.

    // Economy panels reuse the same compact Stamina-style horizontal controls.
    // Shared compact Economy card dimensions remain in use by the original
    // item-weight panels and their Stamina Usage-style source editors.
    private static int EconomyColumns(double width) => width >= 672.0 ? 2 : 1;

    // Full-width top-level sections may be wide; their editable cards stay compact.
    // Measure and render use the same 672-DIP single/two-column breakpoint.
    private static double EconomyCardWidth(double width, int columns)
    {
        const double gap = 12.0;
        var available = (Math.Max(1.0, width - 32.0) - gap * (columns - 1)) / columns;
        return Math.Max(1.0, Math.Min(available, columns == 2 ? 370.0 : 440.0));
    }

    private static double EconomyBoardHeight(double width, int count)
    {
        var rows = (count + EconomyColumns(width) - 1) / EconomyColumns(width);
        return 48.0 + rows * 94.0 + 12.0;
    }

    private static double CouponBoardHeight(double width)
        => width >= 390.0 ? 274.0 : 367.0;

    private static double ServicePriceBoardHeight(double width)
        => width >= 390.0 ? 242.0 : 438.0;

    private static string EconomyValue(ParameterViewModel parameter, double value)
        => parameter.Id switch
        {
            "economy.startingMoney" => value.ToString("N0", CultureInfo.InvariantCulture),
            "economy.repairCostBase" or "economy.traderMinBuyCondition" or "economy.itemValueFloor"
                or "environment.flycatcherMovementSpeed" or "environment.flycatcherTurnSpeed" => $"{value * 100.0:0.#}%",
            "environment.diamondBleeding" => $"{value:0.#} pts",
            _ => $"{value:0.##}x"
        };

    // Money Flow / compact native coupons counter + two independent reward meters.
    // This is a number editor, not a fake 0..100000 progress bar. The exact
    // source-native step remains 10 coupons (keyboard and wheel share it).
    private void DrawCouponFlow(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawHeader(dc, "COUPONS & REWARDS", "", text, muted, accent);
        var width = Math.Min(620.0, ActualWidth - 28.0);
        var left = (ActualWidth - width) / 2.0;
        DrawStartingCouponsRail(dc, P("economy.startingMoney"),
            new Rect(left, 49.0, width, 96.0), text, muted, border, accent, panel);
        DrawText(dc, "QUEST REWARDS", new Point(left + 8.0, 158.0),
            10.0, muted, FontWeights.Bold);

        var twoColumns = ActualWidth >= 390.0;
        const double gap = 10.0;
        var cardWidth = twoColumns ? (width - gap) / 2.0 : width;
        DrawTradingMeter(dc, P("economy.mainQuestReward"), "MAIN QUEST",
            new Rect(left, 175.0, cardWidth, 84.0), false,
            text, muted, border, accent, panel);
        DrawTradingMeter(dc, P("economy.sideQuestReward"), "SIDE QUEST",
            new Rect(twoColumns ? left + cardWidth + gap : left,
                twoColumns ? 175.0 : 268.0, cardWidth, 84.0), false,
            text, muted, border, accent, panel);
    }

    // A direct-position 0..100,000-coupon rail replaces tedious +/- clicks.
    // Value changes use the *native* 10-coupon step on drag and keyboard.
    private void DrawStartingCouponsRail(
        DrawingContext dc, ParameterViewModel? coupons, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .62),
            new Pen(border, .9), card, 4.0, 4.0);
        DrawText(dc, "STARTING COUPONS", new Point(card.Left + 12.0, card.Top + 8.0),
            10.2, text, FontWeights.Bold);
        if (coupons is null)
            return;

        var changed = coupons.IsControlEnabled && coupons.IsValueModifiedFromReference();
        DrawRightAlignedText(dc,
            coupons.CurrentNumber.ToString("N0", CultureInfo.InvariantCulture),
            new Point(card.Right - 13.0, card.Top + 5.0),
            changed ? 16.2 : 15.0,
            changed ? accent : coupons.IsControlEnabled ? text : muted,
            FontWeights.Bold);

        var track = new Rect(card.Left + 18.0, card.Top + 45.0,
            Math.Max(1.0, card.Width - 36.0), 12.0);
        var y = track.Top + track.Height / 2.0;
        var x = ValueToX(coupons.CurrentNumber, coupons.Minimum,
            coupons.Maximum, track, reverse: false);
        var referenceX = ValueToX(coupons.ReferenceNumber, coupons.Minimum,
            coupons.Maximum, track, reverse: false);
        dc.DrawRoundedRectangle(CloneWithOpacity(border, .32), null, track, 3, 3);
        dc.DrawLine(new Pen(CloneWithOpacity(coupons.IsControlEnabled ? accent : muted, .8), 4.0),
            new Point(track.Left, y), new Point(x, y));
        for (var index = 0; index <= 4; index++)
        {
            var tickValue = coupons.Minimum
                + index * (coupons.Maximum - coupons.Minimum) / 4.0;
            var tickX = ValueToX(tickValue, coupons.Minimum,
                coupons.Maximum, track, reverse: false);
            dc.DrawLine(new Pen(muted, .9),
                new Point(tickX, y - 5.0), new Point(tickX, y + 5.0));
            DrawCenteredText(dc, (tickValue / 1000.0).ToString("0", CultureInfo.InvariantCulture) + "K",
                new Point(tickX, card.Bottom - 16.0), 9.0, muted, FontWeights.SemiBold);
        }
        dc.DrawLine(new Pen(muted, 2.0), new Point(referenceX, y - 10.0),
            new Point(referenceX, y + 10.0));
        var hovered = _hoverTarget?.Kind == HitKind.Horizontal
            && ReferenceEquals(_hoverTarget.Parameter, coupons);
        ControlHoverGlow.Draw(dc, new Point(x, y), accent, hovered);
        dc.DrawEllipse(panel, new Pen(coupons.IsControlEnabled ? accent : muted,
            hovered ? 2.8 : 1.7), new Point(x, y), 6.5, 6.5);

        AddHorizontalTarget(coupons,
            new Rect(track.Left - 8.0, track.Top - 12.0,
                track.Width + 16.0, track.Height + 24.0),
            track, coupons.Minimum, coupons.Maximum);
    }

    // Three independent source knobs. In narrow columns their two-row
    // arrangement follows the same measured break point as GetDesiredHeight.
    private void DrawServicePriceKnobs(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawHeader(dc, "SERVICE PRICES", "", text, muted, accent);
        var channels = new (string Id, string Label)[]
        {
            ("economy.fastTravelCost", "FAST TRAVEL"),
            ("economy.upgradeCost", "UPGRADES"),
            ("economy.repairCostBase", "BASE REPAIR")
        };
        var columns = ActualWidth >= 390.0 ? 3 : 2;
        const double gap = 10.0;
        var bankWidth = Math.Min(606.0, ActualWidth - 30.0);
        var cellWidth = Math.Min(194.0, (bankWidth - (columns - 1) * gap) / columns);
        for (var i = 0; i < channels.Length; i++)
        {
            var column = i % columns;
            var row = i / columns;
            var lastRowCount = Math.Min(columns, channels.Length - row * columns);
            var rowWidth = lastRowCount * cellWidth + (lastRowCount - 1) * gap;
            var rowLeft = (ActualWidth - rowWidth) / 2.0;
            DrawSourceKnob(dc, P(channels[i].Id), channels[i].Label,
                new Rect(rowLeft + column * (cellWidth + gap),
                    (columns == 3 ? Math.Max(49.0, (ActualHeight - 181.0) / 2.0) : 49.0)
                        + row * 191.0, cellWidth, 181.0),
                text, muted, border, accent, panel,
                percentageReadout: channels[i].Id == "economy.repairCostBase");
        }
    }

    // Independent native stock multipliers: no suggestion of actual item slots.
    private void DrawTraderStockKnobs(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawHeader(dc, "TRADER STOCK", "", text, muted, accent);
        var channels = new (string Id, string Label)[]
        {
            ("economy.traderAmmoStock", "AMMO STOCK"),
            ("economy.medicStock", "MEDIC STOCK")
        };
        const double cellWidth = 194.0;
        const double gap = 12.0;
        const double top = 49.0;
        var left = (ActualWidth - (2 * cellWidth + gap)) / 2.0;
        for (var i = 0; i < channels.Length; i++)
            DrawSourceKnob(dc, P(channels[i].Id), channels[i].Label,
                new Rect(left + i * (cellWidth + gap), top, cellWidth, 181.0),
                text, muted, border, accent, panel);
    }

    private void DrawEconomyCard(
        DrawingContext dc, ParameterViewModel? parameter, string label, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        dc.DrawRoundedRectangle(panel, new Pen(border, 0.9), card, 4.0, 4.0);
        if (parameter is null)
        {
            dc.DrawText(CreateText(label, 10.0, text, FontWeights.Bold,
                    Math.Max(1.0, card.Width - 24.0)),
                new Point(card.Left + 12.0, card.Top + 9.0));
            DrawText(dc, "PARAMETER UNAVAILABLE",
                new Point(card.Left + 12.0, card.Top + 34.0),
                9.0, muted, FontWeights.Normal);
            return;
        }

        var modified = parameter.IsValueModifiedFromReference();
        var value = EconomyValue(parameter, parameter.CurrentNumber);
        var readout = CreateText(value, modified ? 12.2 : 11.0,
            modified ? accent : text, FontWeights.Bold);
        // Reserve the measured readout width, not an arbitrary 95-DIP lane.
        // At minimum window width titles cannot overwrite the live value.
        var labelWidth = Math.Max(1.0, card.Width - 36.0
            - readout.WidthIncludingTrailingWhitespace - 10.0);
        dc.DrawText(CreateText(label, 10.0, text, FontWeights.Bold, labelWidth),
            new Point(card.Left + 12.0, card.Top + 9.0));
        dc.DrawText(readout, new Point(
            card.Right - 12.0 - readout.WidthIncludingTrailingWhitespace,
            card.Top + 8.0));

        var track = new Rect(card.Left + 14.0, card.Top + 40.0,
            Math.Max(1.0, card.Width - 28.0), 12.0);
        var y = track.Top + track.Height / 2.0;
        var x = ValueToX(parameter.CurrentNumber, parameter.Minimum,
            parameter.Maximum, track, reverse: false);
        var referenceX = ValueToX(parameter.ReferenceNumber, parameter.Minimum,
            parameter.Maximum, track, reverse: false);

        dc.DrawLine(new Pen(border, 3.0),
            new Point(track.Left, y), new Point(track.Right, y));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.70), 3.0),
            new Point(track.Left, y), new Point(x, y));
        // Repair reputation has a meaningful separate 1x "no modifier"
        // reference; it is NOT the selected-difficulty Base marker.
        if (parameter.Id is "economy.repairFriend" or "economy.repairNeutral"
            or "economy.repairDisaffection" or "economy.repairEnemy")
        {
            var unmodifiedX = ValueToX(1.0, parameter.Minimum,
                parameter.Maximum, track, reverse: false);
            if (Math.Abs(unmodifiedX - referenceX) >= 2.0)
                dc.DrawLine(new Pen(CloneWithOpacity(muted, 0.55), 1.0)
                    { DashStyle = DashStyles.Dash },
                    new Point(unmodifiedX, y - 8.0), new Point(unmodifiedX, y + 8.0));
        }
        dc.DrawLine(new Pen(muted, 1.8),
            new Point(referenceX, y - 8.0), new Point(referenceX, y + 8.0));

        var hovered = ReferenceEquals(_hoverTarget?.Parameter, parameter);
        ControlHoverGlow.Draw(dc, new Point(x, y), accent, hovered);

        dc.DrawEllipse(panel, new Pen(accent,
            hovered || IsSelected(parameter) ? 2.5 : 1.5),
            new Point(x, y), 6.0, 6.0);
        dc.DrawEllipse(accent, null, new Point(x, y), 2.5, 2.5);

        AddHorizontalTarget(parameter,
            new Rect(track.Left - 6.0, track.Top - 6.0,
                track.Width + 12.0, track.Height + 12.0),
            track, parameter.Minimum, parameter.Maximum);

        DrawText(dc, EconomyValue(parameter, parameter.Minimum),
            new Point(track.Left, card.Top + 60.0),
            9.0, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, EconomyValue(parameter, parameter.Maximum),
            new Point(track.Right, card.Top + 60.0),
            9.0, muted, FontWeights.Normal);
    }

    // Item Weight is two independent compact source cards, not vertical mixer
    // strips. The current value is the native item-mass multiplier in both.
    private void DrawEquipmentWeightBoard(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawCenteredText(dc, "ITEM WEIGHT",
            new Point(ActualWidth / 2.0, 21.0), 12.5, accent, FontWeights.Bold);

        var controls = new (string Id, string Label)[]
        {
            ("equipment.ammoWeight", "AMMO WEIGHT"),
            ("equipment.consumableWeight", "FOOD & MEDICINE")
        };
        var columns = EconomyColumns(ActualWidth);
        var cardWidth = EconomyCardWidth(ActualWidth, columns);
        const double gap = 12.0;
        var bankWidth = columns * cardWidth + (columns - 1) * gap;
        var left = (ActualWidth - bankWidth) / 2.0;

        for (var i = 0; i < controls.Length; i++)
        {
            var rect = new Rect(left + (i % columns) * (cardWidth + gap),
                48.0 + (i / columns) * 94.0, cardWidth, 80.0);
            DrawEconomyCard(dc, P(controls[i].Id), controls[i].Label,
                rect, text, muted, border, accent, panel);
        }
    }

    private static readonly (string Id, string Top, string Bottom)[] BoxYieldMixerChannels =
    {
        ("loot.destructibleAmmoQuantity", "AMMO", "YIELD"),
        ("loot.destructibleGrenadeQuantity", "GRENADE", "YIELD")
    };

    private static readonly (string Id, string Top, string Bottom)[] StashRewardMixerChannels =
    {
        ("loot.cheapStashGrenadeChance", "BASIC", "RGD-5 %"),
        ("loot.cheapStashGrenadeQuantity", "RGD-5", "AMOUNT"),
        ("loot.expensiveStashMoneyChance", "RARE", "COUPON %")
    };

    private static (string Id, string Top, string Bottom)[] GetMixerChannels(string family)
        => family switch
        {
            "BreakableBoxYields" => BoxYieldMixerChannels,
            "StashRewardProfile" => StashRewardMixerChannels,
            _ => Array.Empty<(string Id, string Top, string Bottom)>()
        };

    private const double MixerSidePadding = 16.0;
    private const double MixerStripGap = 10.0;
    private const double MixerStripTop = 56.0;
    private const double MixerStripHeight = 280.0;
    private const double MixerRowGap = 12.0;
    private const double MixerFooterHeight = 30.0;

    private static int GetMixerColumns(double boardWidth, int channelCount)
    {
        var inner = Math.Max(0.0, boardWidth - 2.0 * MixerSidePadding);
        var columns = inner >= 852.0 ? 6
            : inner >= 596.0 ? 4
            : inner >= 420.0 ? 3
            : inner >= 272.0 ? 2 : 1;
        columns = Math.Min(columns, channelCount);

        // A threshold is only advisory; never squeeze a strip below 128 DIPs.
        while (columns > 1
            && (inner - (columns - 1) * MixerStripGap) / columns < 128.0)
        {
            columns--;
        }
        return Math.Max(1, columns);
    }

    private static double GetMixerHeight(double boardWidth, int channelCount)
    {
        var columns = GetMixerColumns(boardWidth, channelCount);
        var rows = (channelCount + columns - 1) / columns;
        return MixerStripTop + rows * MixerStripHeight
            + (rows - 1) * MixerRowGap + MixerFooterHeight;
    }

    private void DrawNumericMixerBoard(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var channels = GetMixerChannels(Family);
        var title = Family switch
        {
            "BreakableBoxYields" => "BREAKABLE BOX YIELDS / MIXER",
            "StashRewardProfile" => "STASH REWARD ROLLS / MIXER",
            _ => "ITEM WEIGHT / MIXER"
        };
        var description = Family switch
        {
            "BreakableBoxYields" => "contents per destroyed box, not box spawn frequency",
            "StashRewardProfile" => "Basic RGD-5 chance/amount and eligible Rare coupon chance are separate",
            _ => "independent ammunition and consumable item mass"
        };
        DrawMixerBoard(dc, channels, title, description, text, muted, border, accent, panel);
    }

    private void DrawMixerBoard(
        DrawingContext dc,
        (string Id, string Top, string Bottom)[] channels,
        string title,
        string subtitle,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, title, subtitle, text, muted, accent);
        var count = channels.Length;
        var inner = Math.Max(1.0, ActualWidth - 2.0 * MixerSidePadding);
        var columns = GetMixerColumns(ActualWidth, count);
        var width = Math.Min(148.0,
            Math.Max(128.0, (inner - (columns - 1) * MixerStripGap) / columns));
        // Center compact fader banks inside wider boards instead of leaving a
        // large, visually unbalanced empty area to the right. The column
        // calculation and source order remain unchanged; the final short row
        // is independently centered without stretching its channels.
        for (var index = 0; index < count; index++)
        {
            var channel = channels[index];
            var row = index / columns;
            var column = index % columns;
            var rowCount = Math.Min(columns, count - row * columns);
            var rowWidth = rowCount * width + (rowCount - 1) * MixerStripGap;
            var rowLeft = Math.Max(MixerSidePadding, (ActualWidth - rowWidth) / 2.0);
            var rect = new Rect(
                rowLeft + column * (width + MixerStripGap),
                MixerStripTop + row * (MixerStripHeight + MixerRowGap),
                width, MixerStripHeight);
            DrawMixerStrip(dc, P(channel.Id), channel.Top, channel.Bottom, rect,
                text, muted, border, accent, panel);
        }
    }

    private void DrawAnomalyGlobalDamage(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawCenteredText(dc, "GLOBAL ANOMALY DAMAGE",
            new Point(ActualWidth / 2.0, 21.0), 12.5, accent, FontWeights.Bold);
        // Reuse the native-source rotary control from Your Weapons, without
        // changing EnvironmentDifficulty.Anomaly_Damage or the selected baseline.
        // This single knob stays centered and bounded within its full-width group.
        const double knobCardWidth = 194.0;
        DrawSourceKnob(dc, P("environment.anomalyDamage"),
            "OVERALL DAMAGE MULTIPLIER",
            new Rect((ActualWidth - knobCardWidth) / 2.0, 49.0,
                knobCardWidth, 181.0), text, muted, border, accent, panel);
    }

    // This native-axis graph uses exactly the same triple-circle grammar as
    // Human Combat and Equipment Wear: X movement, Y turn, center both.
    // Source values are retained movement/turn speed (0–1), not negative CFG
    // deltas. The existing writer converts each to UI−1 for the two effects.
    private static double FlycatcherPlotSide(double width)
        => Math.Min(210.0, Math.Max(170.0, width * .31));

    private static double FlycatcherEffectsWidth(double width)
        => Math.Min(440.0, Math.Max(248.0, width * .46));

    private static double FlycatcherBoardHeight(double width)
        => Math.Max(348.0, 86.0 + FlycatcherPlotSide(width) + 54.0);

    private void DrawFlycatcherSlowdown(
        DrawingContext dc, Brush text, Brush muted, Brush border, Brush soft,
        Brush accent, Brush panel)
    {
        DrawCenteredText(dc, "FLYCATCHER SLOWDOWN",
            new Point((ActualWidth - FlycatcherEffectsWidth(ActualWidth) - 24.0) / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);

        var movement = P("environment.flycatcherMovementSpeed");
        var turning = P("environment.flycatcherTurnSpeed");
        if (movement is null || turning is null)
        {
            DrawCenteredText(dc, "Flycatcher parameters unavailable.",
                new Point(ActualWidth / 2.0, 160.0), 10.0, muted,
                FontWeights.SemiBold);
            return;
        }

        var side = FlycatcherPlotSide(ActualWidth);
        var effectsWidth = FlycatcherEffectsWidth(ActualWidth);
        var leftAreaWidth = ActualWidth - effectsWidth - 24.0;
        var plot = new Rect((leftAreaWidth - side) / 2.0 + 15.0, 86.0, side, side);
        const double axisGap = 9.0;

        // Axis-aligned label/live-value pair, matching Combat. Only modified
        // numbers and their percentage units turn gold.
        var xCaption = CreateText("MOVEMENT REMAINING", 10.5, text, FontWeights.Bold);
        var xChanged = movement.IsValueModifiedFromReference();
        var xValue = CreateText($"{movement.CurrentNumber * 100.0:0.#}%",
            xChanged ? 14.2 : 13.0, xChanged ? accent : text, FontWeights.Bold);
        var xStart = plot.Left + plot.Width / 2.0 -
            (xCaption.WidthIncludingTrailingWhitespace + axisGap +
             xValue.WidthIncludingTrailingWhitespace) / 2.0;
        dc.DrawText(xCaption, new Point(xStart, plot.Top - 32.0));
        dc.DrawText(xValue, new Point(
            xStart + xCaption.WidthIncludingTrailingWhitespace + axisGap,
            plot.Top - 34.0));

        var yCenter = plot.Top + plot.Height / 2.0;
        var yCaption = CreateText("TURN SPEED REMAINING", 10.5, text, FontWeights.Bold);
        var yChanged = turning.IsValueModifiedFromReference();
        var yValue = CreateText($"{turning.CurrentNumber * 100.0:0.#}%",
            yChanged ? 14.2 : 13.0, yChanged ? accent : text, FontWeights.Bold);
        var yAnchor = plot.Left - 46.0;
        var yStart = yAnchor -
            (yCaption.WidthIncludingTrailingWhitespace + axisGap +
             yValue.WidthIncludingTrailingWhitespace) / 2.0;
        dc.PushTransform(new RotateTransform(-90.0, yAnchor, yCenter));
        dc.DrawText(yCaption, new Point(yStart, yCenter - 7.0));
        dc.DrawText(yValue, new Point(
            yStart + yCaption.WidthIncludingTrailingWhitespace + axisGap,
            yCenter - 9.0));
        dc.Pop();

        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), plot, 4.0, 4.0);
        for (var i = 0; i <= 4; i++)
        {
            var xTick = movement.Minimum + (movement.Maximum - movement.Minimum) * i / 4.0;
            var yTick = turning.Minimum + (turning.Maximum - turning.Minimum) * i / 4.0;
            var gridX = ValueToX(xTick, movement.Minimum, movement.Maximum, plot, false);
            var gridY = ValueToY(yTick, turning.Minimum, turning.Maximum, plot);
            var line = new Pen(CloneWithOpacity(soft, 0.5), 1.0)
                { DashStyle = DashStyles.Dot };
            dc.DrawLine(line, new Point(gridX, plot.Top), new Point(gridX, plot.Bottom));
            dc.DrawLine(line, new Point(plot.Left, gridY), new Point(plot.Right, gridY));
            if (i is 0 or 2 or 4)
            {
                DrawCenteredText(dc, $"{xTick * 100.0:0.#}%",
                    new Point(gridX, plot.Bottom + 32.0), 9.3, text,
                    FontWeights.Normal);
                DrawRightAlignedText(dc, $"{yTick * 100.0:0.#}%",
                    new Point(plot.Left - 12.0, gridY - 7.0), 9.3, text,
                    FontWeights.Normal);
            }
        }

        var baseX = ValueToX(movement.ReferenceNumber,
            movement.Minimum, movement.Maximum, plot, false);
        var baseY = ValueToY(turning.ReferenceNumber,
            turning.Minimum, turning.Maximum, plot);
        var currentX = ValueToX(movement.CurrentNumber,
            movement.Minimum, movement.Maximum, plot, false);
        var currentY = ValueToY(turning.CurrentNumber,
            turning.Minimum, turning.Maximum, plot);

        var basePen = new Pen(CloneWithOpacity(muted, 0.65), 1.2)
            { DashStyle = DashStyles.Dash };
        dc.DrawLine(basePen, new Point(baseX, plot.Top), new Point(baseX, plot.Bottom));
        dc.DrawLine(basePen, new Point(plot.Left, baseY), new Point(plot.Right, baseY));
        dc.DrawEllipse(panel, new Pen(muted, 1.7),
            new Point(baseX, baseY), 6.0, 6.0);

        var currentPen = new Pen(CloneWithOpacity(accent, 0.65), 1.2)
            { DashStyle = DashStyles.Dot };
        dc.DrawLine(currentPen, new Point(currentX, plot.Top),
            new Point(currentX, plot.Bottom));
        dc.DrawLine(currentPen, new Point(plot.Left, currentY),
            new Point(plot.Right, currentY));

        DrawHumanAxisHandle(dc, new Point(currentX, currentY), accent, panel,
            _hoverTarget?.Kind == HitKind.HumanJoint,
            _selectedHitKind == HitKind.HumanJoint);

        var xHandle = new Point(currentX, plot.Bottom + 10.0);
        DrawHumanAxisHandle(dc, xHandle, accent, panel,
            _hoverTarget?.Kind == HitKind.Horizontal &&
                ReferenceEquals(_hoverTarget.Parameter, movement),
            IsSelected(movement) && _selectedHitKind == HitKind.Horizontal);
        AddHorizontalTarget(movement,
            new Rect(currentX - 17.0, plot.Bottom + 1.0, 34.0, 22.0),
            plot, movement.Minimum, movement.Maximum);

        var yHandle = new Point(plot.Left - 11.0, currentY);
        DrawHumanAxisHandle(dc, yHandle, accent, panel,
            _hoverTarget?.Kind == HitKind.Vertical &&
                ReferenceEquals(_hoverTarget.Parameter, turning),
            IsSelected(turning) && _selectedHitKind == HitKind.Vertical);
        AddVerticalTarget(turning,
            new Rect(plot.Left - 28.0, currentY - 14.0, 27.0, 28.0),
            plot, turning.Minimum, turning.Maximum);

        if (movement.IsControlEnabled && turning.IsControlEnabled)
            _hitTargets.Add(new HitTarget(movement,
                new Rect(currentX - 18.0, currentY - 18.0, 36.0, 36.0),
                plot, HitKind.HumanJoint, movement.Minimum, movement.Maximum, false));

        // One unified native-source panel: grouped special effects to the right.
        DrawFlycatcherEffects(dc, text, muted, border, accent, panel,
            new Rect(ActualWidth - effectsWidth - 12.0, 42.0, effectsWidth, 277.0));
    }

    private void DrawFlycatcherEffects(DrawingContext dc, Brush text, Brush muted,
        Brush border, Brush accent, Brush panel, Rect area)
    {
        DrawCenteredText(dc, "SPECIAL ANOMALY EFFECTS",
            new Point(area.Left + area.Width / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);
        DrawCenteredText(dc, "BLEEDING",
            new Point(area.Left + area.Width / 2.0, area.Top + 16.0),
            10.0, muted, FontWeights.Bold);
        const double gap = 9.0;
        var half = (area.Width - gap) / 2.0;
        DrawEconomyCard(dc, P("environment.razorBleeding"), "RAZOR",
            new Rect(area.Left, area.Top + 34.0, half, 86.0),
            text, muted, border, accent, panel);
        DrawEconomyCard(dc, P("environment.diamondBleeding"), "DIAMOND",
            new Rect(area.Left + half + gap, area.Top + 34.0, half, 86.0),
            text, muted, border, accent, panel);
        DrawCenteredText(dc, "PSY DRAIN",
            new Point(area.Left + area.Width / 2.0, area.Top + 150.0),
            10.0, muted, FontWeights.Bold);
        DrawEconomyCard(dc, P("environment.psyAnomalyIntensity"), "INTENSITY",
            new Rect(area.Left, area.Top + 168.0, area.Width, 86.0),
            text, muted, border, accent, panel);
    }

    private void DrawAnomalySpecialEffects(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawCenteredText(dc, "SPECIAL ANOMALY EFFECTS",
            new Point(ActualWidth / 2.0, 21.0), 12.5, accent, FontWeights.Bold);

        var sources = new (string Id, string Label)[]
        {
            ("environment.razorBleeding", "RAZOR BLEEDING"),
            ("environment.diamondBleeding", "DIAMOND BLEEDING"),
            ("environment.psyAnomalyIntensity", "PSY DRAIN INTENSITY")
        };
        var columns = EconomyColumns(ActualWidth);
        var width = EconomyCardWidth(ActualWidth, columns);
        const double gap = 12.0;
        for (var i = 0; i < sources.Length; i++)
        {
            var count = Math.Min(columns, sources.Length - (i / columns) * columns);
            var rowWidth = count * width + (count - 1) * gap;
            var rowLeft = (ActualWidth - rowWidth) / 2.0;
            var card = new Rect(rowLeft + (i % columns) * (width + gap),
                48.0 + (i / columns) * 94.0, width, 80.0);
            DrawEconomyCard(dc, P(sources[i].Id), sources[i].Label,
                card, text, muted, border, accent, panel);
        }
    }

    private void DrawMixerStrip(
        DrawingContext dc,
        ParameterViewModel? parameter,
        string topLabel,
        string bottomLabel,
        Rect strip,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel,
        bool frameless = false)
    {
        var selected = IsSelected(parameter);
        var enabled = parameter?.IsControlEnabled == true;
        var foreground = enabled ? text : muted;
        var valueBrush = !enabled ? muted :
            parameter?.IsValueModifiedFromReference() == true ? accent : text;

        // All mixer strips use handle-only hover; never draw rectangular selection frames.
        if (parameter?.IsValueModifiedFromReference() == true)
        {
            dc.DrawLine(new Pen(accent, 2.5),
                new Point(strip.Left + 3.0, strip.Top + 9.0),
                new Point(strip.Left + 3.0, strip.Bottom - 9.0));
        }

        DrawMixerCentered(dc, topLabel,
            new Point(strip.Left + strip.Width / 2.0, strip.Top + 8.0),
            strip.Width - 13.0, 10.7, foreground, FontWeights.Bold);
        DrawMixerCentered(dc, bottomLabel,
            new Point(strip.Left + strip.Width / 2.0, strip.Top + 22.0),
            strip.Width - 13.0, 10.7, foreground, FontWeights.Bold);

        if (parameter is null)
        {
            DrawMixerCentered(dc, "CFG UNAVAILABLE",
                new Point(strip.Left + strip.Width / 2.0, strip.Top + 58.0),
                strip.Width - 13.0, 10.0, muted, FontWeights.SemiBold);
            return;
        }

        var center = strip.Left + strip.Width / 2.0;
        DrawMixerCentered(dc, MixerValue(parameter, parameter.CurrentNumber),
            new Point(center, strip.Top + 38.0),
            strip.Width - 14.0, parameter.IsValueModifiedFromReference() ? 17.4 : 16.2,
            valueBrush, FontWeights.Bold);
        // The grey reference tick conveys Base without redundant "BASE 1x" text.
        // Remove the text across all mixer families, preserving the reference marker.
        DrawMixerCentered(dc, MixerValue(parameter, parameter.Maximum),
            new Point(center, strip.Top + 78.0),
            strip.Width - 14.0, frameless ? 13.0 : 11.3, muted, FontWeights.Normal);

        // Vertical source scale: top is real maximum, bottom real minimum.
        // The strip layout normalizes only screen position, never the stored CFG.
        var rail = new Rect(center - 3.0, strip.Top + 100.0, 6.0, 150.0);
        var currentY = ValueToY(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, rail);
        var baseY = ValueToY(parameter.ReferenceNumber,
            parameter.Minimum, parameter.Maximum, rail);

        dc.DrawLine(new Pen(border, 5.0),
            new Point(center, rail.Top), new Point(center, rail.Bottom));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, enabled ? 0.75 : 0.3), 5.0),
            new Point(center, rail.Bottom), new Point(center, currentY));

        // Base marker lives next to the rail to avoid stealing the live
        // handle's drag area. Its clickable rectangle is registered last.
        dc.DrawLine(new Pen(muted, 2.2),
            new Point(center + 13.0, baseY),
            new Point(center + 29.0, baseY));

        dc.DrawEllipse(panel,
            new Pen(accent, selected ? 2.4 : 1.7),
            new Point(center, currentY), 9.0, 9.0);
        dc.DrawLine(new Pen(accent, 2.2),
            new Point(center - 7.0, currentY),
            new Point(center + 7.0, currentY));

        DrawMixerCentered(dc, MixerValue(parameter, parameter.Minimum),
            new Point(center, strip.Top + 260.0),
            strip.Width - 14.0, frameless ? 13.0 : 9.5, muted, FontWeights.Normal);

        // Clicking the label/value area selects this source for the rationale,
        // but it must never edit the value; the active rail/marker is above it
        // in reverse-order hit testing.
        _hitTargets.Add(new HitTarget(parameter, strip, strip,
            HitKind.MixerSelect, parameter.Minimum, parameter.Maximum, false));

        if (!enabled)
            return;

        // Drag/click owns only this exact source value. Existing Snap() and
        // centralized constraints determine the actual stored numeric step.
        AddVerticalTarget(parameter,
            new Rect(center - 15.0, rail.Top - 7.0, 30.0, rail.Height + 14.0),
            rail, parameter.Minimum, parameter.Maximum);
        _hitTargets.Add(new HitTarget(parameter,
            new Rect(center + 16.0, baseY - 8.0, 20.0, 16.0),
            rail, HitKind.MixerBaseMarker, parameter.Minimum,
            parameter.Maximum, false));
    }

    private static string MixerValue(ParameterViewModel parameter, double raw)
        => parameter.Id switch
        {
            "environment.diamondBleeding" => $"{raw:0.#} pts",
            "economy.repairCostBase" => $"{raw * 100.0:0.#}%",
            "loot.cheapStashGrenadeChance" or "loot.expensiveStashMoneyChance" => $"{raw * 100.0:0.#}%",
            "loot.cheapStashGrenadeQuantity" => $"{raw:0} pcs",
            _ => $"{raw:0.##}x"
        };

    private void DrawMixerCentered(
        DrawingContext dc,
        string label,
        Point centerTop,
        double maxWidth,
        double size,
        Brush color,
        FontWeight weight)
    {
        var formatted = CreateText(label, size, color, weight, maxWidth);
        dc.DrawText(formatted, new Point(
            centerTop.X - formatted.WidthIncludingTrailingWhitespace / 2.0,
            centerTop.Y));
    }

    // The smart-loot extra package is a categorical CFG list, not a multiplier.
    // Ammo/medical controls tune other independent generator surfaces. The actual
    // MIN/MAX weapon condition is an absolute range; its upper band is read-only.
    // One categorical corpse bonus, two native quantities, and one real
    // MIN/MAX condition range. The two vertically separated endpoint handles
    // are independently hit-testable even when both numbers coincide.
    private void DrawNpcLootLoadout(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        var corpse = P("loot.npcCorpseBonus");
        var medical = P("loot.npcMedicalChance");
        var ammo = P("loot.npcWeaponAmmoSupply");
        var condition = P("loot.droppedWeaponCondition");
        DrawHeader(dc, "NPC LOOT & EQUIPMENT", "", text, muted, accent);
        if (corpse is null || medical is null || ammo is null || condition is null
            || corpse.ChoiceOptions.Count == 0)
        {
            DrawCenteredText(dc, "NPC loot data unavailable.",
                new Point(ActualWidth / 2.0, 135.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        const double margin = 17.0;
        const double gap = 10.0;
        var width = Math.Max(1.0, ActualWidth - 2.0 * margin);
        var choiceCard = new Rect(margin, 54.0, width, 90.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0),
            choiceCard, 4.0, 4.0);
        DrawText(dc, "EXTRA CORPSE LOOT", new Point(margin + 11.0, 61.0),
            10.6, text, FontWeights.Bold);
        var count = corpse.ChoiceOptions.Count;
        var cell = Math.Min(180.0, (width - 22.0 - (count - 1) * gap) / count);
        var groupWidth = count * cell + (count - 1) * gap;
        var start = margin + (width - groupWidth) / 2.0;
        for (var i = 0; i < count; i++)
        {
            var rect = new Rect(start + i * (cell + gap), 88.0, cell, 43.0);
            var selected = Math.Abs(corpse.ChoiceIndex - i) < 0.5;
            var hover = _hoverTarget?.Kind == HitKind.Choice
                && Math.Abs(_hoverTarget.VisualMinimum - i) < Epsilon;
            dc.DrawRoundedRectangle(
                selected ? CloneWithOpacity(accent, 0.10) : panel,
                new Pen(selected ? accent : hover ? CloneWithOpacity(accent, 0.7) : border,
                    selected ? 1.6 : 0.9), rect, 4.0, 4.0);
            var caption = CreateText(corpse.ChoiceOptions[i].Label, 10.6,
                selected ? accent : text, FontWeights.SemiBold,
                Math.Max(1.0, cell - 12.0));
            dc.DrawText(caption, new Point(
                rect.Left + (rect.Width - caption.WidthIncludingTrailingWhitespace) / 2.0,
                rect.Top + (rect.Height - caption.Height) / 2.0));
            if (corpse.IsControlEnabled)
                _hitTargets.Add(new HitTarget(corpse, rect, rect, HitKind.Choice, i, i, false));
        }

        var half = (width - gap) / 2.0;
        DrawHorizontalTrackCard(dc, medical, "MEDICAL DROPS",
            new Rect(margin, 155.0, half, 72.0),
            text, muted, border, accent, panel, pairedValueFormatting: true);
        DrawHorizontalTrackCard(dc, ammo, "NPC AMMO",
            new Rect(margin + half + gap, 155.0, half, 72.0),
            text, muted, border, accent, panel, pairedValueFormatting: true);

        // One source-backed example per NPC_Ammo_Smart caliber group, with
        // the first rank containing that caliber shown explicitly. This is
        // a sample count, not a guaranteed corpse drop or a combined rank pool.
        var examples = new (string Label, string Rank, int Min, int Max)[]
        {
            ("12 GA", "NEWBIE", 2, 6),
            ("5.45", "NEWBIE", 15, 20),
            ("9×19", "NEWBIE", 15, 20),
            (".45 ACP", "NEWBIE", 8, 12),
            ("5.56", "EXPERIENCED", 15, 20),
            ("9×18", "VETERAN", 15, 20),
            ("7.62 SNIPER", "VETERAN", 25, 30),
            ("7.62", "VETERAN", 25, 30),
            ("9×39", "MASTER", 15, 20)
        };
        var ammoPackageActive = corpse.CurrentText.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains("NPC_Ammo_Smart", StringComparer.Ordinal);
        var countsModified = ammo.IsValueModifiedFromReference();
        var sampleCard = new Rect(margin, 238.0, width, 172.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 0.9),
            sampleCard, 4.0, 4.0);
        DrawText(dc, "NPC AMMO PREVIEW",
            new Point(sampleCard.Left + 11.0, sampleCard.Top + 7.0),
            10.3, ammoPackageActive ? text : muted, FontWeights.Bold);

        const double previewGap = 6.0;
        const double previewInset = 11.0;
        var previewWidth = (sampleCard.Width - 2.0 * previewInset
            - 2.0 * previewGap) / 3.0;
        for (var i = 0; i < examples.Length; i++)
        {
            var sample = examples[i];
            var tile = new Rect(sampleCard.Left + previewInset
                    + (i % 3) * (previewWidth + previewGap),
                sampleCard.Top + 25.0 + (i / 3) * 45.0, previewWidth, 40.0);
            dc.DrawRoundedRectangle(
                ammoPackageActive ? CloneWithOpacity(panel, 0.95)
                    : CloneWithOpacity(panel, 0.55),
                new Pen(CloneWithOpacity(border, ammoPackageActive ? 0.85 : 0.5), 0.9),
                tile, 4.0, 4.0);
            var low = Math.Max(0, (int)Math.Floor(sample.Min * ammo.CurrentNumber + 0.5));
            var high = Math.Max(0, (int)Math.Floor(sample.Max * ammo.CurrentNumber + 0.5));
            DrawCenteredText(dc, sample.Label + " · " + sample.Rank,
                new Point(tile.Left + tile.Width / 2.0, tile.Top + 3.0),
                9.0, muted, FontWeights.SemiBold);
            DrawCenteredText(dc, $"{low}–{high}",
                new Point(tile.Left + tile.Width / 2.0, tile.Top + 18.0),
                countsModified ? 14.2 : 13.0,
                countsModified ? accent : ammoPackageActive ? text : muted,
                FontWeights.SemiBold);
        }

        if (!ammoPackageActive)
        {
            DrawText(dc, "Inactive until Medicine + Ammo is selected",
                new Point(sampleCard.Left + 12.0, sampleCard.Bottom - 11.0),
                9.5, muted, FontWeights.Normal);
        }

        // This is a dual-ended native RANGE, not two normal slider
        // headers. Show the two editable figures together in the middle
        // of the card, immediately above their shared physical rail.
        var rangeCard = new Rect(margin, 420.0, width, 130.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0),
            rangeCard, 4.0, 4.0);
        DrawText(dc, "DROPPED WEAPON CONDITION",
            new Point(rangeCard.Left + 11.0, rangeCard.Top + 8.0),
            10.8, text, FontWeights.Bold);

        var minModified = Math.Abs(condition.CurrentMinNumber - 0.25) > Epsilon;
        var maxModified = Math.Abs(condition.CurrentMaxNumber - 0.50) > Epsilon;
        var center = rangeCard.Left + rangeCard.Width / 2.0;
        var minReadoutX = center - 73.0;
        var maxReadoutX = center + 73.0;

        // Fixed central readouts never collide, even at MIN == MAX,
        // unlike labels following handles to the same X coordinate.
        DrawCenteredText(dc, "MIN",
            new Point(minReadoutX, rangeCard.Top + 27.0),
            9.6, muted, FontWeights.Bold);
        DrawCenteredText(dc, "MAX",
            new Point(maxReadoutX, rangeCard.Top + 27.0),
            9.6, muted, FontWeights.Bold);
        DrawCenteredText(dc, $"{condition.CurrentMinNumber * 100.0:0}%",
            new Point(minReadoutX, rangeCard.Top + 43.0),
            minModified ? 14.2 : 13.0,
            minModified ? accent : text, FontWeights.SemiBold);
        DrawCenteredText(dc, $"{condition.CurrentMaxNumber * 100.0:0}%",
            new Point(maxReadoutX, rangeCard.Top + 43.0),
            maxModified ? 14.2 : 13.0,
            maxModified ? accent : text, FontWeights.SemiBold);
        dc.DrawLine(new Pen(CloneWithOpacity(border, 0.60), 0.8),
            new Point(center, rangeCard.Top + 28.0),
            new Point(center, rangeCard.Top + 61.0));

        var rail = new Rect(rangeCard.Left + 20.0, rangeCard.Top + 81.0,
            Math.Max(1.0, rangeCard.Width - 40.0), 12.0);
        var y = rail.Top + rail.Height / 2.0;
        var xMin = ValueToX(condition.CurrentMinNumber, 0.0, 1.0, rail, false);
        var xMax = ValueToX(condition.CurrentMaxNumber, 0.0, 1.0, rail, false);

        var backPen = new Pen(CloneWithOpacity(border, 0.85), 3.5)
        {
            StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round
        };
        dc.DrawLine(backPen, new Point(rail.Left, y),
            new Point(rail.Right, y));

        // The common 25-50% authored baseline is a faint SOLID reference
        // segment with tiny endpoint ticks (not conspicuous dashed blocks).
        // The isolated 35-45% vanilla weapon-row outlier remains untouched
        // until the condition is actually edited.
        var ghostMin = ValueToX(0.25, 0.0, 1.0, rail, false);
        var ghostMax = ValueToX(0.50, 0.0, 1.0, rail, false);
        dc.DrawLine(new Pen(CloneWithOpacity(muted, 0.32), 3.0),
            new Point(ghostMin, y), new Point(ghostMax, y));
        var ghostTick = new Pen(CloneWithOpacity(muted, 0.50), 1.0);
        dc.DrawLine(ghostTick, new Point(ghostMin, y - 6.0),
            new Point(ghostMin, y + 6.0));
        dc.DrawLine(ghostTick, new Point(ghostMax, y - 6.0),
            new Point(ghostMax, y + 6.0));

        // The active selected condition band is always the only prominent
        // colored length, with both endpoints ON its single centerline.
        var activePen = new Pen(accent, 4.5)
        {
            StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round
        };
        dc.DrawLine(activePen, new Point(xMin, y), new Point(xMax, y));

        // At nearly coincident X coordinates, briefly stagger the two
        // SAME-looking handles so each remains visible/clickable. Otherwise
        // both lie directly on the rail; no permanent odd vertical offset.
        var overlapping = Math.Abs(xMax - xMin) < 16.0;
        var handleSeparation = overlapping ? 7.0 : 0.0;
        var minPoint = new Point(xMin, y - handleSeparation);
        var maxPoint = new Point(xMax, y + handleSeparation);
        if (overlapping)
        {
            var stem = new Pen(CloneWithOpacity(accent, 0.80), 1.5);
            dc.DrawLine(stem, minPoint, new Point(xMin, y));
            dc.DrawLine(stem, maxPoint, new Point(xMax, y));
        }
        foreach (var point in new[] { minPoint, maxPoint })
        {
            dc.DrawEllipse(panel, new Pen(accent, 1.8), point, 6.5, 6.5);
            dc.DrawEllipse(accent, null, point, 2.3, 2.3);
        }

        // Preserve full-length, 38-DIP tall physical track hit areas for
        // BOTH targets. FindTarget chooses by nearest X, then by actual
        // two-dimensional handle position when X coordinates overlap.
        var fullHit = new Rect(rail.Left - 14.0, y - 19.0,
            rail.Width + 28.0, 38.0);
        AddRangeTarget(condition, fullHit, rail, HitKind.RangeMin);
        AddRangeTarget(condition, fullHit, rail, HitKind.RangeMax);

        DrawText(dc, "0%", new Point(rail.Left, rangeCard.Bottom - 19.0),
            9.5, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, "100%",
            new Point(rail.Right, rangeCard.Bottom - 19.0),
            9.5, muted, FontWeights.Normal);
    }

    // World-loot placement operations are four separate booleans. The source
    // density value belongs to the weapon-reshuffle operation only; it is not
    // a universal percentage of weapons removed from the base game world.
    // World placement toggles are the universal minimal checkbox chrome;
    // weapon density is a dependent source and stays saved when shuffle is off.
    private void DrawWorldLootLayout(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        var uniqueWeapons = P("loot.looseUniqueWeaponShuffle");
        var weapons = P("loot.worldWeaponShuffle");
        var density = P("loot.worldWeaponDensity");
        var attachments = P("loot.worldAttachmentShuffle");
        var detectors = P("loot.worldDetectors");
        var blueprints = P("loot.blueprintShuffle");
        DrawHeader(dc, "WORLD ITEM SPAWNS", "", text, muted, accent);
        if (uniqueWeapons is null || weapons is null || density is null || attachments is null
            || detectors is null || blueprints is null)
        {
            DrawCenteredText(dc, "World placement data unavailable.",
                new Point(ActualWidth / 2.0, 125.0),
                10.0, muted, FontWeights.Bold);
            return;
        }
        const double margin = 17.0;
        const double gap = 10.0;
        var width = Math.Max(1.0, ActualWidth - 2.0 * margin);
        var cell = (width - gap) / 2.0;
        var left = new Rect(margin, 55.0, cell, 207.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 0.9),
            left, 4.0, 4.0);
        DrawTogglePill(dc, weapons, "SHUFFLE WEAPONS",
            new Rect(left.Left + 8.0, left.Top + 10.0,
                left.Width - 16.0, 44.0),
            text, muted, border, accent, panel);
        DrawHorizontalTrackCard(dc, density, "WEAPON DENSITY",
            new Rect(left.Left + 8.0, left.Top + 67.0,
                left.Width - 16.0, 72.0),
            text, muted, border, accent, panel);
        DrawTogglePill(dc, uniqueWeapons, "SHUFFLE UNIQUE WEAPONS",
            new Rect(left.Left + 8.0, left.Top + 153.0,
                left.Width - 16.0, 44.0),
            text, muted, border, accent, panel);

        var otherToggles = new (ParameterViewModel Parameter, string Label)[]
        {
            (attachments, "SHUFFLE ATTACHMENTS"),
            (detectors, "REMOVE DETECTORS"),
            (blueprints, "SHUFFLE BLUEPRINTS")
        };
        for (var i = 0; i < otherToggles.Length; i++)
            DrawTogglePill(dc, otherToggles[i].Parameter, otherToggles[i].Label,
                new Rect(margin + cell + gap, 55.0 + i * 57.0, cell, 46.0),
                text, muted, border, accent, panel);
    }

    // Chance, successful-roll amount and a different stash tier's coupon roll
    // remain three independent source parameters. The preview bars are deliberately
    // read-only and never combine them into an inferred expected-loot value.
    private void DrawPairedSystemBoard(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var board = Family switch
        {
            "MutantCombatPressure" => (
                Title: "MUTANT COMBAT PRESSURE",
                Subtitle: "hit strength and attack frequency are independent",
                First: "combat.mutantDamage",
                FirstLabel: "DAMAGE FROM MUTANTS",
                Second: "combat.mutantAttackCooldown",
                SecondLabel: "ATTACK FREQUENCY",
                Note: "RIGHT = MORE DAMAGE / MORE FREQUENT ATTACKS"),
            "StaminaCore" => (
                Title: "CORE STAMINA",
                Subtitle: "total stamina pool and base recovery per second",
                First: "stamina.maxSP",
                FirstLabel: "STAMINA CAPACITY",
                Second: "stamina.regeneration",
                SecondLabel: "BASE RECOVERY RATE",
                Note: "CAPACITY AND RECOVERY ARE INDEPENDENT CFG VALUES"),
            "PhysicalLimits" => (
                Title: "MOVEMENT LIMITS",
                Subtitle: "vaulting clearance and equipment carrying capacity",
                First: "stamina.vaultMaxHeight",
                FirstLabel: "MAXIMUM VAULT HEIGHT",
                Second: "stamina.maxCarryWeight",
                SecondLabel: "MAXIMUM CARRY WEIGHT",
                Note: "VAULT HEIGHT USES UU  |  INVENTORY CAPACITY USES KG"),
            "TraderStockBoard" => (
                Title: "TRADER STOCK",
                Subtitle: "independent quantity multipliers, not new item selections",
                First: "economy.traderAmmoStock",
                FirstLabel: "REGULAR TRADERS / AMMO SUPPLY",
                Second: "economy.medicStock",
                SecondLabel: "MEDICS / MEDICAL SUPPLY",
                Note: "STOCK QUANTITY ONLY  |  RESTOCK RULES STAY VANILLA"),
            "ItemWeightBoard" => (
                Title: "ITEM WEIGHT",
                Subtitle: "relative inventory mass per item category",
                First: "equipment.ammoWeight",
                FirstLabel: "AMMUNITION WEIGHT",
                Second: "equipment.consumableWeight",
                SecondLabel: "FOOD AND MEDICINE WEIGHT",
                Note: "ITEM WEIGHT DOES NOT CHANGE MAXIMUM CARRY WEIGHT"),
            "WaterStaminaPressure" => (
                Title: "WATER STAMINA PRESSURE",
                Subtitle: "extra stamina drain by water depth",
                First: "stamina.waterMediumDrain",
                FirstLabel: "MEDIUM WATER DRAIN",
                Second: "stamina.waterStrongDrain",
                SecondLabel: "DEEP WATER DRAIN",
                Note: "RIGHT = MORE STAMINA DRAIN AT THAT DEPTH"),
            _ => (
                Title: "RADIATION TREATMENT",
                Subtitle: "radiation removed per item use (raw CFG points)",
                First: "health.ecoMedkitAntirad",
                FirstLabel: "SCIENTIFIC MEDKIT",
                Second: "health.antiRadRadiation",
                SecondLabel: "ANTI-RAD",
                Note: "RIGHT = MORE RADIATION REMOVED PER USE")
        };

        DrawHeader(dc, board.Title, board.Subtitle, text, muted, accent);
        var cardWidth = Math.Max(1.0, ActualWidth - 32.0);
        DrawHorizontalTrackCard(dc, P(board.First), board.FirstLabel,
            new Rect(16.0, 56.0, cardWidth, 72.0),
            text, muted, border, accent, panel, pairedValueFormatting: true);

        // The vault-height track retains the authored WAIST/EYES landmarks.
        // The tick marks share the native track hit area: clicking a tick snaps
        // to that exact source height (130/225/300 uu) with no extra control.
        var isPhysicalLimits = Family == "PhysicalLimits";
        if (isPhysicalLimits && P(board.First) is { } vault)
        {
            var track = new Rect(30.0, 95.0, Math.Max(1.0, cardWidth - 28.0), 12.0);
            foreach (var marker in vault.VanillaMarkers)
            {
                var x = ValueToX(marker.Value, vault.Minimum, vault.Maximum, track, false);
                dc.DrawLine(new Pen(muted, 1.8),
                    new Point(x, 89.0), new Point(x, 95.0));
                var caption = marker.Label.Replace("VANILLA / ", "", StringComparison.Ordinal);
                DrawCenteredText(dc, caption, new Point(x, 136.0),
                    9.0, muted, FontWeights.SemiBold);
                // Clicking the printed landmark (not just the 12px track)
                // chooses the exact authored height. Keep the hit rect inside
                // the card even for the rightmost ABOVE EYES marker.
                var captionWidth = 112.0;
                var captionLeft = Math.Clamp(x - captionWidth / 2.0,
                    13.0, Math.Max(13.0, ActualWidth - captionWidth - 13.0));
                AddHorizontalTarget(vault,
                    new Rect(captionLeft, 132.0, captionWidth, 23.0),
                    new Rect(x, 132.0, 1.0, 23.0),
                    marker.Value, marker.Value);
            }
        }

        DrawHorizontalTrackCard(dc, P(board.Second), board.SecondLabel,
            new Rect(16.0, isPhysicalLimits ? 178.0 : 139.0, cardWidth, 72.0),
            text, muted, border, accent, panel, pairedValueFormatting: true);

        // Bounded, one-line help avoids collision at 520px minimum width.
        dc.DrawText(CreateText(board.Note, 9.4, text, FontWeights.SemiBold,
                Math.Max(1.0, ActualWidth - 36.0)),
            new Point(18.0, Family == "PhysicalLimits" ? 265.0 : 228.0));
    }

    // Matched twin native multipliers: exact Core Health/Core Stamina
    // primary ruler chrome, not two smaller generic horizontal-track cards.
    // The linked Medium <= Deep constraint lives in ParameterViewModel.
    private void DrawWaterStaminaBoard(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawHeader(dc, "WATER STAMINA DRAIN", "", text, muted, accent);
        var width = Math.Max(1.0, ActualWidth - 32.0);
        const double gap = 12.0;
        var cell = (width - gap) / 2.0;
        if (P("stamina.waterMediumDrain") is { } medium)
            DrawCoreResourceRuler(dc, medium, "MEDIUM WATER", "x",
                new Rect(16.0, 51.0, cell, 105.0),
                text, muted, border, accent, panel);
        if (P("stamina.waterStrongDrain") is { } deep)
            DrawCoreResourceRuler(dc, deep, "DEEP WATER", "x",
                new Rect(16.0 + cell + gap, 51.0, cell, 105.0),
                text, muted, border, accent, panel);
    }

    // Unified first Stamina card: four native CFG tracks, no redundant subtitle/footer.
    // Two compact rows; the third control retains body-height landmarks.
    // All four authored vanilla values are identical across difficulties,
    // so no misleading per-difficulty reference labels are displayed.
    // Dedicated native-height ruler, not a fictitious metres or jump-height
    // simulation: Unreal units are the exact CFG unit (uu), and the source's
    // authored WAIST / EYES / ABOVE EYES landmarks stay at 130 / 225 / 300.
    // Vault height and carry capacity share compact horizontal card anatomy.
    // The markers are source-specific; none of these values is a simulated
    // collision or load threshold. Preserve the real native CFG hit mapping.
    private void DrawMovementLimitRuler(
        DrawingContext dc, ParameterViewModel? parameter, string label, string unit,
        Rect card, Brush text, Brush muted, Brush border, Brush accent, Brush panel,
        bool vaultHeight)
    {
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.70),
            new Pen(border, 0.9), card, 4.0, 4.0);
        if (parameter is null)
            return;

        var enabled = parameter.IsControlEnabled;
        var value = $"{parameter.CurrentNumber:0} {unit}";
        var readout = CreateText(value, 12.4, enabled ? text : muted,
            FontWeights.Bold, Math.Max(1.0, card.Width - 26.0));
        if (enabled && parameter.IsValueModifiedFromReference())
        {
            // Changed number only; the real uu/KG unit stays neutral.
            var digits = value.IndexOf(' ');
            if (digits > 0)
            {
                readout.SetForegroundBrush(accent, 0, digits);
                readout.SetFontSize(13.6, 0, digits);
            }
        }

        var availableTitle = Math.Max(1.0,
            card.Width - 26.0 - readout.WidthIncludingTrailingWhitespace - 8.0);
        dc.DrawText(CreateText(label, 10.7, text, FontWeights.Bold, availableTitle),
            new Point(card.Left + 12.0, card.Top + 11.0));
        dc.DrawText(readout,
            new Point(card.Right - 12.0 - readout.WidthIncludingTrailingWhitespace,
                card.Top + 7.0));

        // Both sliders use one shared rail location, scale and hit geometry.
        var rail = new Rect(card.Left + 18.0, card.Top + 45.0,
            Math.Max(1.0, card.Width - 36.0), 12.0);
        var y = rail.Top + rail.Height / 2.0;
        var currentX = ValueToX(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, rail, false);

        dc.DrawLine(new Pen(border, 4.0),
            new Point(rail.Left, y), new Point(rail.Right, y));
        if (enabled)
            dc.DrawLine(new Pen(CloneWithOpacity(accent, .78), 4.0),
                new Point(rail.Left, y), new Point(currentX, y));

        if (vaultHeight)
        {
            // These are authored editor landmarks, not meters or promised
            // traversable obstacle heights. Their captions remain clickable.
            foreach (var marker in parameter.VanillaMarkers)
            {
                var x = ValueToX(marker.Value, parameter.Minimum,
                    parameter.Maximum, rail, false);
                dc.DrawLine(new Pen(CloneWithOpacity(muted, .76), 1.2),
                    new Point(x, y - 8.0), new Point(x, y + 9.0));
            }
        }
        else
        {
            // Native kg calibration only; avoid implying invented
            // encumbrance, sprint, or fatigue thresholds.
            for (var tick = 1; tick <= 3; tick++)
            {
                var kg = parameter.Minimum +
                    tick * (parameter.Maximum - parameter.Minimum) / 4.0;
                var x = ValueToX(kg, parameter.Minimum, parameter.Maximum, rail, false);
                dc.DrawLine(new Pen(CloneWithOpacity(muted, .58), 1.0),
                    new Point(x, y - 8.0), new Point(x, y + 9.0));
                var markerText = CreateText($"{kg:0}", 9.0, muted, FontWeights.SemiBold);
                dc.DrawText(markerText,
                    new Point(x - markerText.WidthIncludingTrailingWhitespace / 2.0,
                        card.Top + 70.0));
            }
        }

        DrawReferenceTick(dc, parameter, rail, y, muted);
        var hovered = enabled && _hoverTarget?.Kind == HitKind.Horizontal
            && ReferenceEquals(_hoverTarget.Parameter, parameter);
        dc.DrawEllipse(panel, new Pen(enabled ? accent : muted,
                hovered || IsSelected(parameter) ? 2.6 : 1.7),
            new Point(currentX, y), 7.0, 7.0);
        if (enabled)
            dc.DrawEllipse(accent, null, new Point(currentX, y), 2.4, 2.4);
        AddHorizontalTarget(parameter,
            new Rect(rail.Left - 7.0, rail.Top - 8.0,
                rail.Width + 14.0, rail.Height + 16.0),
            rail, parameter.Minimum, parameter.Maximum);

        if (vaultHeight)
        {
            foreach (var marker in parameter.VanillaMarkers)
            {
                var x = ValueToX(marker.Value, parameter.Minimum,
                    parameter.Maximum, rail, false);
                var caption = marker.Label.Replace("VANILLA / ", "",
                    StringComparison.Ordinal);
                var fixedHover = _hoverTarget?.Kind == HitKind.Horizontal
                    && ReferenceEquals(_hoverTarget.Parameter, parameter)
                    && Math.Abs(_hoverTarget.VisualMinimum - marker.Value) < Epsilon
                    && Math.Abs(_hoverTarget.VisualMaximum - marker.Value) < Epsilon;
                var captionText = CreateText(caption, 9.0,
                    fixedHover ? Brushes.White : muted, FontWeights.SemiBold,
                    Math.Max(1.0, card.Width - 18.0));
                var captionLeft = Math.Clamp(
                    x - captionText.WidthIncludingTrailingWhitespace / 2.0,
                    card.Left + 9.0,
                    Math.Max(card.Left + 9.0,
                        card.Right - 9.0 - captionText.WidthIncludingTrailingWhitespace));
                // At narrow widths the final ABOVE EYES label drops one
                // short line, rather than overlapping the EYES caption.
                var captionTop = card.Top +
                    (card.Width < 270.0 && marker.Value >= parameter.Maximum - Epsilon
                        ? 82.0 : 69.0);
                dc.DrawText(captionText, new Point(captionLeft, captionTop));
                AddHorizontalTarget(parameter,
                    new Rect(captionLeft - 4.0, captionTop - 3.0,
                        captionText.WidthIncludingTrailingWhitespace + 8.0,
                        captionText.Height + 6.0),
                    new Rect(x, captionTop, 1.0, Math.Max(1.0, captionText.Height)),
                    marker.Value, marker.Value);
            }
        }

        DrawText(dc, $"{parameter.Minimum:0} {unit}",
            new Point(rail.Left, card.Bottom - 19.0),
            9.6, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, $"{parameter.Maximum:0} {unit}",
            new Point(rail.Right, card.Bottom - 19.0),
            9.6, muted, FontWeights.Normal);
    }

    // Core Stamina mirrors Core Health's two primary 105-DIP native rulers:
    // shared typography, rail/handle, limits and reference ticks. Movement
    // keeps its independent source-native vault and carry editors below.
    private void DrawStaminaMovementBoard(DrawingContext dc, Brush text, Brush muted,
        Brush border, Brush accent, Brush panel)
    {
        DrawHeader(dc, "CORE STAMINA", "", text, muted, accent);
        var width = Math.Max(1.0, ActualWidth - 32.0);
        const double gap = 12.0;
        var cell = (width - gap) / 2.0;

        if (P("stamina.maxSP") is { } stamina)
            DrawCoreResourceRuler(dc, stamina, "MAX STAMINA", "SP",
                new Rect(16.0, 51.0, cell, 105.0),
                text, muted, border, accent, panel);
        if (P("stamina.regeneration") is { } recovery)
            DrawCoreResourceRuler(dc, recovery, "BASE RECOVERY", "SP/s",
                new Rect(16.0 + cell + gap, 51.0, cell, 105.0),
                text, muted, border, accent, panel);

        DrawText(dc, "MOVEMENT LIMITS", new Point(19.0, 173.0),
            10.2, muted, FontWeights.Bold);
        DrawMovementLimitRuler(dc, P("stamina.vaultMaxHeight"),
            "MAXIMUM VAULT HEIGHT", "uu",
            new Rect(16.0, 191.0, cell, 116.0),
            text, muted, border, accent, panel, vaultHeight: true);
        DrawMovementLimitRuler(dc, P("stamina.maxCarryWeight"),
            "MAXIMUM CARRY WEIGHT", "KG",
            new Rect(16.0 + cell + gap, 191.0, cell, 116.0),
            text, muted, border, accent, panel, vaultHeight: false);
    }

    // Hunger and Fatigue share exactly the same two native-resource rulers as
    // Core Health and Core Stamina. One measured row precedes each stage graph.
    private const double SurvivalPrimaryRulerTop = 51.0;
    private const double SurvivalPrimaryRulerHeight = 105.0;
    private const double SurvivalPrimaryPreviewTop =
        SurvivalPrimaryRulerTop + SurvivalPrimaryRulerHeight + 12.0;

    // Fatigue stages affect when penalties begin, while their strengths remain
    // separate authored effects. The overview is read-only: thresholds and
    // different combat effects can coincide, so interactive graph points overlap.
    // Each CFG value instead has one full-width, native-range slider below.
    // Two-column editing on wide surfaces; single-column fallback when narrow.
    // Stage graph is read-only to avoid overlapping draggable graph handles.
    private ParameterViewModel? ReloadEffectFor(ParameterViewModel threshold)
        => P(threshold.Id == "survival.sleepMediumThreshold"
            ? "survival.fatigueMediumReloadPenalty"
            : "survival.fatigueStrongReloadPenalty");

    private ParameterViewModel? FatigueEffectFor(ParameterViewModel threshold)
    {
        var stage = threshold.Id == "survival.sleepLowThreshold" ? "Low" :
            threshold.Id == "survival.sleepMediumThreshold" ? "Medium" : "Strong";
        return P("survival.fatigue" + stage +
            (_fatigueReloadInteraction ? "ReloadPenalty" : "AimPenalty"));
    }
    private double FatigueAimMaximum()
        => Math.Max(P("survival.fatigueLowAimPenalty")?.Maximum ?? 0,
            Math.Max(P("survival.fatigueMediumAimPenalty")?.Maximum ?? 0,
                P("survival.fatigueStrongAimPenalty")?.Maximum ?? 0));
    private double FatigueReloadMaximum()
        => Math.Max(P("survival.fatigueMediumReloadPenalty")?.Maximum ?? 0,
            P("survival.fatigueStrongReloadPenalty")?.Maximum ?? 0);

    private void DrawFatigueCurve(
        DrawingContext dc, Brush text, Brush muted, Brush border, Brush soft,
        Brush accent, Brush panel)
    {
        var rate = P("survival.sleepinessRate");
        var relief = P("survival.energyDrinkFatigueRelief");
        var low = P("survival.sleepLowThreshold");
        var medium = P("survival.sleepMediumThreshold");
        var severe = P("survival.sleepStrongThreshold");
        var lowAim = P("survival.fatigueLowAimPenalty");
        var mediumAim = P("survival.fatigueMediumAimPenalty");
        var severeAim = P("survival.fatigueStrongAimPenalty");
        var mediumReload = P("survival.fatigueMediumReloadPenalty");
        var severeReload = P("survival.fatigueStrongReloadPenalty");

        DrawHeader(dc, "FATIGUE PROFILE", string.Empty, text, muted, accent);
        if (rate is null || relief is null || low is null || medium is null ||
            severe is null || lowAim is null || mediumAim is null ||
            severeAim is null || mediumReload is null || severeReload is null)
        {
            DrawCenteredText(dc, "Fatigue settings unavailable.",
                new Point(ActualWidth / 2.0, 140), 10, muted, FontWeights.Bold);
            return;
        }

        var width = Math.Max(460.0, ActualWidth - 34.0);
        var resourceWidth = Math.Max(1.0, ActualWidth - 32.0);
        const double resourceGap = 12.0;
        var half = (resourceWidth - resourceGap) / 2.0;
        DrawCoreResourceRuler(dc, rate, "FATIGUE RATE", "pts/s",
            new Rect(16.0, SurvivalPrimaryRulerTop, half, SurvivalPrimaryRulerHeight),
            text, muted, border, accent, panel);
        DrawCoreResourceRuler(dc, relief, "ENERGY DRINK FATIGUE RELIEF", "x",
            new Rect(16.0 + half + resourceGap, SurvivalPrimaryRulerTop,
                half, SurvivalPrimaryRulerHeight),
            text, muted, border, accent, panel);

        var previewTop = SurvivalPrimaryPreviewTop;
        var preview = new Rect(17, previewTop, width, 80);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1), preview, 4, 4);
        DrawText(dc, "FATIGUE POINTS / STAGE PREVIEW",
            new Point(preview.Left + 12, preview.Top + 7),
            11.0, text, FontWeights.Bold);
        var axis = new Rect(preview.Left + 81, preview.Top + 32, preview.Width - 111, 12);
        double X(double value) => ValueToX(value, 0, 100, axis, false);
        var y = axis.Top + axis.Height / 2;
        var thresholds = new[] { low, medium, severe };
        var stops = new[] { 0.0, low.CurrentNumber, medium.CurrentNumber, severe.CurrentNumber, 100.0 };
        for (var i = 0; i < 4; i++)
            dc.DrawRectangle(CloneWithOpacity(i == 3 ? accent : soft, i == 3 ? 0.72 : 0.2 + i * 0.12),
                null, new Rect(X(stops[i]), y - 4, Math.Max(0, X(stops[i + 1]) - X(stops[i])), 8));
        for (var i = 0; i < 3; i++)
        {
            dc.DrawLine(new Pen(muted, 1.5), new Point(X(thresholds[i].ReferenceNumber), y - 10),
                new Point(X(thresholds[i].ReferenceNumber), y + 10));
            dc.DrawLine(new Pen(accent, 2), new Point(X(thresholds[i].CurrentNumber), y - 11),
                new Point(X(thresholds[i].CurrentNumber), y + 11));
            DrawCenteredText(dc, new[] { "LOW", "MEDIUM", "SEVERE" }[i] +
                $" {thresholds[i].CurrentNumber:0}",
                new Point(preview.Left + 12 + (i + 0.5) * (preview.Width - 24) / 3, preview.Top + 55),
                9.5, i == 2 ? accent : text, FontWeights.SemiBold);
        }

        DrawFatigueCombined(dc, preview.Bottom + 51.0, width,
            low, medium, severe, lowAim, mediumAim, severeAim,
            mediumReload, severeReload, text, muted, border, soft, accent, panel);
    }

    // Gold aim curve and white reload curve share fatigue-point thresholds
    // but have independent native Y scales and individually draggable rings.
    private void DrawFatigueCombined(DrawingContext dc, double top, double width,
        ParameterViewModel low, ParameterViewModel medium, ParameterViewModel severe,
        ParameterViewModel lowAim, ParameterViewModel mediumAim,
        ParameterViewModel severeAim, ParameterViewModel mediumReload,
        ParameterViewModel severeReload, Brush text, Brush muted,
        Brush border, Brush soft, Brush accent, Brush panel)
    {
        var frame = new Rect(17, top, width, 365);
        DrawCenteredText(dc, "FATIGUE COMBAT PROFILE",
            new Point(frame.Left + frame.Width / 2, top - 29), 11.5, accent, FontWeights.Bold);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1), frame, 4, 4);
        var area = new Rect(frame.Left + 81, top + 52, frame.Width - 111, 164);
        double X(double v) => ValueToX(v, 0, 100, area, false);
        var aimMax = Math.Max(lowAim.Maximum, Math.Max(mediumAim.Maximum, severeAim.Maximum));
        var reloadMax = Math.Max(mediumReload.Maximum, severeReload.Maximum);
        double YA(double v) => ValueToY(v, 0, aimMax, area);
        // Reload uses its own slightly shorter vertical range, while retaining
        // the exact native source maximum and shared fatigue-point X positions.
        var reloadArea = new Rect(area.Left, area.Top + 18, area.Width, area.Height - 30);
        double YR(double v) => ValueToY(v, 0, reloadMax, reloadArea);
        var thresholds = new[] { low, medium, severe };
        var aims = new[] { lowAim, mediumAim, severeAim };
        var reloads = new[] { mediumReload, severeReload };
        DrawCenteredText(dc, "FATIGUE POINTS (0-100)",
            new Point(area.Left + area.Width / 2, top + 25), 10.2, text, FontWeights.Bold);
        var left = new Point(area.Left - 55, area.Top + area.Height / 2);
        dc.PushTransform(new RotateTransform(-90, left.X, left.Y));
        DrawCenteredText(dc, "AIM TIME", left, 10.2, accent, FontWeights.Bold);
        dc.Pop();
        // Reload begins at MEDIUM: show a distinct, shorter secondary scale
        // within the chart edge instead of mirroring the complete AIM axis.
        var reloadAxisX = area.Right - 16.0;
        dc.DrawLine(new Pen(CloneWithOpacity(text, .48), 1.2),
            new Point(reloadAxisX, reloadArea.Top),
            new Point(reloadAxisX, reloadArea.Bottom));
        // Place the white Y-axis title vertically at the outer right margin.
        // Do not move the plot, its X mapping or the preview alignment.
        var reloadTitle = new Point(frame.Right - 8.0,
            reloadArea.Top + reloadArea.Height / 2.0);
        dc.PushTransform(new RotateTransform(90, reloadTitle.X, reloadTitle.Y));
        DrawCenteredText(dc, "RELOAD TIME", reloadTitle, 10.2, text, FontWeights.Bold);
        dc.Pop();
        for (int i = 0; i <= 4; i++)
        {
            var ratio = i / 4.0;
            var yy = area.Bottom - ratio * area.Height;
            dc.DrawLine(new Pen(CloneWithOpacity(soft, .35), 1)
                { DashStyle = DashStyles.Dot },
                new Point(area.Left, yy), new Point(area.Right, yy));
            DrawRightAlignedText(dc, $"+{aimMax * ratio * 100:0.#}%",
                new Point(area.Left - 7, yy - 7), 9, accent, FontWeights.SemiBold);
            var reloadTickY = reloadArea.Bottom - ratio * reloadArea.Height;
            dc.DrawLine(new Pen(CloneWithOpacity(text, .5), 1),
                new Point(reloadAxisX - 3, reloadTickY),
                new Point(reloadAxisX + 3, reloadTickY));
            DrawRightAlignedText(dc, $"+{reloadMax * ratio * 100:0.#}%",
                new Point(reloadAxisX - 5, reloadTickY - 7),
                9, text, FontWeights.SemiBold);
        }
        for (int t = 0; t <= 100; t += 25)
            DrawCenteredText(dc, $"{t}", new Point(X(t), area.Bottom + 16),
                9.2, muted, FontWeights.SemiBold);
        void Steps(ParameterViewModel[] effects, bool reload, bool basePreset, Pen pen)
        {
            // Reload's white response is shown only in its MEDIUM–SEVERE
            // domain: no lead-in from zero and no trailing white segment.
            var previousX = reload
                ? X(basePreset ? medium.ReferenceNumber : medium.CurrentNumber)
                : area.Left;
            var previousY = reload
                ? YR(basePreset ? mediumReload.ReferenceNumber : mediumReload.CurrentNumber)
                : area.Bottom;
            for (int i = reload ? 1 : 0; i < effects.Length; i++)
            {
                var threshold = thresholds[i + (reload ? 1 : 0)];
                var x = X(basePreset ? threshold.ReferenceNumber : threshold.CurrentNumber);
                var value = basePreset ? effects[i].ReferenceNumber : effects[i].CurrentNumber;
                var yy = reload ? YR(value) : YA(value);
                dc.DrawLine(pen, new Point(previousX, previousY), new Point(x, previousY));
                dc.DrawLine(pen, new Point(x, previousY), new Point(x, yy));
                previousX = x;
                previousY = yy;
            }
            if (!reload)
                dc.DrawLine(pen, new Point(previousX, previousY),
                    new Point(area.Right, previousY));
        }
        Steps(aims, false, true, new Pen(CloneWithOpacity(accent, .38), 1.5)
            { DashStyle = DashStyles.Dash });
        Steps(reloads, true, true, new Pen(CloneWithOpacity(muted, .7), 1.5)
            { DashStyle = DashStyles.Dash });
        Steps(aims, false, false, new Pen(accent, 2.8));
        Steps(reloads, true, false, new Pen(text, 2.6));
        void Ring(ParameterViewModel threshold, ParameterViewModel effect, bool reload)
        {
            var x = X(threshold.CurrentNumber);
            var yy = reload ? YR(effect.CurrentNumber) : YA(effect.CurrentNumber);
            var by = reload ? YR(effect.ReferenceNumber) : YA(effect.ReferenceNumber);
            dc.DrawEllipse(panel, new Pen(muted, 1.4),
                new Point(X(threshold.ReferenceNumber), by), 6, 6);
            var kind = reload ? HitKind.FatigueReloadJoint : HitKind.FatigueJoint;
            DrawHumanAxisHandle(dc, new Point(x, yy), reload ? text : accent, panel,
                ReferenceEquals(_hoverTarget?.Parameter, threshold) && _hoverTarget?.Kind == kind,
                (reload ? ReferenceEquals(_selectedParameter, effect)
                        : IsSelected(threshold)) && _selectedHitKind == kind);
            _hitTargets.Add(new HitTarget(threshold,
                new Rect(x - 17, yy - 17, 34, 34), reload ? reloadArea : area, kind,
                threshold.Minimum, threshold.Maximum, false));
        }
        for (int i = 0; i < 3; i++) Ring(thresholds[i], aims[i], false);
        for (int i = 0; i < 2; i++) Ring(thresholds[i + 1], reloads[i], true);
        // Separate source-accurate stage readouts. The old long, clipped
        // one-line sentences could not fit three equal-width columns.
        var cellWidth = (frame.Width - 16.0) / 3.0;
        for (var i = 0; i < 3; i++)
        {
            var cell = new Rect(frame.Left + 8.0 + i * cellWidth,
                top + 260.0, cellWidth, 93.0);
            dc.PushClip(new RectangleGeometry(cell));
            var cx = cell.Left + cell.Width / 2.0;
            DrawCenteredText(dc, new[] { "LOW", "MEDIUM", "SEVERE" }[i],
                new Point(cx, top + 262.0),
                10.3, text, FontWeights.Bold);
            DrawCenteredNumericChanges(dc,
                $"AT {thresholds[i].CurrentNumber:0} PTS",
                new Point(cx, top + 282.0),
                10.1, text, accent, FontWeights.SemiBold,
                thresholds[i].IsValueModifiedFromReference());
            DrawCenteredNumericChanges(dc,
                $"AIM +{aims[i].CurrentNumber * 100:0.#}%",
                new Point(cx, top + 304.0),
                10.1, text, accent, FontWeights.SemiBold,
                aims[i].IsValueModifiedFromReference());
            if (i > 0)
                DrawCenteredNumericChanges(dc,
                    $"RELOAD +{reloads[i - 1].CurrentNumber * 100:0.##}%",
                    new Point(cx, top + 326.0),
                    10.1, text, accent, FontWeights.SemiBold,
                    reloads[i - 1].IsValueModifiedFromReference());
            dc.Pop();
        }

    }

    // Master panels share Stamina Usage's compact source-first grammar.
    // Only ACTUAL native parameters are drawn; switch-disabled children retain
    // their staged values and remain non-interactive until enabled.
    private static Brush MasterDisabledInk(Brush text) => CloneWithOpacity(text, 0.70);

    private static (int Columns, double CardWidth) MasterCardGrid(double width)
    {
        var columns = EconomyColumns(width);
        return (columns, EconomyCardWidth(width, columns));
    }

    private static int MasterRows(double width, int count)
        => (count + EconomyColumns(width) - 1) / EconomyColumns(width);

    private static double MasterConditionalHeight(double width) => 298.0;
    private static double MasterCardBankHeight(double width, int count)
        => 107.0 + MasterRows(width, count) * 94.0 + 12.0;
    private const double StarvationPreviewHeight = 176.0;
    private static double MasterStarvationHeight(double width)
        => 110.0 + MasterRows(width, 2) * 94.0 +
            StarvationPreviewHeight + 12.0 + MasterRows(width, 3) * 94.0 + 12.0;

    private void DrawMasterSectionTitle(DrawingContext dc, string title, Brush accent)
        => DrawCenteredText(dc, title, new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);

    private void DrawMasterBundleToggle(
        DrawingContext dc, ParameterViewModel? toggle, string label,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        if (toggle is null)
            return;
        var width = Math.Min(320.0, Math.Max(1.0, ActualWidth - 32.0));
        DrawTogglePill(dc, toggle, label,
            new Rect((ActualWidth - width) / 2.0, 52.0, width, 39.0),
            text, muted, border, accent, panel);
    }

    // A single native range track. A separate difficulty reference marker is
    // never the same thing as 100% normal healing or the zero penalty point.
    private void DrawMasterNativeCard(
        DrawingContext dc, ParameterViewModel? parameter, string title,
        Func<double, string> valueText, Rect card,
        Brush text, Brush muted, Brush border, Brush soft,
        Brush accent, Brush panel)
    {
        dc.DrawRoundedRectangle(panel, new Pen(border, 0.9),
            card, 4.0, 4.0);
        if (parameter is null)
        {
            dc.DrawText(CreateText(title, 10.0, muted, FontWeights.Bold,
                    Math.Max(1.0, card.Width - 24.0)),
                new Point(card.Left + 12.0, card.Top + 8.0));
            return;
        }

        var enabled = parameter.IsControlEnabled;
        var changed = enabled && parameter.IsValueModifiedFromReference();
        // Disabled Master child cards have one readable neutral ink regardless
        // of which bundle owns them. Never apply a second whole-card fade.
        var disabledInk = MasterDisabledInk(text);
        var display = CreateText(valueText(parameter.CurrentNumber),
            changed ? 12.2 : 11.0,
            enabled ? changed ? accent : text : disabledInk, FontWeights.Bold);
        var labelWidth = Math.Max(1.0, card.Width - 36.0 -
            display.WidthIncludingTrailingWhitespace - 9.0);
        dc.DrawText(CreateText(title, 10.1, enabled ? text : disabledInk,
                FontWeights.Bold, labelWidth),
            new Point(card.Left + 12.0, card.Top + 8.0));
        dc.DrawText(display, new Point(
            card.Right - 12.0 - display.WidthIncludingTrailingWhitespace,
            card.Top + 7.0));

        var track = new Rect(card.Left + 14.0, card.Top + 38.0,
            Math.Max(1.0, card.Width - 28.0), 14.0);
        var y = track.Top + track.Height / 2.0;
        var currentX = ValueToX(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, track, false);
        var referenceX = ValueToX(parameter.ReferenceNumber,
            parameter.Minimum, parameter.Maximum, track, false);
        dc.DrawLine(new Pen(soft, 3.0),
            new Point(track.Left, y), new Point(track.Right, y));
        if (enabled)
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.72), 3.0),
                new Point(track.Left, y), new Point(currentX, y));

        // In Master consumable effects 100% means original healing strength,
        // whereas the source Base tick can be Bandage 25%, Medkit 71% or
        // Army Medkit 82%. Preserve this distinct meaningful reference.
        if (parameter.Id is "health.masterBandageModifier"
            or "health.masterMedkitModifier"
            or "health.masterArmyMedkitModifier")
        {
            var normalX = ValueToX(1.0, parameter.Minimum,
                parameter.Maximum, track, false);
            if (Math.Abs(normalX - referenceX) >= 2.0)
                dc.DrawLine(new Pen(CloneWithOpacity(muted, 0.55), 1.0)
                    { DashStyle = DashStyles.Dash },
                    new Point(normalX, y - 9.0), new Point(normalX, y + 9.0));
        }
        dc.DrawLine(new Pen(muted, 1.8),
            new Point(referenceX, y - 8.0),
            new Point(referenceX, y + 8.0));
        dc.DrawEllipse(panel, new Pen(enabled ? accent : muted,
                enabled && IsSelected(parameter) ? 2.4 : 1.5),
            new Point(currentX, y), 6.0, 6.0);
        if (enabled)
            dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

        // Hover halo comes from the common DrawHoveredHandle overlay, not
        // from card/label hover. One target edits exactly this source.
        AddHorizontalTarget(parameter,
            new Rect(track.Left - 6.0, track.Top - 6.0,
                track.Width + 12.0, track.Height + 12.0),
            track, parameter.Minimum, parameter.Maximum);
        DrawText(dc, valueText(parameter.Minimum),
            new Point(track.Left, card.Top + 59.0),
            9.1, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, valueText(parameter.Maximum),
            new Point(track.Right, card.Top + 59.0),
            9.1, muted, FontWeights.Normal);
    }

    private void DrawMasterCardBank(
        DrawingContext dc,
        (string Id, string Label, Func<double, string> Format)[] sources,
        double top,
        Brush text, Brush muted, Brush border, Brush soft, Brush accent,
        Brush panel)
    {
        var (columns, width) = MasterCardGrid(ActualWidth);
        const double gap = 12.0;
        for (var i = 0; i < sources.Length; i++)
        {
            var row = i / columns;
            var inRow = Math.Min(columns, sources.Length - row * columns);
            var rowWidth = inRow * width + (inRow - 1) * gap;
            var left = (ActualWidth - rowWidth) / 2.0;
            var card = new Rect(left + (i % columns) * (width + gap),
                top + row * 94.0, width, 80.0);
            DrawMasterNativeCard(dc, P(sources[i].Id),
                sources[i].Label, sources[i].Format, card,
                text, muted, border, soft, accent, panel);
        }
    }

    private static string MasterPercent(double value) => $"{value * 100:0.#}%";
    private static string MasterPositivePercent(double value) => $"+{value * 100:0.#}%";
    private static string MasterNegativePercent(double value) => $"-{value * 100:0.#}%";
    private static string MasterSeconds(double value) => $"{value:0.#}s";
    private static string MasterHp(double value) => $"{value:0.#} HP";
    private static string MasterScalar(double value) => $"{value:0.##}x";

    private void DrawMasterConditionalPenalty(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush soft, Brush accent, Brush panel)
    {
        var isLowHealth = Family == "MasterLowHealth";
        DrawMasterSectionTitle(dc,
            isLowHealth ? "LOW HEALTH PENALTY" : "RADIATION PENALTY", accent);
        var toggle = P(isLowHealth
            ? "advanced.lowHealthMechanics" : "advanced.radiationMechanics");
        DrawMasterBundleToggle(dc, toggle,
            isLowHealth ? "LOW HEALTH MECHANICS" : "RADIATION MECHANICS",
            text, muted, border, accent, panel);

        var source = P(isLowHealth
            ? "health.masterLowHealthPenalty"
            : "survival.radiationStaminaDrainPenalty");
        // Share the approved frameless, source-step-accurate rotary grammar
        // with all other knobs. The sign belongs to the player-facing
        // penalty display only; the stored native effect stays 0..1.
        const double knobHeight = 181.0;
        var knobWidth = Math.Min(204.0, Math.Max(170.0, ActualWidth - 34.0));
        DrawSourceKnob(dc, source,
            isLowHealth ? "HEALTH RECOVERY LOSS" : "ACTION STAMINA COST",
            new Rect((ActualWidth - knobWidth) / 2.0, 102.0, knobWidth, knobHeight),
            text, muted, border, accent, panel,
            percentageReadout: true, percentageSign: isLowHealth ? -1 : 1);
    }

    // The modifier controls are independent source ValueMultipliers,
    // not effective HP previews. The toggle also governs other non-medical
    // MasterEffectModifier effects which are not exposed as separate sliders.
    private void DrawMasterMedicalHealing(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush soft, Brush accent, Brush panel)
    {
        DrawMasterSectionTitle(dc, "CONSUMABLE EFFECT PENALTIES", accent);
        DrawMasterBundleToggle(dc, P("advanced.masterEffectModifierMechanics"),
            "CONSUMABLE EFFECT BUNDLE",
            text, muted, border, accent, panel);
        var sources = new (string Id, string Label, Func<double, string> Format)[]
        {
            ("health.masterBandageModifier", "BANDAGE HEALING", MasterPercent),
            ("health.masterMedkitModifier", "MEDKIT HEALING", MasterPercent),
            ("health.masterArmyMedkitModifier", "ARMY MEDKIT HEALING", MasterPercent)
        };
        DrawMasterCardBank(dc, sources, 107.0,
            text, muted, border, soft, accent, panel);
    }

    // Shared source-native vertical scale. A meter's own min/max/step,
    // reference marker, selected parameter and edit hit area are preserved.
    // This is an editable gauge, not an inferred rate/probability.
    private void DrawNativeGauge(
        DrawingContext dc, ParameterViewModel? parameter, string label,
        Func<double, string> format, Rect card, Brush text, Brush muted,
        Brush border, Brush accent, Brush panel)
    {
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.60),
            new Pen(CloneWithOpacity(border, 0.85), 0.9), card, 4, 4);
        if (parameter is null)
            return;

        var enabled = parameter.IsControlEnabled;
        var modified = enabled && parameter.IsValueModifiedFromReference();
        var disabledInk = MasterDisabledInk(text);
        var center = card.Left + card.Width / 2.0;
        DrawMixerCentered(dc, label, new Point(center, card.Top + 9.0),
            Math.Max(30.0, card.Width - 10.0), 10.0,
            enabled ? text : disabledInk, FontWeights.Bold);
        DrawMixerCentered(dc, format(parameter.CurrentNumber),
            new Point(center, card.Top + 30.0),
            Math.Max(30.0, card.Width - 10.0), modified ? 13.3 : 12.1,
            !enabled ? disabledInk : modified ? accent : text, FontWeights.Bold);
        DrawMixerCentered(dc, format(parameter.Maximum),
            new Point(center, card.Top + 53.0),
            Math.Max(30.0, card.Width - 10.0), 8.7, muted, FontWeights.Normal);

        var plot = new Rect(center - 8.0, card.Top + 77.0, 16.0,
            Math.Max(18.0, card.Height - 112.0));
        var yNow = ValueToY(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, plot);
        var yBase = ValueToY(parameter.ReferenceNumber,
            parameter.Minimum, parameter.Maximum, plot);
        dc.DrawRoundedRectangle(CloneWithOpacity(border, 0.26),
            new Pen(border, 0.8), plot, 3, 3);
        dc.DrawRoundedRectangle(CloneWithOpacity(enabled ? accent : muted, 0.70),
            null, new Rect(plot.Left + 2, yNow, plot.Width - 4,
                Math.Max(0.0, plot.Bottom - yNow)), 2, 2);
        dc.DrawLine(new Pen(muted, 2.1),
            new Point(plot.Left - 9, yBase), new Point(plot.Right + 9, yBase));
        var hovered = _hoverTarget?.Kind == HitKind.Vertical
            && ReferenceEquals(_hoverTarget.Parameter, parameter);
        dc.DrawLine(new Pen(hovered ? Brushes.White : enabled ? accent : muted,
                hovered ? 3.2 : 2.4),
            new Point(plot.Left - 6, yNow), new Point(plot.Right + 6, yNow));
        DrawMixerCentered(dc, format(parameter.Minimum),
            new Point(center, card.Bottom - 24.0),
            Math.Max(30.0, card.Width - 10.0), 8.7, muted, FontWeights.Normal);
        AddVerticalTarget(parameter,
            new Rect(plot.Left - 18.0, plot.Top - 7.0,
                plot.Width + 36.0, plot.Height + 14.0),
            plot, parameter.Minimum, parameter.Maximum);
    }

    private void DrawSevereFatiguePenaltyBoard(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush soft, Brush accent, Brush panel)
    {
        DrawMasterSectionTitle(dc, "SEVERE FATIGUE", accent);
        DrawMasterBundleToggle(dc, P("advanced.fatigueMechanics"),
            "SEVERE FATIGUE MECHANICS", text, muted, border, accent, panel);
        DrawText(dc, "PENALTY EQUALIZER", new Point(18, 102),
            10.6, muted, FontWeights.Bold);

        var sources = new (string Id, string Label, Func<double, string> Format)[]
        {
            ("survival.severeFatigueMaxStamina", "STAMINA LOST", MasterNegativePercent),
            ("survival.severeFatigueCarryWeight", "CARRY LOST", MasterNegativePercent),
            ("survival.severeFatigueRecoil", "RECOIL", MasterPositivePercent),
            ("survival.severeFatigueAimSway", "AIM SWAY", MasterScalar),
            ("survival.severeFatigueDispersion", "SHOT SPREAD", MasterScalar)
        };
        const double gap = 9.0;
        var width = Math.Min(780.0, ActualWidth - 34.0);
        var cellWidth = (width - 4.0 * gap) / 5.0;
        var x0 = (ActualWidth - width) / 2.0;
        for (var i = 0; i < sources.Length; i++)
        {
            var source = sources[i];
            DrawNativeGauge(dc, P(source.Id), source.Label, source.Format,
                new Rect(x0 + i * (cellWidth + gap), 125.0,
                    cellWidth, 236.0),
                text, muted, border, accent, panel);
        }
    }

    // CORE HEALTH — one large, source-bound HP-capacity ruler followed by
    // four independent recovery controls. No invented HP-per-second preview.
    // Max HP is a pool. Other sources control passive recovery, its wait,
    // and separate engine scalars; no item-healing, medkit or invented
    // low-health threshold is duplicated in this core module.
    private void DrawCoreHealthDashboard(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush soft, Brush accent, Brush panel)
    {
        DrawHeader(dc, "CORE HEALTH", "", text, muted, accent);
        var width = Math.Max(1.0, ActualWidth - 32.0);
        const double gap = 12.0;
        var half = (width - gap) / 2.0;

        // The two primary health parameters share one card layout.
        // Only their native units and number precision differ.
        if (P("health.maxHP") is { } hp)
            DrawCoreResourceRuler(dc, hp, "MAX HEALTH", "HP",
                new Rect(16.0, 51.0, half, 105.0),
                text, muted, border, accent, panel);
        if (P("health.baseRegen") is { } passiveRecovery)
            DrawCoreResourceRuler(dc, passiveRecovery, "PASSIVE RECOVERY", "HP/s",
                new Rect(16.0 + half + gap, 51.0, half, 105.0),
                text, muted, border, accent, panel);

        DrawText(dc, "RECOVERY RESPONSE", new Point(19.0, 173.0),
            10.2, muted, FontWeights.Bold);
        var sources = new (string Id, string Label)[]
        {
            ("health.regenDelay", "RECOVERY DELAY"),
            ("health.regenerationScalar", "RECOVERY MULTIPLIER"),
            ("health.lowHealthRegen", "LOW-HP RECOVERY RATE")
        };
        var columns = ActualWidth >= 700.0 ? 3 : 2;
        var cardWidth = (width - (columns - 1) * gap) / columns;
        for (var i = 0; i < sources.Length; i++)
        {
            var row = i / columns;
            var column = i % columns;
            var onRow = Math.Min(columns, sources.Length - row * columns);
            var rowWidth = cardWidth * onRow + gap * (onRow - 1);
            var left = (ActualWidth - rowWidth) / 2.0;
            DrawHorizontalTrackCard(dc, P(sources[i].Id), sources[i].Label,
                new Rect(left + column * (cardWidth + gap),
                    193.0 + row * 83.0, cardWidth, 72.0),
                text, muted, border, accent, panel, hideBaseText: true,
                healthTypography: true);
        }
    }

    // Shared primary resource-ruler grammar for CORE HEALTH and CORE STAMINA.
    // Both use identical card geometry, typography, handles, native endpoints
    // and hit/ref mapping; only the source values and units differ.
    private void DrawCoreResourceRuler(
        DrawingContext dc, ParameterViewModel parameter, string label, string unit,
        Rect card, Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0),
            card, 4.0, 4.0);

        var active = parameter.IsControlEnabled;
        var modified = active && parameter.IsValueModifiedFromReference();
        var value = unit switch
        {
            "HP" or "SP" => $"{parameter.CurrentNumber:0} {unit}",
            "x" => $"{parameter.CurrentNumber:0.##}x",
            "pts/s" => $"{parameter.CurrentNumber:0.000} pts/s",
            "%" => $"{(parameter.CurrentNumber > Epsilon ? "+" : "")}{parameter.CurrentNumber * 100.0:0.#}%",
            _ => FriendlyValue(parameter)
        };
        // Native 0.001-rate precision takes more room than HP/SP, but uses
        // the same card, title/value positions, rail and modified-only gold.
        var valueSize = unit == "pts/s" ? 16.5 : 19.0;
        var valueText = CreateText(value, valueSize, active ? text : muted,
            FontWeights.Bold, Math.Max(1.0, card.Width - 30.0));
        if (modified)
        {
            // Like Stamina Usage, only the edited numeric token turns gold;
            // HP, HP/s, SP and SP/s remain neutral; Base is not emphasized.
            foreach (System.Text.RegularExpressions.Match token in
                System.Text.RegularExpressions.Regex.Matches(
                    value, @"\d+(?:\.\d+)?"))
            {
                valueText.SetForegroundBrush(accent, token.Index, token.Length);
                valueText.SetFontSize(valueSize + 1.2, token.Index, token.Length);
            }
        }

        // Measure the live number first; reserve a real gap for the title
        // instead of letting two independent text draws collide when narrow.
        var titleWidth = Math.Max(1.0,
            card.Width - 30.0 - valueText.WidthIncludingTrailingWhitespace - 10.0);
        dc.DrawText(CreateText(label, 12.0, text, FontWeights.Bold, titleWidth),
            new Point(card.Left + 15.0, card.Top + 12.0));
        dc.DrawText(valueText,
            new Point(card.Right - 15.0 - valueText.WidthIncludingTrailingWhitespace,
                card.Top + 6.0));

        var track = new Rect(card.Left + 18.0, card.Top + 58.0,
            Math.Max(1.0, card.Width - 36.0), 14.0);
        var y = track.Top + track.Height / 2.0;
        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum,
            parameter.Maximum, track, reverse: false);
        var referenceX = ValueToX(parameter.ReferenceNumber, parameter.Minimum,
            parameter.Maximum, track, reverse: false);
        var hovered = active && ReferenceEquals(_hoverTarget?.Parameter, parameter);

        dc.DrawLine(new Pen(border, 5.0),
            new Point(track.Left, y), new Point(track.Right, y));
        if (active)
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.72), 5.0),
                new Point(track.Left, y), new Point(currentX, y));
        // Fatigue Relief keeps the universal gold/neutral ruler; colored
        // underlines are reserved for the drink duration effect markers.
        dc.DrawLine(new Pen(muted, 1.8),
            new Point(referenceX, y - 10.0),
            new Point(referenceX, y + 10.0));
        dc.DrawEllipse(panel, new Pen(active ? accent : muted,
                hovered || IsSelected(parameter) ? 2.6 : 1.8),
            new Point(currentX, y), 7.0, 7.0);
        if (active)
            dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

        AddHorizontalTarget(parameter,
            new Rect(track.Left - 7.0, track.Top - 9.0,
                track.Width + 14.0, track.Height + 18.0),
            track, parameter.Minimum, parameter.Maximum);

        // The shared ruler must render endpoints in the same player-facing
        // units as its current readout, never the underlying 0.2 fraction.
        var minValue = unit is "HP" or "SP"
            ? $"{parameter.Minimum:0}" : FormatCompact(parameter.Minimum);
        var maxValue = unit is "HP" or "SP"
            ? $"{parameter.Maximum:0}" : FormatCompact(parameter.Maximum);
        var leftBound = unit switch
        {
            "x" => $"{minValue}x",
            "pts/s" => $"{parameter.Minimum:0.000} pts/s",
            "%" => $"{parameter.Minimum * 100.0:0.#}%",
            _ => $"{minValue} {unit}"
        };
        var rightBound = unit switch
        {
            "x" => $"{maxValue}x",
            "pts/s" => $"{parameter.Maximum:0.000} pts/s",
            "%" => $"{parameter.Maximum * 100.0:0.#}%",
            _ => $"{maxValue} {unit}"
        };
        DrawText(dc, leftBound,
            new Point(track.Left, card.Bottom - 20.0),
            10.4, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, rightBound,
            new Point(track.Right, card.Bottom - 20.0),
            10.4, muted, FontWeights.Normal);
    }

    // Native-source knob trio. Each dial owns one real CFG parameter, with
    // original range/step and selected-difficulty reference marker. The arc
    // represents its source value, never simulated damage or jam probability.
    private const double WeaponKnobStartAngle = 135.0;
    private const double WeaponKnobSweepAngle = 270.0;
    private const double WeaponKnobRadius = 47.0;

    private void DrawYourWeaponsKnobs(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawHeader(dc, "YOUR WEAPONS", "", text, muted, accent);

        var channels = new (string Id, string Label)[]
        {
            ("combat.playerWeaponDamage", "WEAPON DAMAGE"),
            ("combat.playerHeadshotMultiplier", "HEADSHOT DAMAGE"),
            ("combat.weaponJamming", "JAM FREQUENCY")
        };
        const double gap = 12.0;
        const double margin = 18.0;
        const double cardTop = 49.0;
        const double cardHeight = 181.0;
        var groupWidth = Math.Min(620.0, ActualWidth);
        var groupLeft = (ActualWidth - groupWidth) / 2.0;
        var cellWidth = (groupWidth - 2.0 * margin - 2.0 * gap) / 3.0;
        for (var i = 0; i < channels.Length; i++)
        {
            var card = new Rect(groupLeft + margin + i * (cellWidth + gap),
                cardTop, cellWidth, cardHeight);
            DrawSourceKnob(dc, P(channels[i].Id), channels[i].Label, card,
                text, muted, border, accent, panel,
                prominent: channels[i].Id == "combat.playerHeadshotMultiplier");
        }
    }

    private void DrawSourceKnob(
        DrawingContext dc, ParameterViewModel? parameter, string label, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel,
        bool percentageReadout = false, bool rateReadout = false,
        int percentageSign = 0, bool rawReadout = false,
        string? nativeUnit = null, bool prominent = false)
    {
        // A hero dial (Headshot Damage) is physically larger without inventing
        // a different range, hit grammar or duplicated source control.
        DrawCenteredText(dc, label, new Point(card.Left + card.Width / 2, card.Top + 19),
            prominent ? 10.7 : 10.0,
            parameter?.IsControlEnabled == true ? text : muted, FontWeights.Bold);
        if (parameter is null)
            return;

        var center = new Point(card.Left + card.Width / 2, card.Top + 97.0);
        var radius = Math.Min(WeaponKnobRadius * (prominent ? 1.18 : 1.0),
            (card.Width - 24.0) / 2.0);
        var range = Math.Max(Epsilon, parameter.Maximum - parameter.Minimum);
        var position = Math.Clamp((parameter.CurrentNumber - parameter.Minimum) / range, 0.0, 1.0);
        var reference = Math.Clamp((parameter.ReferenceNumber - parameter.Minimum) / range, 0.0, 1.0);
        var hovered = _hoverTarget?.Kind == HitKind.Knob
            && ReferenceEquals(_hoverTarget.Parameter, parameter);
        var active = parameter.IsControlEnabled;
        var face = CloneWithOpacity(panel, .96);

        // Industrial dial face and a 270-degree graduated rail, leaving the
        // bottom quadrant open so the limits never fight the center readout.
        dc.DrawEllipse(face, new Pen(CloneWithOpacity(border, .72), 1.2),
            center, radius - 10, radius - 10);
        DrawKnobArc(dc, center, radius, WeaponKnobStartAngle, WeaponKnobSweepAngle,
            new Pen(CloneWithOpacity(border, .95), prominent ? 7.0 : 6.0));
        // The ticks must follow the SAME native min + N * TickFrequency
        // grid used by Snap(), not an arbitrary 10-way visual division.
        // A dense 0.01x source has 251 reachable values on a ~220-DIP arc:
        // draw only a legible 1/2/5/10/... subset of real steps, never
        // implied intermediate positions. Wheel, drag and keyboard retain
        // the complete native editing precision independently of tick density.
        var nativeStep = Math.Max(Epsilon, parameter.TickFrequency);
        var nativeIntervals = (int)Math.Floor(range / nativeStep + 0.000001);
        var pixelsPerStep = radius * WeaponKnobSweepAngle * Math.PI / 180.0
            / Math.Max(1.0, range / nativeStep);
        var tickStride = KnobVisibleStepStride(pixelsPerStep);
        var majorStride = tickStride * (tickStride == 1 ? 4 : 5);

        void DrawStepTick(double value, bool major)
        {
            var ratio = Math.Clamp((value - parameter.Minimum) / range, 0.0, 1.0);
            var angle = WeaponKnobStartAngle + ratio * WeaponKnobSweepAngle;
            dc.DrawLine(new Pen(CloneWithOpacity(muted, major ? .75 : .40),
                    major ? 1.6 : 1.0),
                KnobPoint(center, radius + 5.0, angle),
                KnobPoint(center, radius + (major ? 11.0 : 9.0), angle));
        }

        for (var tickIndex = 0; tickIndex <= nativeIntervals; tickIndex += tickStride)
        {
            var value = parameter.Minimum + tickIndex * nativeStep;
            DrawStepTick(value, tickIndex == 0
                || tickIndex % majorStride == 0
                || Math.Abs(value - parameter.Maximum) <= Epsilon);
        }

        // Last native step and the hard upper bound remain visible even if
        // they don't land on a decimated tick. Never invent an off-grid step.
        if (nativeIntervals % tickStride != 0)
            DrawStepTick(parameter.Minimum + nativeIntervals * nativeStep, true);
        if (parameter.Maximum - (parameter.Minimum + nativeIntervals * nativeStep) > Epsilon)
            DrawStepTick(parameter.Maximum, true);

        // Paint the source arc before the muted Base tick so the reference
        // remains visible even where the gold current arc passes across it.
        if (position > Epsilon)
            DrawKnobArc(dc, center, radius, WeaponKnobStartAngle,
                position * WeaponKnobSweepAngle,
                new Pen(CloneWithOpacity(active ? accent : muted, active ? .94 : .35),
                    prominent ? 7.0 : 6.0));
        var baselineAngle = WeaponKnobStartAngle + WeaponKnobSweepAngle * reference;
        dc.DrawLine(new Pen(muted, 2.4),
            KnobPoint(center, radius - 7, baselineAngle),
            KnobPoint(center, radius + 6, baselineAngle));

        var angleNow = WeaponKnobStartAngle + position * WeaponKnobSweepAngle;
        if (hovered)
            dc.DrawEllipse(CloneWithOpacity(accent, .13), null,
                center, radius - 11, radius - 11);
        dc.DrawLine(new Pen(hovered ? Brushes.White : active ? accent : muted,
                hovered ? 3.0 : 2.4),
            KnobPoint(center, radius - 8, angleNow),
            KnobPoint(center, radius + 3, angleNow));

        var modified = active && parameter.IsValueModifiedFromReference();
        string Percent(double value) =>
            $"{(percentageSign > 0 ? "+" : percentageSign < 0 ? "−" : "")}{value * 100.0:0.#}%";
        // Source-native rates and medicine amounts display the actual stat
        // value plus its physical/gameplay unit, never a fictitious 1x scalar.
        // A two-line center keeps both readable inside the compact dial.
        var currentLabel = rateReadout ? $"{parameter.CurrentNumber:0.00}"
            : percentageReadout ? Percent(parameter.CurrentNumber)
            : rawReadout
                ? $"{(nativeUnit is not null && parameter.CurrentNumber > Epsilon ? "+" : "")}{parameter.CurrentNumber:0.#}"
                : $"{parameter.CurrentNumber:0.##}x";
        // WPF draws at the text's top edge: center every source-backed dial
        // readout, including single-line multiplier/percentage and signed
        // percentage values. Two-line amount/unit or rate/unit pairs share
        // the same centered positions without per-family call-site offsets.
        var currentTop = center.Y - (rateReadout || nativeUnit is not null ? 15.0 : 8.0);
        if (percentageSign != 0)
        {
            // In the two Master signed-percentage dials, only the editable
            // numeric token becomes gold; +/- and % remain neutral.
            var formatted = CreateText(currentLabel, modified ? 16.2 : 15.0,
                active ? text : muted, FontWeights.Bold, card.Width - 12.0);
            if (modified)
            {
                foreach (System.Text.RegularExpressions.Match token in
                    System.Text.RegularExpressions.Regex.Matches(
                        currentLabel, @"\d+(?:\.\d+)?"))
                    formatted.SetForegroundBrush(accent, token.Index, token.Length);
            }
            dc.DrawText(formatted, new Point(
                center.X - formatted.WidthIncludingTrailingWhitespace / 2.0,
                currentTop));
        }
        else
        {
            DrawCenteredText(dc, currentLabel, new Point(center.X, currentTop),
                prominent ? (modified ? 18.2 : 17.0) : (modified ? 16.2 : 15.0),
                active ? modified ? accent : text : muted, FontWeights.Bold);
        }
        if (rateReadout)
            DrawCenteredText(dc, "PTS/S",
                new Point(center.X, center.Y + 5.0),
                9.0, muted, FontWeights.SemiBold);
        else if (nativeUnit is not null)
            DrawCenteredText(dc, nativeUnit,
                new Point(center.X, center.Y + 5.0),
                8.3, muted, FontWeights.SemiBold);
        DrawCenteredText(dc,
            rateReadout ? $"{parameter.Minimum:0.00}"
                : percentageReadout ? Percent(parameter.Minimum)
                : rawReadout ? $"{parameter.Minimum:0.#}"
                : $"{parameter.Minimum:0.##}x",
            new Point(center.X - radius * .80, card.Bottom - 19.0),
            9.0, muted, FontWeights.SemiBold);
        DrawCenteredText(dc,
            rateReadout ? $"{parameter.Maximum:0.00}"
                : percentageReadout ? Percent(parameter.Maximum)
                : rawReadout ? $"{parameter.Maximum:0.#}"
                : $"{parameter.Maximum:0.##}x",
            new Point(center.X + radius * .80, card.Bottom - 19.0),
            9.0, muted, FontWeights.SemiBold);

        if (!active)
            return;

        var hit = new Rect(center.X - radius - 5, center.Y - radius - 5,
            2.0 * (radius + 5), 2.0 * (radius + 5));
        _hitTargets.Add(new HitTarget(parameter, hit, hit, HitKind.Knob,
            parameter.Minimum, parameter.Maximum, false));
    }

    // Readability-only decimation of the physical marks: all rendered ticks
    // still correspond to an integer number of source-native steps.
    // For example: 0..3 in 0.25x -> stride 1 (13 marks);
    // 0..2.5 in 0.01x -> stride 10 (0.10x marks, 0.01x input).
    private static int KnobVisibleStepStride(double pixelsPerStep)
    {
        const double minimumTickSpacing = 6.5;
        var required = minimumTickSpacing / Math.Max(Epsilon, pixelsPerStep);
        var scale = 1;
        while (scale < 100000 && required > 10.0 * scale)
            scale *= 10;
        if (required <= scale)
            return scale;
        if (required <= scale * 2)
            return scale * 2;
        if (required <= scale * 5)
            return scale * 5;
        return scale * 10;
    }

    private static Point KnobPoint(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180.0;
        return new Point(center.X + radius * Math.Cos(radians),
            center.Y + radius * Math.Sin(radians));
    }

    private static void DrawKnobArc(
        DrawingContext dc, Point center, double radius, double startAngle,
        double sweep, Pen pen)
    {
        if (sweep <= Epsilon)
            return;

        var figure = new PathFigure
        {
            StartPoint = KnobPoint(center, radius, startAngle),
            IsClosed = false,
            IsFilled = false
        };
        figure.Segments.Add(new ArcSegment(
            KnobPoint(center, radius, startAngle + sweep),
            new Size(radius, radius), 0.0,
            sweep > 180.0, SweepDirection.Clockwise, true));
        dc.DrawGeometry(null, pen, new PathGeometry(new[] { figure }));
    }

    // Two independently editable native CFG axes plus two more distinct controls.
    // The crosshair is a visual pairing, not an effective DPS, TTK or hit-rate model.
    private static void DrawHumanAxisHandle(
        DrawingContext dc, Point center, Brush accent, Brush panel, bool hovered, bool selected)
    {
        // The X, Y and joint handles intentionally share one visual language.
        // Draw the halo outside the ring so the current value remains legible.
        ControlHoverGlow.Draw(dc, center,
            Application.Current?.TryFindResource("AccentBrush") as Brush ?? accent,
            hovered);
        dc.DrawEllipse(panel, new Pen(accent, hovered || selected ? 3.0 : 1.8),
            center, 7.0, 7.0);
        dc.DrawEllipse(accent, null, center, 2.6, 2.6);
    }

    private void DrawHumanCombatProfile(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "HUMAN COMBAT PROFILE", "", text, muted, accent);

        var health = P("combat.npcHealth");
        var damage = P("combat.npcWeaponDamage");
        var frequency = P("combat.npcAttackCooldown");
        var accuracy = P("combat.npcHipAccuracy");
        var explosion = P("combat.explosionDamage");
        if (health is null || damage is null || frequency is null || accuracy is null || explosion is null)
        {
            DrawCenteredText(dc, "Human combat data unavailable.",
                new Point(ActualWidth / 2.0, 165.0), 10.0, muted, FontWeights.SemiBold);
            return;
        }

        // Tactical Control Console: bounded enemy profile and a compact
        // three-knob attack bank. The 2D plot still edits the native X/Y CFGs.
        var wide = HumanCombatWide(ActualWidth);
        const double profileWidth = 300.0;
        var bankWidth = Math.Min(802.0, Math.Max(1.0, ActualWidth - 26.0));
        var bankLeft = (ActualWidth - bankWidth) / 2.0;
        var profileLeft = wide ? bankLeft : (ActualWidth - profileWidth) / 2.0;

        DrawCenteredText(dc, "ENEMY PROFILE",
            new Point(profileLeft + profileWidth / 2.0, 46.0),
            10.0, muted, FontWeights.Bold);
        DrawHumanCombatReadout(dc, health, "HEALTH",
            profileLeft + profileWidth * .25, text, muted, accent);
        DrawHumanCombatReadout(dc, damage, "GUN DAMAGE",
            profileLeft + profileWidth * .75, text, muted, accent);

        const double plotSide = 191.0;
        var plot = new Rect(profileLeft + (profileWidth - plotSide) / 2.0 + 9.0,
            118.0, plotSide, plotSide);
        DrawCenteredText(dc, "BALANCE EDITOR",
            new Point(profileLeft + profileWidth / 2.0, 102.0),
            9.7, muted, FontWeights.SemiBold);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), plot, 4.0, 4.0);

        // Keep axis names and real source ticks. The profile readouts explain
        // the current values, not another editable setting.
        DrawCenteredText(dc, "ENEMY HEALTH",
            new Point(plot.Left + plot.Width / 2.0, plot.Bottom + 29.0),
            10.0, text, FontWeights.SemiBold);
        var axisX = plot.Left - 61.0;
        var axisY = plot.Top + plot.Height / 2.0;
        var yAxisLabel = CreateText("ENEMY GUN DAMAGE",
            9.7, text, FontWeights.SemiBold);
        dc.PushTransform(new RotateTransform(-90.0, axisX, axisY));
        dc.DrawText(yAxisLabel, new Point(
            axisX - yAxisLabel.WidthIncludingTrailingWhitespace / 2.0,
            axisY - yAxisLabel.Height / 2.0));
        dc.Pop();

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
                    new Point(gridX, plot.Bottom + 13.0), 9.3, muted, FontWeights.Normal);
                DrawRightAlignedText(dc, $"{damageTick:0.##}x",
                    new Point(plot.Left - 13.0, gridY - 7.0), 9.3, muted, FontWeights.Normal);
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
        DrawHumanAxisHandle(dc, new Point(healthX, damageY), accent, panel,
            _hoverTarget?.Kind == HitKind.HumanJoint,
            _selectedHitKind == HitKind.HumanJoint);

        // Axis handles modify their own source; the joint ring modifies both.
        // Empty chart space stays noninteractive.
        var healthHandle = new Point(healthX, plot.Bottom + 10.0);
        DrawHumanAxisHandle(dc, healthHandle, accent, panel,
            _hoverTarget?.Kind == HitKind.Horizontal && ReferenceEquals(_hoverTarget.Parameter, health),
            IsSelected(health) && _selectedHitKind == HitKind.Horizontal);
        AddHorizontalTarget(health, new Rect(healthX - 17.0, plot.Bottom + 1.0, 34.0, 22.0),
            plot, health.Minimum, health.Maximum);

        var damageHandle = new Point(plot.Left - 11.0, damageY);
        DrawHumanAxisHandle(dc, damageHandle, accent, panel,
            _hoverTarget?.Kind == HitKind.Vertical && ReferenceEquals(_hoverTarget.Parameter, damage),
            IsSelected(damage) && _selectedHitKind == HitKind.Vertical);
        AddVerticalTarget(damage, new Rect(plot.Left - 28.0, damageY - 14.0, 27.0, 28.0),
            plot, damage.Minimum, damage.Maximum);

        if (health.IsControlEnabled && damage.IsControlEnabled)
            _hitTargets.Add(new HitTarget(health,
                new Rect(healthX - 18.0, damageY - 18.0, 36.0, 36.0),
                plot, HitKind.HumanJoint, health.Minimum, health.Maximum, false));

        // Real native multipliers, not synthetic combined stats. All three
        // controls reuse the project-wide 270-degree source-knob grammar.
        var knobs = new (ParameterViewModel Source, string Label)[]
        {
            (frequency, "ATTACK FREQUENCY"),
            (accuracy, "FIRE ACCURACY"),
            (explosion, "EXPLOSION DAMAGE")
        };
        const double knobGap = 10.0;
        var knobBankLeft = wide ? profileLeft + profileWidth + 18.0 : bankLeft;
        var knobBankWidth = wide ? bankLeft + bankWidth - knobBankLeft : bankWidth;
        var knobWidth = Math.Min(170.0, (knobBankWidth - 2.0 * knobGap) / 3.0);
        var rowWidth = 3.0 * knobWidth + 2.0 * knobGap;
        var rowLeft = knobBankLeft + (knobBankWidth - rowWidth) / 2.0;
        var knobTop = wide ? 94.0 : 389.0;

        DrawCenteredText(dc, "OFFENSIVE PRESSURE",
            new Point(knobBankLeft + knobBankWidth / 2.0, wide ? 46.0 : 367.0),
            10.0, muted, FontWeights.Bold);
        for (var i = 0; i < knobs.Length; i++)
            DrawSourceKnob(dc, knobs[i].Source, knobs[i].Label,
                new Rect(rowLeft + i * (knobWidth + knobGap), knobTop,
                    knobWidth, 181.0),
                text, muted, border, accent, panel);
    }

    private static bool HumanCombatWide(double width) => width >= 790.0;

    // Match the responsive knob placement exactly: wide is a paired console,
    // narrow stacks the three knobs BELOW the axis-labeled profile.
    private static double HumanCombatBoardHeight(double width)
        => HumanCombatWide(width) ? 366.0 : 589.0;

    private void DrawHumanCombatReadout(
        DrawingContext dc, ParameterViewModel parameter, string label,
        double centerX, Brush text, Brush muted, Brush accent)
    {
        DrawCenteredText(dc, label, new Point(centerX, 68.0),
            9.3, muted, FontWeights.Bold);
        var value = $"{parameter.CurrentNumber:0.##}x";
        var active = parameter.IsControlEnabled;
        var formatted = CreateText(value, 16.0, active ? text : muted,
            FontWeights.Bold);
        // Per-value modification grammar: highlight only digits, not the unit.
        if (active && parameter.IsValueModifiedFromReference())
            formatted.SetForegroundBrush(accent, 0, value.Length - 1);
        dc.DrawText(formatted, new Point(
            centerX - formatted.WidthIncludingTrailingWhitespace / 2.0, 83.0));
    }

    // Editable axes are deliberately independent. The gold crosshair is a
    // read-only summary of the two source-exact Psy spawn override multipliers.
    // Do not imply a simulated spawns-per-minute formula: base encounter timings
    // and the effective runtime paths are not fully exposed by the CFG dump.
    private void DrawPsyPhantomPressure(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush soft, Brush accent, Brush panel)
    {
        DrawHeader(dc, "PSY PHANTOM PRESSURE", "", text, muted, accent);
        var channels = new (string Id, string Label)[]
        {
            ("combat.psySpawnCycleDelay", "SPAWN DELAY"),
            ("combat.psyPhantomCount", "PHANTOM COUNT"),
            ("combat.psyDurabilityDamage", "GEAR DAMAGE")
        };
        const double gap = 12.0;
        var knobWidth = Math.Min(194.0, (ActualWidth - 34.0 - 2 * gap) / 3.0);
        var width = 3 * knobWidth + 2 * gap;
        var left = (ActualWidth - width) / 2.0;
        for (var i = 0; i < channels.Length; i++)
            DrawSourceKnob(dc, P(channels[i].Id), channels[i].Label,
                new Rect(left + i * (knobWidth + gap), 49.0, knobWidth, 181.0),
                text, muted, border, accent, panel);

        // Diagram is illustrative of wait/count intensity, NOT spawn-rate prediction.
        var wait = P("combat.psySpawnCycleDelay");
        var count = P("combat.psyPhantomCount");
        if (wait is not null && count is not null)
        {
            DrawText(dc, "SPAWN CYCLE / RELATIVE PRESSURE",
                new Point(left + 6.0, 249.0), 10.0, muted, FontWeights.Bold);
            var waitLevel = (wait.CurrentNumber - wait.Minimum)
                / Math.Max(Epsilon, wait.Maximum - wait.Minimum);
            var countLevel = (count.CurrentNumber - count.Minimum)
                / Math.Max(Epsilon, count.Maximum - count.Minimum);
            var preview = new Rect(left + 6.0, 271.0, width - 12.0, 45.0);
            dc.DrawRoundedRectangle(CloneWithOpacity(panel, .32),
                new Pen(CloneWithOpacity(border, .6), .8), preview, 3, 3);
            for (var i = 0; i < 12; i++)
            {
                var x = preview.Left + 8 + i * (preview.Width - 16) / 12.0;
                var isCycle = i % Math.Max(1, 1 + (int)Math.Round(waitLevel * 4)) == 0;
                var h = isCycle ? 8.0 + countLevel * 25.0 : 4.0;
                dc.DrawRectangle(isCycle ? accent : CloneWithOpacity(muted, .22),
                    null, new Rect(x, preview.Bottom - h - 5.0, 12.0, h));
            }
        }
        var bleed = P("combat.psyBleeding");
        if (bleed is not null)
            DrawTogglePill(dc, bleed, "PHANTOM BLEEDING",
                new Rect((ActualWidth - 194.0) / 2.0, 330.0, 194.0, 36.0),
                text, muted, border, accent, panel);
    }

    private static double EquipmentWearPlotSide(double width)
        => Math.Min(240.0, Math.Max(200.0, width - 360.0));

    private static double EquipmentWearBoardHeight(double width)
        => 86.0 + EquipmentWearPlotSide(width) + 61.0;

    private void DrawEquipmentWearBalance(
        DrawingContext dc, Brush text, Brush muted, Brush border, Brush soft,
        Brush accent, Brush panel)
    {
        DrawHeader(dc, "EQUIPMENT WEAR", "", text, muted, accent);
        var channels = new (string Id, string Label)[]
        {
            ("equipment.armorDurability", "ARMOR DURABILITY"),
            ("equipment.weaponWearPerShot", "WEAR PER SHOT")
        };
        const double knobWidth = 194.0;
        const double gap = 12.0;
        var left = (ActualWidth - (2 * knobWidth + gap)) / 2.0;
        for (var i = 0; i < channels.Length; i++)
            DrawSourceKnob(dc, P(channels[i].Id), channels[i].Label,
                new Rect(left + i * (knobWidth + gap), 49.0, knobWidth, 181.0),
                text, muted, border, accent, panel);
    }

    private void DrawRepairCurve(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawHeader(dc, "REPAIR PRICE BY REPUTATION", "", text, muted, accent);
        var channels = new (string Id, string Label)[]
        {
            ("economy.repairFriend", "FRIENDLY"),
            ("economy.repairNeutral", "NEUTRAL"),
            ("economy.repairDisaffection", "DISLIKED"),
            ("economy.repairEnemy", "HOSTILE")
        };
        var bank = new Rect((ActualWidth - Math.Min(720.0, ActualWidth - 34.0)) / 2.0,
            Math.Max(52.0, (ActualHeight - 259.0) / 2.0),
            Math.Min(720.0, ActualWidth - 34.0), 259.0);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.65),
            new Pen(border, 0.9), bank, 4, 4);
        var plot = new Rect(bank.Left + 56, bank.Top + 20,
            bank.Width - 84, 157.0);
        const double sharedTop = 3.0;
        for (var tick = 0; tick <= 3; tick++)
        {
            var y = ValueToY(tick, 0.0, sharedTop, plot);
            dc.DrawLine(new Pen(CloneWithOpacity(muted, tick == 0 ? .5 : .23), 0.9)
                { DashStyle = DashStyles.Dot },
                new Point(plot.Left, y), new Point(plot.Right, y));
            DrawRightAlignedText(dc, $"{tick}x",
                new Point(plot.Left - 11, y - 7.0),
                9.3, muted, FontWeights.SemiBold);
        }

        var currentPoints = new List<Point>();
        var basePoints = new List<Point>();
        for (var i = 0; i < channels.Length; i++)
        {
            var parameter = P(channels[i].Id);
            if (parameter is null)
                return;
            var x = plot.Left + (i + 0.5) * plot.Width / channels.Length;
            var y = ValueToY(parameter.CurrentNumber, 0, sharedTop, plot);
            var referenceY = ValueToY(parameter.ReferenceNumber, 0, sharedTop, plot);
            currentPoints.Add(new Point(x, y));
            basePoints.Add(new Point(x, referenceY));
            // Shared 0–3x ruler describes *display position*, not the
            // per-source upper edit limit (Friendly/Neutral/Disliked cap at 2).
            var sourceCeiling = ValueToY(parameter.Maximum, 0, sharedTop, plot);
            dc.DrawLine(new Pen(CloneWithOpacity(muted, .65), 2.0),
                new Point(x - 7, sourceCeiling), new Point(x + 7, sourceCeiling));
            DrawMixerCentered(dc, channels[i].Label,
                new Point(x, plot.Bottom + 13.0),
                plot.Width / 4.0 - 10.0, 9.7, text, FontWeights.Bold);
            DrawMixerCentered(dc, $"{parameter.CurrentNumber:0.##}x",
                new Point(x, plot.Bottom + 37.0),
                plot.Width / 4.0 - 10.0,
                parameter.IsValueModifiedFromReference() ? 12.3 : 11.1,
                parameter.IsValueModifiedFromReference() ? accent : text,
                FontWeights.Bold);
        }

        var basePen = new Pen(CloneWithOpacity(muted, .70), 1.3)
            { DashStyle = DashStyles.Dash };
        var currentPen = new Pen(CloneWithOpacity(accent, .90), 2.7);
        for (var i = 1; i < currentPoints.Count; i++)
        {
            dc.DrawLine(basePen, basePoints[i - 1], basePoints[i]);
            dc.DrawLine(currentPen, currentPoints[i - 1], currentPoints[i]);
        }
        for (var i = 0; i < currentPoints.Count; i++)
        {
            var parameter = P(channels[i].Id)!;
            var pt = currentPoints[i];
            dc.DrawEllipse(panel, new Pen(muted, 1.4), basePoints[i], 4.0, 4.0);
            var hover = _hoverTarget?.Kind == HitKind.Vertical
                && ReferenceEquals(_hoverTarget.Parameter, parameter);
            dc.DrawEllipse(panel, new Pen(hover ? Brushes.White : accent,
                hover ? 2.9 : 2.1), pt, hover ? 7.5 : 6.5, hover ? 7.5 : 6.5);
            dc.DrawEllipse(accent, null, pt, 2.5, 2.5);
            AddVerticalTarget(parameter,
                new Rect(pt.X - 25.0, plot.Top - 4.0, 50.0, plot.Height + 8.0),
                new Rect(pt.X - 1.0, plot.Top, 2.0, plot.Height), 0.0, sharedTop);
        }
    }

    // Colored underlines are reserved for native WATER / ENERGY DRINK
    // timed stamina effects. Fatigue relief, alcohol, medicine, instant
    // stamina gain and generic stamina controls keep standard gold rails.
    private static readonly Brush WaterEffectLineBrush =
        new SolidColorBrush(Color.FromRgb(91, 173, 98));
    // Two game-inspired blues distinguish Energy Drink's independently
    // editable stamina-cost effects in both timeline and compact sliders.
    // Full (100%) cost cut is brighter; partial (30%) is deeper blue.
    private static readonly Brush EnergyDrinkEffectLineBrush =
        new SolidColorBrush(Color.FromRgb(92, 186, 243));
    private static readonly Brush EnergyDrinkPartialCostLineBrush =
        new SolidColorBrush(Color.FromRgb(58, 111, 180));

    private static Brush? DrinkEffectLineBrush(string sourceId) => sourceId switch
    {
        "survival.waterEffectDuration" or "survival.waterRegenDuration"
            => WaterEffectLineBrush,
        "survival.energyPartialCostDuration" => EnergyDrinkPartialCostLineBrush,
        "survival.energyDrinkEffectDuration" or "survival.energyFullCostDuration"
            => EnergyDrinkEffectLineBrush,
        _ => null
    };

    // Purely visual active-value fill beneath the existing source slider.
    // Do not add a second hit target or interpret this as another CFG value.
    private static void DrawDrinkEffectLine(
        DrawingContext dc, ParameterViewModel parameter, double startX,
        double effectMarkerX, double mainY, double offset = 9.0)
    {
        var brush = DrinkEffectLineBrush(parameter.Id);
        if (brush is null || !parameter.IsControlEnabled || effectMarkerX <= startX)
            return;
        dc.DrawLine(new Pen(brush, 2.4),
            new Point(startX, mainY + offset),
            new Point(effectMarkerX, mainY + offset));
    }

    // Drink strength knobs sit above the combined eight-lane duration graph;
    // three independent medicine strength knobs sit beneath it.
    private const double ConsumableStrengthKnobTop = 53.0;
    private const double ConsumableDurationTop = 250.0;

    private void DrawConsumableMixer(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var burst = P("survival.waterInstantStaminaRestore");
        var energyBurst = P("survival.energyDrinkInstantStaminaRestore");
        var waterStamina = P("survival.waterStaminaEffectStrength");
        var energyStamina = P("survival.energyDrinkStaminaRecoveryStrength");
        var waterTime = P("survival.waterEffectDuration");
        var energyTime = P("survival.energyDrinkEffectDuration");
        var vodkaTime = P("survival.vodkaEffectDuration");
        var beerTime = P("survival.beerEffectDuration");
        var magicTime = P("survival.magicVodkaEffectDuration");
        var waterRegenTime = P("survival.waterRegenDuration");
        var energyFullCostTime = P("survival.energyFullCostDuration");
        var energyPartialCostTime = P("survival.energyPartialCostDuration");
        var vincaStrength = P("survival.vincaBleedRecovery");
        var herculesStrength = P("survival.herculesCarryBonus");
        var psiStrength = P("survival.psiBlockRecovery");
        var vincaTime = P("survival.vincaDuration");
        var herculesTime = P("survival.herculesDuration");
        var psiTime = P("survival.psiBlockDuration");

        DrawHeader(dc, "CONSUMABLE EFFECTS",
            string.Empty, text, muted, accent);

        if (burst is null || energyBurst is null || waterStamina is null ||
            energyStamina is null ||
            waterTime is null || energyTime is null ||
            vodkaTime is null || beerTime is null || magicTime is null ||
            waterRegenTime is null || energyFullCostTime is null ||
            energyPartialCostTime is null ||
            vincaStrength is null || herculesStrength is null ||
            psiStrength is null || vincaTime is null ||
            herculesTime is null || psiTime is null)
        {
            DrawCenteredText(dc, "Consumable effect data unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var width = Math.Max(460.0, ActualWidth - 34.0);

        // Two independent absolute-percent instant refills beside the two
        // existing buff multipliers. Compact, native 270-degree rotary grammar.
        const double knobGap = 10.0;
        var knobBankWidth = Math.Min(720.0, ActualWidth - 34.0);
        var knobWidth = (knobBankWidth - 3.0 * knobGap) / 4.0;
        var knobLeft = (ActualWidth - knobBankWidth) / 2.0;
        DrawSourceKnob(dc, burst, "WATER STAMINA GAIN",
            new Rect(knobLeft, ConsumableStrengthKnobTop, knobWidth, 181.0),
            text, muted, border, accent, panel, percentageReadout: true);
        DrawSourceKnob(dc, energyBurst, "ENERGY DRINK STAMINA GAIN",
            new Rect(knobLeft + knobWidth + knobGap, ConsumableStrengthKnobTop,
                knobWidth, 181.0),
            text, muted, border, accent, panel, percentageReadout: true);
        DrawSourceKnob(dc, waterStamina, "WATER STAMINA BUFF",
            new Rect(knobLeft + 2.0 * (knobWidth + knobGap),
                ConsumableStrengthKnobTop, knobWidth, 181.0),
            text, muted, border, accent, panel);
        DrawSourceKnob(dc, energyStamina, "ENERGY DRINK REGEN",
            new Rect(knobLeft + 3.0 * (knobWidth + knobGap),
                ConsumableStrengthKnobTop, knobWidth, 181.0),
            text, muted, border, accent, panel);

        var previewTop = ConsumableDurationTop;

        // One seconds-space editor: five drink multipliers and three direct
        // medicine durations, each with its correct native seconds mapping.
        var preview = new Rect(17.0, previewTop, width, ConsumableTimelineHeight(ActualWidth));
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), preview, 4.0, 4.0);
        DrawText(dc, "EFFECT DURATION  /  SECONDS",
            new Point(preview.Left + 12.0, previewTop + 8.0),
            11.0, text, FontWeights.Bold);
        // This is a single UI range override, not a gameplay CFG toggle.
        // Keep the existing universal 20-DIP checkbox in a compact header cell.
        var overrideRect = new Rect(preview.Right - 190.0, previewTop + 6.0, 178.0, 28.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), overrideRect, 4.0, 4.0);
        var checkboxRect = new Rect(
            overrideRect.Right - BooleanCheckboxChrome.Size - 8.0,
            overrideRect.Top + (overrideRect.Height - BooleanCheckboxChrome.Size) / 2.0,
            BooleanCheckboxChrome.Size, BooleanCheckboxChrome.Size);
        var overrideHovered = _hoverTarget?.Kind == HitKind.DrinkDurationOverride;
        BooleanCheckboxChrome.Draw(dc, checkboxRect, burst.DrinkDurationOverrideEnabled,
            true, overrideHovered, panel, border, accent);
        dc.DrawText(CreateText("OVERRIDE LIMITS", 9.7, text, FontWeights.Bold,
            Math.Max(1.0, checkboxRect.Left - overrideRect.Left - 16.0)),
            new Point(overrideRect.Left + 9.0, overrideRect.Top + 7.0));
        _hitTargets.Add(new HitTarget(burst, overrideRect, overrideRect,
            HitKind.DrinkDurationOverride, 0.0, 1.0, false));

        // The checkbox takes its own header row; axis labels sit underneath it
        // instead of crossing the checkbox and the effect title.
        var timeline = new Rect(preview.Left + 112.0, previewTop + 67.0,
            Math.Max(160.0, preview.Width - 207.0), 226.0);
        const double maxSeconds = 270.0;
        double SecondsX(double seconds) => timeline.Left + timeline.Width *
            Math.Clamp(seconds / maxSeconds, 0.0, 1.0);

        foreach (var seconds in new[] { 0.0, 90.0, 180.0, 270.0 })
        {
            var x = SecondsX(seconds);
            dc.DrawLine(new Pen(border, 1.0) { DashStyle = DashStyles.Dot },
                new Point(x, timeline.Top - 11.0),
                new Point(x, timeline.Top + 111.0));
            DrawCenteredText(dc, $"{seconds:0}s",
                new Point(x, timeline.Top - 20.0),
                9.6, muted, FontWeights.SemiBold);
        }

        var sources = new[] { waterTime, energyTime, vodkaTime, beerTime, magicTime };
        var names = new[] { "WATER", "ENERGY DRINK", "VODKA", "BEER", "MAGIC VODKA" };
        var durations = new[] { 30.0, 45.0, 20.0, 20.0, 90.0 };
        // Three native shorter effect durations now have their own explicit
        // source controls. Small timeline ticks are visual landmarks only;
        // edits happen in the compact native-duration cards directly below.
        var secondary = new ParameterViewModel[][]
        {
            new[] { waterRegenTime },
            new[] { energyFullCostTime, energyPartialCostTime },
            Array.Empty<ParameterViewModel>(),
            Array.Empty<ParameterViewModel>(),
            Array.Empty<ParameterViewModel>()
        };
        for (var i = 0; i < sources.Length; i++)
        {
            var parameter = sources[i];
            var y = timeline.Top + 8.0 + i * 23.0;
            var currentX = SecondsX(durations[i] * parameter.CurrentNumber);
            var referenceX = SecondsX(durations[i] * parameter.ReferenceNumber);
            var maximumX = SecondsX(durations[i] * parameter.Maximum);
            // The main Water/Energy source minima are now the shortest
            // native effect required by their independently edited children.
            // Map the hit lane from its real editable starting point.
            var minimumX = SecondsX(durations[i] * parameter.Minimum);
            var track = new Rect(minimumX, y - 6.0,
                Math.Max(1.0, maximumX - minimumX), 12.0);

            DrawRightAlignedText(dc, names[i],
                new Point(timeline.Left - 13.0, y - 8.0),
                10.4, text, FontWeights.Bold);
            // Any grey tail after this source's own max is not editable.
            dc.DrawLine(new Pen(CloneWithOpacity(border, 0.4), 2.0),
                new Point(timeline.Left, y), new Point(timeline.Right, y));
            dc.DrawLine(new Pen(border, 3.0),
                new Point(timeline.Left, y), new Point(maximumX, y));
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.75), 4.0),
                new Point(timeline.Left, y), new Point(currentX, y));
            // The colored underlines show the actual child-effect windows,
            // not the drink's longer parent duration. Water has one effect
            // marker; Energy Drink has two independent cost-cut markers.
            for (var j = 0; j < secondary[i].Length; j++)
            {
                var shortEffect = secondary[i][j];
                var secondaryX = SecondsX(shortEffect.CurrentNumber);
                // Two separate, slim tracks keep Energy Drink's two effects
                // distinct without adding another slider or legend.
                var effectOffset = secondary[i].Length == 2
                    ? 7.0 + j * 4.0 : 9.0;
                DrawDrinkEffectLine(dc, shortEffect, timeline.Left,
                    secondaryX, y, effectOffset);
                dc.DrawLine(new Pen(CloneWithOpacity(muted, 0.8), 1.7),
                    new Point(secondaryX, y - 6.0),
                    new Point(secondaryX, y + 6.0));
            }
            dc.DrawLine(new Pen(muted, 1.7),
                new Point(referenceX, y - 8.0), new Point(referenceX, y + 8.0));
            dc.DrawEllipse(panel,
                new Pen(accent, IsSelected(parameter) ? 2.5 : 1.6),
                new Point(currentX, y), 6.0, 6.0);
            dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

            var modified = parameter.IsValueModifiedFromReference();
            DrawRightAlignedText(dc,
                $"{durations[i] * parameter.CurrentNumber:0.#}s",
                new Point(preview.Right - 12.0, y - 9.0),
                modified ? 12.2 : 11.0, modified ? accent : text,
                FontWeights.Bold);

            // Keep native source snapping and right-click/R reset. The hit
            // lane spans the chart, while the native editor clamps at max.
            AddHorizontalTarget(parameter,
                new Rect(track.Left - 6.0, y - 10.0,
                    track.Width + 12.0, 20.0),
                track, parameter.Minimum, parameter.Maximum);
        }

        // Medicine always uses its own 0..900s clock; Override Limits changes
        // each source's editable ceiling, not this axis or the drink clock.
        const double medicineMaximumSeconds = 900.0;
        double MedicineX(double seconds) => timeline.Left + timeline.Width *
            Math.Clamp(seconds / medicineMaximumSeconds, 0.0, 1.0);
        var medicineAxisY = timeline.Top + 131.0;
        var medicineFirstY = timeline.Top + 158.0;
        foreach (var seconds in new[] { 0.0, medicineMaximumSeconds / 3.0,
            medicineMaximumSeconds * 2.0 / 3.0, medicineMaximumSeconds })
        {
            var x = MedicineX(seconds);
            dc.DrawLine(new Pen(border, 1.0) { DashStyle = DashStyles.Dot },
                new Point(x, medicineFirstY - 10.0),
                new Point(x, medicineFirstY + 2.0 * 23.0 + 9.0));
            DrawCenteredText(dc, $"{seconds:0}s",
                new Point(x, medicineAxisY),
                9.6, muted, FontWeights.SemiBold);
        }

        var medicineLanes = new (ParameterViewModel Source, string Label)[]
        {
            (vincaTime, "VINCA"),
            (herculesTime, "HERCULES"),
            (psiTime, "PSI BLOCK")
        };
        for (var i = 0; i < medicineLanes.Length; i++)
        {
            var (source, label) = medicineLanes[i];
            var y = medicineFirstY + i * 23.0;
            var currentX = MedicineX(source.CurrentNumber);
            var referenceX = MedicineX(source.ReferenceNumber);
            var minimumX = MedicineX(source.Minimum);
            var maximumX = MedicineX(source.Maximum);
            var track = new Rect(minimumX, y - 6.0,
                Math.Max(1.0, maximumX - minimumX), 12.0);

            DrawRightAlignedText(dc, label,
                new Point(timeline.Left - 13.0, y - 8.0),
                10.4, text, FontWeights.Bold);
            dc.DrawLine(new Pen(CloneWithOpacity(border, 0.4), 2.0),
                new Point(timeline.Left, y), new Point(timeline.Right, y));
            dc.DrawLine(new Pen(border, 3.0),
                new Point(timeline.Left, y), new Point(maximumX, y));
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.75), 4.0),
                new Point(timeline.Left, y), new Point(currentX, y));
            dc.DrawLine(new Pen(muted, 1.7),
                new Point(referenceX, y - 8.0),
                new Point(referenceX, y + 8.0));
            dc.DrawEllipse(panel, new Pen(accent,
                IsSelected(source) ? 2.5 : 1.6),
                new Point(currentX, y), 6.0, 6.0);
            dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

            var modified = source.IsValueModifiedFromReference();
            DrawRightAlignedText(dc, $"{source.CurrentNumber:0}s",
                new Point(preview.Right - 12.0, y - 9.0),
                modified ? 12.2 : 11.0,
                modified ? accent : text, FontWeights.Bold);
            AddHorizontalTarget(source,
                new Rect(track.Left - 6.0, y - 10.0,
                    track.Width + 12.0, 20.0),
                track, source.Minimum, source.Maximum);
        }

        // Independent, compact, source-exact thresholds. Each child ranges
        // from the original native duration to its live main effect duration.
        // No duplicate main sliders or fixed pseudo-timeline hit regions.
        var shortEffects = new (ParameterViewModel Parameter, string Caption)[]
        {
            (waterRegenTime, "WATER +25% STAMINA EFFECT DURATION"),
            (energyFullCostTime, "ENERGY DRINK -100% STAMINA COST DURATION"),
            (energyPartialCostTime, "ENERGY DRINK -30% STAMINA COST DURATION")
        };
        var shortColumns = ConsumableSecondaryColumns(preview.Width);
        var secondaryWidth = (preview.Width - 24.0 -
            (shortColumns - 1) * CompactColumnGap) / shortColumns;
        for (var index = 0; index < shortEffects.Length; index++)
        {
            var (shortEffect, caption) = shortEffects[index];
            var x = preview.Left + 12.0 +
                (index % shortColumns) * (secondaryWidth + CompactColumnGap);
            var y = preview.Top + 300.0 +
                (index / shortColumns) * (CompactTrackHeight + CompactRowGap);
            DrawCompactTrack(dc, shortEffect, caption,
                $"{shortEffect.CurrentNumber:0.#}s",
                $"{shortEffect.Minimum:0.#}s",
                $"{shortEffect.Maximum:0.#}s",
                new Rect(x, y, secondaryWidth, CompactTrackHeight),
                text, muted, border, accent, panel);
        }

        // Position below the whole duration chart, including short effects.
        // MeasureOverride and OnRender share the same responsive geometry.
        var medicineKnobTop = ConsumableMedicineKnobTop(ActualWidth);
        // The three medicine knobs form their own centered row. Reusing the
        // first three cells of the four-drink bank left them visibly off-center.
        var medicineKnobWidth = Math.Min(184.0, knobWidth);
        var medicineRowWidth = 3.0 * medicineKnobWidth + 2.0 * knobGap;
        var medicineLeft = (ActualWidth - medicineRowWidth) / 2.0;
        // Native benefits, not relative sliders: +bleed-recovery points,
        // +kilograms and +psi-protection points. Keep the amount and its
        // unit centered, with a changed-only gold number and neutral unit.
        DrawSourceKnob(dc, vincaStrength, "VINCA BLEED REGEN",
            new Rect(medicineLeft, medicineKnobTop, medicineKnobWidth, 181.0),
            text, muted, border, accent, panel,
            rawReadout: true, nativeUnit: "BLEED PTS");
        DrawSourceKnob(dc, herculesStrength, "HERCULES CARRY",
            new Rect(medicineLeft + medicineKnobWidth + knobGap,
                medicineKnobTop, medicineKnobWidth, 181.0),
            text, muted, border, accent, panel,
            rawReadout: true, nativeUnit: "KG");
        DrawSourceKnob(dc, psiStrength, "PSI BLOCK PROTECTION",
            new Rect(medicineLeft + 2.0 * (medicineKnobWidth + knobGap),
                medicineKnobTop, medicineKnobWidth, 181.0),
            text, muted, border, accent, panel,
            rawReadout: true, nativeUnit: "PSI PTS");

    }

    // One true integer-hours selector, not an arbitrary 14-way percentage
    // slider. The other three Sleep System inputs stay native-source tracks.
    private void DrawSleepingDashboard(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        var threshold = P("survival.sleepEligibilityThreshold");
        var minimumHours = P("survival.minimumSleepHours");
        var healthRestore = P("survival.sleepHealthRestore");
        var staminaRestore = P("survival.sleepStaminaRestore");
        DrawHeader(dc, "SLEEP SYSTEM", string.Empty, text, muted, accent);
        if (threshold is null || minimumHours is null
            || healthRestore is null || staminaRestore is null)
        {
            DrawCenteredText(dc, "Sleep settings unavailable.",
                new Point(ActualWidth / 2.0, 140), 10, muted, FontWeights.Bold);
            return;
        }

        var width = Math.Min(740.0, ActualWidth - 34.0);
        var left = (ActualWidth - width) / 2.0;
        DrawCompactTrack(dc, threshold, "SLEEP UNLOCKS AT",
            $"{threshold.CurrentNumber:0} FATIGUE PTS",
            $"{threshold.Minimum:0} PTS", $"{threshold.Maximum:0} PTS",
            new Rect(left, 54.0, width, 82.0),
            text, muted, border, accent, panel);

        var selector = new Rect(left, 146.0, width, 84.0);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .72),
            new Pen(border, .9), selector, 4, 4);
        DrawText(dc, "MINIMUM HOURS ASLEEP",
            new Point(left + 11, 154.0), 10.6, text, FontWeights.Bold);
        var modified = minimumHours.IsValueModifiedFromReference();
        DrawRightAlignedText(dc, $"{minimumHours.CurrentNumber:0} HOURS",
            new Point(selector.Right - 11, 152.0),
            modified ? 13.0 : 11.8, modified ? accent : text,
            FontWeights.Bold);

        const int hoursCount = 14;
        const double chipGap = 3.0;
        var chipWidth = (width - 22.0 - 13.0 * chipGap) / hoursCount;
        var chipStart = left + 11.0;
        const double chipY = 185.0;
        const double chipHeight = 34.0;
        var firstCenter = chipStart + chipWidth / 2.0;
        var lastCenter = chipStart + 13.0 * (chipWidth + chipGap) + chipWidth / 2.0;
        var stepMap = new Rect(firstCenter, chipY, lastCenter - firstCenter, chipHeight);
        for (var i = 0; i < hoursCount; i++)
        {
            var hour = i + 1;
            var cell = new Rect(chipStart + i * (chipWidth + chipGap),
                chipY, chipWidth, chipHeight);
            var selected = Math.Abs(minimumHours.CurrentNumber - hour) < .5;
            var vanilla = Math.Abs(minimumHours.ReferenceNumber - hour) < .5;
            var hot = _hoverTarget?.Kind == HitKind.Horizontal
                && ReferenceEquals(_hoverTarget.Parameter, minimumHours)
                && _hoverTarget.HitRect.Equals(cell);
            dc.DrawRoundedRectangle(
                selected ? CloneWithOpacity(accent, .16) : panel,
                new Pen(selected ? accent : hot ? CloneWithOpacity(accent, .72) : border,
                    selected ? 1.9 : 1.0), cell, 3, 3);
            DrawMixerCentered(dc, hour.ToString(CultureInfo.InvariantCulture),
                new Point(cell.Left + cell.Width / 2, cell.Top + 9.0),
                chipWidth - 4.0, 10.7,
                selected && modified ? accent : text, FontWeights.Bold);
            if (vanilla)
                dc.DrawLine(new Pen(muted, 2.0),
                    new Point(cell.Left + 5.0, cell.Bottom - 3.0),
                    new Point(cell.Right - 5.0, cell.Bottom - 3.0));
            AddHorizontalTarget(minimumHours, cell, stepMap,
                minimumHours.Minimum, minimumHours.Maximum);
        }

        DrawText(dc, "RECOVERY WHEN YOU WAKE UP",
            new Point(left + 1.0, 247.0), 10.7, accent, FontWeights.Bold);
        const double gap = 12.0;
        var column = (width - gap) / 2.0;
        DrawCompactTrack(dc, healthRestore, "HEALTH RESTORED",
            healthRestore.CurrentNumber <= 0.0
                ? "0 HP" : $"+{healthRestore.CurrentNumber:0.#} HP",
            $"{healthRestore.Minimum / 100.0:0.##}x", $"{healthRestore.Maximum / 100.0:0.##}x",
            new Rect(left, 274.0, column, 82.0),
            text, muted, border, accent, panel);
        DrawCompactTrack(dc, staminaRestore, "STAMINA RESTORE STRENGTH",
            $"+{staminaRestore.CurrentNumber * 100:0.#}%",
            $"{staminaRestore.Minimum * 100:0.#}%",
            $"{staminaRestore.Maximum * 100:0.#}%",
            new Rect(left + column + gap, 274.0, column, 82.0),
            text, muted, border, accent, panel);
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

        var changed = parameter.IsValueModifiedFromReference();
        DrawRightAlignedText(dc, FriendlyValue(parameter),
            new Point(card.Right - 10.0, card.Top + 6.0),
            changed ? 11.2 : 10.0, changed ? accent : text, FontWeights.Bold);

        var track = new Rect(card.Left + 12.0, card.Top + 35.0, Math.Max(50.0, card.Width - 24.0), 20.0);
        var y = track.Top + track.Height / 2.0;
        dc.DrawLine(new Pen(border, 5.0), new Point(track.Left, y), new Point(track.Right, y));

        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track, reverse: false);
        dc.DrawLine(new Pen(accent, 5.0), new Point(track.Left, y), new Point(currentX, y));

        // Do not draw a fake vanilla marker at the edge when the verified
        // 300 HP source is outside this tool's 0-200 HP editor interval.
        if (parameter.ReferenceNumber >= parameter.Minimum
            && parameter.ReferenceNumber <= parameter.Maximum)
        {
            var vanillaX = ValueToX(parameter.ReferenceNumber,
                parameter.Minimum, parameter.Maximum, track, reverse: false);
            dc.DrawLine(new Pen(muted, 1.2),
                new Point(vanillaX, y - 8.0), new Point(vanillaX, y + 8.0));
        }

        dc.DrawEllipse(panel, new Pen(accent, IsSelected(parameter) ? 2.4 : 1.6), new Point(currentX, y), 6.0, 6.0);
        AddHorizontalTarget(parameter, track, track, parameter.Minimum, parameter.Maximum);
    }

    // Trading directions and durability rules are independent authored CFG sources.
    // Reuse the compact Economy card grammar: label/current on one line, rail, bounds.
    // Mirrored source meters: merchant buys from you on the left and sells
    // to you on the right. Directions are mirrored, but actual CFG multipliers
    // are never inverted and each slider retains native range and 0.01x step.
    private void DrawTradingMeter(
        DrawingContext dc, ParameterViewModel? parameter, string label,
        Rect card, bool reverse, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, 0.66),
            new Pen(border, 0.9), card, 4.0, 4.0);
        if (parameter is null)
            return;
        DrawText(dc, label, new Point(card.Left + 12, card.Top + 10),
            10.5, text, FontWeights.Bold);
        var changed = parameter.IsValueModifiedFromReference();
        DrawRightAlignedText(dc, $"{parameter.CurrentNumber:0.##}x",
            new Point(card.Right - 12, card.Top + 8),
            changed ? 13.3 : 12.1, changed ? accent : text, FontWeights.Bold);
        var track = new Rect(card.Left + 18, card.Top + 49,
            card.Width - 36, 12.0);
        var now = ValueToX(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, track, reverse);
        var reference = ValueToX(parameter.ReferenceNumber,
            parameter.Minimum, parameter.Maximum, track, reverse);
        var center = reverse ? track.Right : track.Left;
        dc.DrawRoundedRectangle(CloneWithOpacity(border, .32),
            null, track, 3, 3);
        dc.DrawLine(new Pen(CloneWithOpacity(accent, .78), 5.0),
            new Point(center, track.Top + 6.0),
            new Point(now, track.Top + 6.0));
        dc.DrawLine(new Pen(muted, 2),
            new Point(reference, track.Top - 3),
            new Point(reference, track.Bottom + 3));
        var hover = _hoverTarget?.Kind == HitKind.Horizontal
            && ReferenceEquals(_hoverTarget.Parameter, parameter);
        dc.DrawEllipse(panel, new Pen(hover ? Brushes.White : accent,
            hover ? 3.0 : 2.0),
            new Point(now, track.Top + 6), 6.5, 6.5);
        var low = $"{parameter.Minimum:0.##}x";
        var high = $"{parameter.Maximum:0.##}x";
        DrawText(dc, reverse ? high : low,
            new Point(track.Left, card.Bottom - 19), 9.1, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, reverse ? low : high,
            new Point(track.Right, card.Bottom - 19), 9.1, muted, FontWeights.Normal);
        AddHorizontalTarget(parameter,
            new Rect(track.Left - 8, track.Top - 8, track.Width + 16, track.Height + 16),
            track, parameter.Minimum, parameter.Maximum, reverse);
    }

    private void DrawTradingDashboard(
        DrawingContext dc, Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        DrawHeader(dc, "TRADING", "", text, muted, accent);
        var width = Math.Min(750.0, ActualWidth - 34.0);
        const double gap = 12.0;
        var cell = (width - gap) / 2.0;
        var left = (ActualWidth - width) / 2.0;
        DrawTradingMeter(dc, P("economy.buyCondition"),
            "YOU SELL TO TRADERS",
            new Rect(left, 52.0, cell, 92.0), false,
            text, muted, border, accent, panel);
        DrawTradingMeter(dc, P("economy.sellCondition"),
            "YOU BUY FROM TRADERS",
            new Rect(left + cell + gap, 52.0, cell, 92.0), true,
            text, muted, border, accent, panel);
        DrawText(dc, "TRADE ACCEPTANCE", new Point(left, 158),
            10.2, muted, FontWeights.Bold);
        DrawEconomyCard(dc, P("economy.traderMinBuyCondition"),
            "MINIMUM ACCEPTED CONDITION",
            new Rect(left, 180.0, cell, 80.0),
            text, muted, border, accent, panel);
        DrawEconomyCard(dc, P("economy.itemValueFloor"),
            "BROKEN ITEM VALUE",
            new Rect(left + cell + gap, 180.0, cell, 80.0),
            text, muted, border, accent, panel);
    }

    // Hunger progress and stamina regeneration changes are distinct systems.
    // The threshold ladder is a read-only preview: LOW/MEDIUM/SEVERE can share
    // a source value without creating overlapping drag targets.
    // Compact three-across stage and recovery editing when room permits. At
    // narrow sizes, rows wrap rather than reducing fonts or slider hit areas.
    private void DrawHungerLadder(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
    {
        var rate = P("survival.hungerRate");
        var low = P("survival.hungerLowThreshold");
        var medium = P("survival.hungerMediumThreshold");
        var severe = P("survival.hungerStrongThreshold");
        var wellFed = P("survival.wellFedStaminaRegen");
        var mediumPenalty = P("survival.hungerMediumStaminaRegen");
        var severePenalty = P("survival.hungerStrongStaminaRegen");

        DrawHeader(dc, "HUNGER STATE LADDER",
            string.Empty,
            text, muted, accent);

        if (rate is null || low is null || medium is null || severe is null ||
            wellFed is null || mediumPenalty is null || severePenalty is null)
        {
            DrawCenteredText(dc, "Hunger system data unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var width = Math.Max(460.0, ActualWidth - 34.0);
        var resourceWidth = Math.Max(1.0, ActualWidth - 32.0);
        const double resourceGap = 12.0;
        var half = (resourceWidth - resourceGap) / 2.0;
        DrawCoreResourceRuler(dc, rate, "HUNGER RATE", "pts/s",
            new Rect(16.0, SurvivalPrimaryRulerTop, half, SurvivalPrimaryRulerHeight),
            text, muted, border, accent, panel);
        DrawCoreResourceRuler(dc, wellFed, "WELL-FED STAMINA RECOVERY", "%",
            new Rect(16.0 + half + resourceGap, SurvivalPrimaryRulerTop,
                half, SurvivalPrimaryRulerHeight),
            text, muted, border, accent, panel);

        const double previewTop = SurvivalPrimaryPreviewTop;
        var preview = new Rect(17.0, previewTop, width, 80.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), preview, 4.0, 4.0);
        DrawText(dc, "HUNGER POINTS  /  STAGE PREVIEW",
            new Point(preview.Left + 12.0, previewTop + 7.0),
            11.0, text, FontWeights.Bold);

        // Share the exact 0–100 X coordinates with the lower Hunger profile.
        // The left inset makes room for the profile's rotated Y caption and ticks.
        var axis = new Rect(preview.Left + 81.0, previewTop + 32.0,
            preview.Width - 111.0, 12.0);
        var y = axis.Top + axis.Height / 2.0;
        double At(double value) => ValueToX(value, 0.0, 100.0, axis, false);

        var stops = new[]
        {
            0.0,
            Math.Clamp(low.CurrentNumber, 0.0, 100.0),
            Math.Clamp(medium.CurrentNumber, 0.0, 100.0),
            Math.Clamp(severe.CurrentNumber, 0.0, 100.0),
            100.0
        };
        for (var i = 0; i < stops.Length - 1; i++)
        {
            var a = At(stops[i]);
            var b = At(stops[i + 1]);
            dc.DrawRectangle(CloneWithOpacity(i == 3 ? accent : soft,
                    i == 3 ? 0.72 : 0.22 + i * 0.12), null,
                new Rect(a, y - 4.0, Math.Max(0.0, b - a), 8.0));
        }

        foreach (var threshold in new[] { low, medium, severe })
        {
            var now = At(threshold.CurrentNumber);
            var reference = At(threshold.ReferenceNumber);
            dc.DrawLine(new Pen(muted, 1.5),
                new Point(reference, y - 10.0), new Point(reference, y + 10.0));
            dc.DrawLine(new Pen(accent, 2.0),
                new Point(now, y - 11.0), new Point(now, y + 11.0));
        }

        var previewParameters = new[] { low, medium, severe };
        var previewLabels = new[] { "LOW", "MEDIUM", "SEVERE" };
        var legendWidth = (preview.Width - 24.0) / 3.0;
        for (var i = 0; i < 3; i++)
        {
            var parameter = previewParameters[i];
            var cx = preview.Left + 12.0 + (i + 0.5) * legendWidth;
            DrawCenteredNumericChanges(dc,
                $"{previewLabels[i]} {parameter.CurrentNumber:0}",
                new Point(cx, previewTop + 55.0),
                9.5, text, accent, FontWeights.SemiBold,
                parameter.IsValueModifiedFromReference());
        }

        // Game-facing state profile: the bonus is now edited in the top row.
        // The lower plot spans the same full-width panel as the stage preview.
        var frame = new Rect(17.0, preview.Bottom + 55.0,
            Math.Max(460.0, ActualWidth - 34.0), HungerProfileHeight);
        DrawCenteredText(dc, "HUNGER SURVIVAL PROFILE",
            new Point(ActualWidth / 2.0, frame.Top - 36.0), 11.5, accent, FontWeights.Bold);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), frame, 4.0, 4.0);
        // Leave room only for Y tick labels/title; no separate Well-Fed lane.
        var plot = new Rect(axis.Left - 2.0, frame.Top + 43.0,
            axis.Width + 12.0, 183.0);

        var area = new Rect(plot.Left + 2.0, plot.Top + 8.0,
            plot.Width - 12.0, plot.Height - 21.0);
        double X(double value) => ValueToX(value, 0.0, 100.0, area, false);
        double Y(double percent) => area.Bottom - (percent + 100.0) / 100.0 * area.Height;
        var zeroY = Y(0);
        var medY = Y(-mediumPenalty.CurrentNumber * 100.0);
        var sevY = Y(-severePenalty.CurrentNumber * 100.0);
        var baseMedY = Y(-mediumPenalty.ReferenceNumber * 100.0);
        var baseSevY = Y(-severePenalty.ReferenceNumber * 100.0);
        var lowX = X(low.CurrentNumber);
        var medX = X(medium.CurrentNumber);
        var sevX = X(severe.CurrentNumber);
        var baseLowX = X(low.ReferenceNumber);
        var baseMedX = X(medium.ReferenceNumber);
        var baseSevX = X(severe.ReferenceNumber);

        DrawCenteredText(dc, "HUNGER POINTS (0–100)", new Point(plot.Left + plot.Width / 2.0,
            frame.Top + 27.0), 9.2, text, FontWeights.Bold);
        // Separate vertical axis caption from tick values; rotation is only visual.
        var yAxisCenter = new Point(plot.Left - 52.0, area.Top + area.Height / 2.0);
        dc.PushTransform(new RotateTransform(-90.0, yAxisCenter.X, yAxisCenter.Y));
        DrawCenteredText(dc, "STAMINA RECOVERY LOSS", yAxisCenter,
            9.2, text, FontWeights.Bold);
        dc.Pop();
        foreach (var tick in new[] { -100.0, -75.0, -50.0, -25.0, 0.0 })
        {
            var yy = Y(tick);
            dc.DrawLine(new Pen(CloneWithOpacity(soft, tick == 0 ? 0.85 : 0.35),
                tick == 0 ? 1.5 : 1.0) { DashStyle = tick == 0 ? DashStyles.Solid : DashStyles.Dot },
                new Point(area.Left, yy), new Point(area.Right, yy));
            DrawRightAlignedText(dc, tick == 0 ? "0%" : $"{tick:0}%",
                new Point(plot.Left - 7.0, yy - 7.0),
                10.0, tick == 0 ? text : muted, FontWeights.SemiBold);
        }
        for (var tick = 0; tick <= 100; tick += 25)
            DrawCenteredText(dc, $"{tick}", new Point(X(tick), plot.Bottom + 17.0),
                10.0, tick == 0 || tick == 100 ? text : muted, FontWeights.SemiBold);

        // Transparent stage bands distinguish the three difficulty transitions.
        dc.DrawRectangle(CloneWithOpacity(accent, 0.035), null,
            new Rect(lowX, area.Top, Math.Max(0.0, medX - lowX), area.Height));
        dc.DrawRectangle(CloneWithOpacity(accent, 0.065), null,
            new Rect(medX, area.Top, Math.Max(0.0, sevX - medX), area.Height));
        dc.DrawRectangle(CloneWithOpacity(accent, 0.10), null,
            new Rect(sevX, area.Top, Math.Max(0.0, area.Right - sevX), area.Height));

        // Dashed grey Base shows only authored discrete effects; yellow steps
        // show active MEDIUM and SEVERE effects with no fake interpolation.
        var ghost = new Pen(CloneWithOpacity(muted, 0.72), 1.6)
            { DashStyle = DashStyles.Dash };
        var gold = new Pen(accent, 2.8);
        dc.DrawLine(ghost, new Point(baseLowX, zeroY), new Point(baseMedX, zeroY));
        dc.DrawLine(ghost, new Point(baseMedX, zeroY), new Point(baseMedX, baseMedY));
        dc.DrawLine(ghost, new Point(baseMedX, baseMedY), new Point(baseSevX, baseMedY));
        dc.DrawLine(ghost, new Point(baseSevX, baseMedY), new Point(baseSevX, baseSevY));
        dc.DrawLine(ghost, new Point(baseSevX, baseSevY), new Point(area.Right, baseSevY));
        dc.DrawLine(gold, new Point(lowX, zeroY), new Point(medX, zeroY));
        dc.DrawLine(gold, new Point(medX, zeroY), new Point(medX, medY));
        dc.DrawLine(gold, new Point(medX, medY), new Point(sevX, medY));
        dc.DrawLine(gold, new Point(sevX, medY), new Point(sevX, sevY));
        dc.DrawLine(gold, new Point(sevX, sevY), new Point(area.Right, sevY));

        // LOW moves only its hunger threshold; it has no native regen penalty.
        var lowHandle = new Point(lowX, zeroY);
        dc.DrawEllipse(panel, new Pen(muted, 1.5), new Point(baseLowX, zeroY), 6.0, 6.0);
        DrawHumanAxisHandle(dc, lowHandle, accent, panel,
            ReferenceEquals(_hoverTarget?.Parameter, low), IsSelected(low));
        AddHorizontalTarget(low, new Rect(lowX - 17.0, zeroY - 17.0, 34.0, 34.0),
            area, low.Minimum, low.Maximum);

        void StateNode(ParameterViewModel threshold, ParameterViewModel loss,
            Point now, Point reference, string name)
        {
            dc.DrawEllipse(panel, new Pen(muted, 1.5), reference, 6.0, 6.0);
            DrawHumanAxisHandle(dc, now, accent, panel,
                ReferenceEquals(_hoverTarget?.Parameter, threshold), IsSelected(threshold));
            if (threshold.IsControlEnabled && loss.IsControlEnabled)
                _hitTargets.Add(new HitTarget(threshold,
                    new Rect(now.X - 18.0, now.Y - 18.0, 36.0, 36.0),
                    area, HitKind.HungerJoint, threshold.Minimum, threshold.Maximum, false));
        }
        StateNode(medium, mediumPenalty,
            new Point(medX, medY), new Point(baseMedX, baseMedY), "MEDIUM");
        StateNode(severe, severePenalty,
            new Point(sevX, sevY), new Point(baseSevX, baseSevY), "SEVERE");

        // The X-axis labels end above this dedicated footer. Each state gets
        // three reserved, short lines instead of a clipped paragraph.
        // Keep the rows anchored to the plot rather than moving with handles.
        var footerTop = frame.Top + 269.0;
        var stageWidth = plot.Width / 3.0;
        void StageCaption(int index, string stageName,
            ParameterViewModel threshold, ParameterViewModel? recoveryLoss)
        {
            var cell = new Rect(plot.Left + index * stageWidth,
                footerTop, stageWidth, 63.0);
            dc.PushClip(new RectangleGeometry(cell));
            var cx = cell.Left + cell.Width / 2.0;

            DrawCenteredText(dc, stageName,
                new Point(cx, footerTop + 1.0),
                10.0, text, FontWeights.Bold);
            DrawCenteredNumericChanges(dc,
                $"AT {threshold.CurrentNumber:0} PTS",
                new Point(cx, footerTop + 27.0),
                10.0, text, accent, FontWeights.SemiBold,
                threshold.IsValueModifiedFromReference());

            if (recoveryLoss is not null)
            {
                DrawCenteredNumericChanges(dc,
                    $"-{recoveryLoss.CurrentNumber * 100:0}% RECOVERY",
                    new Point(cx, footerTop + 47.0),
                    9.6, text, accent, FontWeights.SemiBold,
                    recoveryLoss.IsValueModifiedFromReference());
            }
            dc.Pop();
        }

        StageCaption(0, "LOW", low, null);
        StageCaption(1, "MEDIUM", medium, mediumPenalty);
        StageCaption(2, "SEVERE", severe, severePenalty);

    }

    // A source-grounded tick schedule plus hypothetical raw full-health
    // depletion. This does NOT model live HP, healing, mitigation or any
    // other runtime state; all tick markers remain read-only.
    private static string StarvationElapsed(double seconds)
    {
        if (seconds < 60.0)
            return $"{seconds:0.#}s";
        var hours = (int)(seconds / 3600.0);
        var minutes = (int)((seconds % 3600.0) / 60.0);
        var remaining = seconds % 60.0;
        return hours > 0
            ? $"{hours}h {minutes}m {remaining:00.#}s"
            : $"{minutes}m {remaining:00.#}s";
    }

    private void DrawStarvationTimeline(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush soft, Brush accent, Brush panel)
    {
        DrawMasterSectionTitle(dc, "STARVATION PENALTIES", accent);
        var toggle = P("advanced.starvationMechanics");
        var delay = P("survival.starvationPenaltyDelay");
        var interval = P("survival.starvationDamageInterval");
        var damage = P("survival.starvationDamage");

        DrawMasterBundleToggle(dc, toggle, "STARVATION MECHANICS",
            text, muted, border, accent, panel);

        var timing = new (string Id, string Label, Func<double, string> Format)[]
        {
            ("survival.starvationPenaltyDelay", "PENALTIES START", MasterSeconds),
            ("survival.starvationDamageInterval", "DAMAGE EVERY", MasterSeconds)
        };
        DrawMasterCardBank(dc, timing, 110.0,
            text, muted, border, soft, accent, panel);

        var previewTop = 110.0 + MasterRows(ActualWidth, 2) * 94.0;
        var bankWidth = Math.Min(760.0, Math.Max(1.0, ActualWidth - 32.0));
        var preview = new Rect((ActualWidth - bankWidth) / 2.0,
            previewTop, bankWidth, StarvationPreviewHeight);
        dc.DrawRoundedRectangle(panel, new Pen(border, 0.9),
            preview, 4.0, 4.0);
        DrawText(dc, "DAMAGE TICK TIMELINE",
            new Point(preview.Left + 12.0, preview.Top + 9.0),
            10.2, text, FontWeights.Bold);

        if (delay is not null && interval is not null && damage is not null)
        {
            var active = toggle?.CurrentBoolean == true;
            const int previewTicks = 3;
            // This is a three-tick SOURCE SCHEDULE, not a prediction of
            // damage after game protections, healing or other effects.
            var startSeconds = Math.Max(0.0, delay.CurrentNumber);
            var intervalSeconds = Math.Max(0.0, interval.CurrentNumber);
            var endSeconds = startSeconds + previewTicks * intervalSeconds;
            var span = Math.Max(1.0, endSeconds);
            var rawHp = previewTicks * Math.Max(0.0, damage.CurrentNumber);
            DrawRightAlignedText(dc, $"3 TICKS  /  {rawHp:0.#} RAW HP",
                new Point(preview.Right - 12.0, preview.Top + 9.0),
                9.4, active ? text : muted, FontWeights.SemiBold);

            var axis = new Rect(preview.Left + 24.0,
                preview.Top + 67.0, Math.Max(1.0, preview.Width - 48.0), 16.0);
            var y = axis.Top + axis.Height / 2.0;
            double At(double seconds)
                => axis.Left + axis.Width * Math.Clamp(seconds / span, 0.0, 1.0);
            var startX = At(startSeconds);
            var tickGap = Math.Max(0.0, At(startSeconds + intervalSeconds) - startX);

            // Phase captions are spatially grounded in their actual durations.
            // Never call the pre-tick segment SAFE: unrelated penalties can
            // still apply even before this damage-tick schedule begins.
            if (startX - axis.Left > 58.0)
                DrawCenteredText(dc, "DELAY",
                    new Point((axis.Left + startX) / 2.0, preview.Top + 33.0),
                    9.1, muted, FontWeights.SemiBold);
            if (axis.Right - startX > 118.0)
                DrawCenteredText(dc, "DAMAGE WINDOW",
                    new Point((startX + axis.Right) / 2.0, preview.Top + 33.0),
                    9.1, active ? text : muted, FontWeights.SemiBold);

            // A subdued active region and its three numbered ticks show the
            // timing rhythm without creating any draggable/CFG-backed marker.
            dc.DrawLine(new Pen(soft, 3.0),
                new Point(axis.Left, y), new Point(axis.Right, y));
            if (axis.Right > startX + Epsilon)
            {
                dc.DrawRoundedRectangle(
                    CloneWithOpacity(active ? accent : muted, active ? 0.10 : 0.07),
                    null,
                    new Rect(startX, y - 7.0, axis.Right - startX, 14.0),
                    3.0, 3.0);
                dc.DrawLine(new Pen(
                        CloneWithOpacity(active ? accent : muted, active ? .75 : .42), 2.6),
                    new Point(startX, y), new Point(axis.Right, y));
            }
            dc.DrawLine(new Pen(muted, 2.1),
                new Point(startX, y - 12.0), new Point(startX, y + 12.0));
            for (var tick = 1; tick <= previewTicks; tick++)
            {
                var seconds = startSeconds + tick * intervalSeconds;
                var x = At(seconds);
                // Keep numbered tick labels OUTSIDE the actual rail markers.
                // Compressed schedules use a single 1–3 label instead of overlaps.
                dc.DrawEllipse(active ? accent : muted, null,
                    new Point(x, y), 4.7, 4.7);
                if (tickGap >= 20.0 || tick == previewTicks)
                    DrawCenteredText(dc, tickGap < 20.0 ? "1–3" :
                        tick.ToString(CultureInfo.InvariantCulture),
                        new Point(x, preview.Top + 49.0),
                        10.0, active ? text : muted, FontWeights.Bold);
                // Suppress closely packed intermediate timestamp captions;
                // the native controls still show the exact interval.
                if (tick == previewTicks || tickGap >= 45.0)
                    DrawCenteredText(dc, $"{seconds:0.#}s",
                        new Point(x, preview.Top + 91.0),
                        9.2, muted, FontWeights.Normal);
            }
            DrawText(dc, "0s", new Point(axis.Left, preview.Top + 110.0),
                9.0, muted, FontWeights.Normal);
            DrawCenteredText(dc, $"START {startSeconds:0.#}s",
                new Point(Math.Clamp(startX, axis.Left + 65.0, axis.Right - 55.0),
                    preview.Top + 110.0),
                9.0, muted, FontWeights.SemiBold);

            // RAW time-to-zero from configured FULL HP; never a live death timer.
            var perTick = Math.Max(0.0, damage.CurrentNumber);
            var capacity = _starvationMaxHp?.CurrentNumber;
            var hasCapacity = capacity is > 0.0;
            var hasCountdown = hasCapacity && perTick > Epsilon && intervalSeconds > Epsilon;
            var lethalTicks = hasCountdown
                ? (long)Math.Ceiling(capacity!.Value / perTick) : 0L;
            var elapsed = startSeconds + lethalTicks * intervalSeconds;

            // Read-only two-row TTK HUD. No extra input or source ownership.
            var hud = new Rect(preview.Left + 12.0, preview.Top + 127.0,
                Math.Max(1.0, preview.Width - 24.0), 41.0);
            dc.DrawRoundedRectangle(
                CloneWithOpacity(active ? accent : muted, active ? 0.045 : 0.025),
                new Pen(CloneWithOpacity(border, 0.7), 0.8),
                hud, 3.0, 3.0);
            dc.DrawLine(new Pen(active ? accent : muted, 2.0),
                new Point(hud.Left + 1.0, hud.Top + 7.0),
                new Point(hud.Left + 1.0, hud.Bottom - 7.0));

            DrawText(dc, "STARVATION KILLS IN",
                new Point(hud.Left + 12.0, hud.Top + 5.0),
                9.5, active ? text : muted, FontWeights.Bold);
            DrawText(dc, active ? "ESTIMATE · NO HEALING" : "IF ENABLED · ESTIMATE",
                new Point(hud.Left + 12.0, hud.Top + 24.0),
                9.0, muted, FontWeights.SemiBold);

            var changedEstimate = active && (
                delay.IsValueModifiedFromReference() ||
                interval.IsValueModifiedFromReference() ||
                damage.IsValueModifiedFromReference() ||
                (_starvationMaxHp?.IsValueModifiedFromReference() ?? false));
            var timerInk = active ? changedEstimate ? accent : text : muted;
            var hero = hasCountdown ? $"~{StarvationElapsed(elapsed)}"
                : !hasCapacity ? "HP N/A"
                : perTick <= Epsilon ? "NO HP DAMAGE" : "NO TIMER";
            var detailLeft = hud.Right - Math.Min(160.0, hud.Width * 0.35);
            var timerLeft = hud.Left + 151.0;
            var timerWidth = Math.Max(1.0, detailLeft - timerLeft - 12.0);
            dc.DrawText(CreateText(hero, hasCountdown ? 16.0 : 11.0,
                    timerInk, FontWeights.Bold, timerWidth),
                new Point(timerLeft, hud.Top + (hasCountdown ? 8.0 : 12.0)));
            dc.DrawLine(new Pen(CloneWithOpacity(border, 0.55), 0.8),
                new Point(detailLeft - 8.0, hud.Top + 8.0),
                new Point(detailLeft - 8.0, hud.Bottom - 8.0));

            var tickSummary = hasCountdown ? $"{lethalTicks} TICKS"
                : perTick <= Epsilon ? "0 HP / TICK" : "TIME N/A";
            DrawRightAlignedText(dc, tickSummary,
                new Point(hud.Right - 11.0, hud.Top + 5.0),
                10.5, active ? text : muted, FontWeights.Bold);
            DrawRightAlignedText(dc,
                hasCapacity ? $"FULL {capacity!.Value:0.#} HP" : "MAX HP UNAVAILABLE",
                new Point(hud.Right - 11.0, hud.Top + 24.0),
                9.0, muted, FontWeights.SemiBold);
        }

        var effects = new (string Id, string Label, Func<double, string> Format)[]
        {
            ("survival.starvationStaminaDrain", "EXTRA STAMINA COST", MasterPositivePercent),
            ("survival.starvationDamage", "HP LOST PER TICK", MasterHp),
            ("survival.starvationHealthRegenPenalty", "HEALTH RECOVERY BLOCKED", MasterPercent)
        };
        DrawMasterCardBank(dc, effects, previewTop + StarvationPreviewHeight + 12.0,
            text, muted, border, soft, accent, panel);
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
        // Current and Base Preset values get their own line. Independent
        // left/right headings formerly overlapped at the minimum width.
        var previewTextWidth = Math.Max(1.0, preview.Width - 24.0);
        dc.DrawText(CreateText("PROTECTION WINDOW  /  READ ONLY",
                10.0, text, FontWeights.Bold, previewTextWidth),
            new Point(preview.Left + 12.0, 63.0));
        dc.DrawText(CreateText(
                $"PROTECTION  {minimum.CurrentNumber:0.00}–{maximum.CurrentNumber:0.00}",
                9.8, minimum.IsValueModifiedFromReference() ||
                    maximum.IsValueModifiedFromReference() ? accent : text,
                FontWeights.SemiBold, previewTextWidth),
            new Point(preview.Left + 12.0, 81.0));

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
                $"{parameter.CurrentNumber:0.00}",
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
        DrawText(dc, "CURVE WEIGHTS, NOT DAMAGE-REDUCTION %",
            new Point(20.0, 409.0), 8.8, muted, FontWeights.Normal);
    }

    // Radiation has three independent source units. Do not infer a net radiation
    // rate by subtracting a difficulty gain multiplier from a points/second removal.
    // The summary is read-only; each native-range track is edited independently.
    // Three independent source-native 270° controls. Recovery is a rate in
    // PTS/S (not an x multiplier), while build-up and damage are multipliers.
    private void DrawRadiationBalance(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush soft, Brush accent, Brush panel)
    {
        DrawHeader(dc, "RADIATION SYSTEM", string.Empty, text, muted, accent);
        var sources = new (string Id, string Label)[]
        {
            ("survival.radiationAccumulation", "BUILD-UP"),
            ("survival.radiationRecovery", "RECOVERY"),
            ("survival.radiationDamage", "HP DAMAGE")
        };
        const double gap = 12.0;
        var bankWidth = Math.Min(606.0, ActualWidth - 34.0);
        var cell = (bankWidth - gap * 2.0) / 3.0;
        var left = (ActualWidth - bankWidth) / 2.0;
        for (var i = 0; i < sources.Length; i++)
        {
            var source = sources[i];
            DrawSourceKnob(dc, P(source.Id), source.Label,
                new Rect(left + i * (cell + gap), 49.0, cell, 181.0),
                text, muted, border, accent, panel,
                rateReadout: source.Id == "survival.radiationRecovery");
        }
    }

    // Three native stash ammo quantities + three independent bonus rewards.
    // Current/source label/rail obey the Stamina Usage anatomy; no simulated
    // certainty is suggested for independent chance/count rolls.
    // Ammo tiers form one bounded three-column rack; bonus rolls retain
    // their own independent real-source horizontal cards underneath.
    private void DrawCompactStashRewards(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawHeader(dc, "STASH REWARDS", "", text, muted, accent);
        DrawText(dc, "AMMO BY STASH TIER",
            new Point(18, 47), 10.4, muted, FontWeights.Bold);
        var tiers = new (string Id, string Label)[]
        {
            ("loot.cheapStashAmmoQuantity", "BASIC"),
            ("loot.mediumStashAmmoQuantity", "STANDARD"),
            ("loot.expensiveStashAmmoQuantity", "RARE")
        };
        // The three independent stash-tier ammo multipliers share the same native
        // rotary renderer, snap grid and pointer/keyboard behavior as Your Weapons.
        const double gap = 11.0;
        const double knobWidth = 194.0;
        const double knobHeight = 181.0;
        var bankWidth = Math.Min(606.0, ActualWidth - 34.0);
        var left = (ActualWidth - bankWidth) / 2.0;
        for (var i = 0; i < tiers.Length; i++)
        {
            var tier = tiers[i];
            var cellLeft = left + i * (bankWidth / 3.0);
            DrawSourceKnob(dc, P(tier.Id), tier.Label,
                new Rect(cellLeft + (bankWidth / 3.0 - knobWidth) / 2.0,
                    69.0, knobWidth, knobHeight),
                text, muted, border, accent, panel);
        }

        DrawText(dc, "BONUS REWARDS",
            new Point(18, 270), 10.4, muted, FontWeights.Bold);
        var rewards = new (string Id, string Label)[]
        {
            ("loot.cheapStashGrenadeChance", "RGD-5 CHANCE"),
            ("loot.cheapStashGrenadeQuantity", "RGD-5 AMOUNT"),
            ("loot.expensiveStashMoneyChance", "COUPON CHANCE")
        };
        var columns = ActualWidth >= 740.0 ? 3 : 2;
        var cardWidth = Math.Min(280.0,
            (ActualWidth - 34.0 - (columns - 1) * gap) / columns);
        for (var i = 0; i < rewards.Length; i++)
        {
            var row = i / columns;
            var rowCount = Math.Min(columns, rewards.Length - row * columns);
            var rowWidth = rowCount * cardWidth + (rowCount - 1) * gap;
            var x0 = (ActualWidth - rowWidth) / 2.0;
            DrawHorizontalTrackCard(dc, P(rewards[i].Id), rewards[i].Label,
                new Rect(x0 + (i % columns) * (cardWidth + gap),
                    290.0 + row * 83.0, cardWidth, 72.0),
                text, muted, border, accent, panel,
                pairedValueFormatting: true, hideBaseText: true);
        }
    }

    // The real SNG/NATO generator pools are distinct. Their previews show the
    // normalized composition *inside* each pool, never global drop probability.
    // The preview is read-only; every family has one independent full-width editor.
    // SNG and NATO are different authored generator pools. Every colored bar
    // segment is keyed directly underneath by exact caliber and share; legend
    // swatches use the SAME opacity/brush as the segment, even at 0%.
    // SNG 9x18/9x19 maps two source rows; NATO 9x19 maps one.
    private void DrawAmmoDistribution(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush soft, Brush accent, Brush panel)
    {
        DrawHeader(dc, "BREAKABLE BOXES", "", text, muted, accent);
        var ammoYield = P("loot.destructibleAmmoQuantity");
        var weight918 = P("loot.ammoBoxWeight918");
        var weight045 = P("loot.ammoBoxWeight045");
        var weight545 = P("loot.ammoBoxWeight545");
        var weight012 = P("loot.ammoBoxWeight012");
        if (ammoYield is null || weight918 is null || weight045 is null ||
            weight545 is null || weight012 is null)
        {
            DrawCenteredText(dc, "Ammo distribution data unavailable.",
                new Point(ActualWidth / 2.0, 150.0),
                10.0, muted, FontWeights.Bold);
            return;
        }

        const double margin = 17.0;
        const double gap = 10.0;
        var cell = (Math.Max(1.0, ActualWidth - margin * 2.0) - gap) / 2.0;

        DrawText(dc, "YIELD PER BOX", new Point(margin, 47.0),
            10.2, muted, FontWeights.Bold);
        DrawHorizontalTrackCard(dc, ammoYield,
            "AMMO YIELD", new Rect(margin, 66.0, cell, 72.0),
            text, muted, border, accent, panel, pairedValueFormatting: true);
        DrawHorizontalTrackCard(dc, P("loot.destructibleGrenadeQuantity"),
            "GRENADE YIELD", new Rect(margin + cell + gap, 66.0, cell, 72.0),
            text, muted, border, accent, panel, pairedValueFormatting: true);

        // Live, source-exact read-only output counts for EACH of the eight
        // destructible ammo rows. These are ItemGeneratorPrototypes.cfg's
        // DestructibleStash_AmmoSNG/NATO source MinCount/MaxCount fields,
        // NOT the generic NPC_Ammo_Smart corpse package. Only Ammo Yield
        // scales stack size; family Weight determines relative eligibility.
        // Keep the same positive integer half-up arithmetic as
        // CfgPatchWriterService.ScalePositiveInteger (native 0.5..2x).
        var countsModified = ammoYield.IsValueModifiedFromReference();
        var preview = new Rect(margin, 147.0,
            Math.Max(1.0, ActualWidth - 2.0 * margin), 121.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 0.9),
            preview, 4.0, 4.0);
        DrawText(dc, "AMMO YIELD PREVIEW",
            new Point(preview.Left + 11.0, preview.Top + 6.0),
            10.2, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "ROUNDS IF ROLLED",
            new Point(preview.Right - 11.0, preview.Top + 6.0),
            9.4, muted, FontWeights.SemiBold);

        void YieldRow(string pool, double rowTop,
            (string Caliber, int MinCount, int MaxCount, ParameterViewModel Weight)[] items)
        {
            // Source pools have four entries each. Cap sample tile width at
            // 143 DIP and center the full row, so a wide host never leaves
            // four comically stretched read-only tiles.
            const double rowInset = 11.0;
            const double poolLabelWidth = 42.0;
            const double gapToTiles = 9.0;
            const double sampleGap = 7.0;
            var innerWidth = Math.Max(1.0, preview.Width - 2.0 * rowInset);
            var tileWidth = Math.Min(143.0,
                Math.Max(1.0, (innerWidth - poolLabelWidth - gapToTiles
                    - 3.0 * sampleGap) / 4.0));
            var groupWidth = poolLabelWidth + gapToTiles
                + 4.0 * tileWidth + 3.0 * sampleGap;
            var rowLeft = preview.Left + (preview.Width - groupWidth) / 2.0;
            DrawCenteredText(dc, pool,
                new Point(rowLeft + poolLabelWidth / 2.0, rowTop + 12.0),
                10.0, muted, FontWeights.Bold);

            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var tile = new Rect(rowLeft + poolLabelWidth + gapToTiles
                        + i * (tileWidth + sampleGap),
                    rowTop, tileWidth, 39.0);
                // An individually zero-weighted ammo family cannot be
                // rolled, though its authored quantity values are retained.
                var eligible = item.Weight.CurrentNumber > Epsilon;
                dc.DrawRoundedRectangle(
                    CloneWithOpacity(panel, eligible ? 0.93 : 0.55),
                    new Pen(CloneWithOpacity(border, eligible ? 0.80 : 0.45), 0.9),
                    tile, 3.0, 3.0);
                var low = Math.Max(1, (int)Math.Floor(
                    item.MinCount * ammoYield.CurrentNumber + 0.5));
                var high = Math.Max(1, (int)Math.Floor(
                    item.MaxCount * ammoYield.CurrentNumber + 0.5));
                DrawCenteredText(dc, item.Caliber,
                    new Point(tile.Left + tile.Width / 2.0, tile.Top + 3.0),
                    9.7, muted, FontWeights.SemiBold);
                // Count + en dash is one value token; caliber digits are
                // neutral. The 1.2-DIP increase and gold apply only when
                // Ammo Yield itself differs from selected Base Preset.
                DrawCenteredText(dc, $"{low}–{high}",
                    new Point(tile.Left + tile.Width / 2.0, tile.Top + 18.0),
                    countsModified ? 12.8 : 11.6,
                    countsModified ? accent : eligible ? text : muted,
                    FontWeights.SemiBold);
            }
        }

        YieldRow("SNG", 173.0, new[]
        {
            (Caliber: "9×18", MinCount: 3, MaxCount: 6, Weight: weight918),
            (Caliber: "9×19", MinCount: 5, MaxCount: 10, Weight: weight918),
            (Caliber: "5.45", MinCount: 5, MaxCount: 10, Weight: weight545),
            (Caliber: "12 GA", MinCount: 2, MaxCount: 4, Weight: weight012)
        });
        YieldRow("NATO", 219.0, new[]
        {
            (Caliber: ".45 ACP", MinCount: 5, MaxCount: 10, Weight: weight045),
            (Caliber: "9×19", MinCount: 5, MaxCount: 10, Weight: weight918),
            (Caliber: "5.56", MinCount: 5, MaxCount: 10, Weight: weight545),
            (Caliber: "12 GA", MinCount: 5, MaxCount: 10, Weight: weight012)
        });

        DrawText(dc, "AMMO FAMILY WEIGHTS", new Point(margin, 279.0),
            10.2, muted, FontWeights.Bold);
        // One compact row of four native 0..3x source knobs. The 620-DIP
        // Combat knob bank would be too narrow here, so this bank may use
        // up to 780 DIP, while still centering in a full-width Loot card.
        // Their original 0.25x snap and existing SNG/NATO coupling stay intact.
        var weights = new (ParameterViewModel Parameter, string Label)[]
        {
            (weight918, "9×18 / 9×19"),
            (weight045, ".45 ACP"),
            (weight545, "5.45 / 5.56"),
            (weight012, "12 GAUGE")
        };
        const double knobGap = 10.0;
        const double knobCardTop = 298.0;
        const double knobCardHeight = 181.0;
        var knobBankWidth = Math.Min(780.0, ActualWidth - margin * 2.0);
        var knobWidth = (knobBankWidth - 3.0 * knobGap) / weights.Length;
        var knobLeft = (ActualWidth - knobBankWidth) / 2.0;
        for (var i = 0; i < weights.Length; i++)
        {
            DrawSourceKnob(dc, weights[i].Parameter, weights[i].Label,
                new Rect(knobLeft + i * (knobWidth + knobGap),
                    knobCardTop, knobWidth, knobCardHeight),
                text, muted, border, accent, panel);
        }

        DrawText(dc, "AMMO POOL COMPOSITION", new Point(margin, 490.0),
            10.2, muted, FontWeights.Bold);

        var w918 = Math.Max(0.0, weight918.CurrentNumber);
        var w045 = Math.Max(0.0, weight045.CurrentNumber);
        var w545 = Math.Max(0.0, weight545.CurrentNumber);
        var w012 = Math.Max(0.0, weight012.CurrentNumber);
        static double[] Normalize(IReadOnlyList<double> weights)
        {
            var sum = weights.Sum();
            return sum <= Epsilon
                ? weights.Select(_ => 0.0).ToArray()
                : weights.Select(v => Math.Max(0.0, v) / sum).ToArray();
        }
        var sng = Normalize(new[] { 2.0 * w918, w545, w012 });
        var nato = Normalize(new[] { w045, w918, w545, w012 });

        void Pool(string name, IReadOnlyList<string> calibers,
            IReadOnlyList<double> percentages, IReadOnlyList<int> colorIndices, double top)
        {
            var card = new Rect(margin, top,
                Math.Max(1.0, ActualWidth - margin * 2.0), 89.0);
            dc.DrawRoundedRectangle(panel, new Pen(border, 0.9),
                card, 4.0, 4.0);
            DrawText(dc, name, new Point(card.Left + 10.0, top + 5.0),
                10.5, text, FontWeights.Bold);
            var bar = new Rect(card.Left + 10.0, top + 26.0,
                Math.Max(1.0, card.Width - 20.0), 13.0);
            dc.DrawRoundedRectangle(CloneWithOpacity(border, 0.22),
                new Pen(border, 0.7), bar, 2.0, 2.0);
            var tones = new[] { 0.95, 0.72, 0.50, 0.30 };
            var cursor = bar.Left;
            for (var i = 0; i < percentages.Count; i++)
            {
                var span = i == percentages.Count - 1
                    ? Math.Max(0.0, bar.Right - cursor)
                    : Math.Max(0.0, bar.Width * percentages[i]);
                if (percentages[i] > Epsilon && span > Epsilon)
                    dc.DrawRectangle(CloneWithOpacity(accent, tones[colorIndices[i]]), null,
                        new Rect(cursor, bar.Top, span, bar.Height));
                cursor += span;
            }

            // Swatch + caliber label live IN the pool card. No reliance on
            // tooltip, color memory, hover or room inside a tiny 0% segment.
            // The fixed cells are responsive and correspond 1:1 to segments.
            var legendWidth = bar.Width / calibers.Count;
            for (var i = 0; i < calibers.Count; i++)
            {
                var x = bar.Left + i * legendWidth;
                var labelWidth = Math.Max(1.0, legendWidth - 17.0);
                var swatch = new Rect(x, top + 49.0, 10.0, 10.0);
                dc.DrawRectangle(CloneWithOpacity(accent, tones[colorIndices[i]]), null, swatch);
                dc.DrawText(CreateText(calibers[i], 10.2,
                        text, FontWeights.SemiBold, labelWidth),
                    new Point(x + 14.0, top + 46.0));
                DrawText(dc, $"{percentages[i] * 100.0:0.#}%",
                    new Point(x + 14.0, top + 63.0),
                    10.4, muted, FontWeights.SemiBold);
            }
        }

        Pool("SNG POOL",
            new[] { "9×18 / 9×19", "5.45", "12 GAUGE" }, sng,
            new[] { 0, 2, 3 }, 515.0);
        Pool("NATO POOL",
            new[] { ".45 ACP", "9×19", "5.56", "12 GAUGE" }, nato,
            new[] { 1, 0, 2, 3 }, 609.0);
    }

    // Shared density for the large Survival/Health aggregate panels. Rows are
    // column-aware; no independent sliders are stacked inside the same hit area.
    private const double CompactTrackHeight = 82.0;
    // Five drink and three medicine duration lanes in a single chart,
    // followed by the existing three native short-effect controls.
    // Renderer and MeasureOverride share this responsive height.
    private static int ConsumableSecondaryColumns(double availableWidth)
        => availableWidth >= 750.0 ? 3 : availableWidth >= 480.0 ? 2 : 1;

    private static double ConsumableTimelineHeight(double width)
    {
        var previewWidth = Math.Max(460.0, width - 34.0);
        var rows = (3 + ConsumableSecondaryColumns(previewWidth) - 1) /
            ConsumableSecondaryColumns(previewWidth);
        return 300.0 + rows * CompactTrackHeight +
            (rows - 1) * CompactRowGap + 12.0;
    }
    // Reserved space for plot ticks plus three independently readable stage lines.
    private const double HungerProfileHeight = 344.0;
    private const double CompactRowGap = 8.0;
    private const double CompactColumnGap = 10.0;

    private static int CompactColumns(double width)
        => width >= 700.0 ? 2 : 1;

    private static int CompactStageColumns(double width)
        => width >= 850.0 ? 3 : CompactColumns(width);

    private static double CompactGridEnd(double top, int count, int columns)
    {
        var rows = (count + columns - 1) / columns;
        return top + rows * CompactTrackHeight
            + Math.Max(0, rows - 1) * CompactRowGap;
    }

    private static Rect CompactSlot(double top, double width, int columns, int index)
    {
        var columnWidth = (width - (columns - 1) * CompactColumnGap) / columns;
        var column = index % columns;
        var row = index / columns;
        return new Rect(
            17.0 + column * (columnWidth + CompactColumnGap),
            top + row * (CompactTrackHeight + CompactRowGap),
            columnWidth, CompactTrackHeight);
    }

    private void DrawCompactTrack(
        DrawingContext dc,
        ParameterViewModel parameter,
        string label,
        string value,
        string minimumLabel,
        string maximumLabel,
        Rect card,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), card, 4.0, 4.0);
        var contentWidth = Math.Max(1.0, card.Width - 24.0);

        // Determine the actual value width before reserving space for the title.
        // The old fixed 155-DIP reserve truncated labels despite unused room.
        var valueText = CreateText(value, 10.7,
            parameter.IsControlEnabled ? text : muted, FontWeights.Bold, contentWidth);
        if (parameter.IsControlEnabled && parameter.IsValueModifiedFromReference())
            foreach (System.Text.RegularExpressions.Match token in
                System.Text.RegularExpressions.Regex.Matches(value, @"[+\-−]?\d+(?:\.\d+)?(?:[%x×])?"))
            {
                valueText.SetForegroundBrush(accent, token.Index, token.Length);
                valueText.SetFontSize(11.9, token.Index, token.Length);
            }
        // Measure after number highlights enlarge edited values; reserve a
        // genuine 10-DIP gap without making the compact card any wider.
        var labelWidth = Math.Max(1.0,
            contentWidth - valueText.WidthIncludingTrailingWhitespace - 10.0);
        dc.DrawText(CreateText(label, 10.2, text, FontWeights.Bold, labelWidth),
            new Point(card.Left + 12.0, card.Top + 7.0));
        dc.DrawText(valueText,
            new Point(card.Right - 12.0 - valueText.WidthIncludingTrailingWhitespace,
                card.Top + 6.0));

        // Move only the three colored short-effect rails up by four DIP:
        // their separate underline then fits above the existing min/max text.
        var effectLine = DrinkEffectLineBrush(parameter.Id) is not null;
        var track = new Rect(card.Left + 20.0,
            card.Top + (effectLine ? 41.0 : 45.0),
            Math.Max(1.0, card.Width - 40.0), 16.0);
        var y = track.Top + track.Height / 2.0;
        var currentX = ValueToX(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, track, reverse: false);
        var baseX = ValueToX(parameter.ReferenceNumber,
            parameter.Minimum, parameter.Maximum, track, reverse: false);

        dc.DrawLine(new Pen(border, 4.0),
            new Point(track.Left, y), new Point(track.Right, y));
        if (parameter.IsControlEnabled)
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.72), 4.5),
                new Point(track.Left, y), new Point(currentX, y));
        DrawDrinkEffectLine(dc, parameter, track.Left, currentX, y);

        dc.DrawLine(new Pen(muted, 2.0),
            new Point(baseX, y - 10.0), new Point(baseX, y + 10.0));
        dc.DrawEllipse(panel,
            new Pen(parameter.IsControlEnabled ? accent : muted,
                IsSelected(parameter) ? 2.8 : 1.9),
            new Point(currentX, y), 7.0, 7.0);
        if (parameter.IsControlEnabled)
            dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

        AddHorizontalTarget(parameter,
            new Rect(track.Left - 7.0, track.Top - 3.0,
                track.Width + 14.0, track.Height + 6.0),
            track, parameter.Minimum, parameter.Maximum);

        dc.DrawText(CreateText(minimumLabel, 9.2, muted, FontWeights.Normal,
                Math.Max(1.0, track.Width / 2.0 - 4.0)),
            new Point(track.Left, card.Top + 64.0));
        var maxLabelText = CreateText(maximumLabel, 9.2, muted,
            FontWeights.Normal, Math.Max(1.0, track.Width / 2.0 - 4.0));
        dc.DrawText(maxLabelText,
            new Point(track.Right - maxLabelText.WidthIncludingTrailingWhitespace,
                card.Top + 64.0));
    }

    // Small paired panels share this card. Values and titles occupy separate
    // lines and text is trimmed inside the card, never across the next column.
    // Exact axis values are given their own line in each half of a crosshair
    // board. This keeps labels and current/Base Preset values legible at 520 px.
    private void DrawDualParameterHeading(
        DrawingContext dc,
        ParameterViewModel leftParameter,
        string leftLabel,
        ParameterViewModel rightParameter,
        string rightLabel,
        double top,
        Brush text,
        Brush accent)
    {
        var leftX = 18.0;
        var rightX = ActualWidth / 2.0 + 8.0;
        var cellWidth = Math.Max(1.0, (ActualWidth - 54.0) / 2.0);

        void Cell(ParameterViewModel parameter, string label, double x)
        {
            dc.DrawText(CreateText(label, 9.2, text, FontWeights.Bold,
                    Math.Max(1.0, cellWidth - 110.0)), new Point(x, top));
            var changed = parameter.IsControlEnabled &&
                parameter.IsValueModifiedFromReference();
            var current = CreateText($"{parameter.CurrentNumber:0.##}x",
                changed ? 11.3 : 10.1, changed ? accent : text,
                FontWeights.SemiBold, cellWidth);
            dc.DrawText(current, new Point(
                x + cellWidth - current.WidthIncludingTrailingWhitespace, top));
        }

        Cell(leftParameter, leftLabel, leftX);
        Cell(rightParameter, rightLabel, rightX);
    }

    private static string ComparablePairedValue(ParameterViewModel parameter, double rawValue)
        => parameter.Id switch
        {
            "combat.mutantDamage" or "combat.mutantAttackCooldown" or
            "stamina.waterMediumDrain" or "stamina.waterStrongDrain" => $"{rawValue:0.##}x",
            "stamina.maxSP" => $"{rawValue:0} SP",
            "stamina.regeneration" => $"{rawValue:0.##} SP/s",
            "health.ecoMedkitAntirad" or "health.antiRadRadiation" => $"{rawValue:0} radiation",
            "health.lowHealthRegen" => $"{rawValue:0.0} rate",
            "combat.explosionDamage" or
            "economy.fastTravelCost" or "economy.upgradeCost" or
            "economy.traderAmmoStock" or "economy.medicStock" or
            "equipment.ammoWeight" or "equipment.consumableWeight" or
            "environment.anomalyDamage" or "environment.razorBleeding" or
            "environment.psyAnomalyIntensity" => $"{rawValue:0.##}x",
            "economy.repairCostBase" or
            "environment.flycatcherMovementSpeed" or
            "environment.flycatcherTurnSpeed" => $"{rawValue * 100.0:0.#}%",
            "environment.diamondBleeding" => $"{rawValue:0.#} bleed pts",
            "stamina.vaultMaxHeight" => $"{rawValue:0} uu",
            "stamina.maxCarryWeight" => $"{rawValue:0} kg",
            "loot.npcMedicalChance" or "loot.npcWeaponAmmoSupply" => $"{rawValue:0.##}x",
            "loot.destructibleAmmoQuantity" or "loot.destructibleGrenadeQuantity" or
            "loot.cheapStashAmmoQuantity" or "loot.mediumStashAmmoQuantity" or
            "loot.expensiveStashAmmoQuantity" or
            "loot.ammoBoxWeight918" or "loot.ammoBoxWeight045" or
            "loot.ammoBoxWeight545" or "loot.ammoBoxWeight012" => $"{rawValue:0.##}x",
            "loot.cheapStashGrenadeChance" or "loot.expensiveStashMoneyChance" =>
                $"{rawValue * 100.0:0}%",
            "loot.cheapStashGrenadeQuantity" => $"{rawValue:0}",
            _ => FormatCompact(rawValue)
        };

    private void DrawHorizontalTrackCard(
        DrawingContext dc,
        ParameterViewModel? parameter,
        string label,
        Rect card,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel,
        bool pairedValueFormatting = false,
        bool hideBaseText = false,
        bool healthTypography = false,
        bool combatTypography = false)
    {
        var hovered = parameter is not null && parameter.IsControlEnabled &&
            ReferenceEquals(_hoverTarget?.Parameter, parameter);
        dc.DrawRoundedRectangle(panel,
            new Pen(border, 1.0),
            card, 4.0, 4.0);
        var textWidth = Math.Max(1.0, card.Width - 24.0);
        var labelSize = healthTypography ? HealthTypography.Label
            : combatTypography ? 11.2 : 9.8;
        var axisSize = healthTypography ? HealthTypography.Axis
            : combatTypography ? 10.0 : 9.0;
        // Reserve the actual width of the current HP/rate readout, not a
        // hard-coded 150 DIP lane that would clip health names on small windows.
        var valueReserve = (healthTypography || combatTypography) && parameter is not null
            ? CreateText(combatTypography ? $"{parameter.CurrentNumber:0.##}x" : FriendlyValue(parameter),
                (healthTypography ? HealthTypography.Value : 12.0) + 1.2,
                text, FontWeights.SemiBold).WidthIncludingTrailingWhitespace + 12.0
            : 126.0;
        dc.DrawText(CreateText(label, labelSize, text, FontWeights.Bold,
                Math.Max(1.0, card.Width - 24.0 - valueReserve)),
            new Point(card.Left + 12.0, card.Top + 5.0));

        if (parameter is null)
        {
            dc.DrawText(CreateText("CFG PARAMETER UNAVAILABLE", 9.0, muted,
                    FontWeights.Normal, textWidth),
                new Point(card.Left + 12.0, card.Top + 22.0));
            return;
        }

        // Stamina Usage: one readable header line; the muted rail tick
        // alone communicates the selected reference difficulty.
        var value = combatTypography ? $"{parameter.CurrentNumber:0.##}x"
            : pairedValueFormatting ? ComparablePairedValue(parameter, parameter.CurrentNumber)
            : FriendlyValue(parameter);
        var baseSize = healthTypography ? HealthTypography.Value
            : combatTypography ? 12.0 : pairedValueFormatting ? 11.1 : 10.1;
        var valueText = CreateText(value, baseSize,
            parameter.IsControlEnabled ? text : muted, FontWeights.SemiBold, textWidth);
        if (parameter.IsControlEnabled && parameter.IsValueModifiedFromReference())
        {
            foreach (System.Text.RegularExpressions.Match token in
                System.Text.RegularExpressions.Regex.Matches(
                    value, @"[+\-−]?\d+(?:\.\d+)?(?:[%x×])?"))
            {
                valueText.SetForegroundBrush(accent, token.Index, token.Length);
                valueText.SetFontSize(baseSize + 1.2, token.Index, token.Length);
            }
        }
        dc.DrawText(valueText,
            new Point(card.Right - 12.0 - valueText.WidthIncludingTrailingWhitespace,
                card.Top + 5.0));

        var track = new Rect(card.Left + 14.0, card.Top + 34.0,
            Math.Max(1.0, card.Width - 28.0), 12.0);
        var y = track.Top + track.Height / 2.0;

        dc.DrawLine(new Pen(border, 3.5),
            new Point(track.Left, y), new Point(track.Right, y));
        var x = ValueToX(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, track, reverse: false);
        if (parameter.IsControlEnabled)
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.65), 3.5),
                new Point(track.Left, y), new Point(x, y));

        DrawReferenceTick(dc, parameter, track, y, muted);
        dc.DrawEllipse(panel,
            new Pen(parameter.IsControlEnabled ? accent : muted,
                hovered || IsSelected(parameter) ? 2.5 : 1.5),
            new Point(x, y), 6.0, 6.0);
        if (parameter.IsControlEnabled)
            dc.DrawEllipse(accent, null, new Point(x, y), 2.5, 2.5);

        AddHorizontalTarget(parameter,
            new Rect(track.Left - 7.0, track.Top - 4.0,
                track.Width + 14.0, track.Height + 8.0),
            track, parameter.Minimum, parameter.Maximum);

        var halfWidth = Math.Max(1.0, track.Width / 2.0 - 4.0);
        dc.DrawText(CreateText(FormatCompact(parameter.Minimum), axisSize,
                muted, FontWeights.Normal, halfWidth),
            new Point(track.Left, card.Top + 52.0));
        var right = CreateText(FormatCompact(parameter.Maximum), axisSize,
            muted, FontWeights.Normal, halfWidth);
        dc.DrawText(right, new Point(
            track.Right - right.WidthIncludingTrailingWhitespace,
            card.Top + 52.0));
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

        var enabled = parameter.IsControlEnabled;
        var hovered = enabled && _hoverTarget?.Kind == HitKind.Toggle
            && ReferenceEquals(_hoverTarget.Parameter, parameter);

        // Same 20-DIP square, 3-DIP corners and gold check mark as
        // the WPF CheckBox template. The card and caption never glow.
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0),
            rect, 4.0, 4.0);
        var box = new Rect(rect.Right - BooleanCheckboxChrome.Size - 11.0,
            rect.Top + (rect.Height - BooleanCheckboxChrome.Size) / 2.0,
            BooleanCheckboxChrome.Size, BooleanCheckboxChrome.Size);
        BooleanCheckboxChrome.Draw(dc, box, parameter.CurrentBoolean,
            enabled, hovered, panel, border, accent);

        dc.DrawText(CreateText(label, 9.7,
                enabled ? text : muted, FontWeights.Bold,
                Math.Max(1.0, box.Left - rect.Left - 20.0)),
            new Point(rect.Left + 10.0,
                rect.Top + Math.Max(4.0, (rect.Height - 15.0) / 2.0)));

        // Keep the original click / keyboard / right-click hit contract.
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
        // Consistent centered card heading across every aggregate visual.
        DrawCenteredText(dc, title, new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);
        if (!string.IsNullOrWhiteSpace(subtitle))
            DrawCenteredText(dc, subtitle, new Point(ActualWidth / 2.0, 39.0),
                9.5, muted, FontWeights.Normal);
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
        dc.DrawLine(new Pen(muted, 2.0), new Point(x, y - 9.0), new Point(x, y + 9.0));
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

    private void AddChoiceTarget(ParameterViewModel parameter, Rect hitRect, Rect mapRect)
    {
        if (!parameter.IsControlEnabled)
            return;

        _hitTargets.Add(new HitTarget(parameter, hitRect, mapRect, HitKind.Choice,
            parameter.ChoiceMinimum, parameter.ChoiceMaximum, false));
    }

    private void AddRangeTarget(ParameterViewModel parameter, Rect hitRect, Rect mapRect, HitKind kind)
    {
        if (!parameter.IsControlEnabled)
            return;

        _hitTargets.Add(new HitTarget(parameter, hitRect, mapRect, kind,
            parameter.Minimum, parameter.Maximum, false));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var point = e.GetPosition(this);
        var target = FindTarget(point);
        if (target is null || !target.Parameter.IsControlEnabled)
            return;

        _selectedParameter = target.Kind == HitKind.FatigueReloadJoint
            ? ReloadEffectFor(target.Parameter) ?? target.Parameter
            : target.Parameter;
        _selectedHitKind = target.Kind;
        SetHoverTarget(target);
        RaiseParameterSelected(target.Parameter);

        if (target.Kind == HitKind.MixerSelect)
        {
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (target.Kind == HitKind.MixerBaseMarker)
        {
            target.Parameter.ResetToReference();
            _status.ClearTransient();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (target.Kind == HitKind.DrinkDurationOverride)
        {
            target.Parameter.DrinkDurationOverrideEnabled =
                !target.Parameter.DrinkDurationOverrideEnabled;
            _status.ClearTransient();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

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
        // Keep BOTH the edited value and the visible hover indicator on
        // the captured endpoint until mouse-up. A nearby opposite handle
        // must never appear to steal the current drag.
        HitTarget? target;
        if (Family == "NpcLootLoadout" && _dragTarget is not null
            && (_dragTarget.Kind == HitKind.RangeMin || _dragTarget.Kind == HitKind.RangeMax)
            && e.LeftButton == MouseButtonState.Pressed)
            target = _dragTarget;
        else
            target = FindTarget(point);
        SetHoverTarget(target);
        Cursor = target?.Kind switch
        {
            HitKind.Horizontal or HitKind.RangeMin or HitKind.RangeMax => Cursors.SizeWE,
            HitKind.Vertical => Cursors.SizeNS,
            HitKind.Knob => Cursors.Hand,
            HitKind.HumanJoint or HitKind.HungerJoint or HitKind.FatigueJoint or HitKind.FatigueReloadJoint => Cursors.SizeAll,
            HitKind.Toggle or HitKind.DrinkDurationOverride or HitKind.Choice or HitKind.MixerBaseMarker or HitKind.MixerSelect => Cursors.Hand,
            _ => Cursors.Arrow
        };

        if (_dragTarget is null || e.LeftButton != MouseButtonState.Pressed)
            return;

        UpdateTarget(_dragTarget, point);
        e.Handled = true;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        if (Family is not ("PlayerWeaponProfile" or "AnomalyGlobalDamage" or "AmmoDistribution" or "ConsumableMixer" or "StashRewardProfile" or "TraderStockBoard" or "PsyPhantomPressure" or "EquipmentWearBalance" or "ServiceCostsBoard" or "RadiationBalance" or "HumanCombatProfile"))
            return;

        var target = FindTarget(e.GetPosition(this));
        if (target is null || target.Kind != HitKind.Knob
            || !target.Parameter.IsControlEnabled)
            return;

        Focus();
        _selectedParameter = target.Parameter;
        _selectedHitKind = HitKind.Knob;
        SetHoverTarget(target);
        RaiseParameterSelected(target.Parameter);
        var direction = Math.Sign(e.Delta);
        if (direction == 0)
            return;

        target.Parameter.CurrentNumber = Snap(target.Parameter,
            target.Parameter.CurrentNumber + direction * target.Parameter.TickFrequency);
        EnforceFamilyConstraints(target.Parameter);
        _status.ClearTransient();
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_dragTarget is null)
            return;

        _dragTarget = null;
        ReleaseMouseCapture();
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (_dragTarget is null)
        {
            SetHoverTarget(null);
            Cursor = Cursors.Arrow;
        }
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (_dragTarget is null)
            return;

        _dragTarget = null;
        SetHoverTarget(null);
        Cursor = Cursors.Arrow;
        InvalidateVisual();
    }

    private void SetHoverTarget(HitTarget? target)
    {
        // HitTarget records are rebuilt on every render. Compare their source
        // parameter and geometry, not object identity, to avoid invalidating
        // the whole control for every mouse movement.
        if ((_hoverTarget is null && target is null)
            || (_hoverTarget is not null && target is not null
                && ReferenceEquals(_hoverTarget.Parameter, target.Parameter)
                && _hoverTarget.Kind == target.Kind
                && _hoverTarget.HitRect.Equals(target.HitRect)))
        {
            return;
        }

        _hoverTarget = target;
        ToolTip = target is null
            ? null
            : target.Kind switch
            {
                HitKind.Knob => $"{target.Parameter.Label}\nTurn around the dial or scroll to adjust · Right-click = Base Preset",
                HitKind.Toggle => $"{target.Parameter.Label}\nClick to toggle · Right-click = Base Preset",
                HitKind.DrinkDurationOverride =>
                    "Override Consumable Duration Limits\nOFF: native limits. ON: drinks up to 270 s and Vinca, Hercules, Psi Block up to 900 s.\nRight-click resets.",
                HitKind.Choice => $"{target.Parameter.Label}\nChoose a corpse-loot package · Right-click = Base Preset",
                HitKind.MixerBaseMarker => $"{target.Parameter.Label}\nClick to snap to selected Base Preset",
                HitKind.MixerSelect => $"{target.Parameter.Label}\nClick for detailed rationale · Use the fader rail to edit",
                HitKind.RangeMin => "NPC weapon condition MIN\nDrag or use arrow keys · Right-click = reset full range",
                HitKind.RangeMax => "NPC weapon condition MAX\nDrag or use arrow keys · Right-click = reset full range",
                HitKind.HumanJoint when Family == "EquipmentWearBalance" =>
                    "Armor Toughness + Weapon Wear / Shot\nDrag in both directions · Right-click resets both",
                HitKind.HumanJoint when Family == "FlycatcherSlowdown" =>
                    "Flycatcher Move + Turn Remaining\nDrag in both directions · Right-click resets both",
                _ => $"{target.Parameter.Label}\nDrag or use arrow keys to adjust · Right-click = Base Preset"
            };
        InvalidateVisual();
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);

        var target = FindTarget(e.GetPosition(this));
        // A disabled mixer strip remains selectable for its rationale but
        // right-click must not mutate a dependency-disabled source value.
        if (target is null || !target.Parameter.IsControlEnabled)
            return;

        _selectedParameter = target.Kind == HitKind.FatigueReloadJoint
            ? ReloadEffectFor(target.Parameter) ?? target.Parameter
            : target.Parameter;
        _selectedHitKind = target.Kind == HitKind.MixerBaseMarker ? HitKind.Vertical : target.Kind;
        SetHoverTarget(target);
        RaiseParameterSelected(target.Parameter);
        if (target.Kind == HitKind.DrinkDurationOverride)
        {
            target.Parameter.DrinkDurationOverrideEnabled = false;
            _status.ClearTransient();
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        if (target.Kind != HitKind.FatigueReloadJoint)
            target.Parameter.ResetToReference();
        if (target.Kind is HitKind.FatigueJoint or HitKind.FatigueReloadJoint)
        {
            _fatigueReloadInteraction = target.Kind == HitKind.FatigueReloadJoint;
            FatigueEffectFor(target.Parameter)?.ResetToReference();
        }
        if (target.Kind == HitKind.HungerJoint)
            P(target.Parameter.Id == "survival.hungerMediumThreshold"
                ? "survival.hungerMediumStaminaRegen"
                : "survival.hungerStrongStaminaRegen")?.ResetToReference();
        if (target.Kind == HitKind.HumanJoint)
            P(Family switch
            {
                "PsyPhantomPressure" => "combat.psyPhantomCount",
                "EquipmentWearBalance" => "equipment.weaponWearPerShot",
                "FlycatcherSlowdown" => "environment.flycatcherTurnSpeed",
                _ => "combat.npcWeaponDamage"
            })?.ResetToReference();
        EnforceFamilyConstraints(target.Parameter);
        _status.ShowTransient(target.Kind == HitKind.HumanJoint
            ? (Family switch
            {
                "PsyPhantomPressure" => "Psy Cycle Wait + Phantom Count reset to Base Preset.",
                "EquipmentWearBalance" => "Armor Toughness + Weapon Wear reset to Base Preset.",
                "FlycatcherSlowdown" => "Flycatcher Movement + Turn reset to Base Preset.",
                _ => "Enemy Health + Gun Damage reset to Base Preset."
            })
            : $"{ShortLabel(target.Parameter)} reset to Base Preset.");
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.R)
        {
            if (Family == "ConsumableMixer")
            {
                var durationAnchor = P("survival.waterInstantStaminaRestore");
                if (durationAnchor is not null)
                    durationAnchor.DrinkDurationOverrideEnabled = false;
            }
            foreach (var resetParameter in GetFamilyParameters())
                resetParameter.ResetToReference();

            _status.ShowTransient("Visual group reset to Base Preset.");
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        // A previously selected slider can become dependency-disabled when its
        // Master bundle is switched OFF. Keyboard input must respect that
        // transition just like mouse hit targets and the writer.
        if (_selectedParameter is null || !_selectedParameter.IsControlEnabled)
            return;

        if (_selectedHitKind == HitKind.DrinkDurationOverride && e.Key == Key.Space)
        {
            _selectedParameter.DrinkDurationOverrideEnabled =
                !_selectedParameter.DrinkDurationOverrideEnabled;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_selectedParameter.IsToggle && e.Key == Key.Space)
        {
            _selectedParameter.CurrentBoolean = !_selectedParameter.CurrentBoolean;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var isChoice = _selectedParameter.IsChoiceSlider && _selectedHitKind == HitKind.Choice;
        if (isChoice && (e.Key == Key.Space || e.Key == Key.Enter))
        {
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        var isRangeMin = _selectedParameter.IsRange && _selectedHitKind == HitKind.RangeMin;
        var isRangeMax = _selectedParameter.IsRange && _selectedHitKind == HitKind.RangeMax;
        if (!_selectedParameter.IsSlider && !isChoice && !isRangeMin && !isRangeMax)
            return;

        var stepCount = e.Key switch
        {
            Key.Up or Key.Right or Key.Add or Key.OemPlus => 1,
            Key.Down or Key.Left or Key.Subtract or Key.OemMinus => -1,
            Key.PageUp => 10,
            Key.PageDown => -10,
            _ => 0
        };

        var minimum = isChoice ? _selectedParameter.ChoiceMinimum : _selectedParameter.Minimum;
        var maximum = isChoice ? _selectedParameter.ChoiceMaximum : _selectedParameter.Maximum;
        var current = isChoice ? _selectedParameter.ChoiceIndex
            : isRangeMin ? _selectedParameter.CurrentMinNumber
            : isRangeMax ? _selectedParameter.CurrentMaxNumber
            : _selectedParameter.CurrentNumber;
        var step = isChoice ? _selectedParameter.ChoiceTickFrequency :
            ConsumableDurationStep(_selectedParameter);

        double next;
        if (e.Key == Key.Home)
            next = minimum;
        else if (e.Key == Key.End)
            next = maximum;
        else if (stepCount != 0)
            next = current + stepCount * step;
        else
            return;

        if (isChoice)
            _selectedParameter.ChoiceIndex = Math.Clamp(next, minimum, maximum);
        else if (isRangeMin)
            _selectedParameter.CurrentMinNumber = Snap(_selectedParameter, next);
        else if (isRangeMax)
            _selectedParameter.CurrentMaxNumber = Snap(_selectedParameter, next);
        else
            _selectedParameter.CurrentNumber = Snap(_selectedParameter, next);

        EnforceFamilyConstraints(_selectedParameter);
        _status.ClearTransient();
        InvalidateVisual();
        e.Handled = true;
    }

    private void UpdateTarget(HitTarget target, Point point)
    {
        // Also guard an already-captured drag if its parent switch becomes OFF.
        if (!target.Parameter.IsControlEnabled || target.Kind == HitKind.Toggle)
            return;

        if (target.Kind is HitKind.FatigueJoint or HitKind.FatigueReloadJoint)
        {
            _fatigueReloadInteraction = target.Kind == HitKind.FatigueReloadJoint;
            var threshold = target.Parameter;
            var effect = FatigueEffectFor(threshold);
            if (effect is null || !threshold.IsControlEnabled || !effect.IsControlEnabled)
                return;
            var x = Math.Clamp((point.X - target.MapRect.Left) /
                Math.Max(1.0, target.MapRect.Width), 0, 1);
            var y = Math.Clamp((target.MapRect.Bottom - point.Y) /
                Math.Max(1.0, target.MapRect.Height), 0, 1);
            var maximum = Family == "FatigueCurve" &&
                _fatigueReloadInteraction ? FatigueReloadMaximum() : FatigueAimMaximum();
            // White reload ring edits only its own penalty. The gold onset
            // threshold is exclusively owned by the gold aim ring.
            if (!_fatigueReloadInteraction)
                threshold.CurrentNumber = Snap(threshold, threshold.Minimum +
                    x * (threshold.Maximum - threshold.Minimum));
            effect.CurrentNumber = Snap(effect, Math.Clamp(y * maximum,
                effect.Minimum, effect.Maximum));
            EnforceFamilyConstraints(threshold);
            _status.ClearTransient();
            InvalidateVisual();
            return;
        }
        if (target.Kind == HitKind.HungerJoint)
        {
            var threshold = target.Parameter;
            var effect = P(threshold.Id == "survival.hungerMediumThreshold"
                ? "survival.hungerMediumStaminaRegen"
                : "survival.hungerStrongStaminaRegen");
            if (effect is null || !threshold.IsControlEnabled || !effect.IsControlEnabled)
                return;
            var x = Math.Clamp((point.X - target.MapRect.Left) /
                Math.Max(1.0, target.MapRect.Width), 0.0, 1.0);
            var y = Math.Clamp((target.MapRect.Bottom - point.Y) /
                Math.Max(1.0, target.MapRect.Height), 0.0, 1.0);
            threshold.CurrentNumber = Snap(threshold,
                threshold.Minimum + x * (threshold.Maximum - threshold.Minimum));
            // Joint Y spans actual negative recovery effects only: -100..0%.
            var signedEffect = -100.0 + y * 100.0;
            effect.CurrentNumber = Snap(effect,
                Math.Clamp(-signedEffect / 100.0, effect.Minimum, effect.Maximum));
            EnforceFamilyConstraints(threshold);
            _status.ClearTransient();
            InvalidateVisual();
            return;
        }

        if (target.Kind == HitKind.HumanJoint)
        {
            var h = P(Family switch
            {
                "PsyPhantomPressure" => "combat.psySpawnCycleDelay",
                "EquipmentWearBalance" => "equipment.armorDurability",
                "FlycatcherSlowdown" => "environment.flycatcherMovementSpeed",
                _ => "combat.npcHealth"
            });
            var d = P(Family switch
            {
                "PsyPhantomPressure" => "combat.psyPhantomCount",
                "EquipmentWearBalance" => "equipment.weaponWearPerShot",
                "FlycatcherSlowdown" => "environment.flycatcherTurnSpeed",
                _ => "combat.npcWeaponDamage"
            });
            if (h is null || d is null || !h.IsControlEnabled || !d.IsControlEnabled)
                return;
            var x = Math.Clamp((point.X - target.MapRect.Left) / Math.Max(1.0, target.MapRect.Width), 0.0, 1.0);
            var y = Math.Clamp((target.MapRect.Bottom - point.Y) / Math.Max(1.0, target.MapRect.Height), 0.0, 1.0);
            h.CurrentNumber = Snap(h, h.Minimum + x * (h.Maximum - h.Minimum));
            d.CurrentNumber = Snap(d, d.Minimum + y * (d.Maximum - d.Minimum));
            EnforceFamilyConstraints(h);
            EnforceFamilyConstraints(d);
            _status.ClearTransient();
            InvalidateVisual();
            return;
        }

        // Dial edits directly by pointer angle, unlike rectangular X/Y bars.
        // The lower 90-degree gap snaps to its nearest end stop. Clicking
        // the center selects a knob without changing its configured value.
        if (target.Kind == HitKind.Knob)
        {
            var center = new Point(target.MapRect.Left + target.MapRect.Width / 2,
                target.MapRect.Top + target.MapRect.Height / 2);
            var dx = point.X - center.X;
            var dy = point.Y - center.Y;
            if (dx * dx + dy * dy < 20.0 * 20.0)
                return;

            var angle = Math.Atan2(dy, dx) * 180.0 / Math.PI;
            var clockwise = (angle - WeaponKnobStartAngle + 360.0) % 360.0;
            var normalized = clockwise > WeaponKnobSweepAngle
                ? (clockwise < 315.0 ? 1.0 : 0.0)
                : clockwise / WeaponKnobSweepAngle;
            var value = target.VisualMinimum + normalized *
                (target.VisualMaximum - target.VisualMinimum);
            target.Parameter.CurrentNumber = Snap(target.Parameter, value);
            EnforceFamilyConstraints(target.Parameter);
            _status.ClearTransient();
            InvalidateVisual();
            return;
        }

        double ratio;

        if (target.Kind == HitKind.Choice && Family == "NpcLootLoadout")
        {
            target.Parameter.ChoiceIndex = target.VisualMinimum;
            _status.ClearTransient();
            InvalidateVisual();
            return;
        }

        if (target.Kind is HitKind.Horizontal or HitKind.RangeMin or HitKind.RangeMax)
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
        if (target.Kind == HitKind.Choice)
            target.Parameter.ChoiceIndex = Math.Round(raw, MidpointRounding.AwayFromZero);
        else if (target.Kind == HitKind.RangeMin)
            target.Parameter.CurrentMinNumber = Snap(target.Parameter, raw);
        else if (target.Kind == HitKind.RangeMax)
            target.Parameter.CurrentMaxNumber = Snap(target.Parameter, raw);
        else
            target.Parameter.CurrentNumber = Snap(target.Parameter, raw);

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
        if (Family == "NpcLootLoadout")
        {
            var condition = P("loot.droppedWeaponCondition");
            var lower = _hitTargets.FirstOrDefault(t =>
                ReferenceEquals(t.Parameter, condition) && t.Kind == HitKind.RangeMin);
            var upper = _hitTargets.FirstOrDefault(t =>
                ReferenceEquals(t.Parameter, condition) && t.Kind == HitKind.RangeMax);
            if (condition is not null && lower is not null && upper is not null
                && lower.HitRect.Contains(point))
            {
                var minX = ValueToX(condition.CurrentMinNumber,
                    condition.Minimum, condition.Maximum, lower.MapRect, false);
                var maxX = ValueToX(condition.CurrentMaxNumber,
                    condition.Minimum, condition.Maximum, upper.MapRect, false);
                var dMinX = Math.Abs(point.X - minX);
                var dMaxX = Math.Abs(point.X - maxX);
                var centerY = lower.MapRect.Top + lower.MapRect.Height / 2.0;
                if (dMinX <= 14.0 && dMaxX <= 14.0)
                {
                    var separation = Math.Abs(maxX - minX) < 16.0 ? 7.0 : 0.0;
                    var a = Math.Pow(dMinX, 2) +
                        Math.Pow(point.Y - (centerY - separation), 2);
                    var b = Math.Pow(dMaxX, 2) +
                        Math.Pow(point.Y - (centerY + separation), 2);
                    if (Math.Abs(a - b) > Epsilon)
                        return a < b ? lower : upper;
                }
                if (Math.Abs(dMinX - dMaxX) > 0.001)
                    return dMinX < dMaxX ? lower : upper;
                return _selectedHitKind == HitKind.RangeMax ? upper
                    : _selectedHitKind == HitKind.RangeMin ? lower
                    : point.Y > centerY ? upper : lower;
            }
        }

        // Fatigue rings can overlap because both axes share threshold X.
        // Select the nearest ring center, not whichever was drawn last.
        if (Family == "FatigueCurve")
        {
            HitTarget? nearest = null;
            var nearestDistance = double.PositiveInfinity;
            foreach (var candidate in _hitTargets)
            {
                if (candidate.Kind is not (HitKind.FatigueJoint or HitKind.FatigueReloadJoint)
                    || !candidate.HitRect.Contains(point))
                    continue;
                var cx = candidate.HitRect.Left + candidate.HitRect.Width / 2;
                var cy = candidate.HitRect.Top + candidate.HitRect.Height / 2;
                var distance = (point.X - cx) * (point.X - cx) +
                    (point.Y - cy) * (point.Y - cy);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = candidate;
                }
            }
            if (nearest is not null)
                return nearest;
        }
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
    {
        if (_parameters.TryGetValue(id, out var parameter))
            return parameter;

        // Some WPF data templates resolve Family before their Window-level
        // Parameters binding has fully settled. Recover the real source VM
        // from the bound collection instead of showing a false missing-CFG
        // placeholder. Rebind on Loaded also covers reused template instances.
        parameter = Parameters?.OfType<ParameterViewModel>()
            .FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.Ordinal));
        if (parameter is not null && GetFamilyIds(Family).Contains(id))
        {
            _parameters[id] = parameter;
            parameter.PropertyChanged += Parameter_PropertyChanged;
        }
        return parameter;
    }

    private bool IsSelected(ParameterViewModel? parameter)
        => parameter is not null && ReferenceEquals(parameter, _selectedParameter);

    // All eight drink durations step in real seconds: OFF = 1s, ON = 5s.
    private static double ConsumableDurationSecondsPerUnit(string id) => id switch
    {
        "survival.waterEffectDuration" => 30.0,
        "survival.energyDrinkEffectDuration" => 45.0,
        "survival.vodkaEffectDuration" or "survival.beerEffectDuration" => 20.0,
        "survival.magicVodkaEffectDuration" => 90.0,
        "survival.waterRegenDuration" or "survival.energyFullCostDuration" or
        "survival.energyPartialCostDuration" => 1.0,
        _ => 0.0
    };

    private double ConsumableDurationStep(ParameterViewModel parameter)
    {
        var secondsPerUnit = Family == "ConsumableMixer"
            ? ConsumableDurationSecondsPerUnit(parameter.Id) : 0.0;
        if (secondsPerUnit <= 0.0)
            return parameter.TickFrequency;
        var anchor = P("survival.waterInstantStaminaRestore");
        return (anchor?.DrinkDurationOverrideEnabled == true ? 5.0 : 1.0) / secondsPerUnit;
    }

    private double Snap(ParameterViewModel parameter, double raw)
    {
        var step = Math.Max(0.0000001, ConsumableDurationStep(parameter));
        var origin = Family == "ConsumableMixer" &&
            ConsumableDurationSecondsPerUnit(parameter.Id) > 0.0 ? 0.0 : parameter.Minimum;
        var snapped = origin +
                      Math.Round((raw - origin) / step, MidpointRounding.AwayFromZero) * step;

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

    private FormattedText CreateText(string value, double size, Brush brush, FontWeight weight,
        double maxWidth = double.PositiveInfinity)
    {
        // Across aggregate labels, suppress redundant Base 1x badges while
        // retaining the live value and non-unit reference values.
        value = System.Text.RegularExpressions.Regex.Replace(value,
            @"\s*(?:\||/)\s*BASE\s+1(?:\.0+)?x\b|\bBASE\s+1(?:\.0+)?x\b",
            "", System.Text.RegularExpressions.RegexOptions.IgnoreCase).TrimEnd();
        var formatted = new FormattedText(
            value,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            Math.Max(9.0, size),
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        if (!double.IsInfinity(maxWidth))
        {
            // Ellipsize instead of allowing long CFG labels and percentage
            // values to escape the card at 1024-wide / high-DPI layouts.
            formatted.MaxTextWidth = Math.Max(1.0, maxWidth);
            formatted.MaxLineCount = 1;
            formatted.Trimming = TextTrimming.CharacterEllipsis;
        }

        return formatted;
    }

    // Shared rule for compound gamer-facing captions. Numeric tokens map 1:1
    // to their source parameters: only changed values turn gold. Labels,
    // units, signs, percentage symbols and unchanged numbers stay neutral.
    // Callers must provide the change flag in the same order as numeric tokens.
    private void DrawCenteredNumericChanges(
        DrawingContext dc, string caption, Point center, double size,
        Brush normal, Brush changedBrush, FontWeight weight, params bool[] changed)
    {
        var formatted = CreateText(caption, size, normal, weight);
        var numbers = System.Text.RegularExpressions.Regex.Matches(
            caption, @"[+\-−]?\d+(?:\.\d+)?(?:[%x×])?");
        for (var i = 0; i < numbers.Count && i < changed.Length; i++)
            if (changed[i])
            {
                formatted.SetForegroundBrush(changedBrush,
                    numbers[i].Index, numbers[i].Length);
                formatted.SetFontSize(Math.Max(9.0, size) + 1.2,
                    numbers[i].Index, numbers[i].Length);
            }
        // Same centered alignment as existing labels, no layout shifts.
        dc.DrawText(formatted, new Point(
            center.X - formatted.Width / 2.0,
            center.Y - formatted.Height / 2.0));
    }

    private void DrawChangedNumbers(DrawingContext dc, string value, Point position,
        double size, Brush normal, Brush accent, FontWeight weight,
        bool modified, bool rightAligned = false)
    {
        var formatted = CreateText(value, size, normal, weight);
        if (modified)
        {
            var first = System.Text.RegularExpressions.Regex.Match(
                value, @"[+\-−]?\d+(?:\.\d+)?(?:[%x×])?");
            if (first.Success)
            {
                formatted.SetForegroundBrush(accent, first.Index, first.Length);
                formatted.SetFontSize(Math.Max(9.0, size) + 1.2,
                    first.Index, first.Length);
            }
        }
        dc.DrawText(formatted, new Point(
            rightAligned ? position.X - formatted.WidthIncludingTrailingWhitespace : position.X,
            position.Y));
    }

    private void DrawText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
    {
        const double edge = 12.0;
        var maxWidth = Math.Max(1.0, ActualWidth - edge - point.X);
        dc.DrawText(CreateText(value, size, brush, weight, maxWidth), point);
    }

    private void DrawRightAlignedText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
    {
        const double edge = 12.0;
        var right = Math.Min(point.X, ActualWidth - edge);
        var formatted = CreateText(value, size, brush, weight,
            Math.Max(1.0, right - edge));
        dc.DrawText(formatted,
            new Point(right - formatted.WidthIncludingTrailingWhitespace, point.Y));
    }

    private void DrawCenteredText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
    {
        const double edge = 12.0;
        var formatted = CreateText(value, size, brush, weight,
            Math.Max(1.0, ActualWidth - edge * 2.0));
        var left = Math.Clamp(
            point.X - formatted.WidthIncludingTrailingWhitespace / 2.0,
            edge, Math.Max(edge, ActualWidth - edge - formatted.WidthIncludingTrailingWhitespace));
        dc.DrawText(formatted, new Point(left, point.Y));
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

            "survival.waterInstantStaminaRestore" or
            "survival.waterStaminaEffectStrength" or
            "survival.energyDrinkStaminaRecoveryStrength" or
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

    private static double GetDesiredHeight(string family, double width)
        => family switch
        {
            "BlastDamageBoard" => 305.0,
            "CouponFlow" => CouponBoardHeight(width),
            "ServiceCostsBoard" => ServicePriceBoardHeight(width),
            "TraderStockBoard" => 242.0,
            "ItemWeightBoard" => EconomyBoardHeight(width, 2),
            "BreakableBoxYields" => GetMixerHeight(width, BoxYieldMixerChannels.Length),
            "AnomalyGlobalDamage" => 242.0,
            "FlycatcherSlowdown" => FlycatcherBoardHeight(width),
            "AnomalySpecialEffects" => EconomyBoardHeight(width, 3),
            "NpcLootLoadout" => 564.0,
            "WorldLootLayout" => 275.0,
            "StashRewardProfile" => width >= 740.0 ? 382.0 : 465.0,
            "PhysicalLimits" => 335.0,
            "StaminaCore" => 323.0,
            "MutantCombatPressure" => 149.0,
            "WaterStaminaPressure" => 171.0,
            "FatigueCurve" => CompactFatigueHeight(width),
            "EquipmentWearBalance" => 242.0,
            "PsyPhantomPressure" => 378.0,
            "HumanCombatProfile" => HumanCombatBoardHeight(width),
            "PlayerWeaponProfile" => 242.0,
            "CoreHealthDashboard" => width >= 700.0 ? 282.0 : 365.0,
            "SevereFatiguePenaltyBoard" => 376.0,
            "MasterMedicalHealing" => MasterCardBankHeight(width, 3),
            "MasterLowHealth" => MasterConditionalHeight(width),
            "MasterRadiationPenalty" => MasterConditionalHeight(width),
            "ConsumableMixer" => CompactConsumableHeight(width),
            "SleepingDashboard" => 370.0,
            "HungerLadder" => CompactHungerHeight(width),
            "StarvationTimeline" => MasterStarvationHeight(width),
            "RepeatedHitBand" => 455.0,
            "RadiationBalance" => 242.0,
            "TradingDashboard" => 273.0,
            "RepairCurve" => 324.0,
            "AmmoDistribution" => 715.0,
            _ => 340.0
        };

    // These use the same section coordinates and CompactGridEnd layout as
    // their renderers. Measure and OnRender must agree at every breakpoint.
    private static double CompactFatigueHeight(double width)
    {
        return SurvivalPrimaryPreviewTop + 80.0 + 51.0 + 365.0 + 12.0;
    }

    private static double CompactSleepHeight(double width)
    {
        var columns = CompactColumns(width);
        var benefitsTitle = CompactGridEnd(55.0, 2, columns) + 21.0;
        return CompactGridEnd(benefitsTitle + 25.0, 2, columns) + 14.0;
    }

    private static double ConsumableMedicineKnobTop(double width)
        => ConsumableDurationTop + ConsumableTimelineHeight(width) + 14.0;

    private static double CompactConsumableHeight(double width)
        => ConsumableMedicineKnobTop(width) + 181.0 + 14.0;

    private static double CompactHungerHeight(double width)
    {
        var previewBottom = SurvivalPrimaryPreviewTop + 80.0;
        var frameBottom = previewBottom + 55.0 + HungerProfileHeight;
        return frameBottom + 12.0;
    }

    private static IReadOnlyList<string> GetFamilyIds(string family)
        => family switch
        {
            "BlastDamageBoard" => new[]
            {
                "combat.explosionDamage"
            },
            "CouponFlow" => new[]
            {
                "economy.startingMoney",
                "economy.mainQuestReward",
                "economy.sideQuestReward"
            },
            "PhysicalLimits" => new[]
            {
                "stamina.vaultMaxHeight",
                "stamina.maxCarryWeight"
            },
            "TraderStockBoard" => new[]
            {
                "economy.traderAmmoStock",
                "economy.medicStock"
            },
            "ServiceCostsBoard" => new[]
            {
                "economy.fastTravelCost",
                "economy.upgradeCost",
                "economy.repairCostBase"
            },
            "ItemWeightBoard" => new[]
            {
                "equipment.ammoWeight",
                "equipment.consumableWeight"
            },
            "AnomalyGlobalDamage" => new[] { "environment.anomalyDamage" },
            "FlycatcherSlowdown" => new[]
            {
                "environment.flycatcherMovementSpeed",
                "environment.flycatcherTurnSpeed"
            },
            "AnomalySpecialEffects" => new[]
            {
                "environment.razorBleeding",
                "environment.diamondBleeding",
                "environment.psyAnomalyIntensity"
            },
            "NpcLootLoadout" => new[]
            {
                "loot.npcCorpseBonus",
                "loot.npcMedicalChance",
                "loot.npcWeaponAmmoSupply",
                "loot.droppedWeaponCondition"
            },
            "WorldLootLayout" => new[]
            {
                "loot.looseUniqueWeaponShuffle",
                "loot.worldWeaponShuffle",
                "loot.worldWeaponDensity",
                "loot.worldAttachmentShuffle",
                "loot.worldDetectors",
                "loot.blueprintShuffle"
            },
            "StashRewardProfile" => new[]
            {
                "loot.cheapStashAmmoQuantity",
                "loot.mediumStashAmmoQuantity",
                "loot.expensiveStashAmmoQuantity",
                "loot.cheapStashGrenadeChance",
                "loot.cheapStashGrenadeQuantity",
                "loot.expensiveStashMoneyChance"
            },
            "BreakableBoxYields" => Array.Empty<string>(),
            "MutantCombatPressure" => new[] { "combat.mutantDamage", "combat.mutantAttackCooldown" },
            "StaminaCore" => new[] {
                "stamina.maxSP", "stamina.regeneration",
                "stamina.vaultMaxHeight", "stamina.maxCarryWeight" },
            "WaterStaminaPressure" => new[] { "stamina.waterMediumDrain", "stamina.waterStrongDrain" },
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
                "combat.npcHipAccuracy",
                "combat.explosionDamage"
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
                "health.regenerationScalar",
                "health.lowHealthRegen"
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
                "survival.waterInstantStaminaRestore",
                "survival.energyDrinkInstantStaminaRestore",
                "survival.waterStaminaEffectStrength",
                "survival.energyDrinkStaminaRecoveryStrength",
                "survival.waterEffectDuration",
                "survival.waterRegenDuration",
                "survival.energyDrinkEffectDuration",
                "survival.energyFullCostDuration",
                "survival.energyPartialCostDuration",
                "survival.vodkaEffectDuration",
                "survival.beerEffectDuration",
                "survival.magicVodkaEffectDuration",
                "survival.vincaBleedRecovery",
                "survival.vincaDuration",
                "survival.herculesCarryBonus",
                "survival.herculesDuration",
                "survival.psiBlockRecovery",
                "survival.psiBlockDuration"
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
                "loot.destructibleAmmoQuantity",
                "loot.destructibleGrenadeQuantity",
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
        Knob,
        HumanJoint,
        HungerJoint,
        FatigueJoint,
        FatigueReloadJoint,
        MixerBaseMarker,
        MixerSelect,
        DrinkDurationOverride,
        Toggle,
        Choice,
        RangeMin,
        RangeMax
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

public sealed class AggregateParameterSelectedEventArgs : EventArgs
{
    public AggregateParameterSelectedEventArgs(ParameterViewModel parameter) => Parameter = parameter;
    public ParameterViewModel Parameter { get; }
}
