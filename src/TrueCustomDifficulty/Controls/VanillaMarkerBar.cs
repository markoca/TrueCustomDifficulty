using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Draws proportional vanilla/reference markers underneath a numeric slider.
/// Markers are clickable: selecting one snaps the owning slider to that exact value.
/// The parent view decides which parameter field (normal, choice, range MIN/MAX) receives it.
/// </summary>
public sealed class VanillaMarkerBar : FrameworkElement
{
    private readonly List<MarkerHitTarget> _hitTargets = new();
    private VanillaMarkerViewModel? _hoveredMarker;

    public VanillaMarkerBar()
    {
        IsEnabledChanged += HandleIsEnabledChanged;
    }

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(VanillaMarkerBar),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(VanillaMarkerBar),
        new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty MarkersProperty = DependencyProperty.Register(
        nameof(Markers), typeof(IEnumerable), typeof(VanillaMarkerBar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    public event EventHandler<VanillaMarkerClickedEventArgs>? MarkerClicked;

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public IEnumerable? Markers
    {
        get => (IEnumerable?)GetValue(MarkersProperty);
        set => SetValue(MarkersProperty, value);
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        _hitTargets.Clear();

        // A transparent surface makes the marker strip reliably hit-testable across the
        // slightly enlarged click targets while remaining visually unchanged. Empty space
        // still does nothing because OnMouseLeftButtonDown requires a marker hit target.
        dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize));

        if (Markers is null || ActualWidth <= 1 || Maximum <= Minimum)
            return;

        // Reference markers are intentionally low contrast, even on hover.
        // They stay clickable but never compete with the live slider value/handle.
        var markerBrush = TryFindResource("RadarMutedTextBrush") as Brush ?? Brushes.Gray;
        var textBrush = TryFindResource("RadarMutedTextBrush") as Brush ?? Brushes.Gray;
        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var typeface = new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

        // Keep extreme markers and labels inside the control bounds.
        const double edgePadding = 5d;
        var usableWidth = Math.Max(1d, ActualWidth - edgePadding * 2d);
        const double dotY = 3d;

        foreach (var item in Markers)
        {
            if (item is not VanillaMarkerViewModel marker)
                continue;

            var normalized = (marker.Value - Minimum) / (Maximum - Minimum);
            if (normalized < 0d || normalized > 1d)
                continue;

            var x = edgePadding + normalized * usableWidth;
            var dotBrush = markerBrush;
            dc.DrawEllipse(dotBrush, null, new Point(x, dotY), 2.1, 2.1);

            // Keep the connection to the slider subtle; the thumb/value should remain the primary focus.
            dc.DrawLine(new Pen(dotBrush, 0.7d), new Point(x, 0d), new Point(x, 1.5d));

            var markerTextBrush = IsEnabled && Equals(_hoveredMarker, marker) ? Brushes.White : textBrush;

            var text = new FormattedText(
                marker.Label,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                9.0d,
                markerTextBrush,
                dpi);

            var textX = Math.Clamp(x - text.Width / 2d, 0d, Math.Max(0d, ActualWidth - text.Width));
            var textPoint = new Point(textX, 6d);
            dc.DrawText(text, textPoint);

            // Make both the dot/stem and the visible label easy to click without turning the
            // whole marker bar into a hidden slider. Overlaps resolve to the closest marker X.
            var hitRect = new Rect(x - 7d, 0d, 14d, Math.Min(ActualHeight, 11d));
            hitRect.Union(new Rect(
                Math.Max(0d, textX - 3d),
                Math.Max(0d, textPoint.Y - 2d),
                Math.Min(ActualWidth, text.Width + 6d),
                Math.Max(0d, Math.Min(ActualHeight - Math.Max(0d, textPoint.Y - 2d), text.Height + 4d))));

            _hitTargets.Add(new MarkerHitTarget(marker, x, hitRect));
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var target = IsEnabled ? FindMarker(e.GetPosition(this)) : null;
        Cursor = target is not null ? Cursors.Hand : Cursors.Arrow;
        SetHoveredMarker(target?.Marker);
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        Cursor = Cursors.Arrow;
        SetHoveredMarker(null);
    }

    private void HandleIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (!IsEnabled)
        {
            Cursor = Cursors.Arrow;
            SetHoveredMarker(null);
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        if (!IsEnabled)
            return;

        var target = FindMarker(e.GetPosition(this));
        if (target is null)
            return;

        MarkerClicked?.Invoke(this, new VanillaMarkerClickedEventArgs(target.Marker));
        e.Handled = true;
    }

    private void SetHoveredMarker(VanillaMarkerViewModel? marker)
    {
        if (Equals(_hoveredMarker, marker))
            return;

        _hoveredMarker = marker;
        InvalidateVisual();
    }

    private MarkerHitTarget? FindMarker(Point point)
        => _hitTargets
            .Where(target => target.HitRect.Contains(point))
            .OrderBy(target => Math.Abs(point.X - target.CenterX))
            .FirstOrDefault();

    private sealed record MarkerHitTarget(VanillaMarkerViewModel Marker, double CenterX, Rect HitRect);
}

public sealed class VanillaMarkerClickedEventArgs : EventArgs
{
    public VanillaMarkerClickedEventArgs(VanillaMarkerViewModel marker) => Marker = marker;

    public VanillaMarkerViewModel Marker { get; }
    public double Value => Marker.Value;
}
