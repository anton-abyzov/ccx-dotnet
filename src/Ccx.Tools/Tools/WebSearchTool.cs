using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;

namespace Ccx.Tools.Tools;

public sealed class WebSearchTool : ITool
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public string Name => "WebSearch";

    public string Description => "Search the web for information. Returns search results as text.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "query": { "type": "string", "description": "The search query" }
            },
            "required": ["query"]
        }
        """).RootElement.Clone();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        var query = input.TryGetProperty("query", out var q) ? q.GetString() : null;
        if (string.IsNullOrWhiteSpace(query))
            return ToolResult.Error("query is required.");

        try
        {
            var encoded = HttpUtility.UrlEncode(query);
            var url = $"https://html.duckduckgo.com/html/?q={encoded}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "ccx-dotnet/1.0");

            using var response = await Http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var html = await response.Content.ReadAsStringAsync(ct);
            var results = ExtractSearchResults(html);

            if (string.IsNullOrWhiteSpace(results))
                return ToolResult.Success($"No results found for: {query}");

            if (results.Length > 50_000)
                results = results[..50_000] + "\n... (truncated)";

            return ToolResult.Success(results);
        }
        catch (HttpRequestException ex)
        {
            return ToolResult.Error($"Search error: {ex.Message}");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return ToolResult.Error("Search request timed out.");
        }
    }

    private static string ExtractSearchResults(string html)
    {
        // Extract result snippets from DuckDuckGo HTML
        var results = new List<string>();

        // Match result links and snippets
        var linkMatches = Regex.Matches(html, @"<a[^>]+class=""result__a""[^>]*>(.*?)</a>", RegexOptions.Singleline);
        var snippetMatches = Regex.Matches(html, @"<a[^>]+class=""result__snippet""[^>]*>(.*?)</a>", RegexOptions.Singleline);

        for (var i = 0; i < linkMatches.Count && i < 10; i++)
        {
            var title = StripHtml(linkMatches[i].Groups[1].Value).Trim();
            var snippet = i < snippetMatches.Count
                ? StripHtml(snippetMatches[i].Groups[1].Value).Trim()
                : "";

            if (!string.IsNullOrEmpty(title))
            {
                results.Add($"{i + 1}. {title}");
                if (!string.IsNullOrEmpty(snippet))
                    results.Add($"   {snippet}");
                results.Add("");
            }
        }

        return string.Join("\n", results).Trim();
    }

    private static string StripHtml(string html)
    {
        html = Regex.Replace(html, @"<[^>]+>", "");
        html = html.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
                    .Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&nbsp;", " ");
        return html.Trim();
    }
}
