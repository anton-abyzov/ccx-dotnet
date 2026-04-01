using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using Ccx.Api.Models;
using Ccx.Api.Serialization;

namespace Ccx.Api;

public sealed class ClaudeClient : IClaudeClient
{
    private const string ApiUrl = "https://api.anthropic.com/v1/messages";
    private const string ApiVersion = "2023-06-01";

    private readonly HttpClient _http;
    private readonly string _token;
    private readonly bool _isOAuth;
    private readonly string _model;

    public ClaudeClient(HttpClient http, string token, string model, bool isOAuth = false)
    {
        _http = http;
        _token = token;
        _model = model;
        _isOAuth = isOAuth;
    }

    public async IAsyncEnumerable<StreamEvent> StreamMessageAsync(
        MessageRequest request,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        request.Stream = true;
        request.Model ??= _model;

        var json = JsonSerializer.Serialize(request, CcxJsonContext.Default.MessageRequest);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, ApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        if (_isOAuth)
        {
            httpRequest.Headers.Add("Authorization", $"Bearer {_token}");
            httpRequest.Headers.Add("anthropic-beta", "oauth-2025-04-20");
        }
        else
        {
            httpRequest.Headers.Add("x-api-key", _token);
        }
        httpRequest.Headers.Add("anthropic-version", ApiVersion);

        using var response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        string? currentEvent = null;
        string? line;

        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {

            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                currentEvent = line[7..];
            }
            else if (line.StartsWith("data: ", StringComparison.Ordinal) && currentEvent is not null)
            {
                var data = line[6..];
                var evt = JsonSerializer.Deserialize(data, CcxJsonContext.Default.StreamEvent);
                if (evt is not null)
                {
                    yield return evt;
                }
                currentEvent = null;
            }
        }
    }
}
