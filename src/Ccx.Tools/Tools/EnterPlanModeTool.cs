using System.Text.Json;

namespace Ccx.Tools.Tools;

public sealed class EnterPlanModeTool : ITool
{
    public string Name => "EnterPlanMode";

    public string Description => "Set read-only planning mode. Tools that modify files will be blocked.";

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
        var markerPath = Path.Combine(home, ".claude", ".plan_mode");

        Directory.CreateDirectory(Path.GetDirectoryName(markerPath)!);
        await File.WriteAllTextAsync(markerPath, DateTime.UtcNow.ToString("o"), ct);

        return ToolResult.Success("Plan mode enabled. File-modifying tools are now blocked.");
    }
}
