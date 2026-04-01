namespace Ccx.Tui;

/// <summary>
/// Persists command history to ~/.ccx_history across sessions.
/// </summary>
public sealed class CommandHistory
{
    private readonly string _filePath;
    private readonly List<string> _entries = [];
    private const int MaxEntries = 1000;

    public CommandHistory()
    {
        _filePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".ccx_history");
        Load();
    }

    public IReadOnlyList<string> Entries => _entries;

    public void Add(string entry)
    {
        if (string.IsNullOrWhiteSpace(entry)) return;

        // Deduplicate consecutive
        if (_entries.Count > 0 && _entries[^1] == entry) return;

        _entries.Add(entry);

        // Trim oldest
        if (_entries.Count > MaxEntries)
            _entries.RemoveRange(0, _entries.Count - MaxEntries);

        Append(entry);
    }

    private void Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return;
            var lines = File.ReadAllLines(_filePath);
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    _entries.Add(line);
            }

            // Trim if file was too large
            if (_entries.Count > MaxEntries)
                _entries.RemoveRange(0, _entries.Count - MaxEntries);
        }
        catch { /* ignore corrupt history */ }
    }

    private void Append(string entry)
    {
        try
        {
            File.AppendAllText(_filePath, entry + "\n");
        }
        catch { /* non-critical */ }
    }
}
