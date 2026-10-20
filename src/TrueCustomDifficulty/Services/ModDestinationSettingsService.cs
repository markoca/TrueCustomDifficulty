using System.IO;
using System.Text.Json;

namespace TrueCustomDifficulty.Services;

/// <summary>
/// Game mod destination settings. Prefer a portable file beside the EXE; when
/// Windows denies writes there, persist in the current user's LocalAppData.
/// </summary>
public sealed class ModDestinationSettingsService
{
    private const string FileName = "user-settings.json";
    private readonly string _portableSettingsPath;
    private readonly string? _userSettingsPath;
    private readonly Action<string, string> _writeFile;

    public ModDestinationSettingsService()
        : this(AppContext.BaseDirectory,
               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))
    {
    }

    // Path and writer injection keeps permission/fallback behavior testable
    // without relying on machine-specific filesystem ACLs or admin accounts.
    internal ModDestinationSettingsService(
        string executableDirectory,
        string localAppDataDirectory,
        Action<string, string>? writeFile = null)
    {
        _portableSettingsPath = Path.Combine(executableDirectory, FileName);
        _userSettingsPath = string.IsNullOrWhiteSpace(localAppDataDirectory)
            ? null
            : Path.Combine(localAppDataDirectory, "TrueCustomDifficulty", FileName);
        _writeFile = writeFile ?? WriteAtomically;
    }

    private string ReadPath =>
        _userSettingsPath is not null && File.Exists(_userSettingsPath)
            ? _userSettingsPath
            : _portableSettingsPath;

    private sealed class UserSettings
    {
        public string ModsFolder { get; set; } = string.Empty;
    }

    public string LoadPath()
    {
        var path = ReadPath;
        if (!File.Exists(path))
            return string.Empty;
        try
        {
            return JsonSerializer.Deserialize<UserSettings>(File.ReadAllText(path))?.ModsFolder ?? "";
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"Invalid {path}; correct or delete the file.", ex);
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
        var json = JsonSerializer.Serialize(
            new UserSettings { ModsFolder = path },
            new JsonSerializerOptions { WriteIndented = true });

        // Once a user-profile fallback exists, it remains authoritative.
        // Otherwise, prefer the portable file and only fall back if its
        // directory or existing settings file denies the write.
        if (_userSettingsPath is not null && File.Exists(_userSettingsPath))
        {
            SaveForCurrentUser(json);
            return;
        }

        try
        {
            _writeFile(_portableSettingsPath, json);
        }
        catch (UnauthorizedAccessException)
        {
            SaveForCurrentUser(json);
        }
    }

    private void SaveForCurrentUser(string json)
    {
        if (_userSettingsPath is null)
            throw new IOException(
                "Cannot save Settings: the application folder is not writable and LocalAppData is unavailable.");

        Directory.CreateDirectory(Path.GetDirectoryName(_userSettingsPath)!);
        _writeFile(_userSettingsPath, json);
    }

    private static void WriteAtomically(string destination, string json)
    {
        var temp = destination + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temp, json);
            File.Move(temp, destination, overwrite: true);
        }
        finally
        {
            // A failed atomic replace must not hide the original permission
            // error and prevent the LocalAppData fallback.
            try
            {
                if (File.Exists(temp))
                    File.Delete(temp);
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
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
