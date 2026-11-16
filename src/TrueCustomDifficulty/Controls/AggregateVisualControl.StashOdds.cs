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
    private static readonly string[] StashOddsTierLabels = { "BASIC", "STANDARD", "RARE", "SMART" };

    // SMART column = the typical world smart stash: 812 of the 831 regular
    // SmartLoot actors carry exactly StashMedicine_Smart + an ammo SmartLoot
    // prototype (766 CommonRare, 46 Cheap); CommonRare is the representative.
    private static readonly string[] StashSmartTypicalPrototypes =
        { "StashMedicine_Smart", "Stash_Ammo_Smart_CommonRare" };

    /// <summary>
    /// SMART column odds as a range: a SmartLoot group spawns with a chance
    /// between its MinSpawnChance (no need) and MaxSpawnChance (high need),
    /// then rolls ItemSetCount sets; a Weight set picks one entry, a Chance
    /// set rolls each entry. Averaged over the four player ranks.
    /// </summary>
    private static (double[] Low, double[] High, double[][] RankLow, double[][] RankHigh)
        StashSmartOdds(StashEffectiveState state, Func<string, int> indexOf, int count)
    {
        var baseline = StashPreviewBaseline.Value;
        var remaining = Math.Clamp(1.0 - state.Value("loot.genericStashReduction", 0.0), 0.0, 1.0);
        var groups = StashSourceGroups.Value
            .Where(g => g.IsSmart && StashSmartTypicalPrototypes.Contains(g.Prototype)).ToArray();
        var low = new double[count];
        var high = new double[count];
        var rankLow = new double[4][];
        var rankHigh = new double[4][];
        for (var rank = 0; rank < 4; rank++)
        {
            rankLow[rank] = new double[count];
            rankHigh[rank] = new double[count];
            var noneLow = Enumerable.Repeat(1.0, count).ToArray();
            var noneHigh = Enumerable.Repeat(1.0, count).ToArray();
            foreach (var group in groups.Where(g => g.RankIndex == rank))
            {
                var (minRaw, maxRaw, setsRaw) = group.EffectiveSpawn(state);
                var minSpawn = (minRaw.Length > 0 ? StashEffectiveState.Number(minRaw) : 0.0) * remaining;
                var maxSpawn = (maxRaw.Length > 0 ? StashEffectiveState.Number(maxRaw) : 0.0) * remaining;
                var sets = setsRaw.Length > 0 ? Math.Max(0, (int)StashEffectiveState.Number(setsRaw)) : 1;
                if (sets == 0) continue;
                var values = group.Items.Select(item => (Kind: indexOf(item.ItemSid),
                    Value: Math.Max(0.0, StashEffectiveState.Number(
                        group.EffectiveSelection(baseline, state, item))))).ToArray();
                var total = values.Sum(v => v.Value);
                foreach (var kind in values.Where(v => v.Kind >= 0).Select(v => v.Kind).Distinct())
                {
                    double perSetNone;
                    if (group.Field == "Weight")
                    {
                        if (total <= Epsilon) continue;
                        perSetNone = 1.0 - values.Where(v => v.Kind == kind).Sum(v => v.Value) / total;
                    }
                    else
                        perSetNone = values.Where(v => v.Kind == kind)
                            .Aggregate(1.0, (acc, v) => acc * (1.0 - Math.Clamp(v.Value, 0.0, 1.0)));
                    var pick = 1.0 - Math.Pow(Math.Clamp(perSetNone, 0.0, 1.0), sets);
                    noneLow[kind] *= 1.0 - Math.Clamp(minSpawn, 0.0, 1.0) * pick;
                    noneHigh[kind] *= 1.0 - Math.Clamp(maxSpawn, 0.0, 1.0) * pick;
                }
            }
            for (var k = 0; k < count; k++)
            {
                rankLow[rank][k] = 1.0 - noneLow[k];
                rankHigh[rank][k] = 1.0 - noneHigh[k];
                low[k] += rankLow[rank][k] / 4.0;
                high[k] += rankHigh[rank][k] / 4.0;
            }
        }
        return (low, high, rankLow, rankHigh);
    }

    private static bool StashSmartAuthored(Func<string, int> indexOf, int kind)
        => StashSourceGroups.Value.Any(g => g.IsSmart
            && StashSmartTypicalPrototypes.Contains(g.Prototype)
            && g.Items.Any(item => indexOf(item.ItemSid) == kind
                && StashEffectiveState.Number(item.SourceRaw) > Epsilon));

    // SMART cell: solid fill to the low-need odds, light fill to high-need,
    // "low–high%" right, grey tick at the untouched high-need value.
    /// <summary>Hover text: the SMART odds per player rank (they differ a lot).</summary>
    private static string StashSmartRankHint(double[][] rankLow, double[][] rankHigh, int kind)
    {
        var ranks = new[] { "Newbie", "Experienced", "Veteran", "Master" };
        var parts = Enumerable.Range(0, 4).Select(r => rankHigh[r][kind] <= Epsilon
            ? ranks[r] + " —"
            : $"{ranks[r]} {rankLow[r][kind] * 100.0:0}–{rankHigh[r][kind] * 100.0:0}%");
        return "Typical world smart stash, by your rank (low need – high need): "
            + string.Join(" · ", parts);
    }

    private void DrawStashSmartRange(DrawingContext dc, double x, double y, double columnWidth,
        double low, double high, double vanillaHigh, Brush tone,
        Brush text, Brush muted, Brush border, Brush accent)
    {
        low = Math.Clamp(low, 0.0, 1.0);
        high = Math.Clamp(high, low, 1.0);
        var changed = Math.Abs(high - Math.Clamp(vanillaHigh, 0.0, 1.0)) > 0.005;
        var label = Math.Round(low * 100.0) == Math.Round(high * 100.0)
            ? $"{high * 100.0:0}%" : $"{low * 100.0:0}–{high * 100.0:0}%";
        var number = CreateText(label, 10.0,
            changed ? accent : high <= Epsilon ? muted : text, FontWeights.Bold);
        const double numberWidth = 52.0;
        var bar = new Rect(x, y + 5.0, Math.Max(1.0, columnWidth - numberWidth - 6.0), 7.0);
        dc.DrawRectangle(CloneWithOpacity(border, .30), null, bar);
        if (high > Epsilon)
            dc.DrawRectangle(CloneWithOpacity(tone, .35), null,
                new Rect(bar.Left, bar.Top, bar.Width * high, bar.Height));
        if (low > Epsilon)
            dc.DrawRectangle(CloneWithOpacity(tone, .9), null,
                new Rect(bar.Left, bar.Top, bar.Width * low, bar.Height));
        var tick = bar.Left + bar.Width * Math.Clamp(vanillaHigh, 0.0, 1.0);
        dc.DrawLine(new Pen(CloneWithOpacity(muted, .75), 1.2),
            new Point(tick, bar.Top - 2.0), new Point(tick, bar.Bottom + 2.0));
        dc.DrawText(number, new Point(
            bar.Right + 6.0 + numberWidth - number.WidthIncludingTrailingWhitespace, y));
    }

    /// <summary>Shared 4-column geometry of ODDS tables and the ADJUST ODDS grid.</summary>
    private static (double GridLeft, double ColumnWidth, double Gap) StashOddsColumns(
        Rect outer, double labelWidth)
    {
        const double gap = 12.0;
        var gridLeft = outer.Left + 12.0 + labelWidth;
        return (gridLeft, Math.Max(1.0, (outer.Right - 12.0 - gridLeft - 3.0 * gap) / 4.0), gap);
    }
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
        var (gridLeft, columnWidth, columnGap) = StashOddsColumns(outer, labelWidth);
        var smartNow = StashSmartOdds(current, StashCategoryIndex, StashOddsCategoryLabels.Length);
        var smartWas = StashSmartOdds(reference, StashCategoryIndex, StashOddsCategoryLabels.Length);
        var headerY = outer.Top + 30.0;
        for (var t = 0; t < 4; t++)
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
            var smartX = gridLeft + 3 * (columnWidth + columnGap);
            if (StashSmartAuthored(StashCategoryIndex, k))
            {
                DrawStashSmartRange(dc, smartX, y, columnWidth, smartNow.Low[k], smartNow.High[k],
                    smartWas.High[k], StashMixPalette[k], text, muted, border, accent);
                _stashChanceTooltipAreas.Add((new Rect(smartX, y - 2.0, columnWidth, rowPitch),
                    StashSmartRankHint(smartNow.RankLow, smartNow.RankHigh, k)));
            }
            else
                DrawText(dc, "—", new Point(smartX, y), 10.0,
                    CloneWithOpacity(muted, .7), FontWeights.SemiBold);
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
