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
    private const double DesiredHeight = 605.0;
    private const int DamageRow = 14;
    private const double MaxContentWidth = 1080.0;
    private const double ColumnGap = 12.0;

    private static readonly MutantDefinition[] Mutants =
    {
        new("combat.mutantHealth.bayun", "Bayun", 400.0, false),
        new("combat.mutantHealth.blinddog", "Blind Dog", 63.0, false),
        new("combat.mutantHealth.bloodsucker", "Bloodsucker", 500.0, true),
        new("combat.mutantHealth.boar", "Boar", 300.0, false),
        new("combat.mutantHealth.burer", "Burer", 400.0, false),
        new("combat.mutantHealth.chimera", "Chimera", 1400.0, false),
        new("combat.mutantHealth.controller", "Controller", 500.0, false),
        new("combat.mutantHealth.deer", "Deer", 600.0, false),
        new("combat.mutantHealth.flesh", "Flesh", 280.0, false),
        new("combat.mutantHealth.poltergeist", "Poltergeist", 400.0, false),
        new("combat.mutantHealth.pseudodog", "Pseudodog", 300.0, false),
        new("combat.mutantHealth.pseudogiant", "Pseudogiant", 2500.0, false),
        new("combat.mutantHealth.snork", "Snork", 150.0, false),
        new("combat.mutantHealth.tushkan", "Tushkan", 19.0, false),
    };

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Mutants.Length];
    private readonly Dictionary<int, HitTarget> _targets = new();
    // Marker labels are separate clickable hit regions, not part of the fader.
    private readonly List<DamageMarkerHitTarget> _damageMarkerTargets = new();
    private ParameterViewModel? _damage;
    private int _hoverDamageMarker = -1;
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
        var width = double.IsInfinity(availableSize.Width) ? 900.0 : Math.Max(760.0, availableSize.Width);
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

        if (_damage is not null)
            _damage.PropertyChanged -= Parameter_PropertyChanged;
        _damage = null;
        _hoverDamageMarker = -1;
        _damageMarkerTargets.Clear();
        Array.Clear(_parameters, 0, _parameters.Length);

        if (Parameters is not null)
        {
            var byId = Parameters.OfType<ParameterViewModel>().ToDictionary(x => x.Id, StringComparer.Ordinal);
            if (byId.TryGetValue("combat.mutantDamage", out _damage))
                _damage.PropertyChanged += Parameter_PropertyChanged;
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
            panel, border, accent, soft, 8.0,
            accentEdge: true, technicalMarks: true);

        _targets.Clear();
        _damageMarkerTargets.Clear();
        DrawCenteredText(dc, "MUTANT DAMAGE AND HEALTH", new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);

        // Stamina Usage-style grouped, independent horizontal controls.
        // Seven rows, two columns, alphabetically ordered down each column.
        var content = ContentRect();
        DrawDamageTrack(dc, content, text, muted, border, accent, panel);
        const double cardHeight = 58.0;
        const double rowPitch = 65.0;
        var cardWidth = (content.Width - ColumnGap) / 2.0;
        for (var i = 0; i < Mutants.Length; i++)
        {
            var column = i / 7;
            var row = i % 7;
            DrawRow(dc, i, new Rect(
                content.Left + column * (cardWidth + ColumnGap),
                125.0 + row * rowPitch, cardWidth, cardHeight),
                text, muted, border, accent, panel);
        }
    }

    private void DrawDamageTrack(DrawingContext dc, Rect content, Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        if (_damage is not { } damage)
            return;
        var card = new Rect(content.Left, 43.0, content.Width, 71.0);
        dc.DrawRoundedRectangle(panel, new Pen(border, 0.9), card, 4.0, 4.0);
        DrawText(dc, "MUTANT HIT DAMAGE", new Point(card.Left + 11.0, card.Top + 7.0), 10.8, text, FontWeights.Bold);
        var value = CreateText($"{damage.CurrentNumber:0.##}x hit dmg", 11.0,
            damage.IsValueModifiedFromReference() ? accent : text, FontWeights.Bold);
        dc.DrawText(value, new Point(card.Right - 11.0 - value.Width, card.Top + 7.0));
        var rail = new Rect(card.Left + 14.0, card.Top + 34.0, card.Width - 28.0, 9.0);
        var y = rail.Top + rail.Height / 2.0;
        var currentX = ValueToX(damage.CurrentNumber, damage.Minimum, damage.Maximum, rail);
        dc.DrawLine(new Pen(border, 3), new Point(rail.Left, y), new Point(rail.Right, y));
        dc.DrawLine(new Pen(accent, 3), new Point(rail.Left, y), new Point(currentX, y));
        for (var i = 0; i < damage.VanillaMarkers.Count; i++)
        {
            var marker = damage.VanillaMarkers[i];
            var markerX = ValueToX(marker.Value, damage.Minimum, damage.Maximum, rail);
            var hovered = i == _hoverDamageMarker;
            var markerBrush = hovered ? Brushes.White : muted;
            dc.DrawEllipse(markerBrush, null, new Point(markerX, y + 10.0), 2, 2);
            var label = CreateText(marker.Label.ToUpperInvariant(), 9.5,
                markerBrush, FontWeights.SemiBold);
            var labelX = Math.Clamp(markerX - label.Width / 2.0,
                card.Left + 8.0, Math.Max(card.Left + 8.0, card.Right - 8.0 - label.Width));
            var labelY = y + 12.0;
            dc.DrawText(label, new Point(labelX, labelY));

            // Include both dot and complete caption; clicking either snaps to
            // the exact authored vanilla value (even e.g. 0.35x off the 0.1x grid).
            var hit = new Rect(markerX - 8.0, y + 5.0, 16.0, 14.0);
            hit.Union(new Rect(labelX - 5.0, labelY - 2.0,
                label.Width + 10.0, label.Height + 4.0));
            _damageMarkerTargets.Add(new DamageMarkerHitTarget(marker.Value, hit));
        }
        ControlHoverGlow.Draw(dc, new Point(currentX, y), accent, _hoverRow == DamageRow);
        dc.DrawEllipse(panel, new Pen(accent, _selectedRow == DamageRow ? 2.5 : 1.5),
            new Point(currentX, y), 6, 6);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);
        _targets[DamageRow] = new HitTarget(new Rect(rail.Left - 6.0, rail.Top - 8.0,
            rail.Width + 12.0, 23.0), rail);
    }

    private ParameterViewModel? ParameterAt(int row)
        => row == DamageRow ? _damage : row >= 0 && row < _parameters.Length ? _parameters[row] : null;

    private void DrawRow(
        DrawingContext dc, int row, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        if (_parameters[row] is not { } parameter)
            return;

        var mutant = Mutants[row];
        var modified = parameter.IsValueModifiedFromReference();
        var hovered = row == _hoverRow;
        var selected = row == _selectedRow;

        // Static captions and card fill never turn gold on slider hover.
        dc.DrawRoundedRectangle(panel,
            new Pen(border, 0.9), card, 4.0, 4.0);
        DrawText(dc, mutant.Label.ToUpperInvariant(),
            new Point(card.Left + 11.0, card.Top + 6.0),
            10.8, text, FontWeights.Bold);

        // Top row: species left, current multiplier centered, derived real HP right.
        // Modified values alone gain the accent and +1.2 DIP, never the HP unit.
        DrawSourceNumber(dc, $"{parameter.CurrentNumber:0.##}x",
            new Point(card.Left + card.Width / 2.0, card.Top + 12.0),
            11.2, text, accent, FontWeights.Bold, modified);

        var currentHp = mutant.VanillaHp * parameter.CurrentNumber;
        var hpLabel = $"{currentHp:0} HP";
        var hpText = CreateText(hpLabel, 10.5, muted, FontWeights.SemiBold);
        if (modified)
        {
            var hpDigits = hpLabel.IndexOf(' ');
            if (hpDigits > 0)
            {
                hpText.SetForegroundBrush(accent, 0, hpDigits);
                hpText.SetFontSize(11.7, 0, hpDigits);
            }
        }
        dc.DrawText(hpText,
            new Point(card.Right - 11.0 - hpText.WidthIncludingTrailingWhitespace,
                card.Top + 6.0));

        var track = new Rect(card.Left + 12.0, card.Top + 42.0,
            Math.Max(1.0, card.Width - 24.0), 10.0);
        var y = track.Top + track.Height / 2.0;
        var currentX = ValueToX(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, track);
        var baseX = ValueToX(parameter.ReferenceNumber,
            parameter.Minimum, parameter.Maximum, track);

        dc.DrawLine(new Pen(border, 3.0),
            new Point(track.Left, y), new Point(track.Right, y));
        dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.7), 3.0),
            new Point(track.Left, y), new Point(currentX, y));
        dc.DrawLine(new Pen(muted, 1.8),
            new Point(baseX, y - 8.0), new Point(baseX, y + 8.0));

        ControlHoverGlow.Draw(dc, new Point(currentX, y), accent, hovered);
        dc.DrawEllipse(panel,
            new Pen(accent, hovered || selected ? 2.5 : 1.5),
            new Point(currentX, y), 6.0, 6.0);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);

        _targets[row] = new HitTarget(
            new Rect(track.Left - 6.0, track.Top - 7.0,
                track.Width + 12.0, track.Height + 14.0),
            track);
    }

    private Rect ContentRect()
    {
        var width = Math.Min(MaxContentWidth, Math.Max(760.0, ActualWidth - 32.0));
        return new Rect((ActualWidth - width) / 2.0, 0.0, width, ActualHeight);
    }

    private static double HpToX(double hp, Rect bar)
        => bar.Left + bar.Width * Math.Clamp(hp / GlobalHpMaximum, 0.0, 1.0);

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        // Hit-test markers before the rail: their dots partially overlap it.
        // A marker click must never start a drag or go through normal tick snapping.
        var point = e.GetPosition(this);
        var markerIndex = FindDamageMarker(point);
        if (markerIndex >= 0 && _damage is { } damage)
        {
            _selectedRow = DamageRow;
            damage.SnapCurrentNumberToMarker(_damageMarkerTargets[markerIndex].Value);
            _status.Clear();
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var row = FindTarget(point);
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
        var markerHover = _dragRow < 0 ? FindDamageMarker(point) : -1;
        var hover = markerHover >= 0 ? -1 : FindTarget(point);

        if (hover != _hoverRow || markerHover != _hoverDamageMarker)
        {
            _hoverRow = hover;
            _hoverDamageMarker = markerHover;
            InvalidateVisual();
        }

        Cursor = markerHover >= 0 || hover >= 0 ? Cursors.Hand : Cursors.Arrow;

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
        _hoverDamageMarker = -1;
        Cursor = Cursors.Arrow;
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
        var point = e.GetPosition(this);
        var row = FindDamageMarker(point) >= 0 ? DamageRow : FindTarget(point);
        if (row < 0 || ParameterAt(row) is not { } parameter)
            return;

        _selectedRow = row;
        parameter.ResetToReference();
        _status.ShowTransient(row == DamageRow ? "Mutant hit damage reset." : $"{Mutants[row].Label} health reset to Base Preset.");
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
            _damage?.ResetToReference();

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

        if (direction == 0 || ParameterAt(_selectedRow) is not { } selected)
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
        if (!_targets.TryGetValue(row, out var target) || ParameterAt(row) is not { } parameter)
            return;

        var ratio = Math.Clamp((point.X - target.MapRect.Left) / Math.Max(1.0, target.MapRect.Width), 0.0, 1.0);
        var raw = parameter.Minimum + ratio * (parameter.Maximum - parameter.Minimum);
        var tick = Math.Max(parameter.TickFrequency, Epsilon);
        // Align discrete multiplier steps with vanilla 1.00x. The old
        // minimum-anchored grid at 0.25x excluded 1.00x for 0.10x steps.
        var snapped = 1.0 + Math.Round((raw - 1.0) / tick, MidpointRounding.AwayFromZero) * tick;

        parameter.CurrentNumber = Math.Clamp(snapped, parameter.Minimum, parameter.Maximum);
        _status.Clear();
        InvalidateVisual();
    }

    private int FindDamageMarker(Point point)
    {
        for (var i = 0; i < _damageMarkerTargets.Count; i++)
        {
            if (_damageMarkerTargets[i].HitRect.Contains(point))
                return i;
        }
        return -1;
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

    private static double ValueToY(double value, double minimum, double maximum, Rect track)
        => track.Bottom - track.Height * Math.Clamp(
            (value - minimum) / Math.Max(Epsilon, maximum - minimum), 0.0, 1.0);

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

    private void DrawSourceNumber(DrawingContext dc, string label,
        Point center, double size, Brush text, Brush accent,
        FontWeight weight, bool modified)
    {
        var formatted = CreateText(label, size, text, weight);
        if (modified)
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                label, @"[+\-−]?\d+(?:\.\d+)?(?:[%x×])?");
            if (match.Success)
            {
                formatted.SetForegroundBrush(accent, match.Index, match.Length);
                formatted.SetFontSize(Math.Max(9.0, size) + 1.2,
                    match.Index, match.Length);
            }
        }
        dc.DrawText(formatted, new Point(center.X - formatted.Width / 2,
            center.Y - formatted.Height / 2));
    }

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
    private sealed record DamageMarkerHitTarget(double Value, Rect HitRect);
}
