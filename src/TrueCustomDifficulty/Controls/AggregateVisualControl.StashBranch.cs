using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using TrueCustomDifficulty.Services;

namespace TrueCustomDifficulty.Controls;

// A bounded, read-only native branch inspector directly below the six knobs.
// Browser navigation is transient UI state: it never changes presets/CFG output.
public sealed partial class AggregateVisualControl
{
    private static readonly Lazy<IReadOnlyList<StashBranchPreview>> StashBranches =
        new(() => StashBranchPreviewCatalog.Load(StashPreviewBaseline.Value));
    private readonly List<(Rect Hit, Action Navigate)> _stashBrowserTargets = new();
    // The six knobs use one global category factor. This native browser
    // is optional read-only inspection, not an independent tuning axis.
    private bool _stashBrowserExpanded;
    private int _stashBrowserMode; // 0 Basic, 1 Standard, 2 Rare, 3 SmartLoot
    private int _stashBrowserRank = 1; // Experienced
    private int _stashBrowserPrototype;
    private int _stashBrowserGroup;
    private int _stashBrowserHover = -1;

    // Shared native category semantics. The ItemGenerator and SmartLoot
    // writers have slightly different source SID capitalization (AntiRad vs
    // Antirad), so resolve through BOTH production mappings. Unclassified
    // future source items remain neutral rather than assigned an invented pool.
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

    // The app's top Base Preset is the sole difficulty selector.
    // UI Novice/Stalker/Veteran/Master map to native Easy/Medium/Hard/Stalker.
    // Resolve on every render/filter so changing Base Preset updates the browser.
    private string CurrentStashNativeDifficulty => SelectedReferenceDifficulty switch
    {
        "Novice" => "Easy",
        "Stalker" => "Medium",
        "Veteran" => "Hard",
        "Master" => "Stalker",
        _ => throw new InvalidOperationException(
            "Unknown Base Preset difficulty: " + SelectedReferenceDifficulty)
    };

    private static IReadOnlyList<string> StashSmartPrototypes =>
        StashBranches.Value.Where(b => b.IsSmart)
            .Select(b => b.Prototype).Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal).ToArray();

    private IReadOnlyList<StashBranchPreview> AvailableStashBranches()
    {
        var tier = _stashBrowserMode switch
        {
            0 => "Cheap", 1 => "Medium", 2 => "Expensive", _ => ""
        };
        var prototype = StashSmartPrototypes[_stashBrowserPrototype];
        return StashBranches.Value.Where(x =>
                (_stashBrowserMode == 3
                    ? x.IsSmart && x.Prototype == prototype
                    : !x.IsSmart && x.Tier == tier)
                && x.AppliesTo(_stashBrowserRank,
                    CurrentStashNativeDifficulty))
            .ToArray();
    }

    private void MoveStashGroup(int delta)
    {
        var count = AvailableStashBranches().Count;
        _stashBrowserGroup = count == 0 ? 0
            : (_stashBrowserGroup + delta % count + count) % count;
    }

    private void NavigateStash(int action, int delta)
    {
        switch (action)
        {
            case 0: _stashBrowserMode = (int)Math.Clamp(delta, 0, 3); break;
            case 2: _stashBrowserRank = (_stashBrowserRank + delta + 4) % 4; break;
            case 3:
                var count = StashSmartPrototypes.Count;
                _stashBrowserPrototype = (_stashBrowserPrototype + delta + count) % count;
                break;
            case 4: MoveStashGroup(delta); InvalidateVisual(); return;
        }
        _stashBrowserGroup = 0;
        InvalidateVisual();
    }

    private void ToggleStashBrowser()
    {
        _stashBrowserExpanded = !_stashBrowserExpanded;
        _stashBrowserTargets.Clear();
        _stashBrowserHover = -1;
        InvalidateMeasure();
        InvalidateVisual();
    }

    private bool TryStashBrowserClick(Point point)
    {
        if (Family != "StashContentMix") return false;
        foreach (var target in _stashBrowserTargets)
        {
            if (!target.Hit.Contains(point)) continue;
            target.Navigate();
            return true;
        }
        return false;
    }

    private void UpdateStashBrowserHover(Point point)
    {
        if (Family != "StashContentMix") return;
        var next = _stashBrowserTargets.FindIndex(x => x.Hit.Contains(point));
        // OnMouseMove already picks the source knob/slider cursor. A browser
        // miss must NOT reset that cursor to Arrow; an unchanged browser hover
        // must still display Hand after the common hover-handler ran.
        var changed = next != _stashBrowserHover;
        _stashBrowserHover = next;
        if (_dragTarget is null && next >= 0)
            Cursor = Cursors.Hand;
        if (changed) InvalidateVisual();
    }

    private void DrawStashBrowserButton(
        DrawingContext dc, string caption, Rect rect, bool selected,
        Action action, Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        var index = _stashBrowserTargets.Count;
        var hovered = index == _stashBrowserHover;
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, selected ? .92 : .45),
            new Pen(CloneWithOpacity(selected ? accent : border,
                hovered ? .98 : selected ? .72 : .42), hovered ? 1.4 : .9),
            rect, 3.0, 3.0);
        DrawCenteredText(dc, caption,
            new Point(rect.Left + rect.Width / 2.0, rect.Top + 5.0),
            10.2, selected || hovered ? text : muted, FontWeights.SemiBold);
        _stashBrowserTargets.Add((rect, action));
    }

    private void DrawStashBranchBrowser(
        DrawingContext dc, Rect outer,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        _stashBrowserTargets.Clear();
        // The hot target list is rebuilt with each paint, so never keep a
        // stale hover index after changing a generator or shrinking the list.
        // The disclosure button gets an additional dynamic hit target.
        _stashBrowserHover = Math.Max(-1, _stashBrowserHover);
        dc.DrawRoundedRectangle(CloneWithOpacity(panel, .58),
            new Pen(CloneWithOpacity(border, .65), .9),
            outer, 4.0, 4.0);
        var x = outer.Left + 12.0;
        var width = outer.Width - 24.0;
        DrawText(dc, "NATIVE BRANCH", new Point(x, outer.Top + 7.0),
            11.0, text, FontWeights.Bold);
        DrawRightAlignedText(dc, "SOURCE FIELDS · CONDITIONAL OUTPUT",
            new Point(outer.Right - 12.0, outer.Top + 8.0),
            8.9, muted, FontWeights.SemiBold);

        var labels = new[] { "BASIC", "STANDARD", "RARE", "SMARTLOOT" };
        const double gap = 7.0;
        var cell = (width - gap * 3.0) / 4.0;
        for (var i = 0; i < 4; i++)
        {
            var index = i;
            DrawStashBrowserButton(dc, labels[i],
                new Rect(x + i * (cell + gap), outer.Top + 30.0, cell, 24.0),
                _stashBrowserMode == i,
                () => NavigateStash(0, index), text, muted, border, accent, panel);
        }

        // Only native RANK is navigable for tier groups. Difficulty comes
        // from the top Base Preset; SmartLoot still has PROTOTYPE + RANK.
        var railY = outer.Top + 64.0;
        var half = (width - 12.0) / 2.0;
        void NavRail(double railX, double railWidth, string title,
            string current, int kind, double centerFactor = .60)
        {
            DrawText(dc, title,
                new Point(railX, railY + 5.0), 9.0, muted, FontWeights.SemiBold);
            var center = railX + railWidth * centerFactor;
            var leftButton = new Rect(center - 92.0, railY, 24.0, 25.0);
            var rightButton = new Rect(center + 68.0, railY, 24.0, 25.0);
            DrawStashBrowserButton(dc, "‹", leftButton, false,
                () => NavigateStash(kind, -1), text, muted, border, accent, panel);
            DrawStashBrowserButton(dc, "›", rightButton, false,
                () => NavigateStash(kind, +1), text, muted, border, accent, panel);
            DrawCenteredText(dc, current, new Point(center, railY + 6.0),
                10.2, text, FontWeights.SemiBold);
        }
        if (_stashBrowserMode == 3)
        {
            var name = StashSmartPrototypes[_stashBrowserPrototype];
            // Prototypes can be long, and the selector never extends its cell.
            var compact = name.Replace("Stash_", "").Replace("Stash", "");
            if (compact.Length > 20) compact = compact[..19] + "…";
            NavRail(x, half, "PROTOTYPE", compact, 3);
        }
        if (_stashBrowserMode == 3)
            NavRail(x + half + 12.0, half, "RANK",
                StashBranchPreview.RankName(_stashBrowserRank).ToUpperInvariant(), 2);
        else
            NavRail(x + (width - half) / 2.0, half, "RANK",
                StashBranchPreview.RankName(_stashBrowserRank).ToUpperInvariant(), 2, .50);

        var available = AvailableStashBranches();
        if (_stashBrowserGroup >= available.Count)
            _stashBrowserGroup = 0;
        var groupY = outer.Top + 100.0;
        DrawStashBrowserButton(dc, "‹",
            new Rect(x, groupY, 27.0, 26.0), false,
            () => NavigateStash(4, -1), text, muted, border, accent, panel);
        DrawStashBrowserButton(dc, "›",
            new Rect(x + width - 27.0, groupY, 27.0, 26.0), false,
            () => NavigateStash(4, +1), text, muted, border, accent, panel);
        if (available.Count == 0)
        {
            DrawCenteredText(dc, "NO NATIVE GROUP FOR THIS RANK / DIFFICULTY",
                new Point(outer.Left + outer.Width / 2.0, groupY + 8.0),
                10.5, muted, FontWeights.SemiBold);
            return;
        }

        var branch = available[_stashBrowserGroup];
        var shortPath = branch.IsSmart
            ? branch.GroupPath.Split("SmartLootParams.", StringSplitOptions.None).Last()
                .Replace(".Items", "")
            : branch.GroupPath.Replace("ItemGenerator.", "").Replace(".PossibleItems", "");
        var branchTitle = $"{_stashBrowserGroup + 1}/{available.Count} · {branch.Category} · {shortPath}";
        DrawCenteredText(dc, branchTitle,
            new Point(outer.Left + outer.Width / 2.0, groupY + 8.0),
            10.2, text, FontWeights.SemiBold);
        DrawText(dc, branch.Field == "Weight"
                ? "WEIGHT · RELATIVE ONLY INSIDE THIS GROUP"
                : "CHANCE · INDEPENDENT SOURCE FIELDS",
            new Point(x, outer.Top + 134.0), 9.1, muted, FontWeights.SemiBold);
        DrawRightAlignedText(dc, $"{branch.Items.Count} ITEMS",
            new Point(outer.Right - 12.0, outer.Top + 134.0),
            9.0, muted, FontWeights.SemiBold);

        var state = CurrentStashEffectiveState(CurrentStashNativeDifficulty);
        var values = branch.Items.Select(item => StashEffectiveState.Number(
                branch.EffectiveSelection(StashPreviewBaseline.Value, state, item)))
            .ToArray();
        var total = values.Sum();
        var maxCount = Math.Max(1, branch.Items.Count);
        var rowHeight = Math.Min(29.0,
            (outer.Height - 192.0) / maxCount);
        var top = outer.Top + 154.0;
        for (var i = 0; i < branch.Items.Count; i++)
        {
            var item = branch.Items[i];
            var rowY = top + i * rowHeight;
            if (i % 2 == 0)
                dc.DrawRectangle(CloneWithOpacity(border, .11), null,
                    new Rect(x - 2.0, rowY, width + 4.0, rowHeight));
            var colorIndex = StashCategoryIndex(item.ItemSid);
            var ink = colorIndex >= 0 ? StashMixPalette[colorIndex] : muted;
            var original = StashEffectiveState.Number(item.SourceRaw);
            var actual = values[i];
            var min = branch.EffectiveCount(StashPreviewBaseline.Value, state, item, "MinCount");
            var max = branch.EffectiveCount(StashPreviewBaseline.Value, state, item, "MaxCount");
            // Keep current values legible on narrow boards: show quantities
            // on their own short line, omitting a redundant unchanged arrow.
            var countDetail = item.MinCount.Length > 0 && item.MaxCount.Length > 0
                ? item.MinCount == min && item.MaxCount == max
                    ? $"{min}–{max}" : $"{item.MinCount}–{item.MaxCount} → {min}–{max}"
                : "";
            var share = branch.Field == "Weight"
                ? total > Epsilon ? actual / total : 0.0 : actual;
            var name = item.ItemSid.Length > 24
                ? item.ItemSid[..23] + "…" : item.ItemSid;
            DrawText(dc, $"{item.Index} · {name}",
                new Point(x + 4.0, rowY + 1.0), 9.6, text, FontWeights.SemiBold);
            var sourceNumbers = branch.Field == "Chance"
                ? $"{original * 100:0.#}% → {actual * 100:0.#}%"
                : $"{original:0.###} → {actual:0.###}";
            DrawRightAlignedText(dc, branch.Field == "Weight"
                ? $"{sourceNumbers} · {(total > Epsilon ? $"{share * 100:0.#}%" : "UNDEFINED")}"
                : sourceNumbers,
                new Point(x + width - 3.0, rowY + 1.0),
                9.5, muted, FontWeights.SemiBold);
            if (countDetail.Length > 0)
                DrawRightAlignedText(dc, countDetail,
                    new Point(x + width - 3.0, rowY + 12.0),
                    8.9, muted, FontWeights.Normal);
            var bar = new Rect(x + 3.0, rowY + rowHeight - 8.0,
                width - 6.0, 3.0);
            dc.DrawRoundedRectangle(CloneWithOpacity(border, .27), null,
                bar, 1.5, 1.5);
            if (share > Epsilon)
                dc.DrawRoundedRectangle(CloneWithOpacity(ink, .92), null,
                    new Rect(bar.Left, bar.Top, bar.Width * Math.Clamp(share, 0.0, 1.0),
                        bar.Height), 1.5, 1.5);
        }

        var bottom = outer.Bottom - 32.0;
        if (branch.IsSmart)
        {
            var spawn = branch.EffectiveSpawn(state);
            DrawText(dc, $"SPAWN MIN {branch.MinSpawnChance} → {spawn.Min}  ·  MAX {branch.MaxSpawnChance} → {spawn.Max}"
                    + (branch.ItemSetCount.Length > 0
                        ? $"  ·  SETS {branch.ItemSetCount} → {spawn.Sets}" : ""),
                new Point(x, bottom), 9.0, muted, FontWeights.SemiBold);
        }
        else
        {
            var rank = branch.RankGate.Length > 0
                ? branch.RankGate.Replace("ERank::", "") : "ALL RANKS";
            var diff = branch.DifficultyGate.Length > 0
                ? branch.DifficultyGate.Replace("EGameDifficulty::", "") : "ALL DIFFICULTIES";
            DrawText(dc, $"NATIVE GATE · {rank} / {diff}",
                new Point(x, bottom), 8.9, muted, FontWeights.SemiBold);
        }
        DrawText(dc, branch.Field == "Weight" && total <= Epsilon
                ? "ZERO WEIGHT POOL: GAME FALLBACK UNVERIFIED"
                : "SOURCE VALUES, NOT WHOLE-STASH DROP PROBABILITY",
            new Point(x, outer.Bottom - 17.0),
            8.7, muted, FontWeights.Normal);
    }
}
