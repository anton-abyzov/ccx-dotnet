using Spectre.Console;

namespace Ccx.Tui;

/// <summary>
/// Shows spinners and status indicators during tool execution.
/// </summary>
public sealed class StatusDisplay
{
    private readonly IAnsiConsole _console;

    public StatusDisplay(IAnsiConsole console)
    {
        _console = console;
    }

    public async Task<T> WithSpinnerAsync<T>(string message, Func<Task<T>> action)
    {
        return await _console.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync($"[dim]{Markup.Escape(message)}[/]", async _ => await action());
    }

    public async Task WithSpinnerAsync(string message, Func<Task> action)
    {
        await _console.Status()
            .Spinner(Spinner.Known.Dots)
            .SpinnerStyle(Style.Parse("blue"))
            .StartAsync($"[dim]{Markup.Escape(message)}[/]", async _ => await action());
    }
}
