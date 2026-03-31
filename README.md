# ccx-dotnet

A .NET implementation of an AI coding assistant CLI. Cross-platform AOT-compiled binary for enterprise environments. The only AI coding CLI built on .NET.

## Why .NET?

- **AOT single binary** (~30-50MB) via `dotnet publish --self-contained -p:PublishAot=true`
- **Strong async/await** -- Task, IAsyncEnumerable for streaming, Channel for agent communication
- **Enterprise appeal** -- .NET shops are underserved by AI CLI tools
- **Spectre.Console** -- Rich terminal rendering with tables, trees, panels, syntax highlighting
- **Uncontested niche** -- zero AI coding assistants built on .NET

## Architecture

Based on analysis of Claude Code's 512K-line TypeScript architecture:

- **Tool System**: Interface-based tools with DI registration and permission gating
- **Agent Spawning**: Task-based with CancellationToken and Channel for inter-agent IPC
- **TUI**: Spectre.Console with Live rendering, Markup, and interactive prompts
- **Context Management**: Multi-layer compression with IAsyncEnumerable streaming
- **MCP Protocol**: JSON-RPC over stdio/SSE
- **Permission System**: Rule-based with interactive approval flows
- **Streaming API**: HttpClient with ReadAsStreamAsync for SSE parsing

## Tech Stack

| Component | Library |
|-----------|---------|
| TUI | Spectre.Console + Spectre.Console.Cli |
| HTTP/Streaming | HttpClient (stdlib) |
| JSON | System.Text.Json (source-generated) |
| Schema Validation | JsonSchema.Net |
| CLI Parsing | Spectre.Console.Cli |
| Markdown | Markdig + custom Spectre renderer |
| Syntax Highlighting | TextMateSharp |
| Config | Microsoft.Extensions.Configuration |
| DI | Microsoft.Extensions.DependencyInjection |
| Testing | xUnit + FluentAssertions + WireMock.Net + Verify |

## Project Structure

```
src/
  ClaudeCode.Cli/              # CLI entry point (AOT-compatible)
  ClaudeCode.Core/             # Core agent loop and query engine
  ClaudeCode.Api/              # Anthropic API client (streaming, tool_use)
  ClaudeCode.Tools/            # Tool interface and built-in implementations
    Tools/
      BashTool.cs
      FileReadTool.cs
      FileEditTool.cs
      FileWriteTool.cs
      GlobTool.cs
      GrepTool.cs
      AgentTool.cs
      WebFetchTool.cs
  ClaudeCode.Permissions/      # Permission DSL, rules, interactive prompts
  ClaudeCode.Compact/          # 4-layer context compression
  ClaudeCode.Memory/           # Memory system (user, project, feedback, reference)
  ClaudeCode.Skills/           # Skill loading and execution
  ClaudeCode.Mcp/              # MCP protocol client
  ClaudeCode.Config/           # Settings cascade, CLAUDE.md parsing
  ClaudeCode.Tui/              # Spectre.Console UI components
tests/
  ClaudeCode.Core.Tests/
  ClaudeCode.Api.Tests/
  ClaudeCode.Tools.Tests/
  ClaudeCode.Permissions.Tests/
  ClaudeCode.Integration.Tests/
```

## Getting Started

```sh
dotnet tool install -g ccx-dotnet
```

## Development

```sh
git clone https://github.com/anton-abyzov/ccx-dotnet.git
cd ccx-dotnet
dotnet build
dotnet test
```

## Requirements

- .NET 10 SDK
- AOT publishing requires platform-specific toolchain

## License

MIT
