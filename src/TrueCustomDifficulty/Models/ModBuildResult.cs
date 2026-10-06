namespace TrueCustomDifficulty.Models;

public sealed class ModBuildResult
{
    public string OutputDirectory { get; init; } = string.Empty;
    public bool HasMod { get; init; }
    public int PatchFileCount { get; init; }
    public int ParameterCount { get; init; }
    public int EditCount { get; init; }
    public IReadOnlyList<string> Files { get; init; } = Array.Empty<string>();
}
