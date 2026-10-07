using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Base-healing output board for the four primary medical items.
///
/// The historical *Radar class name is kept for XAML compatibility. Every row keeps
/// the original raw HP backing value, shows that value on one shared absolute HP scale,
/// and exposes an interactive 0-200% of vanilla strength track.
/// </summary>
public sealed class MedicalHealingRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double AbsoluteHpMaximum = 200.0;
    private const double RowHeight = 50.0;

    private static readonly HealDefinition[] Items =
    {
        new("health.bandageHealing", "BANDAGE", true),
        new("health.medkitHealing", "MEDKIT", true),
        new("health.armyMedkitHealing", "ARMY MEDKIT", true),
        new("health.ecoMedkitHealing", "SCIENTIFIC", false),
    };

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Items.Length];
    private readonly Dictionary<int, HitTarget> _targets = new();
    private readonly VisualStatusMessage _status;

    private int _selectedRow;
    private int _hoverRow = -1;
    private int _dragRow = -1;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(MedicalHealingRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public MedicalHealingRadar()
    {
        _status = new VisualStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = 320;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 700.0 : Math.Max(560.0, availableSize.Width);
        return new Size(width, 320.0);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((MedicalHealingRadar)d).RebindParameters();

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

            for (var i = 0; i < Items.Length; i++)
            {
                if (!byId.TryGetValue(Items[i].ParameterId, out var parameter))
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

        DrawText(dc, "MEDICAL HEALING OUTPUT", new Point(18.0, 16.0), 12.0, text, FontWeights.Bold);
        DrawText(
            dc,
            "base HP restored by each item, before any separate Master-only healing modifier",
            new Point(18.0, 34.0),
            8.2,
            muted,
            FontWeights.Normal);

        var layout = GetLayout();

        DrawText(dc, "ITEM", new Point(layout.LabelLeft, 62.0), 8.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "BASE HEAL", new Point(layout.ValueCenter, 69.0), 8.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "ABSOLUTE HP  •  0 → 200", new Point(layout.HpBarLeft + layout.HpBarWidth / 2.0, 69.0), 8.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "0%  ←  RELATIVE STRENGTH  →  200%", new Point(layout.TrackLeft + layout.TrackWidth / 2.0, 69.0), 7.7, muted, FontWeights.Bold);

        dc.DrawLine(new Pen(border, 1.0), new Point(14.0, 83.0), new Point(Math.Max(14.0, ActualWidth - 14.0), 83.0));

        for (var row = 0; row < Items.Length; row++)
            DrawRow(dc, row, 80.0 + row * RowHeight, layout, text, muted, border, accent, panel);

        DrawCenteredText(
            dc,
            "BANDAGE / MEDKIT / ARMY CAN BE MODIFIED AGAIN BY MASTER CONSUMABLE PENALTIES   •   SCIENTIFIC IS NOT",
            new Point(ActualWidth / 2.0, 286.0),
            7.4,
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

        var item = Items[row];
        var reference = parameter.ReferenceNumber;
        var relative = Math.Abs(reference) > Epsilon ? parameter.CurrentNumber / reference : 0.0;
        var modified = Math.Abs(parameter.CurrentNumber - reference) > Epsilon;
        var selected = row == _selectedRow;
        var hovered = row == _hoverRow;

        var rowRect = new Rect(14.0, top, Math.Max(1.0, ActualWidth - 28.0), RowHeight - 4.0);
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
            item.Label,
            new Point(layout.LabelLeft, top + 9.0),
            8.7,
            modified ? accent : text,
            FontWeights.Bold);

        DrawText(
            dc,
            item.HasMasterModifier ? "MASTER MODIFIER" : "DIRECT BASE HEAL",
            new Point(layout.LabelLeft, top + 28.0),
            6.5,
            item.HasMasterModifier ? muted : accent,
            FontWeights.SemiBold);

        DrawCenteredText(
            dc,
            $"{parameter.CurrentNumber:0} HP",
            new Point(layout.ValueCenter, top + 17.0),
            10.2,
            modified ? accent : text,
            FontWeights.Bold);

        DrawCenteredText(
            dc,
            $"{relative * 100:0}% vanilla",
            new Point(layout.ValueCenter, top + 36.0),
            6.9,
            muted,
            FontWeights.Normal);

        var hpBar = new Rect(layout.HpBarLeft, top + 11.0, layout.HpBarWidth, 20.0);
        var hpY = hpBar.Top + hpBar.Height / 2.0;

        dc.DrawLine(new Pen(border, 5.0), new Point(hpBar.Left, hpY), new Point(hpBar.Right, hpY));

        var currentHpX = HpToX(parameter.CurrentNumber, hpBar);
        var vanillaHpX = HpToX(reference, hpBar);

        dc.DrawLine(new Pen(accent, 5.0), new Point(hpBar.Left, hpY), new Point(currentHpX, hpY));
        dc.DrawLine(new Pen(muted, 1.1), new Point(vanillaHpX, hpY - 8.0), new Point(vanillaHpX, hpY + 8.0));
        dc.DrawEllipse(accent, null, new Point(currentHpX, hpY), 4.3, 4.3);

        DrawCenteredText(
            dc,
            $"v{reference:0}",
            new Point(vanillaHpX, top + 43.0),
            6.6,
            muted,
            FontWeights.SemiBold);

        var track = new Rect(layout.TrackLeft, top + 11.0, layout.TrackWidth, 20.0);
        var trackY = track.Top + track.Height / 2.0;

        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, trackY), new Point(track.Right, trackY));

        var currentX = track.Left + track.Width * Math.Clamp(relative / 2.0, 0.0, 1.0);
        var vanillaX = track.Left + track.Width * 0.5;

        dc.DrawLine(new Pen(muted, 1.1), new Point(vanillaX, trackY - 8.0), new Point(vanillaX, trackY + 8.0));
        dc.DrawLine(new Pen(accent, 3.1), new Point(track.Left, trackY), new Point(currentX, trackY));

        dc.DrawEllipse(
            panel,
            new Pen(accent, selected ? 2.5 : 1.5),
            new Point(currentX, trackY),
            6.0,
            6.0);
        dc.DrawEllipse(accent, null, new Point(currentX, trackY), 2.7, 2.7);

        DrawText(dc, "0%", new Point(track.Left, top + 34.0), 6.6, muted, FontWeights.Normal);
        DrawCenteredText(dc, "VANILLA", new Point(vanillaX, top + 39.0), 6.5, muted, FontWeights.SemiBold);
        DrawRightAlignedText(dc, "200%", new Point(track.Right, top + 34.0), 6.6, muted, FontWeights.Normal);

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
        _status.ShowTransient($"{Items[row].Label} base heal reset to Base Preset.");
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

            _status.ShowTransient("Medical healing output reset to Base Preset.");
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

        var raw = parameter.ReferenceNumber * ratio * 2.0;
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
        var usable = Math.Max(520.0, ActualWidth - 40.0);
        var labelWidth = Math.Clamp(usable * 0.20, 125.0, 155.0);
        var valueWidth = 88.0;
        var hpBarWidth = Math.Clamp(usable * 0.22, 110.0, 170.0);
        var fixedWidth = labelWidth + valueWidth + hpBarWidth + 48.0;
        var trackWidth = Math.Max(150.0, usable - fixedWidth);

        var left = 22.0;
        var valueCenter = left + labelWidth + valueWidth / 2.0;
        var hpBarLeft = left + labelWidth + valueWidth + 8.0;
        var trackLeft = hpBarLeft + hpBarWidth + 24.0;

        return new Layout(left, valueCenter, hpBarLeft, hpBarWidth, trackLeft, trackWidth);
    }

    private static double HpToX(double hp, Rect bar)
        => bar.Left + bar.Width * Math.Clamp(hp / AbsoluteHpMaximum, 0.0, 1.0);

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

    private sealed record HealDefinition(string ParameterId, string Label, bool HasMasterModifier);
    private sealed record HitTarget(Rect HitRect, Rect MapRect);
    private sealed record Layout(
        double LabelLeft,
        double ValueCenter,
        double HpBarLeft,
        double HpBarWidth,
        double TrackLeft,
        double TrackWidth);
}
