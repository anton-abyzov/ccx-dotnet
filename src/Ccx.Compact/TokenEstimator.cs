namespace Ccx.Compact;

/// <summary>
/// Character-based token estimation with model-specific ratios.
/// </summary>
public static class TokenEstimator
{
    private static readonly Dictionary<string, double> ModelRatios = new(StringComparer.OrdinalIgnoreCase)
    {
        ["claude-opus-4"] = 3.5,
        ["claude-sonnet-4"] = 3.5,
        ["claude-haiku-4"] = 3.5,
        ["default"] = 3.5 // ~3.5 chars per token for Claude models
    };

    public static int Estimate(string text, string? model = null)
    {
        if (string.IsNullOrEmpty(text)) return 0;

        var ratio = ModelRatios.GetValueOrDefault(model ?? "", ModelRatios["default"]);
        return (int)Math.Ceiling(text.Length / ratio);
    }

    public static int EstimateMessages(IEnumerable<string> messages, string? model = null)
    {
        var total = 0;
        foreach (var msg in messages)
            total += Estimate(msg, model) + 4; // +4 for message overhead
        return total;
    }
}
