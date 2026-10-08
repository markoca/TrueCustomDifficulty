using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace TrueCustomDifficulty.Infrastructure;

/// <summary>
/// Optional two-column layout for complete catalog sections. Unlike the
/// parameter-card grid, this panel never splits a section header from its
/// controls or silently changes the catalog's section order.
/// </summary>
public sealed class ResponsiveSectionGridPanel : Panel
{
    private const double ColumnGap = 16.0;
    private const double RowGap = 12.0;
    private const double MinSectionWidth = 540.0;

    // Only pairs whose content respects the existing 520-DIP aggregate minimum at half width are opted in.
    // Other (including complex radar, repair curve and timeline) sections stay
    // full width. Section adjacency remains required to preserve reading order.
    private static readonly IReadOnlyDictionary<string, string> CompactPairs =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Stashes"] = "Breakable Containers",
        };

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = ResolveWidth(availableSize.Width);
        var rows = BuildRows(width);
        double height = 0.0;
        foreach (var row in rows)
        {
            var cardWidth = row.Count == 2 ? (width - ColumnGap) / 2.0 : width;
            double rowHeight = 0.0;
            foreach (var element in row)
            {
                element.Measure(new Size(cardWidth, double.PositiveInfinity));
                rowHeight = Math.Max(rowHeight, element.DesiredSize.Height);
            }
            height += rowHeight + RowGap;
        }
        if (rows.Count > 0)
            height -= RowGap;
        return new Size(width, Math.Max(0.0, height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(0.0, finalSize.Width);
        var rows = BuildRows(width);
        double y = 0.0;
        foreach (var row in rows)
        {
            var cardWidth = row.Count == 2 ? (width - ColumnGap) / 2.0 : width;
            var rowHeight = row.Max(item => item.DesiredSize.Height);
            for (var i = 0; i < row.Count; i++)
                row[i].Arrange(new Rect(i * (cardWidth + ColumnGap), y, cardWidth, rowHeight));
            y += rowHeight + RowGap;
        }
        foreach (UIElement element in InternalChildren)
            if (element.Visibility == Visibility.Collapsed)
                element.Arrange(new Rect(0.0, 0.0, 0.0, 0.0));
        return new Size(width, Math.Max(finalSize.Height, y > 0 ? y - RowGap : 0.0));
    }

    private List<List<UIElement>> BuildRows(double width)
    {
        var elements = InternalChildren.Cast<UIElement>()
            .Where(item => item.Visibility != Visibility.Collapsed).ToList();
        var rows = new List<List<UIElement>>();
        var canPair = (width - ColumnGap) / 2.0 >= MinSectionWidth;
        for (var i = 0; i < elements.Count; i++)
        {
            var row = new List<UIElement> { elements[i] };
            if (canPair && i + 1 < elements.Count
                && IsCompatiblePair(elements[i], elements[i + 1]))
            {
                row.Add(elements[++i]);
            }
            rows.Add(row);
        }
        return rows;
    }

    private static bool IsCompatiblePair(UIElement left, UIElement right)
    {
        var first = (left as FrameworkElement)?.DataContext as CollectionViewGroup;
        var second = (right as FrameworkElement)?.DataContext as CollectionViewGroup;
        return first?.Name is string label
            && second?.Name is string next
            && CompactPairs.TryGetValue(label, out var partner)
            && partner == next;
    }

    private static double ResolveWidth(double width)
        => double.IsInfinity(width) || double.IsNaN(width)
            ? 900.0 : Math.Max(280.0, width);
}
