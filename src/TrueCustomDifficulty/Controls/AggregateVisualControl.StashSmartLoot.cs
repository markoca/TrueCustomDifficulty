using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TrueCustomDifficulty.Services;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// STASH SMART LOOT (Stashes): ON/OFF switch plus two knobs for the stash SmartLoot spawn
/// endpoints — EXTRA LOOT WHEN STOCKED (MinSpawnChance) and EXTRA LOOT WHEN LOW (MaxSpawnChance) —
/// and the chance a set appears, per set kind, from the writer's own
/// effective endpoints (StashBranchPreview.EffectiveSpawn).
/// </summary>
public sealed partial class AggregateVisualControl
{
    private const double StashSmartToggleTop = 14.0;
    private const double StashSmartKnobTop = 59.0;
    private const double StashSmartKnobHeight = 181.0;
    private const double StashSmartGap = 12.0;
    private const double StashSmartRowPitch = 24.0;
    private static readonly (string Label, string Prototype, string? Group)[] StashSmartRows =
    {
        ("MEDS", StashSmartMedicinePrototype, null),
        ("AMMO", StashSmartAmmoPrototype, "PrimaryWeaponParams")
    };

    // SMART LOOT GIVES: one checkbox per kind of smart loot item.
    private static readonly (string Id, string Label)[] StashSmartOddsCells =
    {
        (StashEffectiveState.SmartMedsId, "Meds"),
        (StashEffectiveState.SmartAmmoId, "Ammo"),
        (StashEffectiveState.SmartFoodDrinksId, "Food & Drinks"),
        (StashEffectiveState.SmartGrenadesId, "Grenades")
    };

    private static int StashSmartOddsColumns(double bankWidth) => bankWidth >= 520.0 ? 4 : 2;

    private static double StashSmartOddsHeight(double bankWidth)
    {
        var rows = (StashSmartOddsCells.Length + StashSmartOddsColumns(bankWidth) - 1)
            / StashSmartOddsColumns(bankWidth);
        return 30.0 + rows * WorldListRow + 4.0;
    }

    private static double StashSmartBankWidth(double width) => Math.Max(1.0, Math.Min(620.0, width - 36.0));

    private static double StashSmartLootHeight(double width)
        => StashSmartKnobTop + StashSmartKnobHeight + StashSmartGap
            + StashSmartOddsHeight(StashSmartBankWidth(width)) + StashSmartGap
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
        var bankWidth = StashSmartBankWidth(ActualWidth);
        var left = (ActualWidth - bankWidth) / 2.0;
        var cellWidth = (bankWidth - StashSmartGap) / 2.0;

        // One top-left master checkbox is the board title. OFF disables
        // SmartLoot sets while preserving the source-backed knob values.
        if (P(StashEffectiveState.SmartEnabledId) is { } enabled)
            DrawWorldCheckRow(dc, enabled, "STASH SMART LOOT",
                new Rect(12.0, StashSmartToggleTop, 190.0, WorldListRow),
                StashSmartToggleTop, text, muted, border, accent, panel);

        // Typical set endpoints (most stash sets roll 10% → 80%).
        var knobs = new (string Id, string Label, double Typical)[]
        {
            (StashEffectiveState.SmartStockedId, "EXTRA LOOT WHEN STOCKED", 0.1),
            (StashEffectiveState.SmartShortId, "EXTRA LOOT WHEN LOW", 0.8)
        };
        for (var i = 0; i < knobs.Length; i++)
        {
            var parameter = P(knobs[i].Id);
            var card = new Rect(left + i * (cellWidth + StashSmartGap), StashSmartKnobTop,
                cellWidth, StashSmartKnobHeight);
            // Bare source dials: no individual framed cards inside this board.
            DrawSourceKnob(dc, parameter, knobs[i].Label, card, text, muted, border, accent, panel,
                secondaryReadout: parameter is null ? null : string.Create(
                    System.Globalization.CultureInfo.InvariantCulture,
                    $"{Math.Clamp(knobs[i].Typical * parameter.CurrentNumber, 0.0, 1.0) * 100.0:0}% A SET"),
                secondaryModified: parameter?.IsValueModifiedFromReference() == true);
        }

        // ---- SMART LOOT GIVES: which kinds of items smart loot may give.
        var oddsCard = new Rect(left, StashSmartKnobTop + StashSmartKnobHeight + StashSmartGap,
            bankWidth, StashSmartOddsHeight(bankWidth));
        dc.DrawTrackedRoundedRectangle(panel, new Pen(border, 0.9), oddsCard, 4.0, 4.0);
        DrawText(dc, "SMART LOOT GIVES", new Point(oddsCard.Left + 12.0, oddsCard.Top + 8.0),
            10.0, text, FontWeights.Bold);
        var columns = StashSmartOddsColumns(bankWidth);
        var oddsCellWidth = (oddsCard.Width - 12.0) / columns;
        for (var k = 0; k < StashSmartOddsCells.Length; k++)
        {
            var spec = StashSmartOddsCells[k];
            if (P(spec.Id) is not { } parameter)
                continue;
            var rowTop = oddsCard.Top + 28.0 + (k / columns) * WorldListRow;
            // DrawWorldCheckRow places the box 11 DIP inside its row rect.
            DrawWorldCheckRow(dc, parameter, spec.Label,
                new Rect(oddsCard.Left + 1.0 + (k % columns) * oddsCellWidth, rowTop,
                    oddsCellWidth, WorldListRow),
                rowTop, text, muted, border, accent, panel);
        }

        var table = new Rect(left, oddsCard.Bottom + StashSmartGap,
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
