using System.Text.Json;
using Ccx.Api.Models;
using Ccx.Api.Serialization;
using FluentAssertions;

namespace Ccx.Api.Tests;

public class ModelSerializationTests
{
    [Fact]
    public void MessageRequest_Serializes_WithSnakeCaseProperties()
    {
        var request = new MessageRequest
        {
            Model = "claude-sonnet-4-20250514",
            MaxTokens = 1024,
            Stream = true,
            Messages = [Message.User("Hello")]
        };

        var json = JsonSerializer.Serialize(request, CcxJsonContext.Default.MessageRequest);

        json.Should().Contain("\"model\":");
        json.Should().Contain("\"max_tokens\":1024");
        json.Should().Contain("\"stream\":true");
        json.Should().Contain("\"messages\":");
    }

    [Fact]
    public void MessageRequest_OmitsNullTools()
    {
        var request = new MessageRequest
        {
            Model = "test",
            MaxTokens = 100,
            Messages = []
        };

        var json = JsonSerializer.Serialize(request, CcxJsonContext.Default.MessageRequest);

        json.Should().NotContain("\"tools\"");
    }

    [Fact]
    public void ContentBlock_Text_SerializesCorrectly()
    {
        var block = ContentBlock.CreateText("Hello, world!");

        var json = JsonSerializer.Serialize(block, CcxJsonContext.Default.ContentBlock);

        json.Should().Contain("\"type\":\"text\"");
        json.Should().Contain("\"text\":\"Hello, world!\"");
        json.Should().NotContain("\"id\"");
        json.Should().NotContain("\"tool_use_id\"");
    }

    [Fact]
    public void ContentBlock_ToolResult_SerializesCorrectly()
    {
        var block = ContentBlock.CreateToolResult("toolu_123", "file contents", isError: false);

        var json = JsonSerializer.Serialize(block, CcxJsonContext.Default.ContentBlock);

        json.Should().Contain("\"type\":\"tool_result\"");
        json.Should().Contain("\"tool_use_id\":\"toolu_123\"");
        json.Should().Contain("\"content\":\"file contents\"");
        json.Should().NotContain("\"is_error\"");
    }

    [Fact]
    public void ContentBlock_ToolResult_WithError_IncludesIsError()
    {
        var block = ContentBlock.CreateToolResult("toolu_123", "error msg", isError: true);

        var json = JsonSerializer.Serialize(block, CcxJsonContext.Default.ContentBlock);

        json.Should().Contain("\"is_error\":true");
    }

    [Fact]
    public void StreamEvent_Deserializes_ContentBlockStart()
    {
        var json = """{"type":"content_block_start","index":0,"content_block":{"type":"text","text":""}}""";

        var evt = JsonSerializer.Deserialize(json, CcxJsonContext.Default.StreamEvent);

        evt.Should().NotBeNull();
        evt!.Type.Should().Be("content_block_start");
        evt.Index.Should().Be(0);
        evt.ContentBlock.Should().NotBeNull();
        evt.ContentBlock!.Type.Should().Be("text");
    }

    [Fact]
    public void StreamEvent_Deserializes_ContentBlockDelta()
    {
        var json = """{"type":"content_block_delta","index":0,"delta":{"type":"text_delta","text":"Hello"}}""";

        var evt = JsonSerializer.Deserialize(json, CcxJsonContext.Default.StreamEvent);

        evt.Should().NotBeNull();
        evt!.Type.Should().Be("content_block_delta");
        evt.Delta.Should().NotBeNull();
        evt.Delta!.Type.Should().Be("text_delta");
        evt.Delta.Text.Should().Be("Hello");
    }

    [Fact]
    public void StreamEvent_Deserializes_MessageDelta_WithStopReason()
    {
        var json = """{"type":"message_delta","delta":{"stop_reason":"end_turn"},"usage":{"output_tokens":15}}""";

        var evt = JsonSerializer.Deserialize(json, CcxJsonContext.Default.StreamEvent);

        evt.Should().NotBeNull();
        evt!.Type.Should().Be("message_delta");
        evt.Delta!.StopReason.Should().Be("end_turn");
        evt.Usage!.OutputTokens.Should().Be(15);
    }

    [Fact]
    public void StreamEvent_Deserializes_ToolUseContentBlock()
    {
        var json = """{"type":"content_block_start","index":1,"content_block":{"type":"tool_use","id":"toolu_abc","name":"bash","input":{}}}""";

        var evt = JsonSerializer.Deserialize(json, CcxJsonContext.Default.StreamEvent);

        evt.Should().NotBeNull();
        evt!.ContentBlock!.Type.Should().Be("tool_use");
        evt.ContentBlock.Id.Should().Be("toolu_abc");
        evt.ContentBlock.Name.Should().Be("bash");
        evt.ContentBlock.Input.Should().NotBeNull();
    }

    [Fact]
    public void Message_User_CreatesTextContentBlock()
    {
        var msg = Message.User("test input");

        var json = JsonSerializer.Serialize(msg, CcxJsonContext.Default.Message);

        json.Should().Contain("\"role\":\"user\"");
        json.Should().Contain("\"type\":\"text\"");
        json.Should().Contain("\"text\":\"test input\"");
    }
}
