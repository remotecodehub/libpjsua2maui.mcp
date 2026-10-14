namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Represents an evidence-backed ownership, callback, handle, disposal, or lifecycle observation.</summary>
public sealed record LifecycleFact
{
    public LifecycleFact(
        KnowledgeSetIdentity knowledgeSet,
        KnowledgeNodeReference subject,
        LifecycleFactKind kind,
        KnowledgeFactStatus status,
        string? value = null,
        FactProvenance? provenance = null,
        KnowledgeConfidence confidence = KnowledgeConfidence.Unknown)
    {
        KnowledgeSet = knowledgeSet ?? throw new ArgumentNullException(nameof(knowledgeSet));
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
        Kind = kind;
        Status = status;
        Confidence = confidence;
        Value = string.IsNullOrWhiteSpace(value) ? null : value;
        Provenance = provenance;

        if (provenance is not null && provenance.KnowledgeSet != knowledgeSet)
        {
            throw new ArgumentException("Lifecycle provenance must belong to the same knowledge set.", nameof(provenance));
        }
    }

    public KnowledgeSetIdentity KnowledgeSet { get; }

    public KnowledgeNodeReference Subject { get; }

    public LifecycleFactKind Kind { get; }

    public KnowledgeFactStatus Status { get; }

    public KnowledgeConfidence Confidence { get; }

    public string? Value { get; }

    public FactProvenance? Provenance { get; }
}
