using System.Text;
using Spectre.Console;

namespace Ccx.Tui;

/// <summary>
/// Renders inline diffs with color highlighting.
/// </summary>
public static class DiffRenderer
{
    public static string RenderInlineDiff(string oldText, string newText)
    {
        var oldLines = oldText.Split('\n');
        var newLines = newText.Split('\n');
        var sb = new StringBuilder();

        var maxOld = oldLines.Length;
        var maxNew = newLines.Length;
        var maxLen = Math.Max(maxOld, maxNew);

        for (var i = 0; i < maxLen; i++)
        {
            if (i < maxOld && i < maxNew)
            {
                if (oldLines[i] == newLines[i])
                {
                    sb.AppendLine($"  {Markup.Escape(newLines[i])}");
                }
                else
                {
                    sb.AppendLine($"[red]- {Markup.Escape(oldLines[i])}[/]");
                    sb.AppendLine($"[green]+ {Markup.Escape(newLines[i])}[/]");
                }
            }
            else if (i < maxOld)
            {
                sb.AppendLine($"[red]- {Markup.Escape(oldLines[i])}[/]");
            }
            else
            {
                sb.AppendLine($"[green]+ {Markup.Escape(newLines[i])}[/]");
            }
        }

        return sb.ToString().TrimEnd();
    }
}
