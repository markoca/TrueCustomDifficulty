using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Individual-anomaly damage table.
///
/// The historical *Radar class name is kept for XAML compatibility. Each anomaly is
/// edited on one common 0-200% of vanilla scale while its raw base value remains
/// visible. EffectiveDisplay is shown separately because Global Anomaly Damage and
/// protection layers remain independent gameplay layers.
/// </summary>
public sealed class AnomalyDamageRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double RowHeight = 37.0;

    private static readonly AnomalyDefinition[] Anomalies =
    {
        new("environment.electroAnomalyDamage", "ELECTRO", false),
        new("environment.chemicalAnomalyDamage", "CHEMICAL", false),
        new("environment.fireAnomalyDamage", "FIRE", false),
        new("environment.carouselAnomalyDamage", "CAROUSEL", false),
        new("environment.toxicCloudDamage", "TOXIC CLOUD", false),
        new("environment.steamAnomalyDamage", "STEAM", false),
        new("environment.clickerHealthDamage", "CLICKER", false),
        new("environment.razorHealthDamage", "RAZOR", true),
        new("environment.soapBubbleHealthDamage", "SOAP BUBBLE", false),
        new("environment.fireballHealthDamage", "FIREBALL", false),
        new("environment.diamondHealthDamage", "DIAMOND", false),
    };

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Anomalies.Length];
    private readonly Dictionary<int, HitTarget> _targets = new();
    private readonly VisualStatusMessage _status;

    private int _selectedRow;
    private int _hoverRow = -1;
    private int _dragRow = -1;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(AnomalyDamageRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public AnomalyDamageRadar()
    {
        _status = new VisualStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = 550;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 780.0 : Math.Max(600.0, availableSize.Width);
        return new Size(width, 550.0);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AnomalyDamageRadar)d).RebindParameters();

    private void RebindParameters()
    {
        foreach (var parameter in _parameters)
        {
            if (parameter is not null)
                parameter.PropertyChanged -= Parameter_PropertyChanged;
        }

        Array.Clear(_parameters, 0, _parameters.Length);

        if (Parameters is not null)
        {
            var byId = Parameters
                .OfType<ParameterViewModel>()
                .ToDictionary(x => x.Id, StringComparer.Ordinal);

            for (var i = 0; i < Anomalies.Length; i++)
            {
                if (!byId.TryGetValue(Anomalies[i].ParameterId, out var parameter))
                    continue;

                _parameters[i] = parameter;
                parameter.PropertyChanged += Parameter_PropertyChanged;
            }
        }

        InvalidateVisual();
    }

    private void Parameter_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ParameterViewModel.CurrentNumber)
            or nameof(ParameterViewModel.IsModified)
            or nameof(ParameterViewModel.CurrentDisplay)
            or nameof(ParameterViewModel.EffectiveDisplay)
            or nameof(ParameterViewModel.ReferenceNumber))
        {
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        var text = FindBrush("TextBrush", Brushes.White);
        var muted = FindBrush("RadarMutedTextBrush", Brushes.Gray);
        var border = FindBrush("BorderBrush", Brushes.DimGray);
        var soft = FindBrush("BorderSoftBrush", border);
        var accent = FindBrush("AccentBrush", Brushes.Goldenrod);
        var panel = FindBrush("PanelAltBrush", Brushes.Black);

        IndustrialChrome.DrawFrame(
            dc,
            new Rect(2.0, 2.0, Math.Max(0.0, ActualWidth - 4.0), Math.Max(0.0, ActualHeight - 4.0)),
            panel,
            border,
            accent,
            soft,
            8.0,
            accentEdge: true,
            technicalMarks: true);

        _targets.Clear();

        DrawText(dc, "INDIVIDUAL ANOMALY DAMAGE", new Point(18.0, 16.0), 12.0, text, FontWeights.Bold);
        DrawText(
            dc,
            "base damage tuning per anomaly; Global Anomaly Damage remains a separate multiplier layer",
            new Point(18.0, 34.0),
            8.1,
            muted,
            FontWeights.Normal);

        var layout = GetLayout();

        DrawText(dc, "ANOMALY", new Point(layout.LabelLeft, 63.0), 7.8, muted, FontWeights.Bold);
        DrawCenteredText(dc, "BASE", new Point(layout.BaseCenter, 70.0), 7.8, muted, FontWeights.Bold);
        DrawCenteredText(dc, "% VANILLA", new Point(layout.RelativeCenter, 70.0), 7.8, muted, FontWeights.Bold);
        DrawCenteredText(dc, "EFFECTIVE", new Point(layout.EffectiveCenter, 70.0), 7.8, muted, FontWeights.Bold);
        DrawCenteredText(
            dc,
            "0%  ←  INDIVIDUAL STRENGTH  →  200%",
            new Point(layout.TrackLeft + layout.TrackWidth / 2.0, 70.0),
            7.7,
            muted,
            FontWeights.Bold);

        dc.DrawLine(new Pen(border, 1.0), new Point(14.0, 83.0), new Point(Math.Max(14.0, ActualWidth - 14.0), 83.0));

        for (var row = 0; row < Anomalies.Length; row++)
            DrawRow(dc, row, 91.0 + row * RowHeight, layout, text, muted, border, accent, panel);

        DrawCenteredText(
            dc,
            "RAZOR SCALES BOTH LOW/HIGH-SPEED HEALTH DAMAGE TOGETHER   •   OTHER RAZOR EFFECTS STAY SEPARATE",
            new Point(ActualWidth / 2.0, 508.0),
            7.3,
            muted,
            FontWeights.Normal);

        DrawCenteredText(
            dc,
            "EFFECTIVE IS PRE-PROTECTION / LIVE LAYER OUTPUT WHERE AVAILABLE — PLAYER PROTECTION STILL RESOLVES SEPARATELY",
            new Point(ActualWidth / 2.0, 526.0),
            7.3,
            muted,
            FontWeights.Normal);

        if (!string.IsNullOrWhiteSpace(_status.Text))
        {
            DrawCenteredText(
                dc,
                _status.Text,
                new Point(ActualWidth / 2.0, 544.0),
                8.0,
                accent,
                FontWeights.SemiBold);
        }
    }

    private void DrawRow(
        DrawingContext dc,
        int row,
        double top,
        Layout layout,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var parameter = _parameters[row];
        if (parameter is null)
            return;

        var info = Anomalies[row];
        var reference = parameter.ReferenceNumber;
        var relative = Math.Abs(reference) > Epsilon
            ? parameter.CurrentNumber / reference
            : 1.0;
        var modified = Math.Abs(parameter.CurrentNumber - reference) > Epsilon;
        var selected = row == _selectedRow;
        var hovered = row == _hoverRow;

        var rowRect = new Rect(14.0, top, Math.Max(1.0, ActualWidth - 28.0), RowHeight - 3.0);
        dc.DrawRoundedRectangle(
            selected
                ? CloneWithOpacity(accent, 0.08)
                : hovered
                    ? CloneWithOpacity(border, 0.10)
                    : CloneWithOpacity(panel, 0.01),
            new Pen(CloneWithOpacity(border, 0.45), 0.8),
            rowRect,
            3.0,
            3.0);

        DrawText(
            dc,
            info.Label,
            new Point(layout.LabelLeft, top + 10.0),
            8.2,
            modified ? accent : text,
            FontWeights.Bold);

        var baseText = info.IsMultiplier
            ? $"{parameter.CurrentNumber:0.##}x"
            : $"{parameter.CurrentNumber:0.##}";

        DrawCenteredText(
            dc,
            baseText,
            new Point(layout.BaseCenter, top + 17.0),
            8.5,
            modified ? accent : text,
            FontWeights.Bold);

        DrawCenteredText(
            dc,
            $"{relative * 100:0}%",
            new Point(layout.RelativeCenter, top + 17.0),
            8.2,
            modified ? accent : muted,
            FontWeights.SemiBold);

        var effective = string.IsNullOrWhiteSpace(parameter.EffectiveDisplay)
            ? "—"
            : Compact(parameter.EffectiveDisplay, 22);

        DrawCenteredText(
            dc,
            effective,
            new Point(layout.EffectiveCenter, top + 17.0),
            7.2,
            muted,
            FontWeights.Normal);

        var track = new Rect(layout.TrackLeft, top + 8.0, layout.TrackWidth, 20.0);
        var y = track.Top + track.Height / 2.0;

        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

        var currentX = track.Left + track.Width * Math.Clamp(relative / 2.0, 0.0, 1.0);
        var vanillaX = track.Left + track.Width * 0.5;

        dc.DrawLine(new Pen(muted, 1.1), new Point(vanillaX, y - 8.0), new Point(vanillaX, y + 8.0));
        dc.DrawLine(new Pen(accent, 3.0), new Point(track.Left, y), new Point(currentX, y));

        dc.DrawEllipse(
            panel,
            new Pen(accent, selected ? 2.5 : 1.5),
            new Point(currentX, y),
            5.8,
            5.8);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.6, 2.6);

        DrawText(dc, "0%", new Point(track.Left, top + 28.0), 6.2, muted, FontWeights.Normal);
        DrawCenteredText(dc, "VANILLA", new Point(vanillaX, top + 32.0), 6.1, muted, FontWeights.SemiBold);
        DrawRightAlignedText(dc, "200%", new Point(track.Right, top + 28.0), 6.2, muted, FontWeights.Normal);

        _targets[row] = new HitTarget(
            new Rect(track.Left - 7.0, track.Top - 6.0, track.Width + 14.0, track.Height + 12.0),
            track);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var row = FindTarget(e.GetPosition(this));
        if (row < 0)
            return;

        _selectedRow = row;
        _dragRow = row;
        CaptureMouse();
        UpdateFromPoint(row, e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var point = e.GetPosition(this);
        var hover = FindTarget(point);

        if (hover != _hoverRow)
        {
            _hoverRow = hover;
            InvalidateVisual();
        }

        Cursor = hover >= 0 ? Cursors.Hand : Cursors.Arrow;

        if (_dragRow < 0 || e.LeftButton != MouseButtonState.Pressed)
            return;

        UpdateFromPoint(_dragRow, point);
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        if (_hoverRow < 0)
            return;

        _hoverRow = -1;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_dragRow < 0)
            return;

        _dragRow = -1;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);

        var row = FindTarget(e.GetPosition(this));
        if (row < 0 || _parameters[row] is not { } parameter)
            return;

        _selectedRow = row;
        parameter.ResetToReference();
        _status.ShowTransient($"{Anomalies[row].Label} damage reset to Base Preset.");
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (e.Key == Key.R)
        {
            foreach (var parameter in _parameters)
                parameter?.ResetToReference();

            _status.ShowTransient("Individual anomaly damage reset to Base Preset.");
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var direction = e.Key switch
        {
            Key.Up or Key.Right or Key.Add or Key.OemPlus => 1,
            Key.Down or Key.Left or Key.Subtract or Key.OemMinus => -1,
            _ => 0
        };

        if (direction == 0 || _parameters[_selectedRow] is not { } selected)
            return;

        selected.CurrentNumber = Math.Clamp(
            selected.CurrentNumber + direction * selected.TickFrequency,
            selected.Minimum,
            selected.Maximum);

        _status.Clear();
        InvalidateVisual();
        e.Handled = true;
    }

    private void UpdateFromPoint(int row, Point point)
    {
        if (!_targets.TryGetValue(row, out var target) || _parameters[row] is not { } parameter)
            return;

        var ratio = Math.Clamp(
            (point.X - target.MapRect.Left) / Math.Max(1.0, target.MapRect.Width),
            0.0,
            1.0);

        var multiplier = ratio * 2.0;
        var raw = parameter.ReferenceNumber * multiplier;
        SetSnapped(parameter, raw);

        _status.Clear();
        InvalidateVisual();
    }

    private static void SetSnapped(ParameterViewModel parameter, double raw)
    {
        var tick = Math.Max(parameter.TickFrequency, Epsilon);
        var snapped = parameter.Minimum
            + Math.Round((raw - parameter.Minimum) / tick, MidpointRounding.AwayFromZero) * tick;

        parameter.CurrentNumber = Math.Clamp(snapped, parameter.Minimum, parameter.Maximum);
    }

    private int FindTarget(Point point)
    {
        foreach (var pair in _targets)
        {
            if (pair.Value.HitRect.Contains(point))
                return pair.Key;
        }

        return -1;
    }

    private Layout GetLayout()
    {
        var usable = Math.Max(560.0, ActualWidth - 40.0);
        var labelWidth = Math.Clamp(usable * 0.17, 105.0, 130.0);
        var baseWidth = 66.0;
        var relativeWidth = 76.0;
        var effectiveWidth = Math.Clamp(usable * 0.18, 105.0, 145.0);
        var fixedWidth = labelWidth + baseWidth + relativeWidth + effectiveWidth + 48.0;
        var trackWidth = Math.Max(150.0, usable - fixedWidth);

        var left = 22.0;
        var baseCenter = left + labelWidth + baseWidth / 2.0;
        var relativeCenter = left + labelWidth + baseWidth + relativeWidth / 2.0;
        var effectiveCenter = left + labelWidth + baseWidth + relativeWidth + effectiveWidth / 2.0;
        var trackLeft = left + labelWidth + baseWidth + relativeWidth + effectiveWidth + 22.0;

        return new Layout(left, baseCenter, relativeCenter, effectiveCenter, trackLeft, trackWidth);
    }

    private static string Compact(string value, int maxLength)
    {
        var compact = value.Replace(Environment.NewLine, " ").Trim();
        return compact.Length <= maxLength
            ? compact
            : compact[..Math.Max(1, maxLength - 1)] + "…";
    }

    private static Brush FindBrush(string key, Brush fallback)
        => Application.Current?.TryFindResource(key) as Brush ?? fallback;

    private static Brush CloneWithOpacity(Brush brush, double opacity)
    {
        var clone = brush.Clone();
        clone.Opacity = Math.Clamp(opacity, 0.0, 1.0);
        return clone;
    }

    private void DrawText(DrawingContext dc, string value, Point point, double size, Brush brush, FontWeight weight)
        => dc.DrawText(CreateText(value, size, brush, weight), point);

    private void DrawCenteredText(DrawingContext dc, string value, Point center, double size, Brush brush, FontWeight weight)
    {
        var formatted = CreateText(value, size, brush, weight);
        dc.DrawText(formatted, new Point(center.X - formatted.Width / 2.0, center.Y - formatted.Height / 2.0));
    }

    private void DrawRightAlignedText(DrawingContext dc, string value, Point right, double size, Brush brush, FontWeight weight)
    {
        var formatted = CreateText(value, size, brush, weight);
        dc.DrawText(formatted, new Point(right.X - formatted.Width, right.Y));
    }

    private FormattedText CreateText(string value, double size, Brush brush, FontWeight weight)
    {
        var formatted = new FormattedText(
            value,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            size,
            brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);

        var separatorBrush = FindBrush("AccentBrush", Brushes.Goldenrod);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '|')
                formatted.SetForegroundBrush(separatorBrush, i, 1);
        }

        return formatted;
    }

    private sealed record AnomalyDefinition(string ParameterId, string Label, bool IsMultiplier);
    private sealed record HitTarget(Rect HitRect, Rect MapRect);
    private sealed record Layout(
        double LabelLeft,
        double BaseCenter,
        double RelativeCenter,
        double EffectiveCenter,
        double TrackLeft,
        double TrackWidth);
}
