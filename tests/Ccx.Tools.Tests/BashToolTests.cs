using System.Text.Json;
using Ccx.Tools;
using Ccx.Tools.Tools;
using FluentAssertions;

namespace Ccx.Tools.Tests;

public class BashToolTests
{
    private readonly BashTool _tool = new();
    private readonly ToolContext _ctx = new() { WorkingDirectory = Path.GetTempPath() };

    [Fact]
    public async Task ExecuteAsync_Echo_ReturnsOutput()
    {
        var input = JsonDocument.Parse("""{"command": "echo hello"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Output.Trim().Should().Be("hello");
    }

    [Fact]
    public async Task ExecuteAsync_FailingCommand_ReturnsError()
    {
        var input = JsonDocument.Parse("""{"command": "false"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("Exit code");
    }

    [Fact]
    public async Task ExecuteAsync_DangerousCommand_Blocked()
    {
        var input = JsonDocument.Parse("""{"command": "rm -rf /"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("Blocked");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyCommand_ReturnsError()
    {
        var input = JsonDocument.Parse("""{"command": ""}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
    }

    [Fact]
    public void Name_IsBash() => _tool.Name.Should().Be("Bash");
}
