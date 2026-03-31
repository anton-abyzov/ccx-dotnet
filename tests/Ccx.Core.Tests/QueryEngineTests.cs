using System.Runtime.CompilerServices;
using System.Text.Json;
using Ccx.Api;
using Ccx.Api.Models;
using Ccx.Core;
using Ccx.Tools;
using FluentAssertions;

namespace Ccx.Core.Tests;

public class QueryEngineTests
{
    private static readonly JsonDocument SchemaDoc = JsonDocument.Parse("""{"type":"object","properties":{}}""");

    private sealed class MockClaudeClient : IClaudeClient
    {
        private readonly Func<MessageRequest, IReadOnlyList<StreamEvent>> _handler;

        public MockClaudeClient(Func<MessageRequest, IReadOnlyList<StreamEvent>> handler)
        {
            _handler = handler;
        }

        public async IAsyncEnumerable<StreamEvent> StreamMessageAsync(
            MessageRequest request, [EnumeratorCancellation] CancellationToken ct = default)
        {
            foreach (var evt in _handler(request))
            {
                yield return evt;
            }
            await Task.CompletedTask;
        }
    }

    private sealed class EchoTool : ITool
    {
        public string Name => "echo";
        public string Description => "Echoes input";
        public JsonElement InputSchema => SchemaDoc.RootElement;

        public Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
        {
            var text = input.TryGetProperty("text", out var prop) ? prop.GetString() ?? "" : "";
            return Task.FromResult(ToolResult.Success($"echo: {text}"));
        }
    }

    [Fact]
    public async Task RunAsync_TextResponse_ReturnsBlocks()
    {
        var client = new MockClaudeClient(_ =>
        [
            new StreamEvent
            {
                Type = "content_block_start",
                Index = 0,
                ContentBlock = new ContentBlock { Type = "text", Text = "" }
            },
            new StreamEvent
            {
                Type = "content_block_delta",
                Index = 0,
                Delta = new Delta { Type = "text_delta", Text = "Hello!" }
            },
            new StreamEvent { Type = "content_block_stop", Index = 0 },
            new StreamEvent
            {
                Type = "message_delta",
                Delta = new Delta { StopReason = "end_turn" }
            },
            new StreamEvent { Type = "message_stop" }
        ]);

        var engine = new QueryEngine(client, new ToolRegistry());
        var messages = new List<Message> { Message.User("Hi") };

        var result = await engine.RunAsync(messages);

        result.Should().HaveCount(1);
        result[0].Type.Should().Be("text");
        result[0].Text.Should().Be("Hello!");
    }

    [Fact]
    public async Task RunAsync_ToolUse_ExecutesToolAndLoops()
    {
        var callCount = 0;
        var client = new MockClaudeClient(req =>
        {
            callCount++;
            if (callCount == 1)
            {
                // First call: model requests tool use
                return
                [
                    new StreamEvent
                    {
                        Type = "content_block_start",
                        Index = 0,
                        ContentBlock = new ContentBlock { Type = "tool_use", Id = "toolu_1", Name = "echo" }
                    },
                    new StreamEvent
                    {
                        Type = "content_block_delta",
                        Index = 0,
                        Delta = new Delta { Type = "input_json_delta", PartialJson = "{\"text\":\"world\"}" }
                    },
                    new StreamEvent { Type = "content_block_stop", Index = 0 },
                    new StreamEvent
                    {
                        Type = "message_delta",
                        Delta = new Delta { StopReason = "tool_use" }
                    },
                    new StreamEvent { Type = "message_stop" }
                ];
            }

            // Second call: model gives final text response
            return
            [
                new StreamEvent
                {
                    Type = "content_block_start",
                    Index = 0,
                    ContentBlock = new ContentBlock { Type = "text", Text = "" }
                },
                new StreamEvent
                {
                    Type = "content_block_delta",
                    Index = 0,
                    Delta = new Delta { Type = "text_delta", Text = "Done!" }
                },
                new StreamEvent { Type = "content_block_stop", Index = 0 },
                new StreamEvent
                {
                    Type = "message_delta",
                    Delta = new Delta { StopReason = "end_turn" }
                },
                new StreamEvent { Type = "message_stop" }
            ];
        });

        var tools = new ToolRegistry();
        tools.Register(new EchoTool());

        var engine = new QueryEngine(client, tools);
        var messages = new List<Message> { Message.User("echo world") };

        var result = await engine.RunAsync(messages);

        callCount.Should().Be(2);
        result.Should().HaveCount(1);
        result[0].Text.Should().Be("Done!");

        // user, assistant(tool_use), user(tool_result) — final response is returned, not appended
        messages.Should().HaveCount(3);
        messages[2].Role.Should().Be("user");
        messages[2].Content[0].Type.Should().Be("tool_result");
        messages[2].Content[0].ToolResultContent.Should().Be("echo: world");
    }

    [Fact]
    public async Task RunAsync_UnknownTool_ReturnsError()
    {
        var callCount = 0;
        var client = new MockClaudeClient(req =>
        {
            callCount++;
            if (callCount == 1)
            {
                return
                [
                    new StreamEvent
                    {
                        Type = "content_block_start",
                        Index = 0,
                        ContentBlock = new ContentBlock { Type = "tool_use", Id = "toolu_1", Name = "unknown" }
                    },
                    new StreamEvent
                    {
                        Type = "content_block_delta",
                        Index = 0,
                        Delta = new Delta { Type = "input_json_delta", PartialJson = "{}" }
                    },
                    new StreamEvent { Type = "content_block_stop", Index = 0 },
                    new StreamEvent
                    {
                        Type = "message_delta",
                        Delta = new Delta { StopReason = "tool_use" }
                    },
                    new StreamEvent { Type = "message_stop" }
                ];
            }

            return
            [
                new StreamEvent
                {
                    Type = "content_block_start",
                    Index = 0,
                    ContentBlock = new ContentBlock { Type = "text", Text = "" }
                },
                new StreamEvent
                {
                    Type = "content_block_delta",
                    Index = 0,
                    Delta = new Delta { Type = "text_delta", Text = "OK" }
                },
                new StreamEvent { Type = "content_block_stop", Index = 0 },
                new StreamEvent
                {
                    Type = "message_delta",
                    Delta = new Delta { StopReason = "end_turn" }
                },
                new StreamEvent { Type = "message_stop" }
            ];
        });

        var engine = new QueryEngine(client, new ToolRegistry());
        var messages = new List<Message> { Message.User("use tool") };

        await engine.RunAsync(messages);

        var toolResult = messages[2].Content[0];
        toolResult.Type.Should().Be("tool_result");
        toolResult.IsError.Should().Be(true);
        toolResult.ToolResultContent.Should().Contain("not found");
    }

    [Fact]
    public async Task RunAsync_StreamsTextDelta_ToCallback()
    {
        var client = new MockClaudeClient(_ =>
        [
            new StreamEvent
            {
                Type = "content_block_start",
                Index = 0,
                ContentBlock = new ContentBlock { Type = "text", Text = "" }
            },
            new StreamEvent
            {
                Type = "content_block_delta",
                Index = 0,
                Delta = new Delta { Type = "text_delta", Text = "chunk1" }
            },
            new StreamEvent
            {
                Type = "content_block_delta",
                Index = 0,
                Delta = new Delta { Type = "text_delta", Text = "chunk2" }
            },
            new StreamEvent { Type = "content_block_stop", Index = 0 },
            new StreamEvent
            {
                Type = "message_delta",
                Delta = new Delta { StopReason = "end_turn" }
            }
        ]);

        var chunks = new List<string>();
        var engine = new QueryEngine(client, new ToolRegistry(), text => chunks.Add(text));
        var messages = new List<Message> { Message.User("Hi") };

        await engine.RunAsync(messages);

        chunks.Should().BeEquivalentTo(["chunk1", "chunk2"]);
    }
}
