using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.Services;
using TrueCustomDifficulty.ViewModels;

namespace LootSpecialGeneratorsContract;

internal static partial class Program
{
    private sealed record NestedAttachment(
        string Sid, string Kind, int Tier, string SourceGenerator, bool FitsT1);

    private static Dictionary<string, NestedAttachment> AuditedNestedAttachments()
    {
        static string[][] Csv(string name, int cols)
        {
            using var input = typeof(CfgPatchWriterService).Assembly.GetManifestResourceStream(
                "TrueCustomDifficulty.Data." + name)
                ?? throw new InvalidDataException("Source attachment CSV absent: " + name);
            using var reader = new StreamReader(input);
            return reader.ReadToEnd().Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Skip(1).Select(s => s.TrimEnd('\r').Split(','))
                .Select(row =>
                {
                    Check(row.Length == cols, "Malformed native attachment evidence " + name);
                    return row;
                }).ToArray();
        }

        var rows = Csv("stash-bonus-attachment-progressions.csv", 6);
        var t1Weapons = Csv("gear-progression-ranks.csv", 8)
            .Where(row => row[1] == "Weapon" && row[3] == "T1")
            .Select(row => row[0]).ToHashSet(StringComparer.Ordinal);
        Check(t1Weapons.Count == 8, "Native T1 weapon pool drifted");
        var originals = Csv("world-attachment-generator-manifest.csv", 10);
        var world = Csv("world-attachment-shuffle-pool.csv", 3)
            .ToDictionary(row => row[0], StringComparer.Ordinal);
        var candidates = new Dictionary<string, NestedAttachment>(StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var sid = row[0];
            var source = row[3];
            Check(row[4] == "EXACT_AUDITED_SINGLETON_GENERATOR"
                && row[2] is "T2" or "T3" or "T4"
                && source == "Stash_" + sid
                && world.TryGetValue(sid, out var worldEntry)
                && worldEntry[1] == row[1] && worldEntry[2] == row[2]
                && originals.Any(o => o[4] == source && o[7] == sid
                    && o[6] == "ItemGenerator/[0]/PossibleItems/[0]")
                && originals.Where(o => o[4] == source)
                    .All(o => o[7] == sid
                        && o[6] == "ItemGenerator/[0]/PossibleItems/[0]"),
                "Nested attachment source is not an audited original singleton: " + sid);
            var fittings = row[5].Split(';',
                StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            Check(fittings.Length > 0
                && fittings.Distinct(StringComparer.Ordinal).Count() == fittings.Length,
                "Malformed source weapon fitting evidence: " + sid);
            Check(candidates.TryAdd(source,
                new NestedAttachment(sid, row[1], int.Parse(row[2][1..]),
                    source, fittings.Any(t1Weapons.Contains))),
                "Duplicate nested attachment source reference");
        }
        Check(candidates.Count == 19
            && candidates.Values.Count(c => c.Kind == "Magazine") == 16
            && candidates.Values.Count(c => c.Kind == "Suppressor") == 2
            && candidates.Values.Count(c => c.Kind == "Underbarrel") == 1
            && candidates.Values.All(c => c.Kind != "Optic")
            && candidates.Values.Where(c => c.FitsT1).Select(c => c.Sid)
                .ToHashSet(StringComparer.Ordinal).SetEquals(new[]
                {
                    "GunAK74_MagIncreased", "GunUDP_MagIncreased", "RU_GLaunch_1"
                }),
            "Current 19-source attachment class inventory changed; re-audit optics separately");
        return candidates;
    }

    // Parse only emitted actor-private roots; never interpret a different
    // generator's authored removal BPatch as a source item or an inner roll.
    private static Dictionary<string, Dictionary<string, string>> ReadPrivateGearRoots(string text)
    {
        var roots = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        Dictionary<string, string>? fields = null;
        var stack = new List<string>();
        foreach (var raw in text.Replace("\r", "").Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
                continue;
            if (fields is null)
            {
                var root = Regex.Match(line,
                    @"^(TCD_ExtraGear_[0-9A-F]{32}_[NEVM])\s*:\s*struct\.begin");
                if (!root.Success)
                    continue;
                var id = root.Groups[1].Value;
                fields = new Dictionary<string, string>(StringComparer.Ordinal);
                Check(roots.TryAdd(id, fields), "Duplicate private bonus root " + id);
                stack.Add(id);
                continue;
            }
            if (line == "struct.end")
            {
                Check(stack.Count > 0, "Too many private generator struct.end");
                stack.RemoveAt(stack.Count - 1);
                if (stack.Count == 0)
                    fields = null;
                continue;
            }
            var open = Regex.Match(line, @"^(.*?)\s*:\s*struct\.begin");
            if (open.Success)
            {
                stack.Add(open.Groups[1].Value);
                continue;
            }
            var assignment = Regex.Match(line, @"^(\S+)\s*=\s*(.*?)\s*$");
            Check(assignment.Success,
                "Unexpected syntax in private stash bonus generator: " + line);
            var key = string.Join("/", stack.Skip(1).Append(assignment.Groups[1].Value));
            Check(fields.TryAdd(key, assignment.Groups[2].Value),
                "Duplicated native candidate field " + key);
        }
        Check(stack.Count == 0 && fields is null,
            "Unclosed actor-private stash bonus generator");
        return roots;
    }

    private static void VerifyNativeBonusScenario(
        string name, int coverage, int chance, bool weaponMix, bool dlc)
    {
        var attachments = AuditedNestedAttachments();
        var vm = new MainViewModel(Catalog) { SelectedReferenceDifficulty = "Veteran" };
        vm.AllParameters.Single(p => p.Id == "loot.stashGearEnabled").CurrentBoolean = true;
        vm.AllParameters.Single(p => p.Id == "loot.stashGearCoveragePercent")
            .SnapCurrentNumberToMarker(coverage);
        vm.AllParameters.Single(p => p.Id == "loot.stashGearBonusChance")
            .SnapCurrentNumberToMarker(chance);
        foreach (var kind in new[] { "Weapon", "Armor", "Artifact", "Attachment" })
            vm.AllParameters.Single(p => p.Id == "loot.stashGear" + kind + "Weight")
                .SnapCurrentNumberToMarker(kind == "Attachment" ? 3
                    : kind == "Weapon" && weaponMix ? 1 : 0);
        var dir = Path.Combine(Keep ?? Path.GetTempPath(),
            "TCD_Stash_Native_Bonus_" + name);
        try
        {
            new CfgPatchWriterService().Write(vm, Catalog,
                dlc ? new GameInstallInfo { HasDlc1 = true } : null, dir);
            var text = File.ReadAllText(OutputPath(dir,
                "GameData/ItemGeneratorPrototypes.cfg"));
            var roots = ReadPrivateGearRoots(text);
            var actors = SourceStashGearActors();
            var selected = SelectedSourceStashes(actors, "base", coverage);
            if (dlc)
                selected.UnionWith(SelectedSourceStashes(actors, "dlc1", coverage));
            var sourceBySid = actors.ToDictionary(a => a.Sid, StringComparer.Ordinal);
            var expected = new HashSet<string>(StringComparer.Ordinal);
            foreach (var sid in selected)
            {
                var ranks = sourceBySid[sid].RankCodes == "A"
                    ? "NEVM" : sourceBySid[sid].RankCodes;
                foreach (var rank in ranks)
                    expected.Add("TCD_ExtraGear_" + sid + "_" + rank);
            }
            Check(roots.Count == expected.Count
                && roots.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(expected),
                name + ": unexpected private-generator coverage/rank roots");

            var expectedChance = (chance / 100.0)
                .ToString("0.##", CultureInfo.InvariantCulture);
            var originalWeapons = new Dictionary<string, (int Tier, string Type)>(
                StringComparer.Ordinal);
            using (var resource = typeof(CfgPatchWriterService).Assembly
                .GetManifestResourceStream("TrueCustomDifficulty.Data.gear-progression-ranks.csv")
                ?? throw new InvalidDataException("Missing source equipment ranks"))
            using (var reader = new StreamReader(resource))
                foreach (var row in reader.ReadToEnd()
                    .Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(1)
                    .Select(s => s.TrimEnd('\r').Split(',')))
                    if (row[1] == "Weapon")
                        originalWeapons.Add(row[0], (int.Parse(row[3][1..]), row[2]));

            var nestedCount = 0;
            foreach (var (root, fields) in roots)
            {
                var nativeRank = root[^1];
                var rankTier = "NEVM".IndexOf(nativeRank) + 1;
                Check(rankTier > 0 && fields.TryGetValue("SID", out var declared)
                    && declared == root, name + ": incorrect private root SID");

                var count = weaponMix ? 2 : 1;
                var categoryKeys = fields.Keys.Where(k => Regex.IsMatch(k,
                    @"^ItemGenerator/\[\d+\]/Category$")).ToArray();
                Check(categoryKeys.Length == count
                    && categoryKeys.OrderBy(x => x, StringComparer.Ordinal)
                        .SequenceEqual(Enumerable.Range(0, count).Select(index =>
                            "ItemGenerator/[" + index + "]/Category")),
                    name + ": native candidate indices or count changed");

                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (var index = 0; index < count; index++)
                {
                    var prefix = "ItemGenerator/[" + index + "]/";
                    var itemPrefix = prefix + "PossibleItems/[0]/";
                    string Required(string key)
                    {
                        Check(fields.TryGetValue(key, out var value),
                            name + ": missing native candidate field: " + root + "/" + key);
                        return fields[key];
                    }
                    var category = Required(prefix + "Category");
                    Check(Required(prefix + "bAllowSameCategoryGeneration") == "false"
                        && Required(itemPrefix + "Chance") == expectedChance,
                        name + ": outer candidate gate does not match shared native Chance");

                    var nested = category == "EItemGenerationCategory::SubItemGenerator";
                    var allowed = new HashSet<string>(StringComparer.Ordinal)
                    {
                        prefix + "Category", prefix + "bAllowSameCategoryGeneration",
                        itemPrefix + "Chance"
                    };
                    if (nested)
                    {
                        Check(seen.Add("Attachment"),
                            name + ": repeated nested attachment category");
                        var generator = Required(itemPrefix + "ItemGeneratorPrototypeSID");
                        allowed.Add(itemPrefix + "ItemGeneratorPrototypeSID");
                        Check(attachments.TryGetValue(generator, out var attachment)
                            && (attachment.Tier <= rankTier
                                || rankTier == 1 && attachment.FitsT1),
                            name + ": invalid or overrank nested singleton reference");
                        nestedCount++;
                        // No outer MinCount/MaxCount, item SID, or fake inner
                        // Chance field: original source singleton owns its gate.
                    }
                    else
                    {
                        Check(weaponMix && seen.Add("Weapon")
                            && category is "EItemGenerationCategory::WeaponPrimary"
                                or "EItemGenerationCategory::WeaponPistol",
                            name + ": unexpected direct class/category in paired mix");
                        var sid = Required(itemPrefix + "ItemPrototypeSID");
                        Check(originalWeapons.TryGetValue(sid, out var weapon)
                            && weapon.Tier <= rankTier
                            && category == (weapon.Type == "Pistol"
                                ? "EItemGenerationCategory::WeaponPistol"
                                : "EItemGenerationCategory::WeaponPrimary"),
                            name + ": invalid weapon SID, native category, or progression tier");
                        foreach (var field in new[] { "ItemPrototypeSID", "MinCount",
                                     "MaxCount", "MinDurability", "MaxDurability",
                                     "AmmoMinCount", "AmmoMaxCount" })
                            allowed.Add(itemPrefix + field);
                        Check(Required(itemPrefix + "MinCount") == "1"
                            && Required(itemPrefix + "MaxCount") == "1"
                            && Required(itemPrefix + "MinDurability") == "0.45"
                            && Required(itemPrefix + "MaxDurability") == "0.80"
                            && Required(itemPrefix + "AmmoMinCount") == "0"
                            && Required(itemPrefix + "AmmoMaxCount") == "0",
                            name + ": source-specific weapon candidate fields drifted");
                    }
                    Check(fields.Keys.Where(k => k.StartsWith(prefix,
                            StringComparison.Ordinal)).All(allowed.Contains),
                        name + ": unwanted direct, inner or invented candidate field: " + root);
                }
                Check(seen.Count == count
                    && (weaponMix ? seen.Contains("Weapon") : true)
                    && seen.Contains("Attachment"),
                    name + ": duplicate or missing selected native bonus category");
                Check(fields.Keys.Count == fields.Keys.Distinct(StringComparer.Ordinal).Count(),
                    name + ": duplicate CFG field key");
            }
            Check(nestedCount > 0,
                name + ": no nested attachment was exercised");
            // The original source singleton must be referred to, not cloned
            // or modified. Its internal Chance cannot be inferred here.
            Check(!Regex.IsMatch(text, @"(?m)^Stash_(?:Gun|EN_|RU_)[^\r\n]*:\s*struct\.begin"),
                name + ": source singleton generator unexpectedly rewritten");
            Console.WriteLine("PASS native Bonus Chance " + name + ": "
                + roots.Count + " private roots, " + nestedCount
                + " untouched nested source links; no game RNG claim");
        }
        finally
        {
            if (Keep is null && Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }

    static void VerifyNativeStashGearChancesAndNestedAttachments()
    {
        VerifyNativeBonusScenario("mixed-37", 100, 37, weaponMix: true, dlc: true);
        VerifyNativeBonusScenario("attachment-only-0", 25, 0,
            weaponMix: false, dlc: false);
        VerifyNativeBonusScenario("attachment-only-100", 25, 100,
            weaponMix: false, dlc: true);
    }
}
