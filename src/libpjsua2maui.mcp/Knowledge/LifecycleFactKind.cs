namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Classifies lifecycle and interop facts without turning them into runtime rules.</summary>
public enum LifecycleFactKind
{
    Creation,
    Ownership,
    Handle,
    Disposal,
    NativeDestruction,
    CallbackRegistration,
    CallbackInvocation,
    CallbackReference,
    Dependency,
    InitializationOrder,
    ShutdownOrder,
    ThreadingConstraint,
    UseAfterDisposalRisk
}
