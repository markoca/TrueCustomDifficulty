using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Mutant special-ability pacing board.
///
/// The historical *Radar class name is kept for XAML compatibility. Each row edits
/// the wait-time multiplier for one mutant family and derives the corresponding
/// relative ability-return rate without inventing absolute cooldown seconds.
/// </summary>
public sealed class SpecialAbilityCooldownRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double RowHeight = 47.0;

    private static readonly CooldownDefinition[] Mutants =
    {
        new("combat.mutantCooldown.bloodsucker", "Bloodsucker", "Mist follows"),
        new("combat.mutantCooldown.chimera", "Chimera", null),
        new("combat.mutantCooldown.deer", "Deer", null),
        new("combat.mutantCooldown.pseudogiant", "Pseudogiant", null),
        new("combat.mutantCooldown.pseudodog", "Pseudodog", null),
        new("combat.mutantCooldown.burer", "Burer", "Shooting variant follows"),
        new("combat.mutantCooldown.bayun", "Bayun", "Collar / quest variants follow"),
        new("combat.mutantCooldown.controller", "Controller", "Collar variant follows"),
    };

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Mutants.Length];
    private readonly Dictionary<int, HitTarget> _targets = new();
    private readonly RadarStatusMessage _status;

    private int _selectedRow;
    private int _hoverRow = -1;
    private int _dragRow = -1;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(SpecialAbilityCooldownRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public SpecialAbilityCooldownRadar()
    {
        _status = new RadarStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = 505;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 760.0 : Math.Max(560.0, availableSize.Width);
        return new Size(width, 505.0);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((SpecialAbilityCooldownRadar)d).RebindParameters();

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

            for (var i = 0; i < Mutants.Length; i++)
            {
                if (!byId.TryGetValue(Mutants[i].ParameterId, out var parameter))
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
            or nameof(ParameterViewModel.ReferenceNumber)
            or nameof(ParameterViewModel.ReferenceLabel))
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

        DrawText(dc, "MUTANT SPECIAL-ABILITY PACING", new Point(18.0, 16.0), 12.0, text, FontWeights.Bold);
        DrawText(
            dc,
            "lower wait = abilities return sooner; damage is unchanged",
            new Point(18.0, 34.0),
            8.2,
            muted,
            FontWeights.Normal);

        DrawRightAlignedText(
            dc,
            "WAIT MULTIPLIER — NOT ABSOLUTE COOLDOWN SECONDS",
            new Point(Math.Max(500.0, ActualWidth - 18.0), 18.0),
            7.5,
            muted,
            FontWeights.SemiBold);

        var layout = GetLayout();

        DrawText(dc, "MUTANT", new Point(layout.LabelLeft, 62.0), 8.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "WAIT", new Point(layout.WaitCenter, 69.0), 8.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "ABILITY RETURN", new Point(layout.ReturnCenter, 69.0), 8.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "AGGRESSIVE  ←  WAIT TIME  →  BREATHING ROOM", new Point(layout.TrackLeft + layout.TrackWidth / 2.0, 69.0), 7.8, muted, FontWeights.Bold);

        dc.DrawLine(new Pen(border, 1.0), new Point(14.0, 83.0), new Point(Math.Max(14.0, ActualWidth - 14.0), 83.0));

        for (var row = 0; row < Mutants.Length; row++)
            DrawRow(dc, row, 92.0 + row * RowHeight, layout, text, muted, border, accent, panel);

        var footerY = 92.0 + Mutants.Length * RowHeight + 9.0;
        DrawCenteredText(
            dc,
            "RETURN RATE = 1 ÷ WAIT MULTIPLIER   •   GREY TICK = BASE PRESET   •   LOWER WAIT = MORE FREQUENT SPECIALS",
            new Point(ActualWidth / 2.0, footerY),
            7.5,
            muted,
            FontWeights.Normal);

        if (!string.IsNullOrWhiteSpace(_status.Text))
        {
            DrawCenteredText(
                dc,
                _status.Text,
                new Point(ActualWidth / 2.0, footerY + 19.0),
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

        var info = Mutants[row];
        var selected = row == _selectedRow;
        var hovered = row == _hoverRow;
        var modified = Math.Abs(parameter.CurrentNumber - parameter.ReferenceNumber) > Epsilon;

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
            new Point(layout.LabelLeft, top + 7.0),
            8.6,
            modified ? accent : text,
            FontWeights.Bold);

        if (!string.IsNullOrWhiteSpace(info.SecondaryNote))
            DrawText(dc, info.SecondaryNote!, new Point(layout.LabelLeft, top + 24.0), 6.7, muted, FontWeights.Normal);

        DrawCenteredText(
            dc,
            $"{parameter.CurrentNumber:0.##}x",
            new Point(layout.WaitCenter, top + 16.0),
            10.0,
            modified ? accent : text,
            FontWeights.Bold);

        var returnRate = parameter.CurrentNumber > Epsilon ? 1.0 / parameter.CurrentNumber : 0.0;
        DrawCenteredText(
            dc,
            $"{returnRate:0.##}x",
            new Point(layout.ReturnCenter, top + 13.0),
            9.3,
            returnRate > 1.0 + Epsilon ? accent : text,
            FontWeights.Bold);

        DrawCenteredText(
            dc,
            returnRate > 1.05
                ? "MORE OFTEN"
                : returnRate < 0.95
                    ? "LESS OFTEN"
                    : "BASE RATE",
            new Point(layout.ReturnCenter, top + 29.0),
            6.6,
            returnRate > 1.05 ? accent : muted,
            FontWeights.SemiBold);

        var track = new Rect(layout.TrackLeft, top + 10.0, layout.TrackWidth, 21.0);
        var y = track.Top + track.Height / 2.0;

        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

        var baseX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, track);
        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track);

        dc.DrawLine(new Pen(muted, 1.1), new Point(baseX, y - 8.0), new Point(baseX, y + 8.0));
        dc.DrawLine(new Pen(accent, 3.0), new Point(track.Left, y), new Point(currentX, y));

        dc.DrawEllipse(
            panel,
            new Pen(accent, selected ? 2.5 : 1.5),
            new Point(currentX, y),
            6.0,
            6.0);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.7, 2.7);

        DrawText(dc, "0.25x", new Point(track.Left, top + 31.0), 6.6, muted, FontWeights.Normal);
        DrawCenteredText(dc, $"BASE {parameter.ReferenceNumber:0.##}x", new Point(baseX, top + 35.0), 6.5, muted, FontWeights.SemiBold);
        DrawRightAlignedText(dc, "2x", new Point(track.Right, top + 31.0), 6.6, muted, FontWeights.Normal);

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
        _status.ShowTransient($"{Mutants[row].Label} special-ability wait reset to Base Preset.");
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

            _status.ShowTransient("Mutant special-ability pacing reset to Base Preset.");
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

    private Layout GetLayout()
    {
        var usable = Math.Max(520.0, ActualWidth - 40.0);
        var labelWidth = Math.Clamp(usable * 0.18, 108.0, 138.0);
        var waitWidth = 68.0;
        var returnWidth = 88.0;
        var fixedWidth = labelWidth + waitWidth + returnWidth + 46.0;
        var trackWidth = Math.Max(170.0, usable - fixedWidth);

        var left = 22.0;
        var waitCenter = left + labelWidth + waitWidth / 2.0;
        var returnCenter = left + labelWidth + waitWidth + returnWidth / 2.0;
        var trackLeft = left + labelWidth + waitWidth + returnWidth + 22.0;

        return new Layout(left, waitCenter, returnCenter, trackLeft, trackWidth);
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

    private sealed record CooldownDefinition(string ParameterId, string Label, string? SecondaryNote);
    private sealed record HitTarget(Rect HitRect, Rect MapRect);
    private sealed record Layout(
        double LabelLeft,
        double WaitCenter,
        double ReturnCenter,
        double TrackLeft,
        double TrackWidth);
}
