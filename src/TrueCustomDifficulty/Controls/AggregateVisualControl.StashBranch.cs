using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TrueCustomDifficulty.Services;

namespace TrueCustomDifficulty.Controls;

// Read-only player-facing explanation of the six global Stashes knobs.
// Neither this UI nor its source inventory edits a branch, rank, or spawn chance.
public sealed partial class AggregateVisualControl
{
    private static readonly Lazy<IReadOnlyList<StashBranchPreview>> StashSourceGroups =
        new(() => StashBranchPreviewCatalog.Load(StashPreviewBaseline.Value));

    private static readonly string[] GlobalStashCategoryNames =
    {
        "AMMO", "MEDICAL", "FOOD", "DRINKS", "GRENADES", "COUPONS"
    };

    // These descriptions use actual ItemPrototypeSID categories from the
    // audited ItemGenerator/SmartLoot source catalog, not invented loot.
    private static readonly string[] GlobalStashItemDescriptions =
    {
        "SUPPORTED AMMUNITION",
        "BANDAGES · MEDKITS · ARMY MEDKITS · ANTI-RAD",
        "BREAD · SAUSAGE · CANNED FOOD",
        "WATER · ENERGY DRINKS · BEER · MILK · VODKA",
        "RGD-5 · F1 GRENADES",
        "COUPON BUNDLES (ALL TYPES)"
    };

    private static readonly (string Sid, string Label)[] GlobalMedicalItems =
    {
        ("Bandage", "BANDAGES"),
        ("Medkit", "MEDKITS"),
        ("ArmyMedkit", "ARMY MEDKITS"),
        ("AntiRad", "ANTI-RAD")
    };

    // Ensure every player-facing kind and all six global categories actually
    // exist in the bundled native sources. Do not infer a drop rate from rows.
    private static readonly Lazy<bool> GlobalStashKindsVerified = new(() =>
    {
        var groups = StashSourceGroups.Value;
        var categories = new HashSet<int>();
        var sids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var group in groups)
        {
            foreach (var item in group.Items)
            {
                categories.Add(StashCategoryIndex(item.ItemSid));
                sids.Add(item.ItemSid);
            }
        }
        if (groups.Count != 463 || groups.Sum(x => x.Items.Count) != 1124
            || categories.Count != 6 || categories.Any(x => x < 0)
            || GlobalMedicalItems.Any(x => !sids.Contains(x.Sid))
            || !new[] { "Bread", "Sausage", "CannedFood", "Water", "Energetic",
                "Beer", "Milk", "Vodka", "GrenadeRGD5", "GrenadeF1",
                "MoneyCommon", "MoneyRare", "MoneyEpic", "MoneyLegendary" }
                .All(sids.Contains))
            throw new InvalidDataException(
                "Player-facing global stash item names drifted from source CFG.");
        return true;
    });

    // Shared exact native SID mapping used by both production writers.
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

    // Only original Weight is scaled by these six global knobs. Native Chance
    // entries remain independently possible even when a Weight knob is 0x.
    // Never describe a pure Weight adjustment as a drop probability.
    private static string GlobalStashStatus(int category, StashEffectiveState state)
    {
        var value = state.Value("loot.stashMix" +
            new[] { "Ammo", "Medical", "Food", "Drinks", "Grenades", "Coupons" }[category]);
        if (category == 1)
        {
            var factors = GlobalMedicalItems.Select(item => state.ChanceFactor(item.Sid))
                .ToArray();
            if (factors.All(f => f <= 0.0000001)) return "EXCLUDED";
            if (factors.Any(f => f <= 0.0000001)) return "PARTIAL";
            if (factors.Any(f => f > 1.0000001)) return "ADJUSTED";
        }
        if (value <= 0.0000001) return "WEIGHT OFF";
        if (value < 0.9999999) return "REDUCED";
        if (value > 1.0000001) return "INCREASED";
        return "ORIGINAL";
    }

    private static string GlobalStashExplanation(
        int category, string status, StashEffectiveState state)
    {
        if (category == 1 && status == "PARTIAL")
        {
            var excluded = GlobalMedicalItems
                .Where(item => state.ChanceFactor(item.Sid) <= 0.0000001)
                .Select(item => item.Label).ToArray();
            return "EXCLUDED: " + string.Join(" · ", excluded);
        }
        return status switch
        {
            "EXCLUDED" => "ALL MEDICAL SUBTYPES DISABLED BY MEDICAL CONTROLS",
            "WEIGHT OFF" => "WEIGHT OFF · INDEPENDENT CHANCE MAY STILL APPEAR",
            "REDUCED" => "LOWER PRIORITY IN WEIGHTED SELECTION",
            "INCREASED" => "HIGHER PRIORITY IN WEIGHTED SELECTION",
            "ADJUSTED" => "MEDICAL SOURCE MODIFIERS ACTIVE",
            _ => "ORIGINAL WEIGHT · CHANCE CONTROLS INDEPENDENT"
        };
    }

    private void DrawStashGlobalCoverage(
        DrawingContext dc, Rect outer,
        Brush text, Brush muted, Brush border, Brush panel)
    {
        // Single compact overview of what each knob controls. Native group
        // internals (ItemGen, SmartLoot, Weight, Chance) stay out of this UI.
        _ = GlobalStashKindsVerified.Value;
        var state = CurrentStashEffectiveState();
        var columns = outer.Width >= 670.0 ? 2 : 1;
        const double gap = 12.0;
        const double rowHeight = 73.0;
        var columnWidth = Math.Max(1.0,
            (outer.Width - 24.0 - gap * (columns - 1)) / columns);
        var rowsTop = outer.Top + 36.0;

        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .72),
            new Pen(CloneWithOpacity(border, .68), 1.0),
            outer, 4.0, 4.0);
        DrawText(dc, "STASH ITEM WEIGHTS",
            new Point(outer.Left + 12.0, outer.Top + 10.0),
            10.5, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "CHANCE CONTROLS ARE SEPARATE",
            new Point(outer.Right - 12.0, outer.Top + 11.0),
            8.8, muted, FontWeights.SemiBold);

        for (var i = 0; i < GlobalStashCategoryNames.Length; i++)
        {
            var x = outer.Left + 12.0 + (i % columns) * (columnWidth + gap);
            var y = rowsTop + (i / columns) * rowHeight;
            var status = GlobalStashStatus(i, state);
            var explanation = GlobalStashExplanation(i, status, state);
            var captionWidth = Math.Max(1.0, columnWidth - 125.0);
            var detailsWidth = Math.Max(1.0, columnWidth - 16.0);

            dc.DrawRectangle(StashMixPalette[i], null,
                new Rect(x, y + 4.0, 4.0, 13.0));
            dc.DrawText(CreateText(GlobalStashCategoryNames[i],
                    10.2, text, FontWeights.Bold, captionWidth),
                new Point(x + 10.0, y + 3.0));
            DrawRightAlignedText(dc, status,
                new Point(x + columnWidth, y + 4.0),
                9.5, status == "EXCLUDED" ? text : muted,
                FontWeights.SemiBold);
            dc.DrawText(CreateText(GlobalStashItemDescriptions[i],
                    9.4, text, FontWeights.Normal, detailsWidth),
                new Point(x + 10.0, y + 23.0));
            dc.DrawText(CreateText(explanation,
                    8.8, muted, FontWeights.SemiBold, detailsWidth),
                new Point(x + 10.0, y + 43.0));
            dc.DrawLine(new Pen(CloneWithOpacity(border, .4), .75),
                new Point(x, y + rowHeight - 6.0),
                new Point(x + columnWidth, y + rowHeight - 6.0));
        }

        var caveat = columns == 2
            ? "WEIGHT IS RELATIVE · CHANCE-ONLY ENTRIES REMAIN UNCHANGED"
            : "WEIGHT IS RELATIVE · CHANCE REMAINS SEPARATE";
        dc.DrawText(CreateText(caveat,
                8.5, muted, FontWeights.Normal, Math.Max(1.0, outer.Width - 24.0)),
            new Point(outer.Left + 12.0, outer.Bottom - 17.0));
    }
}
