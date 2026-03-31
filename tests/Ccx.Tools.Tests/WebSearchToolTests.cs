using System.Text.Json;
using Ccx.Tools;
using Ccx.Tools.Tools;
using FluentAssertions;

namespace Ccx.Tools.Tests;

public class WebSearchToolTests
{
    private readonly WebSearchTool _tool = new();
    private readonly ToolContext _ctx = new() { WorkingDirectory = Path.GetTempPath() };

    [Fact]
    public async Task ExecuteAsync_MissingQuery_ReturnsError()
    {
        var input = JsonDocument.Parse("""{}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("query is required");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyQuery_ReturnsError()
    {
        var input = JsonDocument.Parse("""{"query": ""}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("query is required");
    }

    [Fact]
    public void Name_IsWebSearch()
    {
        _tool.Name.Should().Be("WebSearch");
    }

    [Fact]
    public void InputSchema_HasQueryProperty()
    {
        var schema = _tool.InputSchema;
        schema.GetProperty("properties").TryGetProperty("query", out _).Should().BeTrue();
        schema.GetProperty("required").EnumerateArray()
            .Any(e => e.GetString() == "query").Should().BeTrue();
    }
}
