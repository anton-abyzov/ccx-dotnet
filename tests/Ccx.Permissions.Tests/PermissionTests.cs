using Ccx.Permissions;
using FluentAssertions;

namespace Ccx.Permissions.Tests;

public class PermissionRuleTests
{
    [Fact]
    public void Matches_ExactToolName_ReturnsTrue()
    {
        var rule = new PermissionRule { ToolPattern = "Bash", Action = PermissionAction.Allow };
        rule.Matches("Bash").Should().BeTrue();
    }

    [Fact]
    public void Matches_Wildcard_MatchesAll()
    {
        var rule = new PermissionRule { ToolPattern = "*", Action = PermissionAction.Allow };
        rule.Matches("Bash").Should().BeTrue();
        rule.Matches("Read").Should().BeTrue();
    }

    [Fact]
    public void Matches_GlobPattern_Works()
    {
        var rule = new PermissionRule { ToolPattern = "File*", Action = PermissionAction.Allow };
        rule.Matches("FileRead").Should().BeTrue();
        rule.Matches("FileWrite").Should().BeTrue();
        rule.Matches("Bash").Should().BeFalse();
    }

    [Fact]
    public void Matches_WithCommandPattern()
    {
        var rule = new PermissionRule
        {
            ToolPattern = "Bash",
            Action = PermissionAction.Deny,
            CommandPattern = "rm*"
        };
        rule.Matches("Bash", "rm -rf /tmp").Should().BeTrue();
        rule.Matches("Bash", "echo hello").Should().BeFalse();
    }

    [Fact]
    public void Matches_CaseInsensitive()
    {
        var rule = new PermissionRule { ToolPattern = "bash", Action = PermissionAction.Allow };
        rule.Matches("Bash").Should().BeTrue();
        rule.Matches("BASH").Should().BeTrue();
    }
}

public class PermissionClassifierTests
{
    [Fact]
    public void Classify_NoRules_AskUser()
    {
        var classifier = new PermissionClassifier();
        classifier.Classify("Bash").Should().Be(PermissionDecision.AskUser);
    }

    [Fact]
    public void Classify_AllowRule_Allowed()
    {
        var classifier = new PermissionClassifier();
        classifier.AddRule(new PermissionRule { ToolPattern = "Bash", Action = PermissionAction.Allow });

        classifier.Classify("Bash").Should().Be(PermissionDecision.Allowed);
    }

    [Fact]
    public void Classify_DenyRule_Denied()
    {
        var classifier = new PermissionClassifier();
        classifier.AddRule(new PermissionRule { ToolPattern = "Bash", Action = PermissionAction.Deny });

        classifier.Classify("Bash").Should().Be(PermissionDecision.Denied);
    }

    [Fact]
    public void Classify_LastMatchWins()
    {
        var classifier = new PermissionClassifier();
        classifier.AddRule(new PermissionRule { ToolPattern = "*", Action = PermissionAction.Deny });
        classifier.AddRule(new PermissionRule { ToolPattern = "Read", Action = PermissionAction.Allow });

        classifier.Classify("Read").Should().Be(PermissionDecision.Allowed);
        classifier.Classify("Bash").Should().Be(PermissionDecision.Denied);
    }

    [Fact]
    public void Classify_UnmatchedTool_AskUser()
    {
        var classifier = new PermissionClassifier();
        classifier.AddRule(new PermissionRule { ToolPattern = "Bash", Action = PermissionAction.Allow });

        classifier.Classify("Write").Should().Be(PermissionDecision.AskUser);
    }
}
