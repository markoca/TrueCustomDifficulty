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
/// Beer is the primary player-facing tolerance reference; Vodka remains an optional
/// equivalent view. The tolerance parameter scales all thresholds together, while
/// Beer/Vodka strength parameters independently scale each drink's Drunkness contribution.
/// </summary>
public sealed class DrunkennessControl : FrameworkElement
{
    private const string ParameterId = "survival.drunknessThresholdScale";
    private const string BeerStrengthParameterId = "survival.beerDrunknessStrength";
    private const string VodkaStrengthParameterId = "survival.vodkaDrunknessStrength";
    private const double VanillaVeryStrong = 60.0;
    private const double VanillaBeerDrunkness = 15.0;
    private const double VanillaVodkaDrunkness = 30.0;
    private const double MinimumScale = 0.05;
    private const double MaximumScale = 4.0;

    private ParameterViewModel? _parameter;
    private ParameterViewModel? _beerStrengthParameter;
    private ParameterViewModel? _vodkaStrengthParameter;
    private DrinkReference _reference = DrinkReference.Beer;
    private ActiveSlider _activeSlider = ActiveSlider.Tolerance;
    private Rect _vodkaRect;
    private Rect _beerRect;
    private Rect _sliderRect;
    private Rect _beerStrengthSliderRect;
    private Rect _vodkaStrengthSliderRect;
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
        MinHeight = 340;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 720.0 : Math.Max(520.0, availableSize.Width);
        return new Size(width, 340.0);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((DrunkennessControl)d).RebindParameter();

    private void RebindParameter()
    {
        Unsubscribe(_parameter);
        Unsubscribe(_beerStrengthParameter);
        Unsubscribe(_vodkaStrengthParameter);

        var parameters = Parameters?.OfType<ParameterViewModel>().ToList();
        _parameter = parameters?.FirstOrDefault(x => x.Id.Equals(ParameterId, StringComparison.Ordinal));
        _beerStrengthParameter = parameters?.FirstOrDefault(x => x.Id.Equals(BeerStrengthParameterId, StringComparison.Ordinal));
        _vodkaStrengthParameter = parameters?.FirstOrDefault(x => x.Id.Equals(VodkaStrengthParameterId, StringComparison.Ordinal));

        Subscribe(_parameter);
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
        var cardWidth = (totalWidth - gap * 2.0) / 3.0;
        var toleranceCard = new Rect(12.0, 10.0, cardWidth, 120.0);
        var beerCard = new Rect(toleranceCard.Right + gap, 10.0, cardWidth, 120.0);
        var vodkaCard = new Rect(beerCard.Right + gap, 10.0, cardWidth, 120.0);

        DrawToleranceCard(dc, toleranceCard, panelAlt, border, track, text, muted, accent, accentDark);
        DrawStrengthCard(dc, beerCard, DrinkReference.Beer, _beerStrengthParameter, panelAlt, border, track, text, muted, accent, accentDark);
        DrawStrengthCard(dc, vodkaCard, DrinkReference.Vodka, _vodkaStrengthParameter, panelAlt, border, track, text, muted, accent, accentDark);

        DrawProgression(dc, panelAlt, border, text, muted, accent);
        DrawText(dc, "EFFECT PREVIEW", new Point(16, 200), 9.0, muted, FontWeights.Bold);
        DrawTierCards(dc, panelAlt, border, radarMuted, accent);
    }

    private void DrawToleranceCard(
        DrawingContext dc,
        Rect rect,
        Brush background,
        Brush border,
        Brush track,
        Brush text,
        Brush muted,
        Brush accent,
        Brush accentDark)
    {
        dc.DrawRoundedRectangle(background, new Pen(border, 1.0), rect, 3.0, 3.0);

        var requiredDrinks = VanillaVeryStrong * CurrentScale / ReferenceStrength;
        DrawText(dc, "TOLERANCE", new Point(rect.Left + 10, rect.Top + 8), 10.0, accent, FontWeights.Bold);
        DrawRightAlignedText(dc, FormatDrinks(requiredDrinks), new Point(rect.Right - 10, rect.Top + 6), 12.0, text, FontWeights.Bold);

        _beerRect = new Rect(rect.Left + 10, rect.Top + 31, 50, 22);
        _vodkaRect = new Rect(rect.Left + 65, rect.Top + 31, 58, 22);
        DrawSelector(dc, _beerRect, "BEER", _reference == DrinkReference.Beer, _hoverReference == DrinkReference.Beer, background, border, accent, text);
        DrawSelector(dc, _vodkaRect, "VODKA", _reference == DrinkReference.Vodka, _hoverReference == DrinkReference.Vodka, background, border, accent, text);

        var sliderLeft = rect.Left + 12.0;
        var sliderRight = rect.Right - 12.0;
        var sliderY = rect.Top + 77.0;
        _sliderRect = new Rect(sliderLeft, sliderY - 13.0, sliderRight - sliderLeft, 30.0);
        dc.DrawLine(new Pen(track, 3.0), new Point(sliderLeft, sliderY), new Point(sliderRight, sliderY));

        var maxDrinks = MaxReferenceDrinks;
        for (var i = 1; i <= maxDrinks; i++)
        {
            var x = DrinkToX(i, sliderLeft, sliderRight, maxDrinks);
            dc.DrawLine(new Pen(border, 1.0), new Point(x, sliderY - 4), new Point(x, sliderY + 4));
            DrawCenteredText(dc, i.ToString(CultureInfo.InvariantCulture), new Point(x, sliderY + 8), 8.0, muted, FontWeights.SemiBold);
        }

        var displayedRequiredDrinks = Math.Clamp(Math.Ceiling(requiredDrinks - 0.000001), 1.0, maxDrinks);
        dc.DrawEllipse(accent, null, new Point(DrinkToX(displayedRequiredDrinks, sliderLeft, sliderRight, maxDrinks), sliderY), 5.0, 5.0);

        var vanillaDrinks = VanillaVeryStrong / ReferenceStrength;
        var vanillaDisplayDrinks = Math.Clamp(Math.Ceiling(vanillaDrinks - 0.000001), 1.0, maxDrinks);
        var vanillaX = DrinkToX(vanillaDisplayDrinks, sliderLeft, sliderRight, maxDrinks);
        DrawCenteredText(dc, "VANILLA", new Point(vanillaX, rect.Bottom - 15), 8.0, accentDark, FontWeights.Bold);
    }

    private void DrawStrengthCard(
        DrawingContext dc,
        Rect rect,
        DrinkReference drink,
        ParameterViewModel? parameter,
        Brush background,
        Brush border,
        Brush track,
        Brush text,
        Brush muted,
        Brush accent,
        Brush accentDark)
    {
        dc.DrawRoundedRectangle(background, new Pen(border, 1.0), rect, 3.0, 3.0);

        var isBeer = drink == DrinkReference.Beer;
        var vanillaStrength = isBeer ? VanillaBeerDrunkness : VanillaVodkaDrunkness;
        var multiplier = parameter?.CurrentNumber ?? 1.0;
        var rawStrength = vanillaStrength * multiplier;

        DrawText(dc, isBeer ? "BEER" : "VODKA", new Point(rect.Left + 10, rect.Top + 8), 10.0, accent, FontWeights.Bold);
        DrawRightAlignedText(dc, (multiplier * 100.0).ToString("0", CultureInfo.InvariantCulture) + "%", new Point(rect.Right - 10, rect.Top + 6), 12.0, text, FontWeights.Bold);
        DrawText(dc, rawStrength.ToString("0.##", CultureInfo.InvariantCulture) + " DRUNKNESS / DRINK", new Point(rect.Left + 10, rect.Top + 34), 8.5, muted, FontWeights.SemiBold);

        var sliderLeft = rect.Left + 12.0;
        var sliderRight = rect.Right - 12.0;
        var sliderY = rect.Top + 77.0;
        var sliderRect = new Rect(sliderLeft, sliderY - 13.0, sliderRight - sliderLeft, 30.0);
        if (isBeer) _beerStrengthSliderRect = sliderRect;
        else _vodkaStrengthSliderRect = sliderRect;

        dc.DrawLine(new Pen(track, 3.0), new Point(sliderLeft, sliderY), new Point(sliderRight, sliderY));

        var minimum = parameter?.Minimum ?? 0.25;
        var maximum = parameter?.Maximum ?? 2.0;
        dc.DrawEllipse(accent, null, new Point(ValueToX(multiplier, minimum, maximum, sliderLeft, sliderRight), sliderY), 5.0, 5.0);

        var vanillaX = ValueToX(1.0, minimum, maximum, sliderLeft, sliderRight);
        dc.DrawLine(new Pen(accentDark, 1.0), new Point(vanillaX, sliderY - 5), new Point(vanillaX, sliderY + 5));
        DrawCenteredText(dc, "VANILLA", new Point(vanillaX, rect.Bottom - 15), 8.0, accentDark, FontWeights.Bold);
    }

    private void DrawProgression(
        DrawingContext dc,
        Brush panelAlt,
        Brush border,
        Brush text,
        Brush muted,
        Brush accent)
    {
        var title = _reference == DrinkReference.Beer ? "AFTER EACH BEER" : "AFTER EACH VODKA";
        DrawText(dc, title, new Point(16, 143), 9.0, muted, FontWeights.Bold);

        var count = MaxReferenceDrinks;
        var gap = 7.0;
        var left = 16.0;
        var top = 160.0;
        var totalWidth = Math.Max(420.0, ActualWidth - 32.0);
        var chipWidth = (totalWidth - gap * (count - 1)) / count;
        var chipHeight = 30.0;

        for (var drink = 1; drink <= count; drink++)
        {
            var tier = GetTierAfterDrink(drink);
            var rect = new Rect(left + (drink - 1) * (chipWidth + gap), top, chipWidth, chipHeight);
            var isVeryStrong = tier == "VERY STRONG";

            dc.DrawRoundedRectangle(
                panelAlt,
                new Pen(isVeryStrong ? accent : border, isVeryStrong ? 1.4 : 1.0),
                rect,
                3.0,
                3.0);

            DrawText(dc, drink.ToString(CultureInfo.InvariantCulture), new Point(rect.Left + 8, rect.Top + 6), 10.0, text, FontWeights.Bold);
            DrawText(dc, tier, new Point(rect.Left + 25, rect.Top + 7), 8.7, isVeryStrong ? accent : muted, FontWeights.Bold);
        }
    }

    private string GetTierAfterDrink(int drinkCount)
    {
        var drunkness = drinkCount * ReferenceStrength;
        var scale = CurrentScale;

        if (drunkness >= VanillaVeryStrong * scale)
            return "VERY STRONG";

        if (drunkness >= 40.0 * scale)
            return "STRONG";

        if (drunkness >= 20.0 * scale)
            return "MEDIUM";

        return "LOW";
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
        var top = 218.0;
        var totalWidth = Math.Max(460.0, ActualWidth - 32.0);
        var cardWidth = (totalWidth - gap * 3.0) / 4.0;
        var cardHeight = 108.0;

        var tiers = new[]
        {
            new TierInfo(
                "LOW",
                new[] { "Stamina penalty", "Drunk visuals", "Weapon sway reduction" }),
            new TierInfo(
                "MEDIUM",
                new[] { "Stamina drain", "Camera shake", "Weapon sway", "Recoil", "Hunger", "Drunk visuals" }),
            new TierInfo(
                "STRONG",
                new[] { "Stronger stamina drain", "Camera shake", "Stronger weapon sway", "Stronger recoil", "Hunger", "Drunk visuals" }),
            new TierInfo(
                "VERY STRONG",
                new[] { "Max stamina drain", "Camera shake", "Max weapon sway", "Max recoil", "High hunger", "Drunk visuals" })
        };

        for (var i = 0; i < tiers.Length; i++)
        {
            var tier = tiers[i];
            var rect = new Rect(left + i * (cardWidth + gap), top, cardWidth, cardHeight);

            dc.DrawRoundedRectangle(
                panelAlt,
                new Pen(border, 1.0),
                rect,
                3.0,
                3.0);

            DrawText(dc, tier.Label, new Point(rect.Left + 9, rect.Top + 8), 10.5, accent, FontWeights.Bold);

            var y = rect.Top + 29.0;
            foreach (var effect in tier.Effects)
            {
                DrawText(dc, effect, new Point(rect.Left + 9, y), 8.7, muted, FontWeights.Normal);
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
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_beerRect.Contains(point))
        {
            _reference = DrinkReference.Beer;
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
        var count = Math.Max(1, (int)Math.Ceiling(current - 0.000001));
        count += e.Delta > 0 ? 1 : -1;
        SetRequiredDrinks(Math.Clamp(count, 1, MaxReferenceDrinks));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var point = e.GetPosition(this);
        DrinkReference? hoverReference = _vodkaRect.Contains(point)
            ? DrinkReference.Vodka
            : _beerRect.Contains(point)
                ? DrinkReference.Beer
                : null;

        var interactive = hoverReference is not null || _sliderRect.Contains(point);
        Cursor = interactive ? Cursors.Hand : Cursors.Arrow;

        if (_hoverReference != hoverReference)
        {
            _hoverReference = hoverReference;
            InvalidateVisual();
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        _hoverReference = null;
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
        var count = Math.Max(1, (int)Math.Ceiling(current - 0.000001)) + direction;
        SetRequiredDrinks(Math.Clamp(count, 1, MaxReferenceDrinks));
        e.Handled = true;
    }

    private void SetRequiredDrinks(int drinks)
    {
        if (_parameter is null)
            return;

        var scale = drinks * ReferenceStrength / VanillaVeryStrong;
        scale = Math.Clamp(scale, MinimumScale, MaximumScale);
        _parameter.SnapCurrentNumberToMarker(scale);
        InvalidateVisual();
    }

    private double CurrentScale
        => Math.Clamp(_parameter?.CurrentNumber ?? 1.0, MinimumScale, MaximumScale);

    private double ReferenceStrength
        => _reference == DrinkReference.Beer
            ? VanillaBeerDrunkness * (_beerStrengthParameter?.CurrentNumber ?? 1.0)
            : VanillaVodkaDrunkness * (_vodkaStrengthParameter?.CurrentNumber ?? 1.0);

    private int MaxReferenceDrinks
        => _reference == DrinkReference.Beer ? 8 : 4;

    private string FormatDrinks(double drinks)
    {
        // Players consume whole bottles/cans in gameplay. If a threshold falls between
        // two whole drinks (for example 0.5 vodka), show the first whole drink that
        // actually reaches it instead of exposing the internal fractional math.
        var wholeDrinks = Math.Max(1, (int)Math.Ceiling(drinks - 0.000001));
        var maxDrinks = MaxReferenceDrinks;

        if (wholeDrinks > maxDrinks)
        {
            var overflowPlural = _reference == DrinkReference.Beer ? "BEERS" : "VODKAS";
            return $"{maxDrinks.ToString(CultureInfo.InvariantCulture)}+ {overflowPlural}";
        }

        var plural = wholeDrinks == 1
            ? (_reference == DrinkReference.Beer ? "BEER" : "VODKA")
            : (_reference == DrinkReference.Beer ? "BEERS" : "VODKAS");

        return $"{wholeDrinks.ToString(CultureInfo.InvariantCulture)} {plural}";
    }

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

    private sealed record TierInfo(string Label, string[] Effects);
}
