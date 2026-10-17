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


    // Source-authored Basic RGD-5 Chance / integer Amount and Rare coupons.
    // Same compact native rail, but independent persisted source fields.
    private void DrawStashNativeRollControl(
        DrawingContext dc, string id, string label, int category,
        Rect card, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel, bool percentage = true)
    {
        var p = P(id);
        if (p is null) return;
        var enabled = p.IsControlEnabled;
        var tone = StashMixPalette[category];
        var x = card.Left;
        var y = card.Top;
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .54),
            new Pen(CloneWithOpacity(border, .52), .8), card, 3.0, 3.0);
        dc.DrawRectangle(tone, null, new Rect(x + 6.0, y + 7.0, 3.0, 13.0));
        dc.DrawText(CreateText(label, 10.0,
                enabled ? text : muted, FontWeights.SemiBold,
                Math.Max(1.0, card.Width - 102.0)),
            new Point(x + 14.0, y + 5.0));

        var digits = (percentage ? p.CurrentNumber * 100.0 : p.CurrentNumber)
            .ToString(percentage ? "0.#" : "0",
                System.Globalization.CultureInfo.InvariantCulture);
        var modified = enabled && p.IsValueModifiedFromReference();
        var number = CreateText(digits + (percentage ? "%" : ""),
            modified ? 12.6 : 11.8,
            enabled ? text : muted, FontWeights.Bold);
        if (modified) number.SetForegroundBrush(accent, 0, digits.Length);
        dc.DrawText(number,
            new Point(card.Right - number.WidthIncludingTrailingWhitespace - 11.0,
                y + 3.0));

        var rail = new Rect(x + 13.0, y + 26.0,
            Math.Max(1.0, card.Width - 26.0), 8.0);
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

        // Full rail first, then real native endpoints with hover priority.
        // The Amount endpoint is 4 grenades, NEVER an invented 100% drop.
        AddHorizontalTarget(p,
            new Rect(rail.Left - 6.0, rail.Top - 6.0,
                rail.Width + 12.0, rail.Height + 11.0),
            rail, p.Minimum, p.Maximum);
        foreach (var stop in new[] { p.Minimum, p.Maximum })
        {
            var markerX = ValueToX(stop, p.Minimum, p.Maximum, rail, false);
            var markerHover = hovered && _hoverTarget is { } target
                && Math.Abs(target.VisualMinimum - stop) < Epsilon
                && Math.Abs(target.VisualMaximum - stop) < Epsilon;
            var stopValue = percentage ? stop * 100.0 : stop;
            var glyph = CreateText(
                $"{stopValue:0}{(percentage ? "%" : "")}", 9.1,
                markerHover ? Brushes.White : muted, FontWeights.SemiBold);
            var glyphX = stop <= p.Minimum + Epsilon ? rail.Left
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

    // Exactly the categories that can shift relative choice INSIDE either
    // displayed Basic source-native multi-category Weight pool. Global AMMO
    // only occurs in isolated ammo groups here; Basic grenade is Chance-only.
    // Keep both historic IDs cataloged for preset compatibility, but never
    // misrepresent either as an adjustable member of these two mixer pools.
    private static readonly int[] BasicVisibleWeightKinds = { 1, 2, 3, 5 };

    // Breakable Boxes-style compact source dials; global eligible Weight
    // behavior stays independent of these Basic-only preview samples.
    private void DrawStashWeightMixer(
        DrawingContext dc, Rect outer,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        _ = GlobalStashKindsVerified.Value;
        var basicGroups = StashSourceGroups.Value
            .Where(g => !g.IsSmart && g.Tier == "Cheap").ToArray();
        if (basicGroups.Length != 12 || basicGroups.Sum(g => g.Items.Count) != 52)
            throw new InvalidDataException("Native Basic stash source coverage changed.");

        var previewGroups = new[] {
            StashPreviewGroup("BASIC", 7),
            StashPreviewGroup("BASIC", 1)
        };
        if (previewGroups.Any(g => g is null))
            throw new InvalidDataException("Basic weight preview source missing.");
        var relevant = previewGroups.Where(g => g is not null)
            .SelectMany(g => g!.Items)
            .Select(item => StashCategoryIndex(item.Fields["ItemPrototypeSID"]))
            .Distinct().OrderBy(k => k).ToArray();
        if (!relevant.SequenceEqual(BasicVisibleWeightKinds))
            throw new InvalidDataException(
                "Basic native mixer categories changed; review visible knobs.");

        var columns = outer.Width >= 670.0 ? 4 : 2;
        const double inset = 12.0;
        const double gap = 8.0;
        const double cardHeight = 146.0;
        var cardWidth = Math.Max(1.0,
            (outer.Width - 2.0 * inset - (columns - 1) * gap) / columns);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .72),
            new Pen(CloneWithOpacity(border, .68), 1.0), outer, 4.0, 4.0);

        for (var i = 0; i < BasicVisibleWeightKinds.Length; i++)
        {
            var kind = BasicVisibleWeightKinds[i];
            var x = outer.Left + inset + (i % columns) * (cardWidth + gap);
            var y = outer.Top + (i / columns) * (cardHeight + gap);
            // True 0..1x bounds / 0.1x steps, original at 1x. Reducing one
            // category boosts other positive categories' RELATIVE share.
            DrawSourceKnob(dc, P(GlobalStashParameterIds[kind]),
                GlobalStashCategoryNames[kind],
                new Rect(x, y, cardWidth, cardHeight),
                text, muted, border, accent, panel,
                dialAccent: StashMixPalette[kind], dialScale: .74);
        }
    }

    // Breakable Boxes-inspired read-only distribution. Two ACTUAL distinct
    // Basic ItemGenerator Weight branches, no across-branch normalization.
    // Only four of the six cataloged global IDs influence these two pools;
    // chance-only grenade and other rows remain independent of these bars.
    private void DrawStashBasicNativePools(
        DrawingContext dc, Rect outer,
        Brush text, Brush muted, Brush border, Brush panel)
    {
        var experienced = StashPreviewGroup("BASIC", 7);
        var hard = StashPreviewGroup("BASIC", 1);
        if (experienced is null || hard is null
            || experienced.Items.Any(item => !item.Fields.ContainsKey("Weight")
                || item.Fields.ContainsKey("Chance"))
            || hard.Items.Any(item => !item.Fields.ContainsKey("Weight")
                || item.Fields.ContainsKey("Chance")))
            throw new InvalidDataException(
                "Basic source Weight groups are no longer native-only.");
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .66),
            new Pen(CloneWithOpacity(border, .66), .9), outer, 4.0, 4.0);
        dc.DrawText(CreateText("BASIC POOL COMPOSITION",
                10.2, text, FontWeights.Bold, Math.Max(1.0, outer.Width - 24.0)),
            new Point(outer.Left + 12.0, outer.Top + 6.0));
        var inside = new Rect(outer.Left + 12.0, outer.Top + 28.0,
            Math.Max(1.0, outer.Width - 24.0), 48.0);
        DrawStashPoolBar(dc, experienced,
            "EASY/MEDIUM · EXPERIENCED", inside, text, muted, border);
        DrawStashPoolBar(dc, hard,
            "HARD", new Rect(inside.Left, inside.Top + 56.0,
                inside.Width, inside.Height), text, muted, border);
    }

    // Purely informational native source-coverage rows, never sliders or
    // cross-group probability bars. Standard/Rare are Chance-led by CATEGORY,
    // while their remaining authored Weight branches remain explicitly visible.
    private void DrawStashChanceTier(
        DrawingContext dc, string label, string tierName, int expectedGroups,
        Rect outer, Brush text, Brush muted, Brush border, Brush panel)
    {
        var groups = StashSourceGroups.Value
            .Where(g => !g.IsSmart && g.Tier == tierName).ToArray();
        if (groups.Length != expectedGroups)
            throw new InvalidDataException("Stash source groups changed: " + tierName);

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
                        "Unclassified native stash item " + item.ItemSid);
                if (group.Field == "Weight") hasWeight[kind] = true;
                else hasChance[kind] = true;
            }
        }
        var present = Enumerable.Range(0, GlobalStashCategoryNames.Length)
            .Where(index => hasWeight[index] || hasChance[index]).ToArray();
        // Fail closed instead of displaying a friendly but incorrect tier.
        var expectedKinds = tierName == "Medium"
            ? new[] { 0, 1, 3, 5 }
            : tierName == "Expensive"
                ? new[] { 0, 1, 2, 3, 4, 5 }
                : throw new InvalidDataException("Unsupported stash tier preview");
        if (!present.SequenceEqual(expectedKinds)
            || tierName == "Medium" && (
                !hasWeight[0] || hasChance[0]
                || present.Where(k => k != 0).Any(k => hasWeight[k] || !hasChance[k]))
            || tierName == "Expensive" && (
                !hasWeight[0] || !hasChance[0]
                || present.Where(k => k != 0).Any(k => hasWeight[k] || !hasChance[k])))
            throw new InvalidDataException(
                "Native stash Weight/Chance coverage changed: " + tierName);

        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .65),
            new Pen(CloneWithOpacity(border, .61), .9), outer, 4.0, 4.0);
        dc.DrawText(CreateText(label, 10.5, text, FontWeights.Bold),
            new Point(outer.Left + 12.0, outer.Top + 8.0));
        const string description = "CHANCE-LED · NATIVE SOURCES";
        var heading = CreateText(description, 9.0, muted, FontWeights.SemiBold,
            Math.Max(1.0, outer.Width - 130.0));
        dc.DrawText(heading, new Point(
            outer.Right - heading.WidthIncludingTrailingWhitespace - 12.0,
            outer.Top + 9.0));

        var columns = outer.Width >= 670.0 ? present.Length : 2;
        const double inset = 12.0;
        const double gap = 6.0;
        const double pitch = 37.0;
        var cellWidth = Math.Max(1.0,
            (outer.Width - inset * 2.0 - (columns - 1) * gap) / columns);
        for (var n = 0; n < present.Length; n++)
        {
            var kind = present[n];
            var weighted = hasWeight[kind];
            var chance = hasChance[kind];
            var tone = StashMixPalette[kind];
            var x = outer.Left + inset + (n % columns) * (cellWidth + gap);
            var y = outer.Top + 27.0 + (n / columns) * pitch;
            var tile = new Rect(x, y, cellWidth, 33.0);

            // Chance-only = outlined colored card; authored Weight = muted
            // colored fill. Both fields = tinted + outlined, never a percentage.
            var fill = weighted
                ? CloneWithOpacity(tone, .22)
                : CloneWithOpacity(panel, .35);
            var outline = chance ? CloneWithOpacity(tone, .75)
                : CloneWithOpacity(border, .44);
            dc.DrawRoundedRectangle(fill,
                new Pen(outline, chance ? 1.1 : .8), tile, 3.0, 3.0);
            dc.DrawRectangle(tone, null,
                new Rect(tile.Left + 6.0, tile.Top + 6.0, 3.0, 9.0));
            dc.DrawText(CreateText(GlobalStashCategoryNames[kind], 9.1,
                    text, FontWeights.SemiBold, Math.Max(1.0, tile.Width - 19.0)),
                new Point(tile.Left + 13.0, tile.Top + 3.0));
            var fieldLabel = weighted && chance ? "WEIGHT + CHANCE"
                : weighted ? "WEIGHT" : "CHANCE ONLY";
            dc.DrawText(CreateText(fieldLabel, 8.6, muted,
                    FontWeights.SemiBold, Math.Max(1.0, tile.Width - 15.0)),
                new Point(tile.Left + 8.0, tile.Top + 18.0));
        }
    }

}
