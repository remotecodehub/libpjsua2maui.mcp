using System.ComponentModel;
using ModelContextProtocol.Server;

namespace libpjsua2maui.mcp.stdio;

/// <summary>
/// Exposes stdio host health information to connected MCP clients.
/// </summary>
[McpServerToolType]
public sealed class StdioHealthCheckTool(StdioHealthCheckService healthCheckService)
{
    /// <summary>
    /// Returns the stdio host status and assembly version.
    /// </summary>
    [McpServerTool(Name = "healthcheck")]
    [Description("Returns the health status, transport, and version of the local stdio MCP host.")]
    public StdioHealthCheckResult GetHealthCheck()
    {
        return healthCheckService.GetStatus();
    }
}
