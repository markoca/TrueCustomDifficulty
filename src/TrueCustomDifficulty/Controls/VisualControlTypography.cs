namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Shared WPF drawing typography for like-for-like controls. Physical UI
/// geometry, colors and changed-only value highlighting remain owned by each
/// source-backed control and its locked visual family.
/// </summary>
internal static class VisualControlTypography
{
    // Centered accent headings on graph, radar and dashboard boards.
    public const double BoardTitle = 12.5;
    // Scale headings and subdued native graduations below/along a chart.
    public const double AxisTitle = 9.3;
    public const double AxisTick = 9.2;
    // Source-native horizontal track/card titles where clearance permits.
    public const double CardTitle = 10.8;
    // Unified stage captions below the Hunger and Fatigue profile plots.
    public const double SurvivalStageSummary = 10.8;
}
