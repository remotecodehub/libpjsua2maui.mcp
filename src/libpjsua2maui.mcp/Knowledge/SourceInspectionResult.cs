namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Contains source symbols and evidence-backed facts found in one package source artifact.</summary>
public sealed record SourceInspectionResult(
    PackageArtifact Artifact,
    IReadOnlyList<string> Symbols,
    IReadOnlyList<KnowledgeRelationship> Relationships,
    IReadOnlyList<LifecycleFact> LifecycleFacts,
    FactProvenance Provenance);
