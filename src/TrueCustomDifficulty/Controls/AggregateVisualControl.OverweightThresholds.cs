using System.Windows;
using System.Windows.Media;

namespace TrueCustomDifficulty.Controls;

public sealed partial class AggregateVisualControl
{
    // STALKER 2.1 native 50/60/70/80 kg effect thresholds, displayed as
    // percentages of MaxInventoryMass with a read-only live kg result.
    private void DrawOverweightThresholdBoard(
        DrawingContext dc, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel)
    {
        DrawHeader(dc, "OVERWEIGHT THRESHOLDS", "", text, muted, accent);
        var carry = Linked("stamina.maxCarryWeight")?.CurrentNumber ?? 80.0;
        DrawText(dc, $"FOLLOW MAX CARRY WEIGHT · {carry:0.#} KG",
            new Point(19.0, 38.0), 10.0, muted, FontWeights.SemiBold);

        var width = Math.Max(1.0, ActualWidth - 32.0);
        const double gap = 12.0;
        var cell = (width - gap) / 2.0;
        var ids = new[]
        {
            "stamina.overweightLightThreshold",
            "stamina.overweightMediumThreshold",
            "stamina.overweightHeavyThreshold",
            "stamina.overweightCriticalThreshold"
        };
        var captions = new[] { "LIGHT LOAD", "MEDIUM LOAD", "HEAVY LOAD", "CRITICAL LOAD" };

        for (var i = 0; i < ids.Length; i++)
        {
            var parameter = P(ids[i]);
            if (parameter is null)
                continue;
            var x = 16.0 + (i % 2) * (cell + gap);
            var y = 60.0 + (i / 2) * 149.0;
            DrawCoreResourceRuler(dc, parameter, captions[i], "%",
                new Rect(x, y, cell, 105.0),
                text, muted, border, accent, panel);
            DrawCenteredText(dc, $"{carry * parameter.CurrentNumber:0.###} KG LOAD",
                new Point(x + cell / 2.0, y + 124.0),
                11.0, muted, FontWeights.SemiBold);
        }
    }
}
