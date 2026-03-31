namespace Ccx.Skills;

/// <summary>
/// Executes skills by injecting their content as system prompt or user message.
/// </summary>
public sealed class SkillExecutor
{
    private readonly SkillLoader _loader;

    public SkillExecutor(SkillLoader loader)
    {
        _loader = loader;
    }

    public SkillExecutionResult Execute(string skillName, string? args = null)
    {
        var skill = _loader.FindByName(skillName);
        if (skill is null)
            return SkillExecutionResult.NotFound(skillName);

        var body = skill.Body;
        if (!string.IsNullOrEmpty(args))
            body = $"{body}\n\nArguments: {args}";

        return new SkillExecutionResult
        {
            Found = true,
            SkillName = skillName,
            InjectedPrompt = body,
            Mode = skill.Metadata.GetValueOrDefault("mode", "inline") == "agent"
                ? SkillMode.Agent
                : SkillMode.Inline
        };
    }

    public List<string> ListAvailable()
    {
        return _loader.LoadAll().Select(s => s.Name).ToList();
    }
}

public enum SkillMode
{
    Inline,
    Agent
}

public sealed class SkillExecutionResult
{
    public bool Found { get; init; }
    public string SkillName { get; init; } = "";
    public string? InjectedPrompt { get; init; }
    public SkillMode Mode { get; init; }
    public string? ErrorMessage { get; init; }

    public static SkillExecutionResult NotFound(string name) =>
        new() { Found = false, SkillName = name, ErrorMessage = $"Skill '{name}' not found." };
}
