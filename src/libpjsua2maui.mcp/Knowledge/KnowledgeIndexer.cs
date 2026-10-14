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

        var identity = extraction.Acquisition.Manifest.Identity;
        var sourceArtifacts = SortAndValidate(
            extraction.Acquisition.Manifest.Artifacts,
            ArtifactKey,
            ArtifactFingerprint,
            "artifact");
        var indexedArtifacts = sourceArtifacts
            .Select(artifact => WithArtifactProvenance(artifact, identity))
            .ToArray();
        var artifactMap = indexedArtifacts.ToDictionary(artifact => artifact.RelativePath, StringComparer.Ordinal);
        var manifest = new KnowledgeManifest(
            identity,
            extraction.Acquisition.Manifest.Metadata,
            indexedArtifacts,
            KnowledgeManifestStatus.Indexed);

        var symbols = SortAndValidate(extraction.Symbols, SymbolKey, SymbolFingerprint, "symbol")
            .Select(symbol => WithSymbolProvenance(symbol, identity, artifactMap))
            .ToArray();
        var symbolMap = symbols.ToDictionary(SymbolKey, StringComparer.Ordinal);
        var documentation = SortAndValidate(extraction.Documentation, DocumentationKey, DocumentationFingerprint, "documentation")
            .Select(document => WithDocumentationProvenance(document, identity, artifactMap))
            .ToArray();
        var documentationMap = documentation.ToDictionary(DocumentationKey, StringComparer.Ordinal);
        var unassociated = SortAndValidate(extraction.UnassociatedDocumentation, DocumentationKey, DocumentationFingerprint, "unassociated documentation")
            .Select(document => WithDocumentationProvenance(document, identity, artifactMap))
            .ToArray();
        var associations = SortAndValidate(
            extraction.Associations,
            association => $"{DocumentationKey(association.Documentation)}|{SymbolKey(association.Symbol)}",
            association => $"{DocumentationFingerprint(association.Documentation)}|{SymbolFingerprint(association.Symbol)}",
            "documentation association")
            .Select(association => new DocumentationAssociation(
                documentationMap[DocumentationKey(association.Documentation)],
                symbolMap[SymbolKey(association.Symbol)],
                new FactProvenance(
                    identity,
                    ProvenanceKind.PackageArtifact,
                    association.Documentation.SourceArtifact.RelativePath,
                    association.Symbol.Identity,
                    limitations: "The association is established from an exact XML documentation ID match.")))
            .ToArray();
        var relationships = SortAndValidate(
            extraction.Relationships,
            relationship => $"{relationship.RelationshipType}|{relationship.Source}|{relationship.Target}",
            relationship => $"{relationship.KnowledgeSet}|{relationship.RelationshipType}|{relationship.SourceNode}|{relationship.TargetNode}|{relationship.Status}|{relationship.Provenance}",
            "knowledge relationship");

        if (extraction.Relationships.Any(relationship => relationship.KnowledgeSet != identity) ||
            extraction.LifecycleFacts.Any(fact => fact.KnowledgeSet != identity) ||
            extraction.PlatformEvidence.Any(evidence => evidence.KnowledgeSet != identity))
        {
            throw new InvalidDataException("Enrichment facts must belong to the extracted knowledge set.");
        }

        var lifecycleFacts = SortAndValidate(
            extraction.LifecycleFacts,
            fact => $"{fact.Subject}|{fact.Kind}|{fact.Value}",
            fact => $"{fact.KnowledgeSet}|{fact.Subject}|{fact.Kind}|{fact.Status}|{fact.Value}|{fact.Provenance}",
            "lifecycle fact");

        var platformEvidence = SortAndValidate(
            extraction.PlatformEvidence,
            evidence => $"{evidence.Subject}|{evidence.Platform}|{evidence.TargetFramework}|{evidence.RuntimeIdentifier}",
            evidence => $"{evidence.KnowledgeSet}|{evidence.Subject}|{evidence.Platform}|{evidence.TargetFramework}|{evidence.RuntimeIdentifier}|{evidence.ManagedAssembly}|{evidence.NativeArtifact}|{evidence.Confidence}|{evidence.Provenance}|{evidence.Limitations}",
            "platform evidence");

        return Task.FromResult(new IndexedKnowledgeSet(manifest, symbols, documentation, associations, unassociated, relationships, lifecycleFacts, platformEvidence));
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

    private static PackageArtifact WithArtifactProvenance(PackageArtifact artifact, KnowledgeSetIdentity identity)
    {
        return new PackageArtifact(
            artifact.Package,
            artifact.PackageVersion,
            artifact.RelativePath,
            artifact.Kind,
            artifact.TargetFramework,
            artifact.RuntimeIdentifier,
            new FactProvenance(identity, ProvenanceKind.PackageArtifact, artifact.RelativePath));
    }

    private static SymbolRepresentation WithSymbolProvenance(
        SymbolRepresentation symbol,
        KnowledgeSetIdentity identity,
        IReadOnlyDictionary<string, PackageArtifact> artifacts)
    {
        var artifact = symbol.SourceArtifact is null ? null : artifacts[symbol.SourceArtifact.RelativePath];
        return new SymbolRepresentation(
            symbol.Package,
            symbol.PackageVersion,
            symbol.Identity,
            symbol.TargetFramework,
            artifact,
            symbol.DocumentationId,
            symbol.Signature,
            symbol.Parameters,
            symbol.ReturnType,
            new FactProvenance(
                identity,
                ProvenanceKind.PackageArtifact,
                artifact?.RelativePath,
                symbol.Identity,
                limitations: "Managed metadata does not by itself establish native or platform availability."));
    }

    private static DocumentationRecord WithDocumentationProvenance(
        DocumentationRecord documentation,
        KnowledgeSetIdentity identity,
        IReadOnlyDictionary<string, PackageArtifact> artifacts)
    {
        var artifact = artifacts[documentation.SourceArtifact.RelativePath];
        return new DocumentationRecord(
            documentation.Package,
            documentation.PackageVersion,
            documentation.MemberId,
            documentation.Content,
            artifact,
            documentation.TargetFramework,
            new FactProvenance(identity, ProvenanceKind.PackageArtifact, artifact.RelativePath));
    }
}
