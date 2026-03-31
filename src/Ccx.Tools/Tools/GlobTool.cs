using System.Text.Json;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Ccx.Tools.Tools;

public sealed class GlobTool : ITool
{
    public string Name => "Glob";

    public string Description => "Find files matching a glob pattern.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "pattern": { "type": "string", "description": "Glob pattern (e.g. **/*.cs)" },
                "path": { "type": "string", "description": "Directory to search in" }
            },
            "required": ["pattern"]
        }
        """).RootElement.Clone();

    public Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        var pattern = input.GetProperty("pattern").GetString();
        if (string.IsNullOrWhiteSpace(pattern))
            return Task.FromResult(ToolResult.Error("pattern is required."));

        var path = input.TryGetProperty("path", out var p) ? p.GetString() : null;
        path = string.IsNullOrWhiteSpace(path) ? ctx.WorkingDirectory : Path.GetFullPath(path, ctx.WorkingDirectory);

        if (!Directory.Exists(path))
            return Task.FromResult(ToolResult.Error($"Directory not found: {path}"));

        var matcher = new Matcher();
        matcher.AddInclude(pattern);
        var dirInfo = new DirectoryInfoWrapper(new DirectoryInfo(path));
        var result = matcher.Execute(dirInfo);

        var files = result.Files
            .Select(f => Path.Combine(path, f.Path))
            .OrderBy(f => File.GetLastWriteTimeUtc(f))
            .ToList();

        return Task.FromResult(ToolResult.Success(
            files.Count == 0
                ? "No files matched."
                : string.Join('\n', files)));
    }
}
