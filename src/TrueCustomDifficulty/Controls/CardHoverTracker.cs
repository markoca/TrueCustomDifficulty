using System.Windows;
using System.Windows.Media;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Shared "white card hover" layer for every custom-drawn control. While a
/// control renders inside <see cref="BeginRender"/>, every filled + outlined
/// rounded rectangle drawn through <see cref="TrackedDrawing"/> (and every
/// <c>IndustrialChrome.DrawRecessedCard</c>) is remembered as a card. The
/// smallest card under the pointer gets a white outline along its own
/// border. Chips/buttons that are themselves a hit target, tiny shapes and
/// whole-board frames are not cards.
/// </summary>
internal sealed class CardHoverTracker
{
    [ThreadStatic] private static CardHoverTracker? s_current;

    private readonly List<(Rect Rect, double Radius)> _recorded = new();
    private readonly List<(Rect Rect, double Radius)> _cards = new();
    private Rect? _hover;

    public const double MinimumWidth = 46.0;
    public const double MinimumHeight = 30.0;

    public IDisposable BeginRender()
    {
        _recorded.Clear();
        var previous = s_current;
        s_current = this;
        return new Scope(() => s_current = previous);
    }

    internal static void Record(Rect rect, double radius)
    {
        if (s_current is null || rect.IsEmpty
            || rect.Width < MinimumWidth || rect.Height < MinimumHeight)
            return;
        s_current._recorded.Add((rect, radius));
    }

    /// <summary>
    /// Freeze the cards of this frame. <paramref name="controlHits"/> are the
    /// control's own clickable element rectangles (a recorded shape equal to
    /// one of them is a chip, not a card); <paramref name="bounds"/> rejects
    /// shapes covering most of the control (board frames).
    /// </summary>
    public void Finish(IEnumerable<Rect> controlHits, Size bounds)
    {
        _cards.Clear();
        var hits = controlHits.ToArray();
        var area = Math.Max(1.0, bounds.Width * bounds.Height);
        foreach (var (rect, radius) in _recorded)
        {
            if (rect.Width * rect.Height > area * 0.80)
                continue;
            if (hits.Any(hit => Near(hit, rect)))
                continue;
            if (_cards.Any(card => Near(card.Rect, rect)))
                continue;
            _cards.Add((rect, radius));
        }
        if (_hover is Rect hover && !_cards.Any(card => Near(card.Rect, hover)))
            _hover = null;
    }

    public Rect? CardAt(Point point)
    {
        Rect? best = null;
        foreach (var (rect, _) in _cards)
            if (rect.Contains(point)
                && (best is null || rect.Width * rect.Height < best.Value.Width * best.Value.Height))
                best = rect;
        return best;
    }

    /// <returns>True when the hovered card changed (caller re-renders).</returns>
    public bool UpdateHover(Point point)
    {
        var next = CardAt(point);
        if (Nullable.Equals(next, _hover))
            return false;
        _hover = next;
        return true;
    }

    public bool ClearHover()
    {
        if (_hover is null)
            return false;
        _hover = null;
        return true;
    }

    public void DrawHover(DrawingContext dc)
    {
        if (_hover is not Rect hover)
            return;
        var radius = _cards.FirstOrDefault(card => Near(card.Rect, hover)).Radius;
        dc.DrawRoundedRectangle(null, new Pen(Brushes.White, 1.2), hover, radius, radius);
    }

    private static bool Near(Rect a, Rect b)
        => Math.Abs(a.Left - b.Left) < 1.5 && Math.Abs(a.Top - b.Top) < 1.5
            && Math.Abs(a.Width - b.Width) < 3.0 && Math.Abs(a.Height - b.Height) < 3.0;

    private sealed class Scope : IDisposable
    {
        private Action? _dispose;
        public Scope(Action dispose) => _dispose = dispose;
        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null;
        }
    }
}

/// <summary>Drop-in replacements that also report cards to the active tracker.</summary>
internal static class TrackedDrawing
{
    public static void DrawTrackedRoundedRectangle(this DrawingContext dc,
        Brush? brush, Pen? pen, Rect rectangle, double radiusX, double radiusY)
    {
        dc.DrawRoundedRectangle(brush, pen, rectangle, radiusX, radiusY);
        if (brush is not null && pen is not null)
            CardHoverTracker.Record(rectangle, radiusX);
    }
}
