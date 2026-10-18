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
}
