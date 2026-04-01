using System.Text.Json;

namespace Ccx.Tools.Tools;

public sealed class TaskListTool : ITool
{
    public string Name => "TaskList";

    public string Description => "List all tasks and their statuses.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {},
            "required": []
        }
        """).RootElement.Clone();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var tasksRoot = Path.Combine(home, ".claude", "tasks");

        if (!Directory.Exists(tasksRoot))
            return ToolResult.Success("No tasks found.");

        var lines = new List<string>();

        foreach (var teamDir in Directory.GetDirectories(tasksRoot))
        {
            var teamName = Path.GetFileName(teamDir);
            var files = Directory.GetFiles(teamDir, "task-*.json");
            Array.Sort(files);

            if (files.Length == 0) continue;

            lines.Add($"Team: {teamName}");

            foreach (var file in files)
            {
                var json = await File.ReadAllTextAsync(file, ct);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                var id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : Path.GetFileNameWithoutExtension(file);
                var subject = root.TryGetProperty("subject", out var subProp) ? subProp.GetString() : "(no subject)";
                var status = root.TryGetProperty("status", out var stProp) ? stProp.GetString() : "unknown";

                lines.Add($"  {id}: [{status}] {subject}");
            }
        }

        return lines.Count == 0
            ? ToolResult.Success("No tasks found.")
            : ToolResult.Success(string.Join("\n", lines));
    }
}
