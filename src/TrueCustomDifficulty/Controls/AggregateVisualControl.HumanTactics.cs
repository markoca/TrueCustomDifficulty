using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// HUMAN TACTICS: five full-range native-rate sliders with real cooldown
/// readouts, plus a shot-spread reticle. One visual rail per source value.
/// All editing snaps to the existing catalog step.
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

    // Editing uses the FULL native rate range along one visible rail.
    private static double TacticsRateAt(Point point, Rect track, ParameterViewModel parameter)
    {
        var ratio = Math.Clamp((point.X - track.Left) / Math.Max(1.0, track.Width), 0.0, 1.0);
        return parameter.Minimum + ratio * (parameter.Maximum - parameter.Minimum);
    }

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
        DrawText(dc, "TACTICAL COOLDOWNS",
            new Point(laneLeft + 10.0, 46.0), 10.2, text, FontWeights.Bold);

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

    private void DrawHumanTacticsLane(DrawingContext dc,
        HumanCombatPolicy.TacticControl tactic, ParameterViewModel? parameter,
        Rect row, Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        DrawText(dc, tactic.Label,
            new Point(row.Left + 10.0, row.Top + 1.0),
            10.2, parameter?.IsControlEnabled == true ? text : muted, FontWeights.SemiBold);
        if (parameter is null)
            return;

        var (lo, hi) = HumanCombatPolicy.CooldownSeconds(
            tactic, parameter.CurrentNumber, parameter.ReferenceNumber);
        var changed = parameter.IsControlEnabled && parameter.IsValueModifiedFromReference();
        var readout = string.Create(CultureInfo.InvariantCulture, $"{lo:0.#}–{hi:0.#} s");
        DrawRightAlignedText(dc, readout,
            new Point(row.Right - 10.0, row.Top - 1.0),
            changed ? 13.0 : 11.8,
            changed ? ReadoutInk.Numbers(accent, text) : parameter.IsControlEnabled ? text : muted,
            FontWeights.Bold);

        // One full-range rate slider, with cooldown shown above as text.
        var ink = parameter.IsControlEnabled ? accent : muted;
        var track = new Rect(row.Left + 9.0, row.Top + 28.0,
            Math.Max(1.0, row.Width - 18.0), 8.0);
        var y = track.Top + track.Height / 2.0;
        var currentX = ValueToX(parameter.CurrentNumber,
            parameter.Minimum, parameter.Maximum, track, reverse: false);
        var referenceX = ValueToX(parameter.ReferenceNumber,
            parameter.Minimum, parameter.Maximum, track, reverse: false);
        dc.DrawLine(new Pen(CloneWithOpacity(border, 0.85), 1.3),
            new Point(track.Left, y), new Point(track.Right, y));
        dc.DrawLine(new Pen(CloneWithOpacity(ink, 0.5), 2.0),
            new Point(track.Left, y), new Point(currentX, y));
        dc.DrawLine(new Pen(muted, 1.8),
            new Point(referenceX, y - 5.0), new Point(referenceX, y + 5.0));
        var handle = new Point(currentX, y);
        var hovered = _hoverTarget?.Kind == HitKind.TacticalCooldown
            && ReferenceEquals(_hoverTarget.Parameter, parameter);
        ControlHoverGlow.Draw(dc, handle, accent, hovered && parameter.IsControlEnabled);
        dc.DrawEllipse(panel, new Pen(ink, hovered || IsSelected(parameter) ? 2.4 : 1.5),
            handle, 6.0, 6.0);
        dc.DrawEllipse(ink, null, handle, 2.3, 2.3);
        if (parameter.IsControlEnabled)
            _hitTargets.Add(new HitTarget(parameter,
                new Rect(track.Left - 6.0, track.Top - 7.0, track.Width + 12.0, 23.0),
                track, HitKind.TacticalCooldown, parameter.Minimum, parameter.Maximum, false));
    }

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
        dc.DrawEllipse(null, new Pen(CloneWithOpacity(border, 0.75), 0.9),
            center, 63.0, 63.0);
        dc.DrawLine(new Pen(CloneWithOpacity(muted, 0.45), 1.0),
            new Point(center.X - 68.0, center.Y), new Point(center.X + 68.0, center.Y));
        dc.DrawLine(new Pen(CloneWithOpacity(muted, 0.45), 1.0),
            new Point(center.X, center.Y - 68.0), new Point(center.X, center.Y + 68.0));
        dc.DrawEllipse(null,
            new Pen(CloneWithOpacity(muted, 0.8), 1.2) { DashStyle = DashStyles.Dash },
            center, referenceRadius, referenceRadius);
        dc.DrawEllipse(CloneWithOpacity(ink, 0.07),
            new Pen(CloneWithOpacity(ink, 0.88), 1.6),
            center, radius, radius);
        dc.DrawEllipse(text, null, center, 2.2, 2.2);

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
            changed ? 13.5 : 12.2,
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
