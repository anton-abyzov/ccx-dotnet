namespace Ccx.Core.Cost;

public sealed class CostEntry
{
    public required string Model { get; init; }
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public decimal Cost { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Tracks API usage and estimated costs per session.
/// </summary>
public sealed class CostTracker
{
    // Pricing per million tokens (approximate)
    private static readonly Dictionary<string, (decimal Input, decimal Output)> Pricing = new(StringComparer.OrdinalIgnoreCase)
    {
        ["claude-opus-4"] = (15m, 75m),
        ["claude-sonnet-4"] = (3m, 15m),
        ["claude-haiku-4"] = (0.25m, 1.25m),
    };

    private readonly List<CostEntry> _entries = [];
    private readonly object _lock = new();

    public void Record(string model, int inputTokens, int outputTokens)
    {
        var pricing = Pricing.GetValueOrDefault(model, (Input: 3m, Output: 15m));
        var cost = (inputTokens * pricing.Input + outputTokens * pricing.Output) / 1_000_000m;

        lock (_lock)
        {
            _entries.Add(new CostEntry
            {
                Model = model,
                InputTokens = inputTokens,
                OutputTokens = outputTokens,
                Cost = cost
            });
        }
    }

    public int TotalInputTokens
    {
        get { lock (_lock) { return _entries.Sum(e => e.InputTokens); } }
    }

    public int TotalOutputTokens
    {
        get { lock (_lock) { return _entries.Sum(e => e.OutputTokens); } }
    }

    public decimal TotalCost
    {
        get { lock (_lock) { return _entries.Sum(e => e.Cost); } }
    }

    public int RequestCount
    {
        get { lock (_lock) { return _entries.Count; } }
    }

    public string GetSummary()
    {
        lock (_lock)
        {
            return $"Requests: {_entries.Count} | " +
                   $"Input: {TotalInputTokens:N0} tokens | " +
                   $"Output: {TotalOutputTokens:N0} tokens | " +
                   $"Est. cost: ${TotalCost:F4}";
        }
    }

    public IReadOnlyList<CostEntry> GetEntries()
    {
        lock (_lock) { return _entries.ToList(); }
    }
}
