using System.Windows;
using System.Windows.Controls;

namespace TrueCustomDifficulty.Infrastructure;

/// <summary>
/// Locked section layout: every complete catalog section occupies one full-width
/// row. Internal controls retain their own independent arrangements.
/// </summary>
public sealed class ResponsiveSectionGridPanel : Panel
{
    private const double RowGap = 12.0;

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsFinite(availableSize.Width)
            ? Math.Max(280.0, availableSize.Width)
            : 900.0;
        double height = 0.0;
        var visible = InternalChildren.Cast<UIElement>()
            .Where(item => item.Visibility != Visibility.Collapsed).ToList();
        foreach (var child in visible)
        {
            child.Measure(new Size(width, double.PositiveInfinity));
            height += child.DesiredSize.Height + RowGap;
        }
        if (visible.Count > 0)
            height -= RowGap;
        return new Size(width, Math.Max(0.0, height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(0.0, finalSize.Width);
        double y = 0.0;
        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                child.Arrange(new Rect(0.0, 0.0, 0.0, 0.0));
                continue;
            }
            var height = child.DesiredSize.Height;
            child.Arrange(new Rect(0.0, y, width, height));
            y += height + RowGap;
        }
        return new Size(width, Math.Max(finalSize.Height,
            y > 0 ? y - RowGap : 0.0));
    }
}
