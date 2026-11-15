using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using TrueCustomDifficulty.Models;
using TrueCustomDifficulty.ViewModels;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Strict, source-audited percentage reduction of mod-added generator Chance.
/// No installed third-party content is copied, unpacked or edited. An unmatched
/// version fails closed instead of claiming to affect that mod's loot.
/// </summary>
public sealed partial class CfgPatchWriterService
{
    private sealed record AuditedPak(string Name, string Sha256);
    private sealed record AuditedMod(
        string Name,
        AuditedPak[] Packages,
        (string Root, string Branch)[] WorldBranches,
        (string Root, string Branch)[] CorpseBranches,
        double WorldSourceChance,
        double CorpseSourceChance);

    private static readonly string[] GamePassStashRoots =
    {
        "GamePass_Stash_ItemGenerator_Cheap",
        "GamePass_Stash_ItemGenerator_Common_Var1",
        "GamePass_Stash_ItemGenerator_Common_Var2",
        "GamePass_Stash_ItemGenerator_Rare"
    };

    private static readonly string[] BackpackCorpseRoots =
    {
        "GeneralNPC_Neutral_CloseCombat_ItemGenerator",
        "GeneralNPC_Neutral_Recon_ItemGenerator",
        "GeneralNPC_Neutral_Sniper_ItemGenerator",
        "GeneralNPC_Neutral_Stormtrooper_ItemGenerator",
        "GeneralNPC_Bandit_CloseCombat_ItemGenerator",
        "GeneralNPC_Bandit_Recon_ItemGenerator",
        "GeneralNPC_Bandit_Stormtrooper_ItemGenerator",
        "GeneralNPC_Bandit_Heavy_ItemGenerator",
        "GeneralNPC_Mercenaries_CloseCombat_ItemGenerator",
        "GeneralNPC_Mercenaries_Recon_ItemGenerator",
        "GeneralNPC_Mercenaries_Stormtrooper_ItemGenerator",
        "GeneralNPC_Mercenaries_Sniper_ItemGenerator",
        "GeneralNPC_Militaries_CloseCombat_ItemGenerator",
        "GeneralNPC_Militaries_Recon_ItemGenerator",
        "GeneralNPC_Militaries_Stormtrooper_ItemGenerator",
        "GeneralNPC_Militaries_Sniper_ItemGenerator",
        "GeneralNPC_Militaries_Heavy_ItemGenerator",
        "GeneralNPC_Monolith_CloseCombat_ItemGenerator",
        "GeneralNPC_Monolith_Recon_ItemGenerator",
        "GeneralNPC_Monolith_Stormtrooper_ItemGenerator",
        "GeneralNPC_Monolith_Sniper_ItemGenerator",
        "GeneralNPC_Duty_CloseCombat_ItemGenerator",
        "GeneralNPC_Duty_Recon_ItemGenerator",
        "GeneralNPC_Duty_Stormtrooper_ItemGenerator",
        "GeneralNPC_Duty_Sniper_ItemGenerator",
        "GeneralNPC_Duty_Heavy_ItemGenerator",
        "GeneralNPC_Freedom_CloseCombat_ItemGenerator",
        "GeneralNPC_Freedom_Recon_ItemGenerator",
        "GeneralNPC_Freedom_Stormtrooper_ItemGenerator",
        "GeneralNPC_Freedom_Sniper_ItemGenerator",
        "GeneralNPC_Varta_CloseCombat_ItemGenerator",
        "GeneralNPC_Varta_Recon_ItemGenerator",
        "GeneralNPC_Varta_Stormtrooper_ItemGenerator",
        "GeneralNPC_Varta_Sniper_ItemGenerator",
        "GeneralNPC_Varta_Heavy_ItemGenerator",
        "GeneralNPC_Noon_CloseCombat_ItemGenerator",
        "GeneralNPC_Noon_Recon_ItemGenerator",
        "GeneralNPC_Noon_Stormtrooper_ItemGenerator",
        "GeneralNPC_Noon_Sniper_ItemGenerator",
        "GeneralNPC_Spark_CloseCombat_ItemGenerator",
        "GeneralNPC_Spark_Recon_ItemGenerator",
        "GeneralNPC_Spark_Stormtrooper_ItemGenerator",
        "GeneralNPC_Spark_Sniper_ItemGenerator",
        "GeneralNPC_Corpus_CloseCombat_ItemGenerator",
        "GeneralNPC_Corpus_Recon_ItemGenerator",
        "GeneralNPC_Corpus_Stormtrooper_ItemGenerator",
        "GeneralNPC_Corpus_Sniper_ItemGenerator",
        "GeneralNPC_Corpus_Heavy_ItemGenerator"
    };

    private static readonly string[] RepairCorpseRoots =
    {
        "GeneralNPC_Bandit_CloseCombat_ItemGenerator",
        "GeneralNPC_Bandit_Heavy_ItemGenerator",
        "GeneralNPC_Bandit_Recon_ItemGenerator",
        "GeneralNPC_Bandit_Stormtrooper_ItemGenerator",
        "GeneralNPC_Corpus_CloseCombat_ItemGenerator",
        "GeneralNPC_Corpus_Heavy_ItemGenerator",
        "GeneralNPC_Corpus_Recon_ItemGenerator",
        "GeneralNPC_Corpus_Sniper_ItemGenerator",
        "GeneralNPC_Corpus_Stormtrooper_ItemGenerator",
        "GeneralNPC_Duty_CloseCombat_ItemGenerator",
        "GeneralNPC_Duty_Heavy_ItemGenerator",
        "GeneralNPC_Duty_Recon_ItemGenerator",
        "GeneralNPC_Duty_Sniper_ItemGenerator",
        "GeneralNPC_Duty_Stormtrooper_ItemGenerator",
        "GeneralNPC_Freedom_CloseCombat_ItemGenerator",
        "GeneralNPC_Freedom_Recon_ItemGenerator",
        "GeneralNPC_Freedom_Sniper_ItemGenerator",
        "GeneralNPC_Freedom_Stormtrooper_ItemGenerator",
        "GeneralNPC_MALACHITE_Scientist_Recon_ItemGenerator",
        "GeneralNPC_Mercenaries_CloseCombat_ItemGenerator",
        "GeneralNPC_Mercenaries_Recon_ItemGenerator",
        "GeneralNPC_Mercenaries_Sniper_ItemGenerator",
        "GeneralNPC_Mercenaries_Stormtrooper_ItemGenerator",
        "GeneralNPC_Militaries_CloseCombat_ItemGenerator",
        "GeneralNPC_Militaries_Heavy_ItemGenerator",
        "GeneralNPC_Militaries_Recon_ItemGenerator",
        "GeneralNPC_Militaries_Sniper_ItemGenerator",
        "GeneralNPC_Militaries_Stormtrooper_ItemGenerator",
        "GeneralNPC_Monolith_CloseCombat_ItemGenerator",
        "GeneralNPC_Monolith_Recon_ItemGenerator",
        "GeneralNPC_Monolith_Sniper_ItemGenerator",
        "GeneralNPC_Monolith_Stormtrooper_ItemGenerator",
        "GeneralNPC_Neutral_CloseCombat_ItemGenerator",
        "GeneralNPC_Neutral_CloseCombat_No_Armor_ItemGenerator",
        "GeneralNPC_Neutral_Recon_ItemGenerator",
        "GeneralNPC_Neutral_Recon_No_Armor_ItemGenerator",
        "GeneralNPC_Neutral_Sniper_ItemGenerator",
        "GeneralNPC_Neutral_Stormtrooper_ItemGenerator",
        "GeneralNPC_Neutral_Stormtrooper_No_Armor_ItemGenerator",
        "GeneralNPC_Noon_CloseCombat_ItemGenerator",
        "GeneralNPC_Noon_Recon_ItemGenerator",
        "GeneralNPC_Noon_Sniper_ItemGenerator",
        "GeneralNPC_Noon_Stormtrooper_ItemGenerator",
        "GeneralNPC_SIRCAA_Scientist_Recon_ItemGenerator",
        "GeneralNPC_Scientists_Recon_ItemGenerator",
        "GeneralNPC_Scientists_Stormtrooper_ItemGenerator",
        "GeneralNPC_Spark_CloseCombat_ItemGenerator",
        "GeneralNPC_Spark_Recon_ItemGenerator",
        "GeneralNPC_Spark_Sniper_ItemGenerator",
        "GeneralNPC_Spark_Stormtrooper_ItemGenerator",
        "GeneralNPC_Varta_CloseCombat_ItemGenerator",
        "GeneralNPC_Varta_Heavy_ItemGenerator",
        "GeneralNPC_Varta_Recon_ItemGenerator",
        "GeneralNPC_Varta_Sniper_ItemGenerator",
        "GeneralNPC_Varta_Stormtrooper_ItemGenerator"
    };

    // Exact user-supplied 2.1 mod PAK fingerprints, audited on 2026-11-15.
    // Absence does not trigger an edit; mismatching installed versions fail
    // explicitly rather than silently producing a broken or inert BPatch.
    private static readonly AuditedMod[] AuditedLootMods =
    {
        new(
            "ZoneWatch",
            new[]
            {
                new AuditedPak("ZoneWatchStalker2-Windows-NewContent(1).pak",
                    "df9a997ed67c3f3871778c5d6fbb18cc11721b2205cb1915ce719eb2a0caf466")
            },
            new[]
            {
                ("LesserZone_PostOffice_StashGenerator1_47ED900C400F3D651EA30EBBD5B58EA6", "ZST_FieldWatch110"),
                ("Promzona_AhropromResearchInstitute_StashGenerator3_1B259E604F001F533511BD889B771B3C", "ZST_LabSentinel207"),
                ("DugaRadar_Officer\'sQuarters_StashGenerator1_D729B70449C0E7FDE1D0C8A18659D10C", "ZST_SignalmanHold207"),
                ("CementPlant_IslandNearKopachi_Safe", "ZST_FerrymanTally207")
            },
            Array.Empty<(string, string)>(),
            1.0, 0.0),
        new(
            "PointOfNoReturn-BackpacksPack",
            new[]
            {
                new AuditedPak("PointOfNoReturn-BackpacksPackStalker2-Windows-NewContent.pak",
                    "1fd91e43629889a279268c6b8a66d9e4bf310f5295f27f55fccd8dcc011490c6"),
                new AuditedPak("PointOfNoReturn-BackpacksPackStalker2-Windows-OverrideContent.pak",
                    "5326668aba6a7a9fe2b1cfefe7081e2624f3c3dbb4a073244990520084089816")
            },
            GamePassStashRoots.Select(root => (root, "Backpacks")).ToArray(),
            BackpackCorpseRoots.Select(root => (root, "Backpacks")).ToArray(),
            0.10, 0.10),
        new(
            "PointOfNoReturn-RepairSystem",
            new[]
            {
                new AuditedPak("PointOfNoReturn-RepairSystemStalker2-Windows-NewContent.pak",
                    "4da21f3a08c9ee1eb369c09a30e99a103c7e197a6469ab93cfe55199738e86c3"),
                new AuditedPak("PointOfNoReturn-RepairSystemStalker2-Windows-OverrideContent.pak",
                    "d22f05b500efdbc89989b9596cb87a2711ba896997f82378bcf99c00fb73c707")
            },
            GamePassStashRoots.Select(root => (root, "RepairKits")).ToArray(),
            RepairCorpseRoots.Select(root => (root, "RepairKits")).ToArray(),
            0.05, 0.04),
        new(
            "PointOfNoReturn-ArGogglesPack",
            new[]
            {
                new AuditedPak("PointOfNoReturn-ArGogglesPackStalker2-Windows-NewContent.pak",
                    "5ae6c754f276a0f0ea6102569066a2562c6fc4283f440cd5a2721ca7c20ac55c"),
                new AuditedPak("PointOfNoReturn-ArGogglesPackStalker2-Windows-OverrideContent.pak",
                    "298598de23359b5397550a4c44331055e3c02665503d1cdddd617942731e03e2")
            },
            GamePassStashRoots.Select(root => (root, "ArGoggles")).ToArray(),
            Array.Empty<(string, string)>(),
            0.05, 0.0)
    };

    private static int WriteAuditedModItemRemovals(
        MainViewModel vm,
        string? modsFolder,
        IDictionary<string, PatchDocument> documents)
    {
        // Preset sidecars retain percentages, not a destructive binary flag.
        // 0% does not write a CFG; full 100% retains exact branch removal.
        var worldRemoval = vm.WorldModItemsEnabled ? vm.WorldModItemsRemoval : 0.0;
        var corpseRemoval = vm.CorpseModItemsEnabled ? vm.CorpseModItemsRemoval : 0.0;
        if (worldRemoval <= 0.0 && corpseRemoval <= 0.0)
            return 0;

        if (string.IsNullOrWhiteSpace(modsFolder) || !Directory.Exists(modsFolder))
            throw new InvalidDataException("MOD ITEMS removal requires a configured, accessible ~mods folder.");

        var packagePaths = Directory.EnumerateFiles(modsFolder, "*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".pak", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (packagePaths.Any(path => Path.GetFileName(path)
                .Contains("ProjectItemizationReborn", StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException(
                "Project Itemization Reborn is installed. Its bskipref / PackOfItems "
                + "generator replacements have no safe independent mod-loot removal writer yet. "
                + "Set both MOD ITEMS removal sliders to 0% before building; original PIR content stays untouched.");

        var changes = 0;
        var worldMatches = 0;
        var corpseMatches = 0;
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var mod in AuditedLootMods)
        {
            if (!(worldRemoval > 0.0 && mod.WorldBranches.Length > 0)
                && !(corpseRemoval > 0.0 && mod.CorpseBranches.Length > 0))
                continue;

            var candidates = packagePaths
                .Where(path => Path.GetFileName(path).Contains(mod.Name, StringComparison.OrdinalIgnoreCase)
                    || mod.Packages.Any(p => Path.GetFileName(path)
                        .Equals(p.Name, StringComparison.OrdinalIgnoreCase)))
                .ToArray();
            if (candidates.Length == 0)
                continue;

            foreach (var package in mod.Packages)
            {
                var matches = candidates
                    .Where(path => Path.GetFileName(path).Equals(package.Name, StringComparison.OrdinalIgnoreCase))
                    .ToArray();
                if (matches.Length != 1)
                    throw new InvalidDataException(
                        "Unsupported or incomplete " + mod.Name
                        + " installation: expected exactly one " + package.Name
                        + ". No CFG was generated for changed mod item controls.");
                using var stream = File.OpenRead(matches[0]);
                var actual = Convert.ToHexString(SHA256.HashData(stream));
                if (!actual.Equals(package.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException(
                        "Installed " + mod.Name + " PAK is not the audited version ("
                        + package.Name + "). No mod-item removal BPatch was generated.");
            }

            void ScaleBranches(
                (string Root, string Branch)[] branches, double removal, double sourceChance)
            {
                if (removal <= 0.0)
                    return;
                if (sourceChance <= 0.0 || sourceChance > 1.0)
                    throw new InvalidDataException("Invalid audited source Chance for " + mod.Name);
                var effectiveChance = Math.Round(sourceChance * (1.0 - removal), 6,
                    MidpointRounding.AwayFromZero);
                foreach (var (root, branch) in branches)
                {
                    // Patch only the named mod-added acquisition branch, NEVER
                    // the shared mod-owned item pool (also used by merchants).
                    // All audited World/Corpse branches have exactly one
                    // PossibleItems leaf. Authored [*] resolves to [0] inside
                    // a newly-added named branch, as verified in original PAKs.
                    if (!seen.Add(root + "|" + branch))
                        throw new InvalidDataException("Duplicate audited loot branch: " + root + "/" + branch);
                    var document = GetDocument(documents, "GameData/ItemGeneratorPrototypes.cfg");
                    if (removal >= 1.0)
                        document.AddRemovalNode(root, new[] { "ItemGenerator" }, branch);
                    else
                        document.AddAssignment(root,
                            new[] { "ItemGenerator", branch, "PossibleItems", "[0]" },
                            "Chance", effectiveChance.ToString("0.######", CultureInfo.InvariantCulture));
                    changes++;
                }
            }
            if (worldRemoval > 0.0)
            {
                ScaleBranches(mod.WorldBranches, worldRemoval, mod.WorldSourceChance);
                if (mod.WorldBranches.Length > 0) worldMatches++;
            }
            if (corpseRemoval > 0.0)
            {
                ScaleBranches(mod.CorpseBranches, corpseRemoval, mod.CorpseSourceChance);
                if (mod.CorpseBranches.Length > 0) corpseMatches++;
            }
        }

        if ((worldRemoval > 0.0 && worldMatches == 0) || (corpseRemoval > 0.0 && corpseMatches == 0))
            throw new InvalidDataException(
                "MOD ITEMS: no matching audited mod PAK set was found for "
                + (worldRemoval > 0.0 && worldMatches == 0 ? "WORLD" : "CORPSE")
                + ". Install the documented exact mod version or reset the checkbox. "
                + "No unverified mod content was changed.");

        return changes;
    }
}
