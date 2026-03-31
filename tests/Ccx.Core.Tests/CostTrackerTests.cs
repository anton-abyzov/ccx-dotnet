using Ccx.Core.Cost;
using FluentAssertions;

namespace Ccx.Core.Tests;

public class CostTrackerTests
{
    [Fact]
    public void Record_TracksTokens()
    {
        var tracker = new CostTracker();
        tracker.Record("claude-sonnet-4", 1000, 500);

        tracker.TotalInputTokens.Should().Be(1000);
        tracker.TotalOutputTokens.Should().Be(500);
        tracker.RequestCount.Should().Be(1);
    }

    [Fact]
    public void Record_Multiple_Accumulates()
    {
        var tracker = new CostTracker();
        tracker.Record("claude-sonnet-4", 1000, 500);
        tracker.Record("claude-sonnet-4", 2000, 1000);

        tracker.TotalInputTokens.Should().Be(3000);
        tracker.TotalOutputTokens.Should().Be(1500);
        tracker.RequestCount.Should().Be(2);
    }

    [Fact]
    public void TotalCost_CalculatesCorrectly()
    {
        var tracker = new CostTracker();
        tracker.Record("claude-sonnet-4", 1_000_000, 0);

        // Sonnet input: $3/M tokens
        tracker.TotalCost.Should().Be(3m);
    }

    [Fact]
    public void GetSummary_ContainsAllFields()
    {
        var tracker = new CostTracker();
        tracker.Record("claude-sonnet-4", 100, 50);

        var summary = tracker.GetSummary();
        summary.Should().Contain("Requests: 1");
        summary.Should().Contain("Input:");
        summary.Should().Contain("Output:");
        summary.Should().Contain("cost:");
    }

    [Fact]
    public void GetEntries_ReturnsAllEntries()
    {
        var tracker = new CostTracker();
        tracker.Record("claude-opus-4", 100, 50);
        tracker.Record("claude-sonnet-4", 200, 100);

        tracker.GetEntries().Should().HaveCount(2);
    }
}
