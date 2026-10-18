using System;
using System.Linq;
using System.Reflection;
using TrueCustomDifficulty.Controls;

namespace LootSpecialGeneratorsContract;

internal static partial class Program
{
    private static void VerifyStashGearVisualWeightShares()
    {
        // Exactly the same shared 0.1x precision as the CFG class mixer.
        // This presentation must NOT be interpreted as a two-slot selection
        // chance or as a physical stash reward probability.
        var helper = typeof(AggregateVisualControl).GetMethod(
            "StashGearWeightShares", BindingFlags.Static | BindingFlags.NonPublic);
        Check(helper is not null, "Extra Gear relative weight preview is missing");

        static void Scenario(MethodInfo method, double[] weights, double[] expected,
            string label)
        {
            var result = method.Invoke(null, new object[] { weights }) as double[];
            Check(result is not null && result.Length == 4,
                "Extra Gear weight preview shape drift: " + label);
            for (var i = 0; i < expected.Length; i++)
                Check(Math.Abs(result![i] - expected[i]) < 0.000001,
                    "Extra Gear weight preview differs from native build-time weighting: "
                    + label + " / index " + i);
        }

        Scenario(helper!, new[] { 3.0, 1.0, 0.0, 2.0 },
            new[] { .5, 1.0 / 6.0, 0.0, 1.0 / 3.0 }, "3-1-0-2");
        Scenario(helper!, new[] { 1.0, 1.0, 1.0, 1.0 },
            new[] { .25, .25, .25, .25 }, "equal");
        Scenario(helper!, new[] { 0.0, 0.0, 0.0, 0.0 },
            new[] { 0.0, 0.0, 0.0, 0.0 }, "off");
        Scenario(helper!, new[] { 0.0, 0.0, 0.0, 3.0 },
            new[] { 0.0, 0.0, 0.0, 1.0 }, "single attachment");
        Scenario(helper!, new[] { 0.1, 0.2, 0.0, 0.3 },
            new[] { 1.0 / 6.0, 2.0 / 6.0, 0.0, 3.0 / 6.0 },
            "native deciticks");
        Console.WriteLine("PASS Extra Stash Gear read-only weight mix preview parity");
    }
}
