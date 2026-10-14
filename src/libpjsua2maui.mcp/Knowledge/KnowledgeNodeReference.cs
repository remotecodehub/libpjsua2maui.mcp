namespace libpjsua2maui.mcp.Knowledge;

/// <summary>References one node without claiming more identity than the evidence supports.</summary>
public sealed record KnowledgeNodeReference
{
    public KnowledgeNodeReference(
        KnowledgeNodeKind kind,
        KnowledgeNodeResolution resolution,
        string? identifier)
    {
        if (resolution == KnowledgeNodeResolution.Unknown)
        {
            if (!string.IsNullOrWhiteSpace(identifier))
            {
                throw new ArgumentException("An unknown node cannot have an identifier.", nameof(identifier));
            }
        }
        else
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        }

        Kind = kind;
        Resolution = resolution;
        Identifier = string.IsNullOrWhiteSpace(identifier) ? null : identifier;
    }

    public KnowledgeNodeReference(KnowledgeNodeKind kind, string identifier)
        : this(kind, KnowledgeNodeResolution.Identified, identifier)
    {
    }

    public KnowledgeNodeKind Kind { get; }

    public KnowledgeNodeResolution Resolution { get; }

    public string? Identifier { get; }
}
