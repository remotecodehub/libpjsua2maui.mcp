using System.Text.Json;
using libpjsua2maui.mcp.tests.Stdio.TestSupport.Helpers;
using ModelContextProtocol.Client;

namespace libpjsua2maui.mcp.tests.Stdio;

/// <summary>
/// Verifies MCP initialization, tool discovery, and tool invocation over the real stdio process.
/// </summary>
public sealed class StdioMcpProtocolIntegrationTests
{
    [Fact]
    public async Task StdioServer_InitializesAndInvokesHealthcheckTool()
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "stdio integration test",
            Command = StdioHostPaths.DotnetHostPath,
            Arguments = [StdioHostPaths.AssemblyPath],
            InheritEnvironmentVariables = true
        });

        await using var client = await McpClient.CreateAsync(transport);

        var tools = await client.ListToolsAsync();

        Assert.Contains(tools, tool => tool.Name == "healthcheck");

        var result = await client.CallToolAsync(
            "healthcheck",
            new Dictionary<string, object?>());

        var serializedResult = JsonSerializer.Serialize(result);

        Assert.Contains("Healthy", serializedResult);
        Assert.Contains("stdio", serializedResult);
        Assert.Contains("0.1.0.0", serializedResult);
    }
}
