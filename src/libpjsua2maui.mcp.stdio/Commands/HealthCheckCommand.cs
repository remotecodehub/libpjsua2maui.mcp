using System.CommandLine;
using System.Text.Json;
using libpjsua2maui.mcp.stdio;

namespace libpjsua2maui.mcp.stdio.Commands;

/// <summary>
/// Creates the CLI healthcheck command for the stdio host.
/// </summary>
public sealed class HealthCheckCommand(StdioHealthCheckService healthCheckService)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    /// <summary>
    /// Creates a command that prints the stdio host status as JSON.
    /// </summary>
    public Command CreateCommand()
    {
        var command = new Command("healthcheck", "Returns the stdio host status and version.");

        command.SetAction(_ =>
        {
            var result = healthCheckService.GetStatus();
            Console.Out.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            return 0;
        });

        return command;
    }
}
