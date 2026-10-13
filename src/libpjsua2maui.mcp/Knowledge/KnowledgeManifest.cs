using System.Collections.ObjectModel;

namespace libpjsua2maui.mcp.Knowledge;

/// <summary>
/// Immutable manifest for the knowledge observed from one exact package version.
/// </summary>
public sealed record KnowledgeManifest
{
    public KnowledgeManifest(
        KnowledgeSetIdentity identity,
        IReadOnlyDictionary<string, string>? metadata = null,
        IReadOnlyList<PackageArtifact>? artifacts = null,
        KnowledgeManifestStatus status = KnowledgeManifestStatus.Incomplete)
    {
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        Metadata = new ReadOnlyDictionary<string, string>(
            new Dictionary<string, string>(metadata ?? new Dictionary<string, string>(), StringComparer.Ordinal));
        Artifacts = Array.AsReadOnly((artifacts ?? []).ToArray());
        Status = status;

        if (Artifacts.Any(artifact => artifact.Package != Package || artifact.PackageVersion != PackageVersion))
        {
            throw new ArgumentException("All manifest artifacts must belong to the manifest package version.", nameof(artifacts));
        }
    }

    public KnowledgeSetIdentity Identity { get; }

    public PackageIdentity Package => Identity.Package;

    public PackageVersion PackageVersion => Identity.PackageVersion;

    public IReadOnlyDictionary<string, string> Metadata { get; }

    public IReadOnlyList<PackageArtifact> Artifacts { get; }

    public KnowledgeManifestStatus Status { get; }
}
