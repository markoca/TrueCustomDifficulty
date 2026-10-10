using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

public sealed class SpecialAbilityCooldownRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double DesiredHeight = 402.0;
    private const double MaxContentWidth = 1080.0;
    private const double ColumnGap = 12.0;

    private static readonly CooldownDefinition[] Mutants =
    {
        new("combat.mutantDamage", "Damage From Mutants", null),
        new("combat.mutantAttackCooldown", "Attack Frequency", null),
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
    private readonly VisualStatusMessage _status;

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
        _status = new VisualStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = DesiredHeight;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 900.0 : Math.Max(760.0, availableSize.Width);
        return new Size(width, DesiredHeight);
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
        IndustrialChrome.DrawFrame(dc,
            new Rect(2.0, 2.0, Math.Max(0.0, ActualWidth - 4.0),
                Math.Max(0.0, ActualHeight - 4.0)),
            panel, border, accent, soft, 8.0,
            accentEdge: true, technicalMarks: true);
        _targets.Clear();
        var content = ContentRect();
        DrawCenteredText(dc, "MUTANT COMBAT",
            new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);
        const double globalGap = 12.0;
        var globalWidth = (content.Width - globalGap) / 2.0;
        for (var i = 0; i < 2; i++)
            DrawGlobalHorizontal(dc, i,
                new Rect(content.Left + i * (globalWidth + globalGap),
                    53.0, globalWidth, 85.0),
                text, muted, border, accent, panel);
        DrawCenteredText(dc, "SPECIAL ABILITY RECHARGE",
            new Point(ActualWidth / 2.0, 172.0),
            11.4, accent, FontWeights.Bold);
        const int columns = 4;
        const double cardHeight = 79.0;
        const double rowPitch = 92.0;
        var cardWidth = (content.Width - (columns - 1) * ColumnGap) / columns;
        for (var index = 2; index < Mutants.Length; index++)
        {
            var slot = index - 2;
            DrawRechargeCard(dc, index,
                new Rect(content.Left + slot % columns * (cardWidth + ColumnGap),
                    198.0 + slot / columns * rowPitch, cardWidth, cardHeight),
                text, muted, border, accent, panel);
        }
    }

    private void DrawGlobalHorizontal(DrawingContext dc, int row, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        if (_parameters[row] is not { } parameter) return;
        var hovered = row == _hoverRow;
        var selected = row == _selectedRow;
        var changed = parameter.IsValueModifiedFromReference();
        dc.DrawRoundedRectangle(panel, new Pen(border, 0.9), card, 4.0, 4.0);
        DrawText(dc, row == 0 ? "DAMAGE FROM MUTANTS" : "ATTACK FREQUENCY",
            new Point(card.Left + 12.0, card.Top + 9.0),
            11.2, text, FontWeights.Bold);
        DrawRightAlignedText(dc, $"{parameter.CurrentNumber:0.##}x",
            new Point(card.Right - 12.0, card.Top + 7.0),
            changed ? 14.0 : 12.8, changed ? accent : text, FontWeights.Bold);
        var rail = new Rect(card.Left + 16.0, card.Top + 44.0,
            Math.Max(1.0, card.Width - 32.0), 12.0);
        var y = rail.Top + rail.Height / 2.0;
        var x = ValueToX(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, rail);
        var referenceX = ValueToX(parameter.ReferenceNumber,
            parameter.Minimum, parameter.Maximum, rail);
        dc.DrawLine(new Pen(border, 3.0),
            new Point(rail.Left, y), new Point(rail.Right, y));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.70), 3.0),
            new Point(rail.Left, y), new Point(x, y));
        dc.DrawLine(new Pen(muted, 1.6),
            new Point(referenceX, y - 8.0), new Point(referenceX, y + 8.0));
        ControlHoverGlow.Draw(dc, new Point(x, y), accent, hovered);
        dc.DrawEllipse(panel, new Pen(accent, hovered || selected ? 2.5 : 1.5),
            new Point(x, y), 6.0, 6.0);
        dc.DrawEllipse(accent, null, new Point(x, y), 2.5, 2.5);
        DrawText(dc, $"{parameter.Minimum:0.##}x",
            new Point(rail.Left, card.Bottom - 17.0),
            10.0, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, $"{parameter.Maximum:0.##}x",
            new Point(rail.Right, card.Bottom - 17.0),
            10.0, muted, FontWeights.Normal);
        _targets[row] = new HitTarget(
            new Rect(rail.Left - 8.0, rail.Top - 8.0,
                rail.Width + 16.0, rail.Height + 16.0), rail);
    }

    private void DrawRechargeCard(DrawingContext dc, int row, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        if (_parameters[row] is not { } parameter) return;
        var hovered = row == _hoverRow;
        var selected = row == _selectedRow;
        var changed = parameter.IsValueModifiedFromReference();
        dc.DrawRoundedRectangle(panel, new Pen(border, 0.9), card, 4.0, 4.0);
        var rate = RechargeRate(parameter.CurrentNumber);
        var valueText = CreateText($"{rate:0.##}x",
            changed ? 13.4 : 12.2, changed ? accent : text,
            FontWeights.Bold);
        var maxNameWidth = Math.Max(1.0,
            card.Width - valueText.WidthIncludingTrailingWhitespace - 35.0);
        var nameText = CreateText(Mutants[row].Label.ToUpperInvariant(),
            10.3, text, FontWeights.Bold);
        if (nameText.WidthIncludingTrailingWhitespace > maxNameWidth)
            nameText.MaxTextWidth = maxNameWidth;
        dc.DrawText(nameText, new Point(card.Left + 11.0, card.Top + 9.0));
        dc.DrawText(valueText,
            new Point(card.Right - 11.0 - valueText.WidthIncludingTrailingWhitespace,
                card.Top + 7.0));
        var rail = new Rect(card.Left + 13.0, card.Top + 39.0,
            Math.Max(1.0, card.Width - 26.0), 12.0);
        var y = rail.Top + rail.Height / 2.0;
        var currentX = RechargeToX(parameter.CurrentNumber, parameter, rail);
        var referenceX = RechargeToX(parameter.ReferenceNumber, parameter, rail);
        dc.DrawLine(new Pen(border, 3.0),
            new Point(rail.Left, y), new Point(rail.Right, y));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.70), 3.0),
            new Point(rail.Left, y), new Point(currentX, y));
        dc.DrawLine(new Pen(muted, 1.6),
            new Point(referenceX, y - 8.0), new Point(referenceX, y + 8.0));
        ControlHoverGlow.Draw(dc, new Point(currentX, y), accent, hovered);
        dc.DrawEllipse(panel, new Pen(accent, hovered || selected ? 2.5 : 1.5),
            new Point(currentX, y), 6.0, 6.0);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);
        // Native source min/max are inverted into rate-space UI endpoints.
        DrawText(dc, $"{RechargeRate(parameter.Maximum):0.##}x",
            new Point(rail.Left, card.Bottom - 16.0),
            9.7, muted, FontWeights.Normal);
        DrawRightAlignedText(dc, $"{RechargeRate(parameter.Minimum):0.##}x",
            new Point(rail.Right, card.Bottom - 16.0),
            9.7, muted, FontWeights.Normal);
        _targets[row] = new HitTarget(
            new Rect(rail.Left - 6.0, rail.Top - 6.0,
                rail.Width + 12.0, rail.Height + 12.0), rail);
    }

    private static double RechargeRate(double wait)
        => 1.0 / Math.Max(Epsilon, wait);

    // Native editing happens in wait-time units; only the GUI axis is inverted.
    private static double RechargeToX(double wait, ParameterViewModel parameter, Rect track)
    {
        var minRate = RechargeRate(parameter.Maximum);
        var maxRate = RechargeRate(parameter.Minimum);
        var ratio = Math.Clamp((RechargeRate(wait) - minRate) /
            Math.Max(Epsilon, maxRate - minRate), 0.0, 1.0);
        return track.Left + track.Width * ratio;
    }

    private static double XToSourceWait(double x, ParameterViewModel parameter, Rect track)
    {
        var ratio = Math.Clamp((x - track.Left) /
            Math.Max(Epsilon, track.Width), 0.0, 1.0);
        var minRate = RechargeRate(parameter.Maximum);
        var maxRate = RechargeRate(parameter.Minimum);
        return 1.0 / Math.Max(Epsilon, minRate + ratio * (maxRate - minRate));
    }

    private Rect ContentRect()
    {
        var width = Math.Min(MaxContentWidth, Math.Max(760.0, ActualWidth - 32.0));
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
        _status.ShowTransient($"{Mutants[row].Label} reset to Base Preset.");
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

            _status.ShowTransient("Special-ability pacing reset to Base Preset.");
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

        if (_selectedRow < 2)
        {
            // Global Combat keeps its previous keyboard editing contract.
            selected.CurrentNumber = Math.Clamp(
                selected.CurrentNumber + direction * selected.TickFrequency,
                selected.Minimum, selected.Maximum);
        }
        else
        {
            // 0.25x is a step in the PLAYER'S RECHARGE RATE, not in the
            // native reciprocal cooldown wait. Arrows move one visible tick.
            var rate = RechargeRate(selected.CurrentNumber)
                + direction * selected.TickFrequency;
            var boundedRate = Math.Clamp(rate,
                RechargeRate(selected.Maximum), RechargeRate(selected.Minimum));
            selected.CurrentNumber = 1.0 / boundedRate;
        }

        _status.Clear();
        InvalidateVisual();
        e.Handled = true;
    }

    private void UpdateFromPoint(int row, Point point)
    {
        if (!_targets.TryGetValue(row, out var target) || _parameters[row] is not { } parameter)
            return;

        var raw = row < 2
            ? parameter.Minimum +
                Math.Clamp((point.X - target.MapRect.Left) /
                    Math.Max(1.0, target.MapRect.Width), 0.0, 1.0) *
                (parameter.Maximum - parameter.Minimum)
            : XToSourceWait(point.X, parameter, target.MapRect);
        if (row < 2)
        {
            // Ordinary mutant damage/attack tracks still snap in source space.
            var tick = Math.Max(parameter.TickFrequency, Epsilon);
            var snapped = 1.0 + Math.Round((raw - 1.0) / tick,
                MidpointRounding.AwayFromZero) * tick;
            parameter.CurrentNumber = Math.Clamp(snapped, parameter.Minimum, parameter.Maximum);
        }
        else
        {
            // ParameterViewModel snaps the reciprocal RECHARGE display to the
            // 0.25x rate grid, so mouse input matches arrow keys and presets.
            parameter.CurrentNumber = raw;
        }
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

    private sealed record CooldownDefinition(string ParameterId, string Label, string? SecondaryNote);
    private sealed record HitTarget(Rect HitRect, Rect MapRect);
}
