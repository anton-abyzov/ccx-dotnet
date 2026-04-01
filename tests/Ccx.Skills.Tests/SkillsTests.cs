using Ccx.Skills;
using FluentAssertions;

namespace Ccx.Skills.Tests;

public class YamlFrontmatterTests
{
    [Fact]
    public void Parse_WithFrontmatter_ExtractsMetadata()
    {
        var content = """
            ---
            name: test-skill
            description: A test skill
            trigger: when user says test
            ---
            This is the body.
            """;

        var (metadata, body) = YamlFrontmatter.Parse(content);

        metadata["name"].Should().Be("test-skill");
        metadata["description"].Should().Be("A test skill");
        body.Should().Contain("This is the body.");
    }

    [Fact]
    public void Parse_WithoutFrontmatter_ReturnsAllAsBody()
    {
        var content = "Just plain text.";
        var (metadata, body) = YamlFrontmatter.Parse(content);

        metadata.Should().BeEmpty();
        body.Should().Be("Just plain text.");
    }

    [Fact]
    public void Parse_QuotedValues_StripsQuotes()
    {
        var content = """
            ---
            name: "quoted-name"
            ---
            Body.
            """;

        var (metadata, _) = YamlFrontmatter.Parse(content);
        metadata["name"].Should().Be("quoted-name");
    }
}

public class SkillLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public SkillLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void LoadAll_FindsSkillFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "greet.md"), """
            ---
            name: greet
            description: Greeting skill
            ---
            Say hello!
            """);

        var loader = new SkillLoader([_tempDir]);
        var skills = loader.LoadAll();

        skills.Should().HaveCount(1);
        skills[0].Name.Should().Be("greet");
    }

    [Fact]
    public void FindByName_FindsCorrectSkill()
    {
        File.WriteAllText(Path.Combine(_tempDir, "foo.md"), "---\nname: foo\n---\nFoo body.");
        File.WriteAllText(Path.Combine(_tempDir, "bar.md"), "---\nname: bar\n---\nBar body.");

        var loader = new SkillLoader([_tempDir]);
        var skill = loader.FindByName("bar");

        skill.Should().NotBeNull();
        skill!.Name.Should().Be("bar");
    }

    [Fact]
    public void FindByName_NotFound_ReturnsNull()
    {
        var loader = new SkillLoader([_tempDir]);
        loader.FindByName("nonexistent").Should().BeNull();
    }

    public void Dispose() => Directory.Delete(_tempDir, true);
}

public class SkillExecutorTests : IDisposable
{
    private readonly string _tempDir;

    public SkillExecutorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        File.WriteAllText(Path.Combine(_tempDir, "test.md"), "---\nname: test\nmode: inline\n---\nDo the thing.");
    }

    [Fact]
    public void Execute_Found_ReturnsPrompt()
    {
        var loader = new SkillLoader([_tempDir]);
        var executor = new SkillExecutor(loader);
        var result = executor.Execute("test");

        result.Found.Should().BeTrue();
        result.InjectedPrompt.Should().Contain("Do the thing");
        result.Mode.Should().Be(SkillMode.Inline);
    }

    [Fact]
    public void Execute_NotFound_ReturnsError()
    {
        var loader = new SkillLoader([_tempDir]);
        var executor = new SkillExecutor(loader);
        var result = executor.Execute("missing");

        result.Found.Should().BeFalse();
        result.ErrorMessage.Should().Contain("not found");
    }

    [Fact]
    public void Execute_WithArgs_AppendsArgs()
    {
        var loader = new SkillLoader([_tempDir]);
        var executor = new SkillExecutor(loader);
        var result = executor.Execute("test", "extra args");

        result.InjectedPrompt.Should().Contain("extra args");
    }

    public void Dispose() => Directory.Delete(_tempDir, true);
}

public class SkillDiscoveryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _origDir;

    public SkillDiscoveryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _origDir = Directory.GetCurrentDirectory();

        // Create a fake project with .claude/skills/
        var projectSkillsDir = Path.Combine(_tempDir, "project", ".claude", "skills");
        Directory.CreateDirectory(projectSkillsDir);

        File.WriteAllText(Path.Combine(projectSkillsDir, "deploy.md"),
            "---\nname: deploy\ndescription: Deploy to production\n---\nDeploy steps.");

        // Subdir with SKILL.md
        var subSkillDir = Path.Combine(projectSkillsDir, "lint");
        Directory.CreateDirectory(subSkillDir);
        File.WriteAllText(Path.Combine(subSkillDir, "SKILL.md"),
            "---\nname: lint\ndescription: Run linter\n---\nLint the code.");

        Directory.SetCurrentDirectory(Path.Combine(_tempDir, "project"));
    }

    [Fact]
    public void DiscoverAll_FindsFlatSkills()
    {
        var skills = SkillDiscovery.DiscoverAll();
        skills.Should().Contain(s => s.Name == "deploy" && s.Description == "Deploy to production");
    }

    [Fact]
    public void DiscoverAll_FindsSubdirSkills()
    {
        var skills = SkillDiscovery.DiscoverAll();
        skills.Should().Contain(s => s.Name == "lint" && s.Description == "Run linter");
    }

    [Fact]
    public void DiscoverAll_DeduplicatesByName()
    {
        // Add a duplicate name in the project skills dir
        var skillsDir = Path.Combine(_tempDir, "project", ".claude", "skills");
        File.WriteAllText(Path.Combine(skillsDir, "deploy-copy.md"),
            "---\nname: deploy\ndescription: Duplicate\n---\nBody.");

        var skills = SkillDiscovery.DiscoverAll();
        skills.Where(s => s.Name == "deploy").Should().HaveCount(1);
    }

    [Fact]
    public void DiscoverAll_SortsByName()
    {
        var skills = SkillDiscovery.DiscoverAll();
        var names = skills.Select(s => s.Name).ToList();
        names.Should().BeInAscendingOrder(StringComparer.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        Directory.SetCurrentDirectory(_origDir);
        Directory.Delete(_tempDir, true);
    }
}
