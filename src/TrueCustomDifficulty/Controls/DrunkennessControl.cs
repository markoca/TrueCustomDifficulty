using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Player-facing editor for the DrunknessMechanics 20 / 40 / 60 thresholds.
/// Beer/Vodka is a presentation-only reference. The single backing parameter
/// scales all thresholds together, preserving their authored progression.
/// </summary>
public sealed class DrunkennessControl : FrameworkElement
{
    private const string ParameterId = "survival.drunknessThresholdScale";
    private const double VanillaMedium = 20.0;
    private const double VanillaStrong = 40.0;
    private const double VanillaVeryStrong = 60.0;
    private const double BeerDrunkness = 15.0;
    private const double VodkaDrunkness = 30.0;
    private const double MinimumScale = 0.25;
    private const double MaximumScale = 1.50;

    private ParameterViewModel? _parameter;
    private DrinkReference _reference = DrinkReference.Vodka;
    private int _previewDrinks = 1;
    private Rect _vodkaRect;
    private Rect _beerRect;
    private Rect _sliderRect;
    private readonly Dictionary<int, Rect> _previewRects = new();
    private int _hoverPreview = -1;
    private DrinkReference? _hoverReference;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(DrunkennessControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public DrunkennessControl()
    {
        Focusable = true;
        MinHeight = 350;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 720.0 : Math.Max(520.0, availableSize.Width);
        return new Size(width, 350.0);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((DrunkennessControl)d).RebindParameter();

    private void RebindParameter()
    {
        if (_parameter is not null)
            _parameter.PropertyChanged -= Parameter_PropertyChanged;

        _parameter = Parameters?
            .OfType<ParameterViewModel>()
            .FirstOrDefault(x => x.Id.Equals(ParameterId, StringComparison.Ordinal));

        if (_parameter is not null)
            _parameter.PropertyChanged += Parameter_PropertyChanged;

        InvalidateVisual();
    }

    private void Parameter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ParameterViewModel.CurrentNumber)
            or nameof(ParameterViewModel.IsModified)
            or nameof(ParameterViewModel.CurrentDisplay))
        {
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var panel = BrushResource("PanelBrush", new SolidColorBrush(Color.FromRgb(23, 25, 22)));
        var panelAlt = BrushResource("PanelAltBrush", new SolidColorBrush(Color.FromRgb(32, 35, 31)));
        var border = BrushResource("BorderSoftBrush", new SolidColorBrush(Color.FromRgb(44, 48, 42)));
        var text = BrushResource("TextBrush", Brushes.WhiteSmoke);
        var muted = BrushResource("MutedTextBrush", Brushes.Gray);
        var radarMuted = BrushResource("RadarMutedTextBrush", Brushes.LightGray);
        var accent = BrushResource("AccentBrush", Brushes.Gold);
        var accentDark = BrushResource("AccentDarkBrush", Brushes.DarkGoldenrod);
        var track = BrushResource("SliderTrackBrush", new SolidColorBrush(Color.FromRgb(34, 37, 31)));

        var bounds = new Rect(0.5, 0.5, Math.Max(0.0, ActualWidth - 1.0), Math.Max(0.0, ActualHeight - 1.0));
        dc.DrawRoundedRectangle(panel, new Pen(border, 1.0), bounds, 4.0, 4.0);

        DrawText(dc, "DRINKS TO GET VERY DRUNK", new Point(16, 13), 12.0, accent, FontWeights.Bold);
        DrawText(dc, "REFERENCE", new Point(16, 43), 9.0, muted, FontWeights.Bold);

        _vodkaRect = new Rect(82, 36, 72, 25);
        _beerRect = new Rect(160, 36, 62, 25);
        DrawSelector(dc, _vodkaRect, "VODKA", _reference == DrinkReference.Vodka, _hoverReference == DrinkReference.Vodka, panelAlt, border, accent, text);
        DrawSelector(dc, _beerRect, "BEER", _reference == DrinkReference.Beer, _hoverReference == DrinkReference.Beer, panelAlt, border, accent, text);

        var scale = CurrentScale;
        var strength = ReferenceStrength;
        var requiredDrinks = VanillaVeryStrong * scale / strength;
        DrawText(dc, "VERY STRONG AFTER", new Point(250, 43), 9.0, muted, FontWeights.Bold);
        DrawText(dc, FormatDrinks(requiredDrinks), new Point(360, 37), 18.0, text, FontWeights.Bold);

        var sliderLeft = 30.0;
        var sliderRight = Math.Max(sliderLeft + 220.0, ActualWidth - 30.0);
        var sliderY = 91.0;
        _sliderRect = new Rect(sliderLeft, sliderY - 15.0, sliderRight - sliderLeft, 34.0);

        dc.DrawLine(new Pen(track, 3.0), new Point(sliderLeft, sliderY), new Point(sliderRight, sliderY));

        var maxDrinks = MaxReferenceDrinks;
        for (var i = 1; i <= maxDrinks; i++)
        {
            var x = DrinkToX(i, sliderLeft, sliderRight, maxDrinks);
            dc.DrawLine(new Pen(border, 1.0), new Point(x, sliderY - 5), new Point(x, sliderY + 5));
            DrawCenteredText(dc, i.ToString(CultureInfo.InvariantCulture), new Point(x, sliderY + 9), 9.0, muted, FontWeights.SemiBold);
        }

        var handleX = DrinkToX(requiredDrinks, sliderLeft, sliderRight, maxDrinks);
        dc.DrawEllipse(accent, null, new Point(handleX, sliderY), 5.0, 5.0);

        var vanillaDrinks = VanillaVeryStrong / strength;
        var vanillaX = DrinkToX(vanillaDrinks, sliderLeft, sliderRight, maxDrinks);
        DrawCenteredText(dc, "VANILLA", new Point(vanillaX, sliderY + 25), 9.0, accentDark, FontWeights.Bold);

        var medium = VanillaMedium * scale;
        var strong = VanillaStrong * scale;
        var veryStrong = VanillaVeryStrong * scale;
        DrawText(
            dc,
            $"THRESHOLDS  MEDIUM {medium:0.#}   |   STRONG {strong:0.#}   |   VERY STRONG {veryStrong:0.#}",
            new Point(16, 128),
            9.5,
            radarMuted,
            FontWeights.SemiBold);

        DrawText(dc, "BACK-TO-BACK PREVIEW", new Point(16, 158), 9.0, muted, FontWeights.Bold);
        _previewRects.Clear();
        var chipX = 145.0;
        for (var i = 1; i <= maxDrinks; i++)
        {
            var rect = new Rect(chipX, 151, 34, 24);
            _previewRects[i] = rect;
            var selected = _previewDrinks == i;
            var hovered = _hoverPreview == i;
            dc.DrawRoundedRectangle(
                selected ? accentDark : panelAlt,
                new Pen(hovered ? accent : border, 1.0),
                rect,
                3.0,
                3.0);
            DrawCenteredText(dc, i.ToString(CultureInfo.InvariantCulture), new Point(rect.Left + rect.Width / 2.0, rect.Top + 5), 10.0, selected || hovered ? Brushes.White : radarMuted, FontWeights.SemiBold);
            chipX += 39.0;
        }

        var previewPoints = _previewDrinks * strength;
        var state = GetState(previewPoints, medium, strong, veryStrong);
        DrawText(
            dc,
            $"{_previewDrinks} {ReferenceWord(_previewDrinks)}  ->  {StateLabel(state)}",
            new Point(Math.Max(chipX + 8.0, 430.0), 155),
            11.0,
            text,
            FontWeights.Bold);

        DrawTierCards(dc, state, medium, strong, veryStrong, panelAlt, border, muted, accent, text);

        DrawText(
            dc,
            "Preview assumes drinks are stacked before natural sober-up. BEER/VODKA selection does not change gameplay by itself.",
            new Point(16, ActualHeight - 22),
            9.0,
            muted,
            FontWeights.Normal);
    }

    private void DrawTierCards(
        DrawingContext dc,
        DrunkState activeState,
        double medium,
        double strong,
        double veryStrong,
        Brush panelAlt,
        Brush border,
        Brush muted,
        Brush accent,
        Brush text)
    {
        var gap = 8.0;
        var left = 16.0;
        var top = 190.0;
        var totalWidth = Math.Max(460.0, ActualWidth - 32.0);
        var cardWidth = (totalWidth - gap * 3.0) / 4.0;
        var cardHeight = 118.0;

        var tiers = new[]
        {
            new TierInfo(
                DrunkState.Low,
                "LOW",
                "> 0",
                new[] { "Stamina penalty", "Drunk visuals", "Weapon sway reduction" }),
            new TierInfo(
                DrunkState.Medium,
                "MEDIUM",
                $">= {medium:0.#}",
                new[] { "Stamina drain", "Camera shake", "Weapon sway", "Recoil", "Hunger", "Drunk visuals" }),
            new TierInfo(
                DrunkState.Strong,
                "STRONG",
                $">= {strong:0.#}",
                new[] { "Stronger stamina drain", "Camera shake", "Stronger weapon sway", "Stronger recoil", "Hunger", "Drunk visuals" }),
            new TierInfo(
                DrunkState.VeryStrong,
                "VERY STRONG",
                $">= {veryStrong:0.#}",
                new[] { "Max stamina drain", "Camera shake", "Max weapon sway", "Max recoil", "High hunger", "Drunk visuals" })
        };

        for (var i = 0; i < tiers.Length; i++)
        {
            var tier = tiers[i];
            var active = tier.State == activeState;
            var rect = new Rect(left + i * (cardWidth + gap), top, cardWidth, cardHeight);

            dc.DrawRoundedRectangle(
                panelAlt,
                new Pen(active ? accent : border, active ? 1.4 : 1.0),
                rect,
                3.0,
                3.0);

            DrawText(dc, tier.Label, new Point(rect.Left + 9, rect.Top + 7), 10.5, active ? accent : muted, FontWeights.Bold);
            DrawText(dc, tier.Threshold, new Point(rect.Right - 48, rect.Top + 8), 8.5, active ? text : muted, FontWeights.SemiBold);

            var y = rect.Top + 27.0;
            foreach (var effect in tier.Effects)
            {
                DrawText(dc, effect, new Point(rect.Left + 9, y), 8.7, active ? Brushes.White : muted, active ? FontWeights.SemiBold : FontWeights.Normal);
                y += 13.2;
            }
        }
    }

    private void DrawSelector(
        DrawingContext dc,
        Rect rect,
        string label,
        bool selected,
        bool hovered,
        Brush background,
        Brush border,
        Brush accent,
        Brush text)
    {
        dc.DrawRoundedRectangle(
            background,
            new Pen(selected || hovered ? accent : border, selected ? 1.5 : 1.0),
            rect,
            3.0,
            3.0);
        DrawCenteredText(dc, label, new Point(rect.Left + rect.Width / 2.0, rect.Top + 5.0), 9.5, selected || hovered ? Brushes.White : text, FontWeights.Bold);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var point = e.GetPosition(this);

        if (_vodkaRect.Contains(point))
        {
            _reference = DrinkReference.Vodka;
            _previewDrinks = Math.Clamp(_previewDrinks, 1, MaxReferenceDrinks);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_beerRect.Contains(point))
        {
            _reference = DrinkReference.Beer;
            _previewDrinks = Math.Clamp(_previewDrinks, 1, MaxReferenceDrinks);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        foreach (var hit in _previewRects)
        {
            if (!hit.Value.Contains(point))
                continue;

            _previewDrinks = hit.Key;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_sliderRect.Contains(point))
        {
            var ratio = Math.Clamp((point.X - _sliderRect.Left) / Math.Max(1.0, _sliderRect.Width), 0.0, 1.0);
            var count = 1 + (int)Math.Round(ratio * (MaxReferenceDrinks - 1), MidpointRounding.AwayFromZero);
            SetRequiredDrinks(count);
            e.Handled = true;
        }
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);

        if (_parameter is null)
            return;

        _parameter.CurrentNumber = 1.0;
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (_parameter is null || e.Delta == 0)
            return;

        var point = e.GetPosition(this);
        if (!_sliderRect.Contains(point))
            return;

        var current = VanillaVeryStrong * CurrentScale / ReferenceStrength;
        var count = (int)Math.Round(current, MidpointRounding.AwayFromZero);
        count += e.Delta > 0 ? 1 : -1;
        SetRequiredDrinks(Math.Clamp(count, 1, MaxReferenceDrinks));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var point = e.GetPosition(this);
        var hoverReference = _vodkaRect.Contains(point)
            ? DrinkReference.Vodka
            : _beerRect.Contains(point)
                ? DrinkReference.Beer
                : null;

        var hoverPreview = -1;
        foreach (var hit in _previewRects)
        {
            if (hit.Value.Contains(point))
            {
                hoverPreview = hit.Key;
                break;
            }
        }

        var interactive = hoverReference is not null || hoverPreview >= 0 || _sliderRect.Contains(point);
        Cursor = interactive ? Cursors.Hand : Cursors.Arrow;

        if (_hoverReference != hoverReference || _hoverPreview != hoverPreview)
        {
            _hoverReference = hoverReference;
            _hoverPreview = hoverPreview;
            InvalidateVisual();
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverReference = null;
        _hoverPreview = -1;
        Cursor = Cursors.Arrow;
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.R && _parameter is not null)
        {
            _parameter.CurrentNumber = 1.0;
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var direction = e.Key switch
        {
            Key.Right or Key.Up or Key.Add or Key.OemPlus => 1,
            Key.Left or Key.Down or Key.Subtract or Key.OemMinus => -1,
            _ => 0
        };

        if (direction == 0 || _parameter is null)
            return;

        var current = VanillaVeryStrong * CurrentScale / ReferenceStrength;
        var count = (int)Math.Round(current, MidpointRounding.AwayFromZero) + direction;
        SetRequiredDrinks(Math.Clamp(count, 1, MaxReferenceDrinks));
        e.Handled = true;
    }

    private void SetRequiredDrinks(int drinks)
    {
        if (_parameter is null)
            return;

        var scale = drinks * ReferenceStrength / VanillaVeryStrong;
        scale = Math.Clamp(scale, MinimumScale, MaximumScale);
        _parameter.CurrentNumber = Math.Round(scale / 0.25, MidpointRounding.AwayFromZero) * 0.25;
        InvalidateVisual();
    }

    private double CurrentScale
        => Math.Clamp(_parameter?.CurrentNumber ?? 1.0, MinimumScale, MaximumScale);

    private double ReferenceStrength
        => _reference == DrinkReference.Beer ? BeerDrunkness : VodkaDrunkness;

    private int MaxReferenceDrinks
        => _reference == DrinkReference.Beer ? 6 : 3;

    private string ReferenceWord(int count)
        => _reference == DrinkReference.Beer
            ? (count == 1 ? "BEER" : "BEERS")
            : (count == 1 ? "VODKA" : "VODKAS");

    private string FormatDrinks(double drinks)
    {
        var rounded = Math.Round(drinks, 1, MidpointRounding.AwayFromZero);
        var countText = Math.Abs(rounded - Math.Round(rounded)) < 0.0001
            ? Math.Round(rounded).ToString("0", CultureInfo.InvariantCulture)
            : rounded.ToString("0.#", CultureInfo.InvariantCulture);

        var plural = Math.Abs(rounded - 1.0) < 0.0001
            ? (_reference == DrinkReference.Beer ? "BEER" : "VODKA")
            : (_reference == DrinkReference.Beer ? "BEERS" : "VODKAS");

        return $"{countText} {plural}";
    }

    private static DrunkState GetState(double points, double medium, double strong, double veryStrong)
    {
        if (points >= veryStrong)
            return DrunkState.VeryStrong;
        if (points >= strong)
            return DrunkState.Strong;
        if (points >= medium)
            return DrunkState.Medium;
        if (points > 0.0)
            return DrunkState.Low;
        return DrunkState.Sober;
    }

    private static string StateLabel(DrunkState state) => state switch
    {
        DrunkState.Low => "LOW",
        DrunkState.Medium => "MEDIUM",
        DrunkState.Strong => "STRONG",
        DrunkState.VeryStrong => "VERY STRONG",
        _ => "SOBER"
    };

    private static double DrinkToX(double drinks, double left, double right, int maxDrinks)
    {
        if (maxDrinks <= 1)
            return left;

        var ratio = (drinks - 1.0) / (maxDrinks - 1.0);
        return left + Math.Clamp(ratio, 0.0, 1.0) * (right - left);
    }

    private Brush BrushResource(string key, Brush fallback)
        => TryFindResource(key) as Brush ?? fallback;

    private void DrawText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
    {
        var ft = CreateText(value, size, brush, weight);
        dc.DrawText(ft, point);
    }

    private void DrawCenteredText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
    {
        var ft = CreateText(value, size, brush, weight);
        dc.DrawText(ft, new Point(point.X - ft.WidthIncludingTrailingWhitespace / 2.0, point.Y));
    }

    private FormattedText CreateText(string value, double size, Brush brush, FontWeight weight)
        => new(
            value,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            size,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private enum DrinkReference
    {
        Vodka,
        Beer
    }

    private enum DrunkState
    {
        Sober,
        Low,
        Medium,
        Strong,
        VeryStrong
    }

    private sealed record TierInfo(DrunkState State, string Label, string Threshold, string[] Effects);
}
