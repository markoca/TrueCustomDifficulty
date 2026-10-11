using System.Windows;
using System.Windows.Controls;

namespace TrueCustomDifficulty.Infrastructure;

/// <summary>
/// Catalog groups retain their normal full-width rows, except the two
/// independent Master conditional-penalty groups. Their original toggle,
/// source controls and reset scopes are preserved in adjacent group items.
/// </summary>
public sealed class ResponsiveSectionGridPanel : Panel
{
    private const double RowGap = 12.0;
    private const double ColumnGap = 12.0;
    private const double MasterPairMinWidth = 740.0;

    private static bool IsMasterPenaltyPair(UIElement first, UIElement second)
        => first is FrameworkElement { Tag: "Radiation" }
           && second is FrameworkElement { Tag: "Low Health" };

    // Only explicitly adjacent Master group identities are paired. No other
    // groups, tabs or unrelated conditional bundles can enter this layout.
    private List<(UIElement First, UIElement? Second)> VisibleRows(double width)
    {
        var visible = InternalChildren.Cast<UIElement>()
            .Where(item => item.Visibility != Visibility.Collapsed).ToList();
        var rows = new List<(UIElement First, UIElement? Second)>();
        for (var i = 0; i < visible.Count; i++)
        {
            if (width >= MasterPairMinWidth && i + 1 < visible.Count
                && IsMasterPenaltyPair(visible[i], visible[i + 1]))
            {
                rows.Add((visible[i], visible[i + 1]));
                i++;
            }
            else
            {
                rows.Add((visible[i], null));
            }
        }
        return rows;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsFinite(availableSize.Width)
            ? Math.Max(280.0, availableSize.Width)
            : 900.0;
        var rows = VisibleRows(width);
        double height = 0.0;

        foreach (var (first, second) in rows)
        {
            var itemWidth = second is null ? width : (width - ColumnGap) / 2.0;
            first.Measure(new Size(itemWidth, double.PositiveInfinity));
            if (second is not null)
                second.Measure(new Size(itemWidth, double.PositiveInfinity));
            height += Math.Max(first.DesiredSize.Height, second?.DesiredSize.Height ?? 0.0)
                      + RowGap;
        }

        if (rows.Count > 0)
            height -= RowGap;
        return new Size(width, Math.Max(0.0, height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(0.0, finalSize.Width);
        var rows = VisibleRows(width);
        double y = 0.0;

        foreach (var (first, second) in rows)
        {
            if (second is null)
            {
                first.Arrange(new Rect(0.0, y, width, first.DesiredSize.Height));
                y += first.DesiredSize.Height + RowGap;
            }
            else
            {
                var half = (width - ColumnGap) / 2.0;
                var height = Math.Max(first.DesiredSize.Height, second.DesiredSize.Height);
                first.Arrange(new Rect(0.0, y, half, height));
                second.Arrange(new Rect(half + ColumnGap, y, half, height));
                y += height + RowGap;
            }
        }

        foreach (UIElement child in InternalChildren)
        {
            if (child.Visibility == Visibility.Collapsed)
                child.Arrange(new Rect(0.0, 0.0, 0.0, 0.0));
        }
        return new Size(width, Math.Max(finalSize.Height,
            y > 0.0 ? y - RowGap : 0.0));
    }
}
