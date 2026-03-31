using Ccx.Core.Hooks;
using FluentAssertions;

namespace Ccx.Core.Tests;

public class HookRunnerTests
{
    [Fact]
    public async Task RunAsync_MatchingEvent_RunsCommand()
    {
        var runner = new HookRunner();
        runner.Register(new HookDef { Event = "pre-prompt", Command = "echo hook-ran" });

        var result = await runner.RunAsync("pre-prompt");

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Output.Trim().Should().Be("hook-ran");
    }

    [Fact]
    public async Task RunAsync_NoMatchingEvent_ReturnsNull()
    {
        var runner = new HookRunner();
        runner.Register(new HookDef { Event = "pre-prompt", Command = "echo test" });

        var result = await runner.RunAsync("post-response");
        result.Should().BeNull();
    }

    [Fact]
    public async Task RunAsync_FailingCommand_ReturnsNonZeroExit()
    {
        var runner = new HookRunner();
        runner.Register(new HookDef { Event = "test", Command = "exit 1" });

        var result = await runner.RunAsync("test");

        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
    }

    [Fact]
    public void Count_TracksRegisteredHooks()
    {
        var runner = new HookRunner();
        runner.Register(new HookDef { Event = "a", Command = "echo a" });
        runner.Register(new HookDef { Event = "b", Command = "echo b" });

        runner.Count.Should().Be(2);
    }
}
