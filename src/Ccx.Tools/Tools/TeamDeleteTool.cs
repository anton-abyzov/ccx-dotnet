using System.Text.Json;

namespace Ccx.Tools.Tools;

public sealed class TeamDeleteTool : ITool
{
    public string Name => "TeamDelete";

    public string Description => "Remove a team and its task directory.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "team_name": { "type": "string", "description": "Name of the team to delete" }
            },
            "required": ["team_name"]
        }
        """).RootElement.Clone();

    public Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        if (!input.TryGetProperty("team_name", out var nameProp) || string.IsNullOrWhiteSpace(nameProp.GetString()))
            return Task.FromResult(ToolResult.Error("team_name is required."));

        var teamName = nameProp.GetString()!;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var teamDir = Path.Combine(home, ".claude", "teams", teamName);
        var taskDir = Path.Combine(home, ".claude", "tasks", teamName);

        if (Directory.Exists(teamDir))
            Directory.Delete(teamDir, recursive: true);

        if (Directory.Exists(taskDir))
            Directory.Delete(taskDir, recursive: true);

        return Task.FromResult(ToolResult.Success($"Deleted team '{teamName}' and its task directory."));
    }
}
