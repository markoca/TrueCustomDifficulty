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
/// the top. Distances use a square-root scale so 3 m and 240 m both stay
/// readable; labelled rings give the true metres. Drag the cone tip, the
/// circle handle or the ring end; values go straight to the three catalog
/// sliders (same writer, presets and ranges).
/// </summary>
public sealed partial class AggregateVisualControl
{
    private const double AwarenessMaxMeters = 240.0;      // SIGHT at 3x
    private const double AwarenessSightMeters = 80.0;     // CentralVisionDistance
    private const double AwarenessSideMeters = 25.0;      // PeripheralVisionDistance
    private const double AwarenessHearingMeters = 12.0;   // footsteps
    private const double AwarenessMemorySeconds = 30.0;   // alert freeze time
    private const double AwarenessHalfAngle = 85.0;       // HorizontalVisionHalvedAngleDegrees
    private const double AwarenessMemoryGap = 18.0;       // ring outside the radar
    private const double AwarenessLegendRow = 46.0;

    private readonly record struct AwarenessLayout(Point Center, double Radius, Rect Legend, bool Wide, double Height);

    private static AwarenessLayout HumanAwarenessLayout(double width)
    {
        var wide = width >= 600.0;
        var radius = wide
            ? Math.Clamp((width - 340.0) / 2.0, 90.0, 150.0)
            : Math.Clamp((width - 80.0) / 2.0, 80.0, 140.0);
        var centerX = wide ? 24.0 + AwarenessMemoryGap + radius + 12.0 : width / 2.0;
        var center = new Point(centerX, 48.0 + AwarenessMemoryGap + radius + 8.0);
        var radarBottom = center.Y + radius + AwarenessMemoryGap + 14.0;
        Rect legend;
        double height;
        if (wide)
        {
            var left = center.X + radius + AwarenessMemoryGap + 30.0;
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
        => radius * Math.Sqrt(Math.Clamp(meters, 0.0, AwarenessMaxMeters) / AwarenessMaxMeters);

    private static double AwarenessMeters(double pixels, double radius)
    {
        var ratio = Math.Clamp(pixels / Math.Max(1.0, radius), 0.0, 1.0);
        return AwarenessMaxMeters * ratio * ratio;
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

        // Distance rings with true metres (square-root scale).
        var ringPen = new Pen(CloneWithOpacity(border, 0.9), 0.8);
        foreach (var meters in new[] { 10.0, 25.0, 50.0, 100.0, 200.0 })
        {
            var r = AwarenessRadius(meters, radius);
            dc.DrawEllipse(null, ringPen, center, r, r);
            DrawText(dc, string.Create(CultureInfo.InvariantCulture, $"{meters:0} m"),
                new Point(center.X - r + 3.0, center.Y + 2.0), 9.0, muted, FontWeights.Normal);
        }
        dc.DrawEllipse(null, new Pen(border, 1.0), center, radius, radius);

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
            AddAwarenessTarget(sight, tip, center, radius, AwarenessSightMeters, HitKind.RadarRadius);
        }

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
            AddAwarenessTarget(hearing, handle, center, radius, AwarenessHearingMeters, HitKind.RadarRadius);
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
            dc.DrawEllipse(null, new Pen(CloneWithOpacity(border, 0.9), 3.0), center, ringRadius, ringRadius);
            var maximum = Math.Max(0.0001, memory.Maximum);
            var sweep = Math.Clamp(memory.CurrentNumber / maximum, 0.0, 1.0) * 359.9;
            var enabled = memory.IsControlEnabled;
            DrawAwarenessArc(dc, center, ringRadius, sweep,
                new Pen(CloneWithOpacity(enabled ? accent : muted, 0.8), 3.0));
            var referenceAngle = Math.Clamp(memory.ReferenceNumber / maximum, 0.0, 1.0) * 360.0;
            dc.DrawLine(new Pen(muted, 1.8),
                AwarenessPolar(center, ringRadius - 7.0, referenceAngle),
                AwarenessPolar(center, ringRadius + 7.0, referenceAngle));
            var handle = AwarenessPolar(center, ringRadius, sweep);
            DrawAwarenessHandle(dc, memory, handle, accent, panel);
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
            DrawText(dc, rows[i].Caption, new Point(layout.Legend.Left, top + 4.0), 10.0, text, FontWeights.Bold);
            var value = CardValue(parameter, parameter.CurrentNumber);
            dc.DrawText(CreateText(value, modified ? 13.0 : 11.8,
                    modified ? ReadoutInk.Numbers(accent, text) : text, FontWeights.Bold,
                    layout.Legend.Width),
                new Point(layout.Legend.Left, top + 19.0));
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
            var meters = AwarenessMeters(Math.Sqrt(dx * dx + dy * dy), map.Width / 2.0);
            value = meters / Math.Max(0.0001, target.VisualMinimum);
        }
        parameter.CurrentNumber = Snap(parameter, Math.Clamp(value, parameter.Minimum, parameter.Maximum));
        InvalidateVisual();
    }
}
