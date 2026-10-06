using System.Globalization;
using System.Windows.Data;

namespace TrueCustomDifficulty.Infrastructure;

public sealed class ResponsiveCardWidthConverter : IValueConverter
{
    private const double ScrollbarAndChromeAllowance = 24.0;
    private const double CardGap = 8.0;
    private const double TwoColumnThreshold = 720.0;
    private const double ThreeColumnThreshold = 1180.0;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not double availableWidth || double.IsNaN(availableWidth) || availableWidth <= 0.0)
            return 360.0;

        var usableWidth = Math.Max(320.0, availableWidth - ScrollbarAndChromeAllowance);
        var mode = parameter as string;

        if (string.Equals(mode, "full", StringComparison.OrdinalIgnoreCase))
            return Math.Max(280.0, usableWidth - CardGap);

        var columns = usableWidth >= ThreeColumnThreshold
            ? 3
            : usableWidth >= TwoColumnThreshold
                ? 2
                : 1;

        return Math.Max(280.0, (usableWidth / columns) - CardGap);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
