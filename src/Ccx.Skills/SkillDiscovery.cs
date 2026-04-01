namespace Ccx.Skills;

/// <summary>
/// Discovers skills from all known locations: user skills, project skills, and SpecWeave plugins.
/// </summary>
public static class SkillDiscovery
{
    public static List<(string Name, string Description)> DiscoverAll()
    {
        var results = new List<(string Name, string Description)>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in GetSearchPaths())
        {
            if (!Directory.Exists(dir)) continue;

            // Flat .md files in the directory
            foreach (var file in Directory.EnumerateFiles(dir, "*.md"))
                TryAdd(file, results, seen);

            // Subdirectories with SKILL.md
            foreach (var subDir in Directory.EnumerateDirectories(dir))
            {
                var skillFile = Path.Combine(subDir, "SKILL.md");
                if (File.Exists(skillFile))
                    TryAdd(skillFile, results, seen);
            }
        }

        // SpecWeave plugin skills
        foreach (var skillFile in EnumerateSpecWeaveSkills())
            TryAdd(skillFile, results, seen);

        results.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    private static void TryAdd(string filePath, List<(string Name, string Description)> results, HashSet<string> seen)
    {
        try
        {
            var content = File.ReadAllText(filePath);
            var (metadata, _) = YamlFrontmatter.Parse(content);

            var name = metadata.GetValueOrDefault("name")
                ?? Path.GetFileNameWithoutExtension(filePath);

            if (seen.Add(name))
            {
                var description = metadata.GetValueOrDefault("description") ?? "";
                results.Add((name, description));
            }
        }
        catch
        {
            // Skip unreadable files
        }
    }

    private static List<string> GetSearchPaths()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return
        [
            Path.Combine(home, ".claude", "skills"),
            Path.Combine(Directory.GetCurrentDirectory(), ".claude", "skills")
        ];
    }

    private static IEnumerable<string> EnumerateSpecWeaveSkills()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var nvmBase = Path.Combine(home, ".nvm", "versions", "node");

        if (!Directory.Exists(nvmBase))
            yield break;

        foreach (var nodeDir in Directory.EnumerateDirectories(nvmBase))
        {
            var pluginsDir = Path.Combine(nodeDir, "lib", "node_modules", "specweave", "plugins");
            if (!Directory.Exists(pluginsDir)) continue;

            foreach (var pluginDir in Directory.EnumerateDirectories(pluginsDir))
            {
                var skillsDir = Path.Combine(pluginDir, "skills");
                if (!Directory.Exists(skillsDir)) continue;

                foreach (var skillDir in Directory.EnumerateDirectories(skillsDir))
                {
                    var skillFile = Path.Combine(skillDir, "SKILL.md");
                    if (File.Exists(skillFile))
                        yield return skillFile;
                }
            }
        }
    }
}
