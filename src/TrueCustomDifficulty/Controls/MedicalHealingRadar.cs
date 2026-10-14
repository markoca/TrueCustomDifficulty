using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Four paired HEAL/BLEED source controls and four RAD-removal columns,
/// including Beer and Vodka, in one compact horizontal row.
/// Food Healing remains its own switch; all thirteen CFG source controls
/// share one medical card and the existing WPF visual anchor.
/// </summary>
public sealed class MedicalHealingRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double BoardHeight = 370.0;
    private const double PlotTop = 140.0;
    private const double PlotHeight = 151.0;
    // The paired columns share the frame, not their numeric units.
    private const double HealingRulerHp = 200.0;
    private const double BleedingRulerPts = 100.0;
    private static readonly Brush BleedingAccent =
        new SolidColorBrush(Color.FromRgb(179, 111, 51));
    private static readonly Brush RadiationAccent =
        new SolidColorBrush(Color.FromRgb(102, 174, 155));
    private const double RadiationRulerPts = 200.0;
    private const string FoodHealingId = "health.foodHealing";

    private static readonly (string Id, string Label)[] Items =
    {
        ("health.bandageHealing", "BANDAGE"),
        ("health.medkitHealing", "MEDKIT"),
        ("health.armyMedkitHealing", "ARMY MEDKIT"),
        ("health.ecoMedkitHealing", "SCIENTIFIC MEDKIT")
    };

    private static readonly string[] BleedingIds =
    {
        "health.bandageBleeding", "health.medkitBleeding",
        "health.armyMedkitBleeding", "health.ecoMedkitBleeding"
    };

    private static readonly (string Id, string Label)[] RadiationItems =
    {
        ("health.ecoMedkitAntirad", "SCIENTIFIC MEDKIT"),
        ("health.antiRadRadiation", "ANTI-RAD"),
        ("health.beerRadiationRemoval", "BEER"),
        ("health.vodkaRadiationRemoval", "VODKA")
    };

    private readonly ParameterViewModel?[] _medical = new ParameterViewModel?[4];
    private readonly ParameterViewModel?[] _bleeding = new ParameterViewModel?[4];
    private readonly ParameterViewModel?[] _radiation = new ParameterViewModel?[4];
    private ParameterViewModel? _foodHealing;
    private ParameterViewModel? _maxHealth;
    private ParameterViewModel? _bleedingMediumThreshold;
    private ParameterViewModel? _bleedingSevereThreshold;
    private readonly List<HitArea> _hits = new();
    private int _selected = 0;
    private int _dragRow = -1;
    private HitAction _dragAction = HitAction.Healing;
    private HitAction _selectedAction = HitAction.Healing;
    private HitAction _hoverAction = HitAction.Select;
    private int _hoverRow = -2;
    private Rect _foodCheckboxRect;
    private Rect _overrideCheckboxRect;
    private bool _foodCheckboxHovered;
    private bool _overrideCheckboxHovered;

    public static readonly DependencyProperty ParametersProperty =
        DependencyProperty.Register(nameof(Parameters), typeof(IEnumerable),
            typeof(MedicalHealingRadar),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public MedicalHealingRadar()
    {
        Focusable = true;
        MinHeight = BoardHeight;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
        => new(double.IsInfinity(availableSize.Width)
            ? 820.0 : Math.Max(820.0, availableSize.Width), BoardHeight);

    private static void OnParametersChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
        => ((MedicalHealingRadar)d).RebindParameters();

    private void RebindParameters()
    {
        foreach (var p in _medical.Concat(_bleeding).Concat(_radiation))
            if (p is not null) p.PropertyChanged -= ParameterChanged;
        if (_foodHealing is not null)
            _foodHealing.PropertyChanged -= ParameterChanged;
        foreach (var related in new[] { _maxHealth, _bleedingMediumThreshold, _bleedingSevereThreshold })
            if (related is not null) related.PropertyChanged -= ParameterChanged;

        Array.Clear(_medical, 0, _medical.Length);
        Array.Clear(_bleeding, 0, _bleeding.Length);
        Array.Clear(_radiation, 0, _radiation.Length);
        _foodHealing = null;
        _maxHealth = null;
        _bleedingMediumThreshold = null;
        _bleedingSevereThreshold = null;

        var byId = Parameters?.OfType<ParameterViewModel>()
            .ToDictionary(p => p.Id, StringComparer.Ordinal);
        if (byId is not null)
        {
            for (var i = 0; i < Items.Length; i++)
            {
                byId.TryGetValue(Items[i].Id, out _medical[i]);
                byId.TryGetValue(BleedingIds[i], out _bleeding[i]);
            }
            for (var i = 0; i < RadiationItems.Length; i++)
                byId.TryGetValue(RadiationItems[i].Id, out _radiation[i]);
            byId.TryGetValue(FoodHealingId, out _foodHealing);
            byId.TryGetValue("health.maxHP", out _maxHealth);
            byId.TryGetValue("survival.bleedingMediumThreshold", out _bleedingMediumThreshold);
            byId.TryGetValue("survival.bleedingStrongThreshold", out _bleedingSevereThreshold);
        }

        foreach (var p in _medical.Concat(_bleeding).Concat(_radiation))
            if (p is not null) p.PropertyChanged += ParameterChanged;
        if (_foodHealing is not null)
            _foodHealing.PropertyChanged += ParameterChanged;
        foreach (var related in new[] { _maxHealth, _bleedingMediumThreshold, _bleedingSevereThreshold })
            if (related is not null) related.PropertyChanged += ParameterChanged;
        InvalidateVisual();
    }

    private void ParameterChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ParameterViewModel.CurrentNumber)
            or nameof(ParameterViewModel.CurrentBoolean)
            or nameof(ParameterViewModel.ReferenceNumber)
            or nameof(ParameterViewModel.IsModified)
            or nameof(ParameterViewModel.Maximum)
            or nameof(ParameterViewModel.MedicalOverrideEnabled))
            InvalidateVisual();
    }


    // Four tightly grouped HEAL / BLEED vertical pairs on one row.
    // Each pair shares an item card but keeps independent physical scales
    // and distinct production CFG parameters.
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var text = FindBrush("TextBrush", Brushes.WhiteSmoke);
        var muted = FindBrush("RadarMutedTextBrush", Brushes.Gray);
        var border = FindBrush("BorderSoftBrush", Brushes.DimGray);
        var accent = FindBrush("AccentBrush", Brushes.Goldenrod);
        var panel = FindBrush("PanelAltBrush", Brushes.Black);

        IndustrialChrome.DrawFrame(dc,
            new Rect(2, 2, Math.Max(0, ActualWidth - 4), Math.Max(0, ActualHeight - 4)),
            panel, border, accent, border, 8, accentEdge: true, technicalMarks: true);
        _hits.Clear();

        DrawCenteredText(dc, "MEDICAL ITEMS", new Point(ActualWidth / 2, 21),
            HealthTypography.Title, accent, FontWeights.Bold);

        // Keep the existing independent Food Healing boolean, without a fifth
        // column or a duplicate medical section.
        var foodLabel = CreateText("FOOD HEALING", HealthTypography.Label,
            text, FontWeights.Bold);
        _foodCheckboxRect = new Rect(19.0, 48.0,
            BooleanCheckboxChrome.Size, BooleanCheckboxChrome.Size);
        dc.DrawText(foodLabel, new Point(_foodCheckboxRect.Right + 8.0, 49.0));
        // Use the canonical checkbox shape/hover behavior, but raise its
        // muted OFF-state edge contrast against this unusually dark chart.
        // No extra checkbox frame or redundant caption is introduced.
        var checkboxEdge = CloneWithOpacity(muted, .75);
        BooleanCheckboxChrome.Draw(dc, _foodCheckboxRect,
            _foodHealing?.CurrentBoolean == true,
            _foodHealing?.IsControlEnabled == true,
            _foodCheckboxHovered, panel, checkboxEdge, accent);
        _hits.Add(new HitArea(new Rect(_foodCheckboxRect.Left - 3.0,
            45.0, foodLabel.Width + BooleanCheckboxChrome.Size + 17.0, 26.0),
            -1, HitAction.Food));

        var overrideLabel = CreateText("OVERRIDE LIMITS", HealthTypography.Label,
            text, FontWeights.Bold);
        _overrideCheckboxRect = new Rect(ActualWidth - BooleanCheckboxChrome.Size - 19.0,
            48.0, BooleanCheckboxChrome.Size, BooleanCheckboxChrome.Size);
        dc.DrawText(overrideLabel, new Point(
            _overrideCheckboxRect.Left - overrideLabel.Width - 8.0, 49.0));
        BooleanCheckboxChrome.Draw(dc, _overrideCheckboxRect,
            _medical[0]?.MedicalOverrideEnabled == true,
            _medical[0]?.IsControlEnabled == true,
            _overrideCheckboxHovered, panel, checkboxEdge, accent);
        _hits.Add(new HitArea(new Rect(
            _overrideCheckboxRect.Left - overrideLabel.Width - 12.0, 45.0,
            overrideLabel.Width + BooleanCheckboxChrome.Size + 17.0, 26.0),
            -1, HitAction.Override));

        // Native treatment amounts stay editable; only these comparison
        // landmarks depend on Max HP and the current Bleeding stage thresholds.
        // They do NOT add health, alter severity or change item amounts.
        // Compress the four HEAL/BLEED clusters to reserve the right-hand
        // radiation bank without adding another row or a second card.
        var chart = new Rect(52.0, PlotTop,
            Math.Max(325.0, ActualWidth - 430.0), PlotHeight);
        var laneWidth = chart.Width / Items.Length;
        if (_bleedingMediumThreshold is not null && _bleedingSevereThreshold is not null)
            DrawCenteredText(dc,
                $"BLEED STAGES  |  MEDIUM {_bleedingMediumThreshold.CurrentNumber:0} PTS  |  SEVERE {_bleedingSevereThreshold.CurrentNumber:0} PTS",
                new Point(chart.Left + chart.Width / 2.0, 82.0),
                10.0, text, FontWeights.Bold);
        var radLeft = chart.Right + 55.0;
        var radArea = new Rect(radLeft, 88.0,
            Math.Max(258.0, ActualWidth - radLeft - 12.0), 265.0);
        // Vertical Y-axis captions belong to their corresponding physical
        // rulers, not to the per-item bars. Gold = HP / HEAL on the left;
        // copper = PTS / BLEED on the right. Tick numerals stay horizontal.
        var axisMidY = chart.Top + chart.Height / 2.0;
        var hpAxisCenter = new Point(chart.Left - 38.0, axisMidY);
        dc.PushTransform(new RotateTransform(-90.0, hpAxisCenter.X, hpAxisCenter.Y));
        DrawCenteredText(dc, "HP", hpAxisCenter, HealthTypography.Axis,
            accent, FontWeights.Bold);
        dc.Pop();

        var ptsAxisCenter = new Point(chart.Right + 44.0, axisMidY);
        dc.PushTransform(new RotateTransform(90.0, ptsAxisCenter.X, ptsAxisCenter.Y));
        DrawCenteredText(dc, "PTS", ptsAxisCenter, HealthTypography.Axis,
            BleedingAccent, FontWeights.Bold);
        dc.Pop();

        // Aligned grid lines are two labeled rulers, NOT a conversion:
        // left = 0..200 healing HP, right = 0..100 bleeding PTS.
        for (var step = 0; step <= 4; step++)
        {
            var y = chart.Bottom - PlotHeight * step / 4.0;
            dc.DrawLine(new Pen(CloneWithOpacity(border, .48), .8),
                new Point(chart.Left, y), new Point(chart.Right, y));
            DrawRightAlignedText(dc, $"{step * 50}",
                new Point(chart.Left - 8.0, y - 6.0),
                HealthTypography.Axis, muted, FontWeights.SemiBold);
            DrawText(dc, $"{step * 25}",
                new Point(chart.Right + 5.0, y - 6.0),
                HealthTypography.Axis, muted, FontWeights.SemiBold);
        }

        for (var row = 0; row < Items.Length; row++)
        {
            var left = chart.Left + row * laneWidth;
            var center = left + laneWidth / 2.0;
            dc.DrawRoundedRectangle(CloneWithOpacity(panel, .32),
                new Pen(CloneWithOpacity(border, .60), .8),
                new Rect(left + 3.0, 88.0, laneWidth - 6.0, 265.0), 4.0, 4.0);

            // HEAL owns the wider gold bar; BLEED is a narrower copper
            // sub-control immediately to its right, both on this same row.
            // Tighter HEAL/BLEED pair: preserve space between the item
            // clusters and readable independent numeric headers.
            var spread = Math.Min(23.0, laneWidth * .22);
            DrawColumn(dc, row, HitAction.Healing, _medical[row],
                center - spread, Math.Min(39.0, laneWidth * .31),
                HealingRulerHp, chart, text, muted, border, accent);
            DrawColumn(dc, row, HitAction.Bleeding, _bleeding[row],
                center + spread, Math.Min(27.0, laneWidth * .21),
                BleedingRulerPts, chart, text, muted, border, BleedingAccent);
            // MEDIUM/SEVERE stage landmarks on the BLEED bar ONLY. The
            // independently editable treatment removal is read on this same
            // native 0..100 PTS axis. These strokes have no hit regions.
            foreach (var threshold in new[] { _bleedingMediumThreshold, _bleedingSevereThreshold })
            {
                if (threshold is null) continue;
                var markerY = chart.Bottom - PlotHeight *
                    Math.Clamp(threshold.CurrentNumber / BleedingRulerPts, 0.0, 1.0);
                dc.DrawLine(new Pen(CloneWithOpacity(muted, .80), 1.3)
                    { DashStyle = DashStyles.Dash },
                    new Point(center + spread - 17.0, markerY),
                    new Point(center + spread + 17.0, markerY));
            }

            var name = Items[row].Label.Split(' ');
            if (name.Length == 1)
                DrawCenteredText(dc, name[0], new Point(center, 338.0),
                    HealthTypography.Label, text, FontWeights.Bold);
            else
            {
                DrawCenteredText(dc, name[0], new Point(center, 331.0),
                    HealthTypography.Label, text, FontWeights.Bold);
                DrawCenteredText(dc, string.Join(" ", name.Skip(1)),
                    new Point(center, 345.0), HealthTypography.Label,
                    text, FontWeights.Bold);
            }
            if (_medical[row] is { } item)
            {
                var currentPool = Math.Max(1.0, _maxHealth?.CurrentNumber ?? 100.0);
                DrawCenteredText(dc,
                    $"{100.0 * item.CurrentNumber / currentPool:0.#}% MAX HP",
                    new Point(center, 360.0), 8.4, muted, FontWeights.SemiBold);
            }
        }
        DrawRadiationGroup(dc, radArea, chart, text, muted, border, panel);
    }

    private void DrawRadiationGroup(
        DrawingContext dc, Rect area, Rect chart, Brush text,
        Brush muted, Brush border, Brush panel)
    {
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .32),
            new Pen(CloneWithOpacity(border, .65), .8),
            area, 4.0, 4.0);
        DrawCenteredText(dc, "RADIATION REMOVAL",
            new Point(area.Left + area.Width / 2.0, 103.0),
            HealthTypography.Label, RadiationAccent, FontWeights.Bold);

        var radScaleX = area.Right - 27.0;
        var radAxisCenter = new Point(area.Right - 8.0,
            chart.Top + chart.Height / 2.0);
        dc.PushTransform(new RotateTransform(90.0,
            radAxisCenter.X, radAxisCenter.Y));
        DrawCenteredText(dc, "RAD", radAxisCenter,
            HealthTypography.Axis, RadiationAccent, FontWeights.Bold);
        dc.Pop();
        for (var step = 0; step <= 4; step++)
        {
            var y = chart.Bottom - PlotHeight * step / 4.0;
            dc.DrawLine(new Pen(CloneWithOpacity(border, .33), .7),
                new Point(area.Left + 5.0, y),
                new Point(radScaleX - 21.0, y));
            DrawRightAlignedText(dc, $"{step * 50}",
                new Point(radScaleX, y - 6.0),
                HealthTypography.Axis, muted, FontWeights.SemiBold);
        }
        var usable = Math.Max(190.0, area.Width - 48.0);
        for (var i = 0; i < RadiationItems.Length; i++)
        {
            var centerX = area.Left + 10.0 + (i + .5) * usable / RadiationItems.Length;
            DrawColumn(dc, i, HitAction.Radiation, _radiation[i],
                centerX, Math.Min(27.0, usable / RadiationItems.Length * .45),
                RadiationRulerPts, chart, text, muted, border, RadiationAccent);
            var words = RadiationItems[i].Label.Split(' ');
            if (words.Length == 1)
                DrawCenteredText(dc, words[0], new Point(centerX, 338.0),
                    9.0, text, FontWeights.Bold);
            else
            {
                DrawCenteredText(dc, words[0], new Point(centerX, 331.0),
                    9.0, text, FontWeights.Bold);
                DrawCenteredText(dc, string.Join(" ", words.Skip(1)),
                    new Point(centerX, 345.0), 9.0, text, FontWeights.Bold);
            }
        }
    }

    private void DrawColumn(
        DrawingContext dc, int row, HitAction action, ParameterViewModel? parameter,
        double centerX, double barWidth, double rulerMaximum, Rect chart,
        Brush text, Brush muted, Brush border, Brush barAccent)
    {
        if (parameter is null)
            return;
        var current = Math.Clamp(parameter.CurrentNumber, 0.0, rulerMaximum);
        var currentY = chart.Bottom - PlotHeight * current / rulerMaximum;
        var referenceY = chart.Bottom - PlotHeight *
            Math.Clamp(parameter.ReferenceNumber / rulerMaximum, 0.0, 1.0);
        var nativeTop = chart.Bottom - PlotHeight *
            Math.Clamp(parameter.Maximum / rulerMaximum, 0.0, 1.0);
        var barLeft = centerX - barWidth / 2.0;
        var enabled = parameter.IsControlEnabled;
        var color = enabled ? barAccent : muted;

        dc.DrawRectangle(CloneWithOpacity(border, .12), null,
            new Rect(barLeft, nativeTop, barWidth,
                Math.Max(.5, chart.Bottom - nativeTop)));
        dc.DrawRectangle(CloneWithOpacity(color, enabled ? .72 : .22), null,
            new Rect(barLeft, currentY, barWidth,
                Math.Max(.5, chart.Bottom - currentY)));
        // Gray authored reference and maximum marks remain low contrast.
        // The current-value bar/top cap must dominate; the native ceiling
        // and baseline stay useful but visibly subdued reference landmarks.
        dc.DrawLine(new Pen(CloneWithOpacity(muted, .38), .8),
            new Point(barLeft, nativeTop),
            new Point(barLeft + barWidth, nativeTop));
        dc.DrawLine(new Pen(CloneWithOpacity(muted, .68), 1.35),
            new Point(barLeft - 5.0, referenceY),
            new Point(barLeft + barWidth + 5.0, referenceY));

        var hovered = enabled && _hoverRow == row && _hoverAction == action;
        VerticalBarHandleChrome.Draw(dc, new Point(centerX, currentY),
            barWidth, color, hovered);
        var numeric = action switch
        {
            HitAction.Healing => $"{parameter.CurrentNumber:0.#} HP",
            HitAction.Radiation => $"{parameter.CurrentNumber:0.#} RAD",
            _ => $"{parameter.CurrentNumber:0.#} PTS"
        };
        var label = CreateText(numeric, 10.0,
            enabled && parameter.IsValueModifiedFromReference()
                ? barAccent : text, FontWeights.Bold);
        var labelY = action == HitAction.Radiation ? 114.0
            : chart.Width / Items.Length < 95.0 && action == HitAction.Bleeding
                ? 114.0 : 103.0;
        dc.DrawText(label, new Point(centerX - label.Width / 2.0, labelY));
        if (action != HitAction.Radiation)
            DrawCenteredText(dc, action == HitAction.Healing ? "HEAL" : "BLEED",
                new Point(centerX, 312.0), 9.3,
                action == HitAction.Healing ? barAccent : BleedingAccent,
                FontWeights.SemiBold);

        // Click/drag only the concrete source's native 0..Maximum height;
        // the paired visual axis must never expand its actual CFG range.
        var hit = new Rect(centerX - Math.Max(16.0, barWidth / 2.0 + 6.0),
            nativeTop, Math.Max(32.0, barWidth + 12.0),
            Math.Max(1.0, chart.Bottom - nativeTop));
        _hits.Add(new HitArea(hit, row, action));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var hit = FindHit(e.GetPosition(this));
        if (hit is null) return;
        Focus();
        if (hit.Row >= 0)
        {
            _selected = hit.Row;
            _selectedAction = hit.Action;
        }
        if (hit.Action is HitAction.Healing or HitAction.Bleeding or HitAction.Radiation)
        {
            _dragRow = hit.Row;
            _dragAction = hit.Action;
            CaptureMouse();
            UpdateSlider(hit.Row, hit.Action, e.GetPosition(this).Y);
        }
        else
            Apply(hit);
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);
        var hit = FindHit(e.GetPosition(this));
        if (hit is null) return;
        if (hit.Action == HitAction.Override)
        {
            if (_medical[0] is not null) _medical[0]!.MedicalOverrideEnabled = false;
        }
        else if (hit.Row == -1) _foodHealing?.ResetToReference();
        else
        {
            _selected = hit.Row;
            _selectedAction = hit.Action;
            ParameterAt(hit.Row, hit.Action)?.ResetToReference();
        }
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var point = e.GetPosition(this);
        // Keep the active source highlighted while dragging across adjacent
        // columns; otherwise the neighboring BLEED/HEAL bar steals the hover.
        var hit = _dragRow >= 0 && e.LeftButton == MouseButtonState.Pressed
            ? new HitArea(Rect.Empty, _dragRow, _dragAction)
            : FindHit(point);
        var row = hit?.Row ?? -2;
        var action = hit?.Action ?? HitAction.Select;
        var foodCheckHover = _foodHealing?.IsControlEnabled == true
            && _foodCheckboxRect.Contains(point);
        var overrideCheckHover = _medical[0]?.IsControlEnabled == true
            && _overrideCheckboxRect.Contains(point);
        if (_hoverRow != row || _hoverAction != action ||
            _foodCheckboxHovered != foodCheckHover ||
            _overrideCheckboxHovered != overrideCheckHover)
        {
            _hoverRow = row;
            _hoverAction = action;
            _foodCheckboxHovered = foodCheckHover;
            _overrideCheckboxHovered = overrideCheckHover;
            InvalidateVisual();
        }
        Cursor = hit is not null ? action switch
        {
            HitAction.Healing or HitAction.Bleeding or HitAction.Radiation => Cursors.SizeNS,
            HitAction.Food or HitAction.Override => Cursors.Hand,
            _ => Cursors.Arrow
        } : Cursors.Arrow;
        if (_dragRow >= 0 && e.LeftButton == MouseButtonState.Pressed)
        {
            UpdateSlider(_dragRow, _dragAction, e.GetPosition(this).Y);
            e.Handled = true;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragRow < 0) return;
        _dragRow = -1;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverRow = -2;
        _hoverAction = HitAction.Select;
        _foodCheckboxHovered = false;
        _overrideCheckboxHovered = false;
        Cursor = Cursors.Arrow;
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.R)
        {
            foreach (var medicalItem in _medical.Concat(_bleeding).Concat(_radiation))
                medicalItem?.ResetToReference();
            _foodHealing?.ResetToReference();
            if (_medical[0] is not null) _medical[0]!.MedicalOverrideEnabled = false;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var p = ParameterAt(_selected, _selectedAction);
        if (p is null) return;
        var dir = e.Key switch
        {
            Key.Up or Key.Right or Key.Add or Key.OemPlus => 1,
            Key.Down or Key.Left or Key.Subtract or Key.OemMinus => -1,
            _ => 0
        };
        if (dir == 0) return;
        SetSnapped(p, p.CurrentNumber + dir * p.TickFrequency);
        InvalidateVisual();
        e.Handled = true;
    }


    private ParameterViewModel? ParameterAt(int row, HitAction action)
    {
        if (row < 0)
            return null;
        if (action == HitAction.Radiation)
            return row < _radiation.Length ? _radiation[row] : null;
        return row < Items.Length
            ? action == HitAction.Bleeding ? _bleeding[row] : _medical[row]
            : null;
    }

    private void Apply(HitArea hit)
    {
        if (hit.Action == HitAction.Food
            && _foodHealing is { IsControlEnabled: true } food)
            food.CurrentBoolean = !food.CurrentBoolean;
        else if (hit.Action == HitAction.Override
            && _medical[0] is { IsControlEnabled: true } medical)
            medical.MedicalOverrideEnabled = !medical.MedicalOverrideEnabled;
        InvalidateVisual();
    }

    private void UpdateSlider(int row, HitAction action, double y)
    {
        var parameter = ParameterAt(row, action);
        if (parameter is null || !parameter.IsControlEnabled)
            return;
        var hit = _hits.FirstOrDefault(area =>
            area.Row == row && area.Action == action);
        if (hit is null)
            return;

        var ratio = Math.Clamp(1.0 -
            (y - hit.Bounds.Top) / Math.Max(1.0, hit.Bounds.Height), 0, 1);
        SetSnapped(parameter, parameter.Minimum +
            ratio * (parameter.Maximum - parameter.Minimum));
        InvalidateVisual();
    }

    private HitArea? FindHit(Point point)
    {
        // Button hits are appended after their card selector and take priority.
        for (var i = _hits.Count - 1; i >= 0; i--)
            if (_hits[i].Bounds.Contains(point)) return _hits[i];
        return null;
    }

    private static void SetSnapped(ParameterViewModel p, double value)
        => p.CurrentNumber = Snap(p, value);

    private static double Snap(ParameterViewModel p, double value)
    {
        var step = Math.Max(p.TickFrequency, Epsilon);
        var snapped = p.Minimum + Math.Round((value - p.Minimum) / step,
            MidpointRounding.AwayFromZero) * step;
        return Math.Clamp(snapped, p.Minimum, p.Maximum);
    }

    private static Brush FindBrush(string key, Brush fallback)
        => Application.Current?.TryFindResource(key) as Brush ?? fallback;

    private static Brush CloneWithOpacity(Brush brush, double opacity)
    {
        var clone = brush.Clone();
        clone.Opacity = Math.Clamp(opacity, 0, 1);
        return clone;
    }

    private void DrawText(DrawingContext dc, string value, Point point,
        double size, Brush brush, FontWeight weight)
        => dc.DrawText(CreateText(value, size, brush, weight), point);

    private void DrawCenteredText(DrawingContext dc, string value, Point center,
        double size, Brush brush, FontWeight weight)
    {
        var formatted = CreateText(value, size, brush, weight);
        dc.DrawText(formatted,
            new Point(center.X - formatted.Width / 2,
                center.Y - formatted.Height / 2));
    }

    private void DrawRightAlignedText(DrawingContext dc, string value,
        Point right, double size, Brush brush, FontWeight weight)
    {
        var formatted = CreateText(value, size, brush, weight);
        dc.DrawText(formatted, new Point(right.X - formatted.Width, right.Y));
    }

    private FormattedText CreateText(string value, double size,
        Brush brush, FontWeight weight)
        => new(value, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal,
                weight, FontStretches.Normal), Math.Max(9, size), brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private sealed record HitArea(Rect Bounds, int Row, HitAction Action);

    private enum HitAction { Select, Food, Override, Healing, Bleeding, Radiation }
}
