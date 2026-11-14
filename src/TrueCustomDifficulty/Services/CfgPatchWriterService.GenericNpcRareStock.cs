using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Isolated 2.1 generic-NPC merchant stock experiment. Only nine base-game
/// TradePrototypes are relinked to private inherited trader stock generators.
/// Never edits human loadouts, corpse overrides or SmartLoot.
/// NO normal-build changes: TCD_GENERIC_NPC_RARE_STOCK=1 (2%) or test (100%).
/// </summary>
public sealed partial class CfgPatchWriterService
{
    private const string GenericRareEnvironmentKey = "TCD_GENERIC_NPC_RARE_STOCK";
    private const double GenericRareNormalChance = 0.02;
    private const double GenericRareSecondChoiceChance = 0.08;

    private const string RareMasterSid = "TCD_GNRS_RARE_MASTER";
    private const string RarePrimarySid = "TCD_GNRS_PRIMARY";
    private const string RareSecondarySid = "TCD_GNRS_SECONDARY";
    private const string RareBrokenPrimarySid = "TCD_GNRS_BROKEN_PRIMARY";
    private const string RareBrokenPistolSid = "TCD_GNRS_BROKEN_PISTOL";
    private const string RareBetterPrimarySid = "TCD_GNRS_BETTER_PRIMARY";
    private const string RareArtifactSid = "TCD_GNRS_ARTIFACTS";
    private const string RareAttachmentSid = "TCD_GNRS_ATTACHMENTS";
    private const string RareSecondArtifactSid = "TCD_GNRS_SECOND_ARTIFACT";
    private const string RareSecondDetectorSid = "TCD_GNRS_SECOND_DETECTOR";
    private const string RareSecondWeaponSid = "TCD_GNRS_SECOND_WEAPON";

    // Exact (TradePrototype, root stock generator, next FREE group index)
    // verified against the original 2.1 GameData CFG. No named trader, quest,
    // technician, test/template, DLC1, or other player stock paths are touched.
    private static readonly (string Trade, string Root, int AppendGroup)[] GenericRareProfiles =
    {
        ("GeneralNPC_TradePrototype", "GeneralNPC_TradeItemGenerator", 2),
        ("GeneralNPC_TradePrototype_Bandit", "GeneralNPC_TradeItemGenerator_Bandit", 2),
        ("GeneralNPC_TradePrototype_Corpus", "GeneralNPC_TradeItemGenerator_Corpus", 2),
        ("GeneralNPC_TradePrototype_Duty", "GeneralNPC_TradeItemGenerator_Duty", 2),
        ("GeneralNPC_TradePrototype_Freedom", "GeneralNPC_TradeItemGenerator_Freedom", 2),
        ("GeneralNPC_TradePrototype_Mercenary", "GeneralNPC_TradeItemGenerator_Mercenary", 2),
        ("GeneralNPC_TradePrototype_Militaries", "GeneralNPC_TradeItemGenerator_Militaries", 2),
        ("GeneralNPC_TradePrototype_Scientists", "GeneralNPC_TradeItemGenerator_Scientists", 1),
        ("GeneralNPC_TradePrototype_Spark", "GeneralNPC_TradeItemGenerator_Spark", 1)
    };

    private static PatchNode NewRareRoot(PatchDocument doc, string sid)
    {
        var root = doc.AddPlainRoot(sid);
        root.SetAssignment("SID", sid);
        return root;
    }

    private static PatchNode RareGroup(PatchNode root, int index, string category)
    {
        var group = root.GetOrAddChild("ItemGenerator", useBpatch: false)
            .GetOrAddChild("[" + index.ToString(CultureInfo.InvariantCulture) + "]", useBpatch: false);
        group.SetAssignment("Category", "EItemGenerationCategory::" + category);
        return group.GetOrAddChild("PossibleItems", useBpatch: false);
    }

    private static void RareReference(PatchNode possibles, int index, string sid,
        string field, string value)
    {
        var item = possibles.GetOrAddChild(
            "[" + index.ToString(CultureInfo.InvariantCulture) + "]", useBpatch: false);
        item.SetAssignment("ItemGeneratorPrototypeSID", sid);
        item.SetAssignment(field, value);
    }

    private static void RareItemPool(PatchDocument doc, string sid, string category,
        string? minDurability, string? maxDurability, params string[] items)
    {
        var possibles = RareGroup(NewRareRoot(doc, sid), 0, category);
        for (var i = 0; i < items.Length; i++)
        {
            var item = possibles.GetOrAddChild(
                "[" + i.ToString(CultureInfo.InvariantCulture) + "]", useBpatch: false);
            item.SetAssignment("ItemPrototypeSID", items[i]);
            item.SetAssignment("Weight", "1");
            item.SetAssignment("MinCount", "1");
            item.SetAssignment("MaxCount", "1");
            if (minDurability is not null && maxDurability is not null)
            {
                item.SetAssignment("MinDurability", minDurability);
                item.SetAssignment("MaxDurability", maxDurability);
            }
        }
    }

    private static void RareWeightedSelector(PatchDocument doc, string sid,
        params (string Pool, int Weight)[] pools)
    {
        var possibles = RareGroup(NewRareRoot(doc, sid), 0, "SubItemGenerator");
        for (var i = 0; i < pools.Length; i++)
            RareReference(possibles, i, pools[i].Pool, "Weight",
                pools[i].Weight.ToString(CultureInfo.InvariantCulture));
    }

    private static int WriteGenericNpcRareTraderStock(
        IDictionary<string, PatchDocument> documents)
    {
        var mode = Environment.GetEnvironmentVariable(GenericRareEnvironmentKey)?.Trim();
        if (string.IsNullOrEmpty(mode) || mode.Equals("0", StringComparison.Ordinal))
            return 0;
        var primaryChance = mode.Equals("test", StringComparison.OrdinalIgnoreCase)
            ? 1.0 : mode.Equals("1", StringComparison.Ordinal)
                ? GenericRareNormalChance
                : throw new InvalidDataException(
                    GenericRareEnvironmentKey + " must be unset, 0, 1 or test.");
        var source = LoadTraderSellOffers()
            .Where(x => x.TraderType == "GENERIC_NPC_TRADER")
            .GroupBy(x => (x.TradeScope, x.TradeSid))
            .ToArray();
        if (source.Length != GenericRareProfiles.Length
            || GenericRareProfiles.Any(p => !source.Any(g =>
                g.Key.TradeScope == "base" && g.Key.TradeSid == p.Trade
                && g.All(x => x.RootSid == p.Root && x.RootGeneratorScope == "base"
                    && x.TradeGeneratorSlot == 0))))
            throw new InvalidDataException(
                "Generic NPC rare stock requires exactly nine verified 2.1 trader roots.");

        var generator = GetDocument(documents, "GameData/ItemGeneratorPrototypes.cfg");
        var trade = GetDocument(documents, "GameData/TradePrototypes.cfg");
        var edits = 0;
        foreach (var p in GenericRareProfiles)
        {
            // Reuse the exact clone identity from the ordinary category OFF
            // writer, so a rare offer and category removal compose safely.
            var clone = SellCloneSid("base", p.Trade, 0, p.Root);
            var root = generator.GetOrAddReferencedRoot(
                clone, "../ItemGeneratorPrototypes.cfg", p.Root);
            root.SetAssignment("SID", clone);
            var group = root.GetOrAddChild("ItemGenerator", useBpatch: true)
                .GetOrAddChild(
                    "[" + p.AppendGroup.ToString(CultureInfo.InvariantCulture) + "]",
                    useBpatch: false);
            group.SetAssignment("Category", "EItemGenerationCategory::SubItemGenerator");
            var possible = group.GetOrAddChild("PossibleItems", useBpatch: false)
                .GetOrAddChild("[0]", useBpatch: false);
            possible.SetAssignment("ItemGeneratorPrototypeSID", RareMasterSid);
            possible.SetAssignment("Chance", primaryChance.ToString("0.##", CultureInfo.InvariantCulture));

            trade.AddAssignment(p.Trade, new[] { "TradeGenerators", "[0]" },
                "ItemGeneratorPrototypeSID", clone);
            edits += 3;
        }

        // Two independent source-native Chance rolls INSIDE the activated
        // rare master: first always; second 8%. First slot uses one weighted
        // subgenerator and then one weighted item. The second selector uses
        // disjoint SIDs, so two successful slots cannot choose the same item.
        var master = RareGroup(NewRareRoot(generator, RareMasterSid), 0, "SubItemGenerator");
        RareReference(master, 0, RarePrimarySid, "Chance", "1");
        RareReference(master, 1, RareSecondarySid, "Chance",
            GenericRareSecondChoiceChance.ToString("0.##", CultureInfo.InvariantCulture));

        // 74% of initial rare picks are broken weapons; just 8% are
        // >40% condition weapons. The remainder are real anomaly artifacts
        // or weapon attachments; these weights describe the rare-slot mix,
        // NOT independent per-item spawn percentages.
        RareWeightedSelector(generator, RarePrimarySid,
            (RareBrokenPrimarySid, 58), (RareBrokenPistolSid, 16),
            (RareBetterPrimarySid, 8), (RareArtifactSid, 14),
            (RareAttachmentSid, 4));
        RareWeightedSelector(generator, RareSecondarySid,
            (RareSecondArtifactSid, 50), (RareSecondDetectorSid, 25),
            (RareSecondWeaponSid, 25));

        // Original 2.1 ItemPrototypes and other traders independently confirm
        // all these regular, non-quest SIDs. Keep ItemGenerationCategory native
        // to each type; do not mislabel real artifacts as grenades.
        RareItemPool(generator, RareBrokenPrimarySid, "WeaponPrimary",
            "0.02", "0.17", "GunObrez_SG", "GunViper_PP", "GunTOZ_SG", "GunAK74_ST");
        RareItemPool(generator, RareBrokenPistolSid, "WeaponPistol",
            "0.02", "0.17", "GunPM_HG");
        RareItemPool(generator, RareBetterPrimarySid, "WeaponPrimary",
            "0.42", "0.68", "GunM860_SG");
        RareItemPool(generator, RareArtifactSid, "Artifact", null, null,
            "CArtifactThorn", "CArtifactSlug", "GArtifactBloodStone", "GArtifactGravy");
        RareItemPool(generator, RareAttachmentSid, "Attach", null, null,
            "RU_Silen_1", "RU_ColimScope_1");
        RareItemPool(generator, RareSecondArtifactSid, "Artifact", null, null,
            "EArtifactBattery");
        RareItemPool(generator, RareSecondDetectorSid, "Detector", null, null,
            "Bear");
        RareItemPool(generator, RareSecondWeaponSid, "WeaponPrimary",
            "0.42", "0.68", "GunAKU_PP");

        return edits + 11;
    }
}
