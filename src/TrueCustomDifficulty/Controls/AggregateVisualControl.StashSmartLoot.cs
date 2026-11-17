using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TrueCustomDifficulty.Services;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// STASH SMART LOOT (Stashes): ON/OFF switch plus two knobs for the stash SmartLoot spawn
/// endpoints — WHEN STOCKED (MinSpawnChance) and WHEN SHORT (MaxSpawnChance) —
/// and the chance a set appears, per set kind, from the writer's own
/// effective endpoints (StashBranchPreview.EffectiveSpawn).
/// </summary>
public sealed partial class AggregateVisualControl
{
    private const double StashSmartToggleTop = 40.0;
    private const double StashSmartKnobTop = 76.0;
    private const double StashSmartKnobHeight = 181.0;
    private const double StashSmartGap = 12.0;
    private const double StashSmartRowPitch = 24.0;
    private static readonly (string Label, string Prototype, string? Group)[] StashSmartRows =
    {
        ("MEDS", StashSmartMedicinePrototype, null),
        ("AMMO", StashSmartAmmoPrototype, "PrimaryWeaponParams")
    };

    private static double StashSmartLootHeight
        => StashSmartKnobTop + StashSmartKnobHeight + StashSmartGap
            + 30.0 + StashSmartRows.Length * StashSmartRowPitch + 10.0 + 14.0;

    /// <summary>Average effective (low, high) spawn chance of one set kind over its groups.</summary>
    private static (double Low, double High) StashSmartSetChance(
        StashEffectiveState state, string prototype, string? group)
    {
        var spans = StashSourceGroups.Value
            .Where(g => g.IsSmart && g.Prototype == prototype
                && (group is null || g.Category == group))
            .Select(g => g.EffectiveSpawn(state))
            .Select(s => (
                Low: s.Min.Length == 0 ? 0.0 : StashEffectiveState.Number(s.Min),
                High: s.Max.Length == 0 ? 1.0 : StashEffectiveState.Number(s.Max)))
            .ToArray();
        return spans.Length == 0 ? (0.0, 0.0)
            : (spans.Average(s => s.Low), spans.Average(s => s.High));
    }

    private void DrawStashSmartLoot(DrawingContext dc, Brush text, Brush muted,
        Brush border, Brush accent, Brush panel)
    {
        DrawCenteredText(dc, "STASH SMART LOOT", new Point(ActualWidth / 2.0, 21.0),
            VisualControlTypography.BoardTitle, accent, FontWeights.Bold);
        var bankWidth = Math.Max(1.0, Math.Min(620.0, ActualWidth - 36.0));
        var left = (ActualWidth - bankWidth) / 2.0;
        var cellWidth = (bankWidth - StashSmartGap) / 2.0;

        // Master switch: OFF zeroes every stash SmartLoot set; the knobs
        // below dim (requiresToggle) but keep their values.
        if (P(StashEffectiveState.SmartEnabledId) is { } enabled)
            DrawWorldCheckRow(dc, enabled, "Smart loot in stashes",
                new Rect(left - 11.0, StashSmartToggleTop, bankWidth + 11.0, WorldListRow),
                StashSmartToggleTop, text, muted, border, accent, panel);

        // Typical set endpoints (most stash sets roll 10% → 80%).
        var knobs = new (string Id, string Label, double Typical)[]
        {
            (StashEffectiveState.SmartStockedId, "WHEN STOCKED", 0.1),
            (StashEffectiveState.SmartShortId, "WHEN SHORT", 0.8)
        };
        for (var i = 0; i < knobs.Length; i++)
        {
            var parameter = P(knobs[i].Id);
            var card = new Rect(left + i * (cellWidth + StashSmartGap), StashSmartKnobTop,
                cellWidth, StashSmartKnobHeight);
            dc.DrawTrackedRoundedRectangle(panel, new Pen(border, 0.9), card, 4.0, 4.0, parameter);
            DrawSourceKnob(dc, parameter, knobs[i].Label, card, text, muted, border, accent, panel,
                secondaryReadout: parameter is null ? null : string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"{Math.Clamp(knobs[i].Typical * parameter.CurrentNumber, 0.0, 1.0) * 100.0:0}% A SET"),
                secondaryModified: parameter?.IsValueModifiedFromReference() == true);
        }

        var table = new Rect(left, StashSmartKnobTop + StashSmartKnobHeight + StashSmartGap,
            bankWidth, 30.0 + StashSmartRows.Length * StashSmartRowPitch + 10.0);
        dc.DrawTrackedRoundedRectangle(panel, new Pen(border, 0.9), table, 4.0, 4.0);
        DrawText(dc, "CHANCE A SET APPEARS", new Point(table.Left + 12.0, table.Top + 8.0),
            10.0, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "STOCKED – SHORT",
            new Point(table.Right - 12.0, table.Top + 9.0), 9.0, muted, FontWeights.SemiBold);

        var current = CurrentStashEffectiveState(StashDifficultySid());
        var reference = ReferenceStashEffectiveState();
        const double labelWidth = 92.0;
        var barWidth = Math.Max(1.0, table.Width - 24.0 - labelWidth);
        for (var i = 0; i < StashSmartRows.Length; i++)
        {
            var row = StashSmartRows[i];
            var y = table.Top + 30.0 + i * StashSmartRowPitch;
            var tone = StashMixPalette[row.Label == "AMMO" ? 0 : 1];
            dc.DrawRectangle(tone, null, new Rect(table.Left + 12.0, y + 3.0, 3.0, 11.0));
            DrawText(dc, row.Label, new Point(table.Left + 20.0, y), 10.0, text, FontWeights.SemiBold);
            var now = StashSmartSetChance(current, row.Prototype, row.Group);
            var was = StashSmartSetChance(reference, row.Prototype, row.Group);
            DrawStashOddsRangeBar(dc, table.Left + 12.0 + labelWidth, y, barWidth,
                now.Low, now.High, was.Low, was.High, tone, text, muted, border, accent);
        }
    }
}
