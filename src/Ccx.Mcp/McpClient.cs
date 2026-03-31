using System.Text.Json;

namespace Ccx.Mcp;

/// <summary>
/// High-level MCP client wrapping the stdio transport.
/// </summary>
public sealed class McpClient : IAsyncDisposable
{
    private readonly StdioTransport _transport;
    private bool _initialized;

    public McpClient(StdioTransport transport)
    {
        _transport = transport;
    }

    public static McpClient Start(string command, string? args = null)
    {
        var transport = StdioTransport.Start(command, args);
        return new McpClient(transport);
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;

        using var doc = JsonDocument.Parse("""
            {
                "protocolVersion": "2024-11-05",
                "capabilities": {},
                "clientInfo": { "name": "ccx-dotnet", "version": "1.0.0" }
            }
            """);

        var response = await _transport.SendAsync("initialize", doc.RootElement.Clone(), ct);
        if (response.Error is not null)
            throw new InvalidOperationException($"MCP init failed: {response.Error.Message}");

        // Send initialized notification
        await _transport.SendAsync("notifications/initialized", ct: ct);
        _initialized = true;
    }

    public async Task<List<McpToolDef>> ListToolsAsync(CancellationToken ct = default)
    {
        await EnsureInitialized(ct);
        var response = await _transport.SendAsync("tools/list", ct: ct);

        if (response.Result is null) return [];

        if (response.Result.Value.TryGetProperty("tools", out var toolsElem))
        {
            return JsonSerializer.Deserialize(toolsElem.GetRawText(), McpJsonContext.Default.ListMcpToolDef) ?? [];
        }

        return [];
    }

    public async Task<List<McpResource>> ListResourcesAsync(CancellationToken ct = default)
    {
        await EnsureInitialized(ct);
        var response = await _transport.SendAsync("resources/list", ct: ct);

        if (response.Result is null) return [];

        if (response.Result.Value.TryGetProperty("resources", out var elem))
        {
            return JsonSerializer.Deserialize(elem.GetRawText(), McpJsonContext.Default.ListMcpResource) ?? [];
        }

        return [];
    }

    public async Task<JsonElement?> CallToolAsync(string name, JsonElement? arguments = null, CancellationToken ct = default)
    {
        await EnsureInitialized(ct);

        var paramsJson = arguments.HasValue
            ? $$"""{"name": "{{name}}", "arguments": {{arguments.Value.GetRawText()}}}"""
            : $$"""{"name": "{{name}}"}""";

        using var doc = JsonDocument.Parse(paramsJson);
        var response = await _transport.SendAsync("tools/call", doc.RootElement.Clone(), ct);

        if (response.Error is not null)
            throw new InvalidOperationException($"Tool call failed: {response.Error.Message}");

        return response.Result;
    }

    public bool IsConnected => _transport.IsRunning;

    private async Task EnsureInitialized(CancellationToken ct)
    {
        if (!_initialized)
            await InitializeAsync(ct);
    }

    public async ValueTask DisposeAsync()
    {
        await _transport.DisposeAsync();
    }
}
