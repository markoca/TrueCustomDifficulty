using System.IO;
using System.Text.Json;

namespace TrueCustomDifficulty.Services;

/// <summary>Portable installation target, independent from presets and gameplay settings.</summary>
public sealed class ModDestinationSettingsService
{
    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "user-settings.json");

    private sealed class UserSettings
    {
        public string ModsFolder { get; set; } = string.Empty;
    }

    public string LoadPath()
    {
        if (!File.Exists(SettingsPath))
            return string.Empty;
        try
        {
            return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(SettingsPath))?.ModsFolder ?? "";
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Invalid user-settings.json; correct or delete the file.", ex);
        }
    }

    public string RequireConfiguredPath()
    {
        var saved = LoadPath();
        if (string.IsNullOrWhiteSpace(saved))
            throw new InvalidDataException("Set your ~mods folder in Settings before building.");
        return ValidatePath(saved);
    }

    public void SavePath(string value)
    {
        var path = ValidatePath(value);
        var temp = SettingsPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, JsonSerializer.Serialize(
                new UserSettings { ModsFolder = path },
                new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temp, SettingsPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temp))
                File.Delete(temp);
        }
    }

    public static string ValidatePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidDataException("Choose the game's ~mods folder first.");

        string full;
        try
        {
            full = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path.Trim().Trim('"')))
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidDataException("Invalid ~mods folder path.", ex);
        }

        if (!string.Equals(Path.GetFileName(full), "~mods", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Choose the ~mods folder itself, not its parent Paks folder.");
        if (!Directory.Exists(full))
            throw new InvalidDataException("The selected ~mods folder does not exist.");
        return full;
    }
}
