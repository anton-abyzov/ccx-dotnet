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
var dangerouslySkipPermissions = true; // bypass by default, like Go/Rust

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--help" or "-h":
            AnsiConsole.MarkupLine("[bold]ccx[/] — Claude Code for .NET");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Usage:[/] ccx [[options]]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[bold]Options:[/]");
            AnsiConsole.MarkupLine("  --model <model>                Model to use (default: from settings)");
            AnsiConsole.MarkupLine("  --api-key <key>                Anthropic API key");
            AnsiConsole.MarkupLine("  --cost                         Show cost summary on exit");
            AnsiConsole.MarkupLine("  --dangerously-skip-permissions Skip all permission prompts (default)");
            AnsiConsole.MarkupLine("  --permission-mode default      Prompt for permissions");
            AnsiConsole.MarkupLine("  -h, --help                     Show this help");
            return 0;
        case "--model" when i + 1 < args.Length:
            cliModel = args[++i];
            break;
        case "--api-key" when i + 1 < args.Length:
            apiKey = args[++i];
            break;
        case "--cost":
            showCost = true;
            break;
        case "--dangerously-skip-permissions":
            dangerouslySkipPermissions = true;
            break;
        case "--permission-mode" when i + 1 < args.Length:
            var mode = args[++i];
            if (mode == "default")
                dangerouslySkipPermissions = false;
            break;
    }
}

// Auth resolution: CLI > env > macOS Keychain > credentials file > KeyVault
OAuthResolver.AuthResult auth;
try
{
    auth = OAuthResolver.Resolve(apiKey);
}
catch (InvalidOperationException)
{
    // Fallback to KeyVault if OAuth resolver found nothing
    if (keyVault.IsConfigured)
    {
        var vaultKey = await keyVault.GetApiKeyAsync();
        if (!string.IsNullOrEmpty(vaultKey))
        {
            auth = new OAuthResolver.AuthResult(vaultKey, false, "Key Vault");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Error:[/] No auth found. Set ANTHROPIC_API_KEY, pass --api-key, or log in with: claude auth");
            return 1;
        }
    }
    else
    {
        AnsiConsole.MarkupLine("[red]Error:[/] No auth found. Set ANTHROPIC_API_KEY, pass --api-key, or log in with: claude auth");
        return 1;
    }
}

var model = cascade.GetModel(cliModel);

// --- Configure HTTP client with proxy support ---
var handler = ProxyConfig.CreateHandler(settings);
using var http = new HttpClient(handler);

// --- Initialize components ---
var client = new ClaudeClient(http, auth.Token, model, auth.IsOAuth);
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
tools.Register(new WebSearchTool());
tools.Register(new TodoWriteTool());
tools.Register(new NotebookEditTool());
tools.Register(new AgentTool(new AgentSpawner(client)));

// --- Permission system ---
var classifier = new PermissionClassifier();
if (dangerouslySkipPermissions)
{
    // Bypass: allow all tools without prompting
    classifier.AddRule(new PermissionRule { ToolPattern = "*", Action = PermissionAction.Allow });
}
else
{
    foreach (var pattern in cascade.GetAllowedTools())
        classifier.AddRule(new PermissionRule { ToolPattern = pattern, Action = PermissionAction.Allow });
    foreach (var pattern in cascade.GetDeniedTools())
        classifier.AddRule(new PermissionRule { ToolPattern = pattern, Action = PermissionAction.Deny });
}

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
var discoveredSkills = SkillDiscovery.DiscoverAll();

// --- Load CLAUDE.md context ---
var claudeMd = ClaudeMdDiscovery.LoadCombined();

// --- Build system prompt ---
var systemPrompt = PromptBuilder.BuildSystemPrompt(tools.All, claudeMd);

// --- Main loop ---
var engine = new QueryEngine(client, tools, systemPrompt: systemPrompt);

// Show inline welcome panel (scrolls naturally, no full-screen TUI)
InlineRenderer.RenderWelcome(AnsiConsole.Console, model, auth.DisplayName, Directory.GetCurrentDirectory(), tools.Count);
InlineRenderer.RenderFooter(AnsiConsole.Console);
AnsiConsole.WriteLine();

// --- Command history and readline ---
var history = new CommandHistory();
var builtinCmds = new[] { "/help", "/exit", "/quit", "/clear", "/cost", "/model", "/version", "/tools" };
var allSlashCmds = builtinCmds
    .Concat(discoveredSkills.Select(s => "/" + s.Name))
    .ToList();
var readline = new ReadLineEditor(history, allSlashCmds);

var messages = new List<Message>();
using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

while (!cts.IsCancellationRequested)
{
    InlineRenderer.RenderPrompt(AnsiConsole.Console);
    var input = readline.ReadLine();

    if (input is null or "exit" or "quit") break;
    if (string.IsNullOrWhiteSpace(input)) continue;

    history.Add(input);

    // Handle slash commands
    if (input.StartsWith('/'))
    {
        var cmd = input.Split(' ', 2)[0].ToLowerInvariant();

        if (cmd is "/exit" or "/quit")
            break;

        if (cmd is "/help")
        {
            AnsiConsole.MarkupLine("[bold]Commands:[/]");
            AnsiConsole.MarkupLine("  [green]/help[/]     Show this help");
            AnsiConsole.MarkupLine("  [green]/exit[/]     Quit");
            AnsiConsole.MarkupLine("  [green]/clear[/]    Clear screen");
            AnsiConsole.MarkupLine("  [green]/cost[/]     Token usage summary");
            AnsiConsole.MarkupLine("  [green]/model[/]    Current model");
            AnsiConsole.MarkupLine("  [green]/version[/]  Version info");
            AnsiConsole.MarkupLine("  [green]/tools[/]    List available tools");

            if (discoveredSkills.Count > 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[bold]Skills ({discoveredSkills.Count}):[/]");
                foreach (var (name, desc) in discoveredSkills)
                {
                    if (!string.IsNullOrEmpty(desc))
                        AnsiConsole.MarkupLine($"  [cyan]/{Markup.Escape(name)}[/]  {Markup.Escape(desc)}");
                    else
                        AnsiConsole.MarkupLine($"  [cyan]/{Markup.Escape(name)}[/]");
                }
            }

            AnsiConsole.WriteLine();
            continue;
        }

        if (cmd is "/clear")
        {
            AnsiConsole.Clear();
            continue;
        }

        if (cmd is "/cost")
        {
            AnsiConsole.MarkupLine($"[dim]{Markup.Escape(costTracker.GetSummary())}[/]");
            continue;
        }

        if (cmd is "/model")
        {
            AnsiConsole.MarkupLine($"  Model: [green]{Markup.Escape(model)}[/]");
            continue;
        }

        if (cmd is "/version")
        {
            AnsiConsole.MarkupLine("  [bold]ccx[/] v0.1.0 (.NET 10, AOT)");
            continue;
        }

        if (cmd is "/tools")
        {
            AnsiConsole.MarkupLine($"[bold]Registered tools ({tools.Count}):[/]");
            foreach (var t in tools.All)
                AnsiConsole.MarkupLine($"  [green]\u2022[/] {Markup.Escape(t.Name)}");
            AnsiConsole.WriteLine();
            continue;
        }

        // Non-builtin slash command -> try skills
        var parts = input[1..].Split(' ', 2);
        var skillCmd = parts[0];
        var result = skillExecutor.Execute(skillCmd, parts.Length > 1 ? parts[1] : null);
        if (result.Found)
        {
            AnsiConsole.MarkupLine($"[dim]Loaded skill: {Markup.Escape(result.SkillName)}[/]");
            input = result.InjectedPrompt ?? input;
        }
        else
        {
            // Check discovered skills
            var match = discoveredSkills.FirstOrDefault(s =>
                s.Name.Equals(skillCmd, StringComparison.OrdinalIgnoreCase));
            if (match != default)
            {
                // Skill found in discovery but not loadable via executor — inform user
                AnsiConsole.MarkupLine($"[yellow]Skill '{Markup.Escape(skillCmd)}' found but could not be loaded.[/]");
                continue;
            }

            // Suggest similar commands
            var builtins = new[] { "help", "exit", "quit", "clear", "cost", "model", "version", "tools" };
            var allNames = builtins.Concat(discoveredSkills.Select(s => s.Name)).ToList();
            var suggestions = allNames
                .Where(n => n.Contains(skillCmd, StringComparison.OrdinalIgnoreCase)
                    || skillCmd.Contains(n, StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .ToList();

            AnsiConsole.MarkupLine($"[yellow]Unknown command: /{Markup.Escape(skillCmd)}[/]");
            if (suggestions.Count > 0)
                AnsiConsole.MarkupLine($"[dim]Did you mean: {string.Join(", ", suggestions.Select(s => "/" + s))}?[/]");
            continue;
        }
    }

    // Run pre-prompt hooks
    await hookRunner.RunAsync("pre-prompt", new Dictionary<string, string> { ["CCX_INPUT"] = input }, cts.Token);

    messages.Add(Message.User(input));

    try
    {
        var response = await statusDisplay.WithSpinnerAsync("Working...",
            () => engine.RunAsync(messages, cts.Token));

        messages.Add(Message.Assistant(response));

        // Render response with markdown formatting
        var responseText = string.Join("", response
            .Where(b => b.Type == "text" && b.Text is not null)
            .Select(b => b.Text));

        if (!string.IsNullOrEmpty(responseText))
        {
            try
            {
                AnsiConsole.MarkupLine(MarkdownRenderer.ToMarkup(responseText));
            }
            catch
            {
                AnsiConsole.WriteLine(responseText);
            }
        }

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

// Show footer and cost summary
InlineRenderer.RenderFooter(AnsiConsole.Console);
if (showCost || costTracker.RequestCount > 0)
{
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine($"[dim]{Markup.Escape(costTracker.GetSummary())}[/]");
}

return 0;
