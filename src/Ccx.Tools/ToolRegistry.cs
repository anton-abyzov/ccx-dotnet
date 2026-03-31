namespace Ccx.Tools;

public sealed class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools = new(StringComparer.OrdinalIgnoreCase);

    public void Register(ITool tool)
    {
        _tools[tool.Name] = tool;
    }

    public ITool? FindByName(string name)
    {
        return _tools.GetValueOrDefault(name);
    }

    public IReadOnlyCollection<ITool> All => _tools.Values;

    public int Count => _tools.Count;
}
