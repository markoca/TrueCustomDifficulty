using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

// STASH LOOT MIX controls laid out exactly like ODDS PER STASH: one row per
// category, one column per tier. Each cell holds the single control that moves
// that odds cell (catalog parameter or presence sidecar); "—" where the game
// has no such loot in that tier. Every cell keeps X% of the game's odds except
// the two tagged CHANCE cells (Basic RGD-5, Rare coupons), which are the real
// per-stash chance. All cells move in 5% steps.
public sealed partial class AggregateVisualControl
{
    public static readonly DependencyProperty StashPresenceProperty =
        DependencyProperty.Register(nameof(StashPresence), typeof(StashPresenceViewModel),
            typeof(AggregateVisualControl),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender,
                OnStashPresenceChanged));

    public StashPresenceViewModel? StashPresence
    {
        get => (StashPresenceViewModel?)GetValue(StashPresenceProperty);
        set => SetValue(StashPresenceProperty, value);
    }

    private static void OnStashPresenceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (AggregateVisualControl)d;
        if (e.OldValue is StashPresenceViewModel old)
            old.Changed -= control.StashPresence_Changed;
        if (e.NewValue is StashPresenceViewModel now)
            now.Changed += control.StashPresence_Changed;
    }

    private void StashPresence_Changed(object? sender, EventArgs e) => InvalidateVisual();

    private const double StashControlsHeight = 258.0;
    private const double StashControlRowPitch = 34.0;

    // Row = ODDS category (AMMO, MEDS, FOOD, DRINKS, GRENADES, COUPONS);
    // column = BASIC, STANDARD, RARE. Sidecar ids start with "stashPresence.".
    private static readonly string?[,] StashControlGrid =
    {
        { StashPresenceViewModel.CheapAmmoId, StashPresenceViewModel.MediumAmmoId, "loot.expensiveStashAmmoChance" },
        { "loot.stashMixMedical", "loot.mediumStashMedicalChance", "loot.expensiveStashMedicalChance" },
        { "loot.stashMixFood", null, "loot.expensiveStashFoodChance" },
        { "loot.stashMixDrinks", "loot.mediumStashDrinksChance", "loot.expensiveStashDrinksChance" },
        { "loot.cheapStashGrenadeChance", null, "loot.expensiveStashGrenadesChance" },
        { "loot.stashMixCoupons", "loot.mediumStashCouponsChance", "loot.expensiveStashMoneyChance" }
    };

    // Two cells are a real per-stash chance, not a share of game odds.
    private static bool IsStashAbsoluteChanceId(string id)
        => id is "loot.cheapStashGrenadeChance" or "loot.expensiveStashMoneyChance";

    private static bool IsStashPresenceId(string id)
        => id.StartsWith("stashPresence.", StringComparison.Ordinal);

    private static string StashControlHint(string id) => id switch
    {
        _ when IsStashAbsoluteChanceId(id)
            => "CHANCE: the real chance per stash (not a share of game odds). The grey tick is the game's value.",
        "loot.stashMixMedical" or "loot.stashMixFood" or "loot.stashMixDrinks" or "loot.stashMixCoupons"
            => "How much of the game's odds you keep in Basic stashes and smart world stashes. Other items are not affected. 100% = game odds, 0% = never.",
        StashPresenceViewModel.CheapAmmoId
            => "How often a Basic stash's ammo pick gives anything (Basic stash ammo pick only: smart world ammo stashes are a separate game system and keep their own odds). 100% = game odds, 0% = never.",
        _ when IsStashPresenceId(id)
            => "How often a Standard stash's ammo pick gives anything (smart world ammo stashes keep their own odds). 100% = game odds, 0% = never.",
        _ => "How much of the game's own odds you keep. 100% = game odds, 0% = never."
    };

    private readonly List<(string Id, Rect Hit, Rect Rail)> _stashPresenceHits = new();
    private string? _stashPresenceDragging;
    private string? _stashPresenceHover;

    private void DrawStashControlGrid(DrawingContext dc, Rect outer,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        _stashPresenceHits.Clear();
        dc.DrawTrackedRoundedRectangle(CloneWithOpacity(panel, .65),
            new Pen(CloneWithOpacity(border, .61), .9), outer, 4.0, 4.0);
        dc.DrawText(CreateText("ADJUST ODDS", 11.4, text, FontWeights.Bold),
            new Point(outer.Left + 12.0, outer.Top + 8.0));
        DrawRightAlignedText(dc, "100% = GAME ODDS · CHANCE = REAL %",
            new Point(outer.Right - 12.0, outer.Top + 9.0),
            9.4, muted, FontWeights.SemiBold);

        // Same 3-column geometry as ODDS PER STASH, so cells line up.
        const double labelWidth = 92.0;
        var (gridLeft, columnWidth, columnGap) = StashOddsColumns(outer, labelWidth);
        var headerY = outer.Top + 30.0;
        for (var t = 0; t < 3; t++)
            DrawText(dc, StashOddsTierLabels[t],
                new Point(gridLeft + t * (columnWidth + columnGap), headerY),
                9.4, muted, FontWeights.Bold);

        for (var k = 0; k < StashOddsCategoryLabels.Length; k++)
        {
            var y = headerY + 16.0 + k * StashControlRowPitch;
            dc.DrawRectangle(StashMixPalette[k], null,
                new Rect(outer.Left + 12.0, y + 9.0, 3.0, 11.0));
            DrawText(dc, StashOddsCategoryLabels[k],
                new Point(outer.Left + 20.0, y + 6.0), 10.0, text, FontWeights.SemiBold);
            for (var t = 0; t < 3; t++)
            {
                var cell = new Rect(gridLeft + t * (columnWidth + columnGap), y,
                    columnWidth, StashControlRowPitch - 4.0);
                var id = StashControlGrid[k, t];
                if (id is null)
                {
                    DrawText(dc, "—", new Point(cell.Left + 6.0, cell.Top + 6.0),
                        10.0, CloneWithOpacity(muted, .7), FontWeights.SemiBold);
                    continue;
                }
                DrawStashControlCell(dc, id, k, cell, text, muted, border, accent, panel);
            }
        }
    }

    private void DrawStashControlCell(DrawingContext dc, string id, int kind, Rect cell,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        var sidecar = IsStashPresenceId(id);
        var parameter = sidecar ? null : P(id);
        if (!sidecar && parameter is null)
            return;
        var enabled = sidecar ? IsEnabled : parameter!.IsControlEnabled;
        var value = sidecar ? StashPresence?.Get(id) ?? 1.0 : parameter!.CurrentNumber;
        var minimum = sidecar ? 0.0 : parameter!.Minimum;
        var maximum = sidecar ? 1.0 : parameter!.Maximum;
        var reference = sidecar ? 1.0 : parameter!.ReferenceNumber;
        var modified = sidecar ? Math.Abs(value - 1.0) > 0.0000001
            : parameter!.IsValueModifiedFromReference();

        // Each cell is its own small card: white hover + click selects it.
        dc.DrawTrackedRoundedRectangle(CloneWithOpacity(panel, .45),
            new Pen(CloneWithOpacity(border, .45), .7), cell, 3.0, 3.0, parameter);
        _stashChanceTooltipAreas.Add((cell, StashControlHint(id)));

        const double numberWidth = 40.0;
        // Absolute chance cells carry a quiet CHANCE tag before the rail.
        var tagWidth = 0.0;
        if (!sidecar && IsStashAbsoluteChanceId(id))
        {
            var tag = CreateText("CHANCE", 8.4, muted, FontWeights.Bold);
            dc.DrawText(tag, new Point(cell.Left + 8.0, cell.Top + (cell.Height - tag.Height) / 2.0));
            tagWidth = tag.WidthIncludingTrailingWhitespace + 6.0;
        }
        var rail = new Rect(cell.Left + 9.0 + tagWidth, cell.Top + cell.Height / 2.0 - 1.5,
            Math.Max(1.0, cell.Width - numberWidth - 16.0 - tagWidth), 3.0);
        var tone = StashMixPalette[kind];
        double X(double v) => rail.Left + rail.Width
            * Math.Clamp((v - minimum) / Math.Max(Epsilon, maximum - minimum), 0.0, 1.0);
        var cy = rail.Top + rail.Height / 2.0;
        dc.DrawLine(new Pen(CloneWithOpacity(border, .9), 3.0),
            new Point(rail.Left, cy), new Point(rail.Right, cy));
        dc.DrawLine(new Pen(CloneWithOpacity(enabled ? tone : muted, enabled ? .94 : .34), 3.0),
            new Point(rail.Left, cy), new Point(X(value), cy));
        var tick = X(reference);
        dc.DrawLine(new Pen(CloneWithOpacity(muted, .7), 1.2),
            new Point(tick, cy - 5.0), new Point(tick, cy + 5.0));
        var hovered = enabled && (sidecar
            ? _stashPresenceHover == id
            : _hoverTarget?.Kind == HitKind.Horizontal
              && ReferenceEquals(_hoverTarget.Parameter, parameter));
        dc.DrawEllipse(panel, new Pen(hovered ? Brushes.White : enabled ? tone : muted,
                hovered ? 1.8 : 1.4), new Point(X(value), cy), 4.5, 4.5);

        var digits = (value * 100.0).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
        var number = CreateText(digits + "%", 10.6,
            enabled ? text : muted, FontWeights.Bold);
        if (enabled && modified)
            number.SetForegroundBrush(accent, 0, digits.Length);
        dc.DrawText(number, new Point(
            cell.Right - 7.0 - number.WidthIncludingTrailingWhitespace,
            cell.Top + (cell.Height - number.Height) / 2.0));

        var hit = new Rect(rail.Left - 6.0, cell.Top, rail.Width + 12.0, cell.Height);
        if (sidecar)
        {
            if (enabled)
                _stashPresenceHits.Add((id, hit, rail));
        }
        else
            AddHorizontalTarget(parameter!, hit, rail, minimum, maximum);
    }

    private void SetStashPresenceFromPoint(string id, Rect rail, Point point)
    {
        if (StashPresence is null) return;
        var ratio = Math.Clamp((point.X - rail.Left) / Math.Max(Epsilon, rail.Width), 0.0, 1.0);
        StashPresence.Set(id, ratio);
    }

    private bool StashPresenceMouseDown(Point point)
    {
        if (Family != "StashContentMix") return false;
        foreach (var (id, hit, rail) in _stashPresenceHits)
        {
            if (!hit.Contains(point)) continue;
            _stashPresenceDragging = id;
            CaptureMouse();
            SetStashPresenceFromPoint(id, rail, point);
            return true;
        }
        return false;
    }

    private bool StashPresenceMouseMove(Point point, MouseButtonState left)
    {
        if (Family != "StashContentMix") return false;
        if (_stashPresenceDragging is { } dragging && left == MouseButtonState.Pressed)
        {
            var entry = _stashPresenceHits.FirstOrDefault(h => h.Id == dragging);
            if (entry.Id is not null)
                SetStashPresenceFromPoint(dragging, entry.Rail, point);
            return true;
        }
        var hover = _stashPresenceHits.FirstOrDefault(h => h.Hit.Contains(point)).Id;
        if (hover != _stashPresenceHover)
        {
            _stashPresenceHover = hover;
            InvalidateVisual();
        }
        if (hover is null) return false;
        SetHoverTarget(null);
        Cursor = Cursors.Hand;
        return true;
    }

    private bool StashPresenceMouseUp()
    {
        if (_stashPresenceDragging is null) return false;
        _stashPresenceDragging = null;
        ReleaseMouseCapture();
        return true;
    }

    private void StashPresenceLostCapture() => _stashPresenceDragging = null;

    private bool StashPresenceRightClick(Point point)
    {
        if (Family != "StashContentMix" || StashPresence is null) return false;
        var entry = _stashPresenceHits.FirstOrDefault(h => h.Hit.Contains(point));
        if (entry.Id is null) return false;
        StashPresence.Set(entry.Id, 1.0);
        return true;
    }
}
