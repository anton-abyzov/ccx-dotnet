namespace Ccx.Permissions;

public enum PermissionDecision
{
    Allowed,
    Denied,
    AskUser
}

public sealed class PermissionClassifier
{
    private readonly List<PermissionRule> _rules = [];

    public void AddRule(PermissionRule rule) => _rules.Add(rule);

    public void AddRules(IEnumerable<PermissionRule> rules) => _rules.AddRange(rules);

    public PermissionDecision Classify(string toolName, string? command = null)
    {
        // Rules are evaluated in order; last match wins
        PermissionDecision? result = null;

        foreach (var rule in _rules)
        {
            if (rule.Matches(toolName, command))
            {
                result = rule.Action == PermissionAction.Allow
                    ? PermissionDecision.Allowed
                    : PermissionDecision.Denied;
            }
        }

        return result ?? PermissionDecision.AskUser;
    }

    public int RuleCount => _rules.Count;
}
