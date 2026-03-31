using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ccx.Tools.Tools;

public sealed class TodoWriteTool : ITool
{
    private const string TodoFileName = ".ccx-todos.json";

    public string Name => "TodoWrite";

    public string Description => "Write and manage a todo list. Replaces the entire todo list with the provided items.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "todos": {
                    "type": "array",
                    "description": "The complete list of todo items",
                    "items": {
                        "type": "object",
                        "properties": {
                            "id": { "type": "string", "description": "Unique identifier for the todo" },
                            "content": { "type": "string", "description": "The todo item text" },
                            "status": {
                                "type": "string",
                                "enum": ["pending", "in_progress", "completed"],
                                "description": "Current status of the todo"
                            },
                            "priority": {
                                "type": "string",
                                "enum": ["high", "medium", "low"],
                                "description": "Priority level"
                            }
                        },
                        "required": ["id", "content", "status"]
                    }
                }
            },
            "required": ["todos"]
        }
        """).RootElement.Clone();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        if (!input.TryGetProperty("todos", out var todosElement) || todosElement.ValueKind != JsonValueKind.Array)
            return ToolResult.Error("todos array is required.");

        var todos = new List<TodoItem>();
        foreach (var item in todosElement.EnumerateArray())
        {
            var id = item.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            var content = item.TryGetProperty("content", out var contentProp) ? contentProp.GetString() : null;
            var status = item.TryGetProperty("status", out var statusProp) ? statusProp.GetString() : "pending";
            var priority = item.TryGetProperty("priority", out var priorityProp) ? priorityProp.GetString() : null;

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(content))
                return ToolResult.Error("Each todo requires id and content.");

            todos.Add(new TodoItem
            {
                Id = id,
                Content = content,
                Status = status ?? "pending",
                Priority = priority
            });
        }

        var filePath = Path.Combine(ctx.WorkingDirectory, TodoFileName);
        var json = JsonSerializer.Serialize(todos, TodoJsonContext.Default.ListTodoItem);
        await File.WriteAllTextAsync(filePath, json, ct);

        var pending = todos.Count(t => t.Status == "pending");
        var inProgress = todos.Count(t => t.Status == "in_progress");
        var completed = todos.Count(t => t.Status == "completed");

        return ToolResult.Success(
            $"Wrote {todos.Count} todos to {TodoFileName} (pending: {pending}, in_progress: {inProgress}, completed: {completed})");
    }
}

public sealed class TodoItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("status")]
    public string Status { get; set; } = "pending";

    [JsonPropertyName("priority")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Priority { get; set; }
}

[JsonSerializable(typeof(List<TodoItem>))]
[JsonSerializable(typeof(TodoItem))]
internal partial class TodoJsonContext : JsonSerializerContext;
