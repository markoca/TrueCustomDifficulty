using System;
using System.Windows;
using System.Windows.Controls;

namespace TrueCustomDifficulty.Infrastructure;

/// <summary>
/// One responsive Economy editor: the TRADER STOCK board (with its AMMO /
/// MEDICAL SUPPLY rails) on the left; SERVICE PRICES and, under it, TRADER
/// RESTOCK on the right. Narrow windows stack both below the board.
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
        if (InternalChildren.Count != 3)
            throw new InvalidOperationException("Trader Economy requires the stock board, the service bank and the restock board.");

        var table = InternalChildren[0];
        var services = InternalChildren[1];
        var restock = InternalChildren[2];

        if (UseSideColumn(width))
        {
            var (tableWidth, bankWidth) = ColumnWidths(width);
            table.Measure(new Size(tableWidth, double.PositiveInfinity));
            services.Measure(new Size(bankWidth, double.PositiveInfinity));
            restock.Measure(new Size(bankWidth, double.PositiveInfinity));
            var right = services.DesiredSize.Height + Gap + restock.DesiredSize.Height;
            return new Size(width, Math.Max(table.DesiredSize.Height, right));
        }

        table.Measure(new Size(Math.Min(FallbackTableWidth, width), double.PositiveInfinity));
        services.Measure(new Size(Math.Min(FallbackBankWidth, width), double.PositiveInfinity));
        restock.Measure(new Size(Math.Min(FallbackBankWidth, width), double.PositiveInfinity));
        return new Size(width, table.DesiredSize.Height + Gap + services.DesiredSize.Height
            + Gap + restock.DesiredSize.Height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(1.0, finalSize.Width);
        var table = InternalChildren[0];
        var services = InternalChildren[1];
        var restock = InternalChildren[2];

        if (UseSideColumn(width))
        {
            var (tableWidth, bankWidth) = ColumnWidths(width);
            var left = Math.Max(0.0, (width - (tableWidth + Gap + bankWidth)) / 2.0);
            var right = left + tableWidth + Gap;
            table.Arrange(new Rect(left, 0.0, tableWidth, table.DesiredSize.Height));
            services.Arrange(new Rect(right, 0.0, bankWidth, services.DesiredSize.Height));
            restock.Arrange(new Rect(right, services.DesiredSize.Height + Gap, bankWidth, restock.DesiredSize.Height));
            return finalSize;
        }

        var tableWidthNarrow = Math.Min(FallbackTableWidth, width);
        var bankWidthNarrow = Math.Min(FallbackBankWidth, width);
        var bankLeft = (width - bankWidthNarrow) / 2.0;
        table.Arrange(new Rect((width - tableWidthNarrow) / 2.0, 0.0,
            tableWidthNarrow, table.DesiredSize.Height));
        var servicesTop = table.DesiredSize.Height + Gap;
        services.Arrange(new Rect(bankLeft, servicesTop, bankWidthNarrow, services.DesiredSize.Height));
        restock.Arrange(new Rect(bankLeft, servicesTop + services.DesiredSize.Height + Gap,
            bankWidthNarrow, restock.DesiredSize.Height));
        return finalSize;
    }
}
