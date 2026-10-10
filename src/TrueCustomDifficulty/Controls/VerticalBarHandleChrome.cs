using System.Windows;
using System.Windows.Media;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Consistent source-backed current-value marker for editable vertical columns.
/// Unlike a circular thumb, this short cap follows the whole bar's top edge;
/// a hovered bar brightens its cap without inventing another control/axis.
/// </summary>
internal static class VerticalBarHandleChrome
{
    private const double CapOverhang = 7.0;

    public static void Draw(
        DrawingContext dc, Point barTopCenter, double barWidth,
        Brush accent, bool hovered)
    {
        var halfWidth = Math.Max(1.0, barWidth / 2.0);
        var left = new Point(barTopCenter.X - halfWidth - CapOverhang,
            barTopCenter.Y);
        var right = new Point(barTopCenter.X + halfWidth + CapOverhang,
            barTopCenter.Y);

        if (hovered)
        {
            var glow = accent.CloneCurrentValue();
            glow.Opacity = 0.17;
            glow.Freeze();
            dc.DrawLine(new Pen(glow, 8.0), left, right);
        }

        dc.DrawLine(new Pen(hovered ? Brushes.White : accent,
            hovered ? 3.5 : 2.4), left, right);
    }
}
