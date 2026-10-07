using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Rank-aware editor for ALife Director mutant encounter caps.
///
/// The class keeps its historical *Radar name for XAML compatibility, but the renderer
/// is intentionally a matrix: every species owns one multiplier and the four resulting
/// Newbie / Experienced / Veteran / Master caps stay visible at the same time.
/// </summary>
public sealed class MutantGroupSizeRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double RowHeight = 40.0;
    private const double MinimumMultiplier = 0.25;
    private const double MaximumMultiplier = 3.0;

    private static readonly AxisDefinition[] Axes =
    {
        new("alife.mutantSpawn.bayun", "Bayun", new[] { 1.0, 1.0, 1.0, 2.0 }),
        new("alife.mutantSpawn.blindDog", "Blind Dog", new[] { 4.0, 6.0, 8.0, 12.0 }),
        new("alife.mutantSpawn.bloodsucker", "Bloodsucker", new[] { 0.0, 1.0, 1.0, 1.0 }),
        new("alife.mutantSpawn.boar", "Boar", new[] { 2.0, 3.0, 5.0, 6.0 }),
        new("alife.mutantSpawn.burer", "Burer", new[] { 1.0, 1.0, 1.0, 2.0 }),
        new("alife.mutantSpawn.chimera", "Chimera", new[] { 1.0, 1.0, 1.0, 1.0 }),
        new("alife.mutantSpawn.controller", "Controller", new[] { 0.0, 1.0, 1.0, 1.0 }),
        new("alife.mutantSpawn.deer", "Deer", new[] { 1.0, 1.0, 1.0, 1.0 }),
        new("alife.mutantSpawn.flesh", "Flesh", new[] { 2.0, 3.0, 5.0, 6.0 }),
        new("alife.mutantSpawn.poltergeist", "Poltergeist", new[] { 1.0, 1.0, 2.0, 2.0 }),
        new("alife.mutantSpawn.pseudodog", "Pseudodog", new[] { 1.0, 1.0, 1.0, 1.0 }),
        new("alife.mutantSpawn.ratSwarm", "Rat Swarm", new[] { 1.0, 1.0, 1.0, 1.0 }),
        new("alife.mutantSpawn.snork", "Snork", new[] { 1.0, 3.0, 4.0, 6.0 }),
        new("alife.mutantSpawn.tushkan", "Tushkan", new[] { 6.0, 8.0, 12.0, 16.0 }),
    };

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Axes.Length];
    private readonly Dictionary<int, HitTarget> _targets = new();
    private readonly RadarStatusMessage _status;

    private int _selectedRow;
    private int _hoverRow = -1;
    private int _dragRow = -1;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(MutantGroupSizeRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public MutantGroupSizeRadar()
    {
        _status = new RadarStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = 730;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 760.0 : Math.Max(560.0, availableSize.Width);
        return new Size(width, 730.0);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MutantGroupSizeRadar)d).RebindParameters();

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

            for (var i = 0; i < Axes.Length; i++)
            {
                if (!byId.TryGetValue(Axes[i].ParameterId, out var parameter))
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

        DrawText(dc, "MUTANT ENCOUNTER CAPS", new Point(18.0, 16.0), 12.0, text, FontWeights.Bold);
        DrawText(
            dc,
            "one species multiplier preserves its authored Newbie → Master progression",
            new Point(18.0, 34.0),
            8.2,
            muted,
            FontWeights.Normal);

        DrawRightAlignedText(
            dc,
            "CAPS ONLY — NO LAIR POPULATION / RESPAWN / SCENARIO-FREQUENCY CHANGES",
            new Point(Math.Max(520.0, ActualWidth - 18.0), 18.0),
            7.5,
            muted,
            FontWeights.SemiBold);

        var layout = GetLayout();

        DrawText(dc, "MUTANT", new Point(layout.LabelLeft, 61.0), 8.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "MULT", new Point(layout.MultiplierCenter, 68.0), 8.0, muted, FontWeights.Bold);

        var ranks = new[] { "NEWBIE", "EXPERIENCED", "VETERAN", "MASTER" };
        for (var i = 0; i < ranks.Length; i++)
            DrawCenteredText(dc, ranks[i], new Point(layout.RankCenters[i], 68.0), 7.8, muted, FontWeights.Bold);

        DrawCenteredText(dc, "SPECIES MULTIPLIER", new Point(layout.TrackLeft + layout.TrackWidth / 2.0, 68.0), 8.0, muted, FontWeights.Bold);

        var headerY = 82.0;
        dc.DrawLine(new Pen(border, 1.0), new Point(14.0, headerY), new Point(Math.Max(14.0, ActualWidth - 14.0), headerY));

        for (var row = 0; row < Axes.Length; row++)
            DrawRow(dc, row, 90.0 + row * RowHeight, layout, text, muted, border, soft, accent, panel);

        var footerY = 90.0 + Axes.Length * RowHeight + 9.0;
        DrawCenteredText(
            dc,
            "GOLD NUMBERS = CURRENT CAPS   •   SMALL GREY NUMBERS = VANILLA CAPS   •   AUTHORED 0 STAYS 0",
            new Point(ActualWidth / 2.0, footerY),
            7.6,
            muted,
            FontWeights.Normal);

        if (!string.IsNullOrWhiteSpace(_status.Text))
        {
            DrawCenteredText(
                dc,
                _status.Text,
                new Point(ActualWidth / 2.0, footerY + 20.0),
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
        Brush soft,
        Brush accent,
        Brush panel)
    {
        var parameter = _parameters[row];
        if (parameter is null)
            return;

        var info = Axes[row];
        var selected = row == _selectedRow;
        var hovered = row == _hoverRow;
        var modified = Math.Abs(parameter.CurrentNumber - parameter.ReferenceNumber) > Epsilon;

        var rowRect = new Rect(14.0, top, Math.Max(1.0, ActualWidth - 28.0), RowHeight - 3.0);
        var rowFill = selected
            ? CloneWithOpacity(accent, 0.08)
            : hovered
                ? CloneWithOpacity(border, 0.10)
                : CloneWithOpacity(panel, 0.01);

        dc.DrawRoundedRectangle(rowFill, new Pen(CloneWithOpacity(border, 0.45), 0.8), rowRect, 3.0, 3.0);

        DrawText(
            dc,
            info.Label,
            new Point(layout.LabelLeft, top + 10.0),
            8.5,
            hovered ? text : modified ? accent : text,
            modified ? FontWeights.Bold : FontWeights.SemiBold);

        DrawCenteredText(
            dc,
            $"{parameter.CurrentNumber:0.##}x",
            new Point(layout.MultiplierCenter, top + 18.0),
            8.4,
            modified ? accent : muted,
            modified ? FontWeights.Bold : FontWeights.SemiBold);

        var currentCaps = EffectiveCaps(info.AuthoredCaps, parameter.CurrentNumber);
        var vanillaCaps = EffectiveCaps(info.AuthoredCaps, parameter.ReferenceNumber);

        var rowMax = Math.Max(1.0, currentCaps.Concat(vanillaCaps).Max());

        for (var rank = 0; rank < 4; rank++)
        {
            var centerX = layout.RankCenters[rank];
            var cell = new Rect(centerX - layout.RankCellWidth / 2.0, top + 4.0, layout.RankCellWidth, RowHeight - 11.0);

            if (info.AuthoredCaps[rank] <= Epsilon)
            {
                dc.DrawRoundedRectangle(CloneWithOpacity(border, 0.08), null, cell, 2.0, 2.0);
                DrawCenteredText(dc, "0", new Point(centerX, top + 15.0), 9.0, muted, FontWeights.Bold);
                DrawCenteredText(dc, "LOCKED", new Point(centerX, top + 28.0), 6.6, muted, FontWeights.Normal);
                continue;
            }

            var relative = Math.Clamp(currentCaps[rank] / rowMax, 0.0, 1.0);
            var fillRect = new Rect(
                cell.Left + 2.0,
                cell.Bottom - 3.0 - (cell.Height - 6.0) * relative,
                cell.Width - 4.0,
                Math.Max(1.0, (cell.Height - 6.0) * relative));

            dc.DrawRoundedRectangle(CloneWithOpacity(border, 0.07), new Pen(CloneWithOpacity(border, 0.35), 0.7), cell, 2.0, 2.0);
            dc.DrawRectangle(CloneWithOpacity(accent, modified ? 0.16 : 0.08), null, fillRect);

            DrawCenteredText(
                dc,
                currentCaps[rank].ToString("0", CultureInfo.InvariantCulture),
                new Point(centerX, top + 14.0),
                9.0,
                modified ? accent : text,
                FontWeights.Bold);

            DrawCenteredText(
                dc,
                $"v{vanillaCaps[rank]:0}",
                new Point(centerX, top + 28.0),
                6.8,
                muted,
                FontWeights.Normal);
        }

        var track = new Rect(layout.TrackLeft, top + 9.0, layout.TrackWidth, 20.0);
        var y = track.Top + track.Height / 2.0;

        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

        var vanillaX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, track);
        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track);

        dc.DrawLine(new Pen(muted, 1.0), new Point(vanillaX, y - 7.0), new Point(vanillaX, y + 7.0));
        dc.DrawLine(new Pen(accent, 3.0), new Point(track.Left, y), new Point(currentX, y));

        dc.DrawEllipse(
            panel,
            new Pen(accent, selected ? 2.5 : 1.5),
            new Point(currentX, y),
            6.0,
            6.0);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.7, 2.7);

        DrawText(dc, "0.25x", new Point(track.Left, top + 27.0), 6.7, muted, FontWeights.Normal);
        DrawCenteredText(dc, "1x", new Point(vanillaX, top + 31.0), 6.7, muted, FontWeights.SemiBold);
        DrawRightAlignedText(dc, "3x", new Point(track.Right, top + 27.0), 6.7, muted, FontWeights.Normal);

        _targets[row] = new HitTarget(
            new Rect(track.Left - 7.0, track.Top - 6.0, track.Width + 14.0, track.Height + 12.0),
            track);
    }

    private static double[] EffectiveCaps(IReadOnlyList<double> authored, double multiplier)
        => authored
            .Select(value => value <= Epsilon
                ? 0.0
                : Math.Max(1.0, Math.Floor(value * multiplier + 0.5)))
            .ToArray();

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
        _status.ShowTransient($"{Axes[row].Label} reset to Base Preset.");
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

            _status.ShowTransient("Mutant encounter caps reset to Base Preset.");
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
        var usable = Math.Max(520.0, ActualWidth - 36.0);
        var labelWidth = Math.Clamp(usable * 0.16, 92.0, 125.0);
        var multiplierWidth = 54.0;
        var rankCellWidth = Math.Clamp(usable * 0.075, 42.0, 58.0);
        var rankBlockWidth = rankCellWidth * 4.0;
        var fixedWidth = labelWidth + multiplierWidth + rankBlockWidth + 42.0;
        var trackWidth = Math.Max(120.0, usable - fixedWidth);

        var left = 22.0;
        var labelLeft = left;
        var multiplierCenter = left + labelWidth + multiplierWidth / 2.0;
        var ranksLeft = left + labelWidth + multiplierWidth + 10.0;

        var rankCenters = Enumerable.Range(0, 4)
            .Select(i => ranksLeft + rankCellWidth * (i + 0.5))
            .ToArray();

        var trackLeft = ranksLeft + rankBlockWidth + 18.0;

        return new Layout(
            labelLeft,
            multiplierCenter,
            rankCenters,
            rankCellWidth,
            trackLeft,
            trackWidth);
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

    private sealed record AxisDefinition(string ParameterId, string Label, double[] AuthoredCaps);
    private sealed record HitTarget(Rect HitRect, Rect MapRect);
    private sealed record Layout(
        double LabelLeft,
        double MultiplierCenter,
        double[] RankCenters,
        double RankCellWidth,
        double TrackLeft,
        double TrackWidth);
}
