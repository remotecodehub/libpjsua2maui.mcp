namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Identifies a validated package artifact stored locally.</summary>
public sealed record PackageAcquisitionResult
{
    public PackageAcquisitionResult(string artifactPath, KnowledgeManifest manifest, PackageDocumentationValidation documentation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactPath);
        ArtifactPath = Path.GetFullPath(artifactPath);
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        Documentation = documentation ?? throw new ArgumentNullException(nameof(documentation));
    }

    public string ArtifactPath { get; }

    public KnowledgeManifest Manifest { get; }

    public PackageDocumentationValidation Documentation { get; }
}
