using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ccx.Api.Models;

public sealed class ToolDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("input_schema")]
    public JsonElement InputSchema { get; set; }
}
