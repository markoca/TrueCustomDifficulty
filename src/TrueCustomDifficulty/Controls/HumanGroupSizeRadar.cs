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
    private const double DesiredHeight = 350.0;
    private const double MaxCardWidth = 760.0;
    private const double RowTop = 50.0;
    private const double RowPitch = 72.0;
    private const double CardHeight = 66.0;

    private static readonly RankDefinition[] Ranks =
    {
        new("alife.humanGroupSize.newbie", "NEWBIE", 3.0),
        new("alife.humanGroupSize.experienced", "EXPERIENCED", 4.0),
        new("alife.humanGroupSize.veteran", "VETERAN", 5.0),
        new("alife.humanGroupSize.master", "MASTER", 6.0),
    };

    // The composited row is one native source; selecting it must show the
    // correct player-rank rationale instead of always the Newbie anchor.
    public event EventHandler<AggregateParameterSelectedEventArgs>? ParameterSelected;

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
        MinHeight = DesiredHeight;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 700.0 : Math.Max(520.0, availableSize.Width);
        return new Size(width, DesiredHeight);
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

        IndustrialChrome.DrawFrame(dc,
            new Rect(2.0, 2.0, Math.Max(0.0, ActualWidth - 4.0),
                Math.Max(0.0, ActualHeight - 4.0)),
            panel, border, accent, soft, 8.0,
            accentEdge: true, technicalMarks: true);

        _targets.Clear();
        DrawCenteredText(dc, "HUMAN SQUAD SIZE",
            new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);

        const double top = 111.0, bottom = 281.0;
        const double maxAxis = 3.0;
        var left = 62.0;
        var lane = (ActualWidth - left - 20.0) / Ranks.Length;
        for (var tick = 0; tick <= 6; tick++)
        {
            var yTick = bottom - (bottom - top) * tick / 6.0;
            dc.DrawLine(new Pen(CloneWithOpacity(border, .4), .8),
                new Point(left, yTick), new Point(ActualWidth - 20.0, yTick));
            DrawRightAlignedText(dc, $"{tick * .5:0.#}x",
                new Point(left - 8.0, yTick - 6.0), 9.0, muted, FontWeights.SemiBold);
        }
        var axis = new Point(12.0, (top + bottom) / 2);
        dc.PushTransform(new RotateTransform(-90, axis.X, axis.Y));
        DrawCenteredText(dc, "SQUAD SIZE", axis, 9.2, accent, FontWeights.Bold);
        dc.Pop();

        for (var i = 0; i < Ranks.Length; i++)
        {
            if (_parameters[i] is not { } parameter) continue;
            var center = left + (i + .5) * lane;
            var card = new Rect(left + i * lane + 4.0, 65.0, lane - 8.0, 268.0);
            dc.DrawRoundedRectangle(CloneWithOpacity(panel, .35),
                new Pen(CloneWithOpacity(border, .60), .8), card, 4, 4);
            var value = parameter.CurrentNumber;
            var currentY = bottom - (bottom - top) * Math.Clamp(value / maxAxis, 0, 1);
            var referenceY = bottom - (bottom - top) *
                Math.Clamp(parameter.ReferenceNumber / maxAxis, 0, 1);
            var barWidth = Math.Min(40.0, lane * .35);
            var barLeft = center - barWidth / 2;
            dc.DrawRectangle(CloneWithOpacity(border, .12), null,
                new Rect(barLeft, top, barWidth, bottom - top));
            dc.DrawRectangle(CloneWithOpacity(accent, .72), null,
                new Rect(barLeft, currentY, barWidth, Math.Max(.5, bottom - currentY)));
            dc.DrawLine(new Pen(CloneWithOpacity(muted, .68), 1.35),
                new Point(barLeft - 5, referenceY), new Point(barLeft + barWidth + 5, referenceY));
            VerticalBarHandleChrome.Draw(dc, new Point(center, currentY),
                barWidth, accent, _hoverRow == i);
            var cap = EffectiveCap(Ranks[i].VanillaCap, value);
            DrawCenteredText(dc, $"{cap:0} NPC", new Point(center, 82.0), 12,
                parameter.IsValueModifiedFromReference() ? accent : text, FontWeights.Bold);
            DrawCenteredText(dc, $"{value:0.##}x", new Point(center, 100.0), 9.5,
                muted, FontWeights.SemiBold);
            DrawCenteredText(dc, Ranks[i].Label, new Point(center, 307.0),
                9.7, text, FontWeights.Bold);
            _targets[i] = new HitTarget(
                new Rect(center - Math.Max(16, barWidth / 2 + 6), top,
                    Math.Max(32, barWidth + 12), bottom - top),
                new Rect(barLeft, top, barWidth, bottom - top));
        }
    }

    // Identical half-up integer policy to the original panel/CFG writer.
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
        if (_parameters[row] is { } selected)
            ParameterSelected?.Invoke(this,
                new AggregateParameterSelectedEventArgs(selected));
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

        Cursor = hover >= 0 ? Cursors.SizeNS : Cursors.Arrow;

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
        ParameterSelected?.Invoke(this,
            new AggregateParameterSelectedEventArgs(parameter));
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
            (target.MapRect.Bottom - point.Y) / Math.Max(1.0, target.MapRect.Height),
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
