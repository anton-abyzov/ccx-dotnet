using System.Text.Json.Serialization;

namespace Ccx.Api.Models;

public sealed class Message
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = "";

    [JsonPropertyName("content")]
    public List<ContentBlock> Content { get; set; } = [];

    public static Message User(string text) =>
        new() { Role = "user", Content = [ContentBlock.CreateText(text)] };

    public static Message User(List<ContentBlock> content) =>
        new() { Role = "user", Content = content };

    public static Message Assistant(List<ContentBlock> content) =>
        new() { Role = "assistant", Content = content };
}
