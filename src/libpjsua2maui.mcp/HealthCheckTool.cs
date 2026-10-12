using System.ComponentModel;
using ModelContextProtocol.Server;

namespace libpjsua2maui.mcp;

/// <summary>
/// Exposes host health information to connected MCP clients.
/// </summary>
[McpServerToolType]
public sealed class HealthCheckTool(IMcpHostHealthCheckService healthCheckService)
{
    /// <summary>
    /// Returns the host status, transport, and assembly version.
    /// </summary>
    [McpServerTool(Name = "healthcheck")]
    [Description("Returns the health status, transport, and version of this MCP host.")]
    public McpHostHealthCheckResult GetHealthCheck()
    {
        return healthCheckService.GetStatus();
    }
}
