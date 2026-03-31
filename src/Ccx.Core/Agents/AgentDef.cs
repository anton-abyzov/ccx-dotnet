using Ccx.Api.Models;
using Ccx.Tools;

namespace Ccx.Core.Agents;

public sealed class AgentDef
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required string Prompt { get; init; }
    public List<Message> Messages { get; init; } = [];
    public List<string>? AllowedTools { get; init; }
}

public sealed class AgentResult
{
    public required string AgentName { get; init; }
    public required List<ContentBlock> Response { get; init; }
    public bool IsError { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan Duration { get; init; }

    public string GetText()
    {
        return string.Join("", Response
            .Where(b => b.Type == "text" && b.Text is not null)
            .Select(b => b.Text));
    }
}
