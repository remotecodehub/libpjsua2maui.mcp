namespace libpjsua2maui.mcp.Knowledge;

/// <summary>
/// Identifies a managed symbol independently from a TFM-specific representation.
/// </summary>
public sealed record SymbolIdentity
{
    public SymbolIdentity(string qualifiedName, SymbolKind kind)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(qualifiedName);
        QualifiedName = qualifiedName;
        Kind = kind;
    }

    public string QualifiedName { get; }

    public SymbolKind Kind { get; }
}
