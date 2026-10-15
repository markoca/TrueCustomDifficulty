namespace TrueCustomDifficulty.Models;

/// <summary>
/// Completed packaging pipeline stages for the footer activity rail.
/// Deliberately not a timer-based/pretend subprocess percentage: repak
/// and retoc do not expose dependable fine-grained progress.
/// The WPF footer uses 0..6 completed milestones and a live accent pulse.
/// </summary>
public readonly record struct BuildProgressUpdate(int CompletedStages, string Phase);
