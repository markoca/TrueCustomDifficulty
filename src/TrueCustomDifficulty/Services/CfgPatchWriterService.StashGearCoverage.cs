using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

// Selects a deterministic percentage of physical, source-approved stash actors.
// Every chosen rank branch receives a private generator with 1 or 2 actual
// source-audited gear item entries; shared vanilla loot pools are never edited.
public sealed partial class CfgPatchWriterService
{
    private const string StashGearActorRanksResource =
        "TrueCustomDifficulty.Data.stash-gear-actor-ranks.csv";
    private const string StashGearPoolResource =
        "TrueCustomDifficulty.Data.gear-progression-ranks.csv";
    private const string ReservedTrophyStashActor =
        "1CBBBE27428A1766B7F911863C044833";

    private sealed record StashGearActor(
        string ActorSid, string Scope, string Ranks, int ExistingGenerators);

    private sealed record StashGearItem(string Sid, bool Weapon, int Tier, string ItemType);

    private static ulong StashGearStableHash(string key)
        => BinaryPrimitives.ReadUInt64BigEndian(
            SHA256.HashData(Encoding.UTF8.GetBytes("TCD-StashGear-2026-10-18|" + key)));

    private static IReadOnlyList<StashGearItem> LoadStashGearPool()
    {
        var entries = ReadStashCsv(StashGearPoolResource, 8).Select(row =>
        {
            if (row[3].Length != 2 || row[3][0] != 'T'
                || row[3][1] is < '1' or > '4'
                || row[1] is not ("Weapon" or "Armor")
                || row[6] is not ("EXPLICIT_TRADER_TIER" or "EXPLICIT_PLAYER_GEAR_ANCHOR")
                || row[0].Contains("Gauss", StringComparison.OrdinalIgnoreCase)
                || row[0].Contains("RPG", StringComparison.OrdinalIgnoreCase)
                || row[0].Contains("Trophy", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Unsafe progression gear candidate: " + row[0]);
            return new StashGearItem(row[0], row[1] == "Weapon", row[3][1] - '0', row[2]);
        }).ToArray();

        if (entries.Length != 38
            || entries.Select(x => x.Sid).Distinct(StringComparer.Ordinal).Count() != 38
            || Enumerable.Range(1, 4).Any(tier =>
                entries.Count(x => x.Weapon && x.Tier == tier)
                    != new[] { 8, 10, 10, 6 }[tier - 1]
                || entries.Count(x => !x.Weapon && x.Tier == tier) != 1))
            throw new InvalidDataException("Progression gear pool is no longer 34 regular weapons + four suits.");
        return entries;
    }

    private static IReadOnlyList<StashGearActor> LoadStashGearActors()
    {
        var source = ReadStashCsv(StashGearActorRanksResource, 4);
        var rows = source.Select(x =>
        {
            if (!int.TryParse(x[3], out var count))
                throw new InvalidDataException("Invalid original ItemGenerator count for " + x[0]);
            return new StashGearActor(x[0], x[1], x[2], count);
        }).ToArray();

        var approvedBase = LoadSmartStashActors().Select(x => x.ActorSid)
            .ToHashSet(StringComparer.Ordinal);
        var approvedDlc = DlcActors.Value.Select(x => x.ActorSid)
            .ToHashSet(StringComparer.Ordinal);
        var protectedActors = BaseProtectedStashActors.Value;

        if (rows.Length != 915 || rows.Count(x => x.Scope == "base") != 803
            || rows.Count(x => x.Scope == "dlc1") != 112
            || rows.Select(x => x.ActorSid).Distinct(StringComparer.Ordinal).Count() != 915
            || rows.Any(x => x.ActorSid.Length != 32
                || !x.ActorSid.All(Uri.IsHexDigit)
                || protectedActors.Contains(x.ActorSid)
                || x.Scope == "base" && !approvedBase.Contains(x.ActorSid)
                || x.Scope == "dlc1" && !approvedDlc.Contains(x.ActorSid)
                || x.Scope is not ("base" or "dlc1")
                || x.ExistingGenerators is < 0 or > 4
                || x.Ranks is not ("N" or "E" or "V" or "M" or "A" or "X")
                || x.Ranks == "A" && x.ExistingGenerators != 4
                || x.Ranks == "X" && x.ExistingGenerators != 0
                || x.Scope == "dlc1" && x.Ranks is not ("N" or "X"))
            || rows.Count(x => x.Scope == "base" && x.ExistingGenerators > 0
                && x.ActorSid != ReservedTrophyStashActor) != 800
            || rows.Count(x => x.Scope == "dlc1" && x.ExistingGenerators > 0) != 108)
            throw new InvalidDataException("The full physical stash gear eligibility/rank inventory drifted.");

        // Validate each source-audited rank/slot against the independent
        // native ItemGenerator refs retained in the existing 803/112 manifests.
        var baseSources = ReadStashCsv(SmartLootActorResource, 8)
            .Where(x => x[3] == "EmptyInherited")
            .ToDictionary(x => x[0], x => x[2], StringComparer.Ordinal);
        var dlcSources = ReadStashCsv(DlcStashActorResource, 4)
            .ToDictionary(x => x[0], x => x[2], StringComparer.Ordinal);
        foreach (var row in rows)
        {
            var sourceRefs = (row.Scope == "base" ? baseSources : dlcSources)[row.ActorSid];
            var refs = sourceRefs.Split(',', StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries);
            if (refs.Length != row.ExistingGenerators
                || row.Ranks == "A" && !refs.SequenceEqual(new[]
                {
                    "Stash_Cheap_Newbie", "Stash_Cheap_Experienced",
                    "Stash_Cheap_Veteran", "Stash_Cheap_Master"
                }, StringComparer.Ordinal)
                && !refs.SequenceEqual(new[]
                {
                    "Stash_Medium_Newbie", "Stash_Medium_Experienced",
                    "Stash_Medium_Veteran", "Stash_Medium_Master"
                }, StringComparer.Ordinal)
                && !refs.SequenceEqual(new[]
                {
                    "Stash_Expensive_Newbie", "Stash_Expensive_Experienced",
                    "Stash_Expensive_Veteran", "Stash_Expensive_Master"
                }, StringComparer.Ordinal)
                || row.Ranks != "A" && refs.Length > 2
                || refs.Any(x => x.Contains("Blueprint", StringComparison.OrdinalIgnoreCase)
                    || x.Contains("Quest", StringComparison.OrdinalIgnoreCase)
                    || x.Contains("Unique", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("Native stash generator slots/ranks changed for " + row.ActorSid);
        }
        return rows;
    }

    private static IReadOnlyList<StashGearActor> SelectStashGearActors(
        IEnumerable<StashGearActor> candidates, int percentage)
    {
        var ordered = candidates.OrderBy(x =>
                StashGearStableHash("ACTOR|" + x.Scope + "|" + x.ActorSid))
            .ThenBy(x => x.ActorSid, StringComparer.Ordinal).ToArray();
        var count = (ordered.Length * percentage + 50) / 100;
        return ordered.Take(count).ToArray();
    }

    private static int WriteStashGearCoverage(
        IReadOnlyDictionary<string, ParameterViewModel> all,
        bool hasDlc1,
        IDictionary<string, PatchDocument> documents)
    {
        if (!all.TryGetValue("loot.stashGearEnabled", out var master)
            || !master.CurrentBoolean
            || !all.TryGetValue("loot.stashGearCoveragePercent", out var coverage))
            return 0;

        var percent = Math.Clamp((int)Math.Round(coverage.CurrentNumber), 0, 100);
        if (percent == 0)
            return 0;

        var approved = LoadStashGearActors();
        var chosen = SelectStashGearActors(approved.Where(x => x.Scope == "base"
                && x.ExistingGenerators > 0 && x.ActorSid != ReservedTrophyStashActor), percent)
            .Concat(hasDlc1
                ? SelectStashGearActors(approved.Where(x => x.Scope == "dlc1"
                    && x.ExistingGenerators > 0), percent)
                : Array.Empty<StashGearActor>())
            .ToArray();
        var pool = LoadStashGearPool();
        var prototypes = GetDocument(documents, "GameData/ItemGeneratorPrototypes.cfg");
        var written = 0;
        foreach (var actor in chosen)
        {
            var rankCodes = actor.Ranks == "A" ? "NEVM" : actor.Ranks;
            var actors = GetDocument(documents, actor.Scope == "base"
                ? BaseProtectedActorFile : DlcSmartLootActorFile);
            for (var i = 0; i < rankCodes.Length; i++)
            {
                var code = rankCodes[i];
                var tier = code switch
                {
                    'N' => 1, 'E' => 2, 'V' => 3, 'M' => 4,
                    _ => throw new InvalidDataException("Unknown native stash player rank.")
                };
                var rank = new[] { "Newbie", "Experienced", "Veteran", "Master" }[tier - 1];
                var generatorSid = "TCD_ExtraGear_" + actor.ActorSid + "_" + code;
                var originalIndex = actor.Ranks == "A" ? 1 : actor.ExistingGenerators;

                // Patch an EMPTY next slot, never overwrite an authored generator
                // or its quest/attachment reward. Existing rank gate is preserved.
                var parent = actors.GetOrAddNode(actor.ActorSid,
                    new[] { "ItemGeneratorSettings", $"[{i}]", "ItemGenerators" });
                parent.GetOrAddChild($"[{originalIndex}]", useBpatch: false)
                    .SetAssignment("PrototypeSID", generatorSid);
                written++;

                // Per-actor/per-native-rank source: one possible regular weapon,
                // and optionally one suit, both selected from the full 34+4
                // source-backed pool. No generator is shared across physical actors.
                var generator = prototypes.GetOrAddReferencedRoot(
                    generatorSid, "../ItemGeneratorPrototypes.cfg", "[0]");
                generator.SetAssignment("SID", generatorSid);
                var categories = generator.GetOrAddChild("ItemGenerator", useBpatch: false);
                var weapons = pool.Where(x => x.Weapon && x.Tier <= tier).ToArray();
                var armor = pool.Where(x => !x.Weapon && x.Tier <= tier).ToArray();
                var selectedWeapon = weapons[
                    (int)(StashGearStableHash("WEAPON|" + actor.ActorSid + "|" + code)
                          % (ulong)weapons.Length)];
                var selectedArmor = armor[
                    (int)(StashGearStableHash("ARMOR|" + actor.ActorSid + "|" + code)
                          % (ulong)armor.Length)].Sid;
                var includeArmor =
                    (StashGearStableHash("COUNT|" + actor.ActorSid + "|" + code) & 1UL) == 0;

                void AddItem(int index, string categoryName, string sid, bool weapon)
                {
                    var category = categories.GetOrAddChild($"[{index}]", useBpatch: false);
                    category.SetAssignment("Category", "EItemGenerationCategory::" + categoryName);
                    category.SetAssignment("bAllowSameCategoryGeneration", "false");
                    var item = category.GetOrAddChild("PossibleItems", useBpatch: false)
                        .GetOrAddChild("[0]", useBpatch: false);
                    item.SetAssignment("ItemPrototypeSID", sid);
                    item.SetAssignment("Chance", "1");
                    item.SetAssignment("MinCount", "1");
                    item.SetAssignment("MaxCount", "1");
                    item.SetAssignment("MinDurability", weapon ? "0.45" : "0.55");
                    item.SetAssignment("MaxDurability", weapon ? "0.80" : "0.85");
                    if (weapon)
                    {
                        item.SetAssignment("AmmoMinCount", "0");
                        item.SetAssignment("AmmoMaxCount", "0");
                    }
                }

                AddItem(0, selectedWeapon.ItemType == "Pistol" ? "WeaponPistol" : "WeaponPrimary",
                    selectedWeapon.Sid, weapon: true);
                if (includeArmor)
                    AddItem(1, "BodyArmor", selectedArmor, weapon: false);
                written++;
            }
        }
        return written;
    }
}
