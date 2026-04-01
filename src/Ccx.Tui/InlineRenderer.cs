using Spectre.Console;

namespace Ccx.Tui;

/// <summary>
/// Inline terminal renderer — styled text that scrolls naturally, like Claude Code.
/// No full-screen TUI; just colored output with box-drawing borders.
/// </summary>
public static class InlineRenderer
{
    private const string Accent = "#cc7850";

    public static void RenderWelcome(IAnsiConsole console, string model, string auth, string cwd, int toolCount)
    {
        // Header rule
        console.MarkupLine($"[{Accent}]────── CCX-Dotnet v0.1.0 ──────────────────────[/]");

        var grid = new Grid();
        grid.AddColumn(new GridColumn().PadRight(1));
        grid.AddColumn(new GridColumn());

        var leftContent =
            "\n" +
            "  [bold]Welcome back![/]\n\n" +
            $"  [{Accent}]   \u256d\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u256e[/]\n" +
            $"  [{Accent}]   \u2502  \u25c9   \u25c9  \u2502[/]\n" +
            $"  [{Accent}]   \u2502    \u25e1    \u2502[/]\n" +
            $"  [{Accent}]   \u2570\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u256f[/]\n\n" +
            $"  [green]{Markup.Escape(model)}[/] \u00b7 {Markup.Escape(auth)}\n" +
            $"  [dim]{Markup.Escape(ShortenPath(cwd))}[/]\n" +
            $"  [dim]Tools: {toolCount}[/]\n";

        var rightContent =
            "\n" +
            $"[{Accent} bold]Tips for getting started[/]\n" +
            " Type a message to start coding\n" +
            " Run /init to create a CLAUDE.md\n" +
            " Type /help for commands\n" +
            " Ctrl+C to quit\n" +
            "[dim]\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500[/]\n" +
            $"[{Accent} bold]Recent activity[/]\n" +
            "[dim]No recent activity[/]\n\n\n";

        grid.AddRow(new Markup(leftContent), new Markup(rightContent));

        var panel = new Panel(grid)
            .Border(BoxBorder.Rounded)
            .BorderColor(new Color(204, 120, 80));
        console.Write(panel);
        console.WriteLine();
    }

    public static void RenderPrompt(IAnsiConsole console)
    {
        console.Markup($"[{Accent} bold]\u276f[/] ");
    }

    public static void RenderUserMessage(IAnsiConsole console, string text)
    {
        console.MarkupLine($"[bold on grey23] \u276f {Markup.Escape(text)} [/]");
    }

    public static void RenderToolStart(IAnsiConsole console, string name, string detail)
    {
        console.MarkupLine($"[green]\u25cf[/] [bold]{Markup.Escape(name)}({Markup.Escape(detail)})[/]");
    }

    public static void RenderToolOutput(IAnsiConsole console, string output)
    {
        var lines = output.Split('\n');
        const int maxLines = 5;

        foreach (var line in lines.Take(maxLines))
            console.MarkupLine($"  [dim]\u2514 {Markup.Escape(line)}[/]");

        if (lines.Length > maxLines)
            console.MarkupLine($"  [dim]\u2026 {lines.Length - maxLines} more lines[/]");
    }

    public static void RenderAssistantText(IAnsiConsole console, string text)
    {
        console.WriteLine(text);
    }

    public static void RenderSeparator(IAnsiConsole console)
    {
        var width = Math.Min(console.Profile.Width, 80);
        console.MarkupLine($"[dim]{new string('\u2500', width)}[/]");
    }

    public static void RenderFooter(IAnsiConsole console)
    {
        console.MarkupLine($"[dim]? for shortcuts                    \u25cf high \u00b7 /effort[/]");
    }

    private static string ShortenPath(string path)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return path.StartsWith(home) ? "~" + path[home.Length..] : path;
    }
}
