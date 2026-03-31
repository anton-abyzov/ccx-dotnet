namespace Ccx.Api;

public static class SseParser
{
    public static IEnumerable<(string EventType, string Data)> Parse(IEnumerable<string> lines)
    {
        string? currentEvent = null;

        foreach (var line in lines)
        {
            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                currentEvent = line[7..];
            }
            else if (line.StartsWith("data: ", StringComparison.Ordinal) && currentEvent is not null)
            {
                yield return (currentEvent, line[6..]);
                currentEvent = null;
            }
        }
    }
}
