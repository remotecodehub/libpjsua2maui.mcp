namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Persists and loads indexed knowledge without package acquisition.</summary>
public interface IKnowledgeStore
{
    Task SaveAsync(string path, IndexedKnowledgeSet knowledge, CancellationToken cancellationToken = default);
    Task<IndexedKnowledgeSet> LoadAsync(string path, CancellationToken cancellationToken = default);
}
