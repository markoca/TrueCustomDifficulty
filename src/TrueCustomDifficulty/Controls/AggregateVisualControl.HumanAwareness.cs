using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// HUMAN AWARENESS detection radar (top-down, enemy in the centre, facing up).
/// SIGHT = vision field: 80 m straight ahead (CentralVisionDistance) narrowing
/// to 25 m at the 85° edge (PeripheralVisionDistance, HorizontalVisionHalvedAngle).
/// The shape between the two source distances is drawn as a straight blend;
/// the game's own angle curve is an asset, not CFG. HEARING = footstep circle
/// (12 m). MEMORY = search timer ring around the radar (30 s), clockwise from
/// the top. Distance rings now fill the visual radar from 0 to 200 m on a
/// square-root scale. The actual SIGHT source still reaches 240 m at 3x:
/// above 200 m the sector is clipped only visually, and the truthful metres
/// remain in the legend. Dragging the rim tip can overshoot the graphic to
/// access the full underlying range. All sources, presets and writer stay intact.
/// </summary>
public sealed partial class AggregateVisualControl
{
    private const double AwarenessDisplayMaxMeters = 200.0; // visual scale only; SIGHT at 3x remains 240 m
    private const double AwarenessSightMeters = 80.0;     // CentralVisionDistance
    private const double AwarenessSideMeters = 25.0;      // PeripheralVisionDistance
    private const double AwarenessHearingMeters = 12.0;   // footsteps
    private const double AwarenessMemorySeconds = 30.0;   // alert freeze time
    private const double AwarenessHalfAngle = 85.0;       // HorizontalVisionHalvedAngleDegrees
    private const double AwarenessMemoryGap = 18.0;       // ring outside the radar
    private const double AwarenessLegendRow = 46.0;

    // A sight value >200 m shares the outer 200 m marker. Preserve the
    // grabbed source value across the first pointer update; never collapse a
    // 3x (240 m) preset to 2.5x just because its handle is at the rim.
    private double _awarenessSightDragOffsetMeters;

    private readonly record struct AwarenessLayout(Point Center, double Radius, Rect Legend, bool Wide, double Height);

    private static AwarenessLayout HumanAwarenessLayout(double width)
    {
        var wide = width >= 750.0;
        // Side margins leave room for the MEMORY caption outside the ring.
        var radius = wide
            ? Math.Clamp((width - 420.0) / 2.0, 90.0, 150.0)
            : Math.Clamp((width - 170.0) / 2.0, 70.0, 140.0);
        var centerX = wide ? 70.0 + AwarenessMemoryGap + radius : width / 2.0;
        var center = new Point(centerX, 48.0 + AwarenessMemoryGap + radius + 8.0);
        var radarBottom = center.Y + radius + AwarenessMemoryGap + 14.0;
        Rect legend;
        double height;
        if (wide)
        {
            var left = center.X + radius + AwarenessMemoryGap + 70.0;
            legend = new Rect(left, center.Y - AwarenessLegendRow * 1.5,
                Math.Max(1.0, width - left - 16.0), AwarenessLegendRow * 3.0);
            height = radarBottom;
        }
        else
        {
            legend = new Rect(16.0, radarBottom, Math.Max(1.0, width - 32.0), AwarenessLegendRow * 3.0);
            height = legend.Bottom + 8.0;
        }
        return new AwarenessLayout(center, radius, legend, wide, height);
    }

    private static double HumanAwarenessHeight(double width) => HumanAwarenessLayout(width).Height;

    private static double AwarenessRadius(double meters, double radius)
        => radius * Math.Sqrt(Math.Clamp(meters, 0.0, AwarenessDisplayMaxMeters) / AwarenessDisplayMaxMeters);

    private static double AwarenessMeters(double pixels, double radius)
    {
        var ratio = Math.Clamp(pixels / Math.Max(1.0, radius), 0.0, 1.0);
        return AwarenessDisplayMaxMeters * ratio * ratio;
    }

    // SIGHT pointer travel may extend slightly outside the drawn 200 m rim.
    // This keeps 2.5x–3x editable without drawing a misleading 240 m ring.
    private static double AwarenessSightDragMeters(double pixels, double radius)
    {
        var ratio = Math.Max(0.0, pixels / Math.Max(1.0, radius));
        return AwarenessDisplayMaxMeters * ratio * ratio;
    }

    // Angle 0 = straight ahead (up), positive = clockwise.
    private static Point AwarenessPolar(Point center, double radius, double degrees)
    {
        var rad = degrees * Math.PI / 180.0;
        return new Point(center.X + radius * Math.Sin(rad), center.Y - radius * Math.Cos(rad));
    }

    private void DrawHumanAwarenessRadar(DrawingContext dc, Brush text, Brush muted,
        Brush border, Brush accent, Brush panel)
    {
        DrawCenteredText(dc, "HUMAN AWARENESS",
            new Point(ActualWidth / 2.0, 21.0), VisualControlTypography.BoardTitle, accent, FontWeights.Bold);
        var layout = HumanAwarenessLayout(ActualWidth);
        var center = layout.Center;
        var radius = layout.Radius;
        var sight = P(HumanCombatPolicy.SightRangeId);
        var hearing = P(HumanCombatPolicy.HearingRangeId);
        var memory = P(HumanCombatPolicy.AlertMemoryId);

        // Distance rings with true metres (square-root scale). Labels sit on
        // the 6 o'clock axis behind the enemy, where nothing else is drawn.
        var ringPen = new Pen(CloneWithOpacity(border, 0.75), 0.8);
        var ringMeters = new[] { 10.0, 25.0, 50.0, 100.0, AwarenessDisplayMaxMeters };
        foreach (var meters in ringMeters)
            dc.DrawEllipse(null, ringPen, center, AwarenessRadius(meters, radius), AwarenessRadius(meters, radius));
        // HEARING: footstep circle, handle down-right.
        if (hearing is not null)
        {
            var r = AwarenessRadius(AwarenessHearingMeters * hearing.CurrentNumber, radius);
            var ink = hearing.IsControlEnabled ? text : muted;
            dc.DrawEllipse(CloneWithOpacity(ink, 0.06), new Pen(CloneWithOpacity(ink, 0.85), 1.3), center, r, r);
            var reference = AwarenessRadius(AwarenessHearingMeters * hearing.ReferenceNumber, radius);
            dc.DrawEllipse(null, new Pen(muted, 1.0) { DashStyle = DashStyles.Dash }, center, reference, reference);
            var handle = AwarenessPolar(center, r, 135.0);
            DrawAwarenessHandle(dc, hearing, handle, accent, panel);
            DrawText(dc, "HEARING", new Point(handle.X + 10.0, handle.Y + 2.0), 9.5, text, FontWeights.Bold);
            AddAwarenessTarget(hearing, handle, center, radius, AwarenessHearingMeters, HitKind.RadarRadius);
        }

        // SIGHT: vision field from 80 m ahead to 25 m at the 85° edge.
        if (sight is not null)
        {
            var factor = sight.CurrentNumber;
            var field = new StreamGeometry();
            using (var g = field.Open())
            {
                g.BeginFigure(center, true, true);
                for (var a = -AwarenessHalfAngle; a <= AwarenessHalfAngle + 0.01; a += 5.0)
                {
                    var t = Math.Abs(a) / AwarenessHalfAngle;
                    var meters = (AwarenessSightMeters * (1.0 - t) + AwarenessSideMeters * t) * factor;
                    g.LineTo(AwarenessPolar(center, AwarenessRadius(meters, radius), a), true, true);
                }
            }
            field.Freeze();
            var enabled = sight.IsControlEnabled;
            dc.DrawGeometry(CloneWithOpacity(enabled ? accent : muted, 0.16),
                new Pen(CloneWithOpacity(enabled ? accent : muted, 0.75), 1.2), field);

            var tipRadius = AwarenessRadius(AwarenessSightMeters * factor, radius);
            var tip = AwarenessPolar(center, tipRadius, 0.0);
            dc.DrawLine(new Pen(CloneWithOpacity(accent, 0.5), 1.0) { DashStyle = DashStyles.Dash }, center, tip);
            var reference = AwarenessRadius(AwarenessSightMeters * sight.ReferenceNumber, radius);
            dc.DrawLine(new Pen(muted, 1.8),
                new Point(center.X - 8.0, center.Y - reference), new Point(center.X + 8.0, center.Y - reference));
            DrawAwarenessHandle(dc, sight, tip, accent, panel);
            DrawText(dc, "SIGHT", new Point(tip.X + 11.0, tip.Y - 7.0), 9.5, text, FontWeights.Bold);
            AddAwarenessTarget(sight, tip, center, radius, AwarenessSightMeters, HitKind.RadarRadius);
        }

        foreach (var meters in ringMeters)
        {
            var label = CreateText(string.Create(CultureInfo.InvariantCulture, $"{meters:0} m"),
                9.5, muted, FontWeights.SemiBold);
            var at = new Point(center.X - label.Width / 2.0,
                center.Y + AwarenessRadius(meters, radius) - label.Height / 2.0);
            dc.DrawRectangle(panel, null, new Rect(at.X - 2.0, at.Y, label.Width + 4.0, label.Height));
            dc.DrawText(label, at);
        }

        // Enemy marker, facing up.
        var enemy = new StreamGeometry();
        using (var g = enemy.Open())
        {
            g.BeginFigure(new Point(center.X, center.Y - 9.0), true, true);
            g.LineTo(new Point(center.X + 6.5, center.Y + 6.0), true, true);
            g.LineTo(new Point(center.X - 6.5, center.Y + 6.0), true, true);
        }
        enemy.Freeze();
        dc.DrawGeometry(text, null, enemy);

        // MEMORY: search timer ring, clockwise from the top.
        if (memory is not null)
        {
            var ringRadius = radius + AwarenessMemoryGap * 0.6;
            dc.DrawEllipse(null, new Pen(CloneWithOpacity(border, 0.6), 1.2), center, ringRadius, ringRadius);
            var maximum = Math.Max(0.0001, memory.Maximum);
            var sweep = Math.Clamp(memory.CurrentNumber / maximum, 0.0, 1.0) * 359.9;
            var enabled = memory.IsControlEnabled;
            DrawAwarenessArc(dc, center, ringRadius, sweep,
                new Pen(CloneWithOpacity(enabled ? accent : muted, 0.85), 4.0));
            var referenceAngle = Math.Clamp(memory.ReferenceNumber / maximum, 0.0, 1.0) * 360.0;
            dc.DrawLine(new Pen(muted, 1.8),
                AwarenessPolar(center, ringRadius - 7.0, referenceAngle),
                AwarenessPolar(center, ringRadius + 7.0, referenceAngle));
            var handle = AwarenessPolar(center, ringRadius, sweep);
            DrawAwarenessHandle(dc, memory, handle, accent, panel);
            var caption = CreateText("MEMORY", 9.5, text, FontWeights.Bold);
            var outward = AwarenessPolar(center, ringRadius + 16.0, sweep);
            dc.DrawText(caption, new Point(
                Math.Sin(sweep * Math.PI / 180.0) >= 0 ? outward.X : outward.X - caption.Width,
                outward.Y - caption.Height / 2.0));
            AddAwarenessTarget(memory, handle, center, ringRadius, 0.0, HitKind.RadarArc);
        }

        // Legend: caption + the same player text as the panel/cards.
        var rows = new (ParameterViewModel? Parameter, string Caption)[]
        {
            (sight, "SIGHT"), (hearing, "HEARING"), (memory, "MEMORY")
        };
        for (var i = 0; i < rows.Length; i++)
        {
            if (rows[i].Parameter is not { } parameter)
                continue;
            var top = layout.Legend.Top + i * AwarenessLegendRow;
            var modified = parameter.IsValueModifiedFromReference();
            // Swatch = the shape it describes on the radar.
            var swatch = new Point(layout.Legend.Left + 6.0, top + 10.0);
            switch (i)
            {
                case 0:
                    dc.DrawRectangle(CloneWithOpacity(accent, 0.35), new Pen(accent, 1.0),
                        new Rect(swatch.X - 5.0, swatch.Y - 5.0, 10.0, 10.0));
                    break;
                case 1:
                    dc.DrawEllipse(null, new Pen(text, 1.3), swatch, 5.0, 5.0);
                    break;
                default:
                    dc.DrawLine(new Pen(accent, 4.0), new Point(swatch.X - 6.0, swatch.Y), new Point(swatch.X + 6.0, swatch.Y));
                    break;
            }
            DrawText(dc, rows[i].Caption, new Point(layout.Legend.Left + 18.0, top + 3.0), 10.5, text, FontWeights.Bold);
            var value = CardValue(parameter, parameter.CurrentNumber);
            dc.DrawText(CreateText(value, modified ? 14.0 : 13.0,
                    modified ? ReadoutInk.Numbers(accent, text) : text, FontWeights.Bold,
                    Math.Max(1.0, layout.Legend.Width - 18.0)),
                new Point(layout.Legend.Left + 18.0, top + 19.0));
        }
    }

    private void DrawAwarenessHandle(DrawingContext dc, ParameterViewModel parameter, Point position,
        Brush accent, Brush panel)
    {
        var hovered = ReferenceEquals(_hoverTarget?.Parameter, parameter);
        ControlHoverGlow.Draw(dc, position, accent, hovered && parameter.IsControlEnabled);
        dc.DrawEllipse(panel, new Pen(accent, hovered || IsSelected(parameter) ? 2.5 : 1.5), position, 6.0, 6.0);
        dc.DrawEllipse(accent, null, position, 2.5, 2.5);
    }

    private static void DrawAwarenessArc(DrawingContext dc, Point center, double radius, double sweep, Pen pen)
    {
        if (sweep <= 0.5)
            return;
        var arc = new StreamGeometry();
        using (var g = arc.Open())
        {
            g.BeginFigure(AwarenessPolar(center, radius, 0.0), false, false);
            g.ArcTo(AwarenessPolar(center, radius, sweep), new Size(radius, radius), 0.0,
                sweep > 180.0, SweepDirection.Clockwise, true, false);
        }
        arc.Freeze();
        dc.DrawGeometry(null, pen, arc);
    }

    // MapRect = the radar square; VisualMinimum carries the 1x metres
    // (radius targets) so UpdateTarget can turn pointer distance into a factor.
    private void AddAwarenessTarget(ParameterViewModel parameter, Point handle, Point center,
        double radius, double baseMeters, HitKind kind)
    {
        if (!parameter.IsControlEnabled)
            return;
        _hitTargets.Add(new HitTarget(parameter,
            new Rect(handle.X - 13.0, handle.Y - 13.0, 26.0, 26.0),
            new Rect(center.X - radius, center.Y - radius, radius * 2.0, radius * 2.0),
            kind, baseMeters, 0.0, false));
    }

    private void BeginAwarenessSightDrag(HitTarget target, Point point)
    {
        _awarenessSightDragOffsetMeters = 0.0;
        if (target.Kind != HitKind.RadarRadius
            || target.Parameter.Id != HumanCombatPolicy.SightRangeId)
            return;

        var map = target.MapRect;
        var center = new Point(map.Left + map.Width / 2.0, map.Top + map.Height / 2.0);
        var distance = (point - center).Length;
        var actualMeters = AwarenessSightMeters * target.Parameter.CurrentNumber;
        _awarenessSightDragOffsetMeters =
            actualMeters - AwarenessSightDragMeters(distance, map.Width / 2.0);
    }

    private void EndAwarenessSightDrag()
        => _awarenessSightDragOffsetMeters = 0.0;

    private void UpdateAwarenessTarget(HitTarget target, Point point)
    {
        var map = target.MapRect;
        var center = new Point(map.Left + map.Width / 2.0, map.Top + map.Height / 2.0);
        var dx = point.X - center.X;
        var dy = point.Y - center.Y;
        var parameter = target.Parameter;
        double value;
        if (target.Kind == HitKind.RadarArc)
        {
            var angle = (Math.Atan2(dx, -dy) * 180.0 / Math.PI + 360.0) % 360.0;
            value = angle / 360.0 * parameter.Maximum;
        }
        else
        {
            var distance = Math.Sqrt(dx * dx + dy * dy);
            var meters = parameter.Id == HumanCombatPolicy.SightRangeId
                ? AwarenessSightDragMeters(distance, map.Width / 2.0)
                    + _awarenessSightDragOffsetMeters
                : AwarenessMeters(distance, map.Width / 2.0);
            value = meters / Math.Max(0.0001, target.VisualMinimum);
        }
        parameter.CurrentNumber = Snap(parameter, Math.Clamp(value, parameter.Minimum, parameter.Maximum));
        InvalidateVisual();
    }
}
