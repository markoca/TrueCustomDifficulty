using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

public sealed class MutantHealthRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double GlobalHpMaximum = 7500.0;
    private const double DesiredHeight = 380.0;
    private const double MaxContentWidth = 900.0;
    private const double ColumnGap = 16.0;
    private const double RowHeight = 62.0;

    private static readonly MutantDefinition[] Mutants =
    {
        new("combat.mutantHealth.bloodsucker", "Bloodsucker", 500.0, true),
        new("combat.mutantHealth.chimera", "Chimera", 1400.0, false),
        new("combat.mutantHealth.deer", "Deer", 600.0, false),
        new("combat.mutantHealth.pseudogiant", "Pseudogiant", 2500.0, false),
        new("combat.mutantHealth.pseudodog", "Pseudodog", 300.0, false),
        new("combat.mutantHealth.burer", "Burer", 400.0, false),
        new("combat.mutantHealth.bayun", "Bayun", 400.0, false),
        new("combat.mutantHealth.controller", "Controller", 500.0, false),
    };

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Mutants.Length];
    private readonly Dictionary<int, HitTarget> _targets = new();
    private readonly VisualStatusMessage _status;
    private int _selectedRow;
    private int _hoverRow = -1;
    private int _dragRow = -1;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(MutantHealthRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public MutantHealthRadar()
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
        => ((MutantHealthRadar)d).RebindParameters();

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
        DrawText(dc, "MUTANT HEALTH", new Point(content.Left, 16.0), 12.5, text, FontWeights.Bold);
        DrawText(dc, "live MaxHP + direct multiplier", new Point(content.Left, 37.0), 9.5, muted, FontWeights.Normal);

        var columnWidth = (content.Width - ColumnGap) / 2.0;
        for (var i = 0; i < 4; i++)
        {
            DrawRow(dc, i, new Rect(content.Left, 68.0 + i * RowHeight, columnWidth, RowHeight - 6.0), text, muted, border, accent, panel);
            DrawRow(dc, i + 4, new Rect(content.Left + columnWidth + ColumnGap, 68.0 + i * RowHeight, columnWidth, RowHeight - 6.0), text, muted, border, accent, panel);
        }

        DrawCenteredText(
            dc,
            "HP bar is shared 0–7500   •   grey tick = Base Preset",
            new Point(ActualWidth / 2.0, 332.0),
            9.0,
            muted,
            FontWeights.Normal);

        if (!string.IsNullOrWhiteSpace(_status.Text))
            DrawCenteredText(dc, _status.Text, new Point(ActualWidth / 2.0, 355.0), 9.5, accent, FontWeights.SemiBold);
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

        var mutant = Mutants[row];
        var selected = row == _selectedRow;
        var hovered = row == _hoverRow;
        var modified = Math.Abs(parameter.CurrentNumber - parameter.ReferenceNumber) > Epsilon;

        dc.DrawRoundedRectangle(
            selected ? CloneWithOpacity(accent, 0.08)
            : hovered ? CloneWithOpacity(border, 0.10)
            : CloneWithOpacity(panel, 0.02),
            new Pen(CloneWithOpacity(border, 0.45), 0.8),
            card,
            3.0,
            3.0);

        var currentHp = mutant.VanillaHp * parameter.CurrentNumber;
        var vanillaHp = mutant.VanillaHp * parameter.ReferenceNumber;

        DrawText(dc, mutant.Label, new Point(card.Left + 8.0, card.Top + 6.0), 9.5, modified ? accent : text, FontWeights.Bold);
        DrawRightAlignedText(dc, $"{currentHp:0} HP", new Point(card.Right - 8.0, card.Top + 5.0), 10.5, modified ? accent : text, FontWeights.Bold);

        if (mutant.ShowMist)
            DrawText(dc, $"Mist {1000.0 * parameter.CurrentNumber:0} HP", new Point(card.Left + 8.0, card.Top + 23.0), 9.0, muted, FontWeights.Normal);
        else
            DrawText(dc, $"vanilla {vanillaHp:0} HP", new Point(card.Left + 8.0, card.Top + 23.0), 9.0, muted, FontWeights.Normal);

        var hpBar = new Rect(card.Left + 126.0, card.Top + 25.0, Math.Max(80.0, card.Width - 134.0), 8.0);
        var hpY = hpBar.Top + hpBar.Height / 2.0;
        dc.DrawLine(new Pen(border, 4.0), new Point(hpBar.Left, hpY), new Point(hpBar.Right, hpY));

        var currentHpX = HpToX(currentHp, hpBar);
        var vanillaHpX = HpToX(vanillaHp, hpBar);
        dc.DrawLine(new Pen(accent, 4.0), new Point(hpBar.Left, hpY), new Point(currentHpX, hpY));
        dc.DrawLine(new Pen(muted, 1.0), new Point(vanillaHpX, hpY - 5.0), new Point(vanillaHpX, hpY + 5.0));

        var track = new Rect(card.Left + 8.0, card.Top + 41.0, card.Width - 16.0, 10.0);
        var y = track.Top + track.Height / 2.0;
        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

        var baseX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, track);
        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track);
        dc.DrawLine(new Pen(muted, 1.0), new Point(baseX, y - 5.0), new Point(baseX, y + 5.0));
        dc.DrawLine(new Pen(accent, 3.0), new Point(track.Left, y), new Point(currentX, y));
        dc.DrawEllipse(panel, new Pen(accent, selected ? 2.4 : 1.4), new Point(currentX, y), 5.3, 5.3);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.3, 2.3);

        DrawRightAlignedText(dc, $"{parameter.CurrentNumber:0.##}x", new Point(card.Right - 8.0, card.Top + 35.0), 9.0, modified ? accent : muted, FontWeights.SemiBold);

        _targets[row] = new HitTarget(
            new Rect(track.Left - 6.0, track.Top - 6.0, track.Width + 12.0, track.Height + 12.0),
            track);
    }

    private Rect ContentRect()
    {
        var width = Math.Min(MaxContentWidth, Math.Max(540.0, ActualWidth - 32.0));
        return new Rect((ActualWidth - width) / 2.0, 0.0, width, ActualHeight);
    }

    private static double HpToX(double hp, Rect bar)
        => bar.Left + bar.Width * Math.Clamp(hp / GlobalHpMaximum, 0.0, 1.0);

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
        _status.ShowTransient($"{Mutants[row].Label} health reset to Base Preset.");
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

            _status.ShowTransient("Mutant health reset to Base Preset.");
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

    private sealed record MutantDefinition(string ParameterId, string Label, double VanillaHp, bool ShowMist);
    private sealed record HitTarget(Rect HitRect, Rect MapRect);
}
