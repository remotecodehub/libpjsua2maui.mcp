namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Contains repository mapping relationships together with reproducible coverage and parser diagnostics.</summary>
public sealed record RepositoryMappingInspectionResult(
    IReadOnlyList<KnowledgeRelationship> Relationships,
    SourceInspectionCoverage Coverage,
    IReadOnlyList<SourceInspectionDiagnostic> Diagnostics);
