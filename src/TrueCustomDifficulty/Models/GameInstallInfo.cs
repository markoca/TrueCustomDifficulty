namespace TrueCustomDifficulty.Models;

/// <summary>
/// Detected game installation for CFG source lookup and DLC1 scope.
/// Build Difficulty's ~mods destination is a separately configured player setting.
/// </summary>
public sealed class GameInstallInfo
{
    public string RootPath { get; init; } = string.Empty;
    public bool HasDlc1 { get; init; }
}
