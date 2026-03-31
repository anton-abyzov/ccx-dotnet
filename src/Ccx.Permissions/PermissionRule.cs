namespace Ccx.Permissions;

public enum PermissionAction
{
    Allow,
    Deny
}

public sealed class PermissionRule
{
    public required string ToolPattern { get; init; }
    public required PermissionAction Action { get; init; }
    public string? CommandPattern { get; init; }

    public bool Matches(string toolName, string? command = null)
    {
        if (!GlobMatch(ToolPattern, toolName))
            return false;

        if (CommandPattern is not null && command is not null)
            return GlobMatch(CommandPattern, command);

        return CommandPattern is null;
    }

    private static bool GlobMatch(string pattern, string value)
    {
        if (pattern == "*") return true;

        // Simple glob: support * and ? wildcards
        var regexPattern = "^" +
            System.Text.RegularExpressions.Regex.Escape(pattern)
                .Replace("\\*", ".*")
                .Replace("\\?", ".") +
            "$";

        return System.Text.RegularExpressions.Regex.IsMatch(
            value, regexPattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
    }
}
