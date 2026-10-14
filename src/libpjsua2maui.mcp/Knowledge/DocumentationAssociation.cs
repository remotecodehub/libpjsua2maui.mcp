namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Associates XML documentation with a symbol only when the exact documentation ID matches.</summary>
public sealed record DocumentationAssociation
{
    public DocumentationAssociation(DocumentationRecord documentation, SymbolRepresentation symbol, FactProvenance? provenance = null)
    {
        Documentation = documentation ?? throw new ArgumentNullException(nameof(documentation));
        Symbol = symbol ?? throw new ArgumentNullException(nameof(symbol));
        Provenance = provenance;

        if (documentation.Package != symbol.Package || documentation.PackageVersion != symbol.PackageVersion ||
            documentation.TargetFramework != symbol.TargetFramework)
        {
            throw new ArgumentException("Documentation associations must use matching package versions and TFMs.");
        }

        if (provenance is not null &&
            (provenance.KnowledgeSet.Package != documentation.Package || provenance.KnowledgeSet.PackageVersion != documentation.PackageVersion))
        {
            throw new ArgumentException("Association provenance must belong to the same package version.", nameof(provenance));
        }
    }

    public DocumentationRecord Documentation { get; }

    public SymbolRepresentation Symbol { get; }

    public FactProvenance? Provenance { get; }
}
