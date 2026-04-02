# ccx-dotnet

AI coding assistant CLI in .NET 10, part of the [CCX (Community Code Extended)](https://github.com/anton-abyzov/ccx) project. 7.9MB AOT binary, 11 tools, Spectre.Console TUI. The only AI coding CLI built on .NET, targeting enterprise environments.

## Why CCX?

CCX (Community Code Extended) is a custom AI coding assistant built from the ground up using publicly documented API specifications and common patterns in AI-assisted development. Each implementation is its own application with independent architecture decisions and language-idiomatic designs.

ccx-dotnet fills a gap that nothing else does: there are zero AI coding CLIs built for the .NET ecosystem. Enterprise .NET shops running C#, F#, and Azure pipelines have no native option. This implementation brings AOT-compiled performance, Spectre.Console TUI, and the full tool system to the platform where a huge portion of enterprise code actually lives.

Unlike [instructkr/claw-code](https://github.com/instructkr/claw-code) (41.7k stars), which catalogs tool inventories as structured data, ccx-dotnet is a ground-up .NET implementation with real tool execution, DI-based architecture, and a comprehensive test suite.

- Architecture analysis: https://verified-skill.com/insights/claude-code
- CCX umbrella: https://github.com/anton-abyzov/ccx

## Quick Start

```bash
git clone https://github.com/anton-abyzov/ccx-dotnet.git
cd ccx-dotnet
dotnet build
export ANTHROPIC_API_KEY="your-key-here"
dotnet run --project src/Ccx.Cli
```

## Features

- **11 built-in tools** -- Bash, FileRead, FileEdit, FileWrite, Glob, Grep, Agent, WebFetch, NotebookEdit, TodoRead, TodoWrite
- **Slash commands with autocomplete** -- `/help`, `/compact`, `/clear`, `/model`, `/memory`, `/skills`
- **Streaming responses** -- HttpClient SSE streaming with IAsyncEnumerable and real-time tool_use handling
- **Markdown rendering** -- Markdig with custom Spectre.Console renderer
- **Skill discovery** -- loads and executes markdown-based skills
- **Task-based agent spawning** -- CancellationToken and Channel for inter-agent IPC
- **4-layer context compression** -- with IAsyncEnumerable streaming
- **MCP protocol** -- JSON-RPC over stdio/SSE
- **Permission system** -- rule-based with interactive approval flows
- **Memory persistence** -- user, project, feedback, and reference memory types
- **AOT-compatible** -- single binary via `dotnet publish -p:PublishAot=true`

## Slash Commands

| Command | Description |
|---------|-------------|
| `/help` | Show available commands |
| `/compact` | Compress conversation context |
| `/clear` | Clear conversation history |
| `/model` | Switch Claude model |
| `/memory` | View/manage memory entries |
| `/skills` | List available skills |
| `/config` | Show current configuration |

## Why .NET?

- **7.9MB AOT binary** via `dotnet publish --self-contained -p:PublishAot=true`
- **Strong async/await** -- Task, IAsyncEnumerable for streaming, Channel for agent communication
- **Enterprise appeal** -- .NET shops are underserved by AI CLI tools
- **Spectre.Console** -- rich terminal rendering with tables, trees, panels, syntax highlighting
- **Uncontested niche** -- zero AI coding assistants built on .NET
- **DI-first** -- Microsoft.Extensions.DependencyInjection for clean architecture

## Architecture

Based on publicly documented patterns in AI coding assistant architecture:

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
  Ccx.Cli/              # CLI entry point (AOT-compatible)
  Ccx.Core/             # Core agent loop and query engine
  Ccx.Api/              # Anthropic API client (streaming, tool_use)
  Ccx.Tools/            # Tool interface and built-in implementations
    Tools/
      BashTool.cs
      FileReadTool.cs
      FileEditTool.cs
      FileWriteTool.cs
      GlobTool.cs
      GrepTool.cs
      AgentTool.cs
      WebFetchTool.cs
  Ccx.Permissions/      # Permission DSL, rules, interactive prompts
  Ccx.Compact/          # 4-layer context compression
  Ccx.Memory/           # Memory system (user, project, feedback, reference)
  Ccx.Skills/           # Skill loading and execution
  Ccx.Mcp/              # MCP protocol client
  Ccx.Config/           # Settings cascade, CLAUDE.md parsing
  Ccx.Tui/              # Spectre.Console UI components
tests/
  Ccx.Core.Tests/
  Ccx.Api.Tests/
  Ccx.Tools.Tests/
  Ccx.Permissions.Tests/
  Ccx.Integration.Tests/
```

## Getting Started

```sh
dotnet tool install -g ccx-dotnet
```

## Development

```bash
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
