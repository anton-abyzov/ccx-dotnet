using System.Text.Json;
using Ccx.Tools;

namespace Ccx.Tools.Tools;

/// <summary>
/// Spawns a sub-agent with its own QueryEngine to handle a task autonomously.
/// Requires an IAgentSpawner to be injected (AOT-compatible, no reflection).
/// </summary>
public sealed class AgentTool : ITool
{
    private readonly IAgentSpawner _spawner;

    public AgentTool(IAgentSpawner spawner)
    {
        _spawner = spawner;
    }

    public string Name => "Agent";

    public string Description =>
        "Launch a sub-agent to handle a complex task. The agent runs autonomously with its own context and returns a result.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "prompt": { "type": "string", "description": "The task for the agent to perform" },
                "description": { "type": "string", "description": "A short (3-5 word) description of the task" }
            },
            "required": ["prompt"]
        }
        """).RootElement.Clone();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        var prompt = input.TryGetProperty("prompt", out var p) ? p.GetString() : null;
        if (string.IsNullOrWhiteSpace(prompt))
            return ToolResult.Error("prompt is required.");

        var description = input.TryGetProperty("description", out var d) ? d.GetString() : null;

        try
        {
            var result = await Task.Run(() => _spawner.SpawnAsync(prompt, description, ctx, ct), ct);
            return ToolResult.Success(result);
        }
        catch (OperationCanceledException)
        {
            return ToolResult.Error("Agent was cancelled.");
        }
        catch (Exception ex)
        {
            return ToolResult.Error($"Agent error: {ex.Message}");
        }
    }
}

/// <summary>
/// Abstraction for spawning sub-agents. Keeps AgentTool decoupled from QueryEngine (AOT-safe).
/// </summary>
public interface IAgentSpawner
{
    Task<string> SpawnAsync(string prompt, string? description, ToolContext ctx, CancellationToken ct);
}
