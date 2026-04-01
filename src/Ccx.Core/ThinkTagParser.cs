using System.Text.RegularExpressions;

namespace Ccx.Core;

/// <summary>
/// Extracts &lt;think&gt;...&lt;/think&gt; content from text and separates it
/// from regular output for routing through the thinking display path.
/// </summary>
public static partial class ThinkTagParser
{
    [GeneratedRegex(@"<think>([\s\S]*?)</think>", RegexOptions.Compiled)]
    private static partial Regex ThinkTagRegex();

    /// <summary>
    /// Parses text for &lt;think&gt; tags, returning the thinking content and
    /// the remaining text with think tags removed.
    /// </summary>
    public static (string Thinking, string CleanedText) Extract(string text)
    {
        var matches = ThinkTagRegex().Matches(text);
        if (matches.Count == 0)
            return ("", text);

        var thinking = string.Join("\n", matches.Select(m => m.Groups[1].Value.Trim()));
        var cleaned = ThinkTagRegex().Replace(text, "").Trim();
        return (thinking, cleaned);
    }
}
