namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Builds a complete deterministic knowledge set from validated extraction results.</summary>
public interface IKnowledgeIndexer
{
    Task<IndexedKnowledgeSet> IndexAsync(PackageExtractionResult extraction, CancellationToken cancellationToken = default);
}
