using System.Text.Json;

namespace Ccx.Tools.Tools;

public sealed class TeamCreateTool : ITool
{
    public string Name => "TeamCreate";

    public string Description => "Create a named team with task directory for multi-agent coordination.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "team_name": { "type": "string", "description": "Name of the team to create" },
                "description": { "type": "string", "description": "Description of the team's purpose" }
            },
            "required": ["team_name", "description"]
        }
        """).RootElement.Clone();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        if (!input.TryGetProperty("team_name", out var nameProp) || string.IsNullOrWhiteSpace(nameProp.GetString()))
            return ToolResult.Error("team_name is required.");

        if (!input.TryGetProperty("description", out var descProp) || string.IsNullOrWhiteSpace(descProp.GetString()))
            return ToolResult.Error("description is required.");

        var teamName = nameProp.GetString()!;
        var description = descProp.GetString()!;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        var teamDir = Path.Combine(home, ".claude", "teams", teamName);
        var taskDir = Path.Combine(home, ".claude", "tasks", teamName);

        Directory.CreateDirectory(teamDir);
        Directory.CreateDirectory(taskDir);

        var config = new
        {
            name = teamName,
            description,
            created_at = DateTime.UtcNow.ToString("o")
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(teamDir, "config.json"), json, ct);

        return ToolResult.Success($"Created team '{teamName}' at {teamDir} with task directory at {taskDir}.");
    }
}
