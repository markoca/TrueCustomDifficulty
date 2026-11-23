using System.Windows;
using System.Windows.Media;

namespace TrueCustomDifficulty.Controls;

public sealed partial class AggregateVisualControl
{
    // Four existing CFG ratios on one Bleeding-style interactive stage graph.
    // The line shows when the native load penalties start; it does not
    // interpolate sprint speed or change the original effect strengths.
    private void DrawOverweightThresholdBoard(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawHeader(dc, "OVERWEIGHT THRESHOLDS", "", text, muted, accent);
        var carry = Linked("stamina.maxCarryWeight")?.CurrentNumber ?? 80.0;
        DrawText(dc, $"LOAD STAGES · MAX CARRY {carry:0.#} kg",
            new Point(19.0, 39.0), 10.0, muted, FontWeights.SemiBold);

        var ids = new[]
        {
            "stamina.overweightLightThreshold",
            "stamina.overweightMediumThreshold",
            "stamina.overweightHeavyThreshold",
            "stamina.overweightCriticalThreshold"
        };
        var captions = new[] { "LIGHT", "MEDIUM", "HEAVY", "CRITICAL" };
        // Keep the graph visible at the original 365-DIP board height.
        // One shared percentage axis makes all four thresholds comparable.
        var plot = new Rect(110.0, 88.0,
            Math.Max(100.0, ActualWidth - 240.0), 208.0);
        const double scaleMin = 0.0;
        const double scaleMax = 1.25;
        var rows = new[] { 104.0, 166.0, 228.0, 290.0 };

        dc.DrawLine(new Pen(CloneWithOpacity(border, 0.8), 1.0),
            new Point(plot.Left, plot.Top - 13.0),
            new Point(plot.Left, plot.Bottom + 13.0));
        for (var tick = 0; tick <= 125; tick += 25)
        {
            var x = plot.Left + plot.Width * tick / 125.0;
            var dash = new Pen(CloneWithOpacity(muted, tick == 100 ? 0.5 : 0.23), 1.0)
                { DashStyle = DashStyles.Dash };
            dc.DrawLine(dash, new Point(x, plot.Top - 12.0),
                new Point(x, plot.Bottom + 13.0));
            DrawCenteredText(dc, $"{tick}%",
                new Point(x, plot.Bottom + 24.0),
                9.0, muted, FontWeights.Normal);
        }

        var currentPoints = new Point[4];
        var referencePoints = new Point[4];
        for (var i = 0; i < ids.Length; i++)
        {
            var parameter = P(ids[i]);
            if (parameter is null)
                return;
            var y = rows[i];
            var currentX = ValueToX(parameter.CurrentNumber, scaleMin,
                scaleMax, plot);
            var referenceX = ValueToX(parameter.ReferenceNumber, scaleMin,
                scaleMax, plot);
            currentPoints[i] = new Point(currentX, y);
            referencePoints[i] = new Point(referenceX, y);

            dc.DrawLine(new Pen(CloneWithOpacity(border, 0.75), 2.0),
                new Point(plot.Left, y), new Point(plot.Right, y));
            DrawText(dc, captions[i], new Point(17.0, y - 9.0),
                11.0, text, FontWeights.Bold);

            var currentLabel = $"{parameter.CurrentNumber * 100.0:0.#}%";
            var massLabel = $"{carry * parameter.CurrentNumber:0.###} kg";
            var numericInk = parameter.IsValueModifiedFromReference()
                ? ReadoutInk.Numbers(accent, text) : text;
            dc.DrawText(CreateText(currentLabel, 12.0, numericInk,
                    FontWeights.Bold, 99.0),
                new Point(plot.Right + 12.0, y - 15.0));
            DrawText(dc, massLabel, new Point(plot.Right + 12.0, y + 3.0),
                9.5, muted, FontWeights.SemiBold);

            // Each lane is independently clickable/draggable; the existing
            // coordinator enforces 2.5-point stepping and ordered stages.
            AddHorizontalTarget(parameter,
                new Rect(plot.Left - 3.0, y - 21.0,
                    plot.Width + 6.0, 42.0),
                new Rect(plot.Left, y - 1.0, plot.Width, 2.0),
                scaleMin, scaleMax);
        }

        var referencePen = new Pen(CloneWithOpacity(muted, 0.65), 1.6)
            { DashStyle = DashStyles.Dash };
        var livePen = new Pen(accent, 2.7);
        for (var i = 1; i < 4; i++)
        {
            dc.DrawLine(referencePen, referencePoints[i - 1], referencePoints[i]);
            dc.DrawLine(livePen, currentPoints[i - 1], currentPoints[i]);
        }

        for (var i = 0; i < 4; i++)
        {
            var parameter = P(ids[i])!;
            var hovered = ReferenceEquals(_hoverTarget?.Parameter, parameter);
            var selected = IsSelected(parameter);
            dc.DrawEllipse(panel, new Pen(muted, 1.3),
                referencePoints[i], 4.2, 4.2);
            dc.DrawEllipse(panel,
                new Pen(hovered || selected ? Brushes.White : accent,
                    hovered || selected ? 3.0 : 2.2),
                currentPoints[i], hovered || selected ? 8.0 : 6.5,
                hovered || selected ? 8.0 : 6.5);
            dc.DrawEllipse(accent, null, currentPoints[i], 2.4, 2.4);
        }

        DrawText(dc, "DRAG A STAGE · RIGHT-CLICK RESET · R RESET ALL",
            new Point(19.0, 340.0), 9.0, muted, FontWeights.Normal);
    }
}
