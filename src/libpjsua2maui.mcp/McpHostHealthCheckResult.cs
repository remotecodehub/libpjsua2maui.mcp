namespace libpjsua2maui.mcp;

/// <summary>
/// Describes the health status and version of an MCP host.
/// </summary>
/// <param name="Status">The health status of the host.</param>
/// <param name="Transport">The transport provided by the host.</param>
/// <param name="Version">The assembly version of the host.</param>
public sealed record McpHostHealthCheckResult(
    string Status,
    string Transport,
    string Version);
