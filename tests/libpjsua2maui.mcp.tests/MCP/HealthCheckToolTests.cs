using libpjsua2maui.mcp;

namespace libpjsua2maui.mcp.tests.MCP;

public sealed class HealthCheckToolTests
{
    [Fact]
    public void GetHealthCheck_DelegatesToTheConfiguredService()
    {
        var expected = new McpHostHealthCheckResult("Healthy", "test", "1.0");
        var tool = new HealthCheckTool(new StubHealthCheckService(expected));

        Assert.Equal(expected, tool.GetHealthCheck());
    }

    private sealed class StubHealthCheckService(McpHostHealthCheckResult result) : IMcpHostHealthCheckService
    {
        public McpHostHealthCheckResult GetStatus()
        {
            return result;
        }
    }
}
