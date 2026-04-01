using System.Text.Json;

namespace Ccx.Tools.Tools;

public sealed class TaskCreateTool : ITool
{
    public string Name => "TaskCreate";

    public string Description => "Create a task for tracking work progress.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "subject": { "type": "string", "description": "Short title of the task" },
                "description": { "type": "string", "description": "Detailed description of the task" }
            },
            "required": ["subject", "description"]
        }
        """).RootElement.Clone();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        if (!input.TryGetProperty("subject", out var subProp) || string.IsNullOrWhiteSpace(subProp.GetString()))
            return ToolResult.Error("subject is required.");

        if (!input.TryGetProperty("description", out var descProp) || string.IsNullOrWhiteSpace(descProp.GetString()))
            return ToolResult.Error("description is required.");

        var subject = subProp.GetString()!;
        var description = descProp.GetString()!;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var tasksRoot = Path.Combine(home, ".claude", "tasks");

        if (!Directory.Exists(tasksRoot))
            return ToolResult.Error("No task directories found. Create a team first.");

        var teamDirs = Directory.GetDirectories(tasksRoot);
        if (teamDirs.Length == 0)
            return ToolResult.Error("No task directories found. Create a team first.");

        var taskDir = teamDirs[0];
        var existingCount = Directory.GetFiles(taskDir, "task-*.json").Length;
        var taskId = $"task-{existingCount + 1:D3}";

        var task = new
        {
            id = taskId,
            subject,
            description,
            status = "pending",
            created_at = DateTime.UtcNow.ToString("o")
        };

        var json = JsonSerializer.Serialize(task, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(taskDir, $"{taskId}.json"), json, ct);

        return ToolResult.Success($"Created {taskId}: {subject}");
    }
}
