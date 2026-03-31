using Ccx.Compact;
using FluentAssertions;

namespace Ccx.Compact.Tests;

public class TokenEstimatorTests
{
    [Fact]
    public void Estimate_EmptyString_ReturnsZero()
    {
        TokenEstimator.Estimate("").Should().Be(0);
    }

    [Fact]
    public void Estimate_ShortText_ReturnsReasonableCount()
    {
        var tokens = TokenEstimator.Estimate("Hello, world!");
        tokens.Should().BeGreaterThan(0);
        tokens.Should().BeLessThan(20);
    }

    [Fact]
    public void Estimate_LongText_ScalesLinearly()
    {
        var short_ = TokenEstimator.Estimate("Hello");
        var long_ = TokenEstimator.Estimate(new string('a', 1000));
        long_.Should().BeGreaterThan(short_ * 10);
    }

    [Fact]
    public void EstimateMessages_IncludesOverhead()
    {
        var messages = new[] { "Hello", "World" };
        var total = TokenEstimator.EstimateMessages(messages);
        var raw = TokenEstimator.Estimate("Hello") + TokenEstimator.Estimate("World");
        total.Should().BeGreaterThan(raw); // overhead per message
    }
}

public class MicroCompactTests
{
    [Fact]
    public void CompactToolResult_ShortText_Unchanged()
    {
        var result = MicroCompact.CompactToolResult("short text", 1000);
        result.Should().Be("short text");
    }

    [Fact]
    public void CompactToolResult_LongText_Truncated()
    {
        var longText = new string('x', 20_000);
        var result = MicroCompact.CompactToolResult(longText, 1000);

        result.Length.Should().BeLessThan(longText.Length);
        result.Should().Contain("truncated");
    }
}
