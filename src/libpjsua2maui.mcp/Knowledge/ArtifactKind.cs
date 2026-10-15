namespace libpjsua2maui.mcp.Knowledge;

/// <summary>
/// Classifies an artifact discovered in a package.
/// </summary>
public enum ArtifactKind
{
    ManagedAssembly,
    ReferenceAssembly,
    XmlDocumentation,
    PackageMetadata,
    SwigArtifact,
    ManagedSource,
    NativeSource,
    NativeHeader,
    NativeLibrary,
    Other
}
