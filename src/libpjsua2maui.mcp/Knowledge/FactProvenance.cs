namespace libpjsua2maui.mcp.Knowledge;

/// <summary>
/// Records the evidence and limitations behind a knowledge fact.
/// </summary>
public sealed record FactProvenance
{
    public FactProvenance(
        KnowledgeSetIdentity knowledgeSet,
        ProvenanceKind kind,
        string? artifactPath = null,
        SymbolIdentity? symbol = null,
        string? sourceReference = null,
        string? limitations = null)
    {
        KnowledgeSet = knowledgeSet ?? throw new ArgumentNullException(nameof(knowledgeSet));
        Kind = kind;
        ArtifactPath = OptionalText(artifactPath, nameof(artifactPath));
        Symbol = symbol;
        SourceReference = OptionalText(sourceReference, nameof(sourceReference));
        Limitations = OptionalText(limitations, nameof(limitations));
    }

    public KnowledgeSetIdentity KnowledgeSet { get; }

    public ProvenanceKind Kind { get; }

    public string? ArtifactPath { get; }

    public SymbolIdentity? Symbol { get; }

    public string? SourceReference { get; }

    public string? Limitations { get; }

    private static string? OptionalText(string? value, string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("An optional provenance value must contain text when supplied.", parameterName);
        }

        return value;
    }
}
