using System.Text.Json;
using System.Text.Json.Serialization;

namespace TrueCustomDifficulty.Models;

public sealed class ParameterCatalog
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = string.Empty;

    [JsonPropertyName("app")]
    public AppDefinition App { get; set; } = new();

    [JsonPropertyName("source")]
    public SourceDefinition Source { get; set; } = new();

    [JsonPropertyName("scope")]
    public ScopeDefinition Scope { get; set; } = new();

    [JsonPropertyName("difficultyProfiles")]
    public Dictionary<string, DifficultyProfileDefinition> DifficultyProfiles { get; set; } = new();

    [JsonPropertyName("tabs")]
    public List<TabDefinition> Tabs { get; set; } = new();

    [JsonPropertyName("parameters")]
    public List<ParameterDefinition> Parameters { get; set; } = new();

    [JsonPropertyName("sliderConstraints")]
    public List<SliderConstraintDefinition> SliderConstraints { get; set; } = new();
}

public sealed class SliderConstraintDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public string Order { get; set; } = string.Empty;

    [JsonPropertyName("parameterIds")]
    public List<string> ParameterIds { get; set; } = new();
}

public sealed class AppDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("platform")]
    public string Platform { get; set; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("defaultReferenceDifficulty")]
    public string DefaultReferenceDifficulty { get; set; } = "Stalker";
}

public sealed class SourceDefinition
{
    [JsonPropertyName("gameVersion")]
    public string GameVersion { get; set; } = string.Empty;

    [JsonPropertyName("dumpLabel")]
    public string DumpLabel { get; set; } = string.Empty;

    [JsonPropertyName("dlcAware")]
    public bool DlcAware { get; set; }
}

public sealed class ScopeDefinition
{
    [JsonPropertyName("environmentPolicy")]
    public string EnvironmentPolicy { get; set; } = string.Empty;
}

public sealed class DifficultyProfileDefinition
{
    [JsonPropertyName("sid")]
    public string Sid { get; set; } = string.Empty;

    [JsonPropertyName("uiName")]
    public string UiName { get; set; } = string.Empty;
}

public sealed class TabDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("order")]
    public int Order { get; set; }
}

public sealed class ParameterDefinition
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("tab")]
    public string Tab { get; set; } = string.Empty;

    [JsonPropertyName("group")]
    public string Group { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("rationale")]
    public string Rationale { get; set; } = string.Empty;

    [JsonPropertyName("logic")]
    public string Logic { get; set; } = string.Empty;

    [JsonPropertyName("interaction")]
    public string Interaction { get; set; } = string.Empty;

    [JsonPropertyName("control")]
    public string Control { get; set; } = string.Empty;

    [JsonPropertyName("valueKind")]
    public string ValueKind { get; set; } = string.Empty;

    [JsonPropertyName("display")]
    public DisplayDefinition? Display { get; set; }

    [JsonPropertyName("vanilla")]
    public Dictionary<string, JsonElement> Vanilla { get; set; } = new();

    [JsonPropertyName("default")]
    public DefaultDefinition Default { get; set; } = new();

    [JsonPropertyName("slider")]
    public SliderDefinition? Slider { get; set; }

    [JsonPropertyName("targets")]
    public List<TargetDefinition> Targets { get; set; } = new();

    [JsonPropertyName("support")]
    public SupportDefinition Support { get; set; } = new();

    [JsonPropertyName("lootHint")]
    public LootHintDefinition? LootHint { get; set; }

    [JsonPropertyName("markers")]
    public List<ReferenceMarkerDefinition> Markers { get; set; } = new();

    [JsonPropertyName("requiresToggle")]
    public string RequiresToggle { get; set; } = string.Empty;

    // Audit/catalog entries can remain fully mapped while being intentionally
    // excluded from the normal player-facing UI. Default true preserves
    // existing catalog behavior when the field is omitted.
    [JsonPropertyName("exposeInUi")]
    public bool ExposeInUi { get; set; } = true;
}

public sealed class ReferenceMarkerDefinition
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("value")]
    public double Value { get; set; }
}

public sealed class DisplayDefinition
{
    [JsonPropertyName("primary")]
    public string Primary { get; set; } = string.Empty;

    [JsonPropertyName("rawFormat")]
    public string RawFormat { get; set; } = string.Empty;

    [JsonPropertyName("valueLabel")]
    public string ValueLabel { get; set; } = string.Empty;

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;
}

public sealed class DefaultDefinition
{
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;
}

public sealed class SliderDefinition
{
    [JsonPropertyName("min")]
    public double Min { get; set; }

    [JsonPropertyName("max")]
    public double Max { get; set; }

    [JsonPropertyName("step")]
    public double Step { get; set; }

    // By default percentage-based controls snap in display space (5 percentage points).
    // Set this to "absolute" for point-based controls such as stamina SP and
    // bleeding points, where a clean gameplay-unit increment is more useful.
    [JsonPropertyName("stepMode")]
    public string StepMode { get; set; } = string.Empty;
}

public sealed class SupportDefinition
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("notes")]
    public List<string> Notes { get; set; } = new();
}

public sealed class TargetDefinition
{
    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("file")]
    public string File { get; set; } = string.Empty;

    [JsonPropertyName("prototype")]
    public string Prototype { get; set; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("member")]
    public string Member { get; set; } = string.Empty;
}

public sealed class LootHintDefinition
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("chance")]
    public double? Chance { get; set; }

    [JsonPropertyName("min")]
    public double? Min { get; set; }

    [JsonPropertyName("max")]
    public double? Max { get; set; }

    [JsonPropertyName("unit")]
    public string Unit { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("entries")]
    public List<LootHintEntryDefinition> Entries { get; set; } = new();
}

public sealed class LootHintEntryDefinition
{
    [JsonPropertyName("label")]
    public string Label { get; set; } = string.Empty;

    [JsonPropertyName("min")]
    public double Min { get; set; }

    [JsonPropertyName("max")]
    public double Max { get; set; }

    [JsonPropertyName("qualifier")]
    public string Qualifier { get; set; } = string.Empty;

    [JsonPropertyName("selection")]
    public string Selection { get; set; } = string.Empty;
}
