using System.Text;
using System.Text.Json;
using Ccx.Tools;

namespace Ccx.Core;

public static class PromptBuilder
{
    public static string BuildSystemPrompt(IEnumerable<ITool> tools, string claudeMd = "")
    {
        var sb = new StringBuilder();

        sb.AppendLine("You are Claude, an AI assistant by Anthropic. You help users with software engineering tasks.");
        sb.AppendLine();
        sb.AppendLine("# Environment");
        sb.AppendLine($"- Working directory: {Directory.GetCurrentDirectory()}");
        sb.AppendLine($"- Platform: {Environment.OSVersion.Platform}");
        sb.AppendLine($"- Date: {DateTime.UtcNow:yyyy-MM-dd}");
        sb.AppendLine();

        // Tool descriptions
        var toolList = tools.ToList();
        if (toolList.Count > 0)
        {
            sb.AppendLine("# Available Tools");
            sb.AppendLine();
            foreach (var tool in toolList)
            {
                sb.AppendLine($"## {tool.Name}");
                sb.AppendLine(tool.Description);
                sb.AppendLine();
                sb.AppendLine("Input Schema:");
                sb.AppendLine("```json");
                sb.AppendLine(JsonSerializer.Serialize(tool.InputSchema, new JsonSerializerOptions { WriteIndented = true }));
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        sb.AppendLine("# Guidelines");
        sb.AppendLine("- Read files before editing them.");
        sb.AppendLine("- Use the appropriate tool for each task — prefer dedicated tools over Bash when available.");
        sb.AppendLine("- Be concise and direct in responses.");
        sb.AppendLine("- When writing code, follow existing patterns in the codebase.");
        sb.AppendLine("- Do not introduce security vulnerabilities.");
        sb.AppendLine();

        // CLAUDE.md content
        if (!string.IsNullOrWhiteSpace(claudeMd))
        {
            sb.AppendLine("# Project Instructions (CLAUDE.md)");
            sb.AppendLine();
            sb.AppendLine(claudeMd.Trim());
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }
}
