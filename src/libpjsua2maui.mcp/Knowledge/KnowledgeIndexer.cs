namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Indexes package observations using stable ordinal ordering and identity checks.</summary>
public sealed class KnowledgeIndexer : IKnowledgeIndexer
{
    public Task<IndexedKnowledgeSet> IndexAsync(PackageExtractionResult extraction, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(extraction);
        cancellationToken.ThrowIfCancellationRequested();

        if (extraction.Acquisition.Manifest.Status != KnowledgeManifestStatus.Validated)
        {
            throw new InvalidDataException("Only a validated acquisition can be indexed.");
        }

        var manifest = new KnowledgeManifest(
            extraction.Acquisition.Manifest.Identity,
            extraction.Acquisition.Manifest.Metadata,
            SortAndValidate(extraction.Acquisition.Manifest.Artifacts, ArtifactKey, ArtifactFingerprint, "artifact"),
            KnowledgeManifestStatus.Indexed);

        var symbols = SortAndValidate(extraction.Symbols, SymbolKey, SymbolFingerprint, "symbol");
        var documentation = SortAndValidate(extraction.Documentation, DocumentationKey, DocumentationFingerprint, "documentation");
        var unassociated = SortAndValidate(extraction.UnassociatedDocumentation, DocumentationKey, DocumentationFingerprint, "unassociated documentation");
        var associations = SortAndValidate(
            extraction.Associations,
            association => $"{DocumentationKey(association.Documentation)}|{SymbolKey(association.Symbol)}",
            association => $"{DocumentationFingerprint(association.Documentation)}|{SymbolFingerprint(association.Symbol)}",
            "documentation association");
        IReadOnlyList<KnowledgeRelationship> relationships = [];

        return Task.FromResult(new IndexedKnowledgeSet(manifest, symbols, documentation, associations, unassociated, relationships));
    }

    private static IReadOnlyList<T> SortAndValidate<T>(IEnumerable<T> values, Func<T, string> key, Func<T, string> fingerprint, string name)
    {
        var records = values.ToArray();
        var grouped = records.GroupBy(key, StringComparer.Ordinal);
        var result = new List<T>(records.Length);
        foreach (var group in grouped)
        {
            var distinct = group.Select(fingerprint).Distinct(StringComparer.Ordinal).ToArray();
            if (distinct.Length > 1)
            {
                throw new InvalidDataException($"Conflicting duplicate {name} identity '{group.Key}'.");
            }

            result.Add(group.First());
        }

        return result.OrderBy(key, StringComparer.Ordinal).ToArray();
    }

    private static string ArtifactKey(PackageArtifact item) => item.RelativePath;
    private static string ArtifactFingerprint(PackageArtifact item) => $"{item.Package.Id}|{item.PackageVersion}|{item.RelativePath}|{item.Kind}|{item.TargetFramework}|{item.RuntimeIdentifier}";
    private static string SymbolKey(SymbolRepresentation item) => $"{item.TargetFramework}|{item.Identity.Kind}|{item.Identity.QualifiedName}|{item.DocumentationId}|{item.Signature}";
    private static string SymbolFingerprint(SymbolRepresentation item) => $"{item.Package.Id}|{item.PackageVersion}|{SymbolKey(item)}|{string.Join(';', item.Parameters.Select(p => $"{p.Name}:{p.Type}:{p.IsByReference}"))}|{item.ReturnType}|{item.SourceArtifact?.RelativePath}";
    private static string DocumentationKey(DocumentationRecord item) => $"{item.TargetFramework}|{item.MemberId}|{item.SourceArtifact.RelativePath}";
    private static string DocumentationFingerprint(DocumentationRecord item) => $"{item.Package.Id}|{item.PackageVersion}|{DocumentationKey(item)}|{item.Content}";
}
