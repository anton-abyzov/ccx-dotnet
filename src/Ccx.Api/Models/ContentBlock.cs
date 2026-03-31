using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ccx.Api.Models;

public sealed class ContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("input")]
    public JsonElement? Input { get; set; }

    [JsonPropertyName("tool_use_id")]
    public string? ToolUseId { get; set; }

    [JsonPropertyName("content")]
    public string? ToolResultContent { get; set; }

    [JsonPropertyName("is_error")]
    public bool? IsError { get; set; }

    [JsonPropertyName("thinking")]
    public string? Thinking { get; set; }

    public static ContentBlock CreateText(string text) =>
        new() { Type = "text", Text = text };

    public static ContentBlock CreateToolUse(string id, string name, JsonElement input) =>
        new() { Type = "tool_use", Id = id, Name = name, Input = input };

    public static ContentBlock CreateToolResult(string toolUseId, string content, bool isError = false) =>
        new() { Type = "tool_result", ToolUseId = toolUseId, ToolResultContent = content, IsError = isError ? true : null };

    public static ContentBlock CreateThinking(string thinking) =>
        new() { Type = "thinking", Thinking = thinking };
}
