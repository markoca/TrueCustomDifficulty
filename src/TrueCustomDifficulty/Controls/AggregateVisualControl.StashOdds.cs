using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;

namespace TrueCustomDifficulty.Controls;

// STASH LOOT MIX result view: "what a stash holds" per tier and category,
// computed from the same native Basic / Standard / Rare source branches and
// the same StashEffectiveState the CFG writer uses. Read-only.
public sealed partial class AggregateVisualControl
{
    private const double StashOddsHeight = 172.0;

    private static readonly string[] StashOddsTiers = { "Cheap", "Medium", "Expensive" };
    private static readonly string[] StashOddsTierLabels = { "BASIC", "STANDARD", "RARE" };
    private static readonly string[] StashOddsCategoryLabels =
        { "AMMO", "MEDS", "FOOD", "DRINKS", "GRENADES", "COUPONS" };

    private string StashDifficultySid()
        => NpcConsumableRankProfile.DifficultySid(SelectedReferenceDifficulty);

    // Untouched reference values: the vanilla tick on every odds bar.
    private StashEffectiveState ReferenceStashEffectiveState()
    {
        var values = _parameters.Values.Concat(_linkedSources.Values)
            .GroupBy(p => p.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().ReferenceNumber, StringComparer.Ordinal);
        return new StashEffectiveState(values, Array.Empty<string>(), StashDifficultySid());
    }

    /// <summary>
    /// Odds that ONE regular stash of <paramref name="tier"/> holds at least
    /// one item of each category, averaged over the four player ranks.
    /// A native Weight list picks exactly one entry by weight (with the
    /// appended Empty, if any); every native Chance entry rolls on its own.
    /// </summary>
    private static double[] StashCategoryOdds(StashEffectiveState state, string tier)
        => StashOddsCore(state, tier, StashCategoryIndex, StashOddsCategoryLabels.Length);

    /// <summary>Same odds, one row per item SID (Medical Composition).</summary>
    private static double[] StashItemOdds(StashEffectiveState state, string tier,
        IReadOnlyList<string> sids)
        => StashOddsCore(state, tier,
            sid => sids.ToList().FindIndex(x => x.Equals(sid, StringComparison.OrdinalIgnoreCase)),
            sids.Count);

    /// <summary>True when the tier's native source lists contain this SID at all.</summary>
    private static bool StashItemAuthored(string tier, string sid)
        => StashSourceGroups.Value.Any(g => !g.IsSmart && g.Tier == tier
            && g.Items.Any(item => item.ItemSid.Equals(sid, StringComparison.OrdinalIgnoreCase)
                && StashEffectiveState.Number(item.SourceRaw) > Epsilon));

    private static double[] StashOddsCore(StashEffectiveState state, string tier,
        Func<string, int> indexOf, int count)
    {
        var baseline = StashPreviewBaseline.Value;
        var difficulty = state.DifficultySid;
        var groups = StashSourceGroups.Value
            .Where(g => !g.IsSmart && g.Tier == tier).ToArray();
        var odds = new double[count];
        for (var rank = 0; rank < 4; rank++)
        {
            var none = Enumerable.Repeat(1.0, odds.Length).ToArray();
            foreach (var group in groups.Where(g => g.AppliesTo(rank, difficulty)))
            {
                if (group.Field == "Weight" && group.TierCategory is { } category
                    && state.WeightListEdit(baseline, tier, category) is { } edit)
                {
                    // One pick per list; Empty keeps the source total, so each
                    // kind's odds = its kept Weight / (kept Weight + Empty).
                    var total = edit.Values.Sum() + edit.EmptyValue;
                    if (total <= Epsilon) continue;
                    var kinds = category.Items
                        .Select(item => indexOf(item.Fields["ItemPrototypeSID"]))
                        .ToArray();
                    foreach (var kind in kinds.Where(k => k >= 0).Distinct())
                        none[kind] *= 1.0 - Enumerable.Range(0, kinds.Length)
                            .Where(i => kinds[i] == kind).Sum(i => edit.Values[i]) / total;
                }
                else
                {
                    foreach (var item in group.Items)
                    {
                        var kind = indexOf(item.ItemSid);
                        if (kind < 0) continue;
                        var chance = StashEffectiveState.Number(
                            group.EffectiveSelection(baseline, state, item));
                        none[kind] *= 1.0 - Math.Clamp(chance, 0.0, 1.0);
                    }
                }
            }
            for (var k = 0; k < odds.Length; k++)
                odds[k] += (1.0 - none[k]) / 4.0;
        }
        return odds;
    }

    private void DrawStashOdds(DrawingContext dc, Rect outer,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        dc.DrawTrackedRoundedRectangle(CloneWithOpacity(panel, .65),
            new Pen(CloneWithOpacity(border, .61), .9), outer, 4.0, 4.0);
        dc.DrawText(CreateText("ODDS PER STASH", 11.4, text, FontWeights.Bold),
            new Point(outer.Left + 12.0, outer.Top + 8.0));
        DrawRightAlignedText(dc,
            $"{SelectedReferenceDifficulty?.ToUpperInvariant()} · ALL RANKS",
            new Point(outer.Right - 12.0, outer.Top + 9.0),
            9.4, muted, FontWeights.SemiBold);

        var current = CurrentStashEffectiveState(StashDifficultySid());
        var reference = ReferenceStashEffectiveState();
        var now = StashOddsTiers.Select(t => StashCategoryOdds(current, t)).ToArray();
        var was = StashOddsTiers.Select(t => StashCategoryOdds(reference, t)).ToArray();

        const double labelWidth = 92.0;
        const double rowPitch = 20.0;
        var gridLeft = outer.Left + 12.0 + labelWidth;
        var columnGap = 12.0;
        var columnWidth = Math.Max(1.0,
            (outer.Right - 12.0 - gridLeft - 2.0 * columnGap) / 3.0);
        var headerY = outer.Top + 30.0;
        for (var t = 0; t < 3; t++)
            DrawText(dc, StashOddsTierLabels[t],
                new Point(gridLeft + t * (columnWidth + columnGap), headerY),
                9.4, muted, FontWeights.Bold);

        for (var k = 0; k < StashOddsCategoryLabels.Length; k++)
        {
            var y = headerY + 18.0 + k * rowPitch;
            dc.DrawRectangle(StashMixPalette[k], null,
                new Rect(outer.Left + 12.0, y + 3.0, 3.0, 11.0));
            DrawText(dc, StashOddsCategoryLabels[k],
                new Point(outer.Left + 20.0, y), 10.0, text, FontWeights.SemiBold);
            for (var t = 0; t < 3; t++)
            {
                var x = gridLeft + t * (columnWidth + columnGap);
                var value = Math.Clamp(now[t][k], 0.0, 1.0);
                var vanilla = Math.Clamp(was[t][k], 0.0, 1.0);
                var changed = Math.Abs(value - vanilla) > 0.005;
                var number = CreateText($"{value * 100.0:0}%", 10.0,
                    changed ? accent : value <= Epsilon ? muted : text, FontWeights.Bold);
                const double numberWidth = 38.0;
                var bar = new Rect(x, y + 5.0,
                    Math.Max(1.0, columnWidth - numberWidth - 6.0), 7.0);
                dc.DrawRectangle(CloneWithOpacity(border, .30), null, bar);
                if (value > Epsilon)
                    dc.DrawRectangle(CloneWithOpacity(StashMixPalette[k], .9), null,
                        new Rect(bar.Left, bar.Top, bar.Width * value, bar.Height));
                // Subtle reference tick, same grammar as other boards.
                var tick = bar.Left + bar.Width * vanilla;
                dc.DrawLine(new Pen(CloneWithOpacity(muted, .75), 1.2),
                    new Point(tick, bar.Top - 2.0), new Point(tick, bar.Bottom + 2.0));
                dc.DrawText(number, new Point(
                    bar.Right + 6.0 + numberWidth - number.WidthIncludingTrailingWhitespace,
                    y));
            }
        }
    }

    /// <summary>
    /// Real per-row chance range (e.g. "40–100%") of one category in one
    /// Standard/Rare tier for the selected difficulty, after every control.
    /// </summary>
    private string StashTierChanceRange(string tierName, int kind)
    {
        var baseline = StashPreviewBaseline.Value;
        var state = CurrentStashEffectiveState(StashDifficultySid());
        var difficulty = state.DifficultySid;
        var chances = StashSourceGroups.Value
            .Where(g => !g.IsSmart && g.Tier == tierName && g.Field == "Chance"
                && Enumerable.Range(0, 4).Any(rank => g.AppliesTo(rank, difficulty)))
            .SelectMany(g => g.Items
                .Where(item => StashCategoryIndex(item.ItemSid) == kind
                    && StashEffectiveState.Number(item.SourceRaw) > Epsilon)
                .Select(item => Math.Clamp(StashEffectiveState.Number(
                    g.EffectiveSelection(baseline, state, item)), 0.0, 1.0)))
            .ToArray();
        if (chances.Length == 0)
            return "—";
        var low = chances.Min() * 100.0;
        var high = chances.Max() * 100.0;
        return Math.Abs(high - low) < 0.5 ? $"{high:0.#}%" : $"{low:0.#}–{high:0.#}%";
    }
}
