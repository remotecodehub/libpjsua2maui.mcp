using System.Collections.ObjectModel;

namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Contains the complete, deterministic knowledge observed for one package version.</summary>
public sealed record IndexedKnowledgeSet
{
    public IndexedKnowledgeSet(
        KnowledgeManifest manifest,
        IReadOnlyList<SymbolRepresentation> symbols,
        IReadOnlyList<DocumentationRecord> documentation,
        IReadOnlyList<DocumentationAssociation> associations,
        IReadOnlyList<DocumentationRecord> unassociatedDocumentation,
        IReadOnlyList<KnowledgeRelationship>? relationships = null,
        IReadOnlyList<LifecycleFact>? lifecycleFacts = null,
        IReadOnlyList<PlatformEvidence>? platformEvidence = null,
        PackageSymbolInventory? inventory = null)
    {
        Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));
        Symbols = Copy(symbols, nameof(symbols));
        Documentation = Copy(documentation, nameof(documentation));
        Associations = Copy(associations, nameof(associations));
        UnassociatedDocumentation = Copy(unassociatedDocumentation, nameof(unassociatedDocumentation));
        Relationships = Copy(relationships ?? [], nameof(relationships));
        LifecycleFacts = Copy(lifecycleFacts ?? [], nameof(lifecycleFacts));
        PlatformEvidence = Copy(platformEvidence ?? [], nameof(platformEvidence));
        Inventory = inventory ?? PackageSymbolInventoryBuilder.Build(new PackageExtractionResult(
            new PackageAcquisitionResult(manifest.Artifacts.FirstOrDefault()?.RelativePath ?? "indexed-knowledge", manifest, new PackageDocumentationValidation(manifest.PackageVersion, new Dictionary<string, string>(), [], [])),
            Symbols, Documentation, Associations, UnassociatedDocumentation, Relationships, LifecycleFacts, PlatformEvidence));

        if (manifest.Status != KnowledgeManifestStatus.Indexed)
        {
            throw new ArgumentException("An indexed knowledge set requires an Indexed manifest.", nameof(manifest));
        }
    }

    public KnowledgeManifest Manifest { get; }
    public IReadOnlyList<SymbolRepresentation> Symbols { get; }
    public IReadOnlyList<DocumentationRecord> Documentation { get; }
    public IReadOnlyList<DocumentationAssociation> Associations { get; }
    public IReadOnlyList<DocumentationRecord> UnassociatedDocumentation { get; }
    public IReadOnlyList<KnowledgeRelationship> Relationships { get; }
    public IReadOnlyList<LifecycleFact> LifecycleFacts { get; }
    public IReadOnlyList<PlatformEvidence> PlatformEvidence { get; }
    public PackageSymbolInventory Inventory { get; }

    private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> values, string parameterName)
    {
        return new ReadOnlyCollection<T>((values ?? throw new ArgumentNullException(parameterName)).ToArray());
    }
}
