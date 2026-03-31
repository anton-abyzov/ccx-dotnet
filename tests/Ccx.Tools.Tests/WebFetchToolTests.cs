using System.Text.Json;
using Ccx.Tools;
using Ccx.Tools.Tools;
using FluentAssertions;

namespace Ccx.Tools.Tests;

public class WebFetchToolTests
{
    private readonly WebFetchTool _tool = new();
    private readonly ToolContext _ctx = new();

    [Fact]
    public async Task ExecuteAsync_InvalidUrl_ReturnsError()
    {
        var input = JsonDocument.Parse("""{"url": "not-a-url"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("Invalid URL");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyUrl_ReturnsError()
    {
        var input = JsonDocument.Parse("""{"url": ""}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void Name_IsWebFetch() => _tool.Name.Should().Be("WebFetch");
}
