namespace Ccx.Config;

/// <summary>
/// Discovers CLAUDE.md files by walking up the directory tree.
/// </summary>
public static class ClaudeMdDiscovery
{
    private const string FileName = "CLAUDE.md";

    public static List<string> FindAll(string? startDir = null)
    {
        startDir ??= Directory.GetCurrentDirectory();
        var results = new List<string>();

        // Walk up from current directory
        var dir = new DirectoryInfo(startDir);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, FileName);
            if (File.Exists(candidate))
                results.Add(candidate);
            dir = dir.Parent;
        }

        // Also check ~/.claude/CLAUDE.md
        var userClaudeMd = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".claude", FileName);
        if (File.Exists(userClaudeMd) && !results.Contains(userClaudeMd))
            results.Add(userClaudeMd);

        // Reverse so global is first, local is last (override order)
        results.Reverse();
        return results;
    }

    public static string? FindNearest(string? startDir = null)
    {
        startDir ??= Directory.GetCurrentDirectory();
        var dir = new DirectoryInfo(startDir);

        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, FileName);
            if (File.Exists(candidate))
                return candidate;
            dir = dir.Parent;
        }

        return null;
    }

    public static string LoadCombined(string? startDir = null)
    {
        var files = FindAll(startDir);
        if (files.Count == 0) return "";
        return string.Join("\n\n---\n\n", files.Select(File.ReadAllText));
    }
}
