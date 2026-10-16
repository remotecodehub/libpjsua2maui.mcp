using System.CommandLine;
using System.Text.Json;
using libpjsua2maui.mcp.stdio;
using libpjsua2maui.mcp.stdio.Commands;

namespace libpjsua2maui.mcp.tests.Stdio;

public sealed class HealthCheckCommandTests
{
    [Fact]
    public async Task CreateCommand_InvokesHealthcheckAndWritesJson()
    {
        var command = new HealthCheckCommand(new StdioHealthCheckService()).CreateCommand();
        var original = Console.Out;
        using var output = new StringWriter();
        Console.SetOut(output);
        try
        {
#pragma warning disable xUnit1051
            var exitCode = await command.Parse(Array.Empty<string>()).InvokeAsync();
#pragma warning restore xUnit1051
            Assert.Equal(0, exitCode);
        }
        finally
        {
            Console.SetOut(original);
        }

        using var document = JsonDocument.Parse(output.ToString());
        Assert.Equal("stdio", document.RootElement.GetProperty("transport").GetString());
    }
}
