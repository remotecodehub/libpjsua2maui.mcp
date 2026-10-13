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
        FactProvenance? provenance = null)
    {
        KnowledgeSet = knowledgeSet ?? throw new ArgumentNullException(nameof(knowledgeSet));
        RelationshipType = Required(relationshipType, nameof(relationshipType));
        Source = Required(source, nameof(source));
        Target = string.IsNullOrWhiteSpace(target) ? null : target;
        Provenance = provenance;

        if (provenance is not null && provenance.KnowledgeSet != knowledgeSet)
        {
            throw new ArgumentException("Relationship provenance must belong to the same knowledge set.", nameof(provenance));
        }
    }

    public KnowledgeSetIdentity KnowledgeSet { get; }

    public string RelationshipType { get; }

    public string Source { get; }

    public string? Target { get; }

    public FactProvenance? Provenance { get; }

    private static string Required(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return value;
    }
}
