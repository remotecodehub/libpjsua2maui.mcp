using libpjsua2maui.mcp;

namespace libpjsua2maui.mcp.stdio;

/// <summary>
/// Provides host-level health information for the stdio executable.
/// </summary>
public sealed class StdioHealthCheckService : IMcpHostHealthCheckService
{
    /// <summary>
    /// Gets the current host status and its version.
    /// </summary>
    public McpHostHealthCheckResult GetStatus()
    {
        var version = typeof(StdioHealthCheckService).Assembly
            .GetName()
            .Version?
            .ToString() ?? "unknown";

        return new McpHostHealthCheckResult(
            Status: "Healthy",
            Transport: "stdio",
            Version: version);
    }
}
