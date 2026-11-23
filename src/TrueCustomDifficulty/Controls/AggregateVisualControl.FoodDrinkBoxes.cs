using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Rank-aware Food/Drink box selection. Newbie uses Weight, higher ranks use
/// independent Chance. This board never claims a global drop probability.
/// </summary>
public sealed partial class AggregateVisualControl
{
    private const double FoodDrinkBoxesHeight = 651.0;
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

    // ItemGeneratorPrototypes.cfg game 2.1: source Newbie/base relative
    // Weight values, never absolute item percentages.
    private static readonly double[] FoodBoxBaseWeights = { .3, .3, .2, .1 };
    private static readonly double[] DrinkBoxBaseWeights = { .3, .3, .2, .1 };
    private static readonly double[][] FoodBoxRankChances =
    {
        new[] { .6, .6, .4, .3 }, // Experienced
        new[] { .5, .5, .5, .4 }, // Veteran
        new[] { .4, .4, .6, .5 }  // Master
    };
    private static readonly double[][] DrinkBoxRankChances =
    {
        new[] { .6, .6, .4, .3 },
        new[] { .5, .5, .5, .4 },
        new[] { .4, .4, .6, .5 }
    };
    private static readonly double[] FoodDrinkBranchChances = { .6, .7, .8 };

    private void DrawFoodDrinkBoxes(DrawingContext dc, double top,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        var foodBranch = P("loot.foodBoxBranch");
        var drinkBranch = P("loot.drinkBoxBranch");
        var food = FoodBoxItemIds.Select(P).ToArray();
        var drink = DrinkBoxItemIds.Select(P).ToArray();
        if (foodBranch is null || drinkBranch is null ||
            food.Any(p => p is null) || drink.Any(p => p is null))
            return;

        const double margin = 17.0;
        const double gap = 10.0;
        var full = Math.Max(1.0, ActualWidth - margin * 2.0);
        var half = Math.Max(1.0, (full - gap) / 2.0);
        DrawText(dc, "FOOD / DRINK BOXES", new Point(margin, top),
            10.2, muted, FontWeights.Bold);
        DrawHorizontalTrackCard(dc, foodBranch, "FOOD BOX ROLL",
            new Rect(margin, top + 21.0, half, 72.0),
            text, muted, border, accent, panel, pairedValueFormatting: true);
        DrawHorizontalTrackCard(dc, drinkBranch, "DRINK BOX ROLL",
            new Rect(margin + half + gap, top + 21.0, half, 72.0),
            text, muted, border, accent, panel, pairedValueFormatting: true);

        void Knobs(ParameterViewModel?[] sources, string[] labels,
            double rowTop, Brush tone)
        {
            var bank = Math.Min(full, 780.0);
            var cell = Math.Max(1.0, (bank - 3.0 * gap) / 4.0);
            var left = (ActualWidth - bank) / 2.0;
            for (var i = 0; i < 4; i++)
                DrawSourceKnob(dc, sources[i]!, labels[i],
                    new Rect(left + i * (cell + gap), rowTop, cell, 142.0),
                    text, muted, border, accent, panel, dialAccent: tone);
        }

        DrawText(dc, "FOOD CONTENT MIX", new Point(margin, top + 101.0),
            10.2, muted, FontWeights.Bold);
        Knobs(food, FoodBoxLabels, top + 117.0, Brushes.Goldenrod);
        DrawText(dc, "DRINK CONTENT MIX", new Point(margin, top + 268.0),
            10.2, muted, FontWeights.Bold);
        Knobs(drink, DrinkBoxLabels, top + 284.0, Brushes.SteelBlue);

        DrawText(dc, "BOX CONTENT PREVIEW · PLAYER RANK",
            new Point(margin, top + 436.0),
            10.2, muted, FontWeights.Bold);
        var previewTop = top + 455.0;
        DrawFoodDrinkRankCard(dc, new Rect(margin, previewTop, half, 179.0),
            "FOOD", FoodBoxShort, food, FoodBoxBaseWeights,
            FoodBoxRankChances, foodBranch, text, muted, border, accent, panel);
        DrawFoodDrinkRankCard(dc,
            new Rect(margin + half + gap, previewTop, half, 179.0),
            "DRINK", DrinkBoxShort, drink, DrinkBoxBaseWeights,
            DrinkBoxRankChances, drinkBranch, text, muted, border, accent, panel);
    }

    private void DrawFoodDrinkRankCard(DrawingContext dc, Rect card,
        string title, string[] labels, ParameterViewModel?[] items,
        double[] weights, double[][] chanceTable, ParameterViewModel branch,
        Brush text, Brush muted, Brush border, Brush accent, Brush panel)
    {
        dc.DrawTrackedRoundedRectangle(panel, new Pen(border, .9),
            card, 4.0, 4.0);
        DrawText(dc, title + " · NEWBIE WEIGHT / RANK CHANCE",
            new Point(card.Left + 9.0, card.Top + 5.0),
            9.2, text, FontWeights.Bold);

        // Rank names occupy 50 DIP, then four equal-width item columns.
        const double labelWidth = 50.0;
        var colWidth = Math.Max(1.0, (card.Width - labelWidth - 12.0) / 4.0);
        for (var i = 0; i < 4; i++)
            DrawCenteredText(dc, labels[i],
                new Point(card.Left + labelWidth + i * colWidth + colWidth / 2.0,
                    card.Top + 34.0), 8.6, muted, FontWeights.SemiBold);

        var sources = items.Select(x => x!).ToArray();
        var weighted = weights.Select((weight, i) =>
            weight * Math.Max(0.0, sources[i].CurrentNumber)).ToArray();
        var sum = weighted.Sum();
        var rowLabels = new[] { "NEWBIE", "EXP", "VET", "MASTER" };
        for (var rank = 0; rank < 4; rank++)
        {
            var y = card.Top + 54.0 + rank * 24.0;
            DrawText(dc, rowLabels[rank],
                new Point(card.Left + 9.0, y), 8.9, muted, FontWeights.SemiBold);
            for (var col = 0; col < 4; col++)
            {
                var value = rank == 0
                    ? sum <= Epsilon ? 0.0 : weighted[col] / sum
                    : Math.Clamp(chanceTable[rank - 1][col] * sources[col].CurrentNumber,
                        0.0, 1.0);
                var ink = sources[col].IsValueModifiedFromReference()
                    ? ReadoutInk.Numbers(accent, text) : text;
                DrawCenteredText(dc, $"{value * 100:0}%",
                    new Point(card.Left + labelWidth + col * colWidth + colWidth / 2.0, y),
                    9.8, ink, FontWeights.SemiBold);
            }
        }

        // Newbie branch is a relative Weight, NOT a Chance gate.
        var gates = FoodDrinkBranchChances.Select(chance =>
            Math.Clamp(chance * branch.CurrentNumber, 0.0, 1.0)).ToArray();
        var gateInk = branch.IsValueModifiedFromReference()
            ? ReadoutInk.Numbers(accent, text) : text;
        DrawText(dc,
            $"BOX GATE  E {gates[0] * 100:0}%  V {gates[1] * 100:0}%  M {gates[2] * 100:0}%",
            new Point(card.Left + 9.0, card.Bottom - 18.0),
            9.1, gateInk, FontWeights.SemiBold);
    }
}
