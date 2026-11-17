using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// TRADER STOCK board (custom drawn, same grammar as the other boards):
/// AMMO / MEDICAL SUPPLY rails, trader-role chips, one "keep X% of the game's
/// odds" rail per stock category (0% = native SELL toggle off), the Generic
/// NPC GEAR rail (0% = off) and a CLEAR action for legacy restrictions.
/// The historical class name stays for XAML and audit compatibility.
/// </summary>
public sealed class TraderBuysMatrix : FrameworkElement
{
    private readonly CardHoverTracker _cardHover = new();

    protected override void OnRender(DrawingContext dc)
    {
        using (_cardHover.BeginRender())
            RenderCardContent(dc);
        _cardHover.Finish(Array.Empty<Rect>(), RenderSize);
        _cardHover.DrawHover(dc);
    }

    // ---- dependency properties ---------------------------------------------
    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters), typeof(IEnumerable), typeof(TraderBuysMatrix),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender,
            (o, _) => ((TraderBuysMatrix)o).Rebind()));

    public static readonly DependencyProperty TraderItemOverridesProperty = DependencyProperty.Register(
        nameof(TraderItemOverrides), typeof(string), typeof(TraderBuysMatrix),
        new FrameworkPropertyMetadata(string.Empty,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender
            | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty RareGearEnabledProperty = DependencyProperty.Register(
        nameof(RareGearEnabled), typeof(bool), typeof(TraderBuysMatrix),
        new FrameworkPropertyMetadata(false,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RareGearChanceProperty = DependencyProperty.Register(
        nameof(RareGearChance), typeof(double), typeof(TraderBuysMatrix),
        new FrameworkPropertyMetadata(RareGearDefault,
            FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty SellOddsProperty = DependencyProperty.Register(
        nameof(SellOdds), typeof(TraderSellOddsViewModel), typeof(TraderBuysMatrix),
        new PropertyMetadata(null, (o, e) =>
        {
            var matrix = (TraderBuysMatrix)o;
            if (e.OldValue is TraderSellOddsViewModel old) old.Changed -= matrix.OnOddsChanged;
            if (e.NewValue is TraderSellOddsViewModel now) now.Changed += matrix.OnOddsChanged;
            matrix.InvalidateVisual();
        }));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public string TraderItemOverrides
    {
        get => (string)GetValue(TraderItemOverridesProperty);
        set => SetValue(TraderItemOverridesProperty, value);
    }

    public bool RareGearEnabled
    {
        get => (bool)GetValue(RareGearEnabledProperty);
        set => SetValue(RareGearEnabledProperty, value);
    }

    public double RareGearChance
    {
        get => (double)GetValue(RareGearChanceProperty);
        set => SetValue(RareGearChanceProperty, value);
    }

    /// <summary>Partial "keep X% of game odds" per role x category.</summary>
    public TraderSellOddsViewModel? SellOdds
    {
        get => (TraderSellOddsViewModel?)GetValue(SellOddsProperty);
        set => SetValue(SellOddsProperty, value);
    }

    public event EventHandler<AggregateParameterSelectedEventArgs>? ParameterSelected;

    // ---- state ----------------------------------------------------------------
    public const string AmmoSupplyId = "economy.traderAmmoStock";
    public const string MedicalSupplyId = "economy.medicStock";
    private const double RareGearDefault = 0.02;
    private const double Epsilon = 0.0000001;
    private const double RowPitch = 32.0;
    private const double RowHeight = 28.0;
    private const int MaxRows = 10;

    private enum RailKind { Supply, Sell, Gear }

    private sealed record Rail(RailKind Kind, string Id, Rect Hit, Rect Track, Rect Card);

    private Dictionary<string, ParameterViewModel> _source = new(StringComparer.Ordinal);
    private readonly List<ParameterViewModel> _observed = new();
    private readonly List<Rail> _rails = new();
    private readonly List<(string RoleId, Rect Hit)> _roleChips = new();
    private Rect _clearHit = Rect.Empty;
    private string _roleId = "general";
    private string? _selectedRail;
    private string? _hoverRail;
    private string? _dragRail;

    public TraderBuysMatrix()
    {
        Focusable = true;
        SnapsToDevicePixels = true;
        Cursor = Cursors.Arrow;
    }

    private void OnOddsChanged(object? sender, EventArgs e) => InvalidateVisual();

    private void Rebind()
    {
        foreach (var p in _observed) p.PropertyChanged -= OnParameterChanged;
        _observed.Clear();
        _source = Parameters?.OfType<ParameterViewModel>()
            .ToDictionary(p => p.Id, StringComparer.Ordinal)
            ?? new Dictionary<string, ParameterViewModel>(StringComparer.Ordinal);
        foreach (var id in TraderSellsPolicy.ParameterIds
            .Concat(new[] { AmmoSupplyId, MedicalSupplyId })
            .Concat(LegacyBuyIds()))
        {
            if (!_source.TryGetValue(id, out var p)) continue;
            p.PropertyChanged += OnParameterChanged;
            _observed.Add(p);
        }
        InvalidateMeasure();
        InvalidateVisual();
    }

    private static IEnumerable<string> LegacyBuyIds()
        => TraderBuysPolicy.Roles.SelectMany(r =>
            TraderBuysPolicy.Categories.Select(c => TraderBuysPolicy.ParameterId(r, c)));

    private void OnParameterChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ParameterViewModel.CurrentBoolean)
            or nameof(ParameterViewModel.CurrentNumber)
            or nameof(ParameterViewModel.IsOwnValueModified))
        {
            InvalidateMeasure();
            InvalidateVisual();
        }
    }

    // ---- layout -----------------------------------------------------------------
    private const double SupplyTop = 50.0;
    private const double SupplyHeight = 58.0;

    private bool TwoSupplyColumns => ActualWidth >= 340.0;
    private double ChipsTop => SupplyTop + (TwoSupplyColumns ? SupplyHeight : 2 * SupplyHeight + 6.0) + 12.0;
    private double RowsTop => ChipsTop + ChipRows(ActualWidth) * 28.0 + 8.0;

    private static int ChipRows(double width) => width >= 430.0 ? 1 : 2;

    private static double DesiredHeightFor(double width, bool legacy)
    {
        var supply = width >= 340.0 ? SupplyHeight : 2 * SupplyHeight + 6.0;
        return SupplyTop + supply + 12.0 + ChipRows(width) * 28.0 + 8.0
            + MaxRows * RowPitch + (legacy ? 40.0 : 0.0) + 12.0;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 520.0 : Math.Max(300.0, availableSize.Width);
        return new Size(width, DesiredHeightFor(width, LegacyCount() > 0));
    }

    // ---- values ---------------------------------------------------------------------
    private double SellValue(string id)
        => _source.TryGetValue(id, out var p) && p.CurrentBoolean ? SellOdds?.Get(id) ?? 1.0 : 0.0;

    private double SellReference(string id)
        => _source.TryGetValue(id, out var p) && p.ReferenceBoolean ? 1.0 : 0.0;

    private double GearValue => RareGearEnabled ? RareGearChance : 0.0;

    private (int Items, int Buys) LegacyCounts()
    {
        var items = TraderOffersPolicy.Decode(TraderItemOverrides).Count;
        var buys = LegacyBuyIds().Count(id => _source.TryGetValue(id, out var p) && p.IsValueModifiedFromReference());
        return (items, buys);
    }

    private int LegacyCount()
    {
        var (items, buys) = LegacyCounts();
        return items + buys;
    }

    // ---- render ---------------------------------------------------------------------
    private void RenderCardContent(DrawingContext dc)
    {
        var text = FindBrush("TextBrush", Brushes.White);
        var muted = FindBrush("RadarMutedTextBrush", Brushes.Gray);
        var border = FindBrush("BorderSoftBrush", Brushes.DimGray);
        var accent = FindBrush("AccentBrush", Brushes.Goldenrod);
        var panel = FindBrush("PanelAltBrush", Brushes.Black);
        IndustrialChrome.DrawFrame(dc,
            new Rect(2, 2, Math.Max(0, ActualWidth - 4), Math.Max(0, ActualHeight - 4)),
            panel, border, accent, border, 8, accentEdge: true, technicalMarks: true);
        _rails.Clear();
        _roleChips.Clear();
        _clearHit = Rect.Empty;

        Center(dc, "TRADER STOCK", ActualWidth / 2.0, 21.0,
            VisualControlTypography.BoardTitle, accent, FontWeights.Bold);
        Center(dc, "100% = GAME ODDS · 0% = REMOVED", ActualWidth / 2.0, 38.0,
            9.0, muted, FontWeights.SemiBold);

        var left = 12.0;
        var width = Math.Max(1.0, ActualWidth - 24.0);

        // AMMO / MEDICAL SUPPLY: how much each offer carries (all traders).
        var supplyWidth = TwoSupplyColumns ? (width - 10.0) / 2.0 : width;
        DrawSupply(dc, AmmoSupplyId, "AMMO SUPPLY",
            new Rect(left, SupplyTop, supplyWidth, SupplyHeight - 6.0), text, muted, border, accent, panel);
        DrawSupply(dc, MedicalSupplyId, "MEDICAL SUPPLY",
            TwoSupplyColumns
                ? new Rect(left + supplyWidth + 10.0, SupplyTop, supplyWidth, SupplyHeight - 6.0)
                : new Rect(left, SupplyTop + SupplyHeight + 6.0, supplyWidth, SupplyHeight - 6.0),
            text, muted, border, accent, panel);

        // Trader role chips.
        var roles = TraderSellsPolicy.EditorRoles;
        var perRow = ChipRows(ActualWidth) == 1 ? roles.Count : 3;
        var chipWidth = (width - (perRow - 1) * 6.0) / perRow;
        for (var i = 0; i < roles.Count; i++)
        {
            var role = roles[i];
            var chip = new Rect(left + (i % perRow) * (chipWidth + 6.0),
                ChipsTop + (i / perRow) * 28.0, chipWidth, 22.0);
            var selected = role.Id == _roleId;
            var roleModified = TraderSellsPolicy.Categories
                .Where(c => TraderSellsPolicy.IsAvailable(role, c))
                .Any(c => Math.Abs(SellValue(TraderSellsPolicy.ParameterId(role, c))
                    - SellReference(TraderSellsPolicy.ParameterId(role, c))) > Epsilon)
                || (role.Id == "genericNpc" && RareGearEnabled);
            dc.DrawTrackedRoundedRectangle(Clone(panel, selected ? .9 : .45),
                new Pen(selected ? accent : Clone(border, .8), selected ? 1.4 : .9), chip, 3, 3);
            var label = Text(TraderSellsPolicy.DisplayName(role.Id).ToUpperInvariant(), 9.6,
                selected ? accent : text, FontWeights.Bold);
            dc.DrawText(label, new Point(chip.Left + (chip.Width - label.Width) / 2.0,
                chip.Top + (chip.Height - label.Height) / 2.0));
            if (roleModified)
                dc.DrawEllipse(accent, null, new Point(chip.Right - 6.0, chip.Top + 6.0), 2.2, 2.2);
            _roleChips.Add((role.Id, chip));
        }

        // Category rails of the selected role.
        var current = roles.Single(r => r.Id == _roleId);
        var y = RowsTop;
        foreach (var category in TraderSellsPolicy.Categories.Where(c => TraderSellsPolicy.IsAvailable(current, c)))
        {
            var id = TraderSellsPolicy.ParameterId(current, category);
            DrawPercentRow(dc, RailKind.Sell, id, category.Label.ToUpperInvariant(),
                SellValue(id), SellReference(id), _source.GetValueOrDefault(id),
                new Rect(left, y, width, RowHeight), text, muted, border, accent, panel);
            y += RowPitch;
        }
        if (_roleId == "genericNpc")
        {
            DrawPercentRow(dc, RailKind.Gear, "gear", "RARE GEAR", GearValue, 0.0,
                _source.GetValueOrDefault(TraderSellsPolicy.ParameterId(current,
                    TraderSellsPolicy.Categories.First(c => TraderSellsPolicy.IsAvailable(current, c)))),
                new Rect(left, y, width, RowHeight), text, muted, border, accent, panel);
        }

        // Legacy restrictions from older presets: visible and clearable.
        var (items, buys) = LegacyCounts();
        if (items + buys > 0)
        {
            var noticeTop = RowsTop + MaxRows * RowPitch + 4.0;
            var notice = new Rect(left, noticeTop, width, 32.0);
            dc.DrawTrackedRoundedRectangle(Clone(panel, .45), new Pen(Clone(accent, .55), .9), notice, 3, 3);
            var parts = new List<string>();
            if (items > 0) parts.Add($"{items} item exclusions");
            if (buys > 0) parts.Add($"{buys} BUY restrictions");
            var message = Text("OLDER PRESET: " + string.Join(" + ", parts) + " still active",
                9.4, text, FontWeights.SemiBold, Math.Max(1.0, width - 90.0));
            dc.DrawText(message, new Point(notice.Left + 10.0, notice.Top + (notice.Height - message.Height) / 2.0));
            _clearHit = new Rect(notice.Right - 70.0, notice.Top + 5.0, 62.0, 22.0);
            dc.DrawTrackedRoundedRectangle(Clone(panel, .9), new Pen(accent, 1.1), _clearHit, 3, 3);
            Center(dc, "CLEAR", _clearHit.Left + _clearHit.Width / 2.0,
                _clearHit.Top + _clearHit.Height / 2.0, 9.6, accent, FontWeights.Bold);
        }
    }

    private void DrawSupply(DrawingContext dc, string id, string label, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        if (!_source.TryGetValue(id, out var p)) return;
        dc.DrawTrackedRoundedRectangle(panel, new Pen(border, .9), card, 4, 4, p);
        DrawText(dc, label, new Point(card.Left + 10.0, card.Top + 7.0), 10.0, text, FontWeights.Bold);
        var modified = p.IsValueModifiedFromReference();
        var value = Text($"{p.CurrentNumber.ToString("0.##", CultureInfo.InvariantCulture)}x",
            modified ? 12.4 : 11.4, modified ? ReadoutInk.Numbers(accent, text) : text, FontWeights.Bold);
        dc.DrawText(value, new Point(card.Right - 10.0 - value.Width, card.Top + 6.0));
        var track = new Rect(card.Left + 12.0, card.Top + 32.0, Math.Max(1.0, card.Width - 24.0), 10.0);
        DrawTrack(dc, id, track, p.CurrentNumber, p.ReferenceNumber, p.Minimum, p.Maximum,
            p.IsControlEnabled, muted, border, accent, panel);
        _rails.Add(new Rail(RailKind.Supply, id,
            new Rect(track.Left - 6.0, track.Top - 8.0, track.Width + 12.0, track.Height + 16.0), track, card));
    }

    private void DrawPercentRow(DrawingContext dc, RailKind kind, string id, string label,
        double value, double reference, ParameterViewModel? owner, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        dc.DrawTrackedRoundedRectangle(Clone(panel, .55), new Pen(Clone(border, .85), .9), card, 3, 3, owner);
        var name = Text(label, 10.0, text, FontWeights.Bold, 104.0);
        dc.DrawText(name, new Point(card.Left + 10.0, card.Top + (card.Height - name.Height) / 2.0));
        var modified = Math.Abs(value - reference) > Epsilon;
        var readout = kind == RailKind.Gear && value <= Epsilon
            ? "OFF"
            : Math.Round(value * 100.0).ToString(CultureInfo.InvariantCulture) + "%";
        var valueText = Text(readout, modified ? 11.4 : 10.6, modified ? ReadoutInk.Numbers(accent, text) : text, FontWeights.Bold);
        dc.DrawText(valueText, new Point(card.Right - 10.0 - valueText.Width,
            card.Top + (card.Height - valueText.Height) / 2.0));
        var track = new Rect(card.Left + 120.0, card.Top + card.Height / 2.0 - 4.0,
            Math.Max(1.0, card.Width - 120.0 - 58.0), 8.0);
        DrawTrack(dc, id, track, value, reference, 0.0, 1.0, owner?.IsControlEnabled ?? true,
            muted, border, accent, panel);
        _rails.Add(new Rail(kind, id,
            new Rect(track.Left - 6.0, card.Top, track.Width + 12.0, card.Height), track, card));
    }

    private void DrawTrack(DrawingContext dc, string id, Rect track, double value, double reference,
        double minimum, double maximum, bool enabled, Brush muted, Brush border, Brush accent, Brush panel)
    {
        var y = track.Top + track.Height / 2.0;
        double X(double v) => track.Left + track.Width
            * Math.Clamp((v - minimum) / Math.Max(Epsilon, maximum - minimum), 0.0, 1.0);
        var x = X(value);
        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));
        dc.DrawLine(new Pen(Clone(enabled ? accent : muted, .7), 3.0), new Point(track.Left, y), new Point(x, y));
        var rx = X(reference);
        dc.DrawLine(new Pen(muted, 1.6), new Point(rx, y - 7.0), new Point(rx, y + 7.0));
        var hovered = _hoverRail == id || _dragRail == id;
        ControlHoverGlow.Draw(dc, new Point(x, y), accent, hovered && enabled);
        dc.DrawEllipse(panel, new Pen(enabled ? accent : muted, hovered || _selectedRail == id ? 2.5 : 1.5),
            new Point(x, y), 6.0, 6.0);
        dc.DrawEllipse(enabled ? accent : muted, null, new Point(x, y), 2.5, 2.5);
    }

    // ---- editing -----------------------------------------------------------------------
    private void SetRail(Rail rail, double raw)
    {
        switch (rail.Kind)
        {
            case RailKind.Supply when _source.TryGetValue(rail.Id, out var p) && p.IsControlEnabled:
            {
                var step = Math.Max(p.TickFrequency, Epsilon);
                var snapped = Math.Clamp(p.Minimum + Math.Round((raw - p.Minimum) / step,
                    MidpointRounding.AwayFromZero) * step, p.Minimum, p.Maximum);
                p.CurrentNumber = p.ReferenceMagnet(raw, snapped);
                Select(p);
                break;
            }
            case RailKind.Sell when _source.TryGetValue(rail.Id, out var sell) && sell.IsControlEnabled:
            {
                var value = TraderSellOddsViewModel.Snap(raw);
                SellOdds?.Set(rail.Id, value);
                sell.CurrentBoolean = value > Epsilon;
                Select(sell);
                break;
            }
            case RailKind.Gear:
            {
                var value = Math.Round(Math.Clamp(raw, 0.0, 1.0), 2);
                if (value < 0.01)
                    RareGearEnabled = false;
                else
                {
                    RareGearChance = value;
                    RareGearEnabled = true;
                }
                SelectGearOwner();
                break;
            }
        }
        InvalidateVisual();
    }

    private void ResetRail(Rail rail)
    {
        switch (rail.Kind)
        {
            case RailKind.Supply when _source.TryGetValue(rail.Id, out var p):
                p.ResetToReference();
                Select(p);
                break;
            case RailKind.Sell when _source.TryGetValue(rail.Id, out var sell):
                sell.ResetToReference();
                SellOdds?.Set(rail.Id, 1.0);
                Select(sell);
                break;
            case RailKind.Gear:
                RareGearEnabled = false;
                RareGearChance = RareGearDefault;
                SelectGearOwner();
                break;
        }
        InvalidateVisual();
    }

    private void ResetBoard()
    {
        foreach (var id in TraderSellsPolicy.EditableParameterIds)
            if (_source.TryGetValue(id, out var sell)) sell.ResetToReference();
        SellOdds?.Reset();
        RareGearEnabled = false;
        RareGearChance = RareGearDefault;
        foreach (var id in new[] { AmmoSupplyId, MedicalSupplyId })
            if (_source.TryGetValue(id, out var p)) p.ResetToReference();
        InvalidateVisual();
    }

    private void ClearLegacy()
    {
        TraderItemOverrides = string.Empty;
        foreach (var id in LegacyBuyIds())
            if (_source.TryGetValue(id, out var p)) p.ResetToReference();
        InvalidateVisual();
        InvalidateMeasure();
        InvalidateVisual();
    }

    private void Select(ParameterViewModel parameter)
        => ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(parameter));

    // GEAR is a preset sidecar without its own catalog id; the panel shows the
    // STALKERS category help, which explains RARE GEAR.
    private void SelectGearOwner()
    {
        var role = TraderSellsPolicy.Roles.Single(r => r.Id == "genericNpc");
        var first = TraderSellsPolicy.Categories.First(c => TraderSellsPolicy.IsAvailable(role, c));
        if (_source.TryGetValue(TraderSellsPolicy.ParameterId(role, first), out var owner))
            Select(owner);
    }

    private Rail? RailAt(Point point) => _rails.FirstOrDefault(r => r.Hit.Contains(point))
        ?? _rails.FirstOrDefault(r => r.Card.Contains(point));

    private double RawAt(Rail rail, double x)
    {
        var ratio = Math.Clamp((x - rail.Track.Left) / Math.Max(1.0, rail.Track.Width), 0.0, 1.0);
        if (rail.Kind == RailKind.Supply && _source.TryGetValue(rail.Id, out var p))
            return p.Minimum + ratio * (p.Maximum - p.Minimum);
        return ratio;
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();
        var point = e.GetPosition(this);
        if (!_clearHit.IsEmpty && _clearHit.Contains(point))
        {
            ClearLegacy();
            e.Handled = true;
            return;
        }
        foreach (var (roleId, hit) in _roleChips)
        {
            if (!hit.Contains(point)) continue;
            _roleId = roleId;
            var role = TraderSellsPolicy.Roles.Single(r => r.Id == roleId);
            var first = TraderSellsPolicy.Categories.First(c => TraderSellsPolicy.IsAvailable(role, c));
            if (_source.TryGetValue(TraderSellsPolicy.ParameterId(role, first), out var owner))
                Select(owner);
            InvalidateMeasure();
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        var rail = RailAt(point);
        if (rail is null) return;
        _selectedRail = rail.Id;
        if (!rail.Hit.Contains(point))
        {
            // A click on the card (not the track) only selects it.
            if (rail.Kind == RailKind.Gear) SelectGearOwner();
            else if (_source.TryGetValue(rail.Id, out var owner)) Select(owner);
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        _dragRail = rail.Id;
        CaptureMouse();
        SetRail(rail, RawAt(rail, point.X));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_cardHover.UpdateHover(e.GetPosition(this))) InvalidateVisual();
        base.OnMouseMove(e);
        var point = e.GetPosition(this);
        if (_dragRail is not null && e.LeftButton == MouseButtonState.Pressed)
        {
            var rail = _rails.FirstOrDefault(r => r.Id == _dragRail);
            if (rail is not null) SetRail(rail, RawAt(rail, point.X));
            e.Handled = true;
            return;
        }
        var hit = _rails.FirstOrDefault(r => r.Hit.Contains(point));
        var next = hit?.Id;
        if (next != _hoverRail)
        {
            _hoverRail = next;
            InvalidateVisual();
        }
        Cursor = hit is not null || _roleChips.Any(c => c.Hit.Contains(point))
            || (!_clearHit.IsEmpty && _clearHit.Contains(point)) ? Cursors.Hand : Cursors.Arrow;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragRail is null) return;
        _dragRail = null;
        ReleaseMouseCapture();
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        if (_cardHover.ClearHover()) InvalidateVisual();
        base.OnMouseLeave(e);
        _hoverRail = null;
        Cursor = Cursors.Arrow;
        InvalidateVisual();
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);
        var rail = RailAt(e.GetPosition(this));
        if (rail is null) return;
        _selectedRail = rail.Id;
        ResetRail(rail);
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.R)
        {
            ResetBoard();
            e.Handled = true;
            return;
        }
        var rail = _rails.FirstOrDefault(r => r.Id == _selectedRail);
        if (rail is null) return;
        var (value, minimum, maximum, step) = rail.Kind switch
        {
            RailKind.Supply when _source.TryGetValue(rail.Id, out var p)
                => (p.CurrentNumber, p.Minimum, p.Maximum, p.TickFrequency),
            RailKind.Sell => (SellValue(rail.Id), 0.0, 1.0, TraderSellOddsViewModel.Step),
            _ => (GearValue, 0.0, 1.0, 0.01)
        };
        double? next = e.Key switch
        {
            Key.Right or Key.Up or Key.Add or Key.OemPlus => value + step,
            Key.Left or Key.Down or Key.Subtract or Key.OemMinus => value - step,
            Key.Home => minimum,
            Key.End => maximum,
            _ => null
        };
        if (next is null) return;
        SetRail(rail, Math.Clamp(next.Value, minimum, maximum));
        e.Handled = true;
    }

    // ---- drawing helpers ------------------------------------------------------------------
    private static Brush FindBrush(string key, Brush fallback) =>
        Application.Current?.TryFindResource(key) as Brush ?? fallback;

    private static Brush Clone(Brush brush, double opacity)
    {
        var clone = brush.Clone();
        clone.Opacity = Math.Clamp(opacity, 0, 1);
        return clone;
    }

    private void Center(DrawingContext dc, string value, double x, double y,
        double size, Brush brush, FontWeight weight)
    {
        var formatted = Text(value, size, brush, weight);
        dc.DrawText(formatted, new Point(x - formatted.Width / 2, y - formatted.Height / 2));
    }

    private void DrawText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
        => dc.DrawText(Text(value, size, brush, weight), point);

    private FormattedText Text(string value, double size, Brush brush, FontWeight weight,
        double maxWidth = double.PositiveInfinity)
    {
        var (ink, numberInk) = ReadoutInk.Resolve(brush);
        var formatted = new FormattedText(value, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            size, ink, VisualTreeHelper.GetDpi(this).PixelsPerDip);
        ReadoutInk.Highlight(formatted, value, numberInk);
        if (double.IsFinite(maxWidth))
        {
            formatted.MaxTextWidth = Math.Max(1.0, maxWidth);
            formatted.MaxLineCount = 1;
            formatted.Trimming = TextTrimming.CharacterEllipsis;
        }
        return formatted;
    }
}
