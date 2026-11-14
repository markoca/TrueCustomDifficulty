using System;
using System.Windows;
using System.Windows.Controls;

namespace TrueCustomDifficulty.Infrastructure;

/// <summary>
/// A single responsive Economy row: original Trader Stock category table
/// beside portrait Stock Amount (2) and Service Prices (3) knob banks.
/// Parameters and CFG writer behavior are owned by their original models.
/// </summary>
public sealed class TraderEconomyPanel : Panel
{
    private const double Gap = 10.0;
    private const double KnobWidth = 204.0;
    private const double MinTableWidth = 340.0;
    private const double MaxTableWidth = 820.0;

    private static double UsableWidth(double width)
        => double.IsFinite(width) ? Math.Max(1.0, width) : 1120.0;

    private static bool UseThreeColumns(double width)
        => width >= MinTableWidth + 2 * KnobWidth + 2 * Gap;

    private static bool UseTwoKnobColumns(double width)
        => width >= 2 * 180.0 + Gap;

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = UsableWidth(availableSize.Width);
        if (InternalChildren.Count != 3)
            throw new InvalidOperationException("Trader Economy requires a table and two knob banks.");

        var table = InternalChildren[0];
        var stock = InternalChildren[1];
        var services = InternalChildren[2];
        if (UseThreeColumns(width))
        {
            var tableWidth = Math.Min(MaxTableWidth, width - 2 * KnobWidth - 2 * Gap);
            table.Measure(new Size(tableWidth, double.PositiveInfinity));
            stock.Measure(new Size(KnobWidth, double.PositiveInfinity));
            services.Measure(new Size(KnobWidth, double.PositiveInfinity));
            return new Size(width, Math.Max(table.DesiredSize.Height,
                Math.Max(stock.DesiredSize.Height, services.DesiredSize.Height)));
        }

        var narrowTableWidth = Math.Min(MaxTableWidth, width);
        table.Measure(new Size(narrowTableWidth, double.PositiveInfinity));
        var pair = UseTwoKnobColumns(width);
        var bankWidth = pair ? Math.Min(KnobWidth, (width - Gap) / 2.0)
            : Math.Min(KnobWidth, width);
        stock.Measure(new Size(bankWidth, double.PositiveInfinity));
        services.Measure(new Size(bankWidth, double.PositiveInfinity));
        var bankHeight = pair
            ? Math.Max(stock.DesiredSize.Height, services.DesiredSize.Height)
            : stock.DesiredSize.Height + Gap + services.DesiredSize.Height;
        return new Size(width, table.DesiredSize.Height + Gap + bankHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = Math.Max(1.0, finalSize.Width);
        var table = InternalChildren[0];
        var stock = InternalChildren[1];
        var services = InternalChildren[2];

        if (UseThreeColumns(width))
        {
            var tableWidth = Math.Min(MaxTableWidth, width - 2 * KnobWidth - 2 * Gap);
            var totalWidth = tableWidth + 2 * KnobWidth + 2 * Gap;
            var left = Math.Max(0.0, (width - totalWidth) / 2.0);
            table.Arrange(new Rect(left, 0.0, tableWidth, table.DesiredSize.Height));
            stock.Arrange(new Rect(left + tableWidth + Gap, 0.0,
                KnobWidth, stock.DesiredSize.Height));
            services.Arrange(new Rect(left + tableWidth + Gap + KnobWidth + Gap, 0.0,
                KnobWidth, services.DesiredSize.Height));
            return finalSize;
        }

        var narrowTableWidth = Math.Min(MaxTableWidth, width);
        table.Arrange(new Rect((width - narrowTableWidth) / 2.0, 0.0,
            narrowTableWidth, table.DesiredSize.Height));
        var top = table.DesiredSize.Height + Gap;
        if (UseTwoKnobColumns(width))
        {
            var bankWidth = Math.Min(KnobWidth, (width - Gap) / 2.0);
            var left = (width - (2 * bankWidth + Gap)) / 2.0;
            stock.Arrange(new Rect(left, top, bankWidth, stock.DesiredSize.Height));
            services.Arrange(new Rect(left + bankWidth + Gap, top,
                bankWidth, services.DesiredSize.Height));
        }
        else
        {
            var bankWidth = Math.Min(KnobWidth, width);
            var left = (width - bankWidth) / 2.0;
            stock.Arrange(new Rect(left, top, bankWidth, stock.DesiredSize.Height));
            services.Arrange(new Rect(left, top + stock.DesiredSize.Height + Gap,
                bankWidth, services.DesiredSize.Height));
        }
        return finalSize;
    }
}
