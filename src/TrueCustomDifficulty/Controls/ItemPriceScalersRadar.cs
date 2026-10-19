using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Six independent item-value CFG editors on a compact common 0–2x vertical ruler.
/// The historical class name remains for XAML compatibility; Mutant Parts still
/// writes authored ItemPrototypes Cost rather than EconomyDifficulty category values.
/// </summary>
public sealed class ItemPriceScalersRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double BoardHeight = 330.0;
    private const double PlotTop = 105.0;
    private const double PlotHeight = 167.0;
    private static readonly (string Id, string Label)[] Items =
    {
        ("economy.consumableCost", "CONSUMABLES"),
        ("economy.ammoCost", "AMMO"),
        ("economy.armorCost", "ARMOR"),
        ("economy.weaponCost", "WEAPONS"),
        ("economy.artifactCost", "ARTIFACTS"),
        ("economy.mutantPartValue", "MUTANT PARTS")
    };
    private readonly ParameterViewModel?[] _items = new ParameterViewModel?[Items.Length];
    private readonly List<HitTarget> _hits = new();
    private int _selectedIndex;
    private int _hoverIndex = -1;
    private int _dragIndex = -1;

    public static readonly DependencyProperty ParametersProperty =
        DependencyProperty.Register(nameof(Parameters), typeof(IEnumerable),
            typeof(ItemPriceScalersRadar),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public ItemPriceScalersRadar()
    {
        Focusable = true;
        MinHeight = BoardHeight;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(double.IsInfinity(availableSize.Width) ? 740.0 :
            Math.Max(560.0, availableSize.Width), BoardHeight);

    private static void OnParametersChanged(DependencyObject sender,
        DependencyPropertyChangedEventArgs args) =>
        ((ItemPriceScalersRadar)sender).RebindParameters();

    private void RebindParameters()
    {
        foreach (var item in _items)
            if (item is not null) item.PropertyChanged -= ParameterChanged;
        Array.Clear(_items, 0, _items.Length);
        if (Parameters is not null)
        {
            var byId = Parameters.OfType<ParameterViewModel>()
                .ToDictionary(p => p.Id, StringComparer.Ordinal);
            for (var i = 0; i < Items.Length; i++)
            {
                byId.TryGetValue(Items[i].Id, out _items[i]);
                if (_items[i] is not null)
                    _items[i]!.PropertyChanged += ParameterChanged;
            }
        }
        InvalidateVisual();
    }

    private void ParameterChanged(object? sender, PropertyChangedEventArgs args)
    {
        if (args.PropertyName is nameof(ParameterViewModel.CurrentNumber)
            or nameof(ParameterViewModel.ReferenceNumber)
            or nameof(ParameterViewModel.IsModified))
            InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var text = FindBrush("TextBrush", Brushes.White);
        var muted = FindBrush("RadarMutedTextBrush", Brushes.Gray);
        var border = FindBrush("BorderSoftBrush", Brushes.DimGray);
        var accent = FindBrush("AccentBrush", Brushes.Goldenrod);
        var panel = FindBrush("PanelAltBrush", Brushes.Black);
        IndustrialChrome.DrawFrame(dc,
            new Rect(2, 2, Math.Max(0, ActualWidth - 4), Math.Max(0, ActualHeight - 4)),
            panel, border, accent, border, 8, accentEdge: true, technicalMarks: true);
        _hits.Clear();
        Center(dc, "ITEM VALUES", ActualWidth / 2.0, 21,
            VisualControlTypography.BoardTitle, accent, FontWeights.Bold);

        // Every live source has an audited 0–2x native range. All six columns
        // therefore share one REAL scale; never normalize each source to a
        // misleading unrelated ruler.
        const double rulerMax = 2.0;
        var plotLeft = 62.0;
        var plotRight = ActualWidth - 18.0;
        var usable = plotRight - plotLeft;
        var lane = usable / Items.Length;
        var chart = new Rect(plotLeft, PlotTop, usable, PlotHeight);
        for (var step = 0; step <= 4; step++)
        {
            var y = chart.Bottom - step * PlotHeight / 4.0;
            dc.DrawLine(new Pen(Clone(border, .48), .8),
                new Point(plotLeft, y), new Point(plotRight, y));
            var tick = Text($"{step * .5:0.#}x", VisualControlTypography.AxisTick, muted, FontWeights.SemiBold);
            dc.DrawText(tick, new Point(plotLeft - tick.Width - 9.0, y - tick.Height / 2));
        }
        var axis = new Point(12.0, chart.Top + chart.Height / 2);
        dc.PushTransform(new RotateTransform(-90, axis.X, axis.Y));
        Center(dc, "VALUE", axis.X, axis.Y, VisualControlTypography.AxisTitle, accent, FontWeights.Bold);
        dc.Pop();

        for (var i = 0; i < Items.Length; i++)
        {
            var left = plotLeft + i * lane;
            var center = left + lane / 2;
            dc.DrawRoundedRectangle(Clone(panel, .32),
                new Pen(Clone(border, .60), .8),
                new Rect(left + 3, 66, lane - 6, 252), 4, 4);
            var p = _items[i];
            if (p is null) continue;
            var enabled = p.IsControlEnabled;
            var barWidth = Math.Min(35, lane * .39);
            var barLeft = center - barWidth / 2;
            var max = Math.Max(p.Maximum, p.Minimum + Epsilon);
            var nativeTop = chart.Bottom - PlotHeight * Math.Clamp(max / rulerMax, 0, 1);
            var y = chart.Bottom - PlotHeight * Math.Clamp(p.CurrentNumber / rulerMax, 0, 1);
            var referenceY = chart.Bottom - PlotHeight * Math.Clamp(p.ReferenceNumber / rulerMax, 0, 1);
            dc.DrawRectangle(Clone(border, .12), null,
                new Rect(barLeft, nativeTop, barWidth, Math.Max(.5, chart.Bottom - nativeTop)));
            dc.DrawRectangle(Clone(enabled ? accent : muted, enabled ? .72 : .22), null,
                new Rect(barLeft, y, barWidth, Math.Max(.5, chart.Bottom - y)));
            dc.DrawLine(new Pen(Clone(muted, .68), 1.35),
                new Point(barLeft - 5, referenceY), new Point(barLeft + barWidth + 5, referenceY));
            VerticalBarHandleChrome.Draw(dc, new Point(center, y),
                barWidth, enabled ? accent : muted, enabled && _hoverIndex == i);
            Center(dc, $"{p.CurrentNumber:0.##}x", center, 84, 11.3,
                enabled && p.IsValueModifiedFromReference() ? accent : text,
                FontWeights.Bold);
            var words = Items[i].Label.Split(' ');
            if (words.Length == 1)
                Center(dc, words[0], center, 298, 9.1, text, FontWeights.Bold);
            else
            {
                Center(dc, words[0], center, 292, 9.1, text, FontWeights.Bold);
                Center(dc, string.Join(" ", words.Skip(1)), center, 305,
                    9.1, text, FontWeights.Bold);
            }
            var hit = new Rect(center - Math.Max(16, barWidth / 2 + 6),
                nativeTop, Math.Max(32, barWidth + 12), Math.Max(1, chart.Bottom - nativeTop));
            _hits.Add(new HitTarget(i, hit, chart.Bottom - PlotHeight,
                chart.Bottom));
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var hit = Find(e.GetPosition(this));
        if (hit is null || _items[hit.Index]?.IsControlEnabled != true) return;
        Focus();
        _selectedIndex = _dragIndex = hit.Index;
        CaptureMouse();
        SetAt(hit, e.GetPosition(this).Y);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var pos = e.GetPosition(this);
        var hit = Find(pos);
        var hovered = hit?.Index ?? -1;
        if (_hoverIndex != hovered)
        {
            _hoverIndex = hovered;
            InvalidateVisual();
        }
        Cursor = hit is not null ? Cursors.SizeNS : Cursors.Arrow;
        if (_dragIndex >= 0 && e.LeftButton == MouseButtonState.Pressed)
        {
            var target = _hits.FirstOrDefault(x => x.Index == _dragIndex);
            if (target is not null) SetAt(target, pos.Y);
            e.Handled = true;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragIndex < 0) return;
        _dragIndex = -1;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverIndex = -1;
        Cursor = Cursors.Arrow;
        InvalidateVisual();
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);
        var hit = Find(e.GetPosition(this));
        if (hit is null) return;
        _selectedIndex = hit.Index;
        _items[hit.Index]?.ResetToReference();
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.R)
        {
            foreach (var item in _items) item?.ResetToReference();
            InvalidateVisual();
            e.Handled = true;
            return;
        }
        var dir = e.Key switch
        {
            Key.Up or Key.Right or Key.Add or Key.OemPlus => 1,
            Key.Down or Key.Left or Key.Subtract or Key.OemMinus => -1,
            _ => 0
        };
        if (dir == 0 || _items[_selectedIndex] is not { IsControlEnabled: true } p)
            return;
        p.CurrentNumber = Math.Clamp(p.CurrentNumber + dir * p.TickFrequency,
            p.Minimum, p.Maximum);
        InvalidateVisual();
        e.Handled = true;
    }

    private void SetAt(HitTarget hit, double y)
    {
        var p = _items[hit.Index];
        if (p is null || !p.IsControlEnabled) return;
        var ratio = Math.Clamp((hit.Bottom - y) / Math.Max(1, hit.Bottom - hit.Top), 0, 1);
        var raw = p.Minimum + ratio * (p.Maximum - p.Minimum);
        var step = Math.Max(p.TickFrequency, Epsilon);
        p.CurrentNumber = Math.Clamp(p.Minimum +
            Math.Round((raw - p.Minimum) / step, MidpointRounding.AwayFromZero) * step,
            p.Minimum, p.Maximum);
        InvalidateVisual();
    }

    private HitTarget? Find(Point point) =>
        _hits.FirstOrDefault(hit => hit.Bounds.Contains(point));

    private static Brush FindBrush(string key, Brush fallback) =>
        Application.Current?.TryFindResource(key) as Brush ?? fallback;

    private static Brush Clone(Brush brush, double opacity)
    {
        var clone = brush.Clone();
        clone.Opacity = Math.Clamp(opacity, 0, 1);
        return clone;
    }

    private void Center(DrawingContext dc, string value, double x, double y,
        double size, Brush brush, FontWeight weight)
    {
        var formatted = Text(value, size, brush, weight);
        dc.DrawText(formatted, new Point(x - formatted.Width / 2, y - formatted.Height / 2));
    }

    private FormattedText Text(string value, double size, Brush brush, FontWeight weight) =>
        new(value, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal,
                weight, FontStretches.Normal), size, brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private sealed record HitTarget(int Index, Rect Bounds, double Top, double Bottom);
}
