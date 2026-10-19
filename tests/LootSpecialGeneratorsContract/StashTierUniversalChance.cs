using System;
using System.Collections.Generic;
using System.Linq;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;

namespace LootSpecialGeneratorsContract;

internal static partial class Program
{
    private static void VerifyStashTierUniversalChanceScale()
    {
        // The tier scaler must operate on ORIGINAL per-entry native Chance
        // while preserving old hidden category factors, rank/difficulty gates,
        // Weight-only groups, coupon absolute overrides and other tiers.
        var cases = new[]
        {
            ("Medium", "loot.standardStashChanceScale",
                Baseline.Special.MediumStashAmmoProduction.TemplateCategories),
            ("Expensive", "loot.rareStashChanceScale",
                Baseline.Special.ExpensiveStashProduction.TemplateCategories)
        };
        foreach (var (tier, scaleId, groups) in cases)
        {
            var chanceRows = groups.SelectMany(group => group.Items
                .Where(item => item.Fields.ContainsKey("Chance"))
                .Select(item => (Group: group, Item: item))).ToArray();
            var weightRows = groups.SelectMany(group => group.Items
                .Where(item => item.Fields.ContainsKey("Weight"))
                .Select(item => (Group: group, Item: item))).ToArray();
            Check(chanceRows.Length > 0 && weightRows.Length > 0,
                tier + ": native Chance and Weight coverage missing");
            foreach (var factor in new[] { 0.0, 0.5, 1.0, 2.0 })
            {
                var numbers = new Dictionary<string, double>(StringComparer.Ordinal)
                    { [scaleId] = factor };
                var changed = factor == 1.0 ? Array.Empty<string>()
                    : new[] { scaleId };
                var state = new StashEffectiveState(numbers, changed, "Easy");
                foreach (var (group, item) in chanceRows)
                {
                    var authored = StashEffectiveState.Number(item.Fields["Chance"]);
                    var result = StashEffectiveState.Number(state.TierField(
                        Baseline, tier, group, item, "Chance"));
                    Check(Math.Abs(result - Math.Clamp(authored * factor, 0, 1))
                        < 0.000001,
                        tier + ": universal scaler changed original Chance math");
                }
                foreach (var (group, item) in weightRows)
                    Check(state.TierField(Baseline, tier, group, item, "Weight")
                        == item.Fields["Weight"],
                        tier + ": universal Chance scale edited a Weight-only row");
            }

            // An old custom preset with one non-1 category factor still
            // multiplies the matching Chance, rather than losing its setting.
            var sample = chanceRows.FirstOrDefault(row =>
                StashEffectiveState.TierChanceParameterId(tier,
                    row.Item.Fields["ItemPrototypeSID"]) is not null);
            Check(sample.Item is not null, tier + ": missing legacy Chance candidate");
            var legacyId = StashEffectiveState.TierChanceParameterId(
                tier, sample.Item.Fields["ItemPrototypeSID"])!;
            var oldValues = new Dictionary<string, double>
            {
                [scaleId] = 2.0,
                [legacyId] = 0.5
            };
            var oldState = new StashEffectiveState(oldValues,
                oldValues.Keys, "Easy");
            var source = StashEffectiveState.Number(sample.Item.Fields["Chance"]);
            var oldResult = StashEffectiveState.Number(oldState.TierField(
                Baseline, tier, sample.Group, sample.Item, "Chance"));
            Check(Math.Abs(oldResult - source) < 0.000001,
                tier + ": old category factor is not composed with the new scale");
        }
        Console.WriteLine("PASS Standard/Rare common native Chance scale, Weight protection and legacy factor preservation");
    }
}
