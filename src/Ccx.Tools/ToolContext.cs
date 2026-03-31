namespace Ccx.Tools;

public sealed class ToolContext
{
    public string WorkingDirectory { get; init; } = Directory.GetCurrentDirectory();
}
