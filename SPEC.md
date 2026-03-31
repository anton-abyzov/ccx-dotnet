# claude-code-dotnet -- Implementation Spec

## Strategy: .NET 9 AOT, Spectre.Console, Enterprise-First

The only AI coding assistant CLI built on .NET. Targeting enterprise .NET shops
who are underserved by the Python/Node/Rust AI tool ecosystem.

## Phase 1: Foundation (Week 1-4)

### P1-01: Solution scaffold
```
dotnet new sln -n ClaudeCode
dotnet new console -n ClaudeCode.Cli -o src/ClaudeCode.Cli
dotnet new classlib -n ClaudeCode.Core -o src/ClaudeCode.Core
dotnet new classlib -n ClaudeCode.Api -o src/ClaudeCode.Api
dotnet new classlib -n ClaudeCode.Tools -o src/ClaudeCode.Tools
dotnet new xunit -n ClaudeCode.Core.Tests -o tests/ClaudeCode.Core.Tests
```
- AOT-compatible from day one (`<PublishAot>true</PublishAot>`)
- Source-generated JSON serialization (no reflection)
- Spectre.Console.Cli for command parsing

### P1-02: Claude API client
```csharp
public class ClaudeClient
{
    public async IAsyncEnumerable<StreamEvent> StreamMessageAsync(
        MessageRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var response = await _http.SendAsync(httpRequest,
            HttpCompletionOption.ResponseHeadersRead, ct);
        await foreach (var line in response.Content.ReadAsLinesAsync(ct))
        {
            if (TryParseEvent(line, out var evt)) yield return evt;
        }
    }
}
```
- HttpClient with ReadAsStreamAsync for SSE
- Source-generated JsonSerializerContext for all API types
- IAsyncEnumerable for streaming (natural C# pattern)
- Retry with exponential backoff (Polly)

### P1-03: Tool interface
```csharp
public interface ITool
{
    string Name { get; }
    string Description { get; }
    JsonElement InputSchema { get; }
    Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct);
    bool IsConcurrencySafe(JsonElement input);
}
```
- DI registration: `services.AddSingleton<ITool, BashTool>()`
- Tool registry via `IEnumerable<ITool>` injection

### P1-04: Basic query loop
- `QueryEngine.RunAsync()` -- main agentic loop
- Message -> API -> tool_use -> execute -> loop
- CancellationToken throughout for graceful shutdown

## Phase 2: Core Tools (Week 4-8)

### P2-01: Bash tool
- `Process.Start()` with output capture
- Working directory tracking
- Timeout via CancellationTokenSource
- Basic safety: command pattern matching

### P2-02: File tools
- FileRead: `File.ReadLinesAsync()` with line numbers
- FileWrite: `File.WriteAllTextAsync()`
- FileEdit: String replacement with uniqueness validation

### P2-03: Search tools
- Glob: `Microsoft.Extensions.FileSystemGlobbing`
- Grep: Shell out to system ripgrep, fallback to `Regex` + `Directory.EnumerateFiles`

### P2-04: Web tools
- WebFetch: HttpClient + HtmlAgilityPack for HTML-to-markdown
- WebSearch: Brave/Google search API

## Phase 3: TUI (Week 8-12)

### P3-01: Spectre.Console app shell
- `AnsiConsole.Live()` for dynamic rendering
- `Panel`, `Table`, `Tree` for structured output
- Markup for colored/styled text
- SpectreMarkdownRenderer (custom) for response rendering

### P3-02: Interactive prompts
- `SelectionPrompt` for permission dialogs
- `TextPrompt` for user input
- `Status` spinner for tool execution

### P3-03: Code display
- Syntax highlighting via TextMateSharp
- Diff rendering with side-by-side and inline modes

## Phase 4: Agent System (Week 12-16)

### P4-01: Agent spawning
```csharp
public class AgentManager
{
    public async Task<AgentResult> SpawnAsync(AgentDef def, CancellationToken ct)
    {
        return await Task.Run(async () =>
        {
            var engine = new QueryEngine(def.Tools, def.Prompt);
            return await engine.RunAsync(def.Messages, ct);
        }, ct);
    }

    public async Task<AgentResult[]> SpawnParallelAsync(
        IEnumerable<AgentDef> defs, CancellationToken ct)
    {
        return await Task.WhenAll(defs.Select(d => SpawnAsync(d, ct)));
    }
}
```

### P4-02: Permission system
- Rule matching with glob patterns
- Settings from `~/.claude/settings.json`
- Interactive Spectre.Console prompts

### P4-03: Config and CLAUDE.md
- `Microsoft.Extensions.Configuration` for settings cascade
- CLAUDE.md discovery via directory walking
- YamlDotNet for frontmatter parsing

## Phase 5: Context & MCP (Week 16-20)

### P5-01: Context compression
- Token estimation (char-based with model-specific ratios)
- MicroCompact: strip large tool results
- AutoCompact: summarize via Claude API call

### P5-02: MCP client
- JSON-RPC over stdio (Process.Start + StreamReader/StreamWriter)
- Tool and resource discovery
- Connection lifecycle management

### P5-03: Skill system
- Markdown file loading from `~/.claude/skills/`
- YAML frontmatter parsing
- Inline and agent-forked execution

## Phase 6: Enterprise Features (Week 20-24)

### P6-01: AOT publishing and distribution
- `dotnet publish -r win-x64 --self-contained -p:PublishAot=true`
- `dotnet publish -r osx-arm64 --self-contained -p:PublishAot=true`
- `dotnet publish -r linux-x64 --self-contained -p:PublishAot=true`
- NuGet tool package: `dotnet tool install -g claude-code-dotnet`
- GitHub releases with platform binaries

### P6-02: Enterprise config
- Windows Group Policy integration
- Azure Key Vault for API key storage
- Proxy support (corporate environments)

### P6-03: Hook system, vim mode, cost tracking, memory

## Key Decisions

- **AOT from day one**: No reflection, no dynamic assembly loading. Source generators for JSON
- **No Entity Framework**: Pure file-based config and state (no database)
- **Spectre.Console only**: No System.Console direct calls (testable via IAnsiConsole mock)
- **Minimal dependencies**: Lean on stdlib where possible. HttpClient > RestSharp
- **Target .NET 9**: LTS, best AOT support, latest language features
