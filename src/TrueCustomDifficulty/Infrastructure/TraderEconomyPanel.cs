using System;
using System.Windows;
using System.Windows.Controls;

namespace TrueCustomDifficulty.Infrastructure;

/// <summary>
/// One responsive Economy editor: Trader Stock on the left, with horizontal
/// Stock Amount and Service Prices knob banks stacked on its right.
/// Existing parameters and CFG writer behavior remain unchanged.
/// </summary>
public sealed class TraderEconomyPanel : Panel
{
    private const double Gap = 10.0;
    private const double TableShare = 0.30;
    private const double MinTableWidth = 300.0;
    private const double MinBankWidth = 470.0;
    // Only used when the window is too narrow to preserve a readable 30/70 split.
    private const double FallbackTableWidth = 820.0;
    private const double FallbackBankWidth = 606.0;

    private static double UsableWidth(double width)
        => double.IsFinite(width) ? Math.Max(1.0, width) : 1120.0;

    // Split the available content (not the 10-DIP gutter) precisely 30/70.
    // Both Measure and Arrange use the same calculation, with no maximum
    // width on either column that would silently undo the requested ratio.
    private static (double Table, double Banks) ColumnWidths(double width)
    {
        var contentWidth = Math.Max(1.0, width - Gap);
        var tableWidth = contentWidth * TableShare;
        return (tableWidth, contentWidth - tableWidth);
    }

    private static bool UseSideColumn(double width)
    {
        var (tableWidth, bankWidth) = ColumnWidths(width);
        return tableWidth >= MinTableWidth && bankWidth >= MinBankWidth;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = UsableWidth(availableSize.Width);
        if (InternalChildren.Count != 3)
            throw new InvalidOperationException("Trader Economy requires a stock table and two knob banks.");

        var table = InternalChildren[0];
        var stock = InternalChildren[1];
        var services = InternalChildren[2];

        if (UseSideColumn(width))
        {
            var (tableWidth, bankWidth) = ColumnWidths(width);
            table.Measure(new Size(tableWidth, double.PositiveInfinity));
            stock.Measure(new Size(bankWidth, double.PositiveInfinity));
            services.Measure(new Size(bankWidth, double.PositiveInfinity));
            var rightHeight = stock.DesiredSize.Height + Gap + services.DesiredSize.Height;
            return new Size(width, Math.Max(table.DesiredSize.Height, rightHeight));
        }

        // Narrow fallback preserves the horizontal knob orientation within
        // each bank; only the two bank cards move below the stock table.
        var narrowTableWidth = Math.Min(FallbackTableWidth, width);
        var narrowBankWidth = Math.Min(FallbackBankWidth, width);
        table.Measure(new Size(narrowTableWidth, double.PositiveInfinity));
        stock.Measure(new Size(narrowBankWidth, double.PositiveInfinity));
        services.Measure(new Size(narrowBankWidth, double.PositiveInfinity));
        return new Size(width, table.DesiredSize.Height + Gap
            + stock.DesiredSize.Height + Gap + services.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(1.0, finalSize.Width);
        var table = InternalChildren[0];
        var stock = InternalChildren[1];
        var services = InternalChildren[2];

        if (UseSideColumn(width))
        {
            var (tableWidth, bankWidth) = ColumnWidths(width);
            var totalWidth = tableWidth + Gap + bankWidth;
            var left = Math.Max(0.0, (width - totalWidth) / 2.0);
            var right = left + tableWidth + Gap;
            table.Arrange(new Rect(left, 0.0, tableWidth, table.DesiredSize.Height));
            stock.Arrange(new Rect(right, 0.0, bankWidth, stock.DesiredSize.Height));
            services.Arrange(new Rect(right, stock.DesiredSize.Height + Gap,
                bankWidth, services.DesiredSize.Height));
            return finalSize;
        }

        var tableWidthNarrow = Math.Min(FallbackTableWidth, width);
        var bankWidthNarrow = Math.Min(FallbackBankWidth, width);
        table.Arrange(new Rect((width - tableWidthNarrow) / 2.0, 0.0,
            tableWidthNarrow, table.DesiredSize.Height));
        var top = table.DesiredSize.Height + Gap;
        var bankLeft = (width - bankWidthNarrow) / 2.0;
        stock.Arrange(new Rect(bankLeft, top, bankWidthNarrow, stock.DesiredSize.Height));
        services.Arrange(new Rect(bankLeft, top + stock.DesiredSize.Height + Gap,
            bankWidthNarrow, services.DesiredSize.Height));
        return finalSize;
    }
}
