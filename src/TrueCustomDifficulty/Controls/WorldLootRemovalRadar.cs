using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Physical world-loot population depletion board.
///
/// The historical *Radar class name is kept for XAML compatibility. Every row uses
/// the same 0-100% removal semantics and shows the audited base-game / DLC-inclusive
/// populations with approximate removed and remaining actor counts.
/// </summary>
public sealed class WorldLootRemovalRadar : FrameworkElement
{
    private const double Epsilon = 0.000001;
    private const double RowHeight = 56.0;

    private static readonly PopulationDefinition[] Populations =
    {
        new("loot.worldAmmoCratesRemoved", "AMMO BOXES", 1042, 1416, "ammo / grenade box surface"),
        new("loot.worldF1GrenadeBoxes", "F1 GRENADE BOXES", 61, 101, "dedicated F1 boxes; Ammo Boxes can further reduce survivors"),
        new("loot.worldFoodDrinkCratesRemoved", "FOOD & DRINK BOXES", 1100, 1208, "physical food / drink boxes"),
        new("loot.worldMedicalCratesRemoved", "MEDICAL BOXES", 709, 868, "physical medical boxes"),
        new("loot.looseWorldLootRemoved", "LOOSE WORLD LOOT", 5996, 7233, "eligible loose ammo / consumables / grenades"),
    };

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Populations.Length];
    private readonly Dictionary<int, HitTarget> _targets = new();
    private readonly VisualStatusMessage _status;

    private int _selectedRow;
    private int _hoverRow = -1;
    private int _dragRow = -1;

    public static readonly DependencyProperty ParametersProperty = DependencyProperty.Register(
        nameof(Parameters),
        typeof(IEnumerable),
        typeof(WorldLootRemovalRadar),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnParametersChanged));

    public IEnumerable? Parameters
    {
        get => (IEnumerable?)GetValue(ParametersProperty);
        set => SetValue(ParametersProperty, value);
    }

    public WorldLootRemovalRadar()
    {
        _status = new VisualStatusMessage(InvalidateVisual);
        Focusable = true;
        MinHeight = 400;
        Cursor = Cursors.Arrow;
        SnapsToDevicePixels = true;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = double.IsInfinity(availableSize.Width) ? 780.0 : Math.Max(600.0, availableSize.Width);
        return new Size(width, 400.0);
    }

    private static void OnParametersChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((WorldLootRemovalRadar)d).RebindParameters();

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

            for (var i = 0; i < Populations.Length; i++)
            {
                if (!byId.TryGetValue(Populations[i].ParameterId, out var parameter))
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

        DrawCenteredText(dc, "WORLD LOOT DEPLETION", new Point(ActualWidth / 2.0, 21.0),
            12.5, accent, FontWeights.Bold);
        DrawCenteredText(dc, "removes audited physical world actors at build time; 0% keeps vanilla placement", new Point(ActualWidth / 2.0, 39.0),
            9.5, muted, FontWeights.Normal);

        DrawRightAlignedText(
            dc,
            "RANDOM WITHOUT REPLACEMENT ON EACH BUILD",
            new Point(Math.Max(520.0, ActualWidth - 18.0), 18.0),
            7.5,
            muted,
            FontWeights.SemiBold);

        for (var row = 0; row < Populations.Length; row++)
            DrawRow(dc, row, 58.0 + row * RowHeight, text, muted, border, accent, panel);

        DrawCenteredText(
            dc,
            "≈ COUNTS ARE POPULATION ESTIMATES FROM THE SELECTED PERCENTAGE — THE ACTUAL ACTOR SET IS CHOSEN WHEN YOU BUILD",
            new Point(ActualWidth / 2.0, 350.0),
            7.4,
            muted,
            FontWeights.Normal);

        DrawCenteredText(
            dc,
            "QUEST / STORY REWARDS, TRADERS, NPC LOOT, WEAPONS / ARMOR AND SCRIPTED SOURCES ARE OUTSIDE THIS BOARD",
            new Point(ActualWidth / 2.0, 370.0),
            7.4,
            muted,
            FontWeights.Normal);

        if (!string.IsNullOrWhiteSpace(_status.Text))
        {
            DrawCenteredText(
                dc,
                _status.Text,
                new Point(ActualWidth / 2.0, 390.0),
                8.0,
                accent,
                FontWeights.SemiBold);
        }
    }

    private void DrawRow(
        DrawingContext dc,
        int row,
        double top,
        Brush text,
        Brush muted,
        Brush border,
        Brush accent,
        Brush panel)
    {
        var parameter = _parameters[row];
        if (parameter is null)
            return;

        var info = Populations[row];
        var selected = row == _selectedRow;
        var hovered = row == _hoverRow;
        var removal = Math.Clamp(parameter.CurrentNumber, 0.0, 1.0);
        var modified = removal > Epsilon;

        var card = new Rect(14.0, top, Math.Max(1.0, ActualWidth - 28.0), RowHeight - 5.0);
        dc.DrawRoundedRectangle(
            selected
                ? CloneWithOpacity(accent, 0.08)
                : hovered
                    ? CloneWithOpacity(border, 0.10)
                    : CloneWithOpacity(panel, 0.025),
            new Pen(CloneWithOpacity(border, 0.50), 0.8),
            card,
            3.0,
            3.0);

        var labelWidth = Math.Clamp(card.Width * 0.25, 150.0, 205.0);
        var statsWidth = Math.Clamp(card.Width * 0.25, 170.0, 220.0);
        var trackLeft = card.Left + labelWidth + statsWidth + 14.0;
        var trackWidth = Math.Max(170.0, card.Right - trackLeft - 12.0);

        DrawText(
            dc,
            info.Label,
            new Point(card.Left + 10.0, card.Top + 8.0),
            8.6,
            modified ? accent : text,
            FontWeights.Bold);

        DrawText(
            dc,
            info.Note,
            new Point(card.Left + 10.0, card.Top + 24.0),
            9.0,
            muted,
            FontWeights.Normal);

        DrawText(
            dc,
            $"BASE {info.BasePopulation:N0}  |  +DLC1 {info.DlcInclusivePopulation:N0}",
            new Point(card.Left + labelWidth, card.Top + 8.0),
            7.4,
            muted,
            FontWeights.SemiBold);

        var removedBase = EstimateRemoved(info.BasePopulation, removal);
        var remainingBase = Math.Max(0, info.BasePopulation - removedBase);
        var removedDlc = EstimateRemoved(info.DlcInclusivePopulation, removal);
        var remainingDlc = Math.Max(0, info.DlcInclusivePopulation - removedDlc);

        DrawText(
            dc,
            $"≈ REMOVED {removedBase:N0} / {removedDlc:N0}",
            new Point(card.Left + labelWidth, card.Top + 25.0),
            7.5,
            modified ? accent : muted,
            FontWeights.SemiBold);

        DrawText(
            dc,
            $"≈ REMAIN {remainingBase:N0} / {remainingDlc:N0}",
            new Point(card.Left + labelWidth, card.Top + 42.0),
            7.5,
            text,
            FontWeights.SemiBold);

        DrawRightAlignedText(
            dc,
            $"{removal * 100:0}%",
            new Point(card.Right - 12.0, card.Top + 7.0),
            9.4,
            modified ? accent : text,
            FontWeights.Bold);

        var track = new Rect(trackLeft, card.Top + 29.0, trackWidth, 18.0);
        var y = track.Top + track.Height / 2.0;

        dc.DrawLine(new Pen(border, 3.0), new Point(track.Left, y), new Point(track.Right, y));

        var vanillaX = track.Left;
        var currentX = track.Left + track.Width * removal;

        dc.DrawLine(new Pen(muted, 1.1), new Point(vanillaX, y - 8.0), new Point(vanillaX, y + 8.0));
        dc.DrawLine(new Pen(accent, 3.2), new Point(track.Left, y), new Point(currentX, y));

        dc.DrawEllipse(
            panel,
            new Pen(accent, selected ? 2.5 : 1.5),
            new Point(currentX, y),
            6.0,
            6.0);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.7, 2.7);


        _targets[row] = new HitTarget(
            new Rect(track.Left - 7.0, track.Top - 7.0, track.Width + 14.0, track.Height + 14.0),
            track);
    }

    private static int EstimateRemoved(int population, double removal)
        => Math.Clamp(
            (int)Math.Round(population * removal, MidpointRounding.AwayFromZero),
            0,
            population);

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
        _status.ShowTransient($"{Populations[row].Label} removal reset to Base Preset.");
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

            _status.ShowTransient("World loot removal reset to Base Preset.");
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

    private sealed record PopulationDefinition(
        string ParameterId,
        string Label,
        int BasePopulation,
        int DlcInclusivePopulation,
        string Note);

    private sealed record HitTarget(Rect HitRect, Rect MapRect);
}
