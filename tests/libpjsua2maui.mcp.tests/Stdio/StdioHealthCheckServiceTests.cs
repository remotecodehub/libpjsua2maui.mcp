using libpjsua2maui.mcp.stdio;

namespace libpjsua2maui.mcp.tests.Stdio;

public sealed class StdioHealthCheckServiceTests
{
    [Fact]
    public void GetStatus_ReturnsHealthyStdioStatusAndVersion()
    {
        var service = new StdioHealthCheckService();

        var result = service.GetStatus();

        Assert.Equal("Healthy", result.Status);
        Assert.Equal("stdio", result.Transport);
        Assert.False(string.IsNullOrWhiteSpace(result.Version));
    }
}
