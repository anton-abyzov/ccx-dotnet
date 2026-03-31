namespace Ccx.Compact;

/// <summary>
/// Automatic context compression via API summarization call.
/// </summary>
public sealed class AutoCompact
{
    private readonly int _tokenThreshold;
    private readonly Func<string, CancellationToken, Task<string>> _summarize;

    public AutoCompact(int tokenThreshold, Func<string, CancellationToken, Task<string>> summarize)
    {
        _tokenThreshold = tokenThreshold;
        _summarize = summarize;
    }

    public async Task<string> CompactIfNeeded(string conversationText, CancellationToken ct)
    {
        var tokens = TokenEstimator.Estimate(conversationText);
        if (tokens < _tokenThreshold)
            return conversationText;

        // Summarize the first 2/3 of the conversation
        var splitPoint = (int)(conversationText.Length * 0.66);
        var toSummarize = conversationText[..splitPoint];
        var toKeep = conversationText[splitPoint..];

        var summary = await _summarize(
            $"Summarize this conversation context concisely, preserving key decisions and state:\n\n{toSummarize}",
            ct);

        return $"[Conversation summary]: {summary}\n\n---\n\n{toKeep}";
    }
}
