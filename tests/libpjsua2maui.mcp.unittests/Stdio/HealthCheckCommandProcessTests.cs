using System.Diagnostics;
using System.Text.Json;
using libpjsua2maui.mcp.tests.Stdio.TestSupport.Helpers;

namespace libpjsua2maui.mcp.tests.Stdio;

/// <summary>
/// Verifies the CLI entry point by launching the actual stdio assembly.
/// </summary>
public sealed class HealthCheckCommandProcessTests
{
    [Fact]
    public async Task HealthcheckCommand_ReturnsJsonStatusAndSuccessfulExitCode()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = StdioHostPaths.DotnetHostPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add(StdioHostPaths.AssemblyPath);
        startInfo.ArgumentList.Add("healthcheck");

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the stdio CLI process.");

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var standardOutputTask = process.StandardOutput.ReadToEndAsync(timeout.Token);
        var standardErrorTask = process.StandardError.ReadToEndAsync(timeout.Token);

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("The stdio healthcheck command did not exit within 20 seconds.");
        }

        var standardOutput = await standardOutputTask;
        var standardError = await standardErrorTask;

        Assert.True(
            process.ExitCode == 0,
            $"Expected exit code 0, received {process.ExitCode}. stderr: {standardError}");

        using var document = JsonDocument.Parse(standardOutput);
        var root = document.RootElement;

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.Equal("stdio", root.GetProperty("transport").GetString());
        Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("version").GetString()));
    }
}
