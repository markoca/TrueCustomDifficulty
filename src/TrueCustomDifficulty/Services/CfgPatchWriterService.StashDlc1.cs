using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TrueCustomDifficulty.ViewModels;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.Services;

// DLC1-only source-native clone of a separately authored ordinary CheapNoFresh
// stash pool. The six audited actors are the ONLY consumers redirected here;
// quest, clue, CNPP and named unique generator fields are never rewritten.
public sealed partial class CfgPatchWriterService
{
    private const string CheapNoFreshSid = "GamePass_Stash_ItemGenerator_CheapNoFresh";
    private const string TcdCheapNoFreshSid = "TCD_GamePass_Stash_ItemGenerator_CheapNoFresh";
    private const string CheapNoFreshItemResource =
        "TrueCustomDifficulty.Data.cheap-no-fresh-stash-item-entries.csv";

    private sealed record CheapNoFreshItem(
        int Category, int Item, string Sid, string Chance, string Weight,
        string MinCount, string MaxCount);

    private static IReadOnlyList<CheapNoFreshItem> LoadCheapNoFreshItems()
    {
        var rows = ReadStashCsv(CheapNoFreshItemResource, 7);
        var entries = rows.Select(x => new CheapNoFreshItem(
            int.Parse(x[0], CultureInfo.InvariantCulture),
            int.Parse(x[1], CultureInfo.InvariantCulture),
            x[2], x[3], x[4], x[5], x[6])).ToArray();
        if (entries.Length != 35 ||
            entries.Select(x => (x.Category, x.Item)).Distinct().Count() != 35 ||
            entries.Any(x => x.Category is < 0 or > 11 || x.Item is < 0 or > 5 ||
                x.Sid.Length == 0 || StashItemBiasId(x.Sid) is null ||
                (x.Chance.Length == 0 && x.Weight.Length == 0) ||
                !int.TryParse(x.MinCount, out _) || !int.TryParse(x.MaxCount, out _)))
            throw new InvalidDataException("DLC1 CheapNoFresh source inventory changed.");
        return entries;
    }

    private static int WriteDlcCheapNoFreshStashes(
        CfgWriterBaseline baseline,
        IReadOnlyDictionary<string, ParameterViewModel> modified,
        IReadOnlyDictionary<string, ParameterViewModel> all,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> written)
    {
        if (!hasDlc1)
            return 0;
        var ammoId = baseline.Special.CheapStashProduction.AmmoQuantityParameterId;
        var grenadeChanceId = baseline.Special.CheapStashProduction.GrenadeChanceParameterId;
        var grenadeAmountId = baseline.Special.CheapStashProduction.GrenadeQuantityParameterId;
        var relevant = StashMixParameterIds.Concat(StashMedicalParameterIds)
            .Concat(new[] { ammoId, grenadeChanceId, grenadeAmountId,
                "loot.stashGearCheapWeapon", "loot.stashGearCheapArmor" })
            .Where(modified.ContainsKey).ToArray();
        if (relevant.Length == 0)
            return 0;

        var actors = DlcActors.Value.Where(x =>
            x.ActorSid.Length == 32 && CheapNoFreshActorIds.Contains(x.ActorSid))
            .ToArray();
        if (actors.Length != 6)
            throw new InvalidDataException("Expected six safe DLC1 CheapNoFresh actors.");
        var actorRows = ReadStashCsv(DlcStashActorResource, 4);
        if (actorRows.Count(x => x[2] == CheapNoFreshSid) != 6 ||
            actors.Any(x => !actorRows.Any(r => r[0] == x.ActorSid
                && r[2] == CheapNoFreshSid)))
            throw new InvalidDataException("DLC1 CheapNoFresh actor generator scope changed.");

        var ammoChanged = modified.ContainsKey(ammoId);
        var grenadeChanceChanged = modified.ContainsKey(grenadeChanceId);
        var grenadeAmountChanged = modified.ContainsKey(grenadeAmountId);
        var cheapConfig = baseline.Special.CheapStashProduction;
        var sourceGrenadeChance = ParseNumber(cheapConfig.GrenadeEntry.SourceChanceRaw);
        if (sourceGrenadeChance <= 0)
            throw new InvalidDataException("Missing Cheap stash original RGD-5 chance.");
        var grenadeFactor = StashTuningCurrent(all, grenadeChanceId,
            sourceGrenadeChance) / sourceGrenadeChance;
        var grenadeCount = (int)Math.Round(StashTuningCurrent(all,
            grenadeAmountId, cheapConfig.GrenadeEntry.SourceMinCount),
            MidpointRounding.AwayFromZero);

        // Compare against exact authored rows first, to avoid emitting a DLC
        // clone for a value that affects only other stash tiers.
        var patch = new List<(int Category, int Item, string Field, string Value)>();
        foreach (var entry in LoadCheapNoFreshItems())
        {
            var original = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ItemPrototypeSID"] = entry.Sid,
                ["MinCount"] = entry.MinCount,
                ["MaxCount"] = entry.MaxCount
            };
            var categoryId = StashItemBiasId(entry.Sid);
            var isAmmo = entry.Sid.Length > 1 && entry.Sid[0] == 'A'
                && char.IsDigit(entry.Sid[1]);
            var isGrenade = entry.Sid == "GrenadeRGD5";
            var sourceBiasZero = categoryId != null
                && StashTuningCurrent(all, categoryId, 1.0) <= 0.0000001;
            var forcedZero = sourceBiasZero
                || IsStashMedicalSelectionDisabled(entry.Sid, all)
                || (isGrenade && (grenadeChanceChanged && grenadeFactor <= 0
                    || grenadeAmountChanged && grenadeCount == 0));
            foreach (var (field, raw) in new[]
            {
                ("Chance", entry.Chance), ("Weight", entry.Weight),
                ("MinCount", entry.MinCount), ("MaxCount", entry.MaxCount)
            })
            {
                if (raw.Length == 0) continue;
                var tuned = raw;
                if (field is "Chance" or "Weight")
                {
                    if (isGrenade && grenadeChanceChanged && field == "Chance")
                        tuned = FormatLikeSource(Math.Clamp(
                            ParseNumber(raw) * grenadeFactor, 0.0, 1.0), raw);
                    tuned = TuneStashSelectionField(entry.Sid, field, tuned, all);
                    if (forcedZero)
                        tuned = FormatLikeSource(0, raw);
                }
                else if (forcedZero)
                    tuned = "0";
                else if (isAmmo && ammoChanged)
                    tuned = ScalePositiveInteger(int.Parse(raw, CultureInfo.InvariantCulture),
                        StashTuningCurrent(all, ammoId, 1.0))
                        .ToString(CultureInfo.InvariantCulture);
                else if (isGrenade && grenadeAmountChanged)
                    tuned = grenadeCount.ToString(CultureInfo.InvariantCulture);
                else
                    tuned = TuneStashMedicalCountField(original, field, raw, all);
                if (tuned != raw)
                    patch.Add((entry.Category, entry.Item, field, tuned));
            }
        }

        var hasGear = StashTuningCurrent(all, "loot.stashGearCheapWeapon", 0) > 0
            || StashTuningCurrent(all, "loot.stashGearCheapArmor", 0) > 0;
        if (patch.Count == 0 && !hasGear)
            return 0;

        var doc = GetDocument(documents, "GameData/ItemGeneratorPrototypes.cfg");
        var root = doc.GetOrAddReferencedRoot(TcdCheapNoFreshSid,
            "../ItemGeneratorPrototypes.cfg", CheapNoFreshSid);
        root.SetAssignment("SID", TcdCheapNoFreshSid);
        var items = root.GetOrAddChild("ItemGenerator", useBpatch: true);
        var edits = 1;
        foreach (var (category, item, field, value) in patch)
        {
            items.GetOrAddChild($"[{category}]", useBpatch: true)
                .GetOrAddChild("PossibleItems", useBpatch: true)
                .GetOrAddChild($"[{item}]", useBpatch: true)
                .SetAssignment(field, value);
            edits++;
        }
        edits += WriteExtraStashGear(items, all, "Cheap", firstFreeCategory: 12);

        var actorsDoc = GetDocument(documents, DlcSmartLootActorFile);
        foreach (var actor in actors)
        {
            actorsDoc.AddAssignment(actor.ActorSid,
                new[] { "ItemGeneratorSettings", "[0]" },
                "PrototypeSID", TcdCheapNoFreshSid);
            edits++;
        }
        foreach (var id in relevant)
            written.Add(id);
        return edits;
    }

    private static readonly HashSet<string> CheapNoFreshActorIds =
        new(StringComparer.Ordinal)
        {
            "19EB12D645F5460C36D417AA5A224259",
            "4DA6EC814DF17F44E9DF15B54A3B4F35",
            "8F5078E346A2F3D27B238CAF2D72650C",
            "9C2717884C046D315A65DA88E49F310C",
            "F4AF1C714AB9FC51031FF1B0DD87110A",
            "F943F3C84EB1391403756181AF2AB692"
        };
}
