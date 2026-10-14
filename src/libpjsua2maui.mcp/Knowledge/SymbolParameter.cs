namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Describes one parameter observed in a managed method or constructor signature.</summary>
public sealed record SymbolParameter
{
    public SymbolParameter(string name, string type, bool isByReference = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        Name = name;
        Type = type;
        IsByReference = isByReference;
    }

    public string Name { get; }

    public string Type { get; }

    public bool IsByReference { get; }
}
