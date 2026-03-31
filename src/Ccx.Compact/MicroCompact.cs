using System.Text.Json;

namespace Ccx.Compact;

/// <summary>
/// Strips large tool results between turns to reduce context size.
/// </summary>
public static class MicroCompact
{
    private const int MaxToolResultLength = 10_000;
    private const string TruncatedMarker = "[truncated — original was {0} chars]";

    public static string CompactToolResult(string result, int maxLength = MaxToolResultLength)
    {
        if (result.Length <= maxLength) return result;

        var half = maxLength / 2;
        var truncated = string.Concat(
            result.AsSpan(0, half),
            $"\n\n... {string.Format(TruncatedMarker, result.Length)} ...\n\n",
            result.AsSpan(result.Length - half));

        return truncated;
    }

    public static List<JsonElement> CompactMessages(List<JsonElement> messages, int maxTokens)
    {
        var result = new List<JsonElement>();
        var currentTokens = 0;

        // Always keep first and last messages
        for (var i = 0; i < messages.Count; i++)
        {
            var json = messages[i].GetRawText();
            var tokens = TokenEstimator.Estimate(json);

            if (i == 0 || i == messages.Count - 1 || currentTokens + tokens < maxTokens)
            {
                result.Add(messages[i]);
                currentTokens += tokens;
            }
            else
            {
                // Replace middle messages with summary placeholder
                using var doc = JsonDocument.Parse(
                    $$"""{"role": "user", "content": [{"type": "text", "text": "[earlier context compacted]"}]}""");
                result.Add(doc.RootElement.Clone());
                // Skip remaining middle messages up to budget
                while (i < messages.Count - 1 && currentTokens + TokenEstimator.Estimate(messages[i + 1].GetRawText()) >= maxTokens)
                    i++;
            }
        }

        return result;
    }
}
