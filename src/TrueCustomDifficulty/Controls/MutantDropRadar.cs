using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Absolute mutant-part drop-probability board.
///
/// The historical *Radar class name is kept for XAML compatibility. All fourteen
/// mutant families share the same real 0-100% probability scale. Every successful
/// eligible-kill roll yields one part; spawn rate, mutant health and part value remain
/// independent systems.
/// </summary>
public sealed class MutantDropRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double RowHeight = 50.0;

    private static readonly DropDefinition[] Mutants =
    {
        new("loot.mutant.blindDogChance", "BLIND DOG"),
        new("loot.mutant.bloodsuckerChance", "BLOODSUCKER"),
        new("loot.mutant.boarChance", "BOAR"),
        new("loot.mutant.burerChance", "BURER"),
        new("loot.mutant.catChance", "BAYUN"),
        new("loot.mutant.chimeraChance", "CHIMERA"),
        new("loot.mutant.controllerChance", "CONTROLLER"),
        new("loot.mutant.deerChance", "DEER"),
        new("loot.mutant.fleshChance", "FLESH"),
        new("loot.mutant.poltergeistChance", "POLTERGEIST"),
        new("loot.mutant.pseudodogChance", "PSEUDODOG"),
        new("loot.mutant.pseudogiantChance", "PSEUDOGIANT"),
        new("loot.mutant.snorkChance", "SNORK"),
        new("loot.mutant.tushkanChance", "TUSHKAN"),
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
        typeof(MutantDropRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public MutantDropRadar()
    {
        _status = new VisualStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = 470;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 840.0 : Math.Max(660.0, availableSize.Width);
        return new Size(width, 470.0);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MutantDropRadar)d).RebindParameters();

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
            or nameof(ParameterViewModel.IsModified)
            or nameof(ParameterViewModel.CurrentDisplay)
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

        DrawCenteredText(dc, "MUTANT PART DROP CHANCES", new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);


        DrawRightAlignedText(
            dc,
            "SPAWNS / HEALTH / PART VALUE / PROGRESSION STAY SEPARATE",
            new Point(Math.Max(560.0, ActualWidth - 18.0), 18.0),
            7.5,
            muted,
            FontWeights.SemiBold);

        var outerLeft = 18.0;
        var outerRight = Math.Max(outerLeft + 610.0, ActualWidth - 18.0);
        var gap = 18.0;
        var columnWidth = (outerRight - outerLeft - gap) / 2.0;

        DrawColumnHeader(dc, new Rect(outerLeft, 61.0, columnWidth, 24.0), muted, border);
        DrawColumnHeader(dc, new Rect(outerLeft + columnWidth + gap, 61.0, columnWidth, 24.0), muted, border);

        for (var i = 0; i < 7; i++)
        {
            DrawRow(
                dc,
                i,
                new Rect(outerLeft, 86.0 + i * RowHeight, columnWidth, RowHeight - 4.0),
                text,
                muted,
                border,
                accent,
                panel);

            DrawRow(
                dc,
                i + 7,
                new Rect(outerLeft + columnWidth + gap, 86.0 + i * RowHeight, columnWidth, RowHeight - 4.0),
                text,
                muted,
                border,
                accent,
                panel);
        }

        DrawCenteredText(
            dc,
            "≈ N / 100 KILLS IS A LARGE-SAMPLE EXPECTATION, NOT A GUARANTEED SEQUENCE",
            new Point(ActualWidth / 2.0, 438.0),
            7.5,
            muted,
            FontWeights.Normal);

        // No transient yellow reset notifications.
    }

    private void DrawColumnHeader(DrawingContext dc, Rect bounds, Brush muted, Brush border)
    {
        DrawText(dc, "MUTANT", new Point(bounds.Left + 6.0, bounds.Top + 3.0), 7.6, muted, FontWeights.Bold);
        DrawRightAlignedText(dc, "DROP CHANCE  •  EXPECTED / 100", new Point(bounds.Right - 6.0, bounds.Top + 3.0), 7.4, muted, FontWeights.Bold);
        dc.DrawLine(new Pen(border, 1.0), new Point(bounds.Left, bounds.Bottom), new Point(bounds.Right, bounds.Bottom));
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
        var parameter = _parameters[row];
        if (parameter is null)
            return;

        var info = Mutants[row];
        var selected = row == _selectedRow;
        var hovered = row == _hoverRow;
        var chance = Math.Clamp(parameter.CurrentNumber, 0.0, 1.0);
        var vanilla = Math.Clamp(parameter.ReferenceNumber, 0.0, 1.0);
        var modified = Math.Abs(chance - vanilla) > Epsilon;

        dc.DrawRoundedRectangle(
            selected
                ? CloneWithOpacity(accent, 0.08)
                : hovered
                    ? CloneWithOpacity(border, 0.10)
                    : CloneWithOpacity(panel, 0.02),
            new Pen(CloneWithOpacity(border, 0.45), 0.8),
            card,
            3.0,
            3.0);

        DrawText(
            dc,
            info.Label,
            new Point(card.Left + 7.0, card.Top + 7.0),
            7.9,
            text,
            FontWeights.Bold);

        DrawRightAlignedText(
            dc,
            $"{chance * 100:0}%",
            new Point(card.Right - 9.0, card.Top + 6.0),
            9.2,
            modified ? accent : text,
            FontWeights.Bold);

        var track = new Rect(card.Left + 10.0, card.Top + 27.0, card.Width - 20.0, 12.0);
        var y = track.Top + track.Height / 2.0;

        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

        var vanillaX = track.Left + track.Width * vanilla;
        var currentX = track.Left + track.Width * chance;

        dc.DrawLine(new Pen(muted, 1.1), new Point(vanillaX, y - 7.0), new Point(vanillaX, y + 7.0));
        dc.DrawLine(new Pen(accent, 3.0), new Point(track.Left, y), new Point(currentX, y));

        dc.DrawEllipse(
            panel,
            new Pen(accent, selected ? 2.4 : 1.4),
            new Point(currentX, y),
            5.5,
            5.5);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);


        _targets[row] = new HitTarget(
            new Rect(track.Left - 6.0, track.Top - 6.0, track.Width + 12.0, track.Height + 12.0),
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
        _status.ShowTransient($"{Mutants[row].Label} drop chance reset to Base Preset.");
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

            _status.ShowTransient("Mutant part drop chances reset to Base Preset.");
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

    private sealed record DropDefinition(string ParameterId, string Label);
    private sealed record HitTarget(Rect HitRect, Rect MapRect);
}
