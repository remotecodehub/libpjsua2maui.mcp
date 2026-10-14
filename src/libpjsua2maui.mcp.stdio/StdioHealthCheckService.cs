using libpjsua2maui.mcp;

namespace libpjsua2maui.mcp.stdio;

/// <summary>
/// Provides host-level health information for the stdio executable.
/// </summary>
public sealed class StdioHealthCheckService : IMcpHostHealthCheckService
{
    internal static string FormatVersion(Version? version)
    {
        return version?.ToString() ?? "unknown";
    }

    /// <summary>
    /// Gets the current host status and its version.
    /// </summary>
    public McpHostHealthCheckResult GetStatus()
    {
        var version = FormatVersion(typeof(StdioHealthCheckService).Assembly.GetName().Version);

        return new McpHostHealthCheckResult(
            Status: "Healthy",
            Transport: "stdio",
            Version: version);
    }
}
