using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Compact rank-aware editor for ALife Director mutant encounter caps.
/// </summary>
public sealed class MutantGroupSizeRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double DesiredHeight = 546.0;
    private const double MaxContentWidth = 784.0;
    private const double ColumnGap = 12.0;
    private const double RowPitch = 68.0;
    private const double RowTop = 64.0;
    private const double CardHeight = 61.0;

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

    public event EventHandler<AggregateParameterSelectedEventArgs>? ParameterSelected;

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Axes.Length];
    private readonly Dictionary<int, HitTarget> _targets = new();
    private readonly VisualStatusMessage _status;

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
        _status = new VisualStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = DesiredHeight;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 760.0 : Math.Max(560.0, availableSize.Width);
        return new Size(width, DesiredHeight);
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

        IndustrialChrome.DrawFrame(dc,
            new Rect(2.0, 2.0, Math.Max(0.0, ActualWidth - 4.0),
                Math.Max(0.0, ActualHeight - 4.0)),
            panel, border, accent, soft, 8.0,
            accentEdge: true, technicalMarks: true);

        _targets.Clear();
        DrawCenteredText(dc, "MUTANT GROUP SIZE",
            new Point(ActualWidth / 2.0, 21.0),
            VisualControlTypography.BoardTitle, accent, FontWeights.Bold);
        DrawCenteredText(dc, "14 SPECIES  |  N / E / V / M = PLAYER RANK",
            new Point(ActualWidth / 2.0, 43.0),
            9.2, muted, FontWeights.SemiBold);

        // The actual 14 species remain alphabetically ordered, split
        // seven to the left and seven to the right, with bounded tracks.
        var content = ContentRect();
        var columnWidth = (content.Width - ColumnGap) / 2.0;
        for (var i = 0; i < 7; i++)
        {
            DrawSpeciesCard(dc, i,
                new Rect(content.Left, RowTop + i * RowPitch,
                    columnWidth, CardHeight),
                text, muted, border, accent, panel);
            DrawSpeciesCard(dc, i + 7,
                new Rect(content.Left + columnWidth + ColumnGap,
                    RowTop + i * RowPitch, columnWidth, CardHeight),
                text, muted, border, accent, panel);
        }
    }

    private void DrawSpeciesCard(
        DrawingContext dc, int row, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        if (_parameters[row] is not { } parameter)
            return;

        var info = Axes[row];
        var selected = row == _selectedRow;
        var hovered = row == _hoverRow;
        var modified = parameter.IsValueModifiedFromReference();
        var currentCaps = EffectiveCaps(info.AuthoredCaps, parameter.CurrentNumber);
        var referenceCaps = EffectiveCaps(info.AuthoredCaps, parameter.ReferenceNumber);

        dc.DrawRoundedRectangle(panel,
            new Pen(CloneWithOpacity(border, 0.65), 0.9),
            card, 4.0, 4.0);

        DrawText(dc, info.Label.ToUpperInvariant(),
            new Point(card.Left + 10.0, card.Top + 6.0),
            10.0, text, FontWeights.Bold);
        DrawRightAlignedText(dc, $"{parameter.CurrentNumber:0.##}x",
            new Point(card.Right - 10.0, card.Top + 5.0),
            modified ? 12.2 : 11.0,
            modified ? accent : text, FontWeights.Bold);

        DrawText(dc, "N / E / V / M",
            new Point(card.Left + 10.0, card.Top + 25.0),
            9.2, muted, FontWeights.Medium);

        // Color and enlarge only those resulting cap numbers which actually
        // differ from their own source reference. In particular, authored
        // zero caps stay 0 and NEVER falsely turn gold when scaling a rank
        // where a species does not normally spawn.
        var countTexts = currentCaps
            .Select(cap => cap.ToString("0", CultureInfo.InvariantCulture))
            .ToArray();
        var joinedCaps = string.Join(" / ", countTexts);
        var formatted = CreateText(joinedCaps, 10.8, text, FontWeights.Bold);
        var offset = 0;
        for (var index = 0; index < countTexts.Length; index++)
        {
            if (Math.Abs(currentCaps[index] - referenceCaps[index]) > Epsilon)
            {
                formatted.SetForegroundBrush(accent, offset, countTexts[index].Length);
                formatted.SetFontSize(12.0, offset, countTexts[index].Length);
            }
            offset += countTexts[index].Length + (index < countTexts.Length - 1 ? 3 : 0);
        }
        dc.DrawText(formatted, new Point(
            card.Right - 10.0 - formatted.WidthIncludingTrailingWhitespace,
            card.Top + 23.0));

        var track = new Rect(card.Left + 10.0, card.Top + 42.0,
            Math.Max(1.0, card.Width - 20.0), 10.0);
        var y = track.Top + track.Height / 2.0;
        var referenceX = ValueToX(parameter.ReferenceNumber,
            parameter.Minimum, parameter.Maximum, track);
        var currentX = ValueToX(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, track);

        dc.DrawLine(new Pen(border, 3.0),
            new Point(track.Left, y), new Point(track.Right, y));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.75), 3.0),
            new Point(track.Left, y), new Point(currentX, y));
        dc.DrawLine(new Pen(muted, 1.5),
            new Point(referenceX, y - 7.0), new Point(referenceX, y + 7.0));
        ControlHoverGlow.Draw(dc, new Point(currentX, y), accent, hovered);
        dc.DrawEllipse(panel, new Pen(accent, selected ? 2.3 : 1.5),
            new Point(currentX, y), 5.7, 5.7);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.4, 2.4);

        _targets[row] = new HitTarget(
            new Rect(track.Left - 6.0, track.Top - 6.0,
                track.Width + 12.0, track.Height + 12.0), track);
    }

    private Rect ContentRect()
    {
        var width = Math.Min(MaxContentWidth, Math.Max(1.0, ActualWidth - 32.0));
        return new Rect((ActualWidth - width) / 2.0, 0.0, width, ActualHeight);
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
        ParameterSelected?.Invoke(this,
            new AggregateParameterSelectedEventArgs(parameter));
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

        var ratio = Math.Clamp((point.X - target.MapRect.Left) / Math.Max(1.0, target.MapRect.Width), 0.0, 1.0);
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

    private sealed record AxisDefinition(string ParameterId, string Label, double[] AuthoredCaps);
    private sealed record HitTarget(Rect HitRect, Rect MapRect);
}
