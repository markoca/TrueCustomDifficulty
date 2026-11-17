using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// Project-wide rule for changed values: only the numeric tokens of a readout
/// turn gold; words and units ("ahead", "every", "s", "%", "x", "HP") keep
/// the neutral ink. Call sites pass <see cref="Numbers"/> as the text brush;
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

    public static FormattedText Highlight(FormattedText formatted, string value, Brush? numberInk)
    {
        if (numberInk is null)
            return formatted;
        foreach (Match match in Number.Matches(value))
            formatted.SetForegroundBrush(numberInk, match.Index, match.Length);
        return formatted;
    }
}
