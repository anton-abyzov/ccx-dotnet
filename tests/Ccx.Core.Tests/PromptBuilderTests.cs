using System.Text.Json;
using Ccx.Core;
using Ccx.Tools;
using FluentAssertions;

namespace Ccx.Core.Tests;

public class PromptBuilderTests
{
    private sealed class FakeTool : ITool
    {
        public string Name { get; init; } = "TestTool";
        public string Description { get; init; } = "A test tool.";
        public JsonElement InputSchema { get; } = JsonDocument.Parse("""{"type":"object","properties":{}}""").RootElement.Clone();
        public Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct) =>
            Task.FromResult(ToolResult.Success("ok"));
    }

    [Fact]
    public void BuildSystemPrompt_IncludesToolDescriptions()
    {
        var tools = new ITool[]
        {
            new FakeTool { Name = "Bash", Description = "Execute commands" },
            new FakeTool { Name = "Read", Description = "Read files" }
        };

        var prompt = PromptBuilder.BuildSystemPrompt(tools);

        prompt.Should().Contain("## Bash");
        prompt.Should().Contain("Execute commands");
        prompt.Should().Contain("## Read");
        prompt.Should().Contain("Read files");
    }

    [Fact]
    public void BuildSystemPrompt_IncludesClaudeMd()
    {
        var tools = Array.Empty<ITool>();
        var claudeMd = "Always use TypeScript.";

        var prompt = PromptBuilder.BuildSystemPrompt(tools, claudeMd);

        prompt.Should().Contain("Project Instructions (CLAUDE.md)");
        prompt.Should().Contain("Always use TypeScript.");
    }

    [Fact]
    public void BuildSystemPrompt_EmptyClaudeMd_OmitsSection()
    {
        var tools = Array.Empty<ITool>();

        var prompt = PromptBuilder.BuildSystemPrompt(tools, "");

        prompt.Should().NotContain("CLAUDE.md");
    }

    [Fact]
    public void BuildSystemPrompt_NoTools_OmitsToolSection()
    {
        var tools = Array.Empty<ITool>();

        var prompt = PromptBuilder.BuildSystemPrompt(tools);

        prompt.Should().NotContain("## Bash");
    }

    [Fact]
    public void BuildSystemPrompt_IncludesEnvironmentInfo()
    {
        var tools = Array.Empty<ITool>();

        var prompt = PromptBuilder.BuildSystemPrompt(tools);

        prompt.Should().Contain("Working directory");
        prompt.Should().Contain("Platform");
        prompt.Should().Contain("Date");
    }

    [Fact]
    public void BuildSystemPrompt_IncludesGuidelines()
    {
        var prompt = PromptBuilder.BuildSystemPrompt(Array.Empty<ITool>());

        prompt.Should().Contain("Read files before editing");
        prompt.Should().Contain("concise and direct");
    }
}
