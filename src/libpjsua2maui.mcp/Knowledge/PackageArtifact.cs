namespace libpjsua2maui.mcp.Knowledge;

/// <summary>
/// Describes a package artifact while keeping TFM and RID independent and optional.
/// </summary>
public sealed record PackageArtifact
{
    public PackageArtifact(
        PackageIdentity package,
        PackageVersion packageVersion,
        string relativePath,
        ArtifactKind kind,
        string? targetFramework = null,
        string? runtimeIdentifier = null)
    {
        Package = package ?? throw new ArgumentNullException(nameof(package));
        PackageVersion = packageVersion ?? throw new ArgumentNullException(nameof(packageVersion));
        RelativePath = RequirePath(relativePath);
        Kind = kind;
        TargetFramework = OptionalIdentifier(targetFramework, nameof(targetFramework));
        RuntimeIdentifier = OptionalIdentifier(runtimeIdentifier, nameof(runtimeIdentifier));
    }

    public PackageIdentity Package { get; }

    public PackageVersion PackageVersion { get; }

    public string RelativePath { get; }

    public ArtifactKind Kind { get; }

    public string? TargetFramework { get; }

    public string? RuntimeIdentifier { get; }

    private static string RequirePath(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (Path.IsPathRooted(value) || value.Contains('\\'))
        {
            throw new ArgumentException("Artifact paths must be relative package paths using '/'.", nameof(value));
        }

        return value;
    }

    private static string? OptionalIdentifier(string? value, string parameterName)
    {
        if (value is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("An optional platform identifier must contain text when supplied.", parameterName);
        }

        return value;
    }
}
