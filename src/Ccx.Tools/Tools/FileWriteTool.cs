using System.Text.Json;

namespace Ccx.Tools.Tools;

public sealed class FileWriteTool : ITool
{
    public string Name => "Write";

    public string Description => "Create or overwrite a file with the given content.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "file_path": { "type": "string", "description": "Absolute path to the file" },
                "content": { "type": "string", "description": "Content to write" }
            },
            "required": ["file_path", "content"]
        }
        """).RootElement.Clone();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        var filePath = input.GetProperty("file_path").GetString();
        var content = input.GetProperty("content").GetString();

        if (string.IsNullOrWhiteSpace(filePath))
            return ToolResult.Error("file_path is required.");

        filePath = Path.GetFullPath(filePath, ctx.WorkingDirectory);

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllTextAsync(filePath, content ?? "", ct);
        return ToolResult.Success($"Wrote {(content ?? "").Length} bytes to {filePath}");
    }
}
