using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Alcohol editor built on the current DrunknessMechanics stage thresholds.
/// Beer and Vodka remain independent strength controls. OverDrunknessMechanics is
/// deliberately shown only as a separate reference because its full accumulation chain
/// is not folded into this control.
/// </summary>
public sealed class DrunkennessControl : FrameworkElement
{
    private const double HeaderOffset = 42.0;
    private const double BoardHeight = 360.0 + HeaderOffset;

    private const string BeerStrengthParameterId = "survival.beerDrunknessStrength";
    private const string VodkaStrengthParameterId = "survival.vodkaDrunknessStrength";

    private const double VanillaLow = 0.001;
    private const double VanillaMedium = 30.0;
    private const double VanillaStrong = 80.0;
    private const double VanillaBeerDrunkness = 15.0;
    private const double VanillaVodkaDrunkness = 30.0;

    private ParameterViewModel? _beerStrengthParameter;
    private ParameterViewModel? _vodkaStrengthParameter;
    private ActiveSlider _activeSlider = ActiveSlider.Beer;

    private Rect _beerSliderRect;
    private Rect _vodkaSliderRect;
    private bool _isDraggingSlider;
    private ActiveSlider? _hoverSlider;
    private readonly List<ProgressMarker> _progressMarkers = new();
    private ProgressMarker? _hoverProgressMarker;
    private sealed record ProgressMarker(Rect HitRect, ActiveSlider Slider, double Strength, int Drink);


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
        MinHeight = BoardHeight;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 720.0 : Math.Max(520.0, availableSize.Width);
        return new Size(width, BoardHeight);
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

        var panelAlt = BrushResource("PanelAltBrush", new SolidColorBrush(Color.FromRgb(32, 35, 31)));
        var border = BrushResource("BorderSoftBrush", new SolidColorBrush(Color.FromRgb(44, 48, 42)));
        var frameBorder = BrushResource("BorderBrush", border);
        var text = BrushResource("TextBrush", Brushes.WhiteSmoke);
        var muted = BrushResource("MutedTextBrush", Brushes.Gray);
        var radarMuted = BrushResource("RadarMutedTextBrush", Brushes.LightGray);
        var accent = BrushResource("AccentBrush", Brushes.Gold);
        var track = BrushResource("SliderTrackBrush", new SolidColorBrush(Color.FromRgb(34, 37, 31)));

        // Match the shared system-card chrome used by SleepingDashboard and other aggregates.
        IndustrialChrome.DrawFrame(
            dc,
            new Rect(2.0, 2.0, Math.Max(0.0, ActualWidth - 4.0), Math.Max(0.0, ActualHeight - 4.0)),
            panelAlt,
            frameBorder,
            accent,
            border,
            8.0,
            accentEdge: true,
            technicalMarks: true);

        DrawCenteredText(dc, "ALCOHOL TOLERANCE",
            new Point(ActualWidth / 2.0, 21.0), VisualControlTypography.BoardTitle, accent, FontWeights.Bold);
        dc.PushTransform(new TranslateTransform(0.0, HeaderOffset));

        var gap = 8.0;
        var totalWidth = Math.Max(480.0, ActualWidth - 24.0);
        var cardWidth = (totalWidth - gap) / 2.0;
        var beerCard = new Rect(12.0, 10.0, cardWidth, 100.0);
        var vodkaCard = new Rect(beerCard.Right + gap, 10.0, cardWidth, 100.0);

        _progressMarkers.Clear();
        DrawDrinkCard(
            dc,
            beerCard,
            "BEER",
            "BEERS",
            _beerStrengthParameter,
            VanillaBeerDrunkness,
            ActiveSlider.Beer,
            panelAlt,
            border,
            track,
            text,
            muted,
            accent);

        DrawDrinkCard(
            dc,
            vodkaCard,
            "VODKA",
            "VODKAS",
            _vodkaStrengthParameter,
            VanillaVodkaDrunkness,
            ActiveSlider.Vodka,
            panelAlt,
            border,
            track,
            text,
            muted,
            accent);

        IndustrialChrome.DrawEngravedSeparator(dc, 21.0,
            ActualWidth - 21.0, 116.0, muted);

        DrawProgressionRow(
            dc,
            "BEER PROGRESSION",
            _beerStrengthParameter?.CurrentNumber ?? VanillaBeerDrunkness,
            8,
            122.0,
            ActiveSlider.Beer,
            _beerStrengthParameter,
            panelAlt,
            border,
            text,
            muted,
            accent);

        DrawProgressionRow(
            dc,
            "VODKA PROGRESSION",
            _vodkaStrengthParameter?.CurrentNumber ?? VanillaVodkaDrunkness,
            4,
            178.0,
            ActiveSlider.Vodka,
            _vodkaStrengthParameter,
            panelAlt,
            border,
            text,
            muted,
            accent);

        DrawText(dc, "DRUNKNESS STAGES", new Point(16, 234),
            11.2, text, FontWeights.Bold);
        DrawTierCards(dc, panelAlt, border, radarMuted);
        dc.Pop();
    }

    private void DrawDrinkCard(
        DrawingContext dc,
        Rect rect,
        string singular,
        string plural,
        ParameterViewModel? parameter,
        double vanillaStrength,
        ActiveSlider slider,
        Brush background,
        Brush border,
        Brush track,
        Brush text,
        Brush muted,
        Brush accent)
    {
        IndustrialChrome.DrawRecessedCard(dc, rect, background, border, 3.0);

        var strength = parameter?.CurrentNumber ?? vanillaStrength;
        // Fallback readouts align with the source catalog's editable bounds.
        var minimum = parameter?.Minimum ?? (slider == ActiveSlider.Beer ? 8.0 : 15.0);
        var maximum = parameter?.Maximum ?? (slider == ActiveSlider.Beer ? 40.0 : 80.0);
        var drinks = RequiredWholeDrinks(strength);
        var drinkLabel = drinks == 1 ? singular : plural;

        // Row 1: drink name + centered gameplay consequence.
        DrawText(dc, singular, new Point(rect.Left + 10, rect.Top + 7), VisualControlTypography.CardTitle, text, FontWeights.Bold);
        DrawCenteredStatus(
            dc,
            "WASTED AFTER ",
            drinks.ToString(CultureInfo.InvariantCulture),
            " " + drinkLabel,
            new Point(rect.Left + rect.Width / 2.0, rect.Top + 7),
            11.2,
            text,
            parameter?.IsValueModifiedFromReference() == true);

        // Row 2: label and value stay together instead of being split across the card.
        var labelText = CreateText("DRINK STRENGTH", 10.0, muted, FontWeights.SemiBold);
        var labelPoint = new Point(rect.Left + 10, rect.Top + 29);
        dc.DrawText(labelText, labelPoint);

        var valueX = labelPoint.X + labelText.WidthIncludingTrailingWhitespace + 9;
        var changed = parameter?.IsValueModifiedFromReference() == true;
        var currentText = CreateText(
            strength.ToString("0", CultureInfo.InvariantCulture),
            changed ? 14.5 : 13.3,
            changed ? accent : text,
            FontWeights.Bold);
        dc.DrawText(currentText, new Point(valueX, rect.Top + 25));

        DrawText(
            dc,
            " / " + maximum.ToString("0", CultureInfo.InvariantCulture),
            new Point(valueX + currentText.WidthIncludingTrailingWhitespace, rect.Top + 28),
            10.0,
            muted,
            FontWeights.SemiBold);

        var sliderLeft = rect.Left + 12.0;
        var sliderRight = rect.Right - 12.0;
        var sliderY = rect.Top + 58.0;
        var sliderRect = new Rect(sliderLeft, sliderY - 15.0, sliderRight - sliderLeft, 32.0);

        if (slider == ActiveSlider.Beer)
            _beerSliderRect = new Rect(sliderRect.X, sliderRect.Y + HeaderOffset, sliderRect.Width, sliderRect.Height);
        else
            _vodkaSliderRect = new Rect(sliderRect.X, sliderRect.Y + HeaderOffset, sliderRect.Width, sliderRect.Height);

        dc.DrawLine(new Pen(track, 3.0), new Point(sliderLeft, sliderY), new Point(sliderRight, sliderY));
        IndustrialRulerChrome.DrawNativeTicks(dc,
            new Rect(sliderLeft, sliderY - 6.0, sliderRight - sliderLeft, 12.0),
            minimum, maximum, parameter?.TickFrequency ?? 1.0, muted);

        var handleX = ValueToX(strength, minimum, maximum, sliderLeft, sliderRight);
        IndustrialRulerChrome.DrawActiveRailGlint(dc, sliderLeft, handleX, sliderY,
            accent, parameter?.IsControlEnabled == true);
        ControlHoverGlow.Draw(dc, new Point(handleX, sliderY), accent,
            _hoverSlider == slider);
        dc.DrawEllipse(accent, null, new Point(handleX, sliderY), 5.0, 5.0);

        var vanillaX = ValueToX(vanillaStrength, minimum, maximum, sliderLeft, sliderRight);
        dc.DrawLine(new Pen(muted, 1.2),
            new Point(vanillaX, sliderY - 7), new Point(vanillaX, sliderY + 7));

        // One compact scale row directly under the track.
        var scaleY = rect.Top + 72.0;
        DrawText(
            dc,
            minimum.ToString("0", CultureInfo.InvariantCulture),
            new Point(sliderLeft, scaleY),
            10.0,
            muted,
            FontWeights.SemiBold);

        DrawRightAlignedText(
            dc,
            maximum.ToString("0", CultureInfo.InvariantCulture),
            new Point(sliderRight, scaleY),
            10.0,
            muted,
            FontWeights.SemiBold);
    }

    private void DrawProgressionRow(
        DrawingContext dc,
        string title,
        double strength,
        int count,
        double titleY,
        ActiveSlider slider,
        ParameterViewModel? parameter,
        Brush panelAlt,
        Brush border,
        Brush text,
        Brush muted,
        Brush accent)
    {
        DrawText(dc, title, new Point(16, titleY), 10.4, text, FontWeights.Bold);

        var left = 16.0;
        var top = titleY + 16.0;
        var totalWidth = Math.Max(420.0, ActualWidth - 32.0);
        var stripHeight = 34.0;
        var segmentWidth = totalWidth / count;
        var stripRect = new Rect(left, top, totalWidth, stripHeight);

        dc.DrawRoundedRectangle(panelAlt, new Pen(border, 1.0), stripRect, 4.0, 4.0);

        for (var drink = 1; drink <= count; drink++)
        {
            var segmentLeft = left + (drink - 1) * segmentWidth;
            var segmentCenter = segmentLeft + segmentWidth / 2.0;
            var tier = GetTierAfterDrink(drink, strength);
            var isStrong = tier == "WASTED";
            // A stage is selectable only when the corresponding source strength
            // is within its native range. The selected drink reaches that stage.
            var boundary = tier == "WASTED" ? VanillaStrong
                : tier == "DRUNK" ? VanillaMedium : VanillaLow;
            var value = boundary / drink;
            var canSelect = parameter?.IsControlEnabled == true
                && value >= parameter.Minimum && value <= parameter.Maximum
                && tier is "WASTED" or "DRUNK";
            if (canSelect)
                _progressMarkers.Add(new ProgressMarker(
                    new Rect(segmentLeft + 2.0, top + HeaderOffset,
                        segmentWidth - 4.0, stripHeight), slider, value, drink));
            var hovering = canSelect && _hoverProgressMarker?.Slider == slider
                && _hoverProgressMarker.Drink == drink;
            if (drink > 1)
            {
                dc.DrawLine(
                    new Pen(border, 1.0),
                    new Point(segmentLeft, top + 5.0),
                    new Point(segmentLeft, top + stripHeight - 5.0));
            }

            if (isStrong)
            {
                dc.DrawLine(
                    new Pen(accent, 2.0),
                    new Point(segmentLeft + 5.0, top + 1.0),
                    new Point(segmentLeft + segmentWidth - 5.0, top + 1.0));
            }

            if (hovering)
            {
                // Outline only the already-selectable DRUNK/WASTED hit cell.
                dc.DrawRoundedRectangle(null, new Pen(accent, 1.0),
                    new Rect(segmentLeft + 2.0, top + 2.0,
                        segmentWidth - 4.0, stripHeight - 4.0), 3.0, 3.0);
            }

            DrawCenteredText(
                dc,
                FormatOrdinal(drink),
                new Point(segmentCenter, top + 4.0),
                10.0,
                text,
                FontWeights.Bold);

            DrawCenteredText(
                dc,
                tier,
                new Point(segmentCenter, top + 18.0),
                10.0,
                hovering ? Brushes.White : isStrong ? accent : muted,
                FontWeights.Bold);
        }
    }

    private static string GetTierAfterDrink(int drinkCount, double strength)
    {
        var drunkness = drinkCount * strength;

        if (drunkness >= VanillaStrong)
            return "WASTED";

        if (drunkness >= VanillaMedium)
            return "DRUNK";

        if (drunkness >= VanillaLow)
            return "TIPSY";

        return "SOBER";
    }

    private void DrawTierCards(
        DrawingContext dc, Brush panelAlt, Brush border,
        Brush muted)
    {
        const double gap = 8.0;
        const double left = 16.0;
        const double top = 252.0;
        var totalWidth = Math.Max(460.0, ActualWidth - 32.0);
        var cardWidth = (totalWidth - gap * 3.0) / 4.0;
        var stages = new[]
        {
            ("SOBER", "BELOW 0.001"),
            ("TIPSY", "0.001+"),
            ("DRUNK", "30+"),
            ("WASTED", "80+")
        };

        for (var i = 0; i < stages.Length; i++)
        {
            var stage = stages[i];
            var rect = new Rect(left + i * (cardWidth + gap),
                top, cardWidth, 70.0);
            dc.DrawRoundedRectangle(panelAlt, new Pen(border, 1.0),
                rect, 3.0, 3.0);
            DrawText(dc, stage.Item1,
                new Point(rect.Left + 10.0, rect.Top + 9.0),
                11.2, muted, FontWeights.Bold);
            // Fixed authored stage boundaries are read-only and neutral.
            DrawText(dc, stage.Item2,
                new Point(rect.Left + 10.0, rect.Top + 35.0),
                10.4, muted, FontWeights.SemiBold);
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var point = e.GetPosition(this);
        var progress = _progressMarkers.FirstOrDefault(m => m.HitRect.Contains(point));
        if (progress is not null)
        {
            var parameter = progress.Slider == ActiveSlider.Beer
                ? _beerStrengthParameter : _vodkaStrengthParameter;
            if (parameter?.IsControlEnabled == true)
            {
                _activeSlider = progress.Slider;
                var step = Math.Max(0.0000001, parameter.TickFrequency);
                var target = parameter.Minimum + Math.Round(
                    (progress.Strength - parameter.Minimum) / step,
                    MidpointRounding.AwayFromZero) * step;
                parameter.CurrentNumber = Math.Clamp(target, parameter.Minimum, parameter.Maximum);
                InvalidateVisual();
                e.Handled = true;
                return;
            }
        }

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

        var progress = _progressMarkers.FirstOrDefault(m => m.HitRect.Contains(point));
        if (_hoverProgressMarker != progress)
        {
            _hoverProgressMarker = progress;
            InvalidateVisual();
        }
        if (progress is not null)
        {
            Cursor = Cursors.Hand;
            return;
        }
        var next = _beerSliderRect.Contains(point) ? ActiveSlider.Beer
            : _vodkaSliderRect.Contains(point) ? ActiveSlider.Vodka : (ActiveSlider?)null;
        if (_hoverSlider != next)
        {
            _hoverSlider = next;
            InvalidateVisual();
        }
        Cursor = next is not null ? Cursors.Hand : Cursors.Arrow;
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
        _hoverSlider = null;
        _hoverProgressMarker = null;
        Cursor = Cursors.Arrow;
        InvalidateVisual();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.R)
        {
            ResetStrength(_beerStrengthParameter, VanillaBeerDrunkness);
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
        var step = Math.Max(0.0000001, parameter.TickFrequency);
        var snapped = parameter.Minimum +
                      Math.Round((raw - parameter.Minimum) / step, MidpointRounding.AwayFromZero) * step;

        parameter.CurrentNumber = Math.Clamp(snapped, parameter.Minimum, parameter.Maximum);
        InvalidateVisual();
    }

    private void NudgeStrength(ParameterViewModel? parameter, int direction)
    {
        if (parameter is null)
            return;

        parameter.CurrentNumber = Math.Clamp(
            parameter.CurrentNumber + direction * parameter.TickFrequency,
            parameter.Minimum,
            parameter.Maximum);
        InvalidateVisual();
    }

    private void ResetStrength(ParameterViewModel? parameter, double vanilla)
    {
        if (parameter is null)
            return;

        parameter.CurrentNumber = vanilla;
        InvalidateVisual();
    }

    private static int RequiredWholeDrinks(double strength)
        => Math.Max(1, (int)Math.Ceiling(VanillaStrong / Math.Max(0.001, strength)));

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

    private void DrawCenteredStatus(
        DrawingContext dc,
        string prefix,
        string highlighted,
        string suffix,
        Point center,
        double size,
        Brush textBrush,
        bool modified)
    {
        var prefixText = CreateText(prefix, size, textBrush, FontWeights.SemiBold);
        var highlightedText = CreateText(highlighted,
            modified ? size + 1.2 : size,
            modified ? BrushResource("AccentBrush", Brushes.Gold) : textBrush,
            FontWeights.Bold);
        var suffixText = CreateText(suffix, size, textBrush, FontWeights.SemiBold);

        var totalWidth =
            prefixText.WidthIncludingTrailingWhitespace +
            highlightedText.WidthIncludingTrailingWhitespace +
            suffixText.WidthIncludingTrailingWhitespace;

        var x = center.X - totalWidth / 2.0;
        dc.DrawText(prefixText, new Point(x, center.Y));
        x += prefixText.WidthIncludingTrailingWhitespace;

        dc.DrawText(highlightedText, new Point(x, center.Y - 1.6));
        x += highlightedText.WidthIncludingTrailingWhitespace;

        dc.DrawText(suffixText, new Point(x, center.Y));
    }

    private FormattedText CreateText(string value, double size, Brush brush, FontWeight weight)
        => new(
            value,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            Math.Max(9.0, size),
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

    private enum ActiveSlider
    {
        Beer,
        Vodka
    }

}
