namespace Ccx.Config;

public enum MemoryType
{
    User,
    Feedback,
    Project,
    Reference
}

public sealed class MemoryEntry
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required MemoryType Type { get; init; }
    public required string Content { get; init; }
    public required string FilePath { get; init; }
}

/// <summary>
/// File-based memory storage in ~/.claude/projects/{project}/memory/.
/// </summary>
public sealed class MemoryStore
{
    private readonly string _memoryDir;
    private readonly string _indexPath;

    public MemoryStore(string projectPath)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var projectKey = projectPath.Replace("/", "-").Replace("\\", "-").TrimStart('-');
        _memoryDir = Path.Combine(home, ".claude", "projects", projectKey, "memory");
        _indexPath = Path.Combine(_memoryDir, "MEMORY.md");
    }

    public MemoryStore(string memoryDir, bool directPath)
    {
        _memoryDir = memoryDir;
        _indexPath = Path.Combine(_memoryDir, "MEMORY.md");
    }

    public void Save(MemoryEntry entry)
    {
        Directory.CreateDirectory(_memoryDir);

        var fileName = SanitizeFileName(entry.Name) + ".md";
        var filePath = Path.Combine(_memoryDir, fileName);

        var frontmatter = $"""
            ---
            name: {entry.Name}
            description: {entry.Description}
            type: {entry.Type.ToString().ToLowerInvariant()}
            ---

            {entry.Content}
            """;

        File.WriteAllText(filePath, frontmatter);
        UpdateIndex(entry.Name, fileName, entry.Description);
    }

    public MemoryEntry? Load(string name)
    {
        var fileName = SanitizeFileName(name) + ".md";
        var filePath = Path.Combine(_memoryDir, fileName);

        if (!File.Exists(filePath)) return null;

        var text = File.ReadAllText(filePath);
        return ParseEntry(filePath, text);
    }

    public List<MemoryEntry> LoadAll()
    {
        if (!Directory.Exists(_memoryDir)) return [];

        return Directory.GetFiles(_memoryDir, "*.md")
            .Where(f => !f.EndsWith("MEMORY.md"))
            .Select(f => ParseEntry(f, File.ReadAllText(f)))
            .Where(e => e is not null)
            .Cast<MemoryEntry>()
            .ToList();
    }

    public void Delete(string name)
    {
        var fileName = SanitizeFileName(name) + ".md";
        var filePath = Path.Combine(_memoryDir, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);
    }

    public string? LoadIndex()
    {
        return File.Exists(_indexPath) ? File.ReadAllText(_indexPath) : null;
    }

    private void UpdateIndex(string title, string fileName, string description)
    {
        var line = $"- [{title}]({fileName}) — {description}";
        var existing = File.Exists(_indexPath) ? File.ReadAllText(_indexPath) : "# Memory Index\n";

        if (!existing.Contains(fileName))
        {
            File.WriteAllText(_indexPath, existing.TrimEnd() + "\n" + line + "\n");
        }
    }

    private static MemoryEntry? ParseEntry(string filePath, string text)
    {
        if (!text.StartsWith("---")) return null;

        var endIdx = text.IndexOf("---", 3, StringComparison.Ordinal);
        if (endIdx < 0) return null;

        var frontmatter = text[3..endIdx];
        var content = text[(endIdx + 3)..].Trim();

        var name = ExtractField(frontmatter, "name");
        var description = ExtractField(frontmatter, "description");
        var typeStr = ExtractField(frontmatter, "type");

        if (name is null || description is null) return null;

        var type = typeStr?.ToLowerInvariant() switch
        {
            "user" => MemoryType.User,
            "feedback" => MemoryType.Feedback,
            "project" => MemoryType.Project,
            "reference" => MemoryType.Reference,
            _ => MemoryType.Project
        };

        return new MemoryEntry
        {
            Name = name,
            Description = description,
            Type = type,
            Content = content,
            FilePath = filePath
        };
    }

    private static string? ExtractField(string frontmatter, string field)
    {
        foreach (var line in frontmatter.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith($"{field}:", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed[($"{field}:".Length)..].Trim();
            }
        }
        return null;
    }

    private static string SanitizeFileName(string name)
    {
        return string.Join("_", name.Split(Path.GetInvalidFileNameChars()))
            .Replace(' ', '_')
            .ToLowerInvariant();
    }
}
