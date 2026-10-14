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
        string? limitations = null,
        string? repository = null,
        string? revision = null,
        string? tag = null,
        string? targetFramework = null,
        string? runtimeIdentifier = null,
        string? evidence = null)
    {
        KnowledgeSet = knowledgeSet ?? throw new ArgumentNullException(nameof(knowledgeSet));
        Kind = kind;
        ArtifactPath = OptionalText(artifactPath, nameof(artifactPath));
        Symbol = symbol;
        SourceReference = OptionalText(sourceReference, nameof(sourceReference));
        Limitations = OptionalText(limitations, nameof(limitations));
        Repository = OptionalText(repository, nameof(repository));
        Revision = OptionalText(revision, nameof(revision));
        Tag = OptionalText(tag, nameof(tag));
        TargetFramework = OptionalText(targetFramework, nameof(targetFramework));
        RuntimeIdentifier = OptionalText(runtimeIdentifier, nameof(runtimeIdentifier));
        Evidence = OptionalText(evidence, nameof(evidence));
    }

    public KnowledgeSetIdentity KnowledgeSet { get; }

    public ProvenanceKind Kind { get; }

    public string? ArtifactPath { get; }

    public SymbolIdentity? Symbol { get; }

    public string? SourceReference { get; }

    public string? Limitations { get; }
    public string? Repository { get; }
    public string? Revision { get; }
    public string? Tag { get; }
    public string? TargetFramework { get; }
    public string? RuntimeIdentifier { get; }
    public string? Evidence { get; }

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
