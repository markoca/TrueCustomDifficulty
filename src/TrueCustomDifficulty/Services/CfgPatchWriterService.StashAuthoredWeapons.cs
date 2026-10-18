using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

public sealed partial class CfgPatchWriterService
{
    private const string StashAuthoredWeaponRemovalResource =
        "TrueCustomDifficulty.Data.stash-authored-weapon-removal-manifest.csv";

    private const string CompleteStashWeaponInventoryResource =
        "TrueCustomDifficulty.Data.stash-weapon-source-inventory.csv";

    private sealed record StashAuthoredWeapon(
        string ActorSid, string GeneratorSid, string ItemSid, string NodePath);

    private static IReadOnlyList<StashAuthoredWeapon> LoadStashAuthoredWeaponRemovals()
    {
        var rows = ReadStashCsv(StashAuthoredWeaponRemovalResource, 4);
        var approved = LoadSmartStashActors().Select(x => x.ActorSid)
            .ToHashSet(StringComparer.Ordinal);
        var entries = rows.Select(x => new StashAuthoredWeapon(
            x[0], x[1], x[2], x[3])).ToArray();

        // This is the exact source-audited 13-slot REGULAR subset of the
        // 14 authored weapon entries; Gun_Trophy_AR is intentionally withheld.
        // The 106 protected actors (including STC Malachite) are never patched.
        if (entries.Length != 13
            || entries.Select(x => x.ActorSid).Distinct(StringComparer.Ordinal).Count() != 13
            || entries.Select(x => (x.GeneratorSid, x.NodePath)).Distinct().Count() != 13
            || entries.Any(x => x.ActorSid.Length != 32
                || !x.ActorSid.All(Uri.IsHexDigit)
                || !approved.Contains(x.ActorSid)
                || BaseProtectedStashActors.Value.Contains(x.ActorSid)
                || !x.GeneratorSid.EndsWith("_" + x.ActorSid, StringComparison.Ordinal)
                || !x.ItemSid.StartsWith("Gun", StringComparison.Ordinal)
                || x.ItemSid.Contains("Trophy", StringComparison.OrdinalIgnoreCase)
                || x.NodePath != "ItemGenerator/[1]/PossibleItems/[0]"))
        {
            throw new InvalidDataException(
                "Extra Stash Gear authored-weapon removal allowlist drifted; refusing unsafe patch.");
        }

        // The exact removal manifest is ONLY the approved subset of the
        // source-wide base + DLC1 physical ItemContainer weapon inventory.
        // Quest, unique, protected and unreviewed DLC rows must remain inert.
        var inventory = ReadStashCsv(CompleteStashWeaponInventoryResource, 11);
        var sourceCounts = inventory.GroupBy(x => x[9], StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var expectedCounts = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["REMOVE_REGULAR"] = 13,
            ["KEEP_QUEST"] = 16,
            ["KEEP_UNIQUE_REVIEW"] = 1,
            ["KEEP_PROTECTED"] = 1,
            ["KEEP_DLC_SPECIAL_REVIEW"] = 8
        };
        if (inventory.Count != 39
            || sourceCounts.Count != expectedCounts.Count
            || expectedCounts.Any(x => !sourceCounts.TryGetValue(x.Key, out var count)
                || count != x.Value))
            throw new InvalidDataException(
                "Exhaustive stash weapon source inventory changed without review.");

        var sourceApproved = inventory.Where(x => x[9] == "REMOVE_REGULAR").ToArray();
        var approvedIdentity = sourceApproved
            .Select(x => (Actor: x[1], Generator: x[4], Item: x[8],
                Path: "ItemGenerator/[" + x[6] + "]/PossibleItems/[" + x[7] + "]"))
            .ToHashSet();
        if (approvedIdentity.Count != 13
            || sourceApproved.Any(x => x[0] != "base" || x[2] != "WorldMap_WP"
                || x[3] != "EmptyInherited"
                || x[5] is not ("WeaponPrimary" or "WeaponPistol" or "WeaponSecondary"))
            || entries.Any(x => !approvedIdentity.Contains(
                (x.ActorSid, x.GeneratorSid, x.ItemSid, x.NodePath)))
            || inventory.Where(x => x[9] != "REMOVE_REGULAR").Any(x =>
                entries.Any(e => e.ActorSid == x[1] || e.GeneratorSid == x[4])))
            throw new InvalidDataException(
                "Extra stash weapon removals do not match the independently reviewed full inventory.");
        
        return entries;
    }

    private static int WriteStashAuthoredWeaponRemovals(
        IReadOnlyDictionary<string, ParameterViewModel> all,
        IDictionary<string, PatchDocument> documents)
    {
        if (!all.TryGetValue("loot.stashGearEnabled", out var gear)
            || !gear.CurrentBoolean)
            return 0;

        // World Weapon Shuffle assigns and guarantees weapon copies in some
        // of these same exact source slots. Never silently discard assignments
        // after its allocator has counted them.
        if (all.TryGetValue("loot.worldWeaponShuffle", out var shuffle)
            && shuffle.CurrentBoolean)
        {
            throw new InvalidDataException(
                "Extra Stash Gear and World Weapon Shuffle cannot both be enabled. " +
                "They edit the same authored weapon stash slots. Turn off one to build safely.");
        }

        var source = LoadStashAuthoredWeaponRemovals();
        var document = GetDocument(documents, "GameData/ItemGeneratorPrototypes.cfg");
        foreach (var row in source)
        {
            var path = row.NodePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            // Remove only the original weapon PossibleItems entry. Do not
            // remove the parent category, its sibling items/attachments,
            // other item generators or the physical stash actor itself.
            document.AddRemovalNode(row.GeneratorSid, path[..^1], path[^1]);
        }

        return source.Count;
    }
}
