using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Actual native food/drink chances are separate per player rank. Newbie is
/// weighted selection and NEVER displayed as an independent Chance gate.
/// All four previews are read-only: native editors above own the hit targets.
/// </summary>
public sealed partial class AggregateVisualControl
{
    // A Food/Drink-only compact rotary grammar: responsive height is derived
    // from the exact rank header and card positions drawn below.
    private const double FoodDrinkKnobHeight = 108.0;
    private const double FoodDrinkKnobColumnGap = 18.0;
    private const double FoodDrinkKnobRowPitch = 122.0;
    private const double FoodDrinkKnobBankCap = 384.0;
    private static double FoodDrinkRankOffset(double width)
        => width < 600.0 ? 24.0 : 0.0;
    private static double FoodDrinkBoxesHeight(double width)
        => 616.0 + FoodDrinkRankOffset(width);
    private int _foodDrinkSelectedRank = 2; // Veteran player-rank source on opening
    private readonly Rect[] _foodDrinkRankTabs = new Rect[4];

    private static readonly string[] FoodBoxItemIds =
    {
        "loot.foodBoxSausage", "loot.foodBoxBread",
        "loot.foodBoxCanned", "loot.foodBoxMilk"
    };
    private static readonly string[] DrinkBoxItemIds =
    {
        "loot.drinkBoxWater", "loot.drinkBoxBeer",
        "loot.drinkBoxVodka", "loot.drinkBoxEnergy"
    };
    private static readonly string[] FoodBoxLabels =
        { "SAUSAGE", "BREAD", "CANNED FOOD", "MILK" };
    private static readonly string[] DrinkBoxLabels =
        { "WATER", "BEER", "VODKA", "ENERGY DRINK" };
    private static readonly string[] FoodBoxShort =
        { "SAUSAGE", "BREAD", "CANNED", "MILK" };
    private static readonly string[] DrinkBoxShort =
        { "WATER", "BEER", "VODKA", "ENERGY" };
    private static readonly double[] BaseFoodWeights = { .3, .3, .2, .1 };
    private static readonly double[] BaseDrinkWeights = { .3, .3, .2, .1 };
    private static readonly Brush[] FoodPalette =
        { Brushes.Goldenrod, Brushes.DarkOrange, Brushes.BurlyWood, Brushes.Khaki };
    private static readonly Brush[] DrinkPalette =
        { Brushes.SteelBlue, Brushes.DodgerBlue, Brushes.MediumPurple, Brushes.CadetBlue };
    private static readonly string[] RankTabLabels =
        { "NEWBIE", "EXP", "VET", "MASTER" };

    private bool TrySelectFoodDrinkRank(Point point)
    {
        if (Family != "AmmoDistribution" || !IsEnabled)
            return false;
        for (var i = 0; i < _foodDrinkRankTabs.Length; i++)
        {
            if (_foodDrinkRankTabs[i].IsEmpty || !_foodDrinkRankTabs[i].Contains(point))
                continue;
            _foodDrinkSelectedRank = i;
            _selectedParameter = null;
            _selectedHitKind = null;
            InvalidateVisual();
            return true;
        }
        return false;
    }

    private ParameterViewModel? FoodDrinkSource(string root)
        => P(_foodDrinkSelectedRank == 0
            ? root : FoodDrinkBoxPolicy.ChanceId(root, _foodDrinkSelectedRank));

    private void DrawFoodDrinkBoxes(DrawingContext dc, double top,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        const double margin = 17.0;
        const double gap = 10.0;
        var full = Math.Max(1.0, ActualWidth - margin * 2.0);
        var half = Math.Max(1.0, (full - gap) / 2.0);
        var rankOffset = FoodDrinkRankOffset(ActualWidth);
        DrawText(dc, "FOOD / DRINK BOXES", new Point(margin, top),
            10.2, muted, FontWeights.Bold);

        // Compact same-board rank selector: only rank-specific editors change.
        // Current rank is preserved when sliders move; no dropdown scroll theft.
        const double tabWidth = 59.0;
        const double tabGap = 5.0;
        // Center the complete selector, never align it to the right edge.
        // At narrow widths the selector moves below the title, rather than
        // overlapping FOOD / DRINK BOXES or forcing chips offscreen.
        var tabLeft = (ActualWidth - (tabWidth * 4.0 + tabGap * 3.0)) / 2.0;
        for (var i = 0; i < RankTabLabels.Length; i++)
        {
            var tab = new Rect(tabLeft + i * (tabWidth + tabGap),
                top - 2.0 + rankOffset, tabWidth, 19.0);
            _foodDrinkRankTabs[i] = tab;
            var selected = _foodDrinkSelectedRank == i;
            dc.DrawRoundedRectangle(
                selected ? CloneWithOpacity(accent, .17) : CloneWithOpacity(panel, .92),
                new Pen(selected ? accent : border, selected ? 1.3 : .8),
                tab, 3.0, 3.0);
            DrawCenteredText(dc, RankTabLabels[i],
                new Point(tab.Left + tab.Width / 2.0, tab.Top + 2.0),
                9.0, selected ? accent : muted, FontWeights.Bold);
        }

        var foodBranch = FoodDrinkSource("loot.foodBoxBranch");
        var drinkBranch = FoodDrinkSource("loot.drinkBoxBranch");
        var food = FoodBoxItemIds.Select(FoodDrinkSource).ToArray();
        var drink = DrinkBoxItemIds.Select(FoodDrinkSource).ToArray();

        var mode = _foodDrinkSelectedRank == 0 ? "WEIGHT" : "CHANCE";
        DrawHorizontalTrackCard(dc, foodBranch, "FOOD " + mode,
            new Rect(margin, top + 21.0 + rankOffset, half, 72.0),
            text, muted, border, accent, panel, pairedValueFormatting: true);
        DrawHorizontalTrackCard(dc, drinkBranch, "DRINK " + mode,
            new Rect(margin + half + gap, top + 21.0 + rankOffset, half, 72.0),
            text, muted, border, accent, panel, pairedValueFormatting: true);


        // Food LEFT and Drink RIGHT, each a matching 2×2 grid.
        // Cap the dial bank inside each half while preserving equal horizontal
        // columns, identical row pitch, a shared title baseline and native
        // compact knobs. Every hit rectangle follows its rendered center.
        void DrawKnobGrid(ParameterViewModel?[] sources, string[] labels,
            Brush[] palette, double groupLeft)
        {
            var bank = Math.Min(Math.Max(1.0, half - 16.0), FoodDrinkKnobBankCap);
            var cell = Math.Max(1.0, (bank - FoodDrinkKnobColumnGap) / 2.0);
            var left = groupLeft + (half - bank) / 2.0;
            var firstRowTop = top + 117.0 + rankOffset;
            for (var i = 0; i < 4; i++)
            {
                var column = i % 2;
                var row = i / 2;
                var knobLeft = left + column * (cell + FoodDrinkKnobColumnGap);
                var knobTop = firstRowTop + row * FoodDrinkKnobRowPitch;
                DrawSourceKnob(dc, sources[i], labels[i],
                    new Rect(knobLeft, knobTop, cell, FoodDrinkKnobHeight),
                    text, muted, border, accent, panel,
                    percentageReadout: _foodDrinkSelectedRank != 0,
                    dialAccent: palette[i], dialScale: 0.68,
                    compactLayout: true);
            }
        }

        var foodLeft = margin;
        var drinkLeft = margin + half + gap;
        DrawCenteredText(dc, "FOOD CONTENT " + mode,
            new Point(foodLeft + half / 2.0, top + 101.0 + rankOffset),
            10.2, muted, FontWeights.Bold);
        DrawCenteredText(dc, "DRINK CONTENT " + mode,
            new Point(drinkLeft + half / 2.0, top + 101.0 + rankOffset),
            10.2, muted, FontWeights.Bold);
        DrawKnobGrid(food, FoodBoxLabels, FoodPalette, foodLeft);
        DrawKnobGrid(drink, DrinkBoxLabels, DrinkPalette, drinkLeft);

        DrawText(dc, "BOX CONTENT · NATIVE PLAYER-RANK VALUES",
            new Point(margin, top + 363.0 + rankOffset), 10.2, muted, FontWeights.Bold);
        var previewTop = top + 382.0 + rankOffset;
        DrawFoodDrinkRankBars(dc, new Rect(margin, previewTop, half, 217.0),
            "FOOD", FoodBoxShort, FoodBoxItemIds, BaseFoodWeights,
            FoodPalette, "loot.foodBoxBranch",
            text, muted, border, accent, panel);
        DrawFoodDrinkRankBars(dc,
            new Rect(margin + half + gap, previewTop, half, 217.0),
            "DRINK", DrinkBoxShort, DrinkBoxItemIds, BaseDrinkWeights,
            DrinkPalette, "loot.drinkBoxBranch",
            text, muted, border, accent, panel);
    }

    private void DrawFoodDrinkRankBars(DrawingContext dc, Rect card,
        string title, string[] labels, string[] roots, double[] weights,
        Brush[] palette, string branchId,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        dc.DrawTrackedRoundedRectangle(panel, new Pen(border, .9),
            card, 4.0, 4.0);
        DrawText(dc, title + " · WEIGHT MIX / INDEPENDENT CHANCES",
            new Point(card.Left + 10.0, card.Top + 6.0),
            9.2, text, FontWeights.Bold);

        const double labelWidth = 52.0;
        var left = card.Left + labelWidth;
        var totalWidth = Math.Max(1.0, card.Width - labelWidth - 12.0);
        var tileWidth = totalWidth / 4.0;
        for (var i = 0; i < 4; i++)
            DrawCenteredText(dc, labels[i],
                new Point(left + (i + .5) * tileWidth, card.Top + 32.0),
                8.5, muted, FontWeights.SemiBold);

        // NEWBIE: one exact normalized Weight-distribution bar, just like
        // the breakable ammo mix, with a separate absolute-vs-relative label.
        var newbie = roots.Select((id, i) =>
            Math.Max(0.0, P(id)?.CurrentNumber ?? 1.0) * weights[i]).ToArray();
        var total = newbie.Sum();
        DrawText(dc, "NEWBIE", new Point(card.Left + 7.0, card.Top + 55.0),
            8.8, muted, FontWeights.Bold);
        var strip = new Rect(left, card.Top + 58.0, totalWidth, 14.0);
        dc.DrawRoundedRectangle(CloneWithOpacity(border, .28), null, strip, 2.0, 2.0);
        var cursor = strip.Left;
        for (var i = 0; i < 4; i++)
        {
            var share = total <= Epsilon ? 0.0 : newbie[i] / total;
            var width = strip.Width * share;
            if (width <= Epsilon)
                continue;
            var segment = new Rect(cursor, strip.Top, width, strip.Height);
            dc.DrawRectangle(palette[i], null, segment);
            var value = $"{share * 100.0:0}%";
            var label = CreateText(value, 8.4, Brushes.Black, FontWeights.Bold);
            if (label.WidthIncludingTrailingWhitespace + 5.0 <= width)
                dc.DrawText(label, new Point(
                    cursor + (width - label.WidthIncludingTrailingWhitespace) / 2.0,
                    strip.Top + (strip.Height - label.Height) / 2.0));
            cursor += width;
        }

        // Higher ranks: four INDEPENDENT native Chances per row.
        // They may sum over 100%; never normalize them into a fake ammo-style
        // exclusive share. Each mini rail is individually scaled 0..100%.
        for (var rank = 1; rank <= 3; rank++)
        {
            var y = card.Top + 85.0 + (rank - 1) * 34.0;
            DrawText(dc, new[] { "", "EXP", "VET", "MASTER" }[rank],
                new Point(card.Left + 7.0, y + 2.0),
                8.8, muted, FontWeights.Bold);
            for (var i = 0; i < 4; i++)
            {
                var p = P(FoodDrinkBoxPolicy.ChanceId(roots[i], rank));
                var value = Math.Clamp(p?.CurrentNumber
                    ?? FoodDrinkBoxPolicy.VanillaChance(roots[i], rank), 0.0, 1.0);
                var cell = new Rect(left + i * tileWidth + 3.0,
                    y + 18.0, Math.Max(1.0, tileWidth - 6.0), 5.0);
                dc.DrawRoundedRectangle(CloneWithOpacity(border, .65), null, cell, 1.5, 1.5);
                if (value > Epsilon)
                    dc.DrawRoundedRectangle(palette[i], null,
                        new Rect(cell.Left, cell.Top, cell.Width * value, cell.Height),
                        1.5, 1.5);
                var ink = p?.IsValueModifiedFromReference() == true
                    ? ReadoutInk.Numbers(accent, text) : text;
                DrawCenteredText(dc, $"{value * 100:0}%",
                    new Point(left + (i + .5) * tileWidth, y),
                    9.3, ink, FontWeights.SemiBold);
            }
        }

        var gates = Enumerable.Range(1, 3)
            .Select(rank => P(FoodDrinkBoxPolicy.ChanceId(branchId, rank)))
            .ToArray();
        var gateLabel = string.Join("  ", gates.Select((p, i) =>
            $"{(new[] { "E", "V", "M" })[i]} {Math.Clamp(p?.CurrentNumber ?? FoodDrinkBoxPolicy.VanillaChance(branchId, i + 1), 0.0, 1.0) * 100:0}%"));
        DrawText(dc, "BOX GATE  " + gateLabel,
            new Point(card.Left + 9.0, card.Bottom - 18.0),
            9.1, muted, FontWeights.SemiBold);
    }
}
