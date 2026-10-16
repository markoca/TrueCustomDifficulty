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

    private static readonly string[] StashMedicalParameterIds =
    {
        "loot.stashMedicalBandageBias", "loot.stashMedicalMedkitBias",
        "loot.stashMedicalArmyMedkitBias", "loot.stashMedicalAntiRadBias",
        "loot.stashMedkitMinCount", "loot.stashMedkitMaxCount",
        "loot.stashArmyMedkitMinCount", "loot.stashArmyMedkitMaxCount"
    };

    private static readonly string[] StashGearParameterIds =
    {
        "loot.stashGearCheapWeapon", "loot.stashGearCheapArmor",
        "loot.stashGearMediumWeapon", "loot.stashGearMediumArmor",
        "loot.stashGearExpensiveWeapon", "loot.stashGearExpensiveArmor"
    };

    private static bool IsStashMedicalSourceInTier(string parameterId, string tier)
        // ArmyMedkit and AntiRad are authored only in the Rare source template.
        // Do not generate unrelated Basic/Standard clone redirects for them.
        => tier == "Expensive"
            || parameterId is not (
                "loot.stashMedicalArmyMedkitBias"
                or "loot.stashMedicalAntiRadBias"
                or "loot.stashArmyMedkitMinCount"
                or "loot.stashArmyMedkitMaxCount");

    private static bool HasStashTuningChange(
        IReadOnlyDictionary<string, ParameterViewModel> modified, string tier)
        => StashMixParameterIds.Any(modified.ContainsKey)
           || StashMedicalParameterIds.Any(id =>
               IsStashMedicalSourceInTier(id, tier) && modified.ContainsKey(id))
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
        "Vodka" or "Water" or "Milk" or "Energetic" or "Beer" => "loot.stashMixDrinks",
        "GrenadeRGD5" or "GrenadeF1" => "loot.stashMixGrenades",
        "MoneyCommon" or "MoneyRare" or "MoneyEpic" or "MoneyLegendary"
            => "loot.stashMixCoupons",
        _ when itemSid.Length > 1 && itemSid[0] == 'A'
            && char.IsDigit(itemSid[1]) => "loot.stashMixAmmo",
        _ => null
    };

    // Medical 0x is a hard exclusion within production stash generator clones.
    // A zero Weight on an all-zero weighted pool might still be selected by
    // engine fallback logic, so combine zero selection with zero stack counts.
    // Keep this limited to audited medical SIDs, not unrelated stash rewards.
    private static bool IsStashMedicalSelectionDisabled(
        string itemSid, IReadOnlyDictionary<string, ParameterViewModel> all)
    {
        var itemBiasId = itemSid switch
        {
            "Bandage" => "loot.stashMedicalBandageBias",
            "Medkit" => "loot.stashMedicalMedkitBias",
            "ArmyMedkit" => "loot.stashMedicalArmyMedkitBias",
            "AntiRad" => "loot.stashMedicalAntiRadBias",
            "EcoMedkit" => null, // Global MEDICAL applies; no individual slider.
            _ => string.Empty
        };
        if (itemBiasId == string.Empty)
            return false;
        if (itemSid is "Medkit" or "ArmyMedkit"
            && StashTuningCurrent(all, $"loot.stash{itemSid}MaxCount", 0) < -0.5)
            return true;
        return StashTuningCurrent(all, "loot.stashMixMedical", 1) <= 0.0
            || itemBiasId is not null
            && StashTuningCurrent(all, itemBiasId, 1) <= 0.0;
    }

    private static string TuneStashSelectionField(
        string itemSid, string field, string currentRaw,
        IReadOnlyDictionary<string, ParameterViewModel> all)
    {
        if (field is not ("Chance" or "Weight"))
            return currentRaw;
        // Hard-disable both source selection fields and source counts when a
        // medical class is explicitly excluded; zero Weight alone may not be
        // sufficient for an entirely zero-weight selection group.
        if (IsStashMedicalSelectionDisabled(itemSid, all))
            return FormatLikeSource(0.0, currentRaw);
        var biasId = StashItemBiasId(itemSid);
        if (biasId is null)
            return currentRaw;
        var bias = StashTuningCurrent(all, biasId, 1);
        // Global medical bias and per-item factor compose, rather than
        // changing the chances or weights of every medkit as one group.
        var detailId = itemSid switch
        {
            "Bandage" => "loot.stashMedicalBandageBias",
            "Medkit" => "loot.stashMedicalMedkitBias",
            "ArmyMedkit" => "loot.stashMedicalArmyMedkitBias",
            "AntiRad" => "loot.stashMedicalAntiRadBias",
            _ => null
        };
        if (detailId is not null)
            bias *= StashTuningCurrent(all, detailId, 1);
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

    private static string TuneStashMedicalCountField(
        IReadOnlyDictionary<string, string> source, string field,
        string raw, IReadOnlyDictionary<string, ParameterViewModel> all)
    {
        if (field is not ("MinCount" or "MaxCount")
            || !source.TryGetValue("ItemPrototypeSID", out var sid))
            return raw;
        // When a medical entry is suppressed, force zero stack size as well
        // as Chance/Weight=0. This includes Bandage, AntiRad and EcoMedkit,
        // not just the two items with dedicated MIN/MAX editors.
        if (IsStashMedicalSelectionDisabled(sid, all))
            return "0";
        if (sid is not ("Medkit" or "ArmyMedkit"))
            return raw;
        var minFloor = (int)Math.Round(StashTuningCurrent(
            all, $"loot.stash{sid}MinCount", 0));
        var maxCap = (int)Math.Round(StashTuningCurrent(
            all, $"loot.stash{sid}MaxCount", 0));
        if (minFloor == 0 && maxCap == 0)
            return raw;

        if (!source.TryGetValue("MinCount", out var originalMinRaw)
            || !source.TryGetValue("MaxCount", out var originalMaxRaw)
            || !int.TryParse(originalMinRaw, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var sourceMin)
            || !int.TryParse(originalMaxRaw, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out var sourceMax))
            throw new InvalidDataException(
                $"Source stash {sid} has missing/invalid MinCount or MaxCount.");
        if (sourceMin < 0 || sourceMax < sourceMin)
            throw new InvalidDataException($"Invalid source stash {sid} count bounds.");

        // Explicit 0 ITEMS (MAX=-1) always overrides a positive MIN FLOOR:
        // write both native count fields as 0. TuneStashSelectionField also
        // zeros the associated Medkit/ArmyMedkit Chance/Weight entry.
        // Old stored MAX=0 is still AUTO and leaves source counts intact.
        if (maxCap == -1)
            return "0";

        // Positive values are a minimum floor or maximum cap; 0 is AUTO,
        // not an instruction to put zero medical items in a stash.
        var min = minFloor > 0 ? Math.Max(sourceMin, minFloor) : sourceMin;
        var max = maxCap > 0 ? Math.Min(sourceMax, maxCap) : sourceMax;
        if (min > max)
        {
            if (maxCap > 0)
                min = max;  // explicit cap wins
            else
                max = min;  // requested floor extends a lower source maximum
        }
        return (field == "MinCount" ? min : max)
            .ToString(CultureInfo.InvariantCulture);
    }

    private static void MarkModifiedStashTuning(
        IReadOnlyDictionary<string, ParameterViewModel> modified,
        string tier, ISet<string> written)
    {
        foreach (var id in StashMixParameterIds)
            if (modified.ContainsKey(id)) written.Add(id);
        foreach (var id in StashMedicalParameterIds)
            if (IsStashMedicalSourceInTier(id, tier) && modified.ContainsKey(id))
                written.Add(id);
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
