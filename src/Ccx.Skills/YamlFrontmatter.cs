namespace Ccx.Skills;

/// <summary>
/// Simple YAML frontmatter parser (AOT-compatible, no YAML library needed).
/// </summary>
public static class YamlFrontmatter
{
    public static (Dictionary<string, string> Metadata, string Body) Parse(string content)
    {
        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!content.StartsWith("---"))
            return (metadata, content);

        var endIdx = content.IndexOf("---", 3, StringComparison.Ordinal);
        if (endIdx < 0)
            return (metadata, content);

        var frontmatter = content[3..endIdx];
        var body = content[(endIdx + 3)..].TrimStart('\r', '\n');

        foreach (var line in frontmatter.Split('\n'))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#')) continue;

            var colonIdx = trimmed.IndexOf(':');
            if (colonIdx <= 0) continue;

            var key = trimmed[..colonIdx].Trim();
            var value = trimmed[(colonIdx + 1)..].Trim().Trim('"', '\'');
            metadata[key] = value;
        }

        return (metadata, body);
    }
}
