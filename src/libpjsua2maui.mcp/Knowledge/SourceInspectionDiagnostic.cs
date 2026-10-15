namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Describes a source construct that was present but could not be interpreted by the inspector.</summary>
public sealed record SourceInspectionDiagnostic
{
    public SourceInspectionDiagnostic(
        string artifactPath,
        string construct,
        string reason,
        SourceInspectionDiagnosticKind kind = SourceInspectionDiagnosticKind.Unsupported)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(construct);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArtifactPath = artifactPath;
        Construct = construct;
        Reason = reason;
        Kind = kind;
    }

    public string ArtifactPath { get; }
    public string Construct { get; }
    public string Reason { get; }
    public SourceInspectionDiagnosticKind Kind { get; }
}
