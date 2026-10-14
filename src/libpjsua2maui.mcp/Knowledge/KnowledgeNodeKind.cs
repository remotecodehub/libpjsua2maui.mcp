namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Identifies the kind of node referenced by an evidence-backed fact.</summary>
public enum KnowledgeNodeKind
{
    ManagedSymbol,
    SwigSymbol,
    WrapperSymbol,
    NativeSymbol,
    NativeArtifact,
    TextReference
}
