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

    [JsonPropertyName("medicalOverride")]
    public bool MedicalOverride { get; set; }

    // Optional in older presets: independent per-role, per-item SELL exclusions.
    [JsonPropertyName("traderItemOverrides")]
    public string TraderItemOverrides { get; set; } = string.Empty;

    // Optional backwards-compatible trade-only bonus fields. Older factory
    // presets omit them and load with GEAR OFF and the 2% stored chance.
    [JsonPropertyName("genericNpcRareGearEnabled")]
    public bool GenericNpcRareGearEnabled { get; set; }

    // New 0–100% mod generator removal fractions. Null means an older preset,
    // which may contain the legacy binary flags below.
    [JsonPropertyName("worldModItemsRemoval")]
    public double? WorldModItemsRemoval { get; set; }

    [JsonPropertyName("corpseModItemsRemoval")]
    public double? CorpseModItemsRemoval { get; set; }

    // Load-only legacy toggles. Null on saving means they are not serialized.
    [JsonPropertyName("worldModItemsBlocked")]
    public bool? LegacyWorldModItemsBlocked { get; set; }

    [JsonPropertyName("corpseModItemsBlocked")]
    public bool? LegacyCorpseModItemsBlocked { get; set; }

    [JsonPropertyName("genericNpcRareGearChance")]
    public double GenericNpcRareGearChance { get; set; } = 0.02;

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
