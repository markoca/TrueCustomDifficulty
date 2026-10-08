using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Rank-aware editor for ALife Director human squad caps.
///
/// The historical *Radar class name is kept for XAML compatibility, but the control
/// is rendered as a four-rank ladder because the gameplay values are discrete squad
/// caps rather than independent profile axes.
/// </summary>
public sealed class HumanGroupSizeRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double MinimumMultiplier = 0.25;
    private const double MaximumMultiplier = 3.0;

    private static readonly RankDefinition[] Ranks =
    {
        new("alife.humanGroupSize.newbie", "NEWBIE", 3.0),
        new("alife.humanGroupSize.experienced", "EXPERIENCED", 4.0),
        new("alife.humanGroupSize.veteran", "VETERAN", 5.0),
        new("alife.humanGroupSize.master", "MASTER", 6.0),
    };

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Ranks.Length];
    private readonly Dictionary<int, HitTarget> _targets = new();
    private readonly VisualStatusMessage _status;

    private int _selectedRow;
    private int _hoverRow = -1;
    private int _dragRow = -1;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(HumanGroupSizeRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public HumanGroupSizeRadar()
    {
        _status = new VisualStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = 320;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 700.0 : Math.Max(520.0, availableSize.Width);
        return new Size(width, 320.0);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((HumanGroupSizeRadar)d).RebindParameters();

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

            for (var i = 0; i < Ranks.Length; i++)
            {
                if (!byId.TryGetValue(Ranks[i].ParameterId, out var parameter))
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
            or nameof(ParameterViewModel.CurrentDisplay)
            or nameof(ParameterViewModel.IsModified)
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

        DrawCenteredText(dc, "HUMAN SQUAD CAPS", new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);
        DrawCenteredText(dc, "maximum ALife squad size at each player rank", new Point(ActualWidth / 2.0, 39.0),
            9.5, muted, FontWeights.Normal);

        DrawRightAlignedText(
            dc,
            "NO LAIR POPULATION / RESPAWN / SCENARIO-FREQUENCY CHANGES",
            new Point(Math.Max(470.0, ActualWidth - 18.0), 18.0),
            7.5,
            muted,
            FontWeights.SemiBold);

        var left = 24.0;
        var labelWidth = 108.0;
        var capWidth = 118.0;
        var trackLeft = left + labelWidth + capWidth + 24.0;
        var trackWidth = Math.Max(180.0, ActualWidth - trackLeft - 28.0);

        DrawText(dc, "PLAYER RANK", new Point(left, 66.0), 8.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "MAX SQUAD", new Point(left + labelWidth + capWidth / 2.0, 73.0), 8.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "RANK MULTIPLIER", new Point(trackLeft + trackWidth / 2.0, 73.0), 8.0, muted, FontWeights.Bold);

        dc.DrawLine(new Pen(border, 1.0), new Point(14.0, 87.0), new Point(Math.Max(14.0, ActualWidth - 14.0), 87.0));

        for (var row = 0; row < Ranks.Length; row++)
        {
            var top = 88.0 + row * 50.0;
            DrawRankRow(
                dc,
                row,
                top,
                left,
                labelWidth,
                capWidth,
                trackLeft,
                trackWidth,
                text,
                muted,
                border,
                accent,
                panel);
        }

        DrawCenteredText(
            dc,
            "GOLD = CURRENT RESULT   •   vN = VANILLA CAP   •   1x MARKER = BASE PRESET",
            new Point(ActualWidth / 2.0, 294.0),
            7.7,
            muted,
            FontWeights.Normal);

        if (!string.IsNullOrWhiteSpace(_status.Text))
        {
            DrawCenteredText(
                dc,
                _status.Text,
                new Point(ActualWidth / 2.0, 309.0),
                8.0,
                accent,
                FontWeights.SemiBold);
        }
    }

    private void DrawRankRow(
        DrawingContext dc,
        int row,
        double top,
        double left,
        double labelWidth,
        double capWidth,
        double trackLeft,
        double trackWidth,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var parameter = _parameters[row];
        if (parameter is null)
            return;

        var rank = Ranks[row];
        var selected = row == _selectedRow;
        var hovered = row == _hoverRow;
        var modified = Math.Abs(parameter.CurrentNumber - parameter.ReferenceNumber) > Epsilon;

        var rowRect = new Rect(14.0, top, Math.Max(1.0, ActualWidth - 28.0), 49.0);
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
            rank.Label,
            new Point(left, top + 14.0),
            9.0,
            modified ? accent : text,
            FontWeights.Bold);

        var currentCap = EffectiveCap(rank.VanillaCap, parameter.CurrentNumber);
        var vanillaCap = EffectiveCap(rank.VanillaCap, parameter.ReferenceNumber);
        var capCenter = left + labelWidth + capWidth / 2.0;

        DrawCenteredText(
            dc,
            currentCap.ToString("0", CultureInfo.InvariantCulture),
            new Point(capCenter, top + 17.0),
            13.0,
            modified ? accent : text,
            FontWeights.Bold);

        DrawCenteredText(
            dc,
            $"v{vanillaCap:0} NPCs",
            new Point(capCenter, top + 35.0),
            7.2,
            muted,
            FontWeights.Normal);

        var track = new Rect(trackLeft, top + 24.0, trackWidth, 12.0);
        var y = track.Top + track.Height / 2.0;
        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

        var vanillaX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, track);
        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track);

        dc.DrawLine(new Pen(muted, 1.1), new Point(vanillaX, y - 8.0), new Point(vanillaX, y + 8.0));
        dc.DrawLine(new Pen(accent, 3.5), new Point(track.Left, y), new Point(currentX, y));

        dc.DrawEllipse(
            panel,
            new Pen(accent, selected ? 2.5 : 1.5),
            new Point(currentX, y),
            6.5,
            6.5);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.8, 2.8);

        DrawRightAlignedText(dc, $"{parameter.CurrentNumber:0.##}x", new Point(track.Right, top + 5.0), 9.0, modified ? accent : muted, FontWeights.Bold);

        _targets[row] = new HitTarget(
            new Rect(track.Left - 7.0, track.Top - 6.0, track.Width + 14.0, track.Height + 12.0),
            track);
    }

    private static double EffectiveCap(double authoredCap, double multiplier)
        => Math.Max(1.0, Math.Floor(authoredCap * multiplier + 0.5));

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
        _status.ShowTransient($"{Ranks[row].Label} human cap reset to Base Preset.");
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

            _status.ShowTransient("Human squad caps reset to Base Preset.");
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

        var raw = parameter.Minimum + ratio * (parameter.Maximum - parameter.Minimum);
        var tick = Math.Max(parameter.TickFrequency, Epsilon);
        var snapped = parameter.Minimum
            + Math.Round((raw - parameter.Minimum) / tick, MidpointRounding.AwayFromZero) * tick;

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

    private static double ValueToX(double value, double minimum, double maximum, Rect track)
    {
        if (maximum <= minimum + Epsilon)
            return track.Left;

        var ratio = Math.Clamp((value - minimum) / (maximum - minimum), 0.0, 1.0);
        return track.Left + track.Width * ratio;
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

    private sealed record RankDefinition(string ParameterId, string Label, double VanillaCap);
    private sealed record HitTarget(Rect HitRect, Rect MapRect);
}
