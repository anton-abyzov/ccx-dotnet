using System.Text.Json;
using Ccx.Tools;
using Ccx.Tools.Tools;
using FluentAssertions;

namespace Ccx.Tools.Tests;

public class TodoWriteToolTests : IDisposable
{
    private readonly TodoWriteTool _tool = new();
    private readonly string _tempDir;
    private readonly ToolContext _ctx;

    public TodoWriteToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _ctx = new ToolContext { WorkingDirectory = _tempDir };
    }

    [Fact]
    public async Task ExecuteAsync_WritesTodosToFile()
    {
        var input = JsonDocument.Parse("""
            {
                "todos": [
                    { "id": "1", "content": "Write tests", "status": "pending", "priority": "high" },
                    { "id": "2", "content": "Deploy", "status": "completed" }
                ]
            }
            """).RootElement;

        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Output.Should().Contain("2 todos");
        result.Output.Should().Contain("pending: 1");
        result.Output.Should().Contain("completed: 1");

        var filePath = Path.Combine(_tempDir, ".ccx-todos.json");
        File.Exists(filePath).Should().BeTrue();

        var json = await File.ReadAllTextAsync(filePath);
        json.Should().Contain("Write tests");
        json.Should().Contain("Deploy");
    }

    [Fact]
    public async Task ExecuteAsync_MissingTodos_ReturnsError()
    {
        var input = JsonDocument.Parse("""{}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("todos array is required");
    }

    [Fact]
    public async Task ExecuteAsync_MissingId_ReturnsError()
    {
        var input = JsonDocument.Parse("""
            {
                "todos": [
                    { "content": "No id", "status": "pending" }
                ]
            }
            """).RootElement;

        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("requires id and content");
    }

    [Fact]
    public async Task ExecuteAsync_EmptyArray_WritesEmptyFile()
    {
        var input = JsonDocument.Parse("""{"todos": []}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Output.Should().Contain("0 todos");
    }

    [Fact]
    public void Name_IsTodoWrite()
    {
        _tool.Name.Should().Be("TodoWrite");
    }

    public void Dispose() => Directory.Delete(_tempDir, true);
}
