using System.Windows;
using System.Windows.Media;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Canonical hover feedback for every hand-drawn interactive control.
/// Only the active handle (or a switch's indicator) glows; selection,
/// modified state and reference marks must not generate hover halos.
/// </summary>
internal static class ControlHoverGlow
{
    public const double HaloRadius = 15.0;
    public const double RingRadius = 11.0;
    public const double HaloOpacity = 0.17;
    public const double RingOpacity = 0.67;
    public const double RingWidth = 1.6;

    public static void Draw(DrawingContext dc, Point center, Brush accent, bool hovered)
    {
        if (!hovered)
            return;

        var halo = accent.CloneCurrentValue();
        halo.Opacity = HaloOpacity;
        halo.Freeze();
        var ring = accent.CloneCurrentValue();
        ring.Opacity = RingOpacity;
        ring.Freeze();

        dc.DrawEllipse(halo, null, center, HaloRadius, HaloRadius);
        dc.DrawEllipse(null, new Pen(ring, RingWidth), center, RingRadius, RingRadius);
    }
}
