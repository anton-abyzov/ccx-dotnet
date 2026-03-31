using Spectre.Console;

namespace Ccx.Tui;

/// <summary>
/// Renders chat messages using Spectre.Console panels and markup.
/// </summary>
public sealed class ChatRenderer
{
    private readonly IAnsiConsole _console;

    public ChatRenderer(IAnsiConsole console)
    {
        _console = console;
    }

    public void RenderUserMessage(string text)
    {
        _console.Write(new Panel(Markup.Escape(text))
            .Header("[green]You[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Green)
            .Padding(1, 0));
        _console.WriteLine();
    }

    public void RenderAssistantMessage(string text)
    {
        var rendered = MarkdownRenderer.ToMarkup(text);
        _console.Write(new Panel(rendered)
            .Header("[blue]Assistant[/]")
            .Border(BoxBorder.Rounded)
            .BorderColor(Color.Blue)
            .Padding(1, 0));
        _console.WriteLine();
    }

    public void RenderToolCall(string toolName, string? summary = null)
    {
        var desc = string.IsNullOrEmpty(summary) ? toolName : $"{toolName}: {Markup.Escape(summary)}";
        _console.MarkupLine($"  [dim]⟫ {desc}[/]");
    }

    public void RenderToolResult(string toolName, bool isError, string? preview = null)
    {
        var color = isError ? "red" : "dim";
        var status = isError ? "✗" : "✓";
        var text = string.IsNullOrEmpty(preview) ? "" : $" {Markup.Escape(preview[..Math.Min(preview.Length, 80)])}";
        _console.MarkupLine($"  [{color}]{status} {Markup.Escape(toolName)}{text}[/]");
    }

    public void RenderError(string message)
    {
        _console.MarkupLine($"[red]Error:[/] {Markup.Escape(message)}");
    }

    public void RenderSeparator(string? label = null)
    {
        _console.Write(label is not null
            ? new Rule($"[dim]{Markup.Escape(label)}[/]") { Style = Style.Parse("dim") }
            : new Rule { Style = Style.Parse("dim") });
    }
}
