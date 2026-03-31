namespace Ccx.Tools;

public sealed class ToolResult
{
    public string Output { get; init; } = "";
    public bool IsError { get; init; }

    public static ToolResult Success(string output) => new() { Output = output };
    public static ToolResult Error(string output) => new() { Output = output, IsError = true };
}
