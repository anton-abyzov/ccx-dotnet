using System.Text;
using System.Text.RegularExpressions;

namespace Ccx.Tui;

/// <summary>
/// Converts markdown text to Spectre.Console markup.
/// </summary>
public static class MarkdownRenderer
{
    public static string ToMarkup(string markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return "";

        var lines = markdown.Split('\n');
        var sb = new StringBuilder();
        var inCodeBlock = false;
        var codeBlockLang = "";
        var codeBlockContent = new StringBuilder();

        foreach (var line in lines)
        {
            if (line.StartsWith("```"))
            {
                if (!inCodeBlock)
                {
                    inCodeBlock = true;
                    codeBlockLang = line.Length > 3 ? line[3..].Trim() : "";
                    codeBlockContent.Clear();
                }
                else
                {
                    inCodeBlock = false;
                    var code = Spectre.Console.Markup.Escape(codeBlockContent.ToString().TrimEnd());
                    var langLabel = string.IsNullOrEmpty(codeBlockLang) ? "" : $" [dim]({codeBlockLang})[/]";
                    sb.AppendLine($"[dim]───[/]{langLabel}");
                    sb.AppendLine($"[grey]{code}[/]");
                    sb.AppendLine("[dim]───[/]");
                }
                continue;
            }

            if (inCodeBlock)
            {
                codeBlockContent.AppendLine(line);
                continue;
            }

            sb.AppendLine(RenderInline(line));
        }

        return sb.ToString().TrimEnd();
    }

    private static string RenderInline(string line)
    {
        // Headers
        if (line.StartsWith("### "))
            return $"[bold yellow]{EscapeContent(line[4..])}[/]";
        if (line.StartsWith("## "))
            return $"[bold blue]{EscapeContent(line[3..])}[/]";
        if (line.StartsWith("# "))
            return $"[bold underline]{EscapeContent(line[2..])}[/]";

        // Horizontal rule
        if (line is "---" or "***" or "___")
            return "[dim]────────────────────────────────[/]";

        // Bold: **text**
        line = Regex.Replace(line, @"\*\*(.+?)\*\*", m => $"[bold]{EscapeContent(m.Groups[1].Value)}[/]");

        // Italic: *text*
        line = Regex.Replace(line, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", m => $"[italic]{EscapeContent(m.Groups[1].Value)}[/]");

        // Inline code: `text`
        line = Regex.Replace(line, @"`(.+?)`", m => $"[grey on grey23]{Spectre.Console.Markup.Escape(m.Groups[1].Value)}[/]");

        // Links: [text](url)
        line = Regex.Replace(line, @"\[(.+?)\]\((.+?)\)", m =>
            $"[link={Spectre.Console.Markup.Escape(m.Groups[2].Value)}]{EscapeContent(m.Groups[1].Value)}[/]");

        // List items
        if (line.StartsWith("- ") || line.StartsWith("* "))
            return $"  [dim]•[/] {line[2..]}";

        return line;
    }

    private static string EscapeContent(string text) => Spectre.Console.Markup.Escape(text);
}
