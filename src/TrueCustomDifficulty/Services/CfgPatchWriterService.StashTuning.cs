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

    private static readonly string[] StashTierChanceParameterIds =
    {
        "loot.mediumStashMedicalChance", "loot.mediumStashDrinksChance",
        "loot.mediumStashCouponsChance",
        "loot.expensiveStashAmmoChance", "loot.expensiveStashMedicalChance",
        "loot.expensiveStashFoodChance", "loot.expensiveStashDrinksChance",
        "loot.expensiveStashGrenadesChance"
    };

    private static readonly string[] StashMedicalParameterIds =
    {
        "loot.stashMedicalBandageBias", "loot.stashMedicalMedkitBias",
        "loot.stashMedicalArmyMedkitBias", "loot.stashMedicalAntiRadBias",
        "loot.stashMedicalBandageChanceScale", "loot.stashMedicalMedkitChanceScale",
        "loot.stashMedicalArmyMedkitChanceScale", "loot.stashMedicalAntiRadChanceScale",
        "loot.stashMedkitMinCount", "loot.stashMedkitMaxCount",
        "loot.stashArmyMedkitMinCount", "loot.stashArmyMedkitMaxCount"
    };

    private static readonly string[] StashGearParameterIds =
    {
        "loot.stashGearEnabled",
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
                or "loot.stashMedicalArmyMedkitChanceScale"
                or "loot.stashMedicalAntiRadChanceScale"
                or "loot.stashArmyMedkitMinCount"
                or "loot.stashArmyMedkitMaxCount");

    private static bool HasStashTuningChange(
        IReadOnlyDictionary<string, ParameterViewModel> modified, string tier)
        => StashMixParameterIds.Any(modified.ContainsKey)
           || StashTierChanceParameterIds.Any(id =>
               id.StartsWith(tier == "Medium" ? "loot.mediumStash"
                   : tier == "Expensive" ? "loot.expensiveStash" : "loot.cheapStash",
                   StringComparison.Ordinal) && modified.ContainsKey(id))
           || StashMedicalParameterIds.Any(id =>
               IsStashMedicalSourceInTier(id, tier) && modified.ContainsKey(id))
           || modified.ContainsKey("loot.stashGearEnabled")
           || modified.ContainsKey($"loot.stashGear{tier}Weapon")
           || modified.ContainsKey($"loot.stashGear{tier}Armor");

    private static double StashTuningCurrent(
        IReadOnlyDictionary<string, ParameterViewModel> all,
        string id, double otherwise)
        => all.TryGetValue(id, out var p) ? p.CurrentNumber : otherwise;

    // Legacy call sites (direct, Cabin and DLC1) now delegate to the SAME
    // immutable field transformer used by the three tiers and UI previews.
    private static StashEffectiveState StashState(
        IReadOnlyDictionary<string, ParameterViewModel> all)
        => new(all.Values, Array.Empty<string>(), "Easy");

    private static string? StashItemBiasId(string itemSid)
        => StashEffectiveState.ItemBiasId(itemSid);

    private static bool IsStashMedicalSelectionDisabled(
        string itemSid, IReadOnlyDictionary<string, ParameterViewModel> all)
        => StashState(all).MedicalDisabled(itemSid);

    private static string TuneStashSelectionField(
        string itemSid, string field, string currentRaw,
        IReadOnlyDictionary<string, ParameterViewModel> all)
        => StashState(all).TuneSelection(itemSid, field, currentRaw);

    private static string TuneStashMedicalCountField(
        IReadOnlyDictionary<string, string> source, string field,
        string raw, IReadOnlyDictionary<string, ParameterViewModel> all)
        => StashState(all).TuneCount(source, field, raw);

    private static void MarkModifiedStashTuning(
        IReadOnlyDictionary<string, ParameterViewModel> modified,
        string tier, ISet<string> written)
    {
        foreach (var id in StashMixParameterIds)
            if (modified.ContainsKey(id)) written.Add(id);
        foreach (var id in StashTierChanceParameterIds)
            if (id.StartsWith(tier == "Medium" ? "loot.mediumStash"
                : tier == "Expensive" ? "loot.expensiveStash" : "loot.cheapStash",
                StringComparison.Ordinal) && modified.ContainsKey(id))
                written.Add(id);
        foreach (var id in StashMedicalParameterIds)
            if (IsStashMedicalSourceInTier(id, tier) && modified.ContainsKey(id))
                written.Add(id);
        if (modified.ContainsKey("loot.stashGearEnabled"))
            written.Add("loot.stashGearEnabled");
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
        if (!all.TryGetValue("loot.stashGearEnabled", out var master) || !master.CurrentBoolean)
            return 0;
        var weaponLevel = Math.Clamp((int)Math.Round(
            StashTuningCurrent(all, $"loot.stashGear{tier}Weapon", 0)), 0, 3);
        var armorLevel = Math.Clamp((int)Math.Round(
            StashTuningCurrent(all, $"loot.stashGear{tier}Armor", 0)), 0, 3);
        if (weaponLevel == 0 && armorLevel == 0)
            return 0;

        // Source-audited gear tiers: Cheap <= T1, Medium <= T2,
        // Expensive follows PlayerRank T1..T4. No Gauss/RPG/uniques/quests.
        // This is player-rank gating, not unproven regional assignment.
        static string GearSid(string t, bool weapon, int rank) =>
            (t, weapon) switch
            {
                ("Cheap", true) => new[] { "GunPM_HG", "GunUDP_HG", "GunViper_PP", "GunAKU_PP" }[rank],
                ("Medium", true) => new[] { "GunAK74_ST", "GunM860_SG", "GunG37_ST", "GunSVDM_SP" }[rank],
                ("Expensive", true) => new[] { "GunAK74_ST", "GunMark_SP", "GunGrim_ST", "GunKharod_ST" }[rank],
                ("Cheap", false) => "Newbee_Neutral_Armor",
                ("Medium", false) => new[] { "Newbee_Neutral_Armor", "Zorya_Neutral_Armor", "Zorya_Neutral_Armor", "Zorya_Neutral_Armor" }[rank],
                ("Expensive", false) => new[] { "Newbee_Neutral_Armor", "Zorya_Neutral_Armor", "SEVA_Neutral_Armor", "Exoskeleton_Neutral_Armor" }[rank],
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
                category.SetAssignment("bAllowSameCategoryGeneration", "false");
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
