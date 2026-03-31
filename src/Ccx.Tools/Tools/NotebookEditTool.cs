using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ccx.Tools.Tools;

public sealed class NotebookEditTool : ITool
{
    public string Name => "NotebookEdit";

    public string Description => "Edit a Jupyter notebook (.ipynb) file. Can insert, replace, or delete cells.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "file_path": { "type": "string", "description": "Path to the .ipynb file" },
                "operation": {
                    "type": "string",
                    "enum": ["insert", "replace", "delete"],
                    "description": "The operation to perform on cells"
                },
                "cell_index": { "type": "integer", "description": "Zero-based index of the cell to operate on" },
                "cell_type": {
                    "type": "string",
                    "enum": ["code", "markdown", "raw"],
                    "description": "Type of cell (for insert/replace)"
                },
                "source": { "type": "string", "description": "Cell content (for insert/replace)" }
            },
            "required": ["file_path", "operation", "cell_index"]
        }
        """).RootElement.Clone();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        var filePath = input.TryGetProperty("file_path", out var fp) ? fp.GetString() : null;
        if (string.IsNullOrWhiteSpace(filePath))
            return ToolResult.Error("file_path is required.");

        var fullPath = Path.IsPathRooted(filePath)
            ? filePath
            : Path.GetFullPath(filePath, ctx.WorkingDirectory);

        var operation = input.TryGetProperty("operation", out var op) ? op.GetString() : null;
        if (string.IsNullOrWhiteSpace(operation))
            return ToolResult.Error("operation is required.");

        var cellIndex = input.TryGetProperty("cell_index", out var ci) ? ci.GetInt32() : -1;
        if (cellIndex < 0)
            return ToolResult.Error("cell_index must be a non-negative integer.");

        JsonNode? notebook;
        if (File.Exists(fullPath))
        {
            var content = await File.ReadAllTextAsync(fullPath, ct);
            notebook = JsonNode.Parse(content);
        }
        else if (operation == "insert")
        {
            // Create new notebook structure
            notebook = JsonNode.Parse("""
                {
                    "nbformat": 4,
                    "nbformat_minor": 2,
                    "metadata": {
                        "kernelspec": { "display_name": "Python 3", "language": "python", "name": "python3" }
                    },
                    "cells": []
                }
                """);
        }
        else
        {
            return ToolResult.Error($"File not found: {fullPath}");
        }

        if (notebook is null)
            return ToolResult.Error("Failed to parse notebook JSON.");

        var cells = notebook["cells"]?.AsArray();
        if (cells is null)
            return ToolResult.Error("Notebook has no cells array.");

        switch (operation)
        {
            case "insert":
            {
                var cellType = input.TryGetProperty("cell_type", out var ct2) ? ct2.GetString() : "code";
                var source = input.TryGetProperty("source", out var src) ? src.GetString() ?? "" : "";

                var newCell = CreateCell(cellType ?? "code", source);
                var insertAt = Math.Min(cellIndex, cells.Count);
                cells.Insert(insertAt, newCell);

                await WriteNotebookAsync(fullPath, notebook, ct);
                return ToolResult.Success($"Inserted {cellType} cell at index {insertAt}. Notebook has {cells.Count} cells.");
            }
            case "replace":
            {
                if (cellIndex >= cells.Count)
                    return ToolResult.Error($"cell_index {cellIndex} out of range (notebook has {cells.Count} cells).");

                var cellType = input.TryGetProperty("cell_type", out var ct2) ? ct2.GetString() : null;
                var source = input.TryGetProperty("source", out var src) ? src.GetString() ?? "" : "";

                // Preserve cell type if not specified
                cellType ??= cells[cellIndex]?["cell_type"]?.GetValue<string>() ?? "code";

                var newCell = CreateCell(cellType, source);
                cells[cellIndex] = newCell;

                await WriteNotebookAsync(fullPath, notebook, ct);
                return ToolResult.Success($"Replaced cell at index {cellIndex} with {cellType} cell.");
            }
            case "delete":
            {
                if (cellIndex >= cells.Count)
                    return ToolResult.Error($"cell_index {cellIndex} out of range (notebook has {cells.Count} cells).");

                cells.RemoveAt(cellIndex);

                await WriteNotebookAsync(fullPath, notebook, ct);
                return ToolResult.Success($"Deleted cell at index {cellIndex}. Notebook has {cells.Count} cells.");
            }
            default:
                return ToolResult.Error($"Unknown operation: {operation}. Use insert, replace, or delete.");
        }
    }

    private static JsonNode CreateCell(string cellType, string source)
    {
        var sourceLines = SplitSourceLines(source);
        var cell = new JsonObject
        {
            ["cell_type"] = cellType,
            ["metadata"] = new JsonObject(),
            ["source"] = sourceLines
        };

        if (cellType == "code")
        {
            cell["execution_count"] = null;
            cell["outputs"] = new JsonArray();
        }

        return cell;
    }

    private static JsonArray SplitSourceLines(string source)
    {
        if (string.IsNullOrEmpty(source))
            return new JsonArray();

        var lines = source.Split('\n');
        var arr = new JsonArray();
        for (var i = 0; i < lines.Length; i++)
        {
            arr.Add(i < lines.Length - 1 ? lines[i] + "\n" : lines[i]);
        }
        return arr;
    }

    private static async Task WriteNotebookAsync(string path, JsonNode notebook, CancellationToken ct)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = notebook.ToJsonString(options);
        await File.WriteAllTextAsync(path, json + "\n", ct);
    }
}
