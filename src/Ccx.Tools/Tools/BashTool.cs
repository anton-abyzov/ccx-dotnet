using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace Ccx.Tools.Tools;

public sealed class BashTool : ITool
{
    private static readonly HashSet<string> DangerousPatterns = new(StringComparer.OrdinalIgnoreCase)
    {
        "rm -rf /", "mkfs", "dd if=", ":(){", "fork bomb",
        "> /dev/sda", "chmod -R 777 /", "shutdown", "reboot",
        "halt", "init 0", "init 6"
    };

    public string Name => "Bash";

    public string Description => "Execute a bash command and return its output.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "command": { "type": "string", "description": "The bash command to execute" },
                "timeout": { "type": "integer", "description": "Timeout in milliseconds (default 120000)" }
            },
            "required": ["command"]
        }
        """).RootElement.Clone();

    private string _workingDirectory = Directory.GetCurrentDirectory();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        var command = input.GetProperty("command").GetString();
        if (string.IsNullOrWhiteSpace(command))
            return ToolResult.Error("Command is required.");

        if (IsDangerous(command))
            return ToolResult.Error($"Blocked: command matched a dangerous pattern.");

        var timeoutMs = input.TryGetProperty("timeout", out var t) ? t.GetInt32() : 120_000;

        var psi = new ProcessStartInfo("bash")
        {
            WorkingDirectory = ctx.WorkingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(command);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeoutMs);

        try
        {
            using var process = Process.Start(psi)!;
            var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
            var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

            await process.WaitForExitAsync(cts.Token);

            var stdout = await stdoutTask;
            var stderr = await stderrTask;

            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(stdout)) sb.Append(stdout);
            if (!string.IsNullOrEmpty(stderr))
            {
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(stderr);
            }

            var output = sb.ToString();
            if (output.Length > 100_000)
                output = output[..100_000] + "\n... (output truncated)";

            return process.ExitCode == 0
                ? ToolResult.Success(output)
                : ToolResult.Error($"Exit code {process.ExitCode}\n{output}");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            return ToolResult.Error($"Command timed out after {timeoutMs}ms.");
        }
    }

    private static bool IsDangerous(string command)
    {
        foreach (var pattern in DangerousPatterns)
        {
            if (command.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string EscapeArg(string arg)
    {
        return "'" + arg.Replace("'", "'\\''") + "'";
    }
}
