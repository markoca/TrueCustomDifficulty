using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Ten compact, source-SID-specific Chance controls embedded INSIDE the
/// existing NPC Loot aggregate frame. They never change native parameter IDs.
/// </summary>
public sealed partial class AggregateVisualControl
{
    private sealed record NpcConsumableTarget(
        NpcConsumableItemViewModel Item, Rect Checkbox, Rect TrackHit, Rect Rail);

    private IReadOnlyList<NpcConsumableItemViewModel> _npcConsumableItems =
        Array.Empty<NpcConsumableItemViewModel>();
    private readonly List<NpcConsumableTarget> _npcConsumableTargets = new();
    private NpcConsumableItemViewModel? _npcConsumableFocused;
    private NpcConsumableItemViewModel? _npcConsumableDragged;
    private string? _npcConsumableHoverSid;
    private bool _npcConsumableHoverCheckbox;

    public static readonly DependencyProperty NpcConsumablesProperty =
        DependencyProperty.Register(nameof(NpcConsumables), typeof(IEnumerable),
            typeof(AggregateVisualControl),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender, OnNpcConsumablesChanged));

    public IEnumerable? NpcConsumables
    {
        get => (IEnumerable?)GetValue(NpcConsumablesProperty);
        set => SetCurrentValue(NpcConsumablesProperty, value);
    }

    private static void OnNpcConsumablesChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs args)
    {
        var board = (AggregateVisualControl)d;
        foreach (var item in board._npcConsumableItems)
            item.PropertyChanged -= board.NpcConsumable_PropertyChanged;
        board._npcConsumableItems = (args.NewValue as IEnumerable)?
            .OfType<NpcConsumableItemViewModel>().ToArray()
            ?? Array.Empty<NpcConsumableItemViewModel>();
        foreach (var item in board._npcConsumableItems)
            item.PropertyChanged += board.NpcConsumable_PropertyChanged;
        board._npcConsumableFocused = null;
        board._npcConsumableDragged = null;
        board._npcConsumableTargets.Clear();
        board.InvalidateVisual();
    }

    private void NpcConsumable_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(NpcConsumableItemViewModel.Enabled)
            or nameof(NpcConsumableItemViewModel.Multiplier)
            or nameof(NpcConsumableItemViewModel.LegacyChancePercent))
            InvalidateVisual();
    }

    private void DrawNpcConsumables(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        _npcConsumableTargets.Clear();
        if (Family != "NpcLootLoadout")
            return;

        const double gap = 10.0;
        // Compact source-aware board follows the shared top editor strip
        // and the condition card, inside the same width-capped column.
        var layout = CurrentNpcLootLayout(ActualWidth);
        var top = NpcLootConsumablesTop(ActualWidth);
        const double rowHeight = 31.0;
        const double headerHeight = 27.0;
        // All three cards share the tallest card's height; shorter groups
        // (3 items vs 4 medical) spread their rows evenly inside it.
        var maxRows = Math.Max(1, _npcConsumableItems
            .GroupBy(item => item.Group).Select(g => g.Count())
            .DefaultIfEmpty(1).Max());
        var cardHeight = headerHeight + maxRows * rowHeight + 7.0;
        var colWidth = Math.Max(1.0, (layout.ContentWidth - gap * 2.0) / 3.0);
        var editorAvailable = IsEnabled && ModItemsEditorAvailable;

        // Rank-growth preview inputs: the same transforms the CFG writer
        // applies (row multiplier or MED DROPS, then FEWER SUPPLIES).
        var rankProfile = NpcConsumableRankProfile.Shared;
        var difficultySid = NpcConsumableRankProfile.DifficultySid(SelectedReferenceDifficulty);
        var globalMedical = P("loot.npcMedicalChance")?.CurrentNumber ?? 1.0;
        var supplyRemaining = 1.0 - Math.Clamp(
            P("loot.genericCorpseReduction")?.CurrentNumber ?? 0.0, 0.0, 1.0);

        // Same category tints as STASH LOOT MIX (food / drinks / medical).
        foreach (var (group, col, tint) in new[]
        {
            ("FOOD", 0, StashMixPalette[2]),
            ("DRINKS", 1, StashMixPalette[3]),
            ("MEDICAL", 2, StashMixPalette[1])
        })
        {
            var items = _npcConsumableItems
                .Where(item => item.Group == group).ToArray();
            var card = new Rect(layout.ContentLeft + col * (colWidth + gap), top,
                colWidth, cardHeight);
            var pitch = items.Length == 0 ? rowHeight
                : maxRows * rowHeight / items.Length;
            IndustrialChrome.DrawRecessedCard(dc, card, panel, border);
            dc.DrawEllipse(tint, null,
                new Point(card.Left + 14.0, card.Top + 13.5), 3.6, 3.6);
            DrawText(dc, group, new Point(card.Left + 23.0, card.Top + 7.0),
                10.0, text, FontWeights.Bold);
            dc.DrawLine(new Pen(CloneWithOpacity(border, 0.55), 0.8),
                new Point(card.Left + 9.0, card.Top + headerHeight - 3.0),
                new Point(card.Right - 9.0, card.Top + headerHeight - 3.0));

            // Player-rank columns (Newbie / Experienced / Veteran / Master)
            // share one fixed lane just left of the value readout.
            var rankLaneRight = card.Right - 12.0 - NpcRankValueReserve - 6.0;
            var rankLaneLeft = rankLaneRight - NpcRankLaneWidth;
            if (rankProfile is not null)
                for (var rank = 0; rank < NpcConsumableRankProfile.RankInitials.Length; rank++)
                    DrawCenteredText(dc, NpcConsumableRankProfile.RankInitials[rank],
                        new Point(NpcRankBarCenter(rankLaneLeft, rank), card.Top + 8.5),
                        7.6, muted, FontWeights.SemiBold);

            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var enabled = item.Enabled && editorAvailable;
                var rowTop = top + headerHeight + i * pitch
                    + (pitch - rowHeight) / 2.0;
                var box = new Rect(card.Left + 9.0, rowTop + 3.0, 15.0, 15.0);
                var rail = new Rect(card.Left + 14.0, rowTop + 23.0,
                    Math.Max(1.0, card.Width - 28.0), 2.5);
                var trackHit = new Rect(rail.Left - 5.0, rowTop + 16.5,
                    rail.Width + 10.0, 14.5);
                var hovered = _npcConsumableHoverSid == item.Sid;
                BooleanCheckboxChrome.Draw(dc, box, item.Enabled, editorAvailable,
                    hovered && _npcConsumableHoverCheckbox, panel, border, accent);
                // Stamina Usage grammar: label left, single value right.
                dc.DrawText(CreateText(item.Label, 9.6, enabled ? text : muted,
                        FontWeights.SemiBold,
                        Math.Max(1.0, rankLaneLeft - (card.Left + 30.0) - 6.0)),
                    new Point(card.Left + 30.0, rowTop + 2.0));
                if (rankProfile is not null)
                {
                    var inherited = item.IsMedical ? globalMedical : 1.0;
                    var vanilla = rankProfile.Average(item.Sid, difficultySid, chance => chance);
                    var effective = rankProfile.Average(item.Sid, difficultySid,
                        chance => item.ScaleSourceChance(chance, inherited) * supplyRemaining);
                    if (vanilla is not null && effective is not null)
                        DrawNpcRankBars(dc, rankLaneLeft, rowTop + 15.5,
                            vanilla, effective, tint, muted);
                }
                DrawRightAlignedText(dc, item.PercentText,
                    new Point(card.Right - 12.0, rowTop + 2.0),
                    enabled && item.IsEffectiveChange ? 10.8 : 10.1,
                    enabled && item.IsEffectiveChange ? accent
                        : enabled ? text : CloneWithOpacity(muted, 0.7),
                    FontWeights.SemiBold);
                var railY = rail.Top + rail.Height / 2.0;
                dc.DrawLine(new Pen(CloneWithOpacity(border, enabled ? 1.0 : .45), 3.5)
                    {
                        StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round
                    },
                    new Point(rail.Left, railY), new Point(rail.Right, railY));
                // Neutral 1x reference tick (vanilla Chance), no caption.
                var referenceX = rail.Left + rail.Width
                    / NpcConsumableItemViewModel.MaximumMultiplier;
                dc.DrawLine(new Pen(CloneWithOpacity(muted, enabled ? .75 : .4), 1.0),
                    new Point(referenceX, railY - 4.5), new Point(referenceX, railY + 4.5));
                if (enabled)
                {
                    var handleRatio = item.LegacyChancePercent is int legacy
                        ? legacy / 100.0
                        : item.Multiplier / NpcConsumableItemViewModel.MaximumMultiplier;
                    var handleX = rail.Left + rail.Width * handleRatio;
                    dc.DrawLine(new Pen(CloneWithOpacity(accent, .65), 3.5),
                        new Point(rail.Left, railY), new Point(handleX, railY));
                    dc.DrawEllipse(panel, new Pen(accent, hovered
                            && !_npcConsumableHoverCheckbox ? 2.4 : 1.5),
                        new Point(handleX, railY), 5.5, 5.5);
                    dc.DrawEllipse(accent, null, new Point(handleX, railY), 2.2, 2.2);
                }
                _npcConsumableTargets.Add(new NpcConsumableTarget(
                    item, box, trackHit, rail));
            }
        }
    }

    private void SelectNpcConsumable(NpcConsumableItemViewModel item)
    {
        _npcConsumableFocused = item;
        _npcModFocused = false;
        _selectedParameter = null;
        _selectedHitKind = null;
        // Item-specific explanations live in the source-aware NPC rationale;
        // no artificial catalog parameter is introduced for an override SID.
        var owner = item.Group == "MEDICAL"
            ? P("loot.npcMedicalChance") : P("loot.npcCorpseBonus");
        if (owner is not null)
            RaiseParameterSelected(owner);
    }

    private void UpdateNpcConsumable(Point point)
    {
        var item = _npcConsumableDragged;
        if (item is null || !item.Enabled || !IsEnabled || !ModItemsEditorAvailable)
            return;
        var target = _npcConsumableTargets.FirstOrDefault(t =>
            ReferenceEquals(t.Item, item));
        if (target is null) return;
        var ratio = Math.Clamp((point.X - target.Rail.Left) /
            Math.Max(1.0, target.Rail.Width), 0.0, 1.0);
        // Setter snaps to the 0.05x grid and clears any legacy absolute %.
        item.Multiplier = ratio * NpcConsumableItemViewModel.MaximumMultiplier;
    }

    private bool NpcConsumableMouseDown(Point point)
    {
        if (Family != "NpcLootLoadout" || !IsEnabled || !ModItemsEditorAvailable)
            return false;
        foreach (var target in _npcConsumableTargets)
        {
            if (target.Checkbox.Contains(point))
            {
                SelectNpcConsumable(target.Item);
                target.Item.Enabled = !target.Item.Enabled;
                InvalidateVisual();
                return true;
            }
            if (target.TrackHit.Contains(point))
            {
                SelectNpcConsumable(target.Item);
                if (target.Item.Enabled)
                {
                    _npcConsumableDragged = target.Item;
                    CaptureMouse();
                    UpdateNpcConsumable(point);
                }
                return true;
            }
        }
        _npcConsumableFocused = null;
        return false;
    }

    private bool NpcConsumableMouseMove(Point point, MouseButtonState left)
    {
        if (Family != "NpcLootLoadout")
            return false;
        if (_npcConsumableDragged is not null && left == MouseButtonState.Pressed)
        {
            UpdateNpcConsumable(point);
            return true;
        }
        // Existing native aggregate drag targets keep their mouse capture
        // even if the pointer passes over these opt-in tracks.
        if (_dragTarget is not null || _npcModDragging)
            return false;

        var target = _npcConsumableTargets.FirstOrDefault(t =>
            t.Checkbox.Contains(point) || t.TrackHit.Contains(point));
        var nextSid = target?.Item.Sid;
        var nextCheckbox = target?.Checkbox.Contains(point) == true;
        if (nextSid != _npcConsumableHoverSid || nextCheckbox != _npcConsumableHoverCheckbox)
        {
            _npcConsumableHoverSid = nextSid;
            _npcConsumableHoverCheckbox = nextCheckbox;
            InvalidateVisual();
        }
        if (target is null)
            return false;

        SetHoverTarget(null);
        Cursor = !IsEnabled || !ModItemsEditorAvailable ? Cursors.Arrow
            : nextCheckbox ? Cursors.Hand : target.Item.Enabled
                ? Cursors.SizeWE : Cursors.Arrow;
        return true;
    }

    private bool NpcConsumableRightClick(Point point)
    {
        if (Family != "NpcLootLoadout" || !IsEnabled || !ModItemsEditorAvailable)
            return false;
        var target = _npcConsumableTargets.FirstOrDefault(t =>
            t.TrackHit.Contains(point) || t.Checkbox.Contains(point));
        if (target is null)
            return false;
        SelectNpcConsumable(target.Item);
        // The ten source SIDs each span multiple native Chances. Restore
        // every original raw Chance by disabling this optional override.
        target.Item.Enabled = false;
        return true;
    }

    private bool NpcConsumableKeyDown(Key key)
    {
        if (Family != "NpcLootLoadout" || _npcConsumableFocused is not { } item
            || !IsEnabled || !ModItemsEditorAvailable)
            return false;
        if (key == Key.Space)
        {
            item.Enabled = !item.Enabled;
            return true;
        }
        if (key == Key.R)
        {
            item.Enabled = false;
            return true;
        }
        if (!item.Enabled)
            return false;
        var step = key switch
        {
            Key.Right or Key.Up or Key.Add or Key.OemPlus => 1,
            Key.Left or Key.Down or Key.Subtract or Key.OemMinus => -1,
            _ => 0
        };
        if (step == 0 && key is not (Key.Home or Key.End))
            return false;
        item.Multiplier = key == Key.Home ? NpcConsumableItemViewModel.MinimumMultiplier
            : key == Key.End ? NpcConsumableItemViewModel.MaximumMultiplier
            : item.Multiplier + step * NpcConsumableItemViewModel.MultiplierStep;
        return true;
    }

    private const double NpcRankBarWidth = 4.5;
    private const double NpcRankBarGap = 2.5;
    private const double NpcRankBarHeight = 12.0;
    private const double NpcRankValueReserve = 34.0;
    private static double NpcRankLaneWidth =>
        4 * NpcRankBarWidth + 3 * NpcRankBarGap;

    private static double NpcRankBarCenter(double laneLeft, int rank)
        => laneLeft + rank * (NpcRankBarWidth + NpcRankBarGap) + NpcRankBarWidth / 2.0;

    // Four tiny columns: dim ghost = vanilla Chance, category tint = Chance
    // after this board's settings. A raised value shows a muted tick at the
    // vanilla height; an absent rank keeps a 1-DIP baseline stub.
    private void DrawNpcRankBars(
        DrawingContext dc, double laneLeft, double bottom,
        IReadOnlyList<double> vanilla, IReadOnlyList<double> effective,
        Brush tint, Brush muted)
    {
        var ghost = CloneWithOpacity(muted, 0.30);
        var fill = CloneWithOpacity(tint, 0.88);
        var tick = new Pen(CloneWithOpacity(muted, 0.85), 1.0);
        for (var rank = 0; rank < 4; rank++)
        {
            var left = laneLeft + rank * (NpcRankBarWidth + NpcRankBarGap);
            var vanillaHeight = Math.Clamp(vanilla[rank], 0.0, 1.0) * NpcRankBarHeight;
            var effectiveHeight = Math.Clamp(effective[rank], 0.0, 1.0) * NpcRankBarHeight;
            dc.DrawRectangle(ghost, null, new Rect(left, bottom - Math.Max(1.0, vanillaHeight),
                NpcRankBarWidth, Math.Max(1.0, vanillaHeight)));
            if (effectiveHeight > 0.05)
                dc.DrawRectangle(fill, null, new Rect(left, bottom - effectiveHeight,
                    NpcRankBarWidth, effectiveHeight));
            if (effectiveHeight > vanillaHeight + 0.6 && vanillaHeight > 0.05)
                dc.DrawLine(tick, new Point(left - 0.5, bottom - vanillaHeight),
                    new Point(left + NpcRankBarWidth + 0.5, bottom - vanillaHeight));
        }
    }
}
