using Spectre.Console;

namespace Ccx.Tui;

/// <summary>
/// Handles user input with styled prompts.
/// </summary>
public sealed class InputHandler
{
    private readonly IAnsiConsole _console;

    public InputHandler(IAnsiConsole console)
    {
        _console = console;
    }

    public string? ReadInput()
    {
        _console.Markup("[green]> [/]");
        return System.Console.ReadLine();
    }

    public string ReadMultilineInput()
    {
        _console.MarkupLine("[dim]Enter your message (empty line to send):[/]");
        var lines = new List<string>();

        while (true)
        {
            _console.Markup("[green]│ [/]");
            var line = System.Console.ReadLine();
            if (line is null || line.Length == 0) break;
            lines.Add(line);
        }

        return string.Join('\n', lines);
    }
}
