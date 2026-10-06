namespace TrueCustomDifficulty.Models;

public sealed class PresetListItem
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty;
    public override string ToString() => Name;
}
