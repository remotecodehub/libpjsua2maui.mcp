namespace libpjsua2maui.mcp.Knowledge;

/// <summary>
/// Represents a possibly partial, evidence-backed relationship between knowledge nodes.
/// </summary>
public sealed record KnowledgeRelationship
{
    public KnowledgeRelationship(
        KnowledgeSetIdentity knowledgeSet,
        string relationshipType,
        string source,
        string? target = null,
        FactProvenance? provenance = null,
        KnowledgeFactStatus status = KnowledgeFactStatus.Established)
    {
        KnowledgeSet = knowledgeSet ?? throw new ArgumentNullException(nameof(knowledgeSet));
        RelationshipType = Required(relationshipType, nameof(relationshipType));
        Source = Required(source, nameof(source));
        Target = string.IsNullOrWhiteSpace(target) ? null : target;
        SourceNode = new KnowledgeNodeReference(KnowledgeNodeKind.TextReference, KnowledgeNodeResolution.TextReference, source);
        TargetNode = Target is null
            ? new KnowledgeNodeReference(KnowledgeNodeKind.TextReference, KnowledgeNodeResolution.Unknown, null)
            : new KnowledgeNodeReference(KnowledgeNodeKind.TextReference, KnowledgeNodeResolution.TextReference, Target);
        Status = status;
        Provenance = provenance;

        if (provenance is not null && provenance.KnowledgeSet != knowledgeSet)
        {
            throw new ArgumentException("Relationship provenance must belong to the same knowledge set.", nameof(provenance));
        }
    }

    public KnowledgeRelationship(
        KnowledgeSetIdentity knowledgeSet,
        string relationshipType,
        KnowledgeNodeReference source,
        KnowledgeNodeReference? target,
        KnowledgeFactStatus status,
        FactProvenance? provenance = null)
    {
        KnowledgeSet = knowledgeSet ?? throw new ArgumentNullException(nameof(knowledgeSet));
        RelationshipType = Required(relationshipType, nameof(relationshipType));
        SourceNode = source ?? throw new ArgumentNullException(nameof(source));
        TargetNode = target ?? new KnowledgeNodeReference(KnowledgeNodeKind.TextReference, KnowledgeNodeResolution.Unknown, null);
        Status = status;
        Provenance = provenance;
        Source = source.Identifier ?? string.Empty;
        Target = TargetNode.Identifier;

        if (provenance is not null && provenance.KnowledgeSet != knowledgeSet)
        {
            throw new ArgumentException("Relationship provenance must belong to the same knowledge set.", nameof(provenance));
        }
    }

    public KnowledgeSetIdentity KnowledgeSet { get; }

    public string RelationshipType { get; }

    public string Source { get; }

    public string? Target { get; }

    public KnowledgeNodeReference SourceNode { get; }

    public KnowledgeNodeReference TargetNode { get; }

    public KnowledgeFactStatus Status { get; }

    public FactProvenance? Provenance { get; }

    private static string Required(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}
