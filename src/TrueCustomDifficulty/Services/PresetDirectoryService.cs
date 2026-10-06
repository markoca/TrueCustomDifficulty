using System.IO;
using System.Reflection;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Owns the portable Presets directory beside the executable.
/// Owns the seven shipped factory presets and migrates away the retired vanilla-named factory files.
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
        "Hardcore",
        "Hardcore+"
    };

    private static readonly string[] LegacyFactoryPresetNames = { "Novice", "Stalker", "Veteran", "Master" };

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
            if (!File.Exists(destination))
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

    private static void WriteEmbedded(string resourceName, string destinationPath)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"Embedded preset resource '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        File.WriteAllText(destinationPath, reader.ReadToEnd());
    }
}
