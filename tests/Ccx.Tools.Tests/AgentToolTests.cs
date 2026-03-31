using System.Text.Json;
using Ccx.Tools;
using Ccx.Tools.Tools;
using FluentAssertions;

namespace Ccx.Tools.Tests;

public class AgentToolTests
{
    private readonly ToolContext _ctx = new() { WorkingDirectory = Path.GetTempPath() };

    private sealed class FakeSpawner : IAgentSpawner
    {
        public string? LastPrompt { get; private set; }
        public string? LastDescription { get; private set; }
        public string Response { get; set; } = "Agent completed the task.";
        public bool ShouldThrow { get; set; }

        public Task<string> SpawnAsync(string prompt, string? description, ToolContext ctx, CancellationToken ct)
        {
            LastPrompt = prompt;
            LastDescription = description;
            if (ShouldThrow) throw new InvalidOperationException("Spawn failed");
            return Task.FromResult(Response);
        }
    }

    [Fact]
    public async Task ExecuteAsync_SpawnsAgentWithPrompt()
    {
        var spawner = new FakeSpawner { Response = "Done!" };
        var tool = new AgentTool(spawner);

        var input = JsonDocument.Parse("""{"prompt": "Write a test", "description": "write test"}""").RootElement;
        var result = await tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Output.Should().Be("Done!");
        spawner.LastPrompt.Should().Be("Write a test");
        spawner.LastDescription.Should().Be("write test");
    }

    [Fact]
    public async Task ExecuteAsync_MissingPrompt_ReturnsError()
    {
        var spawner = new FakeSpawner();
        var tool = new AgentTool(spawner);

        var input = JsonDocument.Parse("""{"description": "no prompt"}""").RootElement;
        var result = await tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("prompt is required");
    }

    [Fact]
    public async Task ExecuteAsync_SpawnerThrows_ReturnsError()
    {
        var spawner = new FakeSpawner { ShouldThrow = true };
        var tool = new AgentTool(spawner);

        var input = JsonDocument.Parse("""{"prompt": "do something"}""").RootElement;
        var result = await tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("Agent error");
    }

    [Fact]
    public void Name_IsAgent()
    {
        var tool = new AgentTool(new FakeSpawner());
        tool.Name.Should().Be("Agent");
    }

    [Fact]
    public void InputSchema_HasRequiredFields()
    {
        var tool = new AgentTool(new FakeSpawner());
        var schema = tool.InputSchema;

        schema.GetProperty("properties").TryGetProperty("prompt", out _).Should().BeTrue();
        schema.GetProperty("required").EnumerateArray()
            .Any(e => e.GetString() == "prompt").Should().BeTrue();
    }
}
