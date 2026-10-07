using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Stamina pressure board for movement, melee and aiming.
///
/// The historical *Radar class name is kept for XAML compatibility. Action costs are
/// compared on a shared percent-of-vanilla scale while their actual raw stamina values
/// remain visible. Aiming is rendered separately because it is a continuous recovery
/// penalty rather than a one-time action cost.
/// </summary>
public sealed class ActionCostsRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double RowHeight = 46.0;

    private static readonly ActionDefinition[] Actions =
    {
        new("stamina.sprintCost", "SPRINT", "movement", false),
        new("stamina.jumpCost", "JUMP", "movement", true),
        new("stamina.vaultCost", "VAULT", "movement", true),
        new("stamina.meleeNormalCost", "NORMAL MELEE", "melee", true),
        new("stamina.meleeStrongCost", "STRONG MELEE", "melee", true),
        new("stamina.butstockCost", "BUTTSTOCK", "melee", true),
    };

    private const string AimParameterId = "stamina.aimRegenCoef";

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Actions.Length];
    private ParameterViewModel? _aimParameter;
    private readonly Dictionary<int, HitTarget> _targets = new();
    private HitTarget? _aimTarget;
    private readonly VisualStatusMessage _status;

    private int _selectedRow;
    private int _hoverRow = -1;
    private int _dragRow = -1;
    private bool _aimSelected;
    private bool _aimHovered;
    private bool _dragAim;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(ActionCostsRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public ActionCostsRadar()
    {
        _status = new VisualStatusMessage(InvalidateVisual);
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
        => ((ActionCostsRadar)d).RebindParameters();

    private void RebindParameters()
    {
        foreach (var parameter in _parameters)
        {
            if (parameter is not null)
                parameter.PropertyChanged -= Parameter_PropertyChanged;
        }

        if (_aimParameter is not null)
            _aimParameter.PropertyChanged -= Parameter_PropertyChanged;

        Array.Clear(_parameters, 0, _parameters.Length);
        _aimParameter = null;

        if (Parameters is not null)
        {
            var byId = Parameters
                .OfType<ParameterViewModel>()
                .ToDictionary(x => x.Id, StringComparer.Ordinal);

            for (var i = 0; i < Actions.Length; i++)
            {
                if (!byId.TryGetValue(Actions[i].ParameterId, out var parameter))
                    continue;

                _parameters[i] = parameter;
                parameter.PropertyChanged += Parameter_PropertyChanged;
            }

            if (byId.TryGetValue(AimParameterId, out var aim))
            {
                _aimParameter = aim;
                aim.PropertyChanged += Parameter_PropertyChanged;
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
        _aimTarget = null;

        DrawText(dc, "STAMINA PRESSURE", new Point(18.0, 16.0), 12.0, text, FontWeights.Bold);
        DrawText(
            dc,
            "movement and melee costs share one relative load scale; aiming recovery stays separate",
            new Point(18.0, 34.0),
            8.2,
            muted,
            FontWeights.Normal);

        var layout = GetLayout();

        DrawText(dc, "ACTION", new Point(layout.LabelLeft, 61.0), 8.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "RAW", new Point(layout.RawCenter, 68.0), 8.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "% VANILLA", new Point(layout.PercentCenter, 68.0), 8.0, muted, FontWeights.Bold);
        DrawCenteredText(dc, "0%  ←  STAMINA LOAD  →  200%", new Point(layout.TrackLeft + layout.TrackWidth / 2.0, 68.0), 7.8, muted, FontWeights.Bold);

        dc.DrawLine(new Pen(border, 1.0), new Point(14.0, 81.0), new Point(Math.Max(14.0, ActualWidth - 14.0), 81.0));

        DrawSectionLabel(dc, "MOVEMENT", 92.0, muted, border);
        for (var i = 0; i < 3; i++)
            DrawActionRow(dc, i, 108.0 + i * RowHeight, layout, text, muted, border, accent, panel);

        DrawSectionLabel(dc, "MELEE", 250.0, muted, border);
        for (var i = 3; i < Actions.Length; i++)
            DrawActionRow(dc, i, 266.0 + (i - 3) * RowHeight, layout, text, muted, border, accent, panel);

        DrawAimPanel(dc, 410.0, text, muted, border, accent, panel);

        if (!string.IsNullOrWhiteSpace(_status.Text))
        {
            DrawCenteredText(
                dc,
                _status.Text,
                new Point(ActualWidth / 2.0, 488.0),
                8.0,
                accent,
                FontWeights.SemiBold);
        }
    }

    private void DrawSectionLabel(DrawingContext dc, string label, double y, Brush muted, Brush border)
    {
        DrawText(dc, label, new Point(18.0, y), 7.8, muted, FontWeights.Bold);
        dc.DrawLine(new Pen(CloneWithOpacity(border, 0.55), 0.8), new Point(86.0, y + 7.0), new Point(Math.Max(90.0, ActualWidth - 18.0), y + 7.0));
    }

    private void DrawActionRow(
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

        var info = Actions[row];
        var selected = !_aimSelected && row == _selectedRow;
        var hovered = !_aimHovered && row == _hoverRow;
        var reference = parameter.ReferenceNumber;
        var ratio = Math.Abs(reference) > Epsilon ? parameter.CurrentNumber / reference : 0.0;
        var modified = Math.Abs(parameter.CurrentNumber - reference) > Epsilon;

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
            info.Label,
            new Point(layout.LabelLeft, top + 12.0),
            8.5,
            modified ? accent : text,
            FontWeights.Bold);

        DrawCenteredText(
            dc,
            FormatRaw(parameter.CurrentNumber),
            new Point(layout.RawCenter, top + 17.0),
            9.4,
            modified ? accent : text,
            FontWeights.Bold);

        DrawCenteredText(
            dc,
            $"{ratio * 100:0}%",
            new Point(layout.PercentCenter, top + 17.0),
            9.2,
            modified ? accent : muted,
            FontWeights.Bold);

        var track = new Rect(layout.TrackLeft, top + 9.0, layout.TrackWidth, 22.0);
        var y = track.Top + track.Height / 2.0;

        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

        var currentX = track.Left + track.Width * Math.Clamp(ratio / 2.0, 0.0, 1.0);
        var vanillaX = track.Left + track.Width * 0.5;

        dc.DrawLine(new Pen(muted, 1.1), new Point(vanillaX, y - 8.0), new Point(vanillaX, y + 8.0));
        dc.DrawLine(new Pen(accent, 3.2), new Point(track.Left, y), new Point(currentX, y));

        dc.DrawEllipse(
            panel,
            new Pen(accent, selected ? 2.5 : 1.5),
            new Point(currentX, y),
            6.0,
            6.0);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.7, 2.7);

        DrawText(dc, "0%", new Point(track.Left, top + 31.0), 6.6, muted, FontWeights.Normal);
        DrawCenteredText(dc, "VANILLA", new Point(vanillaX, top + 35.0), 6.5, muted, FontWeights.SemiBold);
        DrawRightAlignedText(dc, "200%", new Point(track.Right, top + 31.0), 6.6, muted, FontWeights.Normal);

        _targets[row] = new HitTarget(
            new Rect(track.Left - 7.0, track.Top - 6.0, track.Width + 14.0, track.Height + 12.0),
            track);
    }

    private void DrawAimPanel(
        DrawingContext dc,
        double top,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var parameter = _aimParameter;
        if (parameter is null)
            return;

        var card = new Rect(18.0, top, Math.Max(1.0, ActualWidth - 36.0), 66.0);
        dc.DrawRoundedRectangle(
            _aimSelected
                ? CloneWithOpacity(accent, 0.08)
                : _aimHovered
                    ? CloneWithOpacity(border, 0.10)
                    : CloneWithOpacity(panel, 0.04),
            new Pen(border, 0.9),
            card,
            3.0,
            3.0);

        DrawText(dc, "AIMING STAMINA RECOVERY PENALTY", new Point(card.Left + 10.0, card.Top + 8.0), 8.6, text, FontWeights.Bold);
        DrawText(dc, "continuous recovery slowdown — not a one-time stamina charge", new Point(card.Left + 10.0, card.Top + 24.0), 7.3, muted, FontWeights.Normal);

        DrawRightAlignedText(
            dc,
            $"{parameter.CurrentNumber * 100:0}%",
            new Point(card.Right - 12.0, card.Top + 7.0),
            10.0,
            accent,
            FontWeights.Bold);

        var track = new Rect(card.Left + 235.0, card.Top + 38.0, Math.Max(120.0, card.Width - 255.0), 18.0);
        var y = track.Top + track.Height / 2.0;

        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, track);
        var baseX = ValueToX(parameter.ReferenceNumber, parameter.Minimum, parameter.Maximum, track);

        dc.DrawLine(new Pen(muted, 1.1), new Point(baseX, y - 7.0), new Point(baseX, y + 7.0));
        dc.DrawLine(new Pen(accent, 3.0), new Point(track.Left, y), new Point(currentX, y));

        dc.DrawEllipse(
            panel,
            new Pen(accent, _aimSelected ? 2.5 : 1.5),
            new Point(currentX, y),
            6.0,
            6.0);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.7, 2.7);

        DrawText(dc, "0% — NO AIM PENALTY", new Point(track.Left, card.Top + 54.0), 6.6, muted, FontWeights.Normal);
        DrawCenteredText(dc, "VANILLA 25%", new Point(baseX, card.Top + 59.0), 6.5, muted, FontWeights.SemiBold);
        DrawRightAlignedText(dc, "50%", new Point(track.Right, card.Top + 54.0), 6.6, muted, FontWeights.Normal);

        _aimTarget = new HitTarget(
            new Rect(track.Left - 7.0, track.Top - 6.0, track.Width + 14.0, track.Height + 12.0),
            track);
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var point = e.GetPosition(this);

        if (_aimTarget is not null && _aimTarget.HitRect.Contains(point))
        {
            _aimSelected = true;
            _dragAim = true;
            _dragRow = -1;
            CaptureMouse();
            UpdateAimFromPoint(point);
            e.Handled = true;
            return;
        }

        var row = FindTarget(point);
        if (row < 0)
            return;

        _aimSelected = false;
        _selectedRow = row;
        _dragRow = row;
        _dragAim = false;
        CaptureMouse();
        UpdateFromPoint(row, point);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var point = e.GetPosition(this);
        var aimHover = _aimTarget is not null && _aimTarget.HitRect.Contains(point);
        var hover = aimHover ? -1 : FindTarget(point);

        if (aimHover != _aimHovered || hover != _hoverRow)
        {
            _aimHovered = aimHover;
            _hoverRow = hover;
            InvalidateVisual();
        }

        Cursor = aimHover || hover >= 0 ? Cursors.Hand : Cursors.Arrow;

        if (e.LeftButton != MouseButtonState.Pressed)
            return;

        if (_dragAim)
        {
            UpdateAimFromPoint(point);
            e.Handled = true;
            return;
        }

        if (_dragRow >= 0)
        {
            UpdateFromPoint(_dragRow, point);
            e.Handled = true;
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);

        if (!_aimHovered && _hoverRow < 0)
            return;

        _aimHovered = false;
        _hoverRow = -1;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_dragRow < 0 && !_dragAim)
            return;

        _dragRow = -1;
        _dragAim = false;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);

        var point = e.GetPosition(this);
        if (_aimTarget is not null && _aimTarget.HitRect.Contains(point) && _aimParameter is not null)
        {
            _aimSelected = true;
            _aimParameter.ResetToReference();
            _status.ShowTransient("Aiming recovery penalty reset to Base Preset.");
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var row = FindTarget(point);
        if (row < 0 || _parameters[row] is not { } parameter)
            return;

        _aimSelected = false;
        _selectedRow = row;
        parameter.ResetToReference();
        _status.ShowTransient($"{Actions[row].Label} stamina cost reset to Base Preset.");
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

            _aimParameter?.ResetToReference();
            _status.ShowTransient("Action-cost board reset to Base Preset.");
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

        if (direction == 0)
            return;

        var selected = _aimSelected ? _aimParameter : _parameters[_selectedRow];
        if (selected is null)
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

        var vanillaRatio = ratio * 2.0;
        var raw = parameter.ReferenceNumber * vanillaRatio;
        SetSnapped(parameter, raw);
        _status.Clear();
        InvalidateVisual();
    }

    private void UpdateAimFromPoint(Point point)
    {
        if (_aimTarget is null || _aimParameter is null)
            return;

        var ratio = Math.Clamp(
            (point.X - _aimTarget.MapRect.Left) / Math.Max(1.0, _aimTarget.MapRect.Width),
            0.0,
            1.0);

        var raw = _aimParameter.Minimum + ratio * (_aimParameter.Maximum - _aimParameter.Minimum);
        SetSnapped(_aimParameter, raw);
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
        var labelWidth = Math.Clamp(usable * 0.18, 105.0, 135.0);
        var rawWidth = 70.0;
        var percentWidth = 76.0;
        var fixedWidth = labelWidth + rawWidth + percentWidth + 42.0;
        var trackWidth = Math.Max(170.0, usable - fixedWidth);

        var left = 22.0;
        var rawCenter = left + labelWidth + rawWidth / 2.0;
        var percentCenter = left + labelWidth + rawWidth + percentWidth / 2.0;
        var trackLeft = left + labelWidth + rawWidth + percentWidth + 20.0;

        return new Layout(left, rawCenter, percentCenter, trackLeft, trackWidth);
    }

    private static string FormatRaw(double value)
        => Math.Abs(value - Math.Round(value)) < Epsilon
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.##", CultureInfo.InvariantCulture);

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

    private sealed record ActionDefinition(string ParameterId, string Label, string Group, bool OneTime);
    private sealed record HitTarget(Rect HitRect, Rect MapRect);
    private sealed record Layout(
        double LabelLeft,
        double RawCenter,
        double PercentCenter,
        double TrackLeft,
        double TrackWidth);
}
