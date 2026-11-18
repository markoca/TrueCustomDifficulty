using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Project-wide rule for changed values: only the numeric tokens of a readout
/// turn gold; words and units ("ahead", "every", "s", "%", "×", "HP") keep
/// the neutral ink and the base size; the numbers also grow slightly. Call sites pass <see cref="Numbers"/> as the text brush;
/// each control's CreateText resolves it with <see cref="Resolve"/> and
/// colors the digits with <see cref="Highlight"/>.
/// </summary>
internal static class ReadoutInk
{
    private static readonly Regex Number = new(@"[-+]?\d+(?:[.,]\d+)*", RegexOptions.Compiled);
    private static readonly Dictionary<(Brush Accent, Brush Base), SolidColorBrush> Markers = new();
    private static readonly Dictionary<Brush, (Brush Accent, Brush Base)> ByMarker =
        new(ReferenceEqualityComparer.Instance);

    /// <summary>Brush token meaning "base ink, digits in accent".</summary>
    public static Brush Numbers(Brush accent, Brush baseInk)
    {
        if (!Markers.TryGetValue((accent, baseInk), out var marker))
        {
            // Unique instance; never painted itself (Resolve swaps it out).
            marker = new SolidColorBrush(Colors.Transparent);
            Markers[(accent, baseInk)] = marker;
            ByMarker[marker] = (accent, baseInk);
        }
        return marker;
    }

    /// <summary>Ink to create the text with, plus the number ink (or null).</summary>
    public static (Brush Ink, Brush? NumberInk) Resolve(Brush brush)
        => ByMarker.TryGetValue(brush, out var pair) ? (pair.Base, pair.Accent) : (brush, null);

    /// <summary>How much bigger a changed number is than its text (DIP).</summary>
    public const double NumberGrowth = 1.2;

    /// <summary>Manual variant: one number range gold and grown.</summary>
    public static void Mark(FormattedText formatted, Brush accent, int index, int length, double emSize)
    {
        formatted.SetForegroundBrush(accent, index, length);
        formatted.SetFontSize(Math.Max(9.0, emSize) + NumberGrowth, index, length);
    }

    /// <summary>
    /// Changed numbers turn gold AND grow by <see cref="NumberGrowth"/>; the
    /// words and units around them keep the base ink and the base size.
    /// Callers pass the base size, never a bigger "changed" size.
    /// </summary>
    public static FormattedText Highlight(FormattedText formatted, string value, Brush? numberInk, double emSize)
    {
        if (numberInk is null)
            return formatted;
        foreach (Match match in Number.Matches(value))
        {
            formatted.SetForegroundBrush(numberInk, match.Index, match.Length);
            formatted.SetFontSize(emSize + NumberGrowth, match.Index, match.Length);
        }
        return formatted;
    }
}
