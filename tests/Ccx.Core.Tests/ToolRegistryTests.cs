using System.Text.Json;
using Ccx.Tools;
using FluentAssertions;

namespace Ccx.Core.Tests;

public class ToolRegistryTests
{
    private static readonly JsonDocument SchemaDoc = JsonDocument.Parse("""{"type":"object","properties":{}}""");

    private sealed class FakeTool : ITool
    {
        public string Name { get; init; } = "fake";
        public string Description { get; init; } = "A fake tool";
        public JsonElement InputSchema => SchemaDoc.RootElement;

        public Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
        {
            return Task.FromResult(ToolResult.Success("ok"));
        }
    }

    [Fact]
    public void Register_And_FindByName_ReturnsTool()
    {
        var registry = new ToolRegistry();
        var tool = new FakeTool { Name = "test_tool" };

        registry.Register(tool);

        registry.FindByName("test_tool").Should().BeSameAs(tool);
    }

    [Fact]
    public void FindByName_CaseInsensitive()
    {
        var registry = new ToolRegistry();
        registry.Register(new FakeTool { Name = "MyTool" });

        registry.FindByName("mytool").Should().NotBeNull();
        registry.FindByName("MYTOOL").Should().NotBeNull();
    }

    [Fact]
    public void FindByName_Unknown_ReturnsNull()
    {
        var registry = new ToolRegistry();

        registry.FindByName("nonexistent").Should().BeNull();
    }

    [Fact]
    public void All_Empty_ReturnsEmptyCollection()
    {
        var registry = new ToolRegistry();

        registry.All.Should().BeEmpty();
        registry.Count.Should().Be(0);
    }

    [Fact]
    public void All_WithTools_ReturnsRegisteredTools()
    {
        var registry = new ToolRegistry();
        registry.Register(new FakeTool { Name = "alpha", Description = "Tool A" });
        registry.Register(new FakeTool { Name = "beta", Description = "Tool B" });

        registry.All.Should().HaveCount(2);
        registry.All.Select(t => t.Name).Should().BeEquivalentTo(["alpha", "beta"]);
    }

    [Fact]
    public void Register_SameName_OverwritesPrevious()
    {
        var registry = new ToolRegistry();
        registry.Register(new FakeTool { Name = "dup", Description = "first" });
        registry.Register(new FakeTool { Name = "dup", Description = "second" });

        registry.Count.Should().Be(1);
        registry.FindByName("dup")!.Description.Should().Be("second");
    }
}
