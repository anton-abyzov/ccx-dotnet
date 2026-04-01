using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ccx.Api.Serialization;

namespace Ccx.Api;

public static class OAuthLogin
{
    private const string ClientId = "9d1c250a-e61b-44d9-88ed-5944d1962f5e";
    private const string AuthorizeUrl = "https://claude.ai/oauth/authorize";
    private const string TokenUrl = "https://platform.claude.com/v1/oauth/token";
    private const string Scope = "user:profile user:inference";
    private const string KeychainService = "Claude Code-credentials";

    public static async Task RunAsync()
    {
        // 1. Generate PKCE pair
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        // 2. Find a free port and start HTTP listener
        var port = FindFreePort();
        var redirectUri = $"http://localhost:{port}/oauth/callback";

        using var listener = new HttpListener();
        listener.Prefixes.Add($"http://localhost:{port}/");
        listener.Start();

        // 3. Open browser
        var authUrl = $"{AuthorizeUrl}?response_type=code&client_id={ClientId}" +
                      $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                      $"&scope={Uri.EscapeDataString(Scope)}" +
                      $"&code_challenge={codeChallenge}" +
                      $"&code_challenge_method=S256";

        OpenBrowser(authUrl);
        Console.WriteLine("Waiting for OAuth callback...");

        // 4. Wait for callback
        var context = await listener.GetContextAsync();
        var code = context.Request.QueryString["code"];

        // Send response to browser
        var responseBytes = Encoding.UTF8.GetBytes(
            "<html><body><h2>Login successful! You can close this tab.</h2></body></html>");
        context.Response.ContentType = "text/html";
        context.Response.ContentLength64 = responseBytes.Length;
        await context.Response.OutputStream.WriteAsync(responseBytes);
        context.Response.Close();
        listener.Stop();

        if (string.IsNullOrEmpty(code))
        {
            Console.Error.WriteLine("Error: No authorization code received.");
            return;
        }

        // 5. Exchange code for token
        using var http = new HttpClient();
        var tokenParams = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = ClientId,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier
        };

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent(tokenParams)
        };

        var tokenResponse = await http.SendAsync(tokenRequest);
        var tokenJson = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            Console.Error.WriteLine($"Error: Token exchange failed ({tokenResponse.StatusCode}): {tokenJson}");
            return;
        }

        var tokenResult = JsonSerializer.Deserialize(tokenJson, OAuthJsonContext.Default.OAuthTokenResponse);
        if (tokenResult is null || string.IsNullOrEmpty(tokenResult.AccessToken))
        {
            Console.Error.WriteLine("Error: Invalid token response.");
            return;
        }

        // 6. Save credentials
        var credentialsJson = JsonSerializer.Serialize(
            new OAuthCredentials
            {
                ClaudeAiOAuth = new OAuthTokenData
                {
                    AccessToken = tokenResult.AccessToken,
                    RefreshToken = tokenResult.RefreshToken,
                    ExpiresIn = tokenResult.ExpiresIn,
                    TokenType = tokenResult.TokenType
                }
            },
            OAuthJsonContext.Default.OAuthCredentials);

        SaveToKeychain(credentialsJson);
        SaveToCredentialsFile(credentialsJson);

        Console.WriteLine("Login successful!");
    }

    private static string GenerateCodeVerifier()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Base64UrlEncode(bytes);
    }

    private static string GenerateCodeChallenge(string verifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(verifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static int FindFreePort()
    {
        using var tcp = new TcpListener(IPAddress.Loopback, 0);
        tcp.Start();
        var port = ((IPEndPoint)tcp.LocalEndpoint).Port;
        tcp.Stop();
        return port;
    }

    private static void OpenBrowser(string url)
    {
        if (OperatingSystem.IsMacOS())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "open",
                Arguments = url,
                UseShellExecute = false
            });
        }
        else if (OperatingSystem.IsLinux())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = url,
                UseShellExecute = false
            });
        }
        else if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }

    private static void SaveToKeychain(string json)
    {
        if (!OperatingSystem.IsMacOS()) return;

        try
        {
            var escaped = json.Replace("\"", "\\\"");
            var psi = new ProcessStartInfo
            {
                FileName = "security",
                ArgumentList =
                {
                    "add-generic-password",
                    "-a", "default",
                    "-s", KeychainService,
                    "-w", json,
                    "-U"
                },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit(5000);
        }
        catch
        {
            // Non-fatal: credentials file is the fallback
        }
    }

    private static void SaveToCredentialsFile(string json)
    {
        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var claudeDir = Path.Combine(home, ".claude");
            Directory.CreateDirectory(claudeDir);
            var credPath = Path.Combine(claudeDir, ".credentials.json");
            File.WriteAllText(credPath, json);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Warning: Could not save credentials file: {ex.Message}");
        }
    }
}

// --- AOT-compatible JSON types for OAuth ---

public sealed class OAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }
}

public sealed class OAuthCredentials
{
    [JsonPropertyName("claudeAiOAuth")]
    public OAuthTokenData? ClaudeAiOAuth { get; set; }
}

public sealed class OAuthTokenData
{
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expiresIn")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("tokenType")]
    public string? TokenType { get; set; }
}

[JsonSerializable(typeof(OAuthTokenResponse))]
[JsonSerializable(typeof(OAuthCredentials))]
[JsonSerializable(typeof(OAuthTokenData))]
internal partial class OAuthJsonContext : JsonSerializerContext;
