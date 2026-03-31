using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Ccx.Tools.Tools;

public sealed class GrepTool : ITool
{
    public string Name => "Grep";

    public string Description => "Search file contents using regex. Uses ripgrep if available, else built-in.";

    public JsonElement InputSchema { get; } = JsonDocument.Parse("""
        {
            "type": "object",
            "properties": {
                "pattern": { "type": "string", "description": "Regex pattern to search for" },
                "path": { "type": "string", "description": "Directory or file to search" },
                "glob": { "type": "string", "description": "File glob filter (e.g. *.cs)" },
                "output_mode": { "type": "string", "enum": ["content", "files_with_matches", "count"], "description": "Output mode (default files_with_matches)" },
                "case_insensitive": { "type": "boolean", "description": "Case insensitive search" }
            },
            "required": ["pattern"]
        }
        """).RootElement.Clone();

    public async Task<ToolResult> ExecuteAsync(JsonElement input, ToolContext ctx, CancellationToken ct)
    {
        var pattern = input.GetProperty("pattern").GetString();
        if (string.IsNullOrWhiteSpace(pattern))
            return ToolResult.Error("pattern is required.");

        var path = input.TryGetProperty("path", out var p) ? p.GetString() : null;
        path = string.IsNullOrWhiteSpace(path) ? ctx.WorkingDirectory : Path.GetFullPath(path, ctx.WorkingDirectory);

        var glob = input.TryGetProperty("glob", out var g) ? g.GetString() : null;
        var outputMode = input.TryGetProperty("output_mode", out var om) ? om.GetString() : "files_with_matches";
        var caseInsensitive = input.TryGetProperty("case_insensitive", out var ci) && ci.GetBoolean();

        var rgResult = await TryRipgrep(pattern, path, glob, outputMode!, caseInsensitive, ct);
        if (rgResult is not null)
            return rgResult;

        return BuiltInSearch(pattern, path, glob, outputMode!, caseInsensitive);
    }

    private static async Task<ToolResult?> TryRipgrep(string pattern, string path, string? glob,
        string outputMode, bool caseInsensitive, CancellationToken ct)
    {
        var args = new StringBuilder();
        args.Append("--no-heading ");
        if (caseInsensitive) args.Append("-i ");

        switch (outputMode)
        {
            case "files_with_matches": args.Append("-l "); break;
            case "count": args.Append("-c "); break;
            default: args.Append("-n "); break;
        }

        if (!string.IsNullOrEmpty(glob))
            args.Append($"--glob '{glob}' ");

        args.Append($"-- '{pattern.Replace("'", "\\'")}' '{path.Replace("'", "\\'")}'");

        try
        {
            var psi = new ProcessStartInfo("rg", args.ToString())
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var proc = Process.Start(psi);
            if (proc is null) return null;

            var output = await proc.StandardOutput.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);

            if (proc.ExitCode > 1) return null; // rg error, fallback

            return ToolResult.Success(string.IsNullOrEmpty(output) ? "No matches found." : output.TrimEnd());
        }
        catch
        {
            return null; // rg not found, fallback
        }
    }

    private static ToolResult BuiltInSearch(string pattern, string path, string? glob,
        string outputMode, bool caseInsensitive)
    {
        var regexOptions = RegexOptions.Compiled;
        if (caseInsensitive) regexOptions |= RegexOptions.IgnoreCase;

        Regex regex;
        try { regex = new Regex(pattern, regexOptions); }
        catch (Exception ex) { return ToolResult.Error($"Invalid regex: {ex.Message}"); }

        var searchPattern = string.IsNullOrEmpty(glob) ? "*" : glob;
        var files = Directory.Exists(path)
            ? Directory.EnumerateFiles(path, searchPattern, SearchOption.AllDirectories)
            : File.Exists(path) ? [path] : [];

        var sb = new StringBuilder();
        var matchCount = 0;

        foreach (var file in files)
        {
            if (matchCount > 1000) break;

            try
            {
                var lines = File.ReadAllLines(file);
                var fileHasMatch = false;

                for (var i = 0; i < lines.Length; i++)
                {
                    if (!regex.IsMatch(lines[i])) continue;
                    fileHasMatch = true;
                    matchCount++;

                    if (outputMode == "content")
                        sb.AppendLine($"{file}:{i + 1}:{lines[i]}");
                }

                if (fileHasMatch && outputMode == "files_with_matches")
                    sb.AppendLine(file);
            }
            catch { /* skip unreadable files */ }
        }

        if (outputMode == "count")
            sb.AppendLine(matchCount.ToString());

        return ToolResult.Success(sb.Length == 0 ? "No matches found." : sb.ToString().TrimEnd());
    }
}
