using Ccx.Api;
using Ccx.Api.Models;
using Ccx.Tools;
using Ccx.Tools.Tools;

namespace Ccx.Core.Agents;

/// <summary>
/// Spawns sub-agents with their own QueryEngine and limited tool set. AOT-compatible.
/// </summary>
public sealed class AgentSpawner : IAgentSpawner
{
    private readonly IClaudeClient _client;

    public AgentSpawner(IClaudeClient client)
    {
        _client = client;
    }

    public async Task<string> SpawnAsync(string prompt, string? description, ToolContext ctx, CancellationToken ct)
    {
        // Sub-agents get a limited tool set (no Agent to prevent recursion)
        var tools = new ToolRegistry();
        tools.Register(new BashTool());
        tools.Register(new FileReadTool());
        tools.Register(new FileWriteTool());
        tools.Register(new FileEditTool());
        tools.Register(new GlobTool());
        tools.Register(new GrepTool());

        var engine = new QueryEngine(_client, tools, maxTurns: QueryEngine.AgentMaxTurns);
        var messages = new List<Message> { Message.User(prompt) };

        var response = await engine.RunAsync(messages, ct);

        return string.Join("", response
            .Where(b => b.Type == "text" && b.Text is not null)
            .Select(b => b.Text));
    }
}
