namespace libpjsua2maui.mcp.Knowledge;

/// <summary>
/// Identifies a NuGet package independently from its version.
/// </summary>
public sealed record PackageIdentity
{
    public PackageIdentity(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        Id = id.Trim();
    }

    public string Id { get; }
}
