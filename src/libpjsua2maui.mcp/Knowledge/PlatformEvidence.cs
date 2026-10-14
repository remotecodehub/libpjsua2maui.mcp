namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Records managed and native package evidence for one platform/TFM/RID observation.</summary>
public sealed record PlatformEvidence
{
    public PlatformEvidence(
        KnowledgeSetIdentity knowledgeSet,
        KnowledgeNodeReference subject,
        KnowledgePlatform platform,
        string? targetFramework,
        string? runtimeIdentifier,
        PlatformArtifactStatus managedAssembly,
        PlatformArtifactStatus nativeArtifact,
        KnowledgeConfidence confidence,
        FactProvenance? provenance = null,
        string? limitations = null)
    {
        KnowledgeSet = knowledgeSet ?? throw new ArgumentNullException(nameof(knowledgeSet));
        Subject = subject ?? throw new ArgumentNullException(nameof(subject));
        Platform = platform;
        TargetFramework = Optional(targetFramework, nameof(targetFramework));
        RuntimeIdentifier = Optional(runtimeIdentifier, nameof(runtimeIdentifier));
        ManagedAssembly = managedAssembly;
        NativeArtifact = nativeArtifact;
        Confidence = confidence;
        Provenance = provenance;
        Limitations = Optional(limitations, nameof(limitations));

        if (provenance is not null && provenance.KnowledgeSet != knowledgeSet)
        {
            throw new ArgumentException("Platform provenance must belong to the same knowledge set.", nameof(provenance));
        }
    }

    public KnowledgeSetIdentity KnowledgeSet { get; }
    public KnowledgeNodeReference Subject { get; }
    public KnowledgePlatform Platform { get; }
    public string? TargetFramework { get; }
    public string? RuntimeIdentifier { get; }
    public PlatformArtifactStatus ManagedAssembly { get; }
    public PlatformArtifactStatus NativeArtifact { get; }
    public KnowledgeConfidence Confidence { get; }
    public FactProvenance? Provenance { get; }
    public string? Limitations { get; }

    private static string? Optional(string? value, string parameterName)
    {
        if (value is null) return null;
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Value must contain text when supplied.", parameterName);
        return value;
    }
}
