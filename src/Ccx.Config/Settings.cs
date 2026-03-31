using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ccx.Config;

public sealed class CcxSettings
{
    [JsonPropertyName("model")]
    public string? Model { get; set; }

    [JsonPropertyName("permissions")]
    public PermissionSettings? Permissions { get; set; }

    [JsonPropertyName("hooks")]
    public Dictionary<string, HookDefinition>? Hooks { get; set; }

    [JsonPropertyName("proxy")]
    public ProxySettings? Proxy { get; set; }

    public static CcxSettings Load()
    {
        var paths = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "settings.json"),
            Path.Combine(Directory.GetCurrentDirectory(), ".claude", "settings.json")
        };

        var merged = new CcxSettings();

        foreach (var path in paths)
        {
            if (!File.Exists(path)) continue;
            try
            {
                var json = File.ReadAllText(path);
                var settings = JsonSerializer.Deserialize(json, CcxSettingsContext.Default.CcxSettings);
                if (settings is not null)
                    Merge(merged, settings);
            }
            catch { /* skip invalid files */ }
        }

        return merged;
    }

    private static void Merge(CcxSettings target, CcxSettings source)
    {
        target.Model ??= source.Model;
        target.Permissions ??= source.Permissions;
        target.Proxy ??= source.Proxy;

        if (source.Hooks is not null)
        {
            target.Hooks ??= new();
            foreach (var (key, value) in source.Hooks)
                target.Hooks.TryAdd(key, value);
        }
    }
}

public sealed class PermissionSettings
{
    [JsonPropertyName("allow")]
    public List<string>? Allow { get; set; }

    [JsonPropertyName("deny")]
    public List<string>? Deny { get; set; }
}

public sealed class HookDefinition
{
    [JsonPropertyName("command")]
    public string? Command { get; set; }

    [JsonPropertyName("event")]
    public string? Event { get; set; }
}

public sealed class ProxySettings
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("no_proxy")]
    public List<string>? NoProxy { get; set; }
}

[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(CcxSettings))]
[JsonSerializable(typeof(PermissionSettings))]
[JsonSerializable(typeof(HookDefinition))]
[JsonSerializable(typeof(ProxySettings))]
[JsonSerializable(typeof(Dictionary<string, HookDefinition>))]
public partial class CcxSettingsContext : JsonSerializerContext;
