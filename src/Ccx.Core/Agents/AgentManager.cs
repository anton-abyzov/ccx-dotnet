using System.Collections.Concurrent;
using System.Diagnostics;
using Ccx.Api;
using Ccx.Api.Models;
using Ccx.Tools;

namespace Ccx.Core.Agents;

public sealed class AgentManager
{
    private readonly IClaudeClient _client;
    private readonly ToolRegistry _tools;
    private readonly ConcurrentDictionary<string, AgentResult> _namedResults = new();

    public AgentManager(IClaudeClient client, ToolRegistry tools)
    {
        _client = client;
        _tools = tools;
    }

    public async Task<AgentResult> SpawnAsync(AgentDef def, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var engine = new QueryEngine(_client, _tools);
            var messages = new List<Message>(def.Messages);

            if (!string.IsNullOrEmpty(def.Prompt))
            {
                messages.Insert(0, Message.User(def.Prompt));
            }

            var response = await engine.RunAsync(messages, ct);
            sw.Stop();

            var result = new AgentResult
            {
                AgentName = def.Name,
                Response = response,
                Duration = sw.Elapsed
            };

            _namedResults[def.Name] = result;
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            sw.Stop();
            var errorResult = new AgentResult
            {
                AgentName = def.Name,
                Response = [ContentBlock.CreateText($"Agent error: {ex.Message}")],
                IsError = true,
                ErrorMessage = ex.Message,
                Duration = sw.Elapsed
            };
            _namedResults[def.Name] = errorResult;
            return errorResult;
        }
    }

    public async Task<AgentResult[]> SpawnParallelAsync(IEnumerable<AgentDef> defs, CancellationToken ct)
    {
        var tasks = defs.Select(d => SpawnAsync(d, ct));
        return await Task.WhenAll(tasks);
    }

    public AgentResult? GetResult(string agentName)
    {
        return _namedResults.GetValueOrDefault(agentName);
    }

    public IReadOnlyDictionary<string, AgentResult> AllResults => _namedResults;
}
