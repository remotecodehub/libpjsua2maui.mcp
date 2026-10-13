namespace libpjsua2maui.mcp.Knowledge;

/// <summary>
/// Represents observations of a managed symbol in a package artifact or TFM.
/// </summary>
public sealed record SymbolRepresentation
{
    public SymbolRepresentation(
        PackageIdentity package,
        PackageVersion packageVersion,
        SymbolIdentity identity,
        string? targetFramework = null,
        PackageArtifact? sourceArtifact = null)
    {
        Package = package ?? throw new ArgumentNullException(nameof(package));
        PackageVersion = packageVersion ?? throw new ArgumentNullException(nameof(packageVersion));
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        TargetFramework = string.IsNullOrWhiteSpace(targetFramework) ? null : targetFramework;
        SourceArtifact = sourceArtifact;

        if (sourceArtifact is not null &&
            (sourceArtifact.Package != package || sourceArtifact.PackageVersion != packageVersion))
        {
            throw new ArgumentException("A symbol artifact must belong to the same package version.", nameof(sourceArtifact));
        }
    }

    public PackageIdentity Package { get; }

    public PackageVersion PackageVersion { get; }

    public SymbolIdentity Identity { get; }

    public string? TargetFramework { get; }

    public PackageArtifact? SourceArtifact { get; }
}
