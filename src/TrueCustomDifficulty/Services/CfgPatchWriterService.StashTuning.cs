using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

// Existing source-backed production-only TCD stash clone templates share these
// transforms. This partial class does NOT touch scripted/quest stash prototypes.
public sealed partial class CfgPatchWriterService
{
    private static readonly string[] StashMixParameterIds =
    {
        "loot.stashMixAmmo", "loot.stashMixMedical", "loot.stashMixFood",
        "loot.stashMixDrinks", "loot.stashMixGrenades", "loot.stashMixCoupons"
    };

    private static readonly string[] StashGearParameterIds =
    {
        "loot.stashGearCheapWeapon", "loot.stashGearCheapArmor",
        "loot.stashGearMediumWeapon", "loot.stashGearMediumArmor",
        "loot.stashGearExpensiveWeapon", "loot.stashGearExpensiveArmor"
    };

    private static bool HasStashTuningChange(
        IReadOnlyDictionary<string, ParameterViewModel> modified, string tier)
        => StashMixParameterIds.Any(modified.ContainsKey)
           || modified.ContainsKey($"loot.stashGear{tier}Weapon")
           || modified.ContainsKey($"loot.stashGear{tier}Armor");

    private static double StashTuningCurrent(
        IReadOnlyDictionary<string, ParameterViewModel> all,
        string id, double otherwise)
        => all.TryGetValue(id, out var p) ? p.CurrentNumber : otherwise;

    private static string? StashItemBiasId(string itemSid) => itemSid switch
    {
        "Bandage" or "Medkit" or "ArmyMedkit" or "EcoMedkit" or "AntiRad"
            => "loot.stashMixMedical",
        "Bread" or "Sausage" or "CannedFood" => "loot.stashMixFood",
        "Vodka" or "Water" or "Milk" or "Energetic" => "loot.stashMixDrinks",
        "GrenadeRGD5" or "GrenadeF1" => "loot.stashMixGrenades",
        "MoneyCommon" or "MoneyRare" or "MoneyEpic" or "MoneyLegendary"
            => "loot.stashMixCoupons",
        _ when itemSid.Length > 1 && itemSid[0] == 'A'
            && char.IsDigit(itemSid[1]) => "loot.stashMixAmmo",
        _ => null
    };

    private static string TuneStashSelectionField(
        string itemSid, string field, string currentRaw,
        IReadOnlyDictionary<string, ParameterViewModel> all)
    {
        if (field is not ("Chance" or "Weight"))
            return currentRaw;
        var biasId = StashItemBiasId(itemSid);
        if (biasId is null)
            return currentRaw;
        var bias = StashTuningCurrent(all, biasId, 1);
        if (Math.Abs(bias - 1) < 0.0000001)
            return currentRaw;
        var source = ParseNumber(currentRaw);
        // Chance edits are per-item, not total stash probabilities. Never
        // turn a source-zero row into an active item or go above 100%.
        var tuned = field == "Chance"
            ? Math.Clamp(source * bias, 0.0, 1.0)
            : Math.Max(0.0, source * bias);
        return FormatLikeSource(tuned, currentRaw);
    }

    private static void MarkModifiedStashTuning(
        IReadOnlyDictionary<string, ParameterViewModel> modified,
        string tier, ISet<string> written)
    {
        foreach (var id in StashMixParameterIds)
            if (modified.ContainsKey(id)) written.Add(id);
        foreach (var suffix in new[] { "Weapon", "Armor" })
        {
            var id = $"loot.stashGear{tier}{suffix}";
            if (modified.ContainsKey(id)) written.Add(id);
        }
    }

    private static int WriteExtraStashGear(
        PatchNode itemGenerator,
        IReadOnlyDictionary<string, ParameterViewModel> all,
        string tier, int firstFreeCategory)
    {
        var weaponLevel = Math.Clamp((int)Math.Round(
            StashTuningCurrent(all, $"loot.stashGear{tier}Weapon", 0)), 0, 3);
        var armorLevel = Math.Clamp((int)Math.Round(
            StashTuningCurrent(all, $"loot.stashGear{tier}Armor", 0)), 0, 3);
        if (weaponLevel == 0 && armorLevel == 0)
            return 0;

        // Production ItemGeneratorPrototypes.cfg confirms these regular item
        // SIDs and WeaponPrimary/BodyArmor category syntax. Item selection
        // remains rank-gated. Chance is per-entry, not a promise that N% of
        // unopened stashes will contain the extra gear.
        static string GearSid(string t, bool weapon, int rank) =>
            (t, weapon) switch
            {
                ("Cheap", true) => new[] { "GunPM_HG", "GunUDP_HG", "GunM10_HG", "GunM860_SG" }[rank],
                ("Medium", true) => new[] { "GunM860_SG", "GunAK74_ST", "GunViper_PP", "GunG37_ST" }[rank],
                ("Expensive", true) => new[] { "GunAK74_ST", "GunG37_ST", "GunDnipro_ST", "GunKharod_ST" }[rank],
                ("Cheap", false) => new[] { "Newbee_Neutral_Armor", "Jacket_Bandit_Armor", "SkinJacket_Bandit_Armor", "Middle_Bandit_Armor" }[rank],
                ("Medium", false) => new[] { "Zorya_Neutral_Armor", "Jemmy_Neutral_Armor", "Light_Mercenaries_Armor", "Nasos_Neutral_Armor" }[rank],
                ("Expensive", false) => new[] { "Jemmy_Neutral_Armor", "Nasos_Neutral_Armor", "SEVA_Neutral_Armor", "Exoskeleton_Neutral_Armor" }[rank],
                _ => throw new InvalidDataException($"Unknown stash gear tier '{t}'.")
            };

        var ranks = new[] { "Newbie", "Experienced", "Veteran", "Master" };
        var edits = 0;
        for (var kind = 0; kind < 2; kind++)
        {
            var isWeapon = kind == 0;
            var level = isWeapon ? weaponLevel : armorLevel;
            if (level == 0)
                continue;
            var chance = level switch { 1 => 0.02, 2 => 0.07, 3 => 0.15, _ => 0.0 };
            for (var rank = 0; rank < ranks.Length; rank++)
            {
                var category = itemGenerator.GetOrAddChild(
                    $"[{firstFreeCategory + kind * 4 + rank}]", useBpatch: false);
                category.SetAssignment("Category", isWeapon
                    ? "EItemGenerationCategory::WeaponPrimary"
                    : "EItemGenerationCategory::BodyArmor");
                category.SetAssignment("PlayerRank", $"ERank::{ranks[rank]}");
                category.SetAssignment("bAllowSameCategoryGeneration", "true");
                var item = category.GetOrAddChild("PossibleItems", useBpatch: false)
                    .GetOrAddChild("[0]", useBpatch: false);
                item.SetAssignment("ItemPrototypeSID", GearSid(tier, isWeapon, rank));
                item.SetAssignment("Chance", FormatNumber(chance));
                item.SetAssignment("MinCount", "1");
                item.SetAssignment("MaxCount", "1");
                item.SetAssignment("MinDurability", isWeapon ? "0.35" : "0.45");
                item.SetAssignment("MaxDurability", isWeapon ? "0.65" : "0.75");
                if (isWeapon)
                {
                    item.SetAssignment("AmmoMinCount", "0");
                    item.SetAssignment("AmmoMaxCount", "0");
                }
                edits += isWeapon ? 10 : 8;
            }
        }
        return edits;
    }
}
