using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TrueCustomDifficulty.Services;

namespace TrueCustomDifficulty.Controls;

// Read-only native source coverage for six GLOBAL categories.
// No per-rank, branch, prototype or difficulty editing/navigation.
public sealed partial class AggregateVisualControl
{
    private static readonly Lazy<IReadOnlyList<StashBranchPreview>> StashSourceGroups =
        new(() => StashBranchPreviewCatalog.Load(StashPreviewBaseline.Value));

    private static readonly string[] GlobalStashCategoryNames =
    {
        "AMMO", "MEDICAL", "FOOD", "DRINKS", "GRENADES", "COUPONS"
    };

    private readonly record struct GlobalStashSourceCount(
        int ItemGeneratorWeight, int ItemGeneratorChance,
        int SmartLootWeight, int SmartLootChance)
    {
        public int ItemGenerator => ItemGeneratorWeight + ItemGeneratorChance;
        public int SmartLoot => SmartLootWeight + SmartLootChance;
        public int Weight => ItemGeneratorWeight + SmartLootWeight;
        public int Chance => ItemGeneratorChance + SmartLootChance;
        public int Total => Weight + Chance;
    }

    // Counts are original authored source rows across all ranks, not spawn
    // probabilities or the number of physically reachable stashes.
    private static readonly Lazy<GlobalStashSourceCount[]> GlobalStashSourceCounts =
        new(() =>
        {
            var counts = new int[6, 4];
            foreach (var group in StashSourceGroups.Value)
            {
                var column = (group.IsSmart ? 2 : 0) +
                    (group.Field == "Chance" ? 1 : 0);
                foreach (var item in group.Items)
                {
                    var category = StashCategoryIndex(item.ItemSid);
                    if (category < 0)
                        throw new InvalidDataException(
                            "Unmapped global Stash Content Mix source: " + item.ItemSid);
                    counts[category, column]++;
                }
            }
            var result = Enumerable.Range(0, 6).Select(i =>
                new GlobalStashSourceCount(
                    counts[i, 0], counts[i, 1],
                    counts[i, 2], counts[i, 3])).ToArray();
            if (result.Any(c => c.Total == 0)
                || result.Sum(c => c.Total) != 1124)
                throw new InvalidDataException(
                    "Stash global category source inventory drifted.");
            return result;
        });

    // Shared exact SID classifier for ItemGenerator, SmartLoot and previews.
    private static int StashCategoryIndex(string sid)
        => (StashEffectiveState.ItemBiasId(sid)
            ?? StashEffectiveState.SmartItemBiasId(sid)) switch
        {
            "loot.stashMixAmmo" => 0,
            "loot.stashMixMedical" => 1,
            "loot.stashMixFood" => 2,
            "loot.stashMixDrinks" => 3,
            "loot.stashMixGrenades" => 4,
            "loot.stashMixCoupons" => 5,
            _ => -1
        };

    private void DrawStashGlobalCoverage(
        DrawingContext dc, Rect outer,
        Brush text, Brush muted, Brush border, Brush panel)
    {
        var columns = outer.Width >= 670.0 ? 2 : 1;
        var gap = 12.0;
        var columnWidth = Math.Max(1.0,
            (outer.Width - 24.0 - gap * (columns - 1)) / columns);
        var rowHeight = columns == 2 ? 62.0 : 55.0;
        var rowsTop = outer.Top + 38.0;

        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .72),
            new Pen(CloneWithOpacity(border, .68), 1.0),
            outer, 4.0, 4.0);
        DrawText(dc, "GLOBAL SOURCE COVERAGE",
            new Point(outer.Left + 12.0, outer.Top + 11.0),
            10.5, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "ITEMGEN + SMARTLOOT",
            new Point(outer.Right - 12.0, outer.Top + 12.0),
            8.8, muted, FontWeights.SemiBold);

        var counts = GlobalStashSourceCounts.Value;
        for (var i = 0; i < counts.Length; i++)
        {
            var column = i % columns;
            var row = i / columns;
            var x = outer.Left + 12.0 + column * (columnWidth + gap);
            var y = rowsTop + row * rowHeight;
            var count = counts[i];

            dc.DrawRectangle(StashMixPalette[i], null,
                new Rect(x, y + 5.0, 4.0, 12.0));
            DrawText(dc, GlobalStashCategoryNames[i],
                new Point(x + 10.0, y + 3.0),
                10.0, text, FontWeights.Bold);
            DrawRightAlignedText(dc, $"{count.Total} ENTRIES",
                new Point(x + columnWidth, y + 4.0),
                9.0, muted, FontWeights.SemiBold);
            DrawText(dc, $"WEIGHT {count.Weight}   CHANCE {count.Chance}",
                new Point(x + 10.0, y + 20.0),
                9.3, muted, FontWeights.SemiBold);
            DrawText(dc, $"ITEMGEN {count.ItemGenerator}   WORLD {count.SmartLoot}",
                new Point(x + 10.0, y + 37.0),
                9.0, muted, FontWeights.Normal);
            dc.DrawLine(new Pen(CloneWithOpacity(border, .4), .75),
                new Point(x, y + rowHeight - 7.0),
                new Point(x + columnWidth, y + rowHeight - 7.0));
        }

        DrawText(dc, "NATIVE SOURCE ENTRIES · NOT DROP PROBABILITIES",
            new Point(outer.Left + 12.0, outer.Bottom - 17.0),
            8.6, muted, FontWeights.Normal);
    }
}
