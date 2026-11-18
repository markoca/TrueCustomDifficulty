using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// HUMAN TACTICS (2026-11-17): a COMBAT MINUTE timeline — one row per tactic
/// on a 0–60 s axis, markers at every average cooldown, the first marker's
/// lo–hi window shaded, vanilla rhythm as hollow ghost markers — plus ENEMY
/// ACCURACY as a shot group over a player silhouette. Dragging a row moves
/// its first event (the average pause); all editing snaps to the existing
/// catalog step. The spread figure is relative (DispersionRadius has no
/// documented unit), never a hit-percentage simulation.
/// </summary>
public sealed partial class AggregateVisualControl
{
    private const double TacticsLaneTop = 76.0;
    private const double TacticsLanePitch = 49.0;
    private const double TacticsSpreadMinimum = 50.0;
    private const double TacticsSpreadMaximum = 400.0;

    private static bool HumanTacticsWide(double width) => width >= 600.0;
    private static double HumanTacticsHeight(double width)
        => HumanTacticsWide(width) ? 340.0 : 530.0;

    // COMBAT MINUTE axis: 0–60 s across the row.
    private const double TacticsMinuteSeconds = 60.0;

    // Average pause of one tactic at a rate (middle of the native lo–hi window).
    private static double TacticsAverageSeconds(HumanCombatPolicy.TacticControl tactic,
        double rate, double reference)
    {
        var (lo, hi) = HumanCombatPolicy.CooldownSeconds(tactic, rate, reference);
        return (lo + hi) / 2.0;
    }

    // Editing: the pointer sets the first event's time (average pause) on the
    // 0–60 s axis; rate = average pause at 1x / chosen pause, clamped to the
    // full native rate range.
    private static double TacticsRateAt(Point point, Rect track, ParameterViewModel parameter)
    {
        var tactic = HumanCombatPolicy.Tactics.First(t => t.Id == parameter.Id);
        var seconds = Math.Max(0.25, Math.Clamp((point.X - track.Left) / Math.Max(1.0, track.Width),
            0.0, 1.0) * TacticsMinuteSeconds);
        var atOne = TacticsAverageSeconds(tactic, 1.0, parameter.ReferenceNumber);
        return Math.Clamp(atOne / seconds, parameter.Minimum, parameter.Maximum);
    }

    private static double TacticsSecondsToX(double seconds, Rect track)
        => track.Left + track.Width * Math.Clamp(seconds / TacticsMinuteSeconds, 0.0, 1.0);

    private static double TacticsSpreadRadius(double factor)
    {
        var spread = Math.Clamp(100.0 / Math.Max(0.0001, factor),
            TacticsSpreadMinimum, TacticsSpreadMaximum);
        // Radius follows a UI log scale; it does not simulate bullet hits.
        return 18.0 + Math.Log(spread / TacticsSpreadMinimum)
            / Math.Log(TacticsSpreadMaximum / TacticsSpreadMinimum) * 42.0;
    }

    private void DrawHumanTacticsBoard(DrawingContext dc, Brush text, Brush muted,
        Brush border, Brush accent, Brush panel)
    {
        DrawCenteredText(dc, "HUMAN TACTICS",
            new Point(ActualWidth / 2.0, 21.0),
            VisualControlTypography.BoardTitle, accent, FontWeights.Bold);

        var wide = HumanTacticsWide(ActualWidth);
        var bankWidth = Math.Min(760.0, Math.Max(1.0, ActualWidth - 32.0));
        var bankLeft = (ActualWidth - bankWidth) / 2.0;
        const double spreadColumn = 190.0;
        const double gap = 12.0;
        var laneWidth = wide ? bankWidth - spreadColumn - gap : bankWidth;
        var laneLeft = bankLeft;
        DrawText(dc, "COMBAT MINUTE",
            new Point(laneLeft + 10.0, 46.0), 10.2, text, FontWeights.Bold);
        // Axis labels above the shared 0–60 s tracks.
        var axis = TacticsTrack(new Rect(laneLeft, TacticsLaneTop, laneWidth, 46.0));
        foreach (var (seconds, label) in new[] { (0.0, "0 s"), (30.0, "30 s"), (60.0, "60 s") })
        {
            var x = TacticsSecondsToX(seconds, axis);
            var labelText = CreateText(label, 9.0, muted, FontWeights.SemiBold);
            var lx = seconds <= 0.0 ? x : seconds >= TacticsMinuteSeconds
                ? x - labelText.WidthIncludingTrailingWhitespace
                : x - labelText.WidthIncludingTrailingWhitespace / 2.0;
            dc.DrawText(labelText, new Point(lx, 48.0));
        }

        for (var i = 0; i < HumanCombatPolicy.Tactics.Count; i++)
        {
            var tactic = HumanCombatPolicy.Tactics[i];
            DrawHumanTacticsLane(dc, tactic, P(tactic.Id),
                new Rect(laneLeft, TacticsLaneTop + i * TacticsLanePitch,
                    laneWidth, 46.0), text, muted, border, accent, panel);
        }

        var spread = P(HumanCombatPolicy.AccuracyId);
        var spreadLeft = wide ? bankLeft + laneWidth + gap
            : (ActualWidth - spreadColumn) / 2.0;
        DrawHumanTacticsSpread(dc, spread,
            new Rect(spreadLeft, wide ? 60.0 : 334.0,
                spreadColumn, wide ? 264.0 : 176.0),
            wide, text, muted, border, accent, panel);
    }

    // Track of one COMBAT MINUTE row: label column left, readout column right.
    private static Rect TacticsTrack(Rect row)
        => new(row.Left + 104.0, row.Top + 10.0, Math.Max(1.0, row.Width - 104.0 - 112.0), 22.0);

    private void DrawHumanTacticsLane(DrawingContext dc,
        HumanCombatPolicy.TacticControl tactic, ParameterViewModel? parameter,
        Rect row, Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        var track = TacticsTrack(row);
        DrawText(dc, tactic.Label,
            new Point(row.Left + 10.0, track.Top + 3.0),
            10.2, parameter?.IsControlEnabled == true ? text : muted, FontWeights.SemiBold);
        if (parameter is null)
            return;

        var (lo, hi) = HumanCombatPolicy.CooldownSeconds(
            tactic, parameter.CurrentNumber, parameter.ReferenceNumber);
        var changed = parameter.IsControlEnabled && parameter.IsValueModifiedFromReference();
        var readout = string.Create(CultureInfo.InvariantCulture, $"every {lo:0.#}–{hi:0.#} s");
        DrawRightAlignedText(dc, readout,
            new Point(row.Right - 10.0, track.Top + 2.0),
            11.4,
            changed ? ReadoutInk.Numbers(accent, text) : parameter.IsControlEnabled ? text : muted,
            FontWeights.Bold);

        var ink = parameter.IsControlEnabled ? accent : muted;
        var y = track.Top + track.Height / 2.0;
        DrawTacticsShape(dc, CloneWithOpacity(border, 0.22),
            new Pen(CloneWithOpacity(border, 0.6), 0.8), track, 4.0, 4.0);
        foreach (var seconds in new[] { 30.0 })
        {
            var gx = TacticsSecondsToX(seconds, track);
            dc.DrawLine(new Pen(CloneWithOpacity(border, 0.6), 0.8),
                new Point(gx, track.Top + 3.0), new Point(gx, track.Bottom - 3.0));
        }

        // Vanilla rhythm: hollow ghost markers at every vanilla average pause.
        var vanillaAvg = TacticsAverageSeconds(tactic, parameter.ReferenceNumber, parameter.ReferenceNumber);
        DrawTacticsMarkers(dc, track, vanillaAvg, null,
            new Pen(CloneWithOpacity(muted, 0.75), 1.1), 3.4);

        // Current rhythm: first event's lo–hi window shaded, then a marker at
        // every average pause across the minute.
        var avg = (lo + hi) / 2.0;
        var window = new Rect(TacticsSecondsToX(lo, track), track.Top + 2.0,
            Math.Max(2.0, TacticsSecondsToX(hi, track) - TacticsSecondsToX(lo, track)),
            track.Height - 4.0);
        if (lo < TacticsMinuteSeconds)
            DrawTacticsShape(dc, CloneWithOpacity(ink, 0.16), null, window, 3.0, 3.0);
        DrawTacticsMarkers(dc, track, avg, CloneWithOpacity(ink, 0.9), null, 3.0);

        // Handle = first event (average pause); beyond 60 s it waits at the edge.
        var handle = new Point(TacticsSecondsToX(avg, track), y);
        var hovered = _hoverTarget?.Kind == HitKind.TacticalCooldown
            && ReferenceEquals(_hoverTarget.Parameter, parameter);
        ControlHoverGlow.Draw(dc, handle, accent, hovered && parameter.IsControlEnabled);
        dc.DrawEllipse(panel, new Pen(ink, hovered || IsSelected(parameter) ? 2.4 : 1.6),
            handle, 6.5, 6.5);
        dc.DrawEllipse(ink, null, handle, 2.6, 2.6);
        if (avg > TacticsMinuteSeconds)
            DrawText(dc, "›", new Point(track.Right - 14.0, track.Top + 1.0), 13.0, ink, FontWeights.Bold);
        if (parameter.IsControlEnabled)
            _hitTargets.Add(new HitTarget(parameter,
                new Rect(track.Left - 6.0, track.Top - 4.0, track.Width + 12.0, track.Height + 8.0),
                track, HitKind.TacticalCooldown, parameter.Minimum, parameter.Maximum, false));
    }

    // Markers at every multiple of the pause inside the minute. Dense rhythms
    // (under 7 DIP apart) collapse to short ticks so they stay readable.
    private static void DrawTacticsMarkers(DrawingContext dc, Rect track, double pause,
        Brush? fill, Pen? outline, double radius)
    {
        if (pause <= 0.0) return;
        var y = track.Top + track.Height / 2.0;
        var spacing = track.Width * pause / TacticsMinuteSeconds;
        var dense = spacing < 7.0;
        for (var k = 1; k * pause <= TacticsMinuteSeconds + 1e-9 && k <= 240; k++)
        {
            var x = TacticsSecondsToX(k * pause, track);
            if (dense)
                dc.DrawLine(outline ?? new Pen(fill, 1.4),
                    new Point(x, y - 4.0), new Point(x, y + 4.0));
            else
                dc.DrawEllipse(fill, outline, new Point(x, y), radius, radius);
        }
    }

    // Track, window and silhouette shapes are not cards: no card hover.
    private static void DrawTacticsShape(DrawingContext dc, Brush? fill, Pen? pen,
        Rect rect, double radiusX, double radiusY)
        => dc.DrawGeometry(fill, pen, new RectangleGeometry(rect, radiusX, radiusY));

    // Fixed, deterministic shot positions inside the group (angle, share of radius).
    private static readonly (double Angle, double Distance)[] TacticsShotPattern =
    {
        (0.3, 0.35), (1.4, 0.8), (2.2, 0.55), (2.9, 0.9), (3.7, 0.25),
        (4.4, 0.7), (5.2, 0.95), (5.9, 0.6)
    };

    private void DrawHumanTacticsSpread(DrawingContext dc,
        ParameterViewModel? parameter, Rect area, bool wide,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        DrawCenteredText(dc, "ENEMY ACCURACY",
            new Point(area.Left + area.Width / 2.0, area.Top + 4.0),
            10.2, text, FontWeights.Bold);
        if (parameter is null)
            return;

        var center = new Point(area.Left + area.Width / 2.0,
            area.Top + (wide ? 133.0 : 94.0));
        var changed = parameter.IsControlEnabled && parameter.IsValueModifiedFromReference();
        var ink = parameter.IsControlEnabled ? accent : muted;
        var radius = TacticsSpreadRadius(parameter.CurrentNumber);
        var referenceRadius = TacticsSpreadRadius(parameter.ReferenceNumber);
        // You, as the target: a simple silhouette centred on the chest.
        var scale = wide ? 1.0 : 0.82;
        var body = new Pen(CloneWithOpacity(muted, 0.7), 1.1);
        dc.DrawEllipse(null, body, new Point(center.X, center.Y - 52.0 * scale),
            11.0 * scale, 11.0 * scale);
        DrawTacticsShape(dc, null, body, new Rect(center.X - 20.0 * scale,
            center.Y - 38.0 * scale, 40.0 * scale, 58.0 * scale), 8.0 * scale, 8.0 * scale);
        foreach (var dx in new[] { -11.0, 1.0 })
            DrawTacticsShape(dc, null, body, new Rect(center.X + dx * scale,
                center.Y + 21.0 * scale, 10.0 * scale, 42.0 * scale), 4.0 * scale, 4.0 * scale);

        // Vanilla group (dashed) and the current group with a fixed shot pattern.
        dc.DrawEllipse(null,
            new Pen(CloneWithOpacity(muted, 0.8), 1.2) { DashStyle = DashStyles.Dash },
            center, referenceRadius, referenceRadius);
        dc.DrawEllipse(CloneWithOpacity(ink, 0.10),
            new Pen(CloneWithOpacity(ink, 0.88), 1.6),
            center, radius, radius);
        foreach (var (angle, distance) in TacticsShotPattern)
        {
            var r = radius * distance;
            dc.DrawEllipse(CloneWithOpacity(ink, 0.9), null, new Point(
                center.X + r * Math.Cos(angle), center.Y + r * Math.Sin(angle)), 2.3, 2.3);
        }

        var handle = new Point(center.X + radius / Math.Sqrt(2.0),
            center.Y - radius / Math.Sqrt(2.0));
        var hovered = _hoverTarget?.Kind == HitKind.TacticalSpread
            && ReferenceEquals(_hoverTarget.Parameter, parameter);
        ControlHoverGlow.Draw(dc, handle, accent, hovered && parameter.IsControlEnabled);
        dc.DrawEllipse(panel, new Pen(ink, hovered || IsSelected(parameter) ? 2.4 : 1.5),
            handle, 6.0, 6.0);
        dc.DrawEllipse(ink, null, handle, 2.3, 2.3);

        var value = HumanCombatPolicy.CardText(parameter.Id,
            parameter.CurrentNumber, parameter.ReferenceNumber) ?? "shot spread";
        DrawCenteredText(dc, value,
            new Point(center.X, center.Y + 78.0),
            12.2,
            changed ? ReadoutInk.Numbers(accent, text) : parameter.IsControlEnabled ? text : muted,
            FontWeights.Bold);
        if (parameter.IsControlEnabled)
            _hitTargets.Add(new HitTarget(parameter,
                new Rect(center.X - 63.0, center.Y - 63.0, 126.0, 126.0),
                new Rect(center.X - 63.0, center.Y - 63.0, 126.0, 126.0),
                HitKind.TacticalSpread, 0.0, 0.0, false));
    }

    private void UpdateHumanTacticsTarget(HitTarget target, Point point)
    {
        var parameter = target.Parameter;
        if (target.Kind == HitKind.TacticalCooldown)
        {
            var rate = TacticsRateAt(point, target.MapRect, parameter);
            parameter.CurrentNumber = Snap(parameter,
                Math.Clamp(rate, parameter.Minimum, parameter.Maximum));
        }
        else if (target.Kind == HitKind.TacticalSpread)
        {
            var center = new Point(target.MapRect.Left + target.MapRect.Width / 2.0,
                target.MapRect.Top + target.MapRect.Height / 2.0);
            var dx = point.X - center.X;
            var dy = point.Y - center.Y;
            var radius = Math.Sqrt(dx * dx + dy * dy);
            var ratio = Math.Clamp((radius - 18.0) / 42.0, 0.0, 1.0);
            var spread = TacticsSpreadMinimum * Math.Exp(ratio
                * Math.Log(TacticsSpreadMaximum / TacticsSpreadMinimum));
            parameter.CurrentNumber = Snap(parameter,
                Math.Clamp(100.0 / spread, parameter.Minimum, parameter.Maximum));
        }
        InvalidateVisual();
    }
}
