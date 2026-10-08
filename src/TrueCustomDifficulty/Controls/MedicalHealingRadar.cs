using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Direct medical healing editors and the ordinary-food health switch.
/// The legacy class name remains for the existing XAML template.
/// All five controls operate on their original CFG-backed parameters;
/// this board deliberately contains no read-only preview/chart.
/// </summary>
public sealed class MedicalHealingRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double BoardHeight = 400.0;
    private const double CardHeight = 89.0;
    private const double Gap = 10.0;
    private const string FoodHealingId = "health.foodHealing";

    private static readonly (string Id, string Label)[] Items =
    {
        ("health.bandageHealing", "BANDAGE"),
        ("health.medkitHealing", "MEDKIT"),
        ("health.armyMedkitHealing", "ARMY MEDKIT"),
        ("health.ecoMedkitHealing", "SCIENTIFIC MEDKIT")
    };

    private readonly ParameterViewModel?[] _medical = new ParameterViewModel?[4];
    private ParameterViewModel? _foodHealing;
    private readonly List<HitArea> _hits = new();
    private int _selected = 0;
    private int _dragRow = -1;
    private HitAction _hoverAction = HitAction.Select;
    private int _hoverRow = -2;

    public static readonly DependencyProperty ParametersProperty =
        DependencyProperty.Register(nameof(Parameters), typeof(IEnumerable),
            typeof(MedicalHealingRadar),
            new FrameworkPropertyMetadata(null,
                FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public MedicalHealingRadar()
    {
        Focusable = true;
        MinHeight = BoardHeight;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
        => new(double.IsInfinity(availableSize.Width)
            ? 720.0 : Math.Max(560.0, availableSize.Width), BoardHeight);

    private static void OnParametersChanged(DependencyObject d,
        DependencyPropertyChangedEventArgs e)
        => ((MedicalHealingRadar)d).RebindParameters();

    private void RebindParameters()
    {
        foreach (var p in _medical)
            if (p is not null) p.PropertyChanged -= ParameterChanged;
        if (_foodHealing is not null)
            _foodHealing.PropertyChanged -= ParameterChanged;

        Array.Clear(_medical, 0, _medical.Length);
        _foodHealing = null;

        var byId = Parameters?.OfType<ParameterViewModel>()
            .ToDictionary(p => p.Id, StringComparer.Ordinal);
        if (byId is not null)
        {
            for (var i = 0; i < Items.Length; i++)
                byId.TryGetValue(Items[i].Id, out _medical[i]);
            byId.TryGetValue(FoodHealingId, out _foodHealing);
        }

        foreach (var p in _medical)
            if (p is not null) p.PropertyChanged += ParameterChanged;
        if (_foodHealing is not null)
            _foodHealing.PropertyChanged += ParameterChanged;
        InvalidateVisual();
    }

    private void ParameterChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ParameterViewModel.CurrentNumber)
            or nameof(ParameterViewModel.CurrentBoolean)
            or nameof(ParameterViewModel.ReferenceNumber)
            or nameof(ParameterViewModel.IsModified))
            InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        var text = FindBrush("TextBrush", Brushes.WhiteSmoke);
        var muted = FindBrush("RadarMutedTextBrush", Brushes.Gray);
        var border = FindBrush("BorderSoftBrush", Brushes.DimGray);
        var accent = FindBrush("AccentBrush", Brushes.Goldenrod);
        var panel = FindBrush("PanelAltBrush", Brushes.Black);

        IndustrialChrome.DrawFrame(dc,
            new Rect(2, 2, Math.Max(0, ActualWidth - 4), Math.Max(0, ActualHeight - 4)),
            panel, border, accent, border, 8, accentEdge: true, technicalMarks: true);

        _hits.Clear();
        DrawCenteredText(dc, "MEDICAL HEALING", new Point(ActualWidth / 2, 21),
            12.5, accent, FontWeights.Bold);

        var width = Math.Max(560.0, ActualWidth);
        var contentWidth = width - 32.0;
        var bandageWidth = contentWidth * .66;
        DrawMedicalCard(dc, new Rect(16, 48, bandageWidth - Gap, CardHeight),
            0, text, muted, border, accent, panel);

        var food = new Rect(16 + bandageWidth, 48, contentWidth - bandageWidth, CardHeight);
        dc.DrawRoundedRectangle(panel, new Pen(border, 1), food, 4, 4);
        DrawText(dc, "FOOD HEALING", new Point(food.Left + 12, food.Top + 12),
            10.3, text, FontWeights.Bold);
        var state = _foodHealing?.CurrentBoolean == true;
        var box = new Rect(food.Right - 42, food.Top + 41, 24, 24);
        dc.DrawRoundedRectangle(panel,
            new Pen(_foodHealing?.IsValueModifiedFromReference() == true ? accent : muted, 1.7),
            box, 3, 3);
        if (state)
        {
            dc.DrawLine(new Pen(text, 2.2),
                new Point(box.Left + 5, box.Top + 12),
                new Point(box.Left + 10, box.Top + 17));
            dc.DrawLine(new Pen(text, 2.2),
                new Point(box.Left + 10, box.Top + 17),
                new Point(box.Left + 19, box.Top + 6));
        }
        DrawText(dc, state ? "ON" : "OFF",
            new Point(food.Left + 12, food.Top + 46), 11, text, FontWeights.Bold);
        _hits.Add(new HitArea(food, -1, HitAction.Food));

        DrawText(dc, "MEDKIT HEALING", new Point(18, 157), 10.8, text, FontWeights.Bold);
        DrawText(dc, "HP RESTORED", new Point(18, 178), 9, muted, FontWeights.SemiBold);
        // One common HP scale makes the three medkits directly comparable.
        // Grey ticks retain each item's individual authored vanilla value.
        const double chartTop = 210.0;
        const double chartHeight = 136.0;
        const double chartMaximum = 200.0;
        var chartLeft = 46.0;
        var chartWidth = width - 65.0;
        for (var tick = 0; tick <= 200; tick += 50)
        {
            var y = chartTop + chartHeight * (1.0 - tick / chartMaximum);
            dc.DrawLine(new Pen(CloneWithOpacity(border, .55), .8),
                new Point(chartLeft, y), new Point(chartLeft + chartWidth, y));
            DrawRightAlignedText(dc, $"{tick}", new Point(chartLeft - 7, y - 7),
                8.4, muted, FontWeights.Normal);
        }
        var columnWidth = chartWidth / 3.0;
        for (var i = 1; i < Items.Length; i++)
        {
            var p = _medical[i];
            if (p is null) continue;
            var centerX = chartLeft + (i - .5) * columnWidth;
            var barWidth = Math.Min(72.0, columnWidth * .45);
            var barBottom = chartTop + chartHeight;
            var barTop = chartTop + chartHeight *
                (1.0 - Math.Clamp(p.CurrentNumber / chartMaximum, 0, 1));
            var vanillaY = chartTop + chartHeight *
                (1.0 - Math.Clamp(p.ReferenceNumber / chartMaximum, 0, 1));
            dc.DrawRectangle(CloneWithOpacity(accent, .65), null,
                new Rect(centerX - barWidth / 2, barTop,
                    barWidth, Math.Max(0.5, barBottom - barTop)));
            dc.DrawLine(new Pen(muted, 2.4),
                new Point(centerX - barWidth / 2 - 11, vanillaY),
                new Point(centerX + barWidth / 2 + 11, vanillaY));
            var numeric = $"{p.CurrentNumber:0.#}";
            var label = CreateText(numeric + " HP", 12.2, text, FontWeights.Bold);
            if (p.IsValueModifiedFromReference())
            {
                label.SetForegroundBrush(accent, 0, numeric.Length);
                label.SetFontSize(13.4, 0, numeric.Length);
            }
            dc.DrawText(label, new Point(centerX - label.Width / 2, chartTop - 23));
            DrawCenteredText(dc, Items[i].Label, new Point(centerX, 366),
                9.8, text, FontWeights.Bold);
            _hits.Add(new HitArea(new Rect(centerX - columnWidth / 2 + 5,
                chartTop, columnWidth - 10, chartHeight), i, HitAction.Chart));
        }
    }

    private void DrawMedicalCard(DrawingContext dc, Rect card, int row,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        var selected = _selected == row;
        var p = _medical[row];
        if (p is null) return;

        // One readable dosage editor per item: large HP output, a restrained
        // reference mark and one horizontal interaction. No separate preview.
        // Selection must not paint the medical lane yellow.
        dc.DrawRoundedRectangle(panel, new Pen(border, .8), card, 4, 4);
        DrawText(dc, Items[row].Label, new Point(card.Left + 12, card.Top + 10),
            10.3, text, FontWeights.Bold);
        var modified = p.IsValueModifiedFromReference();
        DrawRightAlignedText(dc, $"{p.CurrentNumber:0.#} HP",
            new Point(card.Right - 12, card.Top + 8),
            13.2, modified ? accent : text, FontWeights.Bold);

        var track = new Rect(card.Left + 17, card.Top + 37,
            card.Width - 34, 25);
        var y = track.Top + track.Height / 2;
        var range = Math.Max(Epsilon, p.Maximum - p.Minimum);
        var currentX = track.Left + track.Width *
            Math.Clamp((p.CurrentNumber - p.Minimum) / range, 0, 1);
        var referenceX = track.Left + track.Width *
            Math.Clamp((p.ReferenceNumber - p.Minimum) / range, 0, 1);
        dc.DrawLine(new Pen(border, 4),
            new Point(track.Left, y), new Point(track.Right, y));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, .74), 4),
            new Point(track.Left, y), new Point(currentX, y));

        // Source reference remains a subtle landmark rather than a second
        // output bar. Small divisions make HP fine-tuning easier to orient.
        for (var step = 1; step < 5; step++)
        {
            var tickX = track.Left + track.Width * step / 5.0;
            dc.DrawLine(new Pen(CloneWithOpacity(muted, .35), 1),
                new Point(tickX, y - 4), new Point(tickX, y + 4));
        }
        dc.DrawLine(new Pen(muted, 1.8),
            new Point(referenceX, y - 11), new Point(referenceX, y + 11));
        dc.DrawEllipse(panel, new Pen(accent, 1.9),
            new Point(currentX, y), 7, 7);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.7, 2.7);
        DrawText(dc, $"{p.Minimum:0.#}", new Point(track.Left, card.Bottom - 18),
            9, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, $"{p.Maximum:0.#} HP",
            new Point(track.Right, card.Bottom - 18),
            9, muted, FontWeights.Normal);
        _hits.Add(new HitArea(new Rect(track.Left - 5, track.Top - 4,
            track.Width + 10, track.Height + 8), row, HitAction.Slider));

    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        var hit = FindHit(e.GetPosition(this));
        if (hit is null) return;
        Focus();
        if (hit.Row >= 0) _selected = hit.Row;
        if (hit.Action is HitAction.Slider or HitAction.Chart)
        {
            _dragRow = hit.Row;
            CaptureMouse();
            UpdateSlider(hit.Row, e.GetPosition(this).X, e.GetPosition(this).Y);
        }
        else
            Apply(hit);
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);
        var hit = FindHit(e.GetPosition(this));
        if (hit is null) return;
        if (hit.Row == -1) _foodHealing?.ResetToReference();
        else
        {
            _selected = hit.Row;
            _medical[hit.Row]?.ResetToReference();
        }
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var hit = FindHit(e.GetPosition(this));
        var row = hit?.Row ?? -2;
        var action = hit?.Action ?? HitAction.Select;
        if (_hoverRow != row || _hoverAction != action)
        {
            _hoverRow = row;
            _hoverAction = action;
            InvalidateVisual();
        }
        Cursor = hit is not null && action != HitAction.Select
            ? Cursors.Hand : Cursors.Arrow;
        if (_dragRow >= 0 && e.LeftButton == MouseButtonState.Pressed)
        {
            UpdateSlider(_dragRow, e.GetPosition(this).X, e.GetPosition(this).Y);
            e.Handled = true;
        }
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);
        if (_dragRow < 0) return;
        _dragRow = -1;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverRow = -2;
        _hoverAction = HitAction.Select;
        Cursor = Cursors.Arrow;
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Key == Key.R)
        {
            foreach (var medicalItem in _medical) medicalItem?.ResetToReference();
            _foodHealing?.ResetToReference();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var p = _medical[_selected];
        if (p is null) return;
        var dir = e.Key switch
        {
            Key.Up or Key.Right or Key.Add or Key.OemPlus => 1,
            Key.Down or Key.Left or Key.Subtract or Key.OemMinus => -1,
            _ => 0
        };
        if (dir == 0) return;
        SetSnapped(p, p.CurrentNumber + dir * p.TickFrequency);
        InvalidateVisual();
        e.Handled = true;
    }

    private void Apply(HitArea hit)
    {
        if (hit.Action == HitAction.Food)
        {
            if (_foodHealing is not null && _foodHealing.IsControlEnabled)
                _foodHealing.CurrentBoolean = !_foodHealing.CurrentBoolean;
        }
        else if (hit.Row >= 0 && _medical[hit.Row] is { } p
            && p.IsControlEnabled)
        {
            if (hit.Action == HitAction.Slider)
                _selected = hit.Row;
        }
        InvalidateVisual();
    }

    private void UpdateSlider(int row, double x, double y)
    {
        if (row < 0 || row >= _medical.Length ||
            _medical[row] is not { } parameter || !parameter.IsControlEnabled)
            return;
        var hit = _hits.FirstOrDefault(area =>
            area.Row == row && (area.Action == HitAction.Slider || area.Action == HitAction.Chart));
        if (hit is null) return;
        var trackLeft = hit.Bounds.Left + 5;
        var trackWidth = hit.Bounds.Width - 10;
        var ratio = hit.Action == HitAction.Chart
            ? Math.Clamp(1.0 - (y - hit.Bounds.Top) / hit.Bounds.Height, 0, 1)
            : Math.Clamp((x - trackLeft) / Math.Max(1, trackWidth), 0, 1);
        SetSnapped(parameter, parameter.Minimum +
            ratio * (parameter.Maximum - parameter.Minimum));
        InvalidateVisual();
    }

    private HitArea? FindHit(Point point)
    {
        // Button hits are appended after their card selector and take priority.
        for (var i = _hits.Count - 1; i >= 0; i--)
            if (_hits[i].Bounds.Contains(point)) return _hits[i];
        return null;
    }

    private static void SetSnapped(ParameterViewModel p, double value)
        => p.CurrentNumber = Snap(p, value);

    private static double Snap(ParameterViewModel p, double value)
    {
        var step = Math.Max(p.TickFrequency, Epsilon);
        var snapped = p.Minimum + Math.Round((value - p.Minimum) / step,
            MidpointRounding.AwayFromZero) * step;
        return Math.Clamp(snapped, p.Minimum, p.Maximum);
    }

    private static Brush FindBrush(string key, Brush fallback)
        => Application.Current?.TryFindResource(key) as Brush ?? fallback;

    private static Brush CloneWithOpacity(Brush brush, double opacity)
    {
        var clone = brush.Clone();
        clone.Opacity = Math.Clamp(opacity, 0, 1);
        return clone;
    }

    private void DrawText(DrawingContext dc, string value, Point point,
        double size, Brush brush, FontWeight weight)
        => dc.DrawText(CreateText(value, size, brush, weight), point);

    private void DrawCenteredText(DrawingContext dc, string value, Point center,
        double size, Brush brush, FontWeight weight)
    {
        var formatted = CreateText(value, size, brush, weight);
        dc.DrawText(formatted,
            new Point(center.X - formatted.Width / 2,
                center.Y - formatted.Height / 2));
    }

    private void DrawRightAlignedText(DrawingContext dc, string value,
        Point right, double size, Brush brush, FontWeight weight)
    {
        var formatted = CreateText(value, size, brush, weight);
        dc.DrawText(formatted, new Point(right.X - formatted.Width, right.Y));
    }

    private FormattedText CreateText(string value, double size,
        Brush brush, FontWeight weight)
        => new(value, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal,
                weight, FontStretches.Normal), Math.Max(9, size), brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private sealed record HitArea(Rect Bounds, int Row, HitAction Action);

    private enum HitAction { Select, Food, Slider, Chart }
}
