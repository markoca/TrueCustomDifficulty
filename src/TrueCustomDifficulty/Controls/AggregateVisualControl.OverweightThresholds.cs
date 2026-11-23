using System.Windows;
using System.Windows.Media;

namespace TrueCustomDifficulty.Controls;

public sealed partial class AggregateVisualControl
{
    // Source-verified STALKER 2.1 WeightEffectParams in LIGHT → CRITICAL order.
    // These are the existing per-stage native effect magnitudes, never sliders.
    private static readonly double[] OverweightSpeedLoss = { 15.0, 25.0, 50.0, 50.0 };
    private static readonly double[] OverweightRegenLoss = { 10.0, 20.0, 35.0, 50.0 };
    private static readonly string[] OverweightThresholdIds =
    {
        "stamina.overweightLightThreshold",
        "stamina.overweightMediumThreshold",
        "stamina.overweightHeavyThreshold",
        "stamina.overweightCriticalThreshold"
    };
    private static readonly string[] OverweightStageNames =
        { "LIGHT", "MEDIUM", "HEAVY", "CRITICAL" };

    // Native MAX CARRY WEIGHT ruler is directly above the Bleeding-style
    // profile. Extra height accommodates both the ruler and four read-only
    // stage cards (2x2 on narrow hosts).
    private static double OverweightProfileHeight(double width)
        => width >= 700.0 ? 612.0 : 726.0;

    private void DrawOverweightThresholdBoard(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawHeader(dc, "OVERWEIGHT SYSTEM", "", text, muted, accent);
        // MAX CARRY WEIGHT belongs to this aggregate now, not Core Stamina.
        // The single native parameter drives the ruler, kilogram axis and
        // four derived stage-card masses.
        var carryParameter = P("stamina.maxCarryWeight");
        var carry = carryParameter?.CurrentNumber ?? 80.0;
        var carryChanged = carryParameter?.IsValueModifiedFromReference() == true;
        var parameters = OverweightThresholdIds.Select(P).ToArray();
        if (parameters.Any(p => p is null))
            return;

        if (carryParameter is not null)
            DrawMovementLimitRuler(dc, carryParameter, "MAX CARRY WEIGHT", "kg",
                new Rect(16.0, 51.0, Math.Max(1.0, ActualWidth - 32.0), 116.0),
                text, muted, border, accent, panel, vaultHeight: false);

        DrawText(dc, "LOAD PENALTY PROFILE",
            new Point(16.0, 185.0), 11.4, accent, FontWeights.Bold);

        // Explicit legend: the graph shows two different, uneditable native
        // penalty magnitudes while X handles edit only activation thresholds.
        var legendX = Math.Max(215.0, ActualWidth - 270.0);
        dc.DrawLine(new Pen(accent, 2.6),
            new Point(legendX, 193.0), new Point(legendX + 20.0, 193.0));
        DrawText(dc, "SPEED LOSS", new Point(legendX + 25.0, 185.0),
            9.1, text, FontWeights.SemiBold);
        dc.DrawLine(new Pen(Brushes.White, 2.0),
            new Point(legendX + 130.0, 193.0), new Point(legendX + 150.0, 193.0));
        DrawText(dc, "REGEN LOSS", new Point(legendX + 155.0, 185.0),
            9.1, text, FontWeights.SemiBold);

        DrawText(dc, "PENALTY %", new Point(16.0, 215.0),
            9.3, text, FontWeights.Bold);
        var plot = new Rect(66.0, 232.0,
            Math.Max(350.0, ActualWidth - 104.0), 184.0);
        dc.DrawTrackedRoundedRectangle(panel, new Pen(border, 1.0),
            plot, 4.0, 4.0);

        // The X domain follows the player's configured carrying capacity:
        // from zero to CURRENT MAX CARRY WEIGHT in kilograms. The actual
        // stored stage values stay percentages (ratio of that capacity).
        // Moving the ruler updates the kilogram ticks without converting
        // preset ratios into absolute kilograms.
        double X(double kilograms) => ValueToX(
            kilograms, 0.0, Math.Max(1.0, carry), plot, reverse: false);
        double Y(double loss) => ValueToY(loss, 0.0, 50.0, plot);
        for (var loss = 0; loss <= 50; loss += 10)
        {
            var y = Y(loss);
            dc.DrawLine(new Pen(CloneWithOpacity(border, loss == 0 ? .85 : .4), 1.0)
                    { DashStyle = loss == 0 ? DashStyles.Solid : DashStyles.Dot },
                new Point(plot.Left, y), new Point(plot.Right, y));
            DrawRightAlignedText(dc, loss.ToString(),
                new Point(plot.Left - 9.0, y - 7.0),
                9.7, muted, FontWeights.SemiBold);
        }
        for (var percent = 0; percent <= 100; percent += 25)
        {
            var kg = carry * percent / 100.0;
            var x = X(kg);
            dc.DrawLine(new Pen(CloneWithOpacity(border, percent == 100 ? .70 : .35), 1.0)
                    { DashStyle = percent is 0 or 100 ? DashStyles.Solid : DashStyles.Dot },
                new Point(x, plot.Top), new Point(x, plot.Bottom));
            DrawCenteredText(dc, $"{kg:0.##}",
                new Point(x, plot.Bottom + 11.0),
                9.5, muted, FontWeights.SemiBold);
        }
        DrawCenteredText(dc, $"CARRIED LOAD (kg) · MAX {carry:0.#} kg",
            new Point(ActualWidth / 2.0 + 18.0, plot.Bottom + 31.0),
            10.1, text, FontWeights.Bold);

        var liveX = parameters.Select(p => X(carry * p!.CurrentNumber)).ToArray();
        // Gray factory markers use current capacity as their axis reference.
        var vanillaX = parameters.Select(p => X(carry * p!.ReferenceNumber)).ToArray();

        void StepCurve(double[] xs, double[] loss, Pen pen)
        {
            var lastX = plot.Left;
            var lastY = Y(0.0);
            for (var i = 0; i < 4; i++)
            {
                var nextY = Y(loss[i]);
                dc.DrawLine(pen, new Point(lastX, lastY), new Point(xs[i], lastY));
                dc.DrawLine(pen, new Point(xs[i], lastY), new Point(xs[i], nextY));
                lastX = xs[i];
                lastY = nextY;
            }
            dc.DrawLine(pen, new Point(lastX, lastY),
                new Point(plot.Right, lastY));
        }

        // The shaded speed-loss stair profile mirrors Bleeding's readable
        // damage profile but does not add another editable source.
        var speedFill = new StreamGeometry();
        using (var context = speedFill.Open())
        {
            context.BeginFigure(new Point(plot.Left, plot.Bottom), true, true);
            var previousY = Y(0.0);
            for (var i = 0; i < 4; i++)
            {
                context.LineTo(new Point(liveX[i], previousY), true, false);
                previousY = Y(OverweightSpeedLoss[i]);
                context.LineTo(new Point(liveX[i], previousY), true, false);
            }
            context.LineTo(new Point(plot.Right, previousY), true, false);
            context.LineTo(new Point(plot.Right, plot.Bottom), true, false);
        }
        speedFill.Freeze();
        dc.DrawGeometry(CloneWithOpacity(accent, .10), null, speedFill);

        StepCurve(vanillaX, OverweightSpeedLoss,
            new Pen(CloneWithOpacity(muted, .67), 1.45) { DashStyle = DashStyles.Dash });
        StepCurve(vanillaX, OverweightRegenLoss,
            new Pen(CloneWithOpacity(muted, .37), 1.0) { DashStyle = DashStyles.Dash });
        StepCurve(liveX, OverweightSpeedLoss, new Pen(accent, 2.8));
        StepCurve(liveX, OverweightRegenLoss, new Pen(Brushes.White, 1.9));

        for (var i = 0; i < 4; i++)
        {
            var parameter = parameters[i]!;
            var speed = new Point(liveX[i], Y(OverweightSpeedLoss[i]));
            var regen = new Point(liveX[i], Y(OverweightRegenLoss[i]));
            var reference = new Point(vanillaX[i], speed.Y);
            var selected = IsSelected(parameter);
            var hover = ReferenceEquals(_hoverTarget?.Parameter, parameter);

            // Reference tick and dotted projection use the original factory X.
            dc.DrawLine(new Pen(CloneWithOpacity(muted, .8), 1.0),
                new Point(reference.X, plot.Bottom - 6),
                new Point(reference.X, plot.Bottom + 7));
            dc.DrawLine(new Pen(CloneWithOpacity(accent, .43), 1.0)
                    { DashStyle = DashStyles.Dot },
                new Point(speed.X, speed.Y), new Point(speed.X, plot.Bottom));
            dc.DrawEllipse(panel, new Pen(muted, 1.2), reference, 4.0, 4.0);
            dc.DrawEllipse(panel, new Pen(Brushes.White, 1.4), regen, 3.8, 3.8);

            // Only gold rings are editable stage-threshold handles.
            ControlHoverGlow.Draw(dc, speed, accent, hover);
            dc.DrawEllipse(panel,
                new Pen(hover || selected ? Brushes.White : accent,
                    hover || selected ? 2.8 : 2.0),
                speed, hover || selected ? 7.5 : 6.0,
                hover || selected ? 7.5 : 6.0);
            dc.DrawEllipse(accent, null, speed, 2.4, 2.4);

            if (!parameter.IsControlEnabled)
                continue;
            // Drag still writes the source-native ratio 0..1. Its mapping
            // is equivalent to 0..carry kg on this dynamic axis; keeping
            // ratios here preserves old presets, steps and stage constraints.
            // The captured target stays active while the ring moves.
            var map = new Rect(plot.Left, speed.Y - 1.0, plot.Width, 2.0);
            AddHorizontalTarget(parameter,
                new Rect(speed.X - 18.0, speed.Y - 18.0, 36.0, 36.0),
                map, 0.0, 1.0);
            // Bleeding-style independent threshold handles on the X baseline.
            dc.DrawEllipse(panel,
                new Pen(hover || selected ? Brushes.White : accent, 1.7),
                new Point(speed.X, plot.Bottom + 2.0), 5.2, 5.2);
            AddHorizontalTarget(parameter,
                new Rect(speed.X - 15.0, plot.Bottom - 9.0, 30.0, 24.0),
                new Rect(plot.Left, plot.Bottom + 1.0, plot.Width, 2.0),
                0.0, 1.0);
        }

        // Bleeding-profile style read-only stage cards. These values are
        // derived from the same four source ratios and the edited MAX CARRY
        // WEIGHT directly above the plot.
        // A card can select its parameter rationale, but does not edit.
        var narrow = ActualWidth < 700.0;
        const double gap = 8.0;
        var columns = narrow ? 2 : 4;
        var bankWidth = Math.Max(1.0, ActualWidth - 32.0);
        var cardWidth = (bankWidth - gap * (columns - 1)) / columns;
        const double cardsTop = 479.0;
        const double cardHeight = 105.0;
        for (var i = 0; i < 4; i++)
        {
            var parameter = parameters[i]!;
            var x = 16.0 + (i % columns) * (cardWidth + gap);
            var y = cardsTop + (i / columns) * (cardHeight + gap);
            var card = new Rect(x, y, cardWidth, cardHeight);
            dc.DrawTrackedRoundedRectangle(panel,
                new Pen(IsSelected(parameter) ? accent : border,
                    IsSelected(parameter) ? 1.6 : 1.0),
                card, 3.0, 3.0, parameter);

            var center = x + cardWidth / 2.0;
            var thresholdChanged = parameter.IsValueModifiedFromReference();
            DrawCenteredText(dc, OverweightStageNames[i],
                new Point(center, y + 7.0),
                10.7, text, FontWeights.Bold);
            DrawCenteredNumericChanges(dc,
                $"STARTS AT {parameter.CurrentNumber * 100.0:0.#}%",
                new Point(center, y + 31.0),
                10.5, text, accent, FontWeights.Bold, thresholdChanged);
            DrawCenteredNumericChanges(dc,
                $"{carry * parameter.CurrentNumber:0.###} kg LOAD",
                new Point(center, y + 51.0),
                11.1, text, accent, FontWeights.Bold,
                thresholdChanged || carryChanged);
            DrawCenteredText(dc,
                $"SPEED -{OverweightSpeedLoss[i]:0}% · REGEN -{OverweightRegenLoss[i]:0}%",
                new Point(center, y + 66.0),
                narrow ? 9.8 : 9.0, muted, FontWeights.SemiBold);
            DrawCenteredText(dc, i >= 2 ? "JOGGING BLOCKED" : "JOGGING ALLOWED",
                new Point(center, y + 85.0),
                9.0, i >= 2 ? text : muted, FontWeights.SemiBold);
        }

        DrawText(dc,
            "DRAG GOLD NODES / AXIS MARKERS · RIGHT-CLICK RESET · R RESET ALL",
            new Point(17.0, cardsTop + (narrow ? 2 : 1) * (cardHeight + gap) + 4.0),
            9.0, muted, FontWeights.Normal);
    }
}
