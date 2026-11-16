using System.Windows;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// NPC LOOT &amp; EQUIPMENT card layer: every card registers its rectangle
/// while painting. Hovering anywhere in a card outlines its border in white;
/// clicking anywhere in it (including on its control) shows that card's own
/// gamer-facing title and text in the right-hand panel.
/// </summary>
public sealed partial class AggregateVisualControl
{
    private readonly List<(NpcLootCard Card, Rect Rect)> _npcCards = new();
    private string? _npcHoverCardKey;
    private NpcLootCard? _npcActiveCard;

    private readonly CardHoverTracker _cardHover = new();

    // Generic cards (all families except NPC LOOT): a click anywhere in a
    // card explains the control in it nearest to the click. Never consumes
    // the click; the control's own handler still runs afterwards.
    private void GenericCardSelectAt(Point point)
    {
        if (Family == "NpcLootLoadout" || _cardHover.CardAt(point) is not Rect card)
            return;
        HitTarget? nearest = null;
        var best = double.MaxValue;
        foreach (var target in _hitTargets)
        {
            var center = new Point(target.HitRect.Left + target.HitRect.Width / 2.0,
                target.HitRect.Top + target.HitRect.Height / 2.0);
            if (!card.Contains(center))
                continue;
            var distance = (center - point).LengthSquared;
            if (distance < best)
            {
                best = distance;
                nearest = target;
            }
        }
        if (nearest is not null)
            ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(nearest.Parameter));
    }

    private void RegisterNpcCard(string key, Rect rect)
    {
        if (!rect.IsEmpty && rect.Width > 1.0
            && NpcLootCards.ByKey.TryGetValue(key, out var card))
            _npcCards.Add((card, rect));
    }

    private NpcLootCard? NpcCardAt(Point point)
    {
        if (Family != "NpcLootLoadout")
            return null;
        foreach (var (card, rect) in _npcCards)
            if (rect.Contains(point))
                return card;
        return null;
    }

    // Never consumes the click: the card's own control still handles it,
    // and any selection it raises reports this card (RaiseParameterSelected).
    private void NpcCardSelectAt(Point point)
    {
        if (Family != "NpcLootLoadout")
            return;
        _npcActiveCard = NpcCardAt(point);
        if (_npcActiveCard is { } card && P(card.OwnerId) is { } owner)
            RaiseParameterSelected(owner);
    }

    private void NpcCardFocusAt(Point point)
    {
        if (NpcCardAt(point) is { } card)
            _npcActiveCard = card;
    }

    private void NpcCardHover(Point point)
    {
        var key = NpcCardAt(point)?.Key;
        if (key == _npcHoverCardKey)
            return;
        _npcHoverCardKey = key;
        InvalidateVisual();
    }

    private void NpcCardMouseLeave()
    {
        if (_npcHoverCardKey is null)
            return;
        _npcHoverCardKey = null;
        InvalidateVisual();
    }

    // White outline along the hovered card's own border (same 4-DIP corners).
    private void DrawNpcCardHover(DrawingContext dc)
    {
        if (_npcHoverCardKey is null)
            return;
        foreach (var (card, rect) in _npcCards)
            if (card.Key == _npcHoverCardKey)
                dc.DrawRoundedRectangle(null, new Pen(Brushes.White, 1.2), rect, 4.0, 4.0);
    }
}
