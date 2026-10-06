using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using TrueCustomDifficulty.Models;

namespace TrueCustomDifficulty.Services;

public sealed class PresetService
{
    public const int CurrentPresetVersion = 1;

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public void Save(PresetFile preset, string path)
    {
        Validate(preset);

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var tempPath = fullPath + ".tmp";
        try
        {
            var json = JsonSerializer.Serialize(preset, Options);
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    public PresetFile Load(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var preset = JsonSerializer.Deserialize<PresetFile>(json, Options)
                          ?? throw new InvalidDataException("Preset file could not be parsed.");
            Validate(preset);
            return preset;
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Preset file is not valid JSON or uses an invalid preset value.", ex);
        }
    }

    private static void Validate(PresetFile preset)
    {
        if (preset.PresetFormatVersion <= 0)
            throw new InvalidDataException("Preset format version must be a positive number.");

        if (preset.PresetFormatVersion > CurrentPresetVersion)
            throw new InvalidDataException(
                $"Preset format version {preset.PresetFormatVersion} is newer than this tool supports ({CurrentPresetVersion}).");

        if (string.IsNullOrWhiteSpace(preset.ReferenceDifficulty))
            throw new InvalidDataException("Preset does not contain a Base Preset.");

        preset.Values ??= new Dictionary<string, PresetValue>(StringComparer.Ordinal);

        foreach (var (id, value) in preset.Values)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new InvalidDataException("Preset contains an empty parameter ID.");

            if (value is null)
                throw new InvalidDataException($"Preset parameter '{id}' has no value object.");

            if (value.Number is double number && !double.IsFinite(number))
                throw new InvalidDataException($"Preset parameter '{id}' contains a non-finite number.");

            if (value.MinNumber is double min && !double.IsFinite(min))
                throw new InvalidDataException($"Preset parameter '{id}' contains a non-finite minimum.");

            if (value.MaxNumber is double max && !double.IsFinite(max))
                throw new InvalidDataException($"Preset parameter '{id}' contains a non-finite maximum.");
        }
    }
}
