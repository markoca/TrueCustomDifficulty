using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

// Adds the independently-authored StashPrototypes SmartLoot layer to the
// existing source-backed Stashes controls. Of 831 audited candidate
// WorldMap_WP actors, 28 carry named clue SIDs and are held back for review.
// Only 803 no-clue ordinary actors may be redirected. Unique, quest and
// independent ItemGeneratorSettings remain unchanged.
public sealed partial class CfgPatchWriterService
{
    private const string SmartLootSourceResource =
        "TrueCustomDifficulty.Data.smartloot-world-source-entries.csv";
    private const string SmartLootActorResource =
        "TrueCustomDifficulty.Data.smartloot-regular-world-actor-manifest.csv";
    private const string DlcSmartLootActorFile = "DLCGameData/DLC1/SpawnActorPrototypes.cfg";
    private const string DlcStashActorResource =
        "TrueCustomDifficulty.Data.dlc1-stash-eligible-actor-manifest.csv";

    private sealed record SmartStashItem(
        string Prototype, int Rank, string Group, int Set, int Item,
        string ItemSid, string Weight, string Chance,
        string MinCount, string MaxCount,
        string MinSpawnChance, string MaxSpawnChance, string ItemSetCount);

    private sealed record SmartStashActor(string ActorSid, string[] SourceSids, string ClueSid);

    private static IReadOnlyList<string[]> ReadStashCsv(string resource, int columns)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidDataException("Missing embedded stash SmartLoot manifest: " + resource);
        using var reader = new StreamReader(stream);
        if (reader.ReadLine() is null)
            throw new InvalidDataException("Missing stash manifest CSV header: " + resource);
        var result = new List<string[]>();
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.Length == 0) continue;
            var values = ParseStashCsvLine(line);
            if (values.Length != columns)
                throw new InvalidDataException(
                    $"Stash manifest {resource} has {values.Length} columns, expected {columns}");
            result.Add(values);
        }
        return result;
    }

    private static string[] ParseStashCsvLine(string line)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else quoted = !quoted;
            }
            else if (ch == ',' && !quoted)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else current.Append(ch);
        }
        if (quoted) throw new InvalidDataException("Unterminated quote in stash manifest");
        result.Add(current.ToString());
        return result.ToArray();
    }

    private static IReadOnlyList<SmartStashItem> LoadSmartStashItems()
    {
        var rows = ReadStashCsv(SmartLootSourceResource, 14);
        if (rows.Count != 991)
            throw new InvalidDataException($"Expected 991 world SmartLoot item rows, found {rows.Count}");
        var entries = rows.Select(x => new SmartStashItem(
            x[0], int.Parse(x[1], CultureInfo.InvariantCulture), x[3],
            int.Parse(x[4], CultureInfo.InvariantCulture),
            int.Parse(x[5], CultureInfo.InvariantCulture),
            x[6], x[7], x[8], x[9], x[10], x[11], x[12], x[13])).ToArray();
        if (entries.Select(x => x.Prototype).Distinct(StringComparer.Ordinal).Count() != 13
            || entries.Any(x => x.Rank is < 0 or > 3 || x.Set < 0 || x.Item < 0
                || x.Prototype.Contains("Corpse", StringComparison.Ordinal)
                || x.Prototype.StartsWith("NPC_", StringComparison.Ordinal)
                || x.ItemSid.Length == 0)
            || entries.Select(x => (x.Prototype, x.Rank, x.Group, x.Set, x.Item))
                .Distinct().Count() != 991)
            throw new InvalidDataException("World SmartLoot source structure changed");
        return entries;
    }

    private static IReadOnlyList<SmartStashActor> LoadSmartStashActors()
    {
        var rows = ReadStashCsv(SmartLootActorResource, 8);
        var actors = rows.Select(x => new SmartStashActor(x[0],
            x[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            x[3])).ToArray();
        if (actors.Length != 831
            || actors.Select(x => x.ActorSid).Distinct(StringComparer.Ordinal).Count() != 831
            || actors.Any(x => x.ActorSid.Length != 32 || x.SourceSids.Length == 0)
            || rows.Any(x => x[2].Contains("Blueprint", StringComparison.OrdinalIgnoreCase)
                || x[2].Contains("Unique", StringComparison.OrdinalIgnoreCase)
                || x[2].Contains("Quest", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Unsafe or unexpected world SmartLoot actor manifest");
        // A named stash clue can indicate an authored/quest-associated reward.
        // Protect those actors until individual actor/gameplay verification.
        var ordinary = actors.Where(x => x.ClueSid == "EmptyInherited").ToArray();
        if (ordinary.Length != 803)
            throw new InvalidDataException("Safe no-clue stash actor boundary drift: expected 803.");
        return ordinary;
    }

    // Read the independently audited DLC actor list. Do not infer DLC approval
    // from the mere presence of a generic generator on a quest/unique stash.
    private static readonly Lazy<IReadOnlyList<SmartStashActor>> DlcActors =
        new(LoadDlcSmartStashActors);

    private static IReadOnlyList<SmartStashActor> LoadDlcSmartStashActors()
    {
        var rows = ReadStashCsv(DlcStashActorResource, 4);
        var allowedGenerators = new HashSet<string>(StringComparer.Ordinal)
        {
            "", "GamePass_Stash_ItemGenerator_Cheap",
            "GamePass_Stash_ItemGenerator_CheapNoFresh",
            "GamePass_Stash_ItemGenerator_Common_Var1",
            "GamePass_Stash_ItemGenerator_Rare"
        };
        var actors = rows.Select(x => new SmartStashActor(x[0],
            x[1].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            x[3])).ToArray();
        if (actors.Length != 112
            || actors.Select(x => x.ActorSid).Distinct(StringComparer.Ordinal).Count() != 112
            || actors.Any(x => x.ActorSid.Length != 32 || !x.ActorSid.All(Uri.IsHexDigit)
                || x.SourceSids.Length == 0 || x.ClueSid != "EmptyInherited")
            || rows.Any(x => !allowedGenerators.Contains(x[2])
                || x[1].Contains("NPC_", StringComparison.Ordinal)
                || x[1].Contains("Corpse", StringComparison.Ordinal)
                || x[1].Contains("Unique", StringComparison.Ordinal)
                || x[1].Contains("Quest", StringComparison.Ordinal)))
            throw new InvalidDataException("Unexpected DLC1 ordinary stash allowlist");
        return actors;
    }

    private const string BaseProtectedActorFile = "GameData/SpawnActorPrototypes.cfg";
    private const string BaseProtectedActorResource =
        "TrueCustomDifficulty.Data.base-stash-protected-actor-manifest.csv";

    // All 106 protected WorldMap_WP ItemContainer roots from the exact base CFG:
    // 77 special-generator actors (including 5 named clues) and 29 other
    // named-clue actors. Do not redirect ANY ItemGeneratorSettings on them,
    // even if a tier manifest also references an otherwise ordinary loot pool.
    private static readonly Lazy<HashSet<string>> BaseProtectedStashActors =
        new(LoadBaseProtectedStashActors);

    private static HashSet<string> LoadBaseProtectedStashActors()
    {
        var entries = ReadStashCsv(BaseProtectedActorResource, 2);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var special = 0;
        var named = 0;
        var overlap = 0;
        foreach (var entry in entries)
        {
            if (entry[0].Length != 32 || !entry[0].All(Uri.IsHexDigit)
                || !ids.Add(entry[0]))
                throw new InvalidDataException("Invalid or duplicate protected base stash actor SID");
            switch (entry[1])
            {
                case "SPECIAL_GENERATOR": special++; break;
                case "NAMED_CLUE": named++; break;
                case "SPECIAL_GENERATOR_AND_NAMED_CLUE": overlap++; break;
                default: throw new InvalidDataException("Unrecognized base stash protection reason");
            }
        }
        if (ids.Count != 106 || special != 72 || named != 29 || overlap != 5
            || !ids.Contains("098E76F14ADD5CFF9C62508DB31013E3")
            || !ids.Contains("02E9BD8341141FEC43FBBB9B6E8F8891"))
            throw new InvalidDataException(
                "The exact 106-actor base world stash protection gate has drifted");
        return ids;
    }

    // This guard is shared by every Basic/Standard/Rare ItemGenerator writer.
    // It protects direct actor edits AND indirect wrapper generator paths,
    // identified by the physical_container_sid column in all three manifests.
    // SmartLoot independently uses its 803-actor positive allowlist.
    private static bool IsApprovedStashTierRedirect(
        string file, string actorSid, string physicalContainerSid)
        // Wrapper ItemGeneratorPrototypes.cfg paths may be tied to a protected
        // physical bag even when their prototype name is not an actor GUID.
        => !BaseProtectedStashActors.Value.Contains(physicalContainerSid)
           && (!file.Equals(BaseProtectedActorFile, StringComparison.OrdinalIgnoreCase)
               || !BaseProtectedStashActors.Value.Contains(actorSid))
           && (!file.Equals(DlcSmartLootActorFile, StringComparison.OrdinalIgnoreCase)
               || DlcActors.Value.Any(x => x.ActorSid == actorSid));

    private static string? SmartStashSourceId(string itemSid) => itemSid switch
    {
        "Bandage" or "Medkit" or "ArmyMedkit" or "Antirad" => "loot.stashMixMedical",
        "Bread" or "Sausage" or "CannedFood" => "loot.stashMixFood",
        "Water" or "Milk" or "Energetic" or "Vodka" or "Beer" => "loot.stashMixDrinks",
        "GrenadeRGD5" or "GrenadeF1" => "loot.stashMixGrenades",
        _ when itemSid.Length > 1 && itemSid[0] == 'A'
            && char.IsDigit(itemSid[1]) => "loot.stashMixAmmo",
        _ => null
    };

    private static double SmartStashBias(
        SmartStashItem item, IReadOnlyDictionary<string, ParameterViewModel> all)
    {
        var sourceId = SmartStashSourceId(item.ItemSid)
            ?? throw new InvalidDataException("Unclassified regular SmartLoot item: " + item.ItemSid);
        var value = StashTuningCurrent(all, sourceId, 1.0);
        var detailId = item.ItemSid switch
        {
            "Bandage" => "loot.stashMedicalBandageBias",
            "Medkit" => "loot.stashMedicalMedkitBias",
            "ArmyMedkit" => "loot.stashMedicalArmyMedkitBias",
            "Antirad" => "loot.stashMedicalAntiRadBias",
            _ => null
        };
        if (detailId is not null)
            value *= StashTuningCurrent(all, detailId, 1.0);
        if ((item.ItemSid is "Medkit" or "ArmyMedkit")
            && StashTuningCurrent(all, $"loot.stash{item.ItemSid}MaxCount", 0) < -0.5)
            return 0.0;
        return value;
    }

    private static PatchNode SmartStashGroupNode(PatchNode root, SmartStashItem item)
        => root.GetOrAddChild("ItemGenerators", useBpatch: true)
            .GetOrAddChild($"[{item.Rank}]", useBpatch: true)
            .GetOrAddChild("SmartLootParams", useBpatch: true)
            .GetOrAddChild(item.Group, useBpatch: true)
            .GetOrAddChild($"[{item.Set}]", useBpatch: true);

    private static int WriteWorldStashSmartLoot(
        IReadOnlyDictionary<string, ParameterViewModel> modified,
        IReadOnlyDictionary<string, ParameterViewModel> all,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents,
        ISet<string> written)
    {
        // Leave original world SmartLoot and all actors untouched at VANILLA.
        var relevant = StashMixParameterIds.Concat(StashMedicalParameterIds)
            .Where(modified.ContainsKey).ToArray();
        if (relevant.Length == 0)
            return 0;
        var source = LoadSmartStashItems();
        var actors = LoadSmartStashActors();
        var groups = source.GroupBy(x => x.Prototype, StringComparer.Ordinal).ToArray();
        var rewritten = new Dictionary<string, string>(StringComparer.Ordinal);
        var edits = 0;
        var doc = GetDocument(documents, "GameData/StashPrototypes.cfg");
        foreach (var group in groups)
        {
            var affected = group.Where(item =>
                Math.Abs(SmartStashBias(item, all) - 1.0) > 0.0000001
                || (item.ItemSid is "Medkit" or "ArmyMedkit")
                && (StashTuningCurrent(all, $"loot.stash{item.ItemSid}MinCount", 0) > 0
                    || StashTuningCurrent(all, $"loot.stash{item.ItemSid}MaxCount", 0) > 0))
                .ToArray();
            if (affected.Length == 0)
                continue;
            var target = "TCD_" + group.Key;
            rewritten.Add(group.Key, target);
            var root = doc.GetOrAddReferencedRoot(target, "../StashPrototypes.cfg", group.Key);
            root.SetAssignment("SID", target);
            edits++;

            // A source group has its own spawn-chance endpoints. Scale those
            // only if every item in that exact group has the same bias.
            // Never force an originally 0..0 MainLoot group to spawn.
            foreach (var category in group.GroupBy(x => (x.Rank, x.Group, x.Set)))
            {
                var items = category.ToArray();
                var biases = items.Select(item => SmartStashBias(item, all)).ToArray();
                var groupNode = SmartStashGroupNode(root, items[0]);
                if (biases.All(x => Math.Abs(x - biases[0]) < 0.0000001)
                    && Math.Abs(biases[0] - 1.0) > 0.0000001)
                {
                    var factor = biases[0];
                    foreach (var pair in new[]
                    {
                        (Name: "MinSpawnChance", Raw: items[0].MinSpawnChance),
                        (Name: "MaxSpawnChance", Raw: items[0].MaxSpawnChance)
                    })
                    {
                        if (string.IsNullOrWhiteSpace(pair.Raw)) continue;
                        groupNode.SetAssignment(pair.Name,
                            FormatLikeSource(Math.Clamp(ParseNumber(pair.Raw) * factor, 0.0, 1.0), pair.Raw));
                        edits++;
                    }
                    if (factor <= 0.0 && !string.IsNullOrWhiteSpace(items[0].ItemSetCount))
                    {
                        groupNode.SetAssignment("ItemSetCount", "0");
                        edits++;
                    }
                }
            }

            foreach (var item in affected)
            {
                var factor = SmartStashBias(item, all);
                var disabled = factor <= 0.0000001;
                var itemNode = SmartStashGroupNode(root, item)
                    .GetOrAddChild("Items", useBpatch: true)
                    .GetOrAddChild($"[{item.Item}]", useBpatch: true);
                foreach (var field in new[] { ("Weight", item.Weight), ("Chance", item.Chance) })
                {
                    if (field.Item2.Length == 0) continue;
                    var original = ParseNumber(field.Item2);
                    var tuned = disabled ? 0.0
                        : field.Item1 == "Chance"
                            ? Math.Clamp(original * factor, 0.0, 1.0)
                            : Math.Max(0.0, original * factor);
                    itemNode.SetAssignment(field.Item1, FormatLikeSource(tuned, field.Item2));
                    edits++;
                }
                if (disabled)
                {
                    // Source selection zero alone is not guaranteed to prevent
                    // engine fallback in an all-zero weighted group.
                    if (item.MinCount.Length != 0)
                    {
                        itemNode.SetAssignment("MinCount", "0");
                        edits++;
                    }
                    if (item.MaxCount.Length != 0)
                    {
                        itemNode.SetAssignment("MaxCount", "0");
                        edits++;
                    }
                }
                else if (item.ItemSid is "Medkit" or "ArmyMedkit")
                {
                    var limits = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["ItemPrototypeSID"] = item.ItemSid,
                        ["MinCount"] = item.MinCount,
                        ["MaxCount"] = item.MaxCount
                    };
                    foreach (var (fieldName, raw) in new[]
                        { ("MinCount", item.MinCount), ("MaxCount", item.MaxCount) })
                    {
                        if (raw.Length == 0) continue;
                        var tuned = TuneStashMedicalCountField(limits, fieldName, raw, all);
                        if (tuned == raw) continue;
                        itemNode.SetAssignment(fieldName, tuned);
                        edits++;
                    }
                }
            }
        }
        if (rewritten.Count == 0) return 0;

        var actorSources = new List<(string File, IReadOnlyList<SmartStashActor> Actors)>
        {
            ("GameData/SpawnActorPrototypes.cfg", actors)
        };
        if (hasDlc1)
            actorSources.Add((DlcSmartLootActorFile, DlcActors.Value));
        foreach (var (file, sourceActors) in actorSources)
        {
            var actorDoc = GetDocument(documents, file);
            foreach (var actor in sourceActors)
            {
                var mapped = actor.SourceSids.Select(x =>
                    rewritten.TryGetValue(x, out var replacement) ? replacement : x).ToArray();
                if (actor.SourceSids.SequenceEqual(mapped, StringComparer.Ordinal))
                    continue;
                actorDoc.AddAssignment(actor.ActorSid, Array.Empty<string>(),
                    "StashPrototypeSID", string.Join(",", mapped));
                edits++;
            }
        }

        foreach (var id in relevant)
            written.Add(id);
        return edits;
    }
}