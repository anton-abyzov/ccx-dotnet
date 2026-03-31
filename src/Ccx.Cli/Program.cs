using Ccx.Api;
using Ccx.Api.Models;
using Ccx.Core;
using Ccx.Tools;
using Ccx.Tools.Tools;
using Spectre.Console;

var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
var model = "claude-sonnet-4-20250514";

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--model" when i + 1 < args.Length:
            model = args[++i];
            break;
        case "--api-key" when i + 1 < args.Length:
            apiKey = args[++i];
            break;
    }
}

if (string.IsNullOrEmpty(apiKey))
{
    AnsiConsole.MarkupLine("[red]Error:[/] Set ANTHROPIC_API_KEY or pass --api-key.");
    return 1;
}

using var http = new HttpClient();
var client = new ClaudeClient(http, apiKey, model);
var tools = new ToolRegistry();

// Register core tools
tools.Register(new BashTool());
tools.Register(new FileReadTool());
tools.Register(new FileWriteTool());
tools.Register(new FileEditTool());
tools.Register(new GlobTool());
tools.Register(new GrepTool());
tools.Register(new WebFetchTool());

var engine = new QueryEngine(client, tools, text => Console.Write(text));

AnsiConsole.MarkupLine("[bold]ccx[/] — Claude Code for .NET");
AnsiConsole.MarkupLine($"Model: [cyan]{Markup.Escape(model)}[/]");
AnsiConsole.WriteLine();

var messages = new List<Message>();
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

while (!cts.IsCancellationRequested)
{
    AnsiConsole.Markup("[green]> [/]");
    var input = Console.ReadLine();

    if (input is null or "exit" or "quit") break;
    if (string.IsNullOrWhiteSpace(input)) continue;

    messages.Add(Message.User(input));

    try
    {
        var response = await engine.RunAsync(messages, cts.Token);
        messages.Add(Message.Assistant(response));
        Console.WriteLine();
    }
    catch (OperationCanceledException)
    {
        AnsiConsole.MarkupLine("\n[yellow]Cancelled.[/]");
        break;
    }
    catch (HttpRequestException ex)
    {
        AnsiConsole.MarkupLine($"\n[red]API error:[/] {Markup.Escape(ex.Message)}");
        messages.RemoveAt(messages.Count - 1);
    }
}

return 0;
