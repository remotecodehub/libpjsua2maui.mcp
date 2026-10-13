namespace libpjsua2maui.mcp.Knowledge;

/// <summary>
/// Deterministically identifies one knowledge set for a package version and optional rebuild revision.
/// </summary>
public sealed record KnowledgeSetIdentity
{
    public KnowledgeSetIdentity(PackageIdentity package, PackageVersion packageVersion, string? revision = null)
    {
        Package = package ?? throw new ArgumentNullException(nameof(package));
        PackageVersion = packageVersion ?? throw new ArgumentNullException(nameof(packageVersion));

        if (revision is not null && string.IsNullOrWhiteSpace(revision))
        {
            throw new ArgumentException("A knowledge revision must contain text when supplied.", nameof(revision));
        }

        Revision = revision;
        Value = revision is null
            ? $"{Package.Id}@{PackageVersion}"
            : $"{Package.Id}@{PackageVersion}#{revision}";
    }

    public PackageIdentity Package { get; }

    public PackageVersion PackageVersion { get; }

    public string? Revision { get; }

    public string Value { get; }

    public override string ToString() => Value;
}
