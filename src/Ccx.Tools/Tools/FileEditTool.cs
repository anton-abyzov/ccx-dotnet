using System.Text.Json;

namespace Ccx.Tools.Tools;

public sealed class FileEditTool : ITool
{
    public string Name => "Edit";

    public string Description => "Perform exact string replacement in a file. old_string must be unique.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "file_path": { "type": "string", "description": "Absolute path to the file" },
                "old_string": { "type": "string", "description": "Exact text to find and replace" },
                "new_string": { "type": "string", "description": "Replacement text" },
                "replace_all": { "type": "boolean", "description": "Replace all occurrences (default false)" }
            },
            "required": ["file_path", "old_string", "new_string"]
        }
        """).RootElement.Clone();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        var filePath = input.GetProperty("file_path").GetString();
        var oldString = input.GetProperty("old_string").GetString();
        var newString = input.GetProperty("new_string").GetString();
        var replaceAll = input.TryGetProperty("replace_all", out var ra) && ra.GetBoolean();

        if (string.IsNullOrWhiteSpace(filePath))
            return ToolResult.Error("file_path is required.");
        if (oldString is null)
            return ToolResult.Error("old_string is required.");

        filePath = Path.GetFullPath(filePath, ctx.WorkingDirectory);

        if (!File.Exists(filePath))
            return ToolResult.Error($"File not found: {filePath}");

        var content = await File.ReadAllTextAsync(filePath, ct);

        var count = CountOccurrences(content, oldString);
        if (count == 0)
            return ToolResult.Error("old_string not found in file.");

        if (count > 1 && !replaceAll)
            return ToolResult.Error($"old_string found {count} times. Use replace_all or provide more context.");

        var updated = replaceAll
            ? content.Replace(oldString, newString ?? "")
            : ReplaceFirst(content, oldString, newString ?? "");

        await File.WriteAllTextAsync(filePath, updated, ct);
        return ToolResult.Success($"Replaced {(replaceAll ? count : 1)} occurrence(s) in {filePath}");
    }

    private static int CountOccurrences(string text, string search)
    {
        var count = 0;
        var index = 0;
        while ((index = text.IndexOf(search, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += search.Length;
        }
        return count;
    }

    private static string ReplaceFirst(string text, string search, string replace)
    {
        var index = text.IndexOf(search, StringComparison.Ordinal);
        if (index < 0) return text;
        return string.Concat(text.AsSpan(0, index), replace, text.AsSpan(index + search.Length));
    }
}
