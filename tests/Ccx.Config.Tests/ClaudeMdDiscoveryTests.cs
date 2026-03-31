using Ccx.Config;
using FluentAssertions;

namespace Ccx.Config.Tests;

public class ClaudeMdDiscoveryTests : IDisposable
{
    private readonly string _tempDir;

    public ClaudeMdDiscoveryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [Fact]
    public void FindNearest_FindsInCurrentDir()
    {
        File.WriteAllText(Path.Combine(_tempDir, "CLAUDE.md"), "# Instructions");

        var result = ClaudeMdDiscovery.FindNearest(_tempDir);
        result.Should().NotBeNull();
        result.Should().EndWith("CLAUDE.md");
    }

    [Fact]
    public void FindNearest_WalksUp()
    {
        var subDir = Path.Combine(_tempDir, "sub", "deep");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(_tempDir, "CLAUDE.md"), "# Instructions");

        var result = ClaudeMdDiscovery.FindNearest(subDir);
        result.Should().NotBeNull();
        result.Should().Be(Path.Combine(_tempDir, "CLAUDE.md"));
    }

    [Fact]
    public void FindNearest_ReturnsNullWhenMissing()
    {
        var result = ClaudeMdDiscovery.FindNearest(_tempDir);
        result.Should().BeNull();
    }

    [Fact]
    public void FindAll_ReturnsMultiple()
    {
        var subDir = Path.Combine(_tempDir, "project");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(_tempDir, "CLAUDE.md"), "# Root");
        File.WriteAllText(Path.Combine(subDir, "CLAUDE.md"), "# Project");

        var results = ClaudeMdDiscovery.FindAll(subDir);
        results.Count.Should().BeGreaterThanOrEqualTo(2);
    }

    [Fact]
    public void LoadCombined_MergesFiles()
    {
        File.WriteAllText(Path.Combine(_tempDir, "CLAUDE.md"), "# Root Instructions");

        var combined = ClaudeMdDiscovery.LoadCombined(_tempDir);
        combined.Should().Contain("Root Instructions");
    }

    public void Dispose() => Directory.Delete(_tempDir, true);
}
