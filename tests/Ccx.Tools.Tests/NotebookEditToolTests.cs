using System.Text.Json;
using System.Text.Json.Nodes;
using Ccx.Tools;
using Ccx.Tools.Tools;
using FluentAssertions;

namespace Ccx.Tools.Tests;

public class NotebookEditToolTests : IDisposable
{
    private readonly NotebookEditTool _tool = new();
    private readonly string _tempDir;
    private readonly ToolContext _ctx;

    public NotebookEditToolTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _ctx = new ToolContext { WorkingDirectory = _tempDir };
    }

    private string CreateNotebook(string content = """
        {
            "nbformat": 4,
            "nbformat_minor": 2,
            "metadata": {},
            "cells": [
                {
                    "cell_type": "code",
                    "metadata": {},
                    "source": ["print('hello')\n"],
                    "execution_count": null,
                    "outputs": []
                }
            ]
        }
        """)
    {
        var path = Path.Combine(_tempDir, "test.ipynb");
        File.WriteAllText(path, content);
        return path;
    }

    [Fact]
    public async Task InsertCell_CreatesNewCell()
    {
        var path = CreateNotebook();
        var input = JsonDocument.Parse($$$"""
            {
                "file_path": "{{{path}}}",
                "operation": "insert",
                "cell_index": 1,
                "cell_type": "markdown",
                "source": "# Header"
            }
            """).RootElement;

        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Output.Should().Contain("Inserted markdown cell");
        result.Output.Should().Contain("2 cells");

        var nb = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        nb["cells"]!.AsArray().Count.Should().Be(2);
        nb["cells"]![1]!["cell_type"]!.GetValue<string>().Should().Be("markdown");
    }

    [Fact]
    public async Task ReplaceCell_UpdatesExistingCell()
    {
        var path = CreateNotebook();
        var input = JsonDocument.Parse($$$"""
            {
                "file_path": "{{{path}}}",
                "operation": "replace",
                "cell_index": 0,
                "source": "print('world')"
            }
            """).RootElement;

        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Output.Should().Contain("Replaced cell at index 0");

        var nb = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        var source = nb["cells"]![0]!["source"]!.AsArray();
        source[0]!.GetValue<string>().Should().Contain("world");
    }

    [Fact]
    public async Task DeleteCell_RemovesCell()
    {
        var path = CreateNotebook();
        var input = JsonDocument.Parse($$$"""
            {
                "file_path": "{{{path}}}",
                "operation": "delete",
                "cell_index": 0
            }
            """).RootElement;

        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Output.Should().Contain("Deleted cell at index 0");
        result.Output.Should().Contain("0 cells");
    }

    [Fact]
    public async Task InsertCell_NewFile_CreatesNotebook()
    {
        var path = Path.Combine(_tempDir, "new.ipynb");
        var input = JsonDocument.Parse($$$"""
            {
                "file_path": "{{{path}}}",
                "operation": "insert",
                "cell_index": 0,
                "cell_type": "code",
                "source": "x = 1"
            }
            """).RootElement;

        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeFalse();
        File.Exists(path).Should().BeTrue();

        var nb = JsonNode.Parse(await File.ReadAllTextAsync(path))!;
        nb["nbformat"]!.GetValue<int>().Should().Be(4);
        nb["cells"]!.AsArray().Count.Should().Be(1);
    }

    [Fact]
    public async Task DeleteCell_OutOfRange_ReturnsError()
    {
        var path = CreateNotebook();
        var input = JsonDocument.Parse($$$"""
            {
                "file_path": "{{{path}}}",
                "operation": "delete",
                "cell_index": 99
            }
            """).RootElement;

        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("out of range");
    }

    [Fact]
    public async Task MissingFile_NonInsert_ReturnsError()
    {
        var input = JsonDocument.Parse("""
            {
                "file_path": "/nonexistent/test.ipynb",
                "operation": "delete",
                "cell_index": 0
            }
            """).RootElement;

        var result = await _tool.ExecuteAsync(input, _ctx, CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.Output.Should().Contain("File not found");
    }

    [Fact]
    public void Name_IsNotebookEdit()
    {
        _tool.Name.Should().Be("NotebookEdit");
    }

    public void Dispose() => Directory.Delete(_tempDir, true);
}
