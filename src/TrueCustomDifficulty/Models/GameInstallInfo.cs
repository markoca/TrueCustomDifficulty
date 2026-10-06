using System.IO;

namespace TrueCustomDifficulty.Models;

public sealed class GameInstallInfo
{
    public string RootPath { get; init; } = string.Empty;
    public string DetectionSource { get; init; } = string.Empty;
    public bool HasDlc1 { get; init; }

    public string BasePakDirectory => Path.Combine(RootPath, "Stalker2", "Content", "Paks");
    public string Dlc1Directory => Path.Combine(RootPath, "DLC1");
    public string ModsDirectory => Path.Combine(RootPath, "Stalker2", "Content", "Paks", "~mods");
}
