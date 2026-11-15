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

        const double margin = 17.0;
        const double gap = 9.0;
        // Compact source-aware board: sits directly after mod reduction, no gap.
        const double top = 553.0;
        const double rowHeight = 31.0;
        const double headerHeight = 27.0;
        var fullWidth = Math.Max(1.0, ActualWidth - 2.0 * margin);
        var colWidth = Math.Max(1.0, (fullWidth - gap * 2.0) / 3.0);
        var editorAvailable = IsEnabled && ModItemsEditorAvailable;

        foreach (var (group, col) in new[]
        {
            ("FOOD", 0), ("DRINKS", 1), ("MEDICAL", 2)
        })
        {
            var items = _npcConsumableItems
                .Where(item => item.Group == group).ToArray();
            var card = new Rect(margin + col * (colWidth + gap), top,
                colWidth, headerHeight + items.Length * rowHeight + 7.0);
            dc.DrawRoundedRectangle(panel, new Pen(border, 0.95), card, 4.0, 4.0);
            DrawText(dc, group, new Point(card.Left + 10.0, card.Top + 7.0),
                10.0, text, FontWeights.Bold);

            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                var enabled = item.Enabled && editorAvailable;
                var rowTop = top + headerHeight + i * rowHeight;
                var box = new Rect(card.Left + 9.0, rowTop + 3.0, 15.0, 15.0);
                var rail = new Rect(card.Left + 13.0, rowTop + 23.0,
                    Math.Max(1.0, card.Width - 26.0), 2.5);
                var trackHit = new Rect(rail.Left, rowTop + 17.5,
                    rail.Width, 13.5);
                var hovered = _npcConsumableHoverSid == item.Sid;
                BooleanCheckboxChrome.Draw(dc, box, item.Enabled, editorAvailable,
                    hovered && _npcConsumableHoverCheckbox, panel, border, accent);
                DrawText(dc, item.Label, new Point(card.Left + 28.0, rowTop + 2.0),
                    9.4, enabled ? text : muted, FontWeights.SemiBold);
                DrawRightAlignedText(dc, item.PercentText,
                    new Point(card.Right - 9.0, rowTop + 2.0),
                    10.1, enabled && item.IsEffectiveChange ? accent
                        : enabled ? text : muted, FontWeights.SemiBold);
                var railY = rail.Top + rail.Height / 2.0;
                var handleRatio = item.LegacyMultiplier is double legacy
                    ? legacy / 2.0 : item.ChancePercent / 100.0;
                var handleX = rail.Left + rail.Width * handleRatio;
                dc.DrawLine(new Pen(CloneWithOpacity(border, enabled ? .85 : .5), 2.5),
                    new Point(rail.Left, railY), new Point(rail.Right, railY));
                if (enabled)
                    dc.DrawLine(new Pen(CloneWithOpacity(accent, .72), 2.5),
                        new Point(rail.Left, railY), new Point(handleX, railY));
                dc.DrawEllipse(panel, new Pen(enabled ? accent : muted,
                    enabled ? 1.55 : .9), new Point(handleX, railY), 4.3, 4.3);
                if (enabled)
                    dc.DrawEllipse(accent, null, new Point(handleX, railY), 1.4, 1.4);
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
