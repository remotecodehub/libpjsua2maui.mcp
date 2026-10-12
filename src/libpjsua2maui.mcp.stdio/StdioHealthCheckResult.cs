namespace libpjsua2maui.mcp.stdio;


/// <summary>
/// Describes the status of the local stdio host.
/// </summary>
/// <param name="Status">The health status of the host.</param>
/// <param name="Transport">The transport provided by the host.</param>
/// <param name="Version">The assembly version of the stdio host.</param>
public sealed record StdioHealthCheckResult(
    string Status,
    string Transport,
    string Version);
