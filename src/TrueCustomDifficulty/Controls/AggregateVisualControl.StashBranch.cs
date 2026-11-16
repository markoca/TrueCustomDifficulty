using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TrueCustomDifficulty.Services;

namespace TrueCustomDifficulty.Controls;

// Shared stash source catalog (native Basic / Standard / Rare and SmartLoot
// groups) and the compact native rail used by the STASH LOOT MIX amount row.
public sealed partial class AggregateVisualControl
{
    // Read-only native source groups behind ODDS PER STASH and the writer checks.
    private static readonly Lazy<IReadOnlyList<StashBranchPreview>> StashSourceGroups =
        new(() => StashBranchPreviewCatalog.Load(StashPreviewBaseline.Value));

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

    // Source-authored Basic RGD-5 Chance / integer Amount and Rare coupons.
    // Same compact native rail, but independent persisted source fields.
    private void DrawStashNativeRollControl(
        DrawingContext dc, string id, string label, int category,
        Rect card, Brush text, Brush muted, Brush border,
        Brush accent, Brush panel, bool percentage = true, bool multiplier = false)
    {
        var p = P(id);
        if (p is null) return;
        var enabled = p.IsControlEnabled;
        var tone = StashMixPalette[category];
        var x = card.Left;
        var y = card.Top;
        dc.DrawTrackedRoundedRectangle(CloneWithOpacity(panel, .54),
            new Pen(CloneWithOpacity(border, .52), .8), card, 3.0, 3.0);
        dc.DrawRectangle(tone, null, new Rect(x + 6.0, y + 7.0, 3.0, 13.0));
        dc.DrawText(CreateText(label, 10.0,
                enabled ? text : muted, FontWeights.SemiBold,
                Math.Max(1.0, card.Width - 102.0)),
            new Point(x + 14.0, y + 5.0));

        var digits = (percentage ? p.CurrentNumber * 100.0 : p.CurrentNumber)
            .ToString(multiplier ? "0.##" : percentage ? "0.#" : "0",
                System.Globalization.CultureInfo.InvariantCulture);
        var modified = enabled && p.IsValueModifiedFromReference();
        var number = CreateText(digits + (multiplier ? "×" : percentage ? "%" : ""),
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
            var endValue = stopValue.ToString(multiplier ? "0.##" : "0",
                System.Globalization.CultureInfo.InvariantCulture);
            var glyph = CreateText(
                endValue + (multiplier ? "×" : percentage ? "%" : ""), 9.1,
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

}
