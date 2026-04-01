using System.Diagnostics;
using System.Text.Json;
using Ccx.Api.Serialization;

namespace Ccx.Api;

public static class OAuthResolver
{
    public record AuthResult(string Token, bool IsOAuth, string DisplayName);

    public static AuthResult Resolve(string? explicitKey = null)
    {
        // 1. Explicit key (--api-key flag)
        if (!string.IsNullOrEmpty(explicitKey))
            return new(explicitKey, false, "API Key");

        // 2. Environment variable
        var envKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!string.IsNullOrEmpty(envKey))
            return new(envKey, false, "API Key (env)");

        // 3. macOS Keychain (Claude Code OAuth token)
        if (OperatingSystem.IsMacOS())
        {
            var keychainResult = ReadKeychain();
            if (keychainResult is not null)
                return keychainResult;
        }

        // 4. Credentials file fallback
        var fileResult = ReadCredentialsFile();
        if (fileResult is not null)
            return fileResult;

        throw new InvalidOperationException(
            "No auth found. Set ANTHROPIC_API_KEY, pass --api-key, or log in with: claude auth");
    }

    private static AuthResult? ReadKeychain()
    {
        try
        {
            var user = Environment.UserName;
            var psi = new ProcessStartInfo
            {
                FileName = "security",
                ArgumentList = { "find-generic-password", "-a", user, "-w", "-s", "Claude Code-credentials" },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc is null) return null;

            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit(5000);

            if (proc.ExitCode != 0 || string.IsNullOrEmpty(output))
                return null;

            return ParseOAuthToken(output, "Claude Code (Keychain)");
        }
        catch
        {
            return null;
        }
    }

    private static AuthResult? ReadCredentialsFile()
    {
        try
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var credPath = Path.Combine(home, ".claude", ".credentials.json");

            if (!File.Exists(credPath))
                return null;

            var json = File.ReadAllText(credPath);
            return ParseOAuthToken(json, "Claude Code (credentials file)");
        }
        catch
        {
            return null;
        }
    }

    internal static AuthResult? ParseOAuthToken(string json, string source)
    {
        try
        {
            var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Try nested: { "claudeAiOAuth": { "accessToken": "..." } }
            if (root.TryGetProperty("claudeAiOAuth", out var oauthNode)
                && oauthNode.TryGetProperty("accessToken", out var nestedToken))
            {
                var token = nestedToken.GetString();
                if (!string.IsNullOrEmpty(token))
                    return new(token, true, source);
            }

            // Try flat: { "accessToken": "..." }
            if (root.TryGetProperty("accessToken", out var flatToken))
            {
                var token = flatToken.GetString();
                if (!string.IsNullOrEmpty(token))
                    return new(token, true, source);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
