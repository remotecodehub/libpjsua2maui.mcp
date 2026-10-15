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
        PackageArtifact? sourceArtifact = null,
        string? documentationId = null,
        string? signature = null,
        IReadOnlyList<SymbolParameter>? parameters = null,
        string? returnType = null,
        FactProvenance? provenance = null,
        string? declaringType = null,
        bool isStatic = false)
    {
        Package = package ?? throw new ArgumentNullException(nameof(package));
        PackageVersion = packageVersion ?? throw new ArgumentNullException(nameof(packageVersion));
        Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        TargetFramework = string.IsNullOrWhiteSpace(targetFramework) ? null : targetFramework;
        SourceArtifact = sourceArtifact;
        DocumentationId = string.IsNullOrWhiteSpace(documentationId) ? null : documentationId;
        Signature = string.IsNullOrWhiteSpace(signature) ? null : signature;
        Parameters = Array.AsReadOnly((parameters ?? []).ToArray());
        ReturnType = string.IsNullOrWhiteSpace(returnType) ? null : returnType;
        DeclaringType = string.IsNullOrWhiteSpace(declaringType) ? InferDeclaringType(identity, identity.Kind) : declaringType;
        IsStatic = isStatic;
        Provenance = provenance;

        if (sourceArtifact is not null &&
            (sourceArtifact.Package != package || sourceArtifact.PackageVersion != packageVersion))
        {
            throw new ArgumentException("A symbol artifact must belong to the same package version.", nameof(sourceArtifact));
        }

        if (provenance is not null &&
            (provenance.KnowledgeSet.Package != package || provenance.KnowledgeSet.PackageVersion != packageVersion))
        {
            throw new ArgumentException("Symbol provenance must belong to the same package version.", nameof(provenance));
        }
    }

    public PackageIdentity Package { get; }

    public PackageVersion PackageVersion { get; }

    public SymbolIdentity Identity { get; }

    public string? TargetFramework { get; }

    public PackageArtifact? SourceArtifact { get; }

    public string? DocumentationId { get; }

    public string? Signature { get; }

    public IReadOnlyList<SymbolParameter> Parameters { get; }

    public string? ReturnType { get; }

    public string DeclaringType { get; }

    public bool IsStatic { get; }

    public FactProvenance? Provenance { get; }

    private static string InferDeclaringType(SymbolIdentity identity, SymbolKind kind)
    {
        if (kind is SymbolKind.Class or SymbolKind.Struct or SymbolKind.Interface or SymbolKind.Enum or SymbolKind.Delegate or SymbolKind.Record or SymbolKind.OtherType)
        {
            return identity.QualifiedName;
        }

        var separator = identity.QualifiedName.IndexOf('.', StringComparison.Ordinal);
        return separator < 0 ? identity.QualifiedName : identity.QualifiedName[..separator];
    }
}
