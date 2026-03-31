namespace Ccx.Config;

/// <summary>
/// Cascading settings resolution: CLI > session > project > user > defaults.
/// </summary>
public sealed class SettingsCascade
{
    private readonly Dictionary<string, string?> _overrides = new(StringComparer.OrdinalIgnoreCase);
    private readonly CcxSettings _settings;

    public SettingsCascade(CcxSettings settings)
    {
        _settings = settings;
    }

    public void SetOverride(string key, string? value) => _overrides[key] = value;

    public string GetModel(string? cliModel = null)
    {
        if (!string.IsNullOrEmpty(cliModel)) return cliModel;
        if (_overrides.TryGetValue("model", out var m) && !string.IsNullOrEmpty(m)) return m;
        return _settings.Model ?? "claude-sonnet-4-20250514";
    }

    public string? GetProxy()
    {
        if (_overrides.TryGetValue("proxy", out var p)) return p;
        return _settings.Proxy?.Url ?? Environment.GetEnvironmentVariable("HTTPS_PROXY");
    }

    public List<string> GetAllowedTools()
    {
        return _settings.Permissions?.Allow ?? [];
    }

    public List<string> GetDeniedTools()
    {
        return _settings.Permissions?.Deny ?? [];
    }
}
