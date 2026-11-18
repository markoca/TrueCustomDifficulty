using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace TrueCustomDifficulty.Controls;

/// <summary>
/// XAML twin of <see cref="ReadoutInk"/>: a TextBlock whose value changed
/// shows only its numbers in gold and <see cref="ReadoutInk.NumberGrowth"/>
/// bigger. Words and units keep the TextBlock's own ink and size.
/// Use instead of Text: NumberRuns.Text, NumberRuns.Highlight, NumberRuns.Accent.
/// </summary>
public static class NumberRuns
{
    private static readonly Regex Number = new(@"[-+−]?\d+(?:[.,]\d+)*", RegexOptions.Compiled);

    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text", typeof(string), typeof(NumberRuns), new PropertyMetadata(null, Changed));

    public static readonly DependencyProperty HighlightProperty = DependencyProperty.RegisterAttached(
        "Highlight", typeof(bool), typeof(NumberRuns), new PropertyMetadata(false, Changed));

    public static readonly DependencyProperty AccentProperty = DependencyProperty.RegisterAttached(
        "Accent", typeof(Brush), typeof(NumberRuns), new PropertyMetadata(null, Changed));

    public static string? GetText(DependencyObject d) => (string?)d.GetValue(TextProperty);
    public static void SetText(DependencyObject d, string? value) => d.SetValue(TextProperty, value);
    public static bool GetHighlight(DependencyObject d) => (bool)d.GetValue(HighlightProperty);
    public static void SetHighlight(DependencyObject d, bool value) => d.SetValue(HighlightProperty, value);
    public static Brush? GetAccent(DependencyObject d) => (Brush?)d.GetValue(AccentProperty);
    public static void SetAccent(DependencyObject d, Brush? value) => d.SetValue(AccentProperty, value);

    private static void Changed(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock block)
            Rebuild(block);
    }

    private static void Rebuild(TextBlock block)
    {
        var text = GetText(block) ?? string.Empty;
        var accent = GetAccent(block);
        block.Inlines.Clear();
        if (!GetHighlight(block) || accent is null)
        {
            block.Inlines.Add(new Run(text));
            return;
        }

        var last = 0;
        foreach (Match match in Number.Matches(text))
        {
            if (match.Index > last)
                block.Inlines.Add(new Run(text[last..match.Index]));
            block.Inlines.Add(new Run(match.Value)
            {
                Foreground = accent,
                FontSize = block.FontSize + ReadoutInk.NumberGrowth
            });
            last = match.Index + match.Length;
        }
        if (last < text.Length)
            block.Inlines.Add(new Run(text[last..]));
    }
}
