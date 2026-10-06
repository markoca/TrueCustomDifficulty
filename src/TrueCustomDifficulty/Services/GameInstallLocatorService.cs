using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.Services;

public sealed class GameInstallLocatorService
{
    private const string SteamAppId = "1643320";
    private const string ExpectedFolderName = "S.T.A.L.K.E.R. 2 Heart of Chornobyl";

    public GameInstallInfo? Detect()
    {
        foreach (var candidate in EnumerateGameCandidates())
        {
            var root = NormalizeExistingDirectory(candidate.Path);
            if (root is null || !LooksLikeGameRoot(root))
                continue;

            return new GameInstallInfo
            {
                RootPath = root,
                DetectionSource = candidate.Source,
                HasDlc1 = LooksLikeDlc1(Path.Combine(root, "DLC1"))
            };
        }

        return null;
    }

    private static IEnumerable<GameCandidate> EnumerateGameCandidates()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in EnumerateRawCandidates())
        {
            if (string.IsNullOrWhiteSpace(candidate.Path))
                continue;

            string normalized;
            try
            {
                normalized = Path.GetFullPath(candidate.Path.Trim().Trim('"'));
            }
            catch
            {
                continue;
            }

            if (seen.Add(normalized))
                yield return new GameCandidate(normalized, candidate.Source);
        }
    }

    private static IEnumerable<GameCandidate> EnumerateRawCandidates()
    {
        var explicitPath = Environment.GetEnvironmentVariable("STALKER2_GAME_DIR");
        if (!string.IsNullOrWhiteSpace(explicitPath))
            yield return new GameCandidate(explicitPath, "STALKER2_GAME_DIR");

        foreach (var steamRoot in EnumerateSteamRoots())
        {
            var install = ResolveSteamAppInstall(steamRoot);
            if (!string.IsNullOrWhiteSpace(install))
                yield return new GameCandidate(install, $"Steam manifest ({steamRoot})");

            // Fallback for a normal Steam library even if the manifest cannot be read.
            yield return new GameCandidate(
                Path.Combine(steamRoot, "steamapps", "common", ExpectedFolderName),
                $"Steam library ({steamRoot})");
        }

        // Wine development fallback. Wine exposes the Linux filesystem through Z:.
        // This keeps Linux testing automatic without changing the Windows release flow.
        const string wineHomeRoot = @"Z:\home";
        if (Directory.Exists(wineHomeRoot))
        {
            IEnumerable<string> homes;
            try
            {
                homes = Directory.EnumerateDirectories(wineHomeRoot);
            }
            catch
            {
                homes = Array.Empty<string>();
            }

            foreach (var home in homes)
            {
                yield return new GameCandidate(
                    Path.Combine(home, ".local", "share", "Steam", "steamapps", "common", ExpectedFolderName),
                    "Wine Z: Steam fallback");
            }
        }
    }

    private static IEnumerable<string> EnumerateSteamRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var registryPath in ReadSteamRegistryRoots())
            AddSteamRootWithLibraries(registryPath, roots);

        var programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (!string.IsNullOrWhiteSpace(programFilesX86))
            AddSteamRootWithLibraries(Path.Combine(programFilesX86, "Steam"), roots);

        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        if (!string.IsNullOrWhiteSpace(programFiles))
            AddSteamRootWithLibraries(Path.Combine(programFiles, "Steam"), roots);

        return roots;
    }

    private static IEnumerable<string> ReadSteamRegistryRoots()
    {
        var probes = new[]
        {
            (RegistryHive.CurrentUser, RegistryView.Default, @"Software\Valve\Steam", "SteamPath"),
            (RegistryHive.LocalMachine, RegistryView.Registry64, @"SOFTWARE\WOW6432Node\Valve\Steam", "InstallPath"),
            (RegistryHive.LocalMachine, RegistryView.Registry32, @"SOFTWARE\Valve\Steam", "InstallPath")
        };

        foreach (var probe in probes)
        {
            string? value = null;
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(probe.Item1, probe.Item2);
                using var key = baseKey.OpenSubKey(probe.Item3);
                value = key?.GetValue(probe.Item4) as string;
            }
            catch
            {
                // Registry probing is best-effort; fallbacks cover portable/Wine installs.
            }

            if (!string.IsNullOrWhiteSpace(value))
                yield return value;
        }
    }

    private static void AddSteamRootWithLibraries(string? steamRoot, HashSet<string> roots)
    {
        if (string.IsNullOrWhiteSpace(steamRoot))
            return;

        var root = NormalizeExistingDirectory(steamRoot);
        if (root is null)
            return;

        roots.Add(root);

        var libraryFile = Path.Combine(root, "steamapps", "libraryfolders.vdf");
        if (!File.Exists(libraryFile))
            return;

        string text;
        try
        {
            text = File.ReadAllText(libraryFile);
        }
        catch
        {
            return;
        }

        foreach (Match match in Regex.Matches(text, "\\\"path\\\"\\s+\\\"(?<path>[^\\\"]+)\\\"", RegexOptions.IgnoreCase))
        {
            var raw = match.Groups["path"].Value.Replace("\\\\", "\\");
            var libraryRoot = NormalizeExistingDirectory(raw);
            if (libraryRoot is not null)
                roots.Add(libraryRoot);
        }
    }

    private static string? ResolveSteamAppInstall(string steamRoot)
    {
        var manifest = Path.Combine(steamRoot, "steamapps", $"appmanifest_{SteamAppId}.acf");
        if (!File.Exists(manifest))
            return null;

        string text;
        try
        {
            text = File.ReadAllText(manifest);
        }
        catch
        {
            return null;
        }

        var match = Regex.Match(text, "\\\"installdir\\\"\\s+\\\"(?<name>[^\\\"]+)\\\"", RegexOptions.IgnoreCase);
        if (!match.Success)
            return null;

        return Path.Combine(steamRoot, "steamapps", "common", match.Groups["name"].Value);
    }

    private static string? NormalizeExistingDirectory(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        try
        {
            var full = Path.GetFullPath(path.Trim().Trim('"'));
            return Directory.Exists(full) ? full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) : null;
        }
        catch
        {
            return null;
        }
    }

    private static bool LooksLikeGameRoot(string root)
    {
        var pakDirectory = Path.Combine(root, "Stalker2", "Content", "Paks");
        if (!Directory.Exists(pakDirectory))
            return false;

        try
        {
            return Directory.EnumerateFiles(pakDirectory, "pakchunk0-Windows.pak", SearchOption.TopDirectoryOnly).Any();
        }
        catch
        {
            return false;
        }
    }

    private static bool LooksLikeDlc1(string dlcDirectory)
    {
        if (!Directory.Exists(dlcDirectory))
            return false;

        try
        {
            var files = Directory.EnumerateFiles(dlcDirectory, "S2_DLC1Stalker2-Windows.*", SearchOption.TopDirectoryOnly)
                .Select(Path.GetExtension)
                .Where(extension => !string.IsNullOrWhiteSpace(extension))
                .Select(extension => extension!.ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return files.Contains(".pak") && files.Contains(".utoc") && files.Contains(".ucas");
        }
        catch
        {
            return false;
        }
    }

    private sealed record GameCandidate(string Path, string Source);
}
