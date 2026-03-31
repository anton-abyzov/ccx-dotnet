using Ccx.Api;
using FluentAssertions;

namespace Ccx.Api.Tests;

public class SseParserTests
{
    [Fact]
    public void Parse_SingleEvent_ReturnsEventTypeAndData()
    {
        var lines = new[]
        {
            "event: message_start",
            "data: {\"type\":\"message_start\"}"
        };

        var events = SseParser.Parse(lines).ToList();

        events.Should().HaveCount(1);
        events[0].EventType.Should().Be("message_start");
        events[0].Data.Should().Be("{\"type\":\"message_start\"}");
    }

    [Fact]
    public void Parse_MultipleEvents_ReturnsAll()
    {
        var lines = new[]
        {
            "event: content_block_start",
            "data: {\"type\":\"content_block_start\",\"index\":0}",
            "",
            "event: content_block_delta",
            "data: {\"type\":\"content_block_delta\",\"index\":0}",
            "",
            "event: content_block_stop",
            "data: {\"type\":\"content_block_stop\",\"index\":0}"
        };

        var events = SseParser.Parse(lines).ToList();

        events.Should().HaveCount(3);
        events[0].EventType.Should().Be("content_block_start");
        events[1].EventType.Should().Be("content_block_delta");
        events[2].EventType.Should().Be("content_block_stop");
    }

    [Fact]
    public void Parse_EmptyLines_AreIgnored()
    {
        var lines = new[]
        {
            "",
            "event: ping",
            "",
            "data: {}",
            ""
        };

        var events = SseParser.Parse(lines).ToList();

        events.Should().HaveCount(1);
        events[0].EventType.Should().Be("ping");
    }

    [Fact]
    public void Parse_DataWithoutEvent_IsIgnored()
    {
        var lines = new[]
        {
            "data: {\"orphan\":true}",
            "event: valid",
            "data: {\"valid\":true}"
        };

        var events = SseParser.Parse(lines).ToList();

        events.Should().HaveCount(1);
        events[0].EventType.Should().Be("valid");
    }

    [Fact]
    public void Parse_NoLines_ReturnsEmpty()
    {
        var events = SseParser.Parse([]).ToList();
        events.Should().BeEmpty();
    }
}
