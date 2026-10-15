namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Records whether the package's primary managed assemblies were enumerated completely.</summary>
public sealed record MetadataEnumerationCoverage
{
    public int ExpectedManagedAssemblies { get; init; }
    public int InspectedManagedAssemblies { get; init; }
    public bool SymbolEnumerationCompleted { get; init; }

    public bool IsComplete => ExpectedManagedAssemblies == InspectedManagedAssemblies &&
        SymbolEnumerationCompleted;
}
