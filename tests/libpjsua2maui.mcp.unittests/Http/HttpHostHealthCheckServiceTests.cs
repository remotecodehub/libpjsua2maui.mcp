using libpjsua2maui.mcp.http;

namespace libpjsua2maui.mcp.tests.Http;

public sealed class HttpHostHealthCheckServiceTests
{
    [Fact]
    public void GetStatus_ReturnsHttpTransportAndAssemblyVersion()
    {
        var result = new HttpHostHealthCheckService().GetStatus();

        Assert.Equal("Healthy", result.Status);
        Assert.Equal("http", result.Transport);
        Assert.False(string.IsNullOrWhiteSpace(result.Version));
    }

    [Theory]
    [InlineData(null, "unknown")]
    [InlineData("1.2.3.4", "1.2.3.4")]
    public void FormatVersion_CoversVersionFallback(string? version, string expected)
    {
        Assert.Equal(expected, HttpHostHealthCheckService.FormatVersion(version is null ? null : Version.Parse(version)));
    }
}
