using System.Text.Json.Serialization;

namespace TrueCustomDifficulty.Models;

public sealed class PresetFile
{
    [JsonPropertyName("profileVersion")]
    public int PresetFormatVersion { get; set; } = 1;

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Custom Preset";

    [JsonPropertyName("toolVersion")]
    public string ToolVersion { get; set; } = string.Empty;

    [JsonPropertyName("gameVersion")]
    public string GameVersion { get; set; } = string.Empty;

    [JsonPropertyName("referenceDifficulty")]
    public string ReferenceDifficulty { get; set; } = "Stalker";

    // UI mode only; it changes allowed editor ranges, never creates a CFG field.
    [JsonPropertyName("drinkDurationOverride")]
    public bool DrinkDurationOverride { get; set; }

    [JsonPropertyName("savedAtUtc")]
    public DateTimeOffset? SavedAtUtc { get; set; }

    [JsonPropertyName("values")]
    public Dictionary<string, PresetValue> Values { get; set; } = new(StringComparer.Ordinal);
}

public sealed class PresetValue
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("number")]
    public double? Number { get; set; }

    [JsonPropertyName("minNumber")]
    public double? MinNumber { get; set; }

    [JsonPropertyName("maxNumber")]
    public double? MaxNumber { get; set; }

    [JsonPropertyName("boolean")]
    public bool? Boolean { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}
