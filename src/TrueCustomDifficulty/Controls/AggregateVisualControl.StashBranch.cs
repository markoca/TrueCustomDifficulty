using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TrueCustomDifficulty.Services;

namespace TrueCustomDifficulty.Controls;

// The global stash source catalog and the compact source-weight mixer.
// Native Chance belongs to separate real CFG controls, not this scale.
public sealed partial class AggregateVisualControl
{
    // Read-only backing catalog for the six editable Weight lanes. These
    // descriptions match the audited native item SIDs; they are not drop odds.
    private static readonly Lazy<IReadOnlyList<StashBranchPreview>> StashSourceGroups =
        new(() => StashBranchPreviewCatalog.Load(StashPreviewBaseline.Value));

    private static readonly string[] GlobalStashParameterIds =
    {
        "loot.stashMixAmmo", "loot.stashMixMedical", "loot.stashMixFood",
        "loot.stashMixDrinks", "loot.stashMixGrenades", "loot.stashMixCoupons"
    };

    private static readonly string[] GlobalStashCategoryNames =
    {
        "AMMO", "MEDICAL", "FOOD", "DRINKS", "GRENADES", "COUPONS"
    };

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

    // Native Chance-only rows remain independent, including at 0x Weight.
    private static string GlobalStashStatus(int category, StashEffectiveState state)
    {
        var value = state.Value(GlobalStashParameterIds[category]);
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

    private static string GlobalStashDetail(int category, string status,
        StashEffectiveState state)
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
            "EXCLUDED" => "MEDICAL SUBTYPES DISABLED",
            "WEIGHT OFF" => "WEIGHT OFF · INDEPENDENT CHANCE MAY STILL ROLL",
            "ADJUSTED" => "MEDICAL SOURCE SETTINGS MODIFIED",
            _ => GlobalStashItemDescriptions[category]
        };
    }


    // Only two independent native Chance controls: Basic RGD-5 and Rare coupons.
    // Neither belongs to the six global source Weight multipliers.
    private void DrawStashNativeChanceControls(
        DrawingContext dc, Rect outer, Brush text, Brush muted,
        Brush border, Brush accent, Brush panel)
    {
        var columns = outer.Width >= 670.0 ? 2 : 1;
        const double inset = 14.0;
        const double gap = 16.0;
        var cellWidth = Math.Max(1.0,
            (outer.Width - 2.0 * inset - (columns - 1) * gap) / columns);
        var entries = new (string Id, string Label, int Category)[]
        {
            ("loot.cheapStashGrenadeChance", "BASIC RGD-5 CHANCE", 4),
            ("loot.expensiveStashMoneyChance", "RARE COUPON CHANCE", 5)
        };

        for (var i = 0; i < entries.Length; i++)
        {
            var p = P(entries[i].Id);
            if (p is null) continue;
            var enabled = p.IsControlEnabled;
            var tone = StashMixPalette[entries[i].Category];
            var x = outer.Left + inset + (i % columns) * (cellWidth + gap);
            var y = outer.Top + (i / columns) * 61.0;
            var card = new Rect(x, y, cellWidth, 56.0);
            dc.DrawRoundedRectangle(CloneWithOpacity(panel, .54),
                new Pen(CloneWithOpacity(border, .52), .8), card, 3.0, 3.0);
            dc.DrawRectangle(tone, null, new Rect(x + 6.0, y + 7.0, 3.0, 13.0));
            dc.DrawText(CreateText(entries[i].Label, 10.0,
                    enabled ? text : muted, FontWeights.SemiBold,
                    Math.Max(1.0, cellWidth - 102.0)),
                new Point(x + 14.0, y + 5.0));

            var digits = (p.CurrentNumber * 100.0).ToString("0.#",
                System.Globalization.CultureInfo.InvariantCulture);
            var modified = enabled && p.IsValueModifiedFromReference();
            var number = CreateText(digits + "%", modified ? 12.6 : 11.8,
                enabled ? text : muted, FontWeights.Bold);
            if (modified) number.SetForegroundBrush(accent, 0, digits.Length);
            dc.DrawText(number,
                new Point(card.Right - number.WidthIncludingTrailingWhitespace - 11.0,
                    y + 3.0));

            var rail = new Rect(x + 13.0, y + 26.0,
                Math.Max(1.0, cellWidth - 26.0), 8.0);
            var mid = rail.Top + rail.Height / 2.0;
            var currentX = ValueToX(p.CurrentNumber,
                p.Minimum, p.Maximum, rail, false);
            var originalX = ValueToX(p.ReferenceNumber,
                p.Minimum, p.Maximum, rail, false);
            var hovered = enabled && _hoverTarget?.Kind == HitKind.Horizontal
                && ReferenceEquals(_hoverTarget.Parameter, p);
            dc.DrawLine(new Pen(CloneWithOpacity(border, .90), 3.5),
                new Point(rail.Left, mid), new Point(rail.Right, mid));
            dc.DrawLine(new Pen(CloneWithOpacity(enabled ? tone : muted,
                    enabled ? .94 : .34), 3.5),
                new Point(rail.Left, mid), new Point(currentX, mid));
            dc.DrawLine(new Pen(CloneWithOpacity(muted, .78), 1.5),
                new Point(originalX, mid - 5.5),
                new Point(originalX, mid + 5.5));
            dc.DrawEllipse(panel,
                new Pen(hovered ? Brushes.White : enabled ? tone : muted,
                    hovered || IsSelected(p) ? 2.3 : 1.5),
                new Point(currentX, mid), 5.3, 5.3);

            // Register long rail before the exact 0% / 100% landmark targets,
            // which override it during reverse hit testing.
            AddHorizontalTarget(p,
                new Rect(rail.Left - 6.0, rail.Top - 6.0,
                    rail.Width + 12.0, rail.Height + 11.0),
                rail, p.Minimum, p.Maximum);
            foreach (var stop in new[] { 0.0, 1.0 })
            {
                var markerX = ValueToX(stop, p.Minimum, p.Maximum, rail, false);
                var isHovered = hovered && _hoverTarget is { } target
                    && Math.Abs(target.VisualMinimum - stop) < Epsilon
                    && Math.Abs(target.VisualMaximum - stop) < Epsilon;
                var glyph = CreateText($"{stop * 100.0:0}%", 9.1,
                    isHovered ? Brushes.White : muted, FontWeights.SemiBold);
                var glyphX = stop == 0.0 ? rail.Left
                    : rail.Right - glyph.WidthIncludingTrailingWhitespace;
                var glyphY = y + 40.5;
                dc.DrawText(glyph, new Point(glyphX, glyphY));
                AddHorizontalTarget(p,
                    new Rect(glyphX - 4.0, glyphY - 2.0,
                        glyph.WidthIncludingTrailingWhitespace + 8.0,
                        glyph.Height + 4.0),
                    new Rect(markerX, glyphY, 1.0, Math.Max(1.0, glyph.Height)),
                    stop, stop);
            }
        }
    }

    // One mixer frame with 3x2 / 6x1 equally scaled, genuinely editable
    // source tracks. Equal widths here represent the common 0..3x INPUT
    // range, not a synthetic six-way selection probability.
    private void DrawStashWeightMixer(
        DrawingContext dc, Rect outer,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        _ = GlobalStashKindsVerified.Value;
        var state = CurrentStashEffectiveState();
        var columns = outer.Width >= 670.0 ? 2 : 1;
        const double inset = 14.0;
        const double gap = 16.0;
        const double rowPitch = 78.0;
        var cellWidth = Math.Max(1.0,
            (outer.Width - 2.0 * inset - (columns - 1) * gap) / columns);

        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .72),
            new Pen(CloneWithOpacity(border, .68), 1.0), outer, 4.0, 4.0);

        for (var i = 0; i < GlobalStashParameterIds.Length; i++)
        {
            var p = P(GlobalStashParameterIds[i]);
            var x = outer.Left + inset + (i % columns) * (cellWidth + gap);
            var y = outer.Top + 13.0 + (i / columns) * rowPitch;
            var enabled = p?.IsControlEnabled == true;
            var name = GlobalStashCategoryNames[i];
            var tone = StashMixPalette[i];

            dc.DrawRectangle(tone, null, new Rect(x, y + 4.0, 3.0, 13.0));
            dc.DrawText(CreateText(name, 10.8, enabled ? text : muted,
                    FontWeights.Bold, Math.Max(1.0, cellWidth - 80.0)),
                new Point(x + 9.0, y + 2.0));
            if (p is null)
                continue;

            // The number is the exact persisted multiplier. Only modified
            // digits grow and turn gold; the x suffix stays neutral.
            var modified = enabled && p.IsValueModifiedFromReference();
            var digits = p.CurrentNumber.ToString("0.#",
                System.Globalization.CultureInfo.InvariantCulture);
            var formatted = CreateText(digits + "x", modified ? 14.0 : 12.8,
                enabled ? text : muted, FontWeights.Bold, Math.Max(1.0, cellWidth - 75.0));
            if (modified)
                formatted.SetForegroundBrush(accent, 0, digits.Length);
            dc.DrawText(formatted, new Point(
                x + cellWidth - formatted.WidthIncludingTrailingWhitespace,
                y - 1.0));

            var rail = new Rect(x + 10.0, y + 28.0,
                Math.Max(1.0, cellWidth - 20.0), 10.0);
            var midY = rail.Top + rail.Height / 2.0;
            var currentX = ValueToX(p.CurrentNumber, p.Minimum, p.Maximum, rail, false);
            var referenceX = ValueToX(p.ReferenceNumber, p.Minimum, p.Maximum, rail, false);
            var hovered = enabled && _hoverTarget?.Kind == HitKind.Horizontal
                && ReferenceEquals(_hoverTarget.Parameter, p);

            dc.DrawLine(new Pen(CloneWithOpacity(border, .94), 4.5),
                new Point(rail.Left, midY), new Point(rail.Right, midY));
            dc.DrawLine(new Pen(CloneWithOpacity(enabled ? tone : muted,
                    enabled ? .96 : .33), 4.5),
                new Point(rail.Left, midY), new Point(currentX, midY));
            dc.DrawLine(new Pen(CloneWithOpacity(muted, .85), 1.8),
                new Point(referenceX, midY - 7.0),
                new Point(referenceX, midY + 7.0));
            dc.DrawEllipse(panel,
                new Pen(hovered ? Brushes.White : enabled ? tone : muted,
                    hovered || IsSelected(p) ? 2.6 : 1.6),
                new Point(currentX, midY), 6.5, 6.5);
            if (enabled)
                dc.DrawEllipse(tone, null, new Point(currentX, midY), 2.2, 2.2);

            // Native 0x/1x/3x landmarks have physical click targets and
            // white hover text, matching the shared Stamina slider contract.
            // Register the long hit rail FIRST, then the marker targets,
            // because FindTarget searches in reverse order.
            AddHorizontalTarget(p,
                new Rect(rail.Left - 6.0, rail.Top - 10.0,
                    rail.Width + 12.0, rail.Height + 16.0),
                rail, p.Minimum, p.Maximum);
            foreach (var stop in new[] { 0.0, 1.0, 3.0 })
            {
                var markerX = ValueToX(stop, p.Minimum, p.Maximum, rail, false);
                var isHovered = hovered && _hoverTarget is { } hoveredTarget
                    && Math.Abs(hoveredTarget.VisualMinimum - stop) < Epsilon
                    && Math.Abs(hoveredTarget.VisualMaximum - stop) < Epsilon;
                var glyph = CreateText($"{stop:0}x", 9.4,
                    isHovered ? Brushes.White : muted, FontWeights.SemiBold);
                var glyphX = stop <= p.Minimum + Epsilon ? rail.Left
                    : stop >= p.Maximum - Epsilon
                        ? rail.Right - glyph.WidthIncludingTrailingWhitespace
                        : markerX - glyph.WidthIncludingTrailingWhitespace / 2.0;
                var glyphY = y + 43.0;
                dc.DrawText(glyph, new Point(glyphX, glyphY));
                AddHorizontalTarget(p,
                    new Rect(glyphX - 4.0, glyphY - 2.0,
                        glyph.WidthIncludingTrailingWhitespace + 8.0,
                        glyph.Height + 4.0),
                    new Rect(markerX, glyphY, 1.0, Math.Max(1.0, glyph.Height)),
                    stop, stop);
            }

            // Inventory/eligibility guidance appears only once, within the
            // same lane. 0x Weight never claims that Chance rows vanished.
            var status = GlobalStashStatus(i, state);
            var detail = GlobalStashDetail(i, status, state);
            dc.DrawText(CreateText(detail, 9.2, muted, FontWeights.SemiBold,
                    Math.Max(1.0, cellWidth - 6.0)),
                new Point(x + 3.0, y + 62.0));
        }

        // One unobtrusive caveat for the mixer, no repeated six-row table.
        dc.DrawText(CreateText("RELATIVE WEIGHT · CHANCE REMAINS INDEPENDENT",
                8.8, muted, FontWeights.SemiBold,
                Math.Max(1.0, outer.Width - 2.0 * inset)),
            new Point(outer.Left + inset, outer.Bottom - 15.0));
    }


    // Tier rows reflect ALL 41 authored ItemGenerator groups rather than one
    // cherry-picked Weight group. Color slots show native item presence,
    // not a 100% cross-group probability. Weight and independently-authored
    // Chance remain distinct even when both occur in the same category.
    private void DrawStashTierWeightPools(
        DrawingContext dc, Rect outer,
        Brush text, Brush muted, Brush border, Brush panel)
    {
        _ = GlobalStashKindsVerified.Value;
        var state = CurrentStashEffectiveState();
        var compact = outer.Width < 670.0;
        const double inset = 12.0;
        const double gap = 5.0;
        var rowPitch = compact ? 82.0 : 64.0;
        var tiers = new (string Label, string SourceTier, int GroupCount)[]
        {
            ("BASIC", "Cheap", 12),
            ("STANDARD", "Medium", 10),
            ("RARE", "Expensive", 19)
        };

        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .70),
            new Pen(CloneWithOpacity(border, .72), 0.9), outer, 4.0, 4.0);
        dc.DrawText(CreateText("STASH CONTENT BY TIER", 10.5,
                text, FontWeights.Bold, Math.Max(1.0, outer.Width - 2.0 * inset)),
            new Point(outer.Left + inset, outer.Top + 8.0));

        for (var t = 0; t < tiers.Length; t++)
        {
            var tier = tiers[t];
            var groups = StashSourceGroups.Value
                .Where(g => !g.IsSmart && g.Tier == tier.SourceTier).ToArray();
            if (groups.Length != tier.GroupCount)
                throw new InvalidDataException(
                    "Stash tier ItemGenerator inventory changed: " + tier.SourceTier);
            var hasWeight = new bool[GlobalStashCategoryNames.Length];
            var hasChance = new bool[GlobalStashCategoryNames.Length];
            foreach (var group in groups)
            {
                if (group.Field is not ("Weight" or "Chance"))
                    throw new InvalidDataException(
                        "Unexpected native stash selection field " + group.Field);
                foreach (var item in group.Items)
                {
                    var kind = StashCategoryIndex(item.ItemSid);
                    if (kind < 0)
                        throw new InvalidDataException(
                            "Unclassified stash tier item " + item.ItemSid);
                    if (group.Field == "Weight")
                        hasWeight[kind] = true;
                    else
                        hasChance[kind] = true;
                }
            }
            var present = Enumerable.Range(0, GlobalStashCategoryNames.Length)
                .Where(index => hasWeight[index] || hasChance[index]).ToArray();
            if (present.Length == 0)
                throw new InvalidDataException("Empty native stash tier " + tier.SourceTier);

            var row = new Rect(outer.Left + inset,
                outer.Top + 30.0 + t * rowPitch,
                Math.Max(1.0, outer.Width - 2.0 * inset), rowPitch - 3.0);
            dc.DrawText(CreateText(tier.Label, 10.2, text,
                    FontWeights.Bold, Math.Max(1.0, row.Width)),
                new Point(row.Left, row.Top));

            // Every segment represents exactly ONE present category, NOT
            // a proportional slice of a combined, unrelated source pool.
            // Color/opacity reflects its global 0..3x setting only if at
            // least one real native Weight item exists for this tier.
            var segmentWidth = Math.Max(1.0,
                (row.Width - gap * (present.Length - 1)) / present.Length);
            for (var n = 0; n < present.Length; n++)
            {
                var kind = present[n];
                var segmentLeft = row.Left + n * (segmentWidth + gap);
                var current = Math.Clamp(
                    state.Value(GlobalStashParameterIds[kind]), 0.0, 3.0);
                var ink = StashMixPalette[kind];
                var weighted = hasWeight[kind];
                var chance = hasChance[kind];
                var selectedWeight = weighted && current > Epsilon;

                // Full segment width means source presence, not likelihood.
                // Chance-only segments have an outline, never a simulated
                // Weight fill. At Weight 0x an independent Chance still exists.
                var fill = selectedWeight
                    ? CloneWithOpacity(ink, 0.40 + 0.60 * current / 3.0)
                    : CloneWithOpacity(border, .16);
                var outline = chance
                    ? CloneWithOpacity(ink, .72)
                    : CloneWithOpacity(border, .40);
                var tile = new Rect(segmentLeft, row.Top + 19.0,
                    segmentWidth, 10.0);
                dc.DrawRoundedRectangle(fill, new Pen(outline,
                    chance ? 1.2 : .65), tile, 1.8, 1.8);
            }

            var legendColumns = compact ? 2 : present.Length;
            var legendGap = 7.0;
            var legendCell = Math.Max(1.0,
                (row.Width - legendGap * (legendColumns - 1)) / legendColumns);
            for (var n = 0; n < present.Length; n++)
            {
                var kind = present[n];
                var column = n % legendColumns;
                var line = n / legendColumns;
                var x = row.Left + column * (legendCell + legendGap);
                var y = row.Top + 35.0 + (compact ? line * 16.0 : 0.0);
                var number = state.Value(GlobalStashParameterIds[kind])
                    .ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
                var status = hasWeight[kind]
                    ? (hasChance[kind] ? number + "x + CHANCE" : number + "x")
                    : "CHANCE ONLY";
                var statusText = CreateText(status, 8.3, muted,
                    FontWeights.SemiBold, Math.Max(1.0, legendCell - 13.0));

                dc.DrawRectangle(StashMixPalette[kind], null,
                    new Rect(x, y + 2.0, 6.0, 7.0));
                if (compact)
                {
                    // On narrow layouts the two-column legend retains one
                    // line per category. Clip names before status overlap.
                    dc.DrawText(CreateText(GlobalStashCategoryNames[kind],
                        8.7, text, FontWeights.SemiBold,
                        Math.Max(1.0, legendCell - statusText.Width - 16.0)),
                        new Point(x + 9.0, y));
                    dc.DrawText(statusText, new Point(
                        x + legendCell - statusText.WidthIncludingTrailingWhitespace, y));
                }
                else
                {
                    dc.DrawText(CreateText(GlobalStashCategoryNames[kind],
                        8.8, text, FontWeights.SemiBold,
                        Math.Max(1.0, legendCell - 11.0)), new Point(x + 9.0, y));
                    dc.DrawText(statusText, new Point(x + 1.0, y + 12.0));
                }
            }
        }
        dc.DrawText(CreateText(
                "WEIGHT = SCALAR · OUTLINED = INDEPENDENT CHANCE · NOT DROP ODDS",
                8.5, muted, FontWeights.SemiBold,
                Math.Max(1.0, outer.Width - 2.0 * inset)),
            new Point(outer.Left + inset, outer.Bottom - 17.0));
    }

}
