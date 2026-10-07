using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Alcohol editor built on fixed vanilla DrunknessMechanics thresholds.
/// Beer and Vodka are independent controls; only each drink's Drunkness value is edited.
/// </summary>
public sealed class DrunkennessControl : FrameworkElement
{
    private const string BeerStrengthParameterId = "survival.beerDrunknessStrength";
    private const string VodkaStrengthParameterId = "survival.vodkaDrunknessStrength";

    private const double VanillaMedium = 20.0;
    private const double VanillaStrong = 40.0;
    private const double VanillaVeryStrong = 60.0;
    private const double VanillaBeerDrunkness = 15.0;
    private const double VanillaVodkaDrunkness = 30.0;

    private ParameterViewModel? _beerStrengthParameter;
    private ParameterViewModel? _vodkaStrengthParameter;
    private ActiveSlider _activeSlider = ActiveSlider.Beer;

    private Rect _beerSliderRect;
    private Rect _vodkaSliderRect;
    private bool _isDraggingSlider;

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
        MinHeight = 400;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 720.0 : Math.Max(520.0, availableSize.Width);
        return new Size(width, 400.0);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((DrunkennessControl)d).RebindParameters();

    private void RebindParameters()
    {
        Unsubscribe(_beerStrengthParameter);
        Unsubscribe(_vodkaStrengthParameter);

        var parameters = Parameters?.OfType<ParameterViewModel>().ToList();
        _beerStrengthParameter = parameters?.FirstOrDefault(x => x.Id.Equals(BeerStrengthParameterId, StringComparison.Ordinal));
        _vodkaStrengthParameter = parameters?.FirstOrDefault(x => x.Id.Equals(VodkaStrengthParameterId, StringComparison.Ordinal));

        Subscribe(_beerStrengthParameter);
        Subscribe(_vodkaStrengthParameter);

        InvalidateVisual();
    }

    private void Subscribe(ParameterViewModel? parameter)
    {
        if (parameter is not null)
            parameter.PropertyChanged += Parameter_PropertyChanged;
    }

    private void Unsubscribe(ParameterViewModel? parameter)
    {
        if (parameter is not null)
            parameter.PropertyChanged -= Parameter_PropertyChanged;
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

        var gap = 8.0;
        var totalWidth = Math.Max(480.0, ActualWidth - 24.0);
        var cardWidth = (totalWidth - gap) / 2.0;
        var beerCard = new Rect(12.0, 10.0, cardWidth, 118.0);
        var vodkaCard = new Rect(beerCard.Right + gap, 10.0, cardWidth, 118.0);

        DrawDrinkCard(
            dc,
            beerCard,
            "BEER",
            "BEERS",
            _beerStrengthParameter,
            VanillaBeerDrunkness,
            8,
            ActiveSlider.Beer,
            panelAlt,
            border,
            track,
            text,
            muted,
            accent,
            accentDark);

        DrawDrinkCard(
            dc,
            vodkaCard,
            "VODKA",
            "VODKAS",
            _vodkaStrengthParameter,
            VanillaVodkaDrunkness,
            4,
            ActiveSlider.Vodka,
            panelAlt,
            border,
            track,
            text,
            muted,
            accent,
            accentDark);

        DrawProgressionRow(
            dc,
            "AFTER EACH BEER",
            _beerStrengthParameter?.CurrentNumber ?? VanillaBeerDrunkness,
            8,
            142.0,
            panelAlt,
            border,
            text,
            muted,
            accent);

        DrawProgressionRow(
            dc,
            "AFTER EACH VODKA",
            _vodkaStrengthParameter?.CurrentNumber ?? VanillaVodkaDrunkness,
            4,
            198.0,
            panelAlt,
            border,
            text,
            muted,
            accent);

        DrawText(dc, "TIER EFFECTS", new Point(16, 254), 9.0, muted, FontWeights.Bold);
        DrawTierCards(dc, panelAlt, border, radarMuted, accent);
    }

    private void DrawDrinkCard(
        DrawingContext dc,
        Rect rect,
        string singular,
        string plural,
        ParameterViewModel? parameter,
        double vanillaStrength,
        int maxDrinks,
        ActiveSlider slider,
        Brush background,
        Brush border,
        Brush track,
        Brush text,
        Brush muted,
        Brush accent,
        Brush accentDark)
    {
        dc.DrawRoundedRectangle(background, new Pen(border, 1.0), rect, 3.0, 3.0);

        var strength = parameter?.CurrentNumber ?? vanillaStrength;
        var drinks = RequiredWholeDrinks(strength, maxDrinks);
        var drinkLabel = drinks == 1 ? singular : plural;

        DrawText(dc, singular, new Point(rect.Left + 10, rect.Top + 8), 10.0, accent, FontWeights.Bold);
        DrawRightAlignedText(
            dc,
            "WASTED AFTER " + drinks.ToString(CultureInfo.InvariantCulture) + " " + drinkLabel,
            new Point(rect.Right - 10, rect.Top + 7),
            9.2,
            text,
            FontWeights.Bold);

        DrawText(dc, "DRINK STRENGTH", new Point(rect.Left + 10, rect.Top + 34), 8.5, muted, FontWeights.Bold);
        DrawRightAlignedText(
            dc,
            strength.ToString("0", CultureInfo.InvariantCulture),
            new Point(rect.Right - 10, rect.Top + 30),
            15.0,
            text,
            FontWeights.Bold);

        var sliderLeft = rect.Left + 12.0;
        var sliderRight = rect.Right - 12.0;
        var sliderY = rect.Top + 72.0;
        var sliderRect = new Rect(sliderLeft, sliderY - 18.0, sliderRight - sliderLeft, 42.0);

        if (slider == ActiveSlider.Beer)
            _beerSliderRect = sliderRect;
        else
            _vodkaSliderRect = sliderRect;

        dc.DrawLine(new Pen(track, 3.0), new Point(sliderLeft, sliderY), new Point(sliderRight, sliderY));

        var minimum = parameter?.Minimum ?? 1.0;
        var maximum = parameter?.Maximum ?? 60.0;
        var handleX = ValueToX(strength, minimum, maximum, sliderLeft, sliderRight);
        dc.DrawEllipse(accent, null, new Point(handleX, sliderY), 5.0, 5.0);

        var vanillaX = ValueToX(vanillaStrength, minimum, maximum, sliderLeft, sliderRight);
        dc.DrawLine(new Pen(accentDark, 1.0), new Point(vanillaX, sliderY - 5), new Point(vanillaX, sliderY + 5));
        DrawCenteredText(dc, "VANILLA", new Point(vanillaX, rect.Bottom - 20), 8.0, accentDark, FontWeights.Bold);

        DrawText(dc, "MIN " + minimum.ToString("0", CultureInfo.InvariantCulture), new Point(sliderLeft, rect.Bottom - 12), 7.7, muted, FontWeights.SemiBold);
        DrawRightAlignedText(dc, "MAX " + maximum.ToString("0", CultureInfo.InvariantCulture), new Point(sliderRight, rect.Bottom - 12), 7.7, muted, FontWeights.SemiBold);
    }

    private void DrawProgressionRow(
        DrawingContext dc,
        string title,
        double strength,
        int count,
        double titleY,
        Brush panelAlt,
        Brush border,
        Brush text,
        Brush muted,
        Brush accent)
    {
        DrawText(dc, title, new Point(16, titleY), 9.0, muted, FontWeights.Bold);

        var gap = 7.0;
        var left = 16.0;
        var top = titleY + 17.0;
        var totalWidth = Math.Max(420.0, ActualWidth - 32.0);
        var chipWidth = (totalWidth - gap * (count - 1)) / count;
        var chipHeight = 30.0;

        for (var drink = 1; drink <= count; drink++)
        {
            var tier = GetTierAfterDrink(drink, strength);
            var rect = new Rect(left + (drink - 1) * (chipWidth + gap), top, chipWidth, chipHeight);
            var isWasted = tier == "WASTED";

            dc.DrawRoundedRectangle(
                panelAlt,
                new Pen(isWasted ? accent : border, isWasted ? 1.4 : 1.0),
                rect,
                3.0,
                3.0);

            DrawText(dc, FormatOrdinal(drink), new Point(rect.Left + 7, rect.Top + 6), 9.0, text, FontWeights.Bold);
            DrawText(dc, tier, new Point(rect.Left + 34, rect.Top + 7), 8.4, isWasted ? accent : muted, FontWeights.Bold);
        }
    }

    private static string GetTierAfterDrink(int drinkCount, double strength)
    {
        var drunkness = drinkCount * strength;

        if (drunkness >= VanillaVeryStrong)
            return "WASTED";

        if (drunkness >= VanillaStrong)
            return "DRUNK";

        if (drunkness >= VanillaMedium)
            return "TIPSY";

        return "BUZZED";
    }

    private void DrawTierCards(
        DrawingContext dc,
        Brush panelAlt,
        Brush border,
        Brush muted,
        Brush accent)
    {
        var gap = 8.0;
        var left = 16.0;
        var top = 272.0;
        var totalWidth = Math.Max(460.0, ActualWidth - 32.0);
        var cardWidth = (totalWidth - gap * 3.0) / 4.0;
        var cardHeight = 112.0;

        var tiers = new[]
        {
            new TierInfo("BUZZED", new[] { "Stamina penalty", "Drunk visuals", "Weapon sway reduction" }),
            new TierInfo("TIPSY", new[] { "Stamina drain", "Camera shake", "Weapon sway", "Recoil", "Hunger", "Drunk visuals" }),
            new TierInfo("DRUNK", new[] { "Stronger stamina drain", "Camera shake", "Stronger weapon sway", "Stronger recoil", "Hunger", "Drunk visuals" }),
            new TierInfo("WASTED", new[] { "Max stamina drain", "Camera shake", "Max weapon sway", "Max recoil", "High hunger", "Drunk visuals" })
        };

        for (var i = 0; i < tiers.Length; i++)
        {
            var tier = tiers[i];
            var rect = new Rect(left + i * (cardWidth + gap), top, cardWidth, cardHeight);

            dc.DrawRoundedRectangle(panelAlt, new Pen(border, 1.0), rect, 3.0, 3.0);
            DrawText(dc, tier.Label, new Point(rect.Left + 9, rect.Top + 8), 10.0, accent, FontWeights.Bold);

            var y = rect.Top + 29.0;
            foreach (var effect in tier.Effects)
            {
                DrawText(dc, effect, new Point(rect.Left + 9, y), 8.5, muted, FontWeights.Normal);
                y += 13.0;
            }
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var point = e.GetPosition(this);

        if (_beerSliderRect.Contains(point))
        {
            _activeSlider = ActiveSlider.Beer;
            _isDraggingSlider = true;
            CaptureMouse();
            SetStrengthFromPoint(_beerStrengthParameter, _beerSliderRect, point.X);
            e.Handled = true;
            return;
        }

        if (_vodkaSliderRect.Contains(point))
        {
            _activeSlider = ActiveSlider.Vodka;
            _isDraggingSlider = true;
            CaptureMouse();
            SetStrengthFromPoint(_vodkaStrengthParameter, _vodkaSliderRect, point.X);
            e.Handled = true;
        }
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);

        var point = e.GetPosition(this);

        if (_beerSliderRect.Contains(point))
        {
            _activeSlider = ActiveSlider.Beer;
            ResetStrength(_beerStrengthParameter, VanillaBeerDrunkness);
            e.Handled = true;
            return;
        }

        if (_vodkaSliderRect.Contains(point))
        {
            _activeSlider = ActiveSlider.Vodka;
            ResetStrength(_vodkaStrengthParameter, VanillaVodkaDrunkness);
            e.Handled = true;
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (e.Delta == 0)
            return;

        var point = e.GetPosition(this);
        var direction = e.Delta > 0 ? 1 : -1;

        if (_beerSliderRect.Contains(point))
        {
            _activeSlider = ActiveSlider.Beer;
            NudgeStrength(_beerStrengthParameter, direction);
            e.Handled = true;
            return;
        }

        if (_vodkaSliderRect.Contains(point))
        {
            _activeSlider = ActiveSlider.Vodka;
            NudgeStrength(_vodkaStrengthParameter, direction);
            e.Handled = true;
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var point = e.GetPosition(this);

        if (_isDraggingSlider && e.LeftButton == MouseButtonState.Pressed)
        {
            if (_activeSlider == ActiveSlider.Beer)
                SetStrengthFromPoint(_beerStrengthParameter, _beerSliderRect, point.X);
            else
                SetStrengthFromPoint(_vodkaStrengthParameter, _vodkaSliderRect, point.X);

            e.Handled = true;
            return;
        }

        var interactive = _beerSliderRect.Contains(point) || _vodkaSliderRect.Contains(point);
        Cursor = interactive ? Cursors.Hand : Cursors.Arrow;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_isDraggingSlider)
        {
            _isDraggingSlider = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        Cursor = Cursors.Arrow;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.R)
        {
            if (_activeSlider == ActiveSlider.Beer)
                ResetStrength(_beerStrengthParameter, VanillaBeerDrunkness);
            else
                ResetStrength(_vodkaStrengthParameter, VanillaVodkaDrunkness);

            e.Handled = true;
            return;
        }

        var direction = e.Key switch
        {
            Key.Right or Key.Up or Key.Add or Key.OemPlus => 1,
            Key.Left or Key.Down or Key.Subtract or Key.OemMinus => -1,
            _ => 0
        };

        if (direction == 0)
            return;

        NudgeStrength(
            _activeSlider == ActiveSlider.Beer ? _beerStrengthParameter : _vodkaStrengthParameter,
            direction);

        e.Handled = true;
    }

    private void SetStrengthFromPoint(ParameterViewModel? parameter, Rect sliderRect, double x)
    {
        if (parameter is null)
            return;

        var ratio = Math.Clamp((x - sliderRect.Left) / Math.Max(1.0, sliderRect.Width), 0.0, 1.0);
        var raw = parameter.Minimum + ratio * (parameter.Maximum - parameter.Minimum);
        parameter.CurrentNumber = Math.Clamp(
            Math.Round(raw, MidpointRounding.AwayFromZero),
            parameter.Minimum,
            parameter.Maximum);
        InvalidateVisual();
    }

    private void NudgeStrength(ParameterViewModel? parameter, int direction)
    {
        if (parameter is null)
            return;

        parameter.CurrentNumber += direction;
        InvalidateVisual();
    }

    private void ResetStrength(ParameterViewModel? parameter, double vanilla)
    {
        if (parameter is null)
            return;

        parameter.CurrentNumber = vanilla;
        InvalidateVisual();
    }

    private static int RequiredWholeDrinks(double strength, int maxDrinks)
        => Math.Clamp(
            Math.Max(1, (int)Math.Ceiling(VanillaVeryStrong / Math.Max(1.0, strength))),
            1,
            maxDrinks);

    private static string FormatOrdinal(int value)
    {
        var mod100 = value % 100;
        if (mod100 is >= 11 and <= 13)
            return value.ToString(CultureInfo.InvariantCulture) + "TH";

        var suffix = (value % 10) switch
        {
            1 => "ST",
            2 => "ND",
            3 => "RD",
            _ => "TH"
        };

        return value.ToString(CultureInfo.InvariantCulture) + suffix;
    }

    private static double ValueToX(double value, double minimum, double maximum, double left, double right)
    {
        if (maximum <= minimum)
            return left;

        var ratio = (value - minimum) / (maximum - minimum);
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

    private void DrawRightAlignedText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
    {
        var ft = CreateText(value, size, brush, weight);
        dc.DrawText(ft, new Point(point.X - ft.WidthIncludingTrailingWhitespace, point.Y));
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

    private enum ActiveSlider
    {
        Beer,
        Vodka
    }

    private sealed record TierInfo(string Label, string[] Effects);
}
