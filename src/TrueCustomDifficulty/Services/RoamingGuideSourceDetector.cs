using System.Diagnostics;
using System.IO;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Detect source presence rather than guessing version from DLC, Steam build IDs,
/// or the 2.0.5 catalog. No external tool beyond the existing repak is needed.
/// </summary>
public static class RoamingGuideSourceDetector
{
    private const string SourceFilename = "FastTravelEconomyPrototypes.cfg";
    private static readonly Dictionary<string, bool> Cache = new(StringComparer.Ordinal);
    private static readonly object Sync = new();

    public static GameInstallInfo Resolve(
        GameInstallInfo? detected, string modsFolder, string repakPath)
    {
        var root = RootFromModsPath(modsFolder);
        if (root is null && !string.IsNullOrWhiteSpace(detected?.RootPath))
            root = detected.RootPath;

        if (root is null)
            throw new InvalidDataException(
                "Cannot identify the installed STALKER 2 version for Guide Travel Fees. " +
                "Select the real game's Stalker2/Content/Paks/~mods destination in Settings.");

        var paks = Path.Combine(root, "Stalker2", "Content", "Paks");
        if (!Directory.Exists(paks))
            throw new InvalidDataException("STALKER 2 Paks directory is missing: " + paks);

        // Only the real, top-level game archives. Never inspect user mods.
        var sources = Directory.EnumerateFiles(paks, "*.pak", SearchOption.TopDirectoryOnly)
            .Select(p => new FileInfo(p))
            .OrderByDescending(p => p.Name.Contains("_P", StringComparison.OrdinalIgnoreCase))
            .ThenBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (sources.Length == 0)
            throw new InvalidDataException("No installed base-game PAKs found for Guide Travel Fees.");

        var signature = string.Join("|", sources.Select(
            p => p.FullName + ":" + p.Length + ":" + p.LastWriteTimeUtc.Ticks));
        bool supports;
        lock (Sync)
        {
            if (!Cache.TryGetValue(signature, out supports))
            {
                supports = sources.Any(p =>
                    ContainsRoamingGuideSource(ListPak(repakPath, p.FullName)));
                Cache[signature] = supports;
            }
        }

        return new GameInstallInfo
        {
            RootPath = root,
            HasDlc1 = detected?.HasDlc1 == true,
            SupportsRoamingGuideEconomy = supports
        };
    }

    private static string? RootFromModsPath(string modsFolder)
    {
        var mods = Path.GetFullPath(modsFolder);
        if (!Path.GetFileName(mods).Equals("~mods", StringComparison.OrdinalIgnoreCase))
            return null;
        var paks = Directory.GetParent(mods);
        if (paks?.Name.Equals("Paks", StringComparison.OrdinalIgnoreCase) != true)
            return null;
        var content = paks.Parent;
        if (content?.Name.Equals("Content", StringComparison.OrdinalIgnoreCase) != true)
            return null;
        var stalker = content.Parent;
        if (stalker?.Name.Equals("Stalker2", StringComparison.OrdinalIgnoreCase) != true)
            return null;
        return stalker.Parent?.FullName;
    }

    private static string ListPak(string repakPath, string pakPath)
    {
        var info = new ProcessStartInfo
        {
            FileName = repakPath,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        info.ArgumentList.Add("list");
        info.ArgumentList.Add(pakPath);
        try
        {
            using var process = Process.Start(info)
                ?? throw new InvalidDataException("repak list could not start.");
            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(60000))
            {
                process.Kill(entireProcessTree: true);
                throw new InvalidDataException("Timed out listing installed CFG sources: " + pakPath);
            }
            var listing = stdout.GetAwaiter().GetResult();
            var error = stderr.GetAwaiter().GetResult();
            if (process.ExitCode != 0)
                throw new InvalidDataException(
                    $"Failed to inspect {pakPath} (repak exit {process.ExitCode}): {error.Trim()}");
            return listing;
        }
        catch (Exception ex) when (ex is IOException or System.ComponentModel.Win32Exception)
        {
            throw new InvalidDataException("Failed to inspect game PAK via repak: " + pakPath, ex);
        }
    }

    public static bool ContainsRoamingGuideSource(string listing)
    {
        return listing.Split('\n').Any(line =>
        {
            var name = line.Trim().Replace('\\', '/');
            var path = "/Stalker2/Content/GameLite/GameData/" + SourceFilename;
            return name.EndsWith(path, StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(path + ".bin", StringComparison.OrdinalIgnoreCase)
                || name.Equals(path[1..], StringComparison.OrdinalIgnoreCase)
                || name.Equals(path[1..] + ".bin", StringComparison.OrdinalIgnoreCase);
        });
    }
}
