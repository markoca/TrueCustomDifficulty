using System;
using System.Collections.Generic;
namespace TrueCustomDifficulty.Services;

/// <summary>
/// Literal native 2.1 Chance values by player rank. Newbie is Weight-based and
/// deliberately does not have a Chance row or artificial percentage input.
/// </summary>
public static class FoodDrinkBoxPolicy
{
    public static readonly string[] Roots =
    {
        "loot.foodBoxBranch",
        "loot.drinkBoxBranch",
        "loot.foodBoxSausage",
        "loot.foodBoxBread",
        "loot.foodBoxCanned",
        "loot.foodBoxMilk",
        "loot.drinkBoxWater",
        "loot.drinkBoxBeer",
        "loot.drinkBoxVodka",
        "loot.drinkBoxEnergy"
    };
    public static readonly string[] Ranks = { "Experienced", "Veteran", "Master" };
    private static readonly IReadOnlyDictionary<string, double[]> VanillaChances =
        new Dictionary<string, double[]>(StringComparer.Ordinal)
        {
            ["loot.foodBoxBranch"] = new[] { 0.6, 0.7, 0.8 },
            ["loot.drinkBoxBranch"] = new[] { 0.6, 0.7, 0.8 },
            ["loot.foodBoxSausage"] = new[] { 0.6, 0.5, 0.4 },
            ["loot.foodBoxBread"] = new[] { 0.6, 0.5, 0.4 },
            ["loot.foodBoxCanned"] = new[] { 0.4, 0.5, 0.6 },
            ["loot.foodBoxMilk"] = new[] { 0.3, 0.4, 0.5 },
            ["loot.drinkBoxWater"] = new[] { 0.6, 0.5, 0.4 },
            ["loot.drinkBoxBeer"] = new[] { 0.6, 0.5, 0.4 },
            ["loot.drinkBoxVodka"] = new[] { 0.4, 0.5, 0.6 },
            ["loot.drinkBoxEnergy"] = new[] { 0.3, 0.4, 0.5 }
        };

    public static string ChanceId(string root, int rankIndex)
    {
        if (rankIndex is < 1 or > 3 || !VanillaChances.ContainsKey(root))
            throw new ArgumentOutOfRangeException(nameof(rankIndex));
        return root + Ranks[rankIndex - 1];
    }

    public static double VanillaChance(string root, int rankIndex)
        => VanillaChances[root][rankIndex - 1];

    // Exactly 10 percentage-point steps, with half-up rounding.
    public static double SnapChance(double value)
        => Math.Round(Math.Clamp(value, 0.0, 1.0) * 10.0,
            MidpointRounding.AwayFromZero) / 10.0;
}
