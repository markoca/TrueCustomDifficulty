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
    private readonly List<HitTarget> _hitTargets = new();
    private readonly VisualStatusMessage _status;

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
        if (Family is "AnomalyEffectsBoard" or "ServiceCostsBoard"
            or "TraderStockBoard" or "ItemWeightBoard" or "BreakableBoxYields")
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
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 760.0 : Math.Max(520.0, availableSize.Width);
        return new Size(width, GetDesiredHeight(Family, width));
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
            or nameof(ParameterViewModel.CurrentBoolean)
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
            case "ServiceCostsBoard":
            case "TraderStockBoard":
            case "ItemWeightBoard":
                DrawNumericMixerBoard(dc, text, muted, border, accent, panel);
                break;
            case "AnomalyEffectsBoard":
                DrawAnomalyEffectsBoard(dc, text, muted, border, accent, panel);
                break;
            case "NpcLootLoadout":
                DrawNpcLootLoadout(dc, text, muted, border, accent, panel);
                break;
            case "WorldLootLayout":
                DrawWorldLootLayout(dc, text, muted, border, accent, panel);
                break;
            case "StashRewardProfile":
                DrawStashRewardProfile(dc, text, muted, border, accent, panel);
                break;
            case "BreakableBoxYields":
                DrawNumericMixerBoard(dc, text, muted, border, accent, panel);
                break;
            case "MutantCombatPressure":
            case "StaminaCore":
            case "WaterStaminaPressure":
            case "RadiationTreatment":
            case "PhysicalLimits":
                DrawPairedSystemBoard(dc, text, muted, border, accent, panel);
                break;
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

        // Subtle, consistent feedback for interactive tracks and toggles across
        // every aggregate family. Previews have no HitTarget and stay read-only.
        var activeTarget = _dragTarget ?? _hoverTarget;
        if (activeTarget is null && IsKeyboardFocused && _selectedParameter is not null)
            activeTarget = _hitTargets.FirstOrDefault(target =>
                ReferenceEquals(target.Parameter, _selectedParameter)
                && (_selectedHitKind is null || target.Kind == _selectedHitKind));

        if (activeTarget is not null)
        {
            dc.PushClip(new RectangleGeometry(
                new Rect(5.0, 5.0, Math.Max(0.0, ActualWidth - 10.0),
                    Math.Max(0.0, ActualHeight - 10.0))));
            dc.DrawRoundedRectangle(
                null,
                new Pen(CloneWithOpacity(accent,
                    _dragTarget is null ? 0.55 : 0.9),
                    _dragTarget is null ? 1.1 : 1.6),
                activeTarget.HitRect, 4.0, 4.0);
            dc.Pop();
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
            "blast damage multiplier for the currently selected Base Preset",
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
        DrawRightAlignedText(dc,
            $"{damage.CurrentNumber:0.##}x  |  BASE {damage.ReferenceNumber:0.##}x",
            new Point(preview.Right - 12.0, 170.0),
            10.3, accent, FontWeights.SemiBold);

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
        DrawText(dc, "GOLD = CURRENT  |  GREY = BASE  |  NO BLAST-RADIUS CHANGE",
            new Point(18.0, 254.0), 9.0, text, FontWeights.SemiBold);
    }

    // Mixer channels are a visual projection of the existing six catalog IDs,
    // never independent values or a new aggregated gameplay multiplier.
    private static readonly (string Id, string Top, string Bottom)[] AnomalyMixerChannels =
    {
        ("environment.anomalyDamage", "ANOMALY", "DAMAGE"),
        ("environment.flycatcherMovementSpeed", "FLYCATCHER", "MOVE"),
        ("environment.flycatcherTurnSpeed", "FLYCATCHER", "TURN"),
        ("environment.razorBleeding", "RAZOR", "BLEED"),
        ("environment.diamondBleeding", "DIAMOND", "BLEED"),
        ("environment.psyAnomalyIntensity", "PSY", "DRAIN")
    };


    private static readonly (string Id, string Top, string Bottom)[] ServiceMixerChannels =
    {
        ("economy.fastTravelCost", "FAST", "TRAVEL"),
        ("economy.upgradeCost", "UPGRADE", "COST"),
        ("economy.repairCostBase", "BASE", "REPAIR")
    };

    private static readonly (string Id, string Top, string Bottom)[] TraderMixerChannels =
    {
        ("economy.traderAmmoStock", "TRADER", "AMMO"),
        ("economy.medicStock", "MEDIC", "STOCK")
    };

    private static readonly (string Id, string Top, string Bottom)[] WeightMixerChannels =
    {
        ("equipment.ammoWeight", "AMMO", "WEIGHT"),
        ("equipment.consumableWeight", "FOOD + MED", "WEIGHT")
    };

    private static readonly (string Id, string Top, string Bottom)[] BoxYieldMixerChannels =
    {
        ("loot.destructibleAmmoQuantity", "AMMO", "YIELD"),
        ("loot.destructibleGrenadeQuantity", "GRENADE", "YIELD")
    };

    private static (string Id, string Top, string Bottom)[] GetMixerChannels(string family)
        => family switch
        {
            "ServiceCostsBoard" => ServiceMixerChannels,
            "TraderStockBoard" => TraderMixerChannels,
            "ItemWeightBoard" => WeightMixerChannels,
            "BreakableBoxYields" => BoxYieldMixerChannels,
            _ => AnomalyMixerChannels
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
            "ServiceCostsBoard" => "SERVICE PRICES / MIXER",
            "TraderStockBoard" => "TRADER STOCK / MIXER",
            "BreakableBoxYields" => "BREAKABLE BOX YIELDS / MIXER",
            _ => "ITEM WEIGHT / MIXER"
        };
        var description = Family switch
        {
            "ServiceCostsBoard" => "travel, upgrade and repair prices use separate CFG multipliers",
            "TraderStockBoard" => "separate ammo and medical stock quantities",
            "BreakableBoxYields" => "contents per destroyed box, not box spawn frequency",
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
        for (var index = 0; index < count; index++)
        {
            var channel = channels[index];
            var row = index / columns;
            var column = index % columns;
            var rect = new Rect(
                MixerSidePadding + column * (width + MixerStripGap),
                MixerStripTop + row * (MixerStripHeight + MixerRowGap),
                width, MixerStripHeight);
            DrawMixerStrip(dc, P(channel.Id), channel.Top, channel.Bottom, rect,
                text, muted, border, accent, panel);
        }
        var rows = (count + columns - 1) / columns;
        var footerY = MixerStripTop + rows * MixerStripHeight
            + (rows - 1) * MixerRowGap + 8.0;
        dc.DrawText(CreateText(
            "GOLD = CURRENT  |  GREY = BASE PRESET  |  RIGHT-CLICK ONE / R ALL",
            9.5, muted, FontWeights.SemiBold,
            Math.Max(1.0, ActualWidth - 2.0 * MixerSidePadding)),
            new Point(MixerSidePadding, footerY));
    }

    private void DrawAnomalyEffectsBoard(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        DrawHeader(dc, "ANOMALY EFFECTS / MIXER",
            "six independent CFG channels  |  grey = Base Preset, gold = current",
            text, muted, accent);

        var count = AnomalyMixerChannels.Length;
        var inner = Math.Max(1.0, ActualWidth - 2.0 * MixerSidePadding);
        var columns = GetMixerColumns(ActualWidth, count);
        var width = Math.Max(128.0,
            (inner - (columns - 1) * MixerStripGap) / columns);

        for (var index = 0; index < count; index++)
        {
            var source = AnomalyMixerChannels[index];
            var row = index / columns;
            var column = index % columns;
            var rect = new Rect(
                MixerSidePadding + column * (width + MixerStripGap),
                MixerStripTop + row * (MixerStripHeight + MixerRowGap),
                width,
                MixerStripHeight);

            DrawMixerStrip(dc, P(source.Id), source.Top, source.Bottom,
                rect, text, muted, border, accent, panel);
        }

        var rowCount = (count + columns - 1) / columns;
        var footerY = MixerStripTop + rowCount * MixerStripHeight
            + (rowCount - 1) * MixerRowGap + 8.0;
        dc.DrawText(CreateText(
                "HIGHER SOURCE VALUE = HIGHER FADER  |  FLYCATCHER = SPEED REMAINING",
                9.5, muted, FontWeights.SemiBold,
                Math.Max(1.0, ActualWidth - 2.0 * MixerSidePadding)),
            new Point(MixerSidePadding, footerY));
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
        Brush panel)
    {
        var selected = IsSelected(parameter);
        var hover = parameter is not null
            && ReferenceEquals(_hoverTarget?.Parameter, parameter);
        var enabled = parameter?.IsControlEnabled == true;
        var foreground = enabled ? text : muted;
        var valueBrush = enabled ? accent : muted;

        dc.DrawRoundedRectangle(panel,
            new Pen(selected ? accent
                : hover ? CloneWithOpacity(accent, 0.7) : border,
                selected ? 1.8 : 1.0), strip, 4.0, 4.0);
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
            strip.Width - 14.0, 13.2, valueBrush, FontWeights.Bold);
        DrawMixerCentered(dc, "BASE " + MixerValue(parameter, parameter.ReferenceNumber),
            new Point(center, strip.Top + 59.0),
            strip.Width - 14.0, 10.1, muted, FontWeights.SemiBold);
        DrawMixerCentered(dc, MixerValue(parameter, parameter.Maximum),
            new Point(center, strip.Top + 78.0),
            strip.Width - 14.0, 9.5, muted, FontWeights.Normal);

        // Vertical source scale: top is real maximum, bottom real minimum.
        // The strip layout normalizes only screen position, never the stored CFG.
        var rail = new Rect(center - 3.0, strip.Top + 100.0, 6.0, 150.0);
        var currentY = ValueToY(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, rail);
        var baseY = ValueToY(parameter.ReferenceNumber,
            parameter.Minimum, parameter.Maximum, rail);

        dc.DrawLine(new Pen(border, 5.0),
            new Point(center, rail.Top), new Point(center, rail.Bottom));
        dc.DrawLine(new Pen(CloneWithOpacity(valueBrush, enabled ? 0.75 : 0.3), 5.0),
            new Point(center, rail.Bottom), new Point(center, currentY));

        // Base marker lives next to the rail to avoid stealing the live
        // handle's drag area. Its clickable rectangle is registered last.
        dc.DrawLine(new Pen(muted, 2.2),
            new Point(center + 13.0, baseY),
            new Point(center + 29.0, baseY));
        dc.DrawEllipse(panel,
            new Pen(valueBrush, selected ? 2.4 : 1.7),
            new Point(center, currentY), 9.0, 9.0);
        dc.DrawLine(new Pen(valueBrush, 2.2),
            new Point(center - 7.0, currentY),
            new Point(center + 7.0, currentY));

        DrawMixerCentered(dc, MixerValue(parameter, parameter.Minimum),
            new Point(center, strip.Top + 260.0),
            strip.Width - 14.0, 9.5, muted, FontWeights.Normal);

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
    private void DrawNpcLootLoadout(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var corpse = P("loot.npcCorpseBonus");
        var medical = P("loot.npcMedicalChance");
        var ammo = P("loot.npcWeaponAmmoSupply");
        var condition = P("loot.droppedWeaponCondition");

        DrawHeader(dc, "NPC LOOT & EQUIPMENT",
            "extra corpse package, normal drops and weapon condition stay separate",
            text, muted, accent);

        if (corpse is null || medical is null || ammo is null || condition is null
            || corpse.ChoiceOptions.Count == 0)
        {
            DrawCenteredText(dc, "NPC loot parameter data unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var width = Math.Max(1.0, ActualWidth - 32.0);

        // Exactly the original distinct smart-loot lists, in their catalog order.
        // There are no synthetic "low/medium/high" steps between options.
        var choices = new Rect(16.0, 55.0, width, 152.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), choices, 4.0, 4.0);
        DrawText(dc, "EXTRA CORPSE LOOT PACKAGE",
            new Point(28.0, 64.0), 10.3, text, FontWeights.Bold);
        DrawText(dc, $"CURRENT: {corpse.CurrentDisplay}  |  BASE: " +
            corpse.ChoiceOptions[(int)Math.Clamp(corpse.ReferenceChoiceIndex, 0.0,
                corpse.ChoiceMaximum)].Label,
            new Point(28.0, 85.0), 10.4, accent, FontWeights.SemiBold);

        var optionCount = corpse.ChoiceOptions.Count;
        var optionGap = 9.0;
        var optionWidth = Math.Max(1.0, (width - 24.0 - optionGap * (optionCount - 1)) / optionCount);
        var firstCenter = choices.Left + 12.0 + optionWidth / 2.0;
        var lastCenter = firstCenter + (optionCount - 1) * (optionWidth + optionGap);
        for (var i = 0; i < optionCount; i++)
        {
            var x = choices.Left + 12.0 + i * (optionWidth + optionGap);
            var option = new Rect(x, 111.0, optionWidth, 48.0);
            var selected = Math.Abs(corpse.ChoiceIndex - i) < 0.5;
            var isBase = Math.Abs(corpse.ReferenceChoiceIndex - i) < 0.5;
            dc.DrawRoundedRectangle(
                selected ? CloneWithOpacity(accent, 0.17) : panel,
                new Pen(selected ? accent : isBase ? muted : border,
                    selected ? 1.8 : 1.0),
                option, 4.0, 4.0);
            var optionText = CreateText(corpse.ChoiceOptions[i].Label, 10.0,
                selected ? accent : text, FontWeights.SemiBold,
                Math.Max(1.0, option.Width - 14.0));
            dc.DrawText(optionText, new Point(
                option.Left + (option.Width - optionText.WidthIncludingTrailingWhitespace) / 2.0,
                option.Top + 16.0));

            var difficultyMarker = corpse.ChoiceMarkers.FirstOrDefault(marker =>
                Math.Abs(marker.Value - i) < Epsilon)?.Label ?? string.Empty;
            var caption = isBase ? (difficultyMarker.Length == 0
                ? "BASE PRESET" : difficultyMarker + "  |  BASE") : difficultyMarker;
            DrawCenteredText(dc, caption, new Point(option.Left + option.Width / 2.0, 168.0),
                9.0, muted, FontWeights.SemiBold);
        }

        AddChoiceTarget(corpse,
            new Rect(choices.Left + 12.0, 111.0, width - 24.0, 48.0),
            new Rect(firstCenter, 111.0, Math.Max(1.0, lastCenter - firstCenter), 48.0));

        DrawText(dc, "CORPSE BONUS DOES NOT REPLACE GENERATED NPC INVENTORY",
            new Point(28.0, 189.0), 9.0, muted, FontWeights.Normal);

        DrawHorizontalTrackCard(dc, medical, "NORMAL NPC MEDICAL DROP CHANCE",
            new Rect(16.0, 218.0, width, 72.0),
            text, muted, border, accent, panel);
        DrawHorizontalTrackCard(dc, ammo, "DROPPED WEAPON / ATTACHED AMMO",
            new Rect(16.0, 300.0, width, 72.0),
            text, muted, border, accent, panel);

        var preview = new Rect(16.0, 386.0, width, 91.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), preview, 4.0, 4.0);
        DrawText(dc, "NPC WEAPON CONDITION RANGE  /  ABSOLUTE %",
            new Point(28.0, 394.0), 10.1, text, FontWeights.Bold);
        DrawRightAlignedText(dc,
            $"{condition.CurrentMinNumber * 100.0:0}% - {condition.CurrentMaxNumber * 100.0:0}%",
            new Point(preview.Right - 12.0, 394.0), 10.8, accent, FontWeights.Bold);

        var band = new Rect(preview.Left + 19.0, 420.0,
            Math.Max(1.0, preview.Width - 38.0), 16.0);
        var bandY = band.Top + band.Height / 2.0;
        dc.DrawLine(new Pen(border, 7.0), new Point(band.Left, bandY),
            new Point(band.Right, bandY));
        // The common authored baseline is 25%-50%. One authored 35%-45%
        // outlier stays unchanged while the range is at default.
        var baseMinX = ValueToX(0.25, 0.0, 1.0, band, reverse: false);
        var baseMaxX = ValueToX(0.50, 0.0, 1.0, band, reverse: false);
        dc.DrawLine(new Pen(muted, 5.0) { DashStyle = DashStyles.Dash },
            new Point(baseMinX, bandY), new Point(baseMaxX, bandY));
        var currentMinX = ValueToX(condition.CurrentMinNumber, 0.0, 1.0, band, reverse: false);
        var currentMaxX = ValueToX(condition.CurrentMaxNumber, 0.0, 1.0, band, reverse: false);
        dc.DrawLine(new Pen(accent, 5.0),
            new Point(currentMinX, bandY), new Point(currentMaxX, bandY));
        dc.DrawEllipse(accent, null, new Point(currentMinX, bandY), 4.0, 4.0);
        dc.DrawEllipse(accent, null, new Point(currentMaxX, bandY), 4.0, 4.0);
        // Read-only comparison: no HitTarget on the overlapping interval handles.
        DrawText(dc, "0%", new Point(band.Left, 439.0), 9.0, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, "100%", new Point(band.Right, 439.0),
            9.0, muted, FontWeights.Normal);
        DrawText(dc,
            condition.IsValueModifiedFromReference()
                ? "CUSTOM RANGE APPLIES TO ALL 121 AUDITED NPC WEAPON ROWS"
                : "VANILLA INTACT / ONE WEAPON ROW RETAINS ITS 35-45% RANGE",
            new Point(28.0, 458.0), 9.0, muted, FontWeights.SemiBold);

        DrawNpcConditionTrack(dc, condition, true,
            new Rect(16.0, 489.0, width, 72.0),
            text, muted, border, accent, panel);
        DrawNpcConditionTrack(dc, condition, false,
            new Rect(16.0, 573.0, width, 72.0),
            text, muted, border, accent, panel);

        DrawText(dc,
            "MIN CANNOT EXCEED MAX  |  MOVED BOUNDARY HAS PRIORITY",
            new Point(18.0, 659.0), 9.1, text, FontWeights.SemiBold);
        DrawText(dc,
            "GREY = BASE  |  RIGHT-CLICK = RESET ONE SETTING  |  R = RESET ALL",
            new Point(18.0, 681.0), 9.0, muted, FontWeights.Normal);
    }

    // Two native 0-100%, 1%-step editor tracks for one range-valued parameter.
    // Property setters own the MIN <= MAX ordering and move the other bound
    // only when needed. Right-click either track resets the complete range.
    private void DrawNpcConditionTrack(
        DrawingContext dc,
        ParameterViewModel condition,
        bool minimumBound,
        Rect card,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), card, 4.0, 4.0);
        var label = minimumBound ? "MINIMUM DROPPED WEAPON CONDITION" :
            "MAXIMUM DROPPED WEAPON CONDITION";
        DrawText(dc, label, new Point(card.Left + 12.0, card.Top + 5.0),
            9.8, text, FontWeights.Bold);

        var current = minimumBound ? condition.CurrentMinNumber : condition.CurrentMaxNumber;
        var baseline = minimumBound ? 0.25 : 0.50;
        DrawText(dc, $"{current * 100.0:0}%  |  BASE {baseline * 100.0:0}%",
            new Point(card.Left + 12.0, card.Top + 22.0), 11.1,
            condition.IsControlEnabled ? accent : muted, FontWeights.SemiBold);

        var track = new Rect(card.Left + 14.0, card.Top + 39.0,
            Math.Max(1.0, card.Width - 28.0), 12.0);
        var y = track.Top + track.Height / 2.0;
        dc.DrawLine(new Pen(border, 3.5),
            new Point(track.Left, y), new Point(track.Right, y));
        var x = ValueToX(current, condition.Minimum, condition.Maximum, track, reverse: false);
        var referenceX = ValueToX(baseline, condition.Minimum, condition.Maximum, track, reverse: false);
        if (condition.IsControlEnabled)
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.65), 3.5),
                new Point(track.Left, y), new Point(x, y));
        dc.DrawLine(new Pen(muted, 2.0),
            new Point(referenceX, y - 9.0), new Point(referenceX, y + 9.0));

        var kind = minimumBound ? HitKind.RangeMin : HitKind.RangeMax;
        var selected = IsSelected(condition) && _selectedHitKind == kind;
        dc.DrawEllipse(panel, new Pen(condition.IsControlEnabled ? accent : muted,
                selected ? 2.5 : 1.5),
            new Point(x, y), 6.0, 6.0);
        if (condition.IsControlEnabled)
            dc.DrawEllipse(accent, null, new Point(x, y), 2.5, 2.5);

        AddRangeTarget(condition,
            new Rect(track.Left - 7.0, track.Top - 4.0,
                track.Width + 14.0, track.Height + 8.0),
            track, kind);
        DrawText(dc, "0%", new Point(track.Left, card.Top + 56.0),
            9.0, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, "100%", new Point(track.Right, card.Top + 56.0),
            9.0, muted, FontWeights.Normal);
    }

    // World-loot placement operations are four separate booleans. The source
    // density value belongs to the weapon-reshuffle operation only; it is not
    // a universal percentage of weapons removed from the base game world.
    private void DrawWorldLootLayout(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var weapons = P("loot.worldWeaponShuffle");
        var density = P("loot.worldWeaponDensity");
        var attachments = P("loot.worldAttachmentShuffle");
        var detectors = P("loot.worldDetectors");
        var blueprints = P("loot.blueprintShuffle");

        DrawHeader(dc, "WORLD LOOT LAYOUT",
            "four independent world changes and one weapon-density setting",
            text, muted, accent);

        if (weapons is null || density is null || attachments is null
            || detectors is null || blueprints is null)
        {
            DrawCenteredText(dc, "World loot layout data unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var fullWidth = Math.Max(1.0, ActualWidth - 32.0);
        var wide = ActualWidth >= 720.0;
        var tileHeight = wide ? 42.0 : 40.0;
        var gap = wide ? 10.0 : 9.0;
        var columns = wide ? 2 : 1;
        var tileWidth = wide ? (fullWidth - 10.0) / 2.0 : fullWidth;

        var toggles = new (ParameterViewModel Parameter, string Label)[]
        {
            (weapons, "SHUFFLE WORLD WEAPONS"),
            (attachments, "SHUFFLE WORLD ATTACHMENTS"),
            (detectors, "REMOVE WORLD DETECTORS"),
            (blueprints, "SHUFFLE BLUEPRINTS")
        };

        for (var index = 0; index < toggles.Length; index++)
        {
            var column = index % columns;
            var row = index / columns;
            var rect = new Rect(16.0 + column * (tileWidth + gap),
                56.0 + row * (tileHeight + gap), tileWidth, tileHeight);
            DrawTogglePill(dc, toggles[index].Parameter, toggles[index].Label,
                rect, text, muted, border, accent, panel);
        }

        var toggleRows = (toggles.Length + columns - 1) / columns;
        var densityTop = 56.0 + toggleRows * (tileHeight + gap) + 6.0;
        DrawHorizontalTrackCard(dc, density, "WORLD WEAPON DENSITY",
            new Rect(16.0, densityTop, fullWidth, 72.0),
            text, muted, border, accent, panel);

        var noteTop = densityTop + 86.0;
        var note = weapons.CurrentBoolean
            ? $"SHUFFLE ON  |  DENSITY {density.CurrentNumber * 100.0:0.#}%"
            : "SHUFFLE OFF  |  DENSITY VALUE SAVED BUT NOT APPLIED";
        DrawText(dc, note, new Point(18.0, noteTop),
            9.5, weapons.CurrentBoolean ? accent : text, FontWeights.Bold);
        DrawText(dc, "0% STILL GUARANTEES ONE OF EACH OF THE 36 REGULAR WEAPON MODELS",
            new Point(18.0, noteTop + 19.0), 9.0, muted, FontWeights.Normal);
        DrawText(dc, "RIGHT-CLICK = RESET ONE  |  R = RESET ALL",
            new Point(18.0, noteTop + 38.0), 9.0, muted, FontWeights.Normal);
    }

    // Chance, successful-roll amount and a different stash tier's coupon roll
    // remain three independent source parameters. The preview bars are deliberately
    // read-only and never combine them into an inferred expected-loot value.
    private void DrawStashRewardProfile(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var grenadeChance = P("loot.cheapStashGrenadeChance");
        var grenadeCount = P("loot.cheapStashGrenadeQuantity");
        var couponChance = P("loot.expensiveStashMoneyChance");

        DrawHeader(dc, "STASH REWARD ROLLS",
            "Basic stash grenade rolls and eligible Rare stash coupon rolls",
            text, muted, accent);

        if (grenadeChance is null || grenadeCount is null || couponChance is null)
        {
            DrawCenteredText(dc, "Stash reward data unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var width = Math.Max(1.0, ActualWidth - 32.0);
        var preview = new Rect(16.0, 55.0, width, 115.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), preview, 4.0, 4.0);

        void ProbabilityLane(ParameterViewModel parameter, string label, double top)
        {
            var textWidth = Math.Max(1.0, (preview.Width - 24.0) / 2.0);
            dc.DrawText(CreateText(label, 10.0, text, FontWeights.Bold, textWidth),
                new Point(preview.Left + 12.0, top));
            DrawRightAlignedText(dc,
                $"{parameter.CurrentNumber * 100.0:0.#}%  |  BASE {parameter.ReferenceNumber * 100.0:0.#}%",
                new Point(preview.Right - 12.0, top), 10.0, accent, FontWeights.SemiBold);

            var meter = new Rect(preview.Left + 12.0, top + 23.0,
                Math.Max(1.0, preview.Width - 24.0), 10.0);
            var y = meter.Top + meter.Height / 2.0;
            var currentX = ValueToX(parameter.CurrentNumber, 0.0, 1.0, meter, reverse: false);
            var referenceX = ValueToX(parameter.ReferenceNumber, 0.0, 1.0, meter, reverse: false);
            dc.DrawLine(new Pen(border, 7.0),
                new Point(meter.Left, y), new Point(meter.Right, y));
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.82), 7.0),
                new Point(meter.Left, y), new Point(currentX, y));
            dc.DrawLine(new Pen(muted, 2.3),
                new Point(referenceX, y - 8.0), new Point(referenceX, y + 8.0));
            // No HitTarget: editing belongs to the full native-range tracks below.
        }

        ProbabilityLane(grenadeChance, "BASIC STASH / RGD-5", 63.0);
        ProbabilityLane(couponChance, "RARE STASH / COUPONS", 115.0);

        DrawHorizontalTrackCard(dc, grenadeChance, "BASIC RGD-5 / DROP CHANCE",
            new Rect(16.0, 182.0, width, 72.0),
            text, muted, border, accent, panel);
        DrawHorizontalTrackCard(dc, grenadeCount, "BASIC RGD-5 / AMOUNT IF DROPPED",
            new Rect(16.0, 264.0, width, 72.0),
            text, muted, border, accent, panel);
        DrawHorizontalTrackCard(dc, couponChance, "RARE STASH / COUPON CHANCE",
            new Rect(16.0, 346.0, width, 72.0),
            text, muted, border, accent, panel);

        DrawText(dc,
            "CHANCE DOES NOT CHANGE STACK SIZE  |  AMOUNT DOES NOT CHANGE CHANCE",
            new Point(18.0, 432.0), 9.1, text, FontWeights.SemiBold);
        DrawText(dc,
            "GREY = BASE PRESET  |  RIGHT-CLICK = RESET ONE  |  R = RESET ALL",
            new Point(18.0, 451.0), 9.0, muted, FontWeights.Normal);
    }

    // Different physical breakable-box populations share a board, but neither
    // their CFG count multipliers nor ammo family selection weights are linked.
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
                FirstLabel: "DAMAGE PER HIT",
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
                var caption = marker.Label.Replace("VANILLA / ", "", StringComparison.Ordinal)
                    + $" {marker.Value:0} uu";
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

    // Fatigue stages affect when penalties begin, while their strengths remain
    // separate authored effects. The overview is read-only: thresholds and
    // different combat effects can coincide, so interactive graph points overlap.
    // Each CFG value instead has one full-width, native-range slider below.
    // Two-column editing on wide surfaces; single-column fallback when narrow.
    // Stage graph is read-only to avoid overlapping draggable graph handles.
    private void DrawFatigueCurve(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush soft,
        Brush accent,
        Brush panel)
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

        DrawHeader(dc, "FATIGUE PENALTIES",
            "sleepiness thresholds and the combat penalties they trigger",
            text, muted, accent);

        if (rate is null || relief is null || low is null || medium is null ||
            severe is null || lowAim is null || mediumAim is null ||
            severeAim is null || mediumReload is null || severeReload is null)
        {
            DrawCenteredText(dc, "Fatigue control data unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var width = Math.Max(460.0, ActualWidth - 34.0);
        var columns = CompactColumns(ActualWidth);
        var stageColumns = CompactStageColumns(ActualWidth);
        var combatColumns = stageColumns;

        void Card(ParameterViewModel parameter, string label, string value,
            string min, string max, double top, int columnsInGroup, int index)
            => DrawCompactTrack(dc, parameter, label, value, min, max,
                CompactSlot(top, width, columnsInGroup, index),
                text, muted, border, accent, panel);

        const double buildTop = 55.0;
        Card(rate, "FATIGUE BUILD-UP",
            $"{rate.CurrentNumber:0.000} pts/s  |  BASE {rate.ReferenceNumber:0.000}",
            "0 pts/s", "0.020 pts/s", buildTop, columns, 0);
        Card(relief, "ENERGY DRINK FATIGUE RELIEF",
            $"{relief.CurrentNumber:0.##}x  |  BASE {relief.ReferenceNumber:0.##}x",
            "0x", "2x", buildTop, columns, 1);

        var previewTop = CompactGridEnd(buildTop, 2, columns) + 12.0;
        var preview = new Rect(17.0, previewTop, width, 80.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), preview, 4.0, 4.0);
        DrawText(dc, "SLEEPINESS POINTS  /  STAGE PREVIEW",
            new Point(preview.Left + 12.0, previewTop + 7.0),
            10.2, text, FontWeights.Bold);

        var track = new Rect(preview.Left + 22.0, previewTop + 32.0,
            preview.Width - 44.0, 12.0);
        var y = track.Top + track.Height / 2.0;
        double At(double value) => ValueToX(value, 0.0, 100.0, track, false);

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
            var x1 = At(stops[i]);
            var x2 = At(stops[i + 1]);
            dc.DrawRectangle(CloneWithOpacity(i == 3 ? accent : soft,
                    i == 3 ? 0.7 : 0.20 + i * 0.13), null,
                new Rect(x1, y - 4.0, Math.Max(0.0, x2 - x1), 8.0));
        }

        foreach (var parameter in new[] { low, medium, severe })
        {
            var currentX = At(parameter.CurrentNumber);
            var baseX = At(parameter.ReferenceNumber);
            dc.DrawLine(new Pen(muted, 1.5),
                new Point(baseX, y - 10.0), new Point(baseX, y + 10.0));
            dc.DrawLine(new Pen(accent, 2.0),
                new Point(currentX, y - 11.0), new Point(currentX, y + 11.0));
        }

        var previewStages = new[] { low, medium, severe };
        var previewLabels = new[] { "LOW", "MEDIUM", "SEVERE" };
        var legendWidth = (preview.Width - 24.0) / 3.0;
        for (var i = 0; i < 3; i++)
        {
            var parameter = previewStages[i];
            var cx = preview.Left + 12.0 + (i + 0.5) * legendWidth;
            DrawCenteredText(dc,
                $"{previewLabels[i]} {parameter.CurrentNumber:0}  |  BASE {parameter.ReferenceNumber:0}",
                new Point(cx, previewTop + 55.0),
                9.5, i == 2 ? accent : text, FontWeights.SemiBold);
        }

        var stagesTop = preview.Bottom + 12.0;
        Card(low, "LOW FATIGUE BEGINS",
            $"{low.CurrentNumber:0} pts  |  BASE {low.ReferenceNumber:0}",
            "0 pts", "100 pts", stagesTop, stageColumns, 0);
        Card(medium, "MEDIUM FATIGUE BEGINS",
            $"{medium.CurrentNumber:0} pts  |  BASE {medium.ReferenceNumber:0}",
            "0 pts", "100 pts", stagesTop, stageColumns, 1);
        Card(severe, "SEVERE FATIGUE BEGINS",
            $"{severe.CurrentNumber:0} pts  |  BASE {severe.ReferenceNumber:0}",
            "0 pts", "100 pts", stagesTop, stageColumns, 2);

        var combatTitleY = CompactGridEnd(stagesTop, 3, stageColumns) + 13.0;
        DrawText(dc, "COMBAT PENALTIES WHEN TIRED",
            new Point(19.0, combatTitleY), 10.8, accent, FontWeights.Bold);
        DrawText(dc, "Aim-time slowdown; reload penalties start at Medium fatigue.",
            new Point(19.0, combatTitleY + 17.0), 9.3, muted, FontWeights.Normal);

        var combatTop = combatTitleY + 37.0;
        Card(lowAim, "LOW FATIGUE / AIM SLOWDOWN",
            $"+{lowAim.CurrentNumber * 100:0.##}%  |  BASE +{lowAim.ReferenceNumber * 100:0.##}%",
            "0%", "+30%", combatTop, combatColumns, 0);
        Card(mediumAim, "MEDIUM FATIGUE / AIM SLOWDOWN",
            $"+{mediumAim.CurrentNumber * 100:0.##}%  |  BASE +{mediumAim.ReferenceNumber * 100:0.##}%",
            "0%", "+50%", combatTop, combatColumns, 1);
        Card(severeAim, "SEVERE FATIGUE / AIM SLOWDOWN",
            $"+{severeAim.CurrentNumber * 100:0.##}%  |  BASE +{severeAim.ReferenceNumber * 100:0.##}%",
            "0%", "+80%", combatTop, combatColumns, 2);
        Card(mediumReload, "MEDIUM FATIGUE / RELOAD SLOWDOWN",
            $"+{mediumReload.CurrentNumber * 100:0.##}%  |  BASE +{mediumReload.ReferenceNumber * 100:0.##}%",
            "0%", "+30%", combatTop, combatColumns, 3);
        Card(severeReload, "SEVERE FATIGUE / RELOAD SLOWDOWN",
            $"+{severeReload.CurrentNumber * 100:0.##}%  |  BASE +{severeReload.ReferenceNumber * 100:0.##}%",
            "0%", "+37.5%", combatTop, combatColumns, 4);

        var end = CompactGridEnd(combatTop, 5, combatColumns);
        DrawText(dc, "NO LOW-FATIGUE RELOAD EFFECT EXISTS IN THE GAME CFG",
            new Point(20.0, end + 12.0), 9.1, muted, FontWeights.SemiBold);
        DrawText(dc, "GREY = BASE  |  RIGHT-CLICK = RESET ONE  |  R = RESET ALL",
            new Point(20.0, end + 30.0), 9.1, muted, FontWeights.Normal);
    }

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
        var lowHealth = P("health.lowHealthRegen");
        if (maxHP is null || delay is null || passive is null
            || effectScale is null || lowHealth is null)
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

        DrawHorizontalTrackCard(dc, lowHealth, "LOW-HEALTH RECOVERY RATE",
            new Rect(16.0, 349.0, width, 72.0),
            text, muted, border, accent, panel, pairedValueFormatting: true);

        DrawCenteredText(dc,
            "0 PASSIVE = VANILLA  |  LOW-HEALTH RECOVERY IS A SEPARATE EFFECT",
            new Point(ActualWidth / 2.0, 432.0),
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

        DrawDualParameterHeading(dc,
            health, "ENEMY HEALTH",
            damage, "GUN DAMAGE",
            51.0, text, accent);

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
            "BOTTOM CIRCLE: ENEMY HEALTH   |   LEFT SQUARE: GUN DAMAGE   |   POINT = PREVIEW",
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

        DrawDualParameterHeading(dc,
            wait, "PHANTOM CYCLE WAIT",
            count, "PHANTOM COUNT",
            51.0, text, accent);

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
            "DRAG BOTTOM CIRCLE: WAIT   |   LEFT SQUARE: COUNT   |   CROSSHAIR = PREVIEW",
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
        DrawDualParameterHeading(dc,
            weapon, "WEAPON WEAR / SHOT",
            armor, "ARMOR DURABILITY",
            49.0, text, accent);

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
        DrawCenteredText(dc, "BOTTOM CIRCLE: ARMOR TOUGHNESS   |   LEFT SQUARE: WEAPON WEAR   |   POINT = PREVIEW",
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

        // Constrain both legend halves: source values should never be hidden
        // under the long comparison caption at the minimum layout width.
        var headingWidth = Math.Max(1.0, overview.Width / 2.0 - 24.0);
        dc.DrawText(CreateText("REPUTATION REPAIR CURVE", 10.1,
                text, FontWeights.Bold, headingWidth),
            new Point(overview.Left + 12.0, 64.0));
        var legendText = CreateText("GOLD = NOW  |  DASHED = BASE", 9.0,
            muted, FontWeights.SemiBold, headingWidth);
        dc.DrawText(legendText, new Point(
            overview.Right - 12.0 - legendText.WidthIncludingTrailingWhitespace, 64.0));
        dc.DrawText(CreateText(
                "1x = UNMODIFIED REPAIR LAYER  |  ABOVE 1x = SURCHARGE",
                9.0, muted, FontWeights.Normal,
                Math.Max(1.0, overview.Width - 24.0)),
            new Point(overview.Left + 12.0, 81.0));

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
            // Title, actual source multiplier, and 1x comparison each get
            // their own line instead of competing across the card header.
            var cardTextWidth = Math.Max(1.0, card.Width - 24.0);
            dc.DrawText(CreateText(label, 10.2, text, FontWeights.Bold,
                    cardTextWidth),
                new Point(card.Left + 12.0, top + 4.0));
            dc.DrawText(CreateText(
                    $"{parameter.CurrentNumber:0.##}x  |  BASE {parameter.ReferenceNumber:0.##}x",
                    10.7, parameter.IsControlEnabled ? accent : muted,
                    FontWeights.Bold, cardTextWidth),
                new Point(card.Left + 12.0, top + 20.0));
            dc.DrawText(CreateText(PriceHint(parameter.CurrentNumber), 9.0,
                    muted, FontWeights.SemiBold, cardTextWidth),
                new Point(card.Left + 12.0, top + 35.0));

            var track = new Rect(card.Left + 22.0, top + 49.0,
                card.Width - 44.0, 12.0);
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
                new Point(track.Left, top + 63.0),
                9.1, muted, FontWeights.Normal);
            DrawRightAlignedText(dc, $"{parameter.Maximum:0.##}x",
                new Point(track.Right, top + 63.0),
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

    // Each treatment has its own raw bleeding-removal value. Comparing against
    // its own vanilla reference is meaningful, but these are not interchangeable
    // multipliers and must remain independent CFG-backed horizontal editors.
    // The normalized four-item preview is deliberately read-only.
    private void DrawBleedingTreatmentMixer(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var bandage = P("health.bandageBleeding");
        var medkit = P("health.medkitBleeding");
        var army = P("health.armyMedkitBleeding");
        var scientific = P("health.ecoMedkitBleeding");

        DrawText(dc, "BLEEDING TREATMENT", new Point(17.0, 13.0),
            11.5, accent, FontWeights.Bold);
        DrawText(dc, "compare removal strength, then adjust each item's real bleeding points",
            new Point(17.0, 32.0), 9.5, text, FontWeights.Normal);

        if (bandage is null || medkit is null || army is null || scientific is null)
        {
            DrawCenteredText(dc, "Bleeding treatment data unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var parameters = new[] { bandage, medkit, army, scientific };
        var labels = new[] { "BANDAGE", "MEDKIT", "ARMY", "SCIENTIFIC" };
        var width = Math.Max(460.0, ActualWidth - 34.0);

        var comparison = new Rect(17.0, 55.0, width, 137.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0),
            comparison, 4.0, 4.0);
        DrawText(dc, "TREATMENT STRENGTH  /  READ-ONLY COMPARISON",
            new Point(comparison.Left + 12.0, 64.0), 10.0, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "0–200% OF OWN VANILLA VALUE",
            new Point(comparison.Right - 12.0, 64.0),
            9.0, muted, FontWeights.SemiBold);
        DrawText(dc, "100% = that item's original effect, not the same number of bleeding points.",
            new Point(comparison.Left + 12.0, 84.0),
            9.1, muted, FontWeights.Normal);

        // Use an identical *relative* 0–200% ruler in each preview column.
        // All actual edits below use raw values, not these normalized bars.
        var colWidth = (comparison.Width - 24.0) / parameters.Length;
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var ratio = Math.Abs(parameter.ReferenceNumber) > Epsilon
                ? parameter.CurrentNumber / parameter.ReferenceNumber
                : 0.0;
            var colLeft = comparison.Left + 12.0 + colWidth * i;
            var centerX = colLeft + colWidth / 2.0;
            var mini = new Rect(colLeft + 10.0, 119.0,
                Math.Max(30.0, colWidth - 20.0), 16.0);
            var y = mini.Top + mini.Height / 2.0;
            var nowX = mini.Left + mini.Width * Math.Clamp(ratio / 2.0, 0.0, 1.0);
            var baseX = mini.Left + mini.Width / 2.0;

            DrawCenteredText(dc, labels[i], new Point(centerX, 101.0),
                9.3, text, FontWeights.Bold);
            dc.DrawLine(new Pen(border, 4.0),
                new Point(mini.Left, y), new Point(mini.Right, y));
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.72), 4.0),
                new Point(mini.Left, y), new Point(nowX, y));
            dc.DrawLine(new Pen(muted, 2.0),
                new Point(baseX, y - 10.0), new Point(baseX, y + 10.0));
            dc.DrawEllipse(panel, new Pen(accent, 1.6),
                new Point(nowX, y), 5.0, 5.0);
            dc.DrawEllipse(accent, null, new Point(nowX, y), 2.0, 2.0);

            DrawCenteredText(dc, $"{ratio * 100.0:0}% VANILLA",
                new Point(centerX, 143.0), 10.2, accent, FontWeights.Bold);
            DrawCenteredText(dc, $"{parameter.CurrentNumber:0.##} BLEEDING PTS",
                new Point(centerX, 164.0), 9.1, muted, FontWeights.Normal);
        }

        DrawText(dc, "EDIT RAW BLEEDING REMOVAL",
            new Point(19.0, 206.0), 10.5, accent, FontWeights.Bold);
        DrawRightAlignedText(dc, "EACH ITEM HAS ITS OWN VANILLA STRENGTH",
            new Point(ActualWidth - 19.0, 206.0),
            9.0, muted, FontWeights.Normal);

        void DrawTreatmentTrack(ParameterViewModel parameter,
            string label, double top)
        {
            var card = new Rect(17.0, top, width, 82.0);
            dc.DrawRoundedRectangle(panel, new Pen(border, 1.0),
                card, 4.0, 4.0);

            var percent = Math.Abs(parameter.ReferenceNumber) > Epsilon
                ? parameter.CurrentNumber / parameter.ReferenceNumber * 100.0
                : 0.0;

            DrawText(dc, label, new Point(card.Left + 12.0, top + 7.0),
                10.5, text, FontWeights.Bold);
            DrawRightAlignedText(dc,
                $"{parameter.CurrentNumber:0.##} pts  |  BASE {parameter.ReferenceNumber:0.##} pts",
                new Point(card.Right - 12.0, top + 7.0),
                11.0, parameter.IsControlEnabled ? accent : muted, FontWeights.Bold);
            DrawText(dc, $"BLEEDING REMOVED ON USE  |  {percent:0}% OF VANILLA",
                new Point(card.Left + 12.0, top + 26.0),
                9.3, muted, FontWeights.Normal);

            var track = new Rect(card.Left + 22.0, top + 45.0,
                card.Width - 44.0, 17.0);
            var y = track.Top + track.Height / 2.0;
            var currentX = ValueToX(parameter.CurrentNumber,
                parameter.Minimum, parameter.Maximum, track, reverse: false);
            var vanillaX = ValueToX(parameter.ReferenceNumber,
                parameter.Minimum, parameter.Maximum, track, reverse: false);

            dc.DrawLine(new Pen(border, 4.0),
                new Point(track.Left, y), new Point(track.Right, y));
            if (parameter.IsControlEnabled)
                dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.72), 4.5),
                    new Point(track.Left, y), new Point(currentX, y));
            dc.DrawLine(new Pen(muted, 2.0),
                new Point(vanillaX, y - 10.0), new Point(vanillaX, y + 10.0));
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

            DrawText(dc, $"{parameter.Minimum:0.##} pts",
                new Point(track.Left, top + 63.0), 9.1, muted, FontWeights.Normal);
            DrawRightAlignedText(dc, $"{parameter.Maximum:0.##} pts",
                new Point(track.Right, top + 63.0), 9.1, muted, FontWeights.Normal);
        }

        DrawTreatmentTrack(bandage, "BANDAGE", 234.0);
        DrawTreatmentTrack(medkit, "MEDKIT", 327.0);
        DrawTreatmentTrack(army, "ARMY MEDKIT", 420.0);
        DrawTreatmentTrack(scientific, "SCIENTIFIC MEDKIT", 513.0);

        DrawText(dc, "BLEEDING REMOVAL, NOT HP HEALING  |  ORIGINAL 5-POINT STEPS",
            new Point(20.0, 614.0), 9.1, muted, FontWeights.SemiBold);
        DrawText(dc, "GREY TICK = BASE PRESET  |  RIGHT-CLICK = RESET ONE  |  R = RESET ALL",
            new Point(20.0, 632.0), 9.0, muted, FontWeights.Normal);
    }

    // The game authors separate immediate strength, sustained strength and timed
    // effects. The previews below are read-only; exactly nine original source
    // controls retain their independent 0-x native multiplier sliders.
    // Authored instant/sustained strengths and durations remain nine independent
    // parameters. On wide surfaces, their cards form two columns without any
    // shortened or overlapping native-range slider tracks.
    private void DrawConsumableMixer(
        DrawingContext dc,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var burst = P("survival.instantStaminaRestore");
        var waterStamina = P("survival.waterStaminaEffectStrength");
        var energyStamina = P("survival.energyDrinkStaminaRecoveryStrength");
        var antiRad = P("survival.alcoholRadiationRemoval");
        var waterTime = P("survival.waterEffectDuration");
        var energyTime = P("survival.energyDrinkEffectDuration");
        var vodkaTime = P("survival.vodkaEffectDuration");
        var beerTime = P("survival.beerEffectDuration");
        var magicTime = P("survival.magicVodkaEffectDuration");

        DrawHeader(dc, "CONSUMABLE EFFECT BOARD",
            "immediate strength, sustained bonuses and effect durations",
            text, muted, accent);

        if (burst is null || waterStamina is null || energyStamina is null ||
            antiRad is null || waterTime is null || energyTime is null ||
            vodkaTime is null || beerTime is null || magicTime is null)
        {
            DrawCenteredText(dc, "Consumable effect data unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var width = Math.Max(460.0, ActualWidth - 34.0);
        var columns = CompactColumns(ActualWidth);
        var durationColumns = CompactStageColumns(ActualWidth);
        const double summaryTop = 55.0;

        void SummaryCard(Rect card, string heading, string first, string second)
        {
            dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), card, 4.0, 4.0);
            var allowed = Math.Max(1.0, card.Width - 24.0);
            dc.DrawText(CreateText(heading, 10.2, text, FontWeights.Bold, allowed),
                new Point(card.Left + 12.0, card.Top + 8.0));
            dc.DrawText(CreateText(first, 10.1, accent, FontWeights.SemiBold, allowed),
                new Point(card.Left + 12.0, card.Top + 32.0));
            dc.DrawText(CreateText(second, 10.2, accent, FontWeights.Bold, allowed),
                new Point(card.Left + 12.0, card.Top + 55.0));
        }

        SummaryCard(CompactSlot(summaryTop, width, columns, 0),
            "INSTANT STAMINA REFILL",
            $"WATER {38.0 * burst.CurrentNumber:0.#}%  |  ENERGY {50.0 * burst.CurrentNumber:0.#}%",
            $"LIMITED ENERGY {100.0 * burst.CurrentNumber:0.#}%");
        SummaryCard(CompactSlot(summaryTop, width, columns, 1),
            "ALCOHOL RADIATION REMOVAL",
            $"BEER {10.0 * antiRad.CurrentNumber:0.#} rad points",
            $"VODKA {50.0 * antiRad.CurrentNumber:0.#} rad points");

        void Card(ParameterViewModel parameter, string label,
            string value, string min, string max, double top, int index,
            int columnsInGroup = 0)
            => DrawCompactTrack(dc, parameter, label, value, min, max,
                CompactSlot(top, width,
                    columnsInGroup == 0 ? columns : columnsInGroup, index),
                text, muted, border, accent, panel);

        var strengthTitle = CompactGridEnd(summaryTop, 2, columns) + 13.0;
        DrawText(dc, "EFFECT STRENGTH",
            new Point(19.0, strengthTitle), 10.8, accent, FontWeights.Bold);
        DrawText(dc, "Strength changes do not alter effect duration.",
            new Point(19.0, strengthTitle + 17.0), 9.3, muted, FontWeights.Normal);
        var strengthTop = strengthTitle + 37.0;

        Card(burst, "INSTANT STAMINA BURST",
            $"{burst.CurrentNumber:0.##}x  |  BASE {burst.ReferenceNumber:0.##}x",
            "0x", "2x", strengthTop, 0);
        Card(antiRad, "BEER / VODKA RADIATION REMOVAL",
            $"{antiRad.CurrentNumber:0.##}x  |  BASE {antiRad.ReferenceNumber:0.##}x",
            "0x", "2x", strengthTop, 1);
        Card(waterStamina, "WATER / SUSTAINED STAMINA",
            $"{waterStamina.CurrentNumber:0.##}x  |  BASE {waterStamina.ReferenceNumber:0.##}x",
            "0x", "2x", strengthTop, 2);
        Card(energyStamina, "ENERGY DRINK / SUSTAINED STAMINA",
            $"{energyStamina.CurrentNumber:0.##}x  |  BASE {energyStamina.ReferenceNumber:0.##}x",
            "0x", "2x", strengthTop, 3);

        var previewTitle = CompactGridEnd(strengthTop, 4, columns) + 13.0;
        DrawText(dc, "TIMED EFFECTS",
            new Point(19.0, previewTitle), 10.8, accent, FontWeights.Bold);
        DrawText(dc,
            $"WATER: REGEN +{25.0 * waterStamina.CurrentNumber:0.#}% / COST -{10.0 * waterStamina.CurrentNumber:0.#}%  |  ENERGY: REGEN +{200.0 * energyStamina.CurrentNumber:0.#}%",
            new Point(19.0, previewTitle + 17.0), 9.3, muted, FontWeights.Normal);

        // Longest effects share a real seconds scale. This chart is a preview,
        // not a fifth hit area or an alternate value-to-seconds editor.
        var previewTop = previewTitle + 37.0;
        var preview = new Rect(17.0, previewTop, width, 160.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0),
            preview, 4.0, 4.0);
        DrawText(dc, "LONGEST EFFECT PER DRINK  /  READ ONLY",
            new Point(preview.Left + 12.0, previewTop + 7.0),
            10.0, text, FontWeights.Bold);

        var timeline = new Rect(preview.Left + 117.0, previewTop + 32.0,
            Math.Max(170.0, preview.Width - 213.0), 116.0);
        const double timelineMax = 270.0;
        double SecondsToX(double seconds) => timeline.Left + timeline.Width *
            Math.Clamp(seconds / timelineMax, 0.0, 1.0);

        foreach (var seconds in new[] { 0.0, 90.0, 180.0, 270.0 })
        {
            var x = SecondsToX(seconds);
            dc.DrawLine(new Pen(border, 1.0) { DashStyle = DashStyles.Dot },
                new Point(x, timeline.Top - 3.0), new Point(x, timeline.Bottom));
            DrawCenteredText(dc, $"{seconds:0}s",
                new Point(x, previewTop + 16.0),
                9.0, muted, FontWeights.SemiBold);
        }

        var durationParameters = new[] { waterTime, energyTime, vodkaTime, beerTime, magicTime };
        var durationLabels = new[] { "WATER", "ENERGY", "VODKA", "BEER", "MAGIC VODKA" };
        var longestSeconds = new[] { 30.0, 45.0, 20.0, 20.0, 90.0 };
        for (var i = 0; i < durationParameters.Length; i++)
        {
            var parameter = durationParameters[i];
            var y = previewTop + 43.0 + i * 22.0;
            var currentSeconds = longestSeconds[i] * parameter.CurrentNumber;
            var referenceSeconds = longestSeconds[i] * parameter.ReferenceNumber;
            var currentX = SecondsToX(currentSeconds);
            var referenceX = SecondsToX(referenceSeconds);

            DrawRightAlignedText(dc, durationLabels[i],
                new Point(timeline.Left - 13.0, y - 7.0),
                9.2, text, FontWeights.Bold);
            dc.DrawLine(new Pen(border, 3.0),
                new Point(timeline.Left, y), new Point(timeline.Right, y));
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.75), 4.0),
                new Point(timeline.Left, y), new Point(currentX, y));
            dc.DrawLine(new Pen(muted, 1.8),
                new Point(referenceX, y - 8.0), new Point(referenceX, y + 8.0));
            dc.DrawEllipse(accent, null, new Point(currentX, y), 4.0, 4.0);
            DrawRightAlignedText(dc, $"{currentSeconds:0.#}s",
                new Point(preview.Right - 12.0, y - 7.0),
                9.2, accent, FontWeights.SemiBold);
        }

        var durationTop = preview.Bottom + 12.0;
        Card(waterTime, "WATER / EFFECT DURATION",
            $"{waterTime.CurrentNumber:0.##}x  |  BASE {waterTime.ReferenceNumber:0.##}x",
            "0x", "3x", durationTop, 0, durationColumns);
        Card(energyTime, "ENERGY DRINK / EFFECT DURATION",
            $"{energyTime.CurrentNumber:0.##}x  |  BASE {energyTime.ReferenceNumber:0.##}x",
            "0x", "3x", durationTop, 1, durationColumns);
        Card(vodkaTime, "VODKA / STAMINA PENALTY DURATION",
            $"{vodkaTime.CurrentNumber:0.##}x  |  BASE {vodkaTime.ReferenceNumber:0.##}x",
            "0x", "3x", durationTop, 2, durationColumns);
        Card(beerTime, "BEER / STAMINA PENALTY DURATION",
            $"{beerTime.CurrentNumber:0.##}x  |  BASE {beerTime.ReferenceNumber:0.##}x",
            "0x", "3x", durationTop, 3, durationColumns);
        Card(magicTime, "MAGIC VODKA / PSI DURATION",
            $"{magicTime.CurrentNumber:0.##}x  |  BASE {magicTime.ReferenceNumber:0.##}x",
            "0x", "3x", durationTop, 4, durationColumns);

        var end = CompactGridEnd(durationTop, 5, durationColumns);
        DrawText(dc,
            $"WATER: {5.0 * waterTime.CurrentNumber:0.#}/{30.0 * waterTime.CurrentNumber:0.#}s  |  ENERGY: {45.0 * energyTime.CurrentNumber:0.#}/{15.0 * energyTime.CurrentNumber:0.#}/{5.0 * energyTime.CurrentNumber:0.#}s",
            new Point(20.0, end + 12.0), 9.1, muted, FontWeights.Normal);
        DrawText(dc,
            $"VODKA: {20.0 * vodkaTime.CurrentNumber:0.#}s  |  BEER: {20.0 * beerTime.CurrentNumber:0.#}s  |  MAGIC: {90.0 * magicTime.CurrentNumber:0.#}s  |  DRUNKENNESS SEPARATE",
            new Point(20.0, end + 30.0), 9.1, muted, FontWeights.Normal);
    }

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

        var halfTextWidth = Math.Max(1.0, overview.Width / 2.0 - 24.0);
        void OverviewLabel(string heading, string meaning, double x)
        {
            dc.DrawText(CreateText(heading, 10.1, accent,
                    FontWeights.Bold, halfTextWidth), new Point(x, 64.0));
            dc.DrawText(CreateText(meaning, 9.3, text,
                    FontWeights.SemiBold, halfTextWidth), new Point(x, 85.0));
        }
        OverviewLabel("YOU SELL  →  TRADER BUYS",
            "YOU RECEIVE COUPONS", overview.Left + 12.0);
        OverviewLabel("TRADER SELLS  →  YOU BUY",
            "YOU SPEND COUPONS", midpoint + 12.0);
        DrawText(dc, "Difficulty pricing values feed trader-specific shop rules.",
            new Point(overview.Left + 12.0, 103.0), 9.0, muted, FontWeights.Normal);

        void DrawPricingTrack(ParameterViewModel parameter, string label, string detail, double top)
        {
            var card = new Rect(17.0, top, width, 91.0);
            dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), card, 4.0, 4.0);
            // Each title, value and gameplay hint has a dedicated bounded line.
            var cardTextWidth = Math.Max(1.0, card.Width - 24.0);
            dc.DrawText(CreateText(label, 10.4, text,
                    FontWeights.Bold, cardTextWidth),
                new Point(card.Left + 12.0, top + 6.0));
            dc.DrawText(CreateText(
                    $"{parameter.CurrentNumber:0.##}x  |  BASE {parameter.ReferenceNumber:0.##}x",
                    11.0, parameter.IsControlEnabled ? accent : muted,
                    FontWeights.Bold, cardTextWidth),
                new Point(card.Left + 12.0, top + 23.0));
            dc.DrawText(CreateText(detail, 9.4, muted,
                    FontWeights.Normal, cardTextWidth),
                new Point(card.Left + 12.0, top + 41.0));

            var track = new Rect(card.Left + 22.0, top + 57.0, card.Width - 44.0, 16.0);
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
                new Point(track.Left, top + 76.0), 9.1, muted, FontWeights.Normal);
            DrawRightAlignedText(dc, $"{parameter.Maximum:0.##}x",
                new Point(track.Right, top + 76.0), 9.1, muted, FontWeights.Normal);
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
        var gateTextWidth = Math.Max(1.0, gate.Width - 24.0);
        dc.DrawText(CreateText("MINIMUM CONDITION TRADERS ACCEPT", 10.4, text,
                FontWeights.Bold, gateTextWidth),
            new Point(gate.Left + 12.0, 339.0));
        dc.DrawText(CreateText(
                $"{minCondition.CurrentNumber * 100:0}%  |  BASE {minCondition.ReferenceNumber * 100:0}%",
                11.0, accent, FontWeights.Bold, gateTextWidth),
            new Point(gate.Left + 12.0, 356.0));
        dc.DrawText(CreateText("Weapons and armor below this durability are refused.", 9.4, muted,
                FontWeights.Normal, gateTextWidth),
            new Point(gate.Left + 12.0, 374.0));

        var gateTrack = new Rect(gate.Left + 22.0, 390.0, gate.Width - 44.0, 16.0);
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
        DrawText(dc, "0%  /  BROKEN", new Point(gateTrack.Left, 411.0),
            9.1, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, "100%  /  PRISTINE",
            new Point(gateTrack.Right, 411.0), 9.1, muted, FontWeights.Normal);

        // ItemCostMinPercent is a global durability-value layer, not a trader price.
        var floor = new Rect(17.0, 439.0, width, 95.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), floor, 4.0, 4.0);
        var floorTextWidth = Math.Max(1.0, floor.Width - 24.0);
        dc.DrawText(CreateText("ITEM VALUE AT 0% DURABILITY", 10.4, text,
                FontWeights.Bold, floorTextWidth),
            new Point(floor.Left + 12.0, 445.0));
        dc.DrawText(CreateText(
                $"{valueFloor.CurrentNumber * 100:0}%  |  BASE {valueFloor.ReferenceNumber * 100:0}%",
                11.0, accent, FontWeights.Bold, floorTextWidth),
            new Point(floor.Left + 12.0, 462.0));
        dc.DrawText(CreateText("Base value retained when broken; not the final sale price.", 9.4, muted,
                FontWeights.Normal, floorTextWidth),
            new Point(floor.Left + 12.0, 480.0));

        var floorTrack = new Rect(floor.Left + 22.0, 496.0, floor.Width - 44.0, 16.0);
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
            new Point(floorTrack.Left, 517.0), 9.1, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, $"{valueFloor.Maximum * 100:0}%",
            new Point(floorTrack.Right, 517.0), 9.1, muted, FontWeights.Normal);

        DrawText(dc, "GREY TICK = BASE PRESET  |  RIGHT-CLICK = RESET ONE  |  R = RESET ALL",
            new Point(20.0, 550.0), 9.1, muted, FontWeights.SemiBold);
        DrawText(dc, "PRICE SETTINGS ARE DIFFICULTY SCALERS, NOT EXACT SHOP PAYOUTS",
            new Point(20.0, 567.0), 9.1, muted, FontWeights.Normal);
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
            "hunger-point thresholds and their stamina recovery effects",
            text, muted, accent);

        if (rate is null || low is null || medium is null || severe is null ||
            wellFed is null || mediumPenalty is null || severePenalty is null)
        {
            DrawCenteredText(dc, "Hunger system data unavailable.",
                new Point(ActualWidth / 2.0, 140.0), 10.0, muted, FontWeights.Bold);
            return;
        }

        var width = Math.Max(460.0, ActualWidth - 34.0);
        var stageColumns = CompactStageColumns(ActualWidth);

        void Card(ParameterViewModel parameter, string label, string value,
            string minimum, string maximum, double top, int columns, int index)
            => DrawCompactTrack(dc, parameter, label, value, minimum, maximum,
                CompactSlot(top, width, columns, index),
                text, muted, border, accent, panel);

        const double buildTop = 55.0;
        Card(rate, "HUNGER BUILD-UP",
            $"{rate.CurrentNumber:0.000} pts/s  |  BASE {rate.ReferenceNumber:0.000}",
            "0 pts/s", "0.030 pts/s", buildTop, 1, 0);

        const double previewTop = buildTop + CompactTrackHeight + 12.0;
        var preview = new Rect(17.0, previewTop, width, 80.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), preview, 4.0, 4.0);
        DrawText(dc, "HUNGER POINTS  /  STAGE PREVIEW",
            new Point(preview.Left + 12.0, previewTop + 7.0),
            10.2, text, FontWeights.Bold);

        var axis = new Rect(preview.Left + 22.0, previewTop + 32.0,
            preview.Width - 44.0, 12.0);
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
            DrawCenteredText(dc,
                $"{previewLabels[i]} {parameter.CurrentNumber:0}  |  BASE {parameter.ReferenceNumber:0}",
                new Point(cx, previewTop + 55.0),
                9.5, i == 2 ? accent : text, FontWeights.SemiBold);
        }

        var stagesTop = preview.Bottom + 12.0;
        Card(low, "LOW HUNGER BEGINS",
            $"{low.CurrentNumber:0} pts  |  BASE {low.ReferenceNumber:0}",
            "0 pts", "100 pts", stagesTop, stageColumns, 0);
        Card(medium, "MEDIUM HUNGER BEGINS",
            $"{medium.CurrentNumber:0} pts  |  BASE {medium.ReferenceNumber:0}",
            "0 pts", "100 pts", stagesTop, stageColumns, 1);
        Card(severe, "SEVERE HUNGER BEGINS",
            $"{severe.CurrentNumber:0} pts  |  BASE {severe.ReferenceNumber:0}",
            "0 pts", "100 pts", stagesTop, stageColumns, 2);

        var recoveryTitleY = CompactGridEnd(stagesTop, 3, stageColumns) + 14.0;
        DrawText(dc, "STAMINA RECOVERY BY HUNGER STATE",
            new Point(19.0, recoveryTitleY), 10.8, accent, FontWeights.Bold);
        DrawText(dc, "Well-fed increases recovery; Medium and Severe apply losses.",
            new Point(19.0, recoveryTitleY + 17.0),
            9.3, muted, FontWeights.Normal);

        var recoveryTop = recoveryTitleY + 37.0;
        Card(wellFed, "WELL-FED RECOVERY BONUS",
            $"+{wellFed.CurrentNumber * 100:0}%  |  BASE +{wellFed.ReferenceNumber * 100:0}%",
            "0%", "+40%", recoveryTop, stageColumns, 0);
        Card(mediumPenalty, "MEDIUM HUNGER RECOVERY LOSS",
            $"-{mediumPenalty.CurrentNumber * 100:0}%  |  BASE -{mediumPenalty.ReferenceNumber * 100:0}%",
            "0%", "-66%", recoveryTop, stageColumns, 1);
        Card(severePenalty, "SEVERE HUNGER RECOVERY LOSS",
            $"-{severePenalty.CurrentNumber * 100:0}%  |  BASE -{severePenalty.ReferenceNumber * 100:0}%",
            "0%", "-100%", recoveryTop, stageColumns, 2);

        var end = CompactGridEnd(recoveryTop, 3, stageColumns);
        DrawText(dc, "MEDIUM / SEVERE KEEP THE ORIGINAL NO-SLEEP EFFECT",
            new Point(20.0, end + 12.0), 9.1, muted, FontWeights.SemiBold);
        DrawText(dc, "GREY = BASE  |  RIGHT-CLICK = RESET ONE  |  R = RESET ALL",
            new Point(20.0, end + 30.0), 9.1, muted, FontWeights.Normal);
    }

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
        // Current and Base Preset values get their own line. Independent
        // left/right headings formerly overlapped at the minimum width.
        var previewTextWidth = Math.Max(1.0, preview.Width - 24.0);
        dc.DrawText(CreateText("PROTECTION WINDOW  /  READ ONLY",
                10.0, text, FontWeights.Bold, previewTextWidth),
            new Point(preview.Left + 12.0, 63.0));
        dc.DrawText(CreateText(
                $"NOW {minimum.CurrentNumber:0.00}–{maximum.CurrentNumber:0.00}  |  BASE {minimum.ReferenceNumber:0.00}–{maximum.ReferenceNumber:0.00}",
                9.8, accent, FontWeights.SemiBold, previewTextWidth),
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
            // Keep each explanation inside its own third of the summary.
            var columnTextWidth = Math.Max(1.0, third - 24.0);
            dc.DrawText(CreateText(headings[i], 9.5,
                    i == 0 ? accent : text, FontWeights.Bold, columnTextWidth),
                new Point(x, 64.0));
            dc.DrawText(CreateText(notes[i], 9.0, muted,
                    FontWeights.Normal, columnTextWidth),
                new Point(x, 84.0));
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

            // Distinct title/value/meaning rows avoid overlap at minimum width.
            var cardTextWidth = Math.Max(1.0, card.Width - 24.0);
            dc.DrawText(CreateText(label, 10.3, text, FontWeights.Bold, cardTextWidth),
                new Point(card.Left + 12.0, top + 6.0));
            dc.DrawText(CreateText(currentAndBase, 10.8,
                    parameter.IsControlEnabled ? accent : muted,
                    FontWeights.Bold, cardTextWidth),
                new Point(card.Left + 12.0, top + 23.0));
            dc.DrawText(CreateText(description, 9.2, muted,
                    FontWeights.Normal, cardTextWidth),
                new Point(card.Left + 12.0, top + 40.0));

            var track = new Rect(card.Left + 21.0, top + 59.0,
                card.Width - 42.0, 14.0);
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

            DrawText(dc, minimumLabel, new Point(track.Left, top + 78.0),
                9.1, muted, FontWeights.Normal);
            DrawRightAlignedText(dc, maximumLabel, new Point(track.Right, top + 78.0),
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

            // Authored pool-entry IDs get a separate caption under the title.
            // Both strings are clipped to this pool instead of competing on a line.
            var poolTextWidth = Math.Max(1.0, card.Width - 24.0);
            dc.DrawText(CreateText(title, 10.4, text,
                    FontWeights.Bold, poolTextWidth),
                new Point(card.Left + 12.0, top + 5.0));
            dc.DrawText(CreateText(sourceNote, 9.1, muted,
                    FontWeights.SemiBold, poolTextWidth),
                new Point(card.Left + 12.0, top + 22.0));

            var stack = new Rect(card.Left + 14.0, top + 38.0,
                card.Width - 28.0, 14.0);
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
                // Column-local bounds prevent one ammo-family caption from
                // intruding into the neighboring family at narrow widths.
                var cellTextWidth = Math.Max(1.0, columnWidth - 6.0);
                void CellText(string value, double size, Brush brush,
                    FontWeight weight, double lineY)
                {
                    var content = CreateText(value, size, brush, weight, cellTextWidth);
                    dc.DrawText(content,
                        new Point(cx - content.WidthIncludingTrailingWhitespace / 2.0, lineY));
                }
                CellText(labels[i], 9.3, text, FontWeights.Bold, top + 55.0);
                CellText($"{shares[i] * 100.0:0}%", 10.8,
                    accent, FontWeights.Bold, top + 70.0);
                CellText($"BASE {baseShares[i] * 100.0:0}%", 9.0,
                    muted, FontWeights.Normal, top + 87.0);
            }
        }

        DrawPool("SNG POOL", "A918D + A919D  /  A545D  /  A012D",
            new[] { "9x18 / 9x19", "5.45", "12 GAUGE" },
            sngShares, sngBaseShares, new[] { 0, 2, 3 }, 55.0);

        DrawPool("NATO POOL", "A045D  /  A919D  /  A556D  /  A012D",
            new[] { ".45 ACP", "9x19", "5.56", "12 GAUGE" },
            natoShares, natoBaseShares, new[] { 1, 0, 2, 3 }, 170.0);

        var headingWidth = Math.Max(1.0, (ActualWidth - 54.0) / 2.0);
        dc.DrawText(CreateText("AMMO FAMILY WEIGHTS", 10.0, text,
                FontWeights.Bold, headingWidth), new Point(19.0, 286.0));
        var selectionHint = CreateText("1x = ORIGINAL  |  0x = DISABLED",
            9.0, muted, FontWeights.Normal, headingWidth);
        dc.DrawText(selectionHint,
            new Point(ActualWidth - 19.0 -
                selectionHint.WidthIncludingTrailingWhitespace, 286.0));

        void DrawFamily(ParameterViewModel parameter, string label, string poolShare, double top)
        {
            var card = new Rect(17.0, top, width, 83.0);
            dc.DrawRoundedRectangle(panel, new Pen(border, 1.0),
                card, 4.0, 4.0);
            var cardTextWidth = Math.Max(1.0, card.Width - 24.0);
            dc.DrawText(CreateText(label, 10.3, text,
                    FontWeights.Bold, cardTextWidth),
                new Point(card.Left + 12.0, top + 5.0));
            dc.DrawText(CreateText(
                    $"{parameter.CurrentNumber:0.##}x  |  BASE {parameter.ReferenceNumber:0.##}x",
                    10.8, parameter.IsControlEnabled ? accent : muted,
                    FontWeights.Bold, cardTextWidth),
                new Point(card.Left + 12.0, top + 21.0));
            dc.DrawText(CreateText(poolShare, 9.3, muted,
                    FontWeights.Normal, cardTextWidth),
                new Point(card.Left + 12.0, top + 37.0));

            var track = new Rect(card.Left + 22.0, top + 52.0,
                card.Width - 44.0, 14.0);
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
            DrawText(dc, $"{parameter.Minimum:0.##}x", new Point(track.Left, top + 68.0),
                9.0, muted, FontWeights.Normal);
            DrawRightAlignedText(dc, $"{parameter.Maximum:0.##}x",
                new Point(track.Right, top + 68.0), 9.0, muted, FontWeights.Normal);
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

    // Shared density for the large Survival/Health aggregate panels. Rows are
    // column-aware; no independent sliders are stacked inside the same hit area.
    private const double CompactTrackHeight = 82.0;
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

        // Every compact card gets its own text boundary. Clipping against the
        // whole control would allow a left-column caption to overlap its neighbor.
        dc.DrawText(CreateText(label, 10.2, text, FontWeights.Bold, contentWidth),
            new Point(card.Left + 12.0, card.Top + 7.0));
        dc.DrawText(CreateText(value, 10.7,
                parameter.IsControlEnabled ? accent : muted,
                FontWeights.Bold, contentWidth),
            new Point(card.Left + 12.0, card.Top + 25.0));

        var track = new Rect(card.Left + 20.0, card.Top + 45.0,
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
            dc.DrawText(CreateText(label, 9.2, text, FontWeights.Bold, cellWidth),
                new Point(x, top));
            dc.DrawText(CreateText(
                    $"{parameter.CurrentNumber:0.##}x  |  BASE {parameter.ReferenceNumber:0.##}x",
                    10.1, parameter.IsControlEnabled ? accent : text,
                    FontWeights.SemiBold, cellWidth),
                new Point(x, top + 14.0));
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
        bool pairedValueFormatting = false)
    {
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), card, 4.0, 4.0);
        var textWidth = Math.Max(1.0, card.Width - 24.0);
        dc.DrawText(CreateText(label, 9.8, text, FontWeights.Bold, textWidth),
            new Point(card.Left + 12.0, card.Top + 5.0));

        if (parameter is null)
        {
            dc.DrawText(CreateText("CFG PARAMETER UNAVAILABLE", 9.0, muted,
                    FontWeights.Normal, textWidth),
                new Point(card.Left + 12.0, card.Top + 22.0));
            return;
        }

        var value = pairedValueFormatting
            ? $"{ComparablePairedValue(parameter, parameter.CurrentNumber)}  |  BASE {ComparablePairedValue(parameter, parameter.ReferenceNumber)}"
            : $"{FriendlyValue(parameter)}  |  BASE {FormatCompact(parameter.ReferenceNumber)}";
        dc.DrawText(CreateText(value, pairedValueFormatting ? 11.1 : 10.1,
                parameter.IsControlEnabled ? accent : muted,
                FontWeights.SemiBold, textWidth),
            new Point(card.Left + 12.0, card.Top + 22.0));

        var track = new Rect(card.Left + 14.0, card.Top + 39.0,
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
                IsSelected(parameter) ? 2.5 : 1.5),
            new Point(x, y), 6.0, 6.0);
        if (parameter.IsControlEnabled)
            dc.DrawEllipse(accent, null, new Point(x, y), 2.5, 2.5);

        AddHorizontalTarget(parameter,
            new Rect(track.Left - 7.0, track.Top - 4.0,
                track.Width + 14.0, track.Height + 8.0),
            track, parameter.Minimum, parameter.Maximum);

        var halfWidth = Math.Max(1.0, track.Width / 2.0 - 4.0);
        dc.DrawText(CreateText(FormatCompact(parameter.Minimum), 9.0,
                muted, FontWeights.Normal, halfWidth),
            new Point(track.Left, card.Top + 56.0));
        var right = CreateText(FormatCompact(parameter.Maximum), 9.0,
            muted, FontWeights.Normal, halfWidth);
        dc.DrawText(right, new Point(
            track.Right - right.WidthIncludingTrailingWhitespace,
            card.Top + 56.0));
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
        dc.DrawRoundedRectangle(fill,
            new Pen(active ? accent : border, active ? 1.8 : 1.0),
            rect, 4.0, 4.0);

        // Reserve a fixed status lane before measuring the caption, so a
        // long bundle name cannot overwrite its ON/OFF value on narrow cards.
        var statusLane = 52.0;
        var labelWidth = Math.Max(1.0, rect.Width - statusLane - 20.0);
        dc.DrawText(CreateText(label, 9.7,
                active ? text : muted, FontWeights.Bold, labelWidth),
            new Point(rect.Left + 10.0, rect.Top + Math.Max(4.0, (rect.Height - 15.0) / 2.0)));

        var status = active ? "ON" : "OFF";
        var statusText = CreateText(status, 10.2,
            active ? accent : muted, FontWeights.Bold,
            statusLane - 12.0);
        dc.DrawText(statusText,
            new Point(rect.Right - 10.0 - statusText.WidthIncludingTrailingWhitespace,
                rect.Top + Math.Max(4.0, (rect.Height - 15.0) / 2.0)));

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
        DrawText(dc, title, new Point(16.0, 13.0), 11.5, accent, FontWeights.Bold);
        DrawText(dc, subtitle, new Point(16.0, 31.0), 9.5, muted, FontWeights.Normal);
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
        if (target is null)
            return;

        _selectedParameter = target.Parameter;
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
        var target = FindTarget(point);
        SetHoverTarget(target);
        Cursor = target?.Kind switch
        {
            HitKind.Horizontal or HitKind.RangeMin or HitKind.RangeMax => Cursors.SizeWE,
            HitKind.Vertical => Cursors.SizeNS,
            HitKind.Toggle or HitKind.Choice or HitKind.MixerBaseMarker or HitKind.MixerSelect => Cursors.Hand,
            _ => Cursors.Arrow
        };

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
                HitKind.Toggle => $"{target.Parameter.Label}\nClick to toggle · Right-click = Base Preset",
                HitKind.Choice => $"{target.Parameter.Label}\nChoose a corpse-loot package · Right-click = Base Preset",
                HitKind.MixerBaseMarker => $"{target.Parameter.Label}\nClick to snap to selected Base Preset",
                HitKind.MixerSelect => $"{target.Parameter.Label}\nClick for detailed rationale · Use the fader rail to edit",
                HitKind.RangeMin => "NPC weapon condition MIN\nDrag or use arrow keys · Right-click = reset full range",
                HitKind.RangeMax => "NPC weapon condition MAX\nDrag or use arrow keys · Right-click = reset full range",
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

        _selectedParameter = target.Parameter;
        _selectedHitKind = target.Kind == HitKind.MixerBaseMarker ? HitKind.Vertical : target.Kind;
        SetHoverTarget(target);
        RaiseParameterSelected(target.Parameter);
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

        var isChoice = _selectedParameter.IsChoiceSlider && _selectedHitKind == HitKind.Choice;
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
        var step = isChoice ? _selectedParameter.ChoiceTickFrequency : _selectedParameter.TickFrequency;

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

    private FormattedText CreateText(string value, double size, Brush brush, FontWeight weight,
        double maxWidth = double.PositiveInfinity)
    {
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

    private static double GetDesiredHeight(string family, double width)
        => family switch
        {
            "BlastDamageBoard" => 305.0,
            "ServiceCostsBoard" => GetMixerHeight(width, ServiceMixerChannels.Length),
            "TraderStockBoard" => GetMixerHeight(width, TraderMixerChannels.Length),
            "ItemWeightBoard" => GetMixerHeight(width, WeightMixerChannels.Length),
            "BreakableBoxYields" => GetMixerHeight(width, BoxYieldMixerChannels.Length),
            "AnomalyEffectsBoard" => GetMixerHeight(width, AnomalyMixerChannels.Length),
            "NpcLootLoadout" => 748.0, // reserve footer/status lane below the native input cards
            "WorldLootLayout" => width >= 720.0 ? 340.0 : 450.0,
            "StashRewardProfile" => 485.0,
            "BreakableBoxYields" => 413.0,
            "PhysicalLimits" => 335.0,
            "MutantCombatPressure" or "StaminaCore" or "WaterStaminaPressure"
                or "RadiationTreatment" => 270.0,
            "FatigueCurve" => CompactFatigueHeight(width),
            "BleedingTreatmentMixer" => 684.0,
            "EquipmentWearBalance" => 320.0,
            "PsyPhantomPressure" => 390.0,
            "HumanCombatProfile" => 390.0,
            "PlayerWeaponProfile" => 330.0,
            "CoreHealthDashboard" => 475.0,
            "SevereFatiguePenaltyBoard" => 390.0,
            "MasterMedicalHealing" => 390.0,
            "MasterLowHealth" => 235.0,
            "MasterRadiationPenalty" => 235.0,
            "ConsumableMixer" => CompactConsumableHeight(width),
            "SleepingDashboard" => 525.0,
            "HungerLadder" => CompactHungerHeight(width),
            "StarvationTimeline" => 532.0,
            "RepeatedHitBand" => 455.0,
            "RadiationBalance" => 490.0,
            "TradingDashboard" => 590.0,
            "RepairCurve" => 640.0,
            "AmmoDistribution" => 750.0,
            _ => 340.0
        };

    // These use the same section coordinates and CompactGridEnd layout as
    // their renderers. Measure and OnRender must agree at every breakpoint.
    private static double CompactFatigueHeight(double width)
    {
        var columns = CompactColumns(width);
        var stageColumns = CompactStageColumns(width);
        var previewTop = CompactGridEnd(55.0, 2, columns) + 12.0;
        var stagesTop = previewTop + 80.0 + 12.0;
        var combatTitle = CompactGridEnd(stagesTop, 3, stageColumns) + 13.0;
        var combatTop = combatTitle + 37.0;
        return CompactGridEnd(combatTop, 5, stageColumns) + 71.0;
    }

    private static double CompactConsumableHeight(double width)
    {
        var columns = CompactColumns(width);
        var strengthTitle = CompactGridEnd(55.0, 2, columns) + 13.0;
        var strengthTop = strengthTitle + 37.0;
        var previewTitle = CompactGridEnd(strengthTop, 4, columns) + 13.0;
        var previewTop = previewTitle + 37.0;
        var durationTop = previewTop + 160.0 + 12.0;
        return CompactGridEnd(durationTop, 5, CompactStageColumns(width)) + 71.0;
    }

    private static double CompactHungerHeight(double width)
    {
        var columns = CompactStageColumns(width);
        var stagesTop = 55.0 + CompactTrackHeight + 12.0 + 80.0 + 12.0;
        var recoveryTitle = CompactGridEnd(stagesTop, 3, columns) + 14.0;
        var recoveryTop = recoveryTitle + 37.0;
        return CompactGridEnd(recoveryTop, 3, columns) + 71.0;
    }

    private static IReadOnlyList<string> GetFamilyIds(string family)
        => family switch
        {
            "BlastDamageBoard" => new[]
            {
                "combat.explosionDamage"
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
            "AnomalyEffectsBoard" => new[]
            {
                "environment.anomalyDamage",
                "environment.flycatcherMovementSpeed",
                "environment.flycatcherTurnSpeed",
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
                "loot.worldWeaponShuffle",
                "loot.worldWeaponDensity",
                "loot.worldAttachmentShuffle",
                "loot.worldDetectors",
                "loot.blueprintShuffle"
            },
            "StashRewardProfile" => new[]
            {
                "loot.cheapStashGrenadeChance",
                "loot.cheapStashGrenadeQuantity",
                "loot.expensiveStashMoneyChance"
            },
            "BreakableBoxYields" => new[]
            {
                "loot.destructibleAmmoQuantity",
                "loot.destructibleGrenadeQuantity"
            },
            "MutantCombatPressure" => new[] { "combat.mutantDamage", "combat.mutantAttackCooldown" },
            "StaminaCore" => new[] { "stamina.maxSP", "stamina.regeneration" },
            "WaterStaminaPressure" => new[] { "stamina.waterMediumDrain", "stamina.waterStrongDrain" },
            "RadiationTreatment" => new[] { "health.ecoMedkitAntirad", "health.antiRadRadiation" },
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
        MixerBaseMarker,
        MixerSelect,
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
