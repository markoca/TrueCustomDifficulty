using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace LootSpecialGeneratorsContract;

internal static partial class Program
{
    private sealed record SourceStashGearActor(
        string Sid, string Scope, string RankCodes, int OriginalSlotCount);

    private static IReadOnlyList<SourceStashGearActor> SourceStashGearActors()
    {
        using var stream = typeof(CfgPatchWriterService).Assembly.GetManifestResourceStream(
            "TrueCustomDifficulty.Data.stash-gear-actor-ranks.csv")
            ?? throw new InvalidDataException("Embedded stash gear rank inventory missing");
        using var reader = new StreamReader(stream);
        var actors = reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1).Select(line => line.TrimEnd('\r').Split(','))
            .Select(row => new SourceStashGearActor(row[0], row[1], row[2],
                int.Parse(row[3]))).ToArray();
        Check(actors.Length == 915
            && actors.Select(a => a.Sid).Distinct(StringComparer.Ordinal).Count() == 915
            && actors.Count(a => a.Scope == "base" && a.OriginalSlotCount > 0
                && a.Sid != "1CBBBE27428A1766B7F911863C044833") == 800
            && actors.Count(a => a.Scope == "dlc1" && a.OriginalSlotCount > 0) == 108,
            "Independent physical stash rank inventory drift");
        return actors;
    }

    private static HashSet<string> SelectedSourceStashes(
        IEnumerable<SourceStashGearActor> source, string scope, int percent)
    {
        var ordered = source.Where(a => a.Scope == scope && a.OriginalSlotCount > 0
                && a.Sid != "1CBBBE27428A1766B7F911863C044833")
            .OrderBy(a => BinaryPrimitives.ReadUInt64BigEndian(SHA256.HashData(
                Encoding.UTF8.GetBytes("TCD-StashGear-2026-10-18|ACTOR|"
                    + a.Scope + "|" + a.Sid))))
            .ThenBy(a => a.Sid, StringComparer.Ordinal).ToArray();
        return ordered.Take((ordered.Length * percent + 50) / 100)
            .Select(a => a.Sid).ToHashSet(StringComparer.Ordinal);
    }

    private static Dictionary<string, string> ExpectedSourceGearLinks(
        IEnumerable<SourceStashGearActor> source, HashSet<string> selected)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var actor in source.Where(a => selected.Contains(a.Sid)))
        {
            var ranks = actor.RankCodes == "A" ? "NEVM" : actor.RankCodes;
            Check(ranks.Length > 0 && ranks.All(c => "NEVM".Contains(c)),
                "Unknown source rank for actor " + actor.Sid);
            for (var rankIndex = 0; rankIndex < ranks.Length; rankIndex++)
            {
                var slotIndex = actor.RankCodes == "A" ? 1 : actor.OriginalSlotCount;
                var key = actor.Sid + "/ItemGeneratorSettings/[" + rankIndex
                    + "]/ItemGenerators/[" + slotIndex + "]/PrototypeSID";
                Check(result.TryAdd(key,
                    "TCD_ExtraGear_" + actor.Sid + "_" + ranks[rankIndex]),
                    "Duplicate source actor/rank slot key: " + key);
            }
        }
        return result;
    }

    private static Dictionary<string, string> ParseGearActorLinks(string text,
        IReadOnlyDictionary<string, string> expected, string scope)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        var stack = new List<string>();
        foreach (var raw in text.Replace("\r", "").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
                continue;
            if (line == "struct.end")
            {
                Check(stack.Count != 0, scope + ": unbalanced native actor CFG");
                stack.RemoveAt(stack.Count - 1);
                continue;
            }
            var open = Regex.Match(line, @"^(?<node>.+?)\s*:\s*struct\.begin");
            if (open.Success)
            {
                stack.Add(open.Groups["node"].Value);
                if (stack.Count == 5 && stack[1] == "ItemGeneratorSettings"
                    && Regex.IsMatch(stack[2], @"^\[\d+\]$")
                    && stack[3] == "ItemGenerators"
                    && Regex.IsMatch(stack[4], @"^\[\d+\]$"))
                {
                    var key = string.Join("/", stack) + "/PrototypeSID";
                    Check(expected.ContainsKey(key),
                        scope + ": original or unexpected stash actor generator slot touched: " + key);
                }
                continue;
            }
            if (stack.Count == 5 && stack[1] == "ItemGeneratorSettings"
                && stack[3] == "ItemGenerators")
            {
                var field = Regex.Match(line, @"^(?<key>\S+)\s*=\s*(?<value>\S+)\s*$");
                Check(field.Success && field.Groups["key"].Value == "PrototypeSID",
                    scope + ": unexpected edit inside actor's original rank slots: " + line);
                var key = string.Join("/", stack) + "/PrototypeSID";
                Check(result.TryAdd(key, field.Groups["value"].Value),
                    scope + ": duplicate bonus slot assignment: " + key);
            }
        }
        Check(stack.Count == 0, scope + ": incomplete native stash actor CFG");
        Check(result.Count == expected.Count
            && result.All(pair => expected.TryGetValue(pair.Key, out var val)
                && val == pair.Value),
            scope + ": emitted actor/rank/slot links differ from independently selected source");
        return result;
    }

    private static void VerifyExactGearCfgLinks(int percent, bool withDlc,
        string baseActorText, string dlcActorText, string itemText)
    {
        var actors = SourceStashGearActors();
        var chosenBase = SelectedSourceStashes(actors, "base", percent);
        var chosenDlc = withDlc
            ? SelectedSourceStashes(actors, "dlc1", percent)
            : new HashSet<string>(StringComparer.Ordinal);
        var expectedBase = ExpectedSourceGearLinks(
            actors.Where(a => a.Scope == "base"), chosenBase);
        var expectedDlc = ExpectedSourceGearLinks(
            actors.Where(a => a.Scope == "dlc1"), chosenDlc);
        ParseGearActorLinks(baseActorText, expectedBase, "base");
        ParseGearActorLinks(dlcActorText, expectedDlc, "dlc1");
        var all = expectedBase.Values.Concat(expectedDlc.Values)
            .ToHashSet(StringComparer.Ordinal);
        Check(all.Count == expectedBase.Count + expectedDlc.Count,
            "Private stash rank generator name collision");
        var roots = Regex.Matches(itemText,
            @"(?m)^(TCD_ExtraGear_[0-9A-F]{32}_[NEVM])\s*:\s*struct\.begin")
            .Select(m => m.Groups[1].Value).ToArray();
        Check(roots.Length == all.Count
            && roots.Distinct(StringComparer.Ordinal).Count() == roots.Length
            && roots.ToHashSet(StringComparer.Ordinal).SetEquals(all),
            "Orphan, duplicated or absent private gear generator root");
        foreach (var root in roots)
        {
            var match = Regex.Match(itemText,
                @"(?ms)^" + Regex.Escape(root)
                    + @"\s*:\s*struct\.begin[^\n]*\n(?<body>.*?)(?=^[^\s\r\n].*?:\s*struct\.begin|\z)");
            Check(match.Success && Regex.IsMatch(match.Groups["body"].Value,
                @"(?m)^\s*SID\s*=\s*" + Regex.Escape(root) + @"\s*$"),
                "Missing or mismatched private generator SID assignment: " + root);
        }
    }

    private static void VerifyEmptyStashGearMix()
    {
        var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
        vm.AllParameters.Single(p => p.Id == "loot.stashGearEnabled").CurrentBoolean = true;
        vm.AllParameters.Single(p => p.Id == "loot.stashGearCoveragePercent")
            .SnapCurrentNumberToMarker(100);
        foreach (var part in new[] { "Weapon", "Armor", "Artifact", "Attachment" })
            vm.AllParameters.Single(p => p.Id == "loot.stashGear" + part + "Weight")
                .SnapCurrentNumberToMarker(0);
        var dir = Path.Combine(Keep ?? Path.GetTempPath(), "TCD_Stash_Gear_Empty_Mix");
        try
        {
            new CfgPatchWriterService().Write(vm, Catalog,
                new GameInstallInfo { HasDlc1 = true }, dir);
            foreach (var file in new[] { "GameData/SpawnActorPrototypes.cfg",
                         "DLCGameData/DLC1/SpawnActorPrototypes.cfg" })
            {
                var path = OutputPath(dir, file);
                Check(!File.Exists(path) || !File.ReadAllText(path).Contains(
                        "TCD_ExtraGear_", StringComparison.Ordinal),
                    "All-zero Bonus Mix linked a private generator: " + file);
            }
            var itemText = File.ReadAllText(OutputPath(dir,
                "GameData/ItemGeneratorPrototypes.cfg"));
            Check(!itemText.Contains("TCD_ExtraGear_", StringComparison.Ordinal)
                && Regex.Matches(itemText,
                    @"(?m)^\s*\[0\]\s*:\s*removenode\s*$").Count == 13,
                "All-zero Bonus Mix must emit zero bonuses and retain 13 authorized removals");
        }
        finally
        {
            if (Keep is null && Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }

    // World Attachment Shuffle and Extra Stash Gear legitimately share
    // 44 ordinary base-game physical stash actors. The former edits existing
    // generator slot references; the latter must APPEND in a distinct slot.
    // Validate the actual merged production CFG, not only a manifest set.
    private static void VerifySharedAttachmentAndGearActorSlots()
    {
        using var stream = typeof(CfgPatchWriterService).Assembly.GetManifestResourceStream(
            "TrueCustomDifficulty.Data.world-attachment-generator-manifest.csv")
            ?? throw new InvalidDataException("Embedded attachment generator manifest missing");
        using var reader = new StreamReader(stream);
        var attachmentRows = reader.ReadToEnd()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Skip(1)
            .Select(line => line.TrimEnd('\r').Split(','))
            .Where(row => row.Length == 10 && row[0] == "base")
            .ToArray();
        var byActor = SourceStashGearActors()
            .Where(actor => actor.Scope == "base")
            .ToDictionary(actor => actor.Sid, StringComparer.Ordinal);
        var shared = attachmentRows.Where(row => byActor.ContainsKey(row[2]))
            .ToArray();
        Check(shared.Length == 51
            && shared.Select(row => row[2]).Distinct(StringComparer.Ordinal).Count() == 44,
            "Shared physical stash/attachment actor inventory changed");

        foreach (var row in shared)
        {
            var actor = byActor[row[2]];
            var bonusIndex = actor.RankCodes == "A" ? 1 : actor.OriginalSlotCount;
            var rankCount = actor.RankCodes == "A" ? 4 : 1;
            foreach (var redirect in row[3].Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                var match = Regex.Match(redirect,
                    @"^ItemGeneratorSettings/\[(\d+)\]/ItemGenerators/\[(\d+)\]$");
                Check(match.Success, "Unexpected source attachment actor path " + redirect);
                var rankIndex = int.Parse(match.Groups[1].Value);
                var originalIndex = int.Parse(match.Groups[2].Value);
                Check(rankIndex >= 0 && rankIndex < rankCount
                    && originalIndex >= 0 && originalIndex < bonusIndex,
                    "Attachment shuffle overwrites Extra Stash Gear slot " + row[2]
                        + " / " + redirect);
            }
        }

        var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
        vm.AllParameters.Single(p => p.Id == "loot.stashGearEnabled")
            .CurrentBoolean = true;
        vm.AllParameters.Single(p => p.Id == "loot.stashGearCoveragePercent")
            .SnapCurrentNumberToMarker(100);
        vm.AllParameters.Single(p => p.Id == "loot.worldAttachmentShuffle")
            .CurrentBoolean = true;

        var dir = Path.Combine(Keep ?? Path.GetTempPath(),
            "TCD_Attach_ExtraGear_Shared_Actor_Slots");
        try
        {
            var built = new CfgPatchWriterService().Write(vm, Catalog, null, dir);
            var cfg = ReadOutput(dir);
            Check(built.EditCount > 0
                && cfg.TryGetValue("GameData/SpawnActorPrototypes.cfg", out var actorFields)
                && cfg.TryGetValue("GameData/ItemGeneratorPrototypes.cfg", out var itemFields),
                "Combined attachment and stash gear writer omitted physical CFG output");

            foreach (var row in shared)
            {
                var actor = byActor[row[2]];
                // Each original attachment redirect retains its private clone
                // rather than being overwritten by a bonus generator link.
                foreach (var redirect in row[3].Split(';', StringSplitOptions.RemoveEmptyEntries))
                {
                    var field = row[2] + "/" + redirect + "/PrototypeSID";
                    Check(actorFields!.TryGetValue(field, out var assigned)
                        && assigned == row[5],
                        "World attachment redirect was lost at shared actor " + field);
                }
                Check(itemFields!.TryGetValue(row[5] + "/SID", out var cloneSid)
                    && cloneSid == row[5],
                    "Shared attachment generator clone was lost: " + row[5]);

                var rankCodes = actor.RankCodes == "A" ? "NEVM" : actor.RankCodes;
                var bonusIndex = actor.RankCodes == "A" ? 1 : actor.OriginalSlotCount;
                for (var rankIndex = 0; rankIndex < rankCodes.Length; rankIndex++)
                {
                    var bonusSid = "TCD_ExtraGear_" + row[2] + "_" + rankCodes[rankIndex];
                    var field = row[2] + "/ItemGeneratorSettings/[" + rankIndex
                        + "]/ItemGenerators/[" + bonusIndex + "]/PrototypeSID";
                    Check(actorFields!.TryGetValue(field, out var assigned)
                        && assigned == bonusSid,
                        "Extra Gear bonus slot was lost at shared actor " + field);
                    Check(itemFields!.TryGetValue(bonusSid + "/SID", out var source)
                        && source == bonusSid,
                        "Shared Extra Gear private generator missing: " + bonusSid);
                }
            }
            Console.WriteLine("PASS merged World Attachment Shuffle + Extra Stash Gear:"
                + " 44 shared actors, 51 native rows and separate private bonus slots");
        }
        finally
        {
            if (Keep is null && Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }
}
