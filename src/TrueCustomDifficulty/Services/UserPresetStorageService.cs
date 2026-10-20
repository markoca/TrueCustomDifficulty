using System.IO;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Per-user mutable preset storage. Factory and legacy portable presets stay
/// readable beside the executable; custom saves do not require app-folder writes.
/// </summary>
public sealed class UserPresetStorageService
{
    private readonly string _directory;

    public UserPresetStorageService()
        : this(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
    {
    }

    internal UserPresetStorageService(string localAppDataDirectory)
    {
        if (string.IsNullOrWhiteSpace(localAppDataDirectory))
            throw new IOException("Cannot save presets because LocalAppData is unavailable.");

        _directory = Path.Combine(
            Path.GetFullPath(localAppDataDirectory),
            "TrueCustomDifficulty",
            "Presets");
    }

    public string EnsureDirectory()
    {
        Directory.CreateDirectory(_directory);
        return _directory;
    }

    public IReadOnlyList<PresetListItem> List()
    {
        if (!Directory.Exists(_directory))
            return Array.Empty<PresetListItem>();

        return Directory.EnumerateFiles(_directory, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => Path.GetFileNameWithoutExtension(path), StringComparer.OrdinalIgnoreCase)
            .Select(path => new PresetListItem
            {
                Name = Path.GetFileNameWithoutExtension(path),
                Path = path
            })
            .ToList();
    }
}
