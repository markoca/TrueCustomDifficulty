using System;
using System.Windows;
using System.Windows.Controls;

namespace TrueCustomDifficulty.Infrastructure;

/// <summary>
/// One responsive Economy editor: the TRADER STOCK board (with its AMMO /
/// MEDICAL SUPPLY rails) on the left and the Service Prices bank on the right.
/// Narrow windows stack the bank below the board.
/// </summary>
public sealed class TraderEconomyPanel : Panel
{
    private const double Gap = 10.0;
    private const double TableShare = 0.42;
    private const double MinTableWidth = 380.0;
    private const double MinBankWidth = 420.0;
    // Only used when the window is too narrow for the side-by-side split.
    private const double FallbackTableWidth = 820.0;
    private const double FallbackBankWidth = 606.0;

    private static double UsableWidth(double width)
        => double.IsFinite(width) ? Math.Max(1.0, width) : 1120.0;

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
        if (InternalChildren.Count != 2)
            throw new InvalidOperationException("Trader Economy requires the stock board and the service bank.");

        var table = InternalChildren[0];
        var services = InternalChildren[1];

        if (UseSideColumn(width))
        {
            var (tableWidth, bankWidth) = ColumnWidths(width);
            table.Measure(new Size(tableWidth, double.PositiveInfinity));
            services.Measure(new Size(bankWidth, double.PositiveInfinity));
            return new Size(width, Math.Max(table.DesiredSize.Height, services.DesiredSize.Height));
        }

        table.Measure(new Size(Math.Min(FallbackTableWidth, width), double.PositiveInfinity));
        services.Measure(new Size(Math.Min(FallbackBankWidth, width), double.PositiveInfinity));
        return new Size(width, table.DesiredSize.Height + Gap + services.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(1.0, finalSize.Width);
        var table = InternalChildren[0];
        var services = InternalChildren[1];

        if (UseSideColumn(width))
        {
            var (tableWidth, bankWidth) = ColumnWidths(width);
            var left = Math.Max(0.0, (width - (tableWidth + Gap + bankWidth)) / 2.0);
            table.Arrange(new Rect(left, 0.0, tableWidth, table.DesiredSize.Height));
            services.Arrange(new Rect(left + tableWidth + Gap, 0.0, bankWidth, services.DesiredSize.Height));
            return finalSize;
        }

        var tableWidthNarrow = Math.Min(FallbackTableWidth, width);
        var bankWidthNarrow = Math.Min(FallbackBankWidth, width);
        table.Arrange(new Rect((width - tableWidthNarrow) / 2.0, 0.0,
            tableWidthNarrow, table.DesiredSize.Height));
        services.Arrange(new Rect((width - bankWidthNarrow) / 2.0, table.DesiredSize.Height + Gap,
            bankWidthNarrow, services.DesiredSize.Height));
        return finalSize;
    }
}
