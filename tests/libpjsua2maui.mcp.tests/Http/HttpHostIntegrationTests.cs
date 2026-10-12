using System.Net;
using System.Text.Json;
using libpjsua2maui.mcp.tests.Http.TestSupport.Fixtures;
using ModelContextProtocol.Client;

namespace libpjsua2maui.mcp.tests.Http;

/// <summary>
/// Exercises the HTTP host's REST health endpoint and MCP Streamable HTTP transport.
/// </summary>
public sealed class HttpHostIntegrationTests
{
    [Fact]
    public async Task HealthEndpoint_ReturnsStatusVersionAndChecks()
    {
        await using var host = await HttpHostProcess.StartAsync();

        using var response = await host.HttpClient.GetAsync("api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = document.RootElement;

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.True(root.GetProperty("version").GetString()?.Length > 0);
        Assert.True(root.GetProperty("totalDurationMs").GetDouble() >= 0);
        Assert.Equal(JsonValueKind.Array, root.GetProperty("checks").ValueKind);
    }

    [Fact]
    public async Task HttpMcpServer_InitializesAndInvokesHealthcheckTool()
    {
        await using var host = await HttpHostProcess.StartAsync();

        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = host.McpEndpoint,
            TransportMode = HttpTransportMode.StreamableHttp
        });

        await using var client = await McpClient.CreateAsync(transport);

        var tools = await client.ListToolsAsync();

        Assert.Contains(tools, tool => tool.Name == "healthcheck");

        var result = await client.CallToolAsync(
            "healthcheck",
            new Dictionary<string, object?>());

        var serializedResult = JsonSerializer.Serialize(result);

        Assert.Contains("Healthy", serializedResult);
        Assert.Contains("http", serializedResult);
        Assert.Contains("0.1.0.0", serializedResult);
    }
}
