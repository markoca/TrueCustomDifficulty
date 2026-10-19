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
        "loot.standardStashChanceScale", "loot.rareStashChanceScale",
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
        "loot.stashGearEnabled", "loot.stashGearCoveragePercent",
        "loot.stashGearWeaponWeight", "loot.stashGearArmorWeight",
        "loot.stashGearArtifactWeight", "loot.stashGearAttachmentWeight",
        "loot.stashGearBonusChance"
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
               ((tier == "Medium" && id == "loot.standardStashChanceScale")
                || (tier == "Expensive" && id == "loot.rareStashChanceScale")
                || id.StartsWith(tier == "Medium" ? "loot.mediumStash"
                    : tier == "Expensive" ? "loot.expensiveStash" : "loot.cheapStash",
                    StringComparison.Ordinal)) && modified.ContainsKey(id))
           || StashMedicalParameterIds.Any(id =>
               IsStashMedicalSourceInTier(id, tier) && modified.ContainsKey(id))
           ;

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
            if (((tier == "Medium" && id == "loot.standardStashChanceScale")
                || (tier == "Expensive" && id == "loot.rareStashChanceScale")
                || id.StartsWith(tier == "Medium" ? "loot.mediumStash"
                    : tier == "Expensive" ? "loot.expensiveStash" : "loot.cheapStash",
                    StringComparison.Ordinal)) && modified.ContainsKey(id))
                written.Add(id);
        foreach (var id in StashMedicalParameterIds)
            if (IsStashMedicalSourceInTier(id, tier) && modified.ContainsKey(id))
                written.Add(id);

    }

}
