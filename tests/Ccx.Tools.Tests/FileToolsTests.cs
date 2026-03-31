using System.Text.Json;
using Ccx.Tools;
using Ccx.Tools.Tools;
using FluentAssertions;

namespace Ccx.Tools.Tests;

public class FileReadToolTests : IDisposable
{
    private readonly FileReadTool _tool = new();
    private readonly string _tempFile;
    private readonly ToolContext _ctx;

    public FileReadToolTests()
    {
        _tempFile = Path.GetTempFileName();
        File.WriteAllText(_tempFile, "line1\nline2\nline3\nline4\nline5\n");
        _ctx = new ToolContext { WorkingDirectory = Path.GetDirectoryName(_tempFile)! };
    }

    [Fact]
    public async Task ExecuteAsync_ReadsFileWithLineNumbers()
    {
        var input = JsonDocument.Parse($$$"""{"file_path": "{{{_tempFile}}}"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Output.Should().Contain("1\tline1");
        result.Output.Should().Contain("2\tline2");
    }

    [Fact]
    public async Task ExecuteAsync_RespectsOffsetAndLimit()
    {
        var input = JsonDocument.Parse($$$"""{"file_path": "{{{_tempFile}}}", "offset": 1, "limit": 2}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Output.Should().Contain("2\tline2");
        result.Output.Should().Contain("3\tline3");
        result.Output.Should().NotContain("1\tline1");
    }

    [Fact]
    public async Task ExecuteAsync_FileNotFound_ReturnsError()
    {
        var input = JsonDocument.Parse("""{"file_path": "/nonexistent/file.txt"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
    }

    public void Dispose() => File.Delete(_tempFile);
}

public class FileWriteToolTests : IDisposable
{
    private readonly FileWriteTool _tool = new();
    private readonly string _tempDir;
    private readonly ToolContext _ctx;

    public FileWriteToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _ctx = new ToolContext { WorkingDirectory = _tempDir };
    }

    [Fact]
    public async Task ExecuteAsync_WritesFile()
    {
        var filePath = Path.Combine(_tempDir, "test.txt");
        var input = JsonDocument.Parse($$$"""{"file_path": "{{{filePath}}}", "content": "hello world"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        File.ReadAllText(filePath).Should().Be("hello world");
    }

    [Fact]
    public async Task ExecuteAsync_CreatesDirectories()
    {
        var filePath = Path.Combine(_tempDir, "sub", "dir", "test.txt");
        var input = JsonDocument.Parse($$$"""{"file_path": "{{{filePath}}}", "content": "nested"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        File.Exists(filePath).Should().BeTrue();
    }

    public void Dispose() => Directory.Delete(_tempDir, true);
}

public class FileEditToolTests : IDisposable
{
    private readonly FileEditTool _tool = new();
    private readonly string _tempFile;
    private readonly ToolContext _ctx;

    public FileEditToolTests()
    {
        _tempFile = Path.GetTempFileName();
        File.WriteAllText(_tempFile, "hello world\ngoodbye world\n");
        _ctx = new ToolContext { WorkingDirectory = Path.GetDirectoryName(_tempFile)! };
    }

    [Fact]
    public async Task ExecuteAsync_ReplacesUniqueString()
    {
        var input = JsonDocument.Parse($$$"""{"file_path": "{{{_tempFile}}}", "old_string": "hello", "new_string": "hi"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        File.ReadAllText(_tempFile).Should().Contain("hi world");
    }

    [Fact]
    public async Task ExecuteAsync_NonUniqueString_ReturnsError()
    {
        var input = JsonDocument.Parse($$$"""{"file_path": "{{{_tempFile}}}", "old_string": "world", "new_string": "earth"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("2 times");
    }

    [Fact]
    public async Task ExecuteAsync_ReplaceAll_ReplacesAll()
    {
        var input = JsonDocument.Parse($$$"""{"file_path": "{{{_tempFile}}}", "old_string": "world", "new_string": "earth", "replace_all": true}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        var content = File.ReadAllText(_tempFile);
        content.Should().Contain("hello earth");
        content.Should().Contain("goodbye earth");
    }

    [Fact]
    public async Task ExecuteAsync_StringNotFound_ReturnsError()
    {
        var input = JsonDocument.Parse($$$"""{"file_path": "{{{_tempFile}}}", "old_string": "notfound", "new_string": "x"}""").RootElement;
        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("not found");
    }

    public void Dispose() => File.Delete(_tempFile);
}
