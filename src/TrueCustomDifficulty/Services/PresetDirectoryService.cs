using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Owns the portable Presets directory beside the executable.
/// Owns the ten shipped factory presets and migrates away the retired vanilla-named factory files.
/// Unrelated user-created preset files are never deleted.
/// </summary>
public sealed class PresetDirectoryService
{
    private const string ReadmeResourceName = "TrueCustomDifficulty.Presets.README.md";
    private static readonly string[] FactoryPresetNames =
    {
        "Immersive",
        "Tactical",
        "Scavenger",
        "Mutant Hunter",
        "Anomaly Runner",
        "Mercenary",
        "Wanderer",
        "Outcast",
        "Hardcore",
        "Hardcore+"
    };

    private static readonly string[] LegacyFactoryPresetNames = { "Novice", "Stalker", "Veteran", "Master" };

    // SHA-1 Git blob identities of the previously shipped, unmodified factory JSONs.
    // Upgrade only exact stock files; locally edited presets remain the user's own.
    private static readonly Dictionary<string, string> PreviousFactoryBlobs = new(StringComparer.Ordinal)
    {
        ["Immersive"] = "2886bc8983cec7cf893b5584cdd95b903fee62b8",
        ["Tactical"] = "917da30873fc9558a94b45ba90bc3b6bdc808788",
        ["Scavenger"] = "87b9e5a7ebcedc67c659abdb2911e46fe7325cef",
        ["Mutant Hunter"] = "4aa35decba71fcc321f7102647d3a2610323a46d",
        ["Anomaly Runner"] = "2fdf56972b7bb118e87cdd7a75895bce489e7bfc",
        ["Hardcore"] = "fbc49bb05db2fa3e05acc7c13c04470e86fb44c5",
        ["Hardcore+"] = "af5987719650099df0565027bb2e33bbcbe14bff"
    };


    public string Ensure()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Presets");
        Directory.CreateDirectory(directory);

        var readmePath = Path.Combine(directory, "README.md");
        WriteEmbedded(ReadmeResourceName, readmePath);

        // v0.9.26.51 factory-preset migration:
        // the four old shipped presets are intentionally retired from the application.
        // Remove only those exact legacy factory filenames; unrelated user presets are untouched.
        foreach (var legacyName in LegacyFactoryPresetNames)
        {
            var legacyPath = Path.Combine(directory, $"{legacyName}.json");
            if (File.Exists(legacyPath))
                File.Delete(legacyPath);
        }

        foreach (var name in FactoryPresetNames)
        {
            var destination = Path.Combine(directory, $"{name}.json");
            if (!File.Exists(destination) || IsUnmodifiedPreviousFactory(name, destination))
                WriteEmbedded($"TrueCustomDifficulty.Presets.{name}.json", destination);
        }

        return directory;
    }

    public IReadOnlyList<PresetListItem> List()
    {
        var directory = Ensure();
        return Directory.EnumerateFiles(directory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => FactoryOrder(Path.GetFileNameWithoutExtension(path)))
            .ThenBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
            .Select(path => new PresetListItem
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path
            })
            .ToList();
    }

    private static int FactoryOrder(string name)
    {
        var index = Array.FindIndex(FactoryPresetNames, n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase));
        return index >= 0 ? index : FactoryPresetNames.Length;
    }

    private static bool IsUnmodifiedPreviousFactory(string name, string path)
    {
        if (!PreviousFactoryBlobs.TryGetValue(name, out var knownHash))
            return false;

        try
        {
            // Git object identity = SHA1("blob " + byte length + NUL + exact bytes).
            // This deliberately rejects every user-edited version, including edits
            // to metadata, rather than guessing whether it is still a factory file.
            var bytes = File.ReadAllBytes(path);
            var header = Encoding.ASCII.GetBytes($"blob {bytes.Length}\0");
            var input = new byte[header.Length + bytes.Length];
            Buffer.BlockCopy(header, 0, input, 0, header.Length);
            Buffer.BlockCopy(bytes, 0, input, header.Length, bytes.Length);
            var actual = Convert.ToHexString(SHA1.HashData(input));
            return actual.Equals(knownHash, StringComparison.OrdinalIgnoreCase);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void WriteEmbedded(string resourceName, string destinationPath)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"Embedded preset resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        File.WriteAllText(destinationPath, reader.ReadToEnd());
    }
}
