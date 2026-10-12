namespace libpjsua2maui.mcp;

/// <summary>
/// Provides health information for an MCP host.
/// </summary>
public interface IMcpHostHealthCheckService
{
    /// <summary>
    /// Gets the current host status and version.
    /// </summary>
    /// <returns>A structured snapshot of the host health information.</returns>
    McpHostHealthCheckResult GetStatus();
}
