using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ccx.Tools.Tools;

public sealed class WebFetchTool : ITool
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public string Name => "WebFetch";

    public string Description => "Fetch a URL and return its content as text.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "url": { "type": "string", "description": "The URL to fetch" }
            },
            "required": ["url"]
        }
        """).RootElement.Clone();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        var url = input.GetProperty("url").GetString();
        if (string.IsNullOrWhiteSpace(url))
            return ToolResult.Error("url is required.");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != "http" && uri.Scheme != "https"))
            return ToolResult.Error("Invalid URL. Must be http or https.");

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, uri);
            request.Headers.Add("User-Agent", "ccx-dotnet/1.0");
            request.Headers.Add("Accept", "text/html,application/xhtml+xml,text/plain,*/*");

            using var response = await Http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(ct);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

            if (contentType.Contains("html", StringComparison.OrdinalIgnoreCase))
                content = HtmlToText(content);

            if (content.Length > 100_000)
                content = content[..100_000] + "\n... (truncated)";

            return ToolResult.Success(content);
        }
        catch (HttpRequestException ex)
        {
            return ToolResult.Error($"HTTP error: {ex.Message}");
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return ToolResult.Error("Request timed out.");
        }
    }

    private static string HtmlToText(string html)
    {
        // Remove script and style blocks
        html = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);

        // Convert block elements to newlines
        html = Regex.Replace(html, @"<(br|p|div|h[1-6]|li|tr)[^>]*>", "\n", RegexOptions.IgnoreCase);

        // Strip remaining tags
        html = Regex.Replace(html, @"<[^>]+>", "");

        // Decode common entities
        html = html.Replace("&amp;", "&").Replace("&lt;", "<").Replace("&gt;", ">")
                    .Replace("&quot;", "\"").Replace("&#39;", "'").Replace("&nbsp;", " ");

        // Collapse whitespace
        html = Regex.Replace(html, @"[ \t]+", " ");
        html = Regex.Replace(html, @"\n{3,}", "\n\n");

        return html.Trim();
    }
}
