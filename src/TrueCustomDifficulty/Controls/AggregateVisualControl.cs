using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Reusable non-radar aggregate editor. Different Family values render the same backing
/// ParameterViewModels as curves, faders, dashboards, ladders, timelines, bands,
/// balance meters or distributions without changing preset/writer semantics.
/// </summary>
public sealed partial class AggregateVisualControl : FrameworkElement
{
    private const double Epsilon = 0.000001;

    private readonly Dictionary<string, ParameterViewModel> _parameters = new(StringComparer.Ordinal);
    // Read-only cross-panel inputs: visual calculations only, never reset/written here.
    private readonly Dictionary<string, ParameterViewModel> _linkedSources = new(StringComparer.Ordinal);
    // Read-only cross-tab input to Starvation's hypothetical full-HP timeline.
    // Deliberately excluded from the Starvation family's editable/reset members.
    private ParameterViewModel? _starvationMaxHp;
    private readonly List<HitTarget> _hitTargets = new();
    private readonly VisualStatusMessage _status;
    private bool _fatigueReloadInteraction;
    // RAD inspection only: no runtime clock, no persisted field simulation.
    private ParameterViewModel? _radiationMaxHp;
    private double _radLevel = 0.0;
    private bool _radRingDragging;
    private bool _radRingFocus;
    // Keep the completed 360-degree clockwise drag at 100 RAD.
    private bool _radRingHadScrub;
    private bool _radRingCompleted;

    // Per-item visual mode; native Weight and Chance values always stay independent.
    private readonly bool[] _stashMedicalShowChance = new bool[4];
    private readonly List<(Rect Hit, int Index, bool Chance)> _stashMedicalModes = new();
    private int _stashMedicalHoveredMode = -1;
    private bool _stashMedicalHoveredChance;
    private HitTarget? _dragTarget;
    private HitTarget? _hoverTarget;
    private int _hoverCouponMarker = -1;
    private readonly List<(Rect Hit, double Value, ParameterViewModel Parameter, int Index)> _couponMarkers = new();
    private ParameterViewModel? _selectedParameter;
    // Distinguish the two handles that edit one range-valued source parameter.
    private HitKind? _selectedHitKind;

    // The aggregate anchor owns the visible row, but each mixer channel must
    // activate its own parameter rationale in the parent view model.
    public event EventHandler<AggregateParameterSelectedEventArgs>? ParameterSelected;

    private void RaiseParameterSelected(ParameterViewModel parameter)
    {
        if (Family is "AnomalyGlobalDamage" or "FlycatcherSlowdown" or "AnomalySpecialEffects" or "ServiceCostsBoard"
            or "CouponFlow" or "TraderStockBoard" or "ItemWeightBoard" or "EquipmentWearBalance"
            or "StashContentMix" or "StashMedicalComposition"
            or "StashGear" or "NpcLootLoadout" or "HumanCombatProfile" or "PlayerWeaponProfile"
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

    // Preserve Base Preset for native field math; the global category panel
    // never filters knobs by rank, branch, prototype or difficulty.
    public static readonly DependencyProperty SelectedReferenceDifficultyProperty =
        DependencyProperty.Register(
            nameof(SelectedReferenceDifficulty),
            typeof(string),
            typeof(AggregateVisualControl),
            new FrameworkPropertyMetadata("Novice",
                FrameworkPropertyMetadataOptions.AffectsRender, OnReferenceDifficultyChanged));

    private static void OnReferenceDifficultyChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (AggregateVisualControl)d;
        if (control.Family != "StashContentMix")
            return;
        // No rank/branch selection exists in this global category UI.
        control.InvalidateVisual();
    }

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

    public string SelectedReferenceDifficulty
    {
        get => (string)GetValue(SelectedReferenceDifficultyProperty);
        set => SetValue(SelectedReferenceDifficultyProperty, value);
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
        foreach (var parameter in _linkedSources.Values)
            parameter.PropertyChanged -= Parameter_PropertyChanged;
        _linkedSources.Clear();
        if (_starvationMaxHp is not null)
            _starvationMaxHp.PropertyChanged -= Parameter_PropertyChanged;
        _starvationMaxHp = null;
        if (_radiationMaxHp is not null)
            _radiationMaxHp.PropertyChanged -= Parameter_PropertyChanged;
        _radiationMaxHp = null;

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

        // Read-only full-HP reference. Never part of Radiation family reset/writes.
        if (Family == "RadiationBalance" && Parameters is not null)
        {
            _radiationMaxHp = Parameters.OfType<ParameterViewModel>()
                .FirstOrDefault(p => p.Id == "health.maxHP");
            if (_radiationMaxHp is not null)
                _radiationMaxHp.PropertyChanged += Parameter_PropertyChanged;
        }

        var linkedIds = Family switch
        {
            "ConsumableMixer" => new[] { "stamina.maxSP", "stamina.maxCarryWeight" },
            "SleepingDashboard" => new[] { "health.maxHP" },
            "FatigueCurve" => new[] { "survival.sleepEligibilityThreshold" },
            "RepairCurve" => new[] { "economy.repairCostBase" },
            // Preview-only cross-family inputs: observe source changes without
            // adding them to this board's edit/reset membership.
            "StashContentMix" => new[]
            {
                "loot.stashMedicalBandageBias", "loot.stashMedicalMedkitBias",
                "loot.stashMedicalArmyMedkitBias", "loot.stashMedicalAntiRadBias",
                "loot.stashMedicalBandageChanceScale", "loot.stashMedicalMedkitChanceScale",
                "loot.stashMedicalArmyMedkitChanceScale", "loot.stashMedicalAntiRadChanceScale",
                // Medical/amount preview-only links remain independent of
                // the three ammo quantity sources owned by this family.
                "loot.stashMedkitMinCount", "loot.stashMedkitMaxCount",
                "loot.stashArmyMedkitMinCount", "loot.stashArmyMedkitMaxCount",
                "loot.cheapStashGrenadeQuantity"
            },
            "StashMedicalComposition" => new[]
            {
                "loot.stashMixMedical"
            },
            "NpcLootLoadout" => new[] { "economy.traderMinBuyCondition" },
            _ => Array.Empty<string>()
        };
        if (Parameters is not null)
            foreach (var source in Parameters.OfType<ParameterViewModel>())
                if (linkedIds.Contains(source.Id, StringComparer.Ordinal))
                {
                    _linkedSources[source.Id] = source;
                    source.PropertyChanged += Parameter_PropertyChanged;
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

        _radRingDragging = false;
        _radRingFocus = false;
        _radRingHadScrub = false;
        _radRingCompleted = false;
        _dragTarget = null;
        _hoverTarget = null;
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
            case "StashContentMix":
                DrawStashContentMix(dc, text, muted, border, accent, panel);
                break;
            case "StashMedicalComposition":
                DrawStashMedicalComposition(dc, text, muted, border, accent, panel);
                break;
            case "StashGear":
                DrawStashGear(dc, text, muted, border, accent, panel);
                break;
            case "StaminaCore":
                DrawStaminaMovementBoard(dc, text, muted, border, accent, panel);
                break;
            case "WaterStaminaPressure":
                DrawWaterStaminaBoard(dc, text, muted, border, accent, panel);
                break;
            case "FatigueCurve":
                DrawFatigueCurve(dc, text, muted, border, soft, accent, panel);
                break;
            case "EquipmentWearBalance":
                DrawEquipmentWearBalance(dc, text, muted, border, soft, accent, panel);
                break;
            case "HumanCombatProfile":
                DrawHumanCombatProfile(dc, text, muted, border, soft, accent, panel);
                break;
            case "PlayerWeaponProfile":
                DrawYourWeaponsKnobs(dc, text, muted, border, accent, panel);
                break;
            case "CoreHealthDashboard":
                DrawCoreHealthDashboard(dc, text, muted, border, accent, panel);
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
        if (target is null || !target.Parameter.IsControlEnabled
            || IsNpcBonusSliderLocked(target.Parameter))
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
        if (Family == "HumanCombatProfile"
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
                or "RepairCurve"))
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

        _couponMarkers.Clear();
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
            var label = (tickValue / 1000.0).ToString("0", CultureInfo.InvariantCulture) + "K";
            DrawCenteredText(dc, label,
                new Point(tickX, card.Bottom - 16.0), 9.0,
                coupons.IsControlEnabled && _hoverCouponMarker == index ? Brushes.White : muted,
                FontWeights.SemiBold);
            if (coupons.IsControlEnabled)
                _couponMarkers.Add((
                    new Rect(tickX - 18.0, card.Bottom - 24.0, 36.0, 25.0),
                    tickValue, coupons, index));
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
        var readout = CreateText(value, modified ? 13.0 : 11.8,
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
        IndustrialRulerChrome.DrawActiveRailGlint(dc, track.Left, x, y,
            accent, parameter.IsControlEnabled);
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
            new Point(ActualWidth / 2.0, 21.0), VisualControlTypography.BoardTitle, accent, FontWeights.Bold);

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

    private void DrawAnomalyGlobalDamage(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawCenteredText(dc, "GLOBAL ANOMALY DAMAGE",
            new Point(ActualWidth / 2.0, 21.0), VisualControlTypography.BoardTitle, accent, FontWeights.Bold);
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
            VisualControlTypography.BoardTitle, accent, FontWeights.Bold);

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
        var xCaption = CreateText("MOVEMENT SPEED", 10.5, text, FontWeights.Bold);
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
        var yCaption = CreateText("TURN SPEED", 10.5, text, FontWeights.Bold);
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

    // Two independent source-native knobs share the Flycatcher board;
    // these do not alter the Flycatcher X/Y axes, difficulty, or damage math.
    private void DrawFlycatcherEffects(DrawingContext dc, Brush text, Brush muted,
        Brush border, Brush accent, Brush panel, Rect area)
    {
        DrawCenteredText(dc, "SPECIAL ANOMALY EFFECTS",
            new Point(area.Left + area.Width / 2.0, 21.0),
            VisualControlTypography.BoardTitle, accent, FontWeights.Bold);

        // Match the standard 181-DIP mechanical source knob grammar exactly.
        // Keep both editors side by side, even in the 248-DIP narrow panel.
        const double gap = 10.0;
        var knobWidth = Math.Min(194.0, (area.Width - gap) / 2.0);
        var rowWidth = knobWidth * 2.0 + gap;
        var left = area.Left + (area.Width - rowWidth) / 2.0;
        var top = area.Top + 34.0;

        DrawCenteredText(dc, "BLEEDING",
            new Point(left + knobWidth / 2.0, area.Top + 9.0),
            10.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "PSY DRAIN",
            new Point(left + knobWidth + gap + knobWidth / 2.0, area.Top + 9.0),
            10.0, muted, FontWeights.Bold);

        DrawSourceKnob(dc, P("environment.razorBleeding"), "GLASS SHARDS",
            new Rect(left, top, knobWidth, 181.0),
            text, muted, border, accent, panel);
        DrawSourceKnob(dc, P("environment.psyAnomalyIntensity"), "INTENSITY",
            new Rect(left + knobWidth + gap, top, knobWidth, 181.0),
            text, muted, border, accent, panel);
    }

    // Kept for existing family/preset compatibility. Use the SAME two-knob
    // implementation if this retired independent visual host is ever shown.
    private void DrawAnomalySpecialEffects(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        var width = Math.Min(440.0, Math.Max(248.0, ActualWidth - 30.0));
        DrawFlycatcherEffects(dc, text, muted, border, accent, panel,
            new Rect((ActualWidth - width) / 2.0, 42.0, width, 277.0));
    }

    // Contextual readout: only dynamic numeric tokens use the gold accent.
    private FormattedText CreateDerivedText(string label, double size,
        Brush neutral, Brush accent, FontWeight weight, double width)
    {
        var result = CreateText(label, size, neutral, weight, width);
        foreach (System.Text.RegularExpressions.Match number in
            System.Text.RegularExpressions.Regex.Matches(label, @"[+\-−]?\d+(?:\.\d+)?(?:[%x×])?"))
            result.SetForegroundBrush(accent, number.Index, number.Length);
        return result;
    }

    private void DrawDerivedCentered(DrawingContext dc, string value, Point top,
        double width, double size, Brush neutral, Brush accent, FontWeight weight)
    {
        var ft = CreateDerivedText(value, size, neutral, accent, weight, width);
        dc.DrawText(ft, new Point(top.X - ft.WidthIncludingTrailingWhitespace / 2.0, top.Y));
    }

    // Second in-dial readouts are derived from source values. Keep the
    // entire line muted; gold applies only to numbers when the derived result
    // differs from the selected difficulty reference. Signs and units stay grey.
    private void DrawKnobSecondaryReadout(
        DrawingContext dc, string label, Point centerTop, double maxWidth,
        Brush muted, Brush accent, bool modified)
    {
        var formatted = CreateText(label, 11.0, muted, FontWeights.Bold, maxWidth);
        if (modified)
        {
            foreach (System.Text.RegularExpressions.Match number in
                System.Text.RegularExpressions.Regex.Matches(label, @"\d+(?:\.\d+)?"))
                formatted.SetForegroundBrush(accent, number.Index, number.Length);
        }

        dc.DrawText(formatted, new Point(
            centerTop.X - formatted.WidthIncludingTrailingWhitespace / 2.0,
            centerTop.Y));
    }

    private void DrawDerivedRight(DrawingContext dc, string value, Point rightTop,
        double size, Brush neutral, Brush accent, FontWeight weight)
    {
        var right = Math.Min(rightTop.X, ActualWidth - 12.0);
        var ft = CreateDerivedText(value, size, neutral, accent, weight,
            Math.Max(1.0, right - 12.0));
        dc.DrawText(ft, new Point(right - ft.WidthIncludingTrailingWhitespace, rightTop.Y));
    }

    private void DrawDerivedLeft(DrawingContext dc, string value, Point topLeft,
        double width, double size, Brush neutral, Brush accent, FontWeight weight)
        => dc.DrawText(CreateDerivedText(value, size, neutral, accent, weight, width), topLeft);

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

    // Sourced 24x24 icon geometry rather than improvised hand-drawn glyphs.
    // Tabler Icons (MIT): first-aid-kit, bottle, ban; Pictogrammers MDI
    // (Apache-2.0): ammunition. Attribution: docs/ICON-ASSET-CREDITS.md.
    // Cached vector geometry renders natively on WPF/Wine without icon fonts.
    private static readonly Geometry[] NpcMedkitIcon =
    [
        Geometry.Parse("M8 8v-2a2 2 0 0 1 2 -2h4a2 2 0 0 1 2 2v2"),
        Geometry.Parse("M4 10a2 2 0 0 1 2 -2h12a2 2 0 0 1 2 2v8a2 2 0 0 1 -2 2h-12a2 2 0 0 1 -2 -2l0 -8"),
        Geometry.Parse("M10 14h4"),
        Geometry.Parse("M12 12v4")
    ];

    private static readonly Geometry[] NpcWaterBottleIcon =
    [
        Geometry.Parse("M10 5h4v-2a1 1 0 0 0 -1 -1h-2a1 1 0 0 0 -1 1v2"),
        Geometry.Parse("M14 3.5c0 1.626 .507 3.212 1.45 4.537l.05 .07a8.093 8.093 0 0 1 1.5 4.694v6.199a2 2 0 0 1 -2 2h-6a2 2 0 0 1 -2 -2v-6.2c0 -1.682 .524 -3.322 1.5 -4.693l.05 -.07a7.823 7.823 0 0 0 1.45 -4.537"),
        Geometry.Parse("M7 14.803a2.4 2.4 0 0 0 1 -.803a2.4 2.4 0 0 1 2 -1a2.4 2.4 0 0 1 2 1a2.4 2.4 0 0 0 2 1a2.4 2.4 0 0 0 2 -1a2.4 2.4 0 0 1 1 -.805")
    ];

    private static readonly Geometry[] NpcNoBonusIcon =
    [
        Geometry.Parse("M3 12a9 9 0 1 0 18 0a9 9 0 1 0 -18 0"),
        Geometry.Parse("M5.7 5.7l12.6 12.6")
    ];

    // MDI "ammunition" SVG path, native fill (not a stroked approximation).
    private static readonly Geometry NpcAmmunitionIcon = Geometry.Parse(
        "M14,22H10V21H14V22M13,10V7H11V10L10,11.5V20H14V11.5L13,10M12,2C12,2 11,3 11,5V6H13V5C13,5 13,3 12,2" +
        "M8,22H4V21H8V22M7,10V7H5V10L4,11.5V20H8V11.5L7,10M6,2C6,2 5,3 5,5V6H7V5C7,5 7,3 6,2" +
        "M20,22H16V21H20V22M19,10V7H17V10L16,11.5V20H20V11.5L19,10M18,2C18,2 17,3 17,5V6H19V5C19,5 19,3 18,2Z");

    private static void DrawNpcOutlineIcon(
        DrawingContext dc, Geometry[] paths, Point center, double size, Brush ink)
    {
        var scale = size / 24.0;
        dc.PushTransform(new MatrixTransform(scale, 0.0, 0.0, scale,
            center.X - 12.0 * scale, center.Y - 12.0 * scale));
        var pen = new Pen(ink, 2.0)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        foreach (var geometry in paths)
            dc.DrawGeometry(null, pen, geometry);
        dc.Pop();
    }

    private static void DrawNpcFilledIcon(
        DrawingContext dc, Geometry geometry, Point center, double size, Brush ink)
    {
        var scale = size / 24.0;
        dc.PushTransform(new MatrixTransform(scale, 0.0, 0.0, scale,
            center.X - 12.0 * scale, center.Y - 12.0 * scale));
        dc.DrawGeometry(ink, null, geometry);
        dc.Pop();
    }

    private static void DrawNpcCorpsePackageIcon(
        DrawingContext dc, string optionValue, Point center, Brush ink)
    {
        if (optionValue.Contains("NPC_Ammo_Smart", StringComparison.Ordinal))
        {
            // Clearly separated first-aid kit + real ammunition pictograms.
            DrawNpcOutlineIcon(dc, NpcMedkitIcon,
                new Point(center.X - 17.5, center.Y), 30.0, ink);
            DrawNpcFilledIcon(dc, NpcAmmunitionIcon,
                new Point(center.X + 17.5, center.Y), 29.0, ink);
        }
        else if (optionValue.Contains("NPC_Water_Smart", StringComparison.Ordinal))
        {
            DrawNpcOutlineIcon(dc, NpcWaterBottleIcon, center, 34.0, ink);
        }
        else
        {
            DrawNpcOutlineIcon(dc, NpcNoBonusIcon, center, 30.0, ink);
        }
    }

    // UI-only gate. The regular medical generator and both preset values
    // keep their original CFG behavior while these editors are greyed out.
    private bool NpcBonusPackageSelected()
    {
        var corpse = P("loot.npcCorpseBonus");
        return corpse is not null && corpse.CurrentText.Split(',',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Contains("NPC_Ammo_Smart", StringComparer.Ordinal);
    }

    private bool IsNpcBonusSliderLocked(ParameterViewModel? parameter)
        => Family == "NpcLootLoadout"
            && parameter is not null
            && (parameter.Id is "loot.npcMedicalChance" or "loot.npcWeaponAmmoSupply")
            && !NpcBonusPackageSelected();

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
        var cell = Math.Min(112.0, (width - 22.0 - (count - 1) * gap) / count);
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
            // Icon-only selector; the source-backed choice stays visible.
            DrawNpcCorpsePackageIcon(dc, corpse.ChoiceOptions[i].Value,
                new Point(rect.Left + rect.Width / 2.0, rect.Top + rect.Height / 2.0),
                hover ? Brushes.White : selected ? accent : text);
            if (corpse.IsControlEnabled)
                _hitTargets.Add(new HitTarget(corpse, rect, rect, HitKind.Choice, i, i, false));
        }

        var half = (width - gap) / 2.0;
        DrawHorizontalTrackCard(dc, medical, "MEDICAL DROPS",
            new Rect(margin, 155.0, half, 72.0),
            text, muted, border, accent, panel, pairedValueFormatting: true);
        DrawHorizontalTrackCard(dc, ammo, "NPC AMMO DROPS",
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
        var ammoPackageActive = NpcBonusPackageSelected();
        var countsModified = ammo.IsValueModifiedFromReference();
        var sampleCard = new Rect(margin, 238.0, width, 172.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 0.9),
            sampleCard, 4.0, 4.0);
        DrawText(dc, "BONUS AMMO PREVIEW",
            new Point(sampleCard.Left + 11.0, sampleCard.Top + 7.0),
            10.3, ammoPackageActive ? text : muted, FontWeights.Bold);

        // Nine read-only bonus-ammo samples form a tight central three-column
        // group; normal weapon-attached ammunition remains independent.
        const double previewGap = 6.0;
        var previewTotalWidth = Math.Min(516.0, sampleCard.Width - 22.0);
        var previewLeft = sampleCard.Left + (sampleCard.Width - previewTotalWidth) / 2.0;
        var previewWidth = (previewTotalWidth - 2.0 * previewGap) / 3.0;
        for (var i = 0; i < examples.Length; i++)
        {
            var sample = examples[i];
            var tile = new Rect(previewLeft
                    + (i % 3) * (previewWidth + previewGap),
                sampleCard.Top + 25.0 + (i / 3) * 41.0, previewWidth, 36.0);
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

        // This is a dual-ended native RANGE, not two normal slider
        // headers. Show the two editable figures together in the middle
        // of the card, immediately above their shared physical rail.
        var rangeCard = new Rect(margin, 420.0, width, 130.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0),
            rangeCard, 4.0, 4.0);
        DrawText(dc, "DROPPED WEAPON CONDITION",
            new Point(rangeCard.Left + 11.0, rangeCard.Top + 8.0),
            VisualControlTypography.CardTitle, text, FontWeights.Bold);
        var accepted = Linked("economy.traderMinBuyCondition");
        if (accepted is not null)
            DrawDerivedRight(dc, $"TRADER ACCEPTS ≥ {accepted.CurrentNumber * 100.0:0}%",
                new Point(rangeCard.Right - 11.0, rangeCard.Top + 8.0),
                8.8, muted, accent, FontWeights.SemiBold);

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
        if (accepted is not null)
        {
            // Trader floor is a filter, NOT a claim that every dropped weapon
            // in the surviving range is sold at a given price.
            var acceptedX = ValueToX(accepted.CurrentNumber, 0.0, 1.0, rail, false);
            dc.DrawLine(new Pen(muted, 1.6) { DashStyle = DashStyles.Dash },
                new Point(acceptedX, y - 16.0), new Point(acceptedX, y + 16.0));
        }

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
                var kgHover = enabled && _hoverTarget?.Kind == HitKind.Horizontal
                    && ReferenceEquals(_hoverTarget.Parameter, parameter)
                    && Math.Abs(_hoverTarget.VisualMinimum - kg) < Epsilon
                    && Math.Abs(_hoverTarget.VisualMaximum - kg) < Epsilon;
                var markerText = CreateText($"{kg:0}", 9.0,
                    kgHover ? Brushes.White : muted, FontWeights.SemiBold);
                var labelX = x - markerText.WidthIncludingTrailingWhitespace / 2.0;
                var labelY = card.Top + 70.0;
                dc.DrawText(markerText, new Point(labelX, labelY));
                AddHorizontalTarget(parameter,
                    new Rect(labelX - 4.0, labelY - 3.0,
                        markerText.WidthIncludingTrailingWhitespace + 8.0,
                        markerText.Height + 6.0),
                    new Rect(x, labelY, 1.0, Math.Max(1.0, markerText.Height)),
                    kg, kg);
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
            DrawCoreResourceRuler(dc, stamina, "MAX STAMINA", "stamina",
                new Rect(16.0, 51.0, cell, 105.0),
                text, muted, border, accent, panel);
        if (P("stamina.regeneration") is { } recovery)
            DrawCoreResourceRuler(dc, recovery, "BASE RECOVERY", "stamina/s",
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
        // Sleep availability is an independent CFG threshold, drawn as a
        // passive source-linked landmark, not a fourth fatigue stage editor.
        var sleepGate = Linked("survival.sleepEligibilityThreshold");
        if (sleepGate is not null)
            DrawDerivedRight(dc, $"SLEEP AVAILABLE {sleepGate.CurrentNumber:0} PTS",
                new Point(preview.Right - 12.0, preview.Top + 8.0),
                9.0, muted, accent, FontWeights.SemiBold);
        var axis = new Rect(preview.Left + 81, preview.Top + 32, preview.Width - 111, 12);
        double X(double value) => ValueToX(value, 0, 100, axis, false);
        var y = axis.Top + axis.Height / 2;
        if (sleepGate is not null)
        {
            var sleepX = X(sleepGate.CurrentNumber);
            dc.DrawLine(new Pen(CloneWithOpacity(text, .85), 1.6)
                { DashStyle = DashStyles.Dash },
                new Point(sleepX, y - 14.0), new Point(sleepX, y + 14.0));
            dc.DrawEllipse(panel, new Pen(text, 1.2),
                new Point(sleepX, y - 13.0), 3.0, 3.0);
        }
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
            rate, low, medium, severe, lowAim, mediumAim, severeAim,
            mediumReload, severeReload, text, muted, border, soft, accent, panel);
    }

    // Gold aim curve and white reload curve share fatigue-point thresholds
    // but have independent native Y scales and individually draggable rings.
    private void DrawFatigueCombined(DrawingContext dc, double top, double width,
        ParameterViewModel rate, ParameterViewModel low,
        ParameterViewModel medium, ParameterViewModel severe,
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
                VisualControlTypography.AxisTick, muted, FontWeights.SemiBold);
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
        // Real-time onset is a passive projection from 0 SleepinessPoints.
        // Keep threshold, onset, AIM and RELOAD within the original cells.
        var cellWidth = (frame.Width - 16.0) / 3.0;
        for (var i = 0; i < 3; i++)
        {
            var cell = new Rect(frame.Left + 8.0 + i * cellWidth,
                top + 260.0, cellWidth, 93.0);
            dc.PushClip(new RectangleGeometry(cell));
            var cx = cell.Left + cell.Width / 2.0;
            var stage = new[] { "LOW", "MEDIUM", "SEVERE" }[i];
            var thresholdText = $"{stage} AT {thresholds[i].CurrentNumber:0} PTS";
            var elapsed = StageOnsetEstimate(thresholds[i].CurrentNumber, rate.CurrentNumber);
            var timeText = $"AFTER {elapsed}";
            var headline = $"{thresholdText} {timeText}";
            // Measure headline before choosing a one- or two-line treatment.
            // Use the same larger stage text as Hunger, measuring the full
            // headline with clearance for selectively enlarged gold numbers.
            var wrap = cellWidth < 205.0 || CreateText(headline,
                VisualControlTypography.SurvivalStageSummary,
                text, FontWeights.SemiBold).WidthIncludingTrailingWhitespace > cellWidth - 22.0;
            if (wrap)
            {
                DrawCenteredNumericChanges(dc, thresholdText,
                    new Point(cx, top + 270.0), VisualControlTypography.SurvivalStageSummary, text, accent,
                    FontWeights.SemiBold, thresholds[i].IsValueModifiedFromReference());
                var count = System.Text.RegularExpressions.Regex.Matches(
                    timeText, @"\d+").Count;
                DrawCenteredNumericChanges(dc, timeText,
                    new Point(cx, top + 291.0), VisualControlTypography.SurvivalStageSummary, text, accent,
                    FontWeights.SemiBold, Enumerable.Repeat(true, count).ToArray());
            }
            else
            {
                var count = System.Text.RegularExpressions.Regex.Matches(
                    headline, @"\d+").Count;
                var highlight = new bool[count];
                if (count > 0)
                    highlight[0] = thresholds[i].IsValueModifiedFromReference();
                for (var token = 1; token < count; token++)
                    highlight[token] = true;
                DrawCenteredNumericChanges(dc, headline,
                    new Point(cx, top + 275.0), VisualControlTypography.SurvivalStageSummary, text, accent,
                    FontWeights.SemiBold, highlight);
            }
            DrawCenteredNumericChanges(dc,
                $"AIM TIME +{aims[i].CurrentNumber * 100:0.#}%",
                new Point(cx, top + (wrap ? 314.0 : 304.0)),
                VisualControlTypography.SurvivalStageSummary, text, accent, FontWeights.SemiBold,
                aims[i].IsValueModifiedFromReference());
            if (i > 0)
                DrawCenteredNumericChanges(dc,
                    $"RELOAD TIME +{reloads[i - 1].CurrentNumber * 100:0.##}%",
                    new Point(cx, top + (wrap ? 337.0 : 327.0)),
                    VisualControlTypography.SurvivalStageSummary, text, accent, FontWeights.SemiBold,
                    reloads[i - 1].IsValueModifiedFromReference());
            dc.Pop();
        }

    }

    // Master panels share Stamina Usage's compact source-first grammar.
    // Only ACTUAL native parameters are drawn; switch-disabled children retain
    // their staged values and remain non-interactive until enabled.
    private static Brush MasterDisabledInk(Brush text) => CloneWithOpacity(text, 0.70);



    private static double MasterConditionalHeight(double width) => 298.0;
    // The three healing modifiers share one aligned 0–200% bank at every width.
    private const double MasterMedicalBankTop = 103.0;
    private const double MasterMedicalBankHeight = 181.0;
    private const double MasterMedicalHealingHeight =
        MasterMedicalBankTop + MasterMedicalBankHeight + 12.0;
    private const double StarvationKnobsTop = 106.0;
    private const double StarvationKnobHeight = 181.0;
    private const double StarvationPreviewHeight = 176.0;
    private const double StarvationPreviewTop =
        StarvationKnobsTop + StarvationKnobHeight + 12.0;
    private const double StarvationEffectsTop =
        StarvationPreviewTop + StarvationPreviewHeight + 12.0;
    private static double MasterStarvationHeight(double width)
        => StarvationEffectsTop + 80.0 + 14.0;

    private void DrawMasterSectionTitle(DrawingContext dc, string title, Brush accent)
        => DrawCenteredText(dc, title, new Point(ActualWidth / 2.0, 21.0),
            VisualControlTypography.BoardTitle, accent, FontWeights.Bold);

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
            changed ? 13.0 : 11.8,
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
        IndustrialRulerChrome.DrawActiveRailGlint(dc, track.Left, currentX, y,
            accent, enabled);

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


    private static string MasterPercent(double value) => $"{value * 100:0.#}%";
    private static string MasterPositivePercent(double value) => $"+{value * 100:0.#}%";
    private static string MasterNegativePercent(double value) => $"-{value * 100:0.#}%";
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

    // Three independently writable source multipliers use one aligned,
    // compact instrument bank. Only actual native source tracks can be edited.
    // The 100% full-healing guide is separate from each difficulty reference.
    private void DrawMasterHealingLane(
        DrawingContext dc, ParameterViewModel? parameter, string label,
        Rect bank, int index, Brush text, Brush muted, Brush border,
        Brush soft, Brush accent, Brush panel)
    {
        var rowTop = bank.Top + 11.0 + index * 49.0;
        if (parameter is null)
        {
            DrawText(dc, label + "  ·  SOURCE UNAVAILABLE",
                new Point(bank.Left + 16.0, rowTop + 1.0),
                10.1, muted, FontWeights.SemiBold);
            return;
        }

        var enabled = parameter.IsControlEnabled;
        var changed = enabled && parameter.IsValueModifiedFromReference();
        var disabledInk = MasterDisabledInk(text);
        var value = MasterPercent(parameter.CurrentNumber);
        var valueText = CreateText(value, 12.6,
            enabled ? text : disabledInk, FontWeights.Bold);
        if (changed)
        {
            foreach (System.Text.RegularExpressions.Match token in
                System.Text.RegularExpressions.Regex.Matches(value, @"\d+(?:\.\d+)?"))
            {
                valueText.SetForegroundBrush(accent, token.Index, token.Length);
                valueText.SetFontSize(13.8, token.Index, token.Length);
            }
        }
        var labelWidth = Math.Max(1.0,
            bank.Width - 46.0 - valueText.WidthIncludingTrailingWhitespace - 10.0);
        dc.DrawText(CreateText(label, 10.3, enabled ? text : disabledInk,
                FontWeights.Bold, labelWidth),
            new Point(bank.Left + 16.0, rowTop));
        dc.DrawText(valueText, new Point(
            bank.Right - 16.0 - valueText.WidthIncludingTrailingWhitespace,
            rowTop - 2.0));

        var track = new Rect(bank.Left + 18.0, rowTop + 26.0,
            Math.Max(1.0, bank.Width - 36.0), 10.0);
        var y = track.Top + track.Height / 2.0;
        var currentX = ValueToX(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, track, false);
        var referenceX = ValueToX(parameter.ReferenceNumber,
            parameter.Minimum, parameter.Maximum, track, false);

        dc.DrawLine(new Pen(soft, 3.2),
            new Point(track.Left, y), new Point(track.Right, y));
        if (enabled)
            dc.DrawLine(new Pen(CloneWithOpacity(accent, .72), 3.2),
                new Point(track.Left, y), new Point(currentX, y));
        IndustrialRulerChrome.DrawActiveRailGlint(dc, track.Left, currentX, y,
            accent, enabled);

        // Only draw a separate 100% guide when it is inside the range;
        // at the new native endpoint, the 100% label already explains the limit.
        if (parameter.Minimum < 1.0 && parameter.Maximum > 1.0)
        {
            var fullHealingX = ValueToX(1.0, parameter.Minimum,
                parameter.Maximum, track, false);
            if (Math.Abs(fullHealingX - referenceX) >= 2.0)
                dc.DrawLine(new Pen(CloneWithOpacity(muted, .55), 1.0)
                    { DashStyle = DashStyles.Dash },
                    new Point(fullHealingX, y - 7.0),
                    new Point(fullHealingX, y + 7.0));
        }
        dc.DrawLine(new Pen(muted, 1.8),
            new Point(referenceX, y - 8.0),
            new Point(referenceX, y + 8.0));
        dc.DrawEllipse(panel,
            new Pen(enabled ? accent : muted,
                enabled && IsSelected(parameter) ? 2.4 : 1.5),
            new Point(currentX, y), 6.0, 6.0);
        if (enabled)
            dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

        AddHorizontalTarget(parameter,
            new Rect(track.Left - 6.0, track.Top - 6.0,
                track.Width + 12.0, track.Height + 12.0),
            track, parameter.Minimum, parameter.Maximum);

        if (index < 2)
            IndustrialChrome.DrawEngravedSeparator(dc,
                bank.Left + 14.0, bank.Right - 14.0,
                rowTop + 43.0, muted);
    }

    // These modifiers are independent source ValueMultipliers, not HP previews.
    // Keep the universal parent toggle; avoid a dangling third card and
    // compare all three on one continuous native 0–100% horizontal axis.
    private void DrawMasterMedicalHealing(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush soft, Brush accent, Brush panel)
    {
        DrawMasterSectionTitle(dc, "CONSUMABLE EFFECT PENALTIES", accent);
        DrawMasterBundleToggle(dc, P("advanced.masterEffectModifierMechanics"),
            "HEALING ITEM EFFICIENCY REDUCTION",
            text, muted, border, accent, panel);

        var bankWidth = Math.Min(680.0, Math.Max(1.0, ActualWidth - 34.0));
        var bank = new Rect((ActualWidth - bankWidth) / 2.0,
            MasterMedicalBankTop, bankWidth, MasterMedicalBankHeight);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .78),
            new Pen(border, .9), bank, 4.0, 4.0);

        var channels = new (string Id, string Label)[]
        {
            ("health.masterBandageModifier", "BANDAGE HEALING"),
            ("health.masterMedkitModifier", "MEDKIT HEALING"),
            ("health.masterArmyMedkitModifier", "ARMY MEDKIT HEALING")
        };
        for (var i = 0; i < channels.Length; i++)
            DrawMasterHealingLane(dc, P(channels[i].Id), channels[i].Label,
                bank, i, text, muted, border, soft, accent, panel);

        // The shared axis is a neutral guide, not a fourth editable control.
        var first = P(channels[0].Id);
        var scaleMin = first?.Minimum ?? 0.0;
        var scaleMax = first?.Maximum ?? 1.0;
        var left = bank.Left + 18.0;
        var right = bank.Right - 18.0;
        var scaleTop = bank.Bottom - 19.0;
        DrawText(dc, MasterPercent(scaleMin),
            new Point(left, scaleTop), 9.0, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, MasterPercent(scaleMax),
            new Point(right, scaleTop), 9.0, muted, FontWeights.Normal);
        if (scaleMin < 1.0 && scaleMax > 1.0)
        {
            var centerX = left + (1.0 - scaleMin) / (scaleMax - scaleMin) *
                (right - left);
            DrawCenteredText(dc, "100%", new Point(centerX, scaleTop),
                9.0, muted, FontWeights.Normal);
        }
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
        Brush accent, Brush panel)
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

    // Shared primary resource-ruler grammar for CORE HEALTH and CORE stamina.
    // Both use identical card geometry, typography, handles, native endpoints
    // and hit/ref mapping; only the source values and units differ.
    private void DrawCoreResourceRuler(
        DrawingContext dc, ParameterViewModel parameter, string label, string unit,
        Rect card, Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        IndustrialChrome.DrawRecessedCard(dc, card, panel, border);

        var active = parameter.IsControlEnabled;
        var modified = active && parameter.IsValueModifiedFromReference();
        var value = unit switch
        {
            "HP" or "stamina" => $"{parameter.CurrentNumber:0} {unit}",
            "x" => $"{parameter.CurrentNumber:0.##}x",
            "pts/s" => $"{parameter.CurrentNumber:0.000} pts/s",
            "%" => $"{(parameter.CurrentNumber > Epsilon ? "+" : "")}{parameter.CurrentNumber * 100.0:0.#}%",
            _ => FriendlyValue(parameter)
        };
        // Native 0.001-rate precision takes more room than HP/stamina, but uses
        // the same card, title/value positions, rail and modified-only gold.
        var valueSize = unit == "pts/s" ? 16.5
            : unit is "stamina" or "stamina/s" ? 16.0 : 19.0;
        var valueText = CreateText(value, valueSize, active ? text : muted,
            FontWeights.Bold, Math.Max(1.0, card.Width - 30.0));
        if (modified)
        {
            // Like Stamina Usage, only the edited numeric token turns gold;
            // HP, HP/s, stamina and stamina/s remain neutral; Base is not emphasized.
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
        IndustrialRulerChrome.DrawActiveRailGlint(dc, track.Left, currentX, y, accent, active);
        IndustrialRulerChrome.DrawNativeTicks(dc, track,
            parameter.Minimum, parameter.Maximum, parameter.TickFrequency, muted);
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
        var minValue = unit is "HP" or "stamina"
            ? $"{parameter.Minimum:0}" : FormatCompact(parameter.Minimum);
        var maxValue = unit is "HP" or "stamina"
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
        string? nativeUnit = null, bool prominent = false,
        bool positivePrefix = true, string? secondaryReadout = null,
        bool inlineNativeUnit = false, bool secondaryModified = false,
        Brush? dialAccent = null, double dialScale = 1.0)
    {
        // All larger hero dials (Headshot, Starvation HP and Radiation HP)
        // raise ONLY the caption by 9 DIP to restore the ordinary knob's
        // title-to-top-tick clearance. Keep the dial/gesture geometry intact.
        var titleY = card.Top + (prominent ? 10.0 : 19.0);
        DrawCenteredText(dc, label, new Point(card.Left + card.Width / 2, titleY),
            KnobTypography.CaptionSize,
            parameter?.IsControlEnabled == true ? text : muted, FontWeights.Bold);
        if (parameter is null)
            return;

        var center = new Point(card.Left + card.Width / 2, card.Top + 97.0);
        var radius = Math.Min(WeaponKnobRadius * (prominent ? 1.18 : 1.0) * dialScale,
            (card.Width - 24.0) / 2.0);
        var range = Math.Max(Epsilon, parameter.Maximum - parameter.Minimum);
        var position = Math.Clamp((parameter.CurrentNumber - parameter.Minimum) / range, 0.0, 1.0);
        var reference = Math.Clamp((parameter.ReferenceNumber - parameter.Minimum) / range, 0.0, 1.0);
        var hovered = _hoverTarget?.Kind == HitKind.Knob
            && ReferenceEquals(_hoverTarget.Parameter, parameter);
        var active = parameter.IsControlEnabled;
        var dialInk = dialAccent ?? accent;
        var face = CloneWithOpacity(panel, .96);

        // Industrial dial face and a 270-degree graduated rail, leaving the
        // bottom quadrant open so the limits never fight the center readout.
        // Keep the quiet face fill but no concentric interior rings:
        // the outer 270-degree source arc, pointer and native ticks carry the shape.
        dc.DrawEllipse(face, null, center, radius - 10, radius - 10);
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
                new Pen(CloneWithOpacity(active ? dialInk : muted, active ? .94 : .35),
                    prominent ? 7.0 : 6.0));
        var baselineAngle = WeaponKnobStartAngle + WeaponKnobSweepAngle * reference;
        dc.DrawLine(new Pen(muted, 2.4),
            KnobPoint(center, radius - 7, baselineAngle),
            KnobPoint(center, radius + 6, baselineAngle));

        var angleNow = WeaponKnobStartAngle + position * WeaponKnobSweepAngle;
        if (hovered)
            dc.DrawEllipse(CloneWithOpacity(dialInk, .13), null,
                center, radius - 11, radius - 11);
        dc.DrawLine(new Pen(hovered ? Brushes.White : active ? dialInk : muted,
                hovered ? 3.0 : 2.4),
            KnobPoint(center, radius - 8, angleNow),
            KnobPoint(center, radius + 3, angleNow));
        // A tiny mechanical pointer rivet follows the current source position.
        // Base stays represented solely by its original muted tick.
        dc.DrawEllipse(active ? CloneWithOpacity(dialInk, .82) : CloneWithOpacity(muted, .35),
            null, KnobPoint(center, radius + 2.0, angleNow), 1.7, 1.7);

        var modified = active && parameter.IsValueModifiedFromReference();
        string Percent(double value) =>
            $"{(percentageSign > 0 ? "+" : percentageSign < 0 ? "−" : "")}{value * 100.0:0.#}%";
        // Source-native rates and medicine amounts display the actual stat
        // value plus its physical/gameplay unit, never a fictitious 1x scalar.
        // A two-line center keeps both readable inside the compact dial.
        var currentLabel = rateReadout ? $"{parameter.CurrentNumber:0.00}"
            : percentageReadout ? Percent(parameter.CurrentNumber)
            : rawReadout
                ? $"{(positivePrefix && nativeUnit is not null && parameter.CurrentNumber > Epsilon ? "+" : "")}{parameter.CurrentNumber:0.#}"
                : $"{parameter.CurrentNumber:0.##}x";
        // WPF draws at the text's top edge: center every source-backed dial
        // readout, including single-line multiplier/percentage and signed
        // percentage values. Two-line amount/unit or rate/unit pairs share
        // the same centered positions without per-family call-site offsets.
        var currentTop = center.Y - (rateReadout || nativeUnit is not null || secondaryReadout is not null ? 15.0 : 8.0);
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
        else if (inlineNativeUnit && nativeUnit is not null)
        {
            // Hercules: one centered value line; only the editable number turns gold.
            var combined = CreateText($"{currentLabel} {nativeUnit}",
                prominent ? (modified ? 18.2 : 17.0) : (modified ? 16.2 : 15.0),
                active ? text : muted, FontWeights.Bold, card.Width - 12.0);
            if (modified)
                combined.SetForegroundBrush(accent, 0, currentLabel.Length);
            dc.DrawText(combined, new Point(
                center.X - combined.WidthIncludingTrailingWhitespace / 2.0, currentTop));
        }
        else
        {
            DrawCenteredText(dc, currentLabel, new Point(center.X, currentTop),
                prominent ? (modified ? 18.2 : 17.0) : (modified ? 16.2 : 15.0),
                active ? modified ? accent : text : muted, FontWeights.Bold);
        }
        if (secondaryReadout is not null)
            DrawKnobSecondaryReadout(dc, secondaryReadout,
                new Point(center.X, center.Y + 7.0),
                Math.Max(1.0, card.Width - 12.0),
                muted, accent, active && secondaryModified);
        if (rateReadout)
            DrawCenteredText(dc, "PTS/S",
                new Point(center.X, center.Y + 5.0),
                9.0, muted, FontWeights.SemiBold);
        else if (nativeUnit is not null && !inlineNativeUnit)
            DrawCenteredText(dc, nativeUnit,
                new Point(center.X, center.Y + 5.0),
                8.3, muted, FontWeights.SemiBold);
        DrawCenteredText(dc,
            rateReadout ? $"{parameter.Minimum:0.00}"
                : percentageReadout ? Percent(parameter.Minimum)
                : rawReadout ? $"{parameter.Minimum:0.#}"
                : $"{parameter.Minimum:0.##}x",
            new Point(center.X - radius * .80, card.Bottom - KnobTypography.EndpointInsetFromBottom),
            KnobTypography.EndpointSize, Brushes.White, FontWeights.SemiBold);
        DrawCenteredText(dc,
            rateReadout ? $"{parameter.Maximum:0.00}"
                : percentageReadout ? Percent(parameter.Maximum)
                : rawReadout ? $"{parameter.Maximum:0.#}"
                : $"{parameter.Maximum:0.##}x",
            new Point(center.X + radius * .80, card.Bottom - KnobTypography.EndpointInsetFromBottom),
            KnobTypography.EndpointSize, Brushes.White, FontWeights.SemiBold);

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

        // Tactical Control Console: bounded dual-axis source editor and a compact
        // three-knob attack bank. The 2D plot still edits the native X/Y CFGs.
        var wide = HumanCombatWide(ActualWidth);
        const double profileWidth = 300.0;
        var bankWidth = Math.Min(802.0, Math.Max(1.0, ActualWidth - 26.0));
        var bankLeft = (ActualWidth - bankWidth) / 2.0;
        var profileLeft = wide ? bankLeft : (ActualWidth - profileWidth) / 2.0;

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
            // Actual repair-price layers multiply: Base Repair times this
            // reputation factor; show the combined factor but edit only the
            // plotted reputation source. Difficulty layer remains independent;
            // this is not a final coupon bill.
            var baseRepair = Linked("economy.repairCostBase")?.CurrentNumber ?? 0.7;
            DrawDerivedCentered(dc, $"{baseRepair * parameter.CurrentNumber:0.##}x",
                new Point(x, plot.Bottom + 57.0),
                plot.Width / 4.0 - 10.0, 9.4, muted, accent, FontWeights.SemiBold);
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
    // The Water 30-second cost-cut duration and its separate magnitude knob
    // share the warm UI accent yellow (#F0C419), unlike the green 5s bonus.
    private static readonly Brush WaterCostCutLineBrush =
        new SolidColorBrush(Color.FromRgb(240, 196, 25));

    private static Brush? DrinkEffectLineBrush(string sourceId) => sourceId switch
    {
        "survival.waterRegenDuration" or "survival.waterStaminaEffectStrength"
            => WaterEffectLineBrush,
        "survival.waterEffectDuration" or "survival.waterStaminaCostReduction"
            => WaterCostCutLineBrush,
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

    // Three drink strength knobs sit above the combined eight-lane duration graph;
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
        var waterCostReduction = P("survival.waterStaminaCostReduction");
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
            waterCostReduction is null || energyStamina is null ||
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

        // Two direct-percent refills and the separate Energy regeneration knob;
        // Water's two distinct bonus/cost-cut effects use green/yellow
        // source-native knobs below, avoiding duplicate controls.
        const double knobGap = 10.0;
        var knobBankWidth = Math.Min(620.0, ActualWidth - 34.0);
        var knobWidth = (knobBankWidth - 2.0 * knobGap) / 3.0;
        var knobLeft = (ActualWidth - knobBankWidth) / 2.0;
        // Read-only refill points are centered with the editable native percentage.
        var maxStamina = Linked("stamina.maxSP");
        var staminaPool = Math.Max(1.0, maxStamina?.CurrentNumber ?? 100.0);
        var referenceStaminaPool = Math.Max(1.0, maxStamina?.ReferenceNumber ?? 100.0);
        DrawSourceKnob(dc, burst, "WATER STAMINA GAIN",
            new Rect(knobLeft, ConsumableStrengthKnobTop, knobWidth, 181.0),
            text, muted, border, accent, panel, percentageReadout: true,
            secondaryReadout: $"+{burst.CurrentNumber * staminaPool:0.#} stamina",
            secondaryModified: Math.Abs(burst.CurrentNumber * staminaPool
                - burst.ReferenceNumber * referenceStaminaPool) > Epsilon);
        DrawSourceKnob(dc, energyBurst, "ENERGY DRINK STAMINA GAIN",
            new Rect(knobLeft + knobWidth + knobGap, ConsumableStrengthKnobTop,
                knobWidth, 181.0),
            text, muted, border, accent, panel, percentageReadout: true,
            secondaryReadout: $"+{energyBurst.CurrentNumber * staminaPool:0.#} stamina",
            secondaryModified: Math.Abs(energyBurst.CurrentNumber * staminaPool
                - energyBurst.ReferenceNumber * referenceStaminaPool) > Epsilon);
        DrawSourceKnob(dc, energyStamina, "ENERGY DRINK REGEN",
            new Rect(knobLeft + 2.0 * (knobWidth + knobGap),
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
            Math.Max(1.0, checkboxRect.Left - overrideRect.Left - 19.0)),
            new Point(overrideRect.Left + 12.0, overrideRect.Top + 7.0));
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
            // Match Water's duration lane to the independent cost-cut knob.
            var laneBrush = i == 0 ? WaterCostCutLineBrush : accent;
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

            // The EFFECT DURATION seconds heading already names the unit.
            // Keep WATER one-line like the other drink rows.
            DrawRightAlignedText(dc, names[i],
                new Point(timeline.Left - 13.0, y - 8.0),
                10.4, text, FontWeights.Bold);
            // Any grey tail after this source's own max is not editable.
            dc.DrawLine(new Pen(CloneWithOpacity(border, 0.4), 2.0),
                new Point(timeline.Left, y), new Point(timeline.Right, y));
            dc.DrawLine(new Pen(border, 3.0),
                new Point(timeline.Left, y), new Point(maximumX, y));
            dc.DrawLine(new Pen(CloneWithOpacity(laneBrush, 0.75), 4.0),
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
            // No duplicate yellow parent underline: WATER already has its
            // editable yellow rail. Keep the short green Water child marker
            // and both blue Energy child-effect markers in the loop above.
            dc.DrawLine(new Pen(muted, 1.7),
                new Point(referenceX, y - 8.0), new Point(referenceX, y + 8.0));
            dc.DrawEllipse(panel,
                new Pen(laneBrush, IsSelected(parameter) ? 2.5 : 1.6),
                new Point(currentX, y), 6.0, 6.0);
            dc.DrawEllipse(laneBrush, null, new Point(currentX, y), 2.5, 2.5);

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

        // Five native sources in two drink groups; group frames replace
        // individual mini-card borders. Equal cell sizes when side by side.
        var effectsWidth = Math.Min(850.0, preview.Width - 24.0);
        var sideBySide = effectsWidth >= ConsumableEffectsInlineMinimumWidth;
        var left = preview.Left + (preview.Width - effectsWidth) / 2.0;
        var top = preview.Top + ConsumableEffectsGroupTop;
        var uniformWidth = (effectsWidth - ConsumableEffectsGroupGap -
            4.0 * ConsumableEffectsGroupPadding - 3.0 * ConsumableEffectsCellGap) / 5.0;
        var waterWidth = sideBySide
            ? uniformWidth * 3.0 + ConsumableEffectsCellGap * 2.0 +
                ConsumableEffectsGroupPadding * 2.0
            : effectsWidth;
        var energyWidth = sideBySide
            ? uniformWidth * 2.0 + ConsumableEffectsCellGap +
                ConsumableEffectsGroupPadding * 2.0
            : Math.Min(480.0, effectsWidth);
        var waterGroup = new Rect(left, top, waterWidth, ConsumableEffectsGroupHeight);
        var energyGroup = new Rect(
            sideBySide ? waterGroup.Right + ConsumableEffectsGroupGap :
                left + (effectsWidth - energyWidth) / 2.0,
            sideBySide ? top : waterGroup.Bottom + ConsumableEffectsGroupGap,
            energyWidth, ConsumableEffectsGroupHeight);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .76),
            new Pen(border, .9), waterGroup, 4.0, 4.0);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .76),
            new Pen(border, .9), energyGroup, 4.0, 4.0);
        DrawText(dc, "WATER", new Point(waterGroup.Left + 10.0, waterGroup.Top + 7.0),
            10.2, text, FontWeights.Bold);
        DrawText(dc, "ENERGY DRINK", new Point(energyGroup.Left + 10.0, energyGroup.Top + 7.0),
            10.2, text, FontWeights.Bold);

        // The Water cap remains a useful live projection but need not repeat
        // the +25% already shown by the separate yellow stamina bonus knob.
        var waterBonusPercent = 25.0 * waterStamina.CurrentNumber;
        var waterCap = staminaPool * (1.0 + waterBonusPercent / 100.0);
        var referenceWaterCap = referenceStaminaPool *
            (1.0 + 25.0 * waterStamina.ReferenceNumber / 100.0);
        var waterCapCaption = $"{waterCap:0.#} stamina cap";
        // The derived header remains compact: larger neutral unit and
        // larger bold number; modified digits alone use the gold accent.
        var capLabel = CreateText(waterCapCaption, 11.2, muted,
            FontWeights.SemiBold, Math.Max(1.0, waterGroup.Width - 88.0));
        var capNumber = System.Text.RegularExpressions.Regex.Match(
            waterCapCaption, @"^\d+(?:\.\d+)?");
        if (capNumber.Success)
        {
            capLabel.SetFontSize(13.0, capNumber.Index, capNumber.Length);
            capLabel.SetFontWeight(FontWeights.Bold, capNumber.Index, capNumber.Length);
            if (Math.Abs(waterCap - referenceWaterCap) > Epsilon)
                capLabel.SetForegroundBrush(accent, capNumber.Index, capNumber.Length);
        }
        dc.DrawText(capLabel, new Point(
            waterGroup.Right - 10.0 - capLabel.WidthIncludingTrailingWhitespace,
            waterGroup.Top + 4.0));

        var waterCellWidth = (waterGroup.Width - 2.0 * ConsumableEffectsGroupPadding -
            2.0 * ConsumableEffectsCellGap) / 3.0;
        var energyCellWidth = (energyGroup.Width - 2.0 * ConsumableEffectsGroupPadding -
            ConsumableEffectsCellGap) / 2.0;
        Rect WaterCell(int index) => new(
            waterGroup.Left + ConsumableEffectsGroupPadding +
                index * (waterCellWidth + ConsumableEffectsCellGap),
            waterGroup.Top + ConsumableEffectsHeaderHeight,
            waterCellWidth, ConsumableMiniKnobHeight);
        Rect EnergyCell(int index) => new(
            energyGroup.Left + ConsumableEffectsGroupPadding +
                index * (energyCellWidth + ConsumableEffectsCellGap),
            energyGroup.Top + ConsumableEffectsHeaderHeight,
            energyCellWidth, ConsumableMiniKnobHeight);

        DrawMiniConsumableKnob(dc, waterRegenTime, "WATER BONUS DURATION",
            $"{waterRegenTime.CurrentNumber:0.#}s",
            $"{waterRegenTime.Minimum:0.#}s", $"{waterRegenTime.Maximum:0.#}s",
            WaterCell(0), text, muted, border, accent, panel, WaterEffectLineBrush);
        // The native source scales -10% SPDrain. Show that signed player
        // effect directly instead of a positive percentage + LESS suffix.
        var waterCostPercent = 10.0 * waterCostReduction.CurrentNumber;
        var waterCostText = waterCostPercent <= Epsilon
            ? "0%" : $"-{waterCostPercent:0.#}%";
        DrawMiniConsumableKnob(dc, waterCostReduction, "STAMINA COST",
            waterCostText, "0%", "-20%", WaterCell(1),
            text, muted, border, accent, panel, WaterCostCutLineBrush);
        DrawMiniConsumableKnob(dc, waterStamina, "WATER STAMINA BONUS",
            $"+{25.0 * waterStamina.CurrentNumber:0.#}%",
            "0%", "50%", WaterCell(2),
            text, muted, border, accent, panel, WaterCostCutLineBrush);
        DrawMiniConsumableKnob(dc, energyFullCostTime, "NO COST DURATION",
            $"{energyFullCostTime.CurrentNumber:0.#}s",
            $"{energyFullCostTime.Minimum:0.#}s", $"{energyFullCostTime.Maximum:0.#}s",
            EnergyCell(0), text, muted, border, accent, panel, EnergyDrinkEffectLineBrush);
        DrawMiniConsumableKnob(dc, energyPartialCostTime, "-30% COST DURATION",
            $"{energyPartialCostTime.CurrentNumber:0.#}s",
            $"{energyPartialCostTime.Minimum:0.#}s", $"{energyPartialCostTime.Maximum:0.#}s",
            EnergyCell(1), text, muted, border, accent, panel,
            EnergyDrinkPartialCostLineBrush);

        // Position below the whole duration chart, including short effects.
        // MeasureOverride and OnRender share the same responsive geometry.
        var medicineKnobTop = ConsumableMedicineKnobTop(ActualWidth);
        // The three medicine knobs form their own centered row. Reusing the
        // first three cells of the four-drink bank left them visibly off-center.
        // Same compact hero-dial geometry as the Radiation center knob:
        // redistribute 18 DIP from the side cards to Hercules, not the row width.
        const double medicineHeroExtra = 18.0;
        var medicineBankWidth = 3.0 * Math.Min(184.0, knobWidth) + 2.0 * knobGap;
        var medicineSideWidth = (medicineBankWidth - 2.0 * knobGap - medicineHeroExtra) / 3.0;
        var medicineHeroWidth = medicineSideWidth + medicineHeroExtra;
        var medicineLeft = (ActualWidth - medicineBankWidth) / 2.0;
        // Native benefits, not relative sliders: +bleed-recovery points,
        // +kilograms and +psi-protection points. Keep the amount and its
        // unit centered, with a changed-only gold number and neutral unit.
        DrawSourceKnob(dc, vincaStrength, "VINCA BLEED REGEN",
            new Rect(medicineLeft, medicineKnobTop, medicineSideWidth, 181.0),
            text, muted, border, accent, panel,
            rawReadout: true, nativeUnit: "BLEED PTS");
        var carryWeight = Linked("stamina.maxCarryWeight");
        var baseCarry = carryWeight?.CurrentNumber ?? 80.0;
        var referenceCarry = carryWeight?.ReferenceNumber ?? 80.0;
        DrawSourceKnob(dc, herculesStrength, "HERCULES CARRY",
            new Rect(medicineLeft + medicineSideWidth + knobGap,
                medicineKnobTop, medicineHeroWidth, 188.0),
            text, muted, border, accent, panel,
            rawReadout: true, nativeUnit: "KG", inlineNativeUnit: true,
            secondaryReadout: $"{baseCarry + herculesStrength.CurrentNumber:0.#} WITH BUFF",
            secondaryModified: Math.Abs(baseCarry + herculesStrength.CurrentNumber
                - referenceCarry - herculesStrength.ReferenceNumber) > Epsilon,
            prominent: true);
        DrawSourceKnob(dc, psiStrength, "PSI BLOCK PROTECTION",
            new Rect(medicineLeft + 2.0 * (medicineSideWidth + knobGap) + medicineHeroExtra,
                medicineKnobTop, medicineSideWidth, 181.0),
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

        // Engraved neutral split between sleep eligibility and hour selection.
        IndustrialChrome.DrawEngravedSeparator(dc, left + 8.0, left + width - 8.0,
            141.0, muted);
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

        IndustrialChrome.DrawEngravedSeparator(dc, left + 8.0, left + width - 8.0,
            237.0, muted);
        DrawText(dc, "RECOVERY WHEN YOU WAKE UP",
            new Point(left + 1.0, 247.0), 10.7, accent, FontWeights.Bold);
        const double gap = 12.0;
        var column = (width - gap) / 2.0;
        var maxHealth = Math.Max(1.0, Linked("health.maxHP")?.CurrentNumber ?? 100.0);
        DrawCompactTrack(dc, healthRestore, "HEALTH RESTORED",
            $"+{healthRestore.CurrentNumber:0.#} HP · {100.0 * healthRestore.CurrentNumber / maxHealth:0.#}%",
            $"{healthRestore.Minimum:0} HP", $"{healthRestore.Maximum:0} HP",
            new Rect(left, 274.0, column, 82.0),
            text, muted, border, accent, panel, highlightDerivedLastNumber: true);
        DrawCompactTrack(dc, staminaRestore, "STAMINA RESTORE STRENGTH",
            $"+{staminaRestore.CurrentNumber * 100:0.#}%",
            $"{staminaRestore.Minimum * 100:0.#}%",
            $"{staminaRestore.Maximum * 100:0.#}%",
            new Rect(left + column + gap, 274.0, column, 82.0),
            text, muted, border, accent, panel);
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
        IndustrialRulerChrome.DrawActiveRailGlint(dc, center, now, track.Top + 6.0,
            accent, parameter.IsControlEnabled);
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
        IndustrialChrome.DrawEngravedSeparator(dc, left + 8.0, left + width - 8.0,
            149.0, muted);
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

        // Source-truthful stage / threshold / onset in one compact summary;
        // at narrow widths wrap AFTER so numbers never overlap stage cells.
        var footerTop = frame.Top + 269.0;
        var stageWidth = plot.Width / 3.0;
        void StageCaption(int index, string stageName,
            ParameterViewModel threshold, ParameterViewModel? recoveryLoss)
        {
            var cell = new Rect(plot.Left + index * stageWidth,
                footerTop, stageWidth, 69.0);
            dc.PushClip(new RectangleGeometry(cell));
            var cx = cell.Left + cell.Width / 2.0;
            var elapsed = StageOnsetEstimate(threshold.CurrentNumber, rate.CurrentNumber);
            var thresholdText = $"{stageName} AT {threshold.CurrentNumber:0} PTS";
            var timeText = $"AFTER {elapsed}";
            var headline = $"{thresholdText} {timeText}";
            // Match Fatigue's stage type size. Wrap once the measured headline
            // cannot fit with room for modified-number gold emphasis.
            var narrow = stageWidth < 205.0 || CreateText(headline,
                VisualControlTypography.SurvivalStageSummary,
                text, FontWeights.SemiBold).WidthIncludingTrailingWhitespace > stageWidth - 22.0;
            if (narrow)
            {
                DrawCenteredNumericChanges(dc, thresholdText,
                    new Point(cx, footerTop + 7.0),
                    VisualControlTypography.SurvivalStageSummary,
                    text, accent, FontWeights.SemiBold,
                    threshold.IsValueModifiedFromReference());
                var timeNumbers = System.Text.RegularExpressions.Regex.Matches(
                    timeText, @"\d+").Count;
                DrawCenteredNumericChanges(dc, timeText,
                    new Point(cx, footerTop + 25.0),
                    VisualControlTypography.SurvivalStageSummary,
                    text, accent, FontWeights.SemiBold,
                    Enumerable.Repeat(true, timeNumbers).ToArray());
            }
            else
            {
                var numberCount = System.Text.RegularExpressions.Regex.Matches(
                    headline, @"\d+").Count;
                var highlight = new bool[numberCount];
                if (numberCount > 0)
                    highlight[0] = threshold.IsValueModifiedFromReference();
                for (var j = 1; j < numberCount; j++)
                    highlight[j] = true;
                DrawCenteredNumericChanges(dc, headline,
                    new Point(cx, footerTop + 11.0),
                    VisualControlTypography.SurvivalStageSummary,
                    text, accent, FontWeights.SemiBold, highlight);
            }
            if (recoveryLoss is not null)
            {
                var recoveryText =
                    $"-{recoveryLoss.CurrentNumber * 100:0}% STAMINA RECOVERY";
                var splitRecovery = CreateText(recoveryText,
                    VisualControlTypography.SurvivalStageSummary,
                    text, FontWeights.SemiBold).WidthIncludingTrailingWhitespace > stageWidth - 18.0;
                if (splitRecovery)
                {
                    DrawCenteredNumericChanges(dc,
                        $"-{recoveryLoss.CurrentNumber * 100:0}% STAMINA",
                        new Point(cx, footerTop + (narrow ? 44.0 : 32.0)),
                        VisualControlTypography.SurvivalStageSummary,
                        text, accent, FontWeights.SemiBold,
                        recoveryLoss.IsValueModifiedFromReference());
                    DrawCenteredNumericChanges(dc, "RECOVERY",
                        new Point(cx, footerTop + (narrow ? 60.0 : 49.0)),
                        VisualControlTypography.SurvivalStageSummary,
                        text, accent, FontWeights.SemiBold);
                }
                else
                {
                    DrawCenteredNumericChanges(dc, recoveryText,
                        new Point(cx, footerTop + (narrow ? 49.0 : 37.0)),
                        VisualControlTypography.SurvivalStageSummary,
                        text, accent, FontWeights.SemiBold,
                        recoveryLoss.IsValueModifiedFromReference());
                }
            }
            dc.Pop();
        }
        StageCaption(0, "LOW", low, null);
        StageCaption(1, "MEDIUM", medium, mediumPenalty);
        StageCaption(2, "SEVERE", severe, severePenalty);
    }

    // Elapsed REAL seconds from a zero-point hunger pool, assuming the
    // configured points/second is constant and no food/other effects apply.
    // Whole-second truncation preserves the source-rate arithmetic without
    // interpreting game-day duration or creating a fictitious live clock.
    private static string StageOnsetEstimate(double threshold, double pointsPerSecond)
    {
        if (threshold <= Epsilon)
            return "0s";
        if (pointsPerSecond <= Epsilon)
            return "NEVER";

        var totalSeconds = (int)Math.Floor(Math.Max(0.0, threshold) / pointsPerSecond);
        var hours = totalSeconds / 3600;
        var minutes = totalSeconds % 3600 / 60;
        var seconds = totalSeconds % 60;
        return hours > 0
            ? $"~{hours}h{minutes}m{seconds}s"
            : $"~{minutes}m{seconds}s";
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

        // Three real source-native knobs in one bounded row immediately
        // beneath the mechanics toggle; all share the existing 270° input.
        // Source values remain seconds / HP, not fictitious multipliers.
        var knobs = new (string Id, string Label, string Unit)[]
        {
            ("survival.starvationPenaltyDelay", "PENALTIES START AFTER", "SECONDS"),
            ("survival.starvationDamage", "HP DAMAGE PER TICK", "HP"),
            ("survival.starvationDamageInterval", "DAMAGE EVERY", "SECONDS")
        };
        const double knobGap = 12.0;
        var knobBankWidth = Math.Min(606.0, Math.Max(1.0, ActualWidth - 34.0));
        var knobWidth = (knobBankWidth - 2.0 * knobGap) / 3.0;
        var knobLeft = (ActualWidth - knobBankWidth) / 2.0;
        for (var i = 0; i < knobs.Length; i++)
        {
            DrawSourceKnob(dc, P(knobs[i].Id), knobs[i].Label,
                new Rect(knobLeft + i * (knobWidth + knobGap),
                    StarvationKnobsTop, knobWidth, StarvationKnobHeight),
                text, muted, border, accent, panel,
                rawReadout: true, nativeUnit: knobs[i].Unit, positivePrefix: false,
                prominent: knobs[i].Id == "survival.starvationDamage");
        }

        var previewTop = StarvationPreviewTop;
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

            // One centered read-only sentence in the original 41-DIP slot.
            // It is a hypothetical raw full-HP estimate, not a live death timer.
            var hud = new Rect(preview.Left + 12.0, preview.Top + 127.0,
                Math.Max(1.0, preview.Width - 24.0), 41.0);
            const string prefix = "STARVATION KILLS IN ";
            const string connector = " TICKS AFTER ";
            var tickCountText = hasCountdown
                ? lethalTicks.ToString(CultureInfo.InvariantCulture) : "—";
            var timeText = hasCountdown ? $"~{StarvationElapsed(elapsed)}"
                : !hasCapacity ? "HP N/A"
                : perTick <= Epsilon ? "NO HP DAMAGE" : "NO TIMER";
            var summary = prefix + tickCountText + connector + timeText;
            var summaryText = CreateText(summary, 10.8, active ? text : muted,
                FontWeights.Bold, Math.Max(1.0, hud.Width - 16.0));
            if (active && hasCountdown)
            {
                summaryText.SetForegroundBrush(accent, prefix.Length, tickCountText.Length);
                summaryText.SetFontSize(14.5, prefix.Length, tickCountText.Length);
                var timeOffset = prefix.Length + tickCountText.Length + connector.Length;
                summaryText.SetForegroundBrush(accent, timeOffset, timeText.Length);
                summaryText.SetFontSize(19.0, timeOffset, timeText.Length);
            }
            dc.DrawText(summaryText,
                new Point(hud.Left + (hud.Width - summaryText.WidthIncludingTrailingWhitespace) / 2.0,
                    hud.Top + (hud.Height - summaryText.Height) / 2.0));
        }

        // The remaining two independent native sliders occupy one compact
        // row even below MasterCardBank's unrelated 672-DIP breakpoint.
        var effects = new (string Id, string Label, Func<double, string> Format)[]
        {
            ("survival.starvationStaminaDrain", "EXTRA STAMINA COST", MasterPositivePercent),
            ("survival.starvationHealthRegenPenalty", "HEALTH RECOVERY BLOCKED", MasterPercent)
        };
        const double effectGap = 12.0;
        var effectWidth = Math.Min(370.0,
            Math.Max(1.0, (ActualWidth - 32.0 - effectGap) / 2.0));
        var effectsLeft = (ActualWidth - 2.0 * effectWidth - effectGap) / 2.0;
        for (var i = 0; i < effects.Length; i++)
            DrawMasterNativeCard(dc, P(effects[i].Id),
                effects[i].Label, effects[i].Format,
                new Rect(effectsLeft + i * (effectWidth + effectGap),
                    StarvationEffectsTop, effectWidth, 80.0),
                text, muted, border, soft, accent, panel);
    }

    // Original GameData/EffectPrototypes.cfg ValueMin/ValueMax pairs,
    // verified 1:1 against the shipping CFG writer baseline. These are
    // damage-effect source values, NOT damage-per-second or exposure thresholds.
    // The 11th DEADLY prototype has its own value (20), lower than L10
    // (38.67); show it separately rather than claiming monotonic progression.
    private static readonly (string Label, string Prototype, double SourceDamage)[]
        RadiationDamageStages =
    {
        ("LIGHT", "LightRadiationDamage", 0.2),
        ("MED", "MediumRadiationDamage", 0.3),
        ("STRONG", "StrongRadiationDamage", 0.48),
        ("4", "RadiationLevel4Damage", 0.816),
        ("5", "RadiationLevel5Damage", 1.468),
        ("6", "RadiationLevel6Damage", 2.2),
        ("7", "RadiationLevel7Damage", 4.18),
        ("8", "RadiationLevel8Damage", 8.37),
        ("9", "RadiationLevel9Damage", 17.58),
        ("10", "RadiationLevel10Damage", 38.67),
        ("DEADLY", "DeadlyRadiationDamage", 20),
    };

    private const double RadiationProfileTop = 241.0;
    private const double RadiationProfileHeight = 388.0;
    // Lift the enlarged 142-DIP bezel to leave room for the RAD readout.
    private const double RadiationDialCenterY = 183.0;
    private const double RadiationDialTrackRadius = 122.0;
    private const double RadiationControlsNarrowHeight = 272.0;
    private const double RadiationGap = 12.0;
    private static bool RadiationWide(double width) => width >= 800.0;
    private static double RadiationBoardHeight(double width)
        => RadiationProfileTop + RadiationProfileHeight
            + (RadiationWide(width) ? 0.0
                : RadiationGap + RadiationControlsNarrowHeight) + RadiationGap;

    // Max HP is read only from Health: never an editable Radiation member.
    private double RadiationModelMaxHp => Math.Max(1.0, _radiationMaxHp?.CurrentNumber ?? 100.0);

    // Original threshold-to-prototype mapping. RAD=0 has no HP damage;
    // LIGHT starts at 1, and the special DEADLY prototype starts at 99.
    private static int RadiationStageIndex(double rad)
    {
        if (rad < 1.0) return -1;
        var index = 0;
        foreach (var threshold in RadiationRingThresholds)
            if (rad >= threshold) index++;
        return Math.Min(index, RadiationDamageStages.Length - 1);
    }

    private double RadiationDamageAtLevel(double level)
    {
        var index = RadiationStageIndex(level);
        var damageScale = Math.Max(0.0, P("survival.radiationDamage")?.CurrentNumber ?? 0.0);
        return index < 0 ? 0.0 : RadiationDamageStages[index].SourceDamage * damageScale;
    }

    // A fixed-level full-HP estimate only. Permanent-effect runtime cadence is
    // unverified; one damage charge/second and no protection/healing are assumed.
    private double RadiationTimeToZeroHp()
    {
        var damagePerAssumedCharge = RadiationDamageAtLevel(_radLevel);
        return damagePerAssumedCharge <= Epsilon
            ? double.PositiveInfinity
            : Math.Ceiling(RadiationModelMaxHp / damagePerAssumedCharge);
    }

    private static string RadiationTime(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0.0) return "N/A";
        if (seconds < 60.0)
            return seconds.ToString("0.#", CultureInfo.InvariantCulture) + "s";
        var min = (long)Math.Floor(seconds / 60.0);
        if (min < 60)
            return min.ToString(CultureInfo.InvariantCulture) + "m "
                + (seconds - min * 60.0).ToString("0", CultureInfo.InvariantCulture) + "s";
        return (min / 60).ToString(CultureInfo.InvariantCulture) + "h "
            + (min % 60).ToString("00", CultureInfo.InvariantCulture) + "m";
    }

    private void AdjustRadiationLevel(int step)
    {
        _radLevel = Math.Clamp(_radLevel + step, 0.0, 100.0);
        _radRingFocus = true;
        InvalidateVisual();
    }

    private void ScrubRadiationRing(Point point)
    {
        var ring = RadiationRingRect();
        var center = new Point(ring.Left + ring.Width / 2.0, ring.Top + RadiationDialCenterY);
        var angle = Math.Atan2(point.Y - center.Y, point.X - center.X) + Math.PI / 2.0;
        if (angle < 0.0) angle += Math.PI * 2.0;
        // Twelve o'clock means 0 on initial click, and 100 after a full
        // clockwise turn while the ring drag is captured.
        var level = Math.Clamp(Math.Round(angle * 100.0 / (Math.PI * 2.0)), 0.0, 100.0);
        if (_radRingHadScrub)
        {
            var previous = _radLevel;
            if (previous >= 95.0 && level <= 5.0)
                _radRingCompleted = true;
            else if (!_radRingCompleted && previous <= 5.0 && level >= 95.0)
                level = 0.0;
        }
        if (_radRingCompleted && level < 95.0)
            level = 100.0;
        else if (_radRingCompleted && level < 100.0)
            _radRingCompleted = false;
        _radLevel = level;
        _radRingHadScrub = true;
        InvalidateVisual();
        _radRingFocus = true;
    }

    private bool RadiationRingHit(Point point)
    {
        if (Family != "RadiationBalance") return false;
        var ring = RadiationRingRect();
        var dx = point.X - ring.Left - ring.Width / 2.0;
        var dy = point.Y - ring.Top - RadiationDialCenterY;
        var distance = Math.Sqrt(dx * dx + dy * dy);
        // This is the visible 13-DIP main track, not a disconnected
        // second outer ring. Give the player a modest drag tolerance.
        return distance >= 108.0 && distance <= 136.0;
    }

    private Rect RadiationRingRect()
    {
        var width = Math.Max(1.0, ActualWidth - 32.0);
        return new Rect(16.0, RadiationProfileTop,
            RadiationWide(ActualWidth) ? (width - RadiationGap) / 2.0 : width,
            RadiationProfileHeight);
    }

    private Rect RadiationControlsRect()
    {
        var ring = RadiationRingRect();
        return RadiationWide(ActualWidth)
            ? new Rect(ring.Right + RadiationGap, ring.Top, ring.Width, ring.Height)
            : new Rect(16.0, ring.Bottom + RadiationGap,
                Math.Max(1.0, ActualWidth - 32.0), RadiationControlsNarrowHeight);
    }

    // Authentic RadiationMechanics stage thresholds (read-only CFG facts).
    // 0..100 RAD maps to exactly one complete clockwise revolution.
    private static readonly double[] RadiationRingThresholds =
    {
        10.0, 20.0, 30.0, 40.0, 50.0,
        60.0, 70.0, 80.0, 90.0, 99.0
    };

    private static Point RadiationRingPoint(Point center, double radius, double rad)
    {
        var angle = rad / 100.0 * Math.PI * 2.0 - Math.PI / 2.0;
        return new Point(center.X + radius * Math.Cos(angle),
            center.Y + radius * Math.Sin(angle));
    }

    private static void DrawRadiationRingSector(
        DrawingContext dc, Point center, double radius,
        double startRad, double endRad, Brush ink)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(RadiationRingPoint(center, radius, startRad),
                isFilled: false, isClosed: false);
            ctx.ArcTo(RadiationRingPoint(center, radius, endRad),
                new Size(radius, radius), 0.0,
                isLargeArc: endRad - startRad > 50.0,
                SweepDirection.Clockwise, isStroked: true,
                isSmoothJoin: false);
        }
        geometry.Freeze();
        dc.DrawGeometry(null, new Pen(ink, 13.0), geometry);
    }

    // The game's lower-left HUD radiation dial inspires this in-tool inspector.
    // Thirty-two short teeth follow one exact 0..100 circular scale, while
    // the underlying pointer mapping and CFG stage thresholds stay continuous.
    private const int RadiationHudSegments = 32;
    private const int RadiationHudTickStep = 10; // even HUD ticks, independent of CFG stages
    private const double RadiationHudSegmentGap = 0.36; // RAD units per tooth gap

    private static void DrawRadiationHudSegments(
        DrawingContext dc, Point center, double rad, Brush inactive, Brush active)
    {
        const double radius = RadiationDialTrackRadius;
        var pitch = 100.0 / RadiationHudSegments;
        for (var i = 0; i < RadiationHudSegments; i++)
        {
            var first = i * pitch + RadiationHudSegmentGap / 2.0;
            var last = (i + 1) * pitch - RadiationHudSegmentGap / 2.0;
            DrawRadiationRingSector(dc, center, radius, first, last, inactive);
            var litEnd = Math.Min(last, rad);
            // Partial last tooth preserves each integer RAD; never quantize
            // the actual selection to the number of decorative HUD teeth.
            if (litEnd > first + Epsilon)
                DrawRadiationRingSector(dc, center, radius, first, litEnd, active);
        }
    }

    private static Point RadiationHudPolar(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180.0;
        return new Point(center.X + radius * Math.Cos(radians),
            center.Y + radius * Math.Sin(radians));
    }

    // Three solid vanes and a slightly larger central hub.
    // Leave a narrow, visible radial gap between the hub and each vane.
    // No perforations, holes, pin dots, numbers or imported game asset.
    private static void DrawRadiationHudTrefoil(
        DrawingContext dc, Point center, Brush symbol)
    {
        const double inner = 27.0;
        const double outer = 96.0;
        for (var blade = 0; blade < 3; blade++)
        {
            // Upper-left, upper-right and bottom; each vane stops before the hub.
            var angle = -150.0 + blade * 120.0;
            var from = angle - 33.0;
            var to = angle + 33.0;
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(RadiationHudPolar(center, inner, from),
                    isFilled: true, isClosed: true);
                ctx.LineTo(RadiationHudPolar(center, outer, from), true, false);
                ctx.ArcTo(RadiationHudPolar(center, outer, to),
                    new Size(outer, outer), 0.0, false,
                    SweepDirection.Clockwise, true, false);
                ctx.LineTo(RadiationHudPolar(center, inner, to), true, false);
                ctx.ArcTo(RadiationHudPolar(center, inner, from),
                    new Size(inner, inner), 0.0, false,
                    SweepDirection.Counterclockwise, true, false);
            }
            geometry.Freeze();
            dc.DrawGeometry(symbol,
                new Pen(CloneWithOpacity(Brushes.Black, 0.28), 0.75), geometry);
        }
        // Solid 23-DIP center; 27-DIP vane roots leave a 4-DIP gap.
        dc.DrawEllipse(symbol, null, center, 23.0, 23.0);
    }

    // Native ACCUMULATION RATE, RECOVERY, HP DAMAGE remain the only CFG-editable knobs.
    // The full-circle preview is independently scrubbable, never a game reading.
    private void DrawRadiationBalance(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush soft, Brush accent, Brush panel)
    {
        DrawHeader(dc, "RADIATION SYSTEM", string.Empty, text, muted, accent);
        // HP DAMAGE is the central hero knob. The two smaller source-native
        // knobs remain symmetrical; all three retain their own CFG bindings.
        var sources = new (string Id, string Label)[]
        {
            ("survival.radiationAccumulation", "ACCUMULATION RATE"),
            ("survival.radiationDamage", "HP DAMAGE"),
            ("survival.radiationRecovery", "RECOVERY")
        };
        const double gap = 12.0;
        const double centerExtra = 18.0;
        var bankWidth = Math.Min(606.0, ActualWidth - 34.0);
        var sideWidth = (bankWidth - gap * 2.0 - centerExtra) / 3.0;
        var left = (ActualWidth - bankWidth) / 2.0;
        for (var i = 0; i < sources.Length; i++)
        {
            var source = sources[i];
            var hero = i == 1;
            var knobWidth = sideWidth + (hero ? centerExtra : 0.0);
            var knobLeft = left + i * (sideWidth + gap)
                + (i == 2 ? centerExtra : 0.0);
            DrawSourceKnob(dc, P(source.Id), source.Label,
                new Rect(knobLeft, 49.0, knobWidth, hero ? 188.0 : 181.0),
                text, muted, border, accent, panel,
                rateReadout: source.Id == "survival.radiationRecovery",
                prominent: hero);
        }

        var profile = RadiationRingRect();
        // Borderless grouping fill: keep the current card geometry.
        dc.DrawRoundedRectangle(panel, null, profile, 4.0, 4.0);
        var damage = P("survival.radiationDamage");
        if (damage is null)
            return;

        var center = new Point(profile.Left + profile.Width / 2.0,
            profile.Top + RadiationDialCenterY);
        const double radius = RadiationDialTrackRadius;
        var currentRad = _radLevel;

        // Recessed matte bezel with restrained instrumentation. The dial
        // reads like the game's HUD symbol rather than a technical chart.
        // The 122-DIP outer tooth track is the sole RAD input (hit 108..136).
        dc.DrawEllipse(CloneWithOpacity(soft, 0.12),
            new Pen(CloneWithOpacity(border, 0.9), 1.3),
            center, 142.0, 142.0);
        dc.DrawEllipse(CloneWithOpacity(panel, 0.97),
            new Pen(CloneWithOpacity(muted, 0.46), 1.8),
            center, 136.0, 136.0);
        dc.DrawEllipse(null, new Pen(CloneWithOpacity(soft, 0.42), 1.0),
            center, 109.0, 109.0);
        dc.DrawEllipse(CloneWithOpacity(soft, 0.06),
            new Pen(CloneWithOpacity(border, 0.70), 1.0),
            center, 102.0, 102.0);

        // Exact level is 0..100: the 32 teeth are visual feedback, not quantization.
        // Draw the filled portion of the last tooth instead of snapping
        // radiation/thresholds to the segment count.
        DrawRadiationHudSegments(dc, center, currentRad,
            CloneWithOpacity(muted, 0.56), accent);
        // 0 RAD displays no lit segment and no highlighted cursor.
        if (currentRad > 0.0)
            dc.DrawEllipse(accent, new Pen(panel, 1.5),
                RadiationRingPoint(center, radius, currentRad), 4.1, 4.1);

        // Unnumbered HUD ticks every 10 RAD, evenly spaced from 10 to 100.
        // CFG thresholds remain original, including DEADLY at 99 RAD.
        for (var tick = 10; tick <= 100; tick += RadiationHudTickStep)
        {
            // Each decade marker lights once the preview reaches it.
            dc.DrawLine(new Pen(currentRad >= tick ? accent : muted,
                    tick == 100 ? 2.0 : 1.2),
                RadiationRingPoint(center, 137.0, tick),
                RadiationRingPoint(center, tick == 100 ? 147.0 : 144.0,
                    tick));
        }

        DrawRadiationHudTrefoil(dc, center, CloneWithOpacity(text, 0.82));
        DrawRadiationControls(dc, text, muted, soft, accent, panel);
    }

    // Preview-only RAD selection. No field accumulation model or playback.
    // The time estimate assumes constant RAD, full HP, no healing or mitigation,
    // and one radiation damage charge each second (unverified in engine).
    private void DrawRadiationControls(
        DrawingContext dc, Brush text, Brush muted,
        Brush soft, Brush accent, Brush panel)
    {
        var area = RadiationControlsRect();
        var wide = RadiationWide(ActualWidth);
        dc.DrawRoundedRectangle(panel, null, area, 4.0, 4.0);
        var index = RadiationStageIndex(_radLevel);
        // 0 RAD = level 0; 1–9 = level 1; ... 90–98 = level 10;
        // DEADLY at 99–100 RAD = level 11. Read-only preview stage.
        var level = index + 1;
        var damage = RadiationDamageAtLevel(_radLevel);
        var modified = P("survival.radiationDamage")?.IsValueModifiedFromReference() ?? false;
        var eta = RadiationTimeToZeroHp();
        // One numeric RAD readout, as the large yellow right-side headline.
        // Constrain each line to the panel: narrow layouts must not bleed
        // into the page or turn long death estimates into multi-line text.
        var contentWidth = Math.Max(1.0, area.Width - 32.0);
        void DrawStat(string value, double y, double fontSize, Brush ink, FontWeight weight)
        {
            dc.DrawText(CreateText(value, fontSize, ink, weight, contentWidth),
                new Point(area.Left + 16.0, area.Top + y));
        }

        DrawStat("RADS ACCUMULATED", wide ? 46.0 : 16.0,
            11.5, muted, FontWeights.SemiBold);
        DrawStat(_radLevel.ToString("0", CultureInfo.InvariantCulture) + " RAD",
            wide ? 76.0 : 43.0, 29.0, accent, FontWeights.Bold);
        // Keep LEVEL muted, with a larger yellow number aligned on its baseline.
        var levelPrefix = CreateText("LEVEL ", 13.5, muted,
            FontWeights.SemiBold, contentWidth);
        var levelNumber = CreateText(level.ToString(CultureInfo.InvariantCulture),
            22.0, accent, FontWeights.Bold, contentWidth);
        var levelPosition = new Point(area.Left + 16.0,
            area.Top + (wide ? 117.0 : 81.0));
        dc.DrawText(levelPrefix, levelPosition);
        dc.DrawText(levelNumber, new Point(levelPosition.X + levelPrefix.Width + 12.0,
            levelPosition.Y - 6.0));

        DrawStat("DAMAGE PER TICK", wide ? 165.0 : 115.0,
            11.5, muted, FontWeights.SemiBold);
        DrawStat(damage.ToString("0.###", CultureInfo.InvariantCulture),
            wide ? 192.0 : 140.0, 29.0,
            modified ? accent : text, FontWeights.Bold);

        // Only show estimated elapsed time under DEAD AFTER.
        // One charge/second is a preview assumption, not verified gameplay.
        // At zero effective damage, never claim a lethal countdown.
        var hasDeathEstimate = double.IsFinite(eta);
        DrawStat(hasDeathEstimate ? "DEAD AFTER" : "DEATH ESTIMATE",
            wide ? 244.0 : 180.0, 11.5, muted, FontWeights.SemiBold);
        if (hasDeathEstimate)
        {
            // One prominent time readout, not a duplicated tick count.
            DrawStat("~" + RadiationTime(eta),
                wide ? 273.0 : 199.0, 29.0, accent, FontWeights.Bold);
        }
        else
        {
            DrawStat("NO RAD DAMAGE", wide ? 286.0 : 215.0,
                25.0, text, FontWeights.Bold);
        }
    }

    // The six persisted stash channels scale original native Weight only.
    // Selection Chance is never normalized, combined, or invented here.
    private static readonly Brush[] StashMixPalette =
    {
        new SolidColorBrush(Color.FromRgb(0xF4, 0xC1, 0x3C)), // Ammo
        new SolidColorBrush(Color.FromRgb(0x53, 0xCC, 0x9F)), // Medical
        new SolidColorBrush(Color.FromRgb(0xF0, 0x93, 0x4D)), // Food
        new SolidColorBrush(Color.FromRgb(0x67, 0xB6, 0xE7)), // Drinks
        new SolidColorBrush(Color.FromRgb(0xE9, 0x68, 0x64)), // Grenades
        new SolidColorBrush(Color.FromRgb(0xB6, 0x96, 0xE9))  // Coupons
    };

    private static readonly Brush[] StashMedicalPalette =
    {
        new SolidColorBrush(Color.FromRgb(0xCD, 0xB9, 0x9A)), // Bandages
        new SolidColorBrush(Color.FromRgb(0xE9, 0x91, 0x4F)), // Medkits
        new SolidColorBrush(Color.FromRgb(0x62, 0xB8, 0xE4)), // Army Medkits
        new SolidColorBrush(Color.FromRgb(0xAA, 0x85, 0xD9))  // Anti-Rad
    };

    // BASIC is a visual home for the six GLOBAL category weights.
    // Exactly as in Breakable Boxes, native pools are shown separately:
    // percentages mean relative selection INSIDE one native Weight group,
    // never a synthetic chance to find an item in a physical stash.
    private void DrawStashContentMix(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawHeader(dc, "STASH CONTENT MIX", "", text, muted, accent);
        var width = Math.Min(778.0, Math.Max(1.0, ActualWidth - 30.0));
        var left = (ActualWidth - width) / 2.0;
        var wide = width >= 670.0;

        // Single global source-facing quantity control; the VM propagates
        // this real source multiplier to all three eligible stash tiers.
        DrawStashNativeRollControl(dc, "loot.cheapStashAmmoQuantity",
            "AMMO QUANTITY · ALL TIERS", 0,
            new Rect(left + 14.0, 53.0,
                Math.Min(360.0, Math.Max(1.0, width - 28.0)), 56.0),
            text, muted, border, accent, panel,
            percentage: false, multiplier: true);

        DrawText(dc, "WEIGHT MIX · ALL TIERS",
            new Point(left + 13.0, 125.0), 10.7, text, FontWeights.Bold);
        DrawStashWeightMixer(dc, new Rect(left, 141.0, width,
                wide ? 146.0 : 300.0), text, muted, border, accent, panel);

        DrawStashBasicNativePools(dc,
            new Rect(left, wide ? 295.0 : 449.0, width, 74.0),
            text, muted, border, panel);

        // Basic RGD-5 Chance is a native absolute probability, while
        // Amount is an integer stack override. Neither is ammo quantity.
        const double pairGap = 10.0;
        var pairWidth = Math.Max(1.0, (width - 28.0 - pairGap) / 2.0);
        var pairTop = wide ? 377.0 : 531.0;
        DrawStashNativeRollControl(dc, "loot.cheapStashGrenadeChance",
            "RGD-5 CHANCE", 4,
            new Rect(left + 14.0, pairTop, pairWidth, 56.0),
            text, muted, border, accent, panel);
        DrawStashNativeRollControl(dc, "loot.cheapStashGrenadeQuantity",
            "RGD-5 AMOUNT", 4,
            new Rect(left + 14.0 + pairWidth + pairGap, pairTop, pairWidth, 56.0),
            text, muted, border, accent, panel, percentage: false);

        DrawStashChanceTier(dc, "STANDARD", "Medium", 10,
            new Rect(left, wide ? 441.0 : 595.0, width,
                wide ? 140.0 : 236.0), text, muted, border, accent, panel);
        DrawStashChanceTier(dc, "RARE", "Expensive", 19,
            new Rect(left, wide ? 589.0 : 839.0, width,
                wide ? 236.0 : 332.0), text, muted, border, accent, panel);
    }


    // Mode selects which independent, persisted native field is edited.
    private void DrawStashMedicalComposition(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawHeader(dc, "MEDICAL COMPOSITION", "", text, muted, accent);
        _stashMedicalModes.Clear();
        var width = Math.Min(778.0, Math.Max(1.0, ActualWidth - 30.0));
        var left = (ActualWidth - width) / 2.0;
        var wide = width >= 670.0;
        var columns = wide ? 4 : 2;
        const double gap = 8.0;
        const double inset = 12.0;
        const double cardHeight = 146.0;
        var cardWidth = Math.Max(1.0,
            (width - 2.0 * inset - (columns - 1) * gap) / columns);
        var entries = new (string Label, string WeightId, string ChanceId, Brush Tone)[]
        {
            ("BANDAGES", "loot.stashMedicalBandageBias",
                "loot.stashMedicalBandageChanceScale", StashMedicalPalette[0]),
            ("MEDKITS", "loot.stashMedicalMedkitBias",
                "loot.stashMedicalMedkitChanceScale", StashMedicalPalette[1]),
            ("ARMY MEDKITS", "loot.stashMedicalArmyMedkitBias",
                "loot.stashMedicalArmyMedkitChanceScale", StashMedicalPalette[2]),
            ("ANTI-RAD", "loot.stashMedicalAntiRadBias",
                "loot.stashMedicalAntiRadChanceScale", StashMedicalPalette[3])
        };
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            var x = left + inset + (i % columns) * (cardWidth + gap);
            var y = 53.0 + (i / columns) * (cardHeight + gap);
            var card = new Rect(x, y, cardWidth, cardHeight);
            var chance = _stashMedicalShowChance[i];
            DrawSourceKnob(dc, P(chance ? entry.ChanceId : entry.WeightId),
                entry.Label, card, text, muted, border, accent, panel,
                percentageReadout: chance, dialAccent: entry.Tone, dialScale: .74);
            DrawStashMedicalModeSelector(dc, i, card, entry.WeightId,
                entry.ChanceId, chance, entry.Tone, muted, border);
        }
        DrawText(dc, "QUANTITY",
            new Point(left + 12.0, wide ? 208.0 : 365.0),
            10.8, text, FontWeights.Bold);
        DrawStashMedicalQuantityCards(dc, left, width,
            wide ? 229.0 : 386.0, wide, text, muted, border, panel);
    }

    private void DrawStashMedicalModeSelector(
        DrawingContext dc, int index, Rect card,
        string weightId, string chanceId, bool chance,
        Brush tone, Brush muted, Brush border)
    {
        var half = Math.Min(65.0, (card.Width - 16.0) / 2.0);
        var x = card.Left + (card.Width - half * 2.0) / 2.0;
        for (var mode = 0; mode < 2; mode++)
        {
            var isChance = mode == 1;
            var selected = chance == isChance;
            var rect = new Rect(x + mode * half, card.Top + 36.0, half, 21.0);
            var enabled = P(isChance ? chanceId : weightId)?.IsControlEnabled == true;
            var hover = _stashMedicalHoveredMode == index
                && _stashMedicalHoveredChance == isChance;
            DrawCenteredText(dc, isChance ? "CHANCE" : "WEIGHT",
                new Point(rect.Left + rect.Width / 2.0, rect.Top + 2.0),
                9.1, enabled && (selected || hover) ? Brushes.White : muted,
                selected ? FontWeights.Bold : FontWeights.SemiBold);
            dc.DrawLine(new Pen(selected ? tone : border, selected ? 2.0 : .8),
                new Point(rect.Left + 7.0, rect.Bottom - 1.0),
                new Point(rect.Right - 7.0, rect.Bottom - 1.0));
            if (enabled) _stashMedicalModes.Add((rect, index, isChance));
        }
    }

    // Two independently editable handles on ONE two-way rail per item.
    // MIN: 0 (VANILLA) to 6, MAX: -1 (0 items) / 0 (VANILLA) to 6.
    private void DrawStashMedicalQuantityCards(
        DrawingContext dc, double left, double width, double top, bool wide,
        Brush text, Brush muted, Brush border, Brush panel)
    {
        var families = new (string Name, string Min, string Max, Brush Tone)[]
        {
            ("MEDKITS", "loot.stashMedkitMinCount", "loot.stashMedkitMaxCount",
                StashMedicalPalette[1]),
            ("ARMY MEDKITS", "loot.stashArmyMedkitMinCount",
                "loot.stashArmyMedkitMaxCount", StashMedicalPalette[2])
        };
        const double gap = 10.0;
        var columns = wide ? 2 : 1;
        var cardWidth = (width - gap * (columns - 1)) / columns;
        for (var i = 0; i < families.Length; i++)
        {
            var family = families[i];
            var x = left + (i % columns) * (cardWidth + gap);
            var y = top + (i / columns) * 107.0;
            DrawStashQuantityDualRail(dc, P(family.Min), P(family.Max),
                family.Name, new Rect(x, y, cardWidth, 98.0),
                text, muted, border, panel, family.Tone);
        }
    }

    private void DrawStashQuantityDualRail(
        DrawingContext dc, ParameterViewModel? min, ParameterViewModel? max,
        string name, Rect card, Brush text, Brush muted, Brush border,
        Brush panel, Brush tone)
    {
        var focused = IsSelected(min) || IsSelected(max);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, focused ? .72 : .58),
            new Pen(CloneWithOpacity(focused ? tone : border,
                focused ? .84 : .54), focused ? 1.2 : .9),
            card, 4.0, 4.0);
        dc.DrawRectangle(tone, null,
            new Rect(card.Left + 9.0, card.Top + 9.0, 3.0, 12.0));
        DrawText(dc, name, new Point(card.Left + 18.0, card.Top + 7.0),
            10.8, text, FontWeights.Bold);
        if (min is null || max is null)
        {
            DrawCenteredText(dc, "LOADING",
                new Point(card.Left + card.Width / 2.0, card.Top + 48.0),
                10.0, muted, FontWeights.SemiBold);
            return;
        }

        static string CountLabel(double value) => value < -.5 ? "0"
            : Math.Abs(value) < .5 ? "VANILLA"
            : Math.Round(value).ToString(CultureInfo.InvariantCulture);
        void DrawCaption(ParameterViewModel p, string title, Point point)
        {
            var label = CountLabel(p.CurrentNumber);
            var rendered = CreateText(title + "  " + label, 10.7,
                p.IsControlEnabled ? text : muted, FontWeights.SemiBold,
                Math.Max(1.0, card.Width / 2.0 - 18.0));
            if (p.IsControlEnabled && p.IsValueModifiedFromReference()
                && label != "VANILLA")
                rendered.SetForegroundBrush(Brushes.Yellow, title.Length + 2,
                    label.Length);
            dc.DrawText(rendered, point);
        }
        DrawCaption(min, "MIN", new Point(card.Left + 13.0, card.Top + 33.0));
        DrawCaption(max, "MAX",
            new Point(card.Left + card.Width / 2.0 + 9.0, card.Top + 33.0));
        var middle = card.Left + card.Width / 2.0;
        var y = card.Top + 68.0;
        var minRail = new Rect(card.Left + 18.0, y,
            Math.Max(1.0, middle - card.Left - 30.0), 1.0);
        var maxRail = new Rect(middle + 12.0, y,
            Math.Max(1.0, card.Right - middle - 30.0), 1.0);

        void DrawHalf(ParameterViewModel p, Rect rail,
            double minimum, double maximum, bool reverse)
        {
            dc.DrawLine(new Pen(CloneWithOpacity(border, .94), 3.0),
                new Point(rail.Left, y), new Point(rail.Right, y));
            var pos = ValueToX(p.CurrentNumber, minimum, maximum, rail, reverse);
            var origin = ValueToX(0.0, minimum, maximum, rail, reverse);
            if (p.IsControlEnabled)
                dc.DrawLine(new Pen(CloneWithOpacity(tone, .84), 3.2),
                    new Point(origin, y), new Point(pos, y));
            dc.DrawLine(new Pen(CloneWithOpacity(muted, .6), 1.4),
                new Point(origin, y - 7.0), new Point(origin, y + 7.0));
            var hover = _hoverTarget is { Kind: HitKind.Horizontal } target
                && ReferenceEquals(target.Parameter, p);
            dc.DrawEllipse(panel,
                new Pen(hover ? Brushes.White : p.IsControlEnabled ? tone : muted,
                    hover ? 2.8 : 2.0),
                new Point(pos, y), 7.0, 7.0);
            if (p.IsControlEnabled)
                dc.DrawEllipse(tone, null, new Point(pos, y), 2.5, 2.5);
            AddHorizontalTarget(p,
                new Rect(rail.Left - 6.0, y - 14.0, rail.Width + 12.0, 32.0),
                rail, minimum, maximum, reverse);
        }
        DrawHalf(min, minRail, 0.0, 6.0, true);
        DrawHalf(max, maxRail, -1.0, 6.0, false);
    }

    // Bonus Gear uses the writer's same 0.1x integer weight precision.
    // These are fractions of authored relative CLASS weight, not physical
    // stash drop odds or the final two-without-replacement pick frequency.
    private static double[] StashGearWeightShares(IReadOnlyList<double> raw)
    {
        var ticks = raw.Select(value => Math.Clamp(
            (int)Math.Round(value * 10.0), 0, 30)).ToArray();
        var total = ticks.Sum();
        return ticks.Select(value => total == 0 ? 0.0 :
            (double)value / total).ToArray();
    }

    private static readonly Brush[] StashGearMixPalette =
    {
        new SolidColorBrush(Color.FromRgb(0xE8, 0xA0, 0x54)), // Weapons
        new SolidColorBrush(Color.FromRgb(0x66, 0xAE, 0xD8)), // Armor
        new SolidColorBrush(Color.FromRgb(0x61, 0xC9, 0x99)), // Artifacts
        new SolidColorBrush(Color.FromRgb(0xAC, 0x94, 0xDE))  // Attachments
    };

    // Actor coverage is a build-time subset; four compact category knobs
    // set relative distribution weights, not physical drop percentages.
    private void DrawStashGear(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawHeader(dc, "EXTRA STASH GEAR", "", text, muted, accent);
        var width = Math.Min(630.0, Math.Max(1.0, ActualWidth - 30.0));
        var left = (ActualWidth - width) / 2.0;
        DrawTogglePill(dc, P("loot.stashGearEnabled"), "ENABLE EXTRA GEAR",
            new Rect(left + 2.0, 52.0, Math.Min(252.0, width - 4.0), 30.0),
            text, muted, border, accent, panel);

        var coverage = P("loot.stashGearCoveragePercent");
        if (coverage is null) return;
        const double y = 99.0;
        DrawText(dc, "ELIGIBLE STASHES", new Point(left + 2.0, y),
            11.0, coverage.IsControlEnabled ? text : muted, FontWeights.SemiBold);
        DrawStashNumericReadout(dc, $"{(int)Math.Round(coverage.CurrentNumber)}%",
            new Point(left + width - 4.0, y - 1.0), 13.0,
            coverage.IsControlEnabled, coverage.IsValueModifiedFromReference(),
            text, muted, accent);

        var rail = new Rect(left + 10.0, y + 34.0, Math.Max(1.0, width - 20.0), 2.0);
        dc.DrawLine(new Pen(border, 2.0),
            new Point(rail.Left, rail.Top), new Point(rail.Right, rail.Top));
        var active = coverage.IsControlEnabled;
        var position = ValueToX(coverage.CurrentNumber, 0.0, 100.0, rail, false);
        dc.DrawLine(new Pen(active ? accent : muted, 2.0),
            new Point(rail.Left, rail.Top), new Point(position, rail.Top));
        dc.DrawEllipse(panel, new Pen(active ? accent : muted, 2.2),
            new Point(position, rail.Top), 6.5, 6.5);
        if (active)
            dc.DrawEllipse(accent, null, new Point(position, rail.Top), 2.5, 2.5);
        DrawText(dc, "0%", new Point(rail.Left, rail.Top + 12.0),
            10.0, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, "100%", new Point(rail.Right, rail.Top + 12.0),
            10.0, muted, FontWeights.Normal);
        AddHorizontalTarget(coverage,
            new Rect(rail.Left - 7.0, rail.Top - 15.0, rail.Width + 14.0, 44.0),
            rail, 0.0, 100.0);

        DrawText(dc, "BONUS MIX", new Point(left + 2.0, 177.0),
            11.2, text, FontWeights.Bold);

        // Four source knobs; two rows of two when a 4-wide dial bank
        // would be too narrow for clear captions and hit targets.
        var narrowMix = width < 540.0;
        var mixColumns = narrowMix ? 2 : 4;
        var mixStrip = new Rect(left + 2.0, 194.0,
            Math.Max(1.0, width - 4.0), narrowMix ? 390.0 : 224.0);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .56),
            new Pen(CloneWithOpacity(border, .65), .9),
            mixStrip, 4.0, 4.0);

        var weights = new[]
        {
            ("loot.stashGearWeaponWeight", "WEAPONS"),
            ("loot.stashGearArmorWeight", "ARMOR"),
            ("loot.stashGearArtifactWeight", "ARTIFACTS"),
            ("loot.stashGearAttachmentWeight", "ATTACHMENTS")
        };
        const double mixGap = 8.0;
        const double mixInset = 8.0;
        var mixWidth = Math.Max(1.0,
            (mixStrip.Width - 2.0 * mixInset
                - (mixColumns - 1) * mixGap) / mixColumns);
        for (var i = 0; i < weights.Length; i++)
        {
            var cell = new Rect(mixStrip.Left + mixInset
                    + (i % mixColumns) * (mixWidth + mixGap),
                mixStrip.Top + (i / mixColumns) * 166.0, mixWidth, 166.0);
            DrawSourceKnob(dc, P(weights[i].Item1), weights[i].Item2,
                cell, text, muted, border, accent, panel,
                dialAccent: StashGearMixPalette[i], dialScale: .74);
        }

        // Read-only weight share. The color, order and label of every segment
        // are tied directly to the corresponding knob, with no extra hit zone.
        // Two class picks happen without replacement: displayed percentages
        // are relative source WEIGHTS, never actual category/drop chances.
        var shareTop = mixStrip.Top + (narrowMix ? 332.0 : 166.0);
        var shareEnabled = P("loot.stashGearEnabled")?.CurrentBoolean == true;
        var shares = StashGearWeightShares(weights.Select(channel =>
            P(channel.Item1)?.CurrentNumber ?? 0.0).ToArray());
        DrawText(dc, "WEIGHT SHARE",
            new Point(mixStrip.Left + 10.0, shareTop + 1.0),
            9.1, shareEnabled ? muted : CloneWithOpacity(muted, .55),
            FontWeights.SemiBold);
        var shareTrack = new Rect(mixStrip.Left + 10.0, shareTop + 20.0,
            Math.Max(1.0, mixStrip.Width - 20.0), 8.0);
        dc.DrawRoundedRectangle(CloneWithOpacity(border, .32), null,
            shareTrack, 2.0, 2.0);
        var shareX = shareTrack.Left;
        for (var i = 0; i < shares.Length; i++)
        {
            var segmentWidth = i == shares.Length - 1
                ? Math.Max(0.0, shareTrack.Right - shareX)
                : Math.Max(0.0, shareTrack.Width * shares[i]);
            if (shares[i] > Epsilon && segmentWidth > Epsilon)
                dc.DrawRectangle(shareEnabled ? StashGearMixPalette[i] :
                    CloneWithOpacity(muted, .25), null,
                    new Rect(shareX, shareTrack.Top, segmentWidth, shareTrack.Height));
            shareX += segmentWidth;
        }
        // The fixed legend stays readable even when a segment is zero or too
        // narrow to contain text. No hover-only information or duplicate knob.
        var shareCell = shareTrack.Width / shares.Length;
        for (var i = 0; i < shares.Length; i++)
        {
            var x = shareTrack.Left + i * shareCell;
            dc.DrawRectangle(shareEnabled ? StashGearMixPalette[i] :
                CloneWithOpacity(muted, .35), null,
                new Rect(x, shareTop + 37.0, 6.0, 6.0));
            DrawText(dc, weights[i].Item2,
                new Point(x + 9.0, shareTop + 33.0),
                narrowMix ? 8.1 : 8.8,
                shareEnabled ? text : muted, FontWeights.SemiBold);
            DrawRightAlignedText(dc, $"{shares[i] * 100.0:0.#}%",
                new Point(x + shareCell - 3.0, shareTop + 33.0),
                narrowMix ? 8.7 : 9.4, muted, FontWeights.SemiBold);
        }

        // One real CFG Chance setting, used for every optional bonus category.
        // Unlike the relative mixer weights, this slider writes PossibleItems.Chance.
        var chance = P("loot.stashGearBonusChance");
        if (chance is null) return;
        var chanceY = narrowMix ? 593.0 : 429.0;
        var chanceEnabled = chance.IsControlEnabled;
        DrawText(dc, "BONUS CHANCE · ALL ITEMS", new Point(left + 2.0, chanceY),
            11.0, chanceEnabled ? text : muted, FontWeights.SemiBold);
        DrawStashNumericReadout(dc, $"{(int)Math.Round(chance.CurrentNumber)}%",
            new Point(left + width - 4.0, chanceY - 1.0), 13.0,
            chanceEnabled, chance.IsValueModifiedFromReference(),
            text, muted, accent);
        var chanceRail = new Rect(left + 10.0, chanceY + 27.0,
            Math.Max(1.0, width - 20.0), 2.0);
        dc.DrawLine(new Pen(border, 2.0),
            new Point(chanceRail.Left, chanceRail.Top),
            new Point(chanceRail.Right, chanceRail.Top));
        var chanceX = ValueToX(chance.CurrentNumber, 0.0, 100.0, chanceRail, false);
        if (chanceEnabled)
            dc.DrawLine(new Pen(accent, 2.0),
                new Point(chanceRail.Left, chanceRail.Top),
                new Point(chanceX, chanceRail.Top));
        var chanceHover = chanceEnabled && ReferenceEquals(_hoverTarget?.Parameter, chance);
        dc.DrawEllipse(panel, new Pen(chanceHover ? Brushes.White :
            chanceEnabled ? accent : muted, chanceHover ? 2.6 : 2.0),
            new Point(chanceX, chanceRail.Top), 6.0, 6.0);
        if (chanceEnabled)
            dc.DrawEllipse(accent, null, new Point(chanceX, chanceRail.Top), 2.4, 2.4);
        DrawText(dc, "0%", new Point(chanceRail.Left, chanceRail.Top + 11.0),
            9.8, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, "100%",
            new Point(chanceRail.Right, chanceRail.Top + 11.0),
            9.8, muted, FontWeights.Normal);
        AddHorizontalTarget(chance,
            new Rect(chanceRail.Left - 7.0, chanceRail.Top - 14.0,
                chanceRail.Width + 14.0, 42.0), chanceRail, 0.0, 100.0);
    }

    // The UI reads the SAME embedded ItemGenerator source templates as the
    // writer. Source categories are independent rolls; only Weight values
    // inside one exact PossibleItems branch can be normalized together.
    private static readonly Lazy<CfgWriterBaseline> StashPreviewBaseline =
        new(() => new CfgWriterBaselineService().Load());

    private static CfgWriterCheapStashCategory? StashPreviewGroup(string tier, int index)
    {
        var sources = StashPreviewBaseline.Value.Special;
        var groups = tier switch
        {
            "BASIC" => sources.CheapStashProduction.TemplateCategories,
            "STANDARD" => sources.MediumStashAmmoProduction.TemplateCategories,
            "RARE" => sources.ExpensiveStashProduction.TemplateCategories,
            _ => null
        };
        return groups?.FirstOrDefault(g => g.Node == $"[{index}]");
    }

    // Same immutable numeric snapshot as the actual CFG patch writer.
    // Preview sample is explicitly Easy for difficulty-gated Rare Coupons;
    // the branch selector for all difficulty/rank variants comes in Phase 4.
    private StashEffectiveState CurrentStashEffectiveState(string difficultySid = "Easy")
    {
        var sources = _parameters.Values.Concat(_linkedSources.Values)
            .GroupBy(p => p.Id, StringComparer.Ordinal)
            .Select(g => g.First()).ToArray();
        return new StashEffectiveState(sources,
            sources.Where(p => p.IsValueModifiedFromReference()).Select(p => p.Id),
            difficultySid);
    }

    private void DrawStashPoolBar(
        DrawingContext dc, CfgWriterCheapStashCategory? group,
        string caption, Rect row, Brush text, Brush muted, Brush border,
        string? rankCaption = null)
    {
        if (group is null) return;
        var totals = new double[6];
        foreach (var item in group.Items)
        {
            if (!item.Fields.TryGetValue("Weight", out var raw)
                || !item.Fields.TryGetValue("ItemPrototypeSID", out var sid))
                continue;
            var category = StashCategoryIndex(sid);
            if (category >= 0)
                totals[category] += StashEffectiveState.Number(
                    CurrentStashEffectiveState().TuneSelection(sid, "Weight", raw));
        }
        var total = totals.Sum();
        // Difficulty and player rank are independent authored CFG gates.
        var captionSize = row.Width < 440.0 ? 9.6 : 10.8;
        if (rankCaption is null)
            DrawText(dc, caption, new Point(row.Left, row.Top), captionSize,
                text, FontWeights.SemiBold);
        else
        {
            var secondary = CreateText(rankCaption, row.Width < 440.0 ? 9.0 : 9.8,
                muted, FontWeights.SemiBold, Math.Max(1.0, row.Width * .46));
            var rightX = row.Right - secondary.WidthIncludingTrailingWhitespace;
            dc.DrawText(CreateText(caption, captionSize, text,
                    FontWeights.SemiBold, Math.Max(1.0, rightX - row.Left - 8.0)),
                new Point(row.Left, row.Top));
            dc.DrawText(secondary, new Point(rightX, row.Top));
        }
        var bar = new Rect(row.Left, row.Top + 15.0, row.Width, 11.0);
        dc.DrawRoundedRectangle(CloneWithOpacity(border, .28), null,
            bar, 2.0, 2.0);
        var labels = new[] { "AMMO", "MEDICAL", "FOOD", "DRINKS", "GRENADES", "COUPONS" };
        var cursor = bar.Left;
        for (var i = 0; i < totals.Length; i++)
        {
            var fraction = total > Epsilon ? totals[i] / total : 0.0;
            var span = bar.Width * fraction;
            if (span > Epsilon)
                dc.DrawRectangle(StashMixPalette[i], null,
                    new Rect(cursor, bar.Top, span, bar.Height));
            cursor += span;
        }
        // Only categories with a native member in THIS exact source pool
        // belong in the legend. Disabled members retain their 0% label.
        var present = group.Items
            .Select(x => x.Fields.TryGetValue("ItemPrototypeSID", out var sid)
                ? StashCategoryIndex(sid) : -1)
            .Where(x => x >= 0).Distinct().OrderBy(x => x).ToArray();
        for (var col = 0; col < present.Length; col++)
        {
            var kind = present[col];
            var x = row.Left + col * row.Width / present.Length;
            var slot = row.Width / present.Length;
            dc.DrawRectangle(StashMixPalette[kind], null,
                new Rect(x, row.Top + 31.0, 7.0, 7.0));
            // Larger captions on a wide editor, compact labels on narrow screens.
            var legendSize = row.Width >= 580.0 ? 10.0 : 8.9;
            DrawText(dc, labels[kind], new Point(x + 10.0, row.Top + 28.0),
                legendSize, text, FontWeights.SemiBold);
            var share = total > Epsilon ? totals[kind] * 100.0 / total : 0.0;
            DrawRightAlignedText(dc, $"{share:0.#}%",
                new Point(x + slot - 3.0, row.Top + 28.0),
                row.Width >= 580.0 ? 10.1 : 9.1,
                muted, FontWeights.SemiBold);
        }
    }

    private static CfgWriterCheapStashItem? StashChanceSample(
        string tier, int groupIndex, string expectedSid)
    {
        var group = StashPreviewGroup(tier, groupIndex);
        return group?.Items.FirstOrDefault(x =>
            x.Fields.TryGetValue("ItemPrototypeSID", out var sid)
            && sid == expectedSid && x.Fields.ContainsKey("Chance"));
    }

    private void DrawStashChanceSample(
        DrawingContext dc, string tier, int groupIndex, string sid,
        string label, Rect cell, Brush text, Brush muted, Brush border)
    {
        var source = StashChanceSample(tier, groupIndex, sid);
        if (source is null) return;
        var kind = StashCategoryIndex(sid);
        if (kind < 0) return;
        var category = StashPreviewGroup(tier, groupIndex)!;
        var effective = CurrentStashEffectiveState();
        var tierName = tier switch
        {
            "BASIC" => "Cheap", "STANDARD" => "Medium",
            "RARE" => "Expensive",
            _ => throw new InvalidDataException("Invalid preview tier " + tier)
        };
        // Production tier writer calls TierField for this very category/item.
        // Includes special RGD-5 and difficulty-gated Rare Coupons bonuses.
        var chance = StashEffectiveState.Number(effective.TierField(
            StashPreviewBaseline.Value, tierName, category, source, "Chance"));
        DrawText(dc, label, new Point(cell.Left + 1.0, cell.Top),
            9.4, text, FontWeights.SemiBold);
        DrawRightAlignedText(dc, $"{chance * 100.0:0.#}%",
            new Point(cell.Right - 1.0, cell.Top), 9.6,
            muted, FontWeights.SemiBold);
        var line = new Rect(cell.Left + 1.0, cell.Top + 17.0,
            Math.Max(1.0, cell.Width - 2.0), 6.0);
        dc.DrawRoundedRectangle(CloneWithOpacity(border, .25),
            null, line, 2.0, 2.0);
        if (chance > Epsilon)
            dc.DrawRoundedRectangle(StashMixPalette[kind], null,
                new Rect(line.Left, line.Top, chance * line.Width, line.Height),
                2.0, 2.0);
    }

    private void DrawStashSourcePreview(
        DrawingContext dc, Rect card, Brush text, Brush muted,
        Brush border, Brush accent, Brush panel)
    {
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .60),
            new Pen(CloneWithOpacity(border, .66), .9), card, 4.0, 4.0);
        DrawText(dc, "NATIVE SOURCE POOLS",
            new Point(card.Left + 11.0, card.Top + 7.0),
            10.4, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "WEIGHT ONLY WITHIN EACH POOL",
            new Point(card.Right - 11.0, card.Top + 7.0),
            8.9, muted, FontWeights.SemiBold);
        var inner = new Rect(card.Left + 12.0, card.Top + 27.0,
            Math.Max(1.0, card.Width - 24.0), 48.0);
        DrawStashPoolBar(dc, StashPreviewGroup("BASIC", 7),
            "BASIC / EASY+MEDIUM / EXPERIENCED / CONSUMABLE",
            inner, text, muted, border);
        DrawStashPoolBar(dc, StashPreviewGroup("BASIC", 1),
            "BASIC / HARD / CONSUMABLE",
            new Rect(inner.Left, inner.Top + 57.0, inner.Width, inner.Height),
            text, muted, border);
        DrawText(dc, "SOURCE CHANCE FIELDS",
            new Point(inner.Left, card.Top + 146.0),
            10.0, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "NATIVE CHANCE · INDEPENDENT",
            new Point(inner.Right, card.Top + 146.0),
            8.6, muted, FontWeights.SemiBold);
        var samples = new (string Tier, int Group, string Sid, string Label)[]
        {
            ("RARE", 1, "A918A", "AMMO · RARE"),
            ("BASIC", 0, "Bandage", "MEDICAL · BASIC"),
            ("RARE", 0, "CannedFood", "FOOD · RARE"),
            ("BASIC", 0, "Vodka", "DRINKS · BASIC"),
            ("BASIC", 5, "GrenadeRGD5", "GRENADES · BASIC"),
            ("RARE", 11, "MoneyCommon", "COUPONS · RARE EASY")
        };
        var columns = ActualWidth >= 700.0 ? 3 : 2;
        var gap = 13.0;
        var cellWidth = (inner.Width - (columns - 1) * gap) / columns;
        for (var i = 0; i < samples.Length; i++)
        {
            var sample = samples[i];
            DrawStashChanceSample(dc, sample.Tier, sample.Group,
                sample.Sid, sample.Label,
                new Rect(inner.Left + (i % columns) * (cellWidth + gap),
                    card.Top + 165.0 + (i / columns) * 32.0,
                    cellWidth, 27.0), text, muted, border);
        }
        DrawText(dc, "Easy-tier examples; not whole-stash drop probability.",
            new Point(inner.Left, card.Bottom - 19.0),
            9.0, muted, FontWeights.Normal);
    }

    // Same value alignment and changed-digit-only gold rule on ALL Stashes
    // graphs. Units/suffixes (x, %, ITEMS) remain white even when modified.
    private void DrawStashNumericReadout(
        DrawingContext dc, string value, Point right, double size,
        bool enabled, bool modified, Brush text, Brush muted, Brush accent)
    {
        var formatted = CreateText(value, size, enabled ? text : muted,
            FontWeights.SemiBold);
        if (enabled && modified)
        {
            foreach (System.Text.RegularExpressions.Match number in
                System.Text.RegularExpressions.Regex.Matches(
                    value, @"-?\d+(?:\.\d+)?"))
                formatted.SetForegroundBrush(accent, number.Index, number.Length);
        }
        var x = Math.Min(right.X, ActualWidth - 12.0)
            - formatted.WidthIncludingTrailingWhitespace;
        dc.DrawText(formatted, new Point(x, right.Y));
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
    // Shared geometry for responsive 3+2 grouped source mini-knobs.
    private const double ConsumableMiniKnobHeight = 128.0;
    private const double ConsumableEffectsGroupTop = 300.0;
    private const double ConsumableEffectsHeaderHeight = 24.0;
    private const double ConsumableEffectsGroupPadding = 8.0;
    private const double ConsumableEffectsCellGap = 6.0;
    private const double ConsumableEffectsGroupGap = 12.0;
    private const double ConsumableEffectsInlineMinimumWidth = 730.0;
    private const double ConsumableEffectsGroupHeight =
        ConsumableEffectsHeaderHeight + ConsumableMiniKnobHeight + 8.0;

    private static double ConsumableTimelineHeight(double width)
    {
        var bankWidth = Math.Min(850.0, Math.Max(460.0, width - 34.0) - 24.0);
        return ConsumableEffectsGroupTop +
            (bankWidth >= ConsumableEffectsInlineMinimumWidth
                ? ConsumableEffectsGroupHeight
                : 2.0 * ConsumableEffectsGroupHeight + ConsumableEffectsGroupGap)
            + 12.0;
    }
    // Reserved space for plot ticks plus three independently readable stage lines.
    private const double HungerProfileHeight = 344.0;
    private const double CompactRowGap = 8.0;
    private const double CompactColumnGap = 10.0;





    // Compact form of the shared native 270-degree knob: same editable
    // ParameterViewModel and HitKind.Knob angle mapping, with a smaller ring.
    // The actual CFG scalar is independent from the percentage/seconds readout.
    private void DrawMiniConsumableKnob(
        DrawingContext dc, ParameterViewModel parameter, string caption,
        string value, string minimumLabel, string maximumLabel, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel,
        Brush effectBrush)
    {
        // The enclosing WATER / ENERGY DRINK group owns the only frame.
        var active = parameter.IsControlEnabled;
        var title = CreateText(caption, card.Width < 166.0 ? 9.0 : 9.8,
            active ? text : muted, FontWeights.Bold,
            Math.Max(1.0, card.Width - 12.0));
        dc.DrawText(title, new Point(card.Left + (card.Width -
            title.WidthIncludingTrailingWhitespace) / 2.0, card.Top + 6.0));

        var center = new Point(card.Left + card.Width / 2.0, card.Top + 67.0);
        var radius = Math.Min(32.0, Math.Max(18.0, (card.Width - 48.0) / 2.0));
        var span = Math.Max(Epsilon, parameter.Maximum - parameter.Minimum);
        var t = Math.Clamp((parameter.CurrentNumber - parameter.Minimum) / span, 0.0, 1.0);
        var reference = Math.Clamp((parameter.ReferenceNumber -
            parameter.Minimum) / span, 0.0, 1.0);
        var dialInk = active ? effectBrush : muted;
        dc.DrawEllipse(CloneWithOpacity(panel, .96), null,
            center, radius - 6.0, radius - 6.0);
        DrawKnobArc(dc, center, radius, WeaponKnobStartAngle,
            WeaponKnobSweepAngle, new Pen(border, 4.0));
        // Graduations are only actual multiples of TickFrequency; input
        // remains at full native precision even when most ticks are omitted.
        var nativeStep = Math.Max(Epsilon, parameter.TickFrequency);
        var count = (int)Math.Floor(span / nativeStep + .000001);
        var pxPerStep = radius * WeaponKnobSweepAngle * Math.PI / 180.0 /
            Math.Max(1.0, span / nativeStep);
        var stride = KnobVisibleStepStride(pxPerStep);
        for (var i = 0; i <= count; i += stride)
        {
            var angle = WeaponKnobStartAngle +
                (parameter.Minimum + i * nativeStep - parameter.Minimum) /
                span * WeaponKnobSweepAngle;
            dc.DrawLine(new Pen(CloneWithOpacity(muted, .60), 1.0),
                KnobPoint(center, radius + 3.0, angle),
                KnobPoint(center, radius + 8.0, angle));
        }
        if (t > Epsilon)
            DrawKnobArc(dc, center, radius, WeaponKnobStartAngle,
                t * WeaponKnobSweepAngle,
                new Pen(CloneWithOpacity(dialInk, .95), 4.2));
        var baseAngle = WeaponKnobStartAngle + reference * WeaponKnobSweepAngle;
        dc.DrawLine(new Pen(muted, 2.0),
            KnobPoint(center, radius - 5.0, baseAngle),
            KnobPoint(center, radius + 4.0, baseAngle));

        var angleNow = WeaponKnobStartAngle + t * WeaponKnobSweepAngle;
        var hovered = _hoverTarget?.Kind == HitKind.Knob &&
            ReferenceEquals(_hoverTarget.Parameter, parameter);
        if (hovered)
            dc.DrawEllipse(CloneWithOpacity(dialInk, .13), null,
                center, radius - 5.0, radius - 5.0);
        dc.DrawLine(new Pen(hovered ? Brushes.White : dialInk, hovered ? 2.8 : 2.0),
            KnobPoint(center, radius - 6.0, angleNow),
            KnobPoint(center, radius + 2.0, angleNow));
        dc.DrawEllipse(dialInk, null,
            KnobPoint(center, radius + 2.0, angleNow), 1.5, 1.5);

        var formatted = CreateText(value, 12.4, active ? text : muted,
            FontWeights.Bold, Math.Max(1.0, card.Width - 8.0));
        if (active && parameter.IsValueModifiedFromReference())
            foreach (System.Text.RegularExpressions.Match token in
                System.Text.RegularExpressions.Regex.Matches(value, @"\d+(?:\.\d+)?"))
                formatted.SetForegroundBrush(accent, token.Index, token.Length);
        dc.DrawText(formatted, new Point(center.X -
            formatted.WidthIncludingTrailingWhitespace / 2.0, center.Y - 8.0));
        // Stashes knobs share the same small white lower value grammar as
        // the other native knobs. Keep ConsumableMixer's original typography.
        var stashDial = Family is "StashMedicalComposition" or "StashGear";
        var endpointSize = stashDial ? (card.Width < 166.0 ? 9.0 : 9.8) : 8.7;
        var endpointInk = stashDial ? Brushes.White : muted;
        var endpointY = card.Bottom - (stashDial ? 22.0 : 17.0);
        DrawCenteredText(dc, minimumLabel,
            new Point(center.X - radius * .80, endpointY),
            endpointSize, endpointInk, FontWeights.SemiBold);
        DrawCenteredText(dc, maximumLabel,
            new Point(center.X + radius * .80, endpointY),
            endpointSize, endpointInk, FontWeights.SemiBold);
        if (!active) return;
        var hit = new Rect(center.X - radius - 6.0, center.Y - radius - 6.0,
            2.0 * (radius + 6.0), 2.0 * (radius + 6.0));
        _hitTargets.Add(new HitTarget(parameter, hit, hit, HitKind.Knob,
            parameter.Minimum, parameter.Maximum, false));
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
        Brush panel,
        bool highlightDerivedLastNumber = false)
    {
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), card, 4.0, 4.0);
        var contentWidth = Math.Max(1.0, card.Width - 24.0);

        // Determine the actual value width before reserving space for the title.
        // The old fixed 155-DIP reserve truncated labels despite unused room.
        var valueText = CreateText(value, 11.5,
            parameter.IsControlEnabled ? text : muted, FontWeights.Bold, contentWidth);
        if (parameter.IsControlEnabled && parameter.IsValueModifiedFromReference())
            foreach (System.Text.RegularExpressions.Match token in
                System.Text.RegularExpressions.Regex.Matches(value, @"[+\-−]?\d+(?:\.\d+)?(?:[%x×])?"))
            {
                valueText.SetForegroundBrush(accent, token.Index, token.Length);
                valueText.SetFontSize(12.7, token.Index, token.Length);
            }
        if (highlightDerivedLastNumber && parameter.IsControlEnabled)
        {
            var nums = System.Text.RegularExpressions.Regex.Matches(
                value, @"[+\-−]?\d+(?:\.\d+)?(?:[%x×])?");
            if (nums.Count > 0)
            {
                var derived = nums[nums.Count - 1];
                valueText.SetForegroundBrush(accent, derived.Index, derived.Length);
            }
        }
        // Measure after number highlights enlarge edited values; reserve a
        // genuine 10-DIP gap without making the compact card any wider.
        var labelWidth = Math.Max(1.0,
            contentWidth - valueText.WidthIncludingTrailingWhitespace - 10.0);
        // Single-line captions share the header with seconds. Shorten only
        // the three drink-effect captions on narrow cards instead of wrapping
        // or allowing their text to collide with the value.
        var visibleLabel = card.Width < 235.0 ? parameter.Id switch
        {
            "survival.waterRegenDuration" => "+25% STAMINA",
            "survival.energyFullCostDuration" => "NO STAMINA COST",
            "survival.energyPartialCostDuration" => "-30% STAMINA COST",
            _ => label
        } : label;
        dc.DrawText(CreateText(visibleLabel, card.Width < 235.0 ? 9.0 : 10.2,
                text, FontWeights.Bold, labelWidth),
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

        var trackAccent = accent;
        dc.DrawLine(new Pen(border, 4.0),
            new Point(track.Left, y), new Point(track.Right, y));
        if (parameter.IsControlEnabled)
            dc.DrawLine(new Pen(CloneWithOpacity(trackAccent, 0.9), 4.5),
                new Point(track.Left, y), new Point(currentX, y));
        // Effect-specific underlines are separate; the glow follows only the
        // real primary value and never extends to an unrelated duration marker.
        IndustrialRulerChrome.DrawActiveRailGlint(dc, track.Left, currentX, y,
            trackAccent, parameter.IsControlEnabled);
        DrawDrinkEffectLine(dc, parameter, track.Left, currentX, y);

        dc.DrawLine(new Pen(muted, 2.0),
            new Point(baseX, y - 10.0), new Point(baseX, y + 10.0));
        dc.DrawEllipse(panel,
            new Pen(parameter.IsControlEnabled ? trackAccent : muted,
                IsSelected(parameter) ? 2.8 : 1.9),
            new Point(currentX, y), 7.0, 7.0);
        if (parameter.IsControlEnabled)
            dc.DrawEllipse(trackAccent, null, new Point(currentX, y), 2.5, 2.5);

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

    private static string ComparablePairedValue(ParameterViewModel parameter, double rawValue)
        => parameter.Id switch
        {
            "economy.fastTravelCost" or "economy.upgradeCost" or
            "economy.traderAmmoStock" or "economy.medicStock" or
            "equipment.ammoWeight" or "equipment.consumableWeight" or
            "environment.anomalyDamage" or "environment.razorBleeding" or
            "environment.psyAnomalyIntensity" => $"{rawValue:0.##}x",
            "economy.repairCostBase" or
            "environment.flycatcherMovementSpeed" or
            "environment.flycatcherTurnSpeed" => $"{rawValue * 100.0:0.#}%",
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
        var enabled = parameter?.IsControlEnabled == true && !IsNpcBonusSliderLocked(parameter);
        var hovered = enabled && ReferenceEquals(_hoverTarget?.Parameter, parameter);
        IndustrialChrome.DrawRecessedCard(dc, card, panel,
            enabled ? border : CloneWithOpacity(border, 0.55));
        var textWidth = Math.Max(1.0, card.Width - 24.0);
        var labelSize = healthTypography ? HealthTypography.Label
            : combatTypography ? 11.2 : 9.8;
        var axisSize = healthTypography ? HealthTypography.Axis
            : combatTypography ? 10.0 : 9.0;
        // Reserve the actual width of the current HP/rate readout, not a
        // hard-coded 150 DIP lane that would clip health names on small windows.
        var valueReserve = (healthTypography || combatTypography) && parameter is not null
            ? CreateText(combatTypography ? $"{parameter.CurrentNumber:0.##}x" : FriendlyValue(parameter),
                (healthTypography ? HealthTypography.Value + 0.8 : 12.8) + 1.2,
                text, FontWeights.SemiBold).WidthIncludingTrailingWhitespace + 12.0
            : 126.0;
        dc.DrawText(CreateText(label, labelSize, enabled ? text : muted, FontWeights.Bold,
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
        var baseSize = healthTypography ? HealthTypography.Value + 0.8
            : combatTypography ? 12.8 : pairedValueFormatting ? 11.9 : 10.9;
        var valueText = CreateText(value, baseSize,
            enabled ? text : muted, FontWeights.SemiBold, textWidth);
        if (enabled && parameter.IsValueModifiedFromReference())
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
        if (enabled)
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.65), 3.5),
                new Point(track.Left, y), new Point(x, y));
        IndustrialRulerChrome.DrawActiveRailGlint(dc, track.Left, x, y,
            accent, enabled);

        IndustrialRulerChrome.DrawNativeTicks(dc, track,
            parameter.Minimum, parameter.Maximum, parameter.TickFrequency, muted);
        DrawReferenceTick(dc, parameter, track, y, muted);
        dc.DrawEllipse(panel,
            new Pen(enabled ? accent : muted,
                hovered || IsSelected(parameter) ? 2.5 : 1.5),
            new Point(x, y), 6.0, 6.0);
        if (enabled)
            dc.DrawEllipse(accent, null, new Point(x, y), 2.5, 2.5);

        if (enabled)
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
            VisualControlTypography.BoardTitle, accent, FontWeights.Bold);
        if (!string.IsNullOrWhiteSpace(subtitle))
            DrawCenteredText(dc, subtitle, new Point(ActualWidth / 2.0, 39.0),
                9.5, muted, FontWeights.Normal);
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

    private void AddToggleTarget(ParameterViewModel parameter, Rect hitRect)
    {
        if (!parameter.IsControlEnabled)
            return;

        _hitTargets.Add(new HitTarget(parameter, hitRect, hitRect, HitKind.Toggle, 0.0, 1.0, false));
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
        if (Family == "StashMedicalComposition")
        {
            var selector = _stashMedicalModes.FirstOrDefault(m => m.Hit.Contains(point));
            if (selector.Hit.Width > 0.0)
            {
                var id = selector.Index switch
                {
                    0 => selector.Chance ? "loot.stashMedicalBandageChanceScale" : "loot.stashMedicalBandageBias",
                    1 => selector.Chance ? "loot.stashMedicalMedkitChanceScale" : "loot.stashMedicalMedkitBias",
                    2 => selector.Chance ? "loot.stashMedicalArmyMedkitChanceScale" : "loot.stashMedicalArmyMedkitBias",
                    _ => selector.Chance ? "loot.stashMedicalAntiRadChanceScale" : "loot.stashMedicalAntiRadBias"
                };
                var p = P(id);
                if (p?.IsControlEnabled == true)
                {
                    _stashMedicalShowChance[selector.Index] = selector.Chance;
                    _selectedParameter = p;
                    _selectedHitKind = HitKind.Knob;
                    RaiseParameterSelected(p);
                    InvalidateVisual();
                    e.Handled = true;
                    return;
                }
            }
        }
        var couponMarker = Family == "CouponFlow"
            ? _couponMarkers.FirstOrDefault(m => m.Hit.Contains(point)) : default;
        if (couponMarker.Parameter is { IsControlEnabled: true })
        {
            _selectedParameter = couponMarker.Parameter;
            _selectedHitKind = HitKind.Horizontal;
            couponMarker.Parameter.CurrentNumber = Snap(couponMarker.Parameter, couponMarker.Value);
            RaiseParameterSelected(couponMarker.Parameter);
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        if (RadiationRingHit(point))
        {
            _selectedParameter = null;
            _selectedHitKind = null;
            _radRingDragging = true;
            _radRingHadScrub = false;
            _radRingCompleted = false;
            CaptureMouse();
            ScrubRadiationRing(point);
            e.Handled = true;
            return;
        }
        _radRingFocus = false;
        var target = FindTarget(point);
        if (target is null || !target.Parameter.IsControlEnabled
            || IsNpcBonusSliderLocked(target.Parameter))
            return;

        _selectedParameter = target.Kind == HitKind.FatigueReloadJoint
            ? ReloadEffectFor(target.Parameter) ?? target.Parameter
            : target.Parameter;
        _selectedHitKind = target.Kind;
        SetHoverTarget(target);
        RaiseParameterSelected(target.Parameter);

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
        if (Family == "StashMedicalComposition" && _dragTarget is null)
        {
            var selector = _stashMedicalModes.FirstOrDefault(m => m.Hit.Contains(point));
            var hoverIndex = selector.Hit.Width > 0.0 ? selector.Index : -1;
            if (_stashMedicalHoveredMode != hoverIndex
                || hoverIndex >= 0 && _stashMedicalHoveredChance != selector.Chance)
            {
                _stashMedicalHoveredMode = hoverIndex;
                _stashMedicalHoveredChance = selector.Chance;
                InvalidateVisual();
            }
            if (hoverIndex >= 0)
            {
                SetHoverTarget(null);
                Cursor = Cursors.Hand;
                return;
            }
        }
        if (Family == "CouponFlow" && _dragTarget is null)
        {
            var marker = _couponMarkers.FirstOrDefault(m => m.Hit.Contains(point));
            var next = marker.Parameter?.IsControlEnabled == true ? marker.Index : -1;
            if (_hoverCouponMarker != next)
            {
                _hoverCouponMarker = next;
                InvalidateVisual();
            }
            if (next >= 0)
            {
                Cursor = Cursors.Hand;
                return;
            }
        }
        if (_radRingDragging && e.LeftButton == MouseButtonState.Pressed)
        {
            if (RadiationRingHit(point))
                ScrubRadiationRing(point);
            e.Handled = true;
            return;
        }
        if (_dragTarget is null && RadiationRingHit(point))
        {
            SetHoverTarget(null);
            Cursor = Cursors.Hand;
            return;
        }
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
        if (target is not null && IsNpcBonusSliderLocked(target.Parameter))
            target = null;
        SetHoverTarget(target);
        Cursor = target?.Kind switch
        {
            HitKind.Horizontal or HitKind.RangeMin or HitKind.RangeMax => Cursors.SizeWE,
            HitKind.Vertical => Cursors.SizeNS,
            HitKind.Knob => Cursors.Hand,
            HitKind.HumanJoint or HitKind.HungerJoint or HitKind.FatigueJoint or HitKind.FatigueReloadJoint => Cursors.SizeAll,
            HitKind.Toggle or HitKind.DrinkDurationOverride or HitKind.Choice => Cursors.Hand,
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
        var pointer = e.GetPosition(this);
        if (RadiationRingHit(pointer))
        {
            var radWheelDirection = Math.Sign(e.Delta);
            if (radWheelDirection == 0) return;
            _selectedParameter = null;
            AdjustRadiationLevel(radWheelDirection);
            Focus();
            e.Handled = true;
            return;
        }
        var stashGraph = Family is "StashContentMix"
            or "StashMedicalComposition" or "StashGear";
        if (!stashGraph && Family is not ("PlayerWeaponProfile" or "AnomalyGlobalDamage" or "AmmoDistribution" or "ConsumableMixer" or "TraderStockBoard" or "EquipmentWearBalance" or "ServiceCostsBoard" or "RadiationBalance" or "HumanCombatProfile" or "StarvationTimeline" or "FlycatcherSlowdown" or "AnomalySpecialEffects"))
            return;

        var target = FindTarget(e.GetPosition(this));
        if (target is null
            || target.Kind != HitKind.Knob
               && !(stashGraph && target.Kind == HitKind.Horizontal)
            || !target.Parameter.IsControlEnabled)
            return;

        Focus();
        _radRingFocus = false;
        _selectedParameter = target.Parameter;
        _selectedHitKind = target.Kind;
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

        if (_radRingDragging)
        {
            _radRingDragging = false;
            _radRingHadScrub = false;
            _radRingCompleted = false;
            ReleaseMouseCapture();
            e.Handled = true;
            return;
        }
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
        if (_stashMedicalHoveredMode >= 0)
        {
            _stashMedicalHoveredMode = -1;
            InvalidateVisual();
        }
        if (_hoverCouponMarker >= 0)
        {
            _hoverCouponMarker = -1;
            InvalidateVisual();
        }
        if (_dragTarget is null && !_radRingDragging)
        {
            SetHoverTarget(null);
            Cursor = Cursors.Arrow;
        }
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (_radRingDragging)
        {
            _radRingDragging = false;
            _radRingHadScrub = false;
            _radRingCompleted = false;
            Cursor = Cursors.Arrow;
        }
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
        InvalidateVisual();
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);
        if (RadiationRingHit(e.GetPosition(this)))
        {
            _radLevel = 0.0;
            _radRingFocus = true;
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        var target = FindTarget(e.GetPosition(this));
        // Disabled sources must not be changed by right-click.
        if (target is null || !target.Parameter.IsControlEnabled
            || IsNpcBonusSliderLocked(target.Parameter))
            return;

        _radRingFocus = false;
        _selectedParameter = target.Kind == HitKind.FatigueReloadJoint
            ? ReloadEffectFor(target.Parameter) ?? target.Parameter
            : target.Parameter;
        _selectedHitKind = target.Kind;
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
                "EquipmentWearBalance" => "equipment.weaponWearPerShot",
                "FlycatcherSlowdown" => "environment.flycatcherTurnSpeed",
                _ => "combat.npcWeaponDamage"
            })?.ResetToReference();
        EnforceFamilyConstraints(target.Parameter);
        _status.ShowTransient(target.Kind == HitKind.HumanJoint
            ? (Family switch
            {
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

        // Category knobs use the same keys as all other source knobs.
        if (e.Key == Key.R)
        {
            if (Family == "RadiationBalance")
                _radLevel = 0.0;
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

        if (Family == "RadiationBalance" && _radRingFocus)
        {
            var radKeyStep = e.Key switch
            {
                Key.Right or Key.Up => 1,
                Key.Left or Key.Down => -1,
                _ => 0
            };
            if (radKeyStep != 0 || e.Key is Key.Home or Key.End)
            {
                if (e.Key == Key.Home) _radLevel = 0.0;
                else if (e.Key == Key.End) _radLevel = 100.0;
                else AdjustRadiationLevel(radKeyStep);
                InvalidateVisual();
                e.Handled = true;
                return;
            }
        }
        // A previously selected slider can become dependency-disabled when its
        // Master bundle is switched OFF. Keyboard input must respect that
        // transition just like mouse hit targets and the writer.
        if (_selectedParameter is null || !_selectedParameter.IsControlEnabled
            || IsNpcBonusSliderLocked(_selectedParameter))
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
        if (!target.Parameter.IsControlEnabled || IsNpcBonusSliderLocked(target.Parameter)
            || target.Kind == HitKind.Toggle)
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
                "EquipmentWearBalance" => "equipment.armorDurability",
                "FlycatcherSlowdown" => "environment.flycatcherMovementSpeed",
                _ => "combat.npcHealth"
            });
            var d = P(Family switch
            {
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

    private ParameterViewModel? Linked(string id)
        => _linkedSources.TryGetValue(id, out var value) ? value : null;

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

    private double GetDesiredHeight(string family, double width)
        => family switch
        {
            "CouponFlow" => CouponBoardHeight(width),
            "ServiceCostsBoard" => ServicePriceBoardHeight(width),
            "TraderStockBoard" => 242.0,
            "ItemWeightBoard" => EconomyBoardHeight(width, 2),
            "AnomalyGlobalDamage" => 242.0,
            "FlycatcherSlowdown" => FlycatcherBoardHeight(width),
            "AnomalySpecialEffects" => 270.0,
            "NpcLootLoadout" => 564.0,
            "WorldLootLayout" => 275.0,
            "StashContentMix" => StashContentMixHeight(width),
            "StashMedicalComposition" => StashMedicalCompositionHeight(width),
            "StashGear" => StashGearHeight(width),
            "StaminaCore" => 323.0,
            "WaterStaminaPressure" => 171.0,
            "FatigueCurve" => CompactFatigueHeight(width),
            "EquipmentWearBalance" => 242.0,
            "HumanCombatProfile" => HumanCombatBoardHeight(width),
            "PlayerWeaponProfile" => 242.0,
            "CoreHealthDashboard" => width >= 700.0 ? 282.0 : 365.0,
            "SevereFatiguePenaltyBoard" => 376.0,
            "MasterMedicalHealing" => MasterMedicalHealingHeight,
            "MasterLowHealth" => MasterConditionalHeight(width),
            "MasterRadiationPenalty" => MasterConditionalHeight(width),
            "ConsumableMixer" => CompactConsumableHeight(width),
            "SleepingDashboard" => 370.0,
            "HungerLadder" => CompactHungerHeight(width),
            "StarvationTimeline" => MasterStarvationHeight(width),
            "RadiationBalance" => RadiationBoardHeight(width),
            "TradingDashboard" => 273.0,
            "RepairCurve" => 324.0,
            "AmmoDistribution" => 715.0,
            _ => 340.0
        };

    // Measured heights follow the painted responsive editors, so the
    // last mixer lane and every remaining gear/medical dial stay visible.
    private double StashContentMixHeight(double width)
        => width >= 700.0 ? 839.0 : 1185.0;
    private static double StashMedicalCompositionHeight(double width)
        => width >= 700.0 ? 345.0 : 607.0;
    // Paint width is ActualWidth - 30 DIP. At host widths 540–569 DIP
    // the FOUR-knob mixer still paints in two rows and needs 612 DIP.
    private static double StashGearHeight(double width)
        => width < 570.0 ? 670.0 : 502.0;

    // Renderer measurements and painted card extents must agree at every breakpoint.
    private static double CompactFatigueHeight(double width)
    {
        return SurvivalPrimaryPreviewTop + 80.0 + 51.0 + 365.0 + 12.0;
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
            "CouponFlow" => new[]
            {
                "economy.startingMoney",
                "economy.mainQuestReward",
                "economy.sideQuestReward"
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
                "environment.flycatcherTurnSpeed",
                // The two native source knobs now live beside the crosshair.
                // Include them for live binding/refresh and shared R reset.
                "environment.razorBleeding",
                "environment.psyAnomalyIntensity"
            },
            "AnomalySpecialEffects" => new[]
            {
                "environment.razorBleeding",
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
            "StashContentMix" => new[]
            {
                "loot.stashMixAmmo", "loot.stashMixMedical", "loot.stashMixFood",
                "loot.stashMixDrinks", "loot.stashMixGrenades", "loot.stashMixCoupons",
                "loot.cheapStashAmmoQuantity", "loot.mediumStashAmmoQuantity",
                "loot.expensiveStashAmmoQuantity",
                "loot.standardStashChanceScale", "loot.rareStashChanceScale",
                "loot.cheapStashGrenadeChance", "loot.cheapStashGrenadeQuantity",
                "loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance",
                "loot.mediumStashCouponsChance", "loot.expensiveStashAmmoChance",
                "loot.expensiveStashMedicalChance", "loot.expensiveStashFoodChance",
                "loot.expensiveStashDrinksChance", "loot.expensiveStashGrenadesChance",
                "loot.expensiveStashMoneyChance"
            },
            "StashMedicalComposition" => new[]
            {
                "loot.stashMedicalBandageBias", "loot.stashMedicalMedkitBias",
                "loot.stashMedicalArmyMedkitBias", "loot.stashMedicalAntiRadBias",
                "loot.stashMedicalBandageChanceScale", "loot.stashMedicalMedkitChanceScale",
                "loot.stashMedicalArmyMedkitChanceScale", "loot.stashMedicalAntiRadChanceScale",
                "loot.stashMedkitMinCount", "loot.stashMedkitMaxCount",
                "loot.stashArmyMedkitMinCount", "loot.stashArmyMedkitMaxCount"
            },
            "StashGear" => new[]
            {
                "loot.stashGearEnabled", "loot.stashGearCoveragePercent",
                "loot.stashGearWeaponWeight", "loot.stashGearArmorWeight",
                "loot.stashGearArtifactWeight", "loot.stashGearAttachmentWeight",
                "loot.stashGearBonusChance"
            },
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
                "survival.waterStaminaCostReduction",
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
