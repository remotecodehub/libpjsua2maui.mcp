namespace libpjsua2maui.mcp.Knowledge;

/// <summary>
/// Stores documentation content without assuming it is a summary element.
/// </summary>
public sealed record DocumentationRecord
{
    public DocumentationRecord(
        PackageIdentity package,
        PackageVersion packageVersion,
        string memberId,
        string content,
        PackageArtifact sourceArtifact,
        string? targetFramework = null,
        FactProvenance? provenance = null)
    {
        Package = package ?? throw new ArgumentNullException(nameof(package));
        PackageVersion = packageVersion ?? throw new ArgumentNullException(nameof(packageVersion));
        ArgumentException.ThrowIfNullOrWhiteSpace(memberId);
        ArgumentNullException.ThrowIfNull(content);
        SourceArtifact = sourceArtifact ?? throw new ArgumentNullException(nameof(sourceArtifact));

        if (sourceArtifact.Package != package || sourceArtifact.PackageVersion != packageVersion ||
            sourceArtifact.Kind != ArtifactKind.XmlDocumentation)
        {
            throw new ArgumentException("Documentation must reference an XML artifact from the same package version.", nameof(sourceArtifact));
        }

        MemberId = memberId;
        Content = content;
        TargetFramework = string.IsNullOrWhiteSpace(targetFramework) ? null : targetFramework;
        Provenance = provenance;

        if (provenance is not null &&
            (provenance.KnowledgeSet.Package != package || provenance.KnowledgeSet.PackageVersion != packageVersion))
        {
            throw new ArgumentException("Documentation provenance must belong to the same package version.", nameof(provenance));
        }
    }

    public PackageIdentity Package { get; }

    public PackageVersion PackageVersion { get; }

    public string MemberId { get; }

    public string Content { get; }

    public PackageArtifact SourceArtifact { get; }

    public string? TargetFramework { get; }

    public FactProvenance? Provenance { get; }
}
