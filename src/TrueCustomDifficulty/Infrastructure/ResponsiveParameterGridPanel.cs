using System.Windows;
using System.Windows.Controls;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Infrastructure;

public sealed class ResponsiveParameterGridPanel : Panel
{
    private const double Gap = 8.0;
    private const double TwoColumnThreshold = 700.0;

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = ResolveWidth(availableSize.Width);
        var columns = GetColumnCount(width);
        var cardWidth = GetCardWidth(width, columns);

        double totalHeight = 0.0;
        double rowHeight = 0.0;
        var rowItems = 0;

        foreach (UIElement child in InternalChildren)
        {
            if (!IsLayoutVisible(child))
                continue;

            var fullWidth = IsFullWidth(child);

            if (fullWidth)
            {
                if (rowItems > 0)
                {
                    totalHeight += rowHeight + Gap;
                    rowHeight = 0.0;
                    rowItems = 0;
                }

                child.Measure(new Size(width, double.PositiveInfinity));
                totalHeight += child.DesiredSize.Height + Gap;
                continue;
            }

            child.Measure(new Size(cardWidth, double.PositiveInfinity));
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
            rowItems++;

            if (rowItems >= columns)
            {
                totalHeight += rowHeight + Gap;
                rowHeight = 0.0;
                rowItems = 0;
            }
        }

        if (rowItems > 0)
            totalHeight += rowHeight + Gap;

        if (totalHeight > 0.0)
            totalHeight -= Gap;

        return new Size(width, Math.Max(0.0, totalHeight));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(0.0, finalSize.Width);
        var columns = GetColumnCount(width);
        var cardWidth = GetCardWidth(width, columns);

        double y = 0.0;
        var row = new List<UIElement>(columns);
        double rowHeight = 0.0;

        void ArrangeRow()
        {
            if (row.Count == 0)
                return;

            for (var i = 0; i < row.Count; i++)
            {
                row[i].Arrange(new Rect(
                    i * (cardWidth + Gap),
                    y,
                    cardWidth,
                    rowHeight));
            }

            y += rowHeight + Gap;
            row.Clear();
            rowHeight = 0.0;
        }

        foreach (UIElement child in InternalChildren)
        {
            if (!IsLayoutVisible(child))
            {
                child.Arrange(new Rect(0.0, 0.0, 0.0, 0.0));
                continue;
            }

            if (IsFullWidth(child))
            {
                ArrangeRow();
                var height = child.DesiredSize.Height;
                child.Arrange(new Rect(0.0, y, width, height));
                y += height + Gap;
                continue;
            }

            row.Add(child);
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);

            if (row.Count >= columns)
                ArrangeRow();
        }

        ArrangeRow();

        if (y > 0.0)
            y -= Gap;

        return new Size(width, Math.Max(finalSize.Height, y));
    }

    private static double ResolveWidth(double width)
        => double.IsInfinity(width) || double.IsNaN(width)
            ? 900.0
            : Math.Max(280.0, width);

    private static int GetColumnCount(double width)
        => width >= TwoColumnThreshold ? 2 : 1;

    private static double GetCardWidth(double width, int columns)
        => Math.Max(260.0, (width - Gap * (columns - 1)) / columns);

    private static bool IsLayoutVisible(UIElement child)
    {
        if (child is FrameworkElement { DataContext: ParameterViewModel parameter })
            return parameter.IsControlVisible;

        return child.Visibility != Visibility.Collapsed;
    }

    private static bool IsFullWidth(UIElement child)
    {
        if (child is not FrameworkElement { DataContext: ParameterViewModel parameter })
            return false;

        return parameter.IsAggregateRadarAnchor;
    }
}
