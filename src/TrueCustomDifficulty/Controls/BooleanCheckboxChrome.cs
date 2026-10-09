using System.Windows;
using System.Windows.Media;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Canonical boolean checkbox chrome for every hand-drawn editor.
/// Mirrors the 20-DIP CheckBox template in App.xaml. A hovered checkbox
/// only brightens its thin border; it deliberately has no halo or card wash.
/// </summary>
internal static class BooleanCheckboxChrome
{
    public const double Size = 20.0;
    public const double CornerRadius = 3.0;
    public const double BorderThickness = 1.0;

    public static void Draw(
        DrawingContext dc, Rect box, bool isChecked, bool isEnabled, bool hovered,
        Brush panel, Brush border, Brush accent)
    {
        var edge = isEnabled && hovered
            ? Application.Current?.TryFindResource("RadarMutedTextBrush") as Brush ?? border
            : border;

        dc.DrawRoundedRectangle(panel,
            new Pen(edge, BorderThickness), box, CornerRadius, CornerRadius);

        if (!isChecked)
            return;

        var checkColor = accent;
        var pen = new Pen(checkColor, 2.2)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };
        var sx = box.Width / Size;
        var sy = box.Height / Size;
        Point At(double x, double y) => new(box.Left + x * sx, box.Top + y * sy);
        dc.DrawLine(pen, At(4.5, 10.2), At(8.4, 14.2));
        dc.DrawLine(pen, At(8.4, 14.2), At(15.5, 5.8));
    }
}
