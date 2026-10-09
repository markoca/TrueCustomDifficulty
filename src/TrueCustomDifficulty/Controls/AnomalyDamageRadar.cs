using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

public sealed class AnomalyDamageRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double DesiredHeight = 420.0;
    private const double MaxContentWidth = 900.0;
    private const double ColumnGap = 16.0;
    private const double RowHeight = 49.0;

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
        MinHeight = DesiredHeight;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 780.0 : Math.Max(580.0, availableSize.Width);
        return new Size(width, DesiredHeight);
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
            var byId = Parameters.OfType<ParameterViewModel>().ToDictionary(x => x.Id, StringComparer.Ordinal);
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

        var content = ContentRect();
        DrawCenteredText(dc, "INDIVIDUAL ANOMALY DAMAGE", new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);


        var columnWidth = (content.Width - ColumnGap) / 2.0;
        for (var i = 0; i < 6; i++)
        {
            if (i < 6)
                DrawRow(dc, i, new Rect(content.Left, 68.0 + i * RowHeight, columnWidth, RowHeight - 5.0), text, muted, border, accent, panel);

            var right = i + 6;
            if (right < Anomalies.Length)
                DrawRow(dc, right, new Rect(content.Left + columnWidth + ColumnGap, 68.0 + i * RowHeight, columnWidth, RowHeight - 5.0), text, muted, border, accent, panel);
        }

        DrawCenteredText(
            dc,
            "Global Anomaly Damage and player protection stay separate",
            new Point(ActualWidth / 2.0, 372.0),
            9.0,
            muted,
            FontWeights.Normal);

        // No transient yellow reset notifications.
    }

    private void DrawRow(
        DrawingContext dc,
        int row,
        Rect card,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        if (_parameters[row] is not { } parameter)
            return;

        var info = Anomalies[row];
        var reference = parameter.ReferenceNumber;
        var relative = Math.Abs(reference) > Epsilon ? parameter.CurrentNumber / reference : 1.0;
        var modified = Math.Abs(parameter.CurrentNumber - reference) > Epsilon;
        var selected = row == _selectedRow;
        var hovered = row == _hoverRow;

        dc.DrawRoundedRectangle(
            selected ? CloneWithOpacity(accent, 0.08)
            : hovered ? CloneWithOpacity(accent, 0.09)
            : CloneWithOpacity(panel, 0.02),
            new Pen(CloneWithOpacity(border, 0.45), 0.8),
            card,
            3.0,
            3.0);

        var baseText = info.IsMultiplier
            ? $"{parameter.CurrentNumber:0.##}x"
            : $"{parameter.CurrentNumber:0.##}";

        DrawText(dc, info.Label, new Point(card.Left + 8.0, card.Top + 5.0), 9.3, text, FontWeights.Bold);
        DrawRightAlignedText(
            dc,
            baseText,
            new Point(card.Right - 8.0, card.Top + 5.0),
            9.3,
            hovered ? accent : text,
            FontWeights.Bold);

        var track = new Rect(card.Left + 10.0, card.Top + 27.0, card.Width - 20.0, 10.0);
        var y = track.Top + track.Height / 2.0;
        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

        var currentX = track.Left + track.Width * Math.Clamp(relative / 2.0, 0.0, 1.0);
        var vanillaX = track.Left + track.Width * 0.5;

        dc.DrawLine(new Pen(muted, 1.0), new Point(vanillaX, y - 5.0), new Point(vanillaX, y + 5.0));
        dc.DrawLine(new Pen(accent, 3.0), new Point(track.Left, y), new Point(currentX, y));
        dc.DrawEllipse(panel, new Pen(accent, selected ? 2.4 : 1.4), new Point(currentX, y), 5.1, 5.1);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.2, 2.2);

        _targets[row] = new HitTarget(
            new Rect(track.Left - 6.0, track.Top - 6.0, track.Width + 12.0, track.Height + 12.0),
            track);
    }

    private Rect ContentRect()
    {
        var width = Math.Min(MaxContentWidth, Math.Max(540.0, ActualWidth - 32.0));
        return new Rect((ActualWidth - width) / 2.0, 0.0, width, ActualHeight);
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

        if (_dragRow >= 0 && e.LeftButton == MouseButtonState.Pressed)
        {
            UpdateFromPoint(_dragRow, point);
            e.Handled = true;
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
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

            _status.ShowTransient("Anomaly damage reset to Base Preset.");
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

        var ratio = Math.Clamp((point.X - target.MapRect.Left) / Math.Max(1.0, target.MapRect.Width), 0.0, 1.0);
        var raw = parameter.ReferenceNumber * (ratio * 2.0);
        var tick = Math.Max(parameter.TickFrequency, Epsilon);
        var snapped = parameter.Minimum + Math.Round((raw - parameter.Minimum) / tick, MidpointRounding.AwayFromZero) * tick;

        parameter.CurrentNumber = Math.Clamp(snapped, parameter.Minimum, parameter.Maximum);
        _status.Clear();
        InvalidateVisual();
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

    private static string Compact(string value, int maxLength)
    {
        var compact = value.Replace(Environment.NewLine, " ").Trim();
        return compact.Length <= maxLength ? compact : compact[..Math.Max(1, maxLength - 1)] + "…";
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
            Math.Max(9.0, size),
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
}
