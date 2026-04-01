using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ccx.Tools.Tools;

public sealed class TaskUpdateTool : ITool
{
    public string Name => "TaskUpdate";

    public string Description => "Update the status of an existing task.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "taskId": { "type": "string", "description": "Task identifier (e.g. task-001)" },
                "status": {
                    "type": "string",
                    "enum": ["pending", "in_progress", "completed"],
                    "description": "New status for the task"
                }
            },
            "required": ["taskId", "status"]
        }
        """).RootElement.Clone();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        if (!input.TryGetProperty("taskId", out var idProp) || string.IsNullOrWhiteSpace(idProp.GetString()))
            return ToolResult.Error("taskId is required.");

        if (!input.TryGetProperty("status", out var statusProp) || string.IsNullOrWhiteSpace(statusProp.GetString()))
            return ToolResult.Error("status is required.");

        var taskId = idProp.GetString()!;
        var status = statusProp.GetString()!;

        if (status is not ("pending" or "in_progress" or "completed"))
            return ToolResult.Error("status must be pending, in_progress, or completed.");

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var tasksRoot = Path.Combine(home, ".claude", "tasks");

        if (!Directory.Exists(tasksRoot))
            return ToolResult.Error("No task directories found.");

        foreach (var teamDir in Directory.GetDirectories(tasksRoot))
        {
            var filePath = Path.Combine(teamDir, $"{taskId}.json");
            if (!File.Exists(filePath)) continue;

            var json = await File.ReadAllTextAsync(filePath, ct);
            var node = JsonNode.Parse(json);
            if (node is null)
                return ToolResult.Error($"Failed to parse {taskId}.json.");

            node["status"] = status;
            node["updated_at"] = DateTime.UtcNow.ToString("o");

            var options = new JsonSerializerOptions { WriteIndented = true };
            await File.WriteAllTextAsync(filePath, node.ToJsonString(options), ct);

            return ToolResult.Success($"Updated {taskId} status to '{status}'.");
        }

        return ToolResult.Error($"Task '{taskId}' not found.");
    }
}
