namespace Ccx.Skills;

public sealed class SkillDef
{
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string? Trigger { get; init; }
    public required string Body { get; init; }
    public required string FilePath { get; init; }
    public Dictionary<string, string> Metadata { get; init; } = new();
}

/// <summary>
/// Loads skill definitions from markdown files.
/// </summary>
public sealed class SkillLoader
{
    private readonly List<string> _searchPaths = [];

    public SkillLoader()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        _searchPaths.Add(Path.Combine(home, ".claude", "skills"));
        _searchPaths.Add(Path.Combine(Directory.GetCurrentDirectory(), ".claude", "skills"));
    }

    public SkillLoader(IEnumerable<string> paths)
    {
        _searchPaths.AddRange(paths);
    }

    public List<SkillDef> LoadAll()
    {
        var skills = new List<SkillDef>();

        foreach (var dir in _searchPaths)
        {
            if (!Directory.Exists(dir)) continue;

            foreach (var file in Directory.EnumerateFiles(dir, "*.md"))
            {
                var skill = LoadFile(file);
                if (skill is not null)
                    skills.Add(skill);
            }
        }

        return skills;
    }

    public SkillDef? FindByName(string name)
    {
        return LoadAll().FirstOrDefault(s =>
            s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    private static SkillDef? LoadFile(string filePath)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var (metadata, body) = YamlFrontmatter.Parse(content);

            var name = metadata.GetValueOrDefault("name")
                ?? Path.GetFileNameWithoutExtension(filePath);

            return new SkillDef
            {
                Name = name,
                Description = metadata.GetValueOrDefault("description"),
                Trigger = metadata.GetValueOrDefault("trigger"),
                Body = body,
                FilePath = filePath,
                Metadata = metadata
            };
        }
        catch
        {
            return null;
        }
    }
}
