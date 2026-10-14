namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Associates XML documentation with a symbol only when the exact documentation ID matches.</summary>
public sealed record DocumentationAssociation(DocumentationRecord Documentation, SymbolRepresentation Symbol);
