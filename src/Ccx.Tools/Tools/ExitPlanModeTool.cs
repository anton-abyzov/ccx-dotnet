using System.Text.Json;

namespace Ccx.Tools.Tools;

public sealed class ExitPlanModeTool : ITool
{
    public string Name => "ExitPlanMode";

    public string Description => "Exit planning mode and allow file modifications.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {},
            "required": []
        }
        """).RootElement.Clone();

    public Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var markerPath = Path.Combine(home, ".claude", ".plan_mode");

        if (File.Exists(markerPath))
            File.Delete(markerPath);

        return Task.FromResult(ToolResult.Success("Plan mode disabled. File modifications are now allowed."));
    }
}
