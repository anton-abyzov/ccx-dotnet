using System.Diagnostics;
using System.Text.Json;

namespace Ccx.Mcp;

/// <summary>
/// Manages a JSON-RPC connection over stdio to an MCP server process.
/// </summary>
public sealed class StdioTransport : IAsyncDisposable
{
    private readonly Process _process;
    private readonly StreamWriter _writer;
    private readonly StreamReader _reader;
    private int _nextId;

    private StdioTransport(Process process)
    {
        _process = process;
        _writer = process.StandardInput;
        _reader = process.StandardOutput;
    }

    public static StdioTransport Start(string command, string? args = null, string? workingDir = null)
    {
        var psi = new ProcessStartInfo(command)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (!string.IsNullOrEmpty(args))
            psi.Arguments = args;
        if (!string.IsNullOrEmpty(workingDir))
            psi.WorkingDirectory = workingDir;

        var process = Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start MCP server: {command}");

        return new StdioTransport(process);
    }

    public async Task<JsonRpcResponse> SendAsync(string method, JsonElement? @params = null, CancellationToken ct = default)
    {
        var request = new JsonRpcRequest
        {
            Id = Interlocked.Increment(ref _nextId),
            Method = method,
            Params = @params
        };

        var json = JsonSerializer.Serialize(request, McpJsonContext.Default.JsonRpcRequest);
        await _writer.WriteLineAsync(json.AsMemory(), ct);
        await _writer.FlushAsync(ct);

        // Read response line
        var responseLine = await _reader.ReadLineAsync(ct);
        if (responseLine is null)
            throw new InvalidOperationException("MCP server closed connection.");

        return JsonSerializer.Deserialize(responseLine, McpJsonContext.Default.JsonRpcResponse)
            ?? throw new InvalidOperationException("Invalid JSON-RPC response.");
    }

    public bool IsRunning => !_process.HasExited;

    public async ValueTask DisposeAsync()
    {
        try
        {
            _writer.Close();
            if (!_process.HasExited)
            {
                _process.Kill();
                await _process.WaitForExitAsync();
            }
        }
        catch { /* best effort cleanup */ }
        _process.Dispose();
    }
}
