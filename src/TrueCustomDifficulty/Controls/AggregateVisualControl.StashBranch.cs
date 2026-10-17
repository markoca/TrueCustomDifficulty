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


    // Each example is one audited native ItemGenerator Weight group.
    // These are three SEPARATE pools, not a tier-wide weighted sum or a
    // percentage of physical stashes. Chance-only rows are intentionally
    // excluded, as are unrelated SmartLoot groups and rank branches.
    private void DrawStashTierWeightPools(
        DrawingContext dc, Rect outer,
        Brush text, Brush muted, Brush border, Brush panel)
    {
        _ = GlobalStashKindsVerified.Value;
        var state = CurrentStashEffectiveState();
        var compact = outer.Width < 550.0;
        var rowPitch = compact ? 82.0 : 64.0;
        var examples = new (string Label, string Tier, int GroupIndex,
            string NativeCategory, string RankGate, string DifficultyGate,
            int ItemCount)[]
        {
            ("BASIC · CONSUMABLE · EXPERIENCED · EASY/MEDIUM", "Cheap", 7,
                "EItemGenerationCategory::Consumable", "ERank::Experienced",
                "EGameDifficulty::Easy, EGameDifficulty::Medium", 6),
            ("STANDARD · AMMO · EXPERIENCED", "Medium", 3,
                "EItemGenerationCategory::Ammo", "ERank::Experienced", "", 5),
            ("RARE · AMMO · EXPERIENCED", "Expensive", 6,
                "EItemGenerationCategory::Ammo", "ERank::Experienced", "", 3)
        };

        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .70),
            new Pen(CloneWithOpacity(border, .72), 0.9), outer, 4.0, 4.0);
        dc.DrawText(CreateText("STASH POOL COMPOSITION", 10.5,
                text, FontWeights.Bold, Math.Max(1.0, outer.Width - 24.0)),
            new Point(outer.Left + 12.0, outer.Top + 8.0));

        for (var i = 0; i < examples.Length; i++)
        {
            var example = examples[i];
            var tierLabel = example.Tier switch
            {
                "Cheap" => "BASIC",
                "Medium" => "STANDARD",
                "Expensive" => "RARE",
                _ => throw new InvalidDataException("Unknown stash preview tier")
            };
            var group = StashPreviewGroup(tierLabel, example.GroupIndex);
            if (group is null || group.Items.Count != example.ItemCount
                || group.Fields.GetValueOrDefault("Category", "") != example.NativeCategory
                || group.Fields.GetValueOrDefault("PlayerRank", "") != example.RankGate
                || group.Fields.GetValueOrDefault("Diff", "") != example.DifficultyGate
                || group.Items.Any(item => !item.Fields.ContainsKey("Weight")
                    || item.Fields.ContainsKey("Chance")))
                throw new InvalidDataException(
                    "Stash tier Weight preview source group changed: " + tierLabel);

            var totals = new double[GlobalStashCategoryNames.Length];
            var present = new SortedSet<int>();
            foreach (var item in group.Items)
            {
                var sid = item.Fields["ItemPrototypeSID"];
                var category = StashCategoryIndex(sid);
                if (category < 0)
                    throw new InvalidDataException("Unclassified stash Weight item " + sid);
                present.Add(category);
                // EXACT same field pipeline as the production tier writer.
                var value = StashEffectiveState.Number(state.TierField(
                    StashPreviewBaseline.Value, example.Tier, group, item, "Weight"));
                if (!double.IsFinite(value) || value < 0.0)
                    throw new InvalidDataException("Invalid effective stash Weight " + sid);
                totals[category] += value;
            }

            var sum = totals.Sum();
            var row = new Rect(outer.Left + 12.0,
                outer.Top + 30.0 + i * rowPitch,
                Math.Max(1.0, outer.Width - 24.0), rowPitch - 4.0);
            dc.DrawText(CreateText(example.Label, 10.0, text,
                    FontWeights.SemiBold, Math.Max(1.0, row.Width)),
                new Point(row.Left, row.Top));

            var bar = new Rect(row.Left, row.Top + 19.0,
                Math.Max(1.0, row.Width), 10.0);
            dc.DrawRoundedRectangle(CloneWithOpacity(border, .28), null,
                bar, 2.0, 2.0);
            var cursor = bar.Left;
            var lastNonzero = Array.FindLastIndex(totals, x => x > Epsilon);
            for (var category = 0; category < totals.Length; category++)
            {
                var part = totals[category];
                if (part <= Epsilon || sum <= Epsilon) continue;
                var span = category == lastNonzero
                    ? Math.Max(0.0, bar.Right - cursor)
                    : Math.Max(0.0, bar.Width * part / sum);
                if (span > Epsilon)
                    dc.DrawRectangle(StashMixPalette[category], null,
                        new Rect(cursor, bar.Top, span, bar.Height));
                cursor += span;
            }

            // Breakable Boxes-style swatches/percent labels are valid only
            // INSIDE this specific native group. Retain 0% labels for native
            // members switched off, and never add absent item kinds.
            var kinds = present.ToArray();
            var legendColumns = compact ? Math.Min(2, kinds.Length) : kinds.Length;
            var columnGap = 8.0;
            var slotWidth = Math.Max(1.0,
                (row.Width - columnGap * (legendColumns - 1)) / legendColumns);
            for (var n = 0; n < kinds.Length; n++)
            {
                var category = kinds[n];
                var column = n % legendColumns;
                var line = n / legendColumns;
                var x = row.Left + column * (slotWidth + columnGap);
                var y = row.Top + 37.0 + line * 16.0;
                dc.DrawRectangle(StashMixPalette[category], null,
                    new Rect(x, y + 2.0, 7.0, 7.0));
                dc.DrawText(CreateText(GlobalStashCategoryNames[category],
                        8.6, text, FontWeights.SemiBold,
                        Math.Max(1.0, slotWidth - 52.0)),
                    new Point(x + 10.0, y));
                var share = sum > Epsilon ? 100.0 * totals[category] / sum : 0.0;
                DrawRightAlignedText(dc, $"{share:0.#}%",
                    new Point(x + slotWidth, y),
                    8.6, muted, FontWeights.SemiBold);
            }
            if (sum <= Epsilon)
                DrawRightAlignedText(dc, "NO WEIGHTED ITEMS",
                    new Point(row.Right, row.Top),
                    8.7, muted, FontWeights.SemiBold);
        }

        dc.DrawText(CreateText("ONE SOURCE GROUP PER TIER · NOT STASH DROP ODDS",
                8.6, muted, FontWeights.SemiBold, Math.Max(1.0, outer.Width - 24.0)),
            new Point(outer.Left + 12.0, outer.Bottom - 17.0));
    }
}
