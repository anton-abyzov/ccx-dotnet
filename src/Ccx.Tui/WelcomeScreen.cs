using Spectre.Console;

namespace Ccx.Tui;

/// <summary>
/// Renders the Claude Code-style welcome screen with pet, tips, and status info.
/// </summary>
public static class WelcomeScreen
{
    private const string Version = "0.1.0";

    public static void Render(IAnsiConsole console, string model, string cwd, int toolCount)
    {
        var accentColor = new Color(204, 120, 80);

        // Header rule
        console.Write(new Rule($"[#cc7850]CCX-Dotnet v{Version}[/]")
        {
            Style = new Style(accentColor),
            Justification = Justify.Left
        });

        var grid = new Grid();
        grid.AddColumn(new GridColumn().PadRight(2));
        grid.AddColumn(new GridColumn());

        // Left: welcome + pet + info
        var left = string.Join('\n',
            "",
            "    [bold]Welcome back![/]",
            "",
            "[dim]         ╱▔▔▔▔▔╲[/]",
            "[dim]        ╱ ●   ● ╲[/]",
            "[dim]       ╱    ▽     ╲[/]",
            "[dim]      ╱_____________╲[/]",
            "",
            $" [green]{Markup.Escape(model)}[/] [dim]· API Key[/]",
            $" [dim]{Markup.Escape(ShortenPath(cwd))}[/]",
            $" [dim].NET 10 · AOT · Tools: {toolCount}[/]"
        );

        // Right: tips + activity
        var right = string.Join('\n',
            "[#cc7850 bold]Tips for getting started[/]",
            "Type a message to start coding",
            "",
            "[dim]───────────────────────────[/]",
            "",
            "[#cc7850 bold]Recent activity[/]",
            "[dim]No recent activity[/]",
            "",
            "",
            "",
            ""
        );

        grid.AddRow(new Markup(left), new Markup(right));

        var panel = new Panel(grid)
            .Border(BoxBorder.Rounded)
            .BorderColor(accentColor)
            .Padding(1, 0);

        console.Write(panel);
        console.WriteLine();
    }

    private static string ShortenPath(string path)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return path.StartsWith(home) ? "~" + path[home.Length..] : path;
    }
}
