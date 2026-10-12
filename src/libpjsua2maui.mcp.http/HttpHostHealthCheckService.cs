using libpjsua2maui.mcp;

namespace libpjsua2maui.mcp.http;

/// <summary>
/// Provides health information for the HTTP MCP host.
/// </summary>
public sealed class HttpHostHealthCheckService : IMcpHostHealthCheckService
{
    /// <summary>
    /// Gets the current HTTP host status and assembly version.
    /// </summary>
    public McpHostHealthCheckResult GetStatus()
    {
        var version = typeof(HttpHostHealthCheckService).Assembly
            .GetName()
            .Version?
            .ToString() ?? "unknown";

        return new McpHostHealthCheckResult(
            Status: "Healthy",
            Transport: "http",
            Version: version);
    }
}
