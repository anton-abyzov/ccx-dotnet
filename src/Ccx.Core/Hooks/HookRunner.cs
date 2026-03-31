using System.Diagnostics;

namespace Ccx.Core.Hooks;

public sealed class HookDef
{
    public required string Event { get; init; }
    public required string Command { get; init; }
}

public sealed class HookResult
{
    public string Output { get; init; } = "";
    public int ExitCode { get; init; }
    public bool Success => ExitCode == 0;
}

/// <summary>
/// Runs pre/post tool hooks from settings.
/// </summary>
public sealed class HookRunner
{
    private readonly List<HookDef> _hooks = [];

    public void Register(HookDef hook) => _hooks.Add(hook);
    public void Register(IEnumerable<HookDef> hooks) => _hooks.AddRange(hooks);

    public async Task<HookResult?> RunAsync(string eventName, Dictionary<string, string>? env = null, CancellationToken ct = default)
    {
        var hook = _hooks.FirstOrDefault(h =>
            h.Event.Equals(eventName, StringComparison.OrdinalIgnoreCase));

        if (hook is null) return null;

        var psi = new ProcessStartInfo("bash")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(hook.Command);

        if (env is not null)
        {
            foreach (var (key, value) in env)
                psi.Environment[key] = value;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        try
        {
            using var process = Process.Start(psi)!;
            var stdout = await process.StandardOutput.ReadToEndAsync(cts.Token);
            var stderr = await process.StandardError.ReadToEndAsync(cts.Token);
            await process.WaitForExitAsync(cts.Token);

            return new HookResult
            {
                Output = string.IsNullOrEmpty(stderr) ? stdout : $"{stdout}\n{stderr}",
                ExitCode = process.ExitCode
            };
        }
        catch (OperationCanceledException)
        {
            return new HookResult { Output = "Hook timed out.", ExitCode = -1 };
        }
    }

    public async Task<List<HookResult>> RunAllAsync(string eventName, Dictionary<string, string>? env = null, CancellationToken ct = default)
    {
        var results = new List<HookResult>();
        var hooks = _hooks.Where(h => h.Event.Equals(eventName, StringComparison.OrdinalIgnoreCase));

        foreach (var hook in hooks)
        {
            var psi = new ProcessStartInfo("bash")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("-c");
            psi.ArgumentList.Add(hook.Command);

            if (env is not null)
                foreach (var (key, value) in env)
                    psi.Environment[key] = value;

            try
            {
                using var process = Process.Start(psi)!;
                var output = await process.StandardOutput.ReadToEndAsync(ct);
                await process.WaitForExitAsync(ct);
                results.Add(new HookResult { Output = output, ExitCode = process.ExitCode });
            }
            catch (Exception ex)
            {
                results.Add(new HookResult { Output = ex.Message, ExitCode = -1 });
            }
        }

        return results;
    }

    public int Count => _hooks.Count;
}
