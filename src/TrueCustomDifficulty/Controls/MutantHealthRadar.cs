using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

public sealed class MutantHealthRadar : FrameworkElement
{
    // Shared white card hover (CardHoverTracker): cards drawn by the
    // renderer are recorded and the hovered one is outlined on top.
    private readonly CardHoverTracker _cardHover = new();

    // A click anywhere in a mutant (or hit-damage) card explains that one.
    public event EventHandler<AggregateParameterSelectedEventArgs>? ParameterSelected;

    private void SelectCardAt(Point point)
    {
        if (_cardHover.OwnerAt(point) is { } owner)
            ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(owner));
    }

    protected override void OnRender(DrawingContext dc)
    {
        using (_cardHover.BeginRender())
            RenderCardContent(dc);
        _cardHover.Finish(Array.Empty<Rect>(), RenderSize);
        _cardHover.DrawHover(dc);
    }

    private const double Epsilon = 0.000001;
    // Two global cards (hit damage, special ability pacing) and 14 species
    // cards: a health track, then ATTACKS and ABILITIES side by side.
    private const double DesiredHeight = SpeciesTop + 7 * SpeciesRowPitch + 8.0;
    private const int DamageRow = 14;
    private const int SpecialRow = 15;
    private const int RateRowBase = 16;
    // Move knobs: one row id per knob, from KnobRowBase in MoveKnobs order.
    private const int KnobRowBase = 100;
    private const double GroupRowTop = 47.0;       // ATTACKS caption + slider
    private const double AbilityHeaderTop = 66.0;  // ABILITIES header over its knobs
    private const double KnobRowTop = 80.0;        // knob grid below
    private const double KnobRadius = 18.0;
    private const int KnobColumns = 5;              // most moves on one mutant (attacks + abilities)
    private const double KnobStartAngle = 135.0;
    private const double KnobSweepAngle = 270.0;
    private const double KnobDragPixelsPerStep = 8.0;
    private const double GlobalCardHeight = 71.0;
    private const double SpeciesTop = 205.0;
    private const double SpeciesCardHeight = 156.0;
    private const double SpeciesRowPitch = 163.0;
    private const double MaxContentWidth = 1080.0;
    private const double ColumnGap = 12.0;

    private static readonly MutantDefinition[] Mutants =
    {
        new("combat.mutantHealth.bayun", "Bayun", 400.0),
        new("combat.mutantHealth.blinddog", "Blind Dog", 63.0),
        new("combat.mutantHealth.bloodsucker", "Bloodsucker", 500.0),
        new("combat.mutantHealth.boar", "Boar", 300.0),
        new("combat.mutantHealth.burer", "Burer", 400.0),
        new("combat.mutantHealth.chimera", "Chimera", 1400.0),
        new("combat.mutantHealth.controller", "Controller", 500.0),
        new("combat.mutantHealth.deer", "Deer", 600.0),
        new("combat.mutantHealth.flesh", "Flesh", 280.0),
        new("combat.mutantHealth.poltergeist", "Poltergeist", 400.0),
        new("combat.mutantHealth.pseudodog", "Pseudodog", 300.0),
        new("combat.mutantHealth.pseudogiant", "Pseudogiant", 2500.0),
        new("combat.mutantHealth.snork", "Snork", 150.0),
        new("combat.mutantHealth.tushkan", "Tushkan", 19.0),
    };

    private readonly ParameterViewModel?[] _parameters = new ParameterViewModel?[Mutants.Length];
    private readonly ParameterViewModel?[] _rates = new ParameterViewModel?[Mutants.Length];
    private ParameterViewModel? _special;
    private readonly ParameterViewModel?[] _knobs = new ParameterViewModel?[MutantAggressionPolicy.MoveKnobs.Count];
    private double _knobDragStartY;
    private double _knobDragStartValue;
    private readonly Dictionary<int, HitTarget> _targets = new();
    // Marker labels are separate clickable hit regions, not part of the fader.
    private readonly List<DamageMarkerHitTarget> _damageMarkerTargets = new();
    private ParameterViewModel? _damage;
    private int _hoverDamageMarker = -1;
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

        foreach (var rate in _rates)
        {
            if (rate is not null)
                rate.PropertyChanged -= Parameter_PropertyChanged;
        }
        if (_damage is not null)
            _damage.PropertyChanged -= Parameter_PropertyChanged;
        if (_special is not null)
            _special.PropertyChanged -= Parameter_PropertyChanged;
        foreach (var knob in _knobs)
        {
            if (knob is not null)
                knob.PropertyChanged -= Parameter_PropertyChanged;
        }
        Array.Clear(_knobs, 0, _knobs.Length);
        _damage = null;
        _special = null;
        Array.Clear(_rates, 0, _rates.Length);
        _hoverDamageMarker = -1;
        _damageMarkerTargets.Clear();
        Array.Clear(_parameters, 0, _parameters.Length);

        if (Parameters is not null)
        {
            var byId = Parameters.OfType<ParameterViewModel>().ToDictionary(x => x.Id, StringComparer.Ordinal);
            if (byId.TryGetValue("combat.mutantDamage", out _damage))
                _damage.PropertyChanged += Parameter_PropertyChanged;
            if (byId.TryGetValue(MutantAggressionPolicy.SpecialAbilityRateId, out _special))
                _special.PropertyChanged += Parameter_PropertyChanged;
            for (var k = 0; k < _knobs.Length; k++)
            {
                var (family, _, move) = MutantAggressionPolicy.MoveKnobs[k];
                if (byId.TryGetValue(MutantAggressionPolicy.MoveId(family, move), out var knob))
                {
                    _knobs[k] = knob;
                    knob.PropertyChanged += Parameter_PropertyChanged;
                }
            }
            for (var i = 0; i < Mutants.Length; i++)
            {
                if (!byId.TryGetValue(Mutants[i].ParameterId, out var parameter))
                    continue;

                _parameters[i] = parameter;
                parameter.PropertyChanged += Parameter_PropertyChanged;
                if (byId.TryGetValue(Mutants[i].RateId, out var rate))
                {
                    _rates[i] = rate;
                    rate.PropertyChanged += Parameter_PropertyChanged;
                }
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

    private void RenderCardContent(DrawingContext dc)
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
        DrawCenteredText(dc, "MUTANT DAMAGE, HEALTH AND AGGRESSION", new Point(ActualWidth / 2.0, 21.0),
            VisualControlTypography.BoardTitle, accent, FontWeights.Bold);

        // Stamina Usage-style grouped, independent horizontal controls.
        // Seven rows, two columns, alphabetically ordered down each column.
        var content = ContentRect();
        if (_damage is { } damage)
            DrawGlobalTrack(dc, damage, DamageRow, "MUTANT HIT DAMAGE",
                $"{damage.CurrentNumber:0.##}× hit dmg",
                new Rect(content.Left, 43.0, content.Width, GlobalCardHeight),
                text, muted, border, accent, panel);
        if (_special is { } special)
            DrawGlobalTrack(dc, special, SpecialRow, "SPECIAL ABILITY RATE",
                HumanCombatPolicy.WithoutFactor(MutantAggressionPolicy.SpecialAbilityDisplay(special.CurrentNumber)),
                new Rect(content.Left, 43.0 + GlobalCardHeight + 8.0, content.Width, GlobalCardHeight),
                text, muted, border, accent, panel);
        var cardWidth = (content.Width - ColumnGap) / 2.0;
        for (var i = 0; i < Mutants.Length; i++)
        {
            var column = i / 7;
            var row = i % 7;
            DrawRow(dc, i, new Rect(
                content.Left + column * (cardWidth + ColumnGap),
                SpeciesTop + row * SpeciesRowPitch, cardWidth, SpeciesCardHeight),
                text, muted, border, accent, panel);
        }
    }

    // Full-width global track (hit damage / special ability rate) with
    // clickable per-difficulty vanilla markers.
    private void DrawGlobalTrack(DrawingContext dc, ParameterViewModel parameter, int row,
        string title, string valueLabel, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        dc.DrawTrackedRoundedRectangle(panel, new Pen(border, 0.9), card, 4.0, 4.0, parameter);
        DrawText(dc, title, new Point(card.Left + 11.0, card.Top + 7.0), VisualControlTypography.CardTitle, text, FontWeights.Bold);
        var value = CreateText(valueLabel, 11.0,
            parameter.IsValueModifiedFromReference() ? ReadoutInk.Numbers(accent, text) : text, FontWeights.Bold);
        dc.DrawText(value, new Point(card.Right - 11.0 - value.Width, card.Top + 7.0));
        var rail = new Rect(card.Left + 14.0, card.Top + 34.0, card.Width - 28.0, 9.0);
        var y = rail.Top + rail.Height / 2.0;
        var currentX = ValueToX(parameter.CurrentNumber, parameter.Minimum, parameter.Maximum, rail);
        dc.DrawLine(new Pen(border, 3), new Point(rail.Left, y), new Point(rail.Right, y));
        dc.DrawLine(new Pen(accent, 3), new Point(rail.Left, y), new Point(currentX, y));
        for (var i = 0; i < parameter.VanillaMarkers.Count; i++)
        {
            var marker = parameter.VanillaMarkers[i];
            var markerX = ValueToX(marker.Value, parameter.Minimum, parameter.Maximum, rail);
            var hovered = _damageMarkerTargets.Count == _hoverDamageMarker;
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
            _damageMarkerTargets.Add(new DamageMarkerHitTarget(parameter, row, marker.Value, hit));
        }
        ControlHoverGlow.Draw(dc, new Point(currentX, y), accent, _hoverRow == row);
        dc.DrawEllipse(panel, new Pen(accent, _selectedRow == row ? 2.5 : 1.5),
            new Point(currentX, y), 6, 6);
        dc.DrawEllipse(accent, null, new Point(currentX, y), 2.5, 2.5);
        _targets[row] = new HitTarget(new Rect(rail.Left - 6.0, rail.Top - 8.0,
            rail.Width + 12.0, 23.0), rail);
    }

    private ParameterViewModel? ParameterAt(int row)
        => row == DamageRow ? _damage
            : row == SpecialRow ? _special
            : row >= KnobRowBase && row < KnobRowBase + _knobs.Length ? _knobs[row - KnobRowBase]
            : row >= RateRowBase && row < RateRowBase + _rates.Length ? _rates[row - RateRowBase]
            : row >= 0 && row < _parameters.Length ? _parameters[row] : null;

    private void DrawRow(
        DrawingContext dc, int row, Rect card,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        if (_parameters[row] is not { } parameter)
            return;

        var mutant = Mutants[row];
        var modified = parameter.IsValueModifiedFromReference();

        // Static captions and card fill never turn gold on slider hover.
        dc.DrawTrackedRoundedRectangle(panel,
            new Pen(border, 0.9), card, 4.0, 4.0, parameter);
        DrawText(dc, mutant.Label.ToUpperInvariant(),
            new Point(card.Left + 11.0, card.Top + 6.0),
            10.8, text, FontWeights.Bold);

        // Top row: species left, current multiplier centered, derived real HP right.
        // Current multiplier and actual HP stay legible; only changed digits turn gold.
        DrawSourceNumber(dc, $"{parameter.CurrentNumber:0.##}×",
            new Point(card.Left + card.Width / 2.0, card.Top + 12.0),
            13.0, text, accent, FontWeights.Bold, modified);

        var currentHp = mutant.VanillaHp * parameter.CurrentNumber;
        var hpLabel = $"{currentHp:0} HP";
        var hpText = CreateText(hpLabel, 12.5, text, FontWeights.Bold);
        if (modified)
        {
            var hpDigits = hpLabel.IndexOf(' ');
            if (hpDigits > 0)
            {
                hpText.SetForegroundBrush(accent, 0, hpDigits);
                hpText.SetFontSize(13.7, 0, hpDigits);
            }
        }
        dc.DrawText(hpText,
            new Point(card.Right - 11.0 - hpText.WidthIncludingTrailingWhitespace,
                card.Top + 6.0));

        DrawSpeciesTrack(dc, parameter, row, new Rect(card.Left + 12.0, card.Top + 30.0,
            Math.Max(1.0, card.Width - 24.0), 10.0), muted, border, accent, panel);

        // ATTACKS: caption and slider across the card. Below, every move as a
        // knob on one five-column grid spanning the card (no mutant has more
        // than five): attacks from the left, abilities from the right under an
        // ABILITIES header with a thin divider. Columns line up across cards.
        var family = MutantAggressionPolicy.Families[row];
        var inner = new Rect(card.Left + 12.0, card.Top, Math.Max(1.0, card.Width - 24.0), card.Height);
        if (_rates[row] is { } attackRate)
        {
            var caption = CreateText("ATTACKS", 9.4, muted, FontWeights.Bold);
            dc.DrawText(caption, new Point(inner.Left - 1.0, card.Top + GroupRowTop));
            var trackLeft = inner.Left + caption.Width + 12.0;
            DrawSpeciesTrack(dc, attackRate, RateRowBase + row,
                new Rect(trackLeft, card.Top + GroupRowTop + 2.0, Math.Max(1.0, inner.Right - trackLeft), 10.0),
                muted, border, accent, panel);
        }

        var attacks = MutantAggressionPolicy.Moves(family, MutantAggressionPolicy.Group.Attacks);
        var abilities = MutantAggressionPolicy.Moves(family, MutantAggressionPolicy.Group.Abilities);
        var cell = inner.Width / KnobColumns;
        var knobTop = card.Top + KnobRowTop;
        if (_rates[row] is not null)
            DrawKnobCells(dc, family, MutantAggressionPolicy.Group.Attacks, attacks, _rates[row], inner.Left, cell,
                knobTop, text, muted, border, accent, panel);
        if (abilities.Count > 0)
        {
            var abilityLeft = inner.Left + cell * (KnobColumns - abilities.Count);
            DrawText(dc, "ABILITIES", new Point(abilityLeft + 6.0, card.Top + AbilityHeaderTop), 9.4, muted, FontWeights.Bold);
            if (attacks.Count > 0)
                dc.DrawLine(new Pen(CloneWithOpacity(border, 0.8), 1.0),
                    new Point(abilityLeft, card.Top + AbilityHeaderTop + 2.0),
                    new Point(abilityLeft, card.Bottom - 8.0));
            DrawKnobCells(dc, family, MutantAggressionPolicy.Group.Abilities, abilities, _rates[row], abilityLeft, cell,
                knobTop, text, muted, border, accent, panel);
        }
    }

    private void DrawKnobCells(DrawingContext dc, MutantAggressionPolicy.Family family,
        MutantAggressionPolicy.Group group, IReadOnlyList<MutantAggressionPolicy.Move> moves,
        ParameterViewModel? attackRate, double left, double cell, double top,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        for (var i = 0; i < moves.Count; i++)
        {
            var index = KnobIndex(family, moves[i]);
            if (index < 0 || _knobs[index] is not { } knob)
                continue;
            var center = new Point(left + cell * (i + 0.5), top + KnobRadius + 3.0);
            DrawMoveKnob(dc, family, group, moves[i], attackRate, knob, KnobRowBase + index, center, cell,
                text, muted, border, accent, panel);
        }
    }

    private static int KnobIndex(MutantAggressionPolicy.Family family, MutantAggressionPolicy.Move move)
    {
        for (var k = 0; k < MutantAggressionPolicy.MoveKnobs.Count; k++)
        {
            var entry = MutantAggressionPolicy.MoveKnobs[k];
            if (ReferenceEquals(entry.Family, family) && entry.Move.Slug == move.Slug && entry.Move.Tag == move.Tag)
                return k;
        }
        return -1;
    }

    // Source-native 270° knob: track and value arcs, reference tick, pointer,
    // CAPS caption and the move's real wait in seconds. Only changed digits turn gold.
    private void DrawMoveKnob(DrawingContext dc, MutantAggressionPolicy.Family family,
        MutantAggressionPolicy.Group group, MutantAggressionPolicy.Move move, ParameterViewModel? attackRate,
        ParameterViewModel knob, int row, Point center, double cell,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        var radius = Math.Min(KnobRadius, Math.Max(10.0, cell / 2.0 - 8.0));
        var range = Math.Max(Epsilon, knob.Maximum - knob.Minimum);
        var ratio = Math.Clamp((knob.CurrentNumber - knob.Minimum) / range, 0.0, 1.0);
        var reference = Math.Clamp((knob.ReferenceNumber - knob.Minimum) / range, 0.0, 1.0);
        var hovered = row == _hoverRow;
        var selected = row == _selectedRow;

        ControlHoverGlow.Draw(dc, center, accent, hovered);
        dc.DrawEllipse(CloneWithOpacity(panel, 0.96), new Pen(border, 1.0), center, radius - 5.0, radius - 5.0);
        DrawKnobArc(dc, center, radius, KnobSweepAngle, new Pen(border, 3.6));
        DrawKnobArc(dc, center, radius, KnobSweepAngle * ratio, new Pen(accent, 3.6));
        var referenceAngle = KnobStartAngle + KnobSweepAngle * reference;
        dc.DrawLine(new Pen(muted, 1.6), KnobPoint(center, radius - 5.0, referenceAngle),
            KnobPoint(center, radius + 3.5, referenceAngle));
        var angle = KnobStartAngle + KnobSweepAngle * ratio;
        dc.DrawLine(new Pen(accent, selected || hovered ? 2.6 : 2.0),
            KnobPoint(center, 2.5, angle), KnobPoint(center, radius - 5.5, angle));
        dc.DrawEllipse(accent, null, center, 2.0, 2.0);

        var caption = CreateText(move.Label, 9.4, muted, FontWeights.Bold);
        dc.DrawText(caption, new Point(center.X - caption.Width / 2.0, center.Y + radius + 4.0));
        var rate = attackRate?.CurrentNumber ?? 1.0;
        var rateReference = attackRate?.ReferenceNumber ?? 1.0;
        var seconds = MutantAggressionPolicy.MoveSeconds(family, group, move, rate, rateReference,
            knob.CurrentNumber, knob.ReferenceNumber);
        var vanilla = MutantAggressionPolicy.MoveSeconds(family, group, move, rateReference, rateReference,
            knob.ReferenceNumber, knob.ReferenceNumber);
        var changed = Math.Abs(seconds - vanilla) > 0.0001;
        var value = CreateText(MutantAggressionPolicy.Seconds(seconds), 11.0,
            changed ? ReadoutInk.Numbers(accent, text) : text, FontWeights.Bold);
        dc.DrawText(value, new Point(center.X - value.Width / 2.0, center.Y + radius + 17.0));

        _targets[row] = new HitTarget(
            new Rect(center.X - cell / 2.0 + 1.0, center.Y - radius - 4.0, cell - 2.0, radius * 2.0 + 38.0),
            Rect.Empty);
    }

    private static Point KnobPoint(Point center, double radius, double degrees)
    {
        var radians = degrees * Math.PI / 180.0;
        return new Point(center.X + radius * Math.Cos(radians), center.Y + radius * Math.Sin(radians));
    }

    private static void DrawKnobArc(DrawingContext dc, Point center, double radius, double sweep, Pen pen)
    {
        if (sweep <= Epsilon)
            return;
        var figure = new PathFigure
        {
            StartPoint = KnobPoint(center, radius, KnobStartAngle),
            IsClosed = false,
            IsFilled = false
        };
        figure.Segments.Add(new ArcSegment(KnobPoint(center, radius, KnobStartAngle + sweep),
            new Size(radius, radius), 0.0, sweep > 180.0, SweepDirection.Clockwise, true));
        dc.DrawGeometry(null, pen, new PathGeometry(new[] { figure }));
    }

    private bool IsKnobRow(int row) => row >= KnobRowBase && row < KnobRowBase + _knobs.Length;

    private void DrawSpeciesTrack(DrawingContext dc, ParameterViewModel parameter, int row,
        Rect track, Brush muted, Brush border, Brush accent, Brush panel)
    {
        var hovered = row == _hoverRow;
        var selected = row == _selectedRow;
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

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);
        Focus();

        // Hit-test markers before the rail: their dots partially overlap it.
        // A marker click must never start a drag or go through normal tick snapping.
        var point = e.GetPosition(this);
        var markerIndex = FindDamageMarker(point);
        if (markerIndex >= 0)
        {
            var marker = _damageMarkerTargets[markerIndex];
            _selectedRow = marker.Row;
            marker.Parameter.SnapCurrentNumberToMarker(marker.Value);
            ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(marker.Parameter));
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        var row = FindTarget(point);
        // A track explains its own setting (HEALTH or ATTACK RATE); elsewhere
        // in a card the card's owner is explained.
        if (row >= 0 && ParameterAt(row) is { } trackParameter)
            ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(trackParameter));
        else
            SelectCardAt(point);
        if (row < 0)
            return;

        _selectedRow = row;
        _dragRow = row;
        CaptureMouse();
        if (IsKnobRow(row) && ParameterAt(row) is { } knobParameter)
        {
            // Knobs turn by dragging up / right; a click alone keeps the value.
            _knobDragStartY = point.Y - point.X;
            _knobDragStartValue = knobParameter.CurrentNumber;
            InvalidateVisual();
        }
        else
            UpdateFromPoint(row, e.GetPosition(this));
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_cardHover.UpdateHover(e.GetPosition(this))) InvalidateVisual();
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
            if (IsKnobRow(_dragRow) && ParameterAt(_dragRow) is { } knobParameter)
            {
                var steps = Math.Round((_knobDragStartY - (point.Y - point.X)) / KnobDragPixelsPerStep);
                knobParameter.CurrentNumber = Math.Clamp(_knobDragStartValue + steps * knobParameter.TickFrequency,
                    knobParameter.Minimum, knobParameter.Maximum);
                InvalidateVisual();
            }
            else
                UpdateFromPoint(_dragRow, point);
            e.Handled = true;
        }
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        if (_cardHover.ClearHover()) InvalidateVisual();
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

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        var row = FindTarget(e.GetPosition(this));
        if (row < 0 || ParameterAt(row) is not { } parameter)
        {
            base.OnMouseWheel(e);
            return;
        }
        _selectedRow = row;
        parameter.CurrentNumber = Math.Clamp(
            parameter.CurrentNumber + Math.Sign(e.Delta) * parameter.TickFrequency,
            parameter.Minimum, parameter.Maximum);
        ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(parameter));
        InvalidateVisual();
        e.Handled = true;
    }

    protected override void OnMouseRightButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonDown(e);
        var point = e.GetPosition(this);
        var markerIndex = FindDamageMarker(point);
        var row = markerIndex >= 0 ? _damageMarkerTargets[markerIndex].Row : FindTarget(point);
        if (row < 0 || ParameterAt(row) is not { } parameter)
        {
            SelectCardAt(point);
            return;
        }

        ParameterSelected?.Invoke(this, new AggregateParameterSelectedEventArgs(parameter));
        _selectedRow = row;
        parameter.ResetToReference();
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
            foreach (var rate in _rates)
                rate?.ResetToReference();
            _damage?.ResetToReference();
            _special?.ResetToReference();
            foreach (var knob in _knobs)
                knob?.ResetToReference();

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

        // Off-grid vanilla (e.g. 0.35x hit damage on Novice) stays reachable.
        parameter.CurrentNumber = parameter.ReferenceMagnet(raw,
            Math.Clamp(snapped, parameter.Minimum, parameter.Maximum));
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
                label, @"[+\-−]?\d+(?:\.\d+)?");
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

    private FormattedText CreateText(string value, double size, Brush brush, FontWeight weight)
    {
        var (ink, numberInk) = ReadoutInk.Resolve(brush);
        var formatted = new FormattedText(
            value,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal),
            Math.Max(9.0, size),
            ink,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        ReadoutInk.Highlight(formatted, value, numberInk, Math.Max(9.0, size));

        return formatted;
    }

    private sealed record MutantDefinition(string ParameterId, string Label, double VanillaHp)
    {
        public string RateId => MutantAggressionPolicy.Prefix + ParameterId["combat.mutantHealth.".Length..];
    }
    private sealed record HitTarget(Rect HitRect, Rect MapRect);
    private sealed record DamageMarkerHitTarget(ParameterViewModel Parameter, int Row, double Value, Rect HitRect);
}
