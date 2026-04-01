using System.Text.Json;

namespace Ccx.Tools.Tools;

public sealed class SendMessageTool : ITool
{
    public string Name => "SendMessage";

    public string Description => "Send a message to a teammate or broadcast to all team members.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "to": { "type": "string", "description": "Recipient name or 'all' for broadcast" },
                "message": { "type": "string", "description": "Message content" },
                "summary": { "type": "string", "description": "Optional short summary of the message" }
            },
            "required": ["to", "message"]
        }
        """).RootElement.Clone();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        if (!input.TryGetProperty("to", out var toProp) || string.IsNullOrWhiteSpace(toProp.GetString()))
            return ToolResult.Error("to is required.");

        if (!input.TryGetProperty("message", out var msgProp) || string.IsNullOrWhiteSpace(msgProp.GetString()))
            return ToolResult.Error("message is required.");

        var to = toProp.GetString()!;
        var message = msgProp.GetString()!;
        var summary = input.TryGetProperty("summary", out var sumProp) ? sumProp.GetString() : null;

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var teamsDir = Path.Combine(home, ".claude", "teams");

        if (!Directory.Exists(teamsDir))
            return ToolResult.Error("No teams found. Create a team first.");

        var teamDirs = Directory.GetDirectories(teamsDir);
        if (teamDirs.Length == 0)
            return ToolResult.Error("No teams found. Create a team first.");

        var teamDir = teamDirs[0];
        var messagesDir = Path.Combine(teamDir, "messages");
        Directory.CreateDirectory(messagesDir);

        var entry = new
        {
            from = "agent",
            to,
            message,
            summary,
            timestamp = DateTime.UtcNow.ToString("o")
        };

        var jsonLine = JsonSerializer.Serialize(entry);
        var filePath = Path.Combine(messagesDir, $"{to}.jsonl");
        await File.AppendAllTextAsync(filePath, jsonLine + "\n", ct);

        return ToolResult.Success($"Message sent to '{to}' in team '{Path.GetFileName(teamDir)}'.");
    }
}
