using libpjsua2maui.mcp;

namespace libpjsua2maui.mcp.http;

/// <summary>
/// Provides health information for the HTTP MCP host.
/// </summary>
public sealed class HttpHostHealthCheckService : IMcpHostHealthCheckService
{
    internal static string FormatVersion(Version? version)
    {
        return version?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Gets the current HTTP host status and assembly version.
    /// </summary>
    public McpHostHealthCheckResult GetStatus()
    {
        var version = FormatVersion(typeof(HttpHostHealthCheckService).Assembly.GetName().Version);

        return new McpHostHealthCheckResult(
            Status: "Healthy",
            Transport: "http",
            Version: version);
    }
}
