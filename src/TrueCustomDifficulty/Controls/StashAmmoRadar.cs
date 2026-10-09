using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Stash-ammo quantity board.
///
/// The historical *Radar class name is kept for XAML compatibility. Each tier keeps
/// its original quantity multiplier while live sample stack sizes are calculated from
/// the authored LootHint entries. Ammo membership, selection weights and rank gates
/// remain untouched.
/// </summary>
public sealed class StashAmmoRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double MinimumValue = 0.25;
    private const double MaximumValue = 2.0;
    private const double Step = 0.25;
    private const double TierHeight = 105.0;

    private static readonly TierDefinition[] Tiers =
    {
        new("loot.cheapStashAmmoQuantity", "BASIC", "starter / cheap stash ammo"),
        new("loot.mediumStashAmmoQuantity", "STANDARD", "normal progression stash ammo"),
        new("loot.expensiveStashAmmoQuantity", "RARE", "high-tier / rare stash ammo"),
    };

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Tiers.Length];
    private readonly Dictionary<int, HitTarget> _targets = new();
    private readonly VisualStatusMessage _status;

    private int _selectedTier;
    private int _hoverTier = -1;
    private int _dragTier = -1;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(StashAmmoRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public StashAmmoRadar()
    {
        _status = new VisualStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = 390;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 760.0 : Math.Max(580.0, availableSize.Width);
        return new Size(width, 390.0);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((StashAmmoRadar)d).RebindParameters();

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

            for (var i = 0; i < Tiers.Length; i++)
            {
                if (!byId.TryGetValue(Tiers[i].ParameterId, out var parameter))
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

        DrawCenteredText(dc, "STASH AMMO AMOUNTS", new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);


        for (var tier = 0; tier < Tiers.Length; tier++)
            DrawTier(dc, tier, 58.0 + tier * TierHeight, text, muted, border, accent, panel);

        // Reset notifications intentionally silent.
    }

    private void DrawTier(
        DrawingContext dc,
        int tier,
        double top,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var parameter = _parameters[tier];
        if (parameter is null)
            return;

        var info = Tiers[tier];
        var selected = tier == _selectedTier;
        var hovered = tier == _hoverTier;
        var modified = Math.Abs(parameter.CurrentNumber - parameter.ReferenceNumber) > Epsilon;

        var card = new Rect(16.0, top, Math.Max(1.0, ActualWidth - 32.0), TierHeight - 10.0);
        dc.DrawRoundedRectangle(
            selected
                ? CloneWithOpacity(accent, 0.08)
                : hovered
                    ? CloneWithOpacity(border, 0.10)
                    : CloneWithOpacity(panel, 0.04),
            new Pen(border, 0.9),
            card,
            4.0,
            4.0);

        DrawText(
            dc,
            $"{info.Label} STASH",
            new Point(card.Left + 12.0, card.Top + 9.0),
            9.2,
            text,
            FontWeights.Bold);

        DrawRightAlignedText(
            dc,
            $"{parameter.CurrentNumber:0.##}x",
            new Point(card.Left + 12.0 + Math.Max(180.0, card.Width * 0.41), card.Top + 8.0),
            10.0,
            modified ? accent : text,
            FontWeights.Bold);

        var track = new Rect(card.Left + 12.0, card.Top + 41.0, Math.Max(180.0, card.Width * 0.41), 12.0);
        var trackY = track.Top + track.Height / 2.0;

        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, trackY), new Point(track.Right, trackY));

        var currentX = ValueToX(parameter.CurrentNumber, MinimumValue, MaximumValue, track);
        var vanillaX = ValueToX(parameter.ReferenceNumber, MinimumValue, MaximumValue, track);

        dc.DrawLine(new Pen(muted, 1.1), new Point(vanillaX, trackY - 8.0), new Point(vanillaX, trackY + 8.0));
        dc.DrawLine(new Pen(accent, 3.2), new Point(track.Left, trackY), new Point(currentX, trackY));

        dc.DrawEllipse(
            panel,
            new Pen(accent, selected ? 2.5 : 1.5),
            new Point(currentX, trackY),
            6.0,
            6.0);
        dc.DrawEllipse(accent, null, new Point(currentX, trackY), 2.7, 2.7);


        _targets[tier] = new HitTarget(
            new Rect(track.Left - 7.0, track.Top - 7.0, track.Width + 14.0, track.Height + 14.0),
            track);

        DrawStackPreview(
            dc,
            parameter,
            new Rect(track.Right + 28.0, card.Top + 8.0, Math.Max(180.0, card.Right - track.Right - 40.0), card.Height - 16.0),
            text,
            muted,
            accent);
    }

    private void DrawStackPreview(
        DrawingContext dc,
        ParameterViewModel parameter,
        Rect bounds,
        Brush text,
        Brush muted,
        Brush accent)
    {
        var hint = parameter.Definition.LootHint;

        DrawText(dc, "SAMPLE AMMO STACKS", new Point(bounds.Left, bounds.Top), 7.8, muted, FontWeights.Bold);

        if (hint is null || hint.Kind != "scaledCountEntries" || hint.Entries.Count == 0)
        {
            DrawText(dc, "No authored preview rows.", new Point(bounds.Left, bounds.Top + 22.0), 7.5, muted, FontWeights.Normal);
            return;
        }

        var maxRows = Math.Min(3, hint.Entries.Count);
        var y = bounds.Top + 21.0;

        for (var i = 0; i < maxRows; i++)
        {
            var entry = hint.Entries[i];
            var min = ScaleCount(entry.Min, parameter.CurrentNumber);
            var max = ScaleCount(entry.Max, parameter.CurrentNumber);
            var qualifier = string.IsNullOrWhiteSpace(entry.Qualifier) ? string.Empty : $" [{entry.Qualifier}]";

            DrawText(
                dc,
                $"{entry.Label}{qualifier}",
                new Point(bounds.Left, y),
                7.6,
                i == 0 ? text : muted,
                i == 0 ? FontWeights.SemiBold : FontWeights.Normal);

            DrawRightAlignedText(
                dc,
                $"{min}-{max} rounds",
                new Point(bounds.Right, y),
                7.8,
                accent,
                FontWeights.SemiBold);

            y += 18.0;
        }

        if (hint.Entries.Count > maxRows)
        {
            DrawText(
                dc,
                $"+ {hint.Entries.Count - maxRows} more authored rows",
                new Point(bounds.Left, y + 1.0),
                7.0,
                muted,
                FontWeights.Normal);
        }
    }

    private static int ScaleCount(double source, double multiplier)
        => Math.Max(1, (int)Math.Floor(source * multiplier + 0.5));

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        var tier = FindTarget(e.GetPosition(this));
        if (tier < 0)
            return;

        _selectedTier = tier;
        _dragTier = tier;
        CaptureMouse();
        UpdateFromPoint(tier, e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        var point = e.GetPosition(this);
        var hover = FindTarget(point);

        if (hover != _hoverTier)
        {
            _hoverTier = hover;
            InvalidateVisual();
        }

        Cursor = hover >= 0 ? Cursors.Hand : Cursors.Arrow;

        if (_dragTier < 0 || e.LeftButton != MouseButtonState.Pressed)
            return;

        UpdateFromPoint(_dragTier, point);
        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoverTier < 0)
            return;

        _hoverTier = -1;
        InvalidateVisual();
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (_dragTier < 0)
            return;

        _dragTier = -1;
        ReleaseMouseCapture();
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);

        var tier = FindTarget(e.GetPosition(this));
        if (tier < 0 || _parameters[tier] is not { } parameter)
            return;

        _selectedTier = tier;
        parameter.ResetToReference();
        _status.ShowTransient($"{Tiers[tier].Label} stash ammo reset to Base Preset.");
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

            _status.ShowTransient("Stash ammo amounts reset to Base Preset.");
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

        if (direction == 0 || _parameters[_selectedTier] is not { } selected)
            return;

        selected.CurrentNumber = Math.Clamp(
            selected.CurrentNumber + direction * Step,
            MinimumValue,
            MaximumValue);

        _status.Clear();
        InvalidateVisual();
        e.Handled = true;
    }

    private void UpdateFromPoint(int tier, Point point)
    {
        if (!_targets.TryGetValue(tier, out var target) || _parameters[tier] is not { } parameter)
            return;

        var ratio = Math.Clamp(
            (point.X - target.MapRect.Left) / Math.Max(1.0, target.MapRect.Width),
            0.0,
            1.0);

        var raw = MinimumValue + ratio * (MaximumValue - MinimumValue);
        var snapped = Math.Round(raw / Step, MidpointRounding.AwayFromZero) * Step;

        parameter.CurrentNumber = Math.Clamp(snapped, MinimumValue, MaximumValue);
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

    private sealed record TierDefinition(string ParameterId, string Label, string Subtitle);
    private sealed record HitTarget(Rect HitRect, Rect MapRect);
}
