namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Deterministic denominators and resolutions observed while inspecting generated bindings.</summary>
public sealed record SourceInspectionCoverage
{
    public int ManagedTypesDiscovered { get; init; }
    public int ManagedMembersDiscovered { get; init; }
    public int ManagedMembersWithSwigCalls { get; init; }
    public int SwigCallsDiscovered { get; init; }
    public int SwigCallsResolvedToPInvoke { get; init; }
    public int PInvokeDeclarationsDiscovered { get; init; }
    public int PInvokeDeclarationsResolvedToWrapper { get; init; }
    public int WrapperDefinitionsDiscovered { get; init; }
    public int WrapperInvocationsDiscovered { get; init; }
    public int WrapperInvocationsResolvedToPjprojectDeclaration { get; init; }
    public int PjprojectDeclarationsResolvedToImplementation { get; init; }

    public SourceInspectionCoverage Add(SourceInspectionCoverage other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return this with
        {
            ManagedTypesDiscovered = ManagedTypesDiscovered + other.ManagedTypesDiscovered,
            ManagedMembersDiscovered = ManagedMembersDiscovered + other.ManagedMembersDiscovered,
            ManagedMembersWithSwigCalls = ManagedMembersWithSwigCalls + other.ManagedMembersWithSwigCalls,
            SwigCallsDiscovered = SwigCallsDiscovered + other.SwigCallsDiscovered,
            SwigCallsResolvedToPInvoke = SwigCallsResolvedToPInvoke + other.SwigCallsResolvedToPInvoke,
            PInvokeDeclarationsDiscovered = PInvokeDeclarationsDiscovered + other.PInvokeDeclarationsDiscovered,
            PInvokeDeclarationsResolvedToWrapper = PInvokeDeclarationsResolvedToWrapper + other.PInvokeDeclarationsResolvedToWrapper,
            WrapperDefinitionsDiscovered = WrapperDefinitionsDiscovered + other.WrapperDefinitionsDiscovered,
            WrapperInvocationsDiscovered = WrapperInvocationsDiscovered + other.WrapperInvocationsDiscovered,
            WrapperInvocationsResolvedToPjprojectDeclaration = WrapperInvocationsResolvedToPjprojectDeclaration + other.WrapperInvocationsResolvedToPjprojectDeclaration,
            PjprojectDeclarationsResolvedToImplementation = PjprojectDeclarationsResolvedToImplementation + other.PjprojectDeclarationsResolvedToImplementation
        };
    }
}
