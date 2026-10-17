using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

// Ordinary base-world ItemContainers at Richter's post-prologue Cabin have
// TWO additional explicit Medkit entries, each Chance=1.0 and count=1.
// Unlike the normal Cheap core/SmartLoot source, these are authored directly
// in two location-specific ItemGenerator wrapper categories. Tune ONLY their
// literal medical fields; preserve the wrappers' normal Cheap subgenerator,
// other loot, source SID spelling, quest state and actor assignments.
public sealed partial class CfgPatchWriterService
{
    private const string CabinMedicalResource =
        "TrueCustomDifficulty.Data.cabin-direct-medkit-item-entries.csv";
    private const string CabinMedicalFile = "GameData/ItemGeneratorPrototypes.cfg";

    private sealed record CabinMedicalEntry(
        string ActorSid, string Root, string GeneratorSid, string Category,
        string Item, string ItemSid, string Chance, string MinCount,
        string MaxCount);

    private static IReadOnlyList<CabinMedicalEntry> LoadCabinMedicalEntries()
    {
        var rows = ReadStashCsv(CabinMedicalResource, 9);
        var entries = rows.Select(r => new CabinMedicalEntry(
            r[0], r[1], r[2], r[3], r[4], r[5], r[6], r[7], r[8])).ToArray();
        var expected = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["1C2FD1C141D6DA11E518758CC1CD36AE"] =
                "LesserZone_Cabin_StashGenerator1_1C2FD1C141D6DA11E518758CC1CD36AE",
            ["59FDC1CD4D0D094270C695A18D84373A"] =
                "LesserZone_Cabin_StashGenerator3_59FDC1CD4D0D094270C695A18D84373A"
        };
        if (entries.Length != 2
            || entries.Select(e => e.ActorSid).Distinct(StringComparer.Ordinal).Count() != 2
            || entries.Any(e => !expected.TryGetValue(e.ActorSid, out var root)
                || e.Root != root
                || e.GeneratorSid != root.Replace("Cabin_", "Cabin _", StringComparison.Ordinal)
                || e.Category != "1" || e.Item != "0"
                || e.ItemSid != "Medkit"
                || e.Chance != "1.0" || e.MinCount != "1" || e.MaxCount != "1"))
            throw new InvalidDataException("Cabin direct Medkit source inventory drift.");

        // Both full ActorSIDs must still belong to the audited 803 no-clue
        // base-game regular SmartLoot allowlist. Do not leak into quest stashes.
        var ordinary = ReadStashCsv(SmartLootActorResource, 8);
        if (entries.Any(e => BaseProtectedStashActors.Value.Contains(e.ActorSid)
            || !ordinary.Any(x =>
                x[0] == e.ActorSid && x[2] == e.GeneratorSid
                && x[3] == "EmptyInherited")))
            throw new InvalidDataException("Cabin Medkit actor is no longer an ordinary base stash.");
        return entries;
    }

    private static int WriteCabinDirectMedkits(
        IReadOnlyDictionary<string, ParameterViewModel> modified,
        IReadOnlyDictionary<string, ParameterViewModel> all,
        IDictionary<string, PatchDocument> documents,
        ISet<string> written)
    {
        var medicalIds = new[]
        {
            "loot.stashMixMedical", "loot.stashMedicalMedkitBias",
            "loot.stashMedkitMinCount", "loot.stashMedkitMaxCount"
        };
        if (!medicalIds.Any(modified.ContainsKey))
            return 0;

        PatchDocument? document = null;
        var edits = 0;
        foreach (var entry in LoadCabinMedicalEntries())
        {
            var original = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["ItemPrototypeSID"] = entry.ItemSid,
                ["MinCount"] = entry.MinCount,
                ["MaxCount"] = entry.MaxCount
            };
            foreach (var (field, value) in new[]
            {
                ("Chance", entry.Chance), ("MinCount", entry.MinCount),
                ("MaxCount", entry.MaxCount)
            })
            {
                var updated = field == "Chance"
                    ? TuneStashSelectionField(entry.ItemSid, field, value, all)
                    : TuneStashMedicalCountField(original, field, value, all);
                if (updated == value)
                    continue;
                document ??= GetDocument(documents, CabinMedicalFile);
                document.AddAssignment(entry.Root,
                    new[] { "ItemGenerator", $"[{entry.Category}]",
                        "PossibleItems", $"[{entry.Item}]" },
                    field, updated);
                edits++;
            }
        }

        if (edits > 0)
            foreach (var id in medicalIds)
                if (modified.ContainsKey(id))
                    written.Add(id);
        return edits;
    }
}
