namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Extracts managed metadata and XML documentation from one validated package.</summary>
public interface IPackageKnowledgeExtractor
{
    Task<PackageExtractionResult> ExtractAsync(PackageAcquisitionResult acquisition, CancellationToken cancellationToken = default);
}
