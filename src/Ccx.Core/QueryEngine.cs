using System.Text;
using System.Text.Json;
using Ccx.Api;
using Ccx.Api.Models;
using Ccx.Tools;

namespace Ccx.Core;

public sealed class QueryEngine
{
    public const int DefaultMaxTurns = 200;
    public const int AgentMaxTurns = 100;

    private readonly IClaudeClient _client;
    private readonly ToolRegistry _tools;
    private readonly Action<string>? _onTextDelta;
    private readonly string? _systemPrompt;
    private readonly int _maxTurns;

    public QueryEngine(IClaudeClient client, ToolRegistry tools, Action<string>? onTextDelta = null, string? systemPrompt = null, int? maxTurns = null)
    {
        _client = client;
        _tools = tools;
        _onTextDelta = onTextDelta;
        _systemPrompt = systemPrompt;
        _maxTurns = maxTurns ?? DefaultMaxTurns;
    }

    public async Task<List<ContentBlock>> RunAsync(List<Message> messages, CancellationToken ct = default)
    {
        var turn = 0;
        while (turn < _maxTurns)
        {
            turn++;
            ct.ThrowIfCancellationRequested();

            var request = new MessageRequest
            {
                MaxTokens = 8192,
                Messages = messages,
                Tools = GetToolDefinitions(),
                System = _systemPrompt
            };

            var (blocks, stopReason) = await StreamResponseAsync(request, ct);

            if (stopReason != "tool_use" || blocks.All(b => b.Type != "tool_use"))
            {
                return blocks;
            }

            messages.Add(Message.Assistant(blocks));

            var toolResults = await ExecuteToolsAsync(
                blocks.Where(b => b.Type == "tool_use"), ct);

            messages.Add(Message.User(toolResults));
        }

        return [ContentBlock.CreateText($"Reached maximum turn limit ({_maxTurns}).")];
    }

    private async Task<(List<ContentBlock> Blocks, string? StopReason)> StreamResponseAsync(
        MessageRequest request, CancellationToken ct)
    {
        var blocks = new List<ContentBlock>();
        string? stopReason = null;

        string? blockType = null;
        string? blockId = null;
        string? blockName = null;
        var textBuf = new StringBuilder();
        var jsonBuf = new StringBuilder();

        await foreach (var evt in _client.StreamMessageAsync(request, ct))
        {
            switch (evt.Type)
            {
                case "content_block_start":
                    blockType = evt.ContentBlock?.Type;
                    blockId = evt.ContentBlock?.Id;
                    blockName = evt.ContentBlock?.Name;
                    textBuf.Clear();
                    jsonBuf.Clear();
                    break;

                case "content_block_delta":
                    switch (evt.Delta?.Type)
                    {
                        case "text_delta":
                            textBuf.Append(evt.Delta.Text);
                            _onTextDelta?.Invoke(evt.Delta.Text ?? "");
                            break;
                        case "input_json_delta":
                            jsonBuf.Append(evt.Delta.PartialJson);
                            break;
                        case "thinking_delta":
                            textBuf.Append(evt.Delta.Thinking);
                            break;
                    }
                    break;

                case "content_block_stop":
                    switch (blockType)
                    {
                        case "text":
                            blocks.Add(ContentBlock.CreateText(textBuf.ToString()));
                            break;
                        case "tool_use":
                            var inputJson = jsonBuf.Length > 0 ? jsonBuf.ToString() : "{}";
                            using (var doc = JsonDocument.Parse(inputJson))
                            {
                                blocks.Add(ContentBlock.CreateToolUse(blockId!, blockName!, doc.RootElement.Clone()));
                            }
                            break;
                        case "thinking":
                            blocks.Add(ContentBlock.CreateThinking(textBuf.ToString()));
                            break;
                    }
                    blockType = null;
                    break;

                case "message_delta":
                    stopReason = evt.Delta?.StopReason;
                    break;
            }
        }

        return (blocks, stopReason);
    }

    private List<ToolDefinition>? GetToolDefinitions()
    {
        var tools = _tools.All;
        if (tools.Count == 0) return null;

        return tools.Select(t => new ToolDefinition
        {
            Name = t.Name,
            Description = t.Description,
            InputSchema = t.InputSchema
        }).ToList();
    }

    private async Task<List<ContentBlock>> ExecuteToolsAsync(
        IEnumerable<ContentBlock> toolUseBlocks, CancellationToken ct)
    {
        var results = new List<ContentBlock>();
        var ctx = new ToolContext { WorkingDirectory = Directory.GetCurrentDirectory() };

        foreach (var block in toolUseBlocks)
        {
            var tool = _tools.FindByName(block.Name!);
            if (tool is null)
            {
                results.Add(ContentBlock.CreateToolResult(block.Id!, $"Tool '{block.Name}' not found", isError: true));
                continue;
            }

            var result = await tool.ExecuteAsync(block.Input ?? default, ctx, ct);
            results.Add(ContentBlock.CreateToolResult(block.Id!, result.Output, result.IsError));
        }

        return results;
    }
}
