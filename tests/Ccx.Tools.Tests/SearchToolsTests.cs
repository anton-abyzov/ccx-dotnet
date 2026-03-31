using System.Text.Json;
using Ccx.Tools;
using Ccx.Tools.Tools;
using FluentAssertions;

namespace Ccx.Tools.Tests;

public class GlobToolTests : IDisposable
{
    private readonly GlobTool _tool = new();
    private readonly string _tempDir;
    private readonly ToolContext _ctx;

    public GlobToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "file1.cs"), "");
        File.WriteAllText(Path.Combine(_tempDir, "file2.cs"), "");
        File.WriteAllText(Path.Combine(_tempDir, "readme.md"), "");
        Directory.CreateDirectory(Path.Combine(_tempDir, "sub"));
        File.WriteAllText(Path.Combine(_tempDir, "sub", "file3.cs"), "");
        _ctx = new ToolContext { WorkingDirectory = _tempDir };
    }

    [Fact]
    public async Task ExecuteAsync_MatchesCsFiles()
    {
        var input = JsonDocument.Parse($$$"""{"pattern": "**/*.cs", "path": "{{{_tempDir}}}"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Output.Should().Contain("file1.cs");
        result.Output.Should().Contain("file2.cs");
        result.Output.Should().Contain("file3.cs");
        result.Output.Should().NotContain("readme.md");
    }

    [Fact]
    public async Task ExecuteAsync_NoMatch_ReturnsMessage()
    {
        var input = JsonDocument.Parse($$$"""{"pattern": "**/*.xyz", "path": "{{{_tempDir}}}"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Output.Should().Contain("No files matched");
    }

    public void Dispose() => Directory.Delete(_tempDir, true);
}

public class GrepToolTests : IDisposable
{
    private readonly GrepTool _tool = new();
    private readonly string _tempDir;
    private readonly ToolContext _ctx;

    public GrepToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "code.cs"), "public class Foo\n{\n    int bar = 42;\n}\n");
        File.WriteAllText(Path.Combine(_tempDir, "data.txt"), "no match here\n");
        _ctx = new ToolContext { WorkingDirectory = _tempDir };
    }

    [Fact]
    public async Task ExecuteAsync_FindsPattern()
    {
        var input = JsonDocument.Parse($$$"""{"pattern": "class Foo", "path": "{{{_tempDir}}}"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Output.Should().Contain("code.cs");
    }

    [Fact]
    public async Task ExecuteAsync_NoMatch_ReturnsMessage()
    {
        var input = JsonDocument.Parse($$$"""{"pattern": "nonexistent_pattern_xyz", "path": "{{{_tempDir}}}"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Output.Should().Contain("No matches");
    }

    public void Dispose() => Directory.Delete(_tempDir, true);
}
