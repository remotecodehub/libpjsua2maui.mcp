using System.Reflection;

namespace libpjsua2maui.mcp.stdio;

/// <summary>
/// Provides host-level health information for the stdio executable.
/// </summary>
public sealed class StdioHealthCheckService
{
    /// <summary>
    /// Gets the current host status and its version.
    /// </summary>
    public StdioHealthCheckResult GetStatus()
    {
        var version = typeof(StdioHealthCheckService).Assembly
            .GetName()
            .Version?
            .ToString() ?? "unknown";

        return new StdioHealthCheckResult(
            Status: "Healthy",
            Transport: "stdio",
            Version: version);
    }
}
