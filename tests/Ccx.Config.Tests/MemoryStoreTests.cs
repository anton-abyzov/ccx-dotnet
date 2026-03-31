using Ccx.Config;
using FluentAssertions;

namespace Ccx.Config.Tests;

public class MemoryStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly MemoryStore _store;

    public MemoryStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _store = new MemoryStore(_tempDir, directPath: true);
    }

    [Fact]
    public void Save_CreatesFile()
    {
        var entry = new MemoryEntry
        {
            Name = "test-memory",
            Description = "A test memory",
            Type = MemoryType.Feedback,
            Content = "Don't do X.",
            FilePath = ""
        };

        _store.Save(entry);

        File.Exists(Path.Combine(_tempDir, "test-memory.md")).Should().BeTrue();
    }

    [Fact]
    public void Save_UpdatesIndex()
    {
        var entry = new MemoryEntry
        {
            Name = "indexed",
            Description = "Should appear in index",
            Type = MemoryType.User,
            Content = "Content here.",
            FilePath = ""
        };

        _store.Save(entry);

        var index = _store.LoadIndex();
        index.Should().NotBeNull();
        index.Should().Contain("indexed");
    }

    [Fact]
    public void Load_RoundTrips()
    {
        var entry = new MemoryEntry
        {
            Name = "roundtrip",
            Description = "Round trip test",
            Type = MemoryType.Project,
            Content = "Project context info.",
            FilePath = ""
        };

        _store.Save(entry);
        var loaded = _store.Load("roundtrip");

        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("roundtrip");
        loaded.Type.Should().Be(MemoryType.Project);
        loaded.Content.Should().Be("Project context info.");
    }

    [Fact]
    public void LoadAll_ReturnsAllEntries()
    {
        _store.Save(new MemoryEntry { Name = "a", Description = "d", Type = MemoryType.User, Content = "c", FilePath = "" });
        _store.Save(new MemoryEntry { Name = "b", Description = "d", Type = MemoryType.Feedback, Content = "c", FilePath = "" });

        var all = _store.LoadAll();
        all.Should().HaveCount(2);
    }

    [Fact]
    public void Delete_RemovesFile()
    {
        _store.Save(new MemoryEntry { Name = "delete-me", Description = "d", Type = MemoryType.User, Content = "c", FilePath = "" });
        _store.Delete("delete-me");

        _store.Load("delete-me").Should().BeNull();
    }

    [Fact]
    public void Load_NonExistent_ReturnsNull()
    {
        _store.Load("nonexistent").Should().BeNull();
    }

    public void Dispose() => Directory.Delete(_tempDir, true);
}
