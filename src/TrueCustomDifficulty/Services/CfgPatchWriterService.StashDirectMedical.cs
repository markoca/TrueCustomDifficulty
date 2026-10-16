using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

// Exact ordinary WorldMap stash wrappers: six physical actors, ten direct
// canonical medical entries in addition to their generic tier and SmartLoot.
// All other authored weapon/equipment/booster/quest items stay untouched.
public sealed partial class CfgPatchWriterService
{
    private const string RegularStashDirectMedicalResource =
        "TrueCustomDifficulty.Data.regular-stash-direct-medical-entries.csv";
    private const string RegularStashDirectMedicalFile = "GameData/ItemGeneratorPrototypes.cfg";

    private sealed record RegularDirectMedicalRow(string ActorSid, string Root, string Sid,
        string Category, string Item, string ItemSid, string Chance, string Min, string Max);

    private static IReadOnlyList<RegularDirectMedicalRow> LoadRegularStashDirectMedical()
    {
        var rows = ReadStashCsv(RegularStashDirectMedicalResource, 9);
        var entries = rows.Select(r => new RegularDirectMedicalRow(
            r[0], r[1], r[2], r[3], r[4], r[5], r[6], r[7], r[8])).ToArray();
        var expected = new Dictionary<string, (string Root, int Count)>(StringComparer.Ordinal)
        {
            ["12AA126A43D685C7CD7179854E40C5E9"] = ("Rostok_", 1),
            ["717745A84684B392BF29D5B6C157B489"] =
                ("Wild_Island_AzimuthStation_StashGenerator1_", 4),
            ["89A6611B4CA53AFD52E20CBAC749B162"] =
                ("Garbage_Factory_StashGenerator3_", 1),
            ["A5C617CC4B4C04CC52DAAA86A6AAFDBF"] =
                ("Promzona_ProminCMDFactory_StashGenerator6_", 1),
            ["A781417E408DCEDA7F96D48D07C27D52"] =
                ("Garbage_HydroelectricStation_StashGenerator2_", 1),
            ["BFD606894964956318B5519609B326B0"] =
                ("Promzona_ArmyWarehouses_StashGenerator1_", 2)
        };
        var keyed = entries.Select(x => (x.ActorSid, x.Category, x.Item)).ToArray();
        if (entries.Length != 10 || keyed.Distinct().Count() != 10
            || entries.Select(x => x.ActorSid).Distinct(StringComparer.Ordinal).Count() != 6
            || entries.Any(e => !expected.TryGetValue(e.ActorSid, out var spec)
                || e.Root != spec.Root + e.ActorSid || e.Sid != e.Root
                || e.Category != "1"
                || e.ItemSid is not ("Bandage" or "Medkit" or "ArmyMedkit" or "EcoMedkit")
                || e.Chance != "1"
                || !int.TryParse(e.Min, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out var min) || min < 1
                || !int.TryParse(e.Max, NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out var max) || max < min)
            || expected.Any(pair =>
                entries.Count(x => x.ActorSid == pair.Key) != pair.Value.Count))
            throw new InvalidDataException("Regular stash direct medical source inventory drift.");

        // The direct additions must still belong to the exact 803 ordinary
        // WorldMap no-clue actors; never patch quest/unique generator wrappers.
        var ordinary = ReadStashCsv(SmartLootActorResource, 8);
        if (entries.Any(e => !ordinary.Any(a =>
            a[0] == e.ActorSid && a[2].Split(',', StringSplitOptions.TrimEntries)
                .Contains(e.Sid, StringComparer.Ordinal) && a[3] == "EmptyInherited")))
            throw new InvalidDataException("Non-ordinary stash entered direct medical roster.");
        return entries;
    }

    private static int WriteRegularStashDirectMedical(
        IReadOnlyDictionary<string, ParameterViewModel> modified,
        IReadOnlyDictionary<string, ParameterViewModel> all,
        IDictionary<string, PatchDocument> documents,
        ISet<string> written)
    {
        if (!StashMixParameterIds.Contains("loot.stashMixMedical", StringComparer.Ordinal))
            throw new InvalidDataException("Medical control catalog drift.");

        var relevant = new[]
        {
            "loot.stashMixMedical",
            "loot.stashMedicalBandageBias", "loot.stashMedicalMedkitBias",
            "loot.stashMedicalArmyMedkitBias",
            "loot.stashMedkitMinCount", "loot.stashMedkitMaxCount",
            "loot.stashArmyMedkitMinCount", "loot.stashArmyMedkitMaxCount"
        };
        if (!relevant.Any(modified.ContainsKey))
            return 0;

        PatchDocument? patch = null;
        var edits = 0;
        foreach (var entry in LoadRegularStashDirectMedical())
        {
            var counts = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ItemPrototypeSID"] = entry.ItemSid,
                ["MinCount"] = entry.Min,
                ["MaxCount"] = entry.Max
            };
            foreach (var (field, raw) in new[]
            {
                ("Chance", entry.Chance), ("MinCount", entry.Min), ("MaxCount", entry.Max)
            })
            {
                var updated = field == "Chance"
                    ? TuneStashSelectionField(entry.ItemSid, field, raw, all)
                    : TuneStashMedicalCountField(counts, field, raw, all);
                if (updated == raw)
                    continue;
                patch ??= GetDocument(documents, RegularStashDirectMedicalFile);
                patch.AddAssignment(entry.Root,
                    new[] { "ItemGenerator", $"[{entry.Category}]",
                            "PossibleItems", $"[{entry.Item}]" },
                    field, updated);
                edits++;
            }
        }
        if (edits != 0)
            foreach (var id in relevant)
                if (modified.ContainsKey(id))
                    written.Add(id);
        return edits;
    }
}
