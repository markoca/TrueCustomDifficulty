using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// WPF version of the Localization Workbench industrial Zone chrome:
/// asymmetric chamfers, optional yellow accent rail, and subtle technical corner marks.
/// Intended for major surfaces only, not for every settings row.
/// </summary>
public sealed class IndustrialFrame : Decorator
{
    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
        nameof(Background),
        typeof(Brush),
        typeof(IndustrialFrame),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty BorderBrushProperty = DependencyProperty.Register(
        nameof(BorderBrush),
        typeof(Brush),
        typeof(IndustrialFrame),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
        nameof(AccentBrush),
        typeof(Brush),
        typeof(IndustrialFrame),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TechnicalMarkBrushProperty = DependencyProperty.Register(
        nameof(TechnicalMarkBrush),
        typeof(Brush),
        typeof(IndustrialFrame),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty AccentEdgeProperty = DependencyProperty.Register(
        nameof(AccentEdge),
        typeof(bool),
        typeof(IndustrialFrame),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TechnicalMarksProperty = DependencyProperty.Register(
        nameof(TechnicalMarks),
        typeof(bool),
        typeof(IndustrialFrame),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ChamferSizeProperty = DependencyProperty.Register(
        nameof(ChamferSize),
        typeof(double),
        typeof(IndustrialFrame),
        new FrameworkPropertyMetadata(8.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty PaddingProperty = DependencyProperty.Register(
        nameof(Padding),
        typeof(Thickness),
        typeof(IndustrialFrame),
        new FrameworkPropertyMetadata(new Thickness(0), FrameworkPropertyMetadataOptions.AffectsMeasure));

    public Brush Background
    {
        get => (Brush)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public Brush BorderBrush
    {
        get => (Brush)GetValue(BorderBrushProperty);
        set => SetValue(BorderBrushProperty, value);
    }

    public Brush AccentBrush
    {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    public Brush TechnicalMarkBrush
    {
        get => (Brush)GetValue(TechnicalMarkBrushProperty);
        set => SetValue(TechnicalMarkBrushProperty, value);
    }

    public bool AccentEdge
    {
        get => (bool)GetValue(AccentEdgeProperty);
        set => SetValue(AccentEdgeProperty, value);
    }

    public bool TechnicalMarks
    {
        get => (bool)GetValue(TechnicalMarksProperty);
        set => SetValue(TechnicalMarksProperty, value);
    }

    public double ChamferSize
    {
        get => (double)GetValue(ChamferSizeProperty);
        set => SetValue(ChamferSizeProperty, value);
    }

    public Thickness Padding
    {
        get => (Thickness)GetValue(PaddingProperty);
        set => SetValue(PaddingProperty, value);
    }

    protected override Size MeasureOverride(Size constraint)
    {
        if (Child is null)
            return new Size(Padding.Left + Padding.Right, Padding.Top + Padding.Bottom);

        var childConstraint = new Size(
            Math.Max(0.0, constraint.Width - Padding.Left - Padding.Right),
            Math.Max(0.0, constraint.Height - Padding.Top - Padding.Bottom));

        Child.Measure(childConstraint);

        return new Size(
            Child.DesiredSize.Width + Padding.Left + Padding.Right,
            Child.DesiredSize.Height + Padding.Top + Padding.Bottom);
    }

    protected override Size ArrangeOverride(Size arrangeSize)
    {
        Child?.Arrange(new Rect(
            Padding.Left,
            Padding.Top,
            Math.Max(0.0, arrangeSize.Width - Padding.Left - Padding.Right),
            Math.Max(0.0, arrangeSize.Height - Padding.Top - Padding.Bottom)));

        return arrangeSize;
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        IndustrialChrome.DrawFrame(
            dc,
            new Rect(0.5, 0.5, Math.Max(0.0, ActualWidth - 1.0), Math.Max(0.0, ActualHeight - 1.0)),
            Background,
            BorderBrush,
            AccentBrush,
            TechnicalMarkBrush,
            ChamferSize,
            AccentEdge,
            TechnicalMarks);
    }
}

internal static class IndustrialChrome
{
    // Recessed neutral surface: bounded entirely inside the existing card.
    public static void DrawRecessedCard(
        DrawingContext dc, Rect rect, Brush fill, Brush border, double radius = 4.0)
    {
        dc.DrawRoundedRectangle(fill, new Pen(border, 1.0), rect, radius, radius);
        if (rect.Width < 40.0 || rect.Height < 30.0)
            return;

        var shadow = new Pen(new SolidColorBrush(Color.FromArgb(62, 0, 0, 0)), 1.0);
        var rimBrush = border.CloneCurrentValue();
        rimBrush.Opacity *= .55;
        rimBrush.Freeze();
        var rim = new Pen(rimBrush, 1.0);
        dc.DrawLine(shadow,
            new Point(rect.Left + 5.0, rect.Top + 1.5),
            new Point(rect.Right - 5.0, rect.Top + 1.5));
        dc.DrawLine(rim,
            new Point(rect.Left + 5.0, rect.Bottom - 1.5),
            new Point(rect.Right - 5.0, rect.Bottom - 1.5));
    }

    // Tiny neutral two-edge engraving between genuinely separate controls.
    // Draw only inside an existing gap: no new rows or hit targets.
    public static void DrawEngravedSeparator(
        DrawingContext dc, double left, double right, double y, Brush muted)
    {
        if (right - left < 40.0)
            return;

        var dark = new Pen(new SolidColorBrush(Color.FromArgb(100, 0, 0, 0)), 1.0);
        var lightBrush = muted.CloneCurrentValue();
        lightBrush.Opacity *= 0.20;
        lightBrush.Freeze();
        var light = new Pen(lightBrush, 0.8);
        dc.DrawLine(dark, new Point(left, y), new Point(right, y));
        dc.DrawLine(light, new Point(left, y + 1.3), new Point(right, y + 1.3));
    }

    public static void DrawFrame(
        DrawingContext dc,
        Rect rect,
        Brush background,
        Brush borderBrush,
        Brush accentBrush,
        Brush technicalMarkBrush,
        double chamfer = 8.0,
        bool accentEdge = false,
        bool technicalMarks = true)
    {
        if (rect.Width <= 1.0 || rect.Height <= 1.0)
            return;

        var cut = Math.Max(0.0, Math.Min(chamfer, Math.Min(rect.Width, rect.Height) / 2.0));
        var geometry = new StreamGeometry();

        using (var context = geometry.Open())
        {
            context.BeginFigure(new Point(rect.Left, rect.Top), isFilled: true, isClosed: true);
            context.LineTo(new Point(rect.Right - cut, rect.Top), true, false);
            context.LineTo(new Point(rect.Right, rect.Top + cut), true, false);
            context.LineTo(new Point(rect.Right, rect.Bottom), true, false);
            context.LineTo(new Point(rect.Left + cut, rect.Bottom), true, false);
            context.LineTo(new Point(rect.Left, rect.Bottom - cut), true, false);
        }

        geometry.Freeze();
        dc.DrawGeometry(background, new Pen(borderBrush, 1.0), geometry);

        if (accentEdge)
        {
            dc.DrawLine(
                new Pen(accentBrush, 3.0),
                new Point(rect.Left + 1.5, rect.Top + cut + 5.0),
                new Point(rect.Left + 1.5, rect.Bottom - cut - 5.0));
        }

        if (!technicalMarks)
            return;

        var markLength = Math.Min(18.0, Math.Max(8.0, rect.Width / 12.0));
        var technicalPen = new Pen(technicalMarkBrush, 1.0);

        dc.DrawLine(
            technicalPen,
            new Point(Math.Max(rect.Left, rect.Right - cut - markLength), rect.Top + 3.0),
            new Point(rect.Right - cut - 3.0, rect.Top + 3.0));

        dc.DrawLine(
            technicalPen,
            new Point(rect.Left + cut + 3.0, rect.Bottom - 3.0),
            new Point(Math.Min(rect.Right, rect.Left + cut + markLength), rect.Bottom - 3.0));
    }
}

/// <summary>
/// Passive source-native ruler graduations for compact, hand-drawn controls.
/// No new thresholds, Base highlights or hit targets.
/// </summary>
internal static class IndustrialRulerChrome
{
    // The rail sheen follows the real current fill only, never the reference.
    // It is painted inside the existing track before handles and source ticks.
    public static void DrawActiveRailGlint(
        DrawingContext dc, double fromX, double toX, double y,
        Brush accent, bool enabled)
    {
        if (!enabled || Math.Abs(toX - fromX) < 2.0)
            return;
        var glow = accent.CloneCurrentValue();
        glow.Opacity *= 0.27;
        glow.Freeze();
        dc.DrawLine(new Pen(glow, 0.9),
            new Point(fromX, y - 1.15), new Point(toX, y - 1.15));
    }

    public static void DrawNativeTicks(
        DrawingContext dc, Rect track, double minimum, double maximum,
        double nativeStep, Brush muted)
    {
        if (track.Width < 64.0 || nativeStep <= 0.0 || maximum <= minimum)
            return;

        var intervals = (maximum - minimum) / nativeStep;
        if (!double.IsFinite(intervals) || intervals < 4.0)
            return;

        var tickBrush = muted.CloneCurrentValue();
        tickBrush.Opacity *= 0.38;
        tickBrush.Freeze();
        var pen = new Pen(tickBrush, 0.9);
        pen.Freeze();
        var y = track.Top + track.Height / 2.0;
        var lastX = track.Left;

        for (var marker = 1; marker < 4; marker++)
        {
            // Pick a real native step near each quarter, not a new CFG tier.
            var stepIndex = Math.Round(intervals * marker / 4.0);
            var sourceValue = Math.Min(maximum, minimum + stepIndex * nativeStep);
            var x = track.Left + (sourceValue - minimum) / (maximum - minimum) * track.Width;
            if (x - lastX < 5.0 || track.Right - x < 5.0)
                continue;

            dc.DrawLine(pen, new Point(x, y + 4.5), new Point(x, y + 7.5));
            lastX = x;
        }
    }
}
