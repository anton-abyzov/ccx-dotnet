using Ccx.Api;
using Ccx.Api.Models;
using Ccx.Config;
using Ccx.Core;
using Ccx.Core.Agents;
using Ccx.Core.Cost;
using Ccx.Core.Hooks;
using Ccx.Permissions;
using Ccx.Skills;
using Ccx.Tools;
using Ccx.Tools.Tools;
using Ccx.Tui;
using Spectre.Console;

// --- Load settings ---
var settings = CcxSettings.Load();
var cascade = new SettingsCascade(settings);
var keyVault = new KeyVaultProvider();

string? apiKey = null;
string? cliModel = null;
var showCost = false;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--model" when i + 1 < args.Length:
            cliModel = args[++i];
            break;
        case "--api-key" when i + 1 < args.Length:
            apiKey = args[++i];
            break;
        case "--cost":
            showCost = true;
            break;
    }
}

// API key resolution: CLI > env > KeyVault
apiKey ??= Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
if (string.IsNullOrEmpty(apiKey) && keyVault.IsConfigured)
    apiKey = await keyVault.GetApiKeyAsync();

if (string.IsNullOrEmpty(apiKey))
{
    AnsiConsole.MarkupLine("[red]Error:[/] Set ANTHROPIC_API_KEY or pass --api-key.");
    return 1;
}

var model = cascade.GetModel(cliModel);

// --- Configure HTTP client with proxy support ---
var handler = ProxyConfig.CreateHandler(settings);
using var http = new HttpClient(handler);

// --- Initialize components ---
var client = new ClaudeClient(http, apiKey, model);
var tools = new ToolRegistry();
var costTracker = new CostTracker();
var chatRenderer = new ChatRenderer(AnsiConsole.Console);
var statusDisplay = new StatusDisplay(AnsiConsole.Console);

// Register core tools
tools.Register(new BashTool());
tools.Register(new FileReadTool());
tools.Register(new FileWriteTool());
tools.Register(new FileEditTool());
tools.Register(new GlobTool());
tools.Register(new GrepTool());
tools.Register(new WebFetchTool());

// --- Permission system ---
var classifier = new PermissionClassifier();
foreach (var pattern in cascade.GetAllowedTools())
    classifier.AddRule(new PermissionRule { ToolPattern = pattern, Action = PermissionAction.Allow });
foreach (var pattern in cascade.GetDeniedTools())
    classifier.AddRule(new PermissionRule { ToolPattern = pattern, Action = PermissionAction.Deny });

// --- Hooks ---
var hookRunner = new HookRunner();
if (settings.Hooks is not null)
{
    foreach (var (key, hookDef) in settings.Hooks)
    {
        if (hookDef.Command is not null && hookDef.Event is not null)
            hookRunner.Register(new HookDef { Event = hookDef.Event, Command = hookDef.Command });
    }
}

// --- Agent manager ---
var agentManager = new AgentManager(client, tools);

// --- Skill system ---
var skillLoader = new SkillLoader();
var skillExecutor = new SkillExecutor(skillLoader);

// --- Load CLAUDE.md context ---
var claudeMd = ClaudeMdDiscovery.LoadCombined();

// --- Main loop ---
var engine = new QueryEngine(client, tools, text => AnsiConsole.Write(new Markup(Markup.Escape(text))));

AnsiConsole.MarkupLine("[bold]ccx[/] — Claude Code for .NET");
AnsiConsole.MarkupLine($"Model: [cyan]{Markup.Escape(model)}[/] | Tools: [cyan]{tools.Count}[/]");
if (!string.IsNullOrEmpty(claudeMd))
    AnsiConsole.MarkupLine("[dim]CLAUDE.md loaded[/]");
AnsiConsole.WriteLine();

var messages = new List<Message>();
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

// Add system prompt with CLAUDE.md if available
if (!string.IsNullOrEmpty(claudeMd))
{
    // System prompt will be injected via MessageRequest in QueryEngine
}

while (!cts.IsCancellationRequested)
{
    AnsiConsole.Markup("[green]> [/]");
    var input = Console.ReadLine();

    if (input is null or "exit" or "quit") break;
    if (string.IsNullOrWhiteSpace(input)) continue;

    // Handle slash commands (skills)
    if (input.StartsWith('/'))
    {
        var parts = input[1..].Split(' ', 2);
        var result = skillExecutor.Execute(parts[0], parts.Length > 1 ? parts[1] : null);
        if (result.Found)
        {
            AnsiConsole.MarkupLine($"[dim]Loaded skill: {Markup.Escape(result.SkillName)}[/]");
            input = result.InjectedPrompt ?? input;
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(result.ErrorMessage ?? "Unknown skill")}[/]");
            continue;
        }
    }

    // Run pre-prompt hooks
    await hookRunner.RunAsync("pre-prompt", new Dictionary<string, string> { ["CCX_INPUT"] = input }, cts.Token);

    messages.Add(Message.User(input));

    try
    {
        var response = await statusDisplay.WithSpinnerAsync("Thinking...",
            () => engine.RunAsync(messages, cts.Token));

        messages.Add(Message.Assistant(response));

        // Track cost from response text length (estimate)
        var responseText = string.Join("", response
            .Where(b => b.Type == "text" && b.Text is not null)
            .Select(b => b.Text));
        costTracker.Record(model,
            Ccx.Compact.TokenEstimator.Estimate(input),
            Ccx.Compact.TokenEstimator.Estimate(responseText ?? ""));

        AnsiConsole.WriteLine();

        // Run post-response hooks
        await hookRunner.RunAsync("post-response", ct: cts.Token);
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

// Show cost summary
if (showCost || costTracker.RequestCount > 0)
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[dim]{Markup.Escape(costTracker.GetSummary())}[/]");
}

return 0;
