using System.Collections.ObjectModel;

namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Contains assembly and XML documentation observations from one acquired package.</summary>
public sealed record PackageExtractionResult
{
    public PackageExtractionResult(
        PackageAcquisitionResult acquisition,
        IReadOnlyList<SymbolRepresentation> symbols,
        IReadOnlyList<DocumentationRecord> documentation,
        IReadOnlyList<DocumentationAssociation> associations,
        IReadOnlyList<DocumentationRecord> unassociatedDocumentation,
        IReadOnlyList<KnowledgeRelationship>? relationships = null,
        IReadOnlyList<LifecycleFact>? lifecycleFacts = null,
        IReadOnlyList<PlatformEvidence>? platformEvidence = null,
        SourceInspectionCoverage? sourceCoverage = null,
        IReadOnlyList<SourceInspectionDiagnostic>? sourceDiagnostics = null,
        PackageSymbolInventory? inventory = null,
        MetadataEnumerationCoverage? metadataEnumerationCoverage = null)
    {
        Acquisition = acquisition ?? throw new ArgumentNullException(nameof(acquisition));
        Symbols = Array.AsReadOnly((symbols ?? throw new ArgumentNullException(nameof(symbols))).ToArray());
        Documentation = Array.AsReadOnly((documentation ?? throw new ArgumentNullException(nameof(documentation))).ToArray());
        Associations = Array.AsReadOnly((associations ?? throw new ArgumentNullException(nameof(associations))).ToArray());
        UnassociatedDocumentation = Array.AsReadOnly((unassociatedDocumentation ?? throw new ArgumentNullException(nameof(unassociatedDocumentation))).ToArray());
        Relationships = Array.AsReadOnly((relationships ?? []).ToArray());
        LifecycleFacts = Array.AsReadOnly((lifecycleFacts ?? []).ToArray());
        PlatformEvidence = Array.AsReadOnly((platformEvidence ?? []).ToArray());
        SourceCoverage = sourceCoverage ?? new SourceInspectionCoverage();
        SourceDiagnostics = Array.AsReadOnly((sourceDiagnostics ?? []).ToArray());
        MetadataEnumerationCoverage = metadataEnumerationCoverage;
        Inventory = inventory ?? PackageSymbolInventoryBuilder.Build(this);
        Errors = new ReadOnlyCollection<string>([]);
    }

    public PackageAcquisitionResult Acquisition { get; }
    public IReadOnlyList<SymbolRepresentation> Symbols { get; }
    public IReadOnlyList<DocumentationRecord> Documentation { get; }
    public IReadOnlyList<DocumentationAssociation> Associations { get; }
    public IReadOnlyList<DocumentationRecord> UnassociatedDocumentation { get; }
    public IReadOnlyList<KnowledgeRelationship> Relationships { get; }
    public IReadOnlyList<LifecycleFact> LifecycleFacts { get; }
    public IReadOnlyList<PlatformEvidence> PlatformEvidence { get; }
    public SourceInspectionCoverage SourceCoverage { get; }
    public IReadOnlyList<SourceInspectionDiagnostic> SourceDiagnostics { get; }
    public MetadataEnumerationCoverage? MetadataEnumerationCoverage { get; }
    public IReadOnlyList<string> Errors { get; }
    public PackageSymbolInventory Inventory { get; }
}
