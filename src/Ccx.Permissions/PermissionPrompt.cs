using Spectre.Console;

namespace Ccx.Permissions;

public enum PromptResult
{
    AllowOnce,
    DenyOnce,
    AllowAlways,
    DenyAlways
}

public sealed class PermissionPrompt
{
    private readonly IAnsiConsole _console;

    public PermissionPrompt(IAnsiConsole console)
    {
        _console = console;
    }

    public PromptResult Ask(string toolName, string? details = null)
    {
        _console.WriteLine();
        _console.MarkupLine($"[yellow]Permission required:[/] [bold]{Markup.Escape(toolName)}[/]");
        if (!string.IsNullOrEmpty(details))
            _console.MarkupLine($"[dim]{Markup.Escape(details[..Math.Min(details.Length, 200)])}[/]");

        var choice = _console.Prompt(
            new SelectionPrompt<string>()
                .Title("Allow this action?")
                .AddChoices("Allow once", "Deny once", "Always allow", "Always deny"));

        return choice switch
        {
            "Allow once" => PromptResult.AllowOnce,
            "Deny once" => PromptResult.DenyOnce,
            "Always allow" => PromptResult.AllowAlways,
            "Always deny" => PromptResult.DenyAlways,
            _ => PromptResult.DenyOnce
        };
    }
}
