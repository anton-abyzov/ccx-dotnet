namespace Ccx.Tui;

/// <summary>
/// Custom readline with Up/Down history and Tab slash-command completion.
/// Replaces Console.ReadLine() for interactive input.
/// </summary>
public sealed class ReadLineEditor
{
    private readonly CommandHistory _history;
    private readonly List<string> _completions;

    public ReadLineEditor(CommandHistory history, IEnumerable<string> slashCommands)
    {
        _history = history;
        _completions = slashCommands.Select(c => c.StartsWith('/') ? c : "/" + c).ToList();
    }

    public string? ReadLine()
    {
        var buf = new List<char>();
        var pos = 0;
        var historyIndex = _history.Entries.Count; // past-the-end = current input
        string? savedInput = null;

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            switch (key.Key)
            {
                case ConsoleKey.Enter:
                    Console.WriteLine();
                    return new string(buf.ToArray());

                case ConsoleKey.Backspace:
                    if (pos > 0)
                    {
                        buf.RemoveAt(pos - 1);
                        pos--;
                        Redraw(buf, pos);
                    }
                    break;

                case ConsoleKey.Delete:
                    if (pos < buf.Count)
                    {
                        buf.RemoveAt(pos);
                        Redraw(buf, pos);
                    }
                    break;

                case ConsoleKey.LeftArrow:
                    if (pos > 0)
                    {
                        pos--;
                        Console.Write('\b');
                    }
                    break;

                case ConsoleKey.RightArrow:
                    if (pos < buf.Count)
                    {
                        Console.Write(buf[pos]);
                        pos++;
                    }
                    break;

                case ConsoleKey.Home:
                    while (pos > 0)
                    {
                        Console.Write('\b');
                        pos--;
                    }
                    break;

                case ConsoleKey.End:
                    while (pos < buf.Count)
                    {
                        Console.Write(buf[pos]);
                        pos++;
                    }
                    break;

                case ConsoleKey.UpArrow:
                    if (historyIndex > 0)
                    {
                        if (historyIndex == _history.Entries.Count)
                            savedInput = new string(buf.ToArray());
                        historyIndex--;
                        SetBuffer(buf, ref pos, _history.Entries[historyIndex]);
                    }
                    break;

                case ConsoleKey.DownArrow:
                    if (historyIndex < _history.Entries.Count)
                    {
                        historyIndex++;
                        var text = historyIndex < _history.Entries.Count
                            ? _history.Entries[historyIndex]
                            : savedInput ?? "";
                        SetBuffer(buf, ref pos, text);
                    }
                    break;

                case ConsoleKey.Tab:
                    TryComplete(buf, ref pos);
                    break;

                case ConsoleKey.Escape:
                    // Clear line
                    SetBuffer(buf, ref pos, "");
                    historyIndex = _history.Entries.Count;
                    savedInput = null;
                    break;

                default:
                    if (key.KeyChar >= 32) // printable
                    {
                        buf.Insert(pos, key.KeyChar);
                        pos++;
                        if (pos == buf.Count)
                        {
                            Console.Write(key.KeyChar);
                        }
                        else
                        {
                            Redraw(buf, pos);
                        }
                    }
                    break;
            }
        }
    }

    private void TryComplete(List<char> buf, ref int pos)
    {
        var current = new string(buf.ToArray());
        if (!current.StartsWith('/')) return;

        var matches = _completions
            .Where(c => c.StartsWith(current, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 1)
        {
            SetBuffer(buf, ref pos, matches[0] + " ");
        }
        else if (matches.Count > 1)
        {
            // Show matches below, then redraw
            Console.WriteLine();
            foreach (var m in matches.Take(10))
                Console.WriteLine($"  {m}");
            if (matches.Count > 10)
                Console.WriteLine($"  ... {matches.Count - 10} more");

            // Re-render prompt and current input
            Console.Write("\u276f ");
            Console.Write(current);
        }
    }

    private static void SetBuffer(List<char> buf, ref int pos, string text)
    {
        // Move cursor to start
        if (pos > 0)
            Console.Write(new string('\b', pos));

        // Clear entire line content
        var clearLen = Math.Max(buf.Count, pos);
        Console.Write(new string(' ', clearLen));
        Console.Write(new string('\b', clearLen));

        buf.Clear();
        buf.AddRange(text);
        pos = buf.Count;
        Console.Write(text);
    }

    private static void Redraw(List<char> buf, int pos)
    {
        // Save position, rewrite from start of input
        var cursorLeft = Console.CursorLeft;
        var lineStart = cursorLeft - pos;
        if (lineStart < 0) lineStart = 0;

        Console.CursorLeft = lineStart;
        var text = new string(buf.ToArray());
        Console.Write(text);
        // Clear trailing chars from previous longer content
        Console.Write("  ");
        Console.Write("\b\b");
        // Restore cursor
        Console.CursorLeft = lineStart + pos;
    }
}
