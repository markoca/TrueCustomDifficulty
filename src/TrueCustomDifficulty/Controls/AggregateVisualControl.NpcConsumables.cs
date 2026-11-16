using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
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
            or nameof(NpcConsumableItemViewModel.ChancePercent)
            or nameof(NpcConsumableItemViewModel.LegacyMultiplier))
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
        var colWidth = Math.Max(1.0, (layout.ContentWidth - gap * 2.0) / 3.0);
        var editorAvailable = IsEnabled && ModItemsEditorAvailable;

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
                colWidth, headerHeight + items.Length * rowHeight + 7.0);
            IndustrialChrome.DrawRecessedCard(dc, card, panel, border);
            dc.DrawEllipse(tint, null,
                new Point(card.Left + 14.0, card.Top + 13.5), 3.6, 3.6);
            DrawText(dc, group, new Point(card.Left + 23.0, card.Top + 7.0),
                10.0, text, FontWeights.Bold);
            dc.DrawLine(new Pen(CloneWithOpacity(border, 0.55), 0.8),
                new Point(card.Left + 9.0, card.Top + headerHeight - 3.0),
                new Point(card.Right - 9.0, card.Top + headerHeight - 3.0));

            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var enabled = item.Enabled && editorAvailable;
                var rowTop = top + headerHeight + i * rowHeight;
                var box = new Rect(card.Left + 9.0, rowTop + 3.0, 15.0, 15.0);
                var rail = new Rect(card.Left + 14.0, rowTop + 23.0,
                    Math.Max(1.0, card.Width - 28.0), 2.5);
                var trackHit = new Rect(rail.Left - 5.0, rowTop + 16.5,
                    rail.Width + 10.0, 14.5);
                var hovered = _npcConsumableHoverSid == item.Sid;
                BooleanCheckboxChrome.Draw(dc, box, item.Enabled, editorAvailable,
                    hovered && _npcConsumableHoverCheckbox, panel, border, accent);
                // Stamina Usage grammar: label left, single value right.
                DrawText(dc, item.Label, new Point(card.Left + 30.0, rowTop + 2.0),
                    9.6, enabled ? text : muted, FontWeights.SemiBold);
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
                if (enabled)
                {
                    var handleRatio = item.LegacyMultiplier is double legacy
                        ? legacy / 2.0 : item.ChancePercent / 100.0;
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
        item.ChancePercent = (int)Math.Round(ratio * 100.0,
            MidpointRounding.AwayFromZero);
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
        item.ChancePercent = key == Key.Home ? 0
            : key == Key.End ? 100
            : Math.Clamp(item.ChancePercent + step, 0, 100);
        return true;
    }
}
