using System.Text;
using System.Text.Json;

namespace Ccx.Tools.Tools;

public sealed class FileReadTool : ITool
{
    public string Name => "Read";

    public string Description => "Read a file with line numbers, optional offset and limit.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "file_path": { "type": "string", "description": "Absolute path to the file" },
                "offset": { "type": "integer", "description": "Line number to start from (0-based)" },
                "limit": { "type": "integer", "description": "Number of lines to read" }
            },
            "required": ["file_path"]
        }
        """).RootElement.Clone();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        var filePath = input.GetProperty("file_path").GetString();
        if (string.IsNullOrWhiteSpace(filePath))
            return ToolResult.Error("file_path is required.");

        filePath = Path.GetFullPath(filePath, ctx.WorkingDirectory);

        if (!File.Exists(filePath))
            return ToolResult.Error($"File not found: {filePath}");

        var offset = input.TryGetProperty("offset", out var o) ? o.GetInt32() : 0;
        var limit = input.TryGetProperty("limit", out var l) ? l.GetInt32() : 2000;

        var lines = await File.ReadAllLinesAsync(filePath, ct);
        var sb = new StringBuilder();
        var end = Math.Min(offset + limit, lines.Length);

        for (var i = offset; i < end; i++)
        {
            sb.AppendLine($"{i + 1}\t{lines[i]}");
        }

        return ToolResult.Success(sb.ToString());
    }
}
