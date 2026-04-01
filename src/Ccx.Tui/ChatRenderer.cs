using Spectre.Console;

namespace Ccx.Tui;

/// <summary>
/// Renders chat messages in Claude Code style using Spectre.Console markup.
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
        _console.MarkupLine($"[cyan bold]❯[/] {Markup.Escape(text)}");
        _console.WriteLine();
    }

    public void RenderAssistantMessage(string text)
    {
        var rendered = MarkdownRenderer.ToMarkup(text);
        _console.MarkupLine($"[white]●[/] {rendered}");
        _console.WriteLine();
    }

    public void RenderToolCall(string toolName, string? summary = null)
    {
        var desc = string.IsNullOrEmpty(summary)
            ? Markup.Escape(toolName)
            : $"{Markup.Escape(toolName)}: {Markup.Escape(summary)}";
        _console.MarkupLine($"  [yellow]⚙ {desc}[/]");
    }

    public void RenderToolResult(string toolName, bool isError, string? preview = null)
    {
        var color = isError ? "red" : "green";
        var status = isError ? "✗" : "✓";
        var text = string.IsNullOrEmpty(preview) ? "" : $" {Markup.Escape(preview[..Math.Min(preview.Length, 80)])}";
        _console.MarkupLine($"  [{color}]{status} {Markup.Escape(toolName)}{text}[/]");
    }

    public void RenderError(string message)
    {
        _console.MarkupLine($"[red bold]Error:[/] {Markup.Escape(message)}");
    }

    public void RenderSeparator(string? label = null)
    {
        _console.Write(label is not null
            ? new Rule($"[dim]{Markup.Escape(label)}[/]") { Style = Style.Parse("dim") }
            : new Rule { Style = Style.Parse("dim") });
    }

    public void RenderFooter()
    {
        _console.MarkupLine("[dim]? for shortcuts                    ● high · /effort[/]");
    }
}
