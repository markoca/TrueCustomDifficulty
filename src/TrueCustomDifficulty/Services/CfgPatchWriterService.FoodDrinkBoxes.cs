using System;
using System.Collections.Generic;
using System.IO;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

public sealed partial class CfgPatchWriterService
{
    /// <summary>
    /// Zero Weight can intentionally disable individual food/drink outcomes.
    /// Never emit a weighted branch with zero possible outcomes or both
    /// sibling branches at zero Weight: the game's handling is undefined.
    /// Validate before writing any CFG output.
    /// </summary>
    private static void ValidateFoodDrinkBoxWeights(
        IReadOnlyDictionary<string, ParameterViewModel> parameters)
    {
        const string foodId = "loot.foodBoxBranch";
        const string drinkId = "loot.drinkBoxBranch";
        if (!parameters.TryGetValue(foodId, out var food) ||
            !parameters.TryGetValue(drinkId, out var drink))
            throw new InvalidDataException("Food/Drink box branch controls are missing.");

        if (food.CurrentNumber <= 0 && drink.CurrentNumber <= 0)
            throw new InvalidDataException(
                "FOOD BOX ROLL and DRINK BOX ROLL cannot both be 0×.");

        CheckActiveBranch(food, new[]
        {
            "loot.foodBoxSausage", "loot.foodBoxBread",
            "loot.foodBoxCanned", "loot.foodBoxMilk"
        }, "FOOD");
        CheckActiveBranch(drink, new[]
        {
            "loot.drinkBoxWater", "loot.drinkBoxBeer",
            "loot.drinkBoxVodka", "loot.drinkBoxEnergy"
        }, "DRINK");

        void CheckActiveBranch(ParameterViewModel branch,
            IReadOnlyList<string> ids, string name)
        {
            if (branch.CurrentNumber <= 0)
                return;
            foreach (var id in ids)
                if (parameters.TryGetValue(id, out var item) &&
                    item.CurrentNumber > 0)
                    return;

            throw new InvalidDataException(
                name + " BOX ROLL is enabled, but all of its content weights are 0×. " +
                "Raise at least one item mix knob or disable this branch.");
        }
    }
}
